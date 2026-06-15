using UnityEngine;

namespace Dispatch.Gameplay
{
public class DispatchNodeHackTrigger : MonoBehaviour
{
    [Header("Dispatch")]
    [SerializeField] private DispatchNodeLevelData levelData;
    [SerializeField] private string hackId;
    [SerializeField] private bool disableAfterCompleted = true;
    [SerializeField] private DispatchLevelManager levelManager;

    [Header("Trigger")]
    [SerializeField] private bool launchOnTriggerEnter = true;
    [SerializeField] private string playerTag = "Player";

    public string HackId => hackId;
    public bool IsCompleted { get; private set; }

    private void OnEnable()
    {
        RefreshCompletedState();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!launchOnTriggerEnter)
            return;

        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
            return;

        LaunchHack();
    }

    public void LaunchHack()
    {
        if (disableAfterCompleted && IsCompleted)
            return;

        if (levelData == null)
        {
            Debug.LogWarning("DispatchNodeHackTrigger has no level data assigned.", this);
            return;
        }

        DispatchLevelManager manager = GetLevelManager();
        if (manager == null)
        {
            Debug.LogWarning("DispatchNodeHackTrigger could not find a DispatchLevelManager.", this);
            return;
        }

        manager.LoadLevel(levelData);
        IsCompleted = true;
        RefreshCompletedState();
    }

    public void RefreshCompletedState()
    {
        if (disableAfterCompleted && IsCompleted)
            gameObject.SetActive(false);
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
