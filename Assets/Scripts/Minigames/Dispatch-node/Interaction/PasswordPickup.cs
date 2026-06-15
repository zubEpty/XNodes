using UnityEngine;

namespace Dispatch.Gameplay
{
public class PasswordPickup : NodeInteraction
{
    public string password;
    [SerializeField] private GameObject passwordUi;
    [SerializeField] private bool pauseWhileUiOpen = true;
    [SerializeField] private DispatchGameStateManager gameStateManager;

    private PlayerController activePlayer;

    public override void Trigger(PlayerController player)
    {
        activePlayer = player;
        DispatchGameStateManager manager = GetGameStateManager();

        if (manager != null && player != null)
            manager.RegisterPlayer(player);

        if (passwordUi != null)
            passwordUi.SetActive(true);

        if (pauseWhileUiOpen)
        {
            if (manager != null)
                manager.PauseGameplay();
            else if (player != null)
                player.SetGameplayControlEnabled(false);
        }

        Debug.Log("Got password: " + password);
        // store in inventory system
    }

    public void ExitInteraction()
    {
        if (passwordUi != null)
            passwordUi.SetActive(false);

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
