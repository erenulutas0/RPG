using Cryptforge.Progression;
using UnityEngine;

namespace Cryptforge.Content
{
    // An armour, amulet or ring that drops in the Descent (docs/54 section 3.2): its id (what the save and telemetry
    // write), its name, its slot, the floor that drops it and its painted icon. Rarity, basma and lines belong to each
    // dropped copy (ItemInstance), not to the definition. The run reads the list from EconomyConfig.
    [CreateAssetMenu(menuName = "Cryptforge/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private GearSlot _slot = GearSlot.Armour;
        [SerializeField, Range(1, 3)] private int _floor = 1;
        // The item's own icon on the Anvil's cards and in the bag (Tools/ArtReview/build_upgrade_icons.py paints it on
        // Art/UI/ItemIcons/UI_ItemIcons.png and writes this reference). Without one, AnvilView shows its slot's upgrade
        // card icon, as it did before the items had their own.
        [SerializeField] private Sprite _icon;

        public string Id => _id;
        public string DisplayName => _displayName;
        public GearSlot Slot => _slot;
        public int Floor => _floor;
        public Sprite Icon => _icon;

        public ItemTemplate CreateTemplate() => new ItemTemplate(_id, _displayName, _slot, _floor);
    }
}
