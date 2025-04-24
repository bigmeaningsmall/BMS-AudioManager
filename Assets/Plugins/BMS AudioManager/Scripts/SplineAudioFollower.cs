using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(AudioSource))]
public class SplineAudioFollower : MonoBehaviour
{
    [Header("Spline References")]
    [Tooltip("The SplineContainer that defines the path for this audio to follow")]
    public SplineContainer splineContainer;

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

    [Header("Debug")]
    public bool showDebugVisuals = true;
    public Color debugColor = Color.green;

    // Private references
    private AudioSource audioSource;
    private Transform listenerTransform;
    private float currentSplinePosition = 0f; // 0-1 normalized position along spline
    private bool isInitialized = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        
        // Get the audio listener (typically attached to the main camera)
        AudioListener listener = FindObjectOfType<AudioListener>();
        if (listener != null)
        {
            listenerTransform = listener.transform;
        }
        
        if (splineContainer == null)
        {
            Debug.LogError("SplineAudioFollower requires a SplineContainer reference.");
            enabled = false;
            return;
        }
        
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized || listenerTransform == null) return;
        
        // Check if listener is close enough to the spline
        bool inProximity = IsListenerInProximityOfSpline();
        
        // Only update position along spline if the listener is in proximity
        if (inProximity)
        {
            // Find the closest point on the spline to the listener
            float closestT = FindClosestPointOnSpline(listenerTransform.position);
            
            // Smooth the movement along the spline
            currentSplinePosition = Mathf.Lerp(currentSplinePosition, closestT, Time.deltaTime * movementSpeed * (1 - smoothing));
            
            // Position the audio source along the spline
            transform.position = EvaluateSplinePosition(currentSplinePosition);
        }
        // When not in proximity, the audio object stays where it is
    }

    // Checks if the listener is close enough to any part of the spline
    private bool IsListenerInProximityOfSpline()
    {
        if (splineContainer == null || listenerTransform == null) return false;
        
        // Sample points along the spline to find closest one to listener
        int sampleCount = 20; // Lower for performance, higher for accuracy
        float minDistance = float.MaxValue;
        
        for (int i = 0; i <= sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            Vector3 pointOnSpline = EvaluateSplinePosition(t);
            float distance = Vector3.Distance(pointOnSpline, listenerTransform.position);
            
            if (distance < minDistance)
            {
                minDistance = distance;
            }
        }
        
        // Return true if closest point is within proximity threshold
        return minDistance <= proximityThreshold;
    }

    // Finds the normalized position (0-1) of the closest point on the spline to the target position
    private float FindClosestPointOnSpline(Vector3 targetPosition)
    {
        float closestDistance = float.MaxValue;
        float closestT = 0f;
        
        // Number of samples to check along the spline
        int sampleCount = 50;
        
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
        if (!showDebugVisuals || splineContainer == null) return;
        
        // Draw the proximity threshold
        Gizmos.color = debugColor;
        Gizmos.DrawWireSphere(transform.position, proximityThreshold);
        
        // Draw the current position on the spline
        if (Application.isPlaying && isInitialized)
        {
            Vector3 splinePos = EvaluateSplinePosition(currentSplinePosition);
            Gizmos.DrawSphere(splinePos, 0.5f);
            
            // Draw line to listener if available
            if (listenerTransform != null)
            {
                Gizmos.DrawLine(splinePos, listenerTransform.position);
            }
        }
    }
}