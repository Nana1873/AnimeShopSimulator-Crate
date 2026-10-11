# Research: Q wheel, tools, player states (game v1.0.72)

## Wheel
- `InventoryView` (`_pieMenu` SimplePieMenu.PieMenu, `_toolLockIcons`, `_toolImages`, `_toolAvailability`, `_activeMenuItemId`).
- Items = prefab children of `PieMenu.PieMenuElements.MenuItemsDir`. `PieMenuItem.id` is serialized; a clone keeps the source id → set it.
- Add at runtime: `PieMenuShared.References.MenuItemsManager.MenuItemAdder.Add(pieMenu, List<GameObject>)` (instantiates, re-tracks, redraws). Fallback: instantiate + `pieMenu.MenuItemsTracker.Initialize(dir)` + `PieMenuDrawer.Redraw`.
- Icons/locks by visual index: `ResolveMenuItemIdForVisualIndex(i, count)` = `i+1` if `count == _toolAvailability.Count` else `i`. Keep lists in step.
- `IsMenuItemAvailable(id)`: id<=0 true, else `_toolAvailability[id-1]`, out of range → true.

## Menu ids
0 Hand, 1 TrashBag, 2 Mop, 3 Decor, 4 Bat. `PlayerUIController.HandleInventoryItemSelected(id)` → `IInteractionsControllerCC.Enter*InteractionsState()` → `CloseInventoryView()`. Unknown ids just close.
Hotkeys 1-5 in `PlayerUIController.TryHandleInventoryHotkeys` (same id toggles back to Hands). Key 6 unhandled.
`PlayerUIController.BuildInventoryToolAvailability()` builds the availability list; id 5 is available everywhere by default.

## State machine
- `PlayerInteractionsController._stateMachine` (`StateMachineBase`: `States` dict, `ActiveState`, `OnStateChanged`). States: Hands, Pickup, Building, CarryItems, CashRegister, Collection, TrashBag, Mop, Bat, Decor.
- `get_CurrentInventoryToolMenuItemId` derives id from ActiveState type. `get_CanOpenInventoryMenu` = state in Hands/tools and empty hands.
- `HandleOnStateChanged` rebuilds input bindings from `_interactionsConfig` (data-driven, per state type name).

## Tool visuals + network
- Tool controllers (`PlayerMopController` etc.) `OnBehaviourEnable` → `SetToolVisualActive(true)` + `PlayerAnimationController.SetActiveToolMode(mode)`.
- `PlayerAnimationController.SetActiveToolMode(int)` → local `ApplyToolModeLocal` + `SyncVar<int> _activeToolMode` (server) / `SetActiveToolModeServerRpc` (client). No range check. Remote: `HandleActiveToolModeChanged` → `ApplyToolModeLocal(next)`.
- Unknown mode (e.g. 5) hides all vanilla tools → **crate visual = tool mode 5**, postfix `ApplyToolModeLocal`.

## Chosen approach (Option A)
Stay in Hands state + mod flag, tool mode 5 for visuals, own input handling in OnUpdate, leave crate mode when the state changes or another tool is chosen.
