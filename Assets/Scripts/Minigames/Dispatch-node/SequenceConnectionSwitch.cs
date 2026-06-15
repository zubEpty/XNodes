using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Outsiders.Auditory;

namespace Dispatch.Gameplay
{
public class SequenceConnectionSwitch : MonoBehaviour
{
    private const int RequiredSequenceLength = 2;

    public event Action<Direction[]> OnSequenceGenerated;
    public event Action<Direction[]> OnSequenceSolved;
    public event Action<Direction[], int> OnSequenceProgressChanged;
    public event Action<Direction[], int> OnSequenceFailedWithInput;
    public event Action OnSequenceProgressReset;

    [Header("References")]
    public SwipeInputHandler inputHandler;
    public PlayerController player;
    public DispatchGameStateManager gameStateManager;
    [SerializeField] private GameObject sequenceRoot;
    [SerializeField] private bool showSequenceRootOnEntry = true;
    [SerializeField] private bool hideSequenceRootOnExit = true;
    [SerializeField] private bool pauseGameplayWhileSequenceRootActive = true;

    [Header("Connection Swap")]
    public PathNode sourceNode;
    public PathNode nodeToConnect;
    public PathNode nodeToDisconnect;
    [SerializeField] private bool keepOnlyOneConnectionActive = true;

    [Header("Sequence")]
    [SerializeField] private Direction[] requiredSequence;
    [SerializeField] private bool generateSequenceOnAwake = true;
    [SerializeField] private bool autoResumeGameplayAfterSequenceEnds = false;
    [SerializeField] private bool autoExitOnSuccess = true;
    [SerializeField] private float autoExitDelay = 0.15f;
    [SerializeField] private bool retryAfterWrongSequence = true;
    [SerializeField] private float wrongSequenceRetryDelay = 0.6f;
    public float movementRestoreDelay = 0.15f;

    [Header("Audio")]
    [SerializeField] private AudioEventSO sequenceInputAudio;
    [SerializeField] private AudioEventSO correctSequenceAudio;
    [SerializeField] private AudioEventSO wrongSequenceAudio;

    [Header("Events")]
    public UnityEvent onSequenceStarted;
    public UnityEvent onSwitchSucceeded;
    public UnityEvent onSwitchFailed;

    private readonly List<Direction> enteredSequence = new List<Direction>();
    private CancellationTokenSource restoreMovementCts;
    private CancellationTokenSource autoExitCts;
    private CancellationTokenSource retryAfterFailureCts;
    private bool isListeningForInput;
    private bool isSwitched;

    public bool IsListeningForInput => isListeningForInput;

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

