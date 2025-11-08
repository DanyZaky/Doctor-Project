using UnityEngine;
using UnityEngine.UI;

public class LocalizedImage : MonoBehaviour
{
    public Image imageComponent;
    [SerializeField] Sprite englishSprite;
    [SerializeField] Sprite frenchSprite;

    public bool swapImage;
    [SerializeField] GameObject englishImage;
    [SerializeField] GameObject frenchImage;

    void Start()
    {
        UpdateSprite();
    }

    void OnEnable()
    {
        GameManager.OnLanguageChanged += UpdateSprite;
        UpdateSprite();
    }

    void OnDisable()
    {
        GameManager.OnLanguageChanged -= UpdateSprite;
    }

    public void UpdateSprite()
    {
        if (GameManager.Instance == null)
            return;

        if (swapImage)
        {
            if (GameManager.Instance.currentLanguage == SystemLanguage.French)
            {
                frenchImage.SetActive(true);
                englishImage.SetActive(false);
            }
            else
            {
                englishImage.SetActive(true);
                frenchImage.SetActive(false);
            }

            return;
        }

        Sprite localizedSprite = null;

        if (englishSprite != null || frenchSprite != null)
        {
            if (GameManager.Instance.currentLanguage == SystemLanguage.French && frenchSprite != null)
            {
                localizedSprite = frenchSprite;
            }
            else if (englishSprite != null)
            {
                localizedSprite = englishSprite;
            }
        }
        if (localizedSprite != null)
        {
            imageComponent.sprite = localizedSprite;
        }
    }
}