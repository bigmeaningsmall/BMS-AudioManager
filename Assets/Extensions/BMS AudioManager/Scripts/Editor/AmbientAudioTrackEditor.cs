#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AmbientAudioTrack))]
public class AmbientAudioTrackEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AmbientAudioTrack track = (AmbientAudioTrack)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("DEBUG INFO", EditorStyles.boldLabel);

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

        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    private void OnSceneGUI()
    {
        AmbientAudioTrack track = (AmbientAudioTrack)target;
    
        // Use HandleUtility.GetHandleSize for proper camera-relative scaling
        float handleSize = HandleUtility.GetHandleSize(track.transform.position);
        float spacing = handleSize * 0.5f; // Adjust this multiplier as needed
    
        // Scene view labels - start higher and use consistent spacing
        Vector3 labelPos = track.transform.position + Vector3.up * (handleSize * 2f);
    
        // Fixed header spacing
        Handles.color = Color.white;
        Handles.Label(labelPos, $"Ambient Debug:\nState: {track.CurrentState}");
        labelPos += Vector3.down * (spacing * 0.5f); // Extra space for 2-line header
    
        // Dynamic source labels with consistent spacing
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
}
#endif