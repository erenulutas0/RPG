namespace Cryptforge.Progression
{
    // How a hero's skills 2 and 3 open (docs/57 section 12, the owner's answer of 2026-11-18, and section 14): the first
    // clear of floor 2 opens skill 2 and the first clear of floor 3 opens skill 3 - for the profile, on any difficulty
    // tier and with either hero (PlayerProfile.DeepestFloorCleared, as the map starts open, docs/53) - and the Forge then
    // forges each for each hero: skill 2 for 300 gold, skill 3 for 500, skill 3 once skill 2 is forged. Skill 1 is the
    // hero's own from the start. The gold is the run's, earned in play: no skill is sold for money (docs/58).
    public static class HeroSkillUnlock
    {
        public const int FirstSlot = 1;
        public const int LastSlot = 3;
        public const int SkillTwoFloor = 2;
        public const int SkillThreeFloor = 3;
        public const int SkillTwoPrice = 300;
        public const int SkillThreePrice = 500;

        // Skills 2 and 3 are forged; skill 1 is the hero's own.
        public static bool IsForgeable(int slot) => slot == 2 || slot == 3;

        // The floor whose first clear opens the slot; 0 for skill 1 (open from the start) and for a slot that is none.
        public static int OpeningFloor(int slot) => slot == 2 ? SkillTwoFloor : slot == 3 ? SkillThreeFloor : 0;

        // The Forge's price of the slot; 0 for skill 1 and for a slot that is none.
        public static int Price(int slot) => slot == 2 ? SkillTwoPrice : slot == 3 ? SkillThreePrice : 0;

        // The slot the Forge asks to be forged first: skill 3 waits on skill 2; 0 for none.
        public static int Requires(int slot) => slot == 3 ? 2 : 0;

        // Whether the slot is open for the profile: skill 1 always, skills 2 and 3 once their floor has been cleared.
        public static bool IsOpen(int slot, int deepestFloorCleared) =>
            slot == FirstSlot || (IsForgeable(slot) && deepestFloorCleared >= OpeningFloor(slot));

        // What the Forge shows for a hero's slot: forged (skill 1 always), locked until its floor is cleared, waiting on
        // the skill it requires, then affordable or not with the profile's gold.
        public static SkillForgeStatus StatusOf(int slot, int deepestFloorCleared, bool forged, bool requiredForged, int gold)
        {
            if (slot == FirstSlot || (IsForgeable(slot) && forged))
                return SkillForgeStatus.Forged;
            if (!IsOpen(slot, deepestFloorCleared))
                return SkillForgeStatus.Locked;
            if (Requires(slot) != 0 && !requiredForged)
                return SkillForgeStatus.NeedsEarlierSkill;
            return gold >= Price(slot) ? SkillForgeStatus.Affordable : SkillForgeStatus.TooExpensive;
        }
    }

    // A hero's skill as the Forge shows it (HeroSkillUnlock.StatusOf).
    public enum SkillForgeStatus
    {
        Locked = 0,
        NeedsEarlierSkill = 1,
        TooExpensive = 2,
        Affordable = 3,
        Forged = 4
    }
}
