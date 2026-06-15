using UnityEngine;

namespace Dispatch.Gameplay
{
public class NodePresenceVisibility : MonoBehaviour
{
    public PlayerController player;
    public PathNode watchedNode;
    public GameObject targetObject;
    public bool visibleOnlyWhenPlayerIsOnNode = true;

    void OnEnable()
    {
        ResolveRuntimeReferences();

        if (player != null)
            player.OnCurrentNodeChanged += HandleNodeChanged;

        RefreshVisibility();
    }

    void OnDisable()
    {
        if (player != null)
            player.OnCurrentNodeChanged -= HandleNodeChanged;
    }

    private void HandleNodeChanged(PathNode previousNode, PathNode currentNode)
    {
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        if (targetObject == null)
            targetObject = gameObject;

        if (player == null || watchedNode == null)
            return;

        bool isOnWatchedNode = player != null && watchedNode != null && player.IsOnNode(watchedNode);
        bool shouldBeVisible = visibleOnlyWhenPlayerIsOnNode ? isOnWatchedNode : !isOnWatchedNode;

        targetObject.SetActive(shouldBeVisible);
    }

    private void ResolveRuntimeReferences()
    {
        if (player != null)
            return;

        if (GameBootstrap.Instance != null && GameBootstrap.Instance.player != null)
        {
            player = GameBootstrap.Instance.player;
            return;
        }

        player = FindFirstObjectByType<PlayerController>();
    }
}
}
