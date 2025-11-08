using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HowToAnim : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] Image partGlow;
    [SerializeField] GameObject hightlight;


    void OnEnable()
    {
        PlayAnim(true);
    }


    void PlayAnim(bool play)
    {
        animator.enabled = play;

        if (hightlight != null) hightlight.SetActive(!play);
        if (partGlow != null) partGlow.gameObject.SetActive(false);
    }

    public void PlayPartGlow()
    {
        if (partGlow != null)
        {
            StartCoroutine(PartGlowCoroutine());
        }
    }

    IEnumerator PartGlowCoroutine()
    {
        partGlow.gameObject.SetActive(true);
        partGlow.DOFade(1, 0);
        partGlow.DOFade(0, 1.5f);
        yield return new WaitForSeconds(1.5f);
        partGlow.gameObject.SetActive(false);
    }

    void OnDisable()
    {
        PlayAnim(false);
    }
}
