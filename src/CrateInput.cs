using System;
using System.Collections.Generic;
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
            AimedWorldCrate = !holding && HandsFree(player) ? WorldCrates.Aimed() : 0;
            if (!Game.CanTakeKeys()) return;

            if (DropKey.WasPressed())
            {
                if (holding) SetDown(player);
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

            bool put = Pressed(mouse.leftButton);
            bool take = !put && Pressed(mouse.rightButton);
            if (!put && !take) return;

            Settings.Trace(put ? "LMB" : "RMB");
            var place = AimedPlace();
            Settings.Trace("aimed place: " + (place == null ? "none" : place.name));
            if (place == null)
            {
                if (!_aimingAtPriceTag) Say("Kein Regalfach oder offener Karton im Blick.");
                return;
            }
            if (take) TryTake(place);
            else TryPut(place, groups);
        }

        private static bool HandsFree(Il2CppProject.Code.Gameplay.Player.Controllers.PlayerCharacterController player) =>
            player._interactionsController != null && player._interactionsController.CanOpenInventoryMenu;

        private static void SetDown(Il2CppProject.Code.Gameplay.Player.Controllers.PlayerCharacterController player)
        {
            var cam = Camera.main;
            Vector3 forward = cam != null ? cam.transform.forward : player.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = player.transform.forward;
            forward.Normalize();
            Vector3 pos = player.transform.position + forward * 0.75f;
            // Stand it on whatever is below that point (floor, or a low surface).
            if (Physics.Raycast(pos + Vector3.up * 1.2f, Vector3.down, out RaycastHit hit, 3f, ~0, QueryTriggerInteraction.Ignore))
                pos.y = hit.point.y;
            float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
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
                Say("Deine Kiste ist nicht leer – erst ausräumen oder abstellen.");
                return;
            }
            if (WorldCrates.OwnerOf(id) != 0 && WorldCrates.OwnerOf(id) != player.SteamId)
            {
                Say("Das ist die Kiste von einem Mitspieler.");
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
            if (place.Count <= 0) { Say("Hier liegt nichts."); return; }
            var box = place._pickupProducts;
            if (box != null && !box.IsOpened) { Say("Den Karton zuerst öffnen."); return; }
            Settings.Trace("take: resolving definition");
            var def = ProductHandsUtility.ResolveProductDefinition(AllServices.Get<ProductsService>(), place.Id);
            Settings.Trace("take: definition " + (def == null ? "null" : def.Name + " " + def.Type));
            if (!CrateRules.Allowed(def)) { Say($"{def?.Name ?? "Das"} passt nicht in die Kiste."); return; }
            var player = Game.LocalPlayer;
            if (CrateStore.UsedSpace(player.SteamId) + CrateRules.SpaceOf(def) > CrateRules.Capacity) { Say("Die Kiste ist voll."); return; }
            Settings.Trace("take: sending");
            CrateNet.SendPlaceCommand(place, CrateNet.CmdTake);
            Settings.Trace("take: sent");
        }

        private static void TryPut(ProductPlace place, List<(CrateItem item, int count)> groups)
        {
            Settings.Trace("put: groups " + groups.Count + ", place count " + place.Count);
            if (groups.Count == 0) { Say("Die Kiste ist leer."); return; }
            string productId;
            if (place.Count > 0)
            {
                productId = Ids.ToText(place.Id);
                if (groups.FindIndex(g => Ids.Same(g.item.ProductId, productId)) < 0)
                {
                    Say("In diesem Fach liegt ein anderes Produkt.");
                    return;
                }
            }
            else
            {
                var chosen = groups[Selected].item;
                var def = CrateRules.Definition(chosen.DefinitionId);
                if (def != null && !place.CanPut(def.Type)) { Say("Das passt nicht in dieses Fach."); return; }
                productId = chosen.ProductId;
            }
            var item = groups.Find(g => Ids.Same(g.item.ProductId, productId)).item;
            var putDef = item != null ? CrateRules.Definition(item.DefinitionId) : null;
            var typeSettings = putDef != null ? AllServices.Get<ProductsService>()?._config?.FindProductTypeSetting(putDef.Type) : null;
            if (typeSettings != null && place.Count >= place.GetCapacity(typeSettings, putDef, null)) { Say("Das Fach ist voll."); return; }
            Settings.Trace("put: sending " + productId);
            CrateNet.SendPlaceCommand(place, CrateNet.CmdPut + productId);
            Settings.Trace("put: sent");
        }

        private static bool _aimingAtPriceTag;

        private static ProductPlace AimedPlace()
        {
            _aimingAtPriceTag = false;
            var target = Game.LocalPlayer?._raycastController?.Interactable;
            if (target == null) return null;
            var place = target.TryCast<ProductPlace>();
            if (place != null) return place;
            var interactor = target.TryCast<ProductsPlaceInteractor>();
            if (interactor != null) return interactor.ProductPlace;
            // Price tags stay with the game (change price / remove tag); the slot itself is the crate's.
            if (target.TryCast<ProductPricePlace>() != null) { _aimingAtPriceTag = true; return null; }
            var box = target.TryCast<PickupProducts>();
            if (box != null) return box._productPlace;
            var comp = target.TryCast<Component>();
            var found = comp == null ? null
                : comp.GetComponentInParent<ProductPlace>() ?? comp.GetComponentInParent<PickupProducts>()?._productPlace;
            // Empty slots are not hit by the hands' raycast; the shelf behind them is.
            // Take the shelf's slot nearest to the point you look at.
            if (found == null && comp != null) found = NearestSlot(comp, Game.LocalPlayer._raycastController);
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

        internal static void Say(string text)
        {
            Hint = text;
            HintUntil = Time.time + 2.5f;
        }

        // While the crate is held, clicks belong to the crate, not to the hands' take/price actions.
        [HarmonyPatch]
        private static class BlockHandActions
        {
            private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(TakeItemFromProductPlaceInteraction), nameof(TakeItemFromProductPlaceInteraction.Process));
                yield return AccessTools.Method(typeof(TakeItemFromPickupInteraction), nameof(TakeItemFromPickupInteraction.Process));
                yield return AccessTools.Method(typeof(ScatterShelfProductsInteraction), nameof(ScatterShelfProductsInteraction.Process));
                // E ("move object") is the crate's set-down key while it is held.
                yield return AccessTools.Method(typeof(ReplaceObjectInteraction), nameof(ReplaceObjectInteraction.Process));
                yield return AccessTools.Method(typeof(ReplaceBattleTableInteraction), nameof(ReplaceBattleTableInteraction.Process));
            }

            private static bool Prefix() => !CrateMode.IsLocalActive;
        }
    }
}
