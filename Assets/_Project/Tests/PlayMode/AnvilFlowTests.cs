using System.Collections;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cryptforge.Tests
{
    // A profile with gear for the Anvil's scene tests and captures (docs/54 phase 4): every rarity, levels +0 to +9, the
    // three equipped slots and the Sword's own gear at +7, materials and boosters to spend, and a chosen anvil seed.
    internal static class AnvilFixtures
    {
        public const int Gold = 20000;

        public static PlayerProfile Profile(int anvilSeed = 7, int extraItems = 0, int swordLevel = 7, int ringLevel = 9)
        {
            var items = new System.Collections.Generic.List<GearRecord>
            {
                Item(1, "item_foundry_mail", GearSlot.Armour, UpgradeRarity.Common, 3, Line("damage", 2)),
                Item(2, "item_ember_locket", GearSlot.Amulet, UpgradeRarity.Epic, 7, Line("crit", 3), Line("health", 2), Line("move_speed", 1)),
                Item(3, "item_cinder_ring", GearSlot.Ring, UpgradeRarity.Gold, ringLevel, Line("damage", 4), Line("attack_speed", 3),
                    Line("health", 2), Line("luck", 5)),
                Item(4, "item_mirror_ring", GearSlot.Ring, UpgradeRarity.Rare, 0, Line("elite_damage", 2), Line("area", 1)),
                Item(5, "item_quicksilver_hauberk", GearSlot.Armour, UpgradeRarity.Rare, 2, Line("armour", 3), Line("healing", 2)),
                Item(6, "item_spore_locket", GearSlot.Amulet, UpgradeRarity.Gold, 0, Line("damage", 3), Line("crit", 2), Line("burn_damage", 4),
                    Line("ability_cooldown", 1)),
                Item(7, "item_bark_mail", GearSlot.Armour, UpgradeRarity.Epic, 5, Line("luck", 2), Line("pickup_radius", 3), Line("damage", 1)),
                Item(8, "item_gloam_signet", GearSlot.Ring, UpgradeRarity.Common, 0, Line("move_speed", 2))
            };
            for (int i = 0; i < extraItems; i++)
                items.Add(Item(9 + i, "item_cinder_ring", GearSlot.Ring, (UpgradeRarity)(i % 4), i % 10, Line("damage", 1 + i % 5)));
            var snapshot = new GearSnapshot
            {
                Items = items.ToArray(),
                Weapons = new[]
                {
                    new GearRecord { ItemId = "weapon_sword", Level = swordLevel, Pity = 2, Lines = new[] { Line("crit", 3), Line("health", 2) } }
                },
                EquippedUids = new[] { 1, 2, 3 },
                Materials = new[] { 40, 20, 40 },
                Boosters = new[] { 3, 2, 4, 2 },
                NextItemUid = 9 + extraItems,
                AnvilSeed = anvilSeed
            };
            return new PlayerProfile(Gold, null, null, 3, null, null, null, null, ProfileGear.Restore(snapshot));
        }

        // An anvil seed whose first draw holds (roll under the chance) or fails (roll at or over it).
        public static int SeedWhoseFirstRoll(bool holds, int chancePercent)
        {
            for (int seed = 1; seed < 100000; seed++)
            {
                int roll = AnvilRule.Roll(seed, 0);
                if (holds ? roll < chancePercent : roll >= chancePercent)
                    return seed;
            }
            throw new System.InvalidOperationException("No seed found.");
        }

        private static GearRecord Item(int uid, string id, GearSlot slot, UpgradeRarity rarity, int level, params GearLineRecord[] lines) =>
            new GearRecord { Uid = uid, ItemId = id, Slot = (int)slot, Rarity = (int)rarity, Level = level, Lines = lines };

        private static GearLineRecord Line(string stat, int tier) => new GearLineRecord(stat, tier);
    }

    // The Forge's Anvil, Efsun and Bag tabs in the scene (docs/54 phase 4): the tabs open and close; a held strike holds or
    // fails and never breaks or lowers the item, and a tap is not a strike; a reroll is kept old or taken new; melt asks
    // twice; inherit moves the level for its 10 %; equip; a full bag shows thirty.
    public sealed class AnvilFlowTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private CombatSetup _setup;
        private RelicForgeView _forge;
        private AnvilView _anvil;

        [UnityTearDown]
        public IEnumerator UnloadGameplay()
        {
            Time.timeScale = 1f;
            Scene gameplay = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Anvil Test Cleanup"));
            if (gameplay.path == ScenePath)
                yield return SceneManager.UnloadSceneAsync(gameplay);
            TestProfile.End();
        }

        [UnityTest]
        public IEnumerator TheTabsOpenFromTheForgeAndClose()
        {
            yield return OpenForge(AnvilFixtures.Profile());
            Assert.That(_anvil.AnvilEntry, Is.Not.Null);
            Assert.That(_anvil.BagEntry, Is.Not.Null);
            Tap(_anvil.AnvilEntry);
            yield return Settle();
            Assert.That(_anvil.IsOpen, Is.True);
            Assert.That(_anvil.Current, Is.EqualTo(AnvilView.Tab.Anvil));
            Assert.That(_anvil.Selected.ItemId, Is.EqualTo("weapon_sword"), "The equipped weapon first.");
            Assert.That(_anvil.PieceText(0), Is.EqualTo("+7 Sword"));
            Assert.That(_anvil.PieceText(3), Is.EqualTo("+9 Cinder Ring"));
            Tap(_anvil.TabButton(AnvilView.Tab.Efsun));
            yield return Settle();
            Assert.That(_anvil.Current, Is.EqualTo(AnvilView.Tab.Efsun));
            Tap(_anvil.TabButton(AnvilView.Tab.Bag));
            yield return Settle();
            Assert.That(_anvil.Current, Is.EqualTo(AnvilView.Tab.Bag));
            Tap(_anvil.TabButton(AnvilView.Tab.Forge));
            Assert.That(_anvil.IsOpen, Is.False, "Forge closes the panel.");
            Assert.That(_forge.IsOpen, Is.True, "The Relic Forge stands under it.");
            Tap(_anvil.BagEntry);
            yield return Settle();
            Assert.That(_anvil.IsOpen && _anvil.Current == AnvilView.Tab.Bag, Is.True);
            Assert.That(_setup.Profile.Gold, Is.EqualTo(AnvilFixtures.Gold), "Looking spends nothing.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AFailedStrikeKeepsTheItemAndItsLevel()
        {
            // The armour at +3 struck to +4 is 90 %; the ring at +8 to +9 is 20 %: a seed whose first roll misses 20.
            yield return OpenForge(AnvilFixtures.Profile(AnvilFixtures.SeedWhoseFirstRoll(false, 20), ringLevel: 8));
            Tap(_anvil.AnvilEntry);
            yield return Settle();
            Tap(_anvil.PieceButton(3));
            ItemInstance ring = _anvil.Selected;
            Assert.That(ring.Level, Is.EqualTo(8));
            Assert.That(_anvil.ChanceText, Is.EqualTo("20%"));
            Assert.That(_anvil.CostText, Does.Contain("2,400 gold").And.Contain("6 Gloam Spore"));

            // A tap is not a strike.
            Tap(_anvil.StrikeButton);
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(_setup.Profile.Gold, Is.EqualTo(AnvilFixtures.Gold), "A tap spends nothing.");

            yield return Hold();
            Assert.That(ring.Level, Is.EqualTo(8), "A miss keeps the level.");
            Assert.That(_setup.Profile.Gear.Owns(ring), Is.True, "A miss never breaks the item.");
            Assert.That(ring.Pity, Is.EqualTo(1));
            Assert.That(_anvil.MomentText, Does.Contain("+8 kept"));
            Assert.That(_anvil.MemoryText, Does.Contain("1 / 10"));
            Assert.That(_anvil.ChanceText, Is.EqualTo("22%"), "The anvil remembers: +2 points.");
            Assert.That(_setup.Profile.Gold, Is.EqualTo(AnvilFixtures.Gold - 2400));
            Assert.That(TestProfile.ReadSaved().Gear.FindItem(ring.Uid).Pity, Is.EqualTo(1), "Saved as one change.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AHeldStrikeRaisesTheLevelAndASealCounts()
        {
            yield return OpenForge(AnvilFixtures.Profile(AnvilFixtures.SeedWhoseFirstRoll(true, 30), ringLevel: 8));
            Tap(_anvil.AnvilEntry);
            yield return Settle();
            Tap(_anvil.PieceButton(3));
            ItemInstance ring = _anvil.Selected;
            Assert.That(_anvil.SealToggle.gameObject.activeSelf, Is.True);
            Assert.That(_anvil.HammerToggle.gameObject.activeSelf, Is.False, "No Hammer past +6.");
            Tap(_anvil.SealToggle);
            Assert.That(_anvil.ChanceText, Is.EqualTo("30%"));
            Assert.That(_anvil.BreakdownText, Does.Contain("Seal 10%"));
            yield return Hold();
            Assert.That(ring.Level, Is.EqualTo(9));
            Assert.That(_anvil.MomentText, Does.Contain("+9"));
            Assert.That(_setup.Profile.Gear.Booster(BoosterKind.ForgeSeal), Is.EqualTo(2));
            Assert.That(_anvil.StrikeButton.interactable, Is.False, "+9 is the top.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheWeaponsGearIsMadeOnItsFirstStrike()
        {
            PlayerProfile seed = AnvilFixtures.Profile();
            yield return OpenForge(new PlayerProfile(seed.Gold, null, null, 3, null, null, null, null,
                ProfileGear.Restore(new GearSnapshot { AnvilSeed = 3 })));
            Tap(_anvil.AnvilEntry);
            yield return Settle();
            Assert.That(_anvil.Selected, Is.Null, "No gear before the first touch.");
            Assert.That(_anvil.WeaponId, Is.EqualTo("weapon_sword"));
            Assert.That(_setup.Profile.Gear.WeaponGear("weapon_sword"), Is.Null, "Opening the tab makes nothing.");
            yield return Hold();
            ItemInstance sword = _setup.Profile.Gear.WeaponGear("weapon_sword");
            Assert.That(sword, Is.Not.Null);
            Assert.That(sword.Level, Is.EqualTo(1), "+1 always holds.");
            Assert.That(sword.Lines.Count, Is.EqualTo(EfsunRule.WeaponLines));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ARerollIsKeptOldOrTakenNew()
        {
            yield return OpenForge(AnvilFixtures.Profile());
            Tap(_anvil.AnvilEntry);
            yield return Settle();
            Tap(_anvil.PieceButton(2));
            Tap(_anvil.TabButton(AnvilView.Tab.Efsun));
            yield return Settle();
            ItemInstance locket = _anvil.Selected;
            string before = _anvil.CurrentLinesText;
            Assert.That(before, Does.Contain("Crit chance"));
            Tap(_anvil.RerollWithGold);
            Assert.That(_setup.Profile.Gold, Is.EqualTo(AnvilFixtures.Gold - 160), "An Epic's reroll.");
            Assert.That(_anvil.NewLinesText, Is.Not.Empty, "The new set beside the old.");
            Assert.That(_anvil.CurrentLinesText, Is.EqualTo(before));
            Tap(_anvil.KeepOld);
            Assert.That(_anvil.CurrentLinesText, Is.EqualTo(before), "Kept the old.");
            Tap(_anvil.RerollWithScroll);
            Assert.That(_setup.Profile.Gear.Booster(BoosterKind.EfsunScroll), Is.EqualTo(3));
            string shown = _anvil.NewLinesText;
            Tap(_anvil.TakeNew);
            Assert.That(_anvil.CurrentLinesText, Is.EqualTo(shown), "Took the new.");
            Assert.That(locket.Lines.Count, Is.EqualTo(3));
            Tap(_anvil.AddLine);
            Assert.That(locket.Lines.Count, Is.EqualTo(4), "A Binding Rune's line.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheBagEquipsMeltsTwiceAndInherits()
        {
            yield return OpenForge(AnvilFixtures.Profile());
            Tap(_anvil.BagEntry);
            yield return Settle();
            ProfileGear gear = _setup.Profile.Gear;
            Assert.That(_anvil.BagCountText, Is.EqualTo("Bag  8 / 30"));

            ItemInstance hauberk = gear.FindItem(5);
            _anvil.PickBagItem(hauberk);
            Tap(_anvil.EquipButton);
            Assert.That(gear.Equipped(GearSlot.Armour), Is.SameAs(hauberk));

            ItemInstance signet = gear.FindItem(8);
            _anvil.PickBagItem(signet);
            int gold = _setup.Profile.Gold;
            Tap(_anvil.MeltButton);
            Assert.That(gear.Owns(signet), Is.True, "The first tap only asks.");
            Assert.That(_anvil.MeltText, Does.Contain("+20 gold"));
            Tap(_anvil.MeltButton);
            Assert.That(gear.Owns(signet), Is.False);
            Assert.That(_setup.Profile.Gold, Is.EqualTo(gold + 20));

            ItemInstance bark = gear.FindItem(7);
            ItemInstance mail = gear.FindItem(1);
            _anvil.PickBagItem(bark);
            Tap(_anvil.InheritButton);
            _anvil.PickBagItem(mail);
            Assert.That(_anvil.InheritText, Does.Contain("+5"));
            gold = _setup.Profile.Gold;
            Tap(_anvil.InheritButton);
            Assert.That(mail.Level, Is.EqualTo(5));
            Assert.That(gear.Owns(bark), Is.False, "The source is used up.");
            Assert.That(gold - _setup.Profile.Gold, Is.EqualTo(AnvilRule.InheritGold(5)));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AFullBagShowsThirtyAndSaysSo()
        {
            yield return OpenForge(AnvilFixtures.Profile(extraItems: ProfileGear.BagCapacity - 8));
            Tap(_anvil.BagEntry);
            yield return Settle();
            Assert.That(_anvil.BagCountText, Is.EqualTo("Bag  30 / 30"));
            for (int i = 0; i < ProfileGear.BagCapacity; i++)
                Assert.That(_anvil.TileItem(i), Is.Not.Null, "place " + i);
            Assert.That(_anvil.DetailText, Does.Contain("full"));
            LogAssert.NoUnexpectedReceived();
        }

        // Ends the run, opens the result's Forge, and finds the Anvil's panel beside it.
        private IEnumerator OpenForge(PlayerProfile seed)
        {
            TestProfile.Begin(seed);
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            _setup = GameObject.Find("Combat Setup").GetComponent<CombatSetup>();
            _forge = Object.FindFirstObjectByType<RelicForgeView>();
            GameObject.Find("Vanguard").GetComponent<Health>().ApplyDamage(new DamageContext(100000f));
            yield return null;
            yield return new WaitForSecondsRealtime(.6f);
            Tap(GameObject.Find("Forge Button").GetComponent<Button>());
            Assert.That(_forge.IsOpen, Is.True);
            yield return new WaitForSecondsRealtime(.4f);
            _anvil = _forge.Anvil;
            Assert.That(_anvil, Is.Not.Null);
        }

        private static IEnumerator Settle()
        {
            yield return new WaitForSecondsRealtime(AnvilView.InputDelay + .05f);
        }

        // A finger held on the strike for its 0.4 s, then lifted.
        private IEnumerator Hold()
        {
            Assert.That(_anvil.StrikeButton.interactable, Is.True, _anvil.StrikeText);
            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(_anvil.StrikeButton.gameObject, data, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSecondsRealtime(HoldToStrike.Seconds + .15f);
            ExecuteEvents.Execute(_anvil.StrikeButton.gameObject, data, ExecuteEvents.pointerUpHandler);
            yield return null;
        }

        private static void Tap(Button button) =>
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
    }
}
