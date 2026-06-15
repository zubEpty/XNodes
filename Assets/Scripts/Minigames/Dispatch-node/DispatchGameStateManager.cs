using System;
using UnityEngine;

namespace Dispatch.Gameplay
{
public class DispatchGameStateManager : MonoBehaviour
{
    public enum DispatchGameState
    {
        Playing,
        Paused
    }

    public static DispatchGameStateManager Instance { get; private set; }

    public event Action<DispatchGameState> OnGameStateChanged;

    [SerializeField] private PlayerController currentPlayer;
    [SerializeField] private DispatchGameState currentState = DispatchGameState.Paused;
    [SerializeField] private bool gameplayResumeLocked;

    [Header("Runtime Status")]
    [SerializeField] private bool isPlayingInInspector;
    [SerializeField] private bool isPausedInInspector;
    [SerializeField] private bool isResumeLockedInInspector;
    [SerializeField] private bool playerMovementEnabledInInspector;
    [SerializeField] private bool playerRotateBodyOnMoveInInspector;

    public PlayerController CurrentPlayer => currentPlayer;
    public DispatchGameState CurrentState => currentState;
    public bool IsPaused => currentState == DispatchGameState.Paused;
    public bool IsPlaying => currentState == DispatchGameState.Playing;
    public bool IsResumeLocked => gameplayResumeLocked;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple DispatchGameStateManager instances found. Keeping the first instance.");
            return;
        }

        Instance = this;
        ApplyStateToCurrentPlayer();
        RefreshInspectorStatus();
    }

#if UNITY_EDITOR
    void Update()
    {
        RefreshInspectorStatus();
    }
#endif

    void OnValidate()
    {
        RefreshInspectorStatus();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RegisterPlayer(PlayerController player)
    {
        currentPlayer = player;
        ApplyStateToCurrentPlayer();
        RefreshInspectorStatus();
    }

    public void UnregisterPlayer(PlayerController player)
    {
        if (currentPlayer == player)
            currentPlayer = null;

        RefreshInspectorStatus();
    }

    public void PauseGameplay()
    {
        SetState(DispatchGameState.Paused);
    }

    public void ResumeGameplay()
    {
        if (gameplayResumeLocked)
            return;

        SetState(DispatchGameState.Playing);
    }

    public void StartGame()
    {
        UnlockGameplayResume();
        ResumeGameplay();
    }

    public void InitializeGame()
    {
        StartGame();
    }

    public void LockGameplayResume()
    {
        gameplayResumeLocked = true;
        SetState(DispatchGameState.Paused);
        RefreshInspectorStatus();
    }

    public void UnlockGameplayResume()
    {
        if (!gameplayResumeLocked)
            return;

        gameplayResumeLocked = false;
        RefreshInspectorStatus();
    }

    public void SetState(DispatchGameState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
        ApplyStateToCurrentPlayer();
        RefreshInspectorStatus();
        OnGameStateChanged?.Invoke(currentState);
    }

    private void ApplyStateToCurrentPlayer()
    {
        if (currentPlayer == null)
            return;

        currentPlayer.SetGameplayControlEnabled(currentState == DispatchGameState.Playing);
        RefreshInspectorStatus();
    }

    private void RefreshInspectorStatus()
    {
        isPlayingInInspector = currentState == DispatchGameState.Playing;
        isPausedInInspector = currentState == DispatchGameState.Paused;
        isResumeLockedInInspector = gameplayResumeLocked;
        playerMovementEnabledInInspector = currentPlayer != null && currentPlayer.movementEnabled;
        playerRotateBodyOnMoveInInspector = currentPlayer != null && currentPlayer.rotateBodyOnMove;
    }
}
}
