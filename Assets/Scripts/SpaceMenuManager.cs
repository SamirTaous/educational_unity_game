using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SpaceMenuManager : MonoBehaviour
{
    public void LoadLoginScreen()
    {
        SceneManager.LoadScene("LoginScreen");
    }
}
