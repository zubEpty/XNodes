using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Outsiders.Auditory;

namespace Dispatch.Gameplay
{
public class DispatchNodeCompletionPresenter : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private DispatchGameStateManager gameStateManager;
    [SerializeField] private bool pauseGameplayDuringSequence = true;
    [SerializeField] private bool resumeGameplayAfterSuccessPopupCloses = true;

    [Header("Console UI")]
    [SerializeField] private GameObject consoleWindowRoot;
    [SerializeField] private CanvasGroup consoleCanvasGroup;
    [SerializeField] private TMP_Text consoleText;

    [Header("Success UI")]
    [SerializeField] private GameObject successWindowRoot;
    [SerializeField] private CanvasGroup successCanvasGroup;
    [SerializeField] private TMP_Text successTitleText;
    [SerializeField] private string successTitle = "BREACH SUCCESSFUL";
    [SerializeField] private TMP_Text successMessageText;
    [SerializeField] private string successMessage = "SYSTEM ACCESS GRANTED";

    [SerializeField] private Button _accessExitButton;
    [SerializeField] private AudioEventSO grantAccessButtonAudio;

    [Header("Sequence Content")]
    [SerializeField] private string[] consoleLines =
    {
        "EXECUTING PAYLOAD",
        "BREACHING FIREWALL",
        "SYSTEM GATEWAY BYPASSED",
        "BREACHED SYSTEM"
    };

    [Header("Timing")]
    [SerializeField] private float consoleFadeDuration = 0.2f;
    [SerializeField] private float lineTypeDuration = 0.8f;
    [SerializeField] private float lineHoldDuration = 0.35f;
    [SerializeField] private float lineClearDuration = 0.15f;
    [SerializeField] private float successPopupDuration = 0.35f;

    [Header("Events")]
    [SerializeField] private UnityEvent onNodeCompleted;
    [SerializeField] private UnityEvent onSuccessPopupShown;
    [SerializeField] private UnityEvent onSuccessPopupClosed;

    private Sequence activeSequence;
    private bool isRunning;
    private bool hasSubmittedCompletion;
    private string accumulatedConsoleText = string.Empty;
    private int visibleCharacterCount;

    void Awake()
    {
        PrepareUiState();
        RegisterAccessExitButton();
    }

    void OnDestroy()
    {
        UnregisterAccessExitButton();
        KillActiveSequence();
    }

    public void CompleteNode()
    {
        if (isRunning)
            return;

        hasSubmittedCompletion = false;
        DispatchGameStateManager manager = GetGameStateManager();

        if (pauseGameplayDuringSequence && manager != null)
            manager.LockGameplayResume();

        KillActiveSequence();
        PrepareUiState();

        onNodeCompleted?.Invoke();
        isRunning = true;

        activeSequence = DOTween.Sequence().SetUpdate(true);

        activeSequence.AppendCallback(ShowConsoleWindow);

        for (int i = 0; i < consoleLines.Length; i++)
        {
            string line = consoleLines[i];
            activeSequence.AppendCallback(() => BeginTypingLine(line));
            activeSequence.AppendInterval(GetLineTypeDuration(line));
            activeSequence.AppendInterval(lineHoldDuration);
        }

        activeSequence.AppendInterval(lineClearDuration);
        activeSequence.AppendCallback(HideConsoleWindow);
        activeSequence.AppendInterval(consoleFadeDuration);
        activeSequence.AppendCallback(ShowSuccessWindow);
        activeSequence.OnComplete(() =>
        {
            isRunning = false;
            activeSequence = null;
        });
    }

    public void CloseSuccessPopup()
    {
        if (successWindowRoot == null)
        {
            ResumeGameplayIfNeeded();
            return;
        }

        DOTween.Kill(successCanvasGroup);

        Sequence closeSequence = DOTween.Sequence().SetUpdate(true);
        closeSequence.Append(successCanvasGroup != null
            ? successCanvasGroup.DOFade(0f, successPopupDuration)
            : DOVirtual.DelayedCall(successPopupDuration, () => { }));
        closeSequence.AppendCallback(() =>
        {
            successWindowRoot.SetActive(false);
            onSuccessPopupClosed?.Invoke();
            ResumeGameplayIfNeeded();
        });
    }

    public void SetSuccessMessage(string title, string message)
    {
        successTitle = title;
        successMessage = message;

        if (successTitleText != null)
            successTitleText.text = successTitle;

        if (successMessageText != null)
            successMessageText.text = successMessage;
    }

    private void PrepareUiState()
    {
        if (consoleText != null)
        {
            consoleText.text = string.Empty;
            consoleText.maxVisibleCharacters = 0;
        }

        accumulatedConsoleText = string.Empty;
        visibleCharacterCount = 0;

        if (consoleCanvasGroup != null)
            consoleCanvasGroup.alpha = 0f;

        if (successCanvasGroup != null)
            successCanvasGroup.alpha = 0f;

        if (consoleWindowRoot != null)
            consoleWindowRoot.SetActive(false);

        if (successWindowRoot != null)
            successWindowRoot.SetActive(false);
    }

    private void ShowConsoleWindow()
    {
        if (consoleWindowRoot != null)
            consoleWindowRoot.SetActive(true);

        if (consoleCanvasGroup != null)
        {
            consoleCanvasGroup.alpha = 0f;
            consoleCanvasGroup.DOFade(1f, consoleFadeDuration).SetUpdate(true);
        }
    }

