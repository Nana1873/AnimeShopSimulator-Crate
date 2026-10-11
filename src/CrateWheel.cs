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
        private static Sprite _icon;

        private static void Ensure(InventoryView view)
        {
            var pie = view?._pieMenu;
            if (pie == null || !pie.IsInitialized) return;
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

            _vanillaIconCount = view._toolImages?.Count ?? 0;
            _crateIconIndex = -1;
            AdoptIcon(view, source.transform, clone.transform);
            AdoptLock(view, source.transform, clone.transform);
            _viewWithCrate = view.Pointer;

            view.RefreshLockIcons();
            view.RefreshToolImages();
            Settings.Debug($"wheel after: {tracker.PieMenuItems.Count} items, {view._toolImages?.Count} icons (crate icon index {_crateIconIndex}).");
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
                if (copy != null) locks.Add(copy.gameObject);
                return;
            }
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
                if (__instance.Pointer != _viewWithCrate || _crateIconIndex < 0) return true;
                if (visualsCount != _vanillaIconCount + 1) return true; // lock list or other list
                if (visualIndex == _crateIconIndex) { __result = CrateMode.CrateMenuId; return false; }
                int entries = EntryCount(__instance);
                __result = entries == _vanillaIconCount ? visualIndex + 1 : visualIndex;
                return false;
            }
        }
    }
}
