using UnityEngine;

public class DialogTester : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TriggerDialogue(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TriggerDialogue(2);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TriggerDialogue(3);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            TriggerDialogue(4);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            TriggerDialogue(5);
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            TriggerDialogue(6);
        }
    }

    private void TriggerDialogue(int dialogNumber)
    {
        string dialogName = $"dialog{dialogNumber}";

        /*// Play dialogue using AudioEventManager
        AudioEventManager.playDialogueTrack(
            null, // No transform to attach
            0, // Default track number
            dialogName, // Dialogue track name
            1.0f, // Volume
            1.0f, // Pitch
            0f, // Spatial blend
            FadeType.FadeInOut, // Fade type
            0.5f, // Fade duration
            "TestEvent" // Event name
        );*/

        Debug.Log($"Triggered {dialogName}");
    }
}