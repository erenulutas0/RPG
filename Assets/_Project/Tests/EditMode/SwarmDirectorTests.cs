using System;
using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The continuous swarm's rules (docs/27, S6c), each one a thing the scene and the simulation both rely on.
    public sealed class SwarmDirectorTests
    {
        private static readonly int[] Ladder = { 3, 3, 4, 5 };

        [Test]
        public void TheOpeningIsTheLadderFirstWaveWhole()
        {
            var director = new SwarmDirector(Ladder);
            var spawns = new List<SwarmDirector.Spawn>();
            director.Open(spawns);

            Assert.That(spawns.Count, Is.EqualTo(3));
            for (int i = 0; i < 3; i++)
            {
                Assert.That(spawns[i].Slot, Is.EqualTo(i));
                Assert.That((spawns[i].Wave, spawns[i].Index), Is.EqualTo((0, i)));
                Assert.That(spawns[i].Difficulty, Is.EqualTo(1f), "The room opens at the difficulty its floor authored.");
            }
            Assert.That(director.Slots, Is.EqualTo(3));
            Assert.That(director.Spawned, Is.EqualTo(3));
            Assert.Throws<InvalidOperationException>(() => director.Open(spawns), "A swarm opens once.");
        }

        [Test]
        public void AFallenPlaceWaitsItsBreathAndThenTheNextInLineWalksIn()
        {
            var director = new SwarmDirector(Ladder);
            var spawns = new List<SwarmDirector.Spawn>();
            director.Open(spawns);

            director.Fell(1);
            director.Tick(SwarmRule.RespawnSeconds * 0.5f, 2, spawns);
            Assert.That(spawns, Is.Empty, "Half a breath is not enough.");

            director.Tick(SwarmRule.RespawnSeconds * 0.5f, 2, spawns);
            Assert.That(spawns.Count, Is.EqualTo(1));
            Assert.That(spawns[0].Slot, Is.EqualTo(1), "The fallen place is the one that fills.");
            Assert.That((spawns[0].Wave, spawns[0].Index), Is.EqualTo((1, 0)),
                "The second wave's first enemy is next in line after the opening.");
        }

        [Test]
        public void TheCrowdGrowsWithTheLadderAndOnlyByBeingKilled()
        {
            var director = new SwarmDirector(Ladder);
            var spawns = new List<SwarmDirector.Spawn>();
            director.Open(spawns);

            // Nothing dies: nothing new arrives, however long the hero stands there.
            director.Tick(60f, 3, spawns);
            Assert.That(spawns, Is.Empty, "A hero who kills nothing meets nothing new.");

            // Kill through the queue. The cap is the size of the wave the queue has reached: 3, 3, then 4, then 5.
            Assert.That(director.CapAt(0), Is.EqualTo(3));
            Assert.That(director.CapAt(6), Is.EqualTo(4), "Past the first two waves the third's four may stand.");
            Assert.That(director.CapAt(10), Is.EqualTo(5));
            Assert.That(director.CapAt(1000), Is.EqualTo(5), "Past the ladder its last wave's size holds.");

            director.Locate(15, out int wave, out int index);
            Assert.That((wave, index), Is.EqualTo((3, 0)), "Past the end of the ladder its last wave repeats.");
            director.Locate(17, out wave, out index);
            Assert.That((wave, index), Is.EqualTo((3, 2)));
        }

        [Test]
        public void ANewPlaceOpensWhenTheQueueReachesABiggerWave()
        {
            var director = new SwarmDirector(new[] { 2, 3 });
            var spawns = new List<SwarmDirector.Spawn>();
            director.Open(spawns);
            Assert.That(director.Slots, Is.EqualTo(2));

            // Both fall; after a breath both places refill from the second wave, and the second wave holds three, so a
            // third place opens as well.
            director.Fell(0);
            director.Fell(1);
            director.Tick(SwarmRule.RespawnSeconds, 0, spawns);

            Assert.That(director.Slots, Is.EqualTo(3), "The crowd grew to the second wave's size.");
            Assert.That(spawns.Count, Is.EqualTo(3));
            var slots = new List<int>();
            foreach (SwarmDirector.Spawn s in spawns)
                slots.Add(s.Slot);
            Assert.That(slots, Is.EquivalentTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void TheRampRunsOnTheClockAndAnEnemyKeepsTheDifficultyItWasBornWith()
        {
            Assert.That(SwarmRule.DifficultyAtSeconds(0f), Is.EqualTo(1f));
            Assert.That(SwarmRule.DifficultyAtSeconds(SwarmRule.StepSeconds * 0.99f), Is.EqualTo(1f),
                "The first step lasts a whole step.");
            Assert.That(SwarmRule.DifficultyAtSeconds(SwarmRule.StepSeconds * 3f), Is.EqualTo(SwarmRule.DifficultyAt(3)));

            var director = new SwarmDirector(Ladder);
            var spawns = new List<SwarmDirector.Spawn>();
            director.Open(spawns);
            director.Tick(SwarmRule.StepSeconds * 4f, 3, spawns);
            director.Fell(0);
            director.Tick(SwarmRule.RespawnSeconds, 2, spawns);
            Assert.That(spawns.Count, Is.EqualTo(1));
            Assert.That(spawns[0].Difficulty, Is.EqualTo(SwarmRule.DifficultyAtSeconds(director.Seconds)),
                "An enemy that walks in late walks in harder.");
            Assert.That(spawns[0].Difficulty, Is.GreaterThan(1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => SwarmRule.DifficultyAtSeconds(-1f));
        }

        [Test]
        public void TheExitIsHeldNotTouched()
        {
            var arena = new ArenaGeometry(-9f, 9f, 9f);
            ExitRule.SpotFor(arena, out float x, out float y);
            Assert.That(x, Is.Zero);
            Assert.That(y, Is.GreaterThan(ChestRule.PreferredDistance), "The way out stands well beyond the chest.");
            Assert.That(arena.IsOnPlatform(x, y, HeroMotion.EdgeMargin), Is.True, "And it can be reached.");

            // The hold counts only while the hero stands still on it: a frame of moving, on the exit or off it, is a
            // frame of not holding, which is what keeps a kiting hero from leaving by running circles across it.
            var dwell = new ExitDwell();
            Assert.That(dwell.Step(ExitRule.DwellSeconds * 0.6f, true), Is.False);
            Assert.That(dwell.Step(0.1f, false), Is.False, "Moving or stepping off starts the count again.");
            Assert.That(dwell.Held, Is.Zero);
            Assert.That(dwell.Step(ExitRule.DwellSeconds * 0.6f, true), Is.False);
            Assert.That(dwell.Step(ExitRule.DwellSeconds * 0.6f, true), Is.True, "A full second, unbroken, leaves.");
            Assert.That(dwell.Left, Is.True);

            Assert.That(ExitRule.IsWithinReach(x + 0.5f, y, x, y), Is.True);
            Assert.That(ExitRule.IsWithinReach(x + 1.5f, y, x, y), Is.False);

            var small = new ArenaGeometry(-3f, 3f, 3f);
            ExitRule.SpotFor(small, out _, out float smallY);
            Assert.That(small.IsOnPlatform(0f, smallY, HeroMotion.EdgeMargin), Is.True, "A small arena keeps it reachable.");
        }

        [Test]
        public void AFallenPlaceInThePackCanBeFilledAgainAndALivingOneCannot()
        {
            var motion = new PackMotion(0.9f, 1f, new ArenaGeometry(-9f, 9f, 9f), 0f, 0f);
            var first = new HealthState(10f);
            var second = new HealthState(10f);
            int slot = motion.Add(first, new EntryPlacement(EntrySide.Far, 0f, 0f, 5f), 2f, 1.5f);
            Assert.Throws<InvalidOperationException>(
                () => motion.Replace(slot, second, new EntryPlacement(EntrySide.Far, 0f, 0f, 5f), 2f, 1.5f),
                "A living enemy keeps its place.");

            first.ApplyDamage(new DamageContext(10f));
            motion.Replace(slot, second, new EntryPlacement(EntrySide.Near, 0f, 1f, -4f), 2f, 1.5f);
            Assert.That(motion.Count, Is.EqualTo(1), "Filling a place adds no place.");
            Assert.That((motion.XOf(slot), motion.YOf(slot)), Is.EqualTo((1f, -4f)), "The newcomer enters where it was placed.");
            Assert.Throws<ArgumentOutOfRangeException>(
                () => motion.Replace(3, second, new EntryPlacement(EntrySide.Far, 0f, 0f, 5f), 2f, 1.5f));
        }
    }
}
