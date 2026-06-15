using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class RectFadeTween : MonoBehaviour
{
    public enum FadeMode
    {
        FadeIn,
        FadeOut,
        FadeInAndOut
    }

    [Header("Target")]
    [SerializeField] private CanvasGroup _target;
    [SerializeField] private bool _playOnEnable = true;
    [SerializeField] private bool _resetToInitialAlphaBeforePlay = true;

    [Header("Fade")]
    [SerializeField] private FadeMode _fadeMode = FadeMode.FadeInAndOut;
    [SerializeField] [Range(0f, 1f)] private float _fadeAmount = 0.35f;

    [Header("Timing")]
    [SerializeField] private float _duration = 0.4f;
    [SerializeField] private float _startDelay = 0f;
    [SerializeField] private Ease _ease = Ease.InOutSine;
    [SerializeField] private bool _useUnscaledTime = true;

    [Header("Loop")]
    [SerializeField] private int _loopCount = -1;
    [SerializeField] private LoopType _loopType = LoopType.Restart;

    private Tween _fadeTween;
    private float _initialAlpha;

    private void Awake()
    {
        if (_target == null)
        {
            _target = GetComponent<CanvasGroup>();
        }

        if (_target != null)
        {
            _initialAlpha = _target.alpha;
        }
    }

    private void OnEnable()
    {
        if (_playOnEnable)
        {
            Play();
        }
    }

    private void OnDisable()
    {
        Stop();
    }

    [ContextMenu("Play")]
    public void Play()
    {
        if (_target == null)
        {
            return;
        }

        Stop();

        if (_resetToInitialAlphaBeforePlay)
        {
            _target.alpha = _initialAlpha;
        }

        float clampedAmount = Mathf.Clamp01(_fadeAmount);

        switch (_fadeMode)
        {
            case FadeMode.FadeIn:
                _fadeTween = CreateSingleDirectionTween(Mathf.Clamp01(_initialAlpha + clampedAmount));
                break;

            case FadeMode.FadeOut:
                _fadeTween = CreateSingleDirectionTween(Mathf.Clamp01(_initialAlpha - clampedAmount));
                break;

            default:
                _fadeTween = CreateInOutTween(clampedAmount);
                break;
        }
    }

    [ContextMenu("Stop")]
    public void Stop()
    {
        if (_fadeTween != null)
        {
            if (_fadeTween.IsActive())
            {
                _fadeTween.Kill();
            }

            _fadeTween = null;
        }
    }

    private Tween CreateSingleDirectionTween(float targetAlpha)
    {
        if (_duration <= 0f)
        {
            return null;
        }

        return _target
            .DOFade(targetAlpha, _duration)
            .SetEase(_ease)
            .SetLoops(_loopCount, _loopType)
            .SetUpdate(_useUnscaledTime)
            .SetDelay(_startDelay)
            .SetTarget(_target);
    }

    private Tween CreateInOutTween(float fadeAmount)
    {
        if (_duration <= 0f)
        {
            return null;
        }

        float halfDuration = _duration * 0.5f;
        float lowerAlpha = Mathf.Clamp01(_initialAlpha - fadeAmount);
        float upperAlpha = Mathf.Clamp01(_initialAlpha + fadeAmount);

        if (Mathf.Approximately(lowerAlpha, upperAlpha))
        {
            return null;
        }

        return _target
            .DOFade(upperAlpha, halfDuration)
            .From(lowerAlpha)
            .SetEase(_ease)
            .SetLoops(_loopCount, LoopType.Yoyo)
            .SetUpdate(_useUnscaledTime)
            .SetDelay(_startDelay)
            .SetTarget(_target);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _duration = Mathf.Max(0f, _duration);
        _startDelay = Mathf.Max(0f, _startDelay);
        _fadeAmount = Mathf.Clamp01(_fadeAmount);

        if (_target == null)
        {
            _target = GetComponent<CanvasGroup>();
        }
    }
#endif
}
