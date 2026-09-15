using System;
using System.Globalization;
using System.Threading;
using Cryptforge.Analytics;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The line format is the contract with whoever reads the log later: key rules, envelope order, escaping and numbers
    // that do not change with the phone's language.
    public sealed class TelemetryJsonTests
    {
        private static readonly DateTime SampleTime = new DateTime(2026, 9, 15, 13, 4, 5, 67, DateTimeKind.Utc);

        [Test]
        public void FieldKeysMustBeUniqueSnakeCaseAndNotEnvelopeNames()
        {
            var fields = new TelemetryFields().Add("a", 1L).Add("hero_id", "hero_vanguard").Add("floor_2", true);
            Assert.That(fields.Count, Is.EqualTo(3));
            Assert.That(fields.KeyAt(1), Is.EqualTo("hero_id"));
            Assert.That(fields.JsonValueAt(1), Is.EqualTo("\"hero_vanguard\""));
            Assert.That(fields.IndexOf("floor_2"), Is.EqualTo(2));
            Assert.That(fields.IndexOf("missing"), Is.EqualTo(-1));

            Assert.Throws<ArgumentNullException>(() => fields.Add(null, 1L));
            foreach (string bad in new[] { "", "Hero", "hero_Id", "2x", "_x", "hero-id", "hero id", "hero.id", "\u0131d", "\u0130d" })
            {
                Assert.That(TelemetryFields.IsValidKey(bad), Is.False, $"'{bad}' is not snake_case.");
                Assert.Throws<ArgumentException>(() => fields.Add(bad, 1L), $"'{bad}' was accepted.");
            }
            foreach (string envelope in new[] { "v", "seq", "t", "st", "session", "run", "event" })
                Assert.Throws<ArgumentException>(() => fields.Add(envelope, 1L), $"The envelope key '{envelope}' was accepted.");
            Assert.Throws<ArgumentException>(() => fields.Add("hero_id", "hero_other"), "A duplicate key was accepted.");
            Assert.That(fields.Count, Is.EqualTo(3), "A rejected key adds nothing.");
            Assert.Throws<ArgumentOutOfRangeException>(() => fields.KeyAt(3));
            Assert.Throws<ArgumentOutOfRangeException>(() => fields.JsonValueAt(-1));

            Assert.Throws<ArgumentException>(() => new TelemetryRecord(1, SampleTime, 0, "s", null, "RunStart", null));
            Assert.Throws<ArgumentNullException>(() => new TelemetryRecord(1, SampleTime, 0, "s", null, null, null));
            Assert.Throws<ArgumentNullException>(() => new TelemetryRecord(1, SampleTime, 0, null, null, "run_start", null));
            Assert.Throws<ArgumentException>(() => new TelemetryRecord(1, SampleTime, 0, "s", "", "run_start", null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TelemetryRecord(0, SampleTime, 0, "s", null, "run_start", null));
            Assert.Throws<ArgumentException>(() => TelemetryJson.Line(default), "An empty record has nothing to write.");
        }

        [Test]
        public void ALineWritesTheEnvelopeInOrderThenTheFieldsInInsertionOrder()
        {
            var fields = new TelemetryFields()
                .Add("floor_index", 1L)
                .Add("floor_id", "floor_ember_halls")
                .Add("ratio", 0.25)
                .Add("healthy", true)
                .Add("room_kind", "boss");
            var inRun = new TelemetryRecord(42, SampleTime, 12.3456, "abc", "abc-2", "room_start", fields);

            string line = TelemetryJson.Line(inRun);
            Assert.That(line, Is.EqualTo(
                "{\"v\":1,\"seq\":42,\"t\":\"2026-09-15T13:04:05.067Z\",\"st\":12.346,\"session\":\"abc\",\"run\":\"abc-2\"," +
                "\"event\":\"room_start\",\"floor_index\":1,\"floor_id\":\"floor_ember_halls\",\"ratio\":0.250,\"healthy\":true," +
                "\"room_kind\":\"boss\"}"));
            Assert.That(line, Does.Not.Contain("\n"), "One line without a trailing newline.");

            var outsideRun = new TelemetryRecord(43, SampleTime.AddMilliseconds(1), 0, "abc", null, "currency_spent", null);
            Assert.That(TelemetryJson.Line(outsideRun), Is.EqualTo(
                "{\"v\":1,\"seq\":43,\"t\":\"2026-09-15T13:04:05.068Z\",\"st\":0.000,\"session\":\"abc\",\"event\":\"currency_spent\"}"),
                "No run key outside a run, and no fields when there are none.");
            Assert.That(TelemetryJson.SchemaVersion, Is.EqualTo(1));
        }

        [Test]
        public void StringsAreEscapedPerRfc8259()
        {
            Assert.That(TelemetryJson.Quote("plain_id"), Is.EqualTo("\"plain_id\""));
            Assert.That(TelemetryJson.Quote("say \"hi\" \\ back/slash"), Is.EqualTo("\"say \\\"hi\\\" \\\\ back/slash\""));
            Assert.That(TelemetryJson.Quote("a\nb\r\tc\u0000\u0001\u001f"), Is.EqualTo("\"a\\u000ab\\u000d\\u0009c\\u0000\\u0001\\u001f\""),
                "Control characters are written as \\u escapes, so the line never breaks.");
            Assert.That(TelemetryJson.Quote("\u007f \u00e9 \u011f \ud83d\udd25"), Is.EqualTo("\"\u007f \u00e9 \u011f \ud83d\udd25\""),
                "Printable text and a complete surrogate pair are written as they are.");
            Assert.That(TelemetryJson.Quote("x\ud800y\udc00"), Is.EqualTo("\"x\\ud800y\\udc00\""), "Lone surrogates are escaped.");
            Assert.Throws<ArgumentNullException>(() => TelemetryJson.Quote(null));

            var fields = new TelemetryFields().Add("item_id", "a\"b\n");
            string line = TelemetryJson.Line(new TelemetryRecord(1, SampleTime, 1, "se\"ss", "run\\1", "chest_opened", fields));
            Assert.That(line, Is.EqualTo(
                "{\"v\":1,\"seq\":1,\"t\":\"2026-09-15T13:04:05.067Z\",\"st\":1.000,\"session\":\"se\\\"ss\",\"run\":\"run\\\\1\"," +
                "\"event\":\"chest_opened\",\"item_id\":\"a\\\"b\\u000a\"}"));
        }

        [Test]
        public void NumbersAndTimesStayInvariantUnderATurkishCulture()
        {
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;
            CultureInfo previousUi = Thread.CurrentThread.CurrentUICulture;
            try
            {
                var turkish = new CultureInfo("tr-TR");
                Thread.CurrentThread.CurrentCulture = turkish;
                Thread.CurrentThread.CurrentUICulture = turkish;
                Assert.That(1.5.ToString(), Is.EqualTo("1,5"), "The culture is really in effect for this test.");

                var fields = new TelemetryFields()
                    .Add("heal_fraction", 1.5)
                    .Add("active_sec", -1234567.8916)
                    .Add("small", 0.0004)
                    .Add("gold", -42L)
                    .Add("big", 9007199254740993L)
                    .Add("id", "\u0130stanbul");
                string line = TelemetryJson.Line(new TelemetryRecord(1234567, SampleTime, 3600.5, "s", null, "room_complete", fields));

                Assert.That(line, Is.EqualTo(
                    "{\"v\":1,\"seq\":1234567,\"t\":\"2026-09-15T13:04:05.067Z\",\"st\":3600.500,\"session\":\"s\",\"event\":\"room_complete\"," +
                    "\"heal_fraction\":1.500,\"active_sec\":-1234567.892,\"small\":0.000,\"gold\":-42,\"big\":9007199254740993," +
                    "\"id\":\"\u0130stanbul\"}"));
                Assert.That(TelemetryFields.IsValidKey("id"), Is.True, "Key rules do not use culture-aware casing.");
                Assert.That(TelemetryJson.Timestamp(SampleTime.ToLocalTime()), Is.EqualTo("2026-09-15T13:04:05.067Z"),
                    "A local time is converted to UTC.");
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
                Thread.CurrentThread.CurrentUICulture = previousUi;
            }
            Assert.That(Thread.CurrentThread.CurrentCulture, Is.EqualTo(previous));
        }

        [Test]
        public void NullAndNonFiniteValuesAreWrittenAsNull()
        {
            var fields = new TelemetryFields()
                .Add("relic_id", (string)null)
                .Add("nan", double.NaN)
                .Add("up", double.PositiveInfinity)
                .Add("down", double.NegativeInfinity)
                .Add("tiny_negative", -0.0004)
                .Add("single", 0.1f);
            Assert.That(fields.JsonValueAt(0), Is.EqualTo("null"));

            string line = TelemetryJson.Line(new TelemetryRecord(7, SampleTime, double.NaN, "s", "s-1", "run_start", fields));
            Assert.That(line, Is.EqualTo(
                "{\"v\":1,\"seq\":7,\"t\":\"2026-09-15T13:04:05.067Z\",\"st\":null,\"session\":\"s\",\"run\":\"s-1\",\"event\":\"run_start\"," +
                "\"relic_id\":null,\"nan\":null,\"up\":null,\"down\":null,\"tiny_negative\":0.000,\"single\":0.100}"));
        }
    }
}
