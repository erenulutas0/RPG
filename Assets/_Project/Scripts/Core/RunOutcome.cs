namespace Cryptforge.Core
{
    public enum RunOutcome
    {
        None,
        // Cleared the final authored floor.
        Victory,
        Defeat,
        // Chose Extract at a floor checkpoint.
        Extracted
    }
}
