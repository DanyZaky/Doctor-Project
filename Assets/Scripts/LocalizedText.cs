using UnityEngine;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    [SerializeField] string englishText;
    [SerializeField] string frenchText;

    public TextMeshProUGUI textMeshPro;

    void Start()
    {
        UpdateText();
    }

    void OnEnable()
    {
        GameManager.OnLanguageChanged += UpdateText;
        UpdateText();
    }

    public void UpdateText()
    {
        if (GameManager.Instance == null)
            return;

        string localizedText = "";

        if (!string.IsNullOrEmpty(englishText) || !string.IsNullOrEmpty(frenchText))
        {
            if (GameManager.Instance.currentLanguage == SystemLanguage.French && !string.IsNullOrEmpty(frenchText))
            {
                localizedText = frenchText;
            }
            else if (!string.IsNullOrEmpty(englishText))
            {
                localizedText = englishText;
            }
        }

        if (textMeshPro != null && !string.IsNullOrEmpty(localizedText))
        {
            textMeshPro.text = localizedText;
        }
    }

    public void SetDirectText(string english, string french)
    {
        englishText = english;
        frenchText = french;
        UpdateText();
    }

    void OnDisable()
    {
        GameManager.OnLanguageChanged -= UpdateText;
    }
}