using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Dispatch.Gameplay
{
public class DispatchLevelTimer : MonoBehaviour
{
    private static readonly int WarningColorId = Shader.PropertyToID("_WarningColor");
    private static readonly int WarningIntensityId = Shader.PropertyToID("_WarningIntensity");

    [Header("Runtime")]
    [SerializeField] private DispatchGameStateManager gameStateManager;
    [SerializeField] private DispatchLevelManager levelManager;
    [SerializeField] private bool startOnEnable = true;
    [SerializeField] private bool countWhileGameplayPaused = false;

    [Header("Timer")]
    [SerializeField, Min(0f)] private float durationSeconds = 165f;
    [SerializeField] private GameObject timerRoot;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private string timeFormat = "{0:00}:{1:00}";

    [Header("Score")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private int scorePadding = 5;
    [SerializeField, Min(0)] private int maxCompletionScore = 1000;
    [SerializeField, Min(0)] private int minCompletionScore = 100;
    [SerializeField, Min(0)] private int enemyCatchPenalty = 100;

    [Header("Timeout UI")]
    [SerializeField] private GameObject failedPanelRoot;

    [Header("Low Time Warning")]
    [SerializeField] private bool enableLowTimeWarning = true;
    [SerializeField, Min(0f)] private float lowTimeWarningThresholdSeconds = 10f;
    [SerializeField, Min(0.1f)] private float warningPulseSpeed = 4f;
    [SerializeField] private Color warningColor = new Color(1f, 0.04f, 0.02f, 1f);
    [SerializeField] private Transform borderRoot;
    [SerializeField] private Image[] borderImages;
    [SerializeField] private Renderer circuitSurfaceRenderer;
    [SerializeField, Min(0f)] private float surfaceWarningIntensity = 1.65f;

    [Header("Events")]
    [SerializeField] private UnityEvent onTimerStarted;
    [SerializeField] private UnityEvent onTimerExpired;
    [SerializeField] private UnityEvent onRetry;

    private float remainingSeconds;
    private float elapsedSeconds;
    private int currentScore;
    private bool isRunning;
    private bool hasExpired;
    private bool timerEnabledForCurrentLevel = true;
    private Color[] defaultBorderColors;
    private MaterialPropertyBlock warningPropertyBlock;
    private int lastDisplayedSeconds = -1;
    private float lastAppliedWarningIntensity = -1f;

    public int CurrentScore => currentScore;
    public float ElapsedSeconds => elapsedSeconds;

    void Awake()
    {
        ResolveReferences();
        ResolveWarningReferences();
        CacheDefaultBorderColors();
        ResetTimerState();
    }

    void OnEnable()
    {
        ResolveReferences();

        if (levelManager != null)
        {
            levelManager.OnLevelLoaded += HandleLevelLoaded;
            levelManager.OnLevelCompleted += HandleLevelCompleted;
        }

        ApplyCurrentLevelTimerSettings();

        if (startOnEnable && IsDispatchGameStarted())
            StartTimerForCurrentLevel();
    }

    void OnDisable()
    {
        if (levelManager != null)
        {
            levelManager.OnLevelLoaded -= HandleLevelLoaded;
            levelManager.OnLevelCompleted -= HandleLevelCompleted;
        }
    }

    void Update()
    {
        if (!isRunning || hasExpired)
            return;

        if (!countWhileGameplayPaused && gameStateManager != null && gameStateManager.IsPaused)
            return;

        float deltaTime = Time.deltaTime;
        elapsedSeconds += deltaTime;
        remainingSeconds = Mathf.Max(0f, remainingSeconds - deltaTime);
        RefreshTimerTextIfNeeded();
        RefreshLowTimeWarning();

        if (remainingSeconds <= 0f)
            ExpireTimer();
    }

    public void StartTimer()
    {
        StartTimerForCurrentLevel();
    }

    public void StartTimerForCurrentLevel()
    {
        ResolveReferences();
        ApplyCurrentLevelTimerSettings();

        if (!timerEnabledForCurrentLevel)
        {
            StopTimer();
            ResetTimerState();
            SetTimerVisible(false);
            return;
        }

        ResetTimerState();
        isRunning = true;
        SetTimerVisible(true);
        onTimerStarted?.Invoke();
    }

    public void StopTimer()
    {
        isRunning = false;
        RefreshLowTimeWarning();
    }

    public void RetryCurrentLevel()
    {
        ResolveReferences();

        if (failedPanelRoot != null)
            failedPanelRoot.SetActive(false);

        if (gameStateManager != null)
            gameStateManager.ResumeGameplay();

        if (levelManager != null)
            levelManager.ReloadCurrentLevel();

        onRetry?.Invoke();
    }

    public void SetDuration(float seconds)
    {
        durationSeconds = Mathf.Max(0f, seconds);
        ResetTimerState();
    }

    public void RegisterEnemyCatchPenalty()
    {
        AddScore(-enemyCatchPenalty);
    }

    public void ResetScore()
    {
        currentScore = 0;
        RefreshScoreText();
    }

    public void SetScore(int score)
    {
        currentScore = Mathf.Max(0, score);
        RefreshScoreText();
    }

    private void ExpireTimer()
    {
        hasExpired = true;
        isRunning = false;
        remainingSeconds = 0f;
        RefreshTimerText();
        RefreshLowTimeWarning();

        if (gameStateManager != null)
            gameStateManager.PauseGameplay();

        if (failedPanelRoot != null)
            failedPanelRoot.SetActive(true);

        onTimerExpired?.Invoke();
    }

    private void ResetTimerState()
    {
        remainingSeconds = durationSeconds;
        elapsedSeconds = 0f;
        hasExpired = false;
        lastDisplayedSeconds = -1;

        if (failedPanelRoot != null)
            failedPanelRoot.SetActive(false);

        RefreshTimerText();
        RefreshScoreText();
        RefreshLowTimeWarning();
    }

    private void RefreshTimerText()
    {
        if (timerText == null)
            return;

        int totalSeconds = Mathf.CeilToInt(remainingSeconds);
        lastDisplayedSeconds = totalSeconds;
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = string.Format(timeFormat, minutes, seconds);
    }

    private void RefreshTimerTextIfNeeded()
    {
        int totalSeconds = Mathf.CeilToInt(remainingSeconds);
        if (totalSeconds == lastDisplayedSeconds)
            return;

        RefreshTimerText();
    }

    private void RefreshScoreText()
    {
        if (scoreText != null)
            scoreText.text = currentScore.ToString($"D{scorePadding}");
    }

    private void AddScore(int amount)
    {
        currentScore = Mathf.Max(0, currentScore + amount);
        RefreshScoreText();
    }

    private void ApplyCompletionScore()
    {
        if (!timerEnabledForCurrentLevel)
            return;

        float timePercentRemaining = durationSeconds > 0f
            ? Mathf.Clamp01(remainingSeconds / durationSeconds)
            : 1f;

        int earnedScore = Mathf.RoundToInt(Mathf.Lerp(minCompletionScore, maxCompletionScore, timePercentRemaining));
        AddScore(earnedScore);
    }

    private void HandleLevelLoaded(DispatchNodeLevelData levelData, int levelIndex)
    {
        ApplyLevelTimerSettings(levelData);

        if (IsDispatchGameStarted())
            StartTimerForCurrentLevel();
        else
            StopTimer();
    }

    private void HandleLevelCompleted(DispatchNodeLevelData levelData, int levelIndex)
    {
        ApplyCompletionScore();
        StopTimer();
    }

    private void ApplyCurrentLevelTimerSettings()
    {
        DispatchNodeLevelData levelData = levelManager != null
            ? levelManager.CurrentLevelData
            : null;

        ApplyLevelTimerSettings(levelData);
    }

    private void ApplyLevelTimerSettings(DispatchNodeLevelData levelData)
    {
        bool isTutorial = levelData != null
            && string.Equals(levelData.LevelSceneName, "Dispatch_node_tutorial_level", System.StringComparison.OrdinalIgnoreCase);

        timerEnabledForCurrentLevel = levelData == null || (levelData.UseTimer && !isTutorial);

        if (levelData != null && levelData.TimerDurationSeconds > 0f)
            durationSeconds = levelData.TimerDurationSeconds;

        SetTimerVisible(timerEnabledForCurrentLevel);
    }

    private bool IsDispatchGameStarted()
    {
        return GameBootstrap.Instance != null && GameBootstrap.Instance.IsGameStarted;
    }

    private void SetTimerVisible(bool isVisible)
    {
        if (timerRoot != null)
        {
            timerRoot.SetActive(isVisible);
            return;
        }

        if (timerText != null)
            timerText.gameObject.SetActive(isVisible);
    }

    private void RefreshLowTimeWarning()
    {
        if ((borderImages == null || borderImages.Length == 0 || circuitSurfaceRenderer == null) && Application.isPlaying)
            ResolveWarningReferences();

        if (borderImages != null && (defaultBorderColors == null || defaultBorderColors.Length != borderImages.Length))
            CacheDefaultBorderColors();

        float intensity = 0f;

        if (enableLowTimeWarning && isRunning && !hasExpired && remainingSeconds <= lowTimeWarningThresholdSeconds)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * warningPulseSpeed * Mathf.PI * 2f);
            float urgency = lowTimeWarningThresholdSeconds <= 0f
                ? 1f
                : 1f - Mathf.Clamp01(remainingSeconds / lowTimeWarningThresholdSeconds);

            intensity = Mathf.Lerp(0.25f, 1f, pulse) * Mathf.Lerp(0.55f, 1f, urgency);
        }

        if (Mathf.Abs(intensity - lastAppliedWarningIntensity) < 0.001f)
            return;

        lastAppliedWarningIntensity = intensity;
        ApplyBorderWarning(intensity);
        ApplySurfaceWarning(intensity);
    }

    private void ApplyBorderWarning(float intensity)
    {
        if (borderImages == null)
            return;

        for (int i = 0; i < borderImages.Length; i++)
        {
            Image borderImage = borderImages[i];
            if (borderImage == null)
                continue;

            Color defaultColor = defaultBorderColors != null && i < defaultBorderColors.Length
                ? defaultBorderColors[i]
                : borderImage.color;

            borderImage.color = Color.Lerp(defaultColor, warningColor, intensity);
        }
    }

    private void ApplySurfaceWarning(float intensity)
    {
        if (circuitSurfaceRenderer == null)
            return;

        warningPropertyBlock ??= new MaterialPropertyBlock();
        circuitSurfaceRenderer.GetPropertyBlock(warningPropertyBlock);
        warningPropertyBlock.SetColor(WarningColorId, warningColor);
        warningPropertyBlock.SetFloat(WarningIntensityId, intensity * surfaceWarningIntensity);
        circuitSurfaceRenderer.SetPropertyBlock(warningPropertyBlock);
    }

    private void ResolveReferences()
    {
        if (gameStateManager == null)
            gameStateManager = DispatchGameStateManager.Instance;

        if (levelManager == null)
            levelManager = DispatchLevelManager.Instance;

        if (gameStateManager == null && GameBootstrap.Instance != null)
            gameStateManager = GameBootstrap.Instance.gameStateManager;
    }

    private void ResolveWarningReferences()
    {
        if (borderRoot == null)
        {
            GameObject borderObject = GameObject.Find("Canvas_Border");
            if (borderObject != null)
                borderRoot = borderObject.transform;
        }

        if ((borderImages == null || borderImages.Length == 0) && borderRoot != null)
            borderImages = borderRoot.GetComponentsInChildren<Image>(true);

        if (circuitSurfaceRenderer == null)
        {
            DispatchNodePlayerGlowBinder glowBinder = FindFirstObjectByType<DispatchNodePlayerGlowBinder>();
            if (glowBinder != null)
                circuitSurfaceRenderer = glowBinder.GetComponent<Renderer>();
        }
    }

    private void CacheDefaultBorderColors()
    {
        if (borderImages == null)
        {
            defaultBorderColors = System.Array.Empty<Color>();
            return;
        }

        defaultBorderColors = new Color[borderImages.Length];
        for (int i = 0; i < borderImages.Length; i++)
            defaultBorderColors[i] = borderImages[i] != null ? borderImages[i].color : Color.white;
    }
}
}
