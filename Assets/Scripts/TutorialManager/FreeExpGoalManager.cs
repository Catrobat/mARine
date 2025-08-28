using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Splines;

public class FreeExpGoalManager : MonoBehaviour
{
    public static FreeExpGoalManager Instance;

    [Header("Marie Buddy reference")]
    [SerializeField] private MarineBuddy marineBuddy;

    [SerializeField] private CrossPlatformTTS ttsManager;
    [SerializeField] private bool autoStart;
    [SerializeField] private IdentifyCreatureMiniGame identifyCreatureMiniGame; 

    public void SetMarineBuddy(MarineBuddy buddy)
    {
        marineBuddy = buddy;
    }

    public enum FreeExplorerGoals
    {
        Scan,
        Spawn,
        IntroGreeting,
        IdentifyMultipleCreatures,
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
        public string dynamicTargetName;
        public Color highlightColor = Color.cyan;
        public UnityEvent onGoalStart;
        public UnityEvent onGoalComplete;
        public List<SplineContainer> marineBuddySplines;
        public List<string> dynamicSplineNames;
        public float splineDuration = 5f;
    }

    [Header("Pre-Portal Setup")]
    [Tooltip("Steps to run before the portal is spawned, like Scan and Spawn")]
    [SerializeField] private List<FreeExpStep> prePortalSteps;

    [System.Serializable]
    public class LayerTutorial
    {
        public string layerName;
        public List<FreeExpStep> steps;
    }

    [Header("Tutorial Configuration")]
    public List<LayerTutorial> layerTutorials;

    // Fast runtime data-structure
    private Dictionary<string, Queue<FreeExpStep>> layerStepQueues; 

    // Dictionary to store the current progress of any started tutorial
    private Dictionary<string, Queue<FreeExpStep>> currentTutorialStates;

    // public List<FreeExpStep> tutorialSteps;
    // private Queue<FreeExpStep> goalQueue;
    private FreeExpStep currentStep;

    private bool tutorialRunning = false;  // Flag -> Checker (tutorial)
    private Coroutine delayedCompletionRoutine;
    
    // private int currentGoalIndex = 0;
    // public string currentLayerName; 

    private Queue<FreeExpStep> activeGoalQueue;
    private string currentActiveLayer;

