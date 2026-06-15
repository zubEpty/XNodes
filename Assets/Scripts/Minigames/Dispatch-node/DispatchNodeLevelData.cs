using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dispatch.Gameplay
{
[CreateAssetMenu(fileName = "DispatchNodeLevelData", menuName = "CyberSecHex/Dispatch Node/Level Data")]
public class DispatchNodeLevelData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string levelId;
    [SerializeField] private string displayName;

    [Header("Scene")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset levelScene;
#endif
    [SerializeField] private string levelSceneName;

    [Header("Timer")]
    [SerializeField] private bool useTimer = true;
    [SerializeField, Min(0f)] private float timerDurationSeconds = 165f;

    public string LevelId => levelId;
    public string DisplayName => displayName;
    public string LevelSceneName => levelSceneName;
    public bool UseTimer => useTimer;
    public float TimerDurationSeconds => timerDurationSeconds;
    public bool HasScene => !string.IsNullOrEmpty(levelSceneName);

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (levelScene != null)
            levelSceneName = levelScene.name;
    }
#endif
}
}
