using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TeacherMenuManager : MonoBehaviour
{
    public TMP_Text userIdText;

    void Start()
    {
        if (!string.IsNullOrEmpty(SessionData.user_id))
        {
            userIdText.text = "معرف المستخدم: " + SessionData.user_id;

            if (userIdText.GetComponent<FixArabicTMProUGUI>() == null)
            {
                userIdText.gameObject.AddComponent<FixArabicTMProUGUI>();
            }
        }
        else
        {
            userIdText.text = "غير متصل";

            if (userIdText.GetComponent<FixArabicTMProUGUI>() == null)
            {
                userIdText.gameObject.AddComponent<FixArabicTMProUGUI>();
            }
        }
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
