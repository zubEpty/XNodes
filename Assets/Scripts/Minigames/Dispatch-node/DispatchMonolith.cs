using Dispatch.Gameplay;
using UnityEngine;

public class DispatchMonolith : MonoBehaviour
{
    public void UnloadCurrentDispatchLevel()
    {
        DispatchLevelManager levelManager = DispatchLevelManager.Instance;
        if (levelManager == null)
        {
            Debug.LogWarning("[DispatchMonolith] Cannot unload dispatch level because no DispatchLevelManager exists.");
            return;
        }

        levelManager.UnloadCurrentLevel();
    }
}
