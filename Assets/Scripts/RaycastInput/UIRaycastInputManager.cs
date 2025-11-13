using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class UIRaycastInputManager : MonoBehaviour
{
    [Header("References")]
    private Camera playerCamera;

    [Header("Delays")]
    public float uiButtonDelay = 2f;     // for regular UI
    public float gameplayDelay = 0.4f;   // for gameplay elements

    private bool isPressing;
    private RaycastClickable currentClickable;
    private Coroutine delayRoutine;

    // For hover detection
    private RaycastClickable currentHoveredClickable;
    private UIButtonEnhancer currentHoveredEnhancer;

    // New Input System
    private InputAction clickAction;
    private bool isReadyForNextClick = true;

    private void Start()
    {
        playerCamera = Camera.main;
    }

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

    void Update()
    {
        // Handle hover detection for mouse
        if (Mouse.current != null)
        {
            Vector2 pos = Mouse.current.position.ReadValue();
            HandleHover(pos);
        }
    }

    private void OnMouseClick(CallbackContext ctx)
    {
        if (playerCamera == null) playerCamera = Camera.main;

        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit2D rayCastHit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity);

        if (rayCastHit.collider != null)
        {
            IRaycastClickHandler[] clickHandlers = rayCastHit.collider.GetComponents<IRaycastClickHandler>();

            if (clickHandlers.Length > 0)
            {
                if (!isReadyForNextClick)
                    return;

                RaycastClickable clickable = rayCastHit.collider.GetComponent<RaycastClickable>();
                float delay = clickable != null ? (clickable.isGameplayElement ? gameplayDelay : uiButtonDelay) : gameplayDelay;

                StartCoroutine(DelayAndTriggerAllHandlers(clickHandlers, delay));
                return;
            }
        }
    }

    IEnumerator DelayAndTriggerAllHandlers(IRaycastClickHandler[] handlers, float delay)
    {
        isReadyForNextClick = false;
        yield return new WaitForSeconds(delay);

        for (int i = 0; i < handlers.Length; i++)
        {
            var handler = handlers[i];

            if (handler == null || (handler as MonoBehaviour) == null)
                continue;

            var handlerMono = handler as MonoBehaviour;
            if (!handlerMono.gameObject.activeInHierarchy)
                continue;

            try
            {
                handler.OnRaycastClick();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Exception calling {handler.GetType().Name}: {e.Message}");
            }
        }

        isReadyForNextClick = true;
    }

    // ------- Raycast to UI using Physics2D -------
    RaycastClickable RaycastUI(Vector2 screenPos)
    {
        if (playerCamera == null) return null;

        Ray ray = playerCamera.ScreenPointToRay(screenPos);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity);

        if (hit.collider != null)
        {
            return hit.collider.GetComponent<RaycastClickable>();
        }

        return null;
    }

    // ------- Handle Hover Detection -------
    void HandleHover(Vector2 screenPos)
    {
        var hoveredClickable = RaycastUI(screenPos);

        // If we're hovering over a different object
        if (hoveredClickable != currentHoveredClickable)
        {
            // Exit previous hover
            if (currentHoveredEnhancer != null)
            {
                currentHoveredEnhancer.OnRaycastHoverExit();
            }

            // Enter new hover
            currentHoveredClickable = hoveredClickable;
            currentHoveredEnhancer = null;

            if (currentHoveredClickable != null)
            {
                currentHoveredEnhancer = currentHoveredClickable.GetComponent<UIButtonEnhancer>();
                if (currentHoveredEnhancer != null)
                {
                    currentHoveredEnhancer.OnRaycastHoverEnter();
                }
            }
        }
    }
}
