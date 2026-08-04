using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class BubbleTapController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private WordGameManager gameManager;

    [Header("Detección")]
    [SerializeField] private LayerMask bubbleLayerMask;

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    private void Update()
    {
        if (Pointer.current == null)
            return;

        if (!Pointer.current.press.wasPressedThisFrame)
            return;

        if (gameManager == null || !gameManager.IsRoundActive)
            return;

        // Evita tocar una burbuja al pulsar un botón de la interfaz.
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Vector2 screenPosition =
            Pointer.current.position.ReadValue();

        TrySelectBubble(screenPosition);
    }

    private void TrySelectBubble(Vector2 screenPosition)
    {
        if (worldCamera == null)
            return;

        Vector3 worldPosition =
            worldCamera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x,
                    screenPosition.y,
                    Mathf.Abs(worldCamera.transform.position.z)
                )
            );

        Vector2 worldPoint = new Vector2(
            worldPosition.x,
            worldPosition.y
        );

        Collider2D hitCollider = Physics2D.OverlapPoint(
            worldPoint,
            bubbleLayerMask
        );

        if (hitCollider == null)
            return;

        FloatingLetter selectedLetter =
            hitCollider.GetComponentInParent<FloatingLetter>();

        if (selectedLetter == null)
            return;

        TryCaptureLetter(selectedLetter);
    }

    private void TryCaptureLetter(FloatingLetter selectedLetter)
    {
        bool wasNeeded = gameManager.TryCaptureLetter(
            selectedLetter.Character
        );

        if (wasNeeded)
        {
            selectedLetter.CaptureCorrectly();
        }
        else
        {
            selectedLetter.Reject();
        }
    }
}