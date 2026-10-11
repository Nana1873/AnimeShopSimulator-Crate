using UnityEngine;

namespace AnimeShopCrate
{
    /// <summary>Simple on-screen list of the local crate's contents while it is held.</summary>
    internal static class CrateHud
    {
        private static GUIStyle _box, _line, _selected, _hint, _right, _center;

        internal static void Draw()
        {
            var player = Game.LocalPlayer;
            if (player == null) return;
            if (!CrateMode.IsLocalActive)
            {
                if (CrateInput.AimedWorldCrate != 0)
                {
                    Styles();
                    int n = WorldCrates.ItemCount(CrateInput.AimedWorldCrate);
                    ulong owner = WorldCrates.OwnerOf(CrateInput.AimedWorldCrate);
                    string text = owner != 0 && owner != player.SteamId
                        ? $"Kiste von einem Mitspieler ({n} {(n == 1 ? "Produkt" : "Produkte")})"
                        : $"[{DropKey.Label}] Kiste aufheben ({n} {(n == 1 ? "Produkt" : "Produkte")})";
                    GUI.Label(new Rect(Screen.width / 2f - 300, Screen.height * 0.56f, 600, 30), text, _center);
                }
                DrawHint();
                return;
            }
            Styles();

            var groups = CrateInput.Groups();
            int used = CrateStore.UsedSpace(player.SteamId);
            float lineH = 24f, w = 400f;
            float h = 40f + lineH * Mathf.Max(1, groups.Count) + 46f;
            // Left side, below the level/cleanliness bars: the game's key prompts live on the right.
            float x = 24f, y = Screen.height * 0.22f;

            GUI.Box(new Rect(x, y, w, h), "", _box);
            int count = 0;
            foreach (var g in groups) count += g.count;
            GUI.Label(new Rect(x + 12, y + 8, w - 24, lineH), $"Kiste: {count} {(count == 1 ? "Produkt" : "Produkte")} · Platz {used}/{CrateRules.Capacity}", _line);
            float ly = y + 36;
            if (groups.Count == 0) GUI.Label(new Rect(x + 12, ly, w - 24, lineH), "leer – Rechtsklick auf ein Regalfach", _line);
            for (int i = 0; i < groups.Count; i++)
            {
                bool sel = i == CrateInput.Selected;
                string name = CrateRules.Name(groups[i].item.DefinitionId);
                int space = CrateRules.SpaceOf(groups[i].item.DefinitionId) * groups[i].count;
                var row = new Rect(x + 12, ly + i * lineH, w - 24, lineH);
                GUI.Label(new Rect(row.x, row.y, row.width - 64, row.height), $"{(sel ? "▶ " : "   ")}{groups[i].count}× {name}", sel ? _selected : _line);
                GUI.Label(row, $"{space} Platz", _right);
            }
            GUI.Label(new Rect(x + 12, y + h - 44, w - 24, 20), "LMB einräumen · RMB nehmen · Mausrad wählen", _hint);
            GUI.Label(new Rect(x + 12, y + h - 24, w - 24, 20), $"{DropKey.Label} abstellen · 6 wegpacken", _hint);
            DrawHint();
        }

        private static void DrawHint()
        {
            if (CrateInput.Hint != null && Time.time < CrateInput.HintUntil)
            {
                Styles();
                GUI.Label(new Rect(Screen.width / 2f - 300, Screen.height * 0.62f, 600, 30), CrateInput.Hint, _center);
            }
        }

        private static void Styles()
        {
            if (_box != null) return;
            var bg = new Texture2D(1, 1);
            bg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.55f));
            bg.Apply();
            Object.DontDestroyOnLoad(bg);
            _box = new GUIStyle(GUI.skin.box);
            _box.normal.background = bg;
            _line = new GUIStyle(GUI.skin.label) { fontSize = 16, clipping = TextClipping.Clip, wordWrap = false };
            _line.normal.textColor = Color.white;
            _selected = new GUIStyle(_line) { fontStyle = FontStyle.Bold };
            _selected.normal.textColor = new Color(1f, 0.85f, 0.45f);
            _right = new GUIStyle(_line) { fontSize = 13, alignment = TextAnchor.MiddleRight };
            _right.normal.textColor = new Color(1f, 1f, 1f, 0.6f);
            _center = new GUIStyle(_selected) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
            _hint = new GUIStyle(_line) { fontSize = 12 };
            _hint.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
        }
    }
}
