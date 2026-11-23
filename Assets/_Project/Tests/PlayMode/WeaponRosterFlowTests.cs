using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    // docs/63's weapon roster in the Gameplay scene, behind development/weapon-roster.txt: each hero's catalogue is its own
    // weapon and the shared ones (the Daggers, the Magma Mortar, the Forge Bow), another hero's own never; the first offer
    // is the one the simulation's roster draws for the same seed; with three weapons held the offers show no fourth; the
    // Mortar and the Bow, picked up from the offers, fire; and Ilya unlocks on the Vanguard's clear of The Foundry, since no
    // card could hand it the Staff. Without the file the catalogue is today's.
    public sealed class WeaponRosterFlowTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const string ArcanistId = "hero_cinder_arcanist";
        private CombatSetup _setup;
        private AttackController _attack;

        [UnitySetUp]
        public IEnumerator Begin()
        {
            TestProfile.Begin();
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator End()
        {
            Time.timeScale = 1f;
            Time.captureDeltaTime = 0f;
            Scene old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Weapon roster cleanup " + System.Guid.NewGuid().ToString("N")));
            if (old.path == ScenePath)
                yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        // The foundry, with the roster's file or without, the Vanguard or Ilya, and a run seed when one is named.
        private IEnumerator Load(bool roster, bool arcanist = false, int seed = 0)
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            if (roster)
                File.WriteAllText(DevelopmentStart.WeaponRosterPath, "on");
            if (arcanist)
                File.WriteAllText(DevelopmentStart.HeroPath, ArcanistId);
            if (seed != 0)
                File.WriteAllText(DevelopmentStart.RunSeedPath, seed.ToString(CultureInfo.InvariantCulture));
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            _setup = Object.FindFirstObjectByType<CombatSetup>();
            _attack = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<AttackController>();
        }

        private string[] Catalogue() => _setup.Upgrades.WeaponCards.Select(c => c.Id).ToArray();

        // Answers the open offer with the card choose names (a slot), through the run's choices as a tap does.
        private void Answer(System.Func<UpgradeOffer, int> choose)
        {
            UpgradeOffer offer = _setup.Upgrades.CurrentOffer;
            Assert.That(_setup.Choices.TrySelect(_setup.Choices.Current, choose(offer)), Is.True);
        }

        private static int SlotOf(UpgradeOffer offer, System.Predicate<UpgradeOption> wanted)
        {
            for (int i = 0; i < offer.Choices.Count; i++)
                if (wanted(offer.Choices[i]))
                    return i;
            return -1;
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator WithoutTheFileEveryHeroDrawsFromTodaysCatalogue()
        {
            yield return Load(false);
            Assert.That(_setup.Upgrades.IsCategoryMode, Is.True);
            Assert.That(Catalogue(), Is.EqualTo(new[] { "weapon_sword", "weapon_staff", "weapon_daggers" }));
            Assert.That(DevelopmentStart.WeaponRoster(), Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TheRosterKeepsTheVanguardsSwordToItAndIlyasStaffAway()
        {
            yield return Load(true);
            Assert.That(_setup.HeroWeapon.Id, Is.EqualTo("weapon_sword"));
            Assert.That(Catalogue(), Is.EqualTo(new[]
                {
                    "weapon_sword", "weapon_daggers", WeaponRoster.MagmaMortarId, WeaponRoster.ForgeBowId, WeaponRoster.CinderSawId
                }), "Its own Sword, then the shared weapons; never Ilya's Staff.");
            Assert.That(_setup.RosterSignatures(), Is.EqualTo(DescentSimulation.RosterSignatures), "The simulation's signatures.");
            Assert.That(Catalogue(), Is.EqualTo(DescentSimulation.RosterCatalogue("weapon_sword").Select(w => w.Id).ToArray()),
                "The simulation's roster catalogue, in its order.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TheRosterKeepsIlyasStaffToItAndTheVanguardsSwordAway()
        {
            yield return Load(true, arcanist: true);
            Assert.That(_setup.HeroWeapon.Id, Is.EqualTo("weapon_staff"));
            Assert.That(Catalogue(), Is.EqualTo(new[]
            {
                "weapon_staff", "weapon_daggers", WeaponRoster.MagmaMortarId, WeaponRoster.ForgeBowId, WeaponRoster.CinderSawId
            }));
            Assert.That(Catalogue(), Is.EqualTo(DescentSimulation.RosterCatalogue("weapon_staff").Select(w => w.Id).ToArray()));
            LogAssert.NoUnexpectedReceived();
        }

        // Parity: the scene's first offer under the roster - a bonus level-up granted before anything else happens - is the
        // offer the simulation's roster run draws for the same seed: the same catalogue in the same order, the same pool,
        // the same tomes, the same streams.
        [UnityTest, Timeout(90000)]
        public IEnumerator TheRostersFirstOfferIsTheSimulations()
        {
            const int seed = 6311;
            yield return Load(true, seed: seed);
            Assert.That(_setup.Run.Seed, Is.EqualTo(seed));
            Assert.That(_setup.Upgrades.CurrentOffer, Is.Null, "Nothing is offered before a level.");
            _setup.Run.GrantBonusUpgrade();
            UpgradeOffer scene = _setup.Upgrades.CurrentOffer;
            Assert.That(scene, Is.Not.Null);

            var run = new RunState(DescentSimulation.ExperiencePerLevel, .5f, DescentSimulation.ExperienceGrowth, seed);
            DescentSimulation.HeroWeapon sword = DescentSimulation.Sword();
            WeaponRuntime primary = sword.CreateCategoryRuntime();
            var loadout = new WeaponLoadout(seed);
            loadout.TryAdd(sword.Id, primary);
            WeaponCard[] cards = DescentSimulation.RosterCatalogue(sword.Id).Select(w => w.CreateCard()).ToArray();
            var simulated = new UpgradeService(run, primary, DescentSimulation.Pool(), DescentSimulation.FoundryChoiceCount,
                new HeroStats(100f, 2.5f, HeroStamina.DefaultBar), HeroStamina.CurrentRulesEnabled, loadout, cards,
                DescentSimulation.Tomes());
            run.GrantBonusUpgrade();
            UpgradeOffer expected = simulated.CurrentOffer;
            Assert.That(scene.Choices.Select(c => c.Id).ToArray(), Is.EqualTo(expected.Choices.Select(c => c.Id).ToArray()));
            for (int i = 0; i < expected.Choices.Count; i++)
                Assert.That(scene.RarityAt(i), Is.EqualTo(expected.RarityAt(i)), $"card {i}");
            LogAssert.NoUnexpectedReceived();
        }

        // The full-slot rule in the scene, on a catalogue where a fourth weapon exists: the hero takes every new weapon it
        // is shown until three are held, and then twenty more offers show none, while the three held keep levelling.
        [UnityTest, Timeout(120000)]
        public IEnumerator OnceThreeWeaponsAreHeldTheScenesOffersShowNoFourth()
        {
            yield return Load(true, seed: 2311);
            _attack.enabled = false;
            Object.FindFirstObjectByType<EncounterController>().enabled = false;
            UpgradeService upgrades = _setup.Upgrades;
            for (int i = 0; i < 80 && upgrades.Loadout.Count < WeaponLoadout.MaxSlots; i++)
            {
                if (upgrades.CurrentOffer == null)
                    _setup.Run.GrantBonusUpgrade();
                Answer(offer => System.Math.Max(0, SlotOf(offer, c => c.Category == OfferCategory.NewWeapon)));
            }
            Assert.That(upgrades.Loadout.Count, Is.EqualTo(WeaponLoadout.MaxSlots), "Three held within eighty offers.");
            Assert.That(_attack.WeaponCount, Is.EqualTo(WeaponLoadout.MaxSlots), "Each joined the hero's attack.");
            bool levelled = false;
            for (int i = 0; i < 20; i++)
            {
                if (upgrades.CurrentOffer == null)
                    _setup.Run.GrantBonusUpgrade();
                UpgradeOffer offer = upgrades.CurrentOffer;
                foreach (UpgradeOption card in offer.Choices)
                {
                    Assert.That(card.Category, Is.Not.EqualTo(OfferCategory.NewWeapon), $"offer {offer.Index}: {card.Id}");
                    levelled |= card.Category == OfferCategory.WeaponUpgrade;
                }
                Answer(o => 0);
            }
            Assert.That(upgrades.Loadout.Count, Is.EqualTo(WeaponLoadout.MaxSlots));
            Assert.That(levelled, Is.True, "The weapons held were still offered their levels.");
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        // The Mortar and the Bow taken from the offers (any other offer answered with a card that is not a weapon) join the
        // hero's attack and strike the foundry's enemies, each through its own runtime.
        [UnityTest, Timeout(180000)]
        public IEnumerator ThePickedUpMortarAndBowStrikeInTheScene()
        {
            yield return Load(true, seed: 4242);
            // The hero cannot fall while it waits for both to strike.
            _setup.HeroStats.AddModifier(UpgradeStat.MaxHealth, new StatModifier(ModifierOperation.Flat, 1000000f));
            _attack.GetComponent<Health>().Heal(1000000f);
            UpgradeService upgrades = _setup.Upgrades;
            var wanted = new HashSet<string> { WeaponRoster.MagmaMortarId, WeaponRoster.ForgeBowId };
            for (int i = 0; i < 120 && wanted.Any(id => !upgrades.Loadout.Owns(id)); i++)
            {
                if (upgrades.CurrentOffer == null)
                    _setup.Run.GrantBonusUpgrade();
                Answer(offer =>
                {
                    int take = SlotOf(offer, c => c.Category == OfferCategory.NewWeapon && wanted.Contains(c.Id));
                    if (take >= 0)
                        return take;
                    take = SlotOf(offer, c => c.Category != OfferCategory.NewWeapon);
                    return take >= 0 ? take : 0;
                });
            }
            Assert.That(upgrades.Loadout.Owns(WeaponRoster.MagmaMortarId) && upgrades.Loadout.Owns(WeaponRoster.ForgeBowId), Is.True,
                "Both offered and taken within 120 offers.");
            WeaponRuntime mortar = upgrades.Loadout.At(upgrades.Loadout.IndexOf(WeaponRoster.MagmaMortarId)).Runtime;
            WeaponRuntime bow = upgrades.Loadout.At(upgrades.Loadout.IndexOf(WeaponRoster.ForgeBowId)).Runtime;
            Assert.That((mortar.Pattern.Behavior, mortar.Range), Is.EqualTo((WeaponBehavior.Area, 4f)));
            Assert.That((bow.Pattern.Behavior, bow.Range), Is.EqualTo((WeaponBehavior.DirectHit, 5f)));
            int mortarStrikes = 0, bowStrikes = 0;
            float farthestShot = 0f;
            System.Action<Health, WeaponRuntime> count = (target, weapon) =>
            {
                if (weapon == mortar)
                    mortarStrikes++;
                if (weapon == bow)
                {
                    bowStrikes++;
                    farthestShot = Mathf.Max(farthestShot, Vector3.Distance(target.transform.position, _attack.transform.position));
                }
            };
            _attack.StruckWith += count;
            try
            {
                for (int frame = 0; frame < 60 * 60 && (mortarStrikes == 0 || bowStrikes == 0); frame++)
                {
                    yield return null;
                    if (_setup.Choices.IsOpen)
                        _setup.Choices.TrySelect(_setup.Choices.Current, 0);
                }
            }
            finally
            {
                _attack.StruckWith -= count;
            }
            Assert.That(mortarStrikes, Is.GreaterThan(0), "The Mortar struck.");
            Assert.That(bowStrikes, Is.GreaterThan(0), "The Bow struck.");
            Assert.That(mortar.AttacksMade, Is.GreaterThan(0));
            Assert.That(bow.AttacksMade, Is.GreaterThan(0));
            LogAssert.NoUnexpectedReceived();
        }

        // docs/63 section 13's Cinder Saw in the scene: taken from an offer, it joins the hero's attack as an aura - every
        // strike's target within its circle round the hero (TargetReach, not the Range a target-centred weapon reaches with),
        // every enemy standing inside that circle at the strike taking the full blow, none outside it - and its blade circles
        // the hero (CombatEffectsView) just inside the circle while it is held.
        [UnityTest, Timeout(180000)]
        public IEnumerator ThePickedUpSawStrikesRoundTheHeroAndItsBladeCircles()
        {
            yield return Load(true, seed: 5150);
            _setup.HeroStats.AddModifier(UpgradeStat.MaxHealth, new StatModifier(ModifierOperation.Flat, 1000000f));
            _attack.GetComponent<Health>().Heal(1000000f);
            UpgradeService upgrades = _setup.Upgrades;
            var effects = Object.FindFirstObjectByType<CombatEffectsView>();
            Assert.That(effects.SawCircling, Is.False, "No blade before the Saw is held.");
            for (int i = 0; i < 120 && !upgrades.Loadout.Owns(WeaponRoster.CinderSawId); i++)
            {
                if (upgrades.CurrentOffer == null)
                    _setup.Run.GrantBonusUpgrade();
                Answer(offer =>
                {
                    int take = SlotOf(offer, c => c.Category == OfferCategory.NewWeapon && c.Id == WeaponRoster.CinderSawId);
                    if (take >= 0)
                        return take;
                    take = SlotOf(offer, c => c.Category != OfferCategory.NewWeapon);
                    return take >= 0 ? take : 0;
                });
            }
            Assert.That(upgrades.Loadout.Owns(WeaponRoster.CinderSawId), Is.True, "Offered and taken within 120 offers.");
            WeaponRuntime saw = upgrades.Loadout.At(upgrades.Loadout.IndexOf(WeaponRoster.CinderSawId)).Runtime;
            Assert.That((saw.Pattern.Behavior, saw.TargetReach, saw.Interval), Is.EqualTo((WeaponBehavior.Aura, 2f, .2f)));
            float FloorDistance(Vector3 at) =>
                Mathf.Sqrt(ArenaFloor.DistanceSquared(at.x - _attack.transform.position.x, at.y - _attack.transform.position.y));
            int strikes = 0;
            int circling = 0;
            System.Action<Health, WeaponRuntime> count = (target, weapon) =>
            {
                if (weapon != saw)
                    return;
                strikes++;
                Assert.That(FloorDistance(target.transform.position), Is.LessThanOrEqualTo(saw.TargetReach + 1e-3f),
                    "Its target inside its circle.");
            };
            _attack.StruckWith += count;
            try
            {
                for (int frame = 0; frame < 60 * 60 && strikes < 10; frame++)
                {
                    yield return null;
                    if (_setup.Choices.IsOpen)
                        _setup.Choices.TrySelect(_setup.Choices.Current, 0);
                    if (effects.SawCircling)
                    {
                        // The blade stands SawLift above the floor point it circles through.
                        float outward = FloorDistance(effects.SawPosition - Vector3.up * .18f);
                        Assert.That(outward, Is.InRange(saw.SplashRadius * .8f, saw.SplashRadius * .9f), "The blade just inside the circle.");
                        circling++;
                    }
                }
            }
            finally
            {
                _attack.StruckWith -= count;
            }
            Assert.That(strikes, Is.GreaterThanOrEqualTo(10), "The Saw struck again and again.");
            Assert.That(circling, Is.GreaterThan(0), "Its blade circled the hero while it was held.");
            Assert.That(saw.AttacksMade, Is.GreaterThanOrEqualTo(10));
            LogAssert.NoUnexpectedReceived();
        }

        // Ilya's unlock (docs/63 section 6): on the foundry under the roster the Vanguard's cards can never hand it the
        // Staff, so its clear of The Foundry alone unlocks Ilya; with today's catalogue the Staff could come, and the Sword
        // alone unlocks nothing.
        [UnityTest, Timeout(90000)]
        public IEnumerator UnderTheRosterTheVanguardsClearOfTheFoundryUnlocksIlya()
        {
            yield return Load(true);
            Assert.That(_setup.Profile.HasUnlockedHero(ArcanistId), Is.False);
            _setup.UnlockHeroesForTheFoundry();
            Assert.That(_setup.Profile.HasUnlockedHero(ArcanistId), Is.True, "No card could hand over the Staff.");
            Assert.That(TestProfile.ReadSaved().HasUnlockedHero(ArcanistId), Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator WithTodaysCatalogueTheVanguardsSwordAloneUnlocksNothing()
        {
            yield return Load(false);
            _setup.UnlockHeroesForTheFoundry();
            Assert.That(_setup.Profile.HasUnlockedHero(ArcanistId), Is.False, "The Staff could have come as a card, and did not.");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
