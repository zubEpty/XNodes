using Outsiders.Auditory;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dispatch.Gameplay
{
    public class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }

        public event Action GameEnded;

        public PlayerController player;
        public SwipeInputHandler input;
        public DispatchGameStateManager gameStateManager;
        [SerializeField] private DispatchLevelManager levelManager;
        [SerializeField] private DispatchEnemyIntroTooltipFlow enemyIntroTooltipFlow;
        public bool IsGameStarted { get; private set; }

        [Header("Audio")]
        [SerializeField] private AudioEventSO backgroundMusic;
        [SerializeField] private bool playBackgroundMusicOnStart = true;
        [SerializeField] private float backgroundMusicFadeTime = 1f;

        [Header("Polish")]
        [SerializeField] private bool playLoadFadeOnStart = true;

        [Header("Debug")]
        [SerializeField] private bool showFpsCounter = true;
        [SerializeField] private DispatchFpsCounter fpsCounter;

        private bool hasWon;
        private bool hasFinished;

        void Awake()
        {
            Instance = this;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            ResolveRuntimeReferences();
        }

        void Start()
        {
            ResolveRuntimeReferences();

            if (playLoadFadeOnStart)
                EnsureLoadFade();

            EnsureFpsCounter();

            if (player != null)
                player.Init(input);

            if (gameStateManager != null && player != null)
                gameStateManager.RegisterPlayer(player);

            if (playBackgroundMusicOnStart)
                PlayBackgroundMusic();

            StartGame();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void PauseGamePlay() => gameStateManager.PauseGameplay();
        public void ResumeGamePlay() => gameStateManager.ResumeGameplay();

        public void RestartPhase()
        {
            DispatchLevelManager manager = DispatchLevelManager.Instance;
            if (manager != null)
            {
                manager.ReloadCurrentLevel();
                return;
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void InitializeGame()
        {
            StartGame();
        }

        public void StartGame()
        {
            ResolveRuntimeReferences();

            bool wasGameStarted = IsGameStarted;
            IsGameStarted = true;
            hasFinished = false;

            if (gameStateManager != null)
            {
                gameStateManager.UnlockGameplayResume();
                gameStateManager.ResumeGameplay();
            }

            DispatchLevelTimer timer = FindFirstObjectByType<DispatchLevelTimer>();
            if (timer != null)
            {
                if (!wasGameStarted)
                {
                    timer.ResetScore();
                }

                timer.StartTimerForCurrentLevel();
            }
        }

        public void StopGame()
        {
            ResolveRuntimeReferences();

            if (gameStateManager != null)
                gameStateManager.PauseGameplay();

            DispatchLevelTimer timer = FindFirstObjectByType<DispatchLevelTimer>();
            if (timer != null)
                timer.StopTimer();
        }

        public float GetScorePercent()
        {
            return hasWon ? 1f : 0f;
        }

        public bool GetWinStatus()
        {
            return hasWon;
        }

        public void NotifyGameWon()
        {
            FinishGame(true);
        }

        public void NotifyGameFailed()
        {
            FinishGame(false);
        }

        public void PlayBackgroundMusic()
        {
            if (backgroundMusic == null)
                return;

            Outsiders.Auditory.AudioManager.Instance?.PlayBGM(backgroundMusic, fadeTime: backgroundMusicFadeTime);
        }

        public void StopBGM()
        {
            Outsiders.Auditory.AudioManager.Instance?.StopBGM(backgroundMusic);
        }
        private void FinishGame(bool isWin)
        {

            if (hasFinished)
                return;

            hasFinished = true;
            hasWon = isWin;
            StopGame();
            GameEnded?.Invoke();
        }

        private void ResolveRuntimeReferences()
        {
            if (player == null)
                player = GetComponentInChildren<PlayerController>(true);

            if (player == null)
                player = FindFirstObjectByType<PlayerController>();

            if (input == null)
                input = GetComponentInChildren<SwipeInputHandler>(true);

            if (input == null)
                input = FindFirstObjectByType<SwipeInputHandler>();

            if (gameStateManager == null)
                gameStateManager = GetComponentInChildren<DispatchGameStateManager>(true);

            if (gameStateManager == null)
                gameStateManager = DispatchGameStateManager.Instance;

            if (gameStateManager == null)
                gameStateManager = FindFirstObjectByType<DispatchGameStateManager>();

            if (gameStateManager == null)
                gameStateManager = gameObject.AddComponent<DispatchGameStateManager>();

            if (levelManager == null)
                levelManager = DispatchLevelManager.Instance;

            if (levelManager == null)
                levelManager = FindFirstObjectByType<DispatchLevelManager>();

            if (enemyIntroTooltipFlow == null)
                enemyIntroTooltipFlow = GetComponentInChildren<DispatchEnemyIntroTooltipFlow>(true);

            if (enemyIntroTooltipFlow == null)
                enemyIntroTooltipFlow = gameObject.AddComponent<DispatchEnemyIntroTooltipFlow>();
        }

        private void EnsureLoadFade()
        {
            DispatchSceneLoadFade loadFade = GetComponent<DispatchSceneLoadFade>();
            if (loadFade == null)
                loadFade = gameObject.AddComponent<DispatchSceneLoadFade>();

            loadFade.PlayFadeOut();
        }

        private void EnsureFpsCounter()
        {
            if (!showFpsCounter)
            {
                if (fpsCounter != null)
                    fpsCounter.enabled = false;

                return;
            }

            if (fpsCounter == null)
                fpsCounter = GetComponent<DispatchFpsCounter>();

            if (fpsCounter == null)
                fpsCounter = gameObject.AddComponent<DispatchFpsCounter>();

            fpsCounter.enabled = true;
        }
    }
}
