using System;
using HarmonyLib;
using Il2CppProject.Code.Gameplay.Player.Controllers;
using UnityEngine.InputSystem;

namespace AnimeShopCrate
{
    /// <summary>
    /// Crate mode = the player stays in the game's hands state and the animation controller's
    /// tool mode is <see cref="CrateToolMode"/>. The tool mode is a SyncVar the game already sends
    /// to other players, so everyone sees the crate without extra networking. Any vanilla tool
    /// or state change that resets the tool mode ends crate mode on its own.
    /// </summary>
    internal static class CrateMode
    {
        internal const int CrateMenuId = 5;
        internal const PlayerToolMode CrateToolMode = (PlayerToolMode)5;
        private const string HandsStateName = "PlayerHandsInteractionsState";

        internal static bool IsActive(PlayerAnimationController anim) =>
            anim != null && anim._currentToolMode == CrateToolMode;

        internal static bool IsLocalActive => IsActive(Game.Animation(Game.LocalPlayer));

        internal static void Enter()
        {
            var player = Game.LocalPlayer;
            var anim = Game.Animation(player);
            var interactions = player?._interactionsController;
            if (anim == null || interactions == null) return;
            if (!interactions.CanOpenInventoryMenu)
            {
                Settings.Debug("crate not taken: hands are not free.");
                return;
            }
            // Leave mop/bat/etc. first; their disable hooks reset the tool mode to None.
            interactions.EnterHandsInteractionsState();
            anim.SetActiveToolMode(CrateToolMode);
            Settings.Debug("crate taken.");
        }

        internal static void Exit()
        {
            var anim = Game.Animation(Game.LocalPlayer);
            if (!IsActive(anim)) return;
            anim.SetActiveToolMode(PlayerToolMode.None);
            Settings.Debug("crate put away.");
        }

        internal static void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if ((kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) && IsLocalActive && Game.CanTakeKeys())
            {
                Exit();
                return;
            }
            if (!kb.digit6Key.wasPressedThisFrame && !kb.numpad6Key.wasPressedThisFrame) return;
            var player = Game.LocalPlayer;
            if (player == null || !Game.CanTakeKeys()) return;
            if (IsLocalActive) Exit();
            else Enter();
        }

        private static string ActiveStateName(PlayerInteractionsController c)
        {
            var state = c?._stateMachine?.ActiveState;
            return state == null ? null : state.Cast<Il2CppSystem.Object>().GetIl2CppType().Name;
        }

        // Wheel selection of the crate slot.
        [HarmonyPatch(typeof(PlayerUIController), nameof(PlayerUIController.HandleInventoryItemSelected))]
        private static class SelectPatch
        {
            private static bool Prefix(PlayerUIController __instance, int menuItemId)
            {
                if (menuItemId != CrateMenuId)
                {
                    // Hand is selected while the crate is held: the state does not change, so put it away here.
                    if (menuItemId == 0) Exit();
                    return true;
                }
                try
                {
                    if (!IsLocalActive) Enter();
                    __instance.CloseInventoryView();
                }
                catch (Exception e) { CrateMod.Log.Error("crate select failed: " + e); }
                return false;
            }
        }

        // Highlight the crate slot in the wheel while it is held.
        [HarmonyPatch(typeof(PlayerInteractionsController), nameof(PlayerInteractionsController.CurrentInventoryToolMenuItemId), MethodType.Getter)]
        private static class CurrentIdPatch
        {
            private static void Postfix(PlayerInteractionsController __instance, ref int __result)
            {
                if (__result != 0) return;
                var player = __instance.GetComponentInParent<PlayerCharacterController>();
                if (player != null && IsActive(Game.Animation(player))) __result = CrateMenuId;
            }
        }

        // Taking a box or product into the hands etc. leaves the hands state: put the crate away.
        [HarmonyPatch(typeof(PlayerInteractionsController), nameof(PlayerInteractionsController.HandleOnStateChanged))]
        private static class StatePatch
        {
            private static void Postfix(PlayerInteractionsController __instance)
            {
                try
                {
                    var player = __instance.GetComponentInParent<PlayerCharacterController>();
                    if (player == null || !player.IsOwner) return;
                    var anim = Game.Animation(player);
                    if (!IsActive(anim)) return;
                    string state = ActiveStateName(__instance);
                    if (state != HandsStateName)
                    {
                        Settings.Debug($"state changed to {state}: crate put away.");
                        anim.SetActiveToolMode(PlayerToolMode.None);
                    }
                }
                catch (Exception e) { CrateMod.Log.Error("crate state check failed: " + e); }
            }
        }

        // Show/hide the crate model for local and remote players.
        [HarmonyPatch(typeof(PlayerAnimationController), nameof(PlayerAnimationController.ApplyToolModeLocal))]
        private static class VisualPatch
        {
            private static void Postfix(PlayerAnimationController __instance, PlayerToolMode mode)
            {
                try { CrateVisual.Set(__instance, mode == CrateToolMode); }
                catch (Exception e) { CrateMod.Log.Error("crate visual failed: " + e); }
            }
        }
    }
}
