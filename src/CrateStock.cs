using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppProject.Code.Gameplay.Services;

namespace AnimeShopCrate
{
    /// <summary>
    /// Products in crates still belong to the shop: after the game counts its physical stock (shelf
    /// slots and boxes, used by the order/store computer and reorder helpers), add every crate's
    /// contents, held or standing. Runs on every player, since all of them mirror the crates.
    /// </summary>
    internal static class CrateStock
    {
        [HarmonyPatch(typeof(ProductsService), nameof(ProductsService.RefreshPhysicalStockCounts))]
        private static class RefreshPatch
        {
            private static void Postfix(ProductsService __instance)
            {
                try
                {
                    var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    foreach (ulong owner in CrateStore.Owners) Count(CrateStore.Get(owner), counts);
                    WorldCrates.ForEachContents(list => Count(list, counts));
                    foreach (var kv in counts) __instance.AddPhysicalStock(Ids.FromText(kv.Key), kv.Value);
                }
                catch (Exception e) { Settings.Debug("adding crate stock failed: " + e.Message); }
            }
        }

        private static void Count(List<CrateItem> items, Dictionary<string, int> counts)
        {
            foreach (var it in items)
            {
                if (string.IsNullOrEmpty(it.ProductId)) continue;
                string key = System.Guid.TryParse(it.ProductId, out var g) ? g.ToString("N") : it.ProductId;
                counts[key] = counts.TryGetValue(key, out int n) ? n + 1 : 1;
            }
        }
    }
}
