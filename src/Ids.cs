using System.Runtime.CompilerServices;

namespace AnimeShopCrate
{
    /// <summary>
    /// Converts the game's product ids without calling into IL2CPP. Il2CppSystem.Guid is generated as
    /// a struct, and calling its instance methods (ToString, Equals, ...) hands the runtime an unboxed
    /// pointer, which crashes the game natively. Both types share the same 16-byte layout.
    /// </summary>
    internal static class Ids
    {
        internal static string ToText(Il2CppSystem.Guid id) => Unsafe.As<Il2CppSystem.Guid, System.Guid>(ref id).ToString("N");

        internal static Il2CppSystem.Guid FromText(string text)
        {
            var g = System.Guid.TryParse(text, out var parsed) ? parsed : System.Guid.Empty;
            return Unsafe.As<System.Guid, Il2CppSystem.Guid>(ref g);
        }

        internal static bool Same(string a, string b) => System.Guid.TryParse(a, out var x) && System.Guid.TryParse(b, out var y) && x == y;
    }
}
