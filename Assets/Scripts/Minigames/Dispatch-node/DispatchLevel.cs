using UnityEngine;
using UnityEngine.Events;

namespace Dispatch.Gameplay
{
public class DispatchLevel : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string levelId;

    [Header("Entry")]
    [SerializeField] private PathNode startNode;
    [SerializeField] private bool clearPlayerKeysOnLoad = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onLevelLoaded;
    [SerializeField] private UnityEvent onLevelUnloaded;
    [SerializeField] private UnityEvent onLevelCompleted;

    public string LevelId => levelId;
    public PathNode StartNode => startNode;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    public void BindRuntime(GameBootstrap bootstrap)
    {
        if (bootstrap == null)
            return;

        BindSequenceGates(bootstrap);
        BindSequenceSwitches(bootstrap);
        BindPresenceVisibility(bootstrap);
        BindGoalNodes();
        BindTapPromptJuice();

        if (bootstrap.player != null)
        {
            DispatchNodeKeyRing keyRing = bootstrap.player.GetComponent<DispatchNodeKeyRing>();
            if (keyRing == null)
                keyRing = bootstrap.player.gameObject.AddComponent<DispatchNodeKeyRing>();

            if (clearPlayerKeysOnLoad)
                keyRing.Clear();

            if (startNode != null)
                bootstrap.player.WarpToNode(startNode);
        }

        onLevelLoaded?.Invoke();
    }

    public void NotifyCompleted()
    {
        onLevelCompleted?.Invoke();
        DispatchLevelManager.Instance?.NotifyLevelCompleted(this);
    }

    public void NotifyUnloaded()
    {
        onLevelUnloaded?.Invoke();
    }

    private void BindSequenceGates(GameBootstrap bootstrap)
    {
        SequenceUnlockGate[] gates = GetComponentsInChildren<SequenceUnlockGate>(true);

        for (int i = 0; i < gates.Length; i++)
        {
            SequenceUnlockGate gate = gates[i];
            if (gate.inputHandler == null)
                gate.inputHandler = bootstrap.input;

            if (gate.player == null)
                gate.player = bootstrap.player;

            if (gate.gameStateManager == null)
                gate.gameStateManager = bootstrap.gameStateManager;
        }
    }

    private void BindSequenceSwitches(GameBootstrap bootstrap)
    {
        SequenceConnectionSwitch[] switches = GetComponentsInChildren<SequenceConnectionSwitch>(true);

        for (int i = 0; i < switches.Length; i++)
        {
            SequenceConnectionSwitch sequenceSwitch = switches[i];
            if (sequenceSwitch.inputHandler == null)
                sequenceSwitch.inputHandler = bootstrap.input;

            if (sequenceSwitch.player == null)
                sequenceSwitch.player = bootstrap.player;

            if (sequenceSwitch.gameStateManager == null)
                sequenceSwitch.gameStateManager = bootstrap.gameStateManager;
        }
    }

    private void BindGoalNodes()
    {
        GoalNodeInteraction[] goalNodes = GetComponentsInChildren<GoalNodeInteraction>(true);

        for (int i = 0; i < goalNodes.Length; i++)
            goalNodes[i].BindLevelManager(DispatchLevelManager.Instance);
    }

    private void BindPresenceVisibility(GameBootstrap bootstrap)
    {
        NodePresenceVisibility[] visibilityRules = GetComponentsInChildren<NodePresenceVisibility>(true);

        for (int i = 0; i < visibilityRules.Length; i++)
        {
            if (visibilityRules[i].player == null)
                visibilityRules[i].player = bootstrap.player;
        }
    }

    private void BindTapPromptJuice()
    {
        DispatchTapPromptJuiceBinder binder = GetComponent<DispatchTapPromptJuiceBinder>();
        if (binder == null)
            binder = gameObject.AddComponent<DispatchTapPromptJuiceBinder>();

        binder.Bind(this);
    }
}
}
