using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartClick : MonoBehaviour, IOption, IRaycastClickHandler
{
    [SerializeField] string partName;
    [SerializeField] Image image;
    [SerializeField] PlayerGameplay playerGameplay;
    [SerializeField] Animator anim;
    public Image otherPartImage;

    public bool hasOtherSideBone;
    public Image otherSideImage;
    bool isInteractable = true;

    Collider2D partCollider;
    private RaycastClickable raycastClickable;

    void OnEnable()
    {
        if (hasOtherSideBone)
        {
            otherSideImage.gameObject.SetActive(true);
            otherSideImage.color = new Color(1, 1, 1, 0);
        }

        isInteractable = true;

        if (partCollider != null)
        {
            partCollider.enabled = true;
        }
    }

    void Start()
    {
        if (string.IsNullOrEmpty(partName))
        {
            partName = gameObject.name;
        }

        gameObject.layer = LayerMask.NameToLayer("RayLayer");

        partCollider = GetComponent<Collider2D>();

        // Ensure Image is raycastable
        if (image != null)
        {
            image.raycastTarget = true;
        }

        // Add RaycastClickable component if not present
        raycastClickable = GetComponent<RaycastClickable>();
        if (raycastClickable == null)
        {
            raycastClickable = gameObject.AddComponent<RaycastClickable>();
            raycastClickable.isGameplayElement = true; // Use 0.4s delay for gameplay elements
        }
        if (raycastClickable.onClick == null)
            raycastClickable.onClick = new UnityEngine.Events.UnityEvent();
        raycastClickable.onClick.AddListener(OnRaycastClick);
    }

    // Called by UIRaycastInputManager via IRaycastClickHandler interface
    public void OnRaycastClick()
    {
        Debug.Log($"OnRaycastClick called on {partName}! isInteractable: {isInteractable}");

        if (!isInteractable)
        {
            Debug.Log($"Part {partName} is not interactable!");
            return;
        }

        if (playerGameplay != null)
        {
            Debug.Log($"Calling OnPartClicked for {partName}");
            bool isCorrect = playerGameplay.OnPartClicked(partName);

            if (isCorrect)
            {
                Debug.Log($"Correct answer for {partName}!");
                ShowCorrectFeedback();
                partCollider.enabled = false;
            }
            else
            {
                Debug.Log($"Wrong answer for {partName}");
            }
        }
        else
        {
            Debug.LogError($"PlayerGameplay is null for {partName}!");
        }
    }

    // Legacy method for backward compatibility - can be removed if not used elsewhere
    public void OnClicked()
    {
        OnRaycastClick();
    }

    void ShowCorrectFeedback()
    {
        if (image != null)
        {
            isInteractable = false;

            DOTween.Sequence()
                .Join(image.DOFade(1, 1))
                .AppendInterval(1)
                .Append(image.DOFade(0, 2))
                .AppendCallback(() =>
                {
                    if (hasOtherSideBone)
                    {
                        otherSideImage.gameObject.SetActive(false);
                    }

                    if (transform.parent.name == name)
                    {
                        transform.parent.gameObject.SetActive(false);
                    }

                    gameObject.SetActive(false);
                });

            if (anim)
            {
                anim.SetTrigger("Correct");
            }

            CreateFloatingScore();
        }
    }

    public void ActiveOtherPart()
    {
        if (hasOtherSideBone && otherSideImage != null)
        {
            otherSideImage.gameObject.SetActive(true);
            otherSideImage.color = new Color(1, 1, 1, 0);
        }

        if (otherPartImage != null)
        {
            otherPartImage.gameObject.SetActive(true);
            otherPartImage.color = new Color(1, 1, 1, 0);
        }
    }

    void CreateFloatingScore()
    {
        GameObject floatingScore = new GameObject("FloatingScore");
        TextMeshProUGUI floatingText = floatingScore.AddComponent<TextMeshProUGUI>();
        floatingScore.transform.SetParent(playerGameplay.transform.parent, false);

        Canvas canvas = playerGameplay.canvas;
        floatingScore.transform.SetParent(canvas.transform, false);

        RectTransform floatingRect = floatingScore.GetComponent<RectTransform>();
        floatingRect.sizeDelta = new Vector2(150, 60);

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, transform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            screenPoint,
            null,
            out Vector2 canvasPosition
        );

        floatingRect.anchoredPosition = canvasPosition;

        CopyTextProperties(floatingText, playerGameplay.scoreText);

        floatingText.text = "+5";
        floatingText.fontSize = playerGameplay.scoreText.fontSize * 1.2f;

        Vector3 targetPos = playerGameplay.GetScoreCardPosition();

        Sequence floatingSequence = DOTween.Sequence();

        floatingSequence.Append(floatingRect.DOAnchorPosY(floatingRect.anchoredPosition.y + 50f, 0.5f)
            .SetEase(Ease.OutQuart));

        floatingSequence.Join(floatingRect.DOScale(1.2f, 0.3f)
            .SetEase(Ease.OutBack));

        floatingSequence.AppendInterval(0.2f);
        floatingSequence.Append(floatingRect.DOMove(targetPos, 1.2f)
            .SetEase(Ease.InOutQuart));

        floatingSequence.Join(floatingRect.DOScale(0.8f, 1.2f)
            .SetEase(Ease.InQuart));

        floatingSequence.Join(floatingText.DOFade(0.7f, 0.8f)
            .SetDelay(0.4f));

        floatingSequence.OnComplete(() =>
        {
            playerGameplay.TriggerScoreCardBounce();
            playerGameplay.UpdateScoreDisplay();
            Destroy(floatingScore);
        });
    }

    void CopyTextProperties(TextMeshProUGUI target, TextMeshProUGUI source)
    {
        target.font = source.font;
        target.color = Color.green;
        target.fontSize = source.fontSize;
        target.fontStyle = source.fontStyle;
        target.alignment = source.alignment;
        target.fontMaterial = source.fontMaterial;
        target.enableAutoSizing = source.enableAutoSizing;
        target.fontSizeMin = source.fontSizeMin;
        target.fontSizeMax = source.fontSizeMax;
        target.characterSpacing = source.characterSpacing;
        target.wordSpacing = source.wordSpacing;
        target.lineSpacing = source.lineSpacing;
        target.paragraphSpacing = source.paragraphSpacing;
    }

    public void SetPartName(string name)
    {
        partName = name;
    }

    public string GetPartName()
    {
        return partName;
    }

    public void SetAlpha(float alpha)
    {
        if (image != null)
        {
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }

    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
    }

    public bool IsInteractable()
    {
        return isInteractable;
    }

    private void OnDisable()
    {
        if (hasOtherSideBone && otherSideImage != null)
        {
            otherSideImage.gameObject.SetActive(false);
        }

        if (partCollider != null)
        {
            partCollider.enabled = true;
        }
    }
}