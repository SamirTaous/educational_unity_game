using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class StudentDetailPanel : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text usernameText;
    public TMP_Text currentLevelText;
    public TMP_Dropdown levelDropdown;

    public Button updateLevelButton;
    public Button deleteButton;
    public Button backButton;

    public GameObject studentPanel;
    public GameObject studentListPanel;

    private string currentUsername;
    private StudentManager manager;

    void Start()
    {
        updateLevelButton.onClick.AddListener(OnAssignClicked);
        deleteButton.onClick.AddListener(OnDeleteClicked);
        backButton.onClick.AddListener(OnBackClicked);

        manager = FindObjectOfType<StudentManager>();

        if (levelDropdown.options.Count == 0)
        {
            levelDropdown.ClearOptions();
            levelDropdown.AddOptions(new List<string> { "A1", "A2", "B1", "B2", "C1", "C2" });
        }
    }

    public void Show(string username, string level)
    {
        currentUsername = username;
        usernameText.text = username;

        int index = levelDropdown.options.FindIndex(o => o.text == level);
        if (index >= 0)
            levelDropdown.value = index;

        studentPanel.SetActive(true);
        studentListPanel.SetActive(false);
    }

    void OnAssignClicked()
    {
        string newLevel = levelDropdown.options[levelDropdown.value].text;
        StartCoroutine(UpdateLevel(currentUsername, newLevel));
    }

    void OnDeleteClicked()
    {
        StartCoroutine(DeleteStudent(currentUsername));
    }

    void OnBackClicked()
    {
        studentPanel.SetActive(false);
        studentListPanel.SetActive(true);
    }

    IEnumerator UpdateLevel(string username, string newLevel)
    {
        string url = "http://localhost:5001/api/students/update-level";
        string json = $"{{\"username\":\"{username}\", \"level\":\"{newLevel}\"}}";

        UnityWebRequest req = new UnityWebRequest(url, "POST");
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Level updated.");
            if (currentLevelText != null)
                currentLevelText.text = "Student Level: " + newLevel;

            if (manager != null)
                manager.RefreshList(); // refresh list
        }
        else
        {
            Debug.LogError("Failed to update level: " + req.error);
        }
    }

    IEnumerator DeleteStudent(string username)
    {
        string url = $"http://localhost:5001/api/students/delete/{username}";
        UnityWebRequest req = UnityWebRequest.Delete(url);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Student deleted.");

            if (manager != null)
                manager.RefreshList(); // refresh list

            OnBackClicked();
        }
        else
        {
            Debug.LogError("Failed to delete student: " + req.error);
        }
    }
    
}
