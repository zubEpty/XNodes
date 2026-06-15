using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Outsiders.Auditory;

namespace Dispatch.Gameplay
{
public class SequenceUnlockGate : MonoBehaviour
{
    private const int RequiredSequenceLength = 4;

    public event Action<Direction[]> OnSequenceGenerated;
    public event Action<Direction[]> OnSequenceRevealed;
    public event Action<Direction[], int> OnSequenceProgressChanged;
    public event Action<Direction[], int> OnSequenceFailedWithInput;
    public event Action OnSequenceProgressReset;

    [Header("References")]
    public SwipeInputHandler inputHandler;
    public PlayerController player;
    public DispatchGameStateManager gameStateManager;
    public PathNode sourceNode;
    public PathNode targetNode;

    [Header("Puzzle")]
    [SerializeField] private Direction[] requiredSequence;
    [SerializeField] private bool generateSequenceOnAwake = true;
    public bool requireRevealBeforeEntry = true;
    public float movementRestoreDelay = 0.15f;
    [SerializeField] private bool autoResumeGameplayAfterSequenceEnds = false;
    [SerializeField] private bool retryAfterWrongSequence = true;
    [SerializeField] private float wrongSequenceRetryDelay = 0.6f;

    [Header("Audio")]
    [SerializeField] private AudioEventSO sequenceInputAudio;
    [SerializeField] private AudioEventSO correctSequenceAudio;
    [SerializeField] private AudioEventSO wrongSequenceAudio;

    [Header("State")]
    [SerializeField] private bool sequenceRevealed;
    [SerializeField] private bool isListeningForInput;
    [SerializeField] private bool isUnlocked;

    [Header("Events")]
    public UnityEvent onSequenceRevealed;
    public UnityEvent onEntryStarted;
    public UnityEvent onSequenceSolved;
    public UnityEvent onSequenceFailed;

    private readonly List<Direction> enteredSequence = new List<Direction>();
    private CancellationTokenSource restoreMovementCts;
    private CancellationTokenSource retryAfterFailureCts;

    public bool IsSequenceRevealed => sequenceRevealed;

    void Awake()
    {
        if (generateSequenceOnAwake)
            GenerateNewSequence();
        else
            EnforceSequenceLength();
    }

    void OnEnable()
    {
        ResolveRuntimeReferences();

        if (inputHandler == null)
            return;

        inputHandler.OnSwipe += HandleDirectionInput;
        inputHandler.OnMove += HandleDirectionInput;
    }

    void OnDisable()
    {
        StopDelayedActions();

        if (inputHandler == null)
            return;

        inputHandler.OnSwipe -= HandleDirectionInput;
        inputHandler.OnMove -= HandleDirectionInput;
    }

    public void RevealSequence()
    {
        sequenceRevealed = true;
        OnSequenceRevealed?.Invoke(GetRequiredSequence());
        onSequenceRevealed?.Invoke();

        Debug.Log($"Sequence revealed: {GetSequenceText()}");
    }

    public void GenerateNewSequence()
    {
        requiredSequence = new Direction[RequiredSequenceLength];

        for (int i = 0; i < requiredSequence.Length; i++)
            requiredSequence[i] = GetRandomDirection();

        sequenceRevealed = false;
        isListeningForInput = false;
        isUnlocked = false;
        enteredSequence.Clear();
        OnSequenceGenerated?.Invoke(GetRequiredSequence());
        NotifySequenceReset();
    }

    public Direction[] GetRequiredSequence()
    {
        if (requiredSequence == null)
            return Array.Empty<Direction>();

        Direction[] sequenceCopy = new Direction[requiredSequence.Length];
        Array.Copy(requiredSequence, sequenceCopy, requiredSequence.Length);
        return sequenceCopy;
    }

    public void BeginSequenceEntry()
    {
        ResolveRuntimeReferences();

        if (isUnlocked)
            return;

        if (requireRevealBeforeEntry && !sequenceRevealed)
        {
            Debug.Log("Reveal the sequence before entering it.");
            return;
        }

        if (inputHandler == null || player == null)
        {
            Debug.LogWarning("SequenceUnlockGate is missing required references.");
            return;
        }

        enteredSequence.Clear();
        isListeningForInput = true;
        StopRetryAfterFailure();
        PauseGameplay();
        NotifySequenceReset();
        onEntryStarted?.Invoke();

        Debug.Log("Sequence entry started.");
    }

