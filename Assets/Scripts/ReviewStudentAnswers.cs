using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using SimpleJSON;

public class ReviewStudentAnswers : MonoBehaviour
{
    public TMP_Dropdown textDropdown;
    public TMP_Text selectedTextDisplay;
    public Button loadAnswersButton;

    public GameObject textSelectionPanel;    // NEW: Container for dropdown + passage
    public GameObject studentListPanel;       // NEW: Container for student scroll view
    public GameObject studentListContainer;   // Content inside Scroll View
    public GameObject studentButtonPrefab;

    public GameObject reviewPanel;
    public TMP_Text reviewPanelContent;
    public Button validateButton;
    public GameObject mainView;

    private List<string> textIds = new List<string>();
    private JSONArray loadedAnswers;
    private JSONNode currentAnswer;

    void Start()
    {
        reviewPanel.SetActive(false);
        studentListPanel.SetActive(false);
        StartCoroutine(PopulateTextDropdown());
        loadAnswersButton.onClick.AddListener(LoadUnreviewedAnswers);
        validateButton.onClick.AddListener(ValidateCurrentAnswer);
    }

    IEnumerator PopulateTextDropdown()
    {
        UnityWebRequest request = UnityWebRequest.Get("http://localhost:5000/api/all");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var root = JSON.Parse(request.downloadHandler.text);
            textDropdown.ClearOptions();
            List<string> options = new List<string>();

            for (int i = 0; i < root.Count; i++)
            {
                options.Add("Text " + i);
                textIds.Add(root[i]["id"]);
            }

            textDropdown.AddOptions(options);
            textDropdown.onValueChanged.AddListener(UpdateTextDisplay);
            UpdateTextDisplay(0);
        }
        else
        {
            Debug.LogError("Failed to load texts: " + request.error);
        }
    }

    void UpdateTextDisplay(int index)
    {
        StartCoroutine(LoadTextContent(index));
    }

    IEnumerator LoadTextContent(int index)
    {
        UnityWebRequest request = UnityWebRequest.Get("http://localhost:5000/api/text-by-index/" + index);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var root = JSON.Parse(request.downloadHandler.text);
            selectedTextDisplay.text = root["text"]["text_content"];
        }
    }

    void LoadUnreviewedAnswers()
    {
        if (textSelectionPanel != null) textSelectionPanel.SetActive(false);
        if (studentListPanel != null) studentListPanel.SetActive(true);

        foreach (Transform child in studentListContainer.transform)
            Destroy(child.gameObject);

        string textId = textIds[textDropdown.value];
        StartCoroutine(FetchAnswers(textId));
    }

    IEnumerator FetchAnswers(string textId)
    {
        UnityWebRequest request = UnityWebRequest.Get("http://localhost:5000/api/open-question-answers/by-text/" + textId);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            loadedAnswers = JSON.Parse(request.downloadHandler.text).AsArray;
            foreach (JSONNode answer in loadedAnswers)
            {
                string studentId = answer["student_id"];
                StartCoroutine(CreateStudentButton(answer, studentId));
            }
        }
    }

    IEnumerator CreateStudentButton(JSONNode answerData, string studentId)
    {
        UnityWebRequest request = UnityWebRequest.Get("http://localhost:5000/api/students");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var students = JSON.Parse(request.downloadHandler.text).AsArray;
            foreach (JSONNode s in students)
            {
                if (s["_id"] == studentId)
                {
                    GameObject buttonObj = Instantiate(studentButtonPrefab, studentListContainer.transform);
                    buttonObj.GetComponentInChildren<TMP_Text>().text = s["username"] + " (" + s["level"] + ")";
                    buttonObj.GetComponent<Button>().onClick.AddListener(() => ShowReviewPanel(answerData));
                    break;
                }
            }
        }
    }

    void ShowReviewPanel(JSONNode answerData)
    {
        if (studentListPanel != null) studentListPanel.SetActive(false);
        reviewPanel.SetActive(true);
        mainView.SetActive(false);

        currentAnswer = answerData;

        string content = "Reviewing submission:\n\n";
        JSONArray answers = answerData["answers"].AsArray;
        for (int i = 0; i < answers.Count; i++)
        {
            var ans = answers[i];
            content += $"Q{i + 1}: {ans["question"]}\n";
            content += $"Answer: {ans["answer"]}\n";
            content += $"Score: {ans["score"]}/5\n";
            content += $"Feedback: {ans["feedback"]}\n\n";
        }

        reviewPanelContent.text = content;
    }

    void ValidateCurrentAnswer()
    {
        StartCoroutine(SendValidation());
    }

    IEnumerator SendValidation()
    {
        string jsonData = currentAnswer.ToString();

        UnityWebRequest post = new UnityWebRequest("http://localhost:5000/api/validated-answers", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        post.uploadHandler = new UploadHandlerRaw(bodyRaw);
        post.downloadHandler = new DownloadHandlerBuffer();
        post.SetRequestHeader("Content-Type", "application/json");

        yield return post.SendWebRequest();

        if (post.result == UnityWebRequest.Result.Success)
        {
            string id = currentAnswer["_id"];
            StartCoroutine(DeleteOriginal(id));
        }
        else
        {
            Debug.LogError("Validation failed: " + post.error);
        }
    }

    IEnumerator DeleteOriginal(string id)
    {
        UnityWebRequest delete = UnityWebRequest.Delete("http://localhost:5000/api/open-question-answers/delete/" + id);
        yield return delete.SendWebRequest();

        if (delete.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Validated and removed original");
            reviewPanel.SetActive(false);
            mainView.SetActive(true);
            LoadUnreviewedAnswers();
        }
        else
        {
            Debug.LogError("Delete failed: " + delete.error);
        }
    }
}
