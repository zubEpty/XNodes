using UnityEngine;

namespace Dispatch.Gameplay
{
public class DispatchNodeLevelSceneLoader : MonoBehaviour
{
    [SerializeField] private DispatchNodeLevelData levelData;
    [SerializeField] private DispatchLevelManager levelManager;

    public void Load()
    {
        if (levelData == null)
        {
            Debug.LogWarning("DispatchNodeLevelSceneLoader has no level data assigned.");
            return;
        }

        DispatchLevelManager manager = GetLevelManager();
        if (manager == null)
        {
            Debug.LogWarning("DispatchNodeLevelSceneLoader could not find a DispatchLevelManager.");
            return;
        }

        manager.LoadLevel(levelData);
    }

    public void Load(DispatchNodeLevelData selectedLevelData)
    {
        levelData = selectedLevelData;
        Load();
    }

    private DispatchLevelManager GetLevelManager()
    {
        if (levelManager != null)
            return levelManager;

        if (DispatchLevelManager.Instance != null)
            levelManager = DispatchLevelManager.Instance;
        else
            levelManager = FindFirstObjectByType<DispatchLevelManager>();

        return levelManager;
    }
}
}
