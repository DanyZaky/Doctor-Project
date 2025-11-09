using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonEnhancer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IRaycastClickHandler, IRaycastHoverHandler
{
    [Header("Input Mode")]
    [SerializeField] bool useRaycastInput = false;
    [Tooltip("When true, uses raycast input instead of UI button events")]

    [Header("Animation Settings")]
    [SerializeField] float hoverScale = 1.1f;
    [SerializeField] float clickScale = 0.95f;
    [SerializeField] float animationDuration = 0.1f;
    [SerializeField] Ease hoverEase = Ease.OutBack;
    [SerializeField] Ease clickEase = Ease.OutBounce;
    [SerializeField] ButtonClickType clickType = ButtonClickType.High;
    [SerializeField] Animator animator;

    public bool playSound = true;

    [Header("Audio Settings")]
    [SerializeField] bool playHoverSound = true;
    [SerializeField] bool playClickSound = true;

    [Header("Visual Effects")]
    [SerializeField] bool enableGreyOutEffect = true;
    [SerializeField] Color greyOutColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] float greyOutDuration = 0.1f;

    Vector3 originalScale;
    Color originalColor;
    Button button;
    Image buttonImage;
    Tween currentScaleTween;
    Tween currentColorTween;
    RaycastClickable raycastClickable;

    void Awake()
    {
        originalScale = transform.localScale;
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        raycastClickable = GetComponent<RaycastClickable>();

        // Store original color
        if (buttonImage != null)
        {
            originalColor = buttonImage.color;
        }
    }

    // UI Button Events (original functionality)
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (useRaycastInput) return; // Skip if using raycast input

        if (button != null && !button.interactable)
            return;

        PlayHoverAnimation();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (useRaycastInput) return; // Skip if using raycast input

        if (button != null && !button.interactable)
            return;

        PlayExitAnimation();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (useRaycastInput) return; // Skip if using raycast input

        if (button != null && !button.interactable)
            return;

        PlayClickAnimation();
    }

    // Raycast Input Events
    public void OnRaycastClick()
    {
        if (!useRaycastInput) return; // Skip if not using raycast input

        // Check if button is interactable when using raycast input
        if (button != null && !button.interactable)
            return;

        PlayClickAnimation();
    }

    // Public methods for raycast hover (call these from your raycast system)
    public void OnRaycastHoverEnter()
    {
        if (!useRaycastInput) return;

        // Check if button is interactable when using raycast input
        if (button != null && !button.interactable)
            return;

        PlayHoverAnimation();
    }

    public void OnRaycastHoverExit()
    {
        if (!useRaycastInput) return;

        // Check if button is interactable when using raycast input
        if (button != null && !button.interactable)
            return;

        PlayExitAnimation();
    }

    // Animation Methods
    private void PlayHoverAnimation()
    {
        if (animator != null) animator.enabled = false;

        if (playHoverSound && AudioManager.Instance != null && playSound)
        {
            AudioManager.Instance.PlayButtonHover();
        }

        currentScaleTween?.Kill();
        currentColorTween?.Kill();

        currentScaleTween = transform.DOScale(originalScale * hoverScale, animationDuration)
                                    .SetEase(hoverEase)
                                    .SetUpdate(true);
    }

    private void PlayExitAnimation()
    {
        if (animator != null) animator.enabled = true;

        currentScaleTween?.Kill();
        currentColorTween?.Kill();

        currentScaleTween = transform.DOScale(originalScale, animationDuration)
                                    .SetEase(Ease.OutQuart)
                                    .SetUpdate(true);
    }

    private void PlayClickAnimation()
    {
        if (playClickSound && AudioManager.Instance != null && playSound)
        {
            AudioManager.Instance.PlayButtonClick(clickType);
        }

        currentScaleTween?.Kill();
        currentColorTween?.Kill();

        // Create click animation sequence
        Sequence clickSequence = DOTween.Sequence();

        // Scale animation
        clickSequence.Append(transform.DOScale(originalScale * clickScale, animationDuration * 0.5f)
                                     .SetEase(clickEase)
                                     .SetUpdate(true))
                     .Append(transform.DOScale(originalScale * hoverScale, animationDuration * 0.5f)
                                     .SetEase(Ease.OutBack)
                                     .SetUpdate(true));

        // Grey out effect
        if (enableGreyOutEffect && buttonImage != null)
        {
            Sequence colorSequence = DOTween.Sequence();
            colorSequence.Append(buttonImage.DOColor(greyOutColor, greyOutDuration)
                                           .SetEase(Ease.OutQuart)
                                           .SetUpdate(true))
                        .Append(buttonImage.DOColor(originalColor, greyOutDuration)
                                          .SetEase(Ease.OutQuart)
                                          .SetUpdate(true));

            currentColorTween = colorSequence;
        }

        currentScaleTween = clickSequence;
    }

    private void OnDisable()
    {
        currentScaleTween?.Kill();
        currentColorTween?.Kill();
        transform.localScale = originalScale;
    }

    private void OnDestroy()
    {
        currentScaleTween?.Kill();
        currentColorTween?.Kill();
    }
}