using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public class RectRotationTween : MonoBehaviour
{
    public enum DurationMode
    {
        FixedDuration,
        UseSpeed
    }

    [Header("Target")]
    [SerializeField] private RectTransform _target;
    [SerializeField] private bool _playOnEnable = true;
    [SerializeField] private bool _resetToInitialRotationBeforePlay = false;

    [Header("Rotation")]
    [SerializeField] private Vector3 _rotationPerCycle = new Vector3(0f, 0f, -360f);
    [SerializeField] private RotateMode _rotateMode = RotateMode.FastBeyond360;

    [Header("Timing")]
    [SerializeField] private DurationMode _durationMode = DurationMode.FixedDuration;
    [SerializeField] private float _cycleDuration = 1f;
    [SerializeField] private float _rotationSpeedDegreesPerSecond = 180f;
    [SerializeField] private float _startDelay = 0f;
    [SerializeField] private Ease _ease = Ease.Linear;
    [SerializeField] private bool _useUnscaledTime = true;

    [Header("Loop")]
    [SerializeField] private int _loopCount = -1;
    [SerializeField] private LoopType _loopType = LoopType.Restart;

    private Tween _rotationTween;
    private Quaternion _initialLocalRotation;

    private void Awake()
    {
        if (_target == null)
        {
            _target = transform as RectTransform;
        }

        if (_target != null)
        {
            _initialLocalRotation = _target.localRotation;
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

        if (_resetToInitialRotationBeforePlay)
        {
            _target.localRotation = _initialLocalRotation;
        }

        float duration = GetCycleDuration();
        if (duration <= 0f)
        {
            return;
        }

        _rotationTween = _target
            .DOLocalRotate(_rotationPerCycle, duration, _rotateMode)
            .SetEase(_ease)
            .SetLoops(_loopCount, _loopType)
            .SetUpdate(_useUnscaledTime)
            .SetDelay(_startDelay)
            .SetTarget(_target);
    }

    [ContextMenu("Stop")]
    public void Stop()
    {
        if (_rotationTween != null)
        {
            if (_rotationTween.IsActive())
            {
                _rotationTween.Kill();
            }

            _rotationTween = null;
        }
    }

    private float GetCycleDuration()
    {
        if (_durationMode == DurationMode.FixedDuration)
        {
            return Mathf.Max(0f, _cycleDuration);
        }

        float cycleAngle = _rotationPerCycle.magnitude;
        if (cycleAngle <= 0f)
        {
            return 0f;
        }

        return cycleAngle / Mathf.Max(0.01f, _rotationSpeedDegreesPerSecond);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _cycleDuration = Mathf.Max(0f, _cycleDuration);
        _rotationSpeedDegreesPerSecond = Mathf.Max(0.01f, _rotationSpeedDegreesPerSecond);
        _startDelay = Mathf.Max(0f, _startDelay);

        if (_target == null)
        {
            _target = transform as RectTransform;
        }
    }
#endif
}
