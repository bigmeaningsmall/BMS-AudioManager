#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AmbientAudioTrack))]
public class AmbientAudioTrackEditor : Editor
{
    private Texture2D waveformTexture;
    private bool showWaveform = true;
    private const int WAVEFORM_WIDTH = 400;
    private const int WAVEFORM_HEIGHT = 100;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AmbientAudioTrack track = (AmbientAudioTrack)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("DEBUG INFO", EditorStyles.boldLabel);

        // Static source info (always visible)
        DrawSourceInfo(track);

        if (!Application.isPlaying) 
        {
            EditorGUILayout.HelpBox("Live visualization available during play mode", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("LIVE AUDIO VISUALIZATION", EditorStyles.boldLabel);

        // Dynamic progress and volume displays
        DrawLiveAudioInfo(track);

        // Waveform display
        DrawWaveformSection(track);

        // Keep updating during play
        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    private void DrawSourceInfo(AmbientAudioTrack track)
    {
        // Main Source
        GUI.color = track.MainSource != null ? Color.green : Color.red;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Main Source:", EditorStyles.boldLabel);
        if (track.MainSource != null)
        {
            EditorGUILayout.LabelField($"Vol: {track.MainSource.volume:F2} | Pitch: {track.MainSource.pitch:F2} | Playing: {track.MainSource.isPlaying}");
        }
        else
        {
            EditorGUILayout.LabelField("NULL");
        }
        EditorGUILayout.EndHorizontal();

        // Cue Source
        GUI.color = track.CueSource != null ? Color.yellow : Color.red;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Cue Source:", EditorStyles.boldLabel);
        if (track.CueSource != null)
        {
            EditorGUILayout.LabelField($"Vol: {track.CueSource.volume:F2} | Pitch: {track.CueSource.pitch:F2} | Playing: {track.CueSource.isPlaying}");
        }
        else
        {
            EditorGUILayout.LabelField("NULL");
        }
        EditorGUILayout.EndHorizontal();

        // Outgoing Source
        GUI.color = track.OutgoingSource != null ? Color.cyan : Color.red;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Outgoing Source:", EditorStyles.boldLabel);
        if (track.OutgoingSource != null)
        {
            EditorGUILayout.LabelField($"Vol: {track.OutgoingSource.volume:F2} | Pitch: {track.OutgoingSource.pitch:F2} | Playing: {track.OutgoingSource.isPlaying}");
        }
        else
        {
            EditorGUILayout.LabelField("NULL");
        }
        EditorGUILayout.EndHorizontal();

        GUI.color = Color.white;
        EditorGUILayout.LabelField($"Current State: {track.CurrentState}");
    }

    private void DrawLiveAudioInfo(AmbientAudioTrack track)
    {
        bool hasAnySources = false;

        // Volume and Pitch sliders for active sources - with null checks
        if (track.MainSource != null && track.MainSource)
        {
            hasAnySources = true;
            GUI.color = Color.green;
            EditorGUILayout.LabelField("Main Source", EditorStyles.boldLabel);
            GUI.color = Color.white;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Volume:", GUILayout.Width(60));
            EditorGUILayout.Slider(track.MainSource.volume, 0f, 1f);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pitch:", GUILayout.Width(60));
            EditorGUILayout.Slider(track.MainSource.pitch, 0f, 3f);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
        }

        if (track.CueSource != null && track.CueSource)
        {
            hasAnySources = true;
            GUI.color = Color.yellow;
            EditorGUILayout.LabelField("Cue Source", EditorStyles.boldLabel);
            GUI.color = Color.white;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Volume:", GUILayout.Width(60));
            EditorGUILayout.Slider(track.CueSource.volume, 0f, 1f);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pitch:", GUILayout.Width(60));
            EditorGUILayout.Slider(track.CueSource.pitch, 0f, 3f);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
        }

        if (track.OutgoingSource != null && track.OutgoingSource)
        {
            hasAnySources = true;
            GUI.color = Color.cyan;
            EditorGUILayout.LabelField("Outgoing Source", EditorStyles.boldLabel);
            GUI.color = Color.white;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Volume:", GUILayout.Width(60));
            EditorGUILayout.Slider(track.OutgoingSource.volume, 0f, 1f);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pitch:", GUILayout.Width(60));
            EditorGUILayout.Slider(track.OutgoingSource.pitch, 0f, 3f);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
        }

        if (!hasAnySources)
        {
            EditorGUILayout.HelpBox("No audio sources active", MessageType.Info);
        }

        // Progress bar for main audio
        DrawProgressBar(track);
    }

    private void DrawProgressBar(AmbientAudioTrack track)
    {
        // Find the first active source with proper null checks
        AudioSource activeSource = null;
        
        if (track.MainSource != null && track.MainSource)
            activeSource = track.MainSource;
        else if (track.CueSource != null && track.CueSource)
            activeSource = track.CueSource;
            
        if (activeSource?.clip == null) 
        {
            EditorGUILayout.LabelField("No active audio clip", EditorStyles.miniLabel);
            return;
        }

        float progress = activeSource.time / activeSource.clip.length;
        string timeText = $"{activeSource.time:F1}s / {activeSource.clip.length:F1}s";
        
        EditorGUILayout.LabelField($"Playback Progress: {timeText}");
        
        // Custom progress bar with time position
        Rect progressRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
        EditorGUI.ProgressBar(progressRect, progress, $"{(progress * 100):F0}%");
        
        // Show clip name
        EditorGUILayout.LabelField($"Playing: {activeSource.clip.name}", EditorStyles.miniLabel);
    }

    private void DrawWaveformSection(AmbientAudioTrack track)
    {
        EditorGUILayout.Space();
        
        // Waveform toggle
        showWaveform = EditorGUILayout.Foldout(showWaveform, "Audio Visualization", true);
        if (!showWaveform) return;

        AudioSource activeSource = track.MainSource ?? track.CueSource;
        if (activeSource?.clip == null)
        {
            EditorGUILayout.HelpBox("No audio clip loaded", MessageType.Info);
            return;
        }

        // Check if we can access the audio data
        if (activeSource.clip.loadType != AudioClipLoadType.DecompressOnLoad)
        {
            // Show streaming-friendly visualization instead
            DrawStreamingVisualization(activeSource);
            return;
        }

        // Generate or update waveform texture for decompressed audio
        if (waveformTexture == null || waveformTexture.width != WAVEFORM_WIDTH)
        {
            GenerateWaveformTexture(activeSource.clip);
        }

        if (waveformTexture != null)
        {
            // Draw waveform
            Rect waveformRect = GUILayoutUtility.GetRect(WAVEFORM_WIDTH, WAVEFORM_HEIGHT);
            EditorGUI.DrawPreviewTexture(waveformRect, waveformTexture);
            
            // Draw playback position line
            float progress = activeSource.time / activeSource.clip.length;
            float lineX = waveformRect.x + (progress * waveformRect.width);
            
            Handles.BeginGUI();
            Handles.color = Color.red;
            Handles.DrawLine(new Vector3(lineX, waveformRect.y), new Vector3(lineX, waveformRect.y + waveformRect.height));
            Handles.EndGUI();
            
            // Show waveform info
            EditorGUILayout.LabelField($"Samples: {activeSource.clip.samples} | Frequency: {activeSource.clip.frequency}Hz", EditorStyles.miniLabel);
        }
    }

    private void DrawStreamingVisualization(AudioSource source)
    {
        EditorGUILayout.LabelField("Streaming Audio Visualization", EditorStyles.boldLabel);
        
        // Basic info
        EditorGUILayout.LabelField($"Clip: {source.clip.name}");
        EditorGUILayout.LabelField($"Length: {source.clip.length:F1}s | Channels: {source.clip.channels} | Frequency: {source.clip.frequency}Hz");
        EditorGUILayout.LabelField($"Load Type: {source.clip.loadType} (Optimized for streaming)");
        
        // Time-based progress bar
        float progress = source.time / source.clip.length;
        Rect progressRect = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
        
        // Draw custom progress visualization
        EditorGUI.DrawRect(progressRect, new Color(0.2f, 0.2f, 0.2f));
        
        // Progress fill
        Rect fillRect = new Rect(progressRect.x, progressRect.y, progressRect.width * progress, progressRect.height);
        EditorGUI.DrawRect(fillRect, new Color(0.3f, 0.7f, 0.3f));
        
        // Time markers
        GUI.color = Color.white;
        GUI.Label(new Rect(progressRect.x + 5, progressRect.y + 5, 100, 20), $"{source.time:F1}s");
        GUI.Label(new Rect(progressRect.x + progressRect.width - 50, progressRect.y + 5, 50, 20), $"{source.clip.length:F1}s");
        
        // Playback status
        string status = source.isPlaying ? "▶ Playing" : "⏸ Paused";
        EditorGUILayout.LabelField($"Status: {status} | Volume: {source.volume:F2} | Pitch: {source.pitch:F2}");
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Streaming audio clips don't allow waveform generation for performance reasons.\n" +
            "This is the recommended setting for ambient audio to avoid memory issues.", 
            MessageType.Info);
    }

    private void GenerateWaveformTexture(AudioClip clip)
    {
        if (clip == null) return;

        waveformTexture = new Texture2D(WAVEFORM_WIDTH, WAVEFORM_HEIGHT, TextureFormat.RGBA32, false);
        
        // Try to get audio data with error handling
        float[] samples = null;
        try 
        {
            samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Cannot generate waveform for {clip.name}: {e.Message}");
            
            // Create a "no data" texture
            Color[] pixels = new Color[WAVEFORM_WIDTH * WAVEFORM_HEIGHT];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0.2f, 0.1f, 0.1f, 1f); // Dark red background
            }
            
            // Draw "NO DATA" message in the middle
            int centerX = WAVEFORM_WIDTH / 2;
            int centerY = WAVEFORM_HEIGHT / 2;
            for (int x = centerX - 30; x < centerX + 30; x++)
            {
                for (int y = centerY - 5; y < centerY + 5; y++)
                {
                    if (x >= 0 && x < WAVEFORM_WIDTH && y >= 0 && y < WAVEFORM_HEIGHT)
                    {
                        pixels[y * WAVEFORM_WIDTH + x] = Color.red;
                    }
                }
            }
            
            waveformTexture.SetPixels(pixels);
            waveformTexture.Apply();
            return;
        }

