namespace Cryptforge.Progression
{
    public readonly struct ChoiceCard
    {
        public string Name { get; }
        public string Description { get; }

        public ChoiceCard(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
