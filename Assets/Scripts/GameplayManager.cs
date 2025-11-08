using System.Collections;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class GameplayManager : MonoBehaviour
{
    [Header("Player Panels")]
    [SerializeField] GameObject singlePlayerContainer;
    [SerializeField] GameObject player1Container;
    [SerializeField] GameObject player2Container;

    [SerializeField] PlayerGameplay singlePlayerPanel;
    [SerializeField] PlayerGameplay player1Panel;
    [SerializeField] PlayerGameplay player2Panel;

    [SerializeField] RectTransform player1Pos;

    [Header("Game Over UI")]
    [SerializeField] GameObject winnerPanel;
    [SerializeField] GameOverUI gameOverUI;

    float sharedRemainingTime;
    public bool gameActive = true;

    void Start()
    {
        sharedRemainingTime = GameManager.Instance.gameplayTimer;
        SetupPlayers();
        StartCoroutine(UpdateTimer());
    }

    void SetupPlayers()
    {
        int players = GameManager.Instance.selectedPlayers;
        singlePlayerContainer.SetActive(false);
        player1Container.SetActive(false);
        player2Container.SetActive(false);

        if (players == 1)
        {
            singlePlayerContainer.SetActive(true);
            singlePlayerPanel.Initialize(true, this);
            singlePlayerContainer.GetComponent<RectTransform>().anchoredPosition = player1Pos.anchoredPosition;

            RectTransform parent = singlePlayerPanel.transform.parent as RectTransform;
            var singlePlayerScreenPos = parent.anchoredPosition;
            singlePlayerScreenPos.x = 250;
            parent.anchoredPosition = singlePlayerScreenPos;
        }
        else
        {
            player1Container.SetActive(true);
            player2Container.SetActive(true);
            player1Panel.Initialize(true, this);
            player2Panel.Initialize(false, this);
        }
    }

    IEnumerator UpdateTimer()
    {
        while (sharedRemainingTime > 0 && gameActive)
        {
            yield return new WaitForSeconds(1);
            sharedRemainingTime--;

            NotifyPlayersTimerUpdate(sharedRemainingTime);

            if (sharedRemainingTime is 5)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayeTimerun();
                }

                int players = GameManager.Instance.selectedPlayers;

                if (players is 1)
                {
                    singlePlayerPanel.PlayTimerAnim();
                }
                else
                {
                    player1Panel.PlayTimerAnim();
                    player2Panel.PlayTimerAnim();
                }
            }

            if (sharedRemainingTime is 2)
            {
                if (AudioManager.Instance)
                {
                    AudioManager.Instance.PlayTimeOverSound();
                }
            }
        }

        if (gameActive)
            GameOver();
    }

    void NotifyPlayersTimerUpdate(float time)
    {
        if (GameManager.Instance.selectedPlayers == 1)
        {
            if (singlePlayerPanel != null)
                singlePlayerPanel.UpdateTimerDisplay(time);
        }
        else
        {
            if (player1Panel != null) player1Panel.UpdateTimerDisplay(time);
            if (player2Panel != null) player2Panel.UpdateTimerDisplay(time);
        }
    }

    public float GetRemainingTime()
    {
        return sharedRemainingTime;
    }

    public void GameOver()
    {
        if (winnerPanel != null && gameOverUI != null)
        {
            winnerPanel.SetActive(true);

            if (GameManager.Instance.selectedPlayers == 1)
            {
                int finalScore = singlePlayerPanel != null ? singlePlayerPanel.GetScore() : 0;
                gameOverUI.ShowSinglePlayerResults(finalScore);
            }
            else
            {
                int player1Score = player1Panel != null ? player1Panel.GetScore() : 0;
                int player2Score = player2Panel != null ? player2Panel.GetScore() : 0;
                gameOverUI.ShowTwoPlayerResults(player1Score, player2Score);
            }
        }

        gameActive = false;

        player1Panel.StopAllAnimations();
        player2Panel.StopAllAnimations();
    }
}