        if (samples == null || samples.Length == 0) return;
        
        // Clear texture
        Color[] wavePixels = new Color[WAVEFORM_WIDTH * WAVEFORM_HEIGHT];
        for (int i = 0; i < wavePixels.Length; i++)
        {
            wavePixels[i] = new Color(0.1f, 0.1f, 0.1f, 1f); // Dark background
        }
        
        // Draw waveform
        int samplesPerPixel = samples.Length / WAVEFORM_WIDTH;
        
        for (int x = 0; x < WAVEFORM_WIDTH; x++)
        {
            float maxSample = 0f;
            
            // Find max sample in this pixel's range
            for (int i = 0; i < samplesPerPixel && (x * samplesPerPixel + i) < samples.Length; i++)
            {
                float sample = Mathf.Abs(samples[x * samplesPerPixel + i]);
                if (sample > maxSample) maxSample = sample;
            }
            
            // Draw vertical line for this sample
            int waveHeight = Mathf.RoundToInt(maxSample * WAVEFORM_HEIGHT * 0.8f);
            int centerY = WAVEFORM_HEIGHT / 2;
            
            for (int y = centerY - waveHeight/2; y < centerY + waveHeight/2 && y >= 0 && y < WAVEFORM_HEIGHT; y++)
            {
                wavePixels[y * WAVEFORM_WIDTH + x] = Color.green;
            }
        }
        
