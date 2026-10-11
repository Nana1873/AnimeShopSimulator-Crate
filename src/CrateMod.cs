using AnimeShopCrate;
using MelonLoader;

[assembly: MelonInfo(typeof(CrateMod), "Anime Shop Crate", "1.0.0", "Nana1873")]
[assembly: MelonGame("OneMoreTime", "Anime Shop Simulator")]
// Patches are applied per feature by Hooks, with a report in the log.
[assembly: HarmonyDontPatchAll]

namespace AnimeShopCrate
{
    public sealed class CrateMod : MelonMod
    {
        internal static MelonLogger.Instance Log;

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            Settings.Init();
            CrateStore.Changed += CrateVisual.Refresh;
            Hooks.CheckGameVersion();
            Hooks.Apply(HarmonyInstance);
            if (Hooks.Enabled) Log.Msg("Loaded. Q wheel slot 6 or key 6 = crate. Settings: UserData/MelonPreferences.cfg → [AnimeShopCrate].");
        }

        public override void OnUpdate()
        {
            if (!Hooks.Enabled) return;
            CrateSave.Update();
            CrateMode.Update();
            CrateInput.Update();
            GamePrompts.Update();
            GameUi.UpdatePanel();
        }

        public override void OnGUI()
        {
            if (!Hooks.Enabled) return;
            CrateHud.Draw();
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            Game.Reset();
            CrateInput.Reset();
            CrateSave.ResetScene();
            CrateVisual.Reset();
            WorldCrates.Reset();
            DropKey.Reset();
            CratePlacement.Reset();
            CrateOutline.Reset();
            GamePrompts.Reset();
            GameUi.Reset();
        }
    }
}
