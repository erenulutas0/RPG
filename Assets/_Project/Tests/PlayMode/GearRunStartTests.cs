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
    // foundry, on the loadout's first weapon - and a profile without gear starts exactly as before, to the bit. Another
    // weapon's gear does not count: the weapon slot is per weapon id.
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
            AssertHeroAsAuthored();
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
            Assert.That(hero.Current, Is.EqualTo(hero.Maximum), "and starts full");
        }

        private void AssertHeroAsAuthored()
        {
            Assert.That(_setup.HeroStats.MaxHealth, Is.EqualTo(_setup.Hero.MaximumHealth));
            Assert.That(_setup.HeroStats.Armor, Is.Zero);
            Assert.That(_setup.HeroStats.Luck, Is.Zero);
            var hero = GameObject.Find("Vanguard").GetComponent<Health>();
            Assert.That(hero.Maximum, Is.EqualTo(_setup.Hero.MaximumHealth));
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
