namespace Cryptforge.Progression
{
    // Serialized nowhere, but reported by name in telemetry: append, never reorder. Values 2 and 3 were a swarm room's
    // Press on and Leave, retired when leaving became a place on the arena (docs/27, S6c); they stay unused rather than
    // reassigned, so an old telemetry line never means something new.
    public enum CheckpointKind
    {
        Extract = 0,
        Descend = 1
    }
}
