using System;
using System.Globalization;
using System.IO;
using System.Text;
using Cryptforge.Analytics;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The file store keeps the log bounded on disk and every line whole, and survives its folder being removed or blocked.
    public sealed class TelemetryStoreTests
    {
        // Every sample line is exactly this many bytes including its newline, so file sizes are predictable.
        private const int LineBytes = 30;

        private string _directory;

        [SetUp]
        public void CreateFolder()
        {
            _directory = Path.Combine(Path.GetTempPath(), "cryptforge-telemetry-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void DeleteFolder()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        [Test]
        public void RotationKeepsAtMostMaxFilesDropsTheOldestAndNeverSplitsALine()
        {
            var store = new FileTelemetryStore(_directory, 100, 3);
            Assert.That(store.Location, Is.EqualTo(_directory));
            Assert.That(Encoding.UTF8.GetByteCount(Line(0, 11)), Is.EqualTo(LineBytes), "Three lines fit in 100 bytes, four do not.");

            // Batches of different sizes rotate exactly like single lines: three 30-byte lines fit in 100 bytes.
            int next = 0;
            foreach (int batch in new[] { 1, 2, 3, 5, 4, 5 })
            {
                var text = new StringBuilder();
                for (int i = 0; i < batch; i++)
                    text.Append(Line(next++, 11));
                store.Append(text.ToString());

                string[] files = Directory.GetFiles(_directory);
                Assert.That(files.Length, Is.LessThanOrEqualTo(3), $"After {next} lines.");
                foreach (string file in files)
                {
                    Assert.That(new FileInfo(file).Length, Is.LessThanOrEqualTo(100), $"{Path.GetFileName(file)} after {next} lines.");
                    Assert.That(File.ReadAllText(file), Does.EndWith("\n"), "No file ends in the middle of a line.");
                }
            }

            Assert.That(ReadSlot(2), Is.EqualTo(Line(12, 11) + Line(13, 11) + Line(14, 11)), "Lines 0-11 were rotated out.");
            Assert.That(ReadSlot(1), Is.EqualTo(Line(15, 11) + Line(16, 11) + Line(17, 11)));
            Assert.That(ReadSlot(0), Is.EqualTo(Line(18, 11) + Line(19, 11)));
            Assert.That(Directory.GetFiles(_directory).Length, Is.EqualTo(3));

            // A line longer than a whole file is kept whole in a file of its own.
            string big = Line(99, 131);
            Assert.That(Encoding.UTF8.GetByteCount(big), Is.EqualTo(150));
            store.Append(big);
            Assert.That(ReadSlot(0), Is.EqualTo(big));
            Assert.That(ReadSlot(1), Is.EqualTo(Line(18, 11) + Line(19, 11)));
            Assert.That(ReadSlot(2), Is.EqualTo(Line(15, 11) + Line(16, 11) + Line(17, 11)));

            store.Append(Line(20, 11));
            Assert.That(ReadSlot(0), Is.EqualTo(Line(20, 11)));
            Assert.That(ReadSlot(1), Is.EqualTo(big));
            Assert.That(ReadSlot(2), Is.EqualTo(Line(18, 11) + Line(19, 11)));
            Assert.That(Directory.GetFiles(_directory).Length, Is.EqualTo(3));
            Assert.That(FileTelemetryStore.FileNameAt(0), Is.EqualTo(FileTelemetryStore.FileName));
            Assert.That(FileTelemetryStore.FileNameAt(2), Is.EqualTo("events.2.jsonl"));
        }

        [Test]
        public void ASingleFileStoreDeletesStaleRotatedFilesWhenItRotates()
        {
            File.WriteAllText(Path.Combine(_directory, "events.1.jsonl"), "stale\n");
            File.WriteAllText(Path.Combine(_directory, "events.5.jsonl"), "stale from a larger limit\n");
            var store = new FileTelemetryStore(_directory, 100, 1);

            for (int i = 0; i < 3; i++)
                store.Append(Line(i, 11));
            Assert.That(ReadSlot(0), Is.EqualTo(Line(0, 11) + Line(1, 11) + Line(2, 11)), "No rotation before the file is full.");

            store.Append(Line(3, 11));
            Assert.That(Directory.GetFiles(_directory), Is.EqualTo(new[] { Path.Combine(_directory, FileTelemetryStore.FileName) }));
            Assert.That(ReadSlot(0), Is.EqualTo(Line(3, 11)));
        }

        [Test]
        public void TheFolderIsCreatedAgainAfterItWasDeleted()
        {
            string nested = Path.Combine(_directory, "profile", "telemetry");
            var store = new FileTelemetryStore(nested);
            store.Append("{\"n\":1}\n");
            Assert.That(File.ReadAllText(Path.Combine(nested, FileTelemetryStore.FileName)), Is.EqualTo("{\"n\":1}\n"));

            Directory.Delete(Path.Combine(_directory, "profile"), true);
            const string text = "{\"id\":\"\u0130\u011f\u00fc\u015f\"}\n";
            store.Append(text);

            string path = Path.Combine(nested, FileTelemetryStore.FileName);
            Assert.That(File.ReadAllText(path), Is.EqualTo(text), "Only the new line, after the folder came back.");
            byte[] bytes = File.ReadAllBytes(path);
            Assert.That(bytes[0], Is.EqualTo((byte)'{'), "UTF-8 without a byte-order mark.");
            Assert.That(bytes.Length, Is.EqualTo(Encoding.UTF8.GetByteCount(text)));
        }

        [Test]
        public void ABlockedFolderThrowsAnIoErrorAndBadInputIsRejected()
        {
            string blocked = Path.Combine(_directory, "telemetry");
            File.WriteAllText(blocked, "a file where the folder should be");
            var store = new FileTelemetryStore(blocked);
            Assert.That(() => store.Append("{}\n"), Throws.InstanceOf<IOException>());

            var fine = new FileTelemetryStore(_directory);
            Assert.Throws<ArgumentNullException>(() => fine.Append(null));
            Assert.Throws<ArgumentException>(() => fine.Append("{\"half\":"), "Only complete lines are appended.");
            fine.Append(string.Empty);
            Assert.That(Directory.GetFiles(_directory, "events*"), Is.Empty, "Nothing to write writes nothing.");

            Assert.Throws<ArgumentException>(() => new FileTelemetryStore(null));
            Assert.Throws<ArgumentException>(() => new FileTelemetryStore(string.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FileTelemetryStore(_directory, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FileTelemetryStore(_directory, 100, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => FileTelemetryStore.FileNameAt(-1));
        }

        [Test]
        public void TheLogKeepsLinesWhileTheFolderIsBlockedAndWritesThemOnceItClears()
        {
            string folder = Path.Combine(_directory, "telemetry");
            File.WriteAllText(folder, "blocked");
            var clock = new TelemetryLogTests.FakeClock();
            var log = new TelemetryLog(new FileTelemetryStore(folder), clock);
            log.Enqueue("{\"n\":1}");
            log.Enqueue("{\"n\":2}");

            Assert.That(log.Flush(), Is.False);
            Assert.That(log.FailedWrites, Is.EqualTo(1));
            Assert.That(log.LastError, Does.StartWith(nameof(IOException)));
            Assert.That(log.Buffered, Is.EqualTo(2));

            File.Delete(folder);
            log.Enqueue("{\"n\":3}");
            Assert.That(log.Flush(), Is.True);
            Assert.That(File.ReadAllText(Path.Combine(folder, FileTelemetryStore.FileName)), Is.EqualTo("{\"n\":1}\n{\"n\":2}\n{\"n\":3}\n"));
            Assert.That(log.Written, Is.EqualTo(3));
        }

        private string ReadSlot(int index)
        {
            return File.ReadAllText(Path.Combine(_directory, FileTelemetryStore.FileNameAt(index)));
        }

        // {"n":007,"pad":"xxxxxxxxxxx"} plus a newline: 19 bytes plus the padding.
        private static string Line(int number, int padding)
        {
            return "{\"n\":" + number.ToString("D3", CultureInfo.InvariantCulture) + ",\"pad\":\"" + new string('x', padding) + "\"}\n";
        }
    }
}
