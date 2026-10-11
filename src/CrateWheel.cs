using System;
using HarmonyLib;
using Il2CppProject.Code.Gameplay.UI.Other;
using Il2CppSimplePieMenu;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AnimeShopCrate
{
    /// <summary>
    /// Adds a 6th slot (menu id 5) to the Q wheel by cloning the Bat slot.
    /// The wheel colours and locks tool icons by visual index, so the clone's icon is appended to
    /// the view's icon lists and the index → id mapping is patched to give it id 5.
    /// </summary>
    internal static class CrateWheel
    {
        private static IntPtr _viewWithCrate;
        private static int _crateIconIndex = -1;
        private static int _vanillaIconCount = -1;
        private static int _crateLockIndex = -1;
        private static int _vanillaLockCount = -1;
        private static Sprite _icon;

        private static void Ensure(InventoryView view)
        {
            var pie = view?._pieMenu;
            if (pie == null || !pie.IsInitialized || !Hooks.Enabled) return;
            var tracker = pie.MenuItemsTracker;
            var items = tracker?.PieMenuItems;
            if (items == null) return;

            PieMenuItem source = null;
            foreach (var kv in items)
            {
                if (kv.Value == null) continue;
                if (kv.Value.id == CrateMode.CrateMenuId) return; // already added to this wheel
                if (source == null || kv.Value.id == 4) source = kv.Value;
            }
            if (source == null) return;

            var dir = pie.PieMenuElements?.menuItemsDir;
            if (dir == null) return;
            Settings.Debug($"wheel before: {items.Count} items, {view._toolImages?.Count} icons, {view._toolLockIcons?.Count} locks, {EntryCount(view)} tool entries; cloning '{source.name}' (id {source.id}).");

            var adder = PieMenuShared.References?.MenuItemsManager?.MenuItemAdder;
            if (adder != null)
            {
                var list = new Il2CppSystem.Collections.Generic.List<GameObject>();
                list.Add(source.gameObject);
                adder.Add(pie, list);
            }
            else
            {
                Settings.Debug("PieMenuShared missing, adding the slot by hand.");
                Object.Instantiate(source.gameObject, dir);
                tracker.Initialize(dir);
            }

            var clone = dir.GetChild(dir.childCount - 1)?.GetComponent<PieMenuItem>();
            if (clone == null || clone.Pointer == source.Pointer)
            {
                CrateMod.Log.Warning("could not find the new wheel slot after cloning.");
                return;
            }
            clone.id = CrateMode.CrateMenuId;
            clone.idAssigned = true;
            clone.header = "Kiste";
            clone.details = "Produkte sammeln und einräumen";
            clone.gameObject.name = "Menu Item Crate";

            // The wheel lays slots out counter-clockwise by sibling index starting at the hand.
            // Directly after the hand puts the crate at the clockwise end, i.e. as slot 6.
            clone.transform.SetSiblingIndex(1);
            var drawer = FindDrawer(pie);
            if (drawer != null) drawer.Redraw(pie);
            else { tracker.Initialize(dir); CrateMod.Log.Warning("pie menu drawer not found; the wheel may look uneven."); }

            _vanillaIconCount = view._toolImages?.Count ?? 0;
            _vanillaLockCount = view._toolLockIcons?.Count ?? 0;
            _crateIconIndex = -1;
            _crateLockIndex = -1;
            AdoptIcon(view, source.transform, clone.transform);
            AdoptLock(view, source.transform, clone.transform);
            _viewWithCrate = view.Pointer;

            LayoutHotkeyBadges(pie);
            view.RefreshLockIcons();
            view.RefreshToolImages();
            Settings.Debug($"wheel after: {tracker.PieMenuItems.Count} items, {view._toolImages?.Count} icons (crate icon index {_crateIconIndex}, lock index {_crateLockIndex}).");
            WheelDump.Write(view);
        }

        // The vanilla icon of the cloned slot lives inside the slot: find the same child in the clone.
        private static void AdoptIcon(InventoryView view, Transform source, Transform clone)
        {
            var icons = view._toolImages;
            if (icons == null) return;
            for (int i = 0; i < icons.Count; i++)
            {
                var img = icons[i];
                if (img == null || !img.transform.IsChildOf(source)) continue;
                var copy = Find(clone, RelativePath(source, img.transform))?.GetComponent<Image>();
                if (copy == null) break;
                copy.sprite = Icon();
                copy.preserveAspect = true;
                icons.Add(copy);
                _crateIconIndex = icons.Count - 1;
                return;
            }
            CrateMod.Log.Warning("the wheel icons are not inside the slots; the crate slot has no icon yet.");
        }

        private static void AdoptLock(InventoryView view, Transform source, Transform clone)
        {
            var locks = view._toolLockIcons;
            if (locks == null || _crateIconIndex < 0) return;
            for (int i = 0; i < locks.Count; i++)
            {
                var go = locks[i];
                if (go == null || !go.transform.IsChildOf(source)) continue;
                var copy = Find(clone, RelativePath(source, go.transform));
                if (copy != null) { locks.Add(copy.gameObject); _crateLockIndex = locks.Count - 1; }
                return;
            }
        }

        /// <summary>
        /// The 1-5 hotkey badges are fixed images under "Menu Items", placed for five slots.
        /// Spread them over six slots (clockwise from the top) and add a "6" for the crate.
        /// </summary>
        private static void LayoutHotkeyBadges(PieMenu pie)
        {
            var root = pie.PieMenuElements?.menuItemsDir?.parent;
            if (root == null) return;
            var badges = new System.Collections.Generic.List<RectTransform>();
            float radius = 0f;
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                var text = child.GetComponentInChildren<Il2CppTMPro.TMP_Text>(true);
                if (text == null || !int.TryParse(text.text, out int n) || n < 1 || n > 6) continue;
                var rt = child.TryCast<RectTransform>();
                if (rt == null) continue;
                while (badges.Count < n) badges.Add(null);
                badges[n - 1] = rt;
                radius = Mathf.Max(radius, rt.anchoredPosition.magnitude);
            }
            if (badges.Count < 5 || badges[4] == null || radius <= 0f)
            {
                Settings.Debug("hotkey badges not found; left as they are.");
                return;
            }
            if (badges.Count < 6)
            {
                var six = Object.Instantiate(badges[4].gameObject, root).GetComponent<RectTransform>();
                six.name = "Image (Crate)";
                six.GetComponentInChildren<Il2CppTMPro.TMP_Text>(true).text = "6";
                badges.Add(six);
            }
            for (int i = 0; i < 6; i++)
            {
                if (badges[i] == null) continue;
                float angle = (90f - 60f * i) * Mathf.Deg2Rad;
                badges[i].anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
        }

        private static PieMenuDrawer FindDrawer(PieMenu pie)
        {
            var d = pie.GetComponentInChildren<PieMenuDrawer>(true);
            if (d == null && PieMenuShared.Instance != null) d = PieMenuShared.Instance.GetComponentInChildren<PieMenuDrawer>(true);
            if (d == null) d = Object.FindObjectOfType<PieMenuDrawer>(true);
            return d;
        }

        private static int EntryCount(InventoryView view)
        {
            var list = view._toolAvailability;
            if (list == null) return -1;
            return list.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<Il2CppProject.Code.Gameplay.Configs.InventoryToolAvailability>>().Count;
        }

        private static string RelativePath(Transform root, Transform t)
        {
            string p = t.name;
            for (var x = t.parent; x != null && x != root; x = x.parent) p = x.name + "/" + p;
            return p;
        }

        private static Transform Find(Transform root, string path) => path == root.name ? root : root.Find(path);

        /// <summary>White crate silhouette, tinted by the wheel like the other tool icons.</summary>
        private static Sprite Icon()
        {
            if (_icon != null) return _icon;
            const int s = 128;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { name = "AnimeShopCrate_Icon" };
            var clear = new Color(1, 1, 1, 0);
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                // Crate body tapers slightly towards the bottom.
                float t = (y - 20f) / 70f;
                float inset = 14f + 8f * (1f - Mathf.Clamp01(t));
                bool body = y >= 20 && y <= 90 && x >= inset && x <= s - 1 - inset;
                bool rim = y > 90 && y <= 100 && x >= 10 && x <= s - 11;
                bool handle = y >= 74 && y <= 82 && x >= 48 && x <= 79;
                bool slot1 = y >= 52 && y <= 60 && x >= 30 && x <= 97;
                bool slot2 = y >= 32 && y <= 40 && x >= 34 && x <= 93;
                bool solid = (body || rim) && !handle && !slot1 && !slot2;
                tex.SetPixel(x, y, solid ? Color.white : clear);
            }
            tex.Apply();
            _icon = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
            _icon.name = "AnimeShopCrate_Icon";
            Object.DontDestroyOnLoad(tex);
            return _icon;
        }

        [HarmonyPatch(typeof(InventoryView), nameof(InventoryView.HandlePieMenuInitialized))]
        private static class InitPatch
        {
            private static void Postfix(InventoryView __instance)
            {
                try { Ensure(__instance); }
                catch (Exception e) { CrateMod.Log.Error("adding the wheel slot failed: " + e); }
            }
        }

        [HarmonyPatch(typeof(InventoryView), nameof(InventoryView.OnActivateView))]
        private static class ActivatePatch
        {
            private static void Postfix(InventoryView __instance)
            {
                try { Ensure(__instance); }
                catch (Exception e) { CrateMod.Log.Error("adding the wheel slot failed: " + e); }
            }
        }

        // Icon index → menu id. Vanilla maps index i to id i+1 when there is one icon per tool entry;
        // the extra crate icon breaks that equality, so map it explicitly.
        [HarmonyPatch(typeof(InventoryView), nameof(InventoryView.ResolveMenuItemIdForVisualIndex))]
        private static class IndexPatch
        {
            private static bool Prefix(InventoryView __instance, int visualIndex, int visualsCount, ref int __result)
            {
                if (__instance.Pointer != _viewWithCrate) return true;
                int crateIndex, vanillaCount;
                if (_crateIconIndex >= 0 && visualsCount == _vanillaIconCount + 1) { crateIndex = _crateIconIndex; vanillaCount = _vanillaIconCount; }
                else if (_crateLockIndex >= 0 && visualsCount == _vanillaLockCount + 1) { crateIndex = _crateLockIndex; vanillaCount = _vanillaLockCount; }
                else return true;
                if (visualIndex == crateIndex) { __result = CrateMode.CrateMenuId; return false; }
                // Vanilla rule with the list length it had before the crate was added.
                __result = vanillaCount == EntryCount(__instance) ? visualIndex + 1 : visualIndex;
                return false;
            }
        }
    }
}
