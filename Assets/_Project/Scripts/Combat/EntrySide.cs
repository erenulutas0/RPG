using System;
using Cryptforge.Art;

namespace Cryptforge.Combat
{
    // The four corners of the arena a pack can enter from: far is straight ahead of the hero (up the screen), right and
    // left are the side corners, near is below the hero.
    public enum EntrySide
    {
        Far = 0,
        Right = 1,
        Near = 2,
        Left = 3
    }

    // Where one enemy of a wave enters: the corner it comes from, its formation offset across that corner's centre line
    // (which sets its approach angle), and the floor point it starts on.
    public readonly struct EntryPlacement
    {
        public readonly EntrySide Side;
        public readonly float Lateral;
        public readonly float X;
        public readonly float Y;

        public EntryPlacement(EntrySide side, float lateral, float x, float y)
        {
            Side = side;
            Lateral = lateral;
            X = x;
            Y = y;
        }
    }

    // How a wave spreads its enemies over the corners: slot by slot round the arena, starting one corner further for each
    // wave, so a pack of four comes from every side at once and the packs of a floor circle the hero. Pure, so the scene
    // and the Descent simulation place every enemy identically.
    public static class EntrySides
    {
        public const int Count = 4;
        // Corners as a mask, a bit per side (1 << (int)side); every corner open.
        public const int AllSides = (1 << Count) - 1;
        // The nearest an enemy may start to the hero, in floor units: beyond every enemy's and weapon's reach, so a pack
        // never enters already striking or struck.
        public const float MinimumEntryDistance = 3f;
        // How much of the way to the rim a spot pulled in short of it gives up. Converging onto the rim exactly would leave
        // the spot on the line, where whether it counts as on the platform is decided by the last bit of a float: the
        // Editor's runtime and the pure test runner then disagree by one bit about the same spot. A relief this small moves
        // an entry by a thousandth of a unit at most, which no one can see, and puts it hundreds of bits clear of the line,
        // so every runtime agrees the pack starts on the platform.
        private const float RimRelief = 1e-4f;

