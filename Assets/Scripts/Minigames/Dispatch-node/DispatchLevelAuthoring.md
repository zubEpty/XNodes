# Dispatch Node Level Authoring

## Main Scene

Add `DispatchLevelManager` to the main Dispatch scene.

- Assign the main scene `GameBootstrap`.
- `GameBootstrap` can find `PlayerController`, `SwipeInputHandler`, and `DispatchGameStateManager` automatically if they are in the main scene.
- Create `DispatchNodeLevelData` assets from `Create > CyberSecHex > Dispatch Node > Level Data`.
- Assign each asset's level scene. In the editor, dropping a scene into `Level Scene` fills `Level Scene Name` automatically.
- Add those level data assets to the manager's `Levels` list in order, for example level 1, level 2, level 3.
- The older `Level Scene Names` list is still supported as a fallback if no level data assets are assigned.
- Put those level scenes in Build Settings.
- UI buttons can call `LoadNextLevel`, `ReloadCurrentLevel`, or `CompleteCurrentLevelAndLoadNext`.

## Level Loading

The Dispatch base scene loads level scenes additively through `DispatchLevelManager`.

- Set `Load First Level On Start` if the first configured level should load automatically.
- Set `Starting Level Index` to the first level you want to load.
- Add `DispatchNodeLevelSceneLoader` to a button only if you need to jump to a specific level asset from inside the Dispatch scene.
- Completion UI advances with `DispatchLevelManager.LoadNextLevel`.

## Level Scene

Each handcrafted or generated level scene needs one root object with `DispatchLevel`.

- Assign `Start Node`.
- Keep the level-specific nodes, gates, switches, and visuals under this scene.
- `DispatchLevel` auto-binds runtime references for sequence gates and path switches from the main scene.
- `SequenceUnlockGate` and `SequenceConnectionSwitch` can leave `Input Handler`, `Player`, and `Game State Manager` empty. Runtime binding fills them.

## Node Types

Use node components as building blocks:

- Simple traversal: `PathNode` only.
- Key pickup: add `KeyPickupNodeInteraction`.
- Requires 4 digit key: add `KeyRequiredNodeInteraction`.
- Path pattern switch: use existing `SequenceConnectionSwitch` with `ConnectionSwitchInteraction`.
- Goal: add `GoalNodeInteraction`.

Multiple `NodeInteraction` components can live on the same node and will all trigger when the player enters it.

## UI Buttons

For sequence UI, add `SequenceGateButton` or `SequenceConnectionSwitchButton` to the Unity `Button`.

- Assign the local gate/switch and action.
- Assign `Sequence Root` to the local `Sequence` UI GameObject if pressing the button should open it.
- Keep Unity `Button.onClick` empty unless you have extra visual-only events.
- Do not call `DispatchGameStateManager.PauseGameplay` from level-scene buttons. The gate/switch handles pause/resume through runtime binding.
