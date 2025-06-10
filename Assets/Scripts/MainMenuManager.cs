using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using SimpleJSON;
using System.Collections.Generic;

public class MainMenuManager : MonoBehaviour
{
    public TMP_Text userIdText;
    public TMP_Dropdown textDropdown;

    private List<string> textIds = new List<string>(); 

    public TMP_Text welcomeText;     
    public TMP_Text badgeText;        

    void Start()
    {
        if (!string.IsNullOrEmpty(SessionData.user_id))
            userIdText.text = "User ID: " + SessionData.user_id;
        else
            userIdText.text = "Not logged in.";

        if (!string.IsNullOrEmpty(SessionData.username))
        {
            welcomeText.text = $"Hi {SessionData.username}";
            badgeText.text = SessionData.username.Substring(0, 1).ToUpper();
        }   

        StartCoroutine(PopulateTextDropdown());
    }

    IEnumerator PopulateTextDropdown()
    {
        string url = "http://localhost:5000/api/all";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            var allTexts = JSON.Parse(json);

            textDropdown.ClearOptions();
            List<string> options = new List<string>();

            for (int i = 0; i < allTexts.Count; i++)
            {
                var item = allTexts[i];
                string label = $"Text {i} ({item["questions"].Count} questions)";
                options.Add(label);
                textIds.Add(item["id"]);  // Optional
            }

            textDropdown.AddOptions(options);
            textDropdown.onValueChanged.AddListener(OnDropdownChanged);
        }
        else
        {
            Debug.LogError("Failed to load texts for dropdown: " + request.error);
        }
    }

    void OnDropdownChanged(int index)
    {
        SessionData.selectedTextIndex = index;
        Debug.Log("Selected text index: " + index);
    }

    public void LoadOpenQuestionsScene()
    {
        SceneManager.LoadScene("OpenQuestionsScene");
    }

    public void LoadReadingScene()
    {
        SceneManager.LoadScene("ReadingScene");
    }

    public void LoadClosedQuestionsScene()
    {
        SceneManager.LoadScene("ClosedQuestionsScene");
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}
