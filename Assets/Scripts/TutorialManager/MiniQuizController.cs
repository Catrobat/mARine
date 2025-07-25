using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;

public class MiniQuizController : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject quizPanel;
    public TextMeshProUGUI questionText;
    public Button[] optionButtons;
    public TextMeshProUGUI feedbackText;

    private Action onCorrectAnswer;
    private int correctAnswerIndex;

    private readonly Color correctColor = new Color32(0x4C, 0xAF, 0x50, 0xFF);   // Green
    private readonly Color wrongColor = new Color32(0xF4, 0x43, 0x36, 0xFF);     // Red
    private readonly Color normalColor = new Color32(0xE0, 0xE0, 0xE0, 0xFF);    // Default gray

    public void ShowQuestion(string question, string[] options, int correctIndex, Action onComplete)
    {
        quizPanel.SetActive(true);
        questionText.text = question;
        feedbackText.text = "";

        onCorrectAnswer = () => { if (onComplete != null) onComplete(); }; // Only for correct answer if needed separately
        correctAnswerIndex = correctIndex;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            var btn = optionButtons[i];
            var textComponent = btn.GetComponentInChildren<TextMeshProUGUI>();
            btn.gameObject.SetActive(i < options.Length);

            if (i < options.Length)
            {
                int index = i;

                if (textComponent != null)
                    textComponent.text = options[i];

                btn.interactable = true;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SelectOption(index));
                btn.image.color = normalColor; // Reset button background
            }
        }
    }

    private void SelectOption(int selected)
    {
        foreach (var btn in optionButtons)
            btn.interactable = false;

        // Show correct/incorrect colors
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (!optionButtons[i].gameObject.activeSelf) continue;

            if (i == correctAnswerIndex)
                optionButtons[i].image.color = correctColor;
            else if (i == selected)
                optionButtons[i].image.color = wrongColor;
        }

        if (selected == correctAnswerIndex)
        {
            feedbackText.text = "Correct!";
        }
        else
        {
            feedbackText.text = $"Incorrect! Correct: {optionButtons[correctAnswerIndex].GetComponentInChildren<TextMeshProUGUI>().text}";
        }

        StartCoroutine(WaitThenHide(1.5f, selected == correctAnswerIndex));
    }

    private IEnumerator WaitThenHide(float waitTime, bool isCorrect)
    {
        yield return new WaitForSeconds(waitTime);

        quizPanel.SetActive(false);
        onCorrectAnswer?.Invoke();
    }
}

[System.Serializable]
public class MiniQuizQuestion
{
    public string questionText;
    public string[] options = new string[4];
    public int correctAnswerIndex; 
}   