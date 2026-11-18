using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cryptforge.Core;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/57's skills 2 and 3 in the foundry simulation (phase 1): off by default and inert at Skills 1 - the run is the
    // shipped one field for field, its trace line for line - and on, played by each cohort through SkillFootwork: every
    // skill pressed, each doing what section 3 says, the same seed replaying bit for bit.
    public sealed class FoundrySkillsTests
    {
        private static IEnumerable<DescentSimulation.FoundryResult> Floors(DescentSimulation.FoundryResult run)
        {
            for (DescentSimulation.FoundryResult floor = run; floor != null; floor = floor.NextFloor)
                yield return floor;
        }

        private static DescentSimulation.FoundryLevers Levers(AutopilotCohort cohort, int skills)
        {
            DescentSimulation.FoundryLevers levers = FoundryCohorts.Levers(cohort, DescentSimulation.FinaleFloor, PilotRealism.Typical())
                .Copy($"{cohort.Name}, skills {skills}");
            levers.Skills = skills;
            return levers;
        }

        private static DescentSimulation.FoundryResult Run(DescentSimulation.FoundryHero hero, int seed,
            DescentSimulation.FoundryLevers levers, List<string> trace = null) =>
            DescentSimulation.RunFoundry(hero, seed, levers, DescentSimulation.GloamChain(levers), trace != null ? trace.Add : null);

        // Skills 1, whatever the tuning lever holds, is the run the batch plays: every field of every floor, every trace line.
        [Test]
        public void ARunOfSkillOneAloneIsTheShippedRunFieldForFieldAndLineForLine()
        {
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            {
                DescentSimulation.FoundryLevers shipped = Levers(AutopilotCohort.Careful, 1);
                DescentSimulation.FoundryLevers inert = shipped.Copy("inert");
                inert.TuneSkill = skill => skill.Cooldown = 1f;
                inert.SkillTwoOff = true;
                var a = new List<string>();
                var b = new List<string>();
                DescentSimulation.FoundryResult expected = Run(hero, 3, shipped, a);
                DescentSimulation.FoundryResult actual = Run(hero, 3, inert, b);
                Assert.That(b, Is.EqualTo(a), hero.Name + ": the trace");
                Assert.That(b.Any(line => line.Contains("skill_used")), Is.False);
                DescentSimulation.FoundryResult[] x = Floors(expected).ToArray(), y = Floors(actual).ToArray();
                Assert.That(y.Length, Is.EqualTo(x.Length));
                for (int f = 0; f < x.Length; f++)
                    foreach (FieldInfo field in typeof(DescentSimulation.FoundryResult).GetFields(BindingFlags.Public | BindingFlags.Instance))
                        if (field.Name != nameof(DescentSimulation.FoundryResult.NextFloor) &&
                            field.Name != nameof(DescentSimulation.FoundryResult.Levers))
                            Assert.That(Describe(field.GetValue(y[f])), Is.EqualTo(Describe(field.GetValue(x[f]))),
                                $"{hero.Name} floor {f + 1}: {field.Name}");
                Assert.That(y.Sum(r => r.SkillUses.Sum()), Is.EqualTo(0));
            }
        }

        private static string Describe(object value)
        {
            if (value is System.Collections.IEnumerable list && !(value is string))
                return string.Join(",", list.Cast<object>().Select(Describe));
            if (value == null)
                return "null";
            if (value.GetType().IsValueType || value is string)
                return value.ToString();
            return string.Join(";", value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Select(f => f.Name + "=" + Describe(f.GetValue(value))));
        }

        // Skills on, the careful pilot down the whole Descent: both skills pressed, each doing its work - the guard taking
        // blows and the Rush striking and dazing for the Vanguard; the glyph ticking, slowing and lighting, the ward taking
        // blows for Ilya - and the same seed replaying bit for bit.
        [Test]
        public void SkillsOnEachSkillIsPressedAndDoesItsWorkAndASeedReplays()
        {
            int[] seeds = { 1, 2, 3, 4 };
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            {
                DescentSimulation.FoundryLevers levers = Levers(AutopilotCohort.Careful, 3);
                DescentSimulation.FoundryResult[] floors = seeds.SelectMany(s => Floors(Run(hero, s, levers))).ToArray();
                Assert.That(floors.Sum(f => f.SkillUses[0]), Is.GreaterThan(0), hero.Name + ": skill 2 pressed");
                Assert.That(floors.Sum(f => f.SkillUses[1]), Is.GreaterThan(0), hero.Name + ": skill 3 pressed");
                if (hero.Id == "hero_vanguard")
                {
                    Assert.That(floors.Sum(f => f.GuardedBlows), Is.GreaterThan(0));
                    Assert.That(floors.Sum(f => f.GuardPrevented), Is.GreaterThan(0f));
                    Assert.That(floors.Sum(f => f.RushStruck), Is.GreaterThan(0));
                    Assert.That(floors.Sum(f => f.SkillDealt[1]), Is.GreaterThan(0f));
                }
                else
                {
                    Assert.That(floors.Sum(f => f.SigilTicks), Is.GreaterThan(0));
                    Assert.That(floors.Sum(f => f.SigilSlowed), Is.GreaterThan(0));
                    Assert.That(floors.Sum(f => f.SigilBurnExtra), Is.GreaterThan(0f));
                    Assert.That(floors.Sum(f => f.WardedBlows), Is.GreaterThan(0));
                }
                var first = new List<string>();
                var again = new List<string>();
                Run(hero, 2, levers, first);
                Run(hero, 2, levers, again);
                Assert.That(again, Is.EqualTo(first), hero.Name + ": a seed replays line for line");
                Assert.That(first.Any(line => line.Contains("skill_used slot=2")), Is.True);
            }
        }

        // The clumsy pilot presses on cooldown and the careful one times its presses: on the same seeds the clumsy one
        // presses each skill at least as often. The auto-farmer presses nothing.
        [Test]
        public void TheClumsyPilotPressesOnCooldownAndTheAutoFarmerNever()
        {
            int[] seeds = { 1, 2, 3 };
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
            {
                double Rate(AutopilotCohort cohort, int slot)
                {
                    DescentSimulation.FoundryResult[] floors =
                        seeds.SelectMany(s => Floors(Run(hero, s, Levers(cohort, 3)))).ToArray();
                    return floors.Sum(f => f.SkillUses[slot]) * 60.0 / floors.Sum(f => f.FloorSeconds);
                }
                for (int slot = 0; slot < 2; slot++)
                    Assert.That(Rate(AutopilotCohort.Clumsy, slot), Is.GreaterThanOrEqualTo(Rate(AutopilotCohort.Careful, slot)),
                        $"{hero.Name} skill {slot + 2}");
                Assert.That(Rate(AutopilotCohort.AutoFarmer, 0) + Rate(AutopilotCohort.AutoFarmer, 1), Is.EqualTo(0));
            }
        }

        // Skill 3 alone beside skill 1 (SkillTwoOff) presses only skill 3; Skills 2 only skill 2.
        [Test]
        public void TheSkillsLeverCarriesTheSkillsItNames()
        {
            DescentSimulation.FoundryHero hero = DescentSimulation.Vanguard();
            DescentSimulation.FoundryLevers two = Levers(AutopilotCohort.Clumsy, 2);
            DescentSimulation.FoundryLevers three = Levers(AutopilotCohort.Clumsy, 3);
            three.SkillTwoOff = true;
            DescentSimulation.FoundryResult[] a = Floors(Run(hero, 5, two)).ToArray();
            DescentSimulation.FoundryResult[] b = Floors(Run(hero, 5, three)).ToArray();
            Assert.That((a.Sum(f => f.SkillUses[0]) > 0, a.Sum(f => f.SkillUses[1])), Is.EqualTo((true, 0)));
            Assert.That((b.Sum(f => f.SkillUses[0]), b.Sum(f => f.SkillUses[1]) > 0), Is.EqualTo((0, true)));
        }
    }
}
