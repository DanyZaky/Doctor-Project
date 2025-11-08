using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Settings")]
    public int selectedPlayers = 1;
    public GameTheme selectedTheme = GameTheme.Organs;
    public float gameplayTimer = 60f;
    public SystemLanguage currentLanguage = SystemLanguage.English;

    public static event Action OnLanguageChanged;

    public enum GameTheme
    {
        Organs,
        BodyParts,
        Skeleton,
        Muscles,
        Senses,
        System
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void LoadSettings()
    {
        selectedPlayers = PlayerPrefs.GetInt("SelectedPlayers", 1);
        selectedTheme = (GameTheme)PlayerPrefs.GetInt("SelectedTheme", 0);
        gameplayTimer = PlayerPrefs.GetFloat("GameplayTimer", 60f);

        int savedLanguage = PlayerPrefs.GetInt("GameLanguage", 0);
        currentLanguage = savedLanguage == 0 ? SystemLanguage.English : SystemLanguage.French;
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetInt("SelectedPlayers", selectedPlayers);
        PlayerPrefs.SetInt("SelectedTheme", (int)selectedTheme);
        PlayerPrefs.SetFloat("GameplayTimer", gameplayTimer);
        PlayerPrefs.SetInt("GameLanguage", currentLanguage == SystemLanguage.English ? 0 : 1);
        PlayerPrefs.Save();
    }

    public void SetPlayers(int players)
    {
        selectedPlayers = players;
        SaveSettings();
    }

    public void SetTheme(GameTheme theme)
    {
        selectedTheme = theme;
        SaveSettings();
    }

    public void SetGameplayTimer(float timer)
    {
        gameplayTimer = timer;
        SaveSettings();
    }

    public void SetLanguage(SystemLanguage language)
    {
        currentLanguage = language;
        SaveSettings();

        OnLanguageChanged?.Invoke();
    }

    public void NextTheme()
    {
        GameTheme[] themes = (GameTheme[])Enum.GetValues(typeof(GameTheme));
        int currentIndex = Array.IndexOf(themes, selectedTheme);
        int nextIndex = (currentIndex + 1) % themes.Length;
        selectedTheme = themes[nextIndex];
    }
}