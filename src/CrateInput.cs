using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Il2CppProject.Code.Core.Services;
using Il2CppProject.Code.Gameplay.Player.Products;
using Il2CppProject.Code.Gameplay.Player.States.Hand.Interactions;
using Il2CppProject.Code.Gameplay.Player.States.Pickup.Interactions;
using Il2CppProject.Code.Gameplay.Services;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AnimeShopCrate
{
    /// <summary>
    /// Local player's crate controls while the crate is held:
    /// left click = put the selected product onto the aimed slot, right click = take one product from
    /// the aimed slot or opened box into the crate (hold to repeat), mouse wheel = pick the product.
    /// </summary>
    internal static class CrateInput
    {
        private const float RepeatDelay = 0.35f, RepeatInterval = 0.12f;

        internal static int Selected;
        internal static string Hint;
        internal static float HintUntil;

        private static float _nextRepeat;
        private static bool _synced;

        internal static void Reset()
        {
            _synced = false;
            Selected = 0;
        }

        /// <summary>Distinct products in the local crate, in order of first appearance, with counts.</summary>
        internal static List<(CrateItem item, int count)> Groups()
        {
            var result = new List<(CrateItem, int)>();
            var player = Game.LocalPlayer;
            if (player == null) return result;
            foreach (var it in CrateStore.Get(player.SteamId))
            {
                int i = result.FindIndex(g => Ids.Same(g.Item1.ProductId, it.ProductId));
                if (i < 0) result.Add((it, 1));
                else result[i] = (result[i].Item1, result[i].Item2 + 1);
            }
            return result;
        }

        /// <summary>World crate under the crosshair while the hands are free (0 = none), for the HUD.</summary>
        internal static int AimedWorldCrate;

        internal static void Update()
        {
            var player = Game.LocalPlayer;
            if (player == null) return;
            // Ask the host for all crates once per session (late joiners see crates standing around).
            if (!_synced) { _synced = true; CrateNet.RequestSync(); }

            bool holding = CrateMode.IsLocalActive;
            if (!holding || CratePlacement.Active) ClearTrashAim();
            AimedWorldCrate = !holding && HandsFree(player) ? WorldCrates.Aimed() : 0;
            WorldCrates.SetHovered(AimedWorldCrate);
            if (!Game.CanTakeKeys()) return;

            if (CratePlacement.Active)
            {
                if (!holding || DropKey.WasPressed()) { CratePlacement.Cancel(); return; }
                if (CratePlacement.Update(out Vector3 at, out float facing)) SetDown(at, facing);
                return; // clicks belong to the placement preview
            }
            if (DropKey.WasPressed())
            {
                if (holding) CratePlacement.Begin();
                else if (AimedWorldCrate != 0) PickUp(AimedWorldCrate);
                return;
            }
            if (!holding) return;
            var mouse = Mouse.current;
            if (mouse == null) return;

            var groups = Groups();
            float scroll = mouse.scroll.ReadValue().y;
            if (groups.Count > 0 && Mathf.Abs(scroll) > 0.01f)
                Selected = ((Selected + (scroll < 0 ? 1 : -1)) % groups.Count + groups.Count) % groups.Count;
            if (Selected >= groups.Count) Selected = Math.Max(0, groups.Count - 1);
            CrateVisual.HighlightSelected(groups.Count > 0 ? groups[Selected].item.ProductId : null);

            UpdateTrashAim();
            bool put = Pressed(mouse.leftButton);
            bool take = !put && Pressed(mouse.rightButton);
            if (!put && !take) return;

            if (put && AimedTrash != null)
            {
                Trash(groups);
                return;
            }

            Settings.Trace(put ? "LMB" : "RMB");
            var place = AimedPlace();
            Settings.Trace("aimed place: " + (place == null ? "none" : place.name));
            if (place == null)
            {
                // Only complain at shelves and boxes; a click on a computer, door etc. is not meant for the crate.
                if (_aimingAtStorage) Say(Text.Get("w.nothing"));
                return;
            }
            if (take) TryTake(place);
            else TryPut(place, groups);
        }

        /// <summary>Trash can under the crosshair while holding the crate (null = none).</summary>
        internal static Il2CppProject.Code.Gameplay.Interactions.TrashCan AimedTrash;

        private static void UpdateTrashAim()
        {
            Il2CppProject.Code.Gameplay.Interactions.TrashCan found = null;
            var target = Game.LocalPlayer?._raycastController?.Interactable;
            if (target != null)
            {
                found = target.TryCast<Il2CppProject.Code.Gameplay.Interactions.TrashCan>();
                var comp = found == null ? target.TryCast<Component>() : null;
                if (comp != null) found = comp.GetComponentInParent<Il2CppProject.Code.Gameplay.Interactions.TrashCan>();
            }
            if (found?.Pointer == AimedTrash?.Pointer) return;
            if (found == null || found.Pointer != _confirmedTrash) _confirmedTrash = System.IntPtr.Zero;
            try { AimedTrash?.EnableLook(false); } catch { }
            AimedTrash = found;
            try { AimedTrash?.EnableLook(true); } catch { }
        }

        internal static void ClearTrashAim()
        {
            if (AimedTrash == null) return;
            try { AimedTrash.EnableLook(false); } catch { }
            AimedTrash = null;
        }

        private static System.IntPtr _confirmedTrash;
        private static float _confirmedUntil;

        private static void Trash(List<(CrateItem item, int count)> groups)
        {
            if (groups.Count == 0) { Say(Text.Get("w.crate_empty")); return; }
            if (!Game.TrashAllowed()) { Say(Text.Get("w.trash_off")); return; }
            var chosen = groups[Selected].item;
            var can = AimedTrash;
            // Like the game: ask before throwing away. One "yes" covers more clicks on the same
            // trash can for a short while, so emptying a crate is not a dialog per product.
            if (can.Pointer == _confirmedTrash && Time.time < _confirmedUntil)
            {
                ThrowAway(can, chosen.ProductId);
                return;
            }
            var ui = AllServices.Get<Il2CppProject.Code.Core.UI.UIService>();
            if (ui == null) { ThrowAway(can, chosen.ProductId); return; }
            string productId = chosen.ProductId;
            var p = new Il2CppProject.Code.Gameplay.UI.Other.WarningView.Params();
            p.OnConfirm = (Il2CppSystem.Action)(System.Action)(() =>
            {
                _confirmedTrash = can.Pointer;
                ThrowAway(can, productId);
            });
            p.OnRefuse = (Il2CppSystem.Action)(System.Action)(() => { });
            try { ui.ShowWarningView(p); }
            catch (Exception e) { Settings.Debug("confirm dialog failed: " + e.Message); ThrowAway(can, productId); }
        }

        private static void ThrowAway(Il2CppProject.Code.Gameplay.Interactions.TrashCan can, string productId)
        {
            _confirmedUntil = Time.time + 10f;
            CrateNet.SendCommand(CrateNet.CmdTrash + productId);
            try { can?.PlayTrashSound(); } catch { }
        }

        private static bool HandsFree(Il2CppProject.Code.Gameplay.Player.Controllers.PlayerCharacterController player) =>
            player._interactionsController != null && player._interactionsController.CanOpenInventoryMenu;

        private static void SetDown(Vector3 pos, float yaw)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            CrateNet.SendCommand(CrateNet.CmdDrop + pos.x.ToString("0.###", inv) + "," + pos.y.ToString("0.###", inv) + ","
                                 + pos.z.ToString("0.###", inv) + "," + yaw.ToString("0.#", inv));
            CrateMode.Exit();
        }

        private static void PickUp(int id)
        {
            var player = Game.LocalPlayer;
            if (CrateStore.Get(player.SteamId).Count > 0)
            {
                Say(Text.Get("w.not_empty"));
                return;
            }
            if (WorldCrates.OwnerOf(id) != 0 && WorldCrates.OwnerOf(id) != player.SteamId)
            {
                Say(Text.Get("w.other_crate"));
                return;
            }
            CrateMode.Enter(pickingUp: true);
            CrateNet.SendCommand(CrateNet.CmdPickUp + id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        private static bool Pressed(UnityEngine.InputSystem.Controls.ButtonControl button)
        {
            if (button.wasPressedThisFrame) { _nextRepeat = Time.time + RepeatDelay; return true; }
            if (button.isPressed && Time.time >= _nextRepeat) { _nextRepeat = Time.time + RepeatInterval; return true; }
            return false;
        }

        private static void TryTake(ProductPlace place)
        {
            Settings.Trace("take: count " + place.Count);
            if (place.Count <= 0) { Say(Text.Get("w.slot_empty")); return; }
            var box = place._pickupProducts;
            if (box != null && !box.IsOpened) { Say(Text.Get("w.closed_box")); return; }
            Settings.Trace("take: resolving definition");
            var def = ProductHandsUtility.ResolveProductDefinition(AllServices.Get<ProductsService>(), place.Id);
            Settings.Trace("take: definition " + (def == null ? "null" : def.Name + " " + def.Type));
            if (!CrateRules.Allowed(def)) { Say(Text.Format("w.not_allowed", def?.Name ?? "?")); return; }
            var player = Game.LocalPlayer;
            if (CrateStore.UsedSpace(player.SteamId) + CrateRules.SpaceOf(def) > CrateRules.Capacity) { Say(Text.Get("w.crate_full")); return; }
            Settings.Trace("take: sending");
            CrateNet.SendPlaceCommand(place, CrateNet.CmdTake);
            Settings.Trace("take: sent");
        }

        private static void TryPut(ProductPlace place, List<(CrateItem item, int count)> groups)
        {
            Settings.Trace("put: groups " + groups.Count + ", place count " + place.Count);
            if (groups.Count == 0) { Say(Text.Get("w.crate_empty")); return; }
            string productId;
            if (place.Count > 0)
            {
                productId = Ids.ToText(place.Id);
                if (groups.FindIndex(g => Ids.Same(g.item.ProductId, productId)) < 0)
                {
                    Say(Text.Get("w.wrong_product"));
                    return;
                }
            }
            else
            {
                var chosen = groups[Selected].item;
                var def = CrateRules.Definition(chosen.DefinitionId);
                if (def != null && !place.CanPut(def.Type)) { Say(Text.Get("w.wrong_slot")); return; }
                productId = chosen.ProductId;
            }
            var item = groups.Find(g => Ids.Same(g.item.ProductId, productId)).item;
            var putDef = item != null ? CrateRules.Definition(item.DefinitionId) : null;
            var typeSettings = putDef != null ? AllServices.Get<ProductsService>()?._config?.FindProductTypeSetting(putDef.Type) : null;
            if (typeSettings != null && place.Count >= place.GetCapacity(typeSettings, putDef, null)) { Say(Text.Get("w.slot_full")); return; }
            Settings.Trace("put: sending " + productId);
            CrateNet.SendPlaceCommand(place, CrateNet.CmdPut + productId);
            Settings.Trace("put: sent");
        }

        private static bool _aimingAtStorage;

        private static ProductPlace AimedPlace()
        {
            _aimingAtStorage = false;
            var target = Game.LocalPlayer?._raycastController?.Interactable;
            if (target == null) return null;
            var place = target.TryCast<ProductPlace>();
            if (place != null) return place;
            var interactor = target.TryCast<ProductsPlaceInteractor>();
            if (interactor != null) return interactor.ProductPlace;
            // Price tags stay with the game (change price / remove tag); the slot itself is the crate's.
            if (target.TryCast<ProductPricePlace>() != null) return null;
            var box = target.TryCast<PickupProducts>();
            if (box != null) return box._productPlace;
            var comp = target.TryCast<Component>();
            var found = comp == null ? null
                : comp.GetComponentInParent<ProductPlace>() ?? comp.GetComponentInParent<PickupProducts>()?._productPlace;
            // Empty slots are not hit by the hands' raycast; the shelf behind them is.
            // Take the shelf's slot nearest to the point you look at.
            if (found == null && comp != null) found = NearestSlot(comp, Game.LocalPlayer._raycastController);
            if (found == null && comp != null)
                _aimingAtStorage = comp.GetComponentInParent<PickupProducts>() != null || comp.GetComponentInChildren<ProductPlace>(true) != null;
            if (found == null)
                Settings.Debug("aimed at an unsupported object: " + target.Cast<Il2CppSystem.Object>().GetIl2CppType().FullName
                    + (comp != null ? $" ('{comp.name}')" : ""));
            return found;
        }

        private const float SlotPickRadius = 0.3f;

        private static ProductPlace NearestSlot(Component shelf, Il2CppProject.Code.Gameplay.Player.Controllers.PlayerRaycastController ray)
        {
            if (ray == null || !ray.HasHitPoint) return null;
            Vector3 hit = ray.HitPoint;
            ProductPlace best = null;
            float bestDist = SlotPickRadius;
            var places = shelf.GetComponentsInChildren<ProductPlace>(true);
            foreach (var p in places)
            {
                if (p == null || p._pickupProducts != null) continue;
                var col = p.GetComponent<Collider>() ?? p.GetComponentInChildren<Collider>(true);
                float d = col != null ? Vector3.Distance(col.ClosestPointOnBounds(hit), hit) : Vector3.Distance(p.transform.position, hit);
                if (d < bestDist) { bestDist = d; best = p; }
            }
            return best;
        }

        private static string _lastSaid;
        private static float _lastSaidAt = -10f;

        internal static void Say(string text)
        {
            // Holding a mouse button repeats actions; do not stack the same warning.
            if (text == _lastSaid && Time.time - _lastSaidAt < 2f) return;
            _lastSaid = text;
            _lastSaidAt = Time.time;
            // The game's own warning; the plain overlay only if the HUD is not available.
            if (GameUi.Warn(text)) return;
            Hint = text;
            HintUntil = Time.time + 2.5f;
        }

        // While the crate is held, clicks belong to the crate, not to the hands' take/price actions.
        [HarmonyPatch]
        private static class BlockHandActions
        {
            private static IEnumerable<System.Reflection.MethodBase> TargetMethods() => Candidates().Where(m => m != null);

            private static IEnumerable<System.Reflection.MethodBase> Candidates()
            {
                yield return AccessTools.Method(typeof(TakeItemFromProductPlaceInteraction), nameof(TakeItemFromProductPlaceInteraction.Process));
                yield return AccessTools.Method(typeof(TakeItemFromPickupInteraction), nameof(TakeItemFromPickupInteraction.Process));
                yield return AccessTools.Method(typeof(ScatterShelfProductsInteraction), nameof(ScatterShelfProductsInteraction.Process));
                // E ("move object") is the crate's set-down key while it is held.
                yield return AccessTools.Method(typeof(ReplaceObjectInteraction), nameof(ReplaceObjectInteraction.Process));
                yield return AccessTools.Method(typeof(ReplaceBattleTableInteraction), nameof(ReplaceBattleTableInteraction.Process));
                yield return AccessTools.Method(typeof(SpawnBuildingBoxInteraction), nameof(SpawnBuildingBoxInteraction.Process));
                // Like the game's tools (mop, bat): no computer/register, battle table, wardrobe, auto opener or shop sign.
                yield return AccessTools.Method(typeof(EnterStayPointInteraction), nameof(EnterStayPointInteraction.Process));
                yield return AccessTools.Method(typeof(BattleTableInteraction), nameof(BattleTableInteraction.Process));
                yield return AccessTools.Method(typeof(EnterOutfitChangeInteraction), nameof(EnterOutfitChangeInteraction.Process));
                yield return AccessTools.Method(typeof(ClaimAutoOpenerInteraction), nameof(ClaimAutoOpenerInteraction.Process));
                yield return AccessTools.Method(typeof(StartAutoOpenerInteraction), nameof(StartAutoOpenerInteraction.Process));
                yield return AccessTools.Method(typeof(ToggleCloseOpenSignInteraction), nameof(ToggleCloseOpenSignInteraction.Process));
            }

            private static bool Prefix() => !CrateMode.IsLocalActive;
        }
    }
}
