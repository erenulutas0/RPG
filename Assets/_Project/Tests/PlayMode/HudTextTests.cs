using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine.Profiling;

namespace Cryptforge.Tests
{
    // HudText makes the HUD's whole-number lines - the gold line on every coin, the experience line on every experience
    // drop picked up - as string.Format makes them, with the line as its one allocation where string.Format boxes every
    // number and makes a string of each first. Every format and number reads the same either way, and fails the same
    // way: the project's own lines, and every kind of format or number HudText leaves to string.Format, in several
    // cultures (the owner plays in Turkish).
    public sealed class HudTextTests
    {
        private static readonly string[] Formats =
        {
            "Gold {0}  ({1} at risk)", "Level {0}  |  XP {1} / {2}", "Lv {0} -> {1}", "Danger +{0}%", "{0}:{1:00}",
            "{0}", "{1}{0}", "{2} {1} {0}", "{0}{0}{2}", "", "no placeholders", "{00}", "{01}x", "{0:00}", "{0:D3}",
            "{0,4}|", "{0 }", "{ 0}", "{{0}}", "{{", "}}", "a}b", "{", "}", "{0", "{x}", "{}", "{3}", "{10}", "{123}",
            "ç{1}ğı{0}ş",
        };
        private static readonly int[] Values = { 0, 1, 9, 10, 99, 100, 1000, 12345, int.MaxValue, -1, -42, int.MinValue };
        private static readonly int[] FewValues = { 0, 7, 4500, -3 };
        private static readonly string[] Cultures = { "", "tr-TR", "en-US", "ar-SA", "fa-IR" };
        private static readonly string ExperienceLine = "Level {0}  |  XP {1} / {2}";
        // Everything a measured call makes ends up here, so no call is idle.
        private static string _sink;

        [Test]
        public void EveryLineReadsAsStringFormatReadsIt()
        {
            CultureInfo before = Thread.CurrentThread.CurrentCulture;
            var compared = new List<string>();
            try
            {
                foreach (string name in Cultures)
                {
                    CultureInfo culture;
                    try
                    {
                        culture = CultureInfo.GetCultureInfo(name);
                    }
                    catch (CultureNotFoundException)
                    {
                        continue;
                    }
                    Thread.CurrentThread.CurrentCulture = culture;
                    compared.Add(name.Length == 0 ? "invariant" : name);
                    foreach (string format in Formats)
                    {
                        foreach (int a in Values)
                        {
                            foreach (int b in Values)
                                Same(Outcome(() => HudText.Format(format, a, b)), Outcome(() => string.Format(format, a, b)),
                                    format, a, b);
                            foreach (int b in FewValues)
                                foreach (int c in FewValues)
                                    Same(Outcome(() => HudText.Format(format, a, b, c)),
                                        Outcome(() => string.Format(format, a, b, c)), format, a, b, c);
                        }
                    }
                    Same(Outcome(() => HudText.Format(null, 1, 2)), Outcome(() => string.Format(null, 1, 2)), null, 1, 2);
                    Same(Outcome(() => HudText.Format(null, 1, 2, 3)), Outcome(() => string.Format(null, 1, 2, 3)), null, 1,
                        2, 3);
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = before;
            }
            TestContext.Out.WriteLine("Cultures compared: " + string.Join(", ", compared));
            Assert.That(compared, Does.Contain("tr-TR"), "The owner's culture is among those compared.");
        }

        [Test]
        public void CountsReadAsToStringReadsThem()
        {
            for (int value = -5; value < 250; value++)
                Assert.That(HudText.Count(value), Is.EqualTo(value.ToString()));
            Assert.That(HudText.Count(int.MaxValue), Is.EqualTo(int.MaxValue.ToString()));
            Assert.That(HudText.Count(int.MinValue), Is.EqualTo(int.MinValue.ToString()));
            Assert.That(HudText.Count(7), Is.SameAs(HudText.Count(7)), "A small count is made once.");
        }

        // GC.Alloc samples on this thread while the call runs, as Unity's AllocatingGCMemoryConstraint counts them. The
        // format is a static field, not a literal in the measured lambda: Mono allocates a string for every literal it
        // compiles in, and the first call compiles it - which the warm call before each count takes care of too.
        [Test]
        public void TheLineIsItsOnlyAllocation()
        {
            Action line = () => _sink = HudText.Format(ExperienceLine, 12, 3456, 4000);
            Action count = () => _sink = HudText.Count(42);
            Action format = () => _sink = string.Format(ExperienceLine, 12, 3456, 4000);
            line();
            count();
            format();
            int lineAllocations = Allocations(line);
            int countAllocations = Allocations(count);
            int formatAllocations = Allocations(format);
            TestContext.Out.WriteLine("GC allocations (GC.Alloc samples) for the experience line: HudText " + lineAllocations +
                                      ", string.Format " + formatAllocations + "; a cached count: " + countAllocations);
            Assert.That(HudText.Format(ExperienceLine, 12, 3456, 4000), Is.EqualTo("Level 12  |  XP 3456 / 4000"));
            Assert.That(lineAllocations, Is.EqualTo(1), "The line is HudText's only allocation.");
            Assert.That(countAllocations, Is.Zero, "A cached count allocates nothing.");
            Assert.That(formatAllocations, Is.GreaterThan(lineAllocations), "string.Format boxes the numbers besides.");
        }

        // The message is made only for a difference: tens of thousands of comparisons pass.
        private static void Same(string made, string expected, string format, params int[] args)
        {
            if (made != expected)
                Assert.Fail("Format \"" + format + "\" with " + string.Join(", ", args) + " in culture \"" +
                            CultureInfo.CurrentCulture.Name + "\": HudText " + made + ", string.Format " + expected);
        }

        private static string Outcome(Func<string> make)
        {
            try
            {
                return "text: " + make();
            }
            catch (Exception exception)
            {
                return "throws " + exception.GetType().Name;
            }
        }

        // Disabling the recorder flushes what it held, so only the call's own samples are read back.
        private static int Allocations(Action action)
        {
            Recorder recorder = Recorder.Get("GC.Alloc");
            recorder.enabled = false;
            recorder.FilterToCurrentThread();
            recorder.enabled = true;
            try
            {
                action();
            }
            finally
            {
                recorder.enabled = false;
                recorder.CollectFromAllThreads();
            }
            return recorder.sampleBlockCount;
        }
    }
}
