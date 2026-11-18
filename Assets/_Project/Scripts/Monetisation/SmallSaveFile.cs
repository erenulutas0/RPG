using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Cryptforge.Monetisation
{
    // One small text file written the way ArenaSaveStore writes the arena's (and the profile's store its own): the text
    // goes to a temp file, written and flushed to the disk, the previous file moves to a backup, then the temp file moves
    // into place. A crash at any point leaves at least one whole copy among the three; the owner of the file reads every
    // readable copy and keeps the one with the highest revision. UTF-8 without a byte-order mark.
    internal sealed class SmallSaveFile
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        private readonly string _path;
        private readonly string _tempPath;
        private readonly string _backupPath;

        public string Directory { get; }

        public SmallSaveFile(string directory, string fileName)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("A directory is required.", nameof(directory));
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("A file name is required.", nameof(fileName));
            Directory = directory;
            _path = Path.Combine(directory, fileName);
            _tempPath = _path + ".tmp";
            _backupPath = _path + ".bak";
        }

        // May throw IOException or UnauthorizedAccessException; the caller decides what a failed write costs.
        public void Write(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            System.IO.Directory.CreateDirectory(Directory);
            using (var stream = new FileStream(_tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, Utf8))
            {
                writer.Write(text);
                writer.Flush();
                stream.Flush(true);
            }
            if (File.Exists(_path))
            {
                if (File.Exists(_backupPath))
                    File.Delete(_backupPath);
                File.Move(_path, _backupPath);
            }
            File.Move(_tempPath, _path);
        }

        // The texts of the file, its temp and its backup that exist and can be read, in that order; why any could not be
        // read goes to problems.
        public List<KeyValuePair<string, string>> ReadCopies(List<string> problems)
        {
            var copies = new List<KeyValuePair<string, string>>(3);
            TryRead(_path, copies, problems);
            TryRead(_tempPath, copies, problems);
            TryRead(_backupPath, copies, problems);
            return copies;
        }

        private static void TryRead(string path, List<KeyValuePair<string, string>> copies, List<string> problems)
        {
            if (!File.Exists(path))
                return;
            try
            {
                copies.Add(new KeyValuePair<string, string>(Path.GetFileName(path), File.ReadAllText(path, Utf8)));
            }
            catch (IOException exception)
            {
                problems?.Add($"{Path.GetFileName(path)} could not be read: {exception.Message}");
            }
            catch (UnauthorizedAccessException exception)
            {
                problems?.Add($"{Path.GetFileName(path)} could not be read: {exception.Message}");
            }
        }
    }
}
