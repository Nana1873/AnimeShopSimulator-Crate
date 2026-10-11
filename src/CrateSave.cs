using System;
using System.IO;
using System.Text;
using HarmonyLib;
using Il2CppProject.Code.Core.Saves;
using Il2CppProject.Code.Core.Services;
using Il2CppProject.Code.Gameplay.Controllers;
using Il2CppProject.Code.Gameplay.Services;
using MelonLoader.Utils;

namespace AnimeShopCrate
{
    /// <summary>
    /// Crate contents are kept next to the game's save in UserData/AnimeShopCrate/slot&lt;N&gt;.txt.
    /// The snapshot is taken when the game prepares its save (main thread) and only written once the
    /// game reports the save committed, stamped with the save's transaction id. On load the file is
    /// used only if that id still matches the slot, so an older or newer crate file never mixes with
    /// a different save. Only the host saves and loads.
    /// File: line 1 = transaction id, then one serialized crate per line.
    /// </summary>
    internal static class CrateSave
    {
        private static string _pending;
        private static int _pendingSlot = -1;
        private static bool _loadedThisScene;

        private static string Dir => Path.Combine(MelonEnvironment.UserDataDirectory, "AnimeShopCrate");
        private static string FileFor(int slot) => Path.Combine(Dir, $"slot{slot}.txt");

        internal static void ResetScene() => _loadedThisScene = false;

        private static bool IsHost()
        {
            var net = AllServices.Get<NetworkService>();
            return net != null && net.IsServerInitialized;
        }

        /// <summary>Called every frame; loads the crate file once the host's player exists.</summary>
        internal static void Update()
        {
            if (_loadedThisScene || Game.LocalPlayer == null) return;
            _loadedThisScene = true;
            if (!IsHost()) return;
            try { Load(); }
            catch (Exception e) { CrateMod.Log.Error("loading crates failed: " + e); }
        }

        private static void Load()
        {
            var save = AllServices.Get<SaveService>();
            if (save == null) return;
            int slot = save.SlotNumberActive;
            string file = FileFor(slot);
            CrateStore.Clear();
            WorldCrates.Reset();
            if (!File.Exists(file)) { Settings.Debug($"no crate file for slot {slot}."); return; }

            string[] lines = File.ReadAllLines(file, Encoding.UTF8);
            if (lines.Length == 0) return;
            string committed = CommittedTransactionId(slot);
            if (!string.Equals(lines[0], committed, StringComparison.OrdinalIgnoreCase))
            {
                CrateMod.Log.Warning($"crate file for slot {slot} belongs to a different save (file {lines[0]}, save {committed}); not loaded.");
                return;
            }
            int crates = 0;
            for (int i = 1; i < lines.Length; i++)
                if (WorldCrates.Apply(lines[i]) || CrateStore.Apply(lines[i])) crates++;
            CrateMod.Log.Msg($"loaded {crates} crate(s) for slot {slot}.");
            CrateNet.BroadcastAll();
        }

        // Read from the slot file instead of SaveService.TryGetCommittedTransactionId: calling
        // game methods with out-parameters through the interop layer has crashed the game.
        private static string CommittedTransactionId(int slot)
        {
            try
            {
                string file = Path.Combine(UnityEngine.Application.persistentDataPath, $"{slot}_save.es3");
                if (!File.Exists(file)) return null;
                string text = File.ReadAllText(file);
                var m = System.Text.RegularExpressions.Regex.Match(text,
                    @"""sessionSaveTransactionId""\s*:\s*\{[^}]*?""value""\s*:\s*""([0-9a-fA-F]+)""");
                return m.Success ? m.Groups[1].Value : null;
            }
            catch (Exception e) { CrateMod.Log.Warning("reading the save's transaction id failed: " + e.Message); return null; }
        }

        private static string Snapshot()
        {
            var sb = new StringBuilder();
            foreach (ulong id in CrateStore.Owners)
                if (CrateStore.Get(id).Count > 0) sb.Append(CrateStore.Serialize(id)).Append('\n');
            WorldCrates.AppendSave(sb);
            return sb.ToString();
        }

        // Snapshot while the game collects its save data (main thread). Not hooked on
        // SaveService.TryPrepareSessionSave itself: patching its struct out-parameter crashes the game.
        // OrderController is one of the save components that method calls.
        [HarmonyPatch(typeof(OrderController), nameof(OrderController.Save))]
        private static class SnapshotPatch
        {
            private static void Postfix()
            {
                try
                {
                    var save = AllServices.Get<SaveService>();
                    if (save == null || !save.HasSessionWriteAuthority) return;
                    _pending = Snapshot();
                    _pendingSlot = save.SlotNumberActive;
                    Settings.Trace($"crate snapshot for slot {_pendingSlot}");
                }
                catch (Exception e) { CrateMod.Log.Error("crate snapshot failed: " + e); }
            }
        }

        [HarmonyPatch(typeof(SaveService), nameof(SaveService.RecordCommittedTransaction))]
        private static class CommitPatch
        {
            // May run off the main thread: file work only.
            private static void Postfix(int slotNumber, string transactionId)
            {
                try
                {
                    string data = _pending;
                    if (data == null || slotNumber != _pendingSlot) return;
                    Directory.CreateDirectory(Dir);
                    string file = FileFor(slotNumber), tmp = file + ".tmp";
                    File.WriteAllText(tmp, transactionId + "\n" + data, new UTF8Encoding(false));
                    if (File.Exists(file)) File.Replace(tmp, file, file + ".bak");
                    else File.Move(tmp, file);
                    _pending = null;
                    Settings.Trace($"crate file written for slot {slotNumber}");
                }
                catch (Exception e) { CrateMod.Log.Error("writing the crate file failed: " + e); }
            }
        }

        [HarmonyPatch(typeof(SaveService), nameof(SaveService.TryDelete))]
        private static class DeletePatch
        {
            private static void Postfix(int index, bool __result)
            {
                if (__result) TryRemove(index);
            }
        }

        [HarmonyPatch(typeof(SaveService), nameof(SaveService.CreateNewSessionForSlot))]
        private static class NewGamePatch
        {
            private static void Postfix(int slotNumber, bool __result)
            {
                if (__result) TryRemove(slotNumber);
            }
        }

        private static void TryRemove(int slot)
        {
            try
            {
                string file = FileFor(slot);
                if (File.Exists(file)) File.Delete(file);
            }
            catch (Exception e) { CrateMod.Log.Warning("removing the crate file failed: " + e.Message); }
        }
    }
}
