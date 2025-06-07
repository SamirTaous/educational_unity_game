using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoginManager : MonoBehaviour
{
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_Text errorText;
    public Button loginButton;

    private void Start()
    {
        errorText.gameObject.SetActive(false);
        loginButton.onClick.AddListener(OnLoginClicked);
    }

    void OnLoginClicked()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Please enter both username and password.");
            return;
        }

        StartCoroutine(SendLoginRequest(username, password));
    }

    IEnumerator SendLoginRequest(string username, string password)
    {
        string url = "http://localhost:5000/auth/login";

        LoginRequest payload = new LoginRequest
        {
            username = username,
            password = password
        };

        string json = JsonUtility.ToJson(payload);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            ShowError("Server error: " + request.error);
        }
        else
        {
            string responseText = request.downloadHandler.text;
            LoginResponse res = JsonUtility.FromJson<LoginResponse>(responseText);

            if (responseText.Contains("Invalid credentials") || responseText.Contains("error"))
            {
                ShowError("Login failed. Check your credentials.");
            }
            else
            {
                // Save session data
                SessionData.user_id = res.user_id;
                SessionData.username = res.username;
                SessionData.role = res.role;
                SessionData.level = res.level;

                Debug.Log("✅ Login successful. User ID: " + SessionData.user_id);
                Debug.Log("✅ Role: " + SessionData.role);

                SceneManager.LoadScene("MainMenu");
            }
        }
    }

    void ShowError(string message)
    {
        errorText.text = message;
        errorText.gameObject.SetActive(true);
    }

    [System.Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }

    [System.Serializable]
    public class LoginResponse
    {
        public string message;
        public string username;
        public string role;
        public string level;
        public string user_id;
    }
}
