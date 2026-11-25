namespace Cryptforge.Save
{
    public enum ProfileLoadStatus
    {
        // No profile files: first launch.
        New,
        Loaded,
        // The main file was missing, damaged or older than an interrupted save; a temp or backup file was used.
        Recovered,
        // Profile files existed but none was readable; progress starts over and the damaged file is kept.
        Reset,
        // A profile file (the main, the temp or the backup) was written by a newer build of the game: its save version is
        // above this build's. It is not damaged, so it is never read, moved, written over or deleted: the session plays on a
        // new profile in memory and the store writes nothing (ProfileStore.IsReadOnly) until a build that reads it returns.
        NewerVersion
    }
}
