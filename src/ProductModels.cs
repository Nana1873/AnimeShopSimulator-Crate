using System.Collections.Generic;
using Il2CppProject.Code.Gameplay.Definitions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AnimeShopCrate
{
    /// <summary>
    /// Look-only copies of product models for display inside crates: the prefab's transform tree with
    /// its meshes and materials, none of its scripts (no pooling, instancing or interaction logic).
    /// One hidden template per product definition, cloned per use.
    /// </summary>
    internal static class ProductModels
    {
        internal sealed class Model
        {
            internal GameObject Template;
            internal Bounds Bounds; // in template space
        }

        private static readonly Dictionary<int, Model> Cache = new();
        private static Transform _holder;

        internal static void Reset() => Cache.Clear();

        internal static Model Get(int definitionId)
        {
            if (Cache.TryGetValue(definitionId, out var m) && m != null && m.Template != null) return m;
            m = Build(CrateRules.Definition(definitionId));
            Cache[definitionId] = m;
            return m;
        }

        private static Model Build(ProductDefinition def)
        {
            var prefab = def?.Prefab;
            if (prefab == null) return null;
            if (_holder == null)
            {
                var h = new GameObject("AnimeShopCrate_ModelTemplates");
                h.transform.position = new Vector3(0, -500, 0);
                Object.DontDestroyOnLoad(h);
                _holder = h.transform;
            }

            var root = new GameObject("Model_" + def.Id);
            root.transform.SetParent(_holder, false);
            CopyTree(prefab.transform, root.transform);

            // Bounds while active, relative to the (unrotated, unscaled) template root.
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            bool any = false;
            var b = new Bounds();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var rb = r.bounds;
                if (!any) { b = rb; any = true; } else b.Encapsulate(rb);
            }
            if (!any)
            {
                Object.Destroy(root);
                return null;
            }
            b.center -= root.transform.position;
            DumpParts(def, prefab.transform, renderers, b, root.transform.position);
            root.SetActive(false);
            return new Model { Template = root, Bounds = b };
        }

        // Debug aid: UserData/AnimeShopCrate/models.txt lists each product model's parts and sizes.
        private static void DumpParts(ProductDefinition def, Transform prefab, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<MeshRenderer> renderers, Bounds total, Vector3 origin)
        {
            if (!Settings.DebugLog.Value) return;
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"== {def.Name} (id {def.Id}, type {def.Type}) prefab '{prefab.name}' total size {total.size} centre {total.center}");
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    var mf = r.GetComponent<MeshFilter>();
                    sb.AppendLine($"   {CrateVisual.Path(r.transform)}  size {r.bounds.size}  centre {r.bounds.center - origin}  mesh '{mf?.sharedMesh?.name}'  mat '{(r.sharedMaterial != null ? r.sharedMaterial.name : "-")}'");
                }
                foreach (var sk in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    sb.AppendLine($"   (skinned, copied in rest pose) {sk.name}  mesh '{sk.sharedMesh?.name}'");
                string dir = System.IO.Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "AnimeShopCrate");
                System.IO.Directory.CreateDirectory(dir);
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "models.txt"), sb.ToString());
            }
            catch (System.Exception e) { Settings.Debug("model dump failed: " + e.Message); }
        }

        private static void CopyTree(Transform src, Transform parent)
        {
            if (!src.gameObject.activeSelf) return;
            string n = src.name;
            // Keep only the most detailed level of detail.
            int lod = n.IndexOf("LOD", System.StringComparison.OrdinalIgnoreCase);
            if (lod >= 0 && lod + 3 < n.Length && n[lod + 3] != '0' && char.IsDigit(n[lod + 3])) return;

            var go = new GameObject(n);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = src.localPosition;
            go.transform.localRotation = src.localRotation;
            go.transform.localScale = src.localScale;

            var mf = src.GetComponent<MeshFilter>();
            var mr = src.GetComponent<MeshRenderer>();
            if (mf != null && mr != null && mr.enabled && mf.sharedMesh != null)
            {
                go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterials = mr.sharedMaterials;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            else
            {
                // Skinned parts (some figures): use the mesh in its rest pose as a static copy.
                var sk = src.GetComponent<SkinnedMeshRenderer>();
                if (sk != null && sk.enabled && sk.sharedMesh != null)
                {
                    go.AddComponent<MeshFilter>().sharedMesh = sk.sharedMesh;
                    var r = go.AddComponent<MeshRenderer>();
                    r.sharedMaterials = sk.sharedMaterials;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
            for (int i = 0; i < src.childCount; i++) CopyTree(src.GetChild(i), go.transform);
        }
    }
}
