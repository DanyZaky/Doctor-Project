using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem; // NEW INPUT SYSTEM

public class UIRaycastInputManager : MonoBehaviour
{
    [Header("References")]
    public GraphicRaycaster raycaster;   // from Canvas
    public EventSystem eventSystem;      // EventSystem in the scene

    [Header("Delays")]
    public float uiButtonDelay = 2f;     // for regular UI
    public float gameplayDelay = 0.4f;   // for gameplay elements

    private bool isPressing;
    private RaycastClickable currentClickable;
    private Coroutine delayRoutine;

    // For hover detection
    private RaycastClickable currentHoveredClickable;
    private UIButtonEnhancer currentHoveredEnhancer;

    void Update()
    {
        // Priority: touch (Android/device)
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame
            || Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed
            || Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
        {
            HandleTouch();
        }
        // Fallback: mouse (Editor / PC)
        else if (Mouse.current != null)
        {
            HandleMouse();
        }
    }

    // ------- Mouse (Editor) -------
    void HandleMouse()
    {
        var mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector2 pos = mouse.position.ReadValue();

        // Handle hover detection
        HandleHover(pos);

        if (mouse.leftButton.wasPressedThisFrame)
            StartPress(pos);

        if (mouse.leftButton.wasReleasedThisFrame)
            EndPress();

        if (isPressing && currentClickable != null && mouse.leftButton.isPressed)
            CheckStillOnTarget(pos);
    }

    // ------- Touch (Android / Mobile) -------
    void HandleTouch()
    {
        var touch = Touchscreen.current.primaryTouch;
        Vector2 pos = touch.position.ReadValue();

        // Handle hover detection for touch (when finger is down but not clicking yet)
        if (touch.press.isPressed)
            HandleHover(pos);

        if (touch.press.wasPressedThisFrame)
            StartPress(pos);

        if (touch.press.wasReleasedThisFrame)
            EndPress();

        if (isPressing && currentClickable != null && touch.press.isPressed)
            CheckStillOnTarget(pos);
    }

    // ------- Start press -------
    void StartPress(Vector2 screenPos)
    {
        isPressing = true;
        currentClickable = RaycastUI(screenPos);

        if (currentClickable == null)
            return;

        if (delayRoutine != null)
            StopCoroutine(delayRoutine);

        float delay = currentClickable.isGameplayElement ? gameplayDelay : uiButtonDelay;
        delayRoutine = StartCoroutine(DelayAndTrigger(currentClickable, delay));
    }

    // ------- Release / cancel -------
    void EndPress()
    {
        isPressing = false;
        currentClickable = null;

        if (delayRoutine != null)
        {
            StopCoroutine(delayRoutine);
            delayRoutine = null;
        }
    }

    // ------- Check still on the same target -------
    void CheckStillOnTarget(Vector2 screenPos)
    {
        var hit = RaycastUI(screenPos);
        if (hit != currentClickable)
        {
            // moved out → cancel
            EndPress();
        }
    }

    // ------- Delay before trigger -------
    IEnumerator DelayAndTrigger(RaycastClickable target, float delay)
    {
        float timer = 0f;

        while (timer < delay)
        {
            if (!isPressing || currentClickable != target)
                yield break; // cancelled

            timer += Time.deltaTime;
            yield return null;
        }

        // successfully held long enough → call event
        target.OnRaycastClick();

        // one-time use, reset
        EndPress();
    }

    // ------- Raycast to UI -------
    RaycastClickable RaycastUI(Vector2 screenPos)
    {
        if (raycaster == null || eventSystem == null)
            return null;

        var data = new PointerEventData(eventSystem)
        {
            position = screenPos
        };

        var results = new List<RaycastResult>();
        raycaster.Raycast(data, results);

        for (int i = 0; i < results.Count; i++)
        {
            var clickable = results[i].gameObject.GetComponentInParent<RaycastClickable>();
            if (clickable != null)
                return clickable;
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
