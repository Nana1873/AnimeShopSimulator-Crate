using System;
using System.Collections.Generic;
using System.Text;
using Il2CppProject.Code.Gameplay.UI.Game;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AnimeShopCrate
{
    /// <summary>
    /// UI built from the game's own pieces: warnings through the game's warning widget, and the crate
    /// panel as uGUI under the game's HUD view with its font, panel look and product icons.
    /// </summary>
    internal static class GameUi
    {
        private static GameView View => Game.LocalPlayer?._uiController?._gameView;

        /// <summary>Centre warning like the game's "slot full" messages. False if the HUD is not there.</summary>
        internal static bool Warn(string text)
        {
            try
            {
                var w = View?._warningWidget;
                if (w == null || w._text == null) return false;
                w._text.text = text;
                w.SpawnWarning();
                return true;
            }
            catch (Exception e) { Settings.Debug("warning widget failed: " + e.Message); return false; }
        }

        // ---------- crate panel ----------

        private static GameObject _panel;
        private static RectTransform _rows;
        private static TextMeshProUGUI _title, _footer;
        private static string _shownState;
        private static TMP_FontAsset _font;
        private static Material _fontMaterial;

        internal static void Reset()
        {
            _panel = null;
            _shownState = null;
            _font = null;
        }

        internal static void UpdatePanel()
        {
            var player = Game.LocalPlayer;
            bool show = player != null && CrateMode.IsLocalActive;
            if (!show)
            {
                if (_panel != null && _panel.activeSelf) _panel.SetActive(false);
                return;
            }
            if (_panel == null && !Build()) return;
            if (!_panel.activeSelf) _panel.SetActive(true);

            var groups = CrateInput.Groups();
            int used = CrateStore.UsedSpace(player.SteamId);
            string state = Signature(groups, used);
            if (state == _shownState) return;
            _shownState = state;
            Fill(groups, used);
        }

        private static string Signature(List<(CrateItem item, int count)> groups, int used)
        {
            var sb = new StringBuilder().Append(used).Append('|').Append(CrateInput.Selected).Append('|').Append(Text.German);
            foreach (var g in groups) sb.Append(g.item.DefinitionId).Append('x').Append(g.count).Append(';');
            return sb.ToString();
        }

        private static bool Build()
        {
            var view = View;
            if (view == null) return false;
            try
            {
                var fontSource = view._levelText ?? view._questTitleText;
                if (fontSource != null) { _font = fontSource.font; _fontMaterial = fontSource.fontSharedMaterial; }

                _panel = new GameObject("AnimeShopCrate_Panel");
                var rt = _panel.AddComponent<RectTransform>();
                rt.SetParent(view.transform, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(30f, -170f);
                rt.sizeDelta = new Vector2(520f, 100f);

                var bg = _panel.AddComponent<Image>();
                StyleLike(bg, view);
                var layout = _panel.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(16, 16, 12, 12);
                layout.spacing = 6f;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;
                var fit = _panel.AddComponent<ContentSizeFitter>();
                fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                _title = Label(_panel.transform, "Title", 20f, FontStyles.Bold, TextAlignmentOptions.Left);
                var rowsGo = new GameObject("Rows");
                _rows = rowsGo.AddComponent<RectTransform>();
                _rows.SetParent(_panel.transform, false);
                var rowsLayout = rowsGo.AddComponent<VerticalLayoutGroup>();
                rowsLayout.spacing = 4f;
                rowsLayout.childControlHeight = true;
                rowsLayout.childControlWidth = true;
                rowsLayout.childForceExpandHeight = false;
                rowsLayout.childForceExpandWidth = true;
                _footer = Label(_panel.transform, "Footer", 16f, FontStyles.Normal, TextAlignmentOptions.Left);
                _footer.color = new Color(1f, 1f, 1f, 0.75f);
                Settings.Debug("crate panel built under the game's HUD.");
                return true;
            }
            catch (Exception e)
            {
                CrateMod.Log.Warning("building the crate panel failed: " + e.Message);
                if (_panel != null) Object.Destroy(_panel);
                _panel = null;
                return false;
            }
        }

        // Take the look of one of the game's HUD panels; fall back to a dark translucent box.
        private static void StyleLike(Image target, GameView view)
        {
            Image source = null;
            foreach (var t in new Transform[] { view._questPanel, view._tutorialPanel })
            {
                if (t == null) continue;
                source = t.GetComponent<Image>();
                if (source != null) break;
            }
            if (source != null && source.sprite != null)
            {
                target.sprite = source.sprite;
                target.type = source.type;
                target.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
                target.color = source.color;
            }
            else
            {
                target.color = new Color(0.08f, 0.08f, 0.12f, 0.78f);
            }
        }

        private static TextMeshProUGUI Label(Transform parent, string name, float size, FontStyles style, TextAlignmentOptions align)
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>().SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (_font != null) t.font = _font;
            if (_fontMaterial != null) t.fontSharedMaterial = _fontMaterial;
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = align;
            t.color = Color.white;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false;
            return t;
        }

        private static void Fill(List<(CrateItem item, int count)> groups, int used)
        {
            int count = 0;
            foreach (var g in groups) count += g.count;
            _title.text = $"{Text.Get("crate")}  ·  {count} {Text.Get(count == 1 ? "product" : "products")}  ·  {Text.Get("space")} {used}/{CrateRules.Capacity}";
            _footer.text = groups.Count == 0 ? Text.Get("empty") : "";
            _footer.gameObject.SetActive(groups.Count == 0);

            for (int i = _rows.childCount - 1; i >= 0; i--) Object.Destroy(_rows.GetChild(i).gameObject);
            for (int i = 0; i < groups.Count; i++) Row(groups[i].item, groups[i].count, i == CrateInput.Selected);
        }

        private static void Row(CrateItem item, int count, bool selected)
        {
            var row = new GameObject("Row");
            var rt = row.AddComponent<RectTransform>();
            rt.SetParent(_rows, false);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlHeight = true;
            h.childControlWidth = true;
            h.childForceExpandHeight = false;
            h.childForceExpandWidth = false;
            var le = row.AddComponent<LayoutElement>();
            le.minHeight = 34f;
            if (selected)
            {
                var hl = row.AddComponent<Image>();
                hl.color = new Color(1f, 0.82f, 0.25f, 0.22f);
            }

            var def = CrateRules.Definition(item.DefinitionId);
            var iconGo = new GameObject("Icon");
            iconGo.AddComponent<RectTransform>().SetParent(row.transform, false);
            var icon = iconGo.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            if (def?.Icon != null) icon.sprite = def.Icon; else icon.color = new Color(1, 1, 1, 0);
            var iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.preferredWidth = iconLe.minWidth = 32f;
            iconLe.preferredHeight = iconLe.minHeight = 32f;

            var name = Label(row.transform, "Name", 18f, selected ? FontStyles.Bold : FontStyles.Normal, TextAlignmentOptions.Left);
            name.text = $"{count}×  {CrateRules.Name(item.DefinitionId)}";
            if (selected) name.color = new Color(1f, 0.86f, 0.4f);
            var nameLe = name.gameObject.AddComponent<LayoutElement>();
            nameLe.flexibleWidth = 1f;

            var space = Label(row.transform, "Space", 15f, FontStyles.Normal, TextAlignmentOptions.Right);
            space.text = $"{CrateRules.SpaceOf(item.DefinitionId) * count} {Text.Get("space")}";
            space.color = new Color(1f, 1f, 1f, 0.6f);
            var spaceLe = space.gameObject.AddComponent<LayoutElement>();
            spaceLe.preferredWidth = 80f;
        }
    }
}
