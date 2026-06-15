using UnityEngine;

namespace Dispatch.Gameplay
{
public class DispatchBillboard : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private bool useMainCameraWhenTargetMissing = true;
    [SerializeField] private bool usePlayerWhenCameraMissing = false;
    [SerializeField] private bool lookAwayFromTarget = false;
    [SerializeField] private bool lockPitch = false;
    [SerializeField] private bool lockRoll = true;
    [SerializeField] private Vector3 rotationOffset;

    void LateUpdate()
    {
        ResolveTarget();

        if (target == null)
            return;

        Vector3 direction = lookAwayFromTarget
            ? transform.position - target.position
            : target.position - transform.position;

        if (lockPitch)
            direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        Quaternion finalRotation = lookRotation * Quaternion.Euler(rotationOffset);

        if (lockRoll)
        {
            Vector3 euler = finalRotation.eulerAngles;
            euler.z = 0f;
            finalRotation = Quaternion.Euler(euler);
        }

        transform.rotation = finalRotation;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void ResolveTarget()
    {
        if (target != null)
            return;

        if (useMainCameraWhenTargetMissing && Camera.main != null)
        {
            target = Camera.main.transform;
            return;
        }

        if (usePlayerWhenCameraMissing && GameBootstrap.Instance != null && GameBootstrap.Instance.player != null)
            target = GameBootstrap.Instance.player.transform;
    }
}
}
