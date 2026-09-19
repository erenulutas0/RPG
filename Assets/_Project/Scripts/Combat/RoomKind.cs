namespace Cryptforge.Combat
{
    // Serialized by value in floor assets; append new kinds, never reorder.
    public enum RoomKind
    {
        Combat = 0,
        Forge = 1,
        Elite = 2,
        Boss = 3,
        // A room whose waves do not run out: it hands out its authored waves in order, repeats the last of them, and
        // hardens wave by wave until the player leaves or dies (SwarmRule, docs/27 S4).
        Swarm = 4
    }
}
