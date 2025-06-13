using UnityEngine;
using TMPro;

public class LanguageDropdownHandler : MonoBehaviour
{
    public TMP_Dropdown languageDropdown;

    void Start()
    {
        languageDropdown.value = LanguageManager.CurrentLanguage == LanguageManager.Language.French ? 0 : 1;
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

        ApplyLanguageFix(LanguageManager.CurrentLanguage);
    }

    void OnLanguageChanged(int index)
    {
        if (index == 0)
            LanguageManager.SetLanguage(LanguageManager.Language.French);
        else if (index == 1)
            LanguageManager.SetLanguage(LanguageManager.Language.Arabic);

        ApplyLanguageFix(LanguageManager.CurrentLanguage);

        FindObjectOfType<SpaceMenuManager>()?.UpdateLanguageTexts();
    }

    void ApplyLanguageFix(LanguageManager.Language lang)
    {
        // Fix the caption (selected item text)
        TMP_Text caption = languageDropdown.captionText;
        FixTMPTextComponent(caption, lang);

        // Fix each dropdown list item
        var itemTemplate = languageDropdown.template;
        TMP_Text[] itemTexts = itemTemplate.GetComponentsInChildren<TMP_Text>(true);
        foreach (var item in itemTexts)
        {
            FixTMPTextComponent(item, lang);
        }
    }

    void FixTMPTextComponent(TMP_Text textComponent, LanguageManager.Language lang)
    {
        if (textComponent == null) return;

        var fixer = textComponent.GetComponent<FixArabicTMProUGUI>();

        if (lang == LanguageManager.Language.Arabic)
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
}
