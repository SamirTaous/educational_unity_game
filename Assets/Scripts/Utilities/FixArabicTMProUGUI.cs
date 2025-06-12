using UnityEngine;
using TMPro;
using ArabicSupport;
using System;

[RequireComponent(typeof(TextMeshProUGUI))]
public class FixArabicTMProUGUI : MonoBehaviour
{
    [SerializeField] bool textAlreadySet = true;
    [SerializeField] bool updateInRealTime = true;
    [Tooltip("Update only if the text or size changed (better performance)")]
    [SerializeField] bool updateOnlyOnChange = true;
    [SerializeField] bool useTashkeel = false;
    [SerializeField] bool useHinduNumbers = false;

    private TextMeshProUGUI textMeshPro;
    private RectTransform rectTransform;

    private string neededText;
    private string prevCorrectText = "";
    private bool previousFrameNeededEdit = false;
    private float previousWorldRectWH = 0f;
    private bool initialized = false;

    [SerializeField] bool editTextHere = false;
    [SerializeField] string customText = "";

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        textMeshPro = GetComponent<TextMeshProUGUI>();
        neededText = textMeshPro.text;
        initialized = true;
    }

    void Start()
    {
        if (textAlreadySet)
        {
            rectTransform.hasChanged = false;
            prevCorrectText = textMeshPro.FixArabicTMProUGUILines(useTashkeel, useHinduNumbers, neededText);
            textMeshPro.text = prevCorrectText;
        }
        else if (editTextHere)
        {
            UpdateText(customText);
        }
    }

    void Update()
    {
        if (!updateInRealTime) return;

        float currentRectArea = rectTransform.rect.height + rectTransform.rect.width;

        if (previousFrameNeededEdit || !updateOnlyOnChange)
        {
            string editedText = textMeshPro.FixArabicTMProUGUILines(useTashkeel, useHinduNumbers, neededText);
            textMeshPro.text = editedText;
            previousFrameNeededEdit = false;
            textMeshPro.havePropertiesChanged = false;
        }
        else if (updateOnlyOnChange &&
                 (textMeshPro.havePropertiesChanged || currentRectArea != previousWorldRectWH))
        {
            previousFrameNeededEdit = true;
        }

        previousWorldRectWH = currentRectArea;
    }

    // Use this method to manually update the Arabic text
    public void UpdateText(string text)
    {
        if (!initialized)
            Awake();

        neededText = text;
        textMeshPro.text = textMeshPro.FixArabicTMProUGUILines(useTashkeel, useHinduNumbers, text);
    }
}
