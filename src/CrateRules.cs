using System.Collections.Generic;
using Il2CppProject.Code.Core.Services;
using Il2CppProject.Code.Gameplay.Definitions;
using Il2CppProject.Code.Gameplay.Services;

namespace AnimeShopCrate
{
    /// <summary>What fits into a crate and how much room it takes.</summary>
    internal static class CrateRules
    {
        // Space points per product type. Types missing here do not go into the crate.
        private static readonly Dictionary<EProductType, int> Space = new()
        {
            { EProductType.Pin, 1 },
            { EProductType.Keychain, 1 },
            { EProductType.Booster, 1 },
            { EProductType.Manga, 2 },
            { EProductType.PinPlate, 2 },
            { EProductType.DakiMini, 3 },
            { EProductType.BoxPin, 3 },
            { EProductType.BoxKeychain, 3 },
            { EProductType.Figure, 4 },
            { EProductType.BoxBoosters, 4 },
            { EProductType.BoxFigures, 6 },
            { EProductType.Dress, 4 },
            { EProductType.BigBoxBoosters, 8 },
            { EProductType.Cushion, 8 },
            { EProductType.Daki, 12 },
        };

        internal static int Capacity => Settings.CrateCapacity.Value;

        internal static bool Allowed(ProductDefinition def) => def != null && Space.ContainsKey(def.Type);

        internal static int SpaceOf(ProductDefinition def) =>
            def != null && Space.TryGetValue(def.Type, out int s) ? s : 99;

        internal static int SpaceOf(int definitionId) => SpaceOf(Definition(definitionId));

        internal static ProductDefinition Definition(int definitionId) =>
            AllServices.Get<ProductsService>()?.GetProductDefinition(definitionId);

        internal static string Name(int definitionId)
        {
            var def = Definition(definitionId);
            return def != null ? def.Name : "#" + definitionId;
        }
    }
}
