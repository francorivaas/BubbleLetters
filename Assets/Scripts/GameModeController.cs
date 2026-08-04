using UnityEngine;

public enum GameplayMode
{
    Lasso,
    BubbleTap
}

public class GameModeController : MonoBehaviour
{
    [Header("Modo de juego")]
    [SerializeField] private GameplayMode gameplayMode;

    [Header("Controladores")]
    [SerializeField] private LassoDrawer lassoDrawer;
    [SerializeField] private BubbleTapController bubbleTapController;

    [Header("Generador")]
    [SerializeField] private LetterSpawner letterSpawner;

    public GameplayMode CurrentMode => gameplayMode;

    private void Awake()
    {
        ApplyGameplayMode();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            ApplyGameplayMode();
    }

    public void SetGameplayMode(GameplayMode newMode)
    {
        gameplayMode = newMode;
        ApplyGameplayMode();
    }

    private void ApplyGameplayMode()
    {
        bool useLasso = gameplayMode == GameplayMode.Lasso;
        bool useBubbleTap = gameplayMode == GameplayMode.BubbleTap;

        if (lassoDrawer != null)
            lassoDrawer.enabled = useLasso;

        if (bubbleTapController != null)
            bubbleTapController.enabled = useBubbleTap;

        if (letterSpawner != null)
            letterSpawner.SetGameplayMode(gameplayMode);
    }
}