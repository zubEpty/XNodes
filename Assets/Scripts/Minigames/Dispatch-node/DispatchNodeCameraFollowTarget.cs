using UnityEngine;

namespace Dispatch.Gameplay
{
public class DispatchNodeCameraFollowTarget : MonoBehaviour
{
    public Transform playerTarget;
    public Vector3 offset = new Vector3(0f, 8f, -8f);
    public float smoothTime = 0.2f;
    public bool lockX;
    public bool lockY;
    public bool lockZ;

    private Vector3 velocity;

    void Start()
    {
        SnapToTarget();
    }

    void LateUpdate()
    {
        if (playerTarget == null)
            return;

        Vector3 desiredPosition = GetDesiredPosition(playerTarget);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            smoothTime);
    }

    public void SnapToTarget()
    {
        if (playerTarget == null)
            return;

        Vector3 snappedPosition = playerTarget.position + offset;
        transform.position = snappedPosition;
        velocity = Vector3.zero;
    }

    public void SetTarget(Transform target, bool snapToTarget = false)
    {
        playerTarget = target;
        velocity = Vector3.zero;

        if (snapToTarget)
            SnapToTarget();
    }

    public Vector3 GetDesiredPosition(Transform target)
    {
        if (target == null)
            return transform.position;

        Vector3 desiredPosition = target.position + offset;

        if (lockX)
            desiredPosition.x = transform.position.x;

        if (lockY)
            desiredPosition.y = transform.position.y;

        if (lockZ)
            desiredPosition.z = transform.position.z;

        return desiredPosition;
    }
}
}
