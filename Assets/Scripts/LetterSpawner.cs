using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LetterSpawner : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private WordGameManager gameManager;
    [SerializeField] private FloatingLetter letterPrefab;
    [SerializeField] private BoxCollider2D playArea;

    [Header("Cantidad")]
    [SerializeField, Min(1)] private int initialLetterCount = 10;
    [SerializeField, Min(1)] private int maximumLetterCount = 16;
    [SerializeField, Min(0.1f)] private float spawnInterval = 0.7f;

    [Header("Movimiento")]
    [SerializeField] private float minimumSpeed = 0.8f;
    [SerializeField] private float maximumSpeed = 1.8f;
    [SerializeField] private float edgePadding = 0.4f;

    [Header("Distribución")]
    [SerializeField, Range(0f, 1f)]
    private float neededLetterProbability = 0.55f;

    [Tooltip("Después de esta cantidad de distractores, aparece obligatoriamente una letra necesaria.")]
    [SerializeField, Min(0)]
    private int maximumConsecutiveDistractors = 3;

    [SerializeField]
    private string availableAlphabet = "ABCDEFGHIJKLMNÑOPQRSTUVWXYZ";

    private readonly List<FloatingLetter> activeLetters =
        new List<FloatingLetter>();

    private Coroutine spawningCoroutine;
    private bool isSpawning;
    private int consecutiveDistractors;

    public IReadOnlyList<FloatingLetter> ActiveLetters => activeLetters;

    private void Awake()
    {
        if (playArea == null)
            return;

        if (playArea.offset != Vector2.zero)
        {
            Debug.LogWarning(
                $"El PlayArea tenía un Offset incorrecto: {playArea.offset}. " +
                "Se restableció automáticamente a (0, 0).",
                playArea
            );

            playArea.offset = Vector2.zero;
        }

        Debug.Log(
            $"PlayArea corregido | Centro: {playArea.bounds.center} | " +
            $"Mínimo: {playArea.bounds.min} | " +
            $"Máximo: {playArea.bounds.max}",
            playArea
        );
    }

    public void BeginSpawning()
    {
        StopSpawning();

        isSpawning = true;
        consecutiveDistractors = 0;

        int amountToCreate = Mathf.Min(
            initialLetterCount,
            maximumLetterCount
        );

        for (int i = 0; i < amountToCreate; i++)
            SpawnLetter();

        spawningCoroutine = StartCoroutine(SpawnRoutine());
    }

    public void StopSpawning()
    {
        isSpawning = false;

        if (spawningCoroutine != null)
        {
            StopCoroutine(spawningCoroutine);
            spawningCoroutine = null;
        }
    }

    public void ClearAllLetters()
    {
        StopSpawning();

        for (int i = activeLetters.Count - 1; i >= 0; i--)
        {
            if (activeLetters[i] != null)
                Destroy(activeLetters[i].gameObject);
        }

        activeLetters.Clear();
    }

    public void UnregisterLetter(FloatingLetter letter)
    {
        if (letter != null)
            activeLetters.Remove(letter);
    }

    private IEnumerator SpawnRoutine()
    {
        while (isSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (!isSpawning)
                yield break;

            if (activeLetters.Count < maximumLetterCount)
                SpawnLetter();
        }
    }

    private void SpawnLetter()
    {
        if (letterPrefab == null || playArea == null)
        {
            Debug.LogError(
                "LetterSpawner necesita un prefab y un Play Area."
            );

            return;
        }

        char selectedCharacter = ChooseCharacter();
        Vector2 spawnPosition = GetRandomPosition();

        Vector2 direction = Random.insideUnitCircle.normalized;

        if (direction.sqrMagnitude < 0.01f)
            direction = Vector2.right;

        float speed = Random.Range(minimumSpeed, maximumSpeed);
        Vector2 velocity = direction * speed;

        FloatingLetter newLetter = Instantiate(
            letterPrefab,
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

    private char ChooseCharacter()
    {
        List<char> remainingLetters = gameManager != null
            ? gameManager.GetRemainingLetters()
            : new List<char>();

        bool thereAreNeededLetters = remainingLetters.Count > 0;

        bool forceNeededLetter =
            maximumConsecutiveDistractors > 0 &&
            consecutiveDistractors >= maximumConsecutiveDistractors;

        bool randomlyChooseNeededLetter =
            Random.value <= neededLetterProbability;

        if (thereAreNeededLetters &&
            (forceNeededLetter || randomlyChooseNeededLetter))
        {
            consecutiveDistractors = 0;

            int randomIndex = Random.Range(
                0,
                remainingLetters.Count
            );

            return remainingLetters[randomIndex];
        }

        consecutiveDistractors++;

        if (string.IsNullOrEmpty(availableAlphabet))
            return 'A';

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

        float minimumX = bounds.min.x + edgePadding;
        float maximumX = bounds.max.x - edgePadding;

        float minimumY = bounds.min.y + edgePadding;
        float maximumY = bounds.max.y - edgePadding;

        return new Vector2(
            Random.Range(minimumX, maximumX),
            Random.Range(minimumY, maximumY)
        );
    }
}