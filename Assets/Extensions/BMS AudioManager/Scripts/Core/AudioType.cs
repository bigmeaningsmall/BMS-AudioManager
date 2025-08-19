using UnityEngine;

// Define an enum for audio types - BGM, Ambient, Dialogue, SFX - may extend this later
public enum AudioType
{
    Null,
    BGM,        // Background Music
    Ambient,    // Ambient Sounds
    Dialogue,   // Dialogue Sounds
    SFX         // Sound Effects
}

public class AudioSourceType : MonoBehaviour
{
    public AudioType audioType = AudioType.Null;
    
    //getter setter for audioType
    public AudioType AudioType{
        get{ return audioType; }
        set{ audioType = value; }
    }
}
