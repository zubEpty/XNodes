using UnityEngine;

namespace Dispatch.Gameplay
{
public class ConnectionSwitchInteraction : NodeInteraction
{
    [SerializeField] private GameObject switchUi;
    [SerializeField] private SequenceConnectionSwitch sequenceSwitch;

    public override void Trigger(PlayerController player)
    {
        if (switchUi != null)
            switchUi.SetActive(true);

        if (sequenceSwitch != null)
        {
            sequenceSwitch.player = player;
            sequenceSwitch.BeginSequenceEntry();
        }
    }

    public void ExitInteraction()
    {
        if (sequenceSwitch != null)
        {
            sequenceSwitch.ExitSequenceEntry();
            sequenceSwitch.ResumeGameplay();
        }

        if (switchUi != null)
            switchUi.SetActive(false);
    }
}
}
