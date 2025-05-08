using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuizManager : MonoBehaviour
{
    public TMP_Text questionText;
    public Button[] answerButtons;
    public TMP_Text[] answerButtonTexts;

    public TMP_Text feedbackText;        // ← Add this in Inspector
    public TMP_Text resultText;          // ← Add this in Inspector

    private Question[] questions;
    private int currentQuestionIndex = 0;
    private int correctAnswers = 0;

    void Start()
    {
        LoadQuestions();
        DisplayCurrentQuestion();
    }

    void LoadQuestions()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("questions");
        if (jsonFile != null)
        {
            questions = JsonHelper.FromJson<Question>(jsonFile.text);
        }
        else
        {
            Debug.LogError("questions.json not found in Resources folder.");
        }
    }

    void DisplayCurrentQuestion()
    {
        feedbackText.text = ""; // Clear feedback each time

        if (questions == null || currentQuestionIndex >= questions.Length)
        {
            ShowFinalResult();
            return;
        }

        Question q = questions[currentQuestionIndex];
        questionText.text = q.question;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            int index = i;

            answerButtonTexts[i].text = q.answers[i];
            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(index));
        }
    }

    void OnAnswerSelected(int index)
    {
        if (currentQuestionIndex >= questions.Length)
            return;

        Question q = questions[currentQuestionIndex];

        if (index == q.correctAnswerIndex)
        {
            feedbackText.text = "✅ Correct!";
            correctAnswers++;
        }
        else
        {
            feedbackText.text = "❌ Wrong!";
        }

        // Disable buttons to prevent spamming
        foreach (var button in answerButtons)
        {
            button.interactable = false;
        }

        // Move to next question after short delay
        Invoke(nameof(NextQuestion), 1.5f);
    }

    void NextQuestion()
    {
        foreach (var button in answerButtons)
        {
            button.interactable = true;
        }

        currentQuestionIndex++;
        DisplayCurrentQuestion();
    }

    void ShowFinalResult()
    {
        questionText.text = "Quiz Complete!";
        resultText.text = $"You got {correctAnswers} out of {questions.Length} correct.";

        feedbackText.text = "";
        foreach (var button in answerButtons)
        {
            button.gameObject.SetActive(false);
        }
    }
}
