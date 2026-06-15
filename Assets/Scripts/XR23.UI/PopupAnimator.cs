using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XR23.UI
{
    public class PopupAnimator : MonoBehaviour
    {
        [Header("Root")] [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform root;

        [Header("Elements")] [SerializeField] private RectTransform bgPanels;
        [SerializeField] private RectTransform textBG;
        [SerializeField] private RectTransform textImage;
        [SerializeField] private RectTransform buttons;

        [Header("Settings")] [SerializeField] private float introDuration = 0.55f;
        [SerializeField] private float outroDuration = 0.35f;
        [SerializeField] private bool _startOnEnable = false;

        private Sequence _loopSequence;
        private Sequence _showSequence;

        private List<Button> _btns = new();

        private void Awake()
        {
            var btns = buttons.GetComponentsInChildren<Button>(includeInactive: true);
            foreach (var button in btns)
            {
                _btns.Add(button);
            }
            PrepareInitialState();
            if (_startOnEnable) ShowAsync().Forget();
        }

        private void PrepareInitialState()
        {
            canvasGroup.alpha = 0f;

            root.localScale = Vector3.one * 0.85f;

            bgPanels.localScale = Vector3.one * 1.1f;
            bgPanels.anchoredPosition = new Vector2(0f, -30f);

            buttons.localScale = Vector3.one * 0.9f;

            SetButtonsState(false);
        }

        private void SetButtonsState(bool active)
        {
            if (_btns != null && _btns.Count > 0)
            {
                foreach (var btn in _btns)
                {
                    btn.interactable = active;
                }
            }

        }
        public async UniTask ShowAsync()
        {
            gameObject.SetActive(true);

            PrepareInitialState();

            StartLoopAnimations();

            _showSequence?.Kill();

            _showSequence = DOTween.Sequence();

            _showSequence
                .SetUpdate(true)

                // Main Fade
                .Join(canvasGroup.DOFade(1f, introDuration))

                // Root Pop
                .Join(
                    root.DOScale(1f, introDuration)
                        .SetEase(Ease.OutBack, 1.4f)
                )

                // BG Slide
                .Join(
                    bgPanels.DOAnchorPosY(0f, introDuration)
                        .SetEase(Ease.OutCubic)
                )

                .Join(
                    bgPanels.DOScale(1f, introDuration)
                        .SetEase(Ease.OutCubic)
                )

                // Buttons delayed pop
                .Append(
                    buttons.DOScale(1f, 0.25f)
                        .SetEase(Ease.OutBack)
                );

            await _showSequence.AsyncWaitForCompletion();

            await UniTask.Delay(TimeSpan.FromSeconds(1f));
            SetButtonsState(true);
        }

        public async UniTask HideAsync()
        {
            _loopSequence?.Kill();
            SetButtonsState(false);
            Sequence hideSequence = DOTween.Sequence();

            hideSequence
                .SetUpdate(true)
                .Join(
                    canvasGroup.DOFade(0f, outroDuration)
                )
                .Join(
                    root.DOScale(0.85f, outroDuration)
                        .SetEase(Ease.InBack)
                );

            await hideSequence.AsyncWaitForCompletion();

            gameObject.SetActive(false);
        }

        private void StartLoopAnimations()
        {
            _loopSequence?.Kill();

            _loopSequence = DOTween.Sequence()
                .SetUpdate(true);

            // Endless rotation
            textBG
                .DORotate(
                    new Vector3(0f, 0f, -360f),
                    12f,
                    RotateMode.FastBeyond360
                )
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true);

            // Gentle premium pulse
            textImage
                .DOScale(1.04f, 1.2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void OnDestroy()
        {
            DOTween.Kill(textBG);
            DOTween.Kill(textImage);

            _showSequence?.Kill();
            _loopSequence?.Kill();
        }
    }
}