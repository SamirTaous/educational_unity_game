using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StudentItemUI : MonoBehaviour
{
    public TMP_Text usernameText;
    public TMP_Text levelText;
    public Button rowButton;

    private string currentUsername;

    public void Setup(string username, string level, System.Action<string> onClick)
    {
        currentUsername = username;
        usernameText.text = username;
        levelText.text = level ?? "None";
        rowButton.onClick.AddListener(() => onClick.Invoke(currentUsername));
    }
}
