using System;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.Save;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/57 phase 4, the unlock as built (section 14): floor 2's first clear opens skill 2 and floor 3's opens skill 3, for
    // the profile on any tier and with either hero; the Forge then forges each per hero for 300 and 500 gold, skill 3 after
    // skill 2; the profile's record of what was forged, its save (version 6) and the repair of a damaged one.
    // ProfileStoreTests reads the same through JSON. Also in Tools/CombatChecks.
    public sealed class HeroSkillUnlockTests
    {
        private const string Vanguard = "hero_vanguard";
        private const string Ilya = "hero_cinder_arcanist";

        [Test]
        public void TheRuleIsTheOwners()
        {
            Assert.That((HeroSkillUnlock.OpeningFloor(2), HeroSkillUnlock.OpeningFloor(3)), Is.EqualTo((2, 3)));
            Assert.That((HeroSkillUnlock.Price(2), HeroSkillUnlock.Price(3)), Is.EqualTo((300, 500)));
            Assert.That(HeroSkillUnlock.IsForgeable(1) || HeroSkillUnlock.IsForgeable(0) || HeroSkillUnlock.IsForgeable(4), Is.False);
            Assert.That(HeroSkillUnlock.Price(1), Is.Zero, "skill 1 is the hero's own");
            Assert.That(HeroSkillUnlock.Requires(3), Is.EqualTo(2));
            Assert.That(HeroSkillUnlock.Requires(2), Is.Zero);
            for (int deepest = 0; deepest <= 4; deepest++)
            {
                Assert.That(HeroSkillUnlock.IsOpen(1, deepest), Is.True);
                Assert.That(HeroSkillUnlock.IsOpen(2, deepest), Is.EqualTo(deepest >= 2), $"skill 2 at a record of {deepest}");
                Assert.That(HeroSkillUnlock.IsOpen(3, deepest), Is.EqualTo(deepest >= 3), $"skill 3 at a record of {deepest}");
            }
        }

        [Test]
        public void TheForgeShowsEachState()
        {
            Assert.That(HeroSkillUnlock.StatusOf(1, 0, false, false, 0), Is.EqualTo(SkillForgeStatus.Forged));
            Assert.That(HeroSkillUnlock.StatusOf(2, 1, false, false, 9999), Is.EqualTo(SkillForgeStatus.Locked));
            Assert.That(HeroSkillUnlock.StatusOf(2, 2, false, false, 299), Is.EqualTo(SkillForgeStatus.TooExpensive));
            Assert.That(HeroSkillUnlock.StatusOf(2, 2, false, false, 300), Is.EqualTo(SkillForgeStatus.Affordable));
            Assert.That(HeroSkillUnlock.StatusOf(2, 2, true, false, 0), Is.EqualTo(SkillForgeStatus.Forged));
            Assert.That(HeroSkillUnlock.StatusOf(3, 2, false, true, 9999), Is.EqualTo(SkillForgeStatus.Locked));
            Assert.That(HeroSkillUnlock.StatusOf(3, 3, false, false, 9999), Is.EqualTo(SkillForgeStatus.NeedsEarlierSkill));
            Assert.That(HeroSkillUnlock.StatusOf(3, 4, false, true, 499), Is.EqualTo(SkillForgeStatus.TooExpensive));
            Assert.That(HeroSkillUnlock.StatusOf(3, 4, false, true, 500), Is.EqualTo(SkillForgeStatus.Affordable));
        }

        // A new profile forges nothing; floor 2's clear opens skill 2 on any tier; the Forge spends the price once and the
        // profile raises Changed once, the save's trigger; per hero; skill 3 after floor 3 and skill 2.
        [Test]
        public void AProfileOpensAndForgesItsSkills()
        {
            var profile = new PlayerProfile();
            int changes = 0;
            profile.Changed += () => changes++;
            profile.Deposit(2000);
            changes = 0;
            Assert.That(profile.SkillStatusOf(Vanguard, 2), Is.EqualTo(SkillForgeStatus.Locked));
            Assert.That(profile.TryForgeSkill(Vanguard, 2), Is.False, "not before floor 2's first clear");
            Assert.That((profile.Gold, changes), Is.EqualTo((2000, 0)));

            profile.RecordFloorCleared(1);
            profile.RecordFloorCleared(2, DifficultyTier.Tempered);
            changes = 0;
            Assert.That(profile.SkillStatusOf(Vanguard, 2), Is.EqualTo(SkillForgeStatus.Affordable), "a clear on any tier");
            Assert.That(profile.SkillStatusOf(Ilya, 2), Is.EqualTo(SkillForgeStatus.Affordable), "the clear opens it for every hero");
            Assert.That(profile.SkillStatusOf(Vanguard, 3), Is.EqualTo(SkillForgeStatus.Locked));
            Assert.That(profile.TryForgeSkill(Vanguard, 2), Is.True);
            Assert.That((profile.Gold, changes), Is.EqualTo((1700, 1)), "300 spent, one change");
            Assert.That(profile.HasForgedSkill(Vanguard, 2), Is.True);
            Assert.That(profile.HasForgedSkill(Ilya, 2), Is.False, "the Forge forges it per hero");
            Assert.That(profile.TryForgeSkill(Vanguard, 2), Is.False, "never twice");
            Assert.That((profile.Gold, changes), Is.EqualTo((1700, 1)));

            profile.RecordFloorCleared(3);
            changes = 0;
            Assert.That(profile.SkillStatusOf(Ilya, 3), Is.EqualTo(SkillForgeStatus.NeedsEarlierSkill));
            Assert.That(profile.TryForgeSkill(Ilya, 3), Is.False, "skill 3 after skill 2");
            Assert.That(profile.TryForgeSkill(Vanguard, 3), Is.True);
            Assert.That((profile.Gold, changes), Is.EqualTo((1200, 1)), "500 spent");
            Assert.That(profile.Skills.SlotsFor(Vanguard), Is.EqualTo(3));
            Assert.That(profile.Skills.SlotsFor(Ilya), Is.EqualTo(1));
        }

        [Test]
        public void ShortGoldForgesNothing()
        {
            var profile = new PlayerProfile(299, null, null, 3);
            Assert.That(profile.SkillStatusOf(Ilya, 2), Is.EqualTo(SkillForgeStatus.TooExpensive));
            Assert.That(profile.GoldNeededForSkill(Ilya, 2), Is.EqualTo(1));
            Assert.That(profile.TryForgeSkill(Ilya, 2), Is.False);
            Assert.That(profile.Gold, Is.EqualTo(299));
            profile.Deposit(1);
            Assert.That(profile.TryForgeSkill(Ilya, 2), Is.True);
            Assert.That(profile.Gold, Is.Zero);
            Assert.That(profile.GoldNeededForSkill(Ilya, 2), Is.Zero, "forged");
            Assert.That(profile.GoldNeededForSkill(Ilya, 3), Is.EqualTo(500));
        }

        [Test]
        public void OnlySkillsTwoAndThreeAreForged()
        {
            var profile = new PlayerProfile(9999, null, null, 4);
            Assert.Throws<ArgumentOutOfRangeException>(() => profile.TryForgeSkill(Vanguard, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => profile.TryForgeSkill(Vanguard, 4));
            Assert.Throws<ArgumentException>(() => profile.TryForgeSkill(string.Empty, 2));
            Assert.That(profile.HasForgedSkill(null, 2), Is.False);
            Assert.That(profile.HasForgedSkill(Vanguard, 1), Is.True, "skill 1 is every hero's own");
        }

        // The record through the save's fields and back, and a damaged one repaired (ProfileSkills.Restore).
        [Test]
        public void TheSkillsSurviveTheSaveAndADamagedOneIsRepaired()
        {
            var profile = new PlayerProfile(800, null, null, 3);
            Assert.That(profile.TryForgeSkill(Ilya, 2), Is.True);
            Assert.That(profile.TryForgeSkill(Ilya, 3), Is.True);
            var data = new ProfileSaveData();
            ProfileSkillsSave.Write(profile.Skills.Snapshot(), data);
            Assert.That(data.heroSkills.Length, Is.EqualTo(1));
            Assert.That((data.heroSkills[0].hero, data.heroSkills[0].skillTwo, data.heroSkills[0].skillThree),
                Is.EqualTo((Ilya, true, true)));
            ProfileSkills restored = ProfileSkills.Restore(ProfileSkillsSave.ToSnapshot(data), 3);
            Assert.That(restored.SlotsFor(Ilya), Is.EqualTo(3));
            Assert.That(ProfileSkills.Restore(ProfileSkillsSave.ToSnapshot(data), 2).SlotsFor(Ilya), Is.EqualTo(2),
                "a record short of floor 3 keeps skill 2 alone");

            data.heroSkills = new[]
            {
                null,
                new HeroSkillsSaveData { hero = " ", skillTwo = true },
                new HeroSkillsSaveData { hero = Vanguard, skillTwo = false, skillThree = true },
                new HeroSkillsSaveData { hero = Ilya, skillTwo = true, skillThree = false },
                new HeroSkillsSaveData { hero = Ilya, skillTwo = true, skillThree = true }
            };
            ProfileSkills repaired = ProfileSkills.Restore(ProfileSkillsSave.ToSnapshot(data), 4);
            Assert.That(repaired.HeroCount, Is.EqualTo(1));
            Assert.That(repaired.SlotsFor(Vanguard), Is.EqualTo(1), "skill 3 without skill 2 is not kept");
            Assert.That(repaired.SlotsFor(Ilya), Is.EqualTo(2), "the first record wins");
            Assert.That(ProfileSkills.Restore(null, 4).HeroCount, Is.Zero);
            Assert.That(ProfileSkillsSave.ToSnapshot(new ProfileSaveData()).Records, Is.Empty, "no field, no record");
        }

        // The result screen's line (docs/57 section 14): a run opens a skill when its clears carry the depth record across the
        // skill's floor - skill 2 at floor 2, skill 3 at floor 3, both in one run that clears both for the first time - and
        // never skill 1, never a skill already open, never a run that cleared nothing new.
        [Test]
        public void ARunOpensASkillWhenItsClearsCrossTheSkillsFloor()
        {
            Assert.That(HeroSkillUnlock.OpensBetween(2, 1, 2), Is.True);
            Assert.That(HeroSkillUnlock.OpensBetween(3, 1, 2), Is.False);
            Assert.That(HeroSkillUnlock.OpensBetween(2, 0, 3) && HeroSkillUnlock.OpensBetween(3, 0, 3), Is.True, "both in one run");
            Assert.That(HeroSkillUnlock.OpensBetween(3, 2, 4), Is.True, "the finale's record carries floor 3");
            Assert.That(HeroSkillUnlock.OpensBetween(2, 2, 3), Is.False, "skill 2 was open before the run");
            Assert.That(HeroSkillUnlock.OpensBetween(2, 2, 2) || HeroSkillUnlock.OpensBetween(3, 3, 3), Is.False);
            Assert.That(HeroSkillUnlock.OpensBetween(1, 0, 4), Is.False, "skill 1 is the hero's own from the start");
            Assert.That(HeroSkillUnlock.OpensBetween(4, 0, 9), Is.False);
        }

        [Test]
        public void VersionFiveMigratesWithNothingForged()
        {
            var five = new ProfileSaveData { saveVersion = 5, revision = 3, gold = 700, deepestFloorCleared = 4 };
            ProfileSaveData upgraded = ProfileMigration.Upgrade(five);
            Assert.That(upgraded.saveVersion, Is.EqualTo(6));
            Assert.That(upgraded.heroSkills, Is.Empty);
            Assert.That((upgraded.gold, upgraded.deepestFloorCleared), Is.EqualTo((700, 4)));
            var six = new ProfileSaveData
            {
                saveVersion = 6, heroSkills = new[] { new HeroSkillsSaveData { hero = Vanguard, skillTwo = true } }
            };
            Assert.That(ProfileMigration.Upgrade(six).heroSkills.Length, Is.EqualTo(1), "version 6 passes through");
            Assert.That(ProfileMigration.Upgrade(new ProfileSaveData { saveVersion = 7 }), Is.Null);
        }
    }
}
