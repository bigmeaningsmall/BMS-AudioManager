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
    [SerializeField] private AudioType audioType = AudioType.Null;
    
    // Getter setter for audioType
    public AudioType AudioType
    {
        get { return audioType; }
        set { audioType = value; }
    }
    
    // Optional: Helper method to check if this is a specific type
    public bool IsType(AudioType type) => audioType == type;
    
    // Optional: For debugging in inspector
    public override string ToString() => $"AudioSourceType: {audioType}";
}