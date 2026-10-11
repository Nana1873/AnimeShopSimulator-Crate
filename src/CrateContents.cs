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
            // stacks side by side across the crate. With many different products the stacks go into
            // several rows behind each other instead of all shrinking into one row; the layout with
            // the largest products wins. The front item of each stack shows its cover towards you.
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

            List<List<Stack>> bestRows = null;
            float bestShrink = 0f, bestBand = innerD;
            for (int rowCount = 1; rowCount <= MaxRows; rowCount++)
            {
                float band = (innerD - (rowCount - 1) * RowGap) / rowCount;
                var stacks = BuildStacks(order, byDef, innerW, innerH, band);
                if (stacks.Count == 0) return;
                // Shrink step by step until the stacks fit into rowCount rows.
                float shrink = 1f;
                List<List<Stack>> rows;
                while ((rows = Pack(stacks, innerW, shrink)).Count > rowCount && shrink > MinShrink) shrink -= 0.02f;
                if (rows.Count > rowCount) continue;
                if (shrink > bestShrink + 0.001f) { bestShrink = shrink; bestRows = rows; bestBand = band; }
            }
            if (bestRows == null)
            {
                // Fallback: everything in one row, shrunk to fit.
                var stacks = BuildStacks(order, byDef, innerW, innerH, innerD);
                if (stacks.Count == 0) return;
                float total = -StackGap;
                foreach (var st in stacks) total += st.Footprint.x * st.Scale + StackGap;
                bestShrink = total > innerW ? innerW / total : 1f;
                bestRows = new List<List<Stack>> { stacks };
                bestBand = innerD;
            }

            float rowZ = innerD / 2f; // first row against the far wall
            foreach (var row in bestRows)
            {
                float rowW = -StackGap * bestShrink;
                foreach (var st in row) rowW += (st.Footprint.x * st.Scale + StackGap) * bestShrink;
                float x = -Mathf.Min(rowW, innerW) / 2f;
                foreach (var st in row)
                {
                    float scale = st.Scale * bestShrink;
                    float w = st.Footprint.x * scale, d = st.Footprint.z * scale;
                    Quaternion rot = st.Turn ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
                    float z = rowZ;
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
                        z -= d + Gap * bestShrink;
                    }
                    x += w + StackGap * bestShrink;
                }
                rowZ -= bestBand + RowGap;
            }
        }

        private const float Gap = 0.004f, StackGap = 0.01f, RowGap = 0.012f, MinShrink = 0.25f;
        private const int MaxRows = 4;

        // One stack per product (split when it is deeper than a row), each at the product's own scale.
        private static List<Stack> BuildStacks(List<int> order, Dictionary<int, List<CrateItem>> byDef,
            float innerW, float innerH, float band)
        {
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
                    band * 0.95f / Mathf.Max(fp.z, 0.001f));
                Stack current = null;
                foreach (var it in byDef[defId])
                {
                    if (current == null || current.Depth(scale) + fp.z * scale + Gap > band)
                    {
                        current = new Stack { Model = model, Turn = turn, Footprint = fp, Scale = scale };
                        stacks.Add(current);
                    }
                    current.Items.Add(it);
                }
            }
            return stacks;
        }

        // Fills rows left to right in order; a stack that does not fit starts the next row.
        private static List<List<Stack>> Pack(List<Stack> stacks, float innerW, float shrink)
        {
            var rows = new List<List<Stack>>();
            List<Stack> row = null;
            float used = 0f;
            foreach (var st in stacks)
            {
                float w = st.Footprint.x * st.Scale * shrink;
                if (row == null || used + StackGap * shrink + w > innerW)
                {
                    row = new List<Stack>();
                    rows.Add(row);
                    used = w;
                }
                else used += StackGap * shrink + w;
                row.Add(st);
            }
            return rows;
        }
    }
}
