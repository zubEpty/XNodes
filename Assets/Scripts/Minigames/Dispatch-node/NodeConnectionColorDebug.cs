using UnityEngine;

namespace Dispatch.Gameplay
{
[RequireComponent(typeof(Renderer))]
public class NodeConnectionColorDebug : MonoBehaviour
{
    public Color disconnectedColor = Color.white;
    public Color connectedColor = Color.green;

    private Renderer cachedRenderer;
    private Material runtimeMaterial;

    void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        runtimeMaterial = cachedRenderer.material;
    }

    public void OnConnectColor()
    {
        runtimeMaterial.color = connectedColor;
    }

    public void OnDisconnectColor()
    {
        runtimeMaterial.color = disconnectedColor;
    }

}
}
