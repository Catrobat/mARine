using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizManager: MonoBehaviour {

    [System.Serializable]
    public class Question
    {
        public string questionText;
        public string[] options;
        public int correctAnswerIndex;
    }

    public Question[] questions;
    public TextMeshProUGUI questionText;
    public Button[] optionButtons;  // Assign in inspector
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI scoreText;

    public GameObject quizPanel;
    public GameObject resultPanel;
    public GameObject starfishPrefab;
    public Transform starfishSpawnPoint;
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI resultScoreText;
    public GameObject starfishRewardImage;
    public Button tryAgain;
    public Button tryAgain1;

    public GameObject statusPanel;

    int currentQuestion = 0;
    int score = 0;
    int selectedOption = -1;
    bool hasAnswered = false;

    void Start()
    {
        bool hasUnlockedStarfish = PlayerPrefs.GetInt("StarfishUnlocked", 0) == 1;

        if (hasUnlockedStarfish)
        {
            quizPanel.SetActive(false);
            resultPanel.SetActive(false);
            statusPanel.SetActive(true);


            tryAgain1.onClick.RemoveAllListeners();
            // Attach Listner to TryAgain button
            tryAgain1.onClick.AddListener(() => {
                    statusPanel.SetActive(false);
                    TryAgain();
                    });
        }
        else
        {
            statusPanel.SetActive(false);
            resultPanel.SetActive(false);
            quizPanel.SetActive(true);
            DisplayQuestion();
        }
    }

    void DisplayQuestion()
    {
        selectedOption = -1;
        hasAnswered = false;
        questionText.text = questions[currentQuestion].questionText;
        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = questions[currentQuestion].options[i];
            optionButtons[i].interactable = true;  // Re-enable button
            optionButtons[i].GetComponent<Image>().color = Color.white;
            int index = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => SelectOption(index));
        }
        feedbackText.text = "";
        scoreText.text = $"Score: {score}/{questions.Length}";
    }

    void SelectOption(int index)
    {
        if (hasAnswered) return;
        
        selectedOption = index;
        hasAnswered = true;
        
        // Disable all buttons to prevent further clicking
        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].interactable = false;
        }
        
        // Highlight selected button
        optionButtons[selectedOption].GetComponent<Image>().color = Color.yellow;
        
        // Immediately check answer
        if (selectedOption == questions[currentQuestion].correctAnswerIndex)
        {
            score++;
            feedbackText.text = "Correct!";
            optionButtons[index].GetComponent<Image>().color = Color.green;  // Green -> Correct answer
        }
        else
        {
            feedbackText.text = "Incorrect!";
            optionButtons[index].GetComponent<Image>().color = Color.red;  // Red -> Wrong answer
            
            // Show correct answer
            optionButtons[questions[currentQuestion].correctAnswerIndex].GetComponent<Image>().color = Color.green;
        }
        
        // Update score immediately
        scoreText.text = $"Score: {score}/{questions.Length}";
        
        // Automatically move to the next question after a delay of 2sec
        Invoke("NextQuestion", 2f);
    }

    void NextQuestion()
    {
        currentQuestion++;
        if (currentQuestion < questions.Length)
        {
            DisplayQuestion();
        }
        else
        {
            ShowResults();
        }
    }
    
    void ShowResults()
    {
        quizPanel.SetActive(false);
        resultPanel.SetActive(true);

        // Set result title and score
        if (resultTitleText != null)
            resultTitleText.text = "Quiz Complete!";
        if (resultScoreText != null)
            resultScoreText.text = $"You scored {score} out of {questions.Length}!";

        float percent = (float)score / questions.Length;

        // Show or hide the starfish reward in the result panel
        if (starfishRewardImage != null)
            starfishRewardImage.SetActive(percent >= 0.9f);

        // Optionally, also spawn a 3D starfish in AR if you want
        if (percent >= 0.9f && starfishPrefab != null && starfishSpawnPoint != null) {
            Instantiate(starfishPrefab, starfishSpawnPoint.position, Quaternion.identity);

            // Unlocking the starfish
            StarStatus starStatus = starfishPrefab.GetComponent<StarStatus>();
            if (starStatus != null) {
                starStatus.StarUnlocked();
                PlayerPrefs.SetInt("StarfishUnlocked", 1);  // 1 = true
                PlayerPrefs.Save();
            }
        }

        tryAgain.onClick.RemoveAllListeners();
        tryAgain.onClick.AddListener(() => TryAgain());
    }

    void TryAgain()
    {
        resultPanel.SetActive(false);
        quizPanel.SetActive(true);

        currentQuestion = 0;
        score = 0;
        selectedOption = -1;
        hasAnswered = false;

        DisplayQuestion();
    }
}
