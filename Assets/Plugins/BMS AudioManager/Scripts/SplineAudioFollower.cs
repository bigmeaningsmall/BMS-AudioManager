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
        
        // Check if listener is close enough to the spline by measuring distance to any part of spline
        // This will also update closestDistanceToSpline and closestPointOnSpline
        bool inProximity = IsListenerInProximityOfSpline();
        
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

    // Checks if the listener is close enough to any part of the spline
    private bool IsListenerInProximityOfSpline()
    {
        if (splineContainer == null || listenerTransform == null) return false;
        
        // Reset closest distance tracking
        closestDistanceToSpline = float.MaxValue;
        
        // Sample points along the spline to find closest one to listener
        Vector3 listenerPos = listenerTransform.position;
        
        for (int i = 0; i <= splineSampleCount; i++)
        {
            float t = (float)i / splineSampleCount;
            Vector3 pointOnSpline = EvaluateSplinePosition(t);
            float distance = Vector3.Distance(pointOnSpline, listenerPos);
            
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
        
        // Return true if closest point on spline is within proximity threshold
        return closestDistanceToSpline <= proximityThreshold;
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
                // Visualize the proximity
                Gizmos.color = closestDistanceToSpline <= proximityThreshold ? Color.green : Color.yellow;
                Gizmos.DrawLine(listenerTransform.position, closestPointOnSpline);
                Gizmos.DrawSphere(closestPointOnSpline, 0.5f);
                
                // Draw a dashed line from the closest point to the actual audio position
                Vector3 audioPos = audioObject.transform.position;
                Gizmos.color = debugColor;
                Gizmos.DrawLine(audioPos, closestPointOnSpline);
                
                // Visualize the current position on the spline
                Vector3 currentPos = EvaluateSplinePosition(currentSplinePosition);
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(currentPos, 0.4f);
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
            }
        }
    }
}