    private void Awake()
    {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize the dictionary
            layerStepQueues = new Dictionary<string, Queue<FreeExpStep>>();
            currentTutorialStates = new Dictionary<string, Queue<FreeExpStep>>();

            // Populate the dictionary from the Inspector list
            foreach (var tutorial in layerTutorials)
            {
                // New Queue for each layer
                Queue<FreeExpStep> stepQueue = new Queue<FreeExpStep>(tutorial.steps);
                layerStepQueues[tutorial.layerName] = stepQueue;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnNewLayerLoaded()
    {
        Debug.Log("[FreeExpGoalManager] Layer loaded. Can now resume or reset tutorial steps.");
        // Optional: Call ResolveSceneReferences() again or refresh targets
    }


    IEnumerator Start()
    {


        yield return  StartCoroutine(WaitForTTSManagerReady());

        if (autoStart)
        {
            // Check if there are any pre-portal steps to run
            if (prePortalSteps != null && prePortalSteps.Count > 0)
            {
                // Start the pre-poral tutorial directly
                Debug.Log("Starting the Pre-Portal tutorial steps (Scan, Spawn)...");
                activeGoalQueue = new Queue<FreeExpStep>(prePortalSteps);
                currentActiveLayer = "PrePortal";
                tutorialRunning = true;
                //ResolveSceneReferences(); 
                ProceedToNextStep();
            }
        }
    }

    public void StartFreeExploreTutorial(string layerName)
    {

        // First, check if our buddy exists now that the portal is spawned.
        marineBuddy = MarineBuddy.Instance;
        if (marineBuddy == null)
        {
            Debug.LogError("MarineBuddy.Instance not found! Cannot start in-portal tutorial.");
            return;
        }
/*
        // --- SAVE CURRENT STATE ---
        // If a tutorial is already running, save its current queue before switching 
        if (tutorialRunning && activeGoalQueue != null)
        {
            currentTutorialStates[currentActiveLayer] = activeGoalQueue;
        }
*/
        // --- COMPLETE PREVIOUS STEP & SAVE STATE ---
        if (tutorialRunning)
        {
            // Before switching, explicitly complete the last active step (e.g., "Spawn").
            // This ensures its onGoalComplete event (like hiding UI) is called.
            if (currentStep != null)
            {
                currentStep.onGoalComplete?.Invoke();
            }

            // Now save the remaining queue for the old tutorial.
            if (activeGoalQueue != null)
            {
                currentTutorialStates[currentActiveLayer] = activeGoalQueue;
            }
        }

        // --- LOAD NEW STATE ---
        // Load the tutorial for the new layer     
        currentActiveLayer = layerName;

        // Check if we have a saved, in-progress queue for this layer
        if (currentTutorialStates.ContainsKey(layerName))
        {
            // If yes, resume from where we left off
            activeGoalQueue = currentTutorialStates[layerName];
            Debug.Log($"Resuming tutorial for layer '{layerName}'");
        }
        else if (layerStepQueues.ContainsKey(layerName))  // Otherwise, start the layer's tutorial from the beginning.
        {
            // Clone the initial queue, so that the original data is not modified.
            activeGoalQueue = new Queue<FreeExpStep>(layerStepQueues[layerName]);
            Debug.Log($"Starting new tutorial for layer '{layerName}'");
        }
        else
        {
            Debug.Log($"FreeExpGoalManager: No tutorial configured for layer '{layerName}'");
            tutorialRunning = false;
            return;
        }

        if (activeGoalQueue.Count == 0)
        {
            Debug.Log($"Tutorial for '{layerName}' is already complete.");
            tutorialRunning = false;
            return;
        }

        ResolveSceneReferences();
        tutorialRunning = true;
        ProceedToNextStep();

        /* 

        if(!layerStepQueues.ContainsKey(layerName))
        {
            Debug.LogWarning($"FreeExpGoalManager: No tutorial configured for layer '{layerName}'.");
            return;
        }

        // Get the specific queue for this layer from the dictionary
        activeGoalQueue = layerStepQueues[layerName];
        currentLayerName = layerName;  // Keep track of the current layer  

        if (activeGoalQueue.Count == 0)
        {
            Debug.Log($"Tutorial for '{layerName}' is already complete or has no steps.");
            // Optionally, handle what happens when a completed tutorial is restarted.
            return;
        } */

        // You might still want to resolve references that are specific to the new layer
        // ResolveSceneReferences(); // We can look at improving this method next.

        /* if (tutorialSteps.Count == 0)
        {
            Debug.LogWarning("FreeExpGoalManager: Missing MarineBuddy or steps.");
            return;
        }
        ResolveSceneReferences();
*/

        /* if (!resumeFromLast)
            currentGoalIndex = 0;

        // marineBuddy.SetTTSManager(ttsManager);
        goalQueue = new Queue<FreeExpStep>(tutorialSteps); */
        
    }

    public void ResolveSceneReferences()
    {
        if (activeGoalQueue == null) return;

        // Loop through the steps for the CURRENTLY active tutorial.
        foreach (var step in activeGoalQueue)
        {
            // --- 1. Resolve Dynamic, In-Layer Objects ---
            // If a dynamic name is provided, find the object in the scene.
            if (!string.IsNullOrEmpty(step.dynamicTargetName))
            {
                GameObject foundTarget = GameObject.Find(step.dynamicTargetName);
                if (foundTarget != null)
                {
                    step.targetToHighlight = foundTarget;
                }
                else
                {
                    Debug.LogWarning($"Could not find dynamic target named '{step.dynamicTargetName}' in the current scene.");
                }
            }

            if (step.dynamicSplineNames != null && step.dynamicSplineNames.Count > 0)
            {
                step.marineBuddySplines.Clear();

                foreach (string splineName in step.dynamicSplineNames)
                {
                    GameObject foundSplineObject = GameObject.Find(splineName);
                    if (foundSplineObject != null)
                    {
                        SplineContainer spline = foundSplineObject.GetComponent<SplineContainer>();
                        if (spline != null)
                        {
                            step.marineBuddySplines.Add(spline);
                        }
                        else
                        {
                            Debug.LogWarning($"Found object '{splineName}' but it's missing a SplineContainer component.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Could not find a spline object named '{splineName}");
                    }
                }
            }

            // --- 2. Resolve Persistent UI Objects ---
            // This part handles the UI buttons from your registry.
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

            Debug.Log($"SETUP CHECK for step '{step.goalType}': Found {step.marineBuddySplines.Count} splines.");
        }
    }

    public void ProceedToNextStep() 
    {
        if (activeGoalQueue == null || activeGoalQueue.Count == 0)
        {
            tutorialRunning = false;
            Debug.Log($"Tutorial for layer '{currentActiveLayer}' is complete.");
            return;
        }

        // Deque from the active queue
        currentStep = activeGoalQueue.Dequeue();
        // currentGoalIndex++;  // rethink
        currentStep.onGoalStart?.Invoke();

        if (currentStep.goalType == FreeExplorerGoals.IdentifyMultipleCreatures)
        {
            GameObject currentLayer = LayerLoadManager.Instance.GetCurrentLayerInstance();
            if (currentLayer != null)
            {
                IdentifyCreatureMiniGame minigame = currentLayer.GetComponentInChildren<IdentifyCreatureMiniGame>();
                if (minigame != null) 
                {
                    minigame.StartMiniGame();
                    return;
                }
            }

            Debug.Log("[FreeExpGoalManager] Starting IdentifyCreatureMiniGame...");
            // identifyCreatureMiniGame.StartMiniGame();

            return;
        }

        if (ShouldUseBuddy(currentStep.goalType) && marineBuddy != null)
        {
            if (currentStep.marineBuddySplines != null && currentStep.marineBuddySplines.Count > 0)
            {
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
