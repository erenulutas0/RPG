using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // What the hero carries on the foundry, said on the top panel (docs/42 section 4). A category-mode run holds up to
    // three weapons and three tomes, and before this the player could see neither: the legacy row named the forge's
    // weapon and counted two cards that category mode never offers. Six compact badges take that row's place - weapons
    // left in slot order, tomes right in the order they were taken - each a short name over its level, with the evolved
    // mark on a weapon that has taken its evolution. An empty slot shows as a faint plate, so the row also says how many
    // more a build can take. RunHud adds it in category mode only; it builds itself in code on the HUD's safe area, so the
    // scene does not change.
    public sealed class LoadoutBadgesView : MonoBehaviour
    {
        public const int WeaponSlots = WeaponLoadout.MaxSlots;
        public const int TomeSlots = UpgradeService.MaxTomes;
        // On the 1080-wide canvas the row spans x 52-634 and y 158-230 down from the safe area's top: the band the forge
        // weapon's name (x 52-364, y 163-217) and the two legacy badges (x 420-616, y 155-233) held, both hidden in
        // category mode. That keeps it below the floor label (to 122) and the room pips (to 141), left of the relic badge
        // (from 656) and the danger readout (from 752), and inside the top surface (to 252), so the boss name below the
        // surface (from 265) is never covered either.
        private const float RowLeft = 52f;
        private const float RowTop = 158f;
        private const float BadgeWidth = 90f;
        private const float BadgeHeight = 72f;
        private const float BadgeGap = 6f;
        // Wider than the gap inside a group, so the weapons and the tomes read as two sets of three.
        private const float GroupGap = 18f;
        private const float AccentHeight = 4f;
        // A name this short is shown whole ("Sword", "Force"); a longer one is cut to its first letters ("Dag", "Pre"),
        // which keeps the authored names distinct while fitting 90 px at a legible size.
        private const int WholeNameLength = 5;
        private const int AbbreviationLength = 3;
        private const float EmptyAlpha = .3f;

        private static readonly Color WeaponPlate = new Color(.16f, .11f, .08f, .86f);
        private static readonly Color TomePlate = new Color(.08f, .10f, .17f, .86f);
        private static readonly Color WeaponAccent = new Color(.93f, .62f, .30f, 1f);
        private static readonly Color TomeAccent = new Color(.48f, .68f, .98f, 1f);
        private static readonly Color EvolvedInk = new Color(1f, .80f, .30f, 1f);
        private static readonly Color Ink = new Color(.93f, .91f, .85f, 1f);
        // Levels run 1-8, so a level never formats a new string.
        private static readonly string[] Digits = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        private sealed class Badge
        {
            public Image Plate;
            public Image Accent;
            public Text Name;
            public Text Level;
            public Color PlateColour;
            public Color AccentColour;
            public string ShownId;
            public int ShownLevel;
            public bool ShownEvolved;
            public bool Filled;
            public bool ShownEmpty;
        }

        private CombatSetup _setup;
        private PrototypeTextDefinition _text;
        private RectTransform _row;
        private readonly Badge[] _weapons = new Badge[WeaponSlots];
        private readonly Badge[] _tomes = new Badge[TomeSlots];
        // Short names by weapon or tome id, made once from the catalogue.
        private readonly Dictionary<string, string> _shortNames = new Dictionary<string, string>();
        // The tomes in the order this run took them, so a tome taken later joins on the right instead of reshuffling the
        // ones already shown; the service keeps only the catalogue order.
        private readonly List<string> _tomeOrder = new List<string>(TomeSlots);
        private bool _subscribed;

        // For tests and for anything that wants to read what the player is being shown.
        public RectTransform Row => _row;
        public int ShownWeaponBadges => CountFilled(_weapons);
        public int ShownTomeBadges => CountFilled(_tomes);
        public string WeaponBadgeText(int slot) => TextOf(_weapons, slot);
        public string TomeBadgeText(int slot) => TextOf(_tomes, slot);

        // style is the compact gold count: the row shares its parent, font and layer.
        internal void Initialize(CombatSetup setup, PrototypeTextDefinition text, Text style)
        {
            _setup = setup; _text = text;
            UpgradeService upgrades = _setup.Upgrades;
            if (upgrades.WeaponCards != null)
                for (int i = 0; i < upgrades.WeaponCards.Count; i++)
                    AddShortName(upgrades.WeaponCards[i].Id, upgrades.WeaponCards[i].DisplayName);
            if (upgrades.Tomes != null)
                for (int i = 0; i < upgrades.Tomes.Count; i++)
                    AddShortName(upgrades.Tomes[i].Id, upgrades.Tomes[i].DisplayName);

            var rowObject = new GameObject("Loadout Badges", typeof(RectTransform));
            rowObject.layer = style.gameObject.layer;
            _row = (RectTransform)rowObject.transform;
            _row.SetParent(style.rectTransform.parent, false);
            float groupWidth = WeaponSlots * BadgeWidth + (WeaponSlots - 1) * BadgeGap;
            _row.anchorMin = _row.anchorMax = new Vector2(0f, 1f);
            _row.pivot = new Vector2(0f, 1f);
            _row.anchoredPosition = new Vector2(RowLeft, -RowTop);
            _row.sizeDelta = new Vector2(groupWidth * 2f + GroupGap, BadgeHeight);
            for (int i = 0; i < WeaponSlots; i++)
                _weapons[i] = Build("Weapon Badge " + (i + 1), i * (BadgeWidth + BadgeGap), WeaponPlate, WeaponAccent, style);
            for (int i = 0; i < TomeSlots; i++)
                _tomes[i] = Build("Tome Badge " + (i + 1), groupWidth + GroupGap + i * (BadgeWidth + BadgeGap), TomePlate,
                    TomeAccent, style);

            // A pick raises OfferChanged after it is applied (a weapon taken raises WeaponAcquired just before it); a chest
            // book levels a tome without touching the offer, so it is heard on its own.
            upgrades.OfferChanged += Refresh;
            upgrades.BookGranted += OnBookGranted;
            _subscribed = true;
            Refresh();
        }

        private void AddShortName(string id, string displayName)
        {
            if (string.IsNullOrEmpty(id) || _shortNames.ContainsKey(id))
                return;
            string name = string.IsNullOrEmpty(displayName) ? id : displayName;
            _shortNames.Add(id, name.Length <= WholeNameLength ? name : name.Substring(0, AbbreviationLength));
        }

        private Badge Build(string objectName, float left, Color plate, Color accent, Text style)
        {
            var badge = new Badge { PlateColour = plate, AccentColour = accent };
            RectTransform rect = Child(objectName, _row, style);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, 0f);
            rect.sizeDelta = new Vector2(BadgeWidth, BadgeHeight);
            badge.Plate = rect.gameObject.AddComponent<Image>();
            badge.Plate.raycastTarget = false;

            // A strip along the bottom in the group's colour: y 68-72 inside the badge.
            RectTransform strip = Child("Accent", rect, style);
            strip.anchorMin = new Vector2(0f, 0f);
            strip.anchorMax = new Vector2(1f, 0f);
            strip.pivot = new Vector2(.5f, 0f);
            strip.anchoredPosition = Vector2.zero;
            strip.sizeDelta = new Vector2(0f, AccentHeight);
            badge.Accent = strip.gameObject.AddComponent<Image>();
            badge.Accent.raycastTarget = false;

            // The name on top (y 3-31) and the level under it (y 31-67), so neither crowds the other or the strip.
            badge.Name = Label("Name", rect, style, 1f, -3f, 28f, 24, 16, FontStyle.Normal);
            badge.Level = Label("Level", rect, style, 0f, AccentHeight + 1f, 36f, 34, 24, FontStyle.Bold);
            ShowEmpty(badge);
            return badge;
        }

        private static RectTransform Child(string objectName, Transform parent, Text style)
        {
            var child = new GameObject(objectName, typeof(RectTransform));
            child.layer = style.gameObject.layer;
            var rect = (RectTransform)child.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        // A full-width label pinned to the top (edge 1) or the bottom (edge 0) of its badge.
        private static Text Label(string objectName, RectTransform badge, Text style, float edge, float offset,
            float height, int size, int minimum, FontStyle fontStyle)
        {
            RectTransform rect = Child(objectName, badge, style);
            rect.anchorMin = new Vector2(0f, edge);
            rect.anchorMax = new Vector2(1f, edge);
            rect.pivot = new Vector2(.5f, edge);
            rect.anchoredPosition = new Vector2(0f, offset);
            rect.sizeDelta = new Vector2(-8f, height);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = style.font;
            label.fontSize = size;
            label.fontStyle = fontStyle;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = minimum;
            label.resizeTextMaxSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            label.color = Ink;
            return label;
        }

        private void OnBookGranted(UpgradeOption book, UpgradeRarity rarity) => Refresh();

        // On the service's change events only; a badge's strings are rebuilt when what it shows changes, never per frame.
        private void Refresh()
        {
            UpgradeService upgrades = _setup.Upgrades;
            WeaponLoadout loadout = upgrades.Loadout;
            for (int i = 0; i < WeaponSlots; i++)
            {
                if (loadout != null && i < loadout.Count)
                {
                    WeaponLoadout.Slot slot = loadout.At(i);
                    Show(_weapons[i], slot.WeaponId, slot.Level, upgrades.HasEvolved(slot.WeaponId));
                }
                else
                {
                    ShowEmpty(_weapons[i]);
                }
            }

            if (upgrades.Tomes != null)
                for (int i = 0; i < upgrades.Tomes.Count && _tomeOrder.Count < TomeSlots; i++)
                {
                    string id = upgrades.Tomes[i].Id;
                    if (upgrades.TomeLevel(id) > 0 && !_tomeOrder.Contains(id))
                        _tomeOrder.Add(id);
                }
            for (int i = 0; i < TomeSlots; i++)
            {
                if (i < _tomeOrder.Count)
                    Show(_tomes[i], _tomeOrder[i], upgrades.TomeLevel(_tomeOrder[i]), false);
                else
                    ShowEmpty(_tomes[i]);
            }
        }

        private void Show(Badge badge, string id, int level, bool evolved)
        {
            if (badge.Filled && badge.ShownId == id && badge.ShownLevel == level && badge.ShownEvolved == evolved)
                return;

            string name = _shortNames.TryGetValue(id, out string shortName) ? shortName : id;
            badge.Name.text = evolved ? string.Format(_text.LoadoutEvolvedFormat, name) : name;
            badge.Level.text = level >= 0 && level < Digits.Length ? Digits[level] : level.ToString();
            badge.Name.color = badge.Level.color = evolved ? EvolvedInk : Ink;
            badge.Plate.color = badge.PlateColour;
            badge.Accent.color = evolved ? EvolvedInk : badge.AccentColour;
            badge.ShownId = id;
            badge.ShownLevel = level;
            badge.ShownEvolved = evolved;
            badge.Filled = true;
            badge.ShownEmpty = false;
        }

        private static void ShowEmpty(Badge badge)
        {
            if (badge.ShownEmpty)
                return;

            badge.Name.text = string.Empty;
            badge.Level.text = string.Empty;
            Color plate = badge.PlateColour;
            plate.a *= EmptyAlpha;
            badge.Plate.color = plate;
            Color accent = badge.AccentColour;
            accent.a *= EmptyAlpha;
            badge.Accent.color = accent;
            badge.ShownId = null;
            badge.Filled = false;
            badge.ShownEmpty = true;
        }

        private static int CountFilled(Badge[] badges)
        {
            int filled = 0;
            for (int i = 0; i < badges.Length; i++)
                if (badges[i] != null && badges[i].Filled)
                    filled++;
            return filled;
        }

        private static string TextOf(Badge[] badges, int slot)
        {
            if (slot < 0 || slot >= badges.Length || badges[slot] == null || !badges[slot].Filled)
                return string.Empty;
            return badges[slot].Name.text + " " + badges[slot].Level.text;
        }

        private void OnDestroy()
        {
            if (!_subscribed || _setup == null || _setup.Upgrades == null)
                return;
            _setup.Upgrades.OfferChanged -= Refresh;
            _setup.Upgrades.BookGranted -= OnBookGranted;
        }
    }
}
