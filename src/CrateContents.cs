using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AnimeShopCrate
{
    /// <summary>Fills a crate model with small copies of the products it holds.</summary>
    internal static class CrateContents
    {
        private const int Columns = 3, Rows = 2, Layers = 3;
        private const int MaxShown = Columns * Rows * Layers;

        internal static void Fill(GameObject crate, List<CrateItem> items)
        {
            if (crate == null) return;
            var old = crate.transform.Find("Contents");
            if (old != null) Object.Destroy(old.gameObject);
            if (items == null || items.Count == 0) return;

            var contents = new GameObject("Contents");
            contents.transform.SetParent(crate.transform, false);

            float innerW = CrateVisual.Width - 2 * CrateVisual.Wall;
            float innerD = CrateVisual.Depth - 2 * CrateVisual.Wall;
            float innerH = CrateVisual.Height - CrateVisual.Wall;
            float cellW = innerW / Columns, cellD = innerD / Rows, cellH = innerH / Layers;

            int shown = 0;
            foreach (var item in items)
            {
                if (shown >= MaxShown) break;
                var model = ProductModels.Get(item.DefinitionId);
                if (model == null) continue;

                int layer = shown / (Columns * Rows), inLayer = shown % (Columns * Rows);
                int col = inLayer % Columns, row = inLayer / Columns;
                shown++;

                var copy = Object.Instantiate(model.Template, contents.transform);
                copy.SetActive(true);
                Vector3 size = model.Bounds.size;
                // Tall items (manga, boxes standing up) lie down so they fit the flat cells.
                bool lieDown = size.y > size.x && size.y > size.z;
                Quaternion rot = lieDown ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity;
                Vector3 footprint = lieDown ? new Vector3(size.x, size.z, size.y) : size;
                float scale = Mathf.Min(cellW * 0.92f / Mathf.Max(footprint.x, 0.001f),
                                        cellD * 0.92f / Mathf.Max(footprint.z, 0.001f),
                                        cellH * 0.95f / Mathf.Max(footprint.y, 0.001f));
                scale = Mathf.Min(scale, 1f);

                Vector3 cellCentre = new Vector3(
                    -innerW / 2f + cellW * (col + 0.5f),
                    CrateVisual.Wall + cellH * layer,
                    -innerD / 2f + cellD * (row + 0.5f));
                // Bounds centre over the cell centre, bottom of the (rotated) bounds on the cell floor.
                Vector3 target = new Vector3(cellCentre.x, cellCentre.y + footprint.y * scale / 2f, cellCentre.z);
                copy.transform.localRotation = rot;
                copy.transform.localScale = Vector3.one * scale;
                copy.transform.localPosition = target - rot * (model.Bounds.center * scale);
            }
        }
    }
}
