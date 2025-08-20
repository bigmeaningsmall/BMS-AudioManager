using UnityEngine;

// todo - add  other parameters like fadeType, fadeDuration, fadeTarget, status and audio metrics


[System.Serializable]
public class AudioTrackParamters
{
    public AudioTrackState trackState;
    public Transform attachedTo;
    public int index;
    public string trackName;
    public float volume;
    public float pitch;
    public float spatialBlend;
    public bool loop;
    public float clipProgress;
    public float clipLength;
    public float clipPercent;
    public string eventName;

    // Constructor to initialize the AudioTrackParamters
    public AudioTrackParamters(AudioTrackState trackState, Transform attachedTo, int index, string trackName, float volume, float pitch, float spatialBlend, bool loop, float clipProgress, float clipLength, float clipPercent, string eventName)
    {
        this.trackState = trackState;
        this.attachedTo = attachedTo;
        this.index = index;
        this.trackName = trackName;
        this.volume = volume;
        this.pitch = pitch;
        this.spatialBlend = spatialBlend;
        this.loop = loop;
        this.clipProgress = clipProgress;
        this.clipLength = clipLength;
        this.clipPercent = clipPercent;
        this.eventName = eventName;
    }

}
