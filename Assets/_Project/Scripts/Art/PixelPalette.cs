namespace Cryptforge.Art
{
    // The astral foundry palette, sampled from ArtDirection/2026-09-14/mockups/cosmic-v1.png and rounded to a few tones
    // per material. Every generated placeholder draws from these so the whole screen reads as one style.
    public static class PixelPalette
    {
        // The void and its nebulae.
        public static readonly Rgba VoidDeep = Rgba.FromHex("#070A21");
        public static readonly Rgba VoidMid = Rgba.FromHex("#0E0D23");
        public static readonly Rgba VoidLight = Rgba.FromHex("#1A1440");
        public static readonly Rgba NebulaViolet = Rgba.FromHex("#2D1A70");
        public static readonly Rgba NebulaDust = Rgba.FromHex("#312D43");
        public static readonly Rgba NebulaGlow = Rgba.FromHex("#4A2A9A");
        public static readonly Rgba StarWhite = Rgba.FromHex("#F4F1FF");
        public static readonly Rgba StarLilac = Rgba.FromHex("#B9AEF2");
        public static readonly Rgba StarDim = Rgba.FromHex("#5E5A8F");

        // Dark stone tiles, their grout and the brass inlay.
        public static readonly Rgba StoneLight = Rgba.FromHex("#3A3852");
        public static readonly Rgba Stone = Rgba.FromHex("#2D2B41");
        public static readonly Rgba StoneDark = Rgba.FromHex("#1F1D31");
        public static readonly Rgba Grout = Rgba.FromHex("#110D1F");
        public static readonly Rgba BrassLight = Rgba.FromHex("#F8BA50");
        public static readonly Rgba Brass = Rgba.FromHex("#AC7243");
        public static readonly Rgba BrassDark = Rgba.FromHex("#6B4224");

        // The platform's faces, keel and the distant rock.
        // Dark warm stone: the mockup's faces read as shadowed indigo-brown with only the lava seams bright.
        public static readonly Rgba FaceLight = Rgba.FromHex("#6E4A3A");
        public static readonly Rgba Face = Rgba.FromHex("#46323A");
        public static readonly Rgba FaceDark = Rgba.FromHex("#2A1E2C");
        public static readonly Rgba RockLight = Rgba.FromHex("#3B3358");
        public static readonly Rgba Rock = Rgba.FromHex("#252041");
        public static readonly Rgba RockDark = Rgba.FromHex("#191344");
        public static readonly Rgba Chain = Rgba.FromHex("#2A2A45");
        public static readonly Rgba ChainLight = Rgba.FromHex("#4C4C70");

        // Forge fire: lanterns, lava seams, mites and hit sparks.
        public static readonly Rgba EmberHot = Rgba.FromHex("#FDFE93");
        public static readonly Rgba EmberLight = Rgba.FromHex("#FE7B1A");
        public static readonly Rgba Ember = Rgba.FromHex("#E55640");
        public static readonly Rgba EmberDark = Rgba.FromHex("#A54C2B");
        public static readonly Rgba Lava = Rgba.FromHex("#881007");

        // The hero's blue steel and the quicksilver boss.
        public static readonly Rgba SteelLight = Rgba.FromHex("#9BEBFE");
        public static readonly Rgba Steel = Rgba.FromHex("#718CCF");
        public static readonly Rgba SteelDark = Rgba.FromHex("#354A9C");
        public static readonly Rgba SteelShadow = Rgba.FromHex("#232841");
        public static readonly Rgba SilverLight = Rgba.FromHex("#C9C9E6");
        public static readonly Rgba Silver = Rgba.FromHex("#6F6EA6");
        public static readonly Rgba SilverDark = Rgba.FromHex("#444464");
        public static readonly Rgba Visor = Rgba.FromHex("#A26CFF");

        // Molten slag enemies.
        public static readonly Rgba Slag = Rgba.FromHex("#3D1F26");
        public static readonly Rgba SlagDark = Rgba.FromHex("#1B0C12");
        public static readonly Rgba SlagCrack = Rgba.FromHex("#FE7B1A");

        // Spells, coins, bars and outlines.
        public static readonly Rgba Spell = Rgba.FromHex("#4973CE");
        public static readonly Rgba SpellLight = Rgba.FromHex("#9BEBFE");
        public static readonly Rgba Coin = Rgba.FromHex("#F19101");
        public static readonly Rgba CoinLight = Rgba.FromHex("#FDBF4C");
        public static readonly Rgba HealthRed = Rgba.FromHex("#E53F32");
        public static readonly Rgba BarBack = Rgba.FromHex("#151820");
        public static readonly Rgba Outline = Rgba.FromHex("#08081D");
    }
}
