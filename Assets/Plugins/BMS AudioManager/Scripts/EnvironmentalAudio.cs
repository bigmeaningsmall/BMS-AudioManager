using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles spatial audio in environments with occlusion and reverb effects
/// based on the surrounding environment, including sound path bounces.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class EnvironmentalAudio : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The listener transform (usually the main camera or player)")]
    [SerializeField] private Transform listener;
    [Tooltip("Which layers are considered walls/obstacles")]
    [SerializeField] private LayerMask wallMask = 1; // Default layer
    
    [Header("Occlusion Settings")]
    [Tooltip("Volume when sound is completely occluded with no viable bounce paths")]
    [Range(0f, 1f)]
    [SerializeField] private float fullyOccludedVolume = 0.1f;
    [Tooltip("Base volume when sound has direct line of sight")]
    [Range(0f, 1f)]
    [SerializeField] private float directVolume = 0.8f;
    [Tooltip("Low-pass filter cutoff when sound is completely occluded")]
    [Range(0f, 22000f)]
    [SerializeField] private float fullyOccludedCutoffFrequency = 800f;
    
    [Header("Bounce Settings")]
    [Tooltip("Maximum number of bounces to calculate (higher = more CPU usage)")]
    [Range(0, 4)]
    [SerializeField] private int maxBounces = 2;
    [Tooltip("Maximum ray distance for bounce calculations")]
    [Range(5f, 100f)]
    [SerializeField] private float maxBounceDistance = 30f;
    [Tooltip("Volume reduction per bounce (multiplicative)")]
    [Range(0.1f, 1f)]
    [SerializeField] private float bounceVolumeReduction = 0.6f;
    [Tooltip("Low-pass filter reduction per bounce (multiplicative)")]
    [Range(0.1f, 1f)]
    [SerializeField] private float bounceLowPassReduction = 0.7f;
    
    [Header("Reverb Settings")]
    [Tooltip("Maximum distance to check for walls")]
    [SerializeField] private float maxAnalysisRayDistance = 20f;
    [Tooltip("How often to update environment analysis (seconds)")]
    [Range(0.1f, 2f)]
    [SerializeField] private float analysisInterval = 0.5f;
    [Tooltip("Enable custom reverb instead of presets")]
    [SerializeField] private bool useCustomReverb = true;
    [Tooltip("Reverb preset to use if custom reverb is disabled")]
    [SerializeField] private AudioReverbPreset fallbackReverbPreset = AudioReverbPreset.Hallway;
    
    [Header("Custom Reverb Parameters")]
    [Tooltip("Base room size for reverb")]
    [Range(0f, 1f)]
    [SerializeField] private float baseRoomSize = 0.5f;
    [Tooltip("Base reverb decay time (seconds)")]
    [Range(0.1f, 10f)]
    [SerializeField] private float baseDecayTime = 1.5f;
    [Tooltip("Dry level for direct sound (-10000 to 0 dB)")]
    [Range(-10000f, 0f)]
    [SerializeField] private float baseDryLevel = 0f;
    [Tooltip("Wet level for reverb (-10000 to 0 dB)")]
    [Range(-10000f, 0f)]
    [SerializeField] private float baseWetLevel = -1000f;
    [Tooltip("Base reflections level (-10000 to 1000 dB)")]
    [Range(-10000f, 1000f)]
    [SerializeField] private float baseReflectionsLevel = -900f;
    
    [Header("Debug Visualization")]
    [SerializeField] private bool showRays = true;
    [SerializeField] private Color directPathColor = Color.green;
    [SerializeField] private Color occludedPathColor = Color.red;
    [SerializeField] private Color bouncePathColor = Color.cyan;
    [SerializeField] private Color analysisRayColor = Color.yellow;
    [SerializeField] private float rayDisplayDuration = 1f;
    
    // Component references
    private AudioSource audioSource;
    private AudioReverbFilter reverbFilter;
    private AudioLowPassFilter lowPassFilter;
    
    // Analysis data
    private float nextAnalysisTime;
    private List<DebugRay> debugRays = new List<DebugRay>();
    
    // Sound path tracking
    private bool hasDirectPath = false;
    private List<BouncePath> validBouncePaths = new List<BouncePath>();
    private float currentVolumeTarget = 1f;
    private float currentLowPassTarget = 22000f;
    
    // Data structure for debug ray visualization
    private class DebugRay
    {
        public Vector3 start;
        public Vector3 end;
        public Color color;
        public float endTime;
        
        public DebugRay(Vector3 start, Vector3 end, Color color, float duration)
        {
            this.start = start;
            this.end = end;
            this.color = color;
            this.endTime = Time.time + duration;
        }
    }
    
    // Structure to represent a bounce path
    private class BouncePath
    {
        public List<Vector3> points = new List<Vector3>(); // The points along the path
        public int bounceCount => points.Count - 2; // Number of bounces (excludes start and end points)
        public float totalDistance = 0f;
        public float attenuation = 1f; // Volume multiplier for this path
        public float lowPassFactor = 1f; // Low-pass filter factor (1 = full bandwidth)
        
        public BouncePath(Vector3 start, Vector3 end)
        {
            points.Add(start);
            points.Add(end);
            totalDistance = Vector3.Distance(start, end);
        }
        
        public void AddBouncePoint(Vector3 point)
        {
            // Insert bounce point before the end point
            points.Insert(points.Count - 1, point);
            
            // Recalculate total distance
            totalDistance = 0f;
            for (int i = 0; i < points.Count - 1; i++)
            {
                totalDistance += Vector3.Distance(points[i], points[i+1]);
            }
        }
        
        // Calculate path quality (lower is better)
        public float CalculateQuality()
        {
            // Simple quality formula: distance + bounce penalty
            return totalDistance + (bounceCount * 5f);
        }
    }
    
    private void Awake()
    {
        // Get or add required components
        audioSource = GetComponent<AudioSource>();
        
        // Log initial settings for debugging
        Debug.Log($"[EnvironmentalAudio] Initializing on {gameObject.name}");
        
        // Store initial volume for reference
        float initialVolume = audioSource.volume;
        Debug.Log($"[EnvironmentalAudio] Initial AudioSource volume: {initialVolume}");
        
        // Add and configure reverb filter
        if (GetComponent<AudioReverbFilter>() == null) {
            reverbFilter = gameObject.AddComponent<AudioReverbFilter>();
            Debug.Log("[EnvironmentalAudio] Added AudioReverbFilter component");
        } else {
            reverbFilter = GetComponent<AudioReverbFilter>();
            Debug.Log($"[EnvironmentalAudio] Using existing AudioReverbFilter - current preset: {reverbFilter.reverbPreset}");
        }
            
        // Add and configure low pass filter
        if (GetComponent<AudioLowPassFilter>() == null) {
            lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
            Debug.Log("[EnvironmentalAudio] Added AudioLowPassFilter component");
        } else {
            lowPassFilter = GetComponent<AudioLowPassFilter>();
            Debug.Log($"[EnvironmentalAudio] Using existing AudioLowPassFilter - current cutoff: {lowPassFilter.cutoffFrequency}Hz");
        }
        
        // Initialize filters
        if (useCustomReverb)
        {
            // Set to User preset to enable custom parameter adjustment
            reverbFilter.reverbPreset = AudioReverbPreset.User;
            
            // Initialize custom reverb parameters
            reverbFilter.dryLevel = baseDryLevel;
            reverbFilter.room = baseRoomSize;
            reverbFilter.decayTime = baseDecayTime;
            reverbFilter.reverbLevel = baseWetLevel;
            reverbFilter.reflectionsLevel = baseReflectionsLevel;
            
            Debug.Log("[EnvironmentalAudio] Using custom reverb settings");
        }
        else
        {
            reverbFilter.reverbPreset = AudioReverbPreset.Off;
            Debug.Log("[EnvironmentalAudio] Using reverb presets");
        }
        
        // Initialize lowpass filter
        lowPassFilter.cutoffFrequency = 22000f;
        
        // Find listener if not set
        if (listener == null)
        {
            if (Camera.main != null) {
                listener = Camera.main.transform;
                Debug.Log($"[EnvironmentalAudio] Using main camera as listener: {listener.name}");
            } else {
                Debug.LogWarning("[EnvironmentalAudio] No listener assigned and no main camera found!");
            }
        } else {
            Debug.Log($"[EnvironmentalAudio] Using assigned listener: {listener.name}");
        }
        
        // Verify that AudioSource is configured properly
        if (audioSource.spatialBlend < 0.5f) {
            Debug.LogWarning($"[EnvironmentalAudio] AudioSource has low spatial blend ({audioSource.spatialBlend}). " +
                             "Consider increasing for better spatial effects.");
        }
    }
    
    private void Start()
    {
        // Log initialization for debugging
        Debug.Log($"[EnvironmentalAudio] Starting on {gameObject.name} with audio source {audioSource.clip?.name ?? "No clip"}");
        Debug.Log($"[EnvironmentalAudio] Using wall mask: {LayerMaskToString(wallMask)}");
        Debug.Log($"[EnvironmentalAudio] Max bounces: {maxBounces}, Max bounce distance: {maxBounceDistance}m");
        
        // Check if listener is assigned
        if (listener == null)
        {
            Debug.LogWarning("[EnvironmentalAudio] No listener assigned! Finding main camera as fallback.");
        }
        
        // Force an immediate environment analysis
        AnalyzeEnvironment();
    }
    
    // Helper method to convert layer mask to readable string
    private string LayerMaskToString(LayerMask mask)
    {
        var layers = "";
        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                layers += LayerMask.LayerToName(i) + ", ";
            }
        }
        return string.IsNullOrEmpty(layers) ? "None" : layers.TrimEnd(',', ' ');
    }
    
    private void Update()
    {
        if (!listener) return;
        
        // Update sound paths
        CalculateSoundPaths();
        
        // Update audio parameters based on paths
        UpdateAudioParameters();
        
        // Analyze environment on interval
        if (Time.time >= nextAnalysisTime)
        {
            AnalyzeEnvironment();
            nextAnalysisTime = Time.time + analysisInterval;
        }
        
        // Clean up old debug rays
        if (showRays)
        {
            debugRays.RemoveAll(ray => ray.endTime < Time.time);
        }
    }
    
    private void CalculateSoundPaths()
    {
        if (!listener || !audioSource.isPlaying) return;
        
        // Clear previous paths
        hasDirectPath = false;
        validBouncePaths.Clear();
        
        Vector3 sourcePosition = transform.position;
        Vector3 listenerPosition = listener.position;
        Vector3 dirToListener = (listenerPosition - sourcePosition).normalized;
        float distToListener = Vector3.Distance(sourcePosition, listenerPosition);
        
        // Check for direct path first
        if (Physics.Raycast(sourcePosition, dirToListener, out RaycastHit directHit, distToListener, wallMask))
        {
            // Direct path is occluded
            hasDirectPath = false;
            
            // Add debug ray for the occluded direct path
            if (showRays)
            {
                debugRays.Add(new DebugRay(sourcePosition, directHit.point, occludedPathColor, rayDisplayDuration));
            }
            
            Debug.Log($"[EnvironmentalAudio] Direct path occluded by {directHit.collider.name} at {directHit.distance:F2}m");
            
            // If we have bounces enabled, try to find bounce paths
            if (maxBounces > 0)
            {
                FindBouncePaths();
            }
        }
        else
        {
            // Direct path is clear
            hasDirectPath = true;
            
            // Add debug ray for the direct path
            if (showRays)
            {
                debugRays.Add(new DebugRay(sourcePosition, listenerPosition, directPathColor, rayDisplayDuration));
            }
            
            Debug.Log($"[EnvironmentalAudio] Direct path clear to listener at {distToListener:F2}m");
        }
    }
    
    private void FindBouncePaths()
    {
        Vector3 sourcePosition = transform.position;
        Vector3 listenerPosition = listener.position;
        
        // For first-order bounces: cast rays in multiple directions
        int rayCount = 24; // Number of initial rays to cast
        
        for (int i = 0; i < rayCount; i++)
        {
            // Cast rays in a sphere around the source
            float theta = i * Mathf.PI * 2f / rayCount;
            float phi = Mathf.PI * 0.5f; // Just scan the horizontal plane for simplicity
            
            Vector3 direction = new Vector3(
                Mathf.Sin(phi) * Mathf.Cos(theta),
                Mathf.Cos(phi),
                Mathf.Sin(phi) * Mathf.Sin(theta)
            ).normalized;
            
            // Try to cast a ray to a potential reflection surface
            if (Physics.Raycast(sourcePosition, direction, out RaycastHit hit, maxBounceDistance, wallMask))
            {
                // Found a potential bounce point, calculate reflection
                Vector3 bouncePoint = hit.point;
                Vector3 inDirection = direction;
                Vector3 surfaceNormal = hit.normal;
                
                // Calculate the reflected direction using the normal of the hit surface
                Vector3 reflectedDirection = Vector3.Reflect(inDirection, surfaceNormal);
                
                // See if we can reach the listener from this bounce point
                Vector3 bounceToListener = listenerPosition - bouncePoint;
                float distToListener = bounceToListener.magnitude;
                Vector3 bounceToListenerDir = bounceToListener / distToListener;
                
                // Check alignment with reflected direction
                float alignmentFactor = Vector3.Dot(reflectedDirection, bounceToListenerDir);
                
                // Only continue if the reflection is generally pointing toward the listener
                if (alignmentFactor > 0.3f && distToListener <= maxBounceDistance)
                {
                    // Check if we can reach the listener from the bounce point
                    if (!Physics.Raycast(bouncePoint, bounceToListenerDir, out RaycastHit bounceHit, distToListener, wallMask))
                    {
                        // We found a valid 1-bounce path
                        BouncePath path = new BouncePath(sourcePosition, listenerPosition);
                        path.AddBouncePoint(bouncePoint);
                        
                        // Calculate path attenuation and low-pass filter
                        path.attenuation = CalculatePathAttenuation(path);
                        path.lowPassFactor = CalculatePathLowPass(path);
                        
                        // Add to valid paths
                        validBouncePaths.Add(path);
                        
                        // Add debug rays for bounce path
                        if (showRays)
                        {
                            debugRays.Add(new DebugRay(sourcePosition, bouncePoint, bouncePathColor, rayDisplayDuration));
                            debugRays.Add(new DebugRay(bouncePoint, listenerPosition, bouncePathColor, rayDisplayDuration));
                        }
                        
                        Debug.Log($"[EnvironmentalAudio] Found valid 1-bounce path with distance {path.totalDistance:F2}m, " +
                                 $"attenuation: {path.attenuation:F2}, lowpass: {path.lowPassFactor:F2}");
                        
                        // Early out if we only want a few paths
                        if (validBouncePaths.Count >= 3) 
                            break;
                            
                        // If we have maxBounces > 1, we could continue with additional bounces here...
                    }
                }
            }
        }
        
        // Sort paths by quality (lowest = best)
        if (validBouncePaths.Count > 0)
        {
            validBouncePaths.Sort((a, b) => a.CalculateQuality().CompareTo(b.CalculateQuality()));
            Debug.Log($"[EnvironmentalAudio] Found {validBouncePaths.Count} valid bounce paths. Best path quality: {validBouncePaths[0].CalculateQuality():F2}");
        }
        else
        {
            Debug.Log("[EnvironmentalAudio] No valid bounce paths found - sound is fully occluded");
        }
    }
    
    private float CalculatePathAttenuation(BouncePath path)
    {
        // Base attenuation on distance and bounce count
        float distanceAttenuation = Mathf.Clamp01(1.0f - (path.totalDistance / (maxBounceDistance * 2)));
        float bounceAttenuation = Mathf.Pow(bounceVolumeReduction, path.bounceCount);
        
        return distanceAttenuation * bounceAttenuation * directVolume;
    }
    
    private float CalculatePathLowPass(BouncePath path)
    {
        // Each bounce reduces high frequencies
        float lowPassReduction = Mathf.Pow(bounceLowPassReduction, path.bounceCount);
        
        // Map to a frequency range from fully occluded to open
        float maxFrequency = 22000f;
        float range = maxFrequency - fullyOccludedCutoffFrequency;
        
        return fullyOccludedCutoffFrequency + (range * lowPassReduction);
    }
    
    private void UpdateAudioParameters()
    {
        // Determine target volume and low-pass filter values
        if (hasDirectPath)
        {
            currentVolumeTarget = directVolume;
            currentLowPassTarget = 22000f; // Full bandwidth
        }
        else if (validBouncePaths.Count > 0)
        {
            // Use parameters from the best path
            BouncePath bestPath = validBouncePaths[0];
            currentVolumeTarget = bestPath.attenuation;
            currentLowPassTarget = CalculatePathLowPass(bestPath);
        }
        else
        {
            // Fully occluded
            currentVolumeTarget = fullyOccludedVolume;
            currentLowPassTarget = fullyOccludedCutoffFrequency;
        }
        
        // Smooth transitions
        audioSource.volume = Mathf.Lerp(audioSource.volume, currentVolumeTarget, Time.deltaTime * 5f);
        lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency, currentLowPassTarget, Time.deltaTime * 5f);
        
        // Update reverb based on path quality if using custom reverb
        if (useCustomReverb)
        {
            float pathDistance = hasDirectPath ? 
                Vector3.Distance(transform.position, listener.position) : 
                (validBouncePaths.Count > 0 ? validBouncePaths[0].totalDistance : maxBounceDistance);
                
            // Calculate normalized distance for parameter scaling
            float distanceNorm = Mathf.Clamp01(pathDistance / maxBounceDistance);
            
            // Bounce count affects reverb character
            int bounceCount = hasDirectPath ? 0 : 
                (validBouncePaths.Count > 0 ? validBouncePaths[0].bounceCount : 2);
            
            // More distance/bounces = stronger reverb, less dry signal
            float dryLevelMod = hasDirectPath ? 0f : -200f * bounceCount - 500f * distanceNorm;
            float roomSizeMod = 0.2f * bounceCount + 0.3f * distanceNorm;
            float decayTimeMod = 0.5f * bounceCount + 1.0f * distanceNorm;
            float wetLevelMod = 300f * bounceCount + 500f * distanceNorm;
            
            // Apply custom reverb settings with smooth transitions
            float transitionSpeed = Time.deltaTime * 3f;
            reverbFilter.dryLevel = Mathf.Lerp(reverbFilter.dryLevel, 
                Mathf.Clamp(baseDryLevel + dryLevelMod, -10000f, 0f), transitionSpeed);
            reverbFilter.room = Mathf.Lerp(reverbFilter.room, 
                Mathf.Clamp01(baseRoomSize + roomSizeMod), transitionSpeed);
            reverbFilter.decayTime = Mathf.Lerp(reverbFilter.decayTime, 
                Mathf.Clamp(baseDecayTime + decayTimeMod, 0.1f, 20f), transitionSpeed);
            reverbFilter.reverbLevel = Mathf.Lerp(reverbFilter.reverbLevel, 
                Mathf.Clamp(baseWetLevel + wetLevelMod, -10000f, 0f), transitionSpeed);
            reverbFilter.reflectionsLevel = Mathf.Lerp(reverbFilter.reflectionsLevel, 
                Mathf.Clamp(baseReflectionsLevel + 100f * bounceCount, -10000f, 1000f), transitionSpeed);
            
            Debug.Log($"[EnvironmentalAudio] Reverb - Dry: {reverbFilter.dryLevel:F0}, " +
                     $"Room: {reverbFilter.room:F2}, Decay: {reverbFilter.decayTime:F2}, " +
                     $"Wet: {reverbFilter.reverbLevel:F0}, Bounces: {bounceCount}");
        }
        
        // Log current state for debugging
        Debug.Log($"[EnvironmentalAudio] Current audio - Volume: {audioSource.volume:F2}/{currentVolumeTarget:F2}, " +
                 $"LPF: {lowPassFilter.cutoffFrequency:F0}Hz/{currentLowPassTarget:F0}Hz, Direct path: {hasDirectPath}, Bounce paths: {validBouncePaths.Count}");
    }
    
    private void AnalyzeEnvironment()
    {
        // Cast rays in multiple directions to determine environment properties
        int rayCount = 12; // Number of rays to cast (in a circle)
        float totalWidth = 0f;
        int hitCount = 0;
        
        Debug.Log($"[EnvironmentalAudio] Starting environment analysis with {rayCount} rays, max distance: {maxAnalysisRayDistance}m");
        
        // Find the four closest walls in cardinal directions
        float[] distances = new float[4] { maxAnalysisRayDistance, maxAnalysisRayDistance, maxAnalysisRayDistance, maxAnalysisRayDistance };
        Vector3[] directions = new Vector3[4] 
        {
            Vector3.forward,
            Vector3.right,
            Vector3.back,
            Vector3.left
        };
        string[] dirNames = new string[4] { "Forward", "Right", "Back", "Left" };
        
        // Cast rays in cardinal directions first
        for (int i = 0; i < 4; i++)
        {
            if (Physics.Raycast(transform.position, directions[i], out RaycastHit hit, maxAnalysisRayDistance, wallMask))
            {
                distances[i] = hit.distance;
                hitCount++;
                totalWidth += hit.distance;
                
                Debug.Log($"[EnvironmentalAudio] {dirNames[i]} ray hit: {hit.collider.name} at {hit.distance:F2}m");
                
                // Add debug ray
                if (showRays)
                {
                    debugRays.Add(new DebugRay(transform.position, hit.point, analysisRayColor, rayDisplayDuration));
                }
            }
            else 
            {
                Debug.Log($"[EnvironmentalAudio] {dirNames[i]} ray: No hit within {maxAnalysisRayDistance}m");
                
                if (showRays)
                {
                    // Add debug ray for misses
                    debugRays.Add(new DebugRay(transform.position, transform.position + directions[i] * maxAnalysisRayDistance, 
                                              analysisRayColor, rayDisplayDuration * 0.5f));
                }
            }
        }
        
        // Cast additional rays in between
        for (int i = 0; i < rayCount - 4; i++)
        {
            float angle = i * Mathf.PI * 2f / (rayCount - 4);
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, maxAnalysisRayDistance, wallMask))
            {
                hitCount++;
                totalWidth += hit.distance;
                
                // Add debug ray
                if (showRays)
                {
                    debugRays.Add(new DebugRay(transform.position, hit.point, analysisRayColor, rayDisplayDuration * 0.5f));
                }
            }
            else if (showRays)
            {
                // Add debug ray for misses
                debugRays.Add(new DebugRay(transform.position, transform.position + dir * maxAnalysisRayDistance, 
                                          analysisRayColor, rayDisplayDuration * 0.3f));
            }
        }
        
        // Only use environment analysis for reverb if not using custom reverb
        if (!useCustomReverb)
        {
            // If we found no walls, we're in an open space
            if (hitCount == 0)
            {
                Debug.Log("[EnvironmentalAudio] No walls detected - in open space, disabling reverb");
                reverbFilter.reverbPreset = AudioReverbPreset.Off;
                return;
            }
            
            // Calculate average distance to walls
            float avgDistance = totalWidth / hitCount;
            
            // Determine if we're in a corridor by checking if width and length are very different
            float widthDifference = Mathf.Abs(distances[0] - distances[2]);
            float lengthDifference = Mathf.Abs(distances[1] - distances[3]);
            
            // Higher value indicates more corridor-like environment
            float corridorFactor = Mathf.Max(widthDifference, lengthDifference) / avgDistance;
            
            Debug.Log($"[EnvironmentalAudio] Environment analysis: Avg distance: {avgDistance:F2}m, " +
                     $"Corridor factor: {corridorFactor:F2}, Hits: {hitCount}/{rayCount}");
            
            AudioReverbPreset selectedPreset;
            
            // Choose reverb preset based on average distance and corridor factor
            if (corridorFactor > 1.5f)
            {
                // We're likely in a corridor
                if (avgDistance < 3f) {
                    selectedPreset = AudioReverbPreset.Hallway;
                    Debug.Log("[EnvironmentalAudio] Detected: Narrow corridor");
                }
                else if (avgDistance < 8f) {
                    selectedPreset = AudioReverbPreset.Livingroom;
                    Debug.Log("[EnvironmentalAudio] Detected: Medium corridor");
                }
                else {
                    selectedPreset = AudioReverbPreset.Auditorium;
                    Debug.Log("[EnvironmentalAudio] Detected: Large hallway/corridor");
                }
            }
            else
            {
                // We're in a more open or square space
                if (avgDistance < 5f) {
                    selectedPreset = AudioReverbPreset.Room;
                    Debug.Log("[EnvironmentalAudio] Detected: Room (square space)");
                }
                else {
                    selectedPreset = AudioReverbPreset.Cave;
                    Debug.Log("[EnvironmentalAudio] Detected: Large open space");
                }
            }
            
            // Apply preset if it changed
            if (reverbFilter.reverbPreset != selectedPreset) {
                Debug.Log($"[EnvironmentalAudio] Changing reverb preset from {reverbFilter.reverbPreset} to {selectedPreset}");
                reverbFilter.reverbPreset = selectedPreset;
            }
        }
        else
        {
            // Just log environment info when using custom reverb
            if (hitCount > 0) {
                float avgDistance = totalWidth / hitCount;
                Debug.Log($"[EnvironmentalAudio] Environment analysis (custom reverb): Avg distance: {avgDistance:F2}m, Hits: {hitCount}/{rayCount}");
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showRays || !Application.isPlaying) return;
        
        // Draw all debug rays
        foreach (var ray in debugRays)
        {
            Gizmos.color = ray.color;
            Gizmos.DrawLine(ray.start, ray.end);
            Gizmos.DrawSphere(ray.end, 0.1f);
        }
        
        // Draw bounce path spheres if we have any valid paths
        if (validBouncePaths.Count > 0 && hasDirectPath == false)
        {
            // Draw the best path more prominently
            BouncePath bestPath = validBouncePaths[0];
            Gizmos.color = Color.yellow;
            
            // Draw spheres at each point
            for (int i = 0; i < bestPath.points.Count; i++)
            {
                Gizmos.DrawSphere(bestPath.points[i], 0.2f);
                
                // Draw connecting lines if this isn't the last point
                if (i < bestPath.points.Count - 1)
                {
                    Gizmos.DrawLine(bestPath.points[i], bestPath.points[i+1]);
                }
            }
        }
    }
}