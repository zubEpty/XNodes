using UnityEngine;
using UnityEngine.Events;
using System;

namespace Dispatch.Gameplay
{
public class GoalNodeInteraction : NodeInteraction
{
    public event Action<PlayerController> OnGoalReached;

    [SerializeField] private DispatchLevelManager levelManager;
    [SerializeField] private DispatchNodeCompletionPresenter completionPresenter;
    [SerializeField] private bool loadNextLevelImmediately;
    [SerializeField] private UnityEvent onGoalReached;

    public void BindLevelManager(DispatchLevelManager manager)
    {
        if (levelManager == null)
            levelManager = manager;
    }

    public override void Trigger(PlayerController player)
    {
        OnGoalReached?.Invoke(player);
        onGoalReached?.Invoke();

        if (completionPresenter != null)
            completionPresenter.CompleteNode();

        DispatchLevelManager manager = levelManager != null ? levelManager : DispatchLevelManager.Instance;
        if (manager == null)
            return;

        if (loadNextLevelImmediately)
            manager.CompleteCurrentLevelAndLoadNext();
        else
            manager.CompleteCurrentLevel();
    }
}
}
