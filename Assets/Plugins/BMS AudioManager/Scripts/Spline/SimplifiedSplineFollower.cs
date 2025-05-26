using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer))]
public class SimplifiedSplineFollower : MonoBehaviour
{
    [Header("Basic Settings")]
    [Tooltip("Maximum distance at which the follower will track along the spline")]
    [Range(1f, 50f)]
    public float proximityThreshold = 10f;

    [Tooltip("How quickly the follower object moves along the spline")]
    [Range(0.1f, 10f)]
    public float movementSpeed = 5f;

    [Tooltip("Smoothing factor for follower movement (higher = smoother but less responsive)")]
    [Range(0f, 0.95f)]
    public float smoothing = 0.7f;

    [Header("Closed Spline Behavior")]
    [Tooltip("When enabled, target will be directly followed inside closed splines")]
    public bool directFollowInsideClosedSpline = false;

    [Tooltip("When enabled, follower will not track the target outside of closed splines")]
    public bool onlyTrackInsideClosedSpline = false;

    [Header("Position Offset")]
    [Tooltip("Offset from the spline position (local to spline direction)")]
    public Vector3 positionOffset = Vector3.zero;

    [Tooltip("How quickly the offset transitions when changed")]
    [Range(0.1f, 10f)]
    public float offsetTransitionSpeed = 3.0f;

    [Header("Performance")]
    [Tooltip("Target updates per second (0 = every frame)")]
    [Range(0, 60)]
    public float targetUpdatesPerSecond = 0f;

    [Header("References")]
    [Tooltip("The child GameObject that will follow the spline (optional)")]
    public GameObject followObject;

    [Tooltip("Target transform to follow (optional, will auto-find if not set)")]
    public Transform targetTransform;

    [Header("Sleep Manager")]
    public SplineSleepManager sleepManager = new SplineSleepManager();

    [Header("Debug")]
    public bool showDebugVisuals = true;
    public bool showSamplePoints = false;
    public bool showProximityThreshold = true;
    public bool showSleepBounds = false;
    public bool showClosedSplineArea = true;
    public Color debugColor = Color.green;
    
    [Tooltip("Number of sample points to show along the spline")]
    [Range(10, 100)]
    public int splineSampleCount = 20;

    // Core state
    public enum FollowState
    {
        Normal,           // Following spline normally
        InsideClosed,     // Inside a closed spline
        OutOfRange        // Too far from spline
    }

    private SplineContainer splineContainer;
    private Transform trackingTarget;
    private bool isInitialized = false;

    private FollowState currentState = FollowState.Normal;
    private float currentSplinePosition = 0f;
    private Vector3 currentOffset = Vector3.zero;

    // Update timing
    private float accumulatedTime = 0f;
    private float updateInterval = 0f;

    #region Initialization

    private void Awake()
    {
        splineContainer = GetComponent<SplineContainer>();
        InitializeFollowObject();
        InitializeTarget();
        sleepManager.Initialize(splineContainer);
        UpdateTimingSettings();
    }

    private void Start()
    {
        if (!isInitialized)
        {
            FindDefaultTarget();
        }
    }

    private void InitializeFollowObject()
    {
        if (followObject == null)
        {
            // Try to find existing audio source first
            AudioSource[] childAudioSources = GetComponentsInChildren<AudioSource>();
            if (childAudioSources.Length > 0)
            {
                followObject = childAudioSources[0].gameObject;
            }
            else
            {
                // Create new follower object
                followObject = new GameObject("Spline Follower Object");
                followObject.transform.parent = transform;
                followObject.transform.localPosition = Vector3.zero;
            }
        }
    }

    private void InitializeTarget()
    {
        currentOffset = positionOffset;

        if (targetTransform != null)
        {
            trackingTarget = targetTransform;
            isInitialized = true;
        }
        else
        {
            isInitialized = false;
        }
    }

    private void FindDefaultTarget()
    {
        if (targetTransform != null)
        {
            trackingTarget = targetTransform;
            isInitialized = true;
            return;
        }

        // Try AudioListener first
        AudioListener listener = FindObjectOfType<AudioListener>();
        if (listener != null)
        {
            trackingTarget = listener.transform;
            isInitialized = true;
            return;
        }

        // Try main camera
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            trackingTarget = mainCamera.transform;
            isInitialized = true;
            return;
        }

        // Try any camera
        Camera[] cameras = FindObjectsOfType<Camera>();
        if (cameras.Length > 0)
        {
            trackingTarget = cameras[0].transform;
            isInitialized = true;
            return;
        }