        waveformTexture.SetPixels(wavePixels);
        waveformTexture.Apply();
    }

    private void OnSceneGUI()
    {
        AmbientAudioTrack track = (AmbientAudioTrack)target;
        
        // Use HandleUtility.GetHandleSize for proper camera-relative scaling
        float handleSize = HandleUtility.GetHandleSize(track.transform.position);
        float spacing = handleSize * 0.5f;
        
        // Scene view labels
        Vector3 labelPos = track.transform.position + Vector3.up * (handleSize * 2f);
        
        Handles.color = Color.white;
        Handles.Label(labelPos, $"Ambient Debug:\nState: {track.CurrentState}");
        labelPos += Vector3.down * (spacing * 1.5f);
        
        // Dynamic source labels
        if (track.MainSource != null)
        {
            Handles.color = Color.green;
            Handles.Label(labelPos, $"Main: Vol {track.MainSource.volume:F2} | Pitch {track.MainSource.pitch:F2}");
            labelPos += Vector3.down * spacing;
        }
        
        if (track.CueSource != null)
        {
            Handles.color = Color.yellow;
            Handles.Label(labelPos, $"Cue: Vol {track.CueSource.volume:F2} | Pitch {track.CueSource.pitch:F2}");
            labelPos += Vector3.down * spacing;
        }
        
        if (track.OutgoingSource != null)
        {
            Handles.color = Color.cyan;
            Handles.Label(labelPos, $"Out: Vol {track.OutgoingSource.volume:F2} | Pitch {track.OutgoingSource.pitch:F2}");
        }
    }

    private void OnDisable()
    {
        // Clean up texture
        if (waveformTexture != null)
        {
            DestroyImmediate(waveformTexture);
        }
    }
}
#endif