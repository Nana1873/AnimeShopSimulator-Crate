using System;
using System.Collections.Generic;
using Il2CppProject.Code.Gameplay.Player.Controllers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AnimeShopCrate
{
    /// <summary>
    /// The crate model shown in a player's hands. A placeholder plastic crate built from cubes
    /// until a real model is added. One instance per player, parented to the game's held-item slot
    /// (first-person slot for the local player, third-person slot for others).
    /// </summary>
    internal static class CrateVisual
    {
        // Outer size in metres.
        internal const float Width = 0.42f, Depth = 0.30f, Height = 0.22f, Wall = 0.015f;
        private static readonly Color CrateColor = new Color(0.16f, 0.45f, 0.85f);
        // Built-in pose relative to the held-item slot; the settings offsets are added on top.
        // First person: low and a bit forward, tilted towards the camera so you can look inside.
        private static readonly Vector3 FirstPersonBase = new Vector3(0f, -0.10f, 0.24f);
        private static readonly Vector3 FirstPersonTilt = new Vector3(-38f, 0f, 0f);
        private const float FirstPersonScale = 0.68f;
        // Third person: carried in front of the body at waist height, relative to the character root.
        // The game's third-person product socket sits on the left hand bone and swings with the arm.
        private static readonly Vector3 ThirdPersonBase = new Vector3(0f, 1.0f, 0.27f);

        private sealed class Held
        {
            internal GameObject Crate;
            internal ulong Owner;
        }

        private static readonly Dictionary<IntPtr, Held> Crates = new();

        internal static void Reset() => Crates.Clear();

        /// <summary>Rebuilds the contents of every held crate of this player (called when the crate changes).</summary>
        internal static void Refresh(ulong owner)
        {
            foreach (var h in Crates.Values)
                if (h.Crate != null && h.Owner == owner)
                {
                    CrateContents.Fill(h.Crate, CrateStore.Get(owner));
                    SetLayer(h.Crate.transform, h.Crate.layer);
                }
        }
        private static Material _material;

        internal static void Set(PlayerAnimationController anim, bool visible)
        {
            if (anim == null) return;
            IntPtr key = anim.Pointer;
            Crates.TryGetValue(key, out var held);
            var crate = held?.Crate;
            if (crate == null && !visible) return;

            if (crate == null)
            {
                var player = anim.GetComponentInParent<PlayerCharacterController>();
                if (player == null) return;
                bool firstPerson = player.IsOwner;
                Transform anchor = Anchor(player, firstPerson);
                if (anchor == null)
                {
                    CrateMod.Log.Warning("no held-item slot found on the player; the crate stays invisible.");
                    return;
                }
                crate = Build();
                int layer = anchor.gameObject.layer;
                if (!firstPerson) anchor = BodyRoot(anchor) ?? player.transform;
                crate.transform.SetParent(anchor, false);
                crate.transform.localPosition = (firstPerson ? FirstPersonBase : ThirdPersonBase) + Settings.Offset(firstPerson);
                crate.transform.localRotation = Quaternion.Euler((firstPerson ? FirstPersonTilt : Vector3.zero) + Settings.Tilt(firstPerson));
                crate.transform.localScale = Vector3.one * (firstPerson ? FirstPersonScale : 1f) * Settings.Scale.Value;
                CrateContents.Fill(crate, CrateStore.Get(player.SteamId));
                SetLayer(crate.transform, layer);
                Crates[key] = new Held { Crate = crate, Owner = player.SteamId };
                Settings.Debug($"crate model created under '{Path(anchor)}' (layer {layer}, {(firstPerson ? "first" : "third")} person, root scale {player.transform.lossyScale}).");
            }
            crate.SetActive(visible);
        }

        private static Transform Anchor(PlayerCharacterController player, bool firstPerson)
        {
            var products = player._productsController;
            if (products == null) return null;
            if (firstPerson) return products._fpsPickupSlot?.transform;
            return (products._tpsPickupSlot ?? products._tpsBigPickupSlot)?.transform;
        }

        // The character root does not turn with the player; the body model below it does.
        private static Transform BodyRoot(Transform socket)
        {
            for (var t = socket; t != null; t = t.parent)
                if (t.name.StartsWith("BSMC_CharacterBase", StringComparison.Ordinal)) return t;
            return null;
        }

        internal static GameObject Build()
        {
            var root = new GameObject("AnimeShopCrate_Crate");
            float half = Wall / 2f;
            // Bottom, then walls; the crate's origin is the centre of its bottom face.
            Part(root, new Vector3(0, half, 0), new Vector3(Width, Wall, Depth));
            Part(root, new Vector3(0, Height / 2f, Depth / 2f - half), new Vector3(Width, Height, Wall));
            Part(root, new Vector3(0, Height / 2f, -Depth / 2f + half), new Vector3(Width, Height, Wall));
            Part(root, new Vector3(Width / 2f - half, Height / 2f, 0), new Vector3(Wall, Height, Depth - 2 * Wall));
            Part(root, new Vector3(-Width / 2f + half, Height / 2f, 0), new Vector3(Wall, Height, Depth - 2 * Wall));
            return root;
        }

        private static void Part(GameObject root, Vector3 pos, Vector3 size)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var col = cube.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            cube.transform.SetParent(root.transform, false);
            cube.transform.localPosition = pos;
            cube.transform.localScale = size;
            var r = cube.GetComponent<MeshRenderer>();
            r.sharedMaterial = Material();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static Material Material()
        {
            if (_material != null) return _material;
            foreach (string name in new[] { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Simple Lit", "Universal Render Pipeline/Unlit", "Standard", "Unlit/Color" })
            {
                var shader = Shader.Find(name);
                if (shader == null) continue;
                _material = new Material(shader) { name = "AnimeShopCrate_Plastic" };
                if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", CrateColor);
                if (_material.HasProperty("_Color")) _material.SetColor("_Color", CrateColor);
                if (_material.HasProperty("_Smoothness")) _material.SetFloat("_Smoothness", 0.35f);
                Settings.Debug("crate material uses shader " + name);
                return _material;
            }
            CrateMod.Log.Warning("no usable shader found for the crate; it may render pink.");
            return _material = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        internal static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayer(t.GetChild(i), layer);
        }

        internal static string Path(Transform t)
        {
            string p = t.name;
            for (var x = t.parent; x != null; x = x.parent) p = x.name + "/" + p;
            return p;
        }
    }
}
