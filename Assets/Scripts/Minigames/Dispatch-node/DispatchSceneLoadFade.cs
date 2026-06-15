using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Dispatch.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class DispatchSceneLoadFade : MonoBehaviour
    {
        [SerializeField] private Color fadeColor = new Color(0.02f, 0.08f, 0.16f, 1f);
        [SerializeField, Min(0f)] private float holdTime = 0.08f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.75f;
        [SerializeField] private Ease fadeEase = Ease.OutQuad;
        [SerializeField] private bool playOnStart = true;

        private CanvasGroup canvasGroup;
        private GameObject canvasRoot;
        private Tween fadeTween;

        private void Start()
        {
            if (playOnStart)
                PlayFadeOut();
        }

        private void OnDestroy()
        {
            fadeTween?.Kill();
        }

        [ContextMenu("Play Fade Out")]
        public void PlayFadeOut()
        {
            EnsureOverlay();

            fadeTween?.Kill();
            canvasGroup.alpha = fadeColor.a;
            canvasGroup.blocksRaycasts = true;

            fadeTween = canvasGroup
                .DOFade(0f, fadeDuration)
                .SetDelay(holdTime)
                .SetEase(fadeEase)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    canvasGroup.blocksRaycasts = false;
                    Destroy(canvasRoot);
                });
        }

        private void EnsureOverlay()
        {
            if (canvasGroup != null)
                return;

            canvasRoot = new GameObject("DispatchLoadFadeCanvas");
            Canvas canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            canvasRoot.AddComponent<CanvasScaler>();
            canvasRoot.AddComponent<GraphicRaycaster>();

            GameObject fadeObject = new GameObject("DispatchLoadFade");
            fadeObject.transform.SetParent(canvasRoot.transform, false);

            RectTransform rectTransform = fadeObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Image image = fadeObject.AddComponent<Image>();
            image.color = fadeColor;
            image.raycastTarget = true;

            canvasGroup = fadeObject.AddComponent<CanvasGroup>();
        }
    }
}
