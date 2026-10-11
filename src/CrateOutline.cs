using System;
using Il2CppEPOOutline;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AnimeShopCrate
{
    /// <summary>
    /// Yellow hover outline like the game's boxes and racks: an EPO Outlinable on the crate with the
    /// settings copied from one of the game's own outlinables, switched on while aimed at.
    /// </summary>
    internal static class CrateOutline
    {
        private static Outlinable _template;

        internal static void Reset() => _template = null;

        internal static void Attach(GameObject crate)
        {
            try
            {
                var outline = crate.GetComponent<Outlinable>() ?? crate.AddComponent<Outlinable>();
                outline.AddAllChildRenderersToRenderingList();
                var t = Template();
                if (t != null)
                {
                    outline.RenderStyle = t.RenderStyle;
                    outline.OutlineLayer = t.OutlineLayer;
                    outline.DrawingMode = t.DrawingMode;
                    outline.ComplexMaskingMode = t.ComplexMaskingMode;
                    Copy(t.OutlineParameters, outline.OutlineParameters);
                    Copy(t.FrontParameters, outline.FrontParameters);
                    Copy(t.BackParameters, outline.BackParameters);
                }
                else
                {
                    outline.OutlineParameters.Color = new Color(1f, 0.92f, 0.1f, 1f);
                }
                outline.enabled = false;
            }
            catch (Exception e) { Settings.Debug("crate outline unavailable: " + e.Message); }
        }

        /// <summary>Re-collects renderers after the contents changed.</summary>
        internal static void Refresh(GameObject crate)
        {
            var outline = crate != null ? crate.GetComponent<Outlinable>() : null;
            if (outline == null) return;
            try { outline.AddAllChildRenderersToRenderingList(); }
            catch (Exception e) { Settings.Debug("crate outline refresh failed: " + e.Message); }
        }

        internal static void Set(GameObject crate, bool on)
        {
            var outline = crate != null ? crate.GetComponent<Outlinable>() : null;
            if (outline != null && outline.enabled != on) outline.enabled = on;
        }

        private static void Copy(Outlinable.OutlineProperties from, Outlinable.OutlineProperties to)
        {
            if (from == null || to == null) return;
            to.Enabled = from.Enabled;
            to.Color = from.Color;
            to.DilateShift = from.DilateShift;
            to.BlurShift = from.BlurShift;
            to.fillPass = from.fillPass;
        }

        // Any outlinable of the game will do; boxes and product slots all share the yellow style.
        private static Outlinable Template()
        {
            if (_template != null) return _template;
            foreach (var o in Object.FindObjectsOfType<Outlinable>(true))
            {
                if (o == null || o.name.StartsWith("AnimeShopCrate", StringComparison.Ordinal)) continue;
                _template = o;
                Settings.Debug($"crate outline copies the style of '{o.name}'.");
                break;
            }
            return _template;
        }
    }
}
