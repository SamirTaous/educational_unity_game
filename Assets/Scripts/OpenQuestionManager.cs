using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;

public class OpenQuestionManager : MonoBehaviour
{
    public TMP_Text questionText;
    public TMP_Text progressText;
    public TMP_Text passageText;
    public Slider progressBar;

    public GameObject questionPanel;
    public GameObject passagePanel;
    public TMP_InputField answerInputField;
    public Button nextButton;
    public Button showTextButton;
    public Button backButton;

    public GameObject finishPanel;
    public TMP_Text finishTitleText;
    public TMP_Text finishSummaryText;
    public GameObject questionTitle;
    public Button restartButton;
    public Button backToMenuButton;

    private OpenQuestion[] questions;
    private int currentQuestionIndex = 0;
    private string[] userAnswers;
    private List<float> scores = new List<float>();
    private List<string> feedbacks = new List<string>();
    private string textId = "";

    void Start()
    {
        passagePanel.SetActive(false);
        finishPanel.SetActive(false);
        StartCoroutine(LoadQuestionsByIndex(SessionData.selectedTextIndex));

        backButton.onClick.AddListener(HidePassageAndResume);
        restartButton.onClick.AddListener(RestartQuiz);
        backToMenuButton.onClick.AddListener(GoToMainMenu);
        nextButton.onClick.AddListener(OnNextButtonPressed);
        showTextButton.onClick.AddListener(ShowPassageView);
    }

    IEnumerator LoadQuestionsByIndex(int index)
    {
        string url = $"http://localhost:5001/api/text-by-index/{index}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var root = SimpleJSON.JSON.Parse(request.downloadHandler.text);
                textId = root["text"]["id"];
                passageText.text = root["text"]["text_content"];
                ApplyArabicFix(passageText);

                var qArray = root["questions"].AsArray;
                questions = new OpenQuestion[qArray.Count];
                userAnswers = new string[qArray.Count];

                for (int i = 0; i < qArray.Count; i++)
                {
                    questions[i] = new OpenQuestion
                    {
                        question = qArray[i]["question"],
                        type = qArray[i]["type"],
                        reference = qArray[i]["reference_answer"]
                    };
                }

                DisplayCurrentQuestion();
            }
            else
            {
                Debug.LogError("Failed to load questions: " + request.error);
            }
        }
    }

    void DisplayCurrentQuestion()
    {
        if (currentQuestionIndex >= questions.Length)
        {
            ShowFinalResult();
            return;
        }

        var q = questions[currentQuestionIndex];
        progressText.text = $"{currentQuestionIndex + 1}/{questions.Length}";
        progressBar.value = (float)(currentQuestionIndex + 1) / questions.Length;

        questionText.text = q.question;
        ApplyArabicFix(questionText);

        answerInputField.text = "";
        questionPanel.SetActive(true);
    }

    void OnNextButtonPressed()
    {
        string answer = answerInputField.text.Trim();
        userAnswers[currentQuestionIndex] = answer;
        StartCoroutine(SendAnswerForFeedback(answer));
    }

    IEnumerator SendAnswerForFeedback(string answer)
    {
        string url = "http://localhost:8000/eval";

        var data = new FeedbackRequest
        {
            question = questions[currentQuestionIndex].question,
            reponse_ref = questions[currentQuestionIndex].reference,
            reponse_eleve = answer,
            niveau = SessionData.level
        };

        string jsonData = JsonUtility.ToJson(data);
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<FeedbackResponse>(request.downloadHandler.text);
            scores.Add(response.note);
            feedbacks.Add(response.feedback);
        }
        else
        {
            scores.Add(0f);
            feedbacks.Add("حدث خطأ أثناء التقييم.");
            Debug.LogError("Evaluation failed: " + request.error);
        }

        currentQuestionIndex++;
        DisplayCurrentQuestion();
    }

    void ShowFinalResult()
    {
        float total = 0f;
        string resultText = "النتائج حسب السؤال:\n";

        for (int i = 0; i < scores.Count; i++)
        {
            total += scores[i];
            resultText += $"- س{i + 1}: {scores[i]}/10\n";
        }

        float avg = scores.Count > 0 ? total / scores.Count : 0f;
        resultText += $"\nالمعدل النهائي: {avg:F2}/10";

        finishTitleText.text = "شكراً لك!";
        finishSummaryText.text = resultText;
        ApplyArabicFix(finishTitleText);
        ApplyArabicFix(finishSummaryText);

        SaveAnswersToMongoDB(avg);

        questionPanel.SetActive(false);
        finishPanel.SetActive(true);
        StartCoroutine(FadeInFinishPanel());
    }

    void SaveAnswersToMongoDB(float average)
    {
        string url = "http://localhost:5001/api/open-question-answers";

        var answersArray = new SimpleJSON.JSONArray();
        for (int i = 0; i < questions.Length; i++)
        {
            var obj = new SimpleJSON.JSONObject();
            obj["question"] = questions[i].question;
            obj["answer"] = userAnswers[i];
            obj["score"] = scores[i];
            obj["feedback"] = feedbacks[i];
            answersArray.Add(obj);
        }

        var root = new SimpleJSON.JSONObject();
        root["student_id"] = SessionData.user_id;
        root["text_id"] = textId;
        root["answers"] = answersArray;
        root["final_score"] = average;

        StartCoroutine(PostJsonToAPI(url, root.ToString()));
    }

    IEnumerator PostJsonToAPI(string url, string jsonData)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
            Debug.LogError("Failed to save to DB: " + request.error);
    }

    void ShowPassageView()
    {
        questionPanel.SetActive(false);
        passagePanel.SetActive(true);
    }

    void HidePassageAndResume()
    {
        passagePanel.SetActive(false);
        questionPanel.SetActive(true);
    }

    IEnumerator FadeInFinishPanel()
    {
        CanvasGroup cg = finishPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = finishPanel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        while (cg.alpha < 1f)
        {
            cg.alpha += Time.deltaTime * 2f;
            yield return null;
        }
    }

    void RestartQuiz()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    void ApplyArabicFix(TMP_Text text)
    {
        if (text != null && text.GetComponent<FixArabicTMProUGUI>() == null)
            text.gameObject.AddComponent<FixArabicTMProUGUI>();
    }

    [System.Serializable]
    public class OpenQuestion
    {
        public string question;
        public string type;
        public string reference;
    }

    [System.Serializable]
    public class FeedbackRequest
    {
        public string question;
        public string reponse_ref;
        public string reponse_eleve;
        public string niveau;
    }

    [System.Serializable]
    public class FeedbackResponse
    {
        public string feedback;
        public float note;
    }
}
