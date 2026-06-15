using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dispatch.Gameplay
{
public class DispatchEnemyIntroTooltipFlow : MonoBehaviour
{
    [Header("Level")]
    [SerializeField] private string introLevelSceneName = "Dispatch_node_first_lvl";
    [SerializeField] private bool playEveryTimeLevelLoads = true;

    [Header("References")]
    [SerializeField] private GameBootstrap bootstrap;
    [SerializeField] private DispatchLevelManager levelManager;
    [SerializeField] private DispatchGameStateManager gameStateManager;
    [SerializeField] private DispatchNodeCameraFollowTarget cameraFollowTarget;

    [Header("Tooltip")]
    [SerializeField] private GameObject tooltipBubblePrefab;
    [SerializeField] private TMP_Text tooltipPrefabText;
    [TextArea(2, 4)]
    [SerializeField] private string tooltipMessage = "This is corrupted data node, on sight it'll try to chase and corrupts player too";
    [SerializeField] private Vector2 bubbleSize = new Vector2(520f, 120f);
    [SerializeField] private Vector2 bubbleAnchoredPosition = new Vector2(0f, -260f);
    [SerializeField] private Vector2 textPadding = new Vector2(36f, 22f);
    [SerializeField] private Color bubbleFillColor = new Color(0.98f, 0.96f, 0.88f, 1f);
    [SerializeField] private Color bubbleBorderColor = new Color(0.25f, 0.02f, 0.02f, 1f);
    [SerializeField] private Color bubbleTextColor = new Color(0.25f, 0.02f, 0.02f, 1f);
    [SerializeField] private Sprite bubbleSprite;
    [SerializeField] private TMP_FontAsset tooltipFont;
    [SerializeField] private float tooltipFontSize = 28f;
    [SerializeField] private FontStyles tooltipFontStyle = FontStyles.Bold;
    [SerializeField] private float borderThickness = 4f;

    [Header("Timing")]
    [SerializeField] private float cameraSettleDistance = 0.08f;
    [SerializeField] private float maxCameraSettleWait = 2.5f;
    [SerializeField] private float returnSettleWait = 0.5f;

    private CanvasGroup tooltipCanvasGroup;
    private RectTransform tooltipBubbleRect;
    private TMP_Text tooltipText;
    private Button tooltipButton;
    private CancellationTokenSource activeRoutineCts;
    private bool tooltipTapped;
    private bool hasPlayedThisSession;
    private bool isSubscribed;

    void OnEnable()
    {
        ResolveReferences();
        SubscribeToLevelManager();
    }

    void Start()
    {
        ResolveReferences();
        SubscribeToLevelManager();
    }

    void OnDisable()
    {
        UnsubscribeFromLevelManager();

        CancelActiveRoutine();
    }

    private void HandleLevelLoaded(DispatchNodeLevelData levelData, int levelIndex)
    {
        if (!ShouldPlayForLevel(levelData))
            return;

        DispatchEnemyController enemy = FindEnemyInScene(levelData.LevelSceneName);
        if (enemy == null)
        {
            Debug.LogWarning($"Enemy intro skipped. No DispatchEnemyController found in '{levelData.LevelSceneName}'.");
            return;
        }

        CancelActiveRoutine();

        activeRoutineCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        PlayIntroAsync(enemy.transform, activeRoutineCts).Forget();
    }

    private async UniTaskVoid PlayIntroAsync(Transform enemyTarget, CancellationTokenSource routineCts)
    {
        CancellationToken cancellationToken = routineCts.Token;
        ResolveReferences();

        if (bootstrap == null || bootstrap.player == null || cameraFollowTarget == null || enemyTarget == null)
        {
            CompleteActiveRoutine(routineCts);
            return;
        }

        try
        {
            hasPlayedThisSession = true;

            if (gameStateManager != null)
                gameStateManager.PauseGameplay();

            Transform playerTarget = bootstrap.player.transform;
            cameraFollowTarget.SetTarget(enemyTarget, false);

            await WaitForCameraTargetAsync(enemyTarget, maxCameraSettleWait, cancellationToken);

            ShowTooltip();

            while (!tooltipTapped)
                await UniTask.Yield(cancellationToken);

            HideTooltip();

            cameraFollowTarget.SetTarget(playerTarget, false);
            await WaitForCameraTargetAsync(playerTarget, returnSettleWait, cancellationToken);

            if (gameStateManager != null)
                gameStateManager.ResumeGameplay();
        }
        catch (OperationCanceledException)
        {
        }

        CompleteActiveRoutine(routineCts);
    }

    private async UniTask WaitForCameraTargetAsync(Transform target, float maxWait, CancellationToken cancellationToken)
    {
        if (target == null || cameraFollowTarget == null)
            return;

        float elapsed = 0f;
        while (elapsed < maxWait)
        {
            if (Vector3.Distance(cameraFollowTarget.transform.position, cameraFollowTarget.GetDesiredPosition(target)) <= cameraSettleDistance)
                return;

            elapsed += Time.unscaledDeltaTime;
            await UniTask.Yield(cancellationToken);
        }
    }

    private void CancelActiveRoutine()
    {
        if (activeRoutineCts == null)
            return;

        activeRoutineCts.Cancel();
        activeRoutineCts.Dispose();
        activeRoutineCts = null;
    }

    private void CompleteActiveRoutine(CancellationTokenSource routineCts)
    {
        if (activeRoutineCts != routineCts)
            return;

        activeRoutineCts.Dispose();
        activeRoutineCts = null;
    }

    private void ShowTooltip()
    {
        EnsureTooltipUi();
        tooltipTapped = false;

        if (tooltipText != null)
            tooltipText.text = tooltipMessage;

        if (tooltipCanvasGroup != null)
        {
            tooltipCanvasGroup.alpha = 1f;
            tooltipCanvasGroup.interactable = true;
            tooltipCanvasGroup.blocksRaycasts = true;
        }
    }

    private void HideTooltip()
    {
        if (tooltipCanvasGroup == null)
            return;

        tooltipCanvasGroup.alpha = 0f;
        tooltipCanvasGroup.interactable = false;
        tooltipCanvasGroup.blocksRaycasts = false;
    }

    private void EnsureTooltipUi()
    {
        if (tooltipCanvasGroup != null)
            return;

        EnsureEventSystem();

        Canvas canvas = CreateCanvas();

        GameObject bubbleObject = tooltipBubblePrefab != null
            ? Instantiate(tooltipBubblePrefab)
            : CreateDefaultTooltipBubble();

        bubbleObject.name = "EnemyIntroTooltip";
        bubbleObject.transform.SetParent(canvas.transform, false);

        tooltipBubbleRect = bubbleObject.GetComponent<RectTransform>();
        if (tooltipBubbleRect == null)
            tooltipBubbleRect = bubbleObject.AddComponent<RectTransform>();

        ApplyBubbleRect(tooltipBubbleRect);

        tooltipCanvasGroup = bubbleObject.GetComponent<CanvasGroup>();
        if (tooltipCanvasGroup == null)
            tooltipCanvasGroup = bubbleObject.AddComponent<CanvasGroup>();

        tooltipButton = bubbleObject.GetComponent<Button>();
        if (tooltipButton == null)
            tooltipButton = bubbleObject.AddComponent<Button>();

        tooltipButton.onClick.AddListener(HandleTooltipTapped);

        tooltipText = tooltipPrefabText != null
            ? tooltipPrefabText
            : bubbleObject.GetComponentInChildren<TMP_Text>(true);

        if (tooltipText == null)
            tooltipText = CreateTooltipText(bubbleObject.transform);

        ApplyDefaultStyleIfNeeded(bubbleObject);

        HideTooltip();
    }

    private GameObject CreateDefaultTooltipBubble()
    {
        GameObject bubbleObject = new GameObject(
            "EnemyIntroTooltip",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline),
            typeof(CanvasGroup),
            typeof(Button));

        GameObject textObject = new GameObject("Message", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(bubbleObject.transform, false);

        return bubbleObject;
    }

    private TMP_Text CreateTooltipText(Transform parent)
    {
        GameObject textObject = new GameObject("Message", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        return textObject.GetComponent<TMP_Text>();
    }

    private void ApplyBubbleRect(RectTransform bubbleRect)
    {
        bubbleRect.anchorMin = new Vector2(0.5f, 0.5f);
        bubbleRect.anchorMax = new Vector2(0.5f, 0.5f);
        bubbleRect.pivot = new Vector2(0.5f, 0.5f);
        bubbleRect.anchoredPosition = bubbleAnchoredPosition;
        bubbleRect.sizeDelta = bubbleSize;
    }

    private void ApplyDefaultStyleIfNeeded(GameObject bubbleObject)
    {
        Image bubbleImage = bubbleObject.GetComponent<Image>();
        if (bubbleImage != null)
        {
            bubbleImage.color = bubbleFillColor;
            bubbleImage.sprite = bubbleSprite;
            bubbleImage.type = bubbleSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            bubbleImage.raycastTarget = true;

            tooltipButton.transition = Selectable.Transition.ColorTint;
            tooltipButton.targetGraphic = bubbleImage;
        }

        Outline border = bubbleObject.GetComponent<Outline>();
        if (border == null && tooltipBubblePrefab == null)
            border = bubbleObject.AddComponent<Outline>();

        if (border != null)
        {
            border.effectColor = bubbleBorderColor;
            border.effectDistance = new Vector2(borderThickness, -borderThickness);
            border.useGraphicAlpha = false;
        }

        ColorBlock colors = tooltipButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.98f, 0.91f, 1f);
        colors.pressedColor = new Color(0.9f, 0.84f, 0.74f, 1f);
        colors.selectedColor = colors.highlightedColor;
        tooltipButton.colors = colors;

        if (tooltipText == null)
            return;

        RectTransform textRect = tooltipText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textPadding;
        textRect.offsetMax = -textPadding;

        tooltipText.alignment = TextAlignmentOptions.Center;
        tooltipText.color = bubbleTextColor;
        tooltipText.fontSize = tooltipFontSize;
        tooltipText.fontStyle = tooltipFontStyle;
        tooltipText.enableWordWrapping = true;
        tooltipText.raycastTarget = false;

        if (tooltipFont != null)
            tooltipText.font = tooltipFont;
    }

    private void HandleTooltipTapped()
    {
        tooltipTapped = true;
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject(
            "DispatchRuntimeCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private bool ShouldPlayForLevel(DispatchNodeLevelData levelData)
    {
        if (levelData == null || !levelData.HasScene)
            return false;

        if (!playEveryTimeLevelLoads && hasPlayedThisSession)
            return false;

        return string.Equals(levelData.LevelSceneName, introLevelSceneName, System.StringComparison.OrdinalIgnoreCase);
    }

    private DispatchEnemyController FindEnemyInScene(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            DispatchEnemyController enemy = roots[i].GetComponentInChildren<DispatchEnemyController>(true);
            if (enemy != null)
                return enemy;
        }

        return null;
    }

    private void SubscribeToLevelManager()
    {
        if (levelManager == null)
            levelManager = DispatchLevelManager.Instance;

        if (levelManager != null && !isSubscribed)
        {
            levelManager.OnLevelLoaded += HandleLevelLoaded;
            isSubscribed = true;
        }
    }

    private void UnsubscribeFromLevelManager()
    {
        if (levelManager != null && isSubscribed)
            levelManager.OnLevelLoaded -= HandleLevelLoaded;

        isSubscribed = false;
    }

    private void ResolveReferences()
    {
        if (bootstrap == null)
            bootstrap = GameBootstrap.Instance;

        if (levelManager == null)
            levelManager = DispatchLevelManager.Instance;

        if (gameStateManager == null)
            gameStateManager = bootstrap != null ? bootstrap.gameStateManager : DispatchGameStateManager.Instance;

        if (cameraFollowTarget == null)
            cameraFollowTarget = FindFirstObjectByType<DispatchNodeCameraFollowTarget>();
    }
}
}
