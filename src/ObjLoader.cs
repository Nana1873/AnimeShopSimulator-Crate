using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace AnimeShopCrate
{
    /// <summary>
    /// Minimal Wavefront OBJ reader for the crate model embedded in the DLL (exported from Blender
    /// with Y up, triangulated, normals). One sub-mesh per material in the given order.
    /// OBJ is right-handed, Unity left-handed: X is mirrored and the winding reversed.
    /// </summary>
    internal static class ObjLoader
    {
        internal static Mesh Load(Stream stream, string[] materials, string name)
        {
            var inv = CultureInfo.InvariantCulture;
            var pos = new List<Vector3>();
            var nrm = new List<Vector3>();
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var tris = new List<int>[materials.Length];
            for (int i = 0; i < tris.Length; i++) tris[i] = new List<int>();
            int sub = 0;

            using var reader = new StreamReader(stream);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length < 2) continue;
                string[] p = line.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                switch (p[0])
                {
                    case "v":
                        pos.Add(new Vector3(-float.Parse(p[1], inv), float.Parse(p[2], inv), float.Parse(p[3], inv)));
                        break;
                    case "vn":
                        nrm.Add(new Vector3(-float.Parse(p[1], inv), float.Parse(p[2], inv), float.Parse(p[3], inv)));
                        break;
                    case "usemtl":
                        sub = System.Array.IndexOf(materials, p.Length > 1 ? p[1] : "");
                        if (sub < 0) sub = 0;
                        break;
                    case "f":
                        // Fan-triangulate (the export is already triangles); reverse winding for Unity.
                        int first = verts.Count;
                        for (int k = 1; k < p.Length; k++)
                        {
                            string[] idx = p[k].Split('/');
                            verts.Add(pos[int.Parse(idx[0], inv) - 1]);
                            norms.Add(idx.Length > 2 && idx[2].Length > 0 ? nrm[int.Parse(idx[2], inv) - 1] : Vector3.up);
                        }
                        for (int k = 1; k + 1 < p.Length - 1; k++)
                        {
                            tris[sub].Add(first);
                            tris[sub].Add(first + k + 1);
                            tris[sub].Add(first + k);
                        }
                        break;
                }
            }

            var mesh = new Mesh { name = name };
            if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = new Il2CppStructArray<Vector3>(verts.ToArray());
            mesh.normals = new Il2CppStructArray<Vector3>(norms.ToArray());
            mesh.subMeshCount = materials.Length;
            for (int i = 0; i < materials.Length; i++) mesh.SetTriangles(new Il2CppStructArray<int>(tris[i].ToArray()), i);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
