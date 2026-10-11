using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppProject.Code.Gameplay.Configs;
using Il2CppProject.Code.Gameplay.Player.Controllers;
using Il2CppProject.Code.Gameplay.UI.Game;

namespace AnimeShopCrate
{
    /// <summary>
    /// Key prompts through the game's own prompt list (right side), so they look exactly like
    /// "E Verschieben". Each prompt is an InputAction with an "ASC/" mean key; the widget's text is
    /// replaced with the mod's text when it activates.
    /// </summary>
    internal static class GamePrompts
    {
        private sealed class Prompt
        {
            internal InputAction Action;
            internal bool Shown;
        }

        private static readonly Dictionary<string, Prompt> Prompts = new();
        private static PlayerUIController _ui;
        private static readonly HashSet<string> Wanted = new();

        internal static void Reset()
        {
            Prompts.Clear();
            _ui = null;
            _blockedMeanKeys = null;
        }

        /// <summary>Called once per frame with the prompts that should be visible now.</summary>
        internal static void Update()
        {
            Wanted.Clear();
            var player = Game.LocalPlayer;
            if (player != null)
            {
                if (CratePlacement.Active)
                {
                    Wanted.Add("ASC/place"); Wanted.Add("ASC/cancel"); Wanted.Add("ASC/rotate");
                }
                else if (CrateMode.IsLocalActive)
                {
                    Wanted.Add("ASC/put"); Wanted.Add("ASC/take"); Wanted.Add("ASC/select"); Wanted.Add("ASC/drop");
                }
                else if (CrateInput.AimedWorldCrate != 0 && WorldCrates.OwnerOf(CrateInput.AimedWorldCrate) is ulong o
                         && (o == 0 || o == player.SteamId))
                {
                    Wanted.Add("ASC/pickup");
                }
            }

            var ui = player?._uiController;
            if (ui != _ui)
            {
                // New player/scene: earlier prompts belonged to the old controller.
                foreach (var p in Prompts.Values) p.Shown = false;
                _ui = ui;
            }
            if (_ui == null) return;

            foreach (var kv in Prompts)
                if (kv.Value.Shown && !Wanted.Contains(kv.Key)) Set(kv.Key, kv.Value, false);
            foreach (string key in Wanted)
            {
                if (!Prompts.TryGetValue(key, out var p))
                    Prompts[key] = p = new Prompt { Action = new InputAction(InputFor(key), "", key) };
                if (!p.Shown) Set(key, p, true);
            }
            HideBlockedGamePrompts(player);
        }

        // Game actions the crate takes over while held; their prompts would only confuse.
        private static readonly string[] BlockedInteractions =
        {
            "TakeItemFromProductPlaceInteraction", "TakeItemFromPickupInteraction", "ScatterShelfProductsInteraction",
            "ReplaceObjectInteraction", "ReplaceBattleTableInteraction",
        };
        private static HashSet<string> _blockedMeanKeys;

        private static void HideBlockedGamePrompts(PlayerCharacterController player)
        {
            var widgets = _ui?._gameView?._keyWidgets;
            if (widgets == null) return;
            bool holding = CrateMode.IsLocalActive;
            _blockedMeanKeys ??= CollectBlockedMeanKeys(player);
            for (int i = 0; i < widgets.Count; i++)
            {
                var w = widgets[i];
                if (w == null) continue;
                string mean = w.MeanKey;
                if (mean == null || mean.StartsWith("ASC/", StringComparison.Ordinal)) continue;
                bool visible = !(holding && _blockedMeanKeys.Contains(mean));
                if (w.gameObject.activeSelf != visible) w.gameObject.SetActive(visible);
            }
        }

        private static HashSet<string> CollectBlockedMeanKeys(PlayerCharacterController player)
        {
            var keys = new HashSet<string>();
            var states = player?._interactionsController?._interactionsConfig?._playerStateInteractions;
            if (states == null) return keys;
            for (int i = 0; i < states.Count; i++)
            {
                var state = states[i];
                if (state?._playerStateTypeName == null || !state._playerStateTypeName.Contains("PlayerHandsInteractionsState")) continue;
                var list = state._playerInteractions;
                for (int j = 0; list != null && j < list.Count; j++)
                {
                    var actions = list[j]?._inputActions;
                    for (int k = 0; actions != null && k < actions.Count; k++)
                    {
                        var a = actions[k];
                        if (a?._playerInteractionTypeName == null || a._meanKey == null) continue;
                        foreach (string b in BlockedInteractions)
                            if (a._playerInteractionTypeName.Contains(b)) keys.Add(a._meanKey);
                    }
                }
            }
            Settings.Debug("hiding game prompts while holding the crate: " + string.Join(", ", keys));
            return keys;
        }

        private static void Set(string key, Prompt p, bool show)
        {
            try
            {
                if (show) _ui.ShowControlKey(p.Action);
                else _ui.HideControlKey(p.Action);
                p.Shown = show;
            }
            catch (Exception e)
            {
                p.Shown = show;
                Settings.Debug($"prompt {key} failed: {e.Message}");
            }
        }

        private static InputEnum InputFor(string key) => key switch
        {
            "ASC/put" or "ASC/place" => InputEnum.Use,
            "ASC/take" or "ASC/cancel" => InputEnum.Back,
            "ASC/select" or "ASC/rotate" => InputEnum.RotateHorizontal,
            _ => DropKey.Action,
        };

        [HarmonyPatch(typeof(ControlKeyWidget), nameof(ControlKeyWidget.OnActivate))]
        private static class WidgetTextPatch
        {
            private static void Postfix(ControlKeyWidget __instance)
            {
                try
                {
                    string mean = __instance.MeanKey;
                    if (mean != null && mean.StartsWith("ASC/", StringComparison.Ordinal) && __instance._meanText != null)
                        __instance._meanText.text = Text.Get(mean);
                }
                catch (Exception e) { Settings.Debug("prompt text failed: " + e.Message); }
            }
        }
    }
}
