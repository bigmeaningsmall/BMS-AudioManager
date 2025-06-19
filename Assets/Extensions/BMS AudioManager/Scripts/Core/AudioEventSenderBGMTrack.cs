using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// to be attached to a GameObject that will send an audio event TO PLAY BACKGROUND MUSIC
/// USAGE:
///     attach this script to a GameObject and call the PlayBGM method from an event trigger or script
///     to play music with the parameters set in the inspector
///     ...
///     
/// </summary>

public class AudioEventSenderBGMTrack : MonoBehaviour, IAudioEventSender
{
    [Space(20)]
    // USE THIS TO DETERMINE WHICH EVENT TO SEND (Mutiple scripts can be attached to the same object)
    // Loop through the AudioEventSender_BGM scripts on the object and send the event with the matching eventName
    public string eventName = "Custom BGM Event Name"; //for future use

    
    [Space(10)]
    [Header("Background Music Event Parameters (BGM)")]
    [Space(20)]
    [Tooltip("The track number of the music to play - used if no name is given -1 to ignore")]
    public int musicTrackNumber = 0; // WILL USE THE TRACK NUMBER IF NO NAME IS GIVEN
    public string musicTrackName = "TRACK NAME HERE"; //IF NO NAME IS GIVEN, THE TRACK NUMBER WILL BE USED
    
    [Space(20)]
    public bool playOnEnabled = true;
    public bool loopBGM = true;
    
    [Space(10)]
    [Range(0,1f)]
    public float volume = 0.8f;
    public FadeType fadeType = FadeType.FadeInOut;
    [Range(0,10f)]
    public float fadeDuration = 1.5f;
    
    [Space(10)] 
    [Range(0,5f)]
    public float eventDelay = 0f;
    
    [Header("Collider Settings")]
    public CollisionType collisionType = CollisionType.Trigger;
    public string targetTag = "Player";
    public bool stopOnExit = true;

    [Header("Trigger Zone Visualization")]
    [Tooltip("Use transform scale and material for trigger zone visualization")]
    public bool useTransformScale = true;
    [Tooltip("Show trigger info labels in editor")]
    public bool showTriggerInfo = true;
    [Tooltip("Color when trigger is activated")]
    public Color triggerActiveColor = new Color(0f, 0f, 1f, 0.8f); // Blue when active (music theme)
    
    [Space(20)]
    [Header("TestMode : 'M' to play music, 'N' to stop, 'B' to pause")]
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

    private void OnDisable(){
        if (AudioManager.Instance != null && AudioManager.Instance.isActiveAndEnabled)
        {
            Stop();
        }
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
            if (Input.GetKeyDown(KeyCode.M))
            {
                TriggerActivation();
                Play();
            }
            if (Input.GetKeyDown(KeyCode.N))
            {
                Stop();
            }
            if (Input.GetKeyDown(KeyCode.B))
            {
                Pause();
            }
        }
    }

    public void Play()
    {
        if(eventDelay <= 0)
        {
            PlayBGM();
        }
        else
        {
            StartCoroutine(PlayBGM_Delayed(eventDelay));
        }
    }
    private void PlayBGM()
    {
        //send the PlayBGM Event with parameters from the inspector
        AudioEventManager.playBGMTrack(musicTrackNumber, musicTrackName, volume, fadeType, fadeDuration, loopBGM, eventName);
    }
    
    private IEnumerator PlayBGM_Delayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        //send the PlayBGM Event with parameters from the inspector
        AudioEventManager.playBGMTrack(musicTrackNumber, musicTrackName, volume, fadeType, fadeDuration, loopBGM, eventName);
       
    }

    private void OnTriggerExit(Collider other)
    {
        if (collisionType == CollisionType.Trigger && other.CompareTag(targetTag))
        {
            if(stopOnExit){
                Stop();
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collisionType == CollisionType.Collision && collision.collider.CompareTag(targetTag))
        {
            if(stopOnExit){
                Stop();
            }
        }
    }
    
    public void Stop(){
        if(eventDelay <= 0){
            StopBGM();
        }
        else
        {
            StartCoroutine(StopBGM_Delayed(eventDelay));
        }
    }

    private void StopBGM()
    {
        if (AudioManager.Instance.isFadingMusic)
        {
            // Handle the stop request if the AudioManager is fading
            StartCoroutine(WaitForFadeAndStop());
        }
        else
        {
            // Send the StopBGM Event with parameters from the inspector
            AudioEventManager.stopBGMTrack(fadeDuration);
        }
    }

    private IEnumerator StopBGM_Delayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        StopBGM();
    }

    private IEnumerator WaitForFadeAndStop()
    {
        // Wait until the AudioManager is no longer fading
        while (AudioManager.Instance.isFadingMusic)
        {
            yield return null;
        }
        // Send the StopBGM Event with parameters from the inspector
        AudioEventManager.stopBGMTrack(fadeDuration);
    }
    
    // pause the background music
    public void Pause()
    {
        if(eventDelay <= 0)
        {
            PauseBGM();
        }
        else
        {
            StartCoroutine(PauseBGM_Delayed(eventDelay));
        }
    }
    private void PauseBGM()
    {
        //send the PauseBGM Event with parameters from the inspector
        AudioEventManager.pauseBGMTrack(fadeDuration);
    }
    private IEnumerator PauseBGM_Delayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        //send the PauseBGM Event with parameters from the inspector
        AudioEventManager.pauseBGMTrack(fadeDuration);
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
            $"BGM: {eventName}\n" +
            $"Track Name: {musicTrackName}\n" +
            $"Shape: {shapeInfo}\n" +
            $"Type: {collisionType}\n" +
            $"Tag: {targetTag}\n" +
            $"Scale: {transform.lossyScale}");
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
        UnityEditor.Handles.Label(iconPos, "♫ TRIGGERED ♫");
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