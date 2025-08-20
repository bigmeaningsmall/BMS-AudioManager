using System.Collections;
using UnityEngine;

/// <summary>
/// to be attached to a GameObject that will send an audio event TO PLAY A SOUND EFFECT
/// USAGE:
///     attach this script to a GameObject and call the PlaySFX method from an event trigger or script
///     to play a sound effect with the parameters set in the inspector
///     ...
///     or call the PlaySFX method with a Transform parameter to attach the sound to a different GameObject
/// </summary>

public class AudioEventSenderSFX : MonoBehaviour, IAudioEventSender
{
    
    [Space(20)]
    //  USE THIS AS A TAG TO DETERMINE WHICH EVENT TO SEND (Multiple scripts can be attached to the same object)
    // Loop through the AudioEventSender_SFX scripts on the object and send the event with the matching eventName
    public string eventName = "Custom SFX Event Name"; //for future use //todo - add a custom event name to the AudioEventManager class
    
    [Space(10)] 
    [Header("Sound FX Event Parameters (SFX)")] [Space(5)]
    [Space(20)]
    public string[] sfxName = new string[1]; // The name of the sound effects to play - can be multiple sounds to play randomly
    
    [Space(20)]
    public bool playOnEnabled = true;
    public bool attachSoundToThisTransform = false;
    public Transform transformToAttachTo;
    
    [Space(10)]
    [Range(0, 1f)] public float volume = 1.0f;
    [Range(0, 2f)] public float pitch = 1.0f;
    public bool randomisePitch = true;
    [Range(0, 1f)] public float pitchRange = 0.1f;
    [Range(0, 1f)] public float spatialBlend = 0.5f;
    
    [Space(10)]
    public bool loop = false;

    [Space(10)] 
    public bool randomiseDelay = false;
    [Range(0,5f)]
    public float eventDelay = 0f;
    
    [Space(10)]
    [Range(0,100)]
    public int percentageChanceToPlay = 100;
    
    [Space(10)]
    [Header("3D Audio Settings")]
    public bool useCustom3DSettings = false;
    [Range(0f, 100f)] public float minDistance = 1f;
    [Range(1f, 500f)] public float maxDistance = 500f;
    
    [Space(10)]
    [Header("Position Override")]
    public bool useCustomPosition = false;
    public Vector3 customPosition = Vector3.zero;

    [Header("Collider Settings")] 
    public CollisionType collisionType = CollisionType.Trigger; // Use trigger or collision
    public string targetTag = "Player"; // Tag of the object that can trigger the event

    [Header("Trigger Zone Visualization")]
    [Tooltip("Use transform scale and material for trigger zone visualization")]
    public bool useTransformScale = true;
    [Tooltip("Show trigger info labels in editor")]
    public bool showTriggerInfo = true;
    [Tooltip("Color when trigger is activated")]
    public Color triggerActiveColor = new Color(1f, 0f, 0f, 0.8f); // Red when active

    [Space(20)]
    [Header("TestMode : 'T' to play SFX, 'P' to pause all SFX, 'S' to stop all SFX")]
    public bool testMode = false;

    // For showing activation feedback
    private bool isTriggered = false;
    private float triggerFeedbackTimer = 0f;
    private const float TRIGGER_FEEDBACK_DURATION = 0.2f;
    
    private void OnEnable()
    {
        if (playOnEnabled)
        {
            StartCoroutine(WaitForAudioManagerAndPlay());
        }
    }

    private IEnumerator WaitForAudioManagerAndPlay()
    {
        // Wait until AudioManager.Instance is not null
        while (AudioManager.Instance == null)
        {
            yield return null; // Wait for the next frame
        }
        // Play the sound once AudioManager.Instance is ready
        Play();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (collisionType == CollisionType.Trigger && other.CompareTag(targetTag))
        {
            TriggerActivation();
            Play();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collisionType == CollisionType.Collision && collision.collider.CompareTag(targetTag))
        {
            TriggerActivation();
            Play();
        }
    }

    private void TriggerActivation()
    {
        isTriggered = true;
        triggerFeedbackTimer = TRIGGER_FEEDBACK_DURATION;
    }

    private void Update()
    {
        // Handle trigger feedback timer
        if (isTriggered)
        {
            triggerFeedbackTimer -= Time.deltaTime;
            if (triggerFeedbackTimer <= 0f)
            {
                isTriggered = false;
            }
        }

        // Test mode input
        if (testMode)
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                TriggerActivation();
                Play();
            }
        
            // Use AudioManager's pause state for logging
            if (Input.GetKeyDown(KeyCode.P))
            {
                bool currentState = AudioManager.Instance != null ? AudioManager.Instance.AllSFXPaused : false;
                Debug.Log($"[EventSender] Testing Toggle SFX Pause (currently {(currentState ? "paused" : "playing")})");
                Pause();
            }
        
