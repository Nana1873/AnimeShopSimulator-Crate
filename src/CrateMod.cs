using AnimeShopCrate;
using MelonLoader;

[assembly: MelonInfo(typeof(CrateMod), "Anime Shop Crate", "0.1.0", "jmach")]
[assembly: MelonGame("OneMoreTime", "Anime Shop Simulator")]

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
            // Harmony attributes in this assembly are applied by MelonLoader automatically.
            Log.Msg("Anime Shop Crate loaded. Q wheel slot 6 / key 6 = crate.");
        }

        public override void OnUpdate()
        {
            CrateSave.Update();
            CrateMode.Update();
            CrateInput.Update();
        }

        public override void OnGUI()
        {
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
        }
    }
}
