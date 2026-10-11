using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Il2CppProject.Code.Gameplay.Player.Controllers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AnimeShopCrate
{
    /// <summary>
    /// Crates standing in the world. The host owns the list; every player builds the visuals from the
    /// host's messages:  ASC1W|&lt;id&gt;|x,y,z,yaw|&lt;items&gt;   or   ASC1W|&lt;id&gt;|del
    /// </summary>
    internal static class WorldCrates
    {
        internal const string Prefix = "ASC1W|";
        private const float PickReach = 2.6f;

        private sealed class Crate
        {
            internal int Id;
            internal Vector3 Position;
            internal float Yaw;
            internal ulong Owner;
            internal readonly List<CrateItem> Items = new();
            internal GameObject Visual;
        }

        private static readonly Dictionary<int, Crate> Crates = new();
        private static int _nextId = 1;

        internal static IEnumerable<int> Ids => Crates.Keys;

        /// <summary>Scene change: the visuals are gone with the scene; the host reloads the data from the save.</summary>
        internal static void Reset() => Crates.Clear();

        internal static int ItemCount(int id) => Crates.TryGetValue(id, out var c) ? c.Items.Count : 0;

        internal static ulong OwnerOf(int id) => Crates.TryGetValue(id, out var c) ? c.Owner : 0;

        /// <summary>Every player has one crate: true while that player's crate stands in the world.</summary>
        internal static bool HasCrateOf(ulong owner)
        {
            foreach (var c in Crates.Values) if (c.Owner == owner) return true;
            return false;
        }

        // ---------- host ----------

        internal static void HostDrop(PlayerCharacterController player, Vector3 pos, float yaw)
        {
            ulong owner = player.SteamId;
            if (HasCrateOf(owner)) { Settings.Debug("set down refused: this player's crate already stands somewhere."); return; }
            if (Vector3.Distance(player.transform.position, pos) > 3f) pos = player.transform.position;
            var c = new Crate { Id = _nextId++, Position = pos, Yaw = yaw, Owner = owner };
            var held = CrateStore.Get(owner);
            c.Items.AddRange(held);
            held.Clear();
            Crates[c.Id] = c;
            Settings.Debug($"crate {c.Id} set down by {owner} with {c.Items.Count} products.");
            CrateStore.NotifyChanged(owner);
            CrateNet.Broadcast(owner);
            CrateNet.BroadcastText(Serialize(c));
            Show(c);
        }

        internal static void HostPickUp(PlayerCharacterController player, int id)
        {
            if (!Crates.TryGetValue(id, out var c)) return;
            ulong owner = player.SteamId;
            if (Vector3.Distance(player.transform.position, c.Position) > 4f) { Settings.Debug("pick up refused: too far."); return; }
            if (c.Owner != 0 && c.Owner != owner) { Settings.Debug("pick up refused: someone else's crate."); return; }
            var held = CrateStore.Get(owner);
            if (held.Count > 0) { Settings.Debug("pick up refused: the player's crate is not empty."); return; }
            held.AddRange(c.Items);
            Remove(id);
            Settings.Debug($"crate {id} picked up by {owner}.");
            CrateStore.NotifyChanged(owner);
            CrateNet.Broadcast(owner);
            CrateNet.BroadcastText(Prefix + id.ToString(CultureInfo.InvariantCulture) + "|del");
        }

        internal static void BroadcastAll()
        {
            foreach (var c in Crates.Values) CrateNet.BroadcastText(Serialize(c));
        }

        // ---------- everyone ----------

        internal static string Serialize(int id) => Crates.TryGetValue(id, out var c) ? Serialize(c) : null;

        private static string Serialize(Crate c)
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder(Prefix)
                .Append(c.Id.ToString(inv)).Append('|')
                .Append(c.Position.x.ToString("0.###", inv)).Append(',')
                .Append(c.Position.y.ToString("0.###", inv)).Append(',')
                .Append(c.Position.z.ToString("0.###", inv)).Append(',')
                .Append(c.Yaw.ToString("0.#", inv)).Append(',')
                .Append(c.Owner.ToString(inv)).Append('|');
            CrateStore.AppendItems(sb, c.Items);
            return sb.ToString();
        }

        internal static bool Apply(string message)
        {
            if (message == null || !message.StartsWith(Prefix, StringComparison.Ordinal)) return false;
            var inv = CultureInfo.InvariantCulture;
            string[] parts = message.Substring(Prefix.Length).Split('|');
            if (parts.Length < 2 || !int.TryParse(parts[0], NumberStyles.Integer, inv, out int id)) return true;
            if (parts[1] == "del") { Remove(id); return true; }

            string[] p = parts[1].Split(',');
            if (p.Length < 4) return true;
            if (!Crates.TryGetValue(id, out var c)) Crates[id] = c = new Crate { Id = id };
            c.Position = new Vector3(F(p[0]), F(p[1]), F(p[2]));
            c.Yaw = F(p[3]);
            c.Owner = p.Length > 4 && ulong.TryParse(p[4], NumberStyles.None, inv, out ulong o) ? o : 0;
            c.Items.Clear();
            if (parts.Length > 2) CrateStore.ParseItems(parts[2], c.Items);
            if (id >= _nextId) _nextId = id + 1;
            Show(c);
            return true;
        }

        private static float F(string s) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0f;

        private static void Remove(int id)
        {
            if (!Crates.TryGetValue(id, out var c)) return;
            if (c.Visual != null) Object.Destroy(c.Visual);
            Crates.Remove(id);
        }

        private static void Show(Crate c)
        {
            if (c.Visual == null)
            {
                c.Visual = CrateVisual.Build();
                c.Visual.name = "AnimeShopCrate_WorldCrate_" + c.Id;
            }
            c.Visual.transform.position = c.Position;
            c.Visual.transform.rotation = Quaternion.Euler(0f, c.Yaw, 0f);
            CrateContents.Fill(c.Visual, c.Items);
        }

        /// <summary>The world crate the camera looks at within reach, or 0.</summary>
        internal static int Aimed()
        {
            var cam = Camera.main;
            if (cam == null) return 0;
            Vector3 origin = cam.transform.position, dir = cam.transform.forward;
            int best = 0;
            float bestDist = PickReach;
            foreach (var c in Crates.Values)
            {
                if (c.Visual == null) continue;
                // Ray against the crate's box in its own space.
                var t = c.Visual.transform;
                Vector3 o = t.InverseTransformPoint(origin), d = t.InverseTransformDirection(dir);
                var box = new Bounds(new Vector3(0f, CrateVisual.Height / 2f, 0f),
                                     new Vector3(CrateVisual.Width + 0.06f, CrateVisual.Height + 0.06f, CrateVisual.Depth + 0.06f));
                if (box.IntersectRay(new Ray(o, d), out float dist) && dist < bestDist)
                {
                    bestDist = dist;
                    best = c.Id;
                }
            }
            // Something in between (table, shelf, wall) hides the crate. The crate itself has no collider.
            if (best != 0 && Physics.Raycast(origin, dir, out RaycastHit hit, bestDist, ~0, QueryTriggerInteraction.Ignore))
                return 0;
            return best;
        }

        // ---------- save ----------

        internal static void AppendSave(StringBuilder sb)
        {
            foreach (var c in Crates.Values) sb.Append(Serialize(c)).Append('\n');
        }
    }
}
