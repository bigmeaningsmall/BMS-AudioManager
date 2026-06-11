using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(SplineContainer))]
public class SplineFollower : MonoBehaviour
{
    
    [Header("Closed Spline Behavior")]
    [Tooltip("When enabled, target will be directly followed inside closed splines without tracking the spline")]
    public bool directFollowInsideClosedSpline = false;

    [Tooltip("When enabled, follower will not track the target outside of closed splines")]
    public bool onlyTrackInsideClosedSpline = false;
    
    
    [Header("Tracking Settings")] [Tooltip("Maximum distance at which the follower will track along the spline")] [Range(1f, 50f)]
    public float proximityThreshold = 10f;

    [Tooltip("How quickly the follower object moves along the spline")] [Range(0.1f, 10f)]
    public float movementSpeed = 5f;

    [Tooltip("Smoothing factor for follower movement (higher = smoother but less responsive)")] [Range(0f, 0.95f)]
    public float smoothing = 0.7f;

    [Header("Direction Consistency Settings")]
    [Tooltip("How strongly to maintain the current movement direction (prevents flipping at equidistant points)")]
    [Range(0.1f, 5f)]
    public float directionBias = 1.5f;
    
    [Tooltip("Distance threshold for determining equidistant points")]
    [Range(0.01f, 0.5f)]
    public float equidistantThreshold = 0.05f;

    [Header("Position Offset Settings")] [Tooltip("Offset from the position (local to direction)")]
    public Vector3 positionOffset = Vector3.zero;

    [Tooltip("How quickly the offset transitions when changed (higher = faster)")] [Range(0.1f, 10f)]
    public float offsetTransitionSpeed = 3.0f;

    [Header("Optimisation")]
    [Tooltip("Distance beyond which the follower will enter sleep mode (should be greater than proximityThreshold)")]
    [Range(10f, 200f)]
    public float sleepThreshold = 30f;
    [Tooltip("How often to check if we should wake up when sleeping (in seconds)")] [Range(0.1f, 5f)]
    public float sleepCheckInterval = 0.5f;
    
    [Tooltip("Target updates per second (0 = every frame)")]
    [Range(0, 60)]
    public float targetUpdatesPerSecond = 0f;
    private float accumulatedTime = 0f;
    private float updateInterval = 0f;
    
    [Header("Closed Spline Settings")] [Tooltip("When inside a closed spline, use this mode for positioning")]
    public ClosedSplinePositionMode insidePositionMode = ClosedSplinePositionMode.RelativeToTarget;

    [Tooltip("Smoothing factor for transitions between states")] [Range(0f, 0.95f)]
    public float transitionSmoothing = 0.7f;

    [Tooltip("Maximum time for state transitions (seconds)")] [Range(0.1f, 3.0f)]
    public float transitionTime = 0.5f;

    [Tooltip("Faster exit transition multiplier (higher = quicker exit to spline boundary)")] [Range(1f, 10f)]
    public float exitTransitionSpeedMultiplier = 2.0f;

    [Header("References")] [Tooltip("The child GameObject that will follow the spline (optional, will be created if not set)")]
    public GameObject followObject;

    [Tooltip("Target transform to follow (optional, will use AudioListener or Camera if not set)")]
    public Transform targetTransform;

    [Header("Debug")] public bool showDebugVisuals = true;
    public Color debugColor = Color.green;

    [Tooltip("Show sample points along the spline for visualization")]
    public bool showSamplePoints = false;

    [Tooltip("Number of sample points to check when finding proximity to spline")] [Range(10, 100)]
    public int splineSampleCount = 20;

    // Enum to define positioning modes
    public enum ClosedSplinePositionMode{
        RelativeToTarget,
        FixedOffset
    }

    // Private references
    private SplineContainer splineContainer;
    private Component targetComponent; // Can be AudioSource or any other component
    private Transform trackingTarget; // Reference to the target we're following
    private float currentSplinePosition = 0f; // 0-1 normalized position along spline
    private bool isInitialized = false;
    private float closestDistanceToSpline = float.MaxValue;
    private Vector3 closestPointOnSpline = Vector3.zero;

    // Sleep mode variables
    private bool isSleeping = false;
    private float nextSleepCheckTime = 0f;

