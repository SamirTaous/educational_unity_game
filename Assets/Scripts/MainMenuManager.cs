using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    public TMP_Text userIdText;

    void Start()
    {
        if (!string.IsNullOrEmpty(SessionData.user_id))
        {
            userIdText.text = "User ID: " + SessionData.user_id;
        }
        else
        {
            userIdText.text = "Not logged in.";
        }
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
