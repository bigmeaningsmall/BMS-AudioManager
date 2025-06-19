using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
/// <summary>
/// this script is a test script to play background music and sound effects
/// its a good example of how to use the AudioEventManager class by passing parameters from the inspector
/// the script also shows how to use the AudioEventSender class to play events - you can use this to play events from any script
/// events like PlaySFX and PlayBGM are static methods and can be called from anywhere
/// the receiver component take the parameter to play the audio
/// BMS- AudioManager loads the audio clips from the Resources folder - you can call any audio using its name as a string
/// calling strings is a limitation but works well for most cases in smaller projects with a small collection of audio clips
/// </summary>
public class AudioTester : MonoBehaviour
{
    [Header("Press 'M' to play background music")]
    [Space(5)]
    [Header("Press 'Space' to play SFX")]
    [Space(5)]
    [Header("Press 'I' to play call Play on the AudioEventSender_SFX")] // this method means you can avoid using events directly and use the AudioEventSender_SFX interface 
    [Space(5)]
    [Header("Background Music Event Parameters (BGM)")]
    [Space(10)]
    public int musicTrackNumber = -1; 
    public string musicTrackName = "name";
    [Range(0,1f)]
    public float bgmVolume = 0.6f;
    public FadeType fadeType = FadeType.Crossfade;
    [Range(0,5f)]
    public float fadeDuration = 1.5f;
    public bool loopBGM = true;
    
    [Header("Press 'Space' to play sound effect")]
    [Space(20)]
    [Header("Sound FX Event Parameters (SFX)")]
    [Space(5)]
    // parameters to pass with the SFX event
    public string sfxName = "name";
    [Range(0,1f)]
    public float sfxVolume = 1.0f;
    [Range(0,2f)]
    public float pitch = 1.0f;
    public bool randomisePitch = true;
    [Range(0,1f)]
    public float pitchRange = 0.2f;
    [Range(0,1f)]
    public float spatialBlend = 0.0f;
    

    // Update is called once per frame
    void Update()
    {
        //playe background music  when a number key is pressed
        if (Input.GetKeyDown(KeyCode.M))
        {
            //example with explicit parameters passed
            //AudioEventManager.PlayBGM(0, "Music Name Here", 1.0f, FadeType.Crossfade, 2f,true);
            
            //example with parameters from the inspector
            AudioEventManager.playBGMTrack(musicTrackNumber, musicTrackName, bgmVolume, fadeType, fadeDuration, loopBGM, "null");
        }
        
        //play sound effect when the space bar is pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //example with explicit parameters passed
            AudioEventManager.PlaySFX(this.transform, "SFX Name Here", 1.0f, 1.0f, true, 0.1f, 0f, "null");
            
            //example with parameters from the inspector
            AudioEventManager.PlaySFX(this.transform, sfxName, sfxVolume, pitch, randomisePitch, pitchRange, spatialBlend, "null");
        }
        
        
        if(Input.GetKeyDown(KeyCode.I))
        {
            //to play the event using the sender component - get the component (AudioEventSender_SFX) attached to the gameobject and play the event
            IAudioEventSender sfxSender = GetComponent<AudioEventSenderSFX>();
            if (sfxSender != null)
            {
                sfxSender.Play();
            }
            
            //to instanciate a new sender and play the event - updates are needed to set the parameters
            // IAudioEventSender sfxSender = new AudioEventSender_SFX();
            // sfxSender.Play();
        }
    }
}
