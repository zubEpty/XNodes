using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SimpleUIHoverTween : MonoBehaviour
{
    [Header("Hover Movement")]
    public bool EnableHoverMove = true;
    public Vector2 HoverDirection = Vector2.up;
    public float HoverDistance = 10f;
    public float HoverDuration = 2f;

    [Header("Scale Pulse")]
    public bool EnableScalePulse = true;
    public float ScaleUp = 1.15f;
    public float ScaleDuration = 2f;

    [Header("Tween Settings")]
    public Ease EaseType = Ease.InOutSine;

    private RectTransform _rect;
    private Vector2 _startAnchoredPos;
    private Vector3 _startScale;

    private Tween _moveTween;
    private Tween _scaleTween;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _startAnchoredPos = _rect.anchoredPosition;
        _startScale = _rect.localScale;
    }

    private void OnEnable()
    {
        PlayTweens();
    }

    private void OnDisable()
    {
        KillTweens();
        ResetTransform();
    }

    private void OnDestroy()
    {
        KillTweens();
    }

    public void PlayTweens()
    {
        KillTweens();

        if (EnableHoverMove)
        {
            Vector2 targetPos =
                _startAnchoredPos +
                HoverDirection.normalized * HoverDistance;

            _moveTween = _rect
                .DOAnchorPos(targetPos, HoverDuration)
                .SetEase(EaseType)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        if (EnableScalePulse)
        {
            Vector3 scaleUpTarget = _startScale * ScaleUp;

            _scaleTween = _rect
                .DOScale(scaleUpTarget, ScaleDuration * 0.5f)
                .SetEase(EaseType)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }
    }

    public void KillTweens()
    {
        _moveTween?.Kill();
        _scaleTween?.Kill();

        _moveTween = null;
        _scaleTween = null;
    }

    private void ResetTransform()
    {
        _rect.anchoredPosition = _startAnchoredPos;
        _rect.localScale = _startScale;
    }
}