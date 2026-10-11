using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace AnimeShopCrate
{
    /// <summary>
    /// Procedural plastic shopping crate: slotted walls, handle cut-outs on the short sides, a
    /// thicker rim and a perforated bottom. Built once as a mesh with two sub-meshes (body, rim).
    /// Origin = centre of the bottom face, +Z = forward.
    /// </summary>
    internal static class CrateMesh
    {
        internal const float Width = 0.44f, Depth = 0.32f, Height = 0.26f, Wall = 0.012f;
        private const float RimHeight = 0.028f, RimOut = 0.006f;

        private static Mesh _mesh;

        private readonly struct Hole
        {
            internal readonly float U0, V0, U1, V1;
            internal Hole(float u0, float v0, float u1, float v1) { U0 = u0; V0 = v0; U1 = u1; V1 = v1; }
        }

        internal static Mesh Get()
        {
            if (_mesh != null) return _mesh;
            // The modelled crate (assets/crate.obj, made in Blender) is embedded in the DLL;
            // the procedural one below is the fallback.
            try
            {
                using var stream = typeof(CrateMesh).Assembly.GetManifestResourceStream("AnimeShopCrate.crate.obj");
                if (stream != null)
                {
                    var loaded = ObjLoader.Load(stream, new[] { "CrateBody", "CrateRim" }, "AnimeShopCrate_CrateModel");
                    Object.DontDestroyOnLoad(loaded);
                    Settings.Debug($"crate model loaded: {loaded.vertexCount} vertices, bounds {loaded.bounds.size}.");
                    return _mesh = loaded;
                }
            }
            catch (System.Exception e) { CrateMod.Log.Warning("loading the crate model failed, using the simple one: " + e.Message); }

            var verts = new List<Vector3>();
            var body = new List<int>();
            var rim = new List<int>();

            float wallTop = Height - RimHeight;
            float wallH = wallTop - Wall;

            // Long walls (front/back): two rows of five slots.
            var longHoles = new List<Hole>();
            Slots(longHoles, Width, wallH, 5, 2, 0.05f, 0.062f, 0.035f, 0.03f);
            Panel(verts, body, new Vector3(-Width / 2f, Wall, Depth / 2f - Wall), Vector3.right, Vector3.up, Vector3.forward, Width, wallH, Wall, longHoles);
            Panel(verts, body, new Vector3(-Width / 2f, Wall, -Depth / 2f), Vector3.right, Vector3.up, Vector3.forward, Width, wallH, Wall, longHoles);

            // Short walls (left/right): handle cut-out near the top, slots below.
            float shortW = Depth - 2 * Wall;
            var shortHoles = new List<Hole>
            {
                new Hole(shortW / 2f - 0.055f, wallH - 0.058f, shortW / 2f + 0.055f, wallH - 0.022f),
            };
            Slots(shortHoles, shortW, wallH - 0.075f, 3, 1, 0.045f, 0.07f, 0.035f, 0.03f);
            Panel(verts, body, new Vector3(-Width / 2f, Wall, -Depth / 2f + Wall), Vector3.forward, Vector3.up, Vector3.right, shortW, wallH, Wall, shortHoles);
            Panel(verts, body, new Vector3(Width / 2f - Wall, Wall, -Depth / 2f + Wall), Vector3.forward, Vector3.up, Vector3.right, shortW, wallH, Wall, shortHoles);

            // Bottom with a grid of small drain holes.
            var bottomHoles = new List<Hole>();
            Slots(bottomHoles, Width, Depth, 7, 4, 0.022f, 0.022f, 0.06f, 0.05f);
            Panel(verts, body, new Vector3(-Width / 2f, 0f, -Depth / 2f), Vector3.right, Vector3.forward, Vector3.up, Width, Depth, Wall, bottomHoles);

            // Rim: a slightly wider band around the top.
            float rw = Width + 2 * RimOut, rd = Depth + 2 * RimOut, rt = Wall + RimOut;
            Box(verts, rim, new Vector3(-rw / 2f, wallTop, rd / 2f - rt), new Vector3(rw / 2f, Height, rd / 2f));
            Box(verts, rim, new Vector3(-rw / 2f, wallTop, -rd / 2f), new Vector3(rw / 2f, Height, -rd / 2f + rt));
            Box(verts, rim, new Vector3(-rw / 2f, wallTop, -rd / 2f + rt), new Vector3(-rw / 2f + rt, Height, rd / 2f - rt));
            Box(verts, rim, new Vector3(rw / 2f - rt, wallTop, -rd / 2f + rt), new Vector3(rw / 2f, Height, rd / 2f - rt));
            // Four corner posts tie walls and rim together.
            float p = 0.022f;
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
            {
                float cx = x * (Width / 2f - p / 2f), cz = z * (Depth / 2f - p / 2f);
                Box(verts, body, new Vector3(cx - p / 2f, 0f, cz - p / 2f), new Vector3(cx + p / 2f, wallTop, cz + p / 2f));
            }

            var mesh = new Mesh { name = "AnimeShopCrate_CrateMesh" };
            if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = new Il2CppStructArray<Vector3>(verts.ToArray());
            mesh.subMeshCount = 2;
            mesh.SetTriangles(new Il2CppStructArray<int>(body.ToArray()), 0);
            mesh.SetTriangles(new Il2CppStructArray<int>(rim.ToArray()), 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Object.DontDestroyOnLoad(mesh);
            Settings.Debug($"crate mesh built: {verts.Count} vertices.");
            return _mesh = mesh;
        }

        /// <summary>A grid of equal holes centred on a panel, with the given outer margins.</summary>
        private static void Slots(List<Hole> holes, float width, float height, int cols, int rows, float holeW, float holeH, float marginU, float marginV)
        {
            float gapU = (width - 2 * marginU - cols * holeW) / Mathf.Max(1, cols - 1);
            float gapV = rows > 1 ? (height - 2 * marginV - rows * holeH) / (rows - 1) : 0f;
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                float u0 = cols == 1 ? (width - holeW) / 2f : marginU + c * (holeW + gapU);
                float v0 = rows == 1 ? height - marginV - holeH : marginV + r * (holeH + gapV);
                holes.Add(new Hole(u0, v0, u0 + holeW, v0 + holeH));
            }
        }

        /// <summary>A flat panel (u × v, thickness along n) with rectangular holes, as solid cells.</summary>
        private static void Panel(List<Vector3> verts, List<int> tris, Vector3 origin, Vector3 u, Vector3 v, Vector3 n,
                                  float width, float height, float thickness, List<Hole> holes)
        {
            var us = new SortedSet<float> { 0f, width };
            var vs = new SortedSet<float> { 0f, height };
            foreach (var h in holes) { us.Add(h.U0); us.Add(h.U1); vs.Add(h.V0); vs.Add(h.V1); }
            var ua = new List<float>(us);
            var va = new List<float>(vs);
            for (int i = 0; i + 1 < ua.Count; i++)
            for (int j = 0; j + 1 < va.Count; j++)
            {
                float cu = (ua[i] + ua[i + 1]) / 2f, cv = (va[j] + va[j + 1]) / 2f;
                bool hole = false;
                foreach (var h in holes)
                    if (cu > h.U0 && cu < h.U1 && cv > h.V0 && cv < h.V1) { hole = true; break; }
                if (hole) continue;
                Vector3 a = origin + u * ua[i] + v * va[j];
                Vector3 b = origin + u * ua[i + 1] + v * va[j + 1] + n * thickness;
                Box(verts, tris, Vector3.Min(a, b), Vector3.Max(a, b));
            }
        }

        /// <summary>Axis-aligned cuboid with its own vertices per face (flat shading).</summary>
        private static void Box(List<Vector3> verts, List<int> tris, Vector3 min, Vector3 max)
        {
            Vector3 c000 = new(min.x, min.y, min.z), c100 = new(max.x, min.y, min.z), c010 = new(min.x, max.y, min.z), c110 = new(max.x, max.y, min.z);
            Vector3 c001 = new(min.x, min.y, max.z), c101 = new(max.x, min.y, max.z), c011 = new(min.x, max.y, max.z), c111 = new(max.x, max.y, max.z);
            Quad(verts, tris, c000, c010, c110, c100); // -Z
            Quad(verts, tris, c101, c111, c011, c001); // +Z
            Quad(verts, tris, c001, c011, c010, c000); // -X
            Quad(verts, tris, c100, c110, c111, c101); // +X
            Quad(verts, tris, c010, c011, c111, c110); // +Y
            Quad(verts, tris, c001, c000, c100, c101); // -Y
        }

        private static void Quad(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }
    }
}
