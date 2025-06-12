using UnityEngine;
using TMPro;
using ArabicSupport;

[RequireComponent(typeof(TMP_InputField))]
public class InputArabicFixTMP : MonoBehaviour
{
    private TMP_InputField inputField;
    private TextMeshProUGUI fakeDisplay;

    public bool arabic = true;

    void Start()
    {
        inputField = GetComponent<TMP_InputField>();

        // Hide the original text by setting it transparent
        inputField.textComponent.color = new Color(0, 0, 0, 0);

        // Create a fake display text on top of the input field
        fakeDisplay = Instantiate((TextMeshProUGUI)inputField.textComponent,
                                  inputField.textComponent.transform.position,
                                  inputField.textComponent.transform.rotation,
                                  inputField.transform);

        // Match layout and style
        fakeDisplay.rectTransform.anchorMin = inputField.textComponent.rectTransform.anchorMin;
        fakeDisplay.rectTransform.anchorMax = inputField.textComponent.rectTransform.anchorMax;
        fakeDisplay.rectTransform.anchoredPosition = inputField.textComponent.rectTransform.anchoredPosition;
        fakeDisplay.rectTransform.sizeDelta = inputField.textComponent.rectTransform.sizeDelta;
        fakeDisplay.alignment = inputField.textComponent.alignment;
        fakeDisplay.fontSize = inputField.textComponent.fontSize;
        fakeDisplay.color = Color.black;

        // Listen for input changes
        inputField.onValueChanged.AddListener(delegate { FixNewText(); });
    }

    public string text
    {
        get { return inputField.text; }
    }

    void FixNewText()
    {
        if (arabic)
        {
            fakeDisplay.text = ArabicFixer.Fix(inputField.text);
        }
        else
        {
            fakeDisplay.text = inputField.text;
        }
    }

}
