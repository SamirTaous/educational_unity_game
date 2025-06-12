using TMPro;
using UnityEngine;
using System;

public static class TMPArabicExtensions
{
    public static string FixArabicTMProUGUILines(this TextMeshProUGUI textMesh, bool useTashkeel, bool hinduNumbers, string text)
    {
        textMesh.text = text;
        Canvas.ForceUpdateCanvases();
        TMP_TextInfo newTextInfo = textMesh.GetTextInfo(text);
        string reversedText = "";
        string tempLine;

        for (int i = 0; i < newTextInfo.lineCount; i++)
        {
            int startIndex = newTextInfo.lineInfo[i].firstCharacterIndex;
            tempLine = text.Substring(startIndex, newTextInfo.lineInfo[i].characterCount);
            reversedText += ArabicSupport.ArabicFixer.Fix(tempLine, useTashkeel, hinduNumbers).Trim('\n');
            reversedText += Environment.NewLine;
        }

        return reversedText;
    }
}
