using Cryptforge.Progression;
using UnityEngine;

namespace Cryptforge.Art
{
    // The between-runs and pause screens painted (ScreenArtView), in the in-run HUD's riveted iron, gold inlay and embers:
    // the forge's and the ledger's frame, a forge card's frame for each state, the strip beside a card's words, the well
    // a relic's picture sits in, the primary and secondary buttons with their pressed and disabled plates, the forge's
    // nameplate, the result's title plates and crests (victory and defeat), the pause plate, the hero chooser's niche and
    // pips, and a rule, a stud and a seal for the dividers and the rack's empty places. Tools/ArtReview/build_screen_art.py
    // paints the one sheet they share (Art/UI/Screens/UI_Screens.png, imported as sprites with their 9-slice borders) and
    // writes this asset, with the few lengths the view lays the painted parts out by, in canvas units. One sheet, so the
    // forge and the result batch as one texture.
    [CreateAssetMenu(menuName = "Cryptforge/Art/Screen Set")]
    public sealed class ScreenArtSet : ScriptableObject
    {
        [SerializeField] private Sprite _surface;
        [SerializeField] private Sprite _pausePlate;
        [SerializeField] private Sprite _headerPlate;
        [SerializeField] private Sprite _victoryPlate;
        [SerializeField] private Sprite _defeatPlate;
        [SerializeField] private Sprite _victoryCrest;
        [SerializeField] private Sprite _defeatCrest;
        [SerializeField] private Sprite _equippedCard;
        [SerializeField] private Sprite _affordableCard;
        [SerializeField] private Sprite _ownedCard;
        [SerializeField] private Sprite _lockedCard;
        [SerializeField] private Sprite _stateAccent;
        [SerializeField] private Sprite _iconWell;
        [SerializeField] private Sprite _primary;
        [SerializeField] private Sprite _primaryPressed;
        [SerializeField] private Sprite _primaryDisabled;
        [SerializeField] private Sprite _secondary;
        [SerializeField] private Sprite _secondaryPressed;
        [SerializeField] private Sprite _secondaryDisabled;
        [SerializeField] private Sprite _portraitFrame;
        [SerializeField] private Sprite _pipLit;
        [SerializeField] private Sprite _pipDark;
        [SerializeField] private Sprite _rule;
        [SerializeField] private Sprite _stud;
        [SerializeField] private Sprite _seal;
        [SerializeField] private Vector4 _titlePlateOutset;
        [SerializeField] private float _crestSink;
        [SerializeField] private float _headerPlateWidth;
        [SerializeField] private Vector4 _pausePlateOutset;
        [SerializeField] private float _iconWellMargin;
        [SerializeField] private float _headerRuleLift;
        [SerializeField] private Vector4 _portraitFrameOutset;
        [SerializeField] private float _pipSpacing;
        [SerializeField] private float _emptySlotRuleShare;

        // Sliced: the forge's and the ledger's frame (its face the panels' navy), and the pause plate, its embers banked.
        public Sprite Surface => _surface;
        public Sprite PausePlate => _pausePlate;
        // Sliced: the forge's nameplate behind its title, and the result's title plates.
        public Sprite HeaderPlate => _headerPlate;
        public Sprite VictoryPlate => _victoryPlate;
        public Sprite DefeatPlate => _defeatPlate;
        // At their size, standing on the title plate's top edge: a crown of flame, or the same flames gone out.
        public Sprite VictoryCrest => _victoryCrest;
        public Sprite DefeatCrest => _defeatCrest;
        // Sliced, painted light so the forge's state colour still tints it: the strip beside a card's words.
        public Sprite StateAccent => _stateAccent;
        // Simple, kept to its aspect: the socket round a relic's picture, its inside the painted icons' own navy.
        public Sprite IconWell => _iconWell;
        // Sliced plates for a sprite swap.
        public Sprite Primary => _primary;
        public Sprite PrimaryPressed => _primaryPressed;
        public Sprite PrimaryDisabled => _primaryDisabled;
        public Sprite Secondary => _secondary;
        public Sprite SecondaryPressed => _secondaryPressed;
        public Sprite SecondaryDisabled => _secondaryDisabled;
        // Sliced: the niche round the chooser's portrait, lit from its floor; and a hero's pip, the chosen one lit.
        public Sprite PortraitFrame => _portraitFrame;
        public Sprite PipLit => _pipLit;
        public Sprite PipDark => _pipDark;
        // A gold inlay line, sliced across; the lozenge on the ledger's divider; the seal of a rack's empty place.
        public Sprite Rule => _rule;
        public Sprite Stud => _stud;
        public Sprite Seal => _seal;

        // The result's title plate round the title's rect (left, bottom, right, top; positive outward).
        public Vector4 TitlePlateOutset => _titlePlateOutset;
        // How far below the title plate's top edge the crest's foot stands.
        public float CrestSink => _crestSink;
        // The forge's nameplate: this wide, as tall as its sprite, centred on the title.
        public float HeaderPlateWidth => _headerPlateWidth;
        // The pause plate round the pause title and its hint.
        public Vector4 PausePlateOutset => _pausePlateOutset;
        // How far the well reaches past a relic picture's rect on each side.
        public float IconWellMargin => _iconWellMargin;
        // The rule under each of the forge's section headers: its middle this far above the header's foot.
        public float HeaderRuleLift => _headerRuleLift;
        // The niche round the chooser's portrait.
        public Vector4 PortraitFrameOutset => _portraitFrameOutset;
        // The pips' spacing along the chooser's foot.
        public float PipSpacing => _pipSpacing;
        // A rack's empty place: the rule's share of the card's width.
        public float EmptySlotRuleShare => _emptySlotRuleShare;

        // A forge card's frame for the state the forge shows it in.
        public Sprite CardFor(UnlockStatus status) => status switch
        {
            UnlockStatus.Equipped => _equippedCard,
            UnlockStatus.Affordable => _affordableCard,
            UnlockStatus.Owned => _ownedCard,
            _ => _lockedCard
        };

        public bool IsValid
        {
            get
            {
                Texture2D sheet = _surface != null ? _surface.texture : null;
                if (sheet == null)
                    return false;
                foreach (Sprite sliced in new[] { _surface, _pausePlate, _headerPlate, _victoryPlate, _defeatPlate,
                             _equippedCard, _affordableCard, _ownedCard, _lockedCard, _primary, _primaryPressed,
                             _primaryDisabled, _secondary, _secondaryPressed, _secondaryDisabled, _portraitFrame })
                {
                    if (!On(sliced, sheet) || sliced.border.x <= 0f || sliced.border.y <= 0f || sliced.border.z <= 0f ||
                        sliced.border.w <= 0f)
                        return false;
                }
                foreach (Sprite simple in new[] { _victoryCrest, _defeatCrest, _iconWell, _pipLit, _pipDark, _stud, _seal })
                {
                    if (!On(simple, sheet))
                        return false;
                }
                return On(_rule, sheet) && _rule.border.x > 0f && _rule.border.z > 0f && On(_stateAccent, sheet) &&
                    _stateAccent.border.y > 0f && _stateAccent.border.w > 0f && _headerPlateWidth > 0f && _pipSpacing > 0f &&
                    _emptySlotRuleShare > 0f && _emptySlotRuleShare <= 1f && _iconWellMargin >= 0f;
            }
        }

        private static bool On(Sprite sprite, Texture2D sheet) => sprite != null && sprite.texture == sheet;
    }
}
