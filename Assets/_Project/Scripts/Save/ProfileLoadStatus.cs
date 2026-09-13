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
        Reset
    }
}
