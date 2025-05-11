// OpenQuestionManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;
using System.Collections.Generic;

public class OpenQuestionManager : MonoBehaviour
{
    public TMP_Text questionText;
    public TMP_Text progressText;
    public Slider progressBar;
    public GameObject questionPanel;
    public TMP_InputField answerInputField;
    public Button nextButton;
    public GameObject finishPanel;
    public TMP_Text finishTitleText;
    public TMP_Text finishSummaryText;
    public GameObject questionTitle;
    public Button restartButton;

    private OpenQuestion[] questions;
    private int currentQuestionIndex = 0;
    private string[] userAnswers;

    void Start()
    {
        LoadQuestions();
        DisplayCurrentQuestion();
    }

    void LoadQuestions()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("openquestions");
        if (jsonFile != null)
        {
            FullQuestionData fullData = JsonUtility.FromJson<FullQuestionData>(WrapJson(jsonFile.text));
            questions = System.Array.FindAll(fullData.questions, q => q.type == "open");
            userAnswers = new string[questions.Length];
        }
        else
        {
            Debug.LogError("openquestions.json not found in Resources folder.");
        }
    }

    void DisplayCurrentQuestion()
    {
        if (questions == null || currentQuestionIndex >= questions.Length)
        {
            ShowFinalResult();
            return;
        }

        OpenQuestion q = questions[currentQuestionIndex];

        progressText.text = $"{currentQuestionIndex + 1}/{questions.Length}";
        progressBar.value = (float)(currentQuestionIndex + 1) / questions.Length;

        questionText.text = q.question;
        answerInputField.text = "";
        answerInputField.gameObject.SetActive(true);

        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(OnNextButtonPressed);
    }

    void OnNextButtonPressed()
    {
        userAnswers[currentQuestionIndex] = answerInputField.text.Trim();
        currentQuestionIndex++;
        DisplayCurrentQuestion();
    }

    void ShowFinalResult()
    {
        progressBar.value = 1f;
        progressText.gameObject.SetActive(false);
        progressBar.gameObject.SetActive(false);
        questionPanel.SetActive(false);
        nextButton.gameObject.SetActive(false);
        questionTitle.SetActive(false);
        questionText.gameObject.SetActive(false);
        answerInputField.gameObject.SetActive(false);

        finishPanel.SetActive(true);
        finishTitleText.text = "Thank you!";

        finishSummaryText.text = "Your responses:\n\n";
        for (int i = 0; i < questions.Length; i++)
        {
            finishSummaryText.text += $"→ {userAnswers[i]}\n\n";
        }

        SaveAnswersToJsonFile();

        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(RestartQuiz);

        StartCoroutine(FadeInFinishPanel());
    }

    void SaveAnswersToJsonFile()
    {
        string folderPath = Path.Combine(Application.dataPath, "OpenAnswers");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        string path = Path.Combine(folderPath, "user_answers.json");

        List<AnswerRecord> allAnswers = new List<AnswerRecord>();

        if (File.Exists(path))
        {
            string existingJson = File.ReadAllText(path);
            AnswerList existingList = JsonUtility.FromJson<AnswerList>(existingJson);
            if (existingList != null && existingList.answers != null)
            {
                allAnswers.AddRange(existingList.answers);
            }
        }

        for (int i = 0; i < questions.Length; i++)
        {
            allAnswers.Add(new AnswerRecord
            {
                question = questions[i].question,
                answer = userAnswers[i]
            });
        }

        AnswerList newList = new AnswerList { answers = allAnswers.ToArray() };
        string json = JsonUtility.ToJson(newList, true);
        File.WriteAllText(path, json);

        Debug.Log("Answers saved to JSON at: " + path);
    }

    void RestartQuiz()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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

    string WrapJson(string raw)
    {
        int start = raw.IndexOf("\"questions\":") + "\"questions\":".Length;
        int end = raw.LastIndexOf("]") + 1;
        string questionArray = raw.Substring(start, end - start);
        return "{\"questions\":" + questionArray + "}";
    }

    [System.Serializable]
    public class OpenQuestion
    {
        public string question;
        public string type;
    }

    [System.Serializable]
    public class FullQuestionData
    {
        public OpenQuestion[] questions;
    }

    [System.Serializable]
    public class AnswerRecord
    {
        public string question;
        public string answer;
    }

    [System.Serializable]
    public class AnswerList
    {
        public AnswerRecord[] answers;
    }
}
