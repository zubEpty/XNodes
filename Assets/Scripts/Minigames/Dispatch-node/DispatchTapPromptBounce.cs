using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Dispatch.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class DispatchTapPromptBounce : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField, Min(0f)] private float bounceDistance = 0.1f;
        [SerializeField, Min(0.01f)] private float halfCycleDuration = 0.32f;
        [SerializeField] private Ease ease = Ease.InOutSine;
        [SerializeField] private bool useUnscaledTime = true;

        private Vector2 initialAnchoredPosition;
        private Tween bounceTween;
        private Button button;

        private void Awake()
        {
            if (target == null)
                target = transform as RectTransform;

            if (target != null)
                initialAnchoredPosition = target.anchoredPosition;

            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (button != null)
                button.onClick.AddListener(Stop);
        }

        private void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(Stop);

            Stop();
        }

        private void OnDestroy()
        {
            Stop();
        }


        [ContextMenu("Play Bounce")]
        public void Play()
        {
            EnsureTarget();

            if (target == null)
                return;

            if (!gameObject.activeInHierarchy)
                return;

            Stop();
            initialAnchoredPosition = target.anchoredPosition;

            bounceTween = target
                .DOAnchorPos(initialAnchoredPosition + (Vector2.up * bounceDistance), halfCycleDuration)
                .SetEase(ease)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(useUnscaledTime)
                .SetTarget(target);
        }

        public void Stop()
        {
            if (bounceTween != null)
            {
                if (bounceTween.IsActive())
                    bounceTween.Kill();

                bounceTween = null;
            }

            if (target != null)
                target.anchoredPosition = initialAnchoredPosition;
        }

        private void EnsureTarget()
        {
            if (target == null)
                target = transform as RectTransform;

            if (button == null)
                button = GetComponent<Button>();
        }
    }
}
