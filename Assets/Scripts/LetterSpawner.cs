using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Modos de interacción disponibles.
/// No vuelvas a declarar este enum en GameModeController.
/// </summary>

public class LetterSpawner : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private WordGameManager gameManager;
    [SerializeField] private BoxCollider2D playArea;

    [Header("Modo de juego")]
    [SerializeField] private GameplayMode gameplayMode = GameplayMode.Lasso;

    [Tooltip("Prefab utilizado en el modo donde el jugador rodea las letras.")]
    [FormerlySerializedAs("letterPrefab")]
    [SerializeField] private FloatingLetter lassoLetterPrefab;

    [Tooltip("Prefab utilizado en el modo donde el jugador toca burbujas.")]
    [SerializeField] private FloatingLetter bubbleLetterPrefab;

    [Header("Cantidad")]
    [SerializeField, Min(1)] private int initialLetterCount = 10;
    [SerializeField, Min(1)] private int maximumLetterCount = 16;
    [SerializeField, Min(0.1f)] private float spawnInterval = 0.7f;

    [Header("Movimiento")]
    [SerializeField, Min(0f)] private float minimumSpeed = 0.8f;
    [SerializeField, Min(0f)] private float maximumSpeed = 1.8f;
    [SerializeField, Min(0f)] private float edgePadding = 0.4f;

    [Header("Distribución")]
    [SerializeField, Range(0f, 1f)]
    private float neededLetterProbability = 0.55f;

    [Tooltip(
        "Después de esta cantidad de distractores, " +
        "aparece obligatoriamente una letra necesaria."
    )]
    [SerializeField, Min(0)]
    private int maximumConsecutiveDistractors = 3;

    [Tooltip("Letras que pueden utilizarse como distractores.")]
    [SerializeField]
    private string availableAlphabet = "ABCDEFGHIJKLMNÑOPQRSTUVWXYZ";

    private readonly List<FloatingLetter> activeLetters =
        new List<FloatingLetter>();

    private Coroutine spawningCoroutine;
    private bool isSpawning;
    private int consecutiveDistractors;

    public IReadOnlyList<FloatingLetter> ActiveLetters => activeLetters;

    public GameplayMode CurrentGameplayMode => gameplayMode;

    private void Awake()
    {
        ValidatePlayArea();
    }

    private void OnValidate()
    {
        initialLetterCount = Mathf.Max(1, initialLetterCount);
        maximumLetterCount = Mathf.Max(1, maximumLetterCount);
        spawnInterval = Mathf.Max(0.1f, spawnInterval);

        minimumSpeed = Mathf.Max(0f, minimumSpeed);
        maximumSpeed = Mathf.Max(minimumSpeed, maximumSpeed);

        edgePadding = Mathf.Max(0f, edgePadding);
    }

    /// <summary>
    /// Cambia el prefab que utilizará el generador.
    /// Si el modo cambia durante una ronda activa, reemplaza las letras
    /// existentes por letras correspondientes al nuevo modo.
    /// </summary>
    public void SetGameplayMode(GameplayMode newMode)
    {
        if (gameplayMode == newMode)
            return;

        gameplayMode = newMode;

        bool shouldRestartSpawning =
            Application.isPlaying && isSpawning;

        if (!shouldRestartSpawning)
            return;

        ClearAllLetters();
        BeginSpawning();
    }

    public void BeginSpawning()
    {
        StopSpawning();

        if (!ValidateSpawnConfiguration())
            return;

        isSpawning = true;
        consecutiveDistractors = 0;

        int amountToCreate = Mathf.Min(
            initialLetterCount,
            maximumLetterCount
        );

        for (int i = 0; i < amountToCreate; i++)
        {
            SpawnLetter();
        }

        spawningCoroutine = StartCoroutine(SpawnRoutine());
    }

    public void StopSpawning()
    {
        isSpawning = false;

        if (spawningCoroutine == null)
            return;

        StopCoroutine(spawningCoroutine);
        spawningCoroutine = null;
    }

    public void ClearAllLetters()
    {
        StopSpawning();

        for (int i = activeLetters.Count - 1; i >= 0; i--)
        {
            FloatingLetter letter = activeLetters[i];

            if (letter != null)
                Destroy(letter.gameObject);
        }

        activeLetters.Clear();
    }

    public void UnregisterLetter(FloatingLetter letter)
    {
        if (letter == null)
            return;

        activeLetters.Remove(letter);
    }

    private IEnumerator SpawnRoutine()
    {
        while (isSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (!isSpawning)
                yield break;

            RemoveMissingReferences();

            if (activeLetters.Count < maximumLetterCount)
                SpawnLetter();
        }
    }

    private void SpawnLetter()
    {
        FloatingLetter selectedPrefab =
            GetPrefabForMode(gameplayMode);

        if (selectedPrefab == null)
        {
            Debug.LogError(
                $"No hay un prefab asignado para el modo {gameplayMode}.",
                this
            );

            return;
        }

        if (playArea == null)
        {
            Debug.LogError(
                "LetterSpawner no tiene un PlayArea asignado.",
                this
            );

            return;
        }

        char selectedCharacter = ChooseCharacter();
        Vector2 spawnPosition = GetRandomPosition();

        Vector2 direction = Random.insideUnitCircle.normalized;

        if (direction.sqrMagnitude < 0.01f)
            direction = Vector2.right;

        float speed = Random.Range(
            minimumSpeed,
            maximumSpeed
        );

        Vector2 velocity = direction * speed;

        FloatingLetter newLetter = Instantiate(
            selectedPrefab,
            spawnPosition,
            Quaternion.identity,
            transform
        );

        newLetter.Initialize(
            selectedCharacter,
            playArea.bounds,
            velocity,
            edgePadding,
            this
        );

        activeLetters.Add(newLetter);
    }

    private FloatingLetter GetPrefabForMode(
        GameplayMode selectedMode)
    {
        switch (selectedMode)
        {
            case GameplayMode.BubbleTap:
                return bubbleLetterPrefab;

            case GameplayMode.Lasso:
            default:
                return lassoLetterPrefab;
        }
    }

    private char ChooseCharacter()
    {
        List<char> remainingLetters =
            gameManager != null
                ? gameManager.GetRemainingLetters()
                : new List<char>();

        bool thereAreNeededLetters =
            remainingLetters.Count > 0;

        bool forceNeededLetter =
            maximumConsecutiveDistractors > 0 &&
            consecutiveDistractors >=
            maximumConsecutiveDistractors;

        bool randomlyChooseNeededLetter =
            Random.value <= neededLetterProbability;

        if (thereAreNeededLetters &&
            (forceNeededLetter ||
             randomlyChooseNeededLetter))
        {
            consecutiveDistractors = 0;

            int randomIndex = Random.Range(
                0,
                remainingLetters.Count
            );

            return char.ToUpperInvariant(
                remainingLetters[randomIndex]
            );
        }

        consecutiveDistractors++;

        return ChooseDistractorCharacter(
            remainingLetters
        );
    }

    /// <summary>
    /// Intenta evitar que una letra generada como distractor coincida
    /// accidentalmente con una letra que todavía necesita el jugador.
    /// </summary>
    private char ChooseDistractorCharacter(
        List<char> remainingLetters)
    {
        if (string.IsNullOrWhiteSpace(availableAlphabet))
            return 'A';

        List<char> validDistractors = new List<char>();

        foreach (char character in availableAlphabet)
        {
            char normalizedCharacter =
                char.ToUpperInvariant(character);

            if (char.IsWhiteSpace(normalizedCharacter))
                continue;

            bool isCurrentlyNeeded =
                remainingLetters.Contains(
                    normalizedCharacter
                );

            if (!isCurrentlyNeeded)
            {
                validDistractors.Add(
                    normalizedCharacter
                );
            }
        }

        if (validDistractors.Count > 0)
        {
            int randomIndex = Random.Range(
                0,
                validDistractors.Count
            );

            return validDistractors[randomIndex];
        }

        int alphabetIndex = Random.Range(
            0,
            availableAlphabet.Length
        );

        return char.ToUpperInvariant(
            availableAlphabet[alphabetIndex]
        );
    }

    private Vector2 GetRandomPosition()
    {
        Bounds bounds = playArea.bounds;

        // Evita que un padding demasiado grande produzca
        // límites invertidos.
        float safeHorizontalPadding = Mathf.Min(
            edgePadding,
            bounds.extents.x * 0.9f
        );

        float safeVerticalPadding = Mathf.Min(
            edgePadding,
            bounds.extents.y * 0.9f
        );

        float minimumX =
            bounds.min.x + safeHorizontalPadding;

        float maximumX =
            bounds.max.x - safeHorizontalPadding;

        float minimumY =
            bounds.min.y + safeVerticalPadding;

        float maximumY =
            bounds.max.y - safeVerticalPadding;

        return new Vector2(
            Random.Range(minimumX, maximumX),
            Random.Range(minimumY, maximumY)
        );
    }

    private void RemoveMissingReferences()
    {
        for (int i = activeLetters.Count - 1; i >= 0; i--)
        {
            if (activeLetters[i] == null)
                activeLetters.RemoveAt(i);
        }
    }

    private bool ValidateSpawnConfiguration()
    {
        if (playArea == null)
        {
            Debug.LogError(
                "LetterSpawner necesita un PlayArea.",
                this
            );

            return false;
        }

        FloatingLetter selectedPrefab =
            GetPrefabForMode(gameplayMode);

        if (selectedPrefab == null)
        {
            Debug.LogError(
                $"Falta asignar el prefab del modo {gameplayMode}.",
                this
            );

            return false;
        }

        if (gameManager == null)
        {
            Debug.LogWarning(
                "LetterSpawner no tiene un WordGameManager. " +
                "Solo generará letras aleatorias.",
                this
            );
        }

        return true;
    }

    private void ValidatePlayArea()
    {
        if (playArea == null)
            return;

        if (playArea.offset != Vector2.zero)
        {
            Debug.LogWarning(
                $"El PlayArea tenía un Offset incorrecto: " +
                $"{playArea.offset}. Se restableció a (0, 0).",
                playArea
            );

            playArea.offset = Vector2.zero;
        }

        Debug.Log(
            $"PlayArea | Centro: {playArea.bounds.center} | " +
            $"Mínimo: {playArea.bounds.min} | " +
            $"Máximo: {playArea.bounds.max}",
            playArea
        );
    }
}