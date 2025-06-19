using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioEventSenderDialogueTrack : MonoBehaviour, IAudioEventSender
{
    [Space(20)]
    ///  USE THIS TO DETERMINE WHICH EVENT TO SEND (Multiple scripts can be attached to the same object) //todo this is confusing currenlty as its not implemented and i cant remember the exact plan!!!
    /// Loop through the AudioEventSender_Dialogue scripts on the object and send the event with the matching eventName
    public string eventName = "Custom Dialogue Event Name"; //for future use

    [Space(20)]
    [Header("Attach The AudioSource to Transform -  Null to Attach to AudioManager")]
    public bool attachToThisTransform;
    public Transform transformToAttachTo;
    [Space(10)]
    [Header("Dialogue Audio Event Parameters")]
    [Space(20)]
    [Tooltip("The track number of the dialogue audio to play - used if no name is given -1 to ignore")]
    public int dialogueTrackNumber = 0; // WILL USE THE TRACK NUMBER IF NO NAME IS GIVEN
    public string dialogueTrackName = "TRACK NAME HERE"; //IF NO NAME IS GIVEN, THE TRACK NUMBER WILL BE USED

    [Space(20)]
    public bool playOnEnabled = true;
    public bool loopDialogue = false;

    [Space(10)]
    [Range(0, 1f)]
    public float volume = 1.0f;
    [Range(0, 2f)] public float pitch = 1.0f;
    [Range(0, 1f)] public float spatialBlend = 0f;
    public FadeType fadeType = FadeType.FadeInOut;
    [Range(0, 10f)]
    public float fadeDuration = 0.5f;

    [Space(10)] 
    [Range(0,5f)]
    public float eventDelay = 0f;

    [Header("Collider Settings")]
    public CollisionType collisionType = CollisionType.Trigger;
    public string targetTag = "Player";
    public bool stopOnExit = true;
    
    [Space(20)]
    [Header("TestMode : 'M' to play dialogue, 'N' to stop, 'B' to pause")]
    public bool testMode = false;

    
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

    private void OnDisable()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.isActiveAndEnabled)
        {
            Stop();
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (collisionType == CollisionType.Trigger && other.CompareTag(targetTag))
        {
            Play();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collisionType == CollisionType.Collision && collision.collider.CompareTag(targetTag))
        {
            Play();
        }
    }

    public void Play()
    {
        if (eventDelay <= 0)
        {
            PlayDialogue();
        }
        else
        {
            StartCoroutine(PlayDialogue_Delayed(eventDelay));
        }
    }

    private void PlayDialogue()
    {
        if(!attachToThisTransform && transformToAttachTo == null){
            Debug.LogWarning("No Transform to attach to - using AudioManager");
            //send the PlayDialogue Event with parameters from the inspector
            AudioEventManager.playDialogueTrackAudio(null,dialogueTrackNumber, dialogueTrackName, volume, pitch, spatialBlend, fadeType, fadeDuration, loopDialogue, eventName);
        }
        
        if (attachToThisTransform){
            //send the PlayDialogue Event with parameters from the inspector
            AudioEventManager.playDialogueTrackAudio(this.transform,dialogueTrackNumber, dialogueTrackName, volume, pitch, spatialBlend, fadeType, fadeDuration, loopDialogue, eventName);
        }
        if(transformToAttachTo != null){
            //send the PlayDialogue Event with parameters from the inspector
            AudioEventManager.playDialogueTrackAudio(transformToAttachTo, dialogueTrackNumber, dialogueTrackName, volume, pitch, spatialBlend, fadeType, fadeDuration, loopDialogue, eventName);
        }

    }

    private IEnumerator PlayDialogue_Delayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if(!attachToThisTransform && transformToAttachTo == null){
            Debug.LogWarning("No Transform to attach to - using AudioManager");
            //send the PlayDialogue Event with parameters from the inspector
            AudioEventManager.playDialogueTrackAudio(null,dialogueTrackNumber, dialogueTrackName, volume, pitch, spatialBlend, fadeType, fadeDuration, loopDialogue, eventName);
        }
        
        if (attachToThisTransform){
            //send the PlayDialogue Event with parameters from the inspector
            AudioEventManager.playDialogueTrackAudio(this.transform,dialogueTrackNumber, dialogueTrackName, volume, pitch, spatialBlend, fadeType, fadeDuration, loopDialogue, eventName);
        }
        if(transformToAttachTo != null){
            //send the PlayDialogue Event with parameters from the inspector
            AudioEventManager.playDialogueTrackAudio(transformToAttachTo, dialogueTrackNumber, dialogueTrackName, volume, pitch, spatialBlend, fadeType, fadeDuration, loopDialogue, eventName);
        }
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
    
    public void Stop()
    {
        if (eventDelay <= 0)
        {
            StopDialogue();
        }
        else
        {
            StartCoroutine(StopDialogue_Delayed(eventDelay));
        }
    }

    private void StopDialogue()
    {
        if (AudioManager.Instance.isFadingDialogueAudio)
        {
            // Handle the stop request if the AudioManager is fading
            StartCoroutine(WaitForFadeAndStop());
        }
        else
        {
            // Send the StopDialogue Event with parameters from the inspector
            AudioEventManager.stopDialogueTrackAudio(fadeDuration);
        }
    }

    private IEnumerator StopDialogue_Delayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        StopDialogue();
    }

    private IEnumerator WaitForFadeAndStop()
    {
        // Wait until the AudioManager is no longer fading
        while (AudioManager.Instance.isFadingDialogueAudio)
        {
            yield return null;
        }
        // Send the StopDialogue Event with parameters from the inspector
        AudioEventManager.stopDialogueTrackAudio(fadeDuration);
    }

    // pause the dialogue audio
    public void Pause()
    {
        if (eventDelay <= 0)
        {
            PauseDialogue();
        }
        else
        {
            StartCoroutine(PauseDialogue_Delayed(eventDelay));
        }
    }

    private void PauseDialogue()
    {
        //send the PauseDialogue Event with parameters from the inspector
        AudioEventManager.pauseDialogueTrackAudio(fadeDuration);
    }

    private IEnumerator PauseDialogue_Delayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        //send the PauseDialogue Event with parameters from the inspector
        AudioEventManager.pauseDialogueTrackAudio(fadeDuration);
    }

    //----------------- EDITOR / TESTING-----------------
    // This section is only used in the editor to test the events 
    void Update()
    {
        if (testMode)
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
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
}