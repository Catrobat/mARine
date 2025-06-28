using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using cakeslice;
using UnityEngine.Splines;

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

    private MarineBuddyMovementController movementController;

    private void Awake()
    {
        movementController = GetComponent<MarineBuddyMovementController>();
    }

    public void PerformTutorialStep(
            string instructionText,
            GameObject targetToHighlight,
            Color HighlightColor,
            List<SplineContainer> splines = null,
            float splineDuration = 5f,
            UnityAction onStepComplete = null)
    {
        StartCoroutine(TutorialSequenceRoutine(instructionText, targetToHighlight, HighlightColor, splines, splineDuration, onStepComplete));
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
        else
             Debug.LogWarning("Spline not found");
        
        if (!string.IsNullOrWhiteSpace(instructionText))
            yield return StartCoroutine(PlayTTSRoutine(instructionText));

        if (target != null)
            yield return StartCoroutine(HighlightRoutine(target, color));

        onStepComplete?.Invoke();
    }

    private IEnumerator PlayTTSRoutine(string message)
    {
#if UNITY_EDITOR
        Debug.Log($"[MarineBuddy] (Editor) Speaking: {message}");
        yield return new WaitForSeconds(2.5f); // Simulated delay
#else
        bool done = false;
        if (ttsManager != null)
        {
            // Temporary wrapper using UnityAction -> you might wire this from AndroidTTS or IOS script internally
            ttsManager.Speak(message);
            yield return new WaitForSeconds(2.5f); // fallback wait time
        }
        else
        {
            Debug.LogWarning("MarineBuddy: TTSManager is not assigned.");
            yield return null;
        }
#endif
    }

    private IEnumerator HighlightRoutine(GameObject obj, Color color)
    {
        HighlightObject(obj, color, highlightDuration);
        yield return new WaitForSeconds(highlightDuration);
    }
    

    public void FollowSpline(List<SplineContainer> splines, UnityAction onDone = null, float durationPerSpline = 5f)
    {
        if (movementController != null && splines != null && splines.Count > 0)
            StartCoroutine(FollowSplineSequence(splines, onDone, durationPerSpline));
    }

    private IEnumerator FollowSplineSequence(List<SplineContainer> splines, UnityAction onDone = null, float durationPerSpline = 5f) 
    {
        foreach(var spline in splines)
        {
            yield return StartCoroutine(movementController.FollowSpline(spline, null, durationPerSpline));
        }
        onDone?.Invoke();
    }
    
    private Coroutine currentSpeechCoroutine;

    /* /// <summary>
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
*/
    /// <summary>
    /// Highlights a target with an outline effect.
    /// </summary>
    /* public void HighlightObject(GameObject target, Color? glowColor = null, float? duration = null)
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
*/

    public void HighlightObject(GameObject target, Color? glowColor = null, float? duration = null)
    {
        if (target == null)
        {
            Debug.LogWarning("MarineBuddy: No target to highlight.");
            return;
        }

        Renderer targetRenderer = target.GetComponent<Renderer>();
        if (targetRenderer == null)
        {
            // Try to find a child with a renderer (SkinnedMeshRenderer or MeshRenderer)
            targetRenderer = target.GetComponentInChildren<Renderer>();
            if (targetRenderer == null)
            {
                Debug.LogWarning("MarineBuddy: No renderer found to highlight on target or children.");
                return;
            }

            target = targetRenderer.gameObject;
        }

        var outline = target.GetComponent<Outline>();
        if (outline == null)
        {
            outline = target.AddComponent<Outline>();
        }

        outline.color = 1; // Assuming 1 = cyan in cakeslice
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
