using System;
using HarmonyLib;
using Il2CppProject.Code.Gameplay.Player.Controllers;
using Il2CppProject.Code.Gameplay.Player.Products;
using Il2CppFishNet.Serializing;

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
        internal const string CmdDrop = "ASC1|drop|";     // + x,y,z,yaw
        internal const string CmdPickUp = "ASC1|pickup|"; // + world crate id
        private const string AnyPrefix = "ASC1";

        private static bool IsOurs(string text) => text != null && text.StartsWith(AnyPrefix, StringComparison.Ordinal);

        /// <summary>Routes a host message to the player crates or the world crates.</summary>
        private static void Receive(string text)
        {
            Settings.Trace("received " + (text.Length > 40 ? text.Substring(0, 40) + "…" : text));
            if (!WorldCrates.Apply(text)) CrateStore.Apply(text);
        }

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

        internal static void RequestSync() => SendCommand(CmdSync);

        internal static void SendCommand(string command)
        {
            var anim = Game.Animation(Game.LocalPlayer);
            anim?.RpcWriter___Server_PlayEmojiServer_3615296227(command);
        }

        // ---------- host side ----------

        internal static void Broadcast(ulong steamId)
        {
            var player = CrateServer.FindPlayer(steamId) ?? Game.LocalPlayer;
            var anim = Game.Animation(player);
            if (anim == null) return;
            Settings.Trace("host: broadcasting crate " + steamId);
            anim.RpcWriter___Observers_PlayEmojiObserversRpc_3615296227(CrateStore.Serialize(steamId));
            Settings.Trace("host: broadcast sent");
        }

        /// <summary>Host: sends a message to every player (through the host's own player).</summary>
        internal static void BroadcastText(string text)
        {
            var anim = Game.Animation(Game.LocalPlayer);
            if (anim == null || text == null) return;
            anim.RpcWriter___Observers_PlayEmojiObserversRpc_3615296227(text);
        }

        internal static void BroadcastAll()
        {
            foreach (ulong id in CrateStore.Owners) Broadcast(id);
            WorldCrates.BroadcastAll();
        }

        [HarmonyPatch(typeof(PlayerProductsController), nameof(PlayerProductsController.RpcLogic___AddProductItemRpc_4187530933))]
        private static class PlaceCommandPatch
        {
            private static bool Prefix(PlayerProductsController __instance, ProductPlace A_1, ProductTransferPayload A_2)
            {
                if (A_2 == null || A_2.StartLayer != Marker) return true;
                Settings.Trace("host: place command " + A_2.VariantKeychainId);
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
            private static bool Prefix(PlayerAnimationController __instance, string A_1)
            {
                if (!IsOurs(A_1)) return true;
                try
                {
                    var player = __instance.GetComponentInParent<PlayerCharacterController>();
                    if (A_1 == CmdSync) BroadcastAll();
                    else if (player != null && A_1.StartsWith(CmdDrop, StringComparison.Ordinal))
                    {
                        var f = A_1.Substring(CmdDrop.Length).Split(',');
                        var inv = System.Globalization.CultureInfo.InvariantCulture;
                        if (f.Length >= 4)
                            WorldCrates.HostDrop(player,
                                new UnityEngine.Vector3(float.Parse(f[0], inv), float.Parse(f[1], inv), float.Parse(f[2], inv)),
                                float.Parse(f[3], inv));
                    }
                    else if (player != null && A_1.StartsWith(CmdPickUp, StringComparison.Ordinal)
                             && int.TryParse(A_1.Substring(CmdPickUp.Length), out int id))
                        WorldCrates.HostPickUp(player, id);
                }
                catch (Exception e) { CrateMod.Log.Error("crate command failed on the host: " + e); }
                return false;
            }
        }

        // The game compiled RpcLogic___PlayEmojiObserversRpc into its network reader, so on clients the
        // message never passes through the RpcLogic patch below. Read it here, handle ours, and pass the
        // rest to the game's logic as the reader would.
        [HarmonyPatch(typeof(PlayerAnimationController), nameof(PlayerAnimationController.RpcReader___Observers_PlayEmojiObserversRpc_3615296227))]
        private static class ObserverReaderPatch
        {
            private static bool Prefix(PlayerAnimationController __instance, PooledReader PooledReader0)
            {
                if (PooledReader0 == null) return true;
                string text = PooledReader0.ReadString();
                if (IsOurs(text))
                {
                    try { Receive(text); }
                    catch (Exception e) { CrateMod.Log.Error("crate update failed: " + e); }
                }
                else
                {
                    __instance.RpcLogic___PlayEmojiObserversRpc_3615296227(text);
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(PlayerAnimationController), nameof(PlayerAnimationController.RpcLogic___PlayEmojiObserversRpc_3615296227))]
        private static class ObserverStringPatch
        {
            private static bool Prefix(string A_1)
            {
                if (!IsOurs(A_1)) return true;
                try { Receive(A_1); }
                catch (Exception e) { CrateMod.Log.Error("crate update failed: " + e); }
                return false;
            }
        }
    }
}
