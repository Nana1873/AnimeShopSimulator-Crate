using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace AnimeShopCrate
{
    /// <summary>
    /// Set-down preview like the game's box placement: a see-through crate follows the aim point,
    /// left click places, right click cancels, the mouse wheel turns it. Green = fine, red = not here.
    /// </summary>
    internal static class CratePlacement
    {
        private const float MaxDistance = 3.2f, RotateStep = 15f;
        private static readonly Color Ok = new Color(0.45f, 1f, 0.2f, 0.45f);
        private static readonly Color Bad = new Color(1f, 0.25f, 0.2f, 0.45f);

        internal static bool Active { get; private set; }
        private static GameObject _ghost;
        private static Material _ghostMaterial;
        private static float _yawOffset;
        private static bool _valid;
        private static Vector3 _pos;
        private static float _yaw;

        internal static void Begin()
        {
            Active = true;
            _yawOffset = 0f;
            if (_ghost == null)
            {
                _ghost = CrateVisual.Build();
                _ghost.name = "AnimeShopCrate_PlacementPreview";
                foreach (var r in _ghost.GetComponentsInChildren<MeshRenderer>(true)) r.sharedMaterial = GhostMaterial();
            }
            _ghost.SetActive(true);
        }

        internal static void Cancel()
        {
            Active = false;
            if (_ghost != null) _ghost.SetActive(false);
        }

        internal static void Reset()
        {
            Active = false;
            _ghost = null; // destroyed with the scene
        }

        /// <summary>Per frame while active. Returns true when the player confirmed a valid spot.</summary>
        internal static bool Update(out Vector3 pos, out float yaw)
        {
            pos = default; yaw = 0f;
            var player = Game.LocalPlayer;
            var cam = Camera.main;
            if (player == null || cam == null || _ghost == null) { Cancel(); return false; }

            var mouse = Mouse.current;
            float scroll = mouse != null ? mouse.scroll.ReadValue().y : 0f;
            if (Mathf.Abs(scroll) > 0.01f) _yawOffset += scroll > 0 ? RotateStep : -RotateStep;

            Vector3 flat = cam.transform.forward; flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f) flat = player.transform.forward;
            _yaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg + _yawOffset;

            _valid = false;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, MaxDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                _pos = hit.point;
                _valid = hit.normal.y > 0.75f;
            }
            else
            {
                _pos = cam.transform.position + cam.transform.forward * MaxDistance;
            }
            _ghost.transform.position = _pos;
            _ghost.transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            SetColor(_valid ? Ok : Bad);

            if (mouse == null) return false;
            if (mouse.rightButton.wasPressedThisFrame) { Cancel(); return false; }
            if (mouse.leftButton.wasPressedThisFrame && _valid)
            {
                pos = _pos; yaw = _yaw;
                Cancel();
                return true;
            }
            return false;
        }

        private static void SetColor(Color c)
        {
            if (_ghostMaterial == null) return;
            if (_ghostMaterial.HasProperty("_BaseColor")) _ghostMaterial.SetColor("_BaseColor", c);
            if (_ghostMaterial.HasProperty("_Color")) _ghostMaterial.SetColor("_Color", c);
        }

        private static Material GhostMaterial()
        {
            if (_ghostMaterial != null) return _ghostMaterial;
            // Sprites/Default blends by alpha in any pipeline; URP Unlit set to transparent as a fallback.
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                _ghostMaterial = new Material(shader) { name = "AnimeShopCrate_Ghost" };
            }
            else
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
                _ghostMaterial = new Material(shader) { name = "AnimeShopCrate_Ghost" };
                _ghostMaterial.SetFloat("_Surface", 1f);
                _ghostMaterial.SetFloat("_Blend", 0f);
                _ghostMaterial.SetInt("_SrcBlend", 5);  // SrcAlpha
                _ghostMaterial.SetInt("_DstBlend", 10); // OneMinusSrcAlpha
                _ghostMaterial.SetInt("_ZWrite", 0);
                _ghostMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            _ghostMaterial.renderQueue = 3000;
            Settings.Debug("placement preview uses shader " + _ghostMaterial.shader.name);
            Object.DontDestroyOnLoad(_ghostMaterial);
            SetColor(Ok);
            return _ghostMaterial;
        }
    }
}
