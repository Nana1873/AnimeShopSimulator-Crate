using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AnimeShopCrate
{
    /// <summary>One product in a crate, with the per-item variant data shelves keep for keychains and pins.</summary>
    internal sealed class CrateItem
    {
        internal string ProductId;   // Guid as string ("N" format), the game's runtime product id
        internal int DefinitionId;   // ProductDefinition id, for names/icons/space without a lookup
        internal int VariantDefinitionId;
        internal int VariantKeychainFoil;
        internal string VariantKeychainId = "";
    }

    /// <summary>
    /// Crate contents per player (SteamID). The host's copy is authoritative; clients hold a mirror
    /// that the host refreshes with <see cref="Serialize"/> / <see cref="Apply"/>.
    /// </summary>
    internal static class CrateStore
    {
        internal const string Prefix = "ASC1|";
        private static readonly Dictionary<ulong, List<CrateItem>> Crates = new();

        internal static event Action<ulong> Changed;

        internal static List<CrateItem> Get(ulong steamId)
        {
            if (!Crates.TryGetValue(steamId, out var list)) Crates[steamId] = list = new List<CrateItem>();
            return list;
        }

        internal static IEnumerable<ulong> Owners => Crates.Keys;

        internal static void Clear() => Crates.Clear();

        internal static void NotifyChanged(ulong steamId) => Changed?.Invoke(steamId);

        internal static int UsedSpace(ulong steamId)
        {
            int used = 0;
            foreach (var item in Get(steamId)) used += CrateRules.SpaceOf(item.DefinitionId);
            return used;
        }

        // Format: ASC1|<steamId>|<item>;<item>;...   item = productId,defId,variantDefId,foil,keychainId
        internal static string Serialize(ulong steamId)
        {
            var sb = new StringBuilder(Prefix).Append(steamId.ToString(CultureInfo.InvariantCulture)).Append('|');
            AppendItems(sb, Get(steamId));
            return sb.ToString();
        }

        internal static bool Apply(string message)
        {
            if (message == null || !message.StartsWith(Prefix, StringComparison.Ordinal)) return false;
            string[] parts = message.Substring(Prefix.Length).Split('|');
            if (parts.Length < 2 || !ulong.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out ulong steamId)) return true;
            var list = Get(steamId);
            list.Clear();
            ParseItems(parts[1], list);
            NotifyChanged(steamId);
            return true;
        }

        internal static void AppendItems(StringBuilder sb, List<CrateItem> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var it = list[i];
                if (i > 0) sb.Append(';');
                sb.Append(it.ProductId).Append(',')
                  .Append(it.DefinitionId.ToString(CultureInfo.InvariantCulture)).Append(',')
                  .Append(it.VariantDefinitionId.ToString(CultureInfo.InvariantCulture)).Append(',')
                  .Append(it.VariantKeychainFoil.ToString(CultureInfo.InvariantCulture)).Append(',')
                  .Append(Sanitize(it.VariantKeychainId));
            }
        }

        internal static void ParseItems(string text, List<CrateItem> list)
        {
            if (string.IsNullOrEmpty(text)) return;
            foreach (string entry in text.Split(';'))
            {
                string[] f = entry.Split(',');
                if (f.Length < 5) continue;
                list.Add(new CrateItem
                {
                    ProductId = f[0],
                    DefinitionId = ParseInt(f[1]),
                    VariantDefinitionId = ParseInt(f[2]),
                    VariantKeychainFoil = ParseInt(f[3]),
                    VariantKeychainId = f[4],
                });
            }
        }

        private static int ParseInt(string s) =>
            int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;

        private static string Sanitize(string s) =>
            string.IsNullOrEmpty(s) ? "" : s.Replace(",", "").Replace(";", "").Replace("|", "");
    }
}
