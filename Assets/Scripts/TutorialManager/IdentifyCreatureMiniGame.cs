using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;


public class IdentifyCreatureMiniGame: MonoBehaviour
{
    // These will be found at runtime
    private MarineBuddy marineBuddy;
    private MiniQuizController miniQuizController;

    [Header("Game Data")]
    public List<IdentifyCreatureStep> steps;

    private int currentStepIndex = 0;
    private bool isRunning = false;

    public void StartMiniGame()
    {
        // Find the singletons when the game starts
        marineBuddy = MarineBuddy.Instance;
        miniQuizController = MiniQuizController.Instance;

        if (steps == null || steps.Count == 0 || marineBuddy == null || miniQuizController == null)
        {
            Debug.LogError("[IdentifyCreatureStep] Misssing references or data.");
            return;
        }

        isRunning = true;
        currentStepIndex = 0;
        RunStep();
    }

    private void RunStep()
    {
        if (!isRunning) return;

        if (currentStepIndex >= steps.Count)
        {
            Debug.Log("[IdentifyCreatureMiniGame] Mini-game complete.");
            isRunning = false;
            // Tell the main tutorial manager that we are finished
            FreeExpGoalManager.Instance.CompleteCurrentGoal();
            return;
        }

        IdentifyCreatureStep step = steps[currentStepIndex];

        marineBuddy.PerformTutorialStep(
                step.introText,
                step.targetCreature,
                step.highlightColor,
                step.splinePath,
                step.splineDuration,
                () =>
                {
                miniQuizController.ShowQuestion(
                        step.questionData.questionText,
                        step.questionData.options,
                        step.questionData.correctAnswerIndex,
                        () =>
                        {
                        currentStepIndex++;
                        RunStep();
                        }
                        );

                }
                );
    }

    [System.Serializable]
    public class IdentifyCreatureStep
    {
        [Header("Movement and Instruction.")]
        public string introText;
        public GameObject targetCreature;
        public List<SplineContainer> splinePath;
        public float splineDuration = 5f;
        public Color highlightColor = Color.cyan;

        [Header("Quiz Data")]
        public MiniQuizQuestion questionData;
    }
}
