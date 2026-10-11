using UnityEngine;

namespace AnimeShopCrate
{
    /// <summary>
    /// Fallback overlay for hints when the game's warning widget is not available. Prompts, the crate
    /// panel and warnings normally use the game's own UI (see <see cref="GamePrompts"/>, <see cref="GameUi"/>).
    /// </summary>
    internal static class CrateHud
    {
        private static GUIStyle _center;

        internal static void Draw()
        {
            if (CrateInput.Hint == null || Time.time >= CrateInput.HintUntil) return;
            if (_center == null)
            {
                _center = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _center.normal.textColor = new Color(1f, 0.85f, 0.45f);
            }
            GUI.Label(new Rect(Screen.width / 2f - 300, Screen.height * 0.62f, 600, 30), CrateInput.Hint, _center);
        }
    }
}
