using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;

public class OpenQuestionManager : MonoBehaviour
{
    public TMP_Text questionText;
    public TMP_Text progressText;
    public TMP_Text passageText;         // scrollable text
    public Slider progressBar;

    public GameObject questionPanel;
    public GameObject passagePanel;      // panel containing the scroll view + back button
    public TMP_InputField answerInputField;
    public Button nextButton;
    public Button showTextButton;        // button to show the text from question screen
    public Button backButton;            // button to go back from passage view

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
        passagePanel.SetActive(false);
        finishPanel.SetActive(false);
        StartCoroutine(LoadRandomTextAndQuestions());

        backButton.onClick.AddListener(HidePassageAndResume);
    }

    IEnumerator LoadRandomTextAndQuestions()
    {
        string url = "http://localhost:5000/api/random-text-open-questions";
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                var root = JSON.Parse(json);

                // Load passage text
                string passage = root["text"]["text_content"];
                if (passageText != null) passageText.text = passage;

                // Load open questions
                JSONArray qArray = root["questions"].AsArray;
                questions = new OpenQuestion[qArray.Count];
                userAnswers = new string[qArray.Count];

                for (int i = 0; i < qArray.Count; i++)
                {
                    questions[i] = new OpenQuestion
                    {
                        question = qArray[i]["question"],
                        type = qArray[i]["type"]
                    };
                }

                DisplayCurrentQuestion();
            }
            else
            {
                Debug.LogError("Failed to load from API: " + request.error);
            }
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

        showTextButton.onClick.RemoveAllListeners();
        showTextButton.onClick.AddListener(ShowPassageView);
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
        showTextButton.gameObject.SetActive(false);

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

    void ShowPassageView()
    {
        questionPanel.SetActive(false);
        questionTitle.SetActive(false);
        nextButton.gameObject.SetActive(false);
        progressBar.gameObject.SetActive(false);
        progressText.gameObject.SetActive(false);
        showTextButton.gameObject.SetActive(false);

        passagePanel.SetActive(true);
    }

    void HidePassageAndResume()
    {
        passagePanel.SetActive(false);

        questionPanel.SetActive(true);
        questionTitle.SetActive(true);
        nextButton.gameObject.SetActive(true);
        progressBar.gameObject.SetActive(true);
        progressText.gameObject.SetActive(true);
        showTextButton.gameObject.SetActive(true);
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

    [System.Serializable]
    public class OpenQuestion
    {
        public string question;
        public string type;
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
