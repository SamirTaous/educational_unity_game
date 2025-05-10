using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class QuizManager : MonoBehaviour
{
    public TMP_Text questionText;
    public Button[] answerButtons;
    public TMP_Text[] answerButtonTexts;

    public TMP_Text feedbackText;
    public TMP_Text progressText;
    public Slider progressBar;

    public GameObject questionPanel;       // Drag: holds question box
    public GameObject answerPanel;         // Drag: holds the 4 answers
    public Button nextButton;              // Drag: next button
    public GameObject finishPanel;         // Drag: panel shown at the end
    public TMP_Text finishTitleText;       // Drag: "Quiz Complete!" text
    public TMP_Text finishScoreText;       // Drag: "You got X out of Y" text

    private Question[] questions;
    private int currentQuestionIndex = 0;
    private int correctAnswers = 0;
    private bool answered = false;

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
        if (questions == null || currentQuestionIndex >= questions.Length)
        {
            ShowFinalResult();
            return;
        }

        Question q = questions[currentQuestionIndex];

        // Set progress
        progressText.text = $"{currentQuestionIndex + 1}/{questions.Length}";
        progressBar.value = (float)(currentQuestionIndex + 1) / questions.Length;

        // Show question
        questionText.text = q.question;
        feedbackText.text = "";

        // Assign answers
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
            feedbackText.text = "Correct!";
            correctAnswers++;
        }
        else
        {
            feedbackText.text = "Wrong!";
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
        // Fill progress bar completely
        progressBar.value = 1f;
        progressText.text = "Quiz Complete!";

        // Hide quiz UI
        questionPanel.SetActive(false);
        answerPanel.SetActive(false);
        nextButton.gameObject.SetActive(false);
        progressText.gameObject.SetActive(false);
        progressBar.gameObject.SetActive(false);
        feedbackText.gameObject.SetActive(false);

        // Show finish panel
        finishPanel.SetActive(true);
        finishTitleText.text = "Quiz Complete!";
        finishScoreText.text = $"You got {correctAnswers} out of {questions.Length} correct.";
    }

    public void RestartQuiz()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
