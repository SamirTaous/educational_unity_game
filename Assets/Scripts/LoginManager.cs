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

    public TMP_Text usernamePlaceholderText;
    public TMP_Text passwordPlaceholderText;
    public TMP_Text errorText;
    public TMP_Text loginButtonText;

    public TMP_FontAsset arabicFont;
    public Button loginButton;

    private void Start()
    {
        errorText.gameObject.SetActive(false);
        loginButton.onClick.AddListener(OnLoginClicked);
        ApplyLanguageFix();
    }

    void OnLoginClicked()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError(LanguageManager.CurrentLanguage == LanguageManager.Language.Arabic
                ? "يرجى إدخال اسم المستخدم وكلمة المرور."
                : "Veuillez entrer le nom d'utilisateur et le mot de passe.");
            return;
        }

        StartCoroutine(SendLoginRequest(username, password));
    }

    IEnumerator SendLoginRequest(string username, string password)
    {
        string url = ServerConfig.Get("auth/login");

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
            ShowError(LanguageManager.CurrentLanguage == LanguageManager.Language.Arabic
                ? "خطأ في الخادم: " + request.error
                : "Erreur du serveur : " + request.error);
        }
        else
        {
            string responseText = request.downloadHandler.text;
            LoginResponse res = JsonUtility.FromJson<LoginResponse>(responseText);

            if (responseText.Contains("Invalid credentials") || responseText.Contains("error"))
            {
                ShowError(LanguageManager.CurrentLanguage == LanguageManager.Language.Arabic
                    ? "فشل تسجيل الدخول. تحقق من بيانات الاعتماد الخاصة بك."
                    : "Échec de la connexion. Vérifiez vos identifiants.");
            }
            else
            {
                SessionData.user_id = res.user_id;
                SessionData.username = res.username;
                SessionData.role = res.role;
                SessionData.level = res.level;

                Debug.Log("Connexion réussie. ID utilisateur : " + SessionData.user_id);
                Debug.Log("Rôle : " + SessionData.role);

                if (SessionData.role == "student")
                    SceneManager.LoadScene("MainMenu");
                else if (SessionData.role == "teacher")
                    SceneManager.LoadScene("TeacherMenu");
                else
                    ShowError("Rôle inconnu : " + SessionData.role);
            }
        }
    }

    void ShowError(string message)
    {
        errorText.text = message;
        errorText.gameObject.SetActive(true);
        ApplyLanguageFix();
    }

    void ApplyLanguageFix()
    {
        bool isArabic = LanguageManager.CurrentLanguage == LanguageManager.Language.Arabic;

        // === Error Text ===
        if (errorText != null)
        {
            errorText.alignment = isArabic ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
            if (arabicFont != null && isArabic) errorText.font = arabicFont;
            UpdateFixArabicComponent(errorText, isArabic);
        }

        // === Placeholder: Username ===
        if (usernamePlaceholderText != null)
        {
            usernamePlaceholderText.text = isArabic ? "أدخل اسم المستخدم..." : "Entrer le nom d’utilisateur...";
            usernamePlaceholderText.alignment = isArabic ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
            if (arabicFont != null && isArabic) usernamePlaceholderText.font = arabicFont;
            UpdateFixArabicComponent(usernamePlaceholderText, isArabic);
        }

        // === Placeholder: Password ===
        if (passwordPlaceholderText != null)
        {
            passwordPlaceholderText.text = isArabic ? "أدخل كلمة المرور..." : "Entrer le mot de passe...";
            passwordPlaceholderText.alignment = isArabic ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
            if (arabicFont != null && isArabic) passwordPlaceholderText.font = arabicFont;
            UpdateFixArabicComponent(passwordPlaceholderText, isArabic);
        }

        // === Login Button Text ===
        if (loginButtonText != null)
        {
            loginButtonText.text = isArabic ? "تسجيل الدخول" : "Login";
            loginButtonText.alignment = TextAlignmentOptions.Center;
            if (arabicFont != null && isArabic) loginButtonText.font = arabicFont;
            UpdateFixArabicComponent(loginButtonText, isArabic);
        }
    }

    void UpdateFixArabicComponent(TMP_Text textComponent, bool isArabic)
    {
        if (textComponent == null) return;

        var fixer = textComponent.GetComponent<FixArabicTMProUGUI>();
        if (isArabic)
        {
            if (fixer == null)
                textComponent.gameObject.AddComponent<FixArabicTMProUGUI>();
        }
        else
        {
            if (fixer != null)
                Destroy(fixer);
        }
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
