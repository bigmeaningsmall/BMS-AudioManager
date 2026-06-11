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