using System;
using UnityEngine;

public enum ButtonClickType
{
    High,
    Medium,
    Low
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] AudioClip buttonClickHigh;
    [SerializeField] AudioClip buttonClickMedium;
    [SerializeField] AudioClip buttonClickLow;
    [SerializeField] AudioClip buttonHoverSound;
    [SerializeField] AudioClip gameStartSound;
    [SerializeField] AudioClip correctPart;
    [SerializeField] AudioClip wrongPart;
    [SerializeField] AudioClip winner;
    [SerializeField] AudioClip timeRunout;
    [SerializeField] AudioClip whoosh;
    [SerializeField] AudioClip consectiveScore;
    [SerializeField] AudioClip scoreUpSound;
    [SerializeField] AudioClip nextQuestion;
    [SerializeField] AudioClip timeOver;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayButtonClick(ButtonClickType clickType = ButtonClickType.High)
    {
        AudioClip clipToPlay = null;

        switch (clickType)
        {
            case ButtonClickType.High:
                clipToPlay = buttonClickHigh;
                break;
            case ButtonClickType.Medium:
                clipToPlay = buttonClickMedium;
                break;
            case ButtonClickType.Low:
                clipToPlay = buttonClickLow;
                break;
        }

        if (clipToPlay != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clipToPlay);
        }
    }

    public void PlayButtonHover()
    {
        if (buttonHoverSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(buttonHoverSound);
        }
    }

    public void PlayGameStart()
    {
        if (gameStartSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(gameStartSound);
        }
    }

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
        {
            musicSource.volume = volume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null)
        {
            sfxSource.volume = volume;
        }
    }

    public void PlayCorrectPart()
    {
        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(correctPart);
        }
    }

    public void PlayWrongPart()
    {
        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(wrongPart);
        }
    }

    public void PlayeGameOver()
    {
        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(winner);
        }
    }

    public void PlayeTimerun()
    {
        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(timeRunout);
        }
    }

    public void PlayWhooshSound()
    {
        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(whoosh, 0.5f);
        }
    }

    public void PlayNextQuueationSound()
    {
        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(nextQuestion);
        }
    }

    public void PlayScoreSound(bool consective)
    {
        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(consective ? consectiveScore : scoreUpSound);
        }
    }

    public void PlayTimeOverSound()
    {
        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(timeOver);
        }
    }
}