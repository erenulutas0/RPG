namespace Cryptforge.Analytics
{
    // The game's analytics sink (docs/59 section 3.3): one event of the game's own, by its snake_case name and its fields,
    // as the telemetry already writes them (docs/10). LocalFileAnalytics is the local JSON Lines log behind it; the
    // GameAnalytics backend joins it at SDK integration (docs/59 3.5) through AnalyticsFanOut, mapping the game's events to
    // GameAnalytics' own (progression, resource, business, ad and design events). A sink reads the fields and never adds
    // to them: one instance goes to every sink. Main thread only.
    public interface IAnalytics
    {
        // Under the current run, if any.
        void Emit(string name, TelemetryFields fields = null);

        // Under a given run id (null: outside any run): a result screen's events belong to the run that just ended.
        void EmitForRun(string runId, string name, TelemetryFields fields = null);

        // Writes or sends what is buffered: before a video, and as the app leaves the foreground. True when nothing is left.
        bool Flush();
    }
}
