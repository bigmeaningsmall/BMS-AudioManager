using System.Collections;
using UnityEngine;

/// <summary>
/// Simple, self-contained audio fade controller
/// Handles fading an AudioSource and destroys itself when done
/// </summary>
public class AudioFadeController : MonoBehaviour
{
    private AudioSource targetAudioSource;
    private bool isComplete = false;

    /// <summary>
    /// Static method to create and start a fade out operation
    /// </summary>
    /// <param name="audioSource">AudioSource to fade</param>
    /// <param name="duration">Fade duration in seconds</param>
    /// <param name="fadeTarget">What to fade (volume, pitch, or both)</param>
    /// <returns>The controller instance (mostly for debugging)</returns>
    public static AudioFadeController FadeOut(AudioSource audioSource, float duration, FadeTarget fadeTarget)
    {
        if (audioSource == null) return null;

        GameObject controllerObj = new GameObject("AudioFadeController_FadeOut");
        AudioFadeController controller = controllerObj.AddComponent<AudioFadeController>();
        controller.StartFadeOut(audioSource, duration, fadeTarget);
        return controller;
    }

    /// <summary>
    /// Static method to create and start a fade in operation
    /// </summary>
    /// <param name="audioSource">AudioSource to fade</param>
    /// <param name="duration">Fade duration in seconds</param>
    /// <param name="fadeTarget">What to fade (volume, pitch, or both)</param>
    /// <param name="targetVolume">Target volume to fade to</param>
    /// <param name="targetPitch">Target pitch to fade to</param>
    /// <returns>The controller instance</returns>
    public static AudioFadeController FadeIn(AudioSource audioSource, float duration, FadeTarget fadeTarget, float targetVolume = 1f, float targetPitch = 1f)
    {
        if (audioSource == null) return null;

        GameObject controllerObj = new GameObject("AudioFadeController_FadeIn");
        AudioFadeController controller = controllerObj.AddComponent<AudioFadeController>();
        controller.StartFadeIn(audioSource, duration, fadeTarget, targetVolume, targetPitch);
        return controller;
    }

    private void StartFadeOut(AudioSource audioSource, float duration, FadeTarget fadeTarget)
    {
        targetAudioSource = audioSource;
        StartCoroutine(FadeOutCoroutine(duration, fadeTarget));
    }

    private void StartFadeIn(AudioSource audioSource, float duration, FadeTarget fadeTarget, float targetVolume, float targetPitch)
    {
        targetAudioSource = audioSource;
        StartCoroutine(FadeInCoroutine(duration, fadeTarget, targetVolume, targetPitch));
    }

    private IEnumerator FadeOutCoroutine(float duration, FadeTarget fadeTarget)
    {
        if (targetAudioSource == null)
        {
            CompleteFade();
            yield break;
        }

        float startVolume = targetAudioSource.volume;
        float startPitch = targetAudioSource.pitch;

        // Handle instant fade
        if (fadeTarget == FadeTarget.Ignore || duration <= 0f)
        {
            targetAudioSource.volume = 0f;
            targetAudioSource.pitch = 0f;
            CompleteFade();
            yield break;
        }

        // Gradual fade
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            // Safety check - audio source might be destroyed externally
            if (targetAudioSource == null)
            {
                CompleteFade();
                yield break;
            }

            float progress = t / duration;

            switch (fadeTarget)
            {
                case FadeTarget.FadeVolume:
                    targetAudioSource.volume = Mathf.Lerp(startVolume, 0f, progress);
                    break;

                case FadeTarget.FadePitch:
                    targetAudioSource.pitch = Mathf.Lerp(startPitch, 0f, progress);
                    break;

                case FadeTarget.FadeBoth:
                    targetAudioSource.volume = Mathf.Lerp(startVolume, 0f, progress);
                    targetAudioSource.pitch = Mathf.Lerp(startPitch, 0f, progress);
                    break;
            }

            yield return null;
        }

        // Ensure final values
        if (targetAudioSource != null)
        {
            if (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth)
                targetAudioSource.volume = 0f;
            if (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth)
                targetAudioSource.pitch = 0f;
        }

        CompleteFade();
    }

    private IEnumerator FadeInCoroutine(float duration, FadeTarget fadeTarget, float targetVolume, float targetPitch)
    {
        if (targetAudioSource == null)
        {
            CompleteFade();
            yield break;
        }

        float startVolume = targetAudioSource.volume;
        float startPitch = targetAudioSource.pitch;

        // Handle instant fade
        if (fadeTarget == FadeTarget.Ignore || duration <= 0f)
        {
            targetAudioSource.volume = targetVolume;
            targetAudioSource.pitch = targetPitch;
            CompleteFade();
            yield break;
        }

        // Gradual fade
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            // Safety check
            if (targetAudioSource == null)
            {
                CompleteFade();
                yield break;
            }

            float progress = t / duration;

            switch (fadeTarget)
            {
                case FadeTarget.FadeVolume:
                    targetAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, progress);
                    break;

                case FadeTarget.FadePitch:
                    targetAudioSource.pitch = Mathf.Lerp(startPitch, targetPitch, progress);
                    break;

                case FadeTarget.FadeBoth:
                    targetAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, progress);
                    targetAudioSource.pitch = Mathf.Lerp(startPitch, targetPitch, progress);
                    break;
            }

            yield return null;
        }

        // Ensure final values
        if (targetAudioSource != null)
        {
            if (fadeTarget == FadeTarget.FadeVolume || fadeTarget == FadeTarget.FadeBoth)
                targetAudioSource.volume = targetVolume;
            if (fadeTarget == FadeTarget.FadePitch || fadeTarget == FadeTarget.FadeBoth)
                targetAudioSource.pitch = targetPitch;
        }

        CompleteFade();
    }

    private void CompleteFade()
    {
        isComplete = true;

        // Stop and destroy the audio source
        if (targetAudioSource != null)
        {
            targetAudioSource.Stop();
            Destroy(targetAudioSource.gameObject);
        }

        // Destroy this controller
        Destroy(gameObject);
    }

    public bool IsComplete => isComplete;
}