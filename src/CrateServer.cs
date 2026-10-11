using System;
using Il2CppProject.Code.Core.Services;
using Il2CppProject.Code.Gameplay.Player.Controllers;
using Il2CppProject.Code.Gameplay.Player.Products;
using Il2CppProject.Code.Gameplay.Services;
using UnityEngine;

namespace AnimeShopCrate
{
    /// <summary>Host-side crate logic. Shelf and box changes go through the game's own server methods,
    /// so their SyncVars carry the result to every player.</summary>
    internal static class CrateServer
    {
        private const float MaxReach = 4f;

        internal static PlayerCharacterController FindPlayer(ulong steamId)
        {
            var players = AllServices.Get<PlayerCharacterRegistryService>()?._players;
            if (players == null) return null;
            for (int i = 0; i < players.Count; i++)
                if (players[i] != null && players[i].SteamId == steamId) return players[i];
            return null;
        }

        internal static void HandlePlaceCommand(PlayerCharacterController player, ProductPlace place, string command)
        {
            if (player == null || place == null) return;
            if (Vector3.Distance(player.transform.position, place.transform.position) > MaxReach)
            {
                Settings.Debug("crate command ignored: product place out of reach.");
                return;
            }
            ulong owner = player.SteamId;
            if (WorldCrates.HasCrateOf(owner)) { Settings.Debug("crate command ignored: this player's crate stands in the world."); return; }
            bool changed = command == CrateNet.CmdTake
                ? Take(owner, place)
                : command.StartsWith(CrateNet.CmdPut, StringComparison.Ordinal) && Put(player, owner, place, command.Substring(CrateNet.CmdPut.Length));
            if (!changed) return;
            CrateStore.NotifyChanged(owner);
            CrateNet.Broadcast(owner);
        }

        private static bool Take(ulong owner, ProductPlace place)
        {
            Settings.Trace("host take: start");
            if (place.Count <= 0) return false;
            var products = AllServices.Get<ProductsService>();
            var def = ProductHandsUtility.ResolveProductDefinition(products, place.Id);
            if (!CrateRules.Allowed(def)) { Settings.Debug($"take refused: {def?.Type} does not go into the crate."); return false; }
            if (CrateStore.UsedSpace(owner) + CrateRules.SpaceOf(def) > CrateRules.Capacity) { Settings.Debug("take refused: crate full."); return false; }

            // TryTakeItemByPlayerServer returns through out-parameters (a Guid struct among them),
            // which crashes the game natively through the interop layer. Use the plain path the
            // game's own box transfers use: read the top item, then remove it.
            var pricePlace = place.PricePlace;
            var box = place._pickupProducts;
            if (box != null && !box.IsOpened) return false;
            if (box == null && pricePlace != null && pricePlace.IsPlayerTakeBlocked) { Settings.Debug("take refused: slot reserved by an employee or buyer."); return false; }
            if (box == null && pricePlace == null) return false;
            string id = Ids.ToText(place.Id);
            Settings.Trace("host take: reading top item");
            var payload = place.TryGetItem();
            Settings.Trace("host take: removing");
            place.RemoveLastItemServer();
            Settings.Trace("host take: removed");

            CrateStore.Get(owner).Add(new CrateItem
            {
                ProductId = id,
                DefinitionId = def.Id,
                VariantDefinitionId = payload?.VariantDefinitionId ?? 0,
                VariantKeychainFoil = payload?.VariantKeychainFoil ?? 0,
                VariantKeychainId = payload?.VariantKeychainId ?? "",
            });
            Settings.Debug($"took {def.Name} into the crate of {owner}.");
            return true;
        }

        private static bool Put(PlayerCharacterController player, ulong owner, ProductPlace place, string requestedId)
        {
            var items = CrateStore.Get(owner);
            // A filled slot only takes more of its own product; an empty one takes what the player picked.
            string wanted = place.Count > 0 ? Ids.ToText(place.Id) : requestedId;
            int index = items.FindLastIndex(i => Ids.Same(i.ProductId, wanted));
            if (index < 0) { Settings.Debug("put refused: product not in the crate."); return false; }
            var item = items[index];

            var products = AllServices.Get<ProductsService>();
            var def = CrateRules.Definition(item.DefinitionId);
            if (def == null || !place.CanPut(def.Type)) { Settings.Debug("put refused: slot does not take this product type."); return false; }
            var settings = products?._config?.FindProductTypeSetting(def.Type);
            if (settings != null && place.Count >= place.GetCapacity(settings, def, null)) { Settings.Debug("put refused: slot full."); return false; }

            var payload = new ProductTransferPayload
            {
                StartPosition = CrateWorldPosition(player),
                StartRotation = Quaternion.identity,
                VariantDefinitionId = item.VariantDefinitionId,
                VariantKeychainFoil = item.VariantKeychainFoil,
                VariantKeychainId = item.VariantKeychainId,
                HasValue = true,
            };
            if (!place.TryAddItem(Ids.FromText(item.ProductId), payload)) { Settings.Debug("put refused by the game."); return false; }
            items.RemoveAt(index);
            Settings.Debug($"put {def.Name} from the crate of {owner} onto a slot.");
            return true;
        }

        /// <summary>Host: throws one product of this kind out of the player's crate.</summary>
        internal static void Trash(PlayerCharacterController player, string productId)
        {
            // Same rule the game uses for products in hands.
            if (!Game.TrashAllowed()) { Settings.Debug("trash refused: disabled in this lobby."); return; }
            ulong owner = player.SteamId;
            var items = CrateStore.Get(owner);
            int index = items.FindLastIndex(i => Ids.Same(i.ProductId, productId));
            if (index < 0) return;
            Settings.Debug($"{owner} threw {CrateRules.Name(items[index].DefinitionId)} away.");
            items.RemoveAt(index);
            CrateStore.NotifyChanged(owner);
            CrateNet.Broadcast(owner);
        }

        private static Vector3 CrateWorldPosition(PlayerCharacterController player)
        {
            var t = player.transform;
            return t.position + Vector3.up * 1.1f + t.forward * 0.45f;
        }
    }
}