    public void GenerateNewSequence()
    {
        requiredSequence = new Direction[RequiredSequenceLength];

        for (int i = 0; i < requiredSequence.Length; i++)
            requiredSequence[i] = GetRandomDirection();

        isListeningForInput = false;
        isSwitched = false;
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

    public void BeginSequenceEntry()
    {
        ResolveRuntimeReferences();
        StopAutoExit();
        StopRetryAfterFailure();

        if (inputHandler == null || player == null)
        {
            Debug.LogWarning("SequenceConnectionSwitch is missing required references.");
            return;
        }

        GenerateNewSequence();
        enteredSequence.Clear();
        isListeningForInput = true;
        isSwitched = false;
        if (showSequenceRootOnEntry)
            SetSequenceRootActive(true, false);

        PauseGameplay();
        NotifySequenceReset();
        onSequenceStarted?.Invoke();

        Debug.Log($"Connection switch sequence started: {GetSequenceText()}");
    }

    public void OpenInteraction()
    {
        BeginSequenceEntry();
    }

    public void ExitSequenceEntry()
    {
        StopAutoExit();
        StopRetryAfterFailure();
        isListeningForInput = false;
        enteredSequence.Clear();
        NotifySequenceReset();

        if (hideSequenceRootOnExit)
            SetSequenceRootActive(false, false);

        ResumeGameplay();
    }

    public void ResumeGameplay()
    {
        StopAutoExit();
        StopRetryAfterFailure();

        StopRestoreMovement();

        if (hideSequenceRootOnExit)
            SetSequenceRootActive(false, false);

        DispatchGameStateManager manager = GetGameStateManager();

        if (manager != null)
        {
            manager.ResumeGameplay();
            return;
        }

        if (player != null)
            player.SetGameplayControlEnabled(true);
    }

    private void HandleDirectionInput(Direction direction)
    {
        if (!isListeningForInput)
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

    private void SolveSequence()
    {
        isListeningForInput = false;
        isSwitched = true;
        StopRetryAfterFailure();
        NotifySequenceProgressChanged();

        PathNode nextTargetNode = GetNextTargetNode();
        PathNode previousTargetNode = GetPreviousTargetNode(nextTargetNode);

        if (sourceNode != null)
        {
            if (previousTargetNode != null)
                SetConnectionStateBetweenNodes(sourceNode, previousTargetNode, false);

            if (nextTargetNode != null)
                SetConnectionStateBetweenNodes(sourceNode, nextTargetNode, true);

            if (keepOnlyOneConnectionActive)
                DisableConfiguredAlternates(nextTargetNode, previousTargetNode);
        }

        OnSequenceSolved?.Invoke(GetRequiredSequence());
        onSwitchSucceeded?.Invoke();
        PlayUiAudio(correctSequenceAudio);
        Debug.Log("Connection switched successfully.");

        if (autoExitOnSuccess)
        {
            StartAutoExitAfterSuccess();
            return;
        }

        RestoreGameplayAfterDelayIfNeeded();
    }

    private void FailSequence()
    {
        isListeningForInput = false;
        NotifySequenceFailedWithInput();
        enteredSequence.Clear();

        onSwitchFailed?.Invoke();
        PlayUiAudio(wrongSequenceAudio);
        Debug.Log("Connection switch sequence failed.");

        if (retryAfterWrongSequence)
        {
            StopRetryAfterFailure();
            StartRetryAfterFailure();
            return;
        }

        NotifySequenceReset();
        RestoreGameplayAfterDelayIfNeeded();
    }

    private void RestoreGameplayAfterDelayIfNeeded()
    {
        if (!autoResumeGameplayAfterSequenceEnds)
            return;

        StopRestoreMovement();
        restoreMovementCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        RestoreMovementAsync(restoreMovementCts).Forget();
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

    private void StartAutoExitAfterSuccess()
    {
        StopAutoExit();
        autoExitCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        AutoExitAfterSuccessAsync(autoExitCts).Forget();
    }

    private async UniTaskVoid AutoExitAfterSuccessAsync(CancellationTokenSource routineCts)
    {
        try
        {
            if (autoExitDelay > 0f)
                await UniTask.Delay((int)(autoExitDelay * 1000f), cancellationToken: routineCts.Token);
            else
                await UniTask.Yield(routineCts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        CompleteDelayedAction(ref autoExitCts, routineCts);
        ExitSequenceEntry();
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

    private void StopAutoExit()
    {
        CancelDelayedAction(ref autoExitCts);
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
        StopAutoExit();
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

    public void ResolveRuntimeReferences()
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

    private void NotifySequenceFailedWithInput()
    {
        OnSequenceFailedWithInput?.Invoke(enteredSequence.ToArray(), enteredSequence.Count);
    }

    private void NotifySequenceReset()
    {
        OnSequenceProgressReset?.Invoke();
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

    private PathNode GetNextTargetNode()
    {
        if (sourceNode == null)
            return nodeToConnect;

        bool isPrimaryConnected = nodeToConnect != null && sourceNode.IsConnectionActive(nodeToConnect);
        bool isSecondaryConnected = nodeToDisconnect != null && sourceNode.IsConnectionActive(nodeToDisconnect);

        if (isPrimaryConnected && nodeToDisconnect != null)
            return nodeToDisconnect;

        if (isSecondaryConnected && nodeToConnect != null)
            return nodeToConnect;

        if (nodeToConnect != null)
            return nodeToConnect;

        return nodeToDisconnect;
    }

    private PathNode GetPreviousTargetNode(PathNode nextTargetNode)
    {
        if (nextTargetNode == nodeToConnect)
            return nodeToDisconnect;

        if (nextTargetNode == nodeToDisconnect)
            return nodeToConnect;

        return nodeToDisconnect;
    }

    private void DisableConfiguredAlternates(PathNode nextTargetNode, PathNode previousTargetNode)
    {
        PathNode alternateTarget = previousTargetNode;

        if (alternateTarget == null)
            alternateTarget = nextTargetNode == nodeToConnect ? nodeToDisconnect : nodeToConnect;

        if (alternateTarget != null && alternateTarget != nextTargetNode)
            SetConnectionStateBetweenNodes(sourceNode, alternateTarget, false);
    }

    private void SetConnectionStateBetweenNodes(PathNode fromNode, PathNode toNode, bool isConnected)
    {
        if (fromNode == null || toNode == null)
            return;

        fromNode.SetConnectionState(toNode, isConnected);
        toNode.SetConnectionState(fromNode, isConnected);
    }

    private void SetSequenceRootActive(bool isActive, bool syncGameplayState = true)
    {
        if (sequenceRoot == null)
            TryResolveSequenceRoot();

        if (sequenceRoot != null)
            sequenceRoot.SetActive(isActive);

        if (!pauseGameplayWhileSequenceRootActive || !syncGameplayState)
            return;

        if (isActive)
            PauseGameplay();
        else
            ResumeGameplay();
    }

    private void TryResolveSequenceRoot()
    {
        if (sequenceRoot != null)
            return;

        Transform sequenceTransform = transform.Find("Sequence");

        if (sequenceTransform != null)
            sequenceRoot = sequenceTransform.gameObject;
    }

    private void PlayUiAudio(AudioEventSO audioEvent)
    {
        if (audioEvent == null)
            return;

        Outsiders.Auditory.AudioManager.Instance?.PlayUI(audioEvent);
    }
}
}
