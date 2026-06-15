using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Dispatch.Gameplay
{
public class DispatchLevelManager : MonoBehaviour
{
    public static DispatchLevelManager Instance { get; private set; }

    [Header("Runtime")]
    [SerializeField] private GameBootstrap bootstrap;
    [SerializeField] private bool loadFirstLevelOnStart = true;

    [Header("Level Scenes")]
    [SerializeField] private DispatchNodeLevelData[] levels;
    [SerializeField] private string[] levelSceneNames;
    [SerializeField] private int startingLevelIndex;

    [Header("Events")]
    [SerializeField] private UnityEvent onAllLevelsCompleted;

    public event System.Action<DispatchNodeLevelData, int> OnLevelLoaded;
    public event System.Action<DispatchNodeLevelData, int> OnLevelCompleted;

    private DispatchLevel currentLevel;
    private Scene currentLevelScene;
    private DispatchNodeLevelData currentLevelData;
    private string currentLevelSceneName;
    private int currentLevelIndex = -1;
    private bool isLoading;

    public DispatchLevel CurrentLevel => currentLevel;
    public DispatchNodeLevelData CurrentLevelData => currentLevelData;
    public int CurrentLevelIndex => currentLevelIndex;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple DispatchLevelManager instances found. Keeping the first instance.");
            enabled = false;
            return;
        }

        Instance = this;

        if (bootstrap == null)
            bootstrap = GameBootstrap.Instance;
    }

    void Start()
    {
        if (!loadFirstLevelOnStart)
            return;

        LoadLevel(startingLevelIndex);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void LoadLevel(int levelIndex)
    {
        if (isLoading)
            return;

        int levelCount = GetLevelCount();
        if (levelCount == 0)
        {
            Debug.LogWarning("DispatchLevelManager has no levels configured.");
            return;
        }

        if (levelIndex < 0 || levelIndex >= levelCount)
        {
            CompleteAllLevels();
            return;
        }

        LoadLevelAsync(levelIndex).Forget();
    }

    public void LoadLevel(DispatchNodeLevelData levelData)
    {
        if (isLoading)
            return;

        if (levelData == null)
        {
            Debug.LogWarning("DispatchLevelManager cannot load a null Dispatch Node level.");
            return;
        }

        if (!levelData.HasScene)
        {
            Debug.LogWarning($"Dispatch Node level data '{levelData.name}' has no scene name configured.");
            return;
        }

        LoadLevelAsync(levelData, GetLevelIndex(levelData)).Forget();
    }

    public void LoadNextLevel()
    {
        if (currentLevelIndex < 0)
        {
            CompleteAllLevels();
            return;
        }

        LoadLevel(currentLevelIndex + 1);
    }

    public void ReloadCurrentLevel()
    {
        if (!string.IsNullOrEmpty(currentLevelSceneName))
        {
            LoadLevelAsync(currentLevelData, currentLevelIndex, currentLevelSceneName).Forget();
            return;
        }

        if (currentLevelIndex < 0 && currentLevelData != null)
        {
            LoadLevel(currentLevelData);
            return;
        }

        LoadLevel(currentLevelIndex < 0 ? startingLevelIndex : currentLevelIndex);
    }

    public void CompleteCurrentLevel()
    {
        if (currentLevel != null)
            currentLevel.NotifyCompleted();
    }

    public void CompleteCurrentLevelAndLoadNext()
    {
        CompleteCurrentLevel();
        LoadNextLevel();
    }

    private UniTask LoadLevelAsync(int levelIndex)
    {
        DispatchNodeLevelData levelData = GetLevelData(levelIndex);
        string sceneName = GetLevelSceneName(levelIndex);

        return LoadLevelAsync(levelData, levelIndex, sceneName);
    }

    private UniTask LoadLevelAsync(DispatchNodeLevelData levelData, int levelIndex)
    {
        return LoadLevelAsync(levelData, levelIndex, levelData.LevelSceneName);
    }

    public async UniTask UnloadLevelScene()
    {
        if (currentLevelScene.IsValid() && currentLevelScene.isLoaded)
        {
            PreparePlayerForCurrentLevelUnload();
            bootstrap?.StopBGM();
            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(currentLevelScene);
            if (unloadOperation != null)
                await unloadOperation.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
        }
    } 

    public void UnloadCurrentLevel()
    {
        UnloadLevelScene().Forget();
    }
    
    private async UniTask LoadLevelAsync(DispatchNodeLevelData levelData, int levelIndex, string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("DispatchLevelManager cannot load a level with an empty scene name.");
            return;
        }

        isLoading = true;

        if (currentLevel != null)
        {
            PreparePlayerForCurrentLevelUnload();
            currentLevel.NotifyUnloaded();
        }

        if (currentLevelScene.IsValid() && currentLevelScene.isLoaded)
        {
            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(currentLevelScene);
            if (unloadOperation != null)
                await unloadOperation.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        if (loadOperation == null)
        {
            Debug.LogError($"Could not load dispatch level scene '{sceneName}'. Make sure it is in Build Settings.");
            isLoading = false;
            return;
        }

        await loadOperation.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

        currentLevelScene = SceneManager.GetSceneByName(sceneName);
        currentLevelData = levelData;
        currentLevelSceneName = sceneName;
        currentLevelIndex = levelIndex;
        currentLevel = FindLevelInScene(currentLevelScene);

        if (currentLevel == null)
        {
            Debug.LogError($"Dispatch level scene '{sceneName}' does not contain a DispatchLevel component.");
            isLoading = false;
            return;
        }

        if (bootstrap == null)
            bootstrap = GameBootstrap.Instance;

        DispatchGameStateManager gameStateManager = bootstrap != null
            ? bootstrap.gameStateManager
            : null;

        if (gameStateManager == null)
            gameStateManager = DispatchGameStateManager.Instance;

        SceneManager.SetActiveScene(currentLevelScene);
        currentLevel.BindRuntime(bootstrap);

        if (gameStateManager != null)
        {
            if (bootstrap != null && bootstrap.IsGameStarted)
            {
                gameStateManager.UnlockGameplayResume();
                gameStateManager.ResumeGameplay();
            }
            else
            {
                gameStateManager.PauseGameplay();
            }
        }

        OnLevelLoaded?.Invoke(currentLevelData, currentLevelIndex);
        isLoading = false;
    }

    internal void NotifyLevelCompleted(DispatchLevel level)
    {
        if (level != currentLevel)
            return;

        OnLevelCompleted?.Invoke(currentLevelData, currentLevelIndex);
    }

    private int GetLevelCount()
    {
        if (levels != null && levels.Length > 0)
            return levels.Length;

        return levelSceneNames != null ? levelSceneNames.Length : 0;
    }

    private void CompleteAllLevels()
    {
        onAllLevelsCompleted?.Invoke();
        GameBootstrap.Instance?.NotifyGameWon();
    }

    private DispatchNodeLevelData GetLevelData(int levelIndex)
    {
        if (levels == null || levelIndex < 0 || levelIndex >= levels.Length)
            return null;

        return levels[levelIndex];
    }

    private string GetLevelSceneName(int levelIndex)
    {
        DispatchNodeLevelData levelData = GetLevelData(levelIndex);
        if (levelData != null && levelData.HasScene)
            return levelData.LevelSceneName;

        if (levelSceneNames != null && levelIndex >= 0 && levelIndex < levelSceneNames.Length)
            return levelSceneNames[levelIndex];

        return string.Empty;
    }

    private int GetLevelIndex(DispatchNodeLevelData levelData)
    {
        if (levels == null)
            return -1;

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] == levelData)
                return i;
        }

        return -1;
    }

    private void PreparePlayerForCurrentLevelUnload()
    {
        if (bootstrap == null)
            bootstrap = GameBootstrap.Instance;

        PlayerController player = bootstrap != null
            ? bootstrap.player
            : null;

        if (player == null)
        {
            DispatchGameStateManager gameStateManager = DispatchGameStateManager.Instance;
            player = gameStateManager != null ? gameStateManager.CurrentPlayer : null;
        }

        player?.PrepareForLevelUnload();
    }

    private DispatchLevel FindLevelInScene(Scene scene)
    {
        if (!scene.IsValid())
            return null;

        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            DispatchLevel level = roots[i].GetComponentInChildren<DispatchLevel>(true);
            if (level != null)
                return level;
        }

        return null;
    }
}
}
