using DG.Tweening;
using System.Collections;
using UnityEngine;

public class ShakePosition : MonoBehaviour
{
    public RectTransform targetRect;
    public float duration = 0.3f;
    public float strength = 10;

    void OnEnable()
    {
        StartCoroutine(ShakeIt());
    }

    IEnumerator ShakeIt()
    {
        while (true)
        {
            yield return new WaitForSeconds(3f);
            targetRect.DOShakePosition(duration, strength);
        }
    }

    private void OnDisable()
    {
        StopCoroutine(ShakeIt());
    }
}
