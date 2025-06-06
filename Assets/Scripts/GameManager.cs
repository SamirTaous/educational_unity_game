using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class QuizManager : MonoBehaviour
{
    public TMP_Text questionText;
    public Button[] answerButtons;
    public TMP_Text[] answerButtonTexts;

    public TMP_Text progressText;
    public Slider progressBar;

    public GameObject questionPanel;
    public GameObject answerPanel;
    public Button nextButton;

    public GameObject finishPanel;
    public TMP_Text finishTitleText;
    public TMP_Text finishScoreText;
    public GameObject questionTitle;

    public Button restartButton;
    public Button backToMenuButton; // ✅ New

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

        progressText.text = $"{currentQuestionIndex + 1}/{questions.Length}";
        progressBar.value = (float)(currentQuestionIndex + 1) / questions.Length;

        questionText.text = q.question;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            int index = i;
            answerButtonTexts[i].text = q.answers[i];

            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(index));
            answerButtons[i].interactable = true;

            answerButtons[i].GetComponent<Image>().color = Color.white;
        }

        answered = false;
    }

    void OnAnswerSelected(int index)
    {
        if (answered || currentQuestionIndex >= questions.Length)
            return;

        answered = true;

        Question q = questions[currentQuestionIndex];

        for (int i = 0; i < answerButtons.Length; i++)
        {
            Image btnImage = answerButtons[i].GetComponent<Image>();

            if (i == q.correctAnswerIndex)
            {
                btnImage.color = new Color32(80, 200, 120, 255); // ✅ Green
            }
            else if (i == index)
            {
                btnImage.color = new Color32(200, 80, 80, 255); // ❌ Red
            }
            else
            {
                btnImage.color = Color.white;
            }

            answerButtons[i].interactable = false;
        }

        if (index == q.correctAnswerIndex)
        {
            correctAnswers++;
            StartCoroutine(BounceButton(answerButtons[index]));
        }
        else
        {
            StartCoroutine(ShakeButton(answerButtons[index]));
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
        progressBar.value = 1f;
        progressText.text = "Quiz Complete";

        questionPanel.SetActive(false);
        answerPanel.SetActive(false);
        nextButton.gameObject.SetActive(false);
        progressText.gameObject.SetActive(false);
        progressBar.gameObject.SetActive(false);
        questionTitle.SetActive(false);

        finishPanel.SetActive(true);
        finishTitleText.text = "Quiz Complete";
        finishScoreText.text = $"You got {correctAnswers} out of {questions.Length} correct.";

        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(RestartQuiz);

        backToMenuButton.onClick.RemoveAllListeners(); // ✅
        backToMenuButton.onClick.AddListener(GoToMainMenu); // ✅

        StartCoroutine(FadeInFinishPanel());
    }

    public void RestartQuiz()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void GoToMainMenu() // ✅
    {
        SceneManager.LoadScene("MainMenu");
    }

    IEnumerator FadeInFinishPanel()
    {
        CanvasGroup cg = finishPanel.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        float t = 0f;
        while (cg.alpha < 1f)
        {
            t += Time.deltaTime * 2f;
            cg.alpha = Mathf.Clamp01(t);
            yield return null;
        }
    }

    IEnumerator BounceButton(Button button)
    {
        Vector3 original = button.transform.localScale;
        Vector3 enlarged = original * 1.2f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * 5f;
            button.transform.localScale = Vector3.Lerp(original, enlarged, Mathf.Sin(t * Mathf.PI));
            yield return null;
        }

        button.transform.localScale = original;
    }

    IEnumerator ShakeButton(Button button)
    {
        Vector3 original = button.transform.localPosition;
        float elapsed = 0f;
        float duration = 0.3f;
        float strength = 10f;

        while (elapsed < duration)
        {
            float x = Mathf.Sin(elapsed * 40f) * strength;
            button.transform.localPosition = original + new Vector3(x, 0, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        button.transform.localPosition = original;
    }

    [System.Serializable]
    public class Question
    {
        public string question;
        public string[] answers;
        public int correctAnswerIndex;
    }
}
