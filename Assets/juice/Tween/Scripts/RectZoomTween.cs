using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public class RectZoomTween : MonoBehaviour
{
    public enum ZoomMode
    {
        ZoomIn,
        ZoomOut,
        ZoomInAndOut
    }

    [Header("Target")]
    [SerializeField] private RectTransform _target;
    [SerializeField] private bool _playOnEnable = true;
    [SerializeField] private bool _resetToInitialScaleBeforePlay = true;

    [Header("Zoom")]
    [SerializeField] private ZoomMode _zoomMode = ZoomMode.ZoomInAndOut;
    [SerializeField] private float _zoomAmount = 0.15f;
    [SerializeField] private bool _useUniformScale = true;
    [SerializeField] private Vector3 _zoomAxisMultiplier = Vector3.one;

    [Header("Timing")]
    [SerializeField] private float _duration = 0.4f;
    [SerializeField] private float _startDelay = 0f;
    [SerializeField] private Ease _ease = Ease.InOutSine;
    [SerializeField] private bool _useUnscaledTime = true;

    [Header("Loop")]
    [SerializeField] private int _loopCount = -1;
    [SerializeField] private LoopType _loopType = LoopType.Restart;

    private Tween _zoomTween;
    private Vector3 _initialScale;

    private void Awake()
    {
        if (_target == null)
        {
            _target = transform as RectTransform;
        }

        if (_target != null)
        {
            _initialScale = _target.localScale;
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

        if (_resetToInitialScaleBeforePlay)
        {
            _target.localScale = _initialScale;
        }

        Vector3 scaleOffset = GetScaleOffset();

        switch (_zoomMode)
        {
            case ZoomMode.ZoomIn:
                _zoomTween = CreateSingleDirectionTween(_initialScale + scaleOffset);
                break;

            case ZoomMode.ZoomOut:
                _zoomTween = CreateSingleDirectionTween(_initialScale - scaleOffset);
                break;

            default:
                _zoomTween = CreateInOutTween(scaleOffset);
                break;
        }
    }

    [ContextMenu("Stop")]
    public void Stop()
    {
        if (_zoomTween != null)
        {
            if (_zoomTween.IsActive())
            {
                _zoomTween.Kill();
            }

            _zoomTween = null;
        }
    }

    private Tween CreateSingleDirectionTween(Vector3 targetScale)
    {
        if (_duration <= 0f)
        {
            return null;
        }

        return _target
            .DOScale(targetScale, _duration)
            .SetEase(_ease)
            .SetLoops(_loopCount, _loopType)
            .SetUpdate(_useUnscaledTime)
            .SetDelay(_startDelay)
            .SetTarget(_target);
    }

    private Tween CreateInOutTween(Vector3 scaleOffset)
    {
        if (_duration <= 0f)
        {
            return null;
        }

        float halfDuration = _duration * 0.5f;

        return _target
            .DOScale(_initialScale + scaleOffset, halfDuration)
            .SetEase(_ease)
            .SetLoops(_loopCount, LoopType.Yoyo)
            .SetUpdate(_useUnscaledTime)
            .SetDelay(_startDelay)
            .SetTarget(_target);
    }

    private Vector3 GetScaleOffset()
    {
        float amount = Mathf.Max(0f, _zoomAmount);

        if (_useUniformScale)
        {
            return Vector3.one * amount;
        }

        return new Vector3(
            amount * _zoomAxisMultiplier.x,
            amount * _zoomAxisMultiplier.y,
            amount * _zoomAxisMultiplier.z
        );
    }

    public void Hide()
    {
        transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InSine).SetUpdate(_useUnscaledTime);
        //transform.localScale = Vector3.zero;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _duration = Mathf.Max(0f, _duration);
        _startDelay = Mathf.Max(0f, _startDelay);
        _zoomAmount = Mathf.Max(0f, _zoomAmount);

        if (_target == null)
        {
            _target = transform as RectTransform;
        }
    }
#endif
}
