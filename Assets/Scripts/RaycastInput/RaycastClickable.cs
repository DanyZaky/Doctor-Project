using UnityEngine;
using UnityEngine.Events;

public class RaycastClickable : MonoBehaviour, IRaycastClickHandler
{
    [Header("Is this gameplay element?")]
    [Tooltip("Off = UI Button (2s delay), On = Gameplay Element (0.4s delay)")]
    public bool isGameplayElement = false;

    [Header("Event called after delay & valid hold")]
    public UnityEvent onClick;

    public void OnRaycastClick()
    {
        onClick?.Invoke();
    }
}
