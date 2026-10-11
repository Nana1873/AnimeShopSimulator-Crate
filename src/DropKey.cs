using System;
using Il2CppProject.Code.Core.Services;
using Il2CppProject.Code.Gameplay.Configs;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace AnimeShopCrate
{
    /// <summary>
    /// The key that sets the crate down / picks it up: by default the game's own "drop" binding (the
    /// key that puts boxes down), read from its input config so rebinding in the game carries over.
    /// The DropKey setting can force a specific key instead.
    /// </summary>
    internal static class DropKey
    {
        private static KeyControl _key;
        private static float _nextResolve;

        internal static string Label => _key != null ? _key.displayName : "?";

        /// <summary>The game action whose key is used (for the native key prompt). Hand = E by default.</summary>
        internal static InputEnum Action { get; private set; } = InputEnum.Hand;

        internal static void Reset() => _key = null;

        internal static bool WasPressed()
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            if (_key == null && UnityEngine.Time.time >= _nextResolve)
            {
                _nextResolve = UnityEngine.Time.time + 5f;
                _key = Resolve(kb);
            }
            return _key != null && _key.wasPressedThisFrame;
        }

        private static KeyControl Resolve(Keyboard kb)
        {
            string wanted = Settings.DropKey.Value?.Trim();
            string source = "setting";
            if (string.IsNullOrEmpty(wanted) || wanted.Equals("Game", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var input = AllServices.Get<InputService>();
                    InputEnum action = PlaceBoxAction(out source);
                    Action = action;
                    wanted = input?._config?.GetKey(action, input._player);
                }
                catch (Exception e) { Settings.Debug("reading the game's drop key failed: " + e.Message); wanted = null; }
            }
            var key = Find(kb, wanted) ?? kb.eKey;
            Settings.Debug($"crate drop key: '{wanted}' ({source}) → {key.displayName}");
            return key;
        }

        /// <summary>
        /// The action the game binds to "put a carried box down" (PlacePickupInteraction in the
        /// carry-box state), read from the player's interaction config. Falls back to Drop; a non-keyboard binding falls back to E.
        /// </summary>
        private static InputEnum PlaceBoxAction(out string source)
        {
            source = "game drop key (fallback)";
            var config = Game.LocalPlayer?._interactionsController?._interactionsConfig;
            var states = config?._playerStateInteractions;
            if (states == null) return InputEnum.Drop;
            for (int i = 0; i < states.Count; i++)
            {
                var state = states[i];
                if (state?._playerStateTypeName == null || !state._playerStateTypeName.Contains("PlayerPickupInteractionsState")) continue;
                var list = state._playerInteractions;
                for (int j = 0; list != null && j < list.Count; j++)
                {
                    var actions = list[j]?._inputActions;
                    for (int k = 0; actions != null && k < actions.Count; k++)
                    {
                        var a = actions[k];
                        if (a?._playerInteractionTypeName != null && a._playerInteractionTypeName.Contains("PlacePickupInteraction"))
                        {
                            source = "game key for placing boxes (" + a._input + ")";
                            return a._input;
                        }
                    }
                }
            }
            return InputEnum.Drop;
        }

        private static KeyControl Find(Keyboard kb, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (Enum.TryParse(name.Replace(" ", ""), true, out Key k) && k != Key.None) return kb[k];
            foreach (var key in kb.allKeys)
                if (key != null && (string.Equals(key.displayName, name, StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(key.name, name, StringComparison.OrdinalIgnoreCase)))
                    return key;
            return null;
        }
    }
}
