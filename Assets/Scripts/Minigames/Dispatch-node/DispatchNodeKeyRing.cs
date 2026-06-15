using System.Collections.Generic;
using UnityEngine;

namespace Dispatch.Gameplay
{
public class DispatchNodeKeyRing : MonoBehaviour
{
    private readonly HashSet<string> keys = new HashSet<string>();

    public bool HasKey(string keyCode)
    {
        return string.IsNullOrEmpty(keyCode) || keys.Contains(keyCode);
    }

    public void AddKey(string keyCode)
    {
        if (string.IsNullOrEmpty(keyCode))
            return;

        keys.Add(keyCode);
    }

    public void Clear()
    {
        keys.Clear();
    }
}
}
