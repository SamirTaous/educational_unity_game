using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
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
    }

    IEnumerator LoadQuestionsByIndex(int index)
    {
        string url = $"http://localhost:5000/api/text-by-index/{index}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                var root = JSON.Parse(json);

                textId = root["text"]["id"];
                string passage = root["text"]["text_content"];
                if (passageText != null) passageText.text = passage;

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
                Debug.LogError("Failed to load questions by index: " + request.error);
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
        string answer = answerInputField.text.Trim();
        string questionText = questions[currentQuestionIndex].question;

        userAnswers[currentQuestionIndex] = answer;
        StartCoroutine(SendAnswerForFeedback(answer, questionText));
    }

    IEnumerator SendAnswerForFeedback(string answer, string question)
    {
        string url = "http://localhost:8000/feedback";

        FeedbackRequest requestData = new FeedbackRequest
        {
            reponse = answer,
            question = question,
            niveau = SessionData.level,
            difficulte = SessionData.level
        };

        string jsonData = JsonUtility.ToJson(requestData);
        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;
            FeedbackResponse response = JsonUtility.FromJson<FeedbackResponse>(jsonResponse);
            scores.Add(response.note);
            feedbacks.Add(response.feedback);
        }
        else
        {
            Debug.LogError("API error: " + request.error);
            scores.Add(0f);
            feedbacks.Add("Erreur lors de l’évaluation.");
        }

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

        float total = 0f;
        string resultText = "Résultats par question:\n";

        for (int i = 0; i < scores.Count; i++)
        {
            total += scores[i];
            resultText += $"- Q{i + 1}: {scores[i]}/5\n";
        }

        float average = scores.Count > 0 ? total / scores.Count : 0f;
        resultText += $"\nScore final: {average:F2}/5";

        finishTitleText.text = "Merci !";
        finishSummaryText.text = resultText;

        SaveAnswersToMongoDB();
        StartCoroutine(FadeInFinishPanel());
    }

    void SaveAnswersToMongoDB()
{
    string url = "http://localhost:5000/api/open-question-answers";

    float total = 0f;
    for (int i = 0; i < scores.Count; i++) total += scores[i];
    float average = scores.Count > 0 ? total / scores.Count : 0f;

    JSONArray answerArray = new JSONArray();
    for (int i = 0; i < questions.Length; i++)
    {
        JSONObject obj = new JSONObject();
        obj["question"] = questions[i].question;
        obj["answer"] = userAnswers[i];
        obj["score"] = scores[i];
        obj["feedback"] = feedbacks[i];
        answerArray.Add(obj);
    }

    JSONObject root = new JSONObject();
    root["student_id"] = SessionData.user_id;
    root["text_id"] = textId;
    root["answers"] = answerArray;
    root["final_score"] = average;

    Debug.Log("JSON sent to MongoDB endpoint:\n" + root.ToString());
    StartCoroutine(PostJsonToAPI(url, root.ToString()));
}

    IEnumerator PostJsonToAPI(string url, string jsonData)
    {
        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            Debug.Log("Answers saved to MongoDB.");
        else
            Debug.LogError("MongoDB save failed: " + request.error);
    }

    void RestartQuiz()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
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
        public float score;
        public string feedback;
    }

    [System.Serializable]
    public class MongoSavePayload
    {
        public string student_id;
        public string text_id;
        public AnswerRecord[] answers;
    }

    [System.Serializable]
    public class FeedbackRequest
    {
        public string reponse;
        public string question;
        public string niveau;
        public string difficulte;
    }

    [System.Serializable]
    public class FeedbackResponse
    {
        public string feedback;
        public float note;
    }
}
