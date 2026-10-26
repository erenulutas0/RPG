using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Cryptforge.Combat;

namespace Cryptforge.Progression
{
    // A weapon as a run's offers see it (docs/42, PR 3): what picking it up gives - a fresh runtime - and what each later
    // pick of it can give - one line of its own table. Pure, so UpgradeService never holds a ScriptableObject: the scene
    // builds one from each WeaponDefinition (CreateCard) and the Descent simulation from its HeroWeapon, and a content
    // test holds the two equal, since numbers restated by hand drift (docs/21, the movement budget).
    public sealed class WeaponCard
    {
        private readonly Func<WeaponRuntime> _createRuntime;

        // The WeaponDefinition's id, which is what the loadout tells two weapons apart by.
        public string Id { get; }
        public string DisplayName { get; }
        // The card that offers the weapon to a hero that does not carry it. It applies no modifier of its own - taking it
        // fills a slot - so its stat is only there because every card has one, and its text is the weapon's description.
        public UpgradeOption Acquire { get; }
        // One option per line of the weapon's table, each a WeaponUpgrade card. Seven picks of them take the weapon from
        // level 1 to WeaponLoadout.MaxLevel (the report's 40 section 5.1); a line can come up again, so its MaxStacks is
        // informational and the weapon's level is the limit.
        public IReadOnlyList<UpgradeOption> Lines { get; }
        // The card this weapon evolves into, and the tome it evolves with; null for a weapon with no evolution yet. Read
        // by nothing until the weapon evolutions land (docs/42, PR 5).
        public UpgradeOption Evolution { get; }
        public string EvolvesWithTomeId { get; }

        public WeaponCard(string id, string displayName, string description, Func<WeaponRuntime> createRuntime,
            IReadOnlyList<UpgradeOption> lines, UpgradeOption evolution = null, string evolvesWithTomeId = null)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("A weapon card needs its definition's id.", nameof(id));
            _createRuntime = createRuntime ?? throw new ArgumentNullException(nameof(createRuntime));
            if (lines == null)
                throw new ArgumentNullException(nameof(lines));
            if (evolvesWithTomeId != null && evolvesWithTomeId.Length == 0)
                throw new ArgumentException("Name the tome, or pass null for none.", nameof(evolvesWithTomeId));

            var copies = new UpgradeOption[lines.Count];
            for (int i = 0; i < lines.Count; i++)
            {
                UpgradeOption line = lines[i];
                if (line == null)
                    throw new ArgumentException("A weapon's lines must be non-null.", nameof(lines));
                // A line on a hero stat would have no runtime to write to; the hero's stats are the general cards'.
                if (!WeaponRuntime.Owns(line.Stat))
                    throw new ArgumentException($"Line {line.Id} is on {line.Stat}, which is not a weapon stat.", nameof(lines));
                if (line.Id == id)
                    throw new ArgumentException("A line cannot share its weapon's id.", nameof(lines));
                for (int j = 0; j < i; j++)
                    if (copies[j].Id == line.Id)
                        throw new ArgumentException($"Line {line.Id} appears twice.", nameof(lines));
                copies[i] = line.Category == OfferCategory.WeaponUpgrade ? line : line.WithCategory(OfferCategory.WeaponUpgrade);
            }

            Id = id;
            DisplayName = displayName ?? string.Empty;
            // The description is shown as is: braces are doubled so a card's text formatter never reads one as a slot.
            string text = (description ?? string.Empty).Replace("{", "{{").Replace("}", "}}");
            Acquire = new UpgradeOption(id, DisplayName, text, UpgradeStat.Damage,
                new StatModifier(ModifierOperation.Flat, 0f), 1, null, null, OfferCategory.NewWeapon);
            Lines = new ReadOnlyCollection<UpgradeOption>(copies);
            Evolution = evolution;
            EvolvesWithTomeId = evolvesWithTomeId;
        }

        // A new runtime each call: the loadout seeds it and replays the tomes onto it as it takes the slot.
        public WeaponRuntime CreateRuntime()
        {
            WeaponRuntime runtime = _createRuntime();
            if (runtime == null)
                throw new InvalidOperationException($"Weapon {Id} made no runtime.");
            return runtime;
        }
    }
}
