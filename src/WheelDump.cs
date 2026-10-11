using System;
using System.IO;
using System.Text;
using Il2CppProject.Code.Gameplay.UI.Other;
using Il2CppTMPro;
using MelonLoader.Utils;
using UnityEngine;

namespace AnimeShopCrate
{
    /// <summary>Debug aid: writes the Q wheel's object tree to UserData/AnimeShopCrate/wheel-dump.txt.</summary>
    internal static class WheelDump
    {
        internal static void Write(InventoryView view)
        {
            if (!Settings.DebugLog.Value || view == null) return;
            try
            {
                var sb = new StringBuilder();
                Dump(view.transform, 0, sb);
                string dir = Path.Combine(MelonEnvironment.UserDataDirectory, "AnimeShopCrate");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "wheel-dump.txt"), sb.ToString());
            }
            catch (Exception e) { CrateMod.Log.Warning("wheel dump failed: " + e.Message); }
        }

        private static void Dump(Transform t, int depth, StringBuilder sb)
        {
            if (depth > 10) return;
            sb.Append(' ', depth * 2).Append(t.name);
            if (!t.gameObject.activeSelf) sb.Append(" [off]");
            var rt = t.TryCast<RectTransform>();
            if (rt != null) sb.Append($" pos={rt.anchoredPosition} rot={t.localEulerAngles.z:0}");
            var comps = t.GetComponents<Component>();
            foreach (var c in comps)
            {
                if (c == null) continue;
                string n = c.GetIl2CppType().Name;
                if (n == "RectTransform" || n == "Transform" || n == "CanvasRenderer") continue;
                sb.Append(" <").Append(n).Append('>');
            }
            var tmp = t.GetComponent<TMP_Text>();
            if (tmp != null) sb.Append(" text=\"").Append(tmp.text).Append('"');
            sb.AppendLine();
            for (int i = 0; i < t.childCount; i++) Dump(t.GetChild(i), depth + 1, sb);
        }
    }
}
