using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AnimeShopCrate
{
    /// <summary>Fills a crate model with small copies of the products it holds.</summary>
    internal static class CrateContents
    {
        /// <summary>Outlines the copies of one product (the selected one) inside a crate.</summary>
        internal static void Highlight(GameObject crate, string productId)
        {
            var contents = crate != null ? crate.transform.Find("Contents") : null;
            if (contents == null) return;
            for (int i = 0; i < contents.childCount; i++)
            {
                var child = contents.GetChild(i).gameObject;
                bool on = productId != null && child.name.Length > 2 && Ids.Same(child.name.Substring(2), productId);
                CrateOutline.Set(child, on);
            }
        }


        internal static void Fill(GameObject crate, List<CrateItem> items, bool outlines = false)
        {
            if (crate == null) return;
            var old = crate.transform.Find("Contents");
            if (old != null) Object.Destroy(old.gameObject);
            if (items == null || items.Count == 0) return;

            var contents = new GameObject("Contents");
            contents.transform.SetParent(crate.transform, false);

            // Products stand upright like books in a box: three lanes across, filled from the far
            // wall towards you, the flat side facing you. Same products stay together.
            const int Lanes = 3;
            const float Gap = 0.005f;
            float innerW = CrateVisual.Width - 2 * CrateVisual.Wall;
            float innerD = CrateVisual.Depth - 2 * CrateVisual.Wall;
            float innerH = CrateVisual.Height - CrateVisual.Wall;
            float laneW = innerW / Lanes;

            var sorted = new List<CrateItem>(items);
            sorted.Sort((a, b) => a.DefinitionId.CompareTo(b.DefinitionId));

            int lane = 0;
            float used = 0f;
            foreach (var item in sorted)
            {
                var model = ProductModels.Get(item.DefinitionId);
                if (model == null) continue;
                Vector3 size = model.Bounds.size;
                // Thin side along the crate's depth.
                bool turn = size.x < size.z;
                Quaternion rot = turn ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
                Vector3 fp = turn ? new Vector3(size.z, size.y, size.x) : size;
                float scale = Mathf.Min(1f,
                    laneW * 0.9f / Mathf.Max(fp.x, 0.001f),
                    innerH * 0.85f / Mathf.Max(fp.y, 0.001f),
                    innerD * 0.9f / Mathf.Max(fp.z, 0.001f));
                float depth = fp.z * scale + Gap;
                if (used + depth > innerD)
                {
                    lane++;
                    used = 0f;
                    if (lane >= Lanes) break;
                }

                var copy = Object.Instantiate(model.Template, contents.transform);
                copy.name = "P_" + item.ProductId;
                copy.SetActive(true);
                if (outlines) CrateOutline.Attach(copy);

                float laneX = -innerW / 2f + laneW * (lane + 0.5f);
                float z = innerD / 2f - used - depth / 2f;
                used += depth;
                Vector3 target = new Vector3(laneX, CrateVisual.Wall + fp.y * scale / 2f, z);
                copy.transform.localRotation = rot;
                copy.transform.localScale = Vector3.one * scale;
                copy.transform.localPosition = target - rot * (model.Bounds.center * scale);
            }
        }
    }
}
