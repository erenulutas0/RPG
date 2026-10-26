using System;

namespace Cryptforge.Progression
{
    // What an evolution is and what it costs. Pure, so the scene and the Descent simulation agree about both.
    //
    // An evolution is a card whose gate is another card being **full** rather than merely held: UpgradeOption.EvolvesFromId
    // names it, and the offer engine only ever shows the evolution once its base has reached its stack limit. That gate is
    // the reason an evolution is not also gated by UpgradePool.WidthAt - the width takes cards from the front of the
    // authored order and a run that reached level fifty-one only opens twelve of them, so evolutions sitting behind that
    // would never arrive. Filling a card is a harder thing to do than reaching a level, and it is the gate that means
    // something.
    //
    // And every evolution raises the world's difficulty. The phone session of 2026-09-21 measured both halves of the
    // problem this answers: thirteen of fifty-one level-ups produced nothing at all because the build was full, and the
    // run still had to be *left* rather than lost, at 102 health and falling 2.1 a wave. A slice that only handed out
    // more power would have closed the first hole by digging the second. So the cost is not optional and not a separate
    // card: it is a property of being an evolution, applied by whichever service applies the stack.
    public static class EvolutionRule
    {
        // How much harder every enemy gets for each evolution stack the hero takes, as a share of its authored health and
        // damage. Measured in docs/32.
        public const float DifficultyPerStack = 0.2f;

        // A full base card is not the only gate. A whole Descent grants eleven level-ups and takes them all on one card,
        // so it can fill a five-stack card and would have been offered that card's evolution - and since an evolution
        // ignores the pool's width, nothing else would have stopped it. Requiring the same level the width's first step
        // requires keeps every Descent exactly what it was, seed for seed, and leaves evolutions where they belong: in a
        // run long enough to have earned them.
        public const int MinimumLevel = UpgradePool.FirstExtraAtLevel;

        public static bool Unlocked(int level, bool baseCardFull) => baseCardFull && level >= MinimumLevel;

        // A weapon's evolution, once a run carries several (docs/42, PR 5; the report's 40 section 5.4), is gated on the
        // two halves of a build meeting: the weapon taken to level 5 of its 8 and the tome it evolves with to level 2.
        // Neither is a run level, so a run that spreads its picks evolves later than one that commits, which is the
        // point. The cost is the same as any evolution's.
        public const int WeaponEvolutionLevel = 5;
        public const int WeaponEvolutionTomeLevel = 2;

        public static bool WeaponUnlocked(int weaponLevel, int tomeLevel) =>
            weaponLevel >= WeaponEvolutionLevel && tomeLevel >= WeaponEvolutionTomeLevel;

        public static StatModifier DifficultyCost() =>
            new StatModifier(ModifierOperation.Flat, DifficultyPerStack);
    }
}
