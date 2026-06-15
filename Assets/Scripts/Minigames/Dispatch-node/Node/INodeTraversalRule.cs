namespace Dispatch.Gameplay
{
public interface INodeTraversalRule
{
    bool CanEnter(PlayerController player, PathNode fromNode);
}
}
