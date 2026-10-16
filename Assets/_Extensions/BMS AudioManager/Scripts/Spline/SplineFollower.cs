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

    [Header("Branch Stickiness")]
    [Tooltip("When two parts of the spline are about equally close to the target (a U-bend, a figure-8), the follower " +
             "stays on the part it's already on until the other is closer by more than this fraction. Prevents flip-flopping.")]
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

    [Tooltip("Time (seconds) to move from the spline into the closed area when the target enters it")] [Range(0.1f, 3.0f)]
    public float transitionTime = 0.5f;

    [Header("Rejoining the Spline (after leaving a closed spline)")]
    [Tooltip("Maximum speed (m/s) the follower closes the gap back onto the spline when the target leaves the inside of " +
             "a closed spline. Lower = gentler, avoiding a sudden loudness jump as the sound moves.")]
    [Range(0.5f, 20f)]
    public float rejoinMaxSpeed = 3f;

    [Tooltip("Roughly how long (seconds) the rejoin eases in and out. Higher = softer start and finish.")]
    [Range(0.05f, 2f)]
    public float rejoinSmoothTime = 0.5f;

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

    // False until the follower has been placed on the spline once (first placement snaps, later moves are smoothed)
    private bool hasTrackedPosition = false;
    private float currentDirection = 1f; // last movement direction along the spline (1 = forward, -1 = backward) - debug arrow only

    // Leaving the inside of a closed spline: the follower eases from where it was onto the spline at the exit
    // point. The remaining gap is held as an offset from the (moving) spline point and smoothed to zero, so
    // the closing speed is capped by rejoinMaxSpeed no matter how fast the target walks on.
    private bool rejoining = false;
    private bool rejoinStarting = false;
    private Vector3 rejoinOffset;
    private Vector3 rejoinVelocity;

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
        AudioListener listener = FindAnyObjectByType<AudioListener>();
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
        Camera[] cameras = FindObjectsByType<Camera>();
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
        rejoining = false;
        hasTrackedPosition = true;

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

        // Handle state transitions. Compare against where we're HEADING, not just the current state:
        // during a transition currentState is Transitioning, so checking it alone restarted the
        // "entering" transition every frame while inside, and delayed the exit until it finished.
        if (isInside && !IsHeadingInside())
        {
            // Entering the closed spline
            StartTransition(PositionState.InsideClosed);
        }
        else if (!isInside && IsHeadingInside())
        {
            // Exiting the closed spline - re-seeds the spline position at the exit point
            StartTransition(PositionState.Normal);
        }
    }
    else if (IsHeadingInside())
    {
        // If the spline is no longer closed but we're in (or heading to) the insideClosed state
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

// True when the follower is inside a closed spline, or transitioning toward it
private bool IsHeadingInside()
{
    return currentState == PositionState.InsideClosed ||
           (currentState == PositionState.Transitioning && transitionTargetState == PositionState.InsideClosed);
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
        if (targetState == PositionState.Normal){
            // Leaving the closed area: go straight back to normal spline following, re-seeded at the exit
            // point (the spline position went stale while inside). The gap between the follower and the
            // spline is closed by the speed-capped rejoin in UpdateNormalPosition - a timed transition
            // here rushed the sound across that gap and caused a loudness bump.
            currentSplinePosition = SplineUtilities.FindClosestPointNear(splineContainer, trackingTarget.position,
                currentSplinePosition, Mathf.Max(50, splineSampleCount * 2), equidistantThreshold);
            hasTrackedPosition = true;
            currentState = PositionState.Normal;

            rejoining = true;
            rejoinStarting = true;   // the gap is measured on the next position update
            rejoinVelocity = Vector3.zero;
            return;
        }

        // Entering the closed area: timed transition from the spline toward the inside position
        transitionStartPosition = followObject.transform.position;
        transitionProgress = 0f;
        currentState = PositionState.Transitioning;
        transitionTargetState = targetState;
        transitionTargetPosition = GetInsideClosedPosition();
    }

    private void UpdateTransitionPosition(){
        // Transitions only run when entering the closed area (leaving uses the rejoin in UpdateNormalPosition)
        transitionProgress += Time.deltaTime / transitionTime;

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

            // Keep aiming at the inside position (it may follow the target)
            transitionTargetPosition = GetInsideClosedPosition();
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
            // Find the closest point on the spline to the target (sticking to the current part of the
            // spline when another part is about equally close)
            float closestT = SplineUtilities.FindClosestPointNear(splineContainer, trackingTarget.position,
                currentSplinePosition, Mathf.Max(50, splineSampleCount * 2), equidistantThreshold);

            if (!hasTrackedPosition)
            {
                // First placement: snap to the closest point rather than gliding in from the spline's start
                currentSplinePosition = closestT;
                hasTrackedPosition = true;
            }
            else
            {
                // Smooth the movement along the spline - always the shortest way round. A plain Lerp on t
                // went the long way whenever the target crossed a closed spline's first knot (where t wraps 1 -> 0).
                bool closed = splineContainer.Spline.Closed;
                float delta = SplineUtilities.ShortestDelta(currentSplinePosition, closestT, closed);
                if (Mathf.Abs(delta) > 0.0001f) currentDirection = Mathf.Sign(delta);
                currentSplinePosition = SplineUtilities.StepTowards(currentSplinePosition, closestT,
                    Time.deltaTime * movementSpeed * (1 - smoothing), closed);
            }

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
            Vector3 finalPosition = currentOffset != Vector3.zero
                ? ApplyOffsetToSplinePosition(splinePosition, currentSplinePosition)
                : splinePosition;

            // Just left the inside of a closed spline - ease onto the spline at a capped speed rather than snapping
            if (rejoining){
                if (rejoinStarting){
                    rejoinOffset = followObject.transform.position - finalPosition;
                    rejoinStarting = false;
                }

                rejoinOffset = Vector3.SmoothDamp(rejoinOffset, Vector3.zero, ref rejoinVelocity,
                    rejoinSmoothTime, rejoinMaxSpeed, Time.deltaTime);

                if (rejoinOffset.sqrMagnitude < 0.0004f) // within 2 cm - hand over to normal following
                    rejoining = false;
                else
                    finalPosition += rejoinOffset;
            }

            followObject.transform.position = finalPosition;
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