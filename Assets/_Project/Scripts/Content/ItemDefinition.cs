using Cryptforge.Progression;
using UnityEngine;

namespace Cryptforge.Content
{
    // An armour, amulet or ring that drops in the Descent (docs/54 section 3.2): its id (what the save and telemetry
    // write), its name, its slot and the floor that drops it. Rarity, basma and lines belong to each dropped copy
    // (ItemInstance), not to the definition. The run reads the list from EconomyConfig.
    [CreateAssetMenu(menuName = "Cryptforge/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private GearSlot _slot = GearSlot.Armour;
        [SerializeField, Range(1, 3)] private int _floor = 1;

        public string Id => _id;
        public string DisplayName => _displayName;
        public GearSlot Slot => _slot;
        public int Floor => _floor;

        public ItemTemplate CreateTemplate() => new ItemTemplate(_id, _displayName, _slot, _floor);
    }
}
