using System;
using UnityEngine;

namespace Cryptforge.Art
{
    // Every upgrade card's painted icon, by the card's content id (Tools/ArtReview/build_upgrade_icons.py paints the one
    // sheet they share, Art/UI/UpgradeIcons/UI_UpgradeIcons.png, and writes this asset). Each id has its card icon; a
    // weapon, a weapon's evolution and a tome also have a smaller one for the loadout row's sockets, painted from the same
    // glyph for that size rather than filtered down from the card's. RunChoiceView looks here before its own table, which
    // answers only an id this set lacks; LoadoutBadgesView shows the socket icons. An id it does not hold has none, and
    // the card or the socket stays text, as it was.
    [CreateAssetMenu(menuName = "Cryptforge/Art/Upgrade Icon Set")]
    public sealed class UpgradeIconSet : ScriptableObject
    {
        [Serializable]
        public struct Icon
        {
            [SerializeField] private string _id;
            [SerializeField] private Sprite _card;
            [SerializeField] private Sprite _slot;

            public Icon(string id, Sprite card, Sprite slot)
            {
                _id = id;
                _card = card;
                _slot = slot;
            }

            public string Id => _id;
            public Sprite Card => _card;
            // Null for an id the loadout row never shows: a weapon's line or a general card.
            public Sprite Slot => _slot;
        }

        [SerializeField] private Icon[] _icons;

        public int Count => _icons != null ? _icons.Length : 0;
        public Icon this[int index] => _icons[index];

        // The card icon for this id, or null when the set has none. A handful of entries, read only when a prompt opens,
        // so a scan allocates nothing and needs no table built.
        public Sprite CardIconFor(string id)
        {
            int index = IndexOf(id);
            return index >= 0 ? _icons[index].Card : null;
        }

        // The loadout socket's icon for this id, or null when the set has none for it.
        public Sprite SlotIconFor(string id)
        {
            int index = IndexOf(id);
            return index >= 0 ? _icons[index].Slot : null;
        }

        private int IndexOf(string id)
        {
            if (_icons == null || string.IsNullOrEmpty(id))
                return -1;
            for (int i = 0; i < _icons.Length; i++)
                if (_icons[i].Id == id)
                    return i;
            return -1;
        }
    }
}
