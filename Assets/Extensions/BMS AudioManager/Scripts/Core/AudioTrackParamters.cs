using UnityEngine;

// todo - add  other parameters like fadeType, fadeDuration, fadeTarget, status and audio metrics


[System.Serializable]
public class AudioTrackParamters
{
    public Transform attachedTo;
    public int index;
    public string trackName;
    public float volume;
    public float pitch;
    public float spatialBlend;
    public bool loop;
    public string eventName;

    // Constructor to initialize all parameters
    public AudioTrackParamters(Transform attachedTo, int index, string trackName, float volume, float pitch, float spatialBlend,  bool loop, string eventName)
    {
        this.attachedTo = attachedTo;
        this.index = index;
        this.trackName = trackName;
        this.volume = volume;
        this.pitch = pitch;
        this.spatialBlend = spatialBlend;
        this.loop = loop;
        this.eventName = eventName;
    }
}
