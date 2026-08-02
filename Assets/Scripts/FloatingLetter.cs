using System.Collections;
using TMPro;
using UnityEngine;

public class FloatingLetter : MonoBehaviour
{
    [Header("Referencia")]
    [SerializeField] private TMP_Text letterText;

    [Header("Movimiento orgánico")]
    [SerializeField] private float directionChangeFrequency = 1.4f;
    [SerializeField] private float directionChangeStrength = 28f;
    [SerializeField] private float visualTiltAmount = 7f;
    [SerializeField] private float visualTiltFrequency = 2f;

    [Header("Respuesta visual")]
    [SerializeField] private float captureDuration = 0.18f;
    [SerializeField] private float rejectionDuration = 0.28f;
    [SerializeField] private float rejectionScale = 1.35f;

    private Bounds movementBounds;
    private Vector2 velocity;
    private float edgePadding;
    private float randomTimeOffset;

    private LetterSpawner ownerSpawner;

    private Vector3 originalScale;
    private Color originalColor;

    private bool initialized;
    private bool captured;

    private Coroutine rejectionCoroutine;

    public char Character { get; private set; }

    public Vector2 WorldPosition => transform.position;

    private void Awake()
    {
        originalScale = transform.localScale;

        if (letterText != null)
            originalColor = letterText.color;
    }

    public void Initialize(
        char character,
        Bounds bounds,
        Vector2 initialVelocity,
        float padding,
        LetterSpawner spawner)
    {
        Character = char.ToUpperInvariant(character);

        movementBounds = bounds;
        velocity = initialVelocity;
        edgePadding = padding;
        ownerSpawner = spawner;

        randomTimeOffset = Random.Range(0f, 100f);

        if (letterText != null)
            letterText.text = Character.ToString();

        initialized = true;
    }

    private void Update()
    {
        if (!initialized || captured)
            return;

        UpdateDirection();
        MoveAndBounce();
        UpdateVisualTilt();
    }

    public void CaptureCorrectly()
    {
        if (captured)
            return;

        captured = true;

        DetachFromSpawner();

        if (rejectionCoroutine != null)
            StopCoroutine(rejectionCoroutine);

        StartCoroutine(CaptureRoutine());
    }

    public void Reject()
    {
        if (captured)
            return;

        if (rejectionCoroutine != null)
            StopCoroutine(rejectionCoroutine);

        rejectionCoroutine = StartCoroutine(RejectionRoutine());
    }

    private void UpdateDirection()
    {
        float wave = Mathf.Sin(
            (Time.time + randomTimeOffset) *
            directionChangeFrequency
        );

        float rotationThisFrame =
            wave *
            directionChangeStrength *
            Time.deltaTime;

        Vector3 rotatedVelocity =
            Quaternion.Euler(0f, 0f, rotationThisFrame) *
            new Vector3(velocity.x, velocity.y, 0f);

        velocity = new Vector2(
            rotatedVelocity.x,
            rotatedVelocity.y
        );
    }

    private void MoveAndBounce()
    {
        Vector3 currentPosition = transform.position;

        Vector2 nextPosition =
            (Vector2)currentPosition +
            velocity * Time.deltaTime;

        float minimumX = movementBounds.min.x + edgePadding;
        float maximumX = movementBounds.max.x - edgePadding;
        float minimumY = movementBounds.min.y + edgePadding;
        float maximumY = movementBounds.max.y - edgePadding;

        if (nextPosition.x <= minimumX)
        {
            nextPosition.x = minimumX;
            velocity.x = Mathf.Abs(velocity.x);
        }
        else if (nextPosition.x >= maximumX)
        {
            nextPosition.x = maximumX;
            velocity.x = -Mathf.Abs(velocity.x);
        }

        if (nextPosition.y <= minimumY)
        {
            nextPosition.y = minimumY;
            velocity.y = Mathf.Abs(velocity.y);
        }
        else if (nextPosition.y >= maximumY)
        {
            nextPosition.y = maximumY;
            velocity.y = -Mathf.Abs(velocity.y);
        }

        transform.position = new Vector3(
            nextPosition.x,
            nextPosition.y,
            currentPosition.z
        );
    }

    private void UpdateVisualTilt()
    {
        float tilt = Mathf.Sin(
            (Time.time + randomTimeOffset) *
            visualTiltFrequency
        ) * visualTiltAmount;

        transform.rotation = Quaternion.Euler(0f, 0f, tilt);
    }

    private IEnumerator CaptureRoutine()
    {
        Vector3 startingScale = transform.localScale;
        float timer = 0f;

        while (timer < captureDuration)
        {
            timer += Time.deltaTime;

            float normalizedTime = Mathf.Clamp01(
                timer / captureDuration
            );

            transform.localScale = Vector3.Lerp(
                startingScale,
                Vector3.zero,
                normalizedTime
            );

            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator RejectionRoutine()
    {
        transform.localScale = originalScale;

        if (letterText != null)
            letterText.color = originalColor;

        float timer = 0f;

        while (timer < rejectionDuration)
        {
            timer += Time.deltaTime;

            float normalizedTime = Mathf.Clamp01(
                timer / rejectionDuration
            );

            float pulse = Mathf.Sin(
                normalizedTime * Mathf.PI
            );

            transform.localScale = Vector3.Lerp(
                originalScale,
                originalScale * rejectionScale,
                pulse
            );

            if (letterText != null)
            {
                letterText.color = Color.Lerp(
                    originalColor,
                    Color.red,
                    pulse
                );
            }

            yield return null;
        }

        transform.localScale = originalScale;

        if (letterText != null)
            letterText.color = originalColor;

        rejectionCoroutine = null;
    }

    private void DetachFromSpawner()
    {
        if (ownerSpawner == null)
            return;

        ownerSpawner.UnregisterLetter(this);
        ownerSpawner = null;
    }

    private void OnDestroy()
    {
        DetachFromSpawner();
    }
}