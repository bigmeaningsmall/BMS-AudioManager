using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer))]
public class SplineAudioFollower : MonoBehaviour
{
    [Header("Tracking Settings")]
    [Tooltip("Maximum distance at which the audio will track along the spline")]
    [Range(1f, 50f)]
    public float proximityThreshold = 10f;
    
    [Tooltip("How quickly the audio object moves along the spline")]
    [Range(0.1f, 10f)]
    public float movementSpeed = 5f;
    
    [Tooltip("Smoothing factor for audio movement (higher = smoother but less responsive)")]
    [Range(0f, 0.95f)]
    public float smoothing = 0.7f;
    
    [Header("Optimisation")]
    [Tooltip("Distance beyond which the follower will enter sleep mode (should be greater than proximityThreshold)")]
    [Range(10f, 200f)]
    public float sleepThreshold = 30f;
    
    [Tooltip("How often to check if we should wake up when sleeping (in seconds)")]
    [Range(0.1f, 5f)]
    public float sleepCheckInterval = 0.5f;
    
    [Header("Closed Spline Settings")]
    [Tooltip("When inside a closed spline, position audio at listener with this offset")]
    public Vector3 insideListenerOffset = new Vector3(0, 0, 0);
    
    [Tooltip("Smoothing factor when transitioning from outside to inside closed spline")]
    [Range(0f, 0.95f)]
    public float insideTransitionSmoothing = 0.7f;
    
    [Tooltip("Smoothing factor when transitioning from inside to outside closed spline")]
    [Range(0f, 0.95f)]
    public float outsideTransitionSmoothing = 0.7f;
    
    [Tooltip("Maximum time for transition when exiting a closed spline (seconds)")]
    [Range(0.1f, 3.0f)]
    public float exitTransitionTime = 0.5f;

    [Header("References")]
    [Tooltip("The child GameObject containing the AudioSource (optional, will find automatically if not set)")]
    public GameObject audioObject;

    [Header("Debug")]
    public bool showDebugVisuals = true;
    public Color debugColor = Color.green;
    
    [Tooltip("Show sample points along the spline for visualization")]
    public bool showSamplePoints = false;
    
    [Tooltip("Number of sample points to check when finding proximity to spline")]
    [Range(10, 100)]
    public int splineSampleCount = 20;

    // Private references
    private SplineContainer splineContainer;
    private AudioSource audioSource;
    private Transform listenerTransform;
    private float currentSplinePosition = 0f; // 0-1 normalized position along spline
    private bool isInitialized = false;
    private float closestDistanceToSpline = float.MaxValue;
    private Vector3 closestPointOnSpline = Vector3.zero;
    
    // Sleep mode variables
    private bool isSleeping = false;
    private float nextSleepCheckTime = 0f;
    
    // Closed spline variables
    private bool isInsideClosedSpline = false;
    private bool isTransitioningOutside = false;
    private Vector3 targetPosition = Vector3.zero;
    private Vector3 exitStartPosition = Vector3.zero;
    private Vector3 exitTargetPosition = Vector3.zero;
    private float exitTransitionProgress = 0f;

    private void Awake()
    {
        // Get the SplineContainer component
        splineContainer = GetComponent<SplineContainer>();
        
        // Setup audio object
        if (audioObject == null)
        {
            // Try to find a child with AudioSource
            AudioSource[] childAudioSources = GetComponentsInChildren<AudioSource>();
            if (childAudioSources.Length > 0)
            {
                audioSource = childAudioSources[0];
                audioObject = audioSource.gameObject;
            }
            else
            {
                // Create a child GameObject with AudioSource if none exists
                audioObject = new GameObject("Audio Object");
                audioObject.transform.parent = transform;
                audioObject.transform.localPosition = Vector3.zero;
                audioSource = audioObject.AddComponent<AudioSource>();
                Debug.Log("Created new audio object as child of spline.");
            }
        }
        else
        {
            // Use the provided audio object
            audioSource = audioObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = audioObject.AddComponent<AudioSource>();
            }
        }
        