    // State tracking and transitions
    private enum PositionState{
        Normal,
        InsideClosed,
        Transitioning
    }

    
    private PositionState currentState = PositionState.Normal;
    private Vector3 currentOffset = Vector3.zero;
    private Vector3 targetOffset = Vector3.zero;
    private Vector3 transitionStartPosition = Vector3.zero;
    private Vector3 transitionTargetPosition = Vector3.zero;
    private float transitionProgress = 0f;
    private PositionState transitionTargetState = PositionState.Normal;

    // Direction tracking variables for equidistant point handling
    private float lastSplinePosition = 0f;
    private float currentDirection = 1f; // 1 = forward, -1 = backward
    private float directionChangeTimer = 0f;

    private void Awake(){
        // Get the SplineContainer component
        splineContainer = GetComponent<SplineContainer>();

        // Initialize follower object
        InitializeFollowObject();

        // Initialize variables but don't try to find targets yet
        currentOffset = positionOffset;
        targetOffset = positionOffset;

        // Find target if explicitly set
        if (targetTransform != null){
            trackingTarget = targetTransform;
            isInitialized = true;
        }
        else{
            // We'll attempt to find a target in Start(), or user can call Initialize() manually
            isInitialized = false;
        }
        
        // Calculate update interval
        UpdateTimingSettings();
    }

    private void Start(){
        // Try to find default targets if not already initialized
        if (!isInitialized){
            FindDefaultTarget();
        }
    }

    public void UpdateTimingSettings()
    {
        // Convert FPS to time interval (0 means update every frame)
        updateInterval = (targetUpdatesPerSecond > 0) ? 1f / targetUpdatesPerSecond : 0f;
        accumulatedTime = 0f; // Reset accumulated time when interval changes
    }
    
    /// <summary>
    /// Manually initialize the SplineFollower with a specific target
    /// </summary>
    /// <param name="target">The transform to follow</param>
    public void Initialize(Transform target){
        if (target == null){
            Debug.LogWarning("SplineFollower: Cannot initialize with null target");
            return;
        }

        trackingTarget = target;
        targetTransform = target; // Keep the public reference in sync

        if (followObject == null){
            InitializeFollowObject();
        }

        isInitialized = true;
    }

