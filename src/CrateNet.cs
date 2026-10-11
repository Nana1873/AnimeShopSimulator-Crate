using System;
using HarmonyLib;
using Il2CppProject.Code.Gameplay.Player.Controllers;
using Il2CppProject.Code.Gameplay.Player.Products;

namespace AnimeShopCrate
{
    /// <summary>
    /// Messages between the mod copies. The mod cannot add its own FishNet messages under IL2CPP,
    /// so it rides on existing player RPCs and marks its traffic:
    /// - client → host, aimed at a product place: PlayerProductsController AddProductItemRpc with a
    ///   payload whose StartLayer is <see cref="Marker"/> and whose keychain id holds the command;
    /// - client → host, no place: PlayerAnimationController PlayEmojiServer with a "ASC1|" string;
    /// - host → everyone: PlayerAnimationController PlayEmojiObserversRpc with a "ASC1|" string.
    /// Marked messages are swallowed before the game sees them. RPC names carry a hash that may change
    /// with game updates; the log says so when a patch target is missing.
    /// </summary>
    internal static class CrateNet
    {
        internal const int Marker = 0x41534331; // "ASC1"
        internal const string CmdTake = "take";
        internal const string CmdPut = "put:";
        internal const string CmdSync = "ASC1|sync";

        // ---------- client side ----------

        internal static void SendPlaceCommand(ProductPlace place, string command)
        {
            var player = Game.LocalPlayer;
            var products = player?._productsController;
            if (products == null || place == null) return;
            var payload = new ProductTransferPayload
            {
                StartLayer = Marker,
                VariantKeychainId = command,
                HasValue = true,
            };
            products.RpcWriter___Server_AddProductItemRpc_4187530933(place, payload);
        }

        internal static void RequestSync()
        {
            var anim = Game.Animation(Game.LocalPlayer);
            anim?.RpcWriter___Server_PlayEmojiServer_3615296227(CmdSync);
        }

        // ---------- host side ----------

        internal static void Broadcast(ulong steamId)
        {
            var player = CrateServer.FindPlayer(steamId) ?? Game.LocalPlayer;
            var anim = Game.Animation(player);
            if (anim == null) return;
            anim.RpcWriter___Observers_PlayEmojiObserversRpc_3615296227(CrateStore.Serialize(steamId));
        }

        internal static void BroadcastAll()
        {
            foreach (ulong id in CrateStore.Owners) Broadcast(id);
        }

        [HarmonyPatch(typeof(PlayerProductsController), nameof(PlayerProductsController.RpcLogic___AddProductItemRpc_4187530933))]
        private static class PlaceCommandPatch
        {
            private static bool Prefix(PlayerProductsController __instance, ProductPlace A_1, ProductTransferPayload A_2)
            {
                if (A_2 == null || A_2.StartLayer != Marker) return true;
                try
                {
                    var player = __instance.GetComponentInParent<PlayerCharacterController>();
                    CrateServer.HandlePlaceCommand(player, A_1, A_2.VariantKeychainId ?? "");
                }
                catch (Exception e) { CrateMod.Log.Error("crate command failed on the host: " + e); }
                return false;
            }
        }

        [HarmonyPatch(typeof(PlayerAnimationController), nameof(PlayerAnimationController.RpcLogic___PlayEmojiServer_3615296227))]
        private static class ServerStringPatch
        {
            private static bool Prefix(string A_1)
            {
                if (A_1 == null || !A_1.StartsWith(CrateStore.Prefix, StringComparison.Ordinal)) return true;
                try { if (A_1 == CmdSync) BroadcastAll(); }
                catch (Exception e) { CrateMod.Log.Error("crate sync failed on the host: " + e); }
                return false;
            }
        }

        [HarmonyPatch(typeof(PlayerAnimationController), nameof(PlayerAnimationController.RpcLogic___PlayEmojiObserversRpc_3615296227))]
        private static class ObserverStringPatch
        {
            private static bool Prefix(string A_1)
            {
                if (A_1 == null || !A_1.StartsWith(CrateStore.Prefix, StringComparison.Ordinal)) return true;
                try { CrateStore.Apply(A_1); }
                catch (Exception e) { CrateMod.Log.Error("crate update failed: " + e); }
                return false;
            }
        }
    }
}
