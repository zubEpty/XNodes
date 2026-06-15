using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Outsiders.UI
{
    [DisallowMultipleComponent]
    public sealed class PauseMenuAnimator : MonoBehaviour
    {
        private sealed class ButtonAnimTarget
        {
            public RectTransform RectTransform;
            public CanvasGroup CanvasGroup;
        }

        [Header("Root Canvas")]
        [SerializeField] private CanvasGroup _rootCanvasGroup;

        [Header("Overlay")]
        [SerializeField] private CanvasGroup _overlayCanvasGroup;
        [SerializeField, Min(0f)] private float _overlayFadeInDuration = 0.2f;
        [SerializeField, Min(0f)] private float _overlayFadeOutDuration = 0.15f;
        [SerializeField, Range(0f, 1f)] private float _overlayVisibleAlpha = 1f;
        [SerializeField, Range(0f, 1f)] private float _overlayHiddenAlpha = 0f;
        [SerializeField] private Ease _overlayEase = Ease.OutQuad;

        [Header("Title")]
        [SerializeField] private RectTransform _pauseTitleRectTransform;
        [SerializeField, Min(0f)] private float _titleTravelDuration = 0.45f;
        [SerializeField, Min(0f)] private float _titleScaleDuration = 0.35f;
        [SerializeField, Min(0f)] private float _titleStartOffsetY = 240f;
        [SerializeField, Range(0f, 1f)] private float _titleStartScaleMultiplier = 0.9f;
        [SerializeField] private Ease _titleMoveEase = Ease.OutBack;
        [SerializeField] private Ease _titleScaleEase = Ease.OutBack;

        [Header("Buttons")]
        [SerializeField] private List<RectTransform> _buttons = new();
        [SerializeField, Min(0f)] private float _buttonPopDuration = 0.22f;
        [SerializeField, Min(0f)] private float _buttonStaggerDelay = 0.08f;
        [SerializeField, Min(0f)] private float _buttonCloseStaggerDelay = 0.05f;
        [SerializeField] private Ease _buttonEase = Ease.OutBack;
        [SerializeField] private Ease _buttonCloseEase = Ease.InBack;

        [Header("Runtime")]
        [SerializeField] private bool _useUnscaledTime = true;

        [SerializeField] private RectPopTween _rectPop;

        private readonly List<ButtonAnimTarget> _buttonAnimTargets = new();
        private Vector2 _titleTargetAnchoredPosition;
        private Vector3 _titleTargetScale;
        private Tween _currentTween;

        private void Awake()
        {
            if (_rootCanvasGroup == null)
            {
                TryGetComponent(out _rootCanvasGroup);
            }

            CacheTargets();
            ApplyHiddenState();
        }

        private void OnDisable()
        {
            KillCurrentTween();
        }

        public void OpenFromUI()
        {
            PlayOpenAnimationAsync().Forget();
        }

        public void CloseFromUI()
        {
            PlayCloseAnimationAsync().Forget();
        }

        public UniTask PlayOpenAnimationAsync(CancellationToken cancellationToken = default)
        {
            KillCurrentTween();
            PrepareOpenState();

            var tween = BuildOpenSequence();
            _currentTween = tween;
            
            return AwaitTweenAsync(tween, cancellationToken);
        }

        public UniTask PlayCloseAnimationAsync(CancellationToken cancellationToken = default)
        {
            KillCurrentTween();
            SetRootCanvasVisible(true, false);

            var tween = BuildCloseSequence();
            _currentTween = tween;

            return AwaitTweenAsync(tween, cancellationToken);
        }

        public void SnapOpenState()
        {
            KillCurrentTween();
            ApplyShownState();
        }

        public void SnapClosedState()
        {
            KillCurrentTween();
            ApplyHiddenState();
        }

        private void CacheTargets()
        {
            if (_pauseTitleRectTransform != null)
            {
                _titleTargetAnchoredPosition = _pauseTitleRectTransform.anchoredPosition;
                _titleTargetScale = _pauseTitleRectTransform.localScale;
            }

            _buttonAnimTargets.Clear();

            for (var index = 0; index < _buttons.Count; index++)
            {
                var button = _buttons[index];
                if (button == null)
                {
                    continue;
                }

                _buttonAnimTargets.Add(new ButtonAnimTarget
                {
                    RectTransform = button,
                    CanvasGroup = button.GetComponent<CanvasGroup>(),
                });
            }
        }

        private void ApplyHiddenState()
        {
            SetRootCanvasVisible(false, false);

            if (_overlayCanvasGroup != null)
            {
                _overlayCanvasGroup.alpha = _overlayHiddenAlpha;
                _overlayCanvasGroup.interactable = false;
                _overlayCanvasGroup.blocksRaycasts = false;
            }

            if (_pauseTitleRectTransform != null)
            {
                _pauseTitleRectTransform.anchoredPosition = GetHiddenTitlePosition();
                _pauseTitleRectTransform.localScale = GetHiddenTitleScale();
            }

            for (var index = 0; index < _buttonAnimTargets.Count; index++)
            {
                SetButtonHiddenState(_buttonAnimTargets[index]);
            }
        }

        private void PrepareOpenState()
        {
            SetRootCanvasVisible(true, false);

            if (_overlayCanvasGroup != null)
            {
                _overlayCanvasGroup.alpha = _overlayHiddenAlpha;
                _overlayCanvasGroup.interactable = false;
                _overlayCanvasGroup.blocksRaycasts = false;
            }

            if (_pauseTitleRectTransform != null)
            {
                _pauseTitleRectTransform.anchoredPosition = GetHiddenTitlePosition();
                _pauseTitleRectTransform.localScale = GetHiddenTitleScale();
            }

            for (var index = 0; index < _buttonAnimTargets.Count; index++)
            {
                SetButtonHiddenState(_buttonAnimTargets[index]);
            }
        }

        private void ApplyShownState()
        {
            SetRootCanvasVisible(true, true);

            if (_overlayCanvasGroup != null)
            {
                _overlayCanvasGroup.alpha = _overlayVisibleAlpha;
                _overlayCanvasGroup.interactable = true;
                _overlayCanvasGroup.blocksRaycasts = true;
            }

            if (_pauseTitleRectTransform != null)
            {
                _pauseTitleRectTransform.anchoredPosition = _titleTargetAnchoredPosition;
                _pauseTitleRectTransform.localScale = _titleTargetScale;
            }

            for (var index = 0; index < _buttonAnimTargets.Count; index++)
            {
                SetButtonShownState(_buttonAnimTargets[index]);
            }
        }

        private Sequence BuildOpenSequence()
        {
            var sequence = DOTween.Sequence()
                .SetUpdate(_useUnscaledTime)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            if (_overlayCanvasGroup != null)
            {
                sequence.Append(_overlayCanvasGroup.DOFade(_overlayVisibleAlpha, _overlayFadeInDuration).SetEase(_overlayEase));
            }

            if (_pauseTitleRectTransform != null)
            {
                sequence.Join(_pauseTitleRectTransform.DOAnchorPos(_titleTargetAnchoredPosition, _titleTravelDuration).SetEase(_titleMoveEase));
                sequence.Join(_pauseTitleRectTransform.DOScale(_titleTargetScale, _titleScaleDuration).SetEase(_titleScaleEase).OnComplete(() =>
                {
                    _rectPop?.Play();
                }));
            }

            if (_buttonAnimTargets.Count > 0)
            {
                sequence.AppendInterval(_buttonStaggerDelay);

                for (var index = 0; index < _buttonAnimTargets.Count; index++)
                {
                    var buttonTarget = _buttonAnimTargets[index];

                    if (index > 0)
                    {
                        sequence.AppendInterval(_buttonStaggerDelay);
                    }

                    sequence.Append(buttonTarget.RectTransform.DOScale(Vector3.one, _buttonPopDuration).SetEase(_buttonEase));

                    var buttonCanvasGroup = buttonTarget.CanvasGroup;
                    if (buttonCanvasGroup != null)
                    {
                        sequence.Join(buttonCanvasGroup.DOFade(1f, _buttonPopDuration).SetEase(Ease.OutQuad));
                    }
                }
            }

            sequence.OnComplete(ApplyShownState);
            return sequence;
        }

        private Sequence BuildCloseSequence()
        {
            var sequence = DOTween.Sequence()
                .SetUpdate(_useUnscaledTime)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            for (var index = _buttonAnimTargets.Count - 1; index >= 0; index--)
            {
                if (index < _buttonAnimTargets.Count - 1)
                {
                    sequence.AppendInterval(_buttonCloseStaggerDelay);
                }

                var buttonTarget = _buttonAnimTargets[index];

                sequence.Append(buttonTarget.RectTransform.DOScale(Vector3.zero, _buttonPopDuration).SetEase(_buttonCloseEase));

                var buttonCanvasGroup = buttonTarget.CanvasGroup;
                if (buttonCanvasGroup != null)
                {
                    sequence.Join(buttonCanvasGroup.DOFade(0f, _buttonPopDuration).SetEase(Ease.OutQuad));
                }
            }

            if (_pauseTitleRectTransform != null)
            {
                sequence.Join(_pauseTitleRectTransform.DOAnchorPos(GetHiddenTitlePosition(), _titleTravelDuration).SetEase(Ease.InBack));
                sequence.Join(_pauseTitleRectTransform.DOScale(GetHiddenTitleScale(), _titleScaleDuration).SetEase(Ease.InBack));
            }

            if (_overlayCanvasGroup != null)
            {
                sequence.Append(_overlayCanvasGroup.DOFade(_overlayHiddenAlpha, _overlayFadeOutDuration).SetEase(_overlayEase));
            }

            sequence.OnComplete(ApplyHiddenState);
            return sequence;
        }

        private void SetRootCanvasVisible(bool isVisible, bool isInteractable)
        {
            if (_rootCanvasGroup == null)
            {
                return;
            }

            _rootCanvasGroup.alpha = isVisible ? 1f : 0f;
            _rootCanvasGroup.interactable = isVisible && isInteractable;
            _rootCanvasGroup.blocksRaycasts = isVisible && isInteractable;
        }

        private Vector2 GetHiddenTitlePosition()
        {
            return _titleTargetAnchoredPosition + new Vector2(0f, _titleStartOffsetY);
        }

        private Vector3 GetHiddenTitleScale()
        {
            return _titleTargetScale * _titleStartScaleMultiplier;
        }

        private void SetButtonHiddenState(ButtonAnimTarget buttonTarget)
        {
            if (buttonTarget == null || buttonTarget.RectTransform == null)
            {
                return;
            }

            buttonTarget.RectTransform.localScale = Vector3.zero;

            var buttonCanvasGroup = buttonTarget.CanvasGroup;
            if (buttonCanvasGroup != null)
            {
                buttonCanvasGroup.alpha = 0f;
                buttonCanvasGroup.interactable = false;
                buttonCanvasGroup.blocksRaycasts = false;
            }
        }

        private void SetButtonShownState(ButtonAnimTarget buttonTarget)
        {
            if (buttonTarget == null || buttonTarget.RectTransform == null)
            {
                return;
            }

            buttonTarget.RectTransform.localScale = Vector3.one;

            var buttonCanvasGroup = buttonTarget.CanvasGroup;
            if (buttonCanvasGroup != null)
            {
                buttonCanvasGroup.alpha = 1f;
                buttonCanvasGroup.interactable = true;
                buttonCanvasGroup.blocksRaycasts = true;
            }
        }

        private async UniTask AwaitTweenAsync(Tween tween, CancellationToken cancellationToken)
        {
            if (tween == null)
            {
                return;
            }

            try
            {
                await tween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(cancellationToken);
            }
            finally
            {
                if (_currentTween == tween)
                {
                    _currentTween = null;
                }
            }
        }

        private void KillCurrentTween()
        {
            if (_currentTween == null)
            {
                return;
            }
            _rectPop?.Stop();
            _currentTween.Kill();
            _currentTween = null;
        }
    }
}
