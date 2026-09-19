namespace Cryptforge.Progression
{
    // Serialized nowhere, but reported by name in telemetry: append, never reorder.
    public enum CheckpointKind
    {
        Extract = 0,
        Descend = 1,
        // A swarm room's two: stay for another wave, or take what the room has paid so far and move on. The same
        // service, the same prompt and the same panel as Extract and Descend, because it is the same decision.
        PressOn = 2,
        Leave = 3
    }
}
