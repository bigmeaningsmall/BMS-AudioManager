using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
/// <summary>
/// 
/// </summary>
public class AudioTester : MonoBehaviour
{
    public float fadeDuration = 1f;
    public float volume = 1f;
    public float pitch = 1f;
    public float spatialBlend = 0f;
    public bool loop = false;
    public Transform attachTo;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            AudioEventManager.setAmbientVolume(volume, fadeDuration);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            AudioEventManager.setAmbientPitch(pitch, fadeDuration);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            AudioEventManager.setAmbientSpatialBlend(spatialBlend);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            AudioEventManager.setAmbientLoop(loop);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            AudioEventManager.moveAmbientSource(attachTo);
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {

        }
    }
}
