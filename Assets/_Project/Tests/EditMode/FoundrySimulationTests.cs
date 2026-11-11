using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Cryptforge.Art;
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
                // A strike's or a hit's 1.5 s can outlast the last enemy standing, so engaged is a share of the floor and
                // not a part of the uptime (over the batch it stays under it, docs/41's 2026-11-11 status).
                Assert.That(run.Engaged, Is.InRange(0f, 1f), where + ": engaged is a share of the floor");
                Assert.That(run.Engaged, Is.GreaterThan(0f), where + ": the hero strikes on every floor");
                Assert.That(run.HordeMaxAlive + PackLayout.MaxFormationSize, Is.LessThanOrEqualTo(HordeRule.SceneCap + PackLayout.MaxFormationSize), where);
                Assert.That(run.HordeMaxAlive, Is.LessThanOrEqualTo(HordeRule.SceneCap), where + ": the scene's cap");
                Assert.That(run.FirstStrikeSeconds, Is.InRange(0f, 10f), where + ": the first hall's first wave is fought at once");
            }
        }

        // A run plays floor 1 alone unless its levers ask for more (FoundryLevers.Floors), and asking changes nothing before
        // the checkpoint: a seed's floor 1 is the same floor, field for field, whether or not the run goes on to floor 2. On
        // the floor as it ships both heroes die on floor 1 on seed 2 and clear it on seed 11.
        [Test]
        public void FloorOneIsTheSameFloorWhetherOrNotTheRunGoesOnToFloorTwo()
        {
            Assert.That(FoundryBatch.Shipped().Floors, Is.EqualTo(1), "the shipped floor is floor 1 alone");
            DescentSimulation.FoundryLevers two = FoundryBatch.Shipped();
            two.Floors = 2;
            int wentDown = 0;
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            foreach (int seed in new[] { 2, 11 })
            {
                string where = $"{hero.Name} seed {seed}";
                DescentSimulation.FoundryResult alone = DescentSimulation.RunFoundry(hero, seed);
                DescentSimulation.FoundryResult climb = DescentSimulation.RunFoundry(hero, seed, two);
                Assert.That(alone.NextFloor, Is.Null, where + ": one floor");
                foreach (FieldInfo field in typeof(DescentSimulation.FoundryResult).GetFields(BindingFlags.Public | BindingFlags.Instance))
                    if (field.Name != nameof(DescentSimulation.FoundryResult.NextFloor))
                        AssertSameValue(field.GetValue(climb), field.GetValue(alone), $"{where}: {field.Name}");
                Assert.That(climb.NextFloor != null, Is.EqualTo(climb.Cleared), where + ": a clear goes down, anything else ends the run");
                if (climb.NextFloor != null)
                    wentDown++;
            }
            Assert.That(wentDown, Is.GreaterThan(0), "a run went down to floor 2, so floor 1 was compared with a floor 2 after it");
        }

        // A floor-1 clear goes down to floor 2 as the scene does: after the checkpoint and its advance delay, floor 2's rooms
        // in the asset's order - three halls, the forge, the elite, the Warden - the first fought where the hero stands, the
        // later halls at the districts the climb's round reaches next (the north court, the workyard, the pit's rim; the
        // scene's own route in ClimbFloorContentTests), the gate open as its room clears, its Warden at floor 2's tier and
        // the run's difficulty; the hero carries its level and health down, and the floor's clock starts again.
        [Test]
        public void AFloorOneClearGoesDownToFloorTwoAndPlaysItsRoomsInTheScenesOrder()
        {
            DescentSimulation.FoundryLevers two = FoundryBatch.Shipped();
            two.Floors = 2;
            RoomKind[] rooms = { RoomKind.Combat, RoomKind.Combat, RoomKind.Combat, RoomKind.Forge, RoomKind.Elite, RoomKind.Boss };
            float[][] districts = { new[] { 4f, 12f }, new[] { -25f, 7f }, new[] { -10f, -1.5f } };
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            {
                int wentDown = 0;
                foreach (int seed in Seeds)
                {
                    string where = $"{hero.Name} seed {seed}";
                    var lines = new List<string>();
                    DescentSimulation.FoundryResult first = DescentSimulation.RunFoundry(hero, seed, two, trace: lines.Add);
                    DescentSimulation.FoundryResult run = first.NextFloor;
                    if (run == null)
                        continue;
                    wentDown++;
                    Assert.That(run.Floor, Is.EqualTo(2), where);
                    Assert.That(run.NextFloor, Is.Null, where + ": two floors asked for");
                    Assert.That(run.Outcome, Is.Not.EqualTo(DescentSimulation.FoundryOutcome.Timeout), where);
                    Assert.That(run.Kinds, Is.EqualTo(rooms), where + ": the asset's rooms, no extra elite");

                    // The checkpoint: the hero goes down as floor 1 left it, and the first room waits the advance delay.
                    int descent = lines.FindIndex(l => l.Contains(" floor_descend "));
                    Assert.That(lines[descent], Does.EndWith($"floor=2 level={first.Level} hp={first.HeroHealth.ToString("0.###", CultureInfo.InvariantCulture)}"), where);
                    Assert.That(run.DescentSeconds, Is.EqualTo(DescentSimulation.AdvanceDelay).Within(2.5f / 60f), where);
                    float roomTime = run.RoomSeconds.Where(s => s >= 0f).Sum();
                    Assert.That(roomTime + run.DescentSeconds, Is.EqualTo(run.FloorSeconds).Within(0.05f), where + ": the descent and the rooms are the floor");
                    // Floor 2's accounting on its own seconds: the weapons carried down held from its first frame, its kills
                    // within it.
                    Assert.That(run.WeaponWork.Select(w => w.Id).Take(first.WeaponWork.Count), Is.EqualTo(first.WeaponWork.Select(w => w.Id)), where);
                    Assert.That(run.WeaponWork.Take(first.WeaponWork.Count).All(w => w.HeldFrom < 0.05f), Is.True, where);
                    Assert.That(run.Kills.All(k => k.At >= 0f && k.At <= run.FloorSeconds + 1e-3f), Is.True, where);

                    // Its rooms in order, as far as the run got.
                    var started = lines.Skip(descent).Where(l => l.Contains(" room_start ")).ToList();
                    Assert.That(started.Count, Is.InRange(1, rooms.Length), where);
                    for (int i = 0; i < started.Count; i++)
                        Assert.That(started[i], Does.Contain($"room_start room={i + 1} kind={rooms[i]} "), where);
                    if (run.Cleared)
                        Assert.That(started.Count, Is.EqualTo(rooms.Length), where + ": every room played");
                    Assert.That(run.WalkSeconds[0], Is.EqualTo(-1f), where + ": the first hall is fought where the hero stands");

                    // Every hall reached at its district, the round going on from floor 1's.
                    int hall = 0;
                    for (int i = descent; i < lines.Count; i++)
                    {
                        if (!lines[i].Contains(" hall_reached "))
                            continue;
                        Assert.That(hall, Is.LessThan(districts.Length), where + ": floor 2's later halls");
                        string frame = lines.Skip(i + 1).First(l => l.Contains(" frame "));
                        FoundryMap.ToFloor(districts[hall][0], districts[hall][1], out float x, out float y);
                        float dx = Field(frame, "x") - x, dy = Field(frame, "y") - y;
                        Assert.That(Math.Sqrt(dx * dx + dy * dy), Is.LessThanOrEqualTo(FoundryMap.RoomSiteRadius + .01f),
                            $"{where}: floor 2's hall {hall + 2} at its district");
                        hall++;
                    }
                    if (run.Cleared)
                        Assert.That(hall, Is.EqualTo(districts.Length), where);

                    Assert.That(run.GateWaitSeconds, Is.Zero, where + ": floor 2's gate opens as its room clears");
                    if (run.LevelAtGate >= 0)
                    {
                        Assert.That(run.LevelAtGate, Is.GreaterThanOrEqualTo(first.Level), where + ": the level carried down");
                        Assert.That(run.DifficultyAtGate, Is.GreaterThanOrEqualTo(first.Difficulty), where + ": the difficulty carried down");
                        // Floor 2's own share (8.5 / 1.4), the climb's x1.4 cancelled on its Warden.
                        Assert.That(run.WardenHealth, Is.EqualTo(DescentSimulation.Warden.Health * (8.5f / 1.4f) *
                                                                 ClimbScaling.Health(2) * run.DifficultyAtGate).Within(.01f), where);
                        Assert.That(run.ClockAtGate, Is.InRange(1f, run.FloorSeconds - run.DescentSeconds), where + ": the floor's own clock");
                    }
                    if (run.Cleared)
                    {
                        Assert.That(run.WardenSeconds, Is.GreaterThan(DescentSimulation.RevealTurnSeconds * 2f + DescentSimulation.RevealHoldSeconds), where);
                        Assert.That(run.BossSeconds, Is.GreaterThanOrEqualTo(run.WardenSeconds), where + ": the wave ends with the last of the three");
                        Assert.That(run.ExitSeconds, Is.GreaterThanOrEqualTo(ExitRule.DwellSeconds), where + ": the gate is held");
                    }
                    else
                    {
                        Assert.That(run.DeathRoom, Is.InRange(1, rooms.Length), where);
                    }
                }
                Assert.That(wentDown, Is.GreaterThan(0), hero.Name + ": a seed went down to floor 2");
            }
        }

        // The floor-1 knobs a run's levers set stay on floor 1: floor 2 plays its asset's own (FoundryFloor) - its gate
        // boss's share among them - under the run's other Warden levers and its horde levers, which stand for rules every
        // floor of the scene reads.
        [Test]
        public void AFloorAfterTheFirstPlaysItsOwnKnobsUnderTheRunsRuleLevers()
        {
            DescentSimulation.FoundryLevers run = FoundryBatch.Shipped().Copy("every knob");
            run.HallSurges = 4;
            run.ExtraElites = 2;
            run.ExtraHalls = 2;
            run.HallHoldSeconds = 30f;
            run.NodeHealthScale = 1.5f;
            run.GateNotBeforeSeconds = 240f;
            run.WardenHealthShare = 7f;
            run.WardenRecoverySeconds = 1f;
            run.HordeHealthShare = .6f;
            run.HordeFillRate = 3f;
            run.Floors = 2;
            DescentSimulation.FoundryFloor floor = DescentSimulation.FoundryFloorOne().Next;
            DescentSimulation.FoundryLevers later = DescentSimulation.LaterFloorLevers(run, floor);
            Assert.That((later.HallSurges, later.ExtraElites, later.GateNotBeforeSeconds), Is.EqualTo((floor.HallSurges, floor.ExtraElites, floor.GateNotBeforeSeconds)));
            Assert.That(later.WardenHealthShare, Is.EqualTo(WardenRule.GateHealthShareOn(floor.GateHealthShare)),
                "floor 2's own share, not floor 1's lever");
            Assert.That((later.ExtraHalls, later.HallHoldSeconds, later.NodeHealthScale), Is.EqualTo((0, 0f, 1f)));
            Assert.That((later.WardenRecoverySeconds, later.HordeHealthShare, later.HordeFillRate, later.Floors),
                Is.EqualTo((1f, .6f, 3f, 2)));
            Assert.That(later.Name, Is.EqualTo(run.Name));
            // The research lever still overrides the floor's own share, and is off by default.
            Assert.That(new DescentSimulation.FoundryLevers().LaterWardenHealthShare, Is.LessThanOrEqualTo(0f));
            run.LaterWardenHealthShare = 7.5f;
            Assert.That(DescentSimulation.LaterFloorLevers(run, floor).WardenHealthShare, Is.EqualTo(7.5f));
            Assert.That(DescentSimulation.LaterHalls(DescentSimulation.FoundryFloorOne()), Is.EqualTo(3),
                "floor 1's later halls - the workyard, the pit's rim, the east - its extra elite not counted");
        }

        // The Forge Warden's health as it walks out of the gate (docs/41, status 2026-11-10 later): floor 1's is
        // WardenRule.GateHealthShare of its authored 300, as before; floor 2's own share, 8.5 / 1.4, cancels the climb's
        // x1.4 on it, so at a difficulty of 1.6 it walks out with 4080 instead of 5712 - floor 1's Warden's at the same
        // difficulty. The simulation's floor 2 plays that share (LaterFloorLevers) as EncounterController.SpawnWave does.
        [Test]
        public void FloorTwosWardenHasTheClimbCancelledAndFloorOnesIsUnchanged()
        {
            const float difficulty = 1.6f;
            DescentSimulation.FoundryFloor one = DescentSimulation.FoundryFloorOne();
            DescentSimulation.FoundryFloor two = one.Next;
            float Warden(DescentSimulation.FoundryFloor floor, float share) =>
                FloorScaling.Health(DescentSimulation.Warden.Health,
                    ClimbScaling.Health(floor.FloorNumber) * ClimbScaling.Boss(floor.FloorNumber) * difficulty * share,
                    floor.ModifierHealthPercent);

            float first = FoundryBatch.Shipped().WardenHealthShare;
            Assert.That(first, Is.EqualTo(WardenRule.GateHealthShareOn(one.GateHealthShare)), "floor 1 sets no share of its own");
            Assert.That(Warden(one, first), Is.EqualTo(300f * 8.5f * difficulty).Within(.01f), "floor 1's, unchanged");

            float second = DescentSimulation.LaterFloorLevers(FoundryBatch.Shipped(), two).WardenHealthShare;
            Assert.That(second, Is.EqualTo(WardenRule.GateHealthShareOn(two.GateHealthShare)));
            Assert.That(second, Is.EqualTo(8.5f / 1.4f));
            Assert.That(Warden(two, second), Is.EqualTo(300f * (8.5f / 1.4f) * 1.4f * difficulty).Within(.01f));
            Assert.That(Warden(two, second), Is.EqualTo(4080f).Within(.01f), "floor 1's Warden's at the same difficulty");
            Assert.That(Warden(two, WardenRule.GateHealthShare), Is.EqualTo(5712f).Within(.01f), "floor 2's before");
        }

        // Two values alike all the way down: a value, a string or a struct equal, a list element by element, a class (a
        // weapon's work) field by field.
        private static void AssertSameValue(object actual, object expected, string where)
        {
            if (actual == null || expected == null || actual is string || actual.GetType().IsValueType)
            {
                Assert.That(actual, Is.EqualTo(expected), where);
                return;
            }
            Assert.That(actual.GetType(), Is.EqualTo(expected.GetType()), where);
            if (actual is System.Collections.IEnumerable list)
            {
                object[] a = list.Cast<object>().ToArray(), b = ((System.Collections.IEnumerable)expected).Cast<object>().ToArray();
                Assert.That(a.Length, Is.EqualTo(b.Length), where + " (count)");
                for (int i = 0; i < a.Length; i++)
                    AssertSameValue(a[i], b[i], $"{where}[{i}]");
                return;
            }
            foreach (FieldInfo field in actual.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                AssertSameValue(field.GetValue(actual), field.GetValue(expected), where + "." + field.Name);
        }

        // A number from a trace line's "name=value".
        private static float Field(string line, string name)
        {
            int start = line.IndexOf(" " + name + "=", StringComparison.Ordinal) + name.Length + 2;
            int end = line.IndexOf(' ', start);
            return float.Parse(end < 0 ? line.Substring(start) : line.Substring(start, end - start), CultureInfo.InvariantCulture);
        }

        // docs/40 section 17.3's weapon_effectiveness: each weapon's own accounting adds up to the floor's - what the
        // weapons dealt is what each of them dealt, and every kill has one killing blow - the starting weapon is held from
        // the first frame, and an evolution's procs land nothing before it is taken.
        [Test]
        public void EveryWeaponsWorkAddsUpToTheFloors()
        {
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            foreach (int seed in Seeds)
            {
                DescentSimulation.FoundryResult run = DescentSimulation.RunFoundry(hero, seed);
                string where = $"{hero.Name} seed {seed}";
                List<DescentSimulation.FoundryWeaponWork> work = run.WeaponWork;
                Assert.That(work.Count, Is.EqualTo(run.Weapons.Split(',').Length), where + ": one entry a weapon held");
                Assert.That(work.Select(w => w.Id).Distinct().Count(), Is.EqualTo(work.Count), where);
                Assert.That(work[0].Id, Is.EqualTo(hero.Weapon.Id), where + ": the starting weapon first");
                Assert.That(work[0].HeldFrom, Is.LessThan(0.05f), where + ": held from the first frame");
                Assert.That(work.Sum(w => w.Dealt), Is.EqualTo(run.DealtByWeapons).Within(run.DealtByWeapons * 1e-4f + 0.01f),
                    where + ": the weapons' damage is theirs, every bit of it");
                Assert.That(work.Sum(w => w.DealtWarden), Is.EqualTo(run.WardenFromWeapons).Within(run.WardenFromWeapons * 1e-4f + 0.01f), where);
                Assert.That(work.Sum(w => w.Strikes), Is.GreaterThan(0), where);

                var killers = new HashSet<string>(work.Select(w => w.Id))
                {
                    DescentSimulation.FoundryKill.KilledByBurn, DescentSimulation.FoundryKill.KilledByAbility,
                    DescentSimulation.FoundryKill.KilledByPit
                };
                Assert.That(run.Kills.All(k => k.By != null && killers.Contains(k.By)), Is.True, where + ": every kill has its blow");
                Assert.That(run.Kills.All(k => k.At >= 0f && k.At <= run.FloorSeconds + 1e-3f), Is.True, where);
                Assert.That(run.Kills.Where(k => k.Proc).All(k => work.Any(w => w.Id == k.By)), Is.True,
                    where + ": a proc is a weapon's");
                if (!hero.EmberMemory)
                    Assert.That(run.Kills.Count(k => k.By == DescentSimulation.FoundryKill.KilledByBurn), Is.Zero,
                        where + ": nothing burns without Ember Memory");
                foreach (DescentSimulation.FoundryWeaponWork w in work)
                {
                    Assert.That(w.DealtByProcs, Is.LessThanOrEqualTo(w.DealtAfterEvolving + 1e-3f), where + " " + w.Id);
                    if (w.EvolvedAt >= 0f)
                    {
                        Assert.That(w.EvolvedAt, Is.GreaterThanOrEqualTo(w.HeldFrom), where + " " + w.Id);
                        continue;
                    }
                    Assert.That((w.ProcLandings, w.DealtByProcs, w.DealtAfterEvolving), Is.EqualTo((0, 0f, 0f)),
                        where + " " + w.Id + ": no proc before an evolution");
                    Assert.That(run.Kills.Any(k => k.By == w.Id && k.Proc), Is.False, where + " " + w.Id);
                }
            }
        }

        // The autopilot waits out the pit's glowing band (FootworkRule): it stepped out of it to shelter and straight back in
        // to lure or kite, and the spill burnt it 3-5 times a floor on these seeds; now it does not step in while the band
        // glows, so at most once.
        [Test]
        public void TheAutopilotWaitsOutThePitsGlowingBand()
        {
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            foreach (int seed in Seeds)
            {
                DescentSimulation.FoundryResult run = DescentSimulation.RunFoundry(hero, seed);
                Assert.That(run.EruptionsOnHero, Is.LessThanOrEqualTo(1), $"{hero.Name} seed {seed}: {run.Eruptions} spills");
            }
        }

        // The autopilot takes a new weapon while a slot is free (CardScore): it scored a third 10, under every tome level,
        // and walked out to the Warden with two on every seed; on these it now has three.
        [Test]
        public void TheAutopilotFillsItsWeaponSlotsBeforeTheGate()
        {
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            foreach (int seed in Seeds)
            {
                DescentSimulation.FoundryResult run = DescentSimulation.RunFoundry(hero, seed);
                Assert.That(run.WeaponsAtGate, Is.EqualTo(WeaponLoadout.MaxSlots), $"{hero.Name} seed {seed}: {run.Weapons}");
            }
        }

        // The autopilot's walks to the halls are bounded (DropDetourRule.Walk): with the horde filling at 3 a second, the
        // thickest of the research's levers, the drop chase it had before walked each of these floors 440-466 s, and the
        // Vanguard's 153 and Ilya's 191 of seeds 1-200 past FoundryBatch.LongWalkSeconds. Now none walks long.
        [Test]
        public void OnAThickHordeTheWalksToTheHallsStayShort()
        {
            DescentSimulation.FoundryLevers thick = FoundryBatch.Shipped().Copy("fill rate 3");
            thick.HordeFillRate = 3f;
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            foreach (int seed in new[] { 3, 12 })
            {
                DescentSimulation.FoundryResult run = DescentSimulation.RunFoundry(hero, seed, thick);
                Assert.That(FoundryBatch.WalkedLong(run), Is.False, $"{hero.Name} seed {seed}: walked {run.WalkTotal:0} s");
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

            // The Warden's health grown with the floor's clock as it walks out, as a horde enemy's is before the overrun.
            DescentSimulation.FoundryResult clocked = Run(l => { l.WardenHealthShare = 7f; l.WardenClockFrom = 210f; });
            float Grown(float seconds) => 1f + HordeClock.HealthGrowth * Math.Min(seconds, HordeClock.TargetSeconds) / HordeClock.TargetSeconds;
            if (clocked.LevelAtGate >= 0)
                Assert.That(clocked.WardenHealth / clocked.DifficultyAtGate,
                    Is.EqualTo(DescentSimulation.Warden.Health * 7f * Grown(clocked.ClockAtGate) / Grown(210f)).Within(.01f));

            DescentSimulation.FoundryResult gate = Run(l => l.GateNotBeforeSeconds = 240f);
            if (gate.LevelAtGate >= 0)
                Assert.That(gate.ClockAtGate, Is.GreaterThanOrEqualTo(240f), "the gate waits for the clock");

            DescentSimulation.FoundryResult thin = Run(l => l.HordeExperienceShare = 0.1f);
            Assert.That(thin.Outcome, Is.Not.EqualTo(DescentSimulation.FoundryOutcome.Timeout));
        }

        // The shipped floor's Warden is the game's - WardenRule's health share and recovery, no other candidate on - and every
        // candidate of the Warden's batch (FOUNDRY_LEVERS=3) is another Warden.
        [Test]
        public void TheShippedWardenIsTheGamesAndEveryWardenLeverMovesIt()
        {
            DescentSimulation.FoundryLevers shipped = FoundryBatch.Shipped();
            Assert.That(shipped.WardenHealthShare, Is.EqualTo(WardenRule.GateHealthShare));
            Assert.That(shipped.WardenRecoverySeconds, Is.EqualTo(WardenRule.RecoverySeconds));
            Assert.That(shipped.WardenClockFrom, Is.Zero);
            Assert.That(shipped.WardenGuardShare, Is.EqualTo(1f));
            foreach (FoundryBatch.Lever lever in FoundryBatch.WardenLevers())
            {
                DescentSimulation.FoundryLevers l = lever.Levers;
                bool moves = l.WardenHealthShare != shipped.WardenHealthShare ||
                             l.WardenRecoverySeconds != shipped.WardenRecoverySeconds ||
                             l.WardenClockFrom != shipped.WardenClockFrom || l.WardenGuardShare != shipped.WardenGuardShare;
                Assert.That(moves, Is.True, lever.Name);
            }
        }

        // The Sword is the Warden's weapon in the simulation as in the scene (WardenRule.SwordShare, docs/41 status
        // 2026-11-10), through the one body both strike with: the simulation's Sword is WardenRule's by its id, every weapon
        // it builds - its hero's and its cards' - takes its share from the game's WeaponLoadout as the scene's does, and its
        // strikes land through WeaponRuntime.TryAttack, which the scene's AttackController fires too, with the gate boss
        // WardenSlamService hands the hero's attacker (HeroAttacks, StepWarden's _slamBoss). The simulation has no copy of
        // the rule's arithmetic to drift: WardenRule.DamageOn is its only body.
        [Test]
        public void TheSimulationsWeaponsTakeTheGamesShareOnTheWarden()
        {
            Assert.That(DescentSimulation.Sword().Id, Is.EqualTo(WardenRule.SwordId));
            foreach (DescentSimulation.HeroWeapon weapon in DescentSimulation.WeaponCatalogue())
            {
                float share = weapon.Id == WardenRule.SwordId ? WardenRule.SwordShare : 1f;
                WeaponRuntime started = weapon.CreateCategoryRuntime();
                Assert.That(new WeaponLoadout(1).TryAdd(weapon.Id, started), Is.True, weapon.Id);
                Assert.That(started.GateBossShare, Is.EqualTo(share), weapon.Id + " as the hero's own");
                WeaponCard card = weapon.CreateCard();
                WeaponRuntime picked = card.CreateRuntime();
                Assert.That(new WeaponLoadout(1).TryAdd(card.Id, picked), Is.True, card.Id);
                Assert.That(picked.GateBossShare, Is.EqualTo(share), weapon.Id + " picked up from a card");

                // Its first strike on a gate boss of the Warden's health, as HeroAttacks fires it.
                var warden = new HealthState(DescentSimulation.Warden.Health * WardenRule.GateHealthShare);
                Assert.That(started.TryAttack(warden, null, null, null, null, null, warden), Is.True, weapon.Id);
                Assert.That(warden.Maximum - warden.Current,
                    Is.EqualTo(started.Damage * share).Within(1e-3f), weapon.Id);
            }
        }

        // The Daggers are the elites' weapon in the simulation as in the scene (EliteRule.DaggersShare, docs/41 status
        // 2026-11-11), through the one body both strike with: the simulation's Daggers are EliteRule's by their id,
        // every weapon it builds - its hero's and its cards' - takes its elite share from the game's WeaponLoadout as the
        // scene's does, and its strikes land through WeaponRuntime.TryAttack with the elite room's Captain as the elites
        // (HeroAttacks), as the scene's AttackController strikes with EncounterController.Elites. EliteRule.DamageOn is the
        // rule's only body.
        [Test]
        public void TheSimulationsWeaponsTakeTheGamesShareOnTheElites()
        {
            Assert.That(DescentSimulation.Daggers().Id, Is.EqualTo(EliteRule.DaggersId));
            Assert.That(DescentSimulation.DaggersId, Is.EqualTo(EliteRule.DaggersId));
            foreach (DescentSimulation.HeroWeapon weapon in DescentSimulation.WeaponCatalogue())
            {
                float share = weapon.Id == EliteRule.DaggersId ? EliteRule.DaggersShare : 1f;
                WeaponRuntime started = weapon.CreateCategoryRuntime();
                Assert.That(new WeaponLoadout(1).TryAdd(weapon.Id, started), Is.True, weapon.Id);
                Assert.That(started.EliteShare, Is.EqualTo(share), weapon.Id + " as the hero's own");
                WeaponCard card = weapon.CreateCard();
                WeaponRuntime picked = card.CreateRuntime();
                Assert.That(new WeaponLoadout(1).TryAdd(card.Id, picked), Is.True, card.Id);
                Assert.That(picked.EliteShare, Is.EqualTo(share), weapon.Id + " picked up from a card");

                // Its first strike, no crit, on an elite of ten Grunt Captains' health, as HeroAttacks fires it.
                var captain = new HealthState(DescentSimulation.Captain.Health * 10f);
                Assert.That(started.TryAttack(captain, null, null, null, null, null, null, new IDamageable[] { captain }), Is.True, weapon.Id);
                Assert.That(captain.Maximum - captain.Current, Is.EqualTo(started.Damage * share).Within(1e-3f), weapon.Id);
            }
        }

        // The elites the simulation counts (the Daggers' role, docs/41 status 2026-11-10 night) are an elite room's wave: a
        // run that never entered an elite room fought none, what a weapon took off the Captains is part of what it took off
        // the elites, and that part of what it took off the node waves. The shipped floor's Daggers are the game's, and
        // every other row of the Daggers' research (FoundryBatch.DaggersLevers) moves a Daggers lever or gives the careful
        // pilots a habit (the step-in).
        [Test]
        public void TheElitesAreTheEliteRoomsWavesAndOnlyTheDaggersResearchMovesTheDaggers()
        {
            DescentSimulation.FoundryLevers shipped = FoundryBatch.Shipped();
            Assert.That(shipped.DaggersEliteDamage, Is.EqualTo(1f), "the shipped floor's Daggers are the game's");
            Assert.That(shipped.DaggersEliteCritDamage, Is.EqualTo(1f));
            Assert.That(shipped.DaggersEliteLeaderOnly, Is.False);
            Assert.That(shipped.DaggersReach, Is.Zero);
            Assert.That(shipped.ReachesForDaggers || shipped.DaggersBleedShare > 0f || shipped.DaggersMarkShare > 0f ||
                        shipped.DaggersExecuteBelow > 0f, Is.False, "the Daggers' design levers are off at the shipped floor");
            FoundryBatch.Lever[] levers = FoundryBatch.DaggersLevers();
            Assert.That(levers[0].Levers.DaggersEliteDamage == 1f && levers[0].Levers.DaggersEliteCritDamage == 1f &&
                        levers[0].Levers.DaggersReach == 0f && !ReshapesDaggers(levers[0].Levers) &&
                        levers[0].CarefulHabits == Cryptforge.Core.AutopilotCohort.Habit.None,
                Is.True, "the first row is the shipped floor");
            foreach (FoundryBatch.Lever lever in levers.Skip(1))
                Assert.That(lever.Levers.DaggersEliteDamage != 1f || lever.Levers.DaggersEliteCritDamage != 1f ||
                            lever.Levers.DaggersReach > 0f || ReshapesDaggers(lever.Levers) ||
                            lever.Levers.DaggersEliteLeaderOnly ||
                            lever.CarefulHabits != Cryptforge.Core.AutopilotCohort.Habit.None,
                    Is.True, lever.Name);

            DescentSimulation.FoundryHero hero = DescentSimulation.Vanguard();
            hero.Weapon = DescentSimulation.Daggers();
            int dealt = 0;
            foreach (int seed in new[] { 11, 22, 33, 44, 55, 66 })
            {
                DescentSimulation.FoundryResult run = DescentSimulation.RunFoundry(hero, seed);
                bool enteredElite = Enumerable.Range(0, run.Kinds.Length).Any(i => run.Kinds[i] == RoomKind.Elite && run.WaveSeconds[i] >= 0f) ||
                                    (run.DeathRoom >= 1 && run.Kinds[run.DeathRoom - 1] == RoomKind.Elite);
                if (!enteredElite)
                    Assert.That(run.EliteFought, Is.Zero, $"seed {seed}: no elite room, no elites");
                foreach (DescentSimulation.FoundryWeaponWork work in run.WeaponWork)
                {
                    Assert.That(work.DealtEliteLeader, Is.LessThanOrEqualTo(work.DealtElite + 1e-3f), $"seed {seed} {work.Id}: the Captains are elites");
                    Assert.That(work.DealtElite, Is.LessThanOrEqualTo(work.DealtNode + 1e-3f), $"seed {seed} {work.Id}: the elites are node waves");
                }
                if (run.WeaponWork[0].DealtElite > 0f)
                    dealt++;
            }
            Assert.That(dealt, Is.GreaterThan(0), "the Daggers reach the elites on some seed");
        }

        // The Daggers' design levers (docs/41, status 2026-11-11): a throw, a lunge, a bleed, a mark or an execute.
        private static bool ReshapesDaggers(DescentSimulation.FoundryLevers levers) =>
            levers.ReachesForDaggers || levers.DaggersBleedShare > 0f || levers.DaggersMarkShare > 0f || levers.DaggersExecuteBelow > 0f;

        // The Daggers' design levers do what they say on a Vanguard started with the Daggers: the throw and the lunge fire
        // and reach farther than the Daggers do, the bleed and the mark add to what the Daggers take off the floor without
        // a landing more, and the floor's accounting holds - the Captains part of the elites, the elites of the node waves.
        [Test]
        public void TheDaggersDesignLeversThrowLungeBleedAndMarkAsTheyName()
        {
            DescentSimulation.FoundryHero hero = DescentSimulation.Vanguard();
            hero.Weapon = DescentSimulation.Daggers();
            DescentSimulation.FoundryLevers shipped = FoundryBatch.Shipped();
            DescentSimulation.FoundryLevers Lever(Action<DescentSimulation.FoundryLevers> set)
            {
                DescentSimulation.FoundryLevers levers = shipped.Copy("daggers design");
                set(levers);
                return levers;
            }

            const int seed = 22;
            DescentSimulation.FoundryResult plain = DescentSimulation.RunFoundry(hero, seed, shipped);
            DescentSimulation.FoundryResult thrown = DescentSimulation.RunFoundry(hero, seed, Lever(l => l.DaggersThrowReach = 3.2f));
            DescentSimulation.FoundryResult lunged = DescentSimulation.RunFoundry(hero, seed, Lever(l => l.DaggersLungeReach = 3f));
            DescentSimulation.FoundryResult bled = DescentSimulation.RunFoundry(hero, seed, Lever(l => l.DaggersBleedShare = 1f));
            DescentSimulation.FoundryResult marked = DescentSimulation.RunFoundry(hero, seed, Lever(l => l.DaggersMarkShare = .3f));
            Assert.That(plain.DaggersThrows + plain.DaggersLunges, Is.Zero, "nothing thrown or lunged at the shipped levers");
            Assert.That(thrown.DaggersThrows, Is.GreaterThan(0), "the throw fires");
            Assert.That(lunged.DaggersLunges, Is.GreaterThan(0), "the lunge fires");
            Assert.That(thrown.WeaponWork[0].Landings, Is.GreaterThan(plain.WeaponWork[0].Landings), "a throw lands beyond the Daggers' reach");
            // The wounds deal on top of the landings - little on the horde, which falls to a landing or two before its wounds
            // close - and a wound's ticks are no landings.
            float PerLanding(DescentSimulation.FoundryResult r) => r.WeaponWork[0].Dealt / Math.Max(1, r.WeaponWork[0].Landings);
            Assert.That(PerLanding(bled), Is.GreaterThan(PerLanding(plain)), "the wounds deal on top of the landings");
            Assert.That(PerLanding(bled), Is.LessThan(PerLanding(plain) * 2f), "a wound deals the landing's worth at most");
            Assert.That(marked.WeaponWork.Count, Is.GreaterThan(0));
            foreach (DescentSimulation.FoundryResult run in new[] { thrown, lunged, bled, marked })
                foreach (DescentSimulation.FoundryWeaponWork work in run.WeaponWork)
                {
                    Assert.That(work.DealtEliteLeader, Is.LessThanOrEqualTo(work.DealtElite + 1e-3f), work.Id);
                    Assert.That(work.DealtElite, Is.LessThanOrEqualTo(work.DealtNode + 1e-3f), work.Id);
                }
        }

        // The Warden's own weapon is held from the frame its mark goes down until its hammer is out of the floor again -
        // WardenSlamService runs before every AttackController, and the simulation steps the clock before anything
        // attacks - and every blow lands a whole telegraph after its mark, the Warden enraged under it or not
        // (WardenSlamClock). On these seeds the Warden enrages under a calm mark, whose hammer landed the frame after
        // when the clock read its pace each frame; and its weapon comes ready on a mark's own frame with the hero in reach,
        // where it struck while the clock was stepped after the attackers.
        [Test]
        public void TheWardenStrikesNothingFromItsMarksFrameAndEveryBlowKeepsItsTelegraph()
        {
            const float frame = 1f / 60f;
            int marks = 0, strikes = 0, enragedUnderAMark = 0;
            foreach ((DescentSimulation.FoundryHero hero, int seed) in new[] { (DescentSimulation.Vanguard(), 37), (DescentSimulation.CinderArcanist(), 97) })
            {
                string where = $"{hero.Name} seed {seed}";
                var lines = new List<string>();
                // The clumsy cohort (the old pilot) stands in the Warden's reach; the careful pilot kites past it, so on
                // Unity's runtime its fights can pass without a single strike to check (2026-11-10).
                var levers = new DescentSimulation.FoundryLevers { Cohort = Cryptforge.Core.AutopilotCohort.Clumsy };
                DescentSimulation.RunFoundry(hero, seed, levers, trace: lines.Add);
                // A trace line starts with its frame's time, so the lines of one frame share it.
                var marked = new List<(string Frame, float At, bool Enraged)>();
                var blows = new List<(float At, bool Enraged)>();
                var struck = new List<(string Frame, float At)>();
                foreach (string line in lines)
                {
                    string time = line.Substring(0, line.IndexOf(' '));
                    float at = float.Parse(time, CultureInfo.InvariantCulture);
                    if (line.Contains(" slam_mark "))
                        marked.Add((time, at, line.Contains(" enraged=True")));
                    else if (line.Contains(" slam_blow "))
                        blows.Add((at, line.Contains(" enraged=True")));
                    else if (line.Contains(" warden_strike "))
                        struck.Add((time, at));
                }
                Assert.That(blows.Count, Is.InRange(marked.Count - 1, marked.Count), where + ": a blow for every mark but one the fight ended under");
                for (int i = 0; i < marked.Count; i++)
                {
                    float freeFrom = i < blows.Count ? blows[i].At + WardenRule.RecoverySeconds - frame / 2f : float.MaxValue;
                    foreach ((string strikeFrame, float at) in struck)
                    {
                        Assert.That(strikeFrame, Is.Not.EqualTo(marked[i].Frame), $"{where}: the Warden struck on its mark's frame");
                        Assert.That(at >= marked[i].At && at < freeFrom, Is.False,
                            $"{where}: the Warden struck at {strikeFrame}, its weapon held from the mark at {marked[i].Frame} to the recovery's end");
                    }
                    if (i >= blows.Count)
                        continue;
                    Assert.That(blows[i].At - marked[i].At, Is.GreaterThanOrEqualTo(WardenRule.Telegraph - frame / 2f),
                        $"{where}: the blow after the mark at {marked[i].Frame} lands a telegraph after it");
                    if (!marked[i].Enraged && blows[i].Enraged)
                        enragedUnderAMark++;
                }
                marks += marked.Count;
                strikes += struck.Count;
            }
            Assert.That(marks, Is.GreaterThan(0), "the Warden was fought");
            Assert.That(strikes, Is.GreaterThan(0), "its own weapon struck between its marks");
            // Whether these seeds bring an enrage under a calm mark is the runtime's: Unity's and .NET's run a seed apart
            // (.NET's seed 37 does; Unity's 16 seeds round it did not), so the case itself is pinned where it is pure, in
            // WardenRuleTests, and here only counted.
            TestContext.Out.WriteLine($"{marks} marks, {strikes} strikes between them, {enragedUnderAMark} enraged under a calm mark");
        }

        // Every lever, combination and Warden candidate names a floor the shipped one is not, and a research lever one its
        // own floor (the pre-V0.1 row it is read against) is not either, so no row of the tables reads a floor against itself.
        [Test]
        public void EveryLeverAndComboMovesAKnob()
        {
            DescentSimulation.FoundryLevers shipped = FoundryBatch.Shipped();
            FoundryBatch.Lever[] research = FoundryBatch.ResearchLevers();
            foreach (FoundryBatch.Lever lever in research.Concat(FoundryBatch.ComboLevers()).Concat(FoundryBatch.WardenLevers()))
            {
                Assert.That(SameKnobs(lever.Levers, shipped), Is.False, lever.Name + " moves a knob of the shipped floor");
                if (lever.Basis == null)
                    continue;
                FoundryBatch.Lever basis = research.SingleOrDefault(row => row.Levers.Name == lever.Basis);
                Assert.That(basis, Is.Not.Null, lever.Name + " is read against a row of its list");
                Assert.That(SameKnobs(lever.Levers, basis.Levers), Is.False, lever.Name + " moves a knob of " + lever.Basis);
            }

            DescentSimulation.FoundryLevers before = FoundryBatch.PreV01();
            Assert.That((before.HallSurges, before.ExtraElites, before.GateNotBeforeSeconds), Is.EqualTo((0, 0, 0f)),
                "the floor before V0.1: no surges, no second elite, no gate");
            Assert.That(SameKnobs(shipped, shipped.Copy("again")), Is.True, "a copy is the same floor");
            DescentSimulation.FoundryLevers tuned = shipped.Copy("tuned at shipped");
            tuned.HordeTargets = (int[])HordeRule.TargetByState.Clone();
            Assert.That(SameKnobs(shipped, tuned), Is.True, "the director's shipped crowds are the shipped floor's");
        }

        // docs/41's open decision 2: the horde's growth as a lever is HordeClock's own at the shipped growth, and its body
        // elsewhere.
        [Test]
        public void TheTunedTargetGrowthAtTheShippedGrowthIsTheClocks()
        {
            for (float clock = 0f; clock <= 330f; clock += 3.7f)
                Assert.That(TunedHordeDirector.TargetMultiplier(clock, HordeClock.TargetGrowth),
                    Is.EqualTo(HordeClock.TargetMultiplier(clock)), $"at {clock}");
            Assert.That(TunedHordeDirector.TargetMultiplier(150f, 1.5f), Is.EqualTo(1.75f).Within(1e-6f));
            Assert.That(TunedHordeDirector.TargetMultiplier(400f, 3f), Is.EqualTo(4f).Within(1e-6f));
            int[] shipped = (int[])HordeRule.TargetByState.Clone();
            for (float clock = 0f; clock <= 330f; clock += 7.3f)
                Assert.That(TunedHordeDirector.EffectiveTarget(shipped, HordeState.Pressure, 5, 5, HordeRule.SceneCap, clock,
                        HordeClock.TargetGrowth),
                    Is.EqualTo(HordeRule.EffectiveTarget(HordeState.Pressure, 5, 5, HordeRule.SceneCap, clock)), $"at {clock}");
        }

        // The shipped gate's wait is accounted frame by frame: what the steer did adds up to the wait, the room before the
        // gate cleared before the gate's time, and a gate without a time does not hold.
        [Test]
        public void TheGatesWaitIsAccountedAndATimelessGateDoesNotHold()
        {
            DescentSimulation.FoundryResult shipped = DescentSimulation.RunFoundry(DescentSimulation.Vanguard(), 1, FoundryBatch.Shipped());
            Assert.That(shipped.Cleared, Is.True);
            Assert.That(shipped.GateWaitSeconds, Is.GreaterThan(0f));
            Assert.That(shipped.WaitKite + shipped.WaitDrop + shipped.WaitFight + shipped.WaitWalk + shipped.WaitStill,
                Is.EqualTo(shipped.GateWaitSeconds).Within(0.05f));
            Assert.That(shipped.WaitEngaged, Is.InRange(0f, shipped.GateWaitSeconds + 0.01f));
            Assert.That(shipped.GateReadyClock, Is.InRange(0f, 210f));
            Assert.That(shipped.WaitKills, Is.GreaterThan(0));
            Assert.That(shipped.WaitSiegeWaves, Is.EqualTo(0));

            DescentSimulation.FoundryLevers open = FoundryBatch.Shipped().Copy("no gate time");
            open.GateNotBeforeSeconds = 0f;
            DescentSimulation.FoundryResult run = DescentSimulation.RunFoundry(DescentSimulation.Vanguard(), 1, open);
            Assert.That(run.GateWaitSeconds, Is.EqualTo(0f));
            Assert.That(run.GateReadyClock, Is.EqualTo(shipped.GateReadyClock), "the same floor up to the gate");
        }

        // A gate on a kill quota alone holds until the floor's kills reach it; a siege fills the wait with node waves; a
        // floor below the first plays none of floor 1's gate experiments.
        [Test]
        public void AQuotaGateOpensOnItsKillsAndASiegeSendsWaves()
        {
            DescentSimulation.FoundryLevers quota = FoundryBatch.Shipped().Copy("quota");
            quota.GateNotBeforeSeconds = 0f;
            quota.GateKillQuota = 400;
            DescentSimulation.FoundryResult run = DescentSimulation.RunFoundry(DescentSimulation.Vanguard(), 1, quota);
            Assert.That(run.GateReadyKills, Is.LessThan(400), "seed 1 reaches the gate short of the quota");
            Assert.That(run.GateWaitSeconds, Is.GreaterThan(0f));
            if (run.Cleared)
                Assert.That(run.GateReadyKills + run.WaitKills, Is.GreaterThanOrEqualTo(400));

            DescentSimulation.FoundryLevers siege = FoundryBatch.Shipped().Copy("siege");
            siege.GateSiegeHall = 1;
            DescentSimulation.FoundryResult sieged = DescentSimulation.RunFoundry(DescentSimulation.CinderArcanist(), 1, siege);
            Assert.That(sieged.WaitSiegeWaves, Is.GreaterThan(0));
            Assert.That(sieged.WaitFight, Is.GreaterThan(0f));

            DescentSimulation.FoundryLevers later = DescentSimulation.LaterFloorLevers(siege, DescentSimulation.FoundryFloorOne());
            Assert.That((later.GateKillQuota, later.GateOpenLevel, later.GateSiegeHall), Is.EqualTo((0, 0, 0)));
        }

        // Every knob of a lever set, read off its fields, so a knob added later is compared too; the director's crowds
        // unset are the shipped ones (HordeRule.TargetByState).
        private static bool SameKnobs(DescentSimulation.FoundryLevers a, DescentSimulation.FoundryLevers b)
        {
            foreach (FieldInfo field in typeof(DescentSimulation.FoundryLevers).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.Name == nameof(DescentSimulation.FoundryLevers.Name))
                    continue;
                object x = field.GetValue(a), y = field.GetValue(b);
                if (field.Name == nameof(DescentSimulation.FoundryLevers.HordeTargets))
                {
                    x = x ?? HordeRule.TargetByState;
                    y = y ?? HordeRule.TargetByState;
                }
                bool same = x is int[] xs && y is int[] ys ? xs.SequenceEqual(ys) : Equals(x, y);
                if (!same)
                    return false;
            }
            return true;
        }
    }
}
