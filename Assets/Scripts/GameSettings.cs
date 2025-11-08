using Coffee.UIExtensions;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSettings : MonoBehaviour
{
    [Header("Player Selection")]
    [SerializeField] GameObject player1Glow;
    [SerializeField] GameObject player2Glow;
    [SerializeField] UIShiny player1Shine;
    [SerializeField] UIShiny player2Shine;

    [Header("Language Selection")]
    [SerializeField] Button languageLeftButton;
    [SerializeField] Button languageRightButton;
    [SerializeField] TextMeshProUGUI languageText;
    [SerializeField] Image languageFlagImage;
    [SerializeField] Sprite englishFlag;
    [SerializeField] Sprite frenchFlag;

    [Header("Theme Selection")]
    [SerializeField] Button themeUpButton;
    [SerializeField] Button themeDownButton;

    [SerializeField] Image[] themeDisplayImages = new Image[3];
    [SerializeField] UIShiny[] themeDisplayShines = new UIShiny[3];
    [SerializeField] TextMeshProUGUI[] themeDisplayTexts = new TextMeshProUGUI[3];

    [SerializeField] Sprite normalButtonSprite;
    [SerializeField] Sprite highlightedButtonSprite;

    List<GameManager.GameTheme> allThemes = new List<GameManager.GameTheme>
    {
        GameManager.GameTheme.Organs,
        GameManager.GameTheme.BodyParts,
        GameManager.GameTheme.Skeleton,
        GameManager.GameTheme.Muscles,
        GameManager.GameTheme.Senses,
        GameManager.GameTheme.System
    };

    string[] themeNamesEnglish = { "ORGANS", "BODY PARTS", "SKELETON", "MUSCLES", "SENSES", "SYSTEM" };
    string[] themeNamesFrench = { "ORGANES", "PARTIES DU CORPS", "SQUELETTE", "MUSCLES", "SENS", "SYSTÈME" };

    int currentThemeIndex = 0;

    [Header("Timer Selection")]
    [SerializeField] Button timerDecreaseButton;
    [SerializeField] Button timerIncreaseButton;
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] float timerStep = 30f;
    [SerializeField] float minTimer = 30f;

    [Header("LoadingScreen")]
    [SerializeField] GameObject loadingScreen;
    [SerializeField] TextMeshProUGUI countdownText1, countdownText2;
    [SerializeField] CanvasGroup canvasGroup;

    [Header("Button Cooldown Settings")]
    [SerializeField] float buttonCooldownTime = 2f;
    [SerializeField] float wobbleIntensity = 3f;
    [SerializeField] float wobbleDuration = 0.4f;
    [SerializeField] float clickScale = 1.1f;

    bool isThemeUpOnCooldown = false;
    bool isThemeDownOnCooldown = false;
    bool isTimerDecreaseOnCooldown = false;
    bool isTimerIncreaseOnCooldown = false;
    bool isLanguageToggleOnCooldown = false;


    Tween themeUpWobbleTween;
    Tween themeDownWobbleTween;
    Tween timerDecreaseWobbleTween;
    Tween timerIncreaseWobbleTween;
    Tween languageLeftWobbleTween;
    Tween languageRightWobbleTween;

    Dictionary<Button, Quaternion> originalRotations = new Dictionary<Button, Quaternion>();


    IEnumerator Start()
    {

        yield return null;

        InitializeUI();
        SetupButtonListeners();

        originalRotations[themeUpButton] = themeUpButton.transform.rotation;
        originalRotations[themeDownButton] = themeDownButton.transform.rotation;
        originalRotations[timerDecreaseButton] = timerDecreaseButton.transform.rotation;
        originalRotations[timerIncreaseButton] = timerIncreaseButton.transform.rotation;
        originalRotations[languageLeftButton] = languageLeftButton.transform.rotation;
        originalRotations[languageRightButton] = languageRightButton.transform.rotation;

    }

    void InitializeUI()
    {
        UpdatePlayerSelection();
        UpdateLanguageDisplay();
        SetupThemeNavigation();
        UpdateTimerDisplay();
    }

    void SetupButtonListeners()
    {
        themeUpButton.onClick.AddListener(ThemeUp);
        themeDownButton.onClick.AddListener(ThemeDown);

        timerDecreaseButton.onClick.AddListener(DecreaseTimer);
        timerIncreaseButton.onClick.AddListener(IncreaseTimer);

        if (languageLeftButton != null)
            languageLeftButton.onClick.AddListener(ToggleLanguage);
        if (languageRightButton != null)
            languageRightButton.onClick.AddListener(ToggleLanguage);
    }

    void SetupThemeNavigation()
    {
        currentThemeIndex = allThemes.IndexOf(GameManager.Instance.selectedTheme);

        if (currentThemeIndex == -1)
        {
            currentThemeIndex = 0;
            GameManager.Instance.SetTheme(allThemes[0]);
        }

        UpdateThemeDisplay();
    }

    void ThemeUp()
    {
        if (isThemeUpOnCooldown) return;

        StartCoroutine(ButtonCooldownCoroutine(() => isThemeUpOnCooldown = true, () => isThemeUpOnCooldown = false));
        StartButtonWobble(themeUpButton, ref themeUpWobbleTween);

        currentThemeIndex = (currentThemeIndex - 1 + allThemes.Count) % allThemes.Count;

        GameManager.Instance.SetTheme(allThemes[currentThemeIndex]);
        UpdateThemeDisplay();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick(ButtonClickType.Medium);
        }
    }

    void ThemeDown()
    {
        if (isThemeDownOnCooldown) return;

        StartCoroutine(ButtonCooldownCoroutine(() => isThemeDownOnCooldown = true, () => isThemeDownOnCooldown = false));
        StartButtonWobble(themeDownButton, ref themeDownWobbleTween);

        currentThemeIndex = (currentThemeIndex + 1) % allThemes.Count;

        GameManager.Instance.SetTheme(allThemes[currentThemeIndex]);
        UpdateThemeDisplay();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick(ButtonClickType.Medium);
        }
    }

    string[] GetThemeNames()
    {
        return GameManager.Instance.currentLanguage == SystemLanguage.English
            ? themeNamesEnglish
            : themeNamesFrench;
    }

    void UpdateThemeDisplay()
    {
        string[] currentThemeNames = GetThemeNames();

        themeUpButton.interactable = !isThemeUpOnCooldown;
        themeDownButton.interactable = !isThemeDownOnCooldown;

        for (int i = 0; i < 3; i++)
        {

            int themeIndex;
            if (i == 0)
            {
                themeIndex = (currentThemeIndex - 1 + allThemes.Count) % allThemes.Count;
            }
            else if (i == 1)
            {
                themeIndex = currentThemeIndex;
            }
            else
            {
                themeIndex = (currentThemeIndex + 1) % allThemes.Count;
            }

            themeDisplayTexts[i].text = currentThemeNames[themeIndex];

            bool isSelected = (i == 1);

            themeDisplayImages[i].sprite = isSelected ? highlightedButtonSprite : normalButtonSprite;
            themeDisplayShines[i].enabled = isSelected;

            if (isSelected)
            {
                themeDisplayImages[i].SetNativeSize();
            }
        }
    }

    public void SelectPlayer(int playerCount)
    {
        GameManager.Instance.SetPlayers(playerCount);
        UpdatePlayerSelection();
    }

    void UpdatePlayerSelection()
    {
        bool isPlayer1Selected = GameManager.Instance.selectedPlayers == 1;

        player1Glow.SetActive(isPlayer1Selected);
        player2Glow.SetActive(!isPlayer1Selected);

        player1Shine.enabled = isPlayer1Selected;
        player2Shine.enabled = !isPlayer1Selected;
    }

    public void ToggleLanguage()
    {
        if (isLanguageToggleOnCooldown) return;

        StartCoroutine(ButtonCooldownCoroutine(() => isLanguageToggleOnCooldown = true, () => isLanguageToggleOnCooldown = false));
        StartButtonWobble(languageLeftButton, ref languageLeftWobbleTween);
        StartButtonWobble(languageRightButton, ref languageRightWobbleTween);

        SystemLanguage newLanguage = GameManager.Instance.currentLanguage == SystemLanguage.English
            ? SystemLanguage.French
            : SystemLanguage.English;

        GameManager.Instance.SetLanguage(newLanguage);
        UpdateLanguageDisplay();
        UpdateThemeDisplay();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick(ButtonClickType.Medium);
        }
    }

    void UpdateLanguageDisplay()
    {
        bool isEnglish = GameManager.Instance.currentLanguage == SystemLanguage.English;

        languageFlagImage.sprite = isEnglish ? englishFlag : frenchFlag;
        languageText.text = isEnglish ? "ENGLISH" : "FRANÇAIS";

        // Update language button interactability
        if (languageLeftButton != null)
            languageLeftButton.interactable = !isLanguageToggleOnCooldown;
        if (languageRightButton != null)
            languageRightButton.interactable = !isLanguageToggleOnCooldown;
    }

    void DecreaseTimer()
    {
        if (isTimerDecreaseOnCooldown) return;

        float currentTimer = GameManager.Instance.gameplayTimer;
        if (currentTimer <= minTimer) return; // Don't allow decrease if already at minimum

        StartCoroutine(ButtonCooldownCoroutine(() => isTimerDecreaseOnCooldown = true, () => isTimerDecreaseOnCooldown = false));
        StartButtonWobble(timerDecreaseButton, ref timerDecreaseWobbleTween);

        float newTimer = Mathf.Max(minTimer, currentTimer - timerStep);
        GameManager.Instance.SetGameplayTimer(newTimer);
        UpdateTimerDisplay();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick(ButtonClickType.Medium);
        }
    }

    void IncreaseTimer()
    {
        if (isTimerIncreaseOnCooldown) return;

        StartCoroutine(ButtonCooldownCoroutine(() => isTimerIncreaseOnCooldown = true, () => isTimerIncreaseOnCooldown = false));
        StartButtonWobble(timerIncreaseButton, ref timerIncreaseWobbleTween);

        float currentTimer = GameManager.Instance.gameplayTimer;
        float newTimer = currentTimer + timerStep;
        GameManager.Instance.SetGameplayTimer(newTimer);
        UpdateTimerDisplay();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick(ButtonClickType.Medium);
        }
    }

    void UpdateTimerDisplay()
    {
        float timer = GameManager.Instance.gameplayTimer;
        int minutes = Mathf.FloorToInt(timer / 60);
        int seconds = Mathf.FloorToInt(timer % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        timerDecreaseButton.interactable = timer > minTimer && !isTimerDecreaseOnCooldown;
        timerIncreaseButton.interactable = !isTimerIncreaseOnCooldown;
    }

    IEnumerator ButtonCooldownCoroutine(System.Action setCooldown, System.Action removeCooldown)
    {
        setCooldown();
        yield return new WaitForSeconds(buttonCooldownTime);
        removeCooldown();

        StopAllWobbleAnimations();

        UpdateTimerDisplay();
        UpdateThemeDisplay();
        UpdateLanguageDisplay();
    }

    void StartButtonWobble(Button button, ref Tween wobbleTween)
    {
        if (button == null) return;

        wobbleTween?.Kill();

        Quaternion originalRotation = originalRotations.ContainsKey(button)
            ? originalRotations[button]
            : button.transform.rotation;

        wobbleTween = button.transform.DORotateQuaternion(
            originalRotation * Quaternion.Euler(0, 0, wobbleIntensity),
            wobbleDuration
        )
        .SetEase(Ease.InOutSine).SetDelay(0.2f)
        .SetLoops(-1, LoopType.Yoyo);

        button.transform.DOScale(Vector2.one * clickScale, 0.3f).SetEase(Ease.OutBack);
    }


    void StopButtonWobble(Button button, ref Tween wobbleTween)
    {
        if (button == null) return;

        wobbleTween?.Kill();
        wobbleTween = null;

        if (originalRotations.ContainsKey(button))
            button.transform.rotation = originalRotations[button];

        button.transform.DOScale(Vector2.one, 0.3f).SetEase(Ease.InBack);
    }

    void StopAllWobbleAnimations()
    {
        StopButtonWobble(themeUpButton, ref themeUpWobbleTween);
        StopButtonWobble(themeDownButton, ref themeDownWobbleTween);
        StopButtonWobble(timerDecreaseButton, ref timerDecreaseWobbleTween);
        StopButtonWobble(timerIncreaseButton, ref timerIncreaseWobbleTween);
        StopButtonWobble(languageLeftButton, ref languageLeftWobbleTween);
        StopButtonWobble(languageRightButton, ref languageRightWobbleTween);
    }

    public void PlayGame()
    {
        loadingScreen.SetActive(true);
        StartCoroutine(CountdownRoutine());
    }

    IEnumerator CountdownRoutine()
    {
        int current = 3;
        countdownText1.text = current.ToString();
        countdownText2.text = current.ToString();

        for (int i = 0; i < 2; i++)
        {
            yield return new WaitForSeconds(1);
            current--;
            countdownText1.text = current.ToString();
            countdownText2.text = current.ToString();
            AnimateText();
        }

        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);
        SceneManager.LoadSceneAsync(1);
    }

    void AnimateText()
    {
        countdownText1.transform.DOJump(countdownText1.transform.position, 1f, 1, 0.2f).SetEase(Ease.OutQuad);
        countdownText2.transform.DOJump(countdownText2.transform.position, 1f, 1, 0.2f).SetEase(Ease.OutQuad);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    private void OnDestroy()
    {
        StopAllWobbleAnimations();
    }

    private void OnDisable()
    {
        StopAllWobbleAnimations();
    }
}