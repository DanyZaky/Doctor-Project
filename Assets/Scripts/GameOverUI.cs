using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("Winner Display")]
    [SerializeField] GameObject winnerImage;
    [SerializeField] GameObject drawImage;
    [SerializeField] GameObject nicePlayImage;
    [SerializeField] GameObject stats;
    [SerializeField] GameObject particles1;
    [SerializeField] GameObject particles2;

    [Header("Player 1 Results")]
    [SerializeField] GameObject player1Panel;
    [SerializeField] GameObject p1Star;
    [SerializeField] TextMeshProUGUI player1TimeText;
    [SerializeField] TextMeshProUGUI player1ScoreText;

    [Header("Player 2 Results")]
    [SerializeField] GameObject player2Panel;
    [SerializeField] GameObject p2Star;
    [SerializeField] TextMeshProUGUI player2TimeText;
    [SerializeField] TextMeshProUGUI player2ScoreText;

    [Header("Buttons")]
    [SerializeField] Button mainMenuButton;
    [SerializeField] Button nextButton;
    [SerializeField] Button playAgainButton;

    [Header("Animation")]
    [SerializeField] float animationDuration = 0.5f;
    [SerializeField] float delayBetweenElements = 0.2f;

    void Start()
    {
        SetupButtons();

        DOVirtual.DelayedCall(0.5f, () =>
        {
            stats.SetActive(true);
            particles1.SetActive(true);
            particles2.SetActive(true);
        });

        StartCoroutine(ParticlesLoop());

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayeGameOver();
        }
    }

    IEnumerator ParticlesLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(3);
            particles1.SetActive(false);
            particles1.SetActive(true);
        }
    }

    void SetupButtons()
    {
        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(PlayAgain);
        }

        mainMenuButton.onClick.AddListener(GoToMainMenu);
        nextButton.onClick.AddListener(NextModel);
    }

    public void ShowSinglePlayerResults(int finalScore)
    {
        if (player2Panel != null)
        {
            player2Panel.SetActive(false);
        }

        if (p1Star != null)
        {
            p1Star.SetActive(false);
        }

        DisplayPlayerResults(true, finalScore, GameManager.Instance.gameplayTimer);

        nicePlayImage.SetActive(true);
        winnerImage.SetActive(false);
        drawImage.SetActive(false);
    }

    public void ShowTwoPlayerResults(int player1Score, int player2Score)
    {
        if (player2Panel != null)
        {
            player2Panel.SetActive(true);
        }

        bool isTie = player1Score == player2Score;

        if (isTie)
        {
            p1Star.SetActive(false);
            p2Star.SetActive(false);

            nicePlayImage.SetActive(false);
            winnerImage.SetActive(false);
            drawImage.SetActive(true);
        }
        else
        {
            p1Star.SetActive(player1Score > player2Score);
            p2Star.SetActive(player1Score < player2Score);

            nicePlayImage.SetActive(false);
            winnerImage.SetActive(true);
            drawImage.SetActive(false);
        }

        DisplayPlayerResults(true, player1Score, GameManager.Instance.gameplayTimer);
        DisplayPlayerResults(false, player2Score, GameManager.Instance.gameplayTimer);

        player1ScoreText.transform.localScale = Vector3.zero;
        player2ScoreText.transform.localScale = Vector3.zero;
        player1ScoreText.transform.DOScale(Vector3.one, animationDuration)
                .SetEase(Ease.OutBounce);
        player2ScoreText.transform.DOScale(Vector3.one, animationDuration)
                .SetEase(Ease.OutBounce);
    }

    void DisplayPlayerResults(bool isPlayer1, int score, float totalTime)
    {
        TextMeshProUGUI timeText = isPlayer1 ? player1TimeText : player2TimeText;
        TextMeshProUGUI scoreText = isPlayer1 ? player1ScoreText : player2ScoreText;

        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(totalTime / 60);
            int seconds = Mathf.FloorToInt(totalTime % 60);
            timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        if (scoreText != null)
        {
            string scoreLabel = GameManager.Instance.currentLanguage == SystemLanguage.English
                ? "SCORE: "
                : "SCORE: ";
            scoreText.text = scoreLabel + score;
        }
    }

    void PlayAgain()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick(ButtonClickType.Medium);
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void GoToMainMenu()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick(ButtonClickType.Medium);
        }

        SceneManager.LoadSceneAsync(0);
    }

    void NextModel()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick(ButtonClickType.Medium);
        }

        GameManager.Instance.NextTheme();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