    public void CancelSequenceEntry()
    {
        if (!isListeningForInput)
            return;

        ResetSequenceEntrySilently();
        RestoreGameplayAfterDelayIfNeeded();
    }

    public void ExitSequenceEntry()
    {
        ResetSequenceEntrySilently();
    }

    public string GetSequenceText()
    {
        if (requiredSequence == null || requiredSequence.Length == 0)
            return string.Empty;

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < requiredSequence.Length; i++)
        {
            if (i > 0)
                builder.Append(", ");

            builder.Append(requiredSequence[i]);
        }

        return builder.ToString();
    }

    private void HandleDirectionInput(Direction direction)
    {
        if (!isListeningForInput || isUnlocked)
            return;

        PlayUiAudio(sequenceInputAudio);
        enteredSequence.Add(direction);
        NotifySequenceProgressChanged();

        if (requiredSequence == null || requiredSequence.Length == 0)
        {
            FailSequence();
            return;
        }

        if (enteredSequence.Count < requiredSequence.Length)
            return;

        if (IsEnteredSequenceCorrect())
            SolveSequence();
        else
            FailSequence();
    }

    public void ClearSequence()
    {
        enteredSequence.Clear();
    }

    private void SolveSequence()
    {
        isUnlocked = true;
        isListeningForInput = false;
        StopRetryAfterFailure();
        NotifySequenceProgressChanged();

        if (sourceNode != null && targetNode != null)
            sourceNode.SetConnectionState(targetNode, true);
        
        RestoreGameplayAfterDelayIfNeeded();

        onSequenceSolved?.Invoke();
        PlayUiAudio(correctSequenceAudio);
        DispatchGameStateManager.Instance.ResumeGameplay();
        Debug.Log("Correct sequence entered. Connection unlocked.");
    }

    private void FailSequence()
    {
        isListeningForInput = false;
        NotifySequenceFailedWithInput();
        enteredSequence.Clear();

        onSequenceFailed?.Invoke();
        PlayUiAudio(wrongSequenceAudio);
        Debug.Log("Wrong sequence entered.");

        if (retryAfterWrongSequence)
        {
            StopRetryAfterFailure();
            StartRetryAfterFailure();
            return;
        }

        NotifySequenceReset();
        RestoreGameplayAfterDelayIfNeeded();
    }

    private void ResetSequenceEntrySilently()
    {
        StopRetryAfterFailure();
        isListeningForInput = false;
        enteredSequence.Clear();
        NotifySequenceReset();
    }

    private void RestoreGameplayAfterDelayIfNeeded()
    {
        if (!autoResumeGameplayAfterSequenceEnds)
            return;

        StopRestoreMovement();
        restoreMovementCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        RestoreMovementAsync(restoreMovementCts).Forget();
    }

    public void ResumeGameplay()
    {
        StopRestoreMovement();
        StopRetryAfterFailure();

        DispatchGameStateManager manager = GetGameStateManager();

        if (manager != null)
        {
            manager.ResumeGameplay();
            return;
        }

        if (player != null)
            player.SetGameplayControlEnabled(true);
    }

