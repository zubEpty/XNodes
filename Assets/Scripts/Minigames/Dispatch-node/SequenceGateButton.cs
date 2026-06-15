using Shapes2D;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Outsiders.Auditory;

namespace Dispatch.Gameplay
{
public class SequenceGateButton : MonoBehaviour
{
    public event Action<SequenceGateButton> OnPressed;

    public SequenceUnlockGate gate;
    public ButtonAction action;
    public bool requirePlayerOnNode = true;
    public PathNode requiredNode;
    public Button targetButton;
    public CanvasGroup targetCanvasGroup;
    public float disabledAlpha = 0.45f;
    [SerializeField] private GameObject sequenceRoot;
    [SerializeField] private Button _exitButton;
    [SerializeField] private bool showSequenceRootOnPress = true;
    [SerializeField] private bool hideSequenceRootOnCancel = true;
    [SerializeField] private bool autoRegisterButtonClick = true;
    [SerializeField] private AudioEventSO actionPanelOpenAudio;
    [SerializeField] private UnityEvent onPressed;
    private PlayerController subscribedPlayer;

    public enum ButtonAction
    {
        RevealSequence,
        BeginSequenceEntry,
        CancelSequenceEntry
    }

    void Awake()
    {
        if (targetButton == null)
            targetButton = GetComponent<Button>();

        if (targetCanvasGroup == null)
            targetCanvasGroup = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        TryResolveGate();
        SubscribeToPlayerEvents();

        if (autoRegisterButtonClick && targetButton != null)
        {
            targetButton.onClick.RemoveListener(Press);
            targetButton.onClick.AddListener(Press);
        }

        RefreshVisualState();
    }

    void OnDisable()
    {
        UnsubscribeFromPlayerEvents();

        if (targetButton != null)
            targetButton.onClick.RemoveListener(Press);

        if (_exitButton != null)
            _exitButton.onClick.RemoveListener(HandleExitButtonPressed);
    }

    public void Press()
    {
        if (gate == null)
        {
            TryResolveGate();

            if (gate == null)
                return;
        }

        if (!CanPress(true))
            return;

        if (showSequenceRootOnPress && action != ButtonAction.CancelSequenceEntry)
            SetSequenceRootActive(true);

        switch (action)
        {
            case ButtonAction.RevealSequence:
                gate.RevealSequence();
                break;
            case ButtonAction.BeginSequenceEntry:
                gate.BeginSequenceEntry();
                break;
            case ButtonAction.CancelSequenceEntry:
                gate.CancelSequenceEntry();
                if (hideSequenceRootOnCancel)
                    SetSequenceRootActive(false);
                break;
        }

        OnPressed?.Invoke(this);
        onPressed?.Invoke();
        RefreshVisualState();
    }

    private void SubscribeToPlayerEvents()
    {
        PlayerController activePlayer = gate != null && gate.player != null ? gate.player : GetActivePlayer();
        if (activePlayer != null)
        {
            activePlayer.OnCurrentNodeChanged += HandlePlayerNodeChanged;
            subscribedPlayer = activePlayer;
        }
    }

    private void UnsubscribeFromPlayerEvents()
    {
        if (subscribedPlayer != null)
            subscribedPlayer.OnCurrentNodeChanged -= HandlePlayerNodeChanged;

        subscribedPlayer = null;
    }

    private void HandlePlayerNodeChanged(PathNode previousNode, PathNode currentNode)
    {
        RefreshVisualState();
    }

    private void RefreshVisualState()
    {
        bool canPress = CanPress(false);

        if (targetButton != null)
            targetButton.interactable = canPress;

        if (targetCanvasGroup != null)
            targetCanvasGroup.alpha = canPress ? 1f : disabledAlpha;
    }

    private bool CanPress(bool logFailure)
    {
        if (gate == null)
            TryResolveGate();

        if (!requirePlayerOnNode)
            return true;

        if (gate == null || gate.player == null)
        {
            if (logFailure)
                Debug.LogWarning("SequenceGateButton requires a player reference through SequenceUnlockGate.");
            return false;
        }

        PathNode nodeToCheck = requiredNode != null ? requiredNode : gate.sourceNode;

        if (nodeToCheck == null)
            return true;

        if (gate.player.currentNode != nodeToCheck)
        {
            if (logFailure)
                Debug.Log("Player must be on the correct node to use this button.");
            return false;
        }

        return true;
    }

    private void TryResolveGate()
    {
        if (gate != null)
            return;

        gate = GetComponentInParent<SequenceUnlockGate>();
    }

    private void SetSequenceRootActive(bool isActive)
    {
        if (sequenceRoot == null)
            TryResolveSequenceRoot();

        if (sequenceRoot != null)
        {
            sequenceRoot.SetActive(isActive);
            if (isActive)
                PlayUiAudio(actionPanelOpenAudio);
        }

        if (_exitButton != null)
        {
            _exitButton.onClick.RemoveListener(HandleExitButtonPressed);
            _exitButton.onClick.AddListener(HandleExitButtonPressed);
        }

        if (isActive)
            PauseGameplayForSequenceUi();
        else
            ResumeGameplayForSequenceUi();
    }

    private void HandleExitButtonPressed()
    {
        ResumeGameplayForSequenceUi();
    }

    private void PauseGameplayForSequenceUi()
    {
        PlayerController activePlayer = GetActivePlayer();
        DispatchGameStateManager manager = GetGameStateManager();

        if (manager != null)
        {
            if (activePlayer != null)
                manager.RegisterPlayer(activePlayer);

            manager.PauseGameplay();
            return;
        }

        if (activePlayer != null)
            activePlayer.SetGameplayControlEnabled(false);
    }

    private void ResumeGameplayForSequenceUi()
    {
        DispatchGameStateManager manager = GetGameStateManager();

        if (manager != null)
        {
            manager.ResumeGameplay();
            return;
        }

        PlayerController activePlayer = GetActivePlayer();
        if (activePlayer != null)
            activePlayer.SetGameplayControlEnabled(true);
    }

    private DispatchGameStateManager GetGameStateManager()
    {
        if (gate != null && gate.gameStateManager != null)
            return gate.gameStateManager;

        if (GameBootstrap.Instance != null && GameBootstrap.Instance.gameStateManager != null)
            return GameBootstrap.Instance.gameStateManager;

        if (DispatchGameStateManager.Instance != null)
            return DispatchGameStateManager.Instance;

        return FindFirstObjectByType<DispatchGameStateManager>();
    }

    private PlayerController GetActivePlayer()
    {
        if (gate != null && gate.player != null)
            return gate.player;

        if (GameBootstrap.Instance != null && GameBootstrap.Instance.player != null)
            return GameBootstrap.Instance.player;

        return FindFirstObjectByType<PlayerController>();
    }

    private void TryResolveSequenceRoot()
    {
        if (sequenceRoot != null)
            return;

        Transform sequenceTransform = transform.Find("Sequence");

        if (sequenceTransform == null && transform.parent != null)
            sequenceTransform = transform.parent.Find("Sequence");

        if (sequenceTransform == null && gate != null)
            sequenceTransform = gate.transform.Find("Sequence");

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