        // Get the audio listener (typically attached to the main camera)
        AudioListener listener = FindObjectOfType<AudioListener>();
        if (listener != null)
        {
            listenerTransform = listener.transform;
        }
        
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized || listenerTransform == null) return;
        
        // Get spline closed state
        bool isSplineClosed = splineContainer != null && splineContainer.Spline != null && splineContainer.Spline.Closed;
        
        // Sleep mode handling
        if (isSleeping)
        {
            // Only check for wake-up periodically to save performance
            if (Time.time < nextSleepCheckTime) return;
            
            // Get approximate spline bounds for fast rejection
            Bounds splineBounds = GetApproximateSplineBounds();
            splineBounds.Expand(sleepThreshold); // Expand by sleep threshold
            
            // Quick check if listener is far outside the expanded bounds
            if (!splineBounds.Contains(listenerTransform.position))
            {
                // Still too far away, schedule next check
                nextSleepCheckTime = Time.time + sleepCheckInterval;
                return;
            }
            
            // More detailed check - find actual distance to spline
            float distance = GetDistanceToSpline(listenerTransform.position);
            
            if (distance > sleepThreshold)
            {
                // Still too far, stay asleep
                nextSleepCheckTime = Time.time + sleepCheckInterval;
                return;
            }
            else
            {
                // Wake up - we're close enough to the spline now
                isSleeping = false;
            }
        }
        
    // For closed splines, check if listener is inside
        if (isSplineClosed)
        {
            bool wasInside = isInsideClosedSpline;
            isInsideClosedSpline = IsPointInsideClosedSpline(listenerTransform.position);
            
            if (isInsideClosedSpline)
            {
                // Inside closed spline - position audio near listener position
                targetPosition = listenerTransform.position + insideListenerOffset;
                
                // Smooth the movement
                audioObject.transform.position = Vector3.Lerp(
                    audioObject.transform.position, 
                    targetPosition, 
                    Time.deltaTime * movementSpeed * (1 - insideTransitionSmoothing));
                
                // Reset any exit transition
                isTransitioningOutside = false;
                exitTransitionProgress = 0f;
                
                return; // Skip normal spline following
            }
            else if (wasInside)
            {
                // Just exited the closed spline - start transition
                float closestT = FindClosestPointOnSpline(listenerTransform.position);
                currentSplinePosition = closestT;
                
                // Set up smooth transition
                isTransitioningOutside = true;
                exitStartPosition = audioObject.transform.position;
                exitTargetPosition = EvaluateSplinePosition(currentSplinePosition);
                exitTransitionProgress = 0f;
            }
            else if (isTransitioningOutside)
            {
                // Continue transition to spline
                exitTransitionProgress += Time.deltaTime / exitTransitionTime;
                
                if (exitTransitionProgress >= 1.0f)
                {
                    // Transition complete
                    isTransitioningOutside = false;
                    audioObject.transform.position = exitTargetPosition;
                }
                else
                {
                    // Custom smooth transition - can use different easing functions here
                    float t = 1.0f - Mathf.Pow(1.0f - exitTransitionProgress, 2.0f); // Ease out quad
                    audioObject.transform.position = Vector3.Lerp(
                        exitStartPosition, 
                        exitTargetPosition, 
                        t);
                    
                    // Also update target position as it might have changed
                    float newClosestT = FindClosestPointOnSpline(listenerTransform.position);
                    exitTargetPosition = EvaluateSplinePosition(newClosestT);
                    currentSplinePosition = newClosestT;
                }
                
                // Skip normal spline following during transition
                return;
            }
        }
        else
        {
            // Not a closed spline, reset inside state
            isInsideClosedSpline = false;
            isTransitioningOutside = false;
        }
        
        // Check if listener is close enough to the spline by measuring distance to any part of spline
        float distanceToSpline = GetDistanceToSpline(listenerTransform.position);
        bool inProximity = distanceToSpline <= proximityThreshold;
        
        // Check if we should enter sleep mode
        if (distanceToSpline > sleepThreshold)
        {
            isSleeping = true;
            nextSleepCheckTime = Time.time + sleepCheckInterval;
            return;
        }
        
        // Only update position along spline if the listener is in proximity
        if (inProximity)
        {
            // Find the closest point on the spline to the listener
            float closestT = FindClosestPointOnSpline(listenerTransform.position);
            
            // Smooth the movement along the spline
            currentSplinePosition = Mathf.Lerp(currentSplinePosition, closestT, Time.deltaTime * movementSpeed * (1 - smoothing));
            
            // Position the audio object along the spline
            audioObject.transform.position = EvaluateSplinePosition(currentSplinePosition);
        }
        // When not in proximity, the audio object stays where it is
    }

    // Gets the approximate distance from a point to the spline
    private float GetDistanceToSpline(Vector3 point)
    {
        // Reset closest distance tracking
        closestDistanceToSpline = float.MaxValue;
        
        // Sample points along the spline to find closest one to the point
        for (int i = 0; i <= splineSampleCount; i++)
        {
            float t = (float)i / splineSampleCount;
            Vector3 pointOnSpline = EvaluateSplinePosition(t);
            float distance = Vector3.Distance(pointOnSpline, point);
            
            if (distance < closestDistanceToSpline)
            {
                closestDistanceToSpline = distance;
                closestPointOnSpline = pointOnSpline;
                
                // Early exit optimization - if we're already within threshold, no need to check more points
                if (closestDistanceToSpline <= proximityThreshold)
                {
                    break;
                }
            }
        }
        
        return closestDistanceToSpline;
    }
    
    // Get approximate bounds of the spline for quick rejection tests
    private Bounds GetApproximateSplineBounds()
    {
        // Start with a point on the spline
        Vector3 firstPoint = EvaluateSplinePosition(0);
        Bounds bounds = new Bounds(firstPoint, Vector3.zero);
        
        // Sample a few points to create a bounding box
        int boundsSamples = 10; // Keep this low for performance
        for (int i = 1; i <= boundsSamples; i++)
        {
            float t = (float)i / boundsSamples;
            bounds.Encapsulate(EvaluateSplinePosition(t));
        }
        
        return bounds;
    }

    // Finds the normalized position (0-1) of the closest point on the spline to the target position
    private float FindClosestPointOnSpline(Vector3 targetPosition)
    {
        float closestDistance = float.MaxValue;
        float closestT = 0f;
        
        // Number of samples to check along the spline - use higher resolution for position finding
        int sampleCount = Mathf.Max(50, splineSampleCount * 2);
        
        for (int i = 0; i <= sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            Vector3 pointOnSpline = EvaluateSplinePosition(t);
            
            float distance = Vector3.Distance(pointOnSpline, targetPosition);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestT = t;
            }
        }
        
        return closestT;
    }

    // Gets the position at a normalized point (0-1) along the spline
    private Vector3 EvaluateSplinePosition(float t)
    {
        // Ensure we have a valid spline to evaluate
        if (splineContainer != null && splineContainer.Spline != null)
        {
            return splineContainer.EvaluatePosition(t);
        }
        
        return transform.position;
    }
    
    // Determines if a point is inside a closed spline
    private bool IsPointInsideClosedSpline(Vector3 point)
    {
        if (splineContainer == null || splineContainer.Spline == null || !splineContainer.Spline.Closed)
            return false;
            
        // For this to work, we need to project everything onto a consistent plane
        // We'll use the XZ plane (horizontal plane) as that's most common for gameplay
        
        // Implementation of ray-casting algorithm for point-in-polygon test
        // For a point to be inside, a ray from the point in any fixed direction 
        // will intersect the polygon boundary an odd number of times
        
        int intersectionCount = 0;
        float rayLength = 1000f; // Long enough to extend beyond the spline
        
        // Sample points along the spline to form our polygon
        // We need more points for accurate inside/outside detection
        int polygonSamples = Mathf.Max(50, splineSampleCount * 2);
        Vector3[] splinePoints = new Vector3[polygonSamples];
        
        for (int i = 0; i < polygonSamples; i++)
        {
            float t = (float)i / polygonSamples;
            splinePoints[i] = EvaluateSplinePosition(t);
        }
        
        // Cast a ray in the positive X direction
        Vector3 rayStart = point;
        Vector3 rayEnd = point + new Vector3(rayLength, 0, 0);
        
        // Check intersections with each polygon edge
        for (int i = 0; i < splinePoints.Length; i++)
        {
            // Get current edge (line segment)
            Vector3 p1 = splinePoints[i];
            Vector3 p2 = splinePoints[(i + 1) % splinePoints.Length];
            
            // Check if ray intersects this edge
            if (RayIntersectsLineSegment(rayStart, rayEnd, p1, p2))
            {
                intersectionCount++;
            }
        }
        
        // If intersection count is odd, the point is inside
        return (intersectionCount % 2) == 1;
    }
    
    // Checks if a ray intersects a line segment (projected onto XZ plane)
    private bool RayIntersectsLineSegment(Vector3 rayStart, Vector3 rayEnd, Vector3 lineP1, Vector3 lineP2)
    {
        // Project everything onto XZ plane by ignoring Y component for this calculation
        Vector2 rayStart2D = new Vector2(rayStart.x, rayStart.z);
        Vector2 rayEnd2D = new Vector2(rayEnd.x, rayEnd.z);
        Vector2 lineP12D = new Vector2(lineP1.x, lineP1.z);
        Vector2 lineP22D = new Vector2(lineP2.x, lineP2.z);
        
        // Check if the line segment crosses the ray's path
        // First, check if line segment straddles the ray's X-parallel line
        if ((lineP12D.y > rayStart2D.y && lineP22D.y <= rayStart2D.y) ||
            (lineP22D.y > rayStart2D.y && lineP12D.y <= rayStart2D.y))
        {
            // Calculate the X-coordinate of the intersection
            float intersectX = lineP12D.x + (rayStart2D.y - lineP12D.y) * (lineP22D.x - lineP12D.x) / (lineP22D.y - lineP12D.y);
            
            // Check if intersection is along the positive ray
            if (intersectX >= rayStart2D.x)
            {
                return true;
            }
        }
        
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugVisuals) return;
        
        SplineContainer spline = GetComponent<SplineContainer>();
        if (spline == null) return;
        
        // Draw the spline itself more clearly
        Gizmos.color = new Color(debugColor.r * 0.8f, debugColor.g * 0.8f, debugColor.b * 0.8f, 0.5f);
        
        // Visualize the spline with sample points
        if (showSamplePoints)
        {
            for (int i = 0; i <= splineSampleCount; i++)
            {
                float t = (float)i / splineSampleCount;
                if (spline.Spline != null)
                {
                    Vector3 point = spline.EvaluatePosition(t);
                    Gizmos.DrawSphere(point, 0.2f);
                }
            }
        }
        
        // If we're in play mode and initialized, show the relevant debug visuals
        if (Application.isPlaying && isInitialized)
        {
            // Draw a visual indication of the closest point on the spline to the listener
            if (listenerTransform != null)
            {
                // Get spline closed state
                bool isSplineClosed = spline != null && spline.Spline != null && spline.Spline.Closed;
                
                // Draw different indicators based on inside/outside status for closed splines
                if (isSplineClosed)
                {
                    if (isInsideClosedSpline)
                    {
                        // Inside closed spline indicator
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawLine(listenerTransform.position, audioObject.transform.position);
                        Gizmos.DrawWireSphere(listenerTransform.position, 1.0f);
                        
                        // Draw "inside" icon
                        Gizmos.DrawIcon(listenerTransform.position + Vector3.up * 2f, "d_greenLight", true);
                    }
                    else if (isTransitioningOutside)
                    {
                        // Transitioning out of closed spline
                        Gizmos.color = new Color(0f, 1f, 1f, exitTransitionProgress);
                        Gizmos.DrawLine(listenerTransform.position, audioObject.transform.position);
                        
                        // Draw transition path
                        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
                        Gizmos.DrawLine(exitStartPosition, exitTargetPosition);
                        
                        // Draw progress indicator
                        Gizmos.DrawSphere(Vector3.Lerp(exitStartPosition, exitTargetPosition, exitTransitionProgress), 0.3f);
                    }
                    else
                    {
                        // Outside but closed spline
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(listenerTransform.position, closestPointOnSpline);
                        Gizmos.DrawSphere(closestPointOnSpline, 0.5f);
                    }
                }
                else
                {
                    // Normal open spline - show proximity
                    Gizmos.color = closestDistanceToSpline <= proximityThreshold ? Color.green : Color.yellow;
                    Gizmos.DrawLine(listenerTransform.position, closestPointOnSpline);
                    Gizmos.DrawSphere(closestPointOnSpline, 0.5f);
                }
                
                // Draw sleep state indicator
                if (isSleeping)
                {
                    Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
                    DrawSleepIndicator();
                }
                
                // Draw a dashed line from the closest point to the actual audio position
                Vector3 audioPos = audioObject.transform.position;
                Gizmos.color = debugColor;
                Gizmos.DrawLine(audioPos, closestPointOnSpline);
                
                // Visualize the current position on the spline
                Vector3 currentPos = EvaluateSplinePosition(currentSplinePosition);
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(currentPos, 0.4f);
                
                // Visualize the sleep threshold
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f); // Orange, semi-transparent
                Bounds splineBounds = GetApproximateSplineBounds();
                splineBounds.Expand(sleepThreshold);
                Gizmos.DrawWireCube(splineBounds.center, splineBounds.size);
            }
        }
        else
        {
            // In edit mode, show a representation of the proximity threshold
            if (spline.Spline != null)
            {
                Gizmos.color = new Color(debugColor.r, debugColor.g, debugColor.b, 0.2f);
                
                // Display the proximity threshold along the spline
                int tubeSamples = 12;  // Lower for better editor performance
                for (int i = 0; i <= tubeSamples; i++)
                {
                    float t = (float)i / tubeSamples;
                    Vector3 point = spline.EvaluatePosition(t);
                    Gizmos.DrawWireSphere(point, proximityThreshold);
                }
                
                // If closed spline, draw a special indicator
                if (spline.Spline.Closed)
                {
                    Gizmos.color = new Color(0f, 1f, 1f, 0.15f); // Cyan, very transparent
                    
                    // Draw a filled area to represent the inside of the closed spline
                    // (This is an approximation since we can't actually draw filled polygons with Gizmos)
                    Vector3 center = spline.transform.position;
                    for (int i = 0; i < tubeSamples; i++)
                    {
                        float t1 = (float)i / tubeSamples;
                        float t2 = (float)(i + 1) / tubeSamples;
                        
                        Vector3 p1 = spline.EvaluatePosition(t1);
                        Vector3 p2 = spline.EvaluatePosition(t2);
                        
                        // Draw triangles from center to edges to simulate fill
                        Gizmos.DrawLine(center, p1);
                        Gizmos.DrawLine(center, p2);
                        Gizmos.DrawLine(p1, p2);
                    }
                }
            }
        }
    }
    
    private void DrawSleepIndicator()
    {
        Vector3 pos = transform.position + Vector3.up * 2f;
        
        // Draw sleep indicator
        Gizmos.DrawIcon(pos, "console.infoicon.sml", true);
        
        // Draw sleep bounds
        Bounds splineBounds = GetApproximateSplineBounds();
        splineBounds.Expand(sleepThreshold);
        Gizmos.DrawWireCube(splineBounds.center, splineBounds.size);
    }
}