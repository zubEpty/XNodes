using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Dispatch.Gameplay
{
public class SequenceInputDisplay : MonoBehaviour
{
    public SequenceUnlockGate gate;
    public SequenceConnectionSwitch connectionSwitch;
    public Image[] slots;
    [Header("Highlight")]
    [SerializeField] private Image[] slotHighlights;
    [SerializeField] private Color activeHighlightColor = Color.white;
    [SerializeField] private Color completedHighlightColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private bool keepCompletedHighlightsVisible = false;

    [Header("Sprites")]
    public Sprite patternSprite;
    public Sprite upSprite;
    public Sprite downSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;

    [Header("Display")]
    public bool hideUnusedSlots = false;
    [SerializeField] private bool showConnectionSwitchPatternWhenWaiting = true;
    public Color placeholderColor = Color.white;
    [Range(0f, 1f)] public float placeholderAlpha = 0.5f;
    public Color normalColor = Color.white;
    [Range(0f, 1f)] public float enteredAlpha = 1f;
    public Color wrongColor = Color.red;
    public float wrongInputDisplayTime = 0.5f;
    [SerializeField] private int wrongBlinkCount = 2;
    [SerializeField] private float wrongBlinkInterval = 0.12f;

    private CancellationTokenSource resetCts;
    private bool isShowingFailure;

    void OnEnable()
    {
        if (gate != null)
        {
            gate.OnSequenceProgressChanged += HandleSequenceProgressChanged;
            gate.OnSequenceFailedWithInput += HandleSequenceFailedWithInput;
            gate.OnSequenceProgressReset += ResetDisplay;
        }

        if (connectionSwitch != null)
        {
            connectionSwitch.OnSequenceProgressChanged += HandleSequenceProgressChanged;
            connectionSwitch.OnSequenceFailedWithInput += HandleSequenceFailedWithInput;
            connectionSwitch.OnSequenceProgressReset += ResetDisplay;
        }

        ResetDisplay();
    }

    void OnDisable()
    {
        if (gate != null)
        {
            gate.OnSequenceProgressChanged -= HandleSequenceProgressChanged;
            gate.OnSequenceFailedWithInput -= HandleSequenceFailedWithInput;
            gate.OnSequenceProgressReset -= ResetDisplay;
        }

        if (connectionSwitch != null)
        {
            connectionSwitch.OnSequenceProgressChanged -= HandleSequenceProgressChanged;
            connectionSwitch.OnSequenceFailedWithInput -= HandleSequenceFailedWithInput;
            connectionSwitch.OnSequenceProgressReset -= ResetDisplay;
        }

        StopResetDelay();
    }


    [ContextMenu("Reset")]
    public void ClearSequence()
    {
        ResetDisplay();
    }

    private void HandleSequenceProgressChanged(Direction[] enteredDirections, int count)
    {
        if (isShowingFailure)
            return;

        RenderSequence(enteredDirections, normalColor);
    }

    private void HandleSequenceFailedWithInput(Direction[] enteredDirections, int count)
    {
        StopResetDelay();

        isShowingFailure = true;
        RenderSequence(enteredDirections, wrongColor);

        resetCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        BlinkWrongInputThenResetAsync(enteredDirections, resetCts).Forget();
    }

    private void ResetDisplay()
    {
        StopResetDelay();

        isShowingFailure = false;
        ClearDisplayImmediately();
        RefreshHighlights(0, true);
    }

    private void ClearDisplayImmediately()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            Sprite waitingSprite = GetWaitingSprite(i);
            slots[i].sprite = waitingSprite;
            slots[i].enabled = waitingSprite != null && !hideUnusedSlots;
            slots[i].color = WithAlpha(placeholderColor, placeholderAlpha);

            if (hideUnusedSlots)
                slots[i].gameObject.SetActive(waitingSprite != null);
        }
    }

    private void RenderSequence(Direction[] enteredDirections, Color displayColor)
    {
        ClearDisplayImmediately();

        if (enteredDirections == null)
            return;

        int length = Mathf.Min(enteredDirections.Length, slots.Length);
        for (int i = 0; i < length; i++)
        {
            if (slots[i] == null)
                continue;

            slots[i].sprite = GetSprite(enteredDirections[i]);
            slots[i].enabled = slots[i].sprite != null;
            slots[i].color = WithAlpha(displayColor, enteredAlpha);

            if (hideUnusedSlots)
                slots[i].gameObject.SetActive(true);
        }

        RefreshHighlights(length, false);
    }

    private async UniTaskVoid BlinkWrongInputThenResetAsync(Direction[] enteredDirections, CancellationTokenSource routineCts)
    {
        try
        {
            int blinkCount = Mathf.Max(1, wrongBlinkCount);

            for (int i = 0; i < blinkCount; i++)
            {
                RenderSequence(enteredDirections, wrongColor);
                await UniTask.Delay((int)(wrongBlinkInterval * 1000f), cancellationToken: routineCts.Token);

                ClearDisplayImmediately();
                RefreshHighlights(0, false);
                await UniTask.Delay((int)(wrongBlinkInterval * 1000f), cancellationToken: routineCts.Token);
            }

            float remainingDelay = wrongInputDisplayTime - (blinkCount * wrongBlinkInterval * 2f);
            if (remainingDelay > 0f)
                await UniTask.Delay((int)(remainingDelay * 1000f), cancellationToken: routineCts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        isShowingFailure = false;
        CompleteResetDelay(routineCts);
        ClearDisplayImmediately();
        RefreshHighlights(0, true);
    }

    private void StopResetDelay()
    {
        if (resetCts == null)
            return;

        resetCts.Cancel();
        resetCts.Dispose();
        resetCts = null;
    }

    private void CompleteResetDelay(CancellationTokenSource routineCts)
    {
        if (resetCts != routineCts)
            return;

        resetCts.Dispose();
        resetCts = null;
    }

    private void RefreshHighlights(int enteredCount, bool showFirstSlot)
    {
        if (slotHighlights == null || slotHighlights.Length == 0)
            return;

        int activeIndex = showFirstSlot ? 0 : enteredCount;

        for (int i = 0; i < slotHighlights.Length; i++)
        {
            Image highlight = slotHighlights[i];
            if (highlight == null)
                continue;

            bool isCompleted = i < enteredCount;
            bool isActive = i == activeIndex && i < slots.Length;
            bool shouldShow = isActive || (keepCompletedHighlightsVisible && isCompleted);

            highlight.gameObject.SetActive(shouldShow);

            if (!shouldShow)
                continue;

            highlight.color = isActive ? activeHighlightColor : completedHighlightColor;
        }
    }

    private Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private Sprite GetWaitingSprite(int slotIndex)
    {
        if (showConnectionSwitchPatternWhenWaiting && gate == null && connectionSwitch != null)
        {
            Direction[] requiredSequence = connectionSwitch.GetRequiredSequence();
            if (requiredSequence != null && slotIndex >= 0 && slotIndex < requiredSequence.Length)
                return GetSprite(requiredSequence[slotIndex]);
        }

        return patternSprite;
    }

    private Sprite GetSprite(Direction direction)
    {
        switch (direction)
        {
            case Direction.Up:
                return upSprite;
            case Direction.Down:
                return downSprite;
            case Direction.Left:
                return leftSprite;
            case Direction.Right:
                return rightSprite;
            default:
                return null;
        }
    }
}
}
