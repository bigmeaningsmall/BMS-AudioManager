using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles raytraced audio playback with occlusion and first-order reflections
/// using the image-source method.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class RaytracedAudio_v01 : MonoBehaviour
{
    // Reference to the listener (usually the player's head or camera)
    [Header("References")]
    [SerializeField] private Transform listener;
    // LayerMask defining which layers should be treated as obstacles/walls
    [SerializeField] private LayerMask obstacleMask;

    // Volume multiplier when the direct path to the listener is occluded
    [Header("Occlusion Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float occludedVolume = 0.2f;

    // AudioClip to play for reflections
    [Header("Reflection Settings")]
    [SerializeField] private AudioClip reflectionClip;
    // Base multiplier for reflection volume
    [Range(0f, 1f)]
    [SerializeField] private float reflectionVolumeMultiplier = 0.5f;
    // Minimum time between reflections to prevent overload
    [SerializeField] private float reflectionCooldown = 0.25f;

    // Maximum distance (in meters) for reflections to be considered
    [Header("Max Reflection Distance (m)")]
    [SerializeField] private float maxReflectionDistance = 20f;

    // Speed of sound in air (m/s) for delay calculation
    private const float SpeedOfSound = 343f;

    // Cached AudioSource for playing primary audio
    private AudioSource audioSource;
    // Timestamp when the next reflection is allowed
    private float nextReflectionTime;
    // Array of colliders representing reflective walls
    private Collider[] reflectiveWalls;

    /// <summary>
    /// Struct used for debugging echo positions and pans.
    /// </summary>
    private struct EchoDebug
    {
        public Vector3 Position;
        public float Pan;
    }

    // List to store the last batch of echo debug data for Gizmos
    private readonly List<EchoDebug> _lastEchoes = new();

    /// <summary>
    /// Initializes the AudioSource and ensures it doesn't auto-play.
    /// </summary>
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        // audioSource.playOnAwake = false;
        // audioSource.loop = false;
        // audioSource.Stop();
    }

    /// <summary>
    /// Finds all colliders on the obstacleMask layer to treat as reflective surfaces.
    /// </summary>
    private void Start()
    {
        reflectiveWalls = FindObjectsByType<Collider>(FindObjectsSortMode.None)
            .Where(c => (obstacleMask & (1 << c.gameObject.layer)) != 0)
            .ToArray();
    }

    /// <summary>
    /// Updates occlusion volume and triggers reflections on cooldown while audio is playing.
    /// </summary>
    private void Update()
    {
        // Only run when audio is playing and listener is assigned
        if (!audioSource.isPlaying || !listener) return;

        UpdateOcclusion();

        if (!(Time.time >= nextReflectionTime) || !reflectionClip)
            return;

        DoImageSourceReflections();

        nextReflectionTime = Time.time + reflectionCooldown;
    }

    /// <summary>
    /// Adjusts the main audio volume based on whether the direct path is occluded.
    /// </summary>
    private void UpdateOcclusion()
    {
        Vector3 toListener = listener.position - transform.position;

        // Raycast towards the listener to check for obstacles
        audioSource.volume = Physics.Raycast(transform.position, toListener.normalized, toListener.magnitude, obstacleMask)
            ? occludedVolume
            : .25f;
    }

    /// <summary>
    /// Performs image-source reflections for each cached wall collider.
    /// </summary>
    private void DoImageSourceReflections()
    {
        _lastEchoes.Clear();

        foreach (Collider wall in reflectiveWalls)
        {
            // Construct a plane at the wall's position with its forward as the normal
            Plane plane = new Plane(wall.transform.forward, wall.transform.position);
            // Compute mirrored listener position across that plane
            Vector3 imageListener = ReflectPointAcrossPlane(listener.position, plane);

            // Ray direction towards the mirrored listener
            Vector3 rayDir = (imageListener - transform.position).normalized;
            float maxDist = Vector3.Distance(transform.position, imageListener);

            // Perform raycast; ensure first hit is the current wall
            if (!Physics.Raycast(transform.position, rayDir, out var hit, maxDist, obstacleMask) || hit.collider != wall)
                continue;

            // Calculate pan based on horizontal angle between listener and hit point
            Vector3 toHit = hit.point - listener.position;
            float pan = Vector3.Dot(listener.right, toHit.normalized);
            _lastEchoes.Add(new EchoDebug { Position = hit.point, Pan = pan });

            // Compute distance from source to hit point
            float d1 = Vector3.Distance(transform.position, hit.point);

            // Skip if wall is too far
            if (d1 > maxReflectionDistance)
                continue;

            // Compute distance from hit point to listener
            float d2 = Vector3.Distance(hit.point, listener.position);
            float pathLength = d1 + d2;
            // Apply inverse-square attenuation on the listener leg
            float attenuation = 1f / (1f + d2 * d2);
            float echoVol = audioSource.volume * reflectionVolumeMultiplier * attenuation;

            // Schedule the reflection audio with realistic delay
            StartCoroutine(PlayReflectionWithDelay(hit.point, pathLength, echoVol));
        }
    }

    /// <summary>
    /// Coroutine that waits for calculated delay before playing the reflection clip.
    /// </summary>
    private IEnumerator PlayReflectionWithDelay(Vector3 position, float pathLength, float echoVolume)
    {
        float delay = pathLength / SpeedOfSound;
        yield return new WaitForSeconds(delay);

        AudioSource.PlayClipAtPoint(reflectionClip, position, echoVolume);
    }

    /// <summary>
    /// Reflects a point across the given plane.
    /// </summary>
    private Vector3 ReflectPointAcrossPlane(Vector3 point, Plane plane)
    {
        float d = plane.GetDistanceToPoint(point);
        return point - 2f * d * plane.normal;
    }

    /// <summary>
    /// Draws debug Gizmos for direct path and reflection hit points with panning colors.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!listener)
            return;

        // Draw direct ray to listener
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, listener.position);

        // Draw spheres at reflection points, blue if left pan, red if right
        foreach (EchoDebug debug in _lastEchoes)
        {
            Gizmos.color = (debug.Pan < 0f) ? Color.blue : Color.red;
            Gizmos.DrawSphere(debug.Position, 0.1f);
            Gizmos.DrawLine(listener.position, debug.Position);
        }
    }

    /// <summary>
    /// Starts playback of the primary audio clip and resets reflection timer.
    /// </summary>
    public void StartPlayback()
    {
        if (audioSource.isPlaying)
            return;

        audioSource.Play();
        nextReflectionTime = Time.time;
    }
}