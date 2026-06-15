using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Outsiders.Auditory;

namespace Dispatch.Gameplay
{
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public PathNode currentNode;
    [SerializeField] private AudioEventSO movementAudio;

    [Header("Visual Rotation")]
    [SerializeField] private Transform bodyMesh;
    public bool rotateBodyOnMove = true;
    [SerializeField] private float bodyRotationLerpSpeed = 12f;
    [SerializeField] private float bodyRollDegreesPerUnit = 360f;
    [SerializeField] private Vector3 bodyRotationOffset;

    public event Action<PathNode, PathNode> OnCurrentNodeChanged;

    private bool isMoving;
    public bool movementEnabled = true;
    private Quaternion targetBodyRotation;

    private Direction? bufferedInput;
    private CancellationTokenSource moveCancellationSource;

    private readonly Stack<MoveHistoryEntry> moveHistory = new Stack<MoveHistoryEntry>();

    void Start()
    {
        if (bodyMesh == null)
        {
            bodyMesh = FindDefaultBodyMesh();
        }

        if (bodyMesh != null)
        {
            targetBodyRotation = bodyMesh.rotation;
        }

        currentNode?.SetPlayerPresence(true);
    }

    void OnDisable()
    {
        CancelActiveMovement();

        if (currentNode != null)
            currentNode.SetPlayerPresence(false);
    }

    void OnDestroy()
    {
        CancelActiveMovement();
    }

    private struct MoveHistoryEntry
    {
        public PathNode Node;
        public Direction DirectionTaken;

        public MoveHistoryEntry(PathNode node, Direction directionTaken)
        {
            Node = node;
            DirectionTaken = directionTaken;
        }
    }

    #region Initialization

    public void Init(SwipeInputHandler input)
    {
        if (input == null)
            return;

        input.OnSwipe += HandleInput;
        input.OnMove += HandleInput;
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
    }

    public void SetGameplayControlEnabled(bool enabled)
    {
        movementEnabled = enabled;
        rotateBodyOnMove = enabled;
    }

    public bool IsOnNode(PathNode node)
    {
        return currentNode != null && currentNode == node;
    }

    public void WarpToNode(PathNode node, bool clearHistory = true)
    {
        CancelActiveMovement();

        PathNode previousNode = currentNode;
        if (currentNode != null)
            currentNode.SetPlayerPresence(false);

        currentNode = node;

        if (currentNode != null)
        {
            transform.position = currentNode.transform.position;
            currentNode.SetPlayerPresence(true);
        }

        if (clearHistory)
            moveHistory.Clear();

        bufferedInput = null;
        isMoving = false;

        if (previousNode != currentNode)
            OnCurrentNodeChanged?.Invoke(previousNode, currentNode);
    }

    public void PrepareForLevelUnload()
    {
        PathNode previousNode = currentNode;
        CancelActiveMovement();

        if (currentNode != null)
            currentNode.SetPlayerPresence(false);

        currentNode = null;
        moveHistory.Clear();

        if (previousNode != null)
            OnCurrentNodeChanged?.Invoke(previousNode, null);
    }

    public void CancelActiveMovement()
    {
        if (moveCancellationSource != null)
        {
            moveCancellationSource.Cancel();
            moveCancellationSource.Dispose();
            moveCancellationSource = null;
        }

        bufferedInput = null;
        isMoving = false;
    }

    #endregion

    #region Input Handling

    private void HandleInput(Direction dir)
    {
        if (!movementEnabled) return;

        // Buffer input if currently moving
        if (isMoving)
        {
            bufferedInput = dir;
            return;
        }

        TryMove(dir);
    }

    private void TryMove(Direction dir)
    {
        if (currentNode == null) return;

        PathNode next = currentNode.GetNode(dir);
        bool isBacktracking = false;

        // Check backward movement
        if (next == null && CanMoveBackward(dir, out PathNode previousNode))
        {
            next = previousNode;
            isBacktracking = true;
        }

        if (next != null)
        {
            if (!next.CanPlayerEnter(this, currentNode))
                return;

            MoveToNodeAsync(next, dir, isBacktracking).Forget();
        }
    }

    #endregion

    #region Backtracking

    private bool CanMoveBackward(Direction dir, out PathNode previousNode)
    {
        previousNode = null;

        if (moveHistory.Count == 0)
            return false;

        MoveHistoryEntry lastMove = moveHistory.Peek();

        if (GetOppositeDirection(lastMove.DirectionTaken) != dir)
            return false;

        previousNode = lastMove.Node;
        return previousNode != null;
    }

