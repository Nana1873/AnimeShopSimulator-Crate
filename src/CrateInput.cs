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

        internal static void Update()
        {
            if (!CrateMode.IsLocalActive) return;
            if (!_synced) { _synced = true; CrateNet.RequestSync(); }
            if (!Game.CanTakeKeys()) return;
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
            if (place == null) { Say("Kein Regalfach oder offener Karton im Blick."); return; }
            if (take) TryTake(place);
            else TryPut(place, groups);
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

        private static ProductPlace AimedPlace()
        {
            var target = Game.LocalPlayer?._raycastController?.Interactable;
            if (target == null) return null;
            var place = target.TryCast<ProductPlace>();
            if (place != null) return place;
            var interactor = target.TryCast<ProductsPlaceInteractor>();
            if (interactor != null) return interactor.ProductPlace;
            var price = target.TryCast<ProductPricePlace>();
            if (price != null) return price._productPlace;
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
                yield return AccessTools.Method(typeof(ChangePriceInteraction), nameof(ChangePriceInteraction.Process));
                yield return AccessTools.Method(typeof(ClearProductPricePlaceInteraction), nameof(ClearProductPricePlaceInteraction.Process));
            }

            private static bool Prefix() => !CrateMode.IsLocalActive;
        }
    }
}
