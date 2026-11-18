using Cryptforge.Progression;
using UnityEngine;

namespace Cryptforge.UI
{
    // How a run's difficulty tier is said on the HUD's floor line and the result's title (docs/54 section 21): the tier's
    // name in its colour - the run's choice cards' ladder, Tempered orange, Inferno red, Abyssal gold - beside the words
    // those labels always said. Forged says nothing, so a Forged run's HUD and result read exactly as before the tiers.
    public static class TierText
    {
        public const string Separator = "  ·  ";

        // The tier's colour on the dark HUD and result plates.
        public static Color Colour(DifficultyTier tier) => tier switch
        {
            DifficultyTier.Tempered => new Color32(232, 152, 72, 255),
            DifficultyTier.Inferno => new Color32(236, 92, 72, 255),
            DifficultyTier.Abyssal => new Color32(238, 191, 108, 255),
            _ => new Color32(190, 194, 208, 255)
        };

        // The tier's name in its colour, as rich text.
        public static string Named(DifficultyTier tier) =>
            "<color=#" + ColorUtility.ToHtmlStringRGB(Colour(tier)) + ">" + DifficultyTiers.Name(tier) + "</color>";

        // The HUD's floor line ("Tempered  ·  Floor 1  /  Room 2/5 ..."); the line itself on Forged.
        public static string BeforeFloor(DifficultyTier tier, string floorLine) =>
            tier == DifficultyTier.Forged ? floorLine : Named(tier) + Separator + floorLine;

        // The result's title ("Ascent complete  ·  Tempered"); the title itself on Forged.
        public static string AfterTitle(string title, DifficultyTier tier) =>
            tier == DifficultyTier.Forged ? title : title + Separator + Named(tier);
    }
}
