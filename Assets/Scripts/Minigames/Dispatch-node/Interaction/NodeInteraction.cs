using UnityEngine;

namespace Dispatch.Gameplay
{
public abstract class NodeInteraction : MonoBehaviour
{
    public abstract void Trigger(PlayerController player);
}
}
