using System.Collections;
using Outsiders.SimpleTutorial;
using UnityEngine;

namespace Dispatch.Gameplay
{
public class DispatchNodeTutorialFlow : MonoBehaviour
{
    [Header("Tutorial Steps")]
    [SerializeField] private TutorialStep swipeToLockedNodeStep;
    [SerializeField] private TutorialStep tapGateButtonStep;
    [SerializeField] private TutorialStep passwordNeedsKeyStep;
    [SerializeField] private TutorialStep exitAndMoveToKeyStep;
    [SerializeField] private TutorialStep collectKeyStep;
    [SerializeField] private TutorialStep memorizePatternStep;
    [SerializeField] private TutorialStep finishGameStep;

    [Header("Dispatch References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private PathNode lockedNode;
    [SerializeField] private SequenceUnlockGate sequenceGate;
    [SerializeField] private SequenceGateButton revealSequenceButton;
    [SerializeField] private SequenceGateButton beginEntryButton;
    [SerializeField] private SequenceGateButton keyIconButton;
    [SerializeField] private PathNode keyNode;
    [SerializeField] private KeyPickupNodeInteraction keyPickup;
    [SerializeField] private GoalNodeInteraction goalNode;

    [Header("Step Completion")]
    [SerializeField] private bool completeSwipeStepOnAnyMove;
    [SerializeField] private bool completePasswordStepOnBeginEntryPress;
    [SerializeField] private bool completeMemorizeStepOnSequenceSolved = true;
    [SerializeField] private bool completeMemorizeStepOnGoalReached = true;
    [SerializeField, Min(0f)] private float passwordNeedsKeyAutoCompleteDelay;

    private Coroutine passwordAutoCompleteRoutine;
    private bool lockExitPressed;
    private PlayerController subscribedPlayer;

    void Awake()
    {
        ResolveReferences();
    }

    void OnEnable()
    {
        ResolveReferences();

        TryBindPlayer();

        if (sequenceGate != null)
        {
            sequenceGate.OnSequenceRevealed += HandleSequenceRevealed;
            sequenceGate.OnSequenceProgressChanged += HandleSequenceProgressChanged;

            if (sequenceGate.onEntryStarted != null)
                sequenceGate.onEntryStarted.AddListener(HandleEntryStarted);

            if (sequenceGate.onSequenceSolved != null)
                sequenceGate.onSequenceSolved.AddListener(HandleSequenceSolved);
        }

        if (revealSequenceButton != null)
            revealSequenceButton.OnPressed += HandleGateButtonPressed;

        if (beginEntryButton != null)
        {
            beginEntryButton.OnPressed += HandleGateButtonPressed;
            beginEntryButton.OnExitPressed += HandleLockExitPressed;
        }

        if (keyIconButton != null && keyIconButton != revealSequenceButton && keyIconButton != beginEntryButton)
            keyIconButton.OnPressed += HandleGateButtonPressed;

        if (keyPickup != null)
            keyPickup.OnKeyCollected += HandleKeyCollected;

        if (goalNode != null)
            goalNode.OnGoalReached += HandleGoalReached;

        if (passwordNeedsKeyStep != null && passwordNeedsKeyStep.OnStepStart != null)
            passwordNeedsKeyStep.OnStepStart.AddListener(HandlePasswordStepStarted);
    }

    void Start()
    {
        TryBindPlayer();
    }

    void Update()
    {
        if (subscribedPlayer == null)
            TryBindPlayer();
    }

    void OnDisable()
    {
        UnbindPlayer();

        if (sequenceGate != null)
        {
            sequenceGate.OnSequenceRevealed -= HandleSequenceRevealed;
            sequenceGate.OnSequenceProgressChanged -= HandleSequenceProgressChanged;

            if (sequenceGate.onEntryStarted != null)
                sequenceGate.onEntryStarted.RemoveListener(HandleEntryStarted);

            if (sequenceGate.onSequenceSolved != null)
                sequenceGate.onSequenceSolved.RemoveListener(HandleSequenceSolved);
        }

        if (revealSequenceButton != null)
            revealSequenceButton.OnPressed -= HandleGateButtonPressed;

        if (beginEntryButton != null)
        {
            beginEntryButton.OnPressed -= HandleGateButtonPressed;
            beginEntryButton.OnExitPressed -= HandleLockExitPressed;
        }

        if (keyIconButton != null && keyIconButton != revealSequenceButton && keyIconButton != beginEntryButton)
            keyIconButton.OnPressed -= HandleGateButtonPressed;

        if (keyPickup != null)
            keyPickup.OnKeyCollected -= HandleKeyCollected;

        if (goalNode != null)
            goalNode.OnGoalReached -= HandleGoalReached;

        if (passwordNeedsKeyStep != null && passwordNeedsKeyStep.OnStepStart != null)
            passwordNeedsKeyStep.OnStepStart.RemoveListener(HandlePasswordStepStarted);

        StopPasswordAutoComplete();
    }

    public void RestrictPlayerMove(bool shouldRestrict)
    {
        if (player != null)
            player.SetGameplayControlEnabled(!shouldRestrict);
    }

    private void HandleCurrentNodeChanged(PathNode previousNode, PathNode currentNode)
    {
        if (ShouldCompleteSwipeStep(currentNode))
            TryCompleteStep(swipeToLockedNodeStep);

        if (ShouldShowCollectKeyStep(currentNode))
            ActivateStep(collectKeyStep);
    }

    private bool ShouldCompleteSwipeStep(PathNode currentNode)
    {
        if (currentNode == null)
            return false;

        if (completeSwipeStepOnAnyMove)
            return true;

        PathNode targetNode = lockedNode != null
            ? lockedNode
            : sequenceGate != null ? sequenceGate.sourceNode : null;
        return targetNode != null && currentNode == targetNode;
    }

    private void HandleSequenceRevealed(Direction[] sequence)
    {
        TryCompleteStep(tapGateButtonStep);
    }

    private void HandleSequenceProgressChanged(Direction[] enteredDirections, int count)
    {
        if (count > 0)
            TryCompleteStep(passwordNeedsKeyStep);
    }

    private void HandleEntryStarted()
    {
        if (completePasswordStepOnBeginEntryPress)
            TryCompleteStep(passwordNeedsKeyStep);
    }

    private void HandleGateButtonPressed(SequenceGateButton button)
    {
        if (button == revealSequenceButton)
            TryCompleteStep(tapGateButtonStep);

        if (button == beginEntryButton && completePasswordStepOnBeginEntryPress)
            TryCompleteStep(passwordNeedsKeyStep);

        if (button == keyIconButton || (button == revealSequenceButton && revealSequenceButton == keyIconButton))
            TryCompleteStep(collectKeyStep);
    }

    private void HandleLockExitPressed(SequenceGateButton button)
    {
        lockExitPressed = true;

        if (IsStepActive(exitAndMoveToKeyStep))
            exitAndMoveToKeyStep.Deactivate();

        RestrictPlayerMove(false);

        if (ShouldShowCollectKeyStep(player != null ? player.currentNode : null))
            ActivateStep(collectKeyStep);
    }

    private void HandleKeyCollected(PlayerController collectingPlayer, string keyCode)
    {
        TryCompleteStep(collectKeyStep);
    }

    private void HandleSequenceSolved()
    {
        if (completeMemorizeStepOnSequenceSolved)
            TryCompleteStep(memorizePatternStep);
    }

    private void HandleGoalReached(PlayerController reachingPlayer)
    {
        if (completeMemorizeStepOnGoalReached && IsStepActive(memorizePatternStep))
        {
            TryCompleteStep(memorizePatternStep);
            return;
        }

        TryCompleteStep(finishGameStep);
    }

    private void HandlePasswordStepStarted()
    {
        if (passwordNeedsKeyAutoCompleteDelay <= 0f)
            return;

        StopPasswordAutoComplete();
        passwordAutoCompleteRoutine = StartCoroutine(CompletePasswordStepAfterDelay());
    }

    private IEnumerator CompletePasswordStepAfterDelay()
    {
        yield return new WaitForSeconds(passwordNeedsKeyAutoCompleteDelay);
        passwordAutoCompleteRoutine = null;
        TryCompleteStep(passwordNeedsKeyStep);
    }

    private void StopPasswordAutoComplete()
    {
        if (passwordAutoCompleteRoutine == null)
            return;

        StopCoroutine(passwordAutoCompleteRoutine);
        passwordAutoCompleteRoutine = null;
    }

    private void TryCompleteStep(TutorialStep step)
    {
        if (!IsStepActive(step))
            return;

        step.Complete();
    }

    private bool IsStepActive(TutorialStep step)
    {
        return step != null && step.gameObject.activeInHierarchy;
    }

    private void ActivateStep(TutorialStep step)
    {
        if (step == null || step.gameObject.activeInHierarchy)
            return;

        step.Activate();
    }

    private bool ShouldShowCollectKeyStep(PathNode currentNode)
    {
        if (!lockExitPressed || currentNode == null || collectKeyStep == null)
            return false;

        return keyNode != null && currentNode == keyNode;
    }

    private void TryBindPlayer()
    {
        ResolveReferences();

        if (player == null || subscribedPlayer == player)
            return;

        UnbindPlayer();
        subscribedPlayer = player;
        subscribedPlayer.OnCurrentNodeChanged += HandleCurrentNodeChanged;
        HandleCurrentNodeChanged(null, subscribedPlayer.currentNode);
    }

    private void UnbindPlayer()
    {
        if (subscribedPlayer == null)
            return;

        subscribedPlayer.OnCurrentNodeChanged -= HandleCurrentNodeChanged;
        subscribedPlayer = null;
    }

    private void ResolveReferences()
    {
        if (sequenceGate == null)
            sequenceGate = FindFirstObjectByType<SequenceUnlockGate>();

        if (lockedNode == null && sequenceGate != null)
            lockedNode = sequenceGate.sourceNode;

        if (player == null)
        {
            if (GameBootstrap.Instance != null)
                player = GameBootstrap.Instance.player;

            if (player == null && sequenceGate != null)
                player = sequenceGate.player;

            if (player == null)
                player = FindFirstObjectByType<PlayerController>();
        }

        if (keyPickup == null)
            keyPickup = FindFirstObjectByType<KeyPickupNodeInteraction>();

        if (goalNode == null)
            goalNode = FindFirstObjectByType<GoalNodeInteraction>();

        if (keyIconButton == null)
            keyIconButton = revealSequenceButton;

        if (keyNode == null && keyIconButton != null)
            keyNode = keyIconButton.requiredNode;

        if (exitAndMoveToKeyStep == null)
            exitAndMoveToKeyStep = FindTutorialStepByName("Step4");
    }

    private TutorialStep FindTutorialStepByName(string stepName)
    {
        TutorialStep[] tutorialSteps = FindObjectsByType<TutorialStep>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < tutorialSteps.Length; i++)
        {
            TutorialStep step = tutorialSteps[i];
            if (step != null && step.gameObject.name == stepName)
                return step;
        }

        return null;
    }
}
}
