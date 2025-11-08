using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem; // NEW INPUT SYSTEM

public class UIRaycastInputManager : MonoBehaviour
{
    [Header("References")]
    public GraphicRaycaster raycaster;   // dari Canvas
    public EventSystem eventSystem;      // EventSystem di scene

    [Header("Delays")]
    public float uiButtonDelay = 2f;     // untuk UI biasa
    public float gameplayDelay = 0.4f;   // untuk elemen gameplay

    private bool isPressing;
    private RaycastClickable currentClickable;
    private Coroutine delayRoutine;

    void Update()
    {
        // Prioritas: touch (Android/device)
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

        if (touch.press.wasPressedThisFrame)
            StartPress(pos);

        if (touch.press.wasReleasedThisFrame)
            EndPress();

        if (isPressing && currentClickable != null && touch.press.isPressed)
            CheckStillOnTarget(pos);
    }

    // ------- Mulai tekan -------
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

    // ------- Lepas / batal -------
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

    // ------- Cek masih di target yg sama -------
    void CheckStillOnTarget(Vector2 screenPos)
    {
        var hit = RaycastUI(screenPos);
        if (hit != currentClickable)
        {
            // geser keluar → batal
            EndPress();
        }
    }

    // ------- Delay sebelum trigger -------
    IEnumerator DelayAndTrigger(RaycastClickable target, float delay)
    {
        float timer = 0f;

        while (timer < delay)
        {
            if (!isPressing || currentClickable != target)
                yield break; // dibatalkan

            timer += Time.deltaTime;
            yield return null;
        }

        // sukses hold cukup lama → panggil event
        target.OnRaycastClick();

        // sekali pakai, reset
        EndPress();
    }

    // ------- Raycast ke UI -------
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
}
