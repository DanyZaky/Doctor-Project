using DG.Tweening;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonEnhancer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
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

    Vector3 originalScale;
    Button button;
    Tween currentScaleTween;
    Tween currentColorTween;

    void Awake()
    {
        originalScale = transform.localScale;
        button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable)
            return;

        if (animator != null) animator.enabled = false;

        if (playHoverSound && AudioManager.Instance != null)
        {
            if (!playSound) return;

            AudioManager.Instance.PlayButtonHover();
        }

        currentScaleTween?.Kill();
        currentColorTween?.Kill();

        currentScaleTween = transform.DOScale(originalScale * hoverScale, animationDuration)
                                    .SetEase(hoverEase)
                                    .SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (button != null && !button.interactable)
            return;

        if (animator != null) animator.enabled = true;

        currentScaleTween?.Kill();
        currentColorTween?.Kill();

        currentScaleTween = transform.DOScale(originalScale, animationDuration)
                                    .SetEase(Ease.OutQuart)
                                    .SetUpdate(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (button != null && !button.interactable)
            return;

        if (playClickSound && AudioManager.Instance != null)
        {
            if (!playSound) return;
            AudioManager.Instance.PlayButtonClick(clickType);
        }

        currentScaleTween?.Kill();

        Sequence clickSequence = DOTween.Sequence();
        clickSequence.Append(transform.DOScale(originalScale * clickScale, animationDuration * 0.5f)
                                     .SetEase(clickEase)
                                     .SetUpdate(true))
                     .Append(transform.DOScale(originalScale * hoverScale, animationDuration * 0.5f)
                                     .SetEase(Ease.OutBack)
                                     .SetUpdate(true));

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