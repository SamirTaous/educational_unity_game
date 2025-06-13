using UnityEngine;
using TMPro;
public static class LanguageManager
{
    public enum Language { French, Arabic }
    public static Language CurrentLanguage = Language.French;

    public static void SetLanguage(Language lang)
    {
        CurrentLanguage = lang;
        Debug.Log("Language set to: " + lang);
    }
}
