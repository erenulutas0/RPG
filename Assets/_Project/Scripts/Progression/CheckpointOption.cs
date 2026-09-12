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
            if (kind != CheckpointKind.Extract && kind != CheckpointKind.Descend)
                throw new ArgumentOutOfRangeException(nameof(kind));

            Kind = kind;
            Name = name ?? string.Empty;
            Description = description ?? string.Empty;
        }
    }
}
