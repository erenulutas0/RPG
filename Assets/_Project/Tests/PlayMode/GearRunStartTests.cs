using System.Collections;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // The profile's gear reaches the run (docs/54 phase 5): CombatSetup reads the carried weapon's own gear and the equipped
    // armour, amulet and ring as the run begins and applies them through GearStats.Apply, the body the foundry simulation
    // calls too. A profile with a +9 Sword starts its run striking 10 % harder - in the hall, on its one weapon; on the
    // foundry, on the loadout's first weapon - a +9 armour starts the hero full at +10 % health, the rule lines reach the
    // loadout's shares, the hero's heals, the ability and the drops, and a profile without gear starts exactly as before, to
    // the bit. Another weapon's gear does not count: the weapon slot is per weapon id.
    public sealed class GearRunStartTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private CombatSetup _setup;

        [UnityTearDown]
        public IEnumerator UnloadGameplay()
        {
            Time.timeScale = 1f;
            Scene gameplay = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Gear Test Cleanup"));
            if (gameplay.path == ScenePath)
                yield return SceneManager.UnloadSceneAsync(gameplay);
            TestProfile.End();
        }

        [UnityTest]
        public IEnumerator AProfileWithoutGearStartsAsToday()
        {
            yield return Load(null, false);
            Assert.That(_setup.Gear.IsEmpty, Is.True);
            WeaponRuntime fresh = _setup.HeroWeapon.CreateRuntime();
            Assert.That(_setup.Weapon.Damage, Is.EqualTo(fresh.Damage), "the definition's damage, to the bit");
            Assert.That(_setup.Weapon.Interval, Is.EqualTo(fresh.Interval));
            Assert.That(_setup.Weapon.CritChance, Is.EqualTo(fresh.CritChance));
            Assert.That(_setup.Weapon.Area, Is.EqualTo(fresh.Area));
            AssertHeroAsAuthored();
        }

        [UnityTest]
        public IEnumerator AProfileWithoutGearStartsTheFoundryAsToday()
        {
            yield return Load(null, true);
            Assert.That(_setup.Upgrades.IsCategoryMode, Is.True, "the foundry builds by category");
            Assert.That(_setup.Gear.IsEmpty, Is.True);
            WeaponRuntime fresh = _setup.HeroWeapon.CreateCategoryRuntime();
            WeaponRuntime first = _setup.Upgrades.Loadout.At(0).Runtime;
            Assert.That(first.Damage, Is.EqualTo(fresh.Damage), "the category base, to the bit");
            Assert.That(first.Interval, Is.EqualTo(fresh.Interval));
            Assert.That(first.CritChance, Is.EqualTo(fresh.CritChance));
            AssertHeroAsAuthored(false);
        }

        [UnityTest]
        public IEnumerator APlusNineSwordStrikesTenPercentHarder()
        {
            yield return Load(SwordAt(9), false);
            Assert.That(_setup.HeroWeapon.Id, Is.EqualTo("weapon_sword"));
            Assert.That(_setup.Gear.Total(GearStat.Damage), Is.EqualTo(1000), "+9 is +10 % damage");
            Assert.That(_setup.Gear.Power, Is.EqualTo(1000));
            WeaponRuntime fresh = _setup.HeroWeapon.CreateRuntime();
            Assert.That(_setup.Weapon.Damage, Is.EqualTo(fresh.Damage * 1.1f).Within(1e-4f));
            Assert.That(_setup.Weapon.Interval, Is.EqualTo(fresh.Interval), "nothing but damage");
            Assert.That(_setup.Weapon.CritChance, Is.EqualTo(fresh.CritChance));
            AssertHeroAsAuthored();
        }

        [UnityTest]
        public IEnumerator TheFoundryWearsTheWholeSetOnItsFirstWeapon()
        {
            // The Sword at +9, the Staff at +9 too (not carried, so not counted), and an equipped +9 armour and ring.
            yield return Load(SwordAt(9, staffLevel: 9, armourLevel: 9, ringLevel: 9), true);
            Assert.That(_setup.Gear.Total(GearStat.Damage), Is.EqualTo(1000), "the Sword's +9 alone: the Staff is not carried");
            WeaponRuntime fresh = _setup.HeroWeapon.CreateCategoryRuntime();
            WeaponRuntime first = _setup.Upgrades.Loadout.At(0).Runtime;
            Assert.That(first.Damage, Is.EqualTo(fresh.Damage * 1.1f).Within(1e-4f));
            Assert.That(first.CritChance, Is.EqualTo(fresh.CritChance + .03f).Within(1e-6f), "the ring's three points");
            Assert.That(_setup.HeroStats.MaxHealth, Is.EqualTo(_setup.Hero.MaximumHealth * 1.1f).Within(1e-3f), "the armour's +10 %");
            var hero = GameObject.Find("Vanguard").GetComponent<Health>();
            Assert.That(hero.Maximum, Is.EqualTo(_setup.HeroStats.MaxHealth), "the body wears the sheet");
        }

        [UnityTest]
        public IEnumerator APlusNineArmourStartsTheHeroFullAtItsGain()
        {
            // The hall's first pack walks in from the far end, so nothing has struck the hero yet.
            yield return Load(SwordAt(0, armourLevel: 9), false);
            var hero = GameObject.Find("Vanguard").GetComponent<Health>();
            Assert.That(hero.Maximum, Is.EqualTo(_setup.Hero.MaximumHealth * 1.1f).Within(1e-3f));
            Assert.That(hero.Current, Is.EqualTo(hero.Maximum), "the gain arrives filled");
            Assert.That(_setup.Weapon.Damage, Is.EqualTo(_setup.HeroWeapon.CreateRuntime().Damage), "a +0 Sword adds nothing");
        }

        // The rule lines reach the scene's own objects (docs/54 section 20): a Gold ring with damage vs elites, healing,
        // ability cooldown and pickup radius at tier 5 - the loadout's shares, the hero's heals, the ability and the drops.
        [UnityTest]
        public IEnumerator TheRuleLinesReachTheScenesObjects()
        {
            var snapshot = new GearSnapshot
            {
                Items = new[]
                {
                    new GearRecord
                    {
                        Uid = 1, ItemId = "item_cinder_ring", Slot = (int)GearSlot.Ring, Rarity = (int)UpgradeRarity.Gold, Level = 0,
                        Lines = new[]
                        {
                            new GearLineRecord("elite_damage", 5), new GearLineRecord("healing", 5),
                            new GearLineRecord("ability_cooldown", 5), new GearLineRecord("pickup_radius", 5)
                        }
                    }
                },
                EquippedUids = new[] { 0, 0, 1 },
                NextItemUid = 2,
                AnvilSeed = 7
            };
            yield return Load(new PlayerProfile(0, null, null, 0, null, null, null, null, ProfileGear.Restore(snapshot)), true);
            Assert.That(_setup.Gear.IsEmpty, Is.False);
            Assert.That(_setup.Gear.Power, Is.Zero, "no line of the power index");
            WeaponLoadout loadout = _setup.Upgrades.Loadout;
            Assert.That(loadout.TargetShareFactor, Is.EqualTo(1.06f).Within(1e-6f));
            Assert.That(loadout.At(0).Runtime.GateBossShare, Is.EqualTo(WardenRule.SwordShare * 1.06f).Within(1e-5f), "the Sword's share, +6 %");
            Assert.That(GameObject.Find("Vanguard").GetComponent<Health>().State.HealingReceived, Is.EqualTo(1.09f).Within(1e-6f));
            Assert.That(_setup.Ability.Cooldown, Is.EqualTo(_setup.Hero.Ability.CreateRuntime().Cooldown * .94f).Within(1e-4f));
            Assert.That(_setup.Drops.PickupRadius, Is.EqualTo(Cryptforge.Economy.DropRule.PickupRadius * 1.12f).Within(1e-6f));
            WeaponRuntime fresh = _setup.HeroWeapon.CreateCategoryRuntime();
            Assert.That(loadout.At(0).Runtime.Damage, Is.EqualTo(fresh.Damage), "and the sheet untouched");
        }

        // The sheet and the body as the hero's definition makes them; full, where nothing can have struck yet (the hall).
        private void AssertHeroAsAuthored(bool full = true)
        {
            Assert.That(_setup.HeroStats.MaxHealth, Is.EqualTo(_setup.Hero.MaximumHealth));
            Assert.That(_setup.HeroStats.Armor, Is.Zero);
            Assert.That(_setup.HeroStats.Luck, Is.Zero);
            var hero = GameObject.Find("Vanguard").GetComponent<Health>();
            Assert.That(hero.Maximum, Is.EqualTo(_setup.Hero.MaximumHealth));
            if (full)
                Assert.That(hero.Current, Is.EqualTo(hero.Maximum));
        }

        private static PlayerProfile SwordAt(int level, int staffLevel = 0, int armourLevel = -1, int ringLevel = -1)
        {
            var items = new System.Collections.Generic.List<GearRecord>();
            var equipped = new int[ProfileGear.EquipSlotCount];
            if (armourLevel >= 0)
            {
                items.Add(new GearRecord { Uid = 1, ItemId = "item_foundry_mail", Slot = (int)GearSlot.Armour, Rarity = 1, Level = armourLevel });
                equipped[0] = 1;
            }
            if (ringLevel >= 0)
            {
                items.Add(new GearRecord { Uid = 2, ItemId = "item_cinder_ring", Slot = (int)GearSlot.Ring, Rarity = 1, Level = ringLevel });
                equipped[2] = 2;
            }
            var snapshot = new GearSnapshot
            {
                Items = items.ToArray(),
                Weapons = staffLevel > 0
                    ? new[] { new GearRecord { ItemId = "weapon_sword", Level = level }, new GearRecord { ItemId = "weapon_staff", Level = staffLevel } }
                    : new[] { new GearRecord { ItemId = "weapon_sword", Level = level } },
                EquippedUids = equipped,
                NextItemUid = 3,
                AnvilSeed = 7
            };
            return new PlayerProfile(0, null, null, 0, null, null, null, null, ProfileGear.Restore(snapshot));
        }

        // The scene loaded on a profile (null: a fresh one), on the hall arena or the foundry, its run started at once.
        private IEnumerator Load(PlayerProfile profile, bool foundry)
        {
            TestProfile.Begin(profile);
            if (foundry)
                File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            _setup = GameObject.Find("Combat Setup").GetComponent<CombatSetup>();
            Assert.That(_setup.Hero.Id, Is.EqualTo("hero_vanguard"));
        }
    }
}
