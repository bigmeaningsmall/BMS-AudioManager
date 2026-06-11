using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Handles sleep mode optimization for spline followers
/// </summary>
[System.Serializable]
public class SplineSleepManager
{
    [Header("Sleep Settings")]
    [Tooltip("Distance beyond which the follower will enter sleep mode")]
    [Range(10f, 200f)]
    public float sleepThreshold = 30f;
    
    [Tooltip("How often to check if we should wake up when sleeping (in seconds)")]
    [Range(0.1f, 5f)]
    public float sleepCheckInterval = 0.5f;

    // Private state
    private bool isSleeping = false;
    private float nextSleepCheckTime = 0f;
    private SplineContainer splineContainer;

    public bool IsSleeping => isSleeping;

    public void Initialize(SplineContainer container)
    {
        splineContainer = container;
    }

    /// <summary>
    /// Checks and handles sleep mode. Returns true if sleeping (skip main update)
    /// </summary>
    public bool HandleSleepMode(Transform target)
    {
        if (target == null || splineContainer == null) return false;

        if (isSleeping)
        {
            return HandleWakeUpCheck(target);
        }
        else
        {
            return CheckForSleep(target);
        }
    }

    private bool HandleWakeUpCheck(Transform target)
    {
        // Only check periodically for performance
        if (Time.time < nextSleepCheckTime) return true;

        // Quick bounds check first
        Bounds splineBounds = SplineUtilities.GetApproximateSplineBounds(splineContainer);
        splineBounds.Expand(sleepThreshold);

        if (!splineBounds.Contains(target.position))
        {
            // Still too far away
            nextSleepCheckTime = Time.time + sleepCheckInterval;
            return true;
        }

        // More detailed distance check
        float distance = SplineUtilities.GetDistanceToSpline(splineContainer, target.position);

        if (distance > sleepThreshold)
        {
            // Still too far, stay asleep
            nextSleepCheckTime = Time.time + sleepCheckInterval;
            return true;
        }
        else
        {
            // Wake up - close enough now
            isSleeping = false;
            return false;
        }
    }

    private bool CheckForSleep(Transform target)
    {
        float distanceToSpline = SplineUtilities.GetDistanceToSpline(splineContainer, target.position);
        
        if (distanceToSpline > sleepThreshold)
        {
            isSleeping = true;
            nextSleepCheckTime = Time.time + sleepCheckInterval;
            return true;
        }

        return false;
    }

    public void ForceSleep()
    {
        isSleeping = true;
        nextSleepCheckTime = Time.time + sleepCheckInterval;
    }

    public void ForceWakeUp()
    {
        isSleeping = false;
    }
}