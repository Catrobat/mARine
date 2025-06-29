using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Splines;

public class FreeExpGoalManager : MonoBehaviour
{
    [Header("Marie Buddy reference")]
    [SerializeField] private MarineBuddy marineBuddy;

    [SerializeField] private CrossPlatformTTS ttsManager;
    [SerializeField] private bool autoStart;

    public void SetMarineBuddy(MarineBuddy buddy)
    {
        if (marineBuddy == null)
            marineBuddy = buddy;
    }

    public enum FreeExplorerGoals
    {
        Scan,
        Spawn,
        IntroGreeting,
        ShowPortalInstructions,
        AudioToggleButton,
        AIButton,
        UseDepthSlider,
        ApproachCreature,
        TapInfoButton,
        TapQuizButton,
        TutorialComplete
    }

    [System.Serializable]
    public class FreeExpStep
    {
        public FreeExplorerGoals goalType;
        public string audioInstructionText;
        public GameObject targetToHighlight;
        public Color highlightColor = Color.cyan;
        public UnityEvent onGoalStart;
        public UnityEvent onGoalComplete;
        public List<SplineContainer> marineBuddySplines;
        public float splineDuration = 5f;
    }

    public List<FreeExpStep> tutorialSteps;
    private Queue<FreeExpStep> goalQueue;
    private FreeExpStep currentStep;

    private bool tutorialRunning = false;  // Flag -> Checker (tutorial)
    private Coroutine delayedCompletionRoutine;

    IEnumerator Start()
    {
        yield return  StartCoroutine(WaitForTTSManagerReady());

        if (autoStart)
            StartFreeExploreTutorial();
    }

    public void StartFreeExploreTutorial()
    {
        if (tutorialSteps.Count == 0)
        {
            Debug.LogWarning("FreeExpGoalManager: Missing MarineBuddy or steps.");
            return;
        }

        ResolveSceneReferences();

        // marineBuddy.SetTTSManager(ttsManager);
        goalQueue = new Queue<FreeExpStep>(tutorialSteps);
        tutorialRunning = true;
        ProceedToNextStep();
    }

    public void ResolveSceneReferences()
    {
        foreach (var step in tutorialSteps)
        {
            switch(step.goalType)
            {
                case FreeExplorerGoals.AudioToggleButton:
                    step.targetToHighlight = HighLightTargetRegistry.Instance?.muteButton;
                    break;
                    
                case FreeExplorerGoals.AIButton:
                    step.targetToHighlight = HighLightTargetRegistry.Instance?.aiButton;
                    break;


                case FreeExplorerGoals.UseDepthSlider:
                    step.targetToHighlight = HighLightTargetRegistry.Instance?.depthSlider;
                    break;
            }
        }
    }

    public void ProceedToNextStep() 
    {
        if (goalQueue.Count == 0)
        {
            tutorialRunning = false;
            Debug.Log("Tutorial complete.");
            return;
        }

        currentStep = goalQueue.Dequeue();
        currentStep.onGoalStart?.Invoke();

        if (ShouldUseBuddy(currentStep.goalType) && marineBuddy != null)
        {
            if (currentStep.marineBuddySplines != null && currentStep.marineBuddySplines.Count > 0)
            {
                // Move -> Speak -> Highligh
                /* marineBuddy.FollowSpline(currentStep.marineBuddySplines, () => {
                        marineBuddy.PlayTutorialStep(currentStep.audioInstructionText, () => {
                                if (currentStep.targetToHighlight)
                                marineBuddy.HighlightObject(currentStep.targetToHighlight, currentStep.highlightColor);
                                });
                        },
                        currentStep.splineDuration
                        ); */

                marineBuddy.PerformTutorialStep(
                        currentStep.audioInstructionText,
                        currentStep.targetToHighlight,
                        currentStep.highlightColor,
                        currentStep.marineBuddySplines,
                        currentStep.splineDuration,
                        () => CompleteCurrentGoal()
                        );
            }
            else
            {
                // Speak -> Highlight
                /* marineBuddy.PlayTutorialStep(currentStep.audioInstructionText, () => {
                        if (currentStep.targetToHighlight)
                        marineBuddy.HighlightObject(currentStep.targetToHighlight, currentStep.highlightColor);
                        }); */


                marineBuddy.PerformTutorialStep(
                        currentStep.audioInstructionText,
                        currentStep.targetToHighlight,
                        currentStep.highlightColor,
                        null,
                        0f,
                        () => CompleteCurrentGoal()
                        );
            }
        }
        else if (!string.IsNullOrWhiteSpace(currentStep.audioInstructionText))
        {
            Debug.Log("[FreeExpGoalManager] Using fallback TTS for: " + currentStep.goalType);
            StartCoroutine(WaitAndSpeakTTS(currentStep.audioInstructionText));
        }
        else
        {
            Debug.Log("[FreeExpGoalManager] Skipping TTS for: " + currentStep.goalType);
        }

        // For Scan setp, delay auto-advance for 2s and check for planes
        if (currentStep.goalType == FreeExplorerGoals.Scan && delayedCompletionRoutine == null)
        {
            delayedCompletionRoutine = StartCoroutine(WaitAndMaybeCompleteScanGoal());

        }
    }

    public void CompleteCurrentGoal()
    {
        if (!tutorialRunning) return;

        currentStep.onGoalComplete?.Invoke();
        if (delayedCompletionRoutine != null) StopCoroutine(delayedCompletionRoutine);
        delayedCompletionRoutine = null;
        ProceedToNextStep();
    }

    // Defaults to FreeExplorerGoals.TutorialComplete, if currentStep is null or goalType is unavailable
    public FreeExplorerGoals GetCurrentGoalType()
        => currentStep?.goalType ?? FreeExplorerGoals.TutorialComplete;

    private IEnumerator WaitAndMaybeCompleteScanGoal()
    {
        yield return new WaitForSeconds(2f);
        // WaterPlaneSpawner will complete it externally only if plane is found
    }

    /// <summary>
    /// Determines whether this step should use MarineBuddy features.
    /// Scan and Spawn are setup-only and do not require MarineBuddy.
    /// </summary>
    private bool ShouldUseBuddy(FreeExplorerGoals goal)
    {
        return (goal != FreeExplorerGoals.Scan && goal != FreeExplorerGoals.Spawn);
    }


    private IEnumerator WaitForTTSManagerReady()
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (ttsManager != null && !ttsManager.IsReady && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!ttsManager.IsReady)
        {
            Debug.LogWarning("[FreeExpGoalManager] TTSManager not ready after timeout.");
        }
    }

    private IEnumerator WaitAndSpeakTTS(string message)
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (ttsManager != null && !ttsManager.IsReady && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (ttsManager != null && ttsManager.IsReady)
        {
            ttsManager.Speak(message);
        }
        else
        {
            Debug.LogWarning("[FreeExpGoalManager] TTS was not ready after waiting.");
        }
    }
}
