using System;
using System.Collections.Generic;

namespace AnimeShopCrate
{
    /// <summary>Mod texts in German and English, picked by the game's current language.</summary>
    internal static class Text
    {
        private static readonly Dictionary<string, (string de, string en)> Strings = new()
        {
            ["ASC/put"] = ("Einräumen", "Put on shelf"),
            ["ASC/take"] = ("In die Kiste", "Into crate"),
            ["ASC/trash"] = ("Wegwerfen", "Throw away"),
            ["ASC/select"] = ("Produkt wählen", "Choose product"),
            ["ASC/drop"] = ("Kiste abstellen", "Set crate down"),
            ["ASC/place"] = ("Platzieren", "Place"),
            ["ASC/cancel"] = ("Abbrechen", "Cancel"),
            ["ASC/rotate"] = ("Drehen", "Rotate"),
            ["ASC/pickup"] = ("Kiste aufheben", "Pick up crate"),
            ["crate"] = ("Kiste", "Crate"),
            ["product"] = ("Produkt", "product"),
            ["products"] = ("Produkte", "products"),
            ["space"] = ("Platz", "space"),
            ["empty"] = ("leer – Rechtsklick auf ein Regalfach", "empty – right-click a shelf slot"),
        };

        internal static bool German
        {
            get
            {
                try { return (Il2CppI2.Loc.LocalizationManager.CurrentLanguageCode ?? "de").StartsWith("de", StringComparison.OrdinalIgnoreCase); }
                catch { return true; }
            }
        }

        internal static string Get(string key)
        {
            if (!Strings.TryGetValue(key, out var t)) return key;
            return German ? t.de : t.en;
        }
    }
}