    private Direction GetOppositeDirection(Direction direction)
    {
        switch (direction)
        {
            case Direction.Up: return Direction.Down;
            case Direction.Down: return Direction.Up;
            case Direction.Left: return Direction.Right;
            case Direction.Right: return Direction.Left;
            default: return direction;
        }
    }

    #endregion

    #region Movement

    private Transform FindDefaultBodyMesh()
    {
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>();

        for (int i = 0; i < childRenderers.Length; i++)
        {
            Renderer childRenderer = childRenderers[i];
            if (childRenderer != null && childRenderer.transform != transform)
            {
                return childRenderer.transform;
            }
        }

        return null;
    }

    private void SetBodyMoveRotation(Vector3 start, Vector3 end, bool instant)
    {
        if (!rotateBodyOnMove || bodyMesh == null)
        {
            return;
        }

        Vector3 moveDirection = end - start;
        //moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 rollAxis = Vector3.Cross(Vector3.up, moveDirection.normalized);
        float rollDegrees = moveDirection.magnitude * bodyRollDegreesPerUnit;
        if (Mathf.Abs(moveDirection.x) > Mathf.Abs(moveDirection.z))
        {
            rollDegrees *= -1f;
        }

        Quaternion rollRotation = rollAxis.sqrMagnitude > 0.0001f
            ? Quaternion.AngleAxis(rollDegrees, rollAxis.normalized)
            : Quaternion.identity;

        if (Mathf.Abs(bodyRollDegreesPerUnit) > 0.0001f)
        {
            targetBodyRotation = rollRotation * targetBodyRotation;
        }
        else
        {
            targetBodyRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up)
                * Quaternion.Euler(bodyRotationOffset);
        }

        if (instant)
        {
            bodyMesh.rotation = targetBodyRotation;
        }
    }

    private void UpdateBodyRotation()
    {
        if (!rotateBodyOnMove || bodyMesh == null)
        {
            return;
        }

        float lerpAmount = 1f - Mathf.Exp(-bodyRotationLerpSpeed * Time.deltaTime);
        bodyMesh.rotation = Quaternion.Slerp(bodyMesh.rotation, targetBodyRotation, lerpAmount);
    }

    private async UniTaskVoid MoveToNodeAsync(PathNode target, Direction dir, bool isBacktracking)
    {
        CancelActiveMovement();

        if (target == null || currentNode == null)
            return;

        CancellationTokenSource localMoveSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        moveCancellationSource = localMoveSource;
        CancellationToken cancellationToken = localMoveSource.Token;

        isMoving = true;
        if (currentNode != null)
            currentNode.SetPlayerPresence(false);

        // Handle history
        if (isBacktracking)
        {
            moveHistory.Pop();
        }
        else
        {
            moveHistory.Push(new MoveHistoryEntry(currentNode, dir));
        }

        Vector3 start = transform.position;
        Vector3 end = target.transform.position;

        PlayMovementAudio();

        transform.position = start;
        SetBodyMoveRotation(start, end, false);

        float t = 0f;

        try
        {
            while (t < 1f)
            {
                cancellationToken.ThrowIfCancellationRequested();

                t += Time.deltaTime * moveSpeed;
                transform.position = Vector3.Lerp(start, end, t);
                UpdateBodyRotation();
                await UniTask.Yield(cancellationToken);
            }

            if (target == null)
                return;

            transform.position = end;
            UpdateBodyRotation();

            PathNode previousNode = currentNode;
            currentNode = target;
            if (currentNode != null)
                currentNode.SetPlayerPresence(true);

            OnCurrentNodeChanged?.Invoke(previousNode, currentNode);

            if (target == null)
                return;

            NodeInteraction[] interactions = target.GetComponents<NodeInteraction>();
            for (int i = 0; i < interactions.Length; i++)
            {
                if (interactions[i] != null)
                    interactions[i].Trigger(this);
            }

            //  Handle buffered input (makes it feel smooth like CubeMovement)
            if (bufferedInput.HasValue)
            {
                Direction nextDir = bufferedInput.Value;
                bufferedInput = null;
                HandleInput(nextDir);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            isMoving = false;

            if (moveCancellationSource == localMoveSource)
            {
                moveCancellationSource.Dispose();
                moveCancellationSource = null;
            }
        }
    }

    private void PlayMovementAudio()
    {
        if (movementAudio == null)
            return;

        Outsiders.Auditory.AudioManager.Instance?.PlaySFX(movementAudio, transform.position);
    }

    #endregion
}
}
