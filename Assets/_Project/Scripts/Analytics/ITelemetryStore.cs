namespace Cryptforge.Analytics
{
    // Where serialized lines end up. The log only knows this seam, so tests use an in-memory or failing store.
    public interface ITelemetryStore
    {
        // A human-readable place for the lines (a directory for the file store).
        string Location { get; }

        // Appends complete lines, each ending in '\n'. May throw IOException or UnauthorizedAccessException; the log
        // keeps the lines and retries later.
        void Append(string text);
    }
}
