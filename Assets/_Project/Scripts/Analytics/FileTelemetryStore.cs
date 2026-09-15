using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Cryptforge.Analytics
{
    // Appends telemetry lines to events.jsonl in one folder and rotates by size: when the next line would push the
    // current file past maxFileBytes it becomes events.1.jsonl, events.1 becomes events.2, and so on, and anything past
    // the oldest kept slot is deleted. The folder therefore never holds more than maxFiles files of about maxFileBytes
    // each. Lines are never split across files, and a write that fails part-way is cut back so no half line is left.
    // The folder is created on every append, so a folder deleted while the game runs comes back.
    public sealed class FileTelemetryStore : ITelemetryStore
    {
        public const string FileName = "events.jsonl";
        private const string RotatedPrefix = "events.";
        private const string RotatedExtension = ".jsonl";

        // No byte-order mark, so every file of the set is plain JSON Lines; invalid text is replaced, never thrown on.
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        private readonly string _path;
        private readonly long _maxFileBytes;
        private readonly int _maxFiles;

        public string Location { get; }

        public FileTelemetryStore(string directory, long maxFileBytes = 256 * 1024, int maxFiles = 3)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("A telemetry directory is required.", nameof(directory));
            if (maxFileBytes < 1)
                throw new ArgumentOutOfRangeException(nameof(maxFileBytes));
            if (maxFiles < 1)
                throw new ArgumentOutOfRangeException(nameof(maxFiles));

            Location = directory;
            _path = Path.Combine(directory, FileName);
            _maxFileBytes = maxFileBytes;
            _maxFiles = maxFiles;
        }

        // The file name of a slot: 0 is the current file, 1 the most recently rotated one.
        public static string FileNameAt(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            return index == 0 ? FileName : RotatedPrefix + index.ToString(CultureInfo.InvariantCulture) + RotatedExtension;
        }

        public void Append(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            if (text.Length == 0)
                return;
            if (text[text.Length - 1] != '\n')
                throw new ArgumentException("Telemetry text must end with a complete line.", nameof(text));

            byte[] bytes = Utf8.GetBytes(text);
            Directory.CreateDirectory(Location);
            long length = File.Exists(_path) ? new FileInfo(_path).Length : 0;

            // Walk the lines (0x0A never occurs inside a multi-byte UTF-8 sequence) and write them in runs that fit.
            int chunkStart = 0;
            int lineStart = 0;
            while (lineStart < bytes.Length)
            {
                int lineEnd = Array.IndexOf(bytes, (byte)'\n', lineStart) + 1;
                long pending = lineStart - chunkStart;
                long lineBytes = lineEnd - lineStart;
                // A line larger than a whole file still gets a file of its own rather than being cut.
                if (length + pending > 0 && length + pending + lineBytes > _maxFileBytes)
                {
                    if (pending > 0)
                        Write(bytes, chunkStart, (int)pending);
                    Rotate();
                    length = 0;
                    chunkStart = lineStart;
                }
                lineStart = lineEnd;
            }

            if (lineStart > chunkStart)
                Write(bytes, chunkStart, lineStart - chunkStart);
        }

        private void Write(byte[] bytes, int offset, int count)
        {
            // No fsync: telemetry is written on the main thread and may lose its last batch to a power cut, unlike the
            // profile; the operating system still gets the bytes before this returns. Unbuffered (a buffer size of 1), so
            // a full disk fails inside Write and the cut-back below works: a buffered stream would fail again in SetLength
            // and in Dispose while retrying its buffer, leaving the half line it managed to write.
            using (var stream = new FileStream(_path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 1))
            {
                long start = stream.Seek(0, SeekOrigin.End);
                try
                {
                    stream.Write(bytes, offset, count);
                    stream.Flush();
                }
                catch
                {
                    TryTruncate(stream, start);
                    throw;
                }
            }
        }

        private static void TryTruncate(FileStream stream, long length)
        {
            try
            {
                stream.SetLength(length);
            }
            catch (IOException)
            {
                // The original failure is the one worth reporting; a file that cannot be cut back keeps its tail.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private void Rotate()
        {
            // Slots at or past the oldest kept one are deleted first, including slots left by a larger maxFiles.
            string[] rotated = Directory.GetFiles(Location, RotatedPrefix + "*" + RotatedExtension);
            for (int i = 0; i < rotated.Length; i++)
            {
                int index = RotatedIndex(Path.GetFileName(rotated[i]));
                if (index >= _maxFiles - 1 && index >= 1)
                    File.Delete(rotated[i]);
            }

            for (int index = _maxFiles - 2; index >= 1; index--)
                Move(Path.Combine(Location, FileNameAt(index)), Path.Combine(Location, FileNameAt(index + 1)));

            if (_maxFiles > 1)
                Move(_path, Path.Combine(Location, FileNameAt(1)));
            else if (File.Exists(_path))
                File.Delete(_path);
        }

        private static void Move(string from, string to)
        {
            if (!File.Exists(from))
                return;
            // File.Move cannot overwrite on the .NET Standard profile Unity builds against.
            if (File.Exists(to))
                File.Delete(to);
            File.Move(from, to);
        }

        // The N of "events.N.jsonl", or -1 for any other name.
        private static int RotatedIndex(string fileName)
        {
            int middle = fileName.Length - RotatedPrefix.Length - RotatedExtension.Length;
            if (middle < 1
                || !fileName.StartsWith(RotatedPrefix, StringComparison.Ordinal)
                || !fileName.EndsWith(RotatedExtension, StringComparison.Ordinal))
                return -1;

            string digits = fileName.Substring(RotatedPrefix.Length, middle);
            return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out int index) ? index : -1;
        }
    }
}
