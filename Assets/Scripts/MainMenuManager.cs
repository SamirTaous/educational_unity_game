// MainMenuManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void LoadOpenQuestionsScene()
    {
        SceneManager.LoadScene("OpenQuestionsScene");
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