    private void HideConsoleWindow()
    {
        if (consoleCanvasGroup != null)
        {
            DOTween.Kill(consoleCanvasGroup);
            consoleCanvasGroup.DOFade(0f, consoleFadeDuration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (consoleWindowRoot != null)
                        consoleWindowRoot.SetActive(false);
                });
            return;
        }

        if (consoleWindowRoot != null)
            consoleWindowRoot.SetActive(false);
    }

    private void BeginTypingLine(string line)
    {
        if (consoleText == null)
            return;

        DOTween.Kill(consoleText);
        int startingVisibleCharacters = visibleCharacterCount;

        accumulatedConsoleText = string.IsNullOrEmpty(accumulatedConsoleText)
            ? line
            : accumulatedConsoleText + "\n" + line;

        consoleText.text = accumulatedConsoleText;
        consoleText.maxVisibleCharacters = startingVisibleCharacters;

        int targetVisibleCharacters = accumulatedConsoleText.Length;

        DOTween.To(
                () => consoleText.maxVisibleCharacters,
                value => consoleText.maxVisibleCharacters = value,
                targetVisibleCharacters,
                GetLineTypeDuration(line))
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(consoleText)
            .OnComplete(() => visibleCharacterCount = targetVisibleCharacters);
    }

    private void ShowSuccessWindow()
    {
        if (successTitleText != null)
            successTitleText.text = successTitle;

        if (successMessageText != null)
            successMessageText.text = successMessage;

        if (successWindowRoot != null)
            successWindowRoot.SetActive(true);

        if (successCanvasGroup != null)
        {
            successCanvasGroup.alpha = 0f;
            successCanvasGroup.DOFade(1f, successPopupDuration).SetUpdate(true);
        }

        onSuccessPopupShown?.Invoke();
    }

    private float GetLineTypeDuration(string line)
    {
        if (string.IsNullOrEmpty(line))
            return lineTypeDuration;

        return Mathf.Max(0.05f, lineTypeDuration);
    }

    private DispatchGameStateManager GetGameStateManager()
    {
        if (gameStateManager != null)
            return gameStateManager;

        if (GameBootstrap.Instance != null && GameBootstrap.Instance.gameStateManager != null)
            return GameBootstrap.Instance.gameStateManager;

        return DispatchGameStateManager.Instance;
    }

    private void ResumeGameplayIfNeeded()
    {
        if (!resumeGameplayAfterSuccessPopupCloses)
            return;

        DispatchGameStateManager manager = GetGameStateManager();

        if (manager != null)
            manager.ResumeGameplay();
    }

    private void KillActiveSequence()
    {
        if (activeSequence != null)
        {
            activeSequence.Kill();
            activeSequence = null;
        }

        if (consoleText != null)
            DOTween.Kill(consoleText);

        if (consoleCanvasGroup != null)
            DOTween.Kill(consoleCanvasGroup);

        if (successCanvasGroup != null)
            DOTween.Kill(successCanvasGroup);

        isRunning = false;
    }

    private void RegisterAccessExitButton()
    {
        ResolveAccessExitButton();

        if (_accessExitButton == null)
        {
            Debug.LogWarning($"{nameof(DispatchNodeCompletionPresenter)} could not find an access exit button.", this);
            return;
        }

        _accessExitButton.onClick.RemoveListener(LoadNextLevelFromSuccessPopup);
        _accessExitButton.onClick.AddListener(LoadNextLevelFromSuccessPopup);
    }

    private void UnregisterAccessExitButton()
    {
        if (_accessExitButton != null)
            _accessExitButton.onClick.RemoveListener(LoadNextLevelFromSuccessPopup);
    }

    private void ResolveAccessExitButton()
    {
        if (_accessExitButton != null || successWindowRoot == null)
            return;

        Button[] buttons = successWindowRoot.GetComponentsInChildren<Button>(true);
        if (buttons.Length == 0)
            return;

        for (int i = 0; i < buttons.Length; i++)
        {
            string buttonName = buttons[i].name.ToLowerInvariant();
            if (buttonName.Contains("exit") || buttonName.Contains("access"))
            {
                _accessExitButton = buttons[i];
                return;
            }
        }

        if (buttons.Length == 1)
            _accessExitButton = buttons[0];
    }

    private void LoadNextLevelFromSuccessPopup()
    {
        if (hasSubmittedCompletion)
        {
            Debug.LogWarning("[DispatchNodeCompletionPresenter] Ignored duplicate completion submit.");
            return;
        }

        hasSubmittedCompletion = true;
        PlayGrantAccessButtonAudio();

        if (successWindowRoot != null)
            successWindowRoot.SetActive(false);

        onSuccessPopupClosed?.Invoke();
        ResumeGameplayIfNeeded();

        DispatchLevelManager manager = DispatchLevelManager.Instance;
        if (manager != null)
        {
            manager.LoadNextLevel();
            return;
        }

        GameBootstrap.Instance?.NotifyGameWon();
    }

    private void PlayGrantAccessButtonAudio()
    {
        if (grantAccessButtonAudio == null)
            return;

        Outsiders.Auditory.AudioManager.Instance?.PlayUI(grantAccessButtonAudio);
    }
}
}
