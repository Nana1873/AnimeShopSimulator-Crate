using Il2CppProject.Code.Core.Services;
using Il2CppProject.Code.Core.UI;
using Il2CppProject.Code.Gameplay.Configs;
using Il2CppProject.Code.Gameplay.Player.Controllers;
using Il2CppProject.Code.Gameplay.Services;

namespace AnimeShopCrate
{
    /// <summary>Lookups for game objects the mod needs, cached per scene.</summary>
    internal static class Game
    {
        private static PlayerCharacterController _localPlayer;

        internal static void Reset() => _localPlayer = null;

        internal static PlayerCharacterController LocalPlayer
        {
            get
            {
                if (_localPlayer != null) return _localPlayer;
                var players = AllServices.Get<PlayerCharacterRegistryService>()?._players;
                if (players == null) return null;
                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i];
                    if (p != null && p.IsOwner) return _localPlayer = p;
                }
                return null;
            }
        }

        internal static PlayerAnimationController Animation(PlayerCharacterController player) =>
            player == null ? null : player.GetComponentInChildren<PlayerAnimationController>(true);

        /// <summary>True when the game would accept a gameplay key press (no menu, no cursor).</summary>
        internal static bool CanTakeKeys()
        {
            var input = AllServices.Get<InputService>();
            if (input != null && !input.CanProcessInput(InputEnum.Inventory)) return false;
            var ui = AllServices.Get<UIService>();
            return ui == null || !ui.IsCursorEnabled;
        }
    }
}
