using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Outsiders.Auditory;

namespace Dispatch.Gameplay
{
    
public class DispatchEnemyController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private DispatchGameStateManager gameStateManager;
    [SerializeField] private DispatchLevelManager levelManager;
    [SerializeField] private PathNode currentNode;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float nodeHeight = -0.5f;
    [SerializeField] private float catchDistance = 0.1f;
    [SerializeField] private AudioEventSO enemyMoveAudio;
    [SerializeField] private AudioEventSO enemyCollideAudio;

    [Header("Detection")]
    [SerializeField] private int chaseTriggerDistance = 2;
    [SerializeField, Min(0.02f)] private float thinkInterval = 0.1f;

    private readonly Queue<PathNode> pathBuffer = new Queue<PathNode>();
    private bool isMoving;
    private bool hasCaughtPlayer;
    private float nextThinkTime;

    public PathNode CurrentNode => currentNode;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (hasCaughtPlayer)
            return;

        if (player == null || gameStateManager == null || levelManager == null || currentNode == null)
            ResolveReferences();

        if (player == null || currentNode == null || IsGameplayPaused())
            return;

        if (HasCaughtPlayer())
        {
            HandlePlayerCaught();
            return;
        }

        if (isMoving)
            return;

        if (Time.time < nextThinkTime)
            return;

        nextThinkTime = Time.time + thinkInterval;

        int distanceToPlayer = GetShortestDistanceToPlayer();
        if (distanceToPlayer < 0 || distanceToPlayer > chaseTriggerDistance)
            return;

        if (TryBuildPathTo(player.currentNode))
        {
            PathNode nextNode = pathBuffer.Dequeue();
            MoveToNodeAsync(nextNode).Forget();
        }
    }

    private async UniTaskVoid MoveToNodeAsync(PathNode targetNode)
    {
        if (targetNode == null || currentNode == null)
            return;

        if (!currentNode.HasActiveConnectionTo(targetNode))
            return;

        isMoving = true;
        PlayEnemyMoveAudio();

        Vector3 start = currentNode.transform.position;
        Vector3 end = targetNode.transform.position;
        start.y = nodeHeight;
        end.y = nodeHeight;
        transform.position = start;

        float progress = 0f;
        while (progress < 1f)
        {
            if (IsGameplayPaused())
            {
                await UniTask.Yield(this.GetCancellationTokenOnDestroy());
                continue;
            }

            if (!currentNode.HasActiveConnectionTo(targetNode))
                break;

            progress += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(start, end, progress);

            if (HasCaughtPlayer())
            {
                HandlePlayerCaught();
                isMoving = false;
                return;
            }

            await UniTask.Yield(this.GetCancellationTokenOnDestroy());
        }

        if (progress >= 1f && currentNode.HasActiveConnectionTo(targetNode))
        {
            currentNode = targetNode;
            transform.position = end;

            if (HasCaughtPlayer())
            {
                HandlePlayerCaught();
                isMoving = false;
                return;
            }
        }
        else
        {
            SnapToCurrentNode();
        }

        isMoving = false;
    }

    private bool TryBuildPathTo(PathNode targetNode)
    {
        pathBuffer.Clear();

        if (targetNode == null || currentNode == null || targetNode == currentNode)
            return false;

        Queue<PathNode> frontier = new Queue<PathNode>();
        Dictionary<PathNode, PathNode> previous = new Dictionary<PathNode, PathNode>();

        frontier.Enqueue(currentNode);
        previous[currentNode] = null;

        while (frontier.Count > 0)
        {
            PathNode node = frontier.Dequeue();
            if (node == targetNode)
                break;

            foreach (PathNode neighbor in node.GetConnectedNodes())
            {
                if (neighbor == null || previous.ContainsKey(neighbor))
                    continue;

                previous[neighbor] = node;
                frontier.Enqueue(neighbor);
            }
        }

        if (!previous.ContainsKey(targetNode))
            return false;

        Stack<PathNode> reversedPath = new Stack<PathNode>();
        PathNode step = targetNode;

        while (step != null && step != currentNode)
        {
            reversedPath.Push(step);
            step = previous[step];
        }

        while (reversedPath.Count > 0)
            pathBuffer.Enqueue(reversedPath.Pop());

        return pathBuffer.Count > 0;
    }

    private int GetShortestDistanceToPlayer()
    {
        if (player == null || player.currentNode == null || currentNode == null)
            return -1;

        if (player.currentNode == currentNode)
            return 0;

        Queue<PathNode> frontier = new Queue<PathNode>();
        Dictionary<PathNode, int> distances = new Dictionary<PathNode, int>();

        frontier.Enqueue(currentNode);
        distances[currentNode] = 0;

        while (frontier.Count > 0)
        {
            PathNode node = frontier.Dequeue();
            int distance = distances[node];

            foreach (PathNode neighbor in node.GetConnectedNodes())
            {
                if (neighbor == null || distances.ContainsKey(neighbor))
                    continue;

                int nextDistance = distance + 1;
                if (neighbor == player.currentNode)
                    return nextDistance;

                distances[neighbor] = nextDistance;
                frontier.Enqueue(neighbor);
            }
        }

        return -1;
    }

    private bool HasCaughtPlayer()
    {
        if (player == null)
            return false;

        bool sameNode = player.currentNode != null && player.currentNode == currentNode;
        bool samePosition = (transform.position - player.transform.position).sqrMagnitude <= catchDistance * catchDistance;
        return sameNode || samePosition;
    }

    private void HandlePlayerCaught()
    {
        if (hasCaughtPlayer)
            return;

        hasCaughtPlayer = true;
        pathBuffer.Clear();
        PlayEnemyCollideAudio();
        Debug.Log("Dispatch Enemy caught the player. Game Over.");

        DispatchLevelTimer timer = FindFirstObjectByType<DispatchLevelTimer>();
        if (timer != null)
            timer.RegisterEnemyCatchPenalty();

        DispatchLevelManager manager = GetLevelManager();
        if (manager != null)
        {
            manager.ReloadCurrentLevel();
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private bool IsGameplayPaused()
    {
        return gameStateManager != null && gameStateManager.IsPaused;
    }

    private void SnapToCurrentNode()
    {
        if (currentNode == null)
            return;

        Vector3 position = currentNode.transform.position;
        position.y = nodeHeight;
        transform.position = position;
    }

    private PathNode FindClosestNode()
    {
        PathNode[] allNodes = FindObjectsByType<PathNode>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        PathNode closestNode = null;
        float closestDistance = float.MaxValue;

        foreach (PathNode node in allNodes)
        {
            float distance = Vector3.Distance(transform.position, node.transform.position);
            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            closestNode = node;
        }

        return closestNode;
    }

    private void ResolveReferences()
    {
        if (gameStateManager == null)
            gameStateManager = DispatchGameStateManager.Instance;

        if (levelManager == null)
            levelManager = DispatchLevelManager.Instance;

        if (player == null && GameBootstrap.Instance != null)
            player = GameBootstrap.Instance.player;

        if (player == null && gameStateManager != null)
            player = gameStateManager.CurrentPlayer;

        bool resolvedCurrentNode = false;
        if (currentNode == null)
        {
            currentNode = FindClosestNode();
            resolvedCurrentNode = currentNode != null;
        }

        if (resolvedCurrentNode && !isMoving)
            SnapToCurrentNode();
    }

    private DispatchLevelManager GetLevelManager()
    {
        if (levelManager != null)
            return levelManager;

        levelManager = DispatchLevelManager.Instance;
        return levelManager;
    }

    private void PlayEnemyMoveAudio()
    {
        if (enemyMoveAudio == null)
            return;

        Outsiders.Auditory.AudioManager.Instance?.PlaySFX(enemyMoveAudio, transform.position);
    }

    private void PlayEnemyCollideAudio()
    {
        if (enemyCollideAudio == null)
            return;

        Outsiders.Auditory.AudioManager.Instance?.PlaySFX(enemyCollideAudio, transform.position, priority: 1);
    }
}
}
