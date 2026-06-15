using UnityEngine;
using UnityEngine.Events;

namespace Dispatch.Gameplay
{
public class KeyRequiredNodeInteraction : NodeInteraction, INodeTraversalRule
{
    [SerializeField] private string requiredKeyCode = "0000";
    [SerializeField] private bool triggerWhenEntered = false;
    [SerializeField] private UnityEvent onAccessGranted;
    [SerializeField] private UnityEvent onAccessDenied;

    public bool CanEnter(PlayerController player, PathNode fromNode)
    {
        DispatchNodeKeyRing keyRing = player != null ? player.GetComponent<DispatchNodeKeyRing>() : null;
        bool hasKey = keyRing != null && keyRing.HasKey(requiredKeyCode);

        if (!hasKey)
            onAccessDenied?.Invoke();

        return hasKey;
    }

    public override void Trigger(PlayerController player)
    {
        if (!triggerWhenEntered)
            return;

        onAccessGranted?.Invoke();
    }
}
}
