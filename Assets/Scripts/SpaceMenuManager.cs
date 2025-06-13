using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SpaceMenuManager : MonoBehaviour
{
    public TMP_Text studentText;
    public TMP_Text teacherText;
    public TMP_FontAsset arabicFont; // assign Cairo Medium here via Inspector

    public void UpdateLanguageTexts()
    {
        bool isArabic = LanguageManager.CurrentLanguage == LanguageManager.Language.Arabic;

        UpdateText(studentText, isArabic ? "مساحة التلميذ" : "Student Space", isArabic);
        UpdateText(teacherText, isArabic ? "مساحة الأستاذ" : "Teacher Space", isArabic);
    }

    private void UpdateText(TMP_Text textComponent, string newText, bool isArabic)
    {
        if (textComponent == null) return;

        textComponent.text = newText;

        if (isArabic)
        {
            // Change font
            if (arabicFont != null)
                textComponent.font = arabicFont;

            // Add RTL fix
            if (textComponent.GetComponent<FixArabicTMProUGUI>() == null)
                textComponent.gameObject.AddComponent<FixArabicTMProUGUI>();
        }
        else
        {
            // Reset to center
            textComponent.alignment = TextAlignmentOptions.Center;

            // Remove RTL fix
            var fixer = textComponent.GetComponent<FixArabicTMProUGUI>();
            if (fixer != null)
                Destroy(fixer);
        }
    }

    public void LoadLoginScreen()
    {
        SceneManager.LoadScene("LoginScreen");
    }

    void Start()
    {
        UpdateLanguageTexts();
    }
}
