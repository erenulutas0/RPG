using System;
using Cryptforge.Progression;
using UnityEngine;

namespace Cryptforge.Art
{
    // The run's choice cards painted (RunChoiceView, ChoiceCardPainter): a treatment per tier, the evolution's own, the
    // forge's neutral one and the checkpoint's Extract and Climb, each a card frame, a name plate, an icon well and what an
    // empty well holds; the parts every card shares (the description's recess, the lip over the icon's edge, the press);
    // and the panel round them (the backdrop, the surface's frame, the title's plate and crest).
    // Tools/ArtReview/build_choice_card_art.py paints the one sheet they share (Art/UI/ChoiceCards/UI_ChoiceCards.png,
    // imported as sprites with their 9-slice borders) and writes this asset. One sheet, so the cards batch as one texture.
    [CreateAssetMenu(menuName = "Cryptforge/Art/Choice Card Set")]
    public sealed class ChoiceCardArtSet : ScriptableObject
    {
        // One card's look.
        [Serializable]
        public sealed class Treatment
        {
            [SerializeField] private Sprite _frame;
            [SerializeField] private Sprite _aura;
            [SerializeField] private Vector4 _auraOutset;
            [SerializeField] private Sprite _crest;
            [SerializeField] private Sprite _namePlate;
            [SerializeField] private Sprite _well;
            [SerializeField] private Sprite _sigil;
            [SerializeField] private Color _pressColour = Color.white;

            // Sliced, the card's own image: its rim, its corners and its face.
            public Sprite Frame => _frame;
            // Optional, sliced, laid over the card and past it by AuraOutset canvas units (left, bottom, right, top):
            // gold's radiance and stars, the evolution's heat, horns and fire.
            public Sprite Aura => _aura;
            public Vector4 AuraOutset => _auraOutset;
            // Optional, at its native size on the middle of the card's top edge, its pivot on that edge: gold's clasp,
            // the evolution's crown of fire.
            public Sprite Crest => _crest;
            // Sliced: behind the name, its rule along the name's foot.
            public Sprite NamePlate => _namePlate;
            // Sliced: the socket behind the icon, reaching past it by the set's WellMargin.
            public Sprite Well => _well;
            // What the well holds while the card has no icon.
            public Sprite Sigil => _sigil;
            // The press's colour under a finger.
            public Color PressColour => _pressColour;

            internal bool IsValid(Texture2D sheet) =>
                On(_frame, sheet) && HasBorder(_frame) && (_aura == null ? _auraOutset == Vector4.zero : On(_aura, sheet) &&
                HasBorder(_aura) && _auraOutset.x > 0f && _auraOutset.y > 0f && _auraOutset.z > 0f &&
                _auraOutset.w > 0f) && (_crest == null || On(_crest, sheet)) && On(_namePlate, sheet) &&
                _namePlate.border.x > 0f && On(_well, sheet) && HasBorder(_well) && On(_sigil, sheet) &&
                _pressColour.a > 0f;
        }

        [SerializeField] private Sprite _backdrop;
        [SerializeField] private Sprite _surface;
        [SerializeField] private Sprite _titlePlate;
        [SerializeField] private Sprite _titleCrest;
        [SerializeField] private Sprite _description;
        [SerializeField] private Sprite _wellLip;
        [SerializeField] private Sprite _press;
        [SerializeField] private Color _disabledColour = new Color(0f, 0f, 0f, .6f);
        [SerializeField] private Vector4 _namePlateMargin;
        [SerializeField] private Vector4 _descriptionMargin;
        [SerializeField] private Vector4 _titlePlateMargin;
        [SerializeField] private float _titleRuleHeight;
        [SerializeField] private float _wellMargin;
        [SerializeField] private float _sigilScale = .75f;
        // Green, orange, red and gold, indexed by UpgradeRarity.
        [SerializeField] private Treatment[] _tiers;
        [SerializeField] private Treatment _evolution;
        [SerializeField] private Treatment _neutral;
        [SerializeField] private Treatment _extract;
        [SerializeField] private Treatment _climb;

        // The dim over the run, stretched over the whole screen (its edge texels are extruded on the sheet, so its
        // bilinear edges never fade).
        public Sprite Backdrop => _backdrop;
        // Sliced: the surface the cards stand on.
        public Sprite Surface => _surface;
        // Sliced, behind the title, its edges moved by TitlePlateMargin (left, bottom, right, top; positive outward); and
        // the crest centred on its lower rule, TitleRuleHeight canvas units above the plate's foot.
        public Sprite TitlePlate => _titlePlate;
        public Sprite TitleCrest => _titleCrest;
        public Vector4 TitlePlateMargin => _titlePlateMargin;
        public float TitleRuleHeight => _titleRuleHeight;
        // Sliced: the faint recess behind every card's description.
        public Sprite Description => _description;
        // Sliced: laid over the icon's edge.
        public Sprite WellLip => _wellLip;
        // Sliced, painted light: the button's graphic over the frame, tinted by its state (clear at rest, the
        // treatment's PressColour under a finger, DisabledColour while the card waits).
        public Sprite Press => _press;
        public Color DisabledColour => _disabledColour;
        // How far the name plate and the description's recess reach past their labels' rects (left, bottom, right, top;
        // positive outward), and the well past the icon's, in canvas units.
        public Vector4 NamePlateMargin => _namePlateMargin;
        public Vector4 DescriptionMargin => _descriptionMargin;
        public float WellMargin => _wellMargin;
        // The sigil's size, as a share of the icon's.
        public float SigilScale => _sigilScale;
        public Treatment Evolution => _evolution;
        public Treatment Neutral => _neutral;
        public Treatment Extract => _extract;
        public Treatment Climb => _climb;

        // A tier's treatment; null for a value outside the four.
        public Treatment Tier(UpgradeRarity rarity)
        {
            int tier = (int)rarity;
            return _tiers != null && tier >= 0 && tier < _tiers.Length ? _tiers[tier] : null;
        }

        public bool IsValid
        {
            get
            {
                Texture2D sheet = _surface != null ? _surface.texture : null;
                if (sheet == null || !HasBorder(_surface) || !On(_backdrop, sheet) || !On(_titlePlate, sheet) ||
                    !HasBorder(_titlePlate) || !On(_titleCrest, sheet) || !On(_description, sheet) ||
                    !HasBorder(_description) || !On(_wellLip, sheet) || !HasBorder(_wellLip) || !On(_press, sheet) ||
                    !HasBorder(_press) || _sigilScale <= 0f || _tiers == null ||
                    _tiers.Length != RarityTable.TierCount)
                    return false;
                foreach (Treatment tier in _tiers)
                    if (tier == null || !tier.IsValid(sheet))
                        return false;
                return _evolution != null && _evolution.IsValid(sheet) && _neutral != null && _neutral.IsValid(sheet) &&
                    _extract != null && _extract.IsValid(sheet) && _climb != null && _climb.IsValid(sheet);
            }
        }

        private static bool On(Sprite sprite, Texture2D sheet) => sprite != null && sprite.texture == sheet;

        private static bool HasBorder(Sprite sprite) =>
            sprite != null && sprite.border.x > 0f && sprite.border.y > 0f && sprite.border.z > 0f && sprite.border.w > 0f;
    }
}
