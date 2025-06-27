using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;

public class FreeExpGoalManager : MonoBehaviour
{
    [Header("Marie Buddy reference")]
    private MarineBuddy marineBuddy;

    [SerializeField] private CrossPlatformTTS ttsManager;

    public void SetMarineBuddy(MarineBuddy buddy)
    {
        marineBuddy = buddy;
    }

    public enum FreeExplorerGoals
    {
        Scan,
        Spawn,
        IntroGreeting,
        ShowPortalInstructions,
        ApproachPortal,
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
    }

   public List<FreeExpStep> tutorialSteps;
   private Queue<FreeExpStep> goalQueue;
   private FreeExpStep currentStep;

   private bool tutorialRunning = false;  // Flag -> Checker (tutorial)
   private Coroutine delayedCompletionRoutine;

   IEnumerator Start()
   {
       yield return  StartCoroutine(WaitForTTSManagerReady());

       StartFreeExploreTutorial();
   }

   public void StartFreeExploreTutorial()
   {
       if (tutorialSteps.Count == 0)
       {
           Debug.LogWarning("FreeExpGoalManager: Missing MarineBuddy or steps.");
           return;
       }

       goalQueue = new Queue<FreeExpStep>(tutorialSteps);
       tutorialRunning = true;
       ProceedToNextStep();
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
           marineBuddy.PlayTutorialStep(currentStep.audioInstructionText, () => {
                   if (currentStep.targetToHighlight)
                   marineBuddy.HighlightObject(currentStep.targetToHighlight, currentStep.highlightColor);
                   });
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
