using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AnimeShopCrate
{
    /// <summary>Fills a crate model with small copies of the products it holds.</summary>
    internal static class CrateContents
    {
        private sealed class Stack
        {
            internal ProductModels.Model Model;
            internal bool Turn;
            internal Vector3 Footprint;
            internal float Scale;
            internal readonly List<CrateItem> Items = new();
            internal float Depth(float scale) => Items.Count * (Footprint.z * scale + 0.004f);
        }

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
            if (old != null)
            {
                // Destroy happens at the end of the frame: rename and hide it now so lookups
                // (e.g. the selection outline) find the new contents, not the old ones.
                old.name = "Contents_old";
                old.gameObject.SetActive(false);
                Object.Destroy(old.gameObject);
            }
            if (items == null || items.Count == 0) return;

            var contents = new GameObject("Contents");
            contents.transform.SetParent(crate.transform, false);

            // Products stand upright like books in a box: one stack per product, front to back, the
            // stacks side by side across the crate. Each stack is as wide as its product; if all
            // stacks do not fit across, everything is scaled down together. The front item of each
            // stack shows its cover towards you.
            const float Gap = 0.004f, StackGap = 0.01f;
            float innerW = CrateVisual.Width - 2 * CrateVisual.Wall;
            float innerD = CrateVisual.Depth - 2 * CrateVisual.Wall;
            float innerH = CrateVisual.Height - CrateVisual.Wall;

            var order = new List<int>();
            var byDef = new Dictionary<int, List<CrateItem>>();
            foreach (var it in items)
            {
                if (!byDef.TryGetValue(it.DefinitionId, out var l)) { byDef[it.DefinitionId] = l = new List<CrateItem>(); order.Add(it.DefinitionId); }
                l.Add(it);
            }

            var stacks = new List<Stack>();
            foreach (int defId in order)
            {
                var model = ProductModels.Get(defId);
                if (model == null) continue;
                Vector3 size = model.Bounds.size;
                bool turn = size.x < size.z; // thin side along the crate's depth
                Vector3 fp = turn ? new Vector3(size.z, size.y, size.x) : size;
                // Figures are small next to manga at true size; show them a bit larger in the crate.
                var defType = CrateRules.Definition(defId)?.Type;
                float maxScale = defType == Il2CppProject.Code.Gameplay.Definitions.EProductType.Figure ? 1.3f : 1f;
                float scale = Mathf.Min(maxScale,
                    innerW * 0.9f / Mathf.Max(fp.x, 0.001f),
                    innerH * 0.85f / Mathf.Max(fp.y, 0.001f),
                    innerD * 0.95f / Mathf.Max(fp.z, 0.001f));
                // Split a product over several stacks when it does not fit front to back.
                Stack current = null;
                foreach (var it in byDef[defId])
                {
                    if (current == null || current.Depth(scale) + fp.z * scale + Gap > innerD)
                    {
                        current = new Stack { Model = model, Turn = turn, Footprint = fp, Scale = scale };
                        stacks.Add(current);
                    }
                    current.Items.Add(it);
                }
            }
            if (stacks.Count == 0) return;

            float total = -StackGap;
            foreach (var st in stacks) total += st.Footprint.x * st.Scale + StackGap;
            float shrink = total > innerW ? innerW / total : 1f;

            float x = -Mathf.Min(total * shrink, innerW) / 2f;
            foreach (var st in stacks)
            {
                float scale = st.Scale * shrink;
                float w = st.Footprint.x * scale, d = st.Footprint.z * scale;
                Quaternion rot = st.Turn ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
                float z = innerD / 2f; // start at the far wall
                foreach (var item in st.Items)
                {
                    var copy = Object.Instantiate(st.Model.Template, contents.transform);
                    copy.name = "P_" + item.ProductId;
                    copy.SetActive(true);
                    if (outlines) CrateOutline.Attach(copy);
                    Vector3 target = new Vector3(x + w / 2f, CrateVisual.Wall + st.Footprint.y * scale / 2f, z - d / 2f);
                    copy.transform.localRotation = rot;
                    copy.transform.localScale = Vector3.one * scale;
                    copy.transform.localPosition = target - rot * (st.Model.Bounds.center * scale);
                    z -= d + Gap * shrink;
                }
                x += w + StackGap * shrink;
            }
        }
    }
}
