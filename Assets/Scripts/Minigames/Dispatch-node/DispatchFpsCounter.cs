using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dispatch.Gameplay
{
public class DispatchFpsCounter : MonoBehaviour
{
    [SerializeField, Min(0.05f)] private float refreshInterval = 0.25f;
    [SerializeField] private Vector2 anchoredPosition = new Vector2(18f, -18f);
    [SerializeField] private Vector2 size = new Vector2(140f, 44f);
    [SerializeField] private int fontSize = 22;
    [SerializeField] private Color textColor = new Color(0.78f, 1f, 0.82f, 1f);
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.58f);

    private Canvas overlayCanvas;
    private TextMeshProUGUI fpsText;
    private float elapsed;
    private int frames;

    private void OnEnable()
    {
        EnsureOverlay();
    }

    private void OnDisable()
    {
        if (overlayCanvas != null)
            overlayCanvas.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (overlayCanvas != null)
            Destroy(overlayCanvas.gameObject);
    }

    private void Update()
    {
        if (fpsText == null)
            EnsureOverlay();

        elapsed += Time.unscaledDeltaTime;
        frames++;

        if (elapsed < refreshInterval)
            return;

        float fps = frames / Mathf.Max(elapsed, 0.0001f);
        fpsText.text = $"FPS: {Mathf.RoundToInt(fps)}";

        elapsed = 0f;
        frames = 0;
    }

    private void EnsureOverlay()
    {
        if (overlayCanvas != null)
        {
            overlayCanvas.gameObject.SetActive(true);
            return;
        }

        GameObject canvasObject = new GameObject("DispatchFpsOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        overlayCanvas = canvasObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = anchoredPosition;
        panelRect.sizeDelta = size;

        Image background = panelObject.GetComponent<Image>();
        background.color = backgroundColor;
        background.raycastTarget = false;

        GameObject textObject = new GameObject("FPS", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panelObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 0f);
        textRect.offsetMax = new Vector2(-10f, 0f);

        fpsText = textObject.GetComponent<TextMeshProUGUI>();
        fpsText.text = "FPS: --";
        fpsText.alignment = TextAlignmentOptions.MidlineLeft;
        fpsText.fontSize = fontSize;
        fpsText.color = textColor;
        fpsText.raycastTarget = false;
    }
}
}
