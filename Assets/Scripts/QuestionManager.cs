using System.Collections.Generic;
using UnityEngine;

public class QuestionManager : MonoBehaviour
{
    public static QuestionManager Instance;

    [Header("Question Sets")]
    [SerializeField] QuestionSet questionSetOrgans;
    [SerializeField] QuestionSet questionSetBodyParts;
    [SerializeField] QuestionSet questionSetSkeleton;
    [SerializeField] QuestionSet questionSetMuscle;
    [SerializeField] QuestionSet questionSetSenses;
    [SerializeField] QuestionSet questionSetSystem;

    [Header("Current Game State")]
    [SerializeField] bool useRandomOrder = true;
    [SerializeField] bool avoidRepeats = true;

    HashSet<int> currentlyUsedQuestionIndices = new HashSet<int>();

    private class PlayerQuestionState
    {
        public QuestionSet questionSet;
        public List<Question> availableQuestions;
        public List<Question> usedQuestions;
        public Question currentQuestion;
        public int currentQuestionIndex = -1;
        public string currentQuestionText;
    }

    Dictionary<int, PlayerQuestionState> playerStates = new Dictionary<int, PlayerQuestionState>();

    void Awake()
    {
        Instance = this;
    }

    public void InitializeForPlayer(int playerIndex, GameManager.GameTheme theme)
    {
        QuestionSet selectedQuestionSet = GetQuestionSetForTheme(theme);

        if (selectedQuestionSet == null)
        {
            Debug.LogError($"No question set found for theme: {theme}");
            return;
        }

        if (!playerStates.ContainsKey(playerIndex))
        {
            playerStates[playerIndex] = new PlayerQuestionState();
        }

        PlayerQuestionState playerState = playerStates[playerIndex];
        playerState.questionSet = selectedQuestionSet;
        playerState.availableQuestions = new List<Question>(selectedQuestionSet.questions);
        playerState.usedQuestions = new List<Question>();
        playerState.currentQuestionIndex = -1;

        if (useRandomOrder)
        {
            ShuffleQuestions(playerState.availableQuestions);
        }

        Debug.Log($"Initialized {playerState.availableQuestions.Count} questions for player {playerIndex}, theme: {theme}");
    }

    QuestionSet GetQuestionSetForTheme(GameManager.GameTheme theme)
    {
        return theme switch
        {
            GameManager.GameTheme.Organs => questionSetOrgans,
            GameManager.GameTheme.BodyParts => questionSetBodyParts,
            GameManager.GameTheme.Skeleton => questionSetSkeleton,
            GameManager.GameTheme.Muscles => questionSetMuscle,
            GameManager.GameTheme.Senses => questionSetSenses,
            GameManager.GameTheme.System => questionSetSystem,
            _ => questionSetOrgans,
        };
    }

    public Question GetNextQuestion(int playerIndex)
    {
        if (!playerStates.ContainsKey(playerIndex))
        {
            Debug.LogError($"Player {playerIndex} not initialized!");
            return null;
        }

        PlayerQuestionState playerState = playerStates[playerIndex];

        if (playerState.availableQuestions.Count == 0)
        {
            if (avoidRepeats && playerState.usedQuestions.Count > 0)
            {
                playerState.availableQuestions.AddRange(playerState.usedQuestions);
                playerState.usedQuestions.Clear();

                if (useRandomOrder)
                {
                    ShuffleQuestions(playerState.availableQuestions);
                }
            }
            else if (!avoidRepeats)
            {
                playerState.availableQuestions = new List<Question>(playerState.questionSet.questions);
                if (useRandomOrder)
                {
                    ShuffleQuestions(playerState.availableQuestions);
                }
            }
        }

        if (playerState.availableQuestions.Count == 0)
        {
            Debug.LogWarning($"No questions available for player {playerIndex}!");
            return null;
        }

        Question selectedQuestion = null;
        int selectedQuestionIndex = -1;

        bool isMultiplayer = GameManager.Instance.selectedPlayers > 1;

        if (isMultiplayer)
        {
            for (int i = 0; i < playerState.availableQuestions.Count; i++)
            {
                Question candidateQuestion = playerState.availableQuestions[i];

                int candidateIndex = -1;
                for (int j = 0; j < playerState.questionSet.questions.Length; j++)
                {
                    if (playerState.questionSet.questions[j] == candidateQuestion)
                    {
                        candidateIndex = j;
                        break;
                    }
                }
                if (candidateIndex != -1 && !currentlyUsedQuestionIndices.Contains(candidateIndex))
                {
                    selectedQuestion = candidateQuestion;
                    selectedQuestionIndex = candidateIndex;
                    playerState.availableQuestions.RemoveAt(i);
                    break;
                }
            }

            if (selectedQuestion == null && playerState.availableQuestions.Count > 0)
            {
                selectedQuestion = playerState.availableQuestions[0];
                playerState.availableQuestions.RemoveAt(0);

                for (int j = 0; j < playerState.questionSet.questions.Length; j++)
                {
                    if (playerState.questionSet.questions[j] == selectedQuestion)
                    {
                        selectedQuestionIndex = j;
                        break;
                    }
                }
            }
        }
        else
        {
            selectedQuestion = playerState.availableQuestions[0];
            playerState.availableQuestions.RemoveAt(0);

            for (int j = 0; j < playerState.questionSet.questions.Length; j++)
            {
                if (playerState.questionSet.questions[j] == selectedQuestion)
                {
                    selectedQuestionIndex = j;
                    break;
                }
            }
        }

        if (selectedQuestion == null)
        {
            Debug.LogWarning($"No question could be selected for player {playerIndex}!");
            return null;
        }

        playerState.currentQuestion = selectedQuestion;
        playerState.currentQuestionIndex = selectedQuestionIndex;
        playerState.currentQuestionText = selectedQuestion.GetRandomQuestionForLanguage(GameManager.Instance.currentLanguage);

        if (isMultiplayer && selectedQuestionIndex != -1)
        {
            currentlyUsedQuestionIndices.Add(selectedQuestionIndex);
        }

        if (avoidRepeats)
        {
            playerState.usedQuestions.Add(selectedQuestion);
        }

        return selectedQuestion;
    }

