using System;
using Cryptforge.Content;
using Cryptforge.Localisation;
using Cryptforge.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // The Forge's Skills tab (docs/57 phase 4, section 14): the hero the next run plays (the forge's hero chooser) and its
    // three skills, each on a card in the Forge's look - its painted icon in the well, its name, what it does and its state,
    // the card's colour by state (gold forged, blue affordable, grey short of gold or waiting on skill 2, the plain card
    // locked). Skill 1 is the hero's own; skill 2 opens at floor 2's first clear and skill 3 at floor 3's (any tier, any
    // hero), then a tap forges it for 300 or 500 gold for this hero (PlayerProfile.TryForgeSkill), as a tap forges a relic
    // on the Relic Forge. The tab's entry stands at the right end of the Relic Forge's weapons header, on the forge's
    // primary plate while a skill waits affordable. Gold is earned in play: nothing here is sold for money.
    public sealed partial class AnvilView
    {
        public const string SkillsEntryName = "Forge Skills Button";
        // The page under the wallet: the hero's line, the three cards and the note.
        private const float SkillsTop = 338f;
        private const float SkillCardTop = 84f;
        private const float SkillCardHeight = 300f;
        private const float SkillCardGap = 28f;
        private const float SkillIconSize = 220f;
        private const int SkillPaintSlot = 6;
        private static readonly Color StateForged = new Color32(137, 213, 180, 255);
        private static readonly Color StateWaiting = new Color32(145, 188, 225, 255);
        private static readonly Color LockedIcon = new Color(.45f, .45f, .5f, .8f);

        private GameObject _skillsPage;
        private Text _skillsHero;
        private Text _skillsNote;
        private readonly PieceCard[] _skillCards = new PieceCard[HeroSkillUnlock.LastSlot];
        private readonly SkillForgeStatus[] _skillStates = new SkillForgeStatus[HeroSkillUnlock.LastSlot];

        // The skills' entry beside the weapons' header on the Relic Forge; null without the row.
        public Button SkillsEntry { get; private set; }
        // Whether the entry stands on the primary plate: a skill of the next run's hero waits affordable.
        public bool SkillsEntryCalls { get; private set; }
        // For tests: each slot's card (1-3), its words and state as shown, its icon.
        public Button SkillCardButton(int slot) => _skillCards[slot - 1]?.Button;
        public string SkillCardText(int slot) => _skillCards[slot - 1] == null ? string.Empty
            : _skillCards[slot - 1].Name.text + " | " + _skillCards[slot - 1].Info.text + " | " + _skillCards[slot - 1].Level.text;
        public SkillForgeStatus SkillCardStatus(int slot) => _skillStates[slot - 1];
        public Sprite SkillCardIcon(int slot) => _skillCards[slot - 1]?.Icon.sprite;
        public string SkillsHeroText => _skillsHero != null ? _skillsHero.text : string.Empty;

        // A button at the right end of a row of the forge (the weapons' header, whose words stand at its left), at the row's
        // own inset from the edge, as wide as the nameplate's entries.
        private Button RowEntry(Text row, string name, LocText text, Action open)
        {
            RectTransform rowRect = row.rectTransform;
            var entry = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            entry.layer = row.gameObject.layer;
            var rect = (RectTransform)entry.transform;
            rect.SetParent(rowRect.parent, false);
            rect.SetSiblingIndex(rowRect.GetSiblingIndex() + 1);
            rect.anchorMin = new Vector2(1f, rowRect.anchorMin.y);
            rect.anchorMax = new Vector2(1f, rowRect.anchorMax.y);
            rect.pivot = new Vector2(1f, .5f);
            float right = Mathf.Max(0f, -rowRect.offsetMax.x);
            rect.offsetMin = new Vector2(-(right + EntryWidth), 2f);
            rect.offsetMax = new Vector2(-right, -2f);
            var button = entry.GetComponent<Button>();
            button.targetGraphic = entry.GetComponent<Image>();
            Paint(button, false);
            Text label = Label(rect, name + " Label", _nameSource, 34, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, new Vector2(10f, 6f), new Vector2(-10f, -6f));
            LocalisedLabel.Bind(label, text.Get);
            button.onClick.AddListener(() => open());
            return button;
        }

        // RelicForgeView, as it refreshes: the entry on the primary plate while a skill of the next run's hero is
        // affordable, so the forge calls the player to it.
        internal void RefreshSkillsEntry()
        {
            if (SkillsEntry == null || _setup == null)
                return;
            HeroDefinition hero = _setup.NextRunHero;
            bool calls = false;
            for (int slot = 2; hero != null && slot <= HeroSkillUnlock.LastSlot; slot++)
                calls |= hero.SkillAt(slot) != null &&
                         _setup.Profile.SkillStatusOf(hero.Id, slot) == SkillForgeStatus.Affordable;
            SkillsEntryCalls = calls;
            Paint(SkillsEntry, calls);
        }

        private void BuildSkillsPage(Transform surface)
        {
            _skillsPage = new GameObject("Skills Page", typeof(RectTransform));
            _skillsPage.layer = surface.gameObject.layer;
            var page = (RectTransform)_skillsPage.transform;
            page.SetParent(surface, false);
            float width = SurfaceWidth - 2 * Inset;
            Place(page, Inset, SkillsTop, width, SurfaceHeight - SkillsTop - BottomMargin);

            _skillsHero = Label(page, "Skills Hero", _descriptionSource, 32, TextAnchor.MiddleCenter);
            Row(_skillsHero.rectTransform, 0f, 64f, 0f);
            for (int i = 0; i < _skillCards.Length; i++)
            {
                int slot = i + 1;
                _skillCards[i] = MakeSkillCard(page, "Skill Card " + slot, SkillCardTop + i * (SkillCardHeight + SkillCardGap), width);
                _skillCards[i].Button.onClick.AddListener(() => OnSkillCard(slot));
            }
            _skillsNote = Label(page, "Skills Note", _descriptionSource, 28, TextAnchor.MiddleCenter);
            Row(_skillsNote.rectTransform, SkillCardTop + 3 * (SkillCardHeight + SkillCardGap) + 6f, 100f, 20f);
            _skillsPage.SetActive(false);
        }

        // A skill's card: the icon's well at the left, then the name, what it does and its state (the piece card's Name, Info
        // and Level).
        private PieceCard MakeSkillCard(Transform parent, string name, float y, float width)
        {
            var cardObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            cardObject.layer = parent.gameObject.layer;
            var rect = (RectTransform)cardObject.transform;
            rect.SetParent(parent, false);
            Place(rect, 0f, y, width, SkillCardHeight);
            var card = new PieceCard { Button = cardObject.GetComponent<Button>(), Frame = cardObject.GetComponent<Image>() };
            card.Button.targetGraphic = card.Frame;
            card.Frame.color = new Color(.2f, .17f, .22f, 1f);
            card.Mark = Part(rect, name + " Mark");
            card.Mark.enabled = false;
            card.Icon = Part(rect, name + " Icon");
            card.Icon.preserveAspect = true;
            RectTransform icon = card.Icon.rectTransform;
            icon.anchorMin = icon.anchorMax = new Vector2(0f, .5f);
            icon.pivot = new Vector2(0f, .5f);
            icon.anchoredPosition = new Vector2(40f, 0f);
            icon.sizeDelta = new Vector2(SkillIconSize, SkillIconSize);
            float left = 40f + SkillIconSize + 34f;
            card.Name = Label(rect, name + " Name", _nameSource, 44, TextAnchor.MiddleLeft);
            InCard(card.Name.rectTransform, left, 26f, 36f, 64f);
            card.Info = Label(rect, name + " Description", _descriptionSource, 30, TextAnchor.UpperLeft);
            InCard(card.Info.rectTransform, left, 98f, 36f, 116f);
            card.Level = Label(rect, name + " State", _descriptionSource, 32, TextAnchor.MiddleLeft);
            InCard(card.Level.rectTransform, left, 222f, 36f, 56f);
            return card;
        }

        private void ShowSkills()
        {
            HeroDefinition hero = _setup.NextRunHero;
            _skillsHero.text = Loc.Format("skills.hero", "{0}: the Forge forges each skill once, for this hero",
                HeroText.Name(hero));
            _skillsNote.text = Loc.Text("skills.note",
                "A floor cleared with any hero, on any difficulty, opens its skill for every hero.");
            for (int i = 0; i < _skillCards.Length; i++)
                ShowSkillCard(hero, i + 1);
        }

        private void ShowSkillCard(HeroDefinition hero, int slot)
        {
            PieceCard card = _skillCards[slot - 1];
            AbilityDefinition skill = hero != null ? hero.SkillAt(slot) : null;
            card.Button.gameObject.SetActive(skill != null);
            if (skill == null)
                return;
            SkillForgeStatus status = _setup.Profile.SkillStatusOf(hero.Id, slot);
            _skillStates[slot - 1] = status;
            card.Name.text = AbilityText.Name(skill);
            card.Info.text = AbilityText.Description(skill);
            card.Icon.sprite = skill.Icon;
            card.Icon.enabled = skill.Icon != null;
            card.Icon.color = status == SkillForgeStatus.Locked ? LockedIcon : Color.white;
            switch (status)
            {
                case SkillForgeStatus.Forged:
                    card.Level.text = slot == HeroSkillUnlock.FirstSlot
                        ? Loc.Text("skills.own", "Ready from the first run")
                        : Loc.Text("skills.forged", "Forged: yours in every run");
                    card.Level.color = StateForged;
                    break;
                case SkillForgeStatus.Affordable:
                    card.Level.text = string.Format(Loc.Numbers, _setup.Text.UnlockForgeFormat, HeroSkillUnlock.Price(slot));
                    card.Level.color = Gold;
                    break;
                case SkillForgeStatus.TooExpensive:
                    card.Level.text = string.Format(Loc.Numbers, _setup.Text.UnlockNeedGoldFormat, HeroSkillUnlock.Price(slot),
                        _setup.Profile.GoldNeededForSkill(hero.Id, slot));
                    card.Level.color = Muted;
                    break;
                case SkillForgeStatus.NeedsEarlierSkill:
                    AbilityDefinition earlier = hero.SkillAt(HeroSkillUnlock.Requires(slot));
                    card.Level.text = Loc.Format("skills.needs_earlier", "Forge {0} first", AbilityText.Name(earlier));
                    card.Level.color = StateWaiting;
                    break;
                default:
                    card.Level.text = Loc.Format("skills.locked", "Locked: clear floor {0}", HeroSkillUnlock.OpeningFloor(slot));
                    card.Level.color = Muted;
                    break;
            }
            card.Name.color = status == SkillForgeStatus.Locked ? Muted : Bright;
            // The cards no tap changes read as out of reach; a forged one stays lit and does nothing.
            card.Button.interactable = status == SkillForgeStatus.Affordable || status == SkillForgeStatus.Forged;
            UpgradeRarity? look = null;
            if (status == SkillForgeStatus.Forged)
                look = UpgradeRarity.Gold;
            else if (status == SkillForgeStatus.Affordable)
                look = UpgradeRarity.Rare;
            else if (status != SkillForgeStatus.Locked)
                look = UpgradeRarity.Common;
            PaintCard(SkillPaintSlot + slot - 1, card, look);
        }

        // A tap forges an affordable skill for the next run's hero; the profile's change writes the tab again.
        private void OnSkillCard(int slot)
        {
            if (!Ready() || Current != Tab.Skills || !HeroSkillUnlock.IsForgeable(slot))
                return;
            HeroDefinition hero = _setup.NextRunHero;
            if (hero == null || hero.SkillAt(slot) == null)
                return;
            if (_setup.Profile.TryForgeSkill(hero.Id, slot))
                _inputEnabledAt = Time.unscaledTime + InputDelay;
        }
    }
}