        Debug.LogWarning("SplineFollower: No suitable target found. Please set targetTransform manually.");
    }

    #endregion

    #region Public API

    public void Initialize(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning("SplineFollower: Cannot initialize with null target");
            return;
        }

        trackingTarget = target;
        targetTransform = target;
        isInitialized = true;

        RestartFollowing();
    }

    public void SetTarget(Transform newTarget)
    {
        if (newTarget == null)
        {
            Debug.LogWarning("SplineFollower: Cannot set null target");
            return;
        }

        trackingTarget = newTarget;
        targetTransform = newTarget;

        if (!isInitialized)
        {
            isInitialized = true;
            RestartFollowing();
        }
    }

    public void RestartFollowing()
    {
        if (!isInitialized || trackingTarget == null) return;

        // Find closest point and position follower there immediately
        float closestT = SplineUtilities.FindClosestPoint(splineContainer, trackingTarget.position);
        currentSplinePosition = closestT;

        Vector3 splinePosition = splineContainer.EvaluatePosition(currentSplinePosition);
        Vector3 finalPosition = SplineUtilities.ApplyOffsetToSplinePosition(
            splineContainer, splinePosition, currentSplinePosition, currentOffset);

        followObject.transform.position = finalPosition;
    }

    public void UpdateTimingSettings()
    {
        updateInterval = (targetUpdatesPerSecond > 0) ? 1f / targetUpdatesPerSecond : 0f;
        accumulatedTime = 0f;
    }

    #endregion

    #region Main Update Loop

    private void LateUpdate()
    {
        if (!isInitialized || trackingTarget == null) return;

        // Handle update timing
        if (targetUpdatesPerSecond > 0)
        {
            accumulatedTime += Time.deltaTime;
            if (accumulatedTime < updateInterval) return;
            accumulatedTime -= updateInterval;
        }

        // Check sleep mode first
        if (sleepManager.HandleSleepMode(trackingTarget)) return;

        // Update state and position
        UpdateFollowState();
        UpdatePosition();
    }

    private void UpdateFollowState()
    {
        bool isSplineClosed = splineContainer?.Spline?.Closed ?? false;

        if (isSplineClosed)
        {
            bool isInside = SplineUtilities.IsPointInsideClosedSpline(splineContainer, trackingTarget.position);

            if (!isInside && onlyTrackInsideClosedSpline)
            {
                currentState = FollowState.OutOfRange;
                return;
            }

            currentState = isInside ? FollowState.InsideClosed : FollowState.Normal;
        }
        else
        {
            float distance = SplineUtilities.GetDistanceToSpline(splineContainer, trackingTarget.position);
            currentState = distance <= proximityThreshold ? FollowState.Normal : FollowState.OutOfRange;
        }
    }

    private void UpdatePosition()
    {
        switch (currentState)
        {
            case FollowState.Normal:
                UpdateNormalPosition();
                break;

            case FollowState.InsideClosed:
                if (directFollowInsideClosedSpline)
                {
                    UpdateDirectFollowPosition();
                }
                else
                {
                    UpdateInsideClosedPosition();
                }
                break;

            case FollowState.OutOfRange:
                // Follower stays where it is
                break;
        }

        // Update offset
        UpdateOffset();
    }

    private void UpdateNormalPosition()
    {
        // Find closest point on spline
        float targetT = SplineUtilities.FindClosestPoint(splineContainer, trackingTarget.position);

        // Smooth movement along spline
        currentSplinePosition = Mathf.Lerp(currentSplinePosition, targetT, 
            Time.deltaTime * movementSpeed * (1 - smoothing));

        // Position the follower
        Vector3 splinePosition = splineContainer.EvaluatePosition(currentSplinePosition);
        Vector3 finalPosition = SplineUtilities.ApplyOffsetToSplinePosition(
            splineContainer, splinePosition, currentSplinePosition, currentOffset);

        followObject.transform.position = finalPosition;
    }

    private void UpdateDirectFollowPosition()
    {
        // Simply follow the target directly with offset
        Vector3 targetPosition = trackingTarget.position + currentOffset;
        followObject.transform.position = Vector3.Lerp(
            followObject.transform.position,
            targetPosition,
            Time.deltaTime * movementSpeed * (1 - smoothing));
    }

    private void UpdateInsideClosedPosition()
    {
        // Position relative to target when inside closed spline
        Vector3 targetPosition = trackingTarget.position + currentOffset;
        followObject.transform.position = Vector3.Lerp(
            followObject.transform.position,
            targetPosition,
            Time.deltaTime * movementSpeed * (1 - smoothing));
    }

    private void UpdateOffset()
    {
        // Smoothly transition offset changes
        currentOffset = Vector3.Lerp(currentOffset, positionOffset,
            Time.deltaTime * offsetTransitionSpeed);
    }

    #endregion

    #region Debug Visuals

    private void OnDrawGizmosSelected()
    {
        if (!showDebugVisuals || splineContainer?.Spline == null) return;

        DrawSplineBase();

        if (Application.isPlaying && isInitialized && trackingTarget != null)
        {
            DrawRuntimeDebugVisuals();
        }
        else
        {
            DrawEditorDebugVisuals();
        }
    }

    private void DrawSplineBase()
    {
        // Draw the spline itself
        Gizmos.color = new Color(debugColor.r * 0.8f, debugColor.g * 0.8f, debugColor.b * 0.8f, 0.5f);

        // Show sample points along the spline if enabled
        if (showSamplePoints)
        {
            for (int i = 0; i <= splineSampleCount; i++)
            {
                float t = (float)i / splineSampleCount;
                Vector3 point = splineContainer.EvaluatePosition(t);
                Gizmos.DrawSphere(point, 0.15f);
            }
        }
    }

    private void DrawRuntimeDebugVisuals()
    {
        // Draw proximity threshold first (so it's behind other elements)
        if (showProximityThreshold)
        {
            DrawProximityThreshold();
        }

        // Set color based on current state
        switch (currentState)
        {
            case FollowState.Normal:
                Gizmos.color = Color.green;
                break;
            case FollowState.InsideClosed:
                Gizmos.color = Color.cyan;
                DrawInsideClosedIndicators();
                break;
            case FollowState.OutOfRange:
                Gizmos.color = Color.yellow;
                break;
        }

        // Draw connection between target and follower
        Gizmos.DrawLine(trackingTarget.position, followObject.transform.position);

        // Draw target position
        Gizmos.DrawWireSphere(trackingTarget.position, 0.5f);

        // Draw current position on spline
        Vector3 currentPos = splineContainer.EvaluatePosition(currentSplinePosition);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(currentPos, 0.3f);

        // Draw follower position
        Gizmos.color = debugColor;
        Gizmos.DrawSphere(followObject.transform.position, 0.25f);

        // Draw line from spline position to follower (showing offset)
        if (Vector3.Distance(currentPos, followObject.transform.position) > 0.1f)
        {
            Gizmos.color = new Color(debugColor.r, debugColor.g, debugColor.b, 0.7f);
            Gizmos.DrawLine(currentPos, followObject.transform.position);
        }

        // Draw sleep indicator
        if (sleepManager.IsSleeping)
        {
            DrawSleepIndicator();
        }

        // Draw sleep bounds if enabled
        if (showSleepBounds)
        {
            DrawSleepBounds();
        }

        // Draw proximity information
        DrawProximityIndicators();
    }

    private void DrawEditorDebugVisuals()
    {
        // Show proximity threshold in editor
        if (showProximityThreshold)
        {
            DrawProximityThreshold();
        }

        // Show closed spline area if applicable
        if (showClosedSplineArea && splineContainer.Spline.Closed)
        {
            DrawClosedSplineArea();
        }

        // Show sleep bounds if enabled
        if (showSleepBounds)
        {
            DrawSleepBounds();
        }
    }

    private void DrawInsideClosedIndicators()
    {
        // Draw special indicators when inside closed spline
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(trackingTarget.position, 1.0f);

        // Draw "inside" icon
        Vector3 iconPos = trackingTarget.position + Vector3.up * 2f;
        Gizmos.DrawIcon(iconPos, "d_greenLight", true);

        // Draw direct connection line
        Gizmos.color = new Color(0f, 1f, 1f, 0.6f);
        Gizmos.DrawLine(trackingTarget.position, followObject.transform.position);
    }

    private void DrawSleepIndicator()
    {
        Vector3 pos = transform.position + Vector3.up * 3f;
        
        // Draw sleep icon
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        Gizmos.DrawIcon(pos, "console.infoicon.sml", true);
        
        // Draw "ZZZ" effect with spheres
        for (int i = 0; i < 3; i++)
        {
            Vector3 zzz = pos + Vector3.up * (i * 0.5f) + Vector3.right * (i * 0.2f);
            Gizmos.DrawSphere(zzz, 0.1f);
        }
    }

    private void DrawSleepBounds()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f); // Orange, semi-transparent
        Bounds splineBounds = SplineUtilities.GetApproximateSplineBounds(splineContainer);
        splineBounds.Expand(sleepManager.sleepThreshold);
        Gizmos.DrawWireCube(splineBounds.center, splineBounds.size);
        
        // Add label
        Vector3 labelPos = splineBounds.center + Vector3.up * (splineBounds.size.y * 0.5f + 1f);
        Gizmos.DrawIcon(labelPos, "console.warnicon.sml", true);
    }

    private void DrawProximityThreshold()
    {
        // Make the proximity threshold more visible
        Gizmos.color = new Color(debugColor.r, debugColor.g, debugColor.b, 0.25f);
        
        // Draw proximity threshold tubes along the spline
        int tubeSamples = 15; // Good balance between detail and performance
        for (int i = 0; i <= tubeSamples; i++)
        {
            float t = (float)i / tubeSamples;
            Vector3 point = splineContainer.EvaluatePosition(t);
            
            // Draw wire sphere for proximity threshold
            Gizmos.DrawWireSphere(point, proximityThreshold);
            
            // Also draw a smaller solid sphere to make it more visible
            // Gizmos.color = new Color(debugColor.r, debugColor.g, debugColor.b, 0.1f);
            // Gizmos.DrawSphere(point, proximityThreshold);
            // Gizmos.color = new Color(debugColor.r, debugColor.g, debugColor.b, 0.25f);
        }
        
        // Draw connecting lines between proximity spheres to create a "tube" effect
        for (int i = 0; i < tubeSamples; i++)
        {
            float t1 = (float)i / tubeSamples;
            float t2 = (float)(i + 1) / tubeSamples;
            
            Vector3 point1 = splineContainer.EvaluatePosition(t1);
            Vector3 point2 = splineContainer.EvaluatePosition(t2);
            
            // Draw lines at proximity threshold distance
            Vector3 direction1 = splineContainer.EvaluateTangent(t1);
            Vector3 direction2 = splineContainer.EvaluateTangent(t2);
            
            // Get perpendicular vectors for "tube" outline
            Vector3 right1 = Vector3.Cross(Vector3.up, direction1).normalized * proximityThreshold;
            Vector3 right2 = Vector3.Cross(Vector3.up, direction2).normalized * proximityThreshold;
            
            // Draw tube outline
            Gizmos.color = new Color(debugColor.r, debugColor.g, debugColor.b, 0.3f);
            Gizmos.DrawLine(point1 + right1, point2 + right2);
            Gizmos.DrawLine(point1 - right1, point2 - right2);
        }
    }

    private void DrawClosedSplineArea()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.1f); // Cyan, very transparent
        
        // Draw triangular segments to approximate the filled area
        Vector3 center = splineContainer.transform.position;
        int segments = 20;
        
        for (int i = 0; i < segments; i++)
        {
            float t1 = (float)i / segments;
            float t2 = (float)(i + 1) / segments;
            
            Vector3 p1 = splineContainer.EvaluatePosition(t1);
            Vector3 p2 = splineContainer.EvaluatePosition(t2);
            
            // Draw lines to create triangular fill effect
            Gizmos.DrawLine(center, p1);
            Gizmos.DrawLine(center, p2);
            Gizmos.DrawLine(p1, p2);
        }
        
        // Draw border more prominently
        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        for (int i = 0; i < segments; i++)
        {
            float t1 = (float)i / segments;
            float t2 = (float)(i + 1) / segments;
            
            Vector3 p1 = splineContainer.EvaluatePosition(t1);
            Vector3 p2 = splineContainer.EvaluatePosition(t2);
            
            Gizmos.DrawLine(p1, p2);
        }
    }

    private void DrawProximityIndicators()
    {
        // Show distance information in runtime
        float currentDistance = SplineUtilities.GetDistanceToSpline(splineContainer, trackingTarget.position);
        
        // Color code based on proximity
        if (currentDistance <= proximityThreshold)
        {
            Gizmos.color = Color.green;
        }
        else if (currentDistance <= sleepManager.sleepThreshold)
        {
            Gizmos.color = Color.yellow;
        }
        else
        {
            Gizmos.color = Color.red;
        }
        
        // Draw distance line from target to closest point on spline
        Vector3 closestPoint = splineContainer.EvaluatePosition(currentSplinePosition);
        Gizmos.DrawLine(trackingTarget.position, closestPoint);
        
        // Draw small indicator at closest point
        Gizmos.DrawSphere(closestPoint, 0.2f);
    }

    // Additional utility for showing state information in editor
    private void OnDrawGizmos()
    {
        if (!showDebugVisuals) return;
        
        // Show basic state even when not selected (less intrusive)
        if (Application.isPlaying && isInitialized && trackingTarget != null)
        {
            // Just show a small connection line
            Gizmos.color = new Color(debugColor.r, debugColor.g, debugColor.b, 0.05f);
            Gizmos.DrawLine(trackingTarget.position, followObject.transform.position);
        }
    }

    #endregion
}