    /// <summary>
    /// Initializes the follow object
    /// </summary>
    private void InitializeFollowObject(){
        if (followObject == null){
            // Check if we have an audio source to follow
            AudioSource[] childAudioSources = GetComponentsInChildren<AudioSource>();
            if (childAudioSources.Length > 0){
                targetComponent = childAudioSources[0];
                followObject = targetComponent.gameObject;
            }
            else{
                // Create a child GameObject as follower
                followObject = new GameObject("Spline Follower Object");
                followObject.transform.parent = transform;
                followObject.transform.localPosition = Vector3.zero;

                // We don't necessarily need a component, but we'll add a basic one for reference
                targetComponent = followObject.AddComponent<MeshFilter>();
                Debug.Log("Created new follower object as child of spline.");
            }
        }
        else{
            // Use the provided follow object
            // Try to get a component we can reference
            Component[] components = followObject.GetComponents<Component>();
            if (components.Length > 1) // First component is always Transform
            {
                // Get the first non-Transform component
                for (int i = 0; i < components.Length; i++){
                    if (!(components[i] is Transform)){
                        targetComponent = components[i];
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Attempts to find a suitable default target in the scene
    /// </summary>
    private void FindDefaultTarget(){
        // If target is already explicitly set, use that
        if (targetTransform != null){
            trackingTarget = targetTransform;
            isInitialized = true;
            return;
        }

        // Otherwise, try to find a suitable default target

        // First try to find an AudioListener
        AudioListener listener = FindObjectOfType<AudioListener>();
        if (listener != null){
            trackingTarget = listener.transform;
            isInitialized = true;
            return;
        }

        // If no AudioListener, try the main camera
        Camera mainCamera = Camera.main;
        if (mainCamera != null){
            trackingTarget = mainCamera.transform;
            isInitialized = true;
            return;
        }

        // If still no target, try any camera
        Camera[] cameras = FindObjectsOfType<Camera>();
        if (cameras.Length > 0){
            trackingTarget = cameras[0].transform;
            isInitialized = true;
            return;
        }

        // No suitable target found
        Debug.LogWarning("SplineFollower: No suitable target found. Please set the targetTransform manually or call Initialize().");
        isInitialized = false;
    }

    /// <summary>
    /// Manually restart the follower by finding a new closest point on the spline
    /// Useful after teleporting the target or otherwise making large position changes
    /// </summary>
    public void RestartFollowing(){
        if (!isInitialized || trackingTarget == null) return;

        // Find the closest point on the spline to the target
        float closestT = FindRawClosestPointOnSpline(trackingTarget.position);

        // Immediately position the follower without smoothing
        currentSplinePosition = closestT;
        lastSplinePosition = closestT; // Reset direction tracking

        // Position the follow object at the closest point
        Vector3 splinePosition = EvaluateSplinePosition(currentSplinePosition);

        // Apply offset if needed
        if (currentOffset != Vector3.zero){
            followObject.transform.position = ApplyOffsetToSplinePosition(splinePosition, currentSplinePosition);
        }
        else{
            followObject.transform.position = splinePosition;
        }
    }

    /// <summary>
    /// Set a new target for the follower to track
    /// </summary>
    /// <param name="newTarget">The new transform to follow</param>
    public void SetTarget(Transform newTarget){
        if (newTarget == null){
            Debug.LogWarning("SplineFollower: Cannot set null target");
            return;
        }

        trackingTarget = newTarget;
        targetTransform = newTarget; // Keep the public reference in sync

        // If we weren't initialized before, we are now
        if (!isInitialized){
            isInitialized = true;
            RestartFollowing(); // Position at closest point immediately
        }
    }

private void LateUpdate()
{
    // Skip if not initialized
    if (!isInitialized || trackingTarget == null) return;

    // FPS-based interval (0 = run every frame)
    if (targetUpdatesPerSecond > 0)
    {
        accumulatedTime += Time.deltaTime;
        if (accumulatedTime < updateInterval) return;
        
        // Use multiple of interval to avoid gradual drift
        accumulatedTime -= updateInterval;
        
        // If frame rate dropped dramatically, prevent multiple catch-up updates
        if (accumulatedTime >= updateInterval)
            accumulatedTime = 0f;
    }
    
    if (!isInitialized || trackingTarget == null)
    {
        // Try to find a target if we don't have one yet
        FindDefaultTarget();
        if (!isInitialized || trackingTarget == null) return;
    }

    // Get spline closed state
    bool isSplineClosed = splineContainer != null && splineContainer.Spline != null && splineContainer.Spline.Closed;

    // Handle sleep mode
    if (HandleSleepMode()) return;

    // For closed splines, check if target is inside or outside
    if (isSplineClosed)
    {
        bool isInside = IsPointInsideClosedSpline(trackingTarget.position);

        // Skip outside tracking if configured to only track inside
        if (!isInside && onlyTrackInsideClosedSpline)
        {
            // Optionally: You could make the follower invisible or disable it here
            return;
        }

        // Handle state transitions
        if (isInside && currentState != PositionState.InsideClosed)
        {
            // Entering the closed spline
            StartTransition(PositionState.InsideClosed);
        }
        else if (!isInside && currentState == PositionState.InsideClosed)
        {
            // Exiting the closed spline
            StartTransition(PositionState.Normal);
        }
    }
    else if (currentState == PositionState.InsideClosed)
    {
        // If the spline is no longer closed but we're in insideClosed state
        StartTransition(PositionState.Normal);
    }

    // Update position based on current state
    switch (currentState)
    {
        case PositionState.Normal:
            UpdateNormalPosition();
            break;

        case PositionState.InsideClosed:
            if (directFollowInsideClosedSpline)
            {
                // Directly follow the target when inside closed spline
                UpdateDirectFollowPosition();
            }
            else
            {
                // Use original inside closed spline behavior
                UpdateInsideClosedPosition();
            }
            break;

        case PositionState.Transitioning:
            UpdateTransitionPosition();
            break;
    }
}

// New method to directly follow the target
private void UpdateDirectFollowPosition()
{
    // Simply follow the target directly with the offset applied
    Vector3 targetPosition = trackingTarget.position + currentOffset;
    
    // Apply smoothing if desired
    followObject.transform.position = Vector3.Lerp(
        followObject.transform.position,
        targetPosition,
        Time.deltaTime * movementSpeed * (1 - smoothing));
}

    private bool HandleSleepMode(){
        if (isSleeping){
            // Only check for wake-up periodically to save performance
            if (Time.time < nextSleepCheckTime) return true;

            // Get approximate spline bounds for fast rejection
            Bounds splineBounds = GetApproximateSplineBounds();
            splineBounds.Expand(sleepThreshold); // Expand by sleep threshold

            // Quick check if target is far outside the expanded bounds
            if (!splineBounds.Contains(trackingTarget.position)){
                // Still too far away, schedule next check
                nextSleepCheckTime = Time.time + sleepCheckInterval;
                return true;
            }

            // More detailed check - find actual distance to spline
            float distance = GetDistanceToSpline(trackingTarget.position);

            if (distance > sleepThreshold){
                // Still too far, stay asleep
                nextSleepCheckTime = Time.time + sleepCheckInterval;
                return true;
            }
            else{
                // Wake up - we're close enough to the spline now
                isSleeping = false;
            }
        }
        else{
            // Check if we should enter sleep mode
            float distanceToSpline = GetDistanceToSpline(trackingTarget.position);
            if (distanceToSpline > sleepThreshold){
                isSleeping = true;
                nextSleepCheckTime = Time.time + sleepCheckInterval;
                return true;
            }
        }

        return false;
    }

    private void StartTransition(PositionState targetState){
        // Set up transition
        transitionStartPosition = followObject.transform.position;
        transitionProgress = 0f;
        currentState = PositionState.Transitioning;
        transitionTargetState = targetState;

        // Set up target position based on the destination state
        if (targetState == PositionState.Normal){
            // Find the closest point on the spline to the target
            float closestT = FindRawClosestPointOnSpline(trackingTarget.position);
            currentSplinePosition = closestT;
            lastSplinePosition = closestT; // Reset direction tracking

            // Find the closest point on the spline boundary
            Vector3 boundaryPoint = EvaluateSplinePosition(closestT);

            // Transitioning to normal spline-following - target boundary point
            transitionTargetPosition = boundaryPoint;
            if (currentOffset != Vector3.zero){
                transitionTargetPosition = ApplyOffsetToSplinePosition(transitionTargetPosition, closestT);
            }

            // Set a faster transition time for exiting closed splines to stay in bounds
            // We do this by artificially advancing the transition progress
            transitionProgress = 0.3f; // Start 30% into the transition for faster response
        }
        else{
            // Transitioning to inside closed spline
            transitionTargetPosition = GetInsideClosedPosition();
        }
    }

    private void UpdateTransitionPosition(){
        // Progress the transition - use faster speed when exiting closed spline
        float transitionSpeed = (transitionTargetState == PositionState.Normal) ? exitTransitionSpeedMultiplier : 1.0f;

        transitionProgress += (Time.deltaTime / transitionTime) * transitionSpeed;

        if (transitionProgress >= 1.0f){
            // Transition complete
            currentState = transitionTargetState;
            followObject.transform.position = transitionTargetPosition;
        }
        else{
            // Custom smooth transition - Ease out quad for smoother finish
            float t = 1.0f - Mathf.Pow(1.0f - transitionProgress, 2.0f);
            followObject.transform.position = Vector3.Lerp(
                transitionStartPosition,
                transitionTargetPosition,
                t);

            // Update target position during transition if needed
            if (transitionTargetState == PositionState.Normal){
                // For exiting a closed spline:
                // 1. We want to move quickly to the spline boundary
                // 2. We want to maintain focus on the exit point rather than following the target

                // Find the current closest point on spline
                float newClosestT = FindRawClosestPointOnSpline(trackingTarget.position);
                Vector3 newClosestPoint = EvaluateSplinePosition(newClosestT);

                // Check if this is significantly different from our current target
                // (prevents minor fluctuations but allows major changes)
                if (Vector3.Distance(newClosestPoint, transitionTargetPosition) > proximityThreshold * 0.5f){
                    transitionTargetPosition = newClosestPoint;
                    if (currentOffset != Vector3.zero){
                        transitionTargetPosition = ApplyOffsetToSplinePosition(transitionTargetPosition, newClosestT);
                    }

                    currentSplinePosition = newClosestT;
                    lastSplinePosition = newClosestT; // Update direction tracking
                }
            }
            else if (transitionTargetState == PositionState.InsideClosed){
                // Update target if we're transitioning to inside (could follow target)
                transitionTargetPosition = GetInsideClosedPosition();
            }
        }
    }

    private Vector3 GetInsideClosedPosition(){
        if (insidePositionMode == ClosedSplinePositionMode.RelativeToTarget){
            // Position relative to target with offset
            return trackingTarget.position + currentOffset;
        }
        else{
            // Use fixed offset from spline center point
            return splineContainer.transform.position + currentOffset;
        }
    }

    private void UpdateInsideClosedPosition(){
        // When inside closed spline - position near target or fixed position
        Vector3 targetPosition = GetInsideClosedPosition();

        // Smooth the movement
        followObject.transform.position = Vector3.Lerp(
            followObject.transform.position,
            targetPosition,
            Time.deltaTime * movementSpeed * (1 - transitionSmoothing));
    }

    private void UpdateNormalPosition(){
        // Check if target is close enough to the spline
        bool inProximity = closestDistanceToSpline <= proximityThreshold;

        // Only update position along spline if the target is in proximity
        if (inProximity){
            // Find the closest point on the spline to the target
            float closestT = FindClosestPointWithDirectionLock(trackingTarget.position);

            // Smooth the movement along the spline
            currentSplinePosition = Mathf.Lerp(currentSplinePosition, closestT, Time.deltaTime * movementSpeed * (1 - smoothing));

            // Position the follow object along the spline
            Vector3 splinePosition = EvaluateSplinePosition(currentSplinePosition);

            // Check if offset has changed and update target
            if (targetOffset != positionOffset){
                targetOffset = positionOffset;
            }

            // Smoothly transition the current offset towards target
            currentOffset = Vector3.Lerp(currentOffset, targetOffset,
                Time.deltaTime * offsetTransitionSpeed);

            // Apply offset if needed
            if (currentOffset != Vector3.zero){
                Vector3 offsetPosition = ApplyOffsetToSplinePosition(splinePosition, currentSplinePosition);
                followObject.transform.position = offsetPosition;
            }
            else{
                followObject.transform.position = splinePosition;
            }
        }
        // When not in proximity, the follow object stays where it is
    }

    private Vector3 ApplyOffsetToSplinePosition(Vector3 splinePosition, float t){
        // To apply the offset properly, we need to know the spline direction
        Vector3 splineDirection = EvaluateSplineDirection(t);

        // Create a local coordinate system based on the spline
        Vector3 forward = splineDirection.normalized;
        Vector3 up = Vector3.up; // Default up direction
        Vector3 right = Vector3.Cross(up, forward).normalized;
        up = Vector3.Cross(forward, right).normalized; // Re-calculate up for orthogonality

        // Apply the offset in this local coordinate system
        return splinePosition +
               right * currentOffset.x +
               up * currentOffset.y +
               forward * currentOffset.z;
    }

    // Gets the approximate distance from a point to the spline
    private float GetDistanceToSpline(Vector3 point){
        // Reset closest distance tracking
        closestDistanceToSpline = float.MaxValue;

        // Sample points along the spline to find closest one to the point
        for (int i = 0; i <= splineSampleCount; i++){
            float t = (float)i / splineSampleCount;
            Vector3 pointOnSpline = EvaluateSplinePosition(t);
            float distance = Vector3.Distance(pointOnSpline, point);

            if (distance < closestDistanceToSpline){
                closestDistanceToSpline = distance;
                closestPointOnSpline = pointOnSpline;

                // Early exit optimization - if we're already within threshold, no need to check more points
                if (closestDistanceToSpline <= proximityThreshold){
                    break;
                }
            }
        }

        return closestDistanceToSpline;
    }

    // Get approximate bounds of the spline for quick rejection tests
    private Bounds GetApproximateSplineBounds(){
        // Start with a point on the spline
        Vector3 firstPoint = EvaluateSplinePosition(0);
        Bounds bounds = new Bounds(firstPoint, Vector3.zero);

        // Sample a few points to create a bounding box
        int boundsSamples = 10; // Keep this low for performance
        for (int i = 1; i <= boundsSamples; i++){
            float t = (float)i / boundsSamples;
            bounds.Encapsulate(EvaluateSplinePosition(t));
        }

        return bounds;
    }

    // Improved method for finding closest point with direction consistency
    private float FindClosestPointWithDirectionLock(Vector3 targetPosition)
    {
        // First, find the raw closest point
        float rawClosestT = FindRawClosestPointOnSpline(targetPosition);
        
        // For first initialization, just use the raw closest point
        if (lastSplinePosition == 0f && currentSplinePosition == 0f)
        {
            lastSplinePosition = rawClosestT;
            return rawClosestT;
        }
        
        // Check if we're on a closed spline
        bool isClosedSpline = splineContainer != null && 
                             splineContainer.Spline != null && 
                             splineContainer.Spline.Closed;
        
        if (!isClosedSpline)
        {
            // For open splines, just use the raw closest point
            lastSplinePosition = rawClosestT;
            return rawClosestT;
        }
        
        // For closed splines, we need to handle equidistant points
        
        // 1. Check if there are multiple equidistant points
        bool hasEquidistantPoints = CheckForEquidistantPoints(targetPosition, rawClosestT);
        
        if (hasEquidistantPoints)
        {
            // We have equidistant points - use direction consistency

            // Update the direction based on recent movement
            float signedDistance = CalculateSignedDistance(lastSplinePosition, currentSplinePosition, true);
            
            // Only change direction if it's significant and consistent
            if (Mathf.Abs(signedDistance) > 0.01f)
            {
                float newDirection = Mathf.Sign(signedDistance);
                
                // Change direction only if it's been consistent for a while
                if (newDirection != currentDirection)
                {
                    directionChangeTimer += Time.deltaTime;
                    if (directionChangeTimer > 0.5f) // 0.5 seconds of consistent opposite direction
                    {
                        currentDirection = newDirection;
                        directionChangeTimer = 0f;
                    }
                }
                else
                {
                    directionChangeTimer = 0f; // Reset timer if direction matches
                }
            }
            
            // Calculate a biased position based on current direction
            float biasedT = CalculateBiasedPosition(currentSplinePosition, currentDirection, directionBias * Time.deltaTime);
            
            // Update last position and return the biased position
            lastSplinePosition = currentSplinePosition;
            return biasedT;
        }
        else
        {
            // No equidistant points - regular tracking
            lastSplinePosition = currentSplinePosition;
            return rawClosestT;
        }
    }
    
    // Helper function to check for equidistant points
    private bool CheckForEquidistantPoints(Vector3 targetPosition, float closestT)
    {
        // Find the distance from target to the closest point
        Vector3 closestPoint = EvaluateSplinePosition(closestT);
        float closestDistance = Vector3.Distance(targetPosition, closestPoint);
        
        // Sample around the spline to check for other points with similar distance
        int sampleCount = 12;
        float sampleRange = 0.4f; // 40% of the spline to check
        
        // Start sampling at back half of the range
        float startT = closestT - (sampleRange * 0.5f);
        if (startT < 0) startT += 1f;
        
        int equidistantCount = 0;
        
        for (int i = 0; i <= sampleCount; i++)
        {
            float t = startT + (sampleRange * i / sampleCount);
            t = Mathf.Repeat(t, 1f); // Handle wrap-around for closed splines
            
            // Skip checking the exact closest point
            if (Mathf.Abs(t - closestT) < 0.01f) continue;
            
            Vector3 samplePoint = EvaluateSplinePosition(t);
            float sampleDistance = Vector3.Distance(targetPosition, samplePoint);
            
            // If this point is nearly the same distance as the closest point
            if (Mathf.Abs(sampleDistance - closestDistance) < (closestDistance * equidistantThreshold))
            {
                equidistantCount++;
                if (equidistantCount >= 1) // Found at least one other equidistant point
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    // Calculate signed distance considering wrap-around
    private float CalculateSignedDistance(float fromT, float toT, bool closedSpline)
    {
        if (!closedSpline)
            return toT - fromT;
            
        float directDist = toT - fromT;
        
        // Check if wrapping gives a shorter path
        if (Mathf.Abs(directDist) > 0.5f)
        {
            // We need to wrap around
            if (directDist > 0)
                return directDist - 1f; // Wrapping backward (negative direction)
            else
                return directDist + 1f; // Wrapping forward (positive direction)
        }
        
        return directDist;
    }
    
    // Calculate position biased in the current direction
    private float CalculateBiasedPosition(float currentT, float direction, float bias)
    {
        float biasedT = currentT + (direction * bias);
        
        // Handle wrap-around for closed splines
        biasedT = Mathf.Repeat(biasedT, 1f);
        
        return biasedT;
    }

    // Gets the raw closest point on the spline (without direction locking)
    private float FindRawClosestPointOnSpline(Vector3 targetPosition)
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
    private Vector3 EvaluateSplinePosition(float t){
        // Ensure we have a valid spline to evaluate
        if (splineContainer != null && splineContainer.Spline != null){
            return splineContainer.EvaluatePosition(t);
        }

        return transform.position;
    }

    // Gets the direction (tangent) at a normalized point (0-1) along the spline
    private Vector3 EvaluateSplineDirection(float t){
        // Ensure we have a valid spline to evaluate
        if (splineContainer != null && splineContainer.Spline != null){
            return splineContainer.EvaluateTangent(t);
        }

        return Vector3.forward;
    }

    // Determines if a point is inside a closed spline
    private bool IsPointInsideClosedSpline(Vector3 point){
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

        for (int i = 0; i < polygonSamples; i++){
            float t = (float)i / polygonSamples;
            splinePoints[i] = EvaluateSplinePosition(t);
        }

        // Cast a ray in the positive X direction
        Vector3 rayStart = point;
        Vector3 rayEnd = point + new Vector3(rayLength, 0, 0);

        // Check intersections with each polygon edge
        for (int i = 0; i < splinePoints.Length; i++){
            // Get current edge (line segment)
            Vector3 p1 = splinePoints[i];
            Vector3 p2 = splinePoints[(i + 1) % splinePoints.Length];

            // Check if ray intersects this edge
            if (RayIntersectsLineSegment(rayStart, rayEnd, p1, p2)){
                intersectionCount++;
            }
        }

        // If intersection count is odd, the point is inside
        return (intersectionCount % 2) == 1;
    }

// Checks if a ray intersects a line segment (projected onto XZ plane)
    private bool RayIntersectsLineSegment(Vector3 rayStart, Vector3 rayEnd, Vector3 lineP1, Vector3 lineP2){
        // Project everything onto XZ plane by ignoring Y component for this calculation
        Vector2 rayStart2D = new Vector2(rayStart.x, rayStart.z);
        Vector2 rayEnd2D = new Vector2(rayEnd.x, rayEnd.z);
        Vector2 lineP12D = new Vector2(lineP1.x, lineP1.z);
        Vector2 lineP22D = new Vector2(lineP2.x, lineP2.z);

        // Check if the line segment crosses the ray's path
        // First, check if line segment straddles the ray's X-parallel line
        if ((lineP12D.y > rayStart2D.y && lineP22D.y <= rayStart2D.y) ||
            (lineP22D.y > rayStart2D.y && lineP12D.y <= rayStart2D.y)){
            // Calculate the X-coordinate of the intersection
            float intersectX = lineP12D.x + (rayStart2D.y - lineP12D.y) * (lineP22D.x - lineP12D.x) / (lineP22D.y - lineP12D.y);

            // Check if intersection is along the positive ray
            if (intersectX >= rayStart2D.x){
                return true;
            }
        }

        return false;
    }

    #region Debug Visuals

    private void OnDrawGizmosSelected(){
        if (!showDebugVisuals) return;

        SplineContainer spline = GetComponent<SplineContainer>();
        if (spline == null) return;

        // Draw the spline itself more clearly
        Gizmos.color = new Color(debugColor.r * 0.8f, debugColor.g * 0.8f, debugColor.b * 0.8f, 0.5f);

        // Visualize the spline with sample points
        if (showSamplePoints){
            for (int i = 0; i <= splineSampleCount; i++){
                float t = (float)i / splineSampleCount;
                if (spline.Spline != null){
                    Vector3 point = spline.EvaluatePosition(t);
                    Gizmos.DrawSphere(point, 0.2f);
                }
            }
        }

        // If we're in play mode and initialized, show the relevant debug visuals
        if (Application.isPlaying && isInitialized){
            // Draw a visual indication of the closest point on the spline to the target
            if (trackingTarget != null){
                // Get spline closed state
                bool isSplineClosed = spline != null && spline.Spline != null && spline.Spline.Closed;

                // Draw different indicators based on state
                switch (currentState){
                    case PositionState.InsideClosed:
                        // Inside closed spline indicator
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawLine(trackingTarget.position, followObject.transform.position);
                        Gizmos.DrawWireSphere(trackingTarget.position, 1.0f);

                        // Draw "inside" icon
                        Gizmos.DrawIcon(trackingTarget.position + Vector3.up * 2f, "d_greenLight", true);
                        break;

                    case PositionState.Transitioning:
                        // Transitioning between states
                        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
                        Gizmos.DrawLine(transitionStartPosition, transitionTargetPosition);

                        // Draw progress indicator
                        Gizmos.DrawSphere(Vector3.Lerp(transitionStartPosition, transitionTargetPosition, transitionProgress), 0.3f);
                        break;

                    case PositionState.Normal:
                        // Normal state - show proximity
                        Gizmos.color = closestDistanceToSpline <= proximityThreshold ? Color.green : Color.yellow;
                        Gizmos.DrawLine(trackingTarget.position, closestPointOnSpline);
                        Gizmos.DrawSphere(closestPointOnSpline, 0.5f);
                        break;
                }

                // Draw sleep state indicator
                if (isSleeping){
                    Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
                    DrawSleepIndicator();
                }

                // Draw a dashed line from the closest point to the actual follower position
                Vector3 followerPos = followObject.transform.position;
                Gizmos.color = debugColor;
                Gizmos.DrawLine(followerPos, closestPointOnSpline);

                // Visualize the current position on the spline
                Vector3 currentPos = EvaluateSplinePosition(currentSplinePosition);
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(currentPos, 0.4f);

                // Visualize the sleep threshold
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f); // Orange, semi-transparent
                Bounds splineBounds = GetApproximateSplineBounds();
                splineBounds.Expand(sleepThreshold);
                Gizmos.DrawWireCube(splineBounds.center, splineBounds.size);
                
                // Visualize the tracking direction
                if (isSplineClosed)
                {
                    // Draw an arrow indicating the current tracking direction
                    float arrowT = currentSplinePosition + (currentDirection * 0.05f);
                    arrowT = Mathf.Repeat(arrowT, 1f);
                    Vector3 arrowPos = EvaluateSplinePosition(arrowT);
                    
                    Gizmos.color = new Color(1f, 0.3f, 0.3f); // Reddish
                    Gizmos.DrawLine(currentPos, arrowPos);
                    
                    // Draw a small sphere at the arrow tip
                    Gizmos.DrawSphere(arrowPos, 0.25f);
                }
            }
        }
        else{
            // In edit mode, show a representation of the proximity threshold
            if (spline.Spline != null){
                Gizmos.color = new Color(debugColor.r, debugColor.g, debugColor.b, 0.2f);

                // Display the proximity threshold along the spline
                int tubeSamples = 12; // Lower for better editor performance
                for (int i = 0; i <= tubeSamples; i++){
                    float t = (float)i / tubeSamples;
                    Vector3 point = spline.EvaluatePosition(t);
                    Gizmos.DrawWireSphere(point, proximityThreshold);
                }

                // If closed spline, draw a special indicator
                if (spline.Spline.Closed){
                    Gizmos.color = new Color(0f, 1f, 1f, 0.15f); // Cyan, very transparent

                    // Draw a filled area to represent the inside of the closed spline
                    // (This is an approximation since we can't actually draw filled polygons with Gizmos)
                    Vector3 center = spline.transform.position;
                    for (int i = 0; i < tubeSamples; i++){
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

    private void DrawSleepIndicator(){
        Vector3 pos = transform.position + Vector3.up * 2f;

        // Draw sleep indicator
        Gizmos.DrawIcon(pos, "console.infoicon.sml", true);

        // Draw sleep bounds
        Bounds splineBounds = GetApproximateSplineBounds();
        splineBounds.Expand(sleepThreshold);
        Gizmos.DrawWireCube(splineBounds.center, splineBounds.size);
    }
    
    #endregion
}