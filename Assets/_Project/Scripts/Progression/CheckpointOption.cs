using System;

namespace Cryptforge.Progression
{
    // Names and descriptions arrive formatted with the run's current numbers (gold, next floor).
    public sealed class CheckpointOption
    {
        public CheckpointKind Kind { get; }
        public string Name { get; }
        public string Description { get; }

        public CheckpointOption(CheckpointKind kind, string name, string description)
        {
            // Every kind the enum defines, not a hand-written pair: a swarm room's Press on and Leave arrive through
            // this same option, and a list of two was what kept them out when they were added.
            if (!Enum.IsDefined(typeof(CheckpointKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));

            Kind = kind;
            Name = name ?? string.Empty;
            Description = description ?? string.Empty;
        }
    }
}
