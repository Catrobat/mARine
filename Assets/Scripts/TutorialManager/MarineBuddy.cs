using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using cakeslice;
using UnityEngine.UI;
using UnityEngine.Splines;

public class MarineBuddy : MonoBehaviour
{
    [Header("TTS Settings")]
    private CrossPlatformTTS ttsManager;

    [Header("Highlight Settings")]
    public float highlightDuration = 4f;
    public Color defaultGlowColor = Color.cyan;

    private MarineBuddyMovementController movementController;

    private void Awake()
    {
        movementController = GetComponent<MarineBuddyMovementController>();
    }

    public void SetTTSManager(CrossPlatformTTS tts)
    {
        ttsManager = tts;
    }

    public void PerformTutorialStep(
        string instructionText,
        GameObject targetToHighlight,
        Color highlightColor,
        List<SplineContainer> splines = null,
        float splineDuration = 5f,
        UnityAction onStepComplete = null)
    {
        StartCoroutine(TutorialSequenceRoutine(instructionText, targetToHighlight, highlightColor, splines, splineDuration, onStepComplete));
    }

    private IEnumerator TutorialSequenceRoutine(
        string instructionText,
        GameObject target,
        Color color,
        List<SplineContainer> splines,
        float splineDuration,
        UnityAction onStepComplete)
    {
        if (splines != null && splines.Count > 0)
            yield return StartCoroutine(FollowSplineSequence(splines, null, splineDuration));

        bool ttsDone = false;
        bool highlightDone = false;

        float estimatedTTSDuration = EstimateSpeechDuration(instructionText);

        if (!string.IsNullOrWhiteSpace(instructionText))
            StartCoroutine(PlayTTSRoutineParallel(instructionText, () => ttsDone = true));
        else
            ttsDone = true;

        if (target != null)
        {
            if (target.GetComponent<CanvasRenderer>() != null)
                StartCoroutine(HighLightUIElementsParallel(target, color, estimatedTTSDuration, () => highlightDone = true));
            else
                StartCoroutine(HighlightRoutineParallel(target, color, estimatedTTSDuration, () => highlightDone = true));
        }
        else
        {
            highlightDone = true;
        }

        yield return new WaitUntil(() => ttsDone && highlightDone);

        onStepComplete?.Invoke();
    }

    private IEnumerator PlayTTSRoutineParallel(string message, UnityAction onDone)
    {
        float estimatedDuration = EstimateSpeechDuration(message);

#if UNITY_EDITOR
        Debug.Log($"[MarineBuddy] (Editor) Speaking: {message}");
        yield return new WaitForSeconds(estimatedDuration);
#else
        if (ttsManager != null)
        {
            ttsManager.Speak(message);
            yield return new WaitForSeconds(estimatedDuration);
        }
        else
        {
            Debug.LogWarning("MarineBuddy: TTSManager is not assigned.");
        }
#endif
        onDone?.Invoke();
    }

    private IEnumerator HighlightRoutineParallel(GameObject obj, Color color, float duration, UnityAction onDone)
    {
        HighlightObject(obj, color, duration);
        yield return new WaitForSeconds(duration);
        onDone?.Invoke();
    }

    public IEnumerator HighLightUIElementsParallel(GameObject uiElement, Color? glowColor, float? duration, UnityAction onDone)
    {
        yield return StartCoroutine(HighLightUIElements(uiElement, glowColor, duration));
        onDone?.Invoke();
    }

    public void FollowSpline(List<SplineContainer> splines, UnityAction onDone = null, float durationPerSpline = 5f)
    {
        if (movementController != null && splines != null && splines.Count > 0)
            StartCoroutine(FollowSplineSequence(splines, onDone, durationPerSpline));
    }

    private IEnumerator FollowSplineSequence(List<SplineContainer> splines, UnityAction onDone = null, float durationPerSpline = 5f)
    {
        foreach (var spline in splines)
        {
            yield return StartCoroutine(movementController.FollowSpline(spline, null, durationPerSpline));
        }
        onDone?.Invoke();
    }

    private float EstimateSpeechDuration(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return 2f;

        int wordCount = message.Split(' ').Length;
        return Mathf.Clamp(wordCount * 0.4f, 2f, 10f); // Adjust as needed
    }

    public void HighlightObject(GameObject target, Color? glowColor = null, float? duration = null)
    {
        if (target == null)
        {
            Debug.LogWarning("MarineBuddy: No target to highlight.");
            return;
        }

        Renderer targetRenderer = target.GetComponent<Renderer>() ?? target.GetComponentInChildren<Renderer>();
        if (targetRenderer == null)
        {
            Debug.LogWarning("MarineBuddy: No renderer found on target or children.");
            return;
        }

        target = targetRenderer.gameObject;

        var outline = target.GetComponent<cakeslice.Outline>() ?? target.AddComponent<cakeslice.Outline>();
        outline.color = 1; // cakeslice uses int index to pick outline color
        outline.eraseRenderer = false;

        float durationToUse = duration ?? highlightDuration;
        StartCoroutine(RemoveHighlightAfterDelay(outline, durationToUse));
    }

    private IEnumerator RemoveHighlightAfterDelay(cakeslice.Outline outline, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (outline != null)
            Destroy(outline);
    }

    public IEnumerator HighLightUIElements(GameObject uiElement, Color? glowColor = null, float? duration = null)
    {
        if (uiElement == null)
        {
            Debug.LogWarning("MarineBuddy: No UI element provided.");
            yield break;
        }

        Graphic graphic = uiElement.GetComponent<Graphic>() ?? uiElement.GetComponentInChildren<Graphic>();
        if (graphic == null)
        {
            Debug.LogWarning("MarineBuddy: No Graphic component found for UI highlighting.");
            yield break;
        }

        var outline = graphic.gameObject.GetComponent<UnityEngine.UI.Outline>() ?? graphic.gameObject.AddComponent<UnityEngine.UI.Outline>();

        outline.effectColor = glowColor ?? defaultGlowColor;
        outline.effectDistance = new Vector2(12f, -12f);
        outline.useGraphicAlpha = false;

        Debug.Log($"[MarineBuddy] UI Highlight applied to: {graphic.gameObject.name}");

        yield return new WaitForSeconds(duration ?? highlightDuration);

        if (outline != null)
            Destroy(outline);
    }
}
