using System;
using UnityEngine;
using UnityEngine.UI;
using Outsiders.Auditory;

namespace Dispatch.Gameplay
{
public class SequenceConnectionSwitchButton : MonoBehaviour
{
    public SequenceConnectionSwitch sequenceSwitch;
    public ButtonAction action;
    public bool requirePlayerOnNode = true;
    public PathNode requiredNode;
    public Button targetButton;
    public CanvasGroup targetCanvasGroup;
    public float disabledAlpha = 0.45f;
    [SerializeField] private GameObject sequenceRoot;
    [SerializeField] private bool showSequenceRootOnPress = true;
    [SerializeField] private bool hideSequenceRootOnExit = true;
    [SerializeField] private bool autoRegisterButtonClick = true;
    [SerializeField] private AudioEventSO actionPanelOpenAudio;

    [SerializeField] private Button _exitButton;
    private PlayerController subscribedPlayer;

    public enum ButtonAction
    {
        BeginSequenceEntry,
        ExitSequenceEntry,
        ResumeGameplay
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
        TryResolveSwitch();
        SubscribeToPlayerEvents();

        if (autoRegisterButtonClick && targetButton != null)
        {
            targetButton.onClick.RemoveListener(Press);
            targetButton.onClick.AddListener(Press);
        }

        _exitButton?.onClick.AddListener(DispatchGameStateManager.Instance.ResumeGameplay);
        RefreshVisualState();
    }

    void OnDisable()
    {
        UnsubscribeFromPlayerEvents();

        if (targetButton != null)
            targetButton.onClick.RemoveListener(Press);

        _exitButton?.onClick.RemoveAllListeners();    
    }

    public void Press()
    {
        if (sequenceSwitch == null)
        {
            TryResolveSwitch();

            if (sequenceSwitch == null)
                return;
        }

        if (!CanPress(true))
            return;

        if (showSequenceRootOnPress && action == ButtonAction.BeginSequenceEntry)
            SetSequenceRootActive(true);

        switch (action)
        {
            case ButtonAction.BeginSequenceEntry:
                sequenceSwitch.BeginSequenceEntry();
                break;
            case ButtonAction.ExitSequenceEntry:
                sequenceSwitch.ExitSequenceEntry();
                if (hideSequenceRootOnExit)
                    SetSequenceRootActive(false);
                break;
            case ButtonAction.ResumeGameplay:
                sequenceSwitch.ResumeGameplay();
                if (hideSequenceRootOnExit)
                    SetSequenceRootActive(false);
                break;
        }

        RefreshVisualState();
    }

    private void SubscribeToPlayerEvents()
    {
        PlayerController activePlayer = sequenceSwitch != null ? sequenceSwitch.player : GetActivePlayer();
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
        if (sequenceSwitch == null)
            TryResolveSwitch();

        if (sequenceSwitch != null)
            sequenceSwitch.ResolveRuntimeReferences();

        if (!requirePlayerOnNode)
            return true;

        if (sequenceSwitch == null || sequenceSwitch.player == null)
        {
            if (logFailure)
                Debug.LogWarning("SequenceConnectionSwitchButton requires a player reference through SequenceConnectionSwitch.");
            return false;
        }

        PathNode nodeToCheck = requiredNode != null ? requiredNode : sequenceSwitch.sourceNode;

        if (nodeToCheck == null)
            return true;

        if (sequenceSwitch.player.currentNode != nodeToCheck)
        {
            if (logFailure)
                Debug.Log("Player must be on the correct node to use this button.");
            return false;
        }

        return true;
    }

    private void TryResolveSwitch()
    {
        if (sequenceSwitch != null)
            return;

        sequenceSwitch = GetComponentInParent<SequenceConnectionSwitch>();
        sequenceSwitch?.ResolveRuntimeReferences();
    }

    private PlayerController GetActivePlayer()
    {
        if (sequenceSwitch != null && sequenceSwitch.player != null)
            return sequenceSwitch.player;

        if (GameBootstrap.Instance != null && GameBootstrap.Instance.player != null)
            return GameBootstrap.Instance.player;

        return FindFirstObjectByType<PlayerController>();
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
    }

    private void TryResolveSequenceRoot()
    {
        if (sequenceRoot != null)
            return;

        Transform sequenceTransform = transform.Find("Sequence");

        if (sequenceTransform == null && transform.parent != null)
            sequenceTransform = transform.parent.Find("Sequence");

        if (sequenceTransform == null && sequenceSwitch != null)
            sequenceTransform = sequenceSwitch.transform.Find("Sequence");

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
