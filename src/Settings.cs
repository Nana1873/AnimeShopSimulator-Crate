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
        internal static MelonPreferences_Entry<int> CrateCapacity;
        internal static MelonPreferences_Entry<string> DropKey;

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
            DropKey = cat.CreateEntry("DropKey", "Game", null, "Key to set the crate down / pick it up. 'Game' = the game's own drop key (the one for boxes); or a key name like F or T.");
            CrateCapacity = cat.CreateEntry("CrateCapacity", 24, null, "Crate space (pin/keychain/booster 1, manga 2, figure 4, ...). The host's value counts.");
            ThirdPersonTiltX = cat.CreateEntry("ThirdPersonTiltX", 0f, null, "Extra tilt of the crate on other players (degrees).");
        }

        internal static Vector3 Offset(bool firstPerson) => firstPerson
            ? new Vector3(FirstPersonX.Value, FirstPersonY.Value, FirstPersonZ.Value)
            : new Vector3(ThirdPersonX.Value, ThirdPersonY.Value, ThirdPersonZ.Value);

        internal static Vector3 Tilt(bool firstPerson) =>
            new Vector3(firstPerson ? FirstPersonTiltX.Value : ThirdPersonTiltX.Value, 0f, 0f);

        internal static void Debug(string msg)
        {
            if (!DebugLog.Value) return;
            CrateMod.Log.Msg("[debug] " + msg);
            Trace(msg);
        }

        /// <summary>Crash hunting: appends straight to UserData/AnimeShopCrate/trace.txt, flushed per line.</summary>
        internal static void Trace(string msg)
        {
            if (!DebugLog.Value) return;
            try
            {
                string dir = System.IO.Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "AnimeShopCrate");
                System.IO.Directory.CreateDirectory(dir);
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "trace.txt"), System.DateTime.Now.ToString("HH:mm:ss.fff ") + msg + System.Environment.NewLine);
            }
            catch { }
        }
    }
}
