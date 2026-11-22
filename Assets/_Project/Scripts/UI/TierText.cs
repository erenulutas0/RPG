using Cryptforge.Localisation;
using Cryptforge.Progression;
using UnityEngine;

namespace Cryptforge.UI
{
    // How a run's difficulty tier is said on the map choice's tier row, the HUD's floor line, the pause's title and the
    // result's title (docs/54 section 21), in the language the game speaks (docs/60): the tier's name, in its colour - the
    // run's choice cards' ladder, Tempered orange, Inferno red, Abyssal gold - beside the words those labels always said,
    // how a locked tier opens and what it pays. Forged says nothing beside a title, so a Forged run's HUD and result read
    // exactly as before the tiers.
    //
    // DifficultyTiers keeps the English the save, telemetry and the simulation's reports write (Name, its Id, UnlockHint,
    // RewardText); the words here are the player's, the same in English (TierTextTests hold them together).
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

        // The tier's name: "Tempered" in English.
        public static string Name(DifficultyTier tier) => tier switch
        {
            DifficultyTier.Tempered => Loc.Text("tier.tempered", "Tempered"),
            DifficultyTier.Inferno => Loc.Text("tier.inferno", "Inferno"),
            DifficultyTier.Abyssal => Loc.Text("tier.abyssal", "Abyssal"),
            _ => Loc.Text("tier.forged", "Forged")
        };

        // How a locked tier is opened ("Complete the Ascent on Forged"); empty for Forged, which is always open.
        public static string UnlockHint(DifficultyTier tier) =>
            DifficultyTiers.TryBelow(tier, out DifficultyTier below)
                ? Loc.Format("tier.unlock_hint", "Complete the Ascent on {0}", Name(below))
                : string.Empty;

        // What the tier pays, as a multiplier: "x1", "x1.25", "x1.5", "x2" - in the language's numbers.
        public static string Reward(DifficultyTier tier) =>
            Loc.Format("tier.reward", "x{0:0.##}", DifficultyTiers.RewardPercent(tier) / 100.0);

        // The tier's name in its colour, as rich text.
        public static string Named(DifficultyTier tier) =>
            "<color=#" + ColorUtility.ToHtmlStringRGB(Colour(tier)) + ">" + Name(tier) + "</color>";

        // The HUD's floor line ("Tempered  ·  Floor 1  /  Room 2/5 ..."); the line itself on Forged.
        public static string BeforeFloor(DifficultyTier tier, string floorLine) =>
            tier == DifficultyTier.Forged ? floorLine : Named(tier) + Separator + floorLine;

        // The result's title ("Ascent complete  ·  Tempered"); the title itself on Forged.
        public static string AfterTitle(string title, DifficultyTier tier) =>
            tier == DifficultyTier.Forged ? title : title + Separator + Named(tier);
    }
}
