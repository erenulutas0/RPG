using System;
using System.Collections.Generic;
using Cryptforge.Progression;

namespace Cryptforge.Core
{
    // The profile's forged hero skills (docs/57 phase 4): per hero, whether the Forge has forged its skill 2 and its skill
    // 3, and whether docs/57 5.2's one-line toast has introduced each on the first run that carried it. Skill 1 is every
    // hero's own. A skill opens for the profile at its floor's first clear (HeroSkillUnlock), so the record only says what
    // was bought and shown; PlayerProfile.TryForgeSkill spends the gold and raises Changed once, as MarkSkillToastShown
    // does. Restore repairs a damaged save instead of rejecting it, as ProfileTiers does.
    public sealed class ProfileSkills
    {
        private readonly List<Entry> _heroes = new List<Entry>();

        private sealed class Entry
        {
            public string Hero;
            public bool SkillTwo;
            public bool SkillThree;
            // The toast shown for the forged skill (never before it is forged).
            public bool ToastTwo;
            public bool ToastThree;
        }

        // The heroes with a forged skill, in the order they were first forged (the save's order).
        public int HeroCount => _heroes.Count;

        // Whether the hero carries the slot: skill 1 always, skills 2 and 3 once forged.
        public bool IsForged(string heroId, int slot)
        {
            if (slot == HeroSkillUnlock.FirstSlot)
                return true;
            Entry entry = Find(heroId);
            return entry != null && (slot == 2 ? entry.SkillTwo : slot == 3 && entry.SkillThree);
        }

        // The highest slot the hero carries: 1, 2 or 3 (skill 3 is forged after skill 2).
        public int SlotsFor(string heroId) => IsForged(heroId, 3) ? 3 : IsForged(heroId, 2) ? 2 : 1;

        // Whether the toast that introduces the hero's forged skill 2 or 3 has been shown (docs/57 5.2: once per skill).
        public bool ToastShown(string heroId, int slot)
        {
            Entry entry = Find(heroId);
            return entry != null && (slot == 2 ? entry.ToastTwo : slot == 3 && entry.ToastThree);
        }

        // ---- Changes: PlayerProfile calls these and raises Changed when they return true. ----

        internal bool Forge(string heroId, int slot)
        {
            if (string.IsNullOrEmpty(heroId))
                throw new ArgumentException("A hero id is required.", nameof(heroId));
            if (!HeroSkillUnlock.IsForgeable(slot))
                throw new ArgumentOutOfRangeException(nameof(slot));
            if (IsForged(heroId, slot))
                return false;
            Entry entry = Find(heroId);
            if (entry == null)
            {
                entry = new Entry { Hero = heroId };
                _heroes.Add(entry);
            }
            if (slot == 2)
                entry.SkillTwo = true;
            else
                entry.SkillThree = true;
            return true;
        }

        // The toast shown for a forged skill 2 or 3; false, and nothing changed, for one not forged or already shown.
        internal bool MarkToastShown(string heroId, int slot)
        {
            if (!HeroSkillUnlock.IsForgeable(slot) || !IsForged(heroId, slot) || ToastShown(heroId, slot))
                return false;
            Entry entry = Find(heroId);
            if (slot == 2)
                entry.ToastTwo = true;
            else
                entry.ToastThree = true;
            return true;
        }

        private Entry Find(string heroId)
        {
            if (string.IsNullOrEmpty(heroId))
                return null;
            for (int i = 0; i < _heroes.Count; i++)
            {
                if (_heroes[i].Hero == heroId)
                    return _heroes[i];
            }
            return null;
        }

        // ---- The save. ----

        public SkillSnapshot Snapshot()
        {
            var snapshot = new SkillSnapshot { Records = new HeroSkillRecord[_heroes.Count] };
            for (int i = 0; i < _heroes.Count; i++)
                snapshot.Records[i] = new HeroSkillRecord
                {
                    Hero = _heroes[i].Hero, SkillTwo = _heroes[i].SkillTwo, SkillThree = _heroes[i].SkillThree,
                    SkillTwoToastShown = _heroes[i].ToastTwo, SkillThreeToastShown = _heroes[i].ToastThree
                };
            return snapshot;
        }

        // Rebuilds the forged skills from a save, repairing what is damaged: a missing record or an empty hero id dropped,
        // a repeated hero's later records dropped (the first wins), a skill whose floor the profile has not cleared
        // (profileDeepest, PlayerProfile.DeepestFloorCleared) not forged, skill 3 without skill 2 not forged - so a damaged
        // save never hands a hero a skill it did not earn - and a hero left with nothing not kept; a toast is kept shown only
        // for a skill kept forged. A hero id the roster no longer knows is kept, as the unlocked heroes are. Never throws.
        public static ProfileSkills Restore(SkillSnapshot snapshot, int profileDeepest = 0)
        {
            var skills = new ProfileSkills();
            if (snapshot?.Records == null)
                return skills;
            foreach (HeroSkillRecord record in snapshot.Records)
            {
                if (record == null || string.IsNullOrWhiteSpace(record.Hero) || skills.Find(record.Hero) != null)
                    continue;
                bool two = record.SkillTwo && HeroSkillUnlock.IsOpen(2, profileDeepest);
                bool three = record.SkillThree && two && HeroSkillUnlock.IsOpen(3, profileDeepest);
                if (!two)
                    continue;
                skills._heroes.Add(new Entry
                {
                    Hero = record.Hero, SkillTwo = true, SkillThree = three,
                    ToastTwo = record.SkillTwoToastShown, ToastThree = three && record.SkillThreeToastShown
                });
            }
            return skills;
        }
    }

    // The forged skills as plain values, as the save holds them and before ProfileSkills.Restore has repaired them.
    // Nothing here is trusted.
    public sealed class SkillSnapshot
    {
        public HeroSkillRecord[] Records = new HeroSkillRecord[0];
    }

    // One hero's forged skills: its id (HeroDefinition.Id), whether skills 2 and 3 are forged, and whether each one's
    // toast has been shown (docs/57 5.2).
    public sealed class HeroSkillRecord
    {
        public string Hero;
        public bool SkillTwo;
        public bool SkillThree;
        public bool SkillTwoToastShown;
        public bool SkillThreeToastShown;
    }
}
