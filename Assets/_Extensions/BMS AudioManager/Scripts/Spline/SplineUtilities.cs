using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

/// <summary>
/// Static utility class for common spline operations
/// </summary>
public static class SplineUtilities
{
    /// <summary>
    /// Finds the closest point on a spline to a target position
    /// </summary>
    /// <param name="splineContainer">The spline to search</param>
    /// <param name="targetPosition">The position to find closest point to</param>
    /// <param name="sampleCount">Number of samples to check (higher = more accurate)</param>
    /// <returns>Normalized position (0-1) along the spline</returns>
    public static float FindClosestPoint(SplineContainer splineContainer, Vector3 targetPosition, int sampleCount = 50)
    {
        if (splineContainer?.Spline == null) return 0f;

        float closestDistance = float.MaxValue;
        float closestT = 0f;

        for (int i = 0; i <= sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            Vector3 pointOnSpline = splineContainer.EvaluatePosition(t);
            float distance = Vector3.Distance(pointOnSpline, targetPosition);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestT = t;
            }
        }

        return closestT;
    }

    /// <summary>
    /// Shortest signed step from <paramref name="from"/> to <paramref name="to"/> (both normalised 0-1).
    /// On a closed spline 0 and 1 are the same point (the first knot), so the step wraps through that
    /// seam when that's shorter: 0.95 -> 0.05 is +0.10, not -0.90. Smoothing with a plain Lerp on t
    /// instead sends the follower the long way round whenever the target crosses the seam.
    /// </summary>
    public static float ShortestDelta(float from, float to, bool closed)
    {
        float delta = to - from;
        if (closed) delta = Mathf.Repeat(delta + 0.5f, 1f) - 0.5f;
        return delta;
    }

    /// <summary>
    /// Moves <paramref name="current"/> a fraction (0-1) of the way toward <paramref name="target"/>
    /// along the shortest route - the wrap-aware replacement for Mathf.Lerp on a spline position.
    /// </summary>
    public static float StepTowards(float current, float target, float fraction, bool closed)
    {
        float t = current + ShortestDelta(current, target, closed) * Mathf.Clamp01(fraction);
        return closed ? Mathf.Repeat(t, 1f) : Mathf.Clamp01(t);
    }

    // Reused sample buffer for FindClosestPointNear (avoids a per-frame allocation).
    private static float[] s_sampleDistances = new float[0];

    /// <summary>
    /// Finds the closest point on the spline to <paramref name="targetPosition"/>, preferring the part of
    /// the spline the follower is already on when two parts are about equally close (a U-bend, a figure-8,
    /// a player standing between two stretches). Without this the target can flip between the two parts
    /// frame to frame and the follower swings back and forth.
    /// </summary>
    /// <param name="currentT">Where the follower currently is (normalised 0-1).</param>
    /// <param name="sampleCount">Coarse samples along the spline; the best one is then refined locally.</param>
    /// <param name="tieTolerance">How close (as a fraction of the best distance) another part must be to count as a tie.</param>
    /// <returns>Normalised position (0-1) along the spline.</returns>
    public static float FindClosestPointNear(SplineContainer splineContainer, Vector3 targetPosition, float currentT,
                                             int sampleCount = 50, float tieTolerance = 0.05f)
    {
        if (splineContainer?.Spline == null) return currentT;
        return FindClosestPointNear(t => splineContainer.EvaluatePosition(t), splineContainer.Spline.Closed,
                                    targetPosition, currentT, sampleCount, tieTolerance);
    }

    /// <summary>
    /// Path-agnostic core of <see cref="FindClosestPointNear(SplineContainer, Vector3, float, int, float)"/>:
    /// <paramref name="evaluate"/> maps a normalised t (0-1) to a world position.
    /// </summary>
    public static float FindClosestPointNear(System.Func<float, Vector3> evaluate, bool closed, Vector3 targetPosition,
                                             float currentT, int sampleCount = 50, float tieTolerance = 0.05f)
    {
        sampleCount = Mathf.Max(8, sampleCount);

        // A closed spline's t=1 is the same point as t=0, so sample [0,1) for closed and [0,1] for open.
        int count = closed ? sampleCount : sampleCount + 1;
        if (s_sampleDistances.Length < count) s_sampleDistances = new float[count];

        float best = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            float d = Vector3.Distance(evaluate((float)i / sampleCount), targetPosition);
            s_sampleDistances[i] = d;
            if (d < best) best = d;
        }

        // Every local minimum within tolerance of the best is a candidate; take the one nearest the follower.
        float tieLimit = best + Mathf.Max(best * tieTolerance, 0.01f);
        int chosen = -1;
        float chosenGap = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            float d = s_sampleDistances[i];
            if (d > tieLimit) continue;

            float prev = (i > 0) ? s_sampleDistances[i - 1] : (closed ? s_sampleDistances[count - 1] : float.MaxValue);
            float next = (i < count - 1) ? s_sampleDistances[i + 1] : (closed ? s_sampleDistances[0] : float.MaxValue);
            if (d > prev || d > next) continue; // not a local minimum

            float gap = Mathf.Abs(ShortestDelta(currentT, (float)i / sampleCount, closed));
            if (gap < chosenGap)
            {
                chosenGap = gap;
                chosen = i;
            }
        }
        if (chosen < 0) return currentT; // shouldn't happen - the global best is always a local minimum

        // Refine between the neighbouring samples with a ternary search (the distance is unimodal there).
        float step = 1f / sampleCount;
        float lo = (float)chosen / sampleCount - step;
        float hi = (float)chosen / sampleCount + step;
        if (!closed) { lo = Mathf.Max(lo, 0f); hi = Mathf.Min(hi, 1f); }

        for (int iteration = 0; iteration < 12; iteration++)
        {
            float m1 = lo + (hi - lo) / 3f;
            float m2 = hi - (hi - lo) / 3f;
            float d1 = Vector3.Distance(evaluate(WrapT(m1, closed)), targetPosition);
            float d2 = Vector3.Distance(evaluate(WrapT(m2, closed)), targetPosition);
            if (d1 < d2) hi = m2; else lo = m1;
        }

        return WrapT((lo + hi) * 0.5f, closed);
    }

    private static float WrapT(float t, bool closed) => closed ? Mathf.Repeat(t, 1f) : Mathf.Clamp01(t);

    /// <summary>
    /// Gets the distance from a point to the closest point on a spline
    /// </summary>
    public static float GetDistanceToSpline(SplineContainer splineContainer, Vector3 point, int sampleCount = 20)
    {
        if (splineContainer?.Spline == null) return float.MaxValue;

        float closestDistance = float.MaxValue;

        for (int i = 0; i <= sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            Vector3 pointOnSpline = splineContainer.EvaluatePosition(t);
            float distance = Vector3.Distance(pointOnSpline, point);

            if (distance < closestDistance)
            {
                closestDistance = distance;
            }
        }

        return closestDistance;
    }

    /// <summary>
    /// Checks if a point is inside a closed spline using ray casting
    /// </summary>
    public static bool IsPointInsideClosedSpline(SplineContainer splineContainer, Vector3 point, int polygonSamples = 50)
    {
        if (splineContainer?.Spline == null || !splineContainer.Spline.Closed)
            return false;

        int intersectionCount = 0;
        float rayLength = 1000f;

        // Sample points along the spline to form polygon
        Vector3[] splinePoints = new Vector3[polygonSamples];
        for (int i = 0; i < polygonSamples; i++)
        {
            float t = (float)i / polygonSamples;
            splinePoints[i] = splineContainer.EvaluatePosition(t);
        }

        // Cast ray and count intersections
        Vector3 rayStart = point;
        Vector3 rayEnd = point + new Vector3(rayLength, 0, 0);

        for (int i = 0; i < splinePoints.Length; i++)
        {
            Vector3 p1 = splinePoints[i];
            Vector3 p2 = splinePoints[(i + 1) % splinePoints.Length];

            if (RayIntersectsLineSegment2D(rayStart, rayEnd, p1, p2))
            {
                intersectionCount++;
            }
        }

        return (intersectionCount % 2) == 1;
    }

    /// <summary>
    /// Applies offset to a spline position using local coordinate system
    /// </summary>
    public static Vector3 ApplyOffsetToSplinePosition(SplineContainer splineContainer, Vector3 splinePosition, float t, Vector3 offset)
    {
        if (offset == Vector3.zero || splineContainer?.Spline == null) 
            return splinePosition;

        // Option 1: Convert float3 to Vector3
        float3 directionFloat3 = splineContainer.EvaluateTangent(t);
        float3 normalizedDirFloat3 = math.normalize(directionFloat3);
        Vector3 splineDirection = new Vector3(normalizedDirFloat3.x, normalizedDirFloat3.y, normalizedDirFloat3.z);
        
        // Continue with Vector3 operations
        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(up, splineDirection).normalized;
        up = Vector3.Cross(splineDirection, right).normalized;

        return splinePosition +
           right * offset.x +
           up * offset.y +
           splineDirection * offset.z;
    }

    /// <summary>
    /// Gets approximate bounds of a spline for quick distance checks
    /// </summary>
    public static Bounds GetApproximateSplineBounds(SplineContainer splineContainer, int boundsSamples = 10)
    {
        if (splineContainer?.Spline == null) 
            return new Bounds();

        Vector3 firstPoint = splineContainer.EvaluatePosition(0);
        Bounds bounds = new Bounds(firstPoint, Vector3.zero);

        for (int i = 1; i <= boundsSamples; i++)
        {
            float t = (float)i / boundsSamples;
            bounds.Encapsulate(splineContainer.EvaluatePosition(t));
        }

        return bounds;
    }

    private static bool RayIntersectsLineSegment2D(Vector3 rayStart, Vector3 rayEnd, Vector3 lineP1, Vector3 lineP2)
    {
        // Project to 2D (XZ plane)
        Vector2 rayStart2D = new Vector2(rayStart.x, rayStart.z);
        Vector2 lineP12D = new Vector2(lineP1.x, lineP1.z);
        Vector2 lineP22D = new Vector2(lineP2.x, lineP2.z);

        if ((lineP12D.y > rayStart2D.y && lineP22D.y <= rayStart2D.y) ||
            (lineP22D.y > rayStart2D.y && lineP12D.y <= rayStart2D.y))
        {
            float intersectX = lineP12D.x + (rayStart2D.y - lineP12D.y) * (lineP22D.x - lineP12D.x) / (lineP22D.y - lineP12D.y);
            return intersectX >= rayStart2D.x;
        }

        return false;
    }
}