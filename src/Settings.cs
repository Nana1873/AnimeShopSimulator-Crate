using MelonLoader;
using UnityEngine;

namespace AnimeShopCrate
{
    internal static class Settings
    {
        internal static MelonPreferences_Entry<bool> DebugLog;
        internal static MelonPreferences_Entry<float> FirstPersonX, FirstPersonY, FirstPersonZ;
        internal static MelonPreferences_Entry<float> ThirdPersonX, ThirdPersonY, ThirdPersonZ;
        internal static MelonPreferences_Entry<float> Scale;

        internal static void Init()
        {
            var cat = MelonPreferences.CreateCategory("AnimeShopCrate");
            DebugLog = cat.CreateEntry("DebugLog", true, null, "Write extra diagnostics to the MelonLoader log.");
            FirstPersonX = cat.CreateEntry("FirstPersonOffsetX", 0f, null, "Crate position in your own view, relative to the held-item slot (metres).");
            FirstPersonY = cat.CreateEntry("FirstPersonOffsetY", 0f);
            FirstPersonZ = cat.CreateEntry("FirstPersonOffsetZ", 0f);
            ThirdPersonX = cat.CreateEntry("ThirdPersonOffsetX", 0f, null, "Crate position on other players, relative to their held-item slot (metres).");
            ThirdPersonY = cat.CreateEntry("ThirdPersonOffsetY", 0f);
            ThirdPersonZ = cat.CreateEntry("ThirdPersonOffsetZ", 0f);
            Scale = cat.CreateEntry("CrateScale", 1f, null, "Size multiplier for the crate model.");
        }

        internal static Vector3 Offset(bool firstPerson) => firstPerson
            ? new Vector3(FirstPersonX.Value, FirstPersonY.Value, FirstPersonZ.Value)
            : new Vector3(ThirdPersonX.Value, ThirdPersonY.Value, ThirdPersonZ.Value);

        internal static void Debug(string msg)
        {
            if (DebugLog.Value) CrateMod.Log.Msg("[debug] " + msg);
        }
    }
}
