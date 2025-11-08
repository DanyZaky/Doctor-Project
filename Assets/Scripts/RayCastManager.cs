using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;

public class RayCastManager : MonoBehaviour
{
    [Header("Multi-Touch Settings")]
    [SerializeField] private bool enableScreenZoneDivision = true;
    [SerializeField] private float screenDivisionRatio = 0.5f; // 0.5 = middle of screen
    [SerializeField] private bool debugMode = true;
    [SerializeField] private LayerMask rayLayer;

    [Header("References")]
    [SerializeField] private GameplayManager gameplayManager;

    private Camera playerCamera;
    private Mouse mouse;

    // Track which player clicked
    private int currentClickingPlayer = 0;

    bool canClick = true;
    float clickCooldown = 0.4f;

    private void Start()
    {
        playerCamera = Camera.main;
        mouse = Mouse.current;

        if (mouse == null)
        {
            Debug.LogError("No mouse detected!");
        }

        if (playerCamera == null)
        {
            Debug.LogError("Main camera not found!");
        }
    }

    #region New Input System
    private InputAction clickAction;

    private void Awake()
    {
        clickAction = new InputAction();
        clickAction.AddBinding("<Mouse>/leftButton")
            .WithInteractions("press()");
        clickAction.performed += OnMouseClick;
    }

    private void OnEnable()
    {
        clickAction.Enable();
    }

    private void OnDisable()
    {
        clickAction.Disable();
    }

    void OnMouseClick(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (!canClick) return;

        StartCoroutine(DelayedPhysicsClick());
    }

    IEnumerator DelayedPhysicsClick()
    {
        yield return null; // wait 1 frame so UI finishes processing input

        if (playerCamera == null) playerCamera = Camera.main;
        if (mouse == null) yield break;

        Vector2 mousePosition = mouse.position.ReadValue();

        if (IsPointerOverUIButton(mousePosition))
        {
            if (debugMode)
                Debug.Log("Click ignored pointer is over UI.");
            yield break;
        }

        int clickingPlayer = GetPlayerFromScreenPosition(mousePosition);
        if (clickingPlayer == 0)
        {
            if (debugMode)
                Debug.LogWarning("Click outside valid player zones");
            yield break;
        }

        currentClickingPlayer = clickingPlayer;

        Ray ray = playerCamera.ScreenPointToRay(mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity);

        if (hit.collider != null)
        {
            if (IsValidTargetForPlayer(hit.collider.gameObject, clickingPlayer))
            {
                if (hit.collider.TryGetComponent(out IOption _Opt))
                {
                    if (debugMode)
                        Debug.Log($"Player {clickingPlayer} clicked on: {hit.collider.gameObject.name}");

                    _Opt.OnClicked();
                }
            }
            else if (debugMode)
            {
                Debug.LogWarning($"Player {clickingPlayer} tried to click on Player {(clickingPlayer == 1 ? 2 : 1)}'s area");
            }
        }

        StartCoroutine(ResetCooldown());
    }

    IEnumerator ResetCooldown()
    {
        canClick = false;
        yield return new WaitForSeconds(clickCooldown);
        canClick = true;
    }

