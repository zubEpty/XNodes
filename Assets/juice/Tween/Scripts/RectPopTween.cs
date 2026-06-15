using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public class RectPopTween : MonoBehaviour
{
    public enum PopMode
    {
        Up,
        Down,
        UpAndDown
    }

    [Header("Target")]
    [SerializeField] private RectTransform _target;
    [SerializeField] private bool _playOnEnable = true;
    [SerializeField] private bool _resetToInitialPositionBeforePlay = true;
    [SerializeField] private bool _recacheInitialPositionOnPlay = false;

    [Header("Pop")]
    [SerializeField] private PopMode _popMode = PopMode.UpAndDown;
    [SerializeField] private Vector2 _popDirection = Vector2.up;
    [SerializeField] private float _popDistance = 12f;

    [Header("Timing")]
    [SerializeField] private float _duration = 0.4f;
    [SerializeField] private float _startDelay = 0f;
    [SerializeField] private Ease _ease = Ease.InOutSine;
    [SerializeField] private bool _useUnscaledTime = true;

    [Header("Loop")]
    [SerializeField] private int _loopCount = -1;
    [SerializeField] private LoopType _loopType = LoopType.Restart;

    private Tween _popTween;
    private Vector2 _initialAnchoredPosition;

    private void Awake()
    {
        if (_target == null)
        {
            _target = transform as RectTransform;
        }

        CacheInitialPosition();
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

        if (_recacheInitialPositionOnPlay)
        {
            CacheInitialPosition();
        }

        if (_resetToInitialPositionBeforePlay)
        {
            _target.anchoredPosition = _initialAnchoredPosition;
        }

        Vector2 direction = _popDirection.sqrMagnitude > 0.0001f ? _popDirection.normalized : Vector2.up;
        Vector2 popOffset = direction * Mathf.Max(0f, _popDistance);

        switch (_popMode)
        {
            case PopMode.Up:
                _popTween = CreateSingleDirectionTween(_initialAnchoredPosition + popOffset);
                break;

            case PopMode.Down:
                _popTween = CreateSingleDirectionTween(_initialAnchoredPosition - popOffset);
                break;

            default:
                _popTween = CreateUpAndDownTween(popOffset);
                break;
        }
    }

    [ContextMenu("Stop")]
    public void Stop()
    {
        if (_popTween != null)
        {
            if (_popTween.IsActive())
            {
                _popTween.Kill();
            }

            _popTween = null;
        }
    }

    private Tween CreateSingleDirectionTween(Vector2 targetPosition)
    {
        if (_duration <= 0f)
        {
            return null;
        }

        return _target
            .DOAnchorPos(targetPosition, _duration)
            .SetEase(_ease)
            .SetLoops(_loopCount, _loopType)
            .SetUpdate(_useUnscaledTime)
            .SetDelay(_startDelay)
            .SetTarget(_target);
    }

    private Tween CreateUpAndDownTween(Vector2 popOffset)
    {
        if (_duration <= 0f)
        {
            return null;
        }

        float halfDuration = _duration * 0.5f;

        return _target
            .DOAnchorPos(_initialAnchoredPosition + popOffset, halfDuration)
            .SetEase(_ease)
            .SetLoops(_loopCount, LoopType.Yoyo)
            .SetUpdate(_useUnscaledTime)
            .SetDelay(_startDelay)
            .SetTarget(_target);
    }

    private void CacheInitialPosition()
    {
        if (_target != null)
        {
            _initialAnchoredPosition = _target.anchoredPosition;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _duration = Mathf.Max(0f, _duration);
        _startDelay = Mathf.Max(0f, _startDelay);
        _popDistance = Mathf.Max(0f, _popDistance);

        if (_target == null)
        {
            _target = transform as RectTransform;
        }
    }
#endif
}