            if (Input.GetKeyDown(KeyCode.S))
            {
                Debug.Log("[EventSender] Testing Stop All SFX");
                Stop();
            }
        }
    }
    
    public void Play()
    {
        PlaySFX();
    }
    
    // Play SFX with parameters from the inspector
    private void PlaySFX()
    {
        // Transform attachment logic: 
        // 1. This transform (if enabled)
        // 2. Custom transform (if provided) 
        // 3. AudioManager transform (default)
        Transform attachTo = null;
        Vector3 position = default;
    
        if (useCustomPosition)
        {
            // Use custom Vector3 position, will attach to AudioManager
            position = customPosition;
        }
        else if (attachSoundToThisTransform)
        {
            // Attach to this EventSender's transform
            attachTo = this.transform;
        }
        else if (transformToAttachTo != null)
        {
            // Attach to specified custom transform
            attachTo = transformToAttachTo;
        }
        // else: attachTo remains null, AudioManager will use its own transform (default behavior)
    
        // Determine 3D audio settings
        float minDist = useCustom3DSettings ? minDistance : 1f;
        float maxDist = useCustom3DSettings ? maxDistance : 500f;
    
        Debug.Log($"[EventSender] SFX attachment: attachTo={(attachTo?.name ?? "AudioManager(default)")}, position={position}, spatialBlend={spatialBlend}");
        
        float delay = eventDelay;
        if (randomiseDelay)
        {
            delay = Random.Range(0, eventDelay);
        }
        
        // Send the PlaySFX Event
        AudioEventManager.PlaySFX(sfxName, volume, pitch, randomisePitch, pitchRange, spatialBlend, loop, delay, percentageChanceToPlay, attachTo, position, minDist, maxDist, eventName);
    }
    
    // Interface methods to call SFX management functions:
    public void Stop()
    {
        StopSFX();
    }

    public void Pause()
    {
        PauseSFX();
    }
    
    // PauseSFX to use AudioManager's toggle method:
    /// <summary>
    /// Toggle pause/resume all SFX - calls AudioManager.TogglePauseAllSFX
    /// </summary>
    private void PauseSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.TogglePauseAllSFX();
            Debug.Log($"[EventSender] Toggled SFX pause via interface");
        }
        else
        {
            Debug.LogWarning("[EventSender] AudioManager not available for TogglePauseAllSFX");
        }
    }

    // StopSFX (no state management needed):
    /// <summary>
    /// Stop all SFX - calls multiple AudioManager stop methods
    /// </summary>
    private void StopSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.CancelAllDelayedSFX();
            AudioManager.Instance.StopAllLoopedSFX();
            AudioManager.Instance.StopAllSFX(); // This now resets pause state internally
        
            Debug.Log("[EventSender] Called stop methods via interface");
        }
        else
        {
            Debug.LogWarning("[EventSender] AudioManager not available for Stop SFX methods");
        }
    }

    #region Gizmo Visualization

    private void OnDrawGizmosSelected()
    {
        if (!useTransformScale) return;
        
        // Draw trigger info and activation feedback
        DrawTriggerInfo();
        DrawActivationFeedback();
    }

    private void DrawTriggerInfo()
    {
        if (!showTriggerInfo) return;
        
        // Calculate label position based on object bounds
        Bounds bounds = GetObjectBounds();
        Vector3 labelPos = transform.position + Vector3.up * (bounds.size.y * 0.5f + 1f);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        string shapeInfo = GetShapeInfo();
        UnityEditor.Handles.Label(labelPos, 
            $"SFX: {eventName}\n" +
            $"Shape: {shapeInfo}\n" +
            $"Type: {collisionType}\n" +
            $"Tag: {targetTag}\n" +
            $"Scale: {transform.lossyScale}");
        
        // Draw audio range visualization (if spatial)
        if (spatialBlend > 0f)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.1f); // Yellow, very transparent
            Gizmos.DrawSphere(transform.position, maxDistance); // Show max audio range
        }
        #endif
    }

    private void DrawActivationFeedback()
    {
        if (!isTriggered) return;
        
        // Draw activation pulse
        Gizmos.color = triggerActiveColor;
        Bounds bounds = GetObjectBounds();
        float pulseSize = bounds.size.magnitude * 1.2f;
        Gizmos.DrawWireSphere(transform.position, pulseSize);
        
        // Draw activation icon
        #if UNITY_EDITOR
        Vector3 iconPos = transform.position + Vector3.up * (bounds.size.y * 0.5f + 2f);
        UnityEditor.Handles.color = triggerActiveColor;
        UnityEditor.Handles.Label(iconPos, "♪ TRIGGERED ♪");
        #endif
    }

    private Bounds GetObjectBounds()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }
        
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            return collider.bounds;
        }
        
        // Fallback to transform scale
        return new Bounds(transform.position, transform.lossyScale);
    }

    private string GetShapeInfo()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter?.sharedMesh != null)
        {
            return meshFilter.sharedMesh.name;
        }
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            return col.GetType().Name;
        }
        
        return "Unknown";
    }

    #endregion

    #region Helper Methods for Collider Setup

    [ContextMenu("Setup Collider to Match Transform")]
    private void SetupColliderToMatchTransform()
    {
        Collider col = GetComponent<Collider>();
        
        // Detect best collider type based on mesh
        System.Type bestColliderType = DetectBestColliderType();
        
        if (col == null || col.GetType() != bestColliderType)
        {
            // Remove existing collider if wrong type
            if (col != null)
            {
                DestroyImmediate(col);
            }
            
            // Add correct collider type
            col = (Collider)gameObject.AddComponent(bestColliderType);
        }
        
        // Set collider properties
        col.isTrigger = (collisionType == CollisionType.Trigger);
        
        // The collider will automatically use the transform scale
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
        
        Debug.Log($"Set up {bestColliderType.Name} to match transform scale. Use transform scale to adjust trigger zone size.");
    }

    private System.Type DetectBestColliderType()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter?.sharedMesh != null)
        {
            string meshName = meshFilter.sharedMesh.name.ToLower();
            if (meshName.Contains("sphere") || meshName.Contains("icosphere"))
            {
                return typeof(SphereCollider);
            }
            else if (meshName.Contains("capsule"))
            {
                return typeof(CapsuleCollider);
            }
            else if (meshName.Contains("cube") || meshName.Contains("quad") || meshName.Contains("plane"))
            {
                return typeof(BoxCollider);
            }
        }
        
        // Default to box collider
        return typeof(BoxCollider);
    }

    #endregion
}