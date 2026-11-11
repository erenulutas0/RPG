using System.Linq;
using Cryptforge.Core;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The difficulty research's simulation levers (2026-11-04, docs/cloud/2026-11-04-difficulty.md): a human's hands on the
    // autopilot (PilotRealism) and the structural candidates' hazards. Off, they leave the floor as it was; a realism of
    // zeros plays the very floor the frame-perfect pilot plays, since its dice are its own; a late read of the Warden's mark
    // is hit by the slam; and each hazard puts its marks down. Balance is not pinned here: what they do to the clears is
    // the research's (Tools/CombatChecks/FoundryDifficultyResearch.cs).
    public sealed class FoundryRealismTests
    {
        private const int Seed = 11;

        private static DescentSimulation.FoundryResult Run(DescentSimulation.FoundryHero hero, DescentSimulation.FoundryLevers levers) =>
            DescentSimulation.RunFoundry(hero, Seed, levers);

        private static void AssertSameFloor(DescentSimulation.FoundryResult a, DescentSimulation.FoundryResult b, string where)
        {
            Assert.That(b.Outcome, Is.EqualTo(a.Outcome), where);
            Assert.That(b.FloorSeconds, Is.EqualTo(a.FloorSeconds), where);
            Assert.That(b.RoomSeconds, Is.EqualTo(a.RoomSeconds), where);
            Assert.That(b.Picks, Is.EqualTo(a.Picks), where);
            Assert.That(b.TakenFromHorde, Is.EqualTo(a.TakenFromHorde), where);
            Assert.That(b.HazardMarks, Is.EqualTo(0), where);
            Assert.That(b.Lapses, Is.EqualTo(0), where);
        }

        [Test]
        public void OffTheLeversLeaveTheFloorAsItWas()
        {
            var levers = new DescentSimulation.FoundryLevers();
            Assert.That(levers.Realism, Is.Null);
            Assert.That(levers.HasHazards, Is.False);
            Assert.That(levers.AmbushSurges, Is.False);
            Assert.That(levers.WardenEchoEnragedOnly, Is.False);
            Assert.That(PilotRealism.Named(null), Is.Null);
            Assert.That(PilotRealism.Named(""), Is.Null);
        }

        [Test]
        public void ARealismOfZerosPlaysTheFrameperfectFloor()
        {
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            {
                DescentSimulation.FoundryResult plain = Run(hero, FoundryBatch.Shipped());
                DescentSimulation.FoundryLevers zeros = FoundryBatch.Shipped().Copy("zeros");
                zeros.Realism = new PilotRealism { Name = "zeros" };
                AssertSameFloor(plain, Run(hero, zeros), hero.Name);
            }
        }

        // The mark is 2.2 wide and the hero walks 3.5 a second: read 0.7 s into a 1.1 s telegraph, no escape leaves it.
        [Test]
        public void AMarkReadLateIsHitByTheSlam()
        {
            DescentSimulation.FoundryHero hero = DescentSimulation.CinderArcanist();
            DescentSimulation.FoundryResult careful = Run(hero, FoundryBatch.Shipped());
            Assert.That(careful.Slams, Is.GreaterThan(0), "the careful pilot reached the Warden");
            Assert.That(careful.SlamHits, Is.EqualTo(0));
            DescentSimulation.FoundryLevers late = FoundryBatch.Shipped().Copy("late");
            late.Realism = new PilotRealism { Name = "late", MarkReaction = .7f };
            DescentSimulation.FoundryResult result = Run(hero, late);
            Assert.That(result.SlamHits, Is.GreaterThan(0));
            Assert.That(result.TakenFromSlams, Is.GreaterThan(0f));
        }

        [Test]
        public void TheLapsesAreCountedAndDrawnFromTheSeed()
        {
            DescentSimulation.FoundryLevers levers = FoundryBatch.Shipped().Copy("lapses");
            levers.Realism = PilotRealism.Typical();
            DescentSimulation.FoundryResult a = Run(DescentSimulation.Vanguard(), levers);
            DescentSimulation.FoundryResult b = Run(DescentSimulation.Vanguard(), levers);
            Assert.That(a.Lapses, Is.GreaterThan(0));
            Assert.That(b.Lapses, Is.EqualTo(a.Lapses));
            Assert.That(b.FloorSeconds, Is.EqualTo(a.FloorSeconds));
        }

        [Test]
        public void EachHazardPutsItsMarksDown()
        {
            foreach (FoundryBatch.Lever lever in FoundryBatch.DifficultyLevers().Where(l => l.Levers.HasHazards))
            {
                DescentSimulation.FoundryLevers levers = lever.Levers.Copy(lever.Name);
                levers.Cohort = AutopilotCohort.AutoFarmer;
                DescentSimulation.FoundryResult result = Run(DescentSimulation.CinderArcanist(), levers);
                Assert.That(result.HazardMarks, Is.GreaterThan(0), lever.Name);
            }
        }

        // The echo's second round (2026-11-11): every blow of the careful pilot's Warden has its echo, and with
        // WardenEchoEnragedOnly only the blows after the Warden is enraged - some, never all.
        [Test]
        public void TheEnragedOnlyEchoFollowsTheEnragedBlowsAlone()
        {
            DescentSimulation.FoundryLevers always = FoundryBatch.Shipped().Copy("echo");
            always.WardenEchoTelegraph = 1.1f;
            DescentSimulation.FoundryResult every = Run(DescentSimulation.CinderArcanist(), always);
            Assert.That(every.Slams, Is.GreaterThan(0), "the careful pilot reached the Warden");
            Assert.That(every.HazardMarks, Is.EqualTo(every.Slams));
            DescentSimulation.FoundryLevers enraged = always.Copy("enraged echo");
            enraged.WardenEchoEnragedOnly = true;
            DescentSimulation.FoundryResult result = Run(DescentSimulation.CinderArcanist(), enraged);
            Assert.That(result.HazardMarks, Is.GreaterThan(0));
            Assert.That(result.HazardMarks, Is.LessThan(result.Slams));
        }

        // Counted only when a report asks (FoundryLevers.CountsHarmByPlace), and accounting only: the floor is the same.
        [Test]
        public void TheHealthLostIsCountedByPlaceAndBlow()
        {
            DescentSimulation.FoundryResult plain = Run(DescentSimulation.Vanguard(), FoundryBatch.Shipped());
            Assert.That(plain.TakenBy, Is.Null);
            DescentSimulation.FoundryLevers counted = FoundryBatch.Shipped().Copy("counted");
            counted.CountsHarmByPlace = true;
            DescentSimulation.FoundryResult result = Run(DescentSimulation.Vanguard(), counted);
            AssertSameFloor(plain, result, "counted");
            Assert.That(result.LowestHealthShare, Is.EqualTo(plain.LowestHealthShare));
            Assert.That(result.LowestPhase, Is.EqualTo(plain.LowestPhase));
            float taken = result.TakenFromWave + result.TakenFromHorde + result.TakenFromWarden + result.TakenFromSlams +
                          result.TakenFromPit + result.TakenFromHazards;
            Assert.That(result.TakenBy.Values.Sum(), Is.EqualTo(taken).Within(.01f));
            Assert.That(result.LowestHealthShare, Is.LessThan(1f));
            Assert.That(result.LowestPhase, Is.Not.Null);
            Assert.That(result.TakenBy.Keys.All(k => k.Contains("|")), Is.True);
            Assert.That(result.TakenBy.Keys.Any(k => k.StartsWith(result.LowestPhase + "|", System.StringComparison.Ordinal)), Is.True);
        }
    }
}
