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
                source = "game binding";
                try
                {
                    var input = AllServices.Get<InputService>();
                    wanted = input?._config?.GetKey(InputEnum.Drop, input._player);
                }
                catch (Exception e) { Settings.Debug("reading the game's drop key failed: " + e.Message); wanted = null; }
            }
            var key = Find(kb, wanted) ?? kb.fKey;
            Settings.Debug($"crate drop key: '{wanted}' ({source}) → {key.displayName}");
            return key;
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
