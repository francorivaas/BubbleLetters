using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

[Serializable]
public class WordRoundData
{
    [TextArea]
    public string clue = "Fruta amarilla:";

    public string answer = "BANANA";

    [Tooltip("Usa _ para las letras ocultas. Los espacios se ignoran.")]
    public string initialPattern = "B_N__A";
}

public class WordGameManager : MonoBehaviour
{
    [Header("Contenido")]
    [SerializeField]
    private List<WordRoundData> rounds =
        new List<WordRoundData>();

    [Header("Interfaz")]
    [SerializeField] private TMP_Text clueText;
    [SerializeField] private TMP_Text patternText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text statusText;

    [Header("Sistemas")]
    [SerializeField] private LetterSpawner letterSpawner;

    private int currentRoundIndex;
    private string normalizedAnswer;
    private char[] currentPattern;

    private bool roundActive;
    private float elapsedTime;

    public bool IsRoundActive => roundActive;

    private void Start()
    {
        if (rounds.Count == 0)
        {
            Debug.LogError("WordGameManager no tiene ninguna ronda configurada.");
            enabled = false;
            return;
        }

        StartRound(0);
    }

    private void Update()
    {
        if (!roundActive)
            return;

        elapsedTime += Time.deltaTime;
        UpdateTimerUI();
    }

    public void StartRound(int roundIndex)
    {
        if (roundIndex < 0 || roundIndex >= rounds.Count)
        {
            Debug.LogError($"Índice de ronda inválido: {roundIndex}");
            return;
        }

        currentRoundIndex = roundIndex;

        WordRoundData round = rounds[currentRoundIndex];

        normalizedAnswer = NormalizeText(round.answer);
        string normalizedInitialPattern = NormalizeText(round.initialPattern);

        if (normalizedAnswer.Length == 0)
        {
            Debug.LogError("La respuesta de la ronda está vacía.");
            return;
        }

        if (normalizedAnswer.Length != normalizedInitialPattern.Length)
        {
            Debug.LogError(
                $"La respuesta '{normalizedAnswer}' tiene " +
                $"{normalizedAnswer.Length} caracteres, pero el patrón " +
                $"'{normalizedInitialPattern}' tiene " +
                $"{normalizedInitialPattern.Length}."
            );

            return;
        }

        currentPattern = new char[normalizedAnswer.Length];

        for (int i = 0; i < normalizedAnswer.Length; i++)
        {
            if (normalizedInitialPattern[i] == '_')
            {
                currentPattern[i] = '_';
            }
            else
            {
                // Se usa la letra real de la respuesta para evitar
                // inconsistencias entre patrón y respuesta.
                currentPattern[i] = normalizedAnswer[i];
            }
        }

        elapsedTime = 0f;
        roundActive = true;

        if (clueText != null)
            clueText.text = round.clue;

        if (statusText != null)
            statusText.text = "Rodeá una letra necesaria";

        UpdatePatternUI();
        UpdateTimerUI();

        if (letterSpawner != null)
        {
            letterSpawner.ClearAllLetters();
            letterSpawner.BeginSpawning();
        }
    }

    /// <summary>
    /// Intenta incorporar una letra a la palabra.
    /// Devuelve true cuando todavía hacía falta.
    /// </summary>
    public bool TryCaptureLetter(char capturedLetter)
    {
        if (!roundActive || currentPattern == null)
            return false;

        char normalizedLetter = char.ToUpperInvariant(capturedLetter);

        // Busca la primera posición vacía que necesite esta letra.
        for (int i = 0; i < normalizedAnswer.Length; i++)
        {
            bool slotIsEmpty = currentPattern[i] == '_';
            bool letterMatches = normalizedAnswer[i] == normalizedLetter;

            if (!slotIsEmpty || !letterMatches)
                continue;

            currentPattern[i] = normalizedLetter;

            UpdatePatternUI();

            if (IsWordComplete())
            {
                CompleteRound();
            }
            else if (statusText != null)
            {
                statusText.text = $"¡Bien! Capturaste {normalizedLetter}";
            }

            return true;
        }

        if (statusText != null)
            statusText.text = $"La letra {normalizedLetter} no hace falta";

        return false;
    }

    public List<char> GetRemainingLetters()
    {
        List<char> remainingLetters = new List<char>();

        if (currentPattern == null || string.IsNullOrEmpty(normalizedAnswer))
            return remainingLetters;

        for (int i = 0; i < currentPattern.Length; i++)
        {
            if (currentPattern[i] == '_')
                remainingLetters.Add(normalizedAnswer[i]);
        }

        return remainingLetters;
    }

    public void ReportInvalidLasso(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    public void NextRound()
    {
        if (rounds.Count == 0)
            return;

        int nextIndex = (currentRoundIndex + 1) % rounds.Count;
        StartRound(nextIndex);
    }

    private void CompleteRound()
    {
        roundActive = false;

        if (letterSpawner != null)
            letterSpawner.StopSpawning();

        if (statusText != null)
        {
            statusText.text =
                $"¡Palabra completada en {elapsedTime:0.00} segundos!";
        }
    }

    private bool IsWordComplete()
    {
        for (int i = 0; i < currentPattern.Length; i++)
        {
            if (currentPattern[i] == '_')
                return false;
        }

        return true;
    }

    private void UpdatePatternUI()
    {
        if (patternText == null || currentPattern == null)
            return;

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < currentPattern.Length; i++)
        {
            builder.Append(currentPattern[i]);

            if (i < currentPattern.Length - 1)
                builder.Append("  ");
        }

        patternText.text = builder.ToString();
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
            timerText.text = elapsedTime.ToString("0.00");
    }

    private static string NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        StringBuilder builder = new StringBuilder();

        foreach (char character in value)
        {
            if (char.IsWhiteSpace(character))
                continue;

            builder.Append(char.ToUpperInvariant(character));
        }

        return builder.ToString();
    }
}