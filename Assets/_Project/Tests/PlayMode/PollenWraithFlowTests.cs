using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // The Pollen Wraith on the foundry (docs/52 section 3; the lead's decision 2026-11-17): the Cinder Mite reskinned, no
    // pollen. A Descent started on floor 3 (the Gloam Gardens) fields Wraiths in its horde's role 0 and in its three halls'
    // waves - Ash Orchard, Rootbound Walk, Bloom Gate - each standing in PollenWraithArtSet itself (born on floor 3, it has
    // no bloomed copy) with the Mite's look and the Mite Bite, while Thorn Post's escort stays two Cinder Mites in their
    // bloomed copy and no Mite walks in the horde. Floors 1 and 2 still field Cinder Mites, in the painted set and in the
    // frosted copy, and no Wraith. The run is driven as FoundryRunDriver drives one - the hero placed by hand, every offer
    // answered - with every wave and horde body felled by the test, so the horde keeps standing new bodies up.
    public sealed class PollenWraithFlowTests
    {
        private const string WraithId = "enemy_pollen_wraith";
        private const string MiteId = "enemy_cinder_mite";
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        [UnitySetUp] public IEnumerator Begin() { TestProfile.Begin(); Time.timeScale = 1; yield return null; }

        [UnityTearDown]
        public IEnumerator End()
        {
            Time.timeScale = 1;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Pollen Wraith cleanup " + System.Guid.NewGuid().ToString("N")));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        // The painted set a definition's prefab wears (EnemyLookView._paintedArt), before any floor's copy of it.
        private static EnemyArtSet PaintedSetOf(EnemyDefinition definition)
        {
            var look = definition.Prefab.GetComponent<EnemyLookView>();
            Assert.That(look, Is.Not.Null, definition.Id);
            return (EnemyArtSet)typeof(EnemyLookView).GetField("_paintedArt", Private).GetValue(look);
        }

        // Every horde body felled as it stands, so the horde keeps standing new ones up.
        private static void FellTheHorde(EncounterController encounters)
        {
            for (int i = 0; i < encounters.HordeEnemyCount; i++)
            {
                Health enemy = encounters.HordeEnemyAt(i);
                if (enemy != null && enemy.IsAlive)
                    enemy.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            }
        }

        // Frames until the floor's first wave stands, every offer answered and the hero kept standing.
        private static IEnumerator UntilTheFirstWaveStands(CombatSetup setup, Health hero)
        {
            yield return FoundryRunDriver.Until(setup, hero, () => setup.Encounters.AliveEnemyCount > 0, 20f);
            Assert.That(setup.Encounters.AliveEnemyCount, Is.GreaterThan(0), "The floor's first wave stands.");
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator FloorThreesHallsAndHordeFieldPollenWraithsInTheirOwnSet()
        {
            yield return FoundryRunDriver.Load("floor_gloam_gardens");
            CombatSetup setup = FoundryRunDriver.Setup;
            EncounterController encounters = setup.Encounters;
            Health hero = FoundryRunDriver.Hero;
            FloorDefinition floor = encounters.Floor;
            Assert.That(floor.Id, Is.EqualTo("floor_gloam_gardens"), "The Descent starts on floor 3.");
            Assert.That(floor.Theme, Is.EqualTo(FloorTheme.Gloam));

            EnemyDefinition wraith = floor.HordeRoleAt(0);
            Assert.That(wraith.Id, Is.EqualTo(WraithId), "The horde's role 0 is the Wraith.");
            EnemyArtSet wraithSet = PaintedSetOf(wraith);
            Assert.That(wraithSet, Is.Not.Null);
            Assert.That(wraithSet.name, Is.EqualTo("PollenWraithArtSet"));
            Assert.That(wraithSet.ForTheme(FloorTheme.Gloam), Is.Null, "Born on floor 3: no bloomed copy.");
            RoomDefinition[] rooms = EncounterController.FoundryRooms(floor);
            EnemyDefinition mite = rooms[4].WaveAt(0).EnemyAt(2);
            Assert.That(mite.Id, Is.EqualTo(MiteId), "Thorn Post's escort keeps its Cinder Mites.");
            EnemyArtSet miteBloomed = PaintedSetOf(mite).ForTheme(FloorTheme.Gloam);
            Assert.That(miteBloomed, Is.Not.Null, "The Mite's bloomed copy.");

            // Every body that stands up: a Wraith in its own set with the Mite's look and bite, a Mite in its bloomed copy.
            // What does not is noted, and asserted once the run has been driven.
            var waves = new List<string>();
            var wrong = new List<string>();
            int hordeWraiths = 0, hordeMites = 0, waveWraiths = 0, waveMites = 0;
            string Check(Health body, string where)
            {
                EnemyDefinition definition = encounters.DefinitionOf(body);
                if (definition == null)
                {
                    wrong.Add(where + ": no definition");
                    return null;
                }
                var look = body.GetComponent<EnemyLookView>();
                if (definition.Id == WraithId)
                {
                    AttackController attack = body.GetComponent<AttackController>();
                    if (!ReferenceEquals(definition, wraith))
                        wrong.Add(where + ": another Wraith definition");
                    if (!ReferenceEquals(look.PaintedArt, wraithSet))
                        wrong.Add(where + ": a Wraith in " + (look.PaintedArt != null ? look.PaintedArt.name : "no set"));
                    if (look.Look != EnemyLook.Mite)
                        wrong.Add(where + ": a Wraith with the " + look.Look + " look");
                    if (attack.Weapon.Range != mite.Weapon.Range || attack.Weapon.Interval != mite.Weapon.Interval)
                        wrong.Add(where + ": a Wraith without the Mite Bite");
                }
                else if (definition.Id == MiteId && !ReferenceEquals(look.PaintedArt, miteBloomed))
                {
                    wrong.Add(where + ": a Mite in " + (look.PaintedArt != null ? look.PaintedArt.name : "no set"));
                }
                return definition.Id;
            }
            void Record()
            {
                string wave = PackTestUtility.Describe(encounters);
                waves.Add(wave);
                for (int i = 0; i < encounters.WaveEnemyCount; i++)
                {
                    string id = Check(encounters.WaveEnemyAt(i), wave + " enemy " + (i + 1));
                    if (id == WraithId) waveWraiths++;
                    if (id == MiteId) waveMites++;
                }
            }
            System.Action started = Record;
            System.Action<int> spawned = slot =>
            {
                string id = Check(encounters.HordeEnemyAt(slot), "horde slot " + slot);
                if (id == WraithId) hordeWraiths++;
                if (id == MiteId) hordeMites++;
            };

            encounters.EncounterStarted += started;
            encounters.HordeEnemySpawned += spawned;
            try
            {
                // Ash Orchard's first wave may have stood up before the test listened.
                if (encounters.AliveEnemyCount > 0 && waves.Count == 0)
                    Record();
                // The halls, the forge and on to Thorn Post's wave standing, the horde felled throughout; then the horde alone
                // until a few Wraiths have stood up in it.
                float deadline = Time.realtimeSinceStartup + 240f;
                while (!setup.Run.HasEnded && Time.realtimeSinceStartup < deadline)
                {
                    bool atThornPost = encounters.CurrentRoom != null && encounters.CurrentRoom.Kind == RoomKind.Elite &&
                                       encounters.AliveEnemyCount > 0;
                    if (atThornPost && hordeWraiths >= 3)
                        break;
                    FoundryRunDriver.Keep(setup, hero);
                    if (encounters.IsRoomSiteOpen)
                        FoundryRunDriver.Place(hero, encounters.RoomSiteFloorX, encounters.RoomSiteFloorY);
                    else if (!atThornPost)
                        PackTestUtility.KillWave(encounters);
                    FellTheHorde(encounters);
                    yield return null;
                }
            }
            finally
            {
                encounters.EncounterStarted -= started;
                encounters.HordeEnemySpawned -= spawned;
            }

            Assert.That(setup.Run.HasEnded, Is.False, "The run stands.");
            Assert.That(wrong, Is.Empty);
            Assert.That(waves, Is.EqualTo(new[]
            {
                "1.1:Ash Orchard:Grunt+Pollen Wraith+Pollen Wraith+Pollen Wraith",
                "1.2:Ash Orchard:Runner+Pollen Wraith+Pollen Wraith+Pollen Wraith",
                "2.1:Rootbound Walk:Tank+Pollen Wraith+Pollen Wraith+Pollen Wraith",
                "2.2:Rootbound Walk:Pollen Wraith+Pollen Wraith+Pollen Wraith+Pollen Wraith+Pollen Wraith",
                "3.1:Bloom Gate:Grunt+Grunt+Pollen Wraith+Pollen Wraith+Pollen Wraith",
                "5.1:Thorn Post:Cinder Bloom+Grunt+Cinder Mite+Cinder Mite"
            }), "The halls' Mites are Wraiths; Thorn Post keeps its two Cinder Mites.");
            Assert.That((waveWraiths, waveMites), Is.EqualTo((17, 2)));
            Assert.That(hordeWraiths, Is.GreaterThanOrEqualTo(3), "The horde stands Wraiths up.");
            Assert.That(hordeMites, Is.Zero, "No Cinder Mite walks in floor 3's horde.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator FloorsOneAndTwoStillFieldCinderMites()
        {
            yield return FloorFieldsMites(null, "floor_ember_halls", FloorTheme.Foundry);
            yield return FloorFieldsMites("floor_quicksilver_vaults", "floor_quicksilver_vaults", FloorTheme.Ice);
        }

        // A Descent started on the floor (null: the shipped start, floor 1): its first wave's Mites and the horde's role-0
        // bodies are Cinder Mites in the floor's copy of the Mite's set (the painted set itself on the foundry's floor), and
        // no Wraith stands anywhere.
        private static IEnumerator FloorFieldsMites(string start, string id, FloorTheme theme)
        {
            yield return FoundryRunDriver.Load(start);
            CombatSetup setup = FoundryRunDriver.Setup;
            EncounterController encounters = setup.Encounters;
            Health hero = FoundryRunDriver.Hero;
            FloorDefinition floor = encounters.Floor;
            Assert.That(floor.Id, Is.EqualTo(id));
            Assert.That(floor.Theme, Is.EqualTo(theme));
            EnemyDefinition mite = floor.HordeRoleAt(0);
            Assert.That(mite.Id, Is.EqualTo(MiteId), id + ": the horde's role 0 is the Mite.");
            EnemyArtSet painted = PaintedSetOf(mite);
            Assert.That(painted.name, Is.EqualTo("CinderMiteArtSet"));
            EnemyArtSet shown = theme == FloorTheme.Foundry ? painted : painted.ForTheme(theme);
            Assert.That(shown, Is.Not.Null, id + ": the floor's copy of the Mite's set.");

            var wrong = new List<string>();
            int waveMites = 0, hordeMites = 0, wraiths = 0;
            yield return UntilTheFirstWaveStands(setup, hero);
            for (int i = 0; i < encounters.WaveEnemyCount; i++)
            {
                Health body = encounters.WaveEnemyAt(i);
                string kind = encounters.DefinitionOf(body).Id;
                if (kind == WraithId) wraiths++;
                if (kind != MiteId) continue;
                waveMites++;
                if (!ReferenceEquals(body.GetComponent<EnemyLookView>().PaintedArt, shown))
                    wrong.Add($"{id} wave enemy {i + 1}");
            }
            Assert.That(waveMites, Is.GreaterThan(0), id + ": the first wave brings Cinder Mites.");

            System.Action<int> spawned = slot =>
            {
                Health body = encounters.HordeEnemyAt(slot);
                string kind = encounters.DefinitionOf(body)?.Id;
                if (kind == WraithId) wraiths++;
                if (kind != MiteId) return;
                hordeMites++;
                if (!ReferenceEquals(body.GetComponent<EnemyLookView>().PaintedArt, shown))
                    wrong.Add($"{id} horde slot {slot}");
            };
            encounters.HordeEnemySpawned += spawned;
            try
            {
                float deadline = Time.realtimeSinceStartup + 60f;
                while (hordeMites < 3 && !setup.Run.HasEnded && Time.realtimeSinceStartup < deadline)
                {
                    FoundryRunDriver.Keep(setup, hero);
                    FellTheHorde(encounters);
                    yield return null;
                }
            }
            finally
            {
                encounters.HordeEnemySpawned -= spawned;
            }
            Assert.That(wrong, Is.Empty, id + ": every Mite in the floor's copy of its set.");
            Assert.That(hordeMites, Is.GreaterThanOrEqualTo(3), id + ": the horde stands Cinder Mites up.");
            Assert.That(wraiths, Is.Zero, id + ": no Pollen Wraith.");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
