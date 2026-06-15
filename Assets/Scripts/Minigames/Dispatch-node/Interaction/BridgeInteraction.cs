using UnityEngine;

namespace Dispatch.Gameplay
{
public class BridgeInteraction : NodeInteraction
{
    [SerializeField] private GameObject bridgePuzzleUi;
    [SerializeField] private bool pauseWhileUiOpen = true;
    [SerializeField] private DispatchGameStateManager gameStateManager;

    private PlayerController activePlayer;

    public override void Trigger(PlayerController player)
    {
        activePlayer = player;
        DispatchGameStateManager manager = GetGameStateManager();

        if (manager != null && player != null)
            manager.RegisterPlayer(player);

        if (bridgePuzzleUi != null)
            bridgePuzzleUi.SetActive(true);

        if (pauseWhileUiOpen)
        {
            if (manager != null)
                manager.PauseGameplay();
            else if (player != null)
                player.SetGameplayControlEnabled(false);
        }

        Debug.Log("Open Bridge Puzzle UI");
    }

    public void ExitInteraction()
    {
        if (bridgePuzzleUi != null)
            bridgePuzzleUi.SetActive(false);

        if (!pauseWhileUiOpen)
            return;

        DispatchGameStateManager manager = GetGameStateManager();

        if (manager != null)
            manager.ResumeGameplay();
        else if (activePlayer != null)
            activePlayer.SetGameplayControlEnabled(true);
    }

    private DispatchGameStateManager GetGameStateManager()
    {
        if (gameStateManager != null)
            return gameStateManager;

        if (GameBootstrap.Instance != null && GameBootstrap.Instance.gameStateManager != null)
            return GameBootstrap.Instance.gameStateManager;

        return DispatchGameStateManager.Instance;
    }
}
}
