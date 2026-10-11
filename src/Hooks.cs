using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace AnimeShopCrate
{
    /// <summary>
    /// Applies the Harmony patches feature by feature and reports each one, like
    /// "[Save] hooked 4/4 game methods". If a critical feature cannot hook into the game (usually
    /// after a game update renamed a method), the whole crate turns off so no products can be lost.
    /// </summary>
    internal static class Hooks
    {
        internal const string BuiltForGame = "1.0.72";

        private sealed class Feature
        {
            internal string Name;
            internal bool Critical;
        }

        // Patch classes are grouped by the class they are nested in.
        private static readonly Dictionary<Type, Feature> Features = new()
        {
            [typeof(CrateMode)] = new Feature { Name = "Crate mode", Critical = true },
            [typeof(CrateNet)] = new Feature { Name = "Network", Critical = true },
            [typeof(CrateSave)] = new Feature { Name = "Save", Critical = true },
            [typeof(CrateWheel)] = new Feature { Name = "Q wheel slot" },
            [typeof(CrateInput)] = new Feature { Name = "Hand actions while holding" },
            [typeof(CrateStock)] = new Feature { Name = "Stock count" },
            [typeof(GamePrompts)] = new Feature { Name = "Key prompts" },
        };

        /// <summary>False when a critical feature failed; the crate stays off for this session.</summary>
        internal static bool Enabled { get; private set; } = true;

        internal static void CheckGameVersion()
        {
            string running = UnityEngine.Application.version;
            if (running == BuiltForGame) CrateMod.Log.Msg($"Built for game {BuiltForGame}, running on {running}.");
            else CrateMod.Log.Warning($"Built for game {BuiltForGame}, running on {running}. If the game changed a method the crate hooks, the log says so and the crate stays off where needed.");
        }

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            var patchClasses = typeof(Hooks).Assembly.GetTypes()
                .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0 && t.DeclaringType != null)
                .GroupBy(t => t.DeclaringType);

            var failedCritical = new List<string>();
            foreach (var group in patchClasses)
            {
                Features.TryGetValue(group.Key, out var feature);
                string name = feature?.Name ?? group.Key.Name;
                int hooked = 0, total = 0;
                var errors = new List<string>();
                foreach (var type in group)
                {
                    int expected = Expected(type);
                    total += expected;
                    try
                    {
                        hooked += harmony.CreateClassProcessor(type).Patch()?.Count ?? 0;
                    }
                    catch (Exception e)
                    {
                        errors.Add($"{type.Name}: {Root(e).Message}");
                    }
                }
                if (errors.Count == 0 && hooked >= total)
                {
                    CrateMod.Log.Msg($"[{name}] hooked {hooked}/{total} game methods.");
                    continue;
                }
                string detail = string.Join("; ", errors);
                if (feature?.Critical == true)
                {
                    failedCritical.Add(name);
                    CrateMod.Log.Error($"[{name}] hooked {hooked}/{total} game methods: {detail}. A game update probably renamed a method it needs.");
                }
                else
                {
                    CrateMod.Log.Warning($"[{name}] hooked {hooked}/{total} game methods: {detail}. This part may not work; the rest of the crate does.");
                }
            }

            if (failedCritical.Count > 0)
            {
                Enabled = false;
                CrateMod.Log.Error($"The crate is turned off for this session because {string.Join(", ", failedCritical)} could not hook into the game. Your products and saved crates are untouched. Look for a mod update.");
            }
        }

        // How many game methods a patch class targets (one per [HarmonyPatch] class, or its TargetMethods).
        private static int Expected(Type type)
        {
            // Plain reflection: AccessTools logs a warning for every class without TargetMethods.
            var targets = type.GetMethod("TargetMethods", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (targets == null) return 1;
            try { return ((IEnumerable<System.Reflection.MethodBase>)targets.Invoke(null, null)).Count(); }
            catch { return 1; }
        }

        private static Exception Root(Exception e)
        {
            while (e.InnerException != null) e = e.InnerException;
            return e;
        }
    }
}
