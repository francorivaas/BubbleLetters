using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LassoDrawer : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private BoxCollider2D drawingArea;
    [SerializeField] private LetterSpawner letterSpawner;
    [SerializeField] private WordGameManager gameManager;

    [Header("Configuración del trazo")]
    [SerializeField, Min(0.01f)]
    private float minimumPointDistance = 0.08f;

    [SerializeField, Min(3)]
    private int minimumPointCount = 8;

    [SerializeField, Min(0.01f)]
    private float minimumPolygonArea = 0.12f;

    private readonly List<Vector3> drawingPoints =
        new List<Vector3>(128);

    private bool isDrawing;

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        if (lineRenderer != null)
        {
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = false;
            lineRenderer.positionCount = 0;
        }
    }

    private void Update()
    {
        if (Pointer.current == null)
        {
            if (isDrawing)
                CancelDrawing();

            return;
        }

        Vector2 screenPosition =
            Pointer.current.position.ReadValue();

        if (Pointer.current.press.wasPressedThisFrame)
        {
            BeginDrawing(screenPosition);
        }

        if (isDrawing && Pointer.current.press.isPressed)
        {
            ContinueDrawing(screenPosition);
        }

        if (isDrawing &&
            Pointer.current.press.wasReleasedThisFrame)
        {
            EndDrawing(screenPosition);
        }
    }

    private void BeginDrawing(Vector2 screenPosition)
    {
        if (gameManager == null ||
            !gameManager.IsRoundActive)
        {
            return;
        }

        Vector3 worldPosition =
            ScreenToWorldPosition(screenPosition);

        if (drawingArea != null &&
            !drawingArea.bounds.Contains(worldPosition))
        {
            return;
        }

        ClearDrawing();

        isDrawing = true;
        AddPoint(worldPosition, true);
    }

    private void ContinueDrawing(Vector2 screenPosition)
    {
        Vector3 worldPosition =
            ScreenToWorldPosition(screenPosition);

        AddPoint(worldPosition, false);
    }

    private void EndDrawing(Vector2 screenPosition)
    {
        Vector3 worldPosition =
            ScreenToWorldPosition(screenPosition);

        AddPoint(worldPosition, true);

        isDrawing = false;

        if (drawingPoints.Count < minimumPointCount)
        {
            gameManager.ReportInvalidLasso(
                "El trazo fue demasiado pequeño"
            );

            ClearDrawing();
            return;
        }

        float polygonArea = CalculatePolygonArea(drawingPoints);

        if (polygonArea < minimumPolygonArea)
        {
            gameManager.ReportInvalidLasso(
                "Tenés que rodear la letra"
            );

            ClearDrawing();
            return;
        }

        ResolveCapturedLetters();
        ClearDrawing();
    }

    private void ResolveCapturedLetters()
    {
        if (letterSpawner == null)
            return;

        List<FloatingLetter> enclosedLetters =
            new List<FloatingLetter>();

        IReadOnlyList<FloatingLetter> activeLetters =
            letterSpawner.ActiveLetters;

        for (int i = 0; i < activeLetters.Count; i++)
        {
            FloatingLetter letter = activeLetters[i];

            if (letter == null)
                continue;

            if (IsPointInsidePolygon(
                letter.WorldPosition,
                drawingPoints))
            {
                enclosedLetters.Add(letter);
            }
        }

        if (enclosedLetters.Count == 0)
        {
            gameManager.ReportInvalidLasso(
                "No atrapaste ninguna letra"
            );

            return;
        }

        if (enclosedLetters.Count > 1)
        {
            for (int i = 0; i < enclosedLetters.Count; i++)
                enclosedLetters[i].Reject();

            gameManager.ReportInvalidLasso(
                "Atrapá solamente una letra"
            );

            return;
        }

        FloatingLetter capturedLetter = enclosedLetters[0];

        bool wasNeeded = gameManager.TryCaptureLetter(
            capturedLetter.Character
        );

        if (wasNeeded)
        {
            capturedLetter.CaptureCorrectly();
        }
        else
        {
            capturedLetter.Reject();
        }
    }

    private void AddPoint(
        Vector3 worldPosition,
        bool forceAdd)
    {
        worldPosition.z = 0f;

        if (!forceAdd && drawingPoints.Count > 0)
        {
            Vector3 previousPoint =
                drawingPoints[drawingPoints.Count - 1];

            float distance = Vector3.Distance(
                previousPoint,
                worldPosition
            );

            if (distance < minimumPointDistance)
                return;
        }

        drawingPoints.Add(worldPosition);

        if (lineRenderer != null)
        {
            lineRenderer.positionCount =
                drawingPoints.Count;

            lineRenderer.SetPosition(
                drawingPoints.Count - 1,
                worldPosition
            );
        }
    }

    private Vector3 ScreenToWorldPosition(
        Vector2 screenPosition)
    {
        if (worldCamera == null)
            return Vector3.zero;

        float distanceToGameplayPlane =
            Mathf.Abs(worldCamera.transform.position.z);

        Vector3 screenPoint = new Vector3(
            screenPosition.x,
            screenPosition.y,
            distanceToGameplayPlane
        );

        Vector3 worldPosition =
            worldCamera.ScreenToWorldPoint(screenPoint);

        worldPosition.z = 0f;

        return worldPosition;
    }

    private void CancelDrawing()
    {
        isDrawing = false;
        ClearDrawing();
    }

    private void ClearDrawing()
    {
        drawingPoints.Clear();

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.loop = false;
        }
    }

    private static float CalculatePolygonArea(
        List<Vector3> polygon)
    {
        if (polygon == null || polygon.Count < 3)
            return 0f;

        float area = 0f;

        for (int i = 0; i < polygon.Count; i++)
        {
            int nextIndex = (i + 1) % polygon.Count;

            Vector3 currentPoint = polygon[i];
            Vector3 nextPoint = polygon[nextIndex];

            area +=
                currentPoint.x * nextPoint.y -
                nextPoint.x * currentPoint.y;
        }

        return Mathf.Abs(area) * 0.5f;
    }

    private static bool IsPointInsidePolygon(
        Vector2 point,
        List<Vector3> polygon)
    {
        bool isInside = false;
        int previousIndex = polygon.Count - 1;

        for (int currentIndex = 0;
             currentIndex < polygon.Count;
             currentIndex++)
        {
            Vector2 currentPoint =
                polygon[currentIndex];

            Vector2 previousPoint =
                polygon[previousIndex];

            bool crossesVerticalPosition =
                currentPoint.y > point.y !=
                previousPoint.y > point.y;

            if (crossesVerticalPosition)
            {
                float intersectionX =
                    (previousPoint.x - currentPoint.x) *
                    (point.y - currentPoint.y) /
                    (previousPoint.y - currentPoint.y) +
                    currentPoint.x;

                if (point.x < intersectionX)
                    isInside = !isInside;
            }

            previousIndex = currentIndex;
        }

        return isInside;
    }
}