using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using cakeslice;

public class MarineBuddy : MonoBehaviour
{
    [Header("TTS Settings")]
    private CrossPlatformTTS ttsManager;

    public void SetTTSManager(CrossPlatformTTS tts)
    {
        ttsManager = tts;
    }

    [Header("Highlight Settings")]
    public float highlightDuration = 4f;
    public Color defaultGlowColor = Color.cyan;

    private Coroutine currentSpeechCoroutine;

    /// <summary>
    /// Play a specific tutorial step using native TTS
    /// </summary>
    public void PlayTutorialStep(string instructionText, UnityAction onFinished = null)
    {
        if (string.IsNullOrWhiteSpace(instructionText))
        {
            Debug.LogWarning("MarineBuddy: Instruction text is empty.");
            onFinished?.Invoke();
            return;
        }

        if (ttsManager == null)
        {
            Debug.LogWarning("MarineBiology: No CrossPlatformTTS assigned.");
            onFinished?.Invoke();
            return;
        }

        ttsManager.Speak(instructionText);
        onFinished?.Invoke();
    }

    /// <summary>
    /// Highlights a target with an outline effect.
    /// </summary>
    public void HighlightObject(GameObject target, Color? glowColor = null, float? duration = null)
    {
        if (target == null)
        {
            Debug.LogWarning("MarineBuddy: No target to highlight.");
            return;
        }

        var outline = target.GetComponent<Outline>();
        if (outline == null)
        {
            outline = target.AddComponent<Outline>();
        }

        outline.color = 1;
        outline.eraseRenderer = false;


        float durationToUse = duration ?? highlightDuration;
        StartCoroutine(RemoveHighlightAfterDelay(outline, durationToUse));
    }

    private IEnumerator RemoveHighlightAfterDelay(Outline outline, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (outline != null)
            Destroy(outline);
    }
}
