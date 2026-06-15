using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace Dispatch.Gameplay
{
public class SwipeInputHandler : MonoBehaviour
{
    private NodeInputActions input;

    private Vector2 startPos;
    private Vector2 currentPos;

    public float minSwipeDistance = 50f;

    public Action<Direction> OnSwipe;
    public Action<Direction> OnMove;

    private void Awake()
    {
        input = new NodeInputActions();
    }

    private void OnEnable()
    {
        input.Enable();

        input.Gameplay.Press.started += OnPressStarted;
        input.Gameplay.Press.canceled += OnPressCanceled;

        input.Gameplay.Move.performed += OnMovePerformed;
    }

    private void OnDisable()
    {
        input.Gameplay.Press.started -= OnPressStarted;
        input.Gameplay.Press.canceled -= OnPressCanceled;
        input.Gameplay.Move.performed -= OnMovePerformed;

        input.Disable();
    }

    private void OnPressStarted(InputAction.CallbackContext ctx)
    {
        startPos = input.Gameplay.Position.ReadValue<Vector2>();
    }

    private void OnPressCanceled(InputAction.CallbackContext ctx)
    {
        currentPos = input.Gameplay.Position.ReadValue<Vector2>();
        DetectSwipe();
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        Vector2 value = ctx.ReadValue<Vector2>();
        if (value == Vector2.zero) return;

        Direction dir = GetDirection(value);
        OnMove?.Invoke(dir);
    }

    private void DetectSwipe()
    {
        Vector2 delta = currentPos - startPos;

        if (delta.magnitude < minSwipeDistance)
            return;

        Direction dir = GetDirection(delta);
        OnSwipe?.Invoke(dir);

        Debug.Log($"dir {dir}");
    }

    private Direction GetDirection(Vector2 inputDir)
    {
        if (Mathf.Abs(inputDir.x) > Mathf.Abs(inputDir.y))
            return inputDir.x > 0 ? Direction.Right : Direction.Left;
        else
            return inputDir.y > 0 ? Direction.Up : Direction.Down;
    }
}
}
