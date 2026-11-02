using System;
using System.Collections.Generic;
using System.Linq;
using Cryptforge.Combat;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The foundry floor of the Descent simulation (docs/41 step 5): it plays a seed the same way every time, it never
    // stands more enemies than the scene's cap, its levers move only what they name, and where it had to copy a rule of the
    // game to make a lever of it - the horde director's targets and fill rate, the horde's experience share - the copy is
    // held to the game's own body at the shipped values. Balance is not pinned here: the floor is being tuned, and the
    // batch report (Tools/CombatChecks/FoundryBatchReport.cs) reads it.
    public sealed class FoundrySimulationTests
    {
        // Two of the Editor measurement's seeds: a floor is about a second of the pure runner's time.
        private static readonly int[] Seeds = { 11, 33 };

        [Test]
        public void ASeedPlaysTheSameFloorEveryTime()
        {
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            {
                DescentSimulation.FoundryResult first = DescentSimulation.RunFoundry(hero, 11);
                DescentSimulation.FoundryResult second = DescentSimulation.RunFoundry(hero, 11);
                Assert.That(second.FloorSeconds, Is.EqualTo(first.FloorSeconds), hero.Name);
                Assert.That(second.Outcome, Is.EqualTo(first.Outcome), hero.Name);
                Assert.That(second.Picks, Is.EqualTo(first.Picks), hero.Name);
                Assert.That(second.HordeKills, Is.EqualTo(first.HordeKills), hero.Name);
                Assert.That(second.HeroHealth, Is.EqualTo(first.HeroHealth), hero.Name);
                Assert.That(second.RoomSeconds, Is.EqualTo(first.RoomSeconds), hero.Name);
            }
        }

        // The rooms in order, the walks to the districts, the Warden at the gate and the exit held: whatever the outcome,
        // the floor's accounting adds up.
        [Test]
        public void TheFloorIsPlayedThroughItsRoomsAndAccountedForWhole()
        {
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            foreach (int seed in Seeds)
            {
                DescentSimulation.FoundryResult run = DescentSimulation.RunFoundry(hero, seed);
                string where = $"{hero.Name} seed {seed}";
                Assert.That(run.Outcome, Is.Not.EqualTo(DescentSimulation.FoundryOutcome.Timeout), where);
                float rooms = run.RoomSeconds.Where(s => s >= 0f).Sum();
                Assert.That(rooms, Is.EqualTo(run.FloorSeconds).Within(0.05f), where + ": the rooms are the floor");
                Assert.That(run.RoomSeconds[0], Is.GreaterThan(0f), where);
                Assert.That(run.WalkSeconds[0], Is.EqualTo(-1f), where + ": the first hall is fought where the floor begins");
                if (run.Cleared)
                {
                    Assert.That(run.WalkSeconds[1], Is.GreaterThan(0f), where + ": the second hall is walked to");
                    Assert.That(run.BossSeconds, Is.GreaterThan(DescentSimulation.RevealTurnSeconds * 2f + DescentSimulation.RevealHoldSeconds), where);
                    Assert.That(run.ExitSeconds, Is.GreaterThanOrEqualTo(ExitRule.DwellSeconds), where + ": the gate is held");
                    Assert.That(run.LevelAtGate, Is.GreaterThan(0), where);
                    Assert.That(run.Level, Is.GreaterThanOrEqualTo(run.LevelAtGate), where);
                    Assert.That(run.WardenHealth, Is.EqualTo(DescentSimulation.Warden.Health * WardenRule.GateHealthShare * run.DifficultyAtGate).Within(1e-3f), where);
                }
                else
                {
                    Assert.That(run.DeathRoom, Is.InRange(1, run.RoomSeconds.Length), where);
                    Assert.That(run.HeroHealth, Is.EqualTo(0f), where);
                }
                Assert.That(run.Uptime, Is.InRange(0f, 1f), where);
                Assert.That(run.Engaged, Is.InRange(0f, run.Uptime + 1e-4f), where + ": engaged is a part of the uptime");
                Assert.That(run.HordeMaxAlive + PackLayout.MaxFormationSize, Is.LessThanOrEqualTo(HordeRule.SceneCap + PackLayout.MaxFormationSize), where);
                Assert.That(run.HordeMaxAlive, Is.LessThanOrEqualTo(HordeRule.SceneCap), where + ": the scene's cap");
                Assert.That(run.FirstStrikeSeconds, Is.InRange(0f, 10f), where + ": the first hall's first wave is fought at once");
            }
        }

        // The tuned director with no tuning is the game's HordeDirector: frame for frame, over a floor's worth of frames
        // whose crowd, node waves, state and kills are drawn at random, the same spawns, roles and targets.
        [Test]
        public void TheTunedDirectorAtTheShippedValuesIsTheGamesDirector()
        {
            foreach (int seed in new[] { 1, 11, 22, 4242 })
            {
                var shipped = new HordeDirector(HordeRule.SceneCap, 3, seed, true);
                var tuned = new TunedHordeDirector(HordeRule.SceneCap, 3, seed, null, 1f);
                for (HordeState state = HordeState.Ramp; state <= HordeState.Boss; state++)
                {
                    var weights = new[] { HordeRule.Weight(state, 0), HordeRule.Weight(state, 1), HordeRule.Weight(state, 2) };
                    shipped.SetRoleWeights(state, weights);
                    tuned.SetRoleWeights(state, weights);
                }
                var random = new Random(seed);
                var a = new List<HordeDirector.Spawn>();
                var b = new List<HordeDirector.Spawn>();
                int alive = 0;
                for (int frame = 0; frame < 60 * 400; frame++)
                {
                    if (frame % 600 == 0)
                    {
                        var state = (HordeState)random.Next(4);
                        shipped.SetState(state);
                        tuned.SetState(state);
                    }
                    int deaths = random.Next(100) < 4 ? Math.Min(alive, random.Next(1, 3)) : 0;
                    for (int d = 0; d < deaths; d++)
                    {
                        shipped.Fell();
                        tuned.Fell();
                        alive--;
                    }
                    int other = random.Next(100) < 30 ? random.Next(0, 6) : 0;
                    float delta = random.Next(100) < 2 ? 0.5f : 1f / 60f;
                    shipped.Tick(delta, alive, other, a);
                    tuned.Tick(delta, alive, other, b);
                    Assert.That(b.Select(s => (s.Role, s.Ordinal)), Is.EqualTo(a.Select(s => (s.Role, s.Ordinal))), $"seed {seed} frame {frame}");
                    Assert.That(tuned.TargetNow, Is.EqualTo(shipped.TargetNow), $"seed {seed} frame {frame}");
                    Assert.That(tuned.Seconds, Is.EqualTo(shipped.Seconds), $"seed {seed} frame {frame}");
                    Assert.That(tuned.HealthMultiplier, Is.EqualTo(shipped.HealthMultiplier), $"seed {seed} frame {frame}");
                    Assert.That(tuned.DamageMultiplier, Is.EqualTo(shipped.DamageMultiplier), $"seed {seed} frame {frame}");
                    alive += a.Count;
                }
                Assert.That(tuned.Spawned, Is.EqualTo(shipped.Spawned), $"seed {seed}");
            }
        }

        // The copy of HordeRule.EffectiveTarget's body over given crowds is the original at the shipped crowds, over every
        // state, a window's kills and spawns, and the floor's clock to past its overrun.
        [Test]
        public void TheTunedTargetAtTheShippedCrowdsIsTheGamesTarget()
        {
            int[] shipped = (int[])HordeRule.TargetByState.Clone();
            for (HordeState state = HordeState.Ramp; state <= HordeState.Boss; state++)
            for (int kills = 0; kills <= 24; kills += 3)
            for (int spawns = 0; spawns <= 24; spawns += 4)
            for (float clock = 0f; clock <= 330f; clock += 7.3f)
                Assert.That(TunedHordeDirector.EffectiveTarget(shipped, state, kills, spawns, HordeRule.SceneCap, clock),
                    Is.EqualTo(HordeRule.EffectiveTarget(state, kills, spawns, HordeRule.SceneCap, clock)),
                    $"{state} {kills}/{spawns} at {clock}");
        }

        [Test]
        public void TheTunedExperienceAtTheShippedShareIsTheGamesExperience()
        {
            for (int authored = 0; authored <= 1000; authored++)
            {
                Assert.That(TunedHordeExperience.Copy(authored, HordeRule.ExperienceShare), Is.EqualTo(HordeRule.Experience(authored)), $"{authored}");
                Assert.That(TunedHordeExperience.Experience(authored, HordeRule.ExperienceShare), Is.EqualTo(HordeRule.Experience(authored)), $"{authored}");
            }
            // The research's step: at 0.1 a Grunt and a Runner pay 1, as a Mite does.
            Assert.That(TunedHordeExperience.Experience(10, 0.1f), Is.EqualTo(1));
            Assert.That(TunedHordeExperience.Experience(1, 0.1f), Is.EqualTo(1));
        }

        // Forcing the tuned director on with the shipped crowds and rate plays the floor the game's director plays, to the
        // frame: so every director lever is a delta against the game's floor and nothing else.
        [Test]
        public void TheFloorWithTheTunedDirectorAtTheShippedValuesIsTheShippedFloor()
        {
            DescentSimulation.FoundryLevers tuned = FoundryBatch.Shipped().Copy("tuned at shipped");
            tuned.HordeTargets = (int[])HordeRule.TargetByState.Clone();
            Assert.That(tuned.TunesDirector, Is.True);
            foreach (int seed in Seeds)
            {
                DescentSimulation.FoundryResult shipped = DescentSimulation.RunFoundry(DescentSimulation.CinderArcanist(), seed);
                DescentSimulation.FoundryResult copy = DescentSimulation.RunFoundry(DescentSimulation.CinderArcanist(), seed, tuned);
                Assert.That(copy.FloorSeconds, Is.EqualTo(shipped.FloorSeconds), $"seed {seed}");
                Assert.That(copy.Picks, Is.EqualTo(shipped.Picks), $"seed {seed}");
                Assert.That(copy.HordeSpawned, Is.EqualTo(shipped.HordeSpawned), $"seed {seed}");
            }
        }

        // Each lever does what it names and the floor it makes is still played through.
        [Test]
        public void TheLeversChangeWhatTheyName()
        {
            const int seed = 33;
            DescentSimulation.FoundryHero ilya = DescentSimulation.CinderArcanist();
            DescentSimulation.FoundryResult shipped = DescentSimulation.RunFoundry(ilya, seed);
            DescentSimulation.FoundryResult Run(Action<DescentSimulation.FoundryLevers> set)
            {
                DescentSimulation.FoundryLevers levers = FoundryBatch.Shipped().Copy("lever");
                set(levers);
                return DescentSimulation.RunFoundry(ilya, seed, levers);
            }

            DescentSimulation.FoundryResult elite = Run(l => l.ExtraElites += 1);
            Assert.That(elite.Kinds.Count(k => k == RoomKind.Elite), Is.EqualTo(shipped.Kinds.Count(k => k == RoomKind.Elite) + 1),
                "one more elite room");
            Assert.That(elite.Kinds, Is.EqualTo(new[] { RoomKind.Combat, RoomKind.Combat, RoomKind.Combat, RoomKind.Forge, RoomKind.Elite, RoomKind.Elite, RoomKind.Elite, RoomKind.Boss }),
                "the copies stand before the gate");

            DescentSimulation.FoundryResult halls = Run(l => l.ExtraHalls = 2);
            Assert.That(halls.Kinds.Count(k => k == RoomKind.Combat), Is.EqualTo(5));

            DescentSimulation.FoundryResult surges = Run(l => l.HallSurges += 2);
            Assert.That(surges.WaveKills, Is.GreaterThan(shipped.WaveKills), "two more surges a hall send more node enemies");

            DescentSimulation.FoundryResult held = Run(l => l.HallHoldSeconds = 45f);
            for (int room = 0; room < 3; room++)
                if (held.RoomSeconds[room] >= 0f && (held.Cleared || room + 1 < held.DeathRoom))
                    Assert.That(held.RoomSeconds[room] - Math.Max(0f, held.WalkSeconds[room]), Is.GreaterThanOrEqualTo(45f), $"hall {room + 1} is held");

            DescentSimulation.FoundryResult warden = Run(l => l.WardenHealthShare = 7.5f);
            if (warden.LevelAtGate >= 0)
                Assert.That(warden.WardenHealth / warden.DifficultyAtGate, Is.EqualTo(DescentSimulation.Warden.Health * 7.5f).Within(1e-3f));

            DescentSimulation.FoundryResult gate = Run(l => l.GateNotBeforeSeconds = 240f);
            if (gate.LevelAtGate >= 0)
                Assert.That(gate.ClockAtGate, Is.GreaterThanOrEqualTo(240f), "the gate waits for the clock");

            DescentSimulation.FoundryResult thin = Run(l => l.HordeExperienceShare = 0.1f);
            Assert.That(thin.Outcome, Is.Not.EqualTo(DescentSimulation.FoundryOutcome.Timeout));
        }

        // The research's levers each name a value the shipped floor does not have, so every lever run is another floor.
        [Test]
        public void EveryResearchLeverMovesAKnob()
        {
            DescentSimulation.FoundryLevers shipped = FoundryBatch.Shipped();
            foreach (FoundryBatch.Lever lever in FoundryBatch.ResearchLevers())
            {
                DescentSimulation.FoundryLevers l = lever.Levers;
                bool moves = l.HallSurges != shipped.HallSurges || l.ExtraElites != shipped.ExtraElites ||
                             l.ExtraHalls != shipped.ExtraHalls || l.WardenHealthShare != shipped.WardenHealthShare ||
                             l.HordeExperienceShare != shipped.HordeExperienceShare ||
                             l.HordeHealthShare != shipped.HordeHealthShare || l.TunesDirector ||
                             l.NodeHealthScale != shipped.NodeHealthScale || l.HallHoldSeconds != shipped.HallHoldSeconds ||
                             l.GateNotBeforeSeconds != shipped.GateNotBeforeSeconds;
                Assert.That(moves, Is.True, lever.Name);
            }
        }
    }
}
