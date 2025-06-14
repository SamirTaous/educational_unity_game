using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PDFQuestionGenerator : MonoBehaviour
{
    public Button uploadPDFButton;
    public Button generateButton;
    public TMP_InputField openQuestionInput;
    public TMP_InputField yesNoQuestionInput;
    public TMP_Text outputText;

    private string selectedPDFPath;

    void Start()
    {
        uploadPDFButton.onClick.AddListener(OnUploadPDFClicked);
        generateButton.onClick.AddListener(OnGenerateClicked);
        ApplyArabicFix(outputText);
    }

    void OnUploadPDFClicked()
    {
#if UNITY_EDITOR
        string path = EditorUtility.OpenFilePanel("اختر ملف PDF", "", "pdf");
        if (!string.IsNullOrEmpty(path))
        {
            selectedPDFPath = path;
            outputText.text = "تم اختيار الملف: " + Path.GetFileName(path);
        }
        else
        {
            outputText.text = "لم يتم اختيار أي ملف.";
        }
#else
        outputText.text = "اختيار الملف مدعوم فقط في المحرر.";
#endif
    }

    void OnGenerateClicked()
    {
        if (string.IsNullOrEmpty(selectedPDFPath))
        {
            outputText.text = "يرجى اختيار ملف PDF أولاً.";
            return;
        }

        if (!int.TryParse(openQuestionInput.text, out int openCount))
        {
            outputText.text = "أدخل رقمًا صحيحًا لعدد الأسئلة المفتوحة.";
            return;
        }

        if (!int.TryParse(yesNoQuestionInput.text, out int yesNoCount))
        {
            outputText.text = "أدخل رقمًا صحيحًا لعدد أسئلة نعم/لا.";
            return;
        }

        StartCoroutine(UploadAndGenerateQuestions(selectedPDFPath, openCount, yesNoCount));
    }

    IEnumerator UploadAndGenerateQuestions(string filePath, int openCount, int yesNoCount)
    {
        outputText.text = "جارٍ تحميل الملف ومعالجته...";

        byte[] fileData = File.ReadAllBytes(filePath);

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>
        {
            new MultipartFormFileSection("file", fileData, Path.GetFileName(filePath), "application/pdf"),
            new MultipartFormDataSection("num_open_questions", openCount.ToString()),
            new MultipartFormDataSection("num_yes_no_questions", yesNoCount.ToString())
        };

        UnityWebRequest www = UnityWebRequest.Post("http://localhost:5050/api/generate", formData);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            var data = SimpleJSON.JSON.Parse(www.downloadHandler.text);
            if (data["status"] == "success")
            {
                outputText.text = "شكرًا لك! تم توليد الأسئلة بنجاح وتمت إضافتها إلى قاعدة البيانات.";
                ApplyArabicFix(outputText);
            }
            else
            {
                outputText.text = "حدث خطأ أثناء توليد الأسئلة.";
            }
        }
        else
        {
            outputText.text = "فشل في رفع أو معالجة الملف:\n" + www.error + "\n" + www.downloadHandler.text;
            Debug.LogError("Upload error: " + www.downloadHandler.text);
        }
    }

    void ApplyArabicFix(TMP_Text textComponent)
    {
        if (textComponent != null && textComponent.GetComponent<FixArabicTMProUGUI>() == null)
        {
            textComponent.gameObject.AddComponent<FixArabicTMProUGUI>();
        }
    }
}
