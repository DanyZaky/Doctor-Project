using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HowToPlay : MonoBehaviour
{
    [Header("Models")]
    [SerializeField] GameObject[] characterGameObjects;
    [SerializeField] GameObject[] howToAnims;
    [SerializeField] Button nextTutorialButton;
    [SerializeField] Button previousTutorialButton;
    int currentTutorailIndex = 0;

    [Header("Animation References")]
    [SerializeField] RectTransform imageRectTransform;
    [SerializeField] GameObject imageMask;
    [SerializeField] Image backgroundFading;
    [SerializeField] Animator animator;
    [SerializeField] Button cancelButton;

    [Header("Animation Settings")]
    [SerializeField] float scaleUpDuration = 0.5f;
    [SerializeField] float scaleDownDuration = 0.3f;
    [SerializeField] float targetScale = 2f;
    [SerializeField] Ease scaleUpEase = Ease.OutBack;
    [SerializeField] Ease scaleDownEase = Ease.InBack;

    [Header("Button Cooldown Settings")]
    [SerializeField] float buttonCooldownTime = 2f;
    [SerializeField] float wobbleIntensity = 3f;
    [SerializeField] float wobbleDuration = 0.4f;
    [SerializeField] float clickScale = 1.1f;

    Vector3 originalScale;
    Vector2 originalAnchoredPosition;

    bool isImageExpanded;
    bool isAnimating;

    bool isNextTutorialOnCooldown = false;
    bool isPreviousTutorialOnCooldown = false;

    Tween nextTutorialWobbleTween;
    Tween previousTutorialWobbleTween;

    Tween scaleTween;
    Tween positionTween;
    Tween fadeTween;
    Tween delayTween;

    void Awake()
    {
        if (imageRectTransform != null)
        {
            originalScale = imageRectTransform.localScale;
            originalAnchoredPosition = imageRectTransform.anchoredPosition;
        }
    }

    void Start()
    {
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(ResetImageToOriginal);
            cancelButton.gameObject.SetActive(false);
        }

        if (backgroundFading != null)
        {
            backgroundFading.gameObject.SetActive(false);
            backgroundFading.GetComponent<Button>().enabled = false;
        }

        SetupButtonListeners();
    }

    void SetupButtonListeners()
    {
        if (nextTutorialButton != null)
            nextTutorialButton.onClick.AddListener(NextTutorial);
        if (previousTutorialButton != null)
            previousTutorialButton.onClick.AddListener(PreviousTutorial);
    }

    void ShowModel(int index)
    {
        if (characterGameObjects == null || characterGameObjects.Length == 0)
            return;

        for (int i = 0; i < characterGameObjects.Length; i++)
        {
            if (characterGameObjects[i] != null)
                characterGameObjects[i].SetActive(i == index);
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick(ButtonClickType.Medium);

        ShowTutorial();
    }

    void ShowTutorial()
    {
        howToAnims[currentTutorailIndex].SetActive(false);
        delayTween?.Kill();

        delayTween = DOVirtual.DelayedCall(1, () =>
        {
            howToAnims[currentTutorailIndex].SetActive(true);
        });
    }

    public void NextTutorial()
    {
        if (characterGameObjects == null || characterGameObjects.Length == 0)
            return;

        if (isNextTutorialOnCooldown) return;

        StartCoroutine(ButtonCooldownCoroutine(() => isNextTutorialOnCooldown = true, () => isNextTutorialOnCooldown = false));
        StartButtonWobble(nextTutorialButton, ref nextTutorialWobbleTween);

        currentTutorailIndex = (currentTutorailIndex + 1) % characterGameObjects.Length;
        ShowModel(currentTutorailIndex);
    }

    public void PreviousTutorial()
    {
        if (characterGameObjects == null || characterGameObjects.Length == 0)
            return;

        if (isPreviousTutorialOnCooldown) return;

        StartCoroutine(ButtonCooldownCoroutine(() => isPreviousTutorialOnCooldown = true, () => isPreviousTutorialOnCooldown = false));
        StartButtonWobble(previousTutorialButton, ref previousTutorialWobbleTween);

        currentTutorailIndex = (currentTutorailIndex - 1 + characterGameObjects.Length) % characterGameObjects.Length;
        ShowModel(currentTutorailIndex);
    }

    IEnumerator ButtonCooldownCoroutine(System.Action setCooldown, System.Action removeCooldown)
    {
        setCooldown();
        UpdateTutorialButtonInteractability();
        yield return new WaitForSeconds(buttonCooldownTime);
        removeCooldown();

        // Stop wobble animations when cooldown ends
        StopAllWobbleAnimations();
        UpdateTutorialButtonInteractability();
    }

    void UpdateTutorialButtonInteractability()
    {
        if (nextTutorialButton != null)
            nextTutorialButton.interactable = !isNextTutorialOnCooldown;
        if (previousTutorialButton != null)
            previousTutorialButton.interactable = !isPreviousTutorialOnCooldown;
    }

    void StartButtonWobble(Button button, ref Tween wobbleTween)
    {
        if (button == null) return;

        wobbleTween?.Kill();

        wobbleTween = button.transform.DORotate(new Vector3(0, 0, wobbleIntensity), wobbleDuration)
            .SetEase(Ease.InOutSine).SetDelay(0.2f)
            .SetLoops(-1, LoopType.Yoyo);

        button.transform.DOScale(Vector2.one * clickScale, 0.3f).SetEase(Ease.OutBack);
    }

    void StopButtonWobble(Button button, ref Tween wobbleTween)
    {
        if (button == null) return;

        wobbleTween?.Kill();
        wobbleTween = null;
        button.transform.rotation = Quaternion.identity;

        button.transform.DOScale(Vector2.one, 0.3f).SetEase(Ease.InBack);
    }

    void StopAllWobbleAnimations()
    {
        StopButtonWobble(nextTutorialButton, ref nextTutorialWobbleTween);
        StopButtonWobble(previousTutorialButton, ref previousTutorialWobbleTween);
    }

    public void ScaleUpAndCenterImage()
    {
        if (imageRectTransform == null || isImageExpanded || isAnimating)
            return;

        isImageExpanded = true;
        isAnimating = true;

        if (cancelButton != null) cancelButton.gameObject.SetActive(true);
        if (animator != null) animator.enabled = false;
        if (imageMask != null) imageMask.SetActive(false);

        scaleTween?.Kill();
        positionTween?.Kill();
        fadeTween?.Kill();

        scaleTween = imageRectTransform.DOScale(originalScale * targetScale, scaleUpDuration)
                                      .SetEase(scaleUpEase)
                                      .SetUpdate(true);

        positionTween = imageRectTransform.DOAnchorPos(Vector2.zero, scaleUpDuration)
                                         .SetEase(scaleUpEase)
                                         .SetUpdate(true)
                                         .OnComplete(() => isAnimating = false);

        if (backgroundFading != null)
        {
            backgroundFading.gameObject.SetActive(true);
            backgroundFading.color = new Color(backgroundFading.color.r, backgroundFading.color.g, backgroundFading.color.b, 0);
            fadeTween = backgroundFading.DOFade(0.96f, 0.3f)
                                       .SetUpdate(true)
                                       .OnComplete(() => backgroundFading.GetComponent<Button>().enabled = true);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick(ButtonClickType.Medium);
        }

        ShowTutorial();
    }

    public void ResetImageToOriginal()
    {
        if (imageRectTransform == null || !isImageExpanded || isAnimating)
            return;

        isImageExpanded = false;
        isAnimating = true;

        if (cancelButton != null) cancelButton.gameObject.SetActive(false);
        if (animator != null) animator.enabled = true;
        if (imageMask != null) imageMask.SetActive(true);

        scaleTween?.Kill();
        positionTween?.Kill();
        fadeTween?.Kill();
        DOTween.KillAll();

        scaleTween = imageRectTransform.DOScale(originalScale, scaleDownDuration)
                                      .SetEase(scaleDownEase)
                                      .SetUpdate(true);

        positionTween = imageRectTransform.DOAnchorPos(originalAnchoredPosition, scaleDownDuration)
                                         .SetEase(scaleDownEase)
                                         .SetUpdate(true)
                                         .OnComplete(() => isAnimating = false);

        if (backgroundFading != null)
        {
            backgroundFading.GetComponent<Button>().enabled = false;
            fadeTween = backgroundFading.DOFade(0, 0.5f)
                                       .SetUpdate(true)
                                       .OnComplete(() => backgroundFading.gameObject.SetActive(false));
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick(ButtonClickType.Medium);
        }

        foreach (var howto in howToAnims)
        {
            howto.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        scaleTween?.Kill();
        positionTween?.Kill();
        fadeTween?.Kill();
        delayTween?.Kill();
        StopAllWobbleAnimations();
    }

    private void OnDisable()
    {
        if (imageRectTransform != null)
        {
            scaleTween?.Kill();
            positionTween?.Kill();
            fadeTween?.Kill();
            delayTween?.Kill();
            imageRectTransform.localScale = originalScale;
            imageRectTransform.anchoredPosition = originalAnchoredPosition;
        }

        isImageExpanded = false;
        isAnimating = false;

        if (cancelButton != null)
        {
            cancelButton.gameObject.SetActive(false);
        }

        if (backgroundFading != null)
        {
            backgroundFading.gameObject.SetActive(false);
            backgroundFading.GetComponent<Button>().enabled = false;
        }

        StopAllWobbleAnimations();
    }
}