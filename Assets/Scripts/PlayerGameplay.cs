using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerGameplay : MonoBehaviour, IOption
{
    [Header("UI Elements")]
    public Canvas canvas;
    [SerializeField] RectTransform modelContainer;
    [SerializeField] RectTransform modelRect;
    [SerializeField] RectTransform questionBox;
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] TextMeshProUGUI questionText;
    [SerializeField] Button upArrowButton;
    [SerializeField] Button downArrowButton;
    [SerializeField] Button rotateButton;
    [SerializeField] Button zoomInButton;
    [SerializeField] Button zoomOutButton;
    [SerializeField] RectTransform timerAnim;
    [SerializeField] Transform pilow;
    public TextMeshProUGUI scoreText;

    [Header("Front Models")]
    [SerializeField] Image organModelFront;
    [SerializeField] Image bodypartModelFront;
    [SerializeField] Image skeletonModelFront;
    [SerializeField] Image musclesModelFront;
    [SerializeField] Image sensesModelFront;
    [SerializeField] Image systemModelFront;

    [Header("Back Models")]
    [SerializeField] Image organModelBack;
    [SerializeField] Image bodypartModelBack;
    [SerializeField] Image skeletonModelBack;
    [SerializeField] Image musclesModelBack;
    [SerializeField] Image sensesModelBack;
    [SerializeField] Image systemModelBack;

    [Header("Zoom & Scroll Settings")]
    [SerializeField] float zoomScale = 2f;
    [SerializeField] float scrollStep = 100f;
    [SerializeField] float minScrollOffset = -200f;
    [SerializeField] float maxScrollOffset = 200f;

    [Header("Question Display")]
    [SerializeField] float questionDisplayTime = 2f;

    [Header("Model Entry Animation")]
    [SerializeField] float entryAnimationDuration = 1f;
    [SerializeField] float entryDelayBetweenPlayers = 0.5f;

    [Header("Button Cooldown")]
    [SerializeField] float buttonCooldown = 0.2f;

    bool isPlayer1;
    bool isZoomed;
    bool isFront = true;
    bool isClickBlocked;
    int score;
    int consecutiveCorrect = 0;
    int playerIndex;

    bool isQuestionActive;
    bool canPlay;
    Coroutine questionAnimationCoroutine;

    GameplayManager gameManager;
    Question currentQuestion;

    Image currentFrontModel;
    Image currentBackModel;

    float lastInteractionTime;
    Coroutine idleCoroutine;

    //InputAction scrollAction;
    Vector2 initialPositionModel;
    Vector2 initialPositionModelContainer;
    Vector2 initialPosUpArrow, initialPosDownArrow;
    Vector2 initialPosRotateButton;
    Vector2 initialPosQuestionBox;
    Vector2 initialPosZoomIn, initialPosZoomOut;

    static int playersInitialized = 0;
    static bool isPlayingEntryAnimation = false;
    static List<PlayerGameplay> playerInstances = new List<PlayerGameplay>();

    Coroutine idleTimerCoroutine;
    bool isButtonCooldownActive;

    void Awake()
    {
        initialPositionModel = modelRect.anchoredPosition;
        initialPositionModelContainer = modelContainer.anchoredPosition;
        initialPosQuestionBox = questionBox.anchoredPosition;
        initialPosUpArrow = upArrowButton.gameObject.GetComponent<RectTransform>().anchoredPosition;
        initialPosDownArrow = downArrowButton.gameObject.GetComponent<RectTransform>().anchoredPosition;
        initialPosRotateButton = rotateButton.gameObject.GetComponent<RectTransform>().anchoredPosition;

        SetupButtons();

        DOVirtual.DelayedCall(3, () => canPlay = true);
    }

    public void Initialize(bool _isPlayer1, GameplayManager _gameManager)
    {
        isPlayer1 = _isPlayer1;
        gameManager = _gameManager;

        playerIndex = _isPlayer1 ? 0 : 1;

        if (!playerInstances.Contains(this))
        {
            playerInstances.Add(this);
        }

        LoadTheme();

        if (QuestionManager.Instance != null)
        {
            QuestionManager.Instance.InitializeForPlayer(playerIndex, GameManager.Instance.selectedTheme);
        }

        StartEntryAnimation();

        UpdateScoreDisplay();
    }

    void StartEntryAnimation()
    {
        playersInitialized++;

        int totalPlayers = GameManager.Instance.selectedPlayers;

        if (totalPlayers == 1)
        {
            StartCoroutine(PlayEntryAnimation(0f));
        }
        else if (totalPlayers == 2)
        {
            if (playersInitialized == 2 && !isPlayingEntryAnimation)
            {
                isPlayingEntryAnimation = true;
                StartCoroutine(PlayTwoPlayerEntryAnimation());
            }
        }
    }

    IEnumerator PlayTwoPlayerEntryAnimation()
    {
        PlayerGameplay player1 = null;
        PlayerGameplay player2 = null;

        foreach (var player in playerInstances)
        {
            if (player.isPlayer1)
                player1 = player;
            else
                player2 = player;
        }

        if (player1 != null)
        {
            yield return StartCoroutine(player1.PlayEntryAnimation(0f));
        }

        yield return new WaitForSeconds(entryDelayBetweenPlayers);

        if (player2 != null)
        {
            yield return StartCoroutine(player2.PlayEntryAnimation(0f));
        }

        yield return new WaitForSeconds(0.2f);

        if (player1 != null && player1.currentFrontModel != null)
        {
            player1.currentFrontModel.rectTransform.DOShakePosition(1f, new Vector3(10f, 10f, 0f), 20, 90f, false, true);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayGameStart();
            }
        }

        if (player2 != null && player2.currentFrontModel != null)
        {
            player2.currentFrontModel.rectTransform.DOShakePosition(1f, new Vector3(10f, 10f, 0f), 20, 90f, false, true);
        }

        if (player1 != null) player1.NewQuestion();
        if (player2 != null) player2.NewQuestion();

        isPlayingEntryAnimation = false;
    }

    IEnumerator PlayEntryAnimation(float delay)
    {
        if (currentFrontModel == null)
            yield break;

        if (delay > 0)
            yield return new WaitForSeconds(delay);

        currentFrontModel.gameObject.SetActive(true);

        Vector2 originalPosition = currentFrontModel.rectTransform.anchoredPosition;

        float screenHeight = Screen.height;
        float startY = originalPosition.y - (screenHeight / canvas.scaleFactor);
        currentFrontModel.rectTransform.anchoredPosition = new Vector2(originalPosition.x, startY);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayWhooshSound();
        }

        yield return currentFrontModel.rectTransform.DOAnchorPosY(originalPosition.y, entryAnimationDuration)
            .SetEase(Ease.OutBack)
            .WaitForCompletion();

        if (GameManager.Instance.selectedPlayers == 1)
        {
            yield return new WaitForSeconds(0.2f);
            currentFrontModel.rectTransform.DOShakePosition(1f, new Vector3(10f, 10f, 0f), 20, 90f, false, true);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayGameStart();
            }
            NewQuestion();
        }
    }

    void SetupButtons()
    {
        upArrowButton.GetComponent<RaycastClickable>().onClick.AddListener(() => { if (!isButtonCooldownActive) StartCoroutine(HandleButtonClick(ScrollUp)); });
        downArrowButton.GetComponent<RaycastClickable>().onClick.AddListener(() => { if (!isButtonCooldownActive) StartCoroutine(HandleButtonClick(ScrollDown)); });
        rotateButton.GetComponent<RaycastClickable>().onClick.AddListener(() => { if (!isButtonCooldownActive) StartCoroutine(HandleButtonClick(RotateModel)); });

        if (zoomInButton != null)
            zoomInButton.GetComponent<RaycastClickable>().onClick.AddListener(() => { if (!isButtonCooldownActive) StartCoroutine(HandleButtonClick(() => Zoom(true))); });
        if (zoomOutButton != null)
            zoomOutButton.GetComponent<RaycastClickable>().onClick.AddListener(() => { if (!isButtonCooldownActive) StartCoroutine(HandleButtonClick(() => Zoom(false))); });
    }

    IEnumerator HandleButtonClick(System.Action action)
    {
        if (isButtonCooldownActive) yield break;

        isButtonCooldownActive = true;
        action.Invoke();
        yield return new WaitForSeconds(buttonCooldown);
        isButtonCooldownActive = false;
    }

    void LoadTheme()
    {
        DeactivateAllModels();

        switch (GameManager.Instance.selectedTheme)
        {
            case GameManager.GameTheme.Organs:
                currentFrontModel = organModelFront;
                currentBackModel = organModelBack;
                break;
            case GameManager.GameTheme.BodyParts:
                currentFrontModel = bodypartModelFront;
                currentBackModel = bodypartModelBack;
                break;
            case GameManager.GameTheme.Skeleton:
                currentFrontModel = skeletonModelFront;
                currentBackModel = skeletonModelBack;
                break;
            case GameManager.GameTheme.Muscles:
                currentFrontModel = musclesModelFront;
                currentBackModel = musclesModelBack;
                break;
            case GameManager.GameTheme.Senses:
                currentFrontModel = sensesModelFront;
                currentBackModel = sensesModelBack;
                break;
            case GameManager.GameTheme.System:
                currentFrontModel = systemModelFront;
                currentBackModel = systemModelBack;
                break;
        }

        if (currentBackModel == null)
        {
            rotateButton.gameObject.SetActive(false);
        }
        else
        {
            rotateButton.gameObject.SetActive(true);
        }

        isFront = true;
    }

    void DeactivateAllModels()
    {
        if (organModelFront != null) organModelFront.gameObject.SetActive(false);
        if (bodypartModelFront != null) bodypartModelFront.gameObject.SetActive(false);
        if (skeletonModelFront != null) skeletonModelFront.gameObject.SetActive(false);
        if (musclesModelFront != null) musclesModelFront.gameObject.SetActive(false);
        if (sensesModelFront != null) sensesModelFront.gameObject.SetActive(false);
        if (systemModelFront != null) systemModelFront.gameObject.SetActive(false);

        if (organModelBack != null) organModelBack.gameObject.SetActive(false);
        if (bodypartModelBack != null) bodypartModelBack.gameObject.SetActive(false);
        if (skeletonModelBack != null) skeletonModelBack.gameObject.SetActive(false);
        if (musclesModelBack != null) musclesModelBack.gameObject.SetActive(false);
        if (sensesModelBack != null) sensesModelBack.gameObject.SetActive(false);
        if (systemModelBack != null) systemModelBack.gameObject.SetActive(false);
    }

    void ActivateCurrentModel()
    {
        DeactivateAllModels();

        if (isFront && currentFrontModel != null)
        {
            currentFrontModel.gameObject.SetActive(true);
        }
        else if (!isFront && currentBackModel != null)
        {
            currentBackModel.gameObject.SetActive(true);
        }
        else if (currentFrontModel != null)
        {
            currentFrontModel.gameObject.SetActive(true);
            isFront = true;
        }
    }

    public void UpdateTimerDisplay(float time)
    {
        time = Mathf.Max(0, time);

        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void UpdateScoreDisplay(bool isCorrect = true)
    {
        int currentDisplayedScore = 0;
        string currentText = scoreText.text;

        if (!string.IsNullOrEmpty(currentText) && currentText.Contains("SCORE: "))
        {
            string scoreString = currentText.Replace("SCORE: ", "").Trim();
            if (int.TryParse(scoreString, out int parsedScore))
            {
                currentDisplayedScore = parsedScore;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayScoreSound(consecutiveCorrect > 2);
            }
        }

        DOTween.To(() => currentDisplayedScore, x =>
        {
            currentDisplayedScore = x;
            scoreText.text = "SCORE: " + currentDisplayedScore;
        }, score, 0.5f).SetEase(Ease.OutQuart);

        Color currentColor = scoreText.color;

        if (!isCorrect)
        {
            scoreText.DOColor(Color.red, 0.2f)
                .SetLoops(2, LoopType.Yoyo)
                .OnComplete(() => scoreText.color = currentColor);

            consecutiveCorrect = 0;
        }
    }

    void NewQuestion()
    {
        if (QuestionManager.Instance == null)
        {
            questionText.text = "Click on body!";
            return;
        }

        if (!gameManager.gameActive)
        {
            isQuestionActive = false;
            return;
        }

        if (currentQuestion != null)
        {
            QuestionManager.Instance.FreeQuestionIndex(playerIndex);
        }

        currentQuestion = QuestionManager.Instance.GetNextQuestion(playerIndex);

        if (currentQuestion != null)
        {
            isQuestionActive = true;

            string questionTextToShow = QuestionManager.Instance.GetCurrentQuestionText(playerIndex);
            questionText.text = questionTextToShow;

            questionText.transform.localScale = Vector3.zero;
            questionText.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);

            questionAnimationCoroutine = StartCoroutine(RepeatQuestionBarAnimation());

            ActivateModelPart();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayNextQuueationSound();
            }
        }
        else
        {
            if (GameManager.Instance.currentLanguage is SystemLanguage.French)
            {
                questionText.text = "No more questions available!";
            }
            else
            {
                questionText.text = "Plus aucune question disponible!";
            }
        }

        idleTimerCoroutine = StartCoroutine(IdleTimerCoroutine());
    }

    QuestionSet GetCurrentQuestionSet()
    {
        if (QuestionManager.Instance != null)
        {
            return QuestionManager.Instance.GetCurrentQuestionSet(GameManager.Instance.selectedTheme);
        }
        return null;
    }

    void ActivateModelPart()
    {
        if (QuestionManager.Instance == null || currentQuestion == null) return;

        int questionIndex = QuestionManager.Instance.GetCurrentQuestionIndex(playerIndex);

        if (questionIndex < 0)
        {
            Debug.LogWarning("Invalid question index from QuestionManager");
            return;
        }

        QuestionSet questionSet = GetCurrentQuestionSet();
        if (questionSet == null || questionSet.questions == null)
        {
            Debug.LogWarning("Question set is null");
            return;
        }

        List<int> frontQuestionIndices = new List<int>();
        List<int> backQuestionIndices = new List<int>();

        for (int i = 0; i < questionSet.questions.Length; i++)
        {
            if (questionSet.questions[i].preferredSide == ModelSide.Front)
            {
                frontQuestionIndices.Add(i);
            }
            else
            {
                backQuestionIndices.Add(i);
            }
        }

        Image targetModel;
        int childIndex = -1;

        if (currentQuestion.preferredSide == ModelSide.Front)
        {
            targetModel = currentFrontModel;
            childIndex = frontQuestionIndices.IndexOf(questionIndex);
        }
        else
        {
            targetModel = currentBackModel;
            childIndex = backQuestionIndices.IndexOf(questionIndex);

            if (targetModel == null)
            {
                targetModel = currentFrontModel;
            }
        }

        if (targetModel == null)
        {
            return;
        }

        if (childIndex < 0)
        {
            return;
        }

        DeactivateAllModelChildren();

        int startIndex = 0;
        if (targetModel.transform.childCount > 0 && targetModel.transform.GetChild(0).CompareTag("Visual"))
            startIndex = 1;

        int adjustedIndex = childIndex + startIndex;

        if (adjustedIndex < targetModel.transform.childCount)
        {
            var childObj = targetModel.transform.GetChild(adjustedIndex);
            childObj.gameObject.SetActive(true);

            PartClick partClick = childObj.GetComponent<PartClick>();
            if (partClick != null)
            {
                partClick.ActiveOtherPart();
            }
        }
    }

    void DeactivateAllModelChildren()
    {
        if (currentFrontModel != null)
        {
            for (int i = 0; i < currentFrontModel.transform.childCount; i++)
            {
                var childObj = currentFrontModel.transform.GetChild(i).gameObject;

                if (childObj.CompareTag("Visual"))
                    continue;

                childObj.SetActive(false);
                childObj.GetComponent<PartClick>().SetAlpha(0);
            }
        }

        if (currentBackModel != null)
        {
            for (int i = 0; i < currentBackModel.transform.childCount; i++)
            {
                var childObj = currentBackModel.transform.GetChild(i).gameObject;

                if (childObj.CompareTag("Visual"))
                    continue;

                childObj.SetActive(false);
                childObj.GetComponent<PartClick>().SetAlpha(0);
            }
        }
    }


    public bool OnPartClicked(string partName)
    {
        if (QuestionManager.Instance == null || currentQuestion == null)
        {
            return false;
        }

        if (isClickBlocked) return false;

        bool isCorrect = QuestionManager.Instance.IsCorrectAnswer(playerIndex, partName);

        if (isCorrect)
        {
            score += 5;
            consecutiveCorrect++;

            if (consecutiveCorrect > 1)
            {
                score += consecutiveCorrect - 1;
            }

            isQuestionActive = false;

            ShowCorrectFeedback();
            DOVirtual.DelayedCall(2.5f, NewQuestion);
        }

        return isCorrect;
    }

    void ShowCorrectFeedback()
    {
        if (questionText != null)
        {
            questionText.text = GetCorrectMessage();

            questionText.transform.DOKill();
            questionText.transform.localScale = Vector3.one;
            questionText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);

            Image currentModel = isFront ? currentFrontModel : currentBackModel;

            if (currentModel != null)
            {
                currentModel.transform.DOKill();
                currentModel.transform.localScale = Vector3.one;
                currentModel.transform.DOPunchScale(Vector3.one * 0.05f, 0.3f, 3, 0.5f);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCorrectPart();
            }
        }
    }

    void ShowWrongFeedback()
    {
        if (questionText != null)
        {
            questionText.transform.DOComplete();
            questionText.text = GameManager.Instance.currentLanguage == SystemLanguage.English ? "Wrong Answer" : "Mauvaise Reponse";
            questionText.transform.DOShakePosition(1f, new Vector3(20f, 0f, 0f), 20, 90f, false, true);

            Image currentModel = isFront ? currentFrontModel : currentBackModel;
            if (currentModel != null)
            {
                currentModel.transform.DOShakePosition(0.5f, new Vector3(20f, 0f, 0f), 10, 90f, false, true);
                currentModel.DOColor(Color.yellow, 0.2f)
                    .SetLoops(2, LoopType.Yoyo)
                    .OnComplete(() =>
                    {
                        currentModel.color = Color.white;
                        StartCoroutine(HighlightCorrectPartAfterWrongClick());
                    });
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayWrongPart();
            }
        }
    }
    IEnumerator HighlightCorrectPartAfterWrongClick()
    {
        yield return new WaitForSeconds(0.6f);

        if (currentQuestion == null) yield break;

        int questionIndex = QuestionManager.Instance.GetCurrentQuestionIndex(playerIndex);

        if (questionIndex >= 0)
        {
            QuestionSet questionSet = GetCurrentQuestionSet();
            if (questionSet != null && questionSet.questions != null)
            {
                // Check if we need to rotate to show the correct part
                bool needsRotation = false;
                if (currentBackModel != null)
                {
                    if (isFront && currentQuestion.preferredSide == ModelSide.Back)
                    {
                        needsRotation = true;
                    }
                    else if (!isFront && currentQuestion.preferredSide == ModelSide.Front)
                    {
                        needsRotation = true;
                    }
                }

                // Find the correct part and check if it exists on current side
                if (needsRotation)
                {
                    Image targetModel = currentQuestion.preferredSide == ModelSide.Front ? currentFrontModel : currentBackModel;

                    if (targetModel == null && currentQuestion.preferredSide == ModelSide.Back)
                    {
                        targetModel = currentFrontModel;
                        needsRotation = false; // Don't rotate if back model doesn't exist
                    }

                    if (targetModel != null && needsRotation)
                    {
                        List<int> frontQuestionIndices = new();
                        List<int> backQuestionIndices = new();

                        for (int i = 0; i < questionSet.questions.Length; i++)
                        {
                            if (questionSet.questions[i].preferredSide == ModelSide.Front)
                                frontQuestionIndices.Add(i);
                            else
                                backQuestionIndices.Add(i);
                        }

                        int childIndex = -1;

                        if (currentQuestion.preferredSide == ModelSide.Front)
                        {
                            childIndex = frontQuestionIndices.IndexOf(questionIndex);
                        }
                        else
                        {
                            childIndex = backQuestionIndices.IndexOf(questionIndex);
                        }

                        if (childIndex >= 0)
                        {
                            int startIndex = 0;
                            if (targetModel.transform.childCount > 0 && targetModel.transform.GetChild(0).CompareTag("Visual"))
                                startIndex = 1;

                            int adjustedIndex = childIndex + startIndex;

                            if (adjustedIndex < targetModel.transform.childCount)
                            {
                                GameObject correctPart = targetModel.transform.GetChild(adjustedIndex).gameObject;
                                PartClick partClick = correctPart.GetComponent<PartClick>();

                                // Fixed logic: Always rotate to the correct side, regardless of hasOtherSideBone
                                // The hasOtherSideBone property just determines visual representation, not rotation necessity
                                if (partClick != null)
                                {
                                    // Only skip rotation if the part has otherSideImage active on current side
                                    bool hasVisiblePartOnCurrentSide = false;
                                    if (partClick.hasOtherSideBone && partClick.otherSideImage != null)
                                    {
                                        hasVisiblePartOnCurrentSide = partClick.otherSideImage.gameObject.activeInHierarchy;
                                    }

                                    needsRotation = !hasVisiblePartOnCurrentSide;
                                }
                            }
                        }
                    }

                    if (needsRotation)
                    {
                        RotateModel();
                        yield return new WaitForSeconds(0.2f);
                    }
                }

                // Now highlight the correct part
                Image finalTargetModel = isFront ? currentFrontModel : currentBackModel;

                if (finalTargetModel != null)
                {
                    List<int> frontQuestionIndices = new();
                    List<int> backQuestionIndices = new();

                    for (int i = 0; i < questionSet.questions.Length; i++)
                    {
                        if (questionSet.questions[i].preferredSide == ModelSide.Front)
                            frontQuestionIndices.Add(i);
                        else
                            backQuestionIndices.Add(i);
                    }

                    int childIndex = -1;

                    if (currentQuestion.preferredSide == ModelSide.Front)
                    {
                        childIndex = frontQuestionIndices.IndexOf(questionIndex);
                    }
                    else
                    {
                        childIndex = backQuestionIndices.IndexOf(questionIndex);

                        if (currentBackModel == null)
                        {
                            finalTargetModel = currentFrontModel;
                            childIndex = frontQuestionIndices.IndexOf(questionIndex);
                        }
                    }

                    int startIndex = 0;
                    if (finalTargetModel.transform.childCount > 0 &&
                        finalTargetModel.transform.GetChild(0).CompareTag("Visual"))
                    {
                        startIndex = 1;
                    }

                    int adjustedIndex = childIndex + startIndex;

                    if (adjustedIndex >= 0 && adjustedIndex < finalTargetModel.transform.childCount)
                    {
                        GameObject correctPart = finalTargetModel.transform.GetChild(adjustedIndex).gameObject;
                        PartClick correctPartClick = correctPart.GetComponent<PartClick>();

                        // Try to highlight the correct part or its otherSideImage if available
                        Image imageToHighlight = null;

                        if (correctPartClick != null && correctPartClick.hasOtherSideBone && correctPartClick.otherSideImage != null)
                        {
                            // If part has otherSideImage and it's active, highlight it
                            if (correctPartClick.otherSideImage.gameObject.activeInHierarchy)
                            {
                                imageToHighlight = correctPartClick.otherSideImage;
                            }
                        }

                        // Fallback to main part image
                        if (imageToHighlight == null && correctPart.TryGetComponent<Image>(out var correctPartImage))
                        {
                            imageToHighlight = correctPartImage;
                        }

                        if (imageToHighlight != null)
                        {
                            imageToHighlight.DOFade(1, 0.3f)
                                .SetLoops(4, LoopType.Yoyo).SetEase(Ease.Linear);

                            correctPart.transform.DOKill();
                            correctPart.transform.localScale = Vector3.one;
                            correctPart.transform.DOPunchScale(Vector3.one * 0.15f, 1.2f, 6, 0.8f);
                        }
                    }
                }
            }
        }

        yield return new WaitForSeconds(2);
        NewQuestion();
    }

    string GetCorrectMessage()
    {
        string[] messages = GameManager.Instance.currentLanguage == SystemLanguage.English
            ? new string[] { "Correct!", "Great job!", "Well done!", "Excellent!", "Perfect!" }
            : new string[] { "Correct!", "Bon travail!", "Bien joue!", "Excellent!", "Parfait!" };

        return messages[Random.Range(0, messages.Length)];
    }

    void Zoom(bool zoomIn)
    {
        ResetIdleTimer();

        isZoomed = zoomIn;
        modelRect.DOScale(zoomIn ? new Vector3(zoomScale, zoomScale, 1) : Vector3.one, 0.3f);

        float player1UpDownAdjuster;

        if (GameManager.Instance.selectedPlayers is 2)
        {
            modelContainer.DOAnchorPosX(zoomIn ? (isPlayer1 ? 75 : -75) : initialPositionModelContainer.x, 0.3f);
            player1UpDownAdjuster = -372;
        }
        else
        {
            player1UpDownAdjuster = -420;
        }

        questionBox.DOAnchorPosX(zoomIn ? (isPlayer1 ? -366 : 366) : initialPosQuestionBox.x, 0.3f);
        upArrowButton.gameObject.GetComponent<RectTransform>().DOAnchorPosX(zoomIn ? (isPlayer1 ? player1UpDownAdjuster : 372) : initialPosUpArrow.x, 0.3f);
        downArrowButton.gameObject.GetComponent<RectTransform>().DOAnchorPosX(zoomIn ? (isPlayer1 ? player1UpDownAdjuster : 372) : initialPosDownArrow.x, 0.3f);
        rotateButton.gameObject.GetComponent<RectTransform>().DOAnchorPosY(zoomIn ? 117 : initialPosRotateButton.y, 0.3f);

        if (!zoomIn) modelRect.localPosition = initialPositionModel;
    }

    void ScrollUp()
    {
        if (isZoomed)
        {
            modelRect.localPosition += new Vector3(0, scrollStep, 0);
            ClampScroll();
        }
    }

    void ScrollDown()
    {
        if (isZoomed)
        {
            modelRect.localPosition -= new Vector3(0, scrollStep, 0);
            ClampScroll();
        }
    }

    void ClampScroll()
    {
        Vector3 pos = modelRect.localPosition;
        pos.y = Mathf.Clamp(pos.y, minScrollOffset, maxScrollOffset);
        modelRect.localPosition = pos;
    }

    void RotateModel()
    {
        if (currentBackModel != null)
        {
            ResetIdleTimer();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayWhooshSound();
            }

            transform.DOComplete(transform);

            isFront = !isFront;
            ActivateCurrentModel();

            pilow.DORotate(new Vector3(0, isFront ? 0 : 180, 0), 0, RotateMode.Fast)
                .SetEase(Ease.OutBack);
        }
    }

    public int GetScore()
    {
        return score;
    }

    public int GetConsecutiveCorrect()
    {
        return consecutiveCorrect;
    }

    IEnumerator RepeatQuestionBarAnimation()
    {
        while (isQuestionActive)
        {
            questionBox.DOShakePosition(0.3f, 10f);
            yield return new WaitForSeconds(3f);
        }
    }

    void OnDestroy()
    {
        if (questionAnimationCoroutine != null)
        {
            StopCoroutine(questionAnimationCoroutine);
        }

        //scrollAction?.Disable();

        if (playerInstances.Contains(this))
        {
            playerInstances.Remove(this);
        }

        if (playerInstances.Count == 0)
        {
            playersInitialized = 0;
            isPlayingEntryAnimation = false;
        }
    }

    public void WrongClick()
    {
        if (!canPlay) return;

        ResetIdleTimer();

        if (isQuestionActive && !isClickBlocked)
        {
            isClickBlocked = true;
            ShowWrongFeedback();
            score = Mathf.Max(0, score - 2);
            UpdateScoreDisplay(false);
            StartCoroutine(BlockClicksTemporarily());
        }
    }

    IEnumerator BlockClicksTemporarily()
    {
        yield return new WaitForSeconds(2f);
        isClickBlocked = false;
    }

    public void PlayTimerAnim()
    {
        timerAnim.DOScale(1.05f, 0.5f).SetLoops(10, LoopType.Yoyo).SetEase(Ease.Linear);
    }

    public Vector3 GetScoreCardPosition()
    {
        return scoreText.transform.position;
    }

    public void TriggerScoreCardBounce()
    {
        scoreText.transform.DOKill();
        scoreText.transform.localScale = Vector3.one;
        scoreText.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f, 10, 1f);


        if (consecutiveCorrect > 2)
        {
            CreateScoreParticleExplosion();
        }
    }

    void CreateScoreParticleExplosion()
    {
        for (int i = 0; i < 8; i++)
        {
            GameObject particle = new GameObject("ScoreParticle");
            Image particleImg = particle.AddComponent<Image>();
            particle.transform.SetParent(scoreText.transform.parent, false);

            RectTransform particleRect = particle.GetComponent<RectTransform>();
            RectTransform scoreRect = scoreText.GetComponent<RectTransform>();

            particleRect.anchoredPosition = scoreRect.anchoredPosition;
            particleRect.sizeDelta = new Vector2(10, 10);

            particleImg.color = Color.yellow;

            Vector3 direction = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f), 0
            ).normalized;

            Vector2 targetPos = particleRect.anchoredPosition + (Vector2)(direction * 100f);

            particleRect.DOAnchorPos(targetPos, 0.8f)
                .SetEase(Ease.OutQuart);
            particleImg.DOFade(0, 0.8f);
            particleRect.DOScale(0, 0.8f).SetEase(Ease.InQuart)
                .OnComplete(() => Destroy(particle));
        }

        timerAnim.DOScale(1.1f, 0.5f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.Linear);
    }

    IEnumerator PlayIdleEffect()
    {
        yield return new WaitForSeconds(2);

        while (isQuestionActive && Time.time - lastInteractionTime > 4f)
        {
            Image currentModel = isFront ? currentFrontModel : currentBackModel;
            if (currentModel != null)
            {
                currentModel.transform.DOShakePosition(1f, new Vector3(5f, 5f, 0f), 10, 45f, false, true);
            }

            yield return new WaitForSeconds(5);
        }
        idleCoroutine = null;
    }

    void ResetIdleTimer()
    {
        lastInteractionTime = Time.time;
        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }

        if (idleTimerCoroutine != null)
        {
            StopCoroutine(idleTimerCoroutine);
        }

        if (isQuestionActive)
        {
            idleTimerCoroutine = StartCoroutine(IdleTimerCoroutine());
        }
    }

    IEnumerator IdleTimerCoroutine()
    {
        yield return new WaitForSeconds(4f);

        if (isQuestionActive)
        {
            idleCoroutine = StartCoroutine(PlayIdleEffect());
        }

        idleTimerCoroutine = null;
    }


    public void StopAllAnimations()
    {
        isQuestionActive = false;

        if (questionAnimationCoroutine != null)
        {
            StopCoroutine(questionAnimationCoroutine);
            questionAnimationCoroutine = null;
        }

        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }

        DOTween.Kill(transform);
        DOTween.Kill(questionBox);
        DOTween.Kill(questionText.transform);
        DOTween.Kill(scoreText.transform);
        DOTween.Kill(timerAnim);
        DOTween.Kill(modelRect);
        DOTween.Kill(modelContainer);

        if (currentFrontModel != null)
        {
            DOTween.Kill(currentFrontModel.rectTransform);
        }

        if (currentBackModel != null)
        {
            DOTween.Kill(currentBackModel.rectTransform);
        }
    }

    public void OnClicked()
    {
        WrongClick();
    }
}