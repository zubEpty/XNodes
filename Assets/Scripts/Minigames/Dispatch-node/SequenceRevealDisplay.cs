using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Dispatch.Gameplay
{
public class SequenceRevealDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SequenceUnlockGate gate;
    [SerializeField] private SequenceConnectionSwitch connectionSwitch;
    [SerializeField] private Image[] slots;

    [Header("Direction Sprites")]
    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite downSprite;
    [SerializeField] private Sprite leftSprite;
    [SerializeField] private Sprite rightSprite;

    [Header("Display")]
    [SerializeField] private bool hideUnusedSlots = false;
    [SerializeField] private Color revealedColor = Color.white;
    [SerializeField] private float dimmedAlpha = 0.35f;
    [SerializeField] private float solvedAlpha = 1f;
    [SerializeField] private Color failedColor = Color.red;
    [SerializeField] private float failedBlinkDuration = 0.12f;
    [SerializeField] private int failedBlinkCount = 2;

    private CancellationTokenSource failedBlinkCts;

    void OnEnable()
    {
        if (gate != null)
        {
            gate.OnSequenceGenerated += HandleSequenceGenerated;
            gate.OnSequenceRevealed += HandleSequenceRevealed;
        }

        if (connectionSwitch != null)
        {
            connectionSwitch.OnSequenceGenerated += HandleSequenceGenerated;
            connectionSwitch.OnSequenceSolved += HandleConnectionSwitchSolved;
            connectionSwitch.OnSequenceFailedWithInput += HandleConnectionSwitchFailed;
            connectionSwitch.OnSequenceProgressReset += HandleConnectionSwitchReset;
        }

        RefreshFromGateState();
    }

    void OnDisable()
    {
        if (gate != null)
        {
            gate.OnSequenceGenerated -= HandleSequenceGenerated;
            gate.OnSequenceRevealed -= HandleSequenceRevealed;
        }

        if (connectionSwitch != null)
        {
            connectionSwitch.OnSequenceGenerated -= HandleSequenceGenerated;
            connectionSwitch.OnSequenceSolved -= HandleConnectionSwitchSolved;
            connectionSwitch.OnSequenceFailedWithInput -= HandleConnectionSwitchFailed;
            connectionSwitch.OnSequenceProgressReset -= HandleConnectionSwitchReset;
        }

        StopFailedBlink();
    }

    private void HandleSequenceGenerated(Direction[] sequence)
    {
        if (connectionSwitch != null && gate == null)
        {
            RenderSequence(sequence, dimmedAlpha, revealedColor);
            return;
        }

        ClearDisplay();
    }

    private void HandleSequenceRevealed(Direction[] sequence)
    {
        RenderSequence(sequence, solvedAlpha, revealedColor);
    }

    private void HandleConnectionSwitchSolved(Direction[] sequence)
    {
        StopFailedBlink();
        RenderSequence(sequence, solvedAlpha, revealedColor);
    }

    private void HandleConnectionSwitchFailed(Direction[] sequence, int count)
    {
        StopFailedBlink();
        failedBlinkCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        BlinkFailedSequenceAsync(sequence, failedBlinkCts).Forget();
    }

    private void HandleConnectionSwitchReset()
    {
        if (connectionSwitch == null || gate != null)
            return;

        StopFailedBlink();
        RenderSequence(connectionSwitch.GetRequiredSequence(), dimmedAlpha, revealedColor);
    }

    private void RefreshFromGateState()
    {
        if (gate == null)
        {
            if (connectionSwitch != null)
            {
                RenderSequence(connectionSwitch.GetRequiredSequence(), dimmedAlpha, revealedColor);
                return;
            }

            ClearDisplay();
            return;
        }

        if (gate.IsSequenceRevealed)
            RenderSequence(gate.GetRequiredSequence(), solvedAlpha, revealedColor);
        else
            ClearDisplay();
    }

    private void ClearDisplay()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            slots[i].sprite = null;
            slots[i].color = revealedColor;
            slots[i].enabled = !hideUnusedSlots;

            if (hideUnusedSlots)
                slots[i].gameObject.SetActive(false);
        }
    }

    private void RenderSequence(Direction[] sequence, float alpha, Color baseColor)
    {
        ClearDisplay();

        if (sequence == null)
            return;

        int length = Mathf.Min(sequence.Length, slots.Length);

        for (int i = 0; i < length; i++)
        {
            if (slots[i] == null)
                continue;

            slots[i].sprite = GetSprite(sequence[i]);
            Color slotColor = baseColor;
            slotColor.a = alpha;
            slots[i].color = slotColor;
            slots[i].enabled = slots[i].sprite != null;

            if (hideUnusedSlots)
                slots[i].gameObject.SetActive(true);
        }
    }

    private async UniTaskVoid BlinkFailedSequenceAsync(Direction[] sequence, CancellationTokenSource routineCts)
    {
        try
        {
            for (int i = 0; i < failedBlinkCount; i++)
            {
                RenderSequence(sequence, solvedAlpha, failedColor);
                await UniTask.Delay((int)(failedBlinkDuration * 1000f), cancellationToken: routineCts.Token);
                RenderSequence(sequence, dimmedAlpha, revealedColor);
                await UniTask.Delay((int)(failedBlinkDuration * 1000f), cancellationToken: routineCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        CompleteFailedBlink(routineCts);
        RenderSequence(connectionSwitch.GetRequiredSequence(), dimmedAlpha, revealedColor);
    }

    private void StopFailedBlink()
    {
        if (failedBlinkCts == null)
            return;

        failedBlinkCts.Cancel();
        failedBlinkCts.Dispose();
        failedBlinkCts = null;
    }

    private void CompleteFailedBlink(CancellationTokenSource routineCts)
    {
        if (failedBlinkCts != routineCts)
            return;

        failedBlinkCts.Dispose();
        failedBlinkCts = null;
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
