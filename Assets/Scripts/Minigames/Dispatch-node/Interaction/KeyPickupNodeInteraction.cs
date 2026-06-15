using UnityEngine;
using UnityEngine.Events;
using System;

namespace Dispatch.Gameplay
{
public class KeyPickupNodeInteraction : NodeInteraction
{
    public event Action<PlayerController, string> OnKeyCollected;

    [SerializeField] private string keyCode = "0000";
    [SerializeField] private bool consumeOnce = true;
    [SerializeField] private UnityEvent onKeyCollected;

    private bool collected;

    public override void Trigger(PlayerController player)
    {
        if (consumeOnce && collected)
            return;

        DispatchNodeKeyRing keyRing = player != null ? player.GetComponent<DispatchNodeKeyRing>() : null;
        if (keyRing == null && player != null)
            keyRing = player.gameObject.AddComponent<DispatchNodeKeyRing>();

        if (keyRing == null)
            return;

        keyRing.AddKey(keyCode);
        collected = true;
        OnKeyCollected?.Invoke(player, keyCode);
        onKeyCollected?.Invoke();
    }
}
}
