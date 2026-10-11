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
        internal static MelonPreferences_Entry<float> FirstPersonTiltX, ThirdPersonTiltX;

        internal static void Init()
        {
            var cat = MelonPreferences.CreateCategory("AnimeShopCrate");
            DebugLog = cat.CreateEntry("DebugLog", true, null, "Write extra diagnostics to the MelonLoader log.");
            FirstPersonX = cat.CreateEntry("FirstPersonOffsetX", 0f, null, "Extra shift of the crate in your own view (metres; X right, Y up, Z forward).");
            FirstPersonY = cat.CreateEntry("FirstPersonOffsetY", 0f);
            FirstPersonZ = cat.CreateEntry("FirstPersonOffsetZ", 0f);
            ThirdPersonX = cat.CreateEntry("ThirdPersonOffsetX", 0f, null, "Extra shift of the crate on other players (metres).");
            ThirdPersonY = cat.CreateEntry("ThirdPersonOffsetY", 0f);
            ThirdPersonZ = cat.CreateEntry("ThirdPersonOffsetZ", 0f);
            Scale = cat.CreateEntry("CrateScale", 1f, null, "Size multiplier for the crate model.");
            FirstPersonTiltX = cat.CreateEntry("FirstPersonTiltX", 0f, null, "Extra tilt of the crate in your own view (degrees, negative = towards you).");
            ThirdPersonTiltX = cat.CreateEntry("ThirdPersonTiltX", 0f, null, "Extra tilt of the crate on other players (degrees).");
        }

        internal static Vector3 Offset(bool firstPerson) => firstPerson
            ? new Vector3(FirstPersonX.Value, FirstPersonY.Value, FirstPersonZ.Value)
            : new Vector3(ThirdPersonX.Value, ThirdPersonY.Value, ThirdPersonZ.Value);

        internal static Vector3 Tilt(bool firstPerson) =>
            new Vector3(firstPerson ? FirstPersonTiltX.Value : ThirdPersonTiltX.Value, 0f, 0f);

        internal static void Debug(string msg)
        {
            if (DebugLog.Value) CrateMod.Log.Msg("[debug] " + msg);
        }
    }
}
