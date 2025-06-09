using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TeacherMenuManager : MonoBehaviour
{
    public TMP_Text userIdText;

    void Start()
    {
        if (!string.IsNullOrEmpty(SessionData.user_id))
            userIdText.text = "User ID: " + SessionData.user_id;
        else
            userIdText.text = "Not logged in.";
    }

    public void LoadStudentsList()
    {
        SceneManager.LoadScene("StudentsList");
    }

    public void Logout()
    {
        // Clear session data
        SessionData.user_id = null;
        SessionData.username = null;
        SessionData.role = null;
        SessionData.level = null;

        // Go back to space selection scene
        SceneManager.LoadScene("SpaceMenu"); // Make sure this is added to Build Settings
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}
