using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Question Set", menuName = "Doctor Project/Question Set")]
public class QuestionSet : ScriptableObject
{
    public GameManager.GameTheme theme;

    [Header("Questions")]
    public Question[] questions;
}

[Serializable]
public struct QuestionText
{
    public string questionTextEnglish;
    public string questionTextFrench;
}

[Serializable]
public class Question
{
    [Header("Question Details")]
    public List<QuestionText> questionTexts;
    public string correctPartName;

    public ModelSide preferredSide = ModelSide.Front;

    public QuestionText GetRandomQuestionText()
    {
        if (questionTexts == null || questionTexts.Count == 0)
        {
            return new QuestionText
            {
                questionTextEnglish = "Click on the highlighted part!",
                questionTextFrench = "Cliquez sur la partie en surbrillance!"
            };
        }

        return questionTexts[UnityEngine.Random.Range(0, questionTexts.Count)];
    }

    public string GetRandomQuestionForLanguage(SystemLanguage language)
    {
        QuestionText randomText = GetRandomQuestionText();
        return language == SystemLanguage.English ? randomText.questionTextEnglish : randomText.questionTextFrench;
    }
}

public enum ModelSide
{
    Front,
    Back
}
