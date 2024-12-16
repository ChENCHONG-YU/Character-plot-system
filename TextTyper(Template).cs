using System.Collections;
using UnityEngine;
using TMPro; // 如果你使用的是 TextMeshPro，使用這個命名空間

public class TextTyper : MonoBehaviour
{
    public TMP_Text uiText; // 替換為 TextMeshPro，若使用普通的Text則改為Text
    public float typingSpeed = 0.05f; // 每個字母顯示的間隔時間

    private string fullText;

    private void Start()
    {
        fullText = uiText.text; // 保存完整的文本內容
        uiText.text = ""; // 開始時清空文本

        StartCoroutine(TypeText());
    }

    private IEnumerator TypeText()
    {
        foreach (char letter in fullText.ToCharArray())
        {
            uiText.text += letter; // 每次增加一個字符
            yield return new WaitForSeconds(typingSpeed); // 等待指定的時間間隔
        }
    }
}