        // The unit vector on the floor from the hero toward the side's corner.
        public static void Outward(EntrySide side, out float x, out float y)
        {
            switch (side)
            {
                case EntrySide.Far:
                    x = 0f;
                    y = 1f;
                    break;
                case EntrySide.Right:
                    x = 1f;
                    y = 0f;
                    break;
                case EntrySide.Near:
                    x = 0f;
                    y = -1f;
                    break;
                case EntrySide.Left:
                    x = -1f;
                    y = 0f;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side));
            }
        }

        // The unit vector a formation's lateral offset runs along: the outward vector turned a quarter turn clockwise, so
        // the far side's lateral is +x, as the formations were first laid out.
        public static void Lateral(EntrySide side, out float x, out float y)
        {
            Outward(side, out float outX, out float outY);
            x = outY;
            y = -outX;
        }

        public static EntrySide SideOf(int waveOrdinal, int slot)
        {
            if (waveOrdinal < 0)
                throw new ArgumentOutOfRangeException(nameof(waveOrdinal));
            if (slot < 0)
                throw new ArgumentOutOfRangeException(nameof(slot));
            return (EntrySide)((waveOrdinal + slot) % Count);
        }

        // The side of a wave's slot, with the slot's index among the enemies that share the side and how many they are,
        // so PackLayout can form them up on their own corner.
        public static void Formation(int waveOrdinal, int slot, int count, out EntrySide side, out int indexOnSide, out int countOnSide) =>
            Formation(waveOrdinal, slot, count, AllSides, out side, out indexOnSide, out countOnSide);

        // Formation over the open corners only: the open corners take the slots in turn, in the order the arena goes round,
        // so with every corner open it is the formation above.
        public static void Formation(int waveOrdinal, int slot, int count, int openSides, out EntrySide side, out int indexOnSide,
            out int countOnSide)
        {
            if (waveOrdinal < 0)
                throw new ArgumentOutOfRangeException(nameof(waveOrdinal));
            if (count < 1 || slot < 0 || slot >= count)
                throw new ArgumentOutOfRangeException(nameof(slot));
            if (openSides <= 0 || openSides > AllSides)
                throw new ArgumentOutOfRangeException(nameof(openSides));

            int open = 0;
            for (int bits = openSides; bits != 0; bits &= bits - 1)
                open++;
            int turn = (waveOrdinal + slot) % open;
            side = EntrySide.Far;
            for (int candidate = 0; candidate < Count; candidate++)
            {
                if ((openSides & (1 << candidate)) == 0)
                    continue;
                if (turn-- == 0)
                {
                    side = (EntrySide)candidate;
                    break;
                }
            }
            indexOnSide = slot / open;
            countOnSide = (count - slot % open + open - 1) / open;
        }

        // Lays a wave of count enemies out round the hero standing at (heroX, heroY), into placements[0..count): each
        // corner forms up its enemies with PackLayout, entryDepth from the hero plus their formation depth. An enemy whose
        // spot lies beyond the platform, or inside the margin the hero keeps from its rim, starts short of the rim on the
        // line from the hero instead, as long as that leaves it MinimumEntryDistance away; a corner with an enemy that
        // cannot closes for this wave and the open corners take its enemies. So a hero away from the centre is still
        // approached from every side the platform reaches, and a hero at a corner only from where the platform is; with
        // the hero near the centre nothing moves. Should every corner close, on a platform too small for the wave, each
        // enemy that starts outside is drawn in toward the hero until it stands inside.
        public static void Place(int waveOrdinal, int count, float heroX, float heroY, ArenaGeometry platform, float entryDepth,
            float formationSpacing, EntryPlacement[] placements) =>
            Place(waveOrdinal, count, heroX, heroY, platform, entryDepth, formationSpacing, 0f, placements);

        // Place, with every enemy also starting at least minimumSeparation floor units from the enemies before it in the wave,
        // so bodies never spawn overlapping (callers pass the body spacing PackMotion keeps). A spot that ends up closer than
        // that to an earlier enemy's spot, after any pull-in short of the rim, closes its own corner exactly as a spot over
        // the void does, and the open corners take the wave again; enemies on a corner that is closing anyway are ignored,
        // since they move. Formations keep their slots 1.1 spacing units apart, so while formationSpacing * 1.1 is at least
        // the separation (1 and 0.9 in the scene) and the hero stands near the centre, where nothing is pulled in, no corner
        // ever closes for it and the wave enters exactly as without it; a separation of 0 is the plain Place. What the
        // too-small-platform fallback still cannot guarantee: once every corner has closed, the enemies drawn in toward the
        // hero may stand nearer than MinimumEntryDistance to it and nearer than minimumSeparation to each other.
        public static void Place(int waveOrdinal, int count, float heroX, float heroY, ArenaGeometry platform, float entryDepth,
            float formationSpacing, float minimumSeparation, EntryPlacement[] placements)
        {
            if (placements == null)
                throw new ArgumentNullException(nameof(placements));
            if (count < 1 || count > PackLayout.MaxPackSize || count > placements.Length)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (float.IsNaN(heroX) || float.IsInfinity(heroX) || float.IsNaN(heroY) || float.IsInfinity(heroY))
                throw new ArgumentOutOfRangeException(nameof(heroX));
            if (float.IsNaN(entryDepth) || float.IsInfinity(entryDepth) || entryDepth < 0f)
                throw new ArgumentOutOfRangeException(nameof(entryDepth));
            if (float.IsNaN(minimumSeparation) || float.IsInfinity(minimumSeparation) || minimumSeparation < 0f)
                throw new ArgumentOutOfRangeException(nameof(minimumSeparation));

            float separationSquared = minimumSeparation * minimumSeparation;
            int openSides = AllSides;
            while (true)
            {
                int closing = 0;
                for (int slot = 0; slot < count; slot++)
                {
                    Formation(waveOrdinal, slot, count, openSides, out EntrySide side, out int indexOnSide, out int countOnSide);
                    PackLayout.Offset(indexOnSide, countOnSide, formationSpacing, out float lateral, out float offset);
                    float depth = entryDepth + offset;
                    Outward(side, out float outwardX, out float outwardY);
                    Lateral(side, out float lateralX, out float lateralY);
                    float x = heroX + outwardX * depth + lateralX * lateral;
                    float y = heroY + outwardY * depth + lateralY * lateral;
                    if (!platform.IsOnPlatform(x, y, HeroMotion.EdgeMargin))
                    {
                        float inside = InsideFraction(heroX, heroY, x, y, platform);
                        float dx = (x - heroX) * inside;
                        float dy = (y - heroY) * inside;
                        if (dx * dx + dy * dy >= MinimumEntryDistance * MinimumEntryDistance)
                        {
                            x = heroX + dx;
                            y = heroY + dy;
                        }
                        else
                        {
                            closing |= 1 << (int)side;
                        }
                    }
                    placements[slot] = new EntryPlacement(side, lateral, x, y);
                }

                if (separationSquared > 0f)
                    closing |= CrowdedSides(placements, count, closing, separationSquared);
                if (closing == 0)
                    return;
                if ((openSides & ~closing) == 0)
                    break;
                openSides &= ~closing;
            }

            for (int slot = 0; slot < count; slot++)
                placements[slot] = DrawnInside(placements[slot], heroX, heroY, platform);
        }

        // The corners, beyond those already closing, of every spot nearer than the separation to an earlier spot, slot by slot;
        // spots on a closing corner neither crowd nor count as crowded, since the next layout moves them.
        private static int CrowdedSides(EntryPlacement[] placements, int count, int closing, float separationSquared)
        {
            for (int slot = 1; slot < count; slot++)
            {
                EntryPlacement placement = placements[slot];
                if ((closing & (1 << (int)placement.Side)) != 0)
                    continue;
                for (int other = 0; other < slot; other++)
                {
                    EntryPlacement earlier = placements[other];
                    if ((closing & (1 << (int)earlier.Side)) != 0)
                        continue;
                    float dx = placement.X - earlier.X;
                    float dy = placement.Y - earlier.Y;
                    if (dx * dx + dy * dy < separationSquared)
                    {
                        closing |= 1 << (int)placement.Side;
                        break;
                    }
                }
            }
            return closing;
        }

        // The placement moved in toward the hero until it stands inside the rim's margin.
        private static EntryPlacement DrawnInside(EntryPlacement placement, float heroX, float heroY, ArenaGeometry platform)
        {
            if (platform.IsOnPlatform(placement.X, placement.Y, HeroMotion.EdgeMargin) ||
                !platform.IsOnPlatform(heroX, heroY, HeroMotion.EdgeMargin))
                return placement;

            float inside = InsideFraction(heroX, heroY, placement.X, placement.Y, platform);
            return new EntryPlacement(placement.Side, placement.Lateral,
                heroX + (placement.X - heroX) * inside, heroY + (placement.Y - heroY) * inside);
        }

        // How far along the line from the hero to (x, y) it stays inside the rim's margin, as a fraction of the way, less the
        // rim relief; 0 when the hero's own spot is outside. The platform is convex, so everything short of that point is
        // inside too. The relief is taken at the end rather than from the margin the search uses, so a hero standing on the
        // rim itself still measures from where it stands instead of finding nowhere to put the pack.
        private static float InsideFraction(float heroX, float heroY, float x, float y, ArenaGeometry platform)
        {
            if (!platform.IsOnPlatform(heroX, heroY, HeroMotion.EdgeMargin))
                return 0f;

            float inside = 0f;
            float outside = 1f;
            for (int i = 0; i < 24; i++)
            {
                float middle = (inside + outside) / 2f;
                if (platform.IsOnPlatform(heroX + (x - heroX) * middle, heroY + (y - heroY) * middle, HeroMotion.EdgeMargin))
                    inside = middle;
                else
                    outside = middle;
            }
            return inside * (1f - RimRelief);
        }
    }
}
