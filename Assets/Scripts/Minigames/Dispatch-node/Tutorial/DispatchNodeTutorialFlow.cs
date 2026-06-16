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
    [SerializeField] private TutorialStep collectKeyStep;
    [SerializeField] private TutorialStep memorizePatternStep;
    [SerializeField] private TutorialStep finishGameStep;

    [Header("Dispatch References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private PathNode lockedNode;
    [SerializeField] private SequenceUnlockGate sequenceGate;
    [SerializeField] private SequenceGateButton revealSequenceButton;
    [SerializeField] private SequenceGateButton beginEntryButton;
    [SerializeField] private KeyPickupNodeInteraction keyPickup;
    [SerializeField] private GoalNodeInteraction goalNode;

    [Header("Step Completion")]
    [SerializeField] private bool completeSwipeStepOnAnyMove;
    [SerializeField] private bool completePasswordStepOnBeginEntryPress;
    [SerializeField] private bool completeMemorizeStepOnSequenceSolved = true;
    [SerializeField] private bool completeMemorizeStepOnGoalReached = true;
    [SerializeField, Min(0f)] private float passwordNeedsKeyAutoCompleteDelay;

    private Coroutine passwordAutoCompleteRoutine;

    void Awake()
    {
        ResolveReferences();
    }

    void OnEnable()
    {
        ResolveReferences();

        if (player != null)
            player.OnCurrentNodeChanged += HandleCurrentNodeChanged;

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
            beginEntryButton.OnPressed += HandleGateButtonPressed;

        if (keyPickup != null)
            keyPickup.OnKeyCollected += HandleKeyCollected;

        if (goalNode != null)
            goalNode.OnGoalReached += HandleGoalReached;

        if (passwordNeedsKeyStep != null && passwordNeedsKeyStep.OnStepStart != null)
            passwordNeedsKeyStep.OnStepStart.AddListener(HandlePasswordStepStarted);
    }

    void Start()
    {
        if (player != null && ShouldCompleteSwipeStep(player.currentNode))
            TryCompleteStep(swipeToLockedNodeStep);
    }

    void OnDisable()
    {
        if (player != null)
            player.OnCurrentNodeChanged -= HandleCurrentNodeChanged;

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
            beginEntryButton.OnPressed -= HandleGateButtonPressed;

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
            player.SetGameplayControlEnabled(!shouldRestrict);
        }

    private void HandleCurrentNodeChanged(PathNode previousNode, PathNode currentNode)
    {
        if (ShouldCompleteSwipeStep(currentNode))
            TryCompleteStep(swipeToLockedNodeStep);
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
    }
}
}