    bool IsPointerOverUIButton(Vector2 mousePos)
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = mousePos
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var hit in results)
        {
            if (hit.gameObject.GetComponent<Button>() != null)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines which player clicked based on screen position
    /// </summary>
    int GetPlayerFromScreenPosition(Vector2 screenPosition)
    {
        if (!enableScreenZoneDivision)
        {
            // If screen division is disabled, allow any click (single player mode)
            return 1;
        }

        // Check if it's single or two player mode
        if (GameManager.Instance != null && GameManager.Instance.selectedPlayers == 1)
        {
            return 1; // Single player, everything is player 1
        }

        // Divide screen horizontally (left = player 1, right = player 2)
        float divisionX = Screen.width * screenDivisionRatio;

        if (screenPosition.x < divisionX)
            return 1; // Player 1 (left side)
        else
            return 2; // Player 2 (right side)
    }

    /// <summary>
    /// Checks if the clicked object belongs to the correct player's area
    /// </summary>
    bool IsValidTargetForPlayer(GameObject target, int playerNumber)
    {
        // If not in 2-player mode, all clicks are valid
        if (GameManager.Instance == null || GameManager.Instance.selectedPlayers == 1)
            return true;

        // Get the PartClick component
        PartClick partClick = target.GetComponent<PartClick>();
        if (partClick != null)
        {
            // Get the PlayerGameplay component from the part
            PlayerGameplay playerGameplay = partClick.GetComponent<PlayerGameplay>();
            if (playerGameplay == null)
            {
                // Try to find it in the serialized field (this requires making it public or using reflection)
                // For now, we'll check by parent hierarchy
                playerGameplay = target.GetComponentInParent<PlayerGameplay>();
            }

            // If we found the PlayerGameplay, verify it matches the clicking player
            if (playerGameplay != null)
            {
                // In GameplayManager, player1Panel is for player 1, player2Panel is for player 2
                // We need to determine which panel this part belongs to
                return IsPlayerGameplayForPlayer(playerGameplay, playerNumber);
            }
        }

        // Fallback: Check by parent hierarchy naming convention
        Transform current = target.transform;
        while (current != null)
        {
            string objName = current.name.ToLower();

            if (objName.Contains("player1") || objName.Contains("p1") || objName.Contains("single"))
                return playerNumber == 1;

            if (objName.Contains("player2") || objName.Contains("p2"))
                return playerNumber == 2;

            current = current.parent;
        }

        // Final fallback: Check by screen position of the object itself
        Vector3 worldPos = target.transform.position;
        Vector3 screenPos = playerCamera.WorldToScreenPoint(worldPos);
        int objectPlayer = GetPlayerFromScreenPosition(screenPos);
        return objectPlayer == playerNumber;
    }

    /// <summary>
    /// Determines if a PlayerGameplay instance belongs to a specific player number
    /// </summary>
    bool IsPlayerGameplayForPlayer(PlayerGameplay playerGameplay, int playerNumber)
    {
        if (gameplayManager == null)
            return true;

        // You'll need to expose these as public in GameplayManager or add a method there
        // For now, we check by comparing screen position
        Vector3 worldPos = playerGameplay.transform.position;
        Vector3 screenPos = playerCamera.WorldToScreenPoint(worldPos);
        int detectedPlayer = GetPlayerFromScreenPosition(screenPos);

        return detectedPlayer == playerNumber;
    }

    /// <summary>
    /// Get the current clicking player (useful for other scripts)
    /// </summary>
    public int GetCurrentClickingPlayer()
    {
        return currentClickingPlayer;
    }

    /// <summary>
    /// Enable or disable screen zone division at runtime
    /// </summary>
    public void SetScreenZoneDivision(bool enabled)
    {
        enableScreenZoneDivision = enabled;
    }

    /// <summary>
    /// Adjust the division ratio at runtime (0.5 = middle, 0.3 = 30/70 split, etc.)
    /// </summary>
    public void SetDivisionRatio(float ratio)
    {
        screenDivisionRatio = Mathf.Clamp01(ratio);
    }

    #endregion

    private void OnDrawGizmos()
    {
        if (!enableScreenZoneDivision || !debugMode) return;

        // Draw division line in Scene view
        if (playerCamera != null)
        {
            Gizmos.color = Color.yellow;

            Vector3 topPoint = playerCamera.ScreenToWorldPoint(
                new Vector3(Screen.width * screenDivisionRatio, Screen.height, playerCamera.nearClipPlane + 5));

            Vector3 bottomPoint = playerCamera.ScreenToWorldPoint(
                new Vector3(Screen.width * screenDivisionRatio, 0, playerCamera.nearClipPlane + 5));

            Gizmos.DrawLine(topPoint, bottomPoint);
        }
    }
}