using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Dispatch.Gameplay
{
public enum Direction { Up, Down, Left, Right }

[System.Serializable]
public class NodeConnection
{
    public Direction direction;
    public PathNode targetNode;
    public bool isConnected = true;
    public UnityEvent onConnected;
    public UnityEvent onDisconnected;

    public void SetState(bool connected)
    {
        if (isConnected == connected)
            return;

        isConnected = connected;

        if (isConnected)
            onConnected?.Invoke();
        else
            onDisconnected?.Invoke();
    }
}

public class PathNode : MonoBehaviour
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    public List<NodeConnection> connections;

    [Header("Current Node Highlight")]
    [SerializeField] private Renderer[] highlightRenderers;
    [SerializeField] private Color currentNodeEmissionColor = new Color(0.0990566f, 1f, 0.71345717f, 1f);
    [SerializeField, Min(0f)] private float currentNodeEmissionIntensity = 3f;

    [Header("Unity Event Connection Target")]
    [SerializeField] private Direction eventConnectionDirection;
    [SerializeField] private PathNode eventConnectionTarget;
    [SerializeField] private bool updateEventConnectionBidirectionally = true;

    private MaterialPropertyBlock propertyBlock;
    private INodeTraversalRule[] traversalRules;

    void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        traversalRules = GetComponents<INodeTraversalRule>();

        if (highlightRenderers == null || highlightRenderers.Length == 0)
            highlightRenderers = FindDefaultHighlightRenderers();

        SetPlayerPresence(false);
    }

    private Renderer[] FindDefaultHighlightRenderers()
    {
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
        List<Renderer> nodeRenderers = new List<Renderer>();

        for (int i = 0; i < childRenderers.Length; i++)
        {
            Renderer childRenderer = childRenderers[i];
            if (childRenderer == null || childRenderer.gameObject.name.Contains("Plane"))
                continue;

            nodeRenderers.Add(childRenderer);
        }

        return nodeRenderers.Count > 0 ? nodeRenderers.ToArray() : childRenderers;
    }

    public void SetPlayerPresence(bool isPresent)
    {
        if (highlightRenderers == null)
            return;

        Color emissionColor = isPresent ? currentNodeEmissionColor * currentNodeEmissionIntensity : Color.black;

        for (int i = 0; i < highlightRenderers.Length; i++)
        {
            Renderer nodeRenderer = highlightRenderers[i];
            if (nodeRenderer == null)
                continue;

            nodeRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(EmissionColorId, emissionColor);
            nodeRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    public PathNode GetNode(Direction dir)
    {
        foreach (var c in connections)
        {
            if (c.direction == dir && IsTraversableConnection(c))
                return c.targetNode;
        }
        return null;
    }

    public bool IsConnectionActive(Direction dir)
    {
        foreach (var c in connections)
        {
            if (c.direction == dir)
                return c.isConnected;
        }

        return false;
    }

    public bool IsConnectionActive(PathNode target)
    {
        foreach (var c in connections)
        {
            if (c.targetNode == target)
                return c.isConnected;
        }

        return false;
    }

    public bool IsConnectionTraversable(PathNode target)
    {
        return target != null && IsConnectionActive(target) && target.IsConnectionActive(this);
    }

    public void SetConnectionState(Direction dir, bool connected)
    {
        foreach (var c in connections)
        {
            if (c.direction == dir)
            {
                c.SetState(connected);
                return;
            }
        }
    }

    public void SetConnectionState(PathNode target, bool connected)
    {
        foreach (var c in connections)
        {
            if (c.targetNode == target)
            {
                c.SetState(connected);
                return;
            }
        }
    }

    public void Connect(Direction dir)
    {
        SetConnectionState(dir, true);
    }

    public void Disconnect(Direction dir)
    {
        SetConnectionState(dir, false);
    }

    [ContextMenu("connect")]
    public void ConnectEventDirection()
    {
        SetConnectionState(eventConnectionDirection, true);
    }

    public void DisconnectEventDirection()
    {
        SetConnectionState(eventConnectionDirection, false);
    }

    public void ConnectEventTarget()
    {
        SetConnectionStateWithOptionalReverse(eventConnectionTarget, true);
    }

    public void DisconnectEventTarget()
    {
        SetConnectionStateWithOptionalReverse(eventConnectionTarget, false);
    }

    public void ConnectTo(PathNode target)
    {
        SetConnectionStateWithOptionalReverse(target, true);
    }

    public void DisconnectFrom(PathNode target)
    {
        SetConnectionStateWithOptionalReverse(target, false);
    }

    public IEnumerable<PathNode> GetConnectedNodes()
    {
        if (connections == null)
            yield break;

        foreach (var connection in connections)
        {
            if (!IsTraversableConnection(connection))
                continue;

            yield return connection.targetNode;
        }
    }

    public bool HasActiveConnectionTo(PathNode target)
    {
        return IsConnectionTraversable(target);
    }

    public bool CanPlayerEnter(PlayerController player, PathNode fromNode)
    {
        if (traversalRules == null || traversalRules.Length == 0)
            traversalRules = GetComponents<INodeTraversalRule>();

        for (int i = 0; i < traversalRules.Length; i++)
        {
            INodeTraversalRule rule = traversalRules[i];
            if (rule != null && !rule.CanEnter(player, fromNode))
                return false;
        }

        return true;
    }

    private bool IsTraversableConnection(NodeConnection connection)
    {
        return connection != null
            && connection.isConnected
            && connection.targetNode != null
            && connection.targetNode.IsConnectionActive(this);
    }

    private void SetConnectionStateWithOptionalReverse(PathNode target, bool connected)
    {
        if (target == null)
            return;

        SetConnectionState(target, connected);

        if (updateEventConnectionBidirectionally)
            target.SetConnectionState(this, connected);
    }

    void OnDrawGizmos()
    {
        foreach (var c in connections)
        {
            if (c.targetNode == null)
                continue;

            Gizmos.color = c.isConnected ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, c.targetNode.transform.position);
        }
    }

}
}