    private async UniTaskVoid RestoreMovementAsync(CancellationTokenSource routineCts)
    {
        try
        {
            if (movementRestoreDelay > 0f)
                await UniTask.Delay((int)(movementRestoreDelay * 1000f), cancellationToken: routineCts.Token);
            else
                await UniTask.Yield(routineCts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        CompleteDelayedAction(ref restoreMovementCts, routineCts);
        ResumeGameplay();
    }

    private void StartRetryAfterFailure()
    {
        StopRetryAfterFailure();
        retryAfterFailureCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        RetryAfterFailureAsync(retryAfterFailureCts).Forget();
    }

    private async UniTaskVoid RetryAfterFailureAsync(CancellationTokenSource routineCts)
    {
        try
        {
            if (wrongSequenceRetryDelay > 0f)
                await UniTask.Delay((int)(wrongSequenceRetryDelay * 1000f), cancellationToken: routineCts.Token);
            else
                await UniTask.Yield(routineCts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        NotifySequenceReset();
        isListeningForInput = true;
        CompleteDelayedAction(ref retryAfterFailureCts, routineCts);
    }

    private void StopRetryAfterFailure()
    {
        CancelDelayedAction(ref retryAfterFailureCts);
    }

    private void StopRestoreMovement()
    {
        CancelDelayedAction(ref restoreMovementCts);
    }

    private void StopDelayedActions()
    {
        StopRetryAfterFailure();
        StopRestoreMovement();
    }

    private static void CancelDelayedAction(ref CancellationTokenSource cts)
    {
        if (cts == null)
            return;

        cts.Cancel();
        cts.Dispose();
        cts = null;
    }

    private static void CompleteDelayedAction(ref CancellationTokenSource currentCts, CancellationTokenSource completedCts)
    {
        if (currentCts != completedCts)
            return;

        currentCts.Dispose();
        currentCts = null;
    }

    private void PauseGameplay()
    {
        DispatchGameStateManager manager = GetGameStateManager();

        if (manager != null)
        {
            if (player != null)
                manager.RegisterPlayer(player);

            manager.PauseGameplay();
            return;
        }

        if (player != null)
            player.SetGameplayControlEnabled(false);
    }

    private DispatchGameStateManager GetGameStateManager()
    {
        ResolveRuntimeReferences();

        if (gameStateManager != null)
            return gameStateManager;

        if (GameBootstrap.Instance != null && GameBootstrap.Instance.gameStateManager != null)
            return GameBootstrap.Instance.gameStateManager;

        return DispatchGameStateManager.Instance;
    }

    private void ResolveRuntimeReferences()
    {
        if (GameBootstrap.Instance != null)
        {
            if (inputHandler == null)
                inputHandler = GameBootstrap.Instance.input;

            if (player == null)
                player = GameBootstrap.Instance.player;

            if (gameStateManager == null)
                gameStateManager = GameBootstrap.Instance.gameStateManager;
        }

        if (inputHandler == null)
            inputHandler = FindFirstObjectByType<SwipeInputHandler>();

        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (gameStateManager == null)
            gameStateManager = DispatchGameStateManager.Instance;

        if (gameStateManager == null)
            gameStateManager = FindFirstObjectByType<DispatchGameStateManager>();
    }

    private void EnforceSequenceLength()
    {
        if (requiredSequence == null || requiredSequence.Length == RequiredSequenceLength)
            return;

        Direction[] resizedSequence = new Direction[RequiredSequenceLength];
        int copyLength = Mathf.Min(requiredSequence.Length, resizedSequence.Length);

        for (int i = 0; i < copyLength; i++)
            resizedSequence[i] = requiredSequence[i];

        for (int i = copyLength; i < resizedSequence.Length; i++)
            resizedSequence[i] = GetRandomDirection();

        requiredSequence = resizedSequence;
    }

    private Direction GetRandomDirection()
    {
        return (Direction)UnityEngine.Random.Range(0, 4);
    }

    private void NotifySequenceProgressChanged()
    {
        OnSequenceProgressChanged?.Invoke(enteredSequence.ToArray(), enteredSequence.Count);
    }

    private bool IsEnteredSequenceCorrect()
    {
        if (enteredSequence.Count != requiredSequence.Length)
            return false;

        for (int i = 0; i < requiredSequence.Length; i++)
        {
            if (enteredSequence[i] != requiredSequence[i])
                return false;
        }

        return true;
    }

    private void NotifySequenceFailedWithInput()
    {
        OnSequenceFailedWithInput?.Invoke(enteredSequence.ToArray(), enteredSequence.Count);
    }

    private void NotifySequenceReset()
    {
        OnSequenceProgressReset?.Invoke();
    }

    private void PlayUiAudio(AudioEventSO audioEvent)
    {
        if (audioEvent == null)
            return;

        Outsiders.Auditory.AudioManager.Instance?.PlayUI(audioEvent);
    }
}
}