    public void FreeQuestionIndex(int playerIndex)
    {
        if (!playerStates.ContainsKey(playerIndex))
            return;

        PlayerQuestionState playerState = playerStates[playerIndex];

        if (playerState.currentQuestionIndex != -1)
        {
            currentlyUsedQuestionIndices.Remove(playerState.currentQuestionIndex);
        }
    }

    public string GetCurrentQuestionText(int playerIndex)
    {
        if (!playerStates.ContainsKey(playerIndex) || playerStates[playerIndex].currentQuestion == null)
            return "";

        return playerStates[playerIndex].currentQuestionText ?? "";
    }


    public string RegenerateCurrentQuestionText(int playerIndex)
    {
        if (!playerStates.ContainsKey(playerIndex) || playerStates[playerIndex].currentQuestion == null)
            return "";

        PlayerQuestionState playerState = playerStates[playerIndex];
        playerState.currentQuestionText = playerState.currentQuestion.GetRandomQuestionForLanguage(GameManager.Instance.currentLanguage);

        return playerState.currentQuestionText;
    }

    public bool IsCorrectAnswer(int playerIndex, string partName)
    {
        if (!playerStates.ContainsKey(playerIndex) || playerStates[playerIndex].currentQuestion == null)
            return false;

        Question currentQuestion = playerStates[playerIndex].currentQuestion;
        return currentQuestion.correctPartName.Equals(partName, System.StringComparison.OrdinalIgnoreCase);
    }

    public int GetCurrentQuestionIndex(int playerIndex)
    {
        if (!playerStates.ContainsKey(playerIndex))
            return -1;

        return playerStates[playerIndex].currentQuestionIndex;
    }

    public Question GetCurrentQuestion(int playerIndex)
    {
        if (!playerStates.ContainsKey(playerIndex))
            return null;

        return playerStates[playerIndex].currentQuestion;
    }

    void ShuffleQuestionsForPlayer(List<Question> questions, int playerIndex)
    {
        Random.State originalState = Random.state;
        Random.InitState(System.DateTime.Now.Millisecond + playerIndex * 1000 + Random.Range(0, 10000));

        for (int i = 0; i < questions.Count; i++)
        {
            Question temp = questions[i];
            int randomIndex = Random.Range(i, questions.Count);
            questions[i] = questions[randomIndex];
            questions[randomIndex] = temp;
        }

        Random.state = originalState;
    }

    void ShuffleQuestions(List<Question> questions)
    {
        for (int i = 0; i < questions.Count; i++)
        {
            Question temp = questions[i];
            int randomIndex = Random.Range(i, questions.Count);
            questions[i] = questions[randomIndex];
            questions[randomIndex] = temp;
        }
    }

    public QuestionSet GetCurrentQuestionSet(GameManager.GameTheme theme)
    {
        return GetQuestionSetForTheme(theme);
    }
}