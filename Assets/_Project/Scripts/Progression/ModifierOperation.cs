namespace Cryptforge.Progression
{
    // Serialized by value in upgrade assets; append new operations, never reorder.
    public enum ModifierOperation
    {
        Flat = 0,
        Percent = 1,
        // A factor of its own on the final value, (1 + amount), never summed with the percents: the gear's layer (docs/54
        // section 20, the lead's decision of 2026-11-17), so an item's "+14 %" is 14 % more whatever the cards and tomes
        // hold. No card carries it.
        Multiply = 2
    }
}
