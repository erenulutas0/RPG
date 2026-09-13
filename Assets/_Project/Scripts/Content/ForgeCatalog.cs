using System;
using System.Collections.Generic;
using Cryptforge.Progression;
using UnityEngine;

namespace Cryptforge.Content
{
    // Builds the Relic Forge's options from their definitions and checks every entry once, before any profile or run
    // exists: unique ids, valid relic amounts, readable description formats and weapon combat data that can arm the hero.
    // A bad entry is reported by asset name at startup instead of failing when someone first equips it.
    public static class ForgeCatalog
    {
        public static bool TryCreate(IReadOnlyList<RelicDefinition> relicDefinitions, IReadOnlyList<WeaponDefinition> weaponDefinitions,
            out RelicOption[] relics, out WeaponOption[] weapons, out string error)
        {
            if (relicDefinitions == null)
                throw new ArgumentNullException(nameof(relicDefinitions));
            if (weaponDefinitions == null)
                throw new ArgumentNullException(nameof(weaponDefinitions));

            relics = null;
            weapons = null;
            var relicOptions = new RelicOption[relicDefinitions.Count];
            var weaponOptions = new WeaponOption[weaponDefinitions.Count];
            var ids = new HashSet<string>();
            ScriptableObject current = null;
            try
            {
                for (int i = 0; i < relicDefinitions.Count; i++)
                {
                    current = relicDefinitions[i];
                    if (current == null)
                        throw new ArgumentException($"Relic slot {i} is empty.");
                    relicOptions[i] = relicDefinitions[i].CreateOption();
                    if (!ids.Add(relicOptions[i].Id))
                        throw new ArgumentException($"The id {relicOptions[i].Id} is used twice.");
                }

                ids.Clear();
                for (int i = 0; i < weaponDefinitions.Count; i++)
                {
                    current = weaponDefinitions[i];
                    if (current == null)
                        throw new ArgumentException($"Weapon slot {i} is empty.");
                    weaponOptions[i] = weaponDefinitions[i].CreateForgeOption();
                    // A runtime validates the behavior, splash and crit data of every weapon, not only the carried one.
                    weaponDefinitions[i].CreateRuntime();
                    if (!ids.Add(weaponOptions[i].Id))
                        throw new ArgumentException($"The id {weaponOptions[i].Id} is used twice.");
                }
            }
            catch (Exception exception) when (exception is ArgumentException || exception is FormatException)
            {
                error = current != null ? $"{current.name}: {exception.Message}" : exception.Message;
                return false;
            }

            relics = relicOptions;
            weapons = weaponOptions;
            error = null;
            return true;
        }
    }
}
