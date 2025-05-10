using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuizManager : MonoBehaviour
{
    public TMP_Text questionText;
    public Button[] answerButtons;
    public TMP_Text[] answerButtonTexts;

    public TMP_Text feedbackText;        // Drag in Inspector
    public TMP_Text resultText;          // Drag in Inspector

    private Question[] questions;
    private int currentQuestionIndex = 0;
    private int correctAnswers = 0;

    private bool answered = false;       // New flag to control flow

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
        feedbackText.text = ""; // Clear previous feedback

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
            answerButtons[i].interactable = true;
        }

        answered = false;
    }

    void OnAnswerSelected(int index)
    {
        if (answered || currentQuestionIndex >= questions.Length)
            return;

        answered = true;

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

        foreach (var button in answerButtons)
        {
            button.interactable = false;
        }
    }

    public void OnNextButtonPressed()
    {
        if (!answered) return;

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
