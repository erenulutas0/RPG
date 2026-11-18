using System;

namespace Cryptforge.Localisation
{
    // The languages the game ships in (docs/59 decision 1): English and Turkish. The player's choice is stored by its code
    // (LanguageCodes), never by this number, apart from the profile save (LocalisationRuntime).
    public enum Language
    {
        English = 0,
        Turkish = 1
    }

    public static class LanguageCodes
    {
        public const string English = "en";
        public const string Turkish = "tr";

        public static string Code(Language language) => language == Language.Turkish ? Turkish : English;

        // "en" or "tr", compared ordinally: a Turkish phone's casing rules never decide which language a stored code names.
        public static bool TryParse(string code, out Language language)
        {
            if (string.Equals(code, Turkish, StringComparison.Ordinal))
            {
                language = Language.Turkish;
                return true;
            }
            language = Language.English;
            return string.Equals(code, English, StringComparison.Ordinal);
        }
    }
}
