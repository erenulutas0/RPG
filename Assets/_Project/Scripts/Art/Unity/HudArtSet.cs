using System;
using UnityEngine;

namespace Cryptforge.Art
{
    // The in-run HUD painted (HudArtView): the forged frame of the two panels, the pause button's plate, the room pips,
    // the loadout row's sockets, the ability button's ring and its cooldown band, and three bars - the hero's health,
    // his experience and the boss's - each a trough under its fill and a frame over it, with the chunk a hit takes in
    // pale and a light at the fill's end. Tools/ArtReview/build_hud_art.py paints the one sheet they share
    // (Art/UI/Hud/UI_Hud.png, imported as sprites with their 9-slice borders) and writes this asset. One sheet, so the
    // HUD batches as one texture.
    [CreateAssetMenu(menuName = "Cryptforge/Art/HUD Set")]
    public sealed class HudArtSet : ScriptableObject
    {
        // One painted bar. The window is where its fill lies, given as offsets from the bar's own rect in canvas units
        // (left, bottom, right, top; positive moves an edge outward), so a bar whose frame must keep to its rect can
        // lift its fill inside it; the frame reaches past the window by its outset (left, bottom, right, top).
        [Serializable]
        public sealed class Bar
        {
            [SerializeField] private Sprite _trough;
            [SerializeField] private Sprite _fill;
            [SerializeField] private Sprite _trail;
            [SerializeField] private Sprite _glint;
            [SerializeField] private Sprite _frame;
            [SerializeField] private Vector4 _window;
            [SerializeField] private Vector4 _frameOutset;
            [SerializeField] private float _glintWidth;

            // The channel the fill drains into (sliced), the fill itself (cropped by the bar's fill amount, never
            // stretched past the window it was painted for), the pale chunk under it (optional: a bar that only grows
            // has none), the light at the fill's end and the frame laid over the window's edges (sliced).
            public Sprite Trough => _trough;
            public Sprite Fill => _fill;
            public Sprite Trail => _trail;
            public Sprite Glint => _glint;
            public Sprite Frame => _frame;
            public Vector4 Window => _window;
            public Vector4 FrameOutset => _frameOutset;
            // The light's width in canvas units; it stands as tall as the window and a little more.
            public float GlintWidth => _glintWidth;

            internal bool IsValid(Texture2D sheet, bool needsTrail) =>
                On(_trough, sheet) && On(_fill, sheet) && On(_glint, sheet) && On(_frame, sheet) &&
                (_trail == null ? !needsTrail : On(_trail, sheet)) && _glintWidth > 0f && HasBorder(_frame) &&
                HasBorder(_trough);
        }

        [SerializeField] private Sprite _panel;
        [SerializeField] private Sprite _button;
        [SerializeField] private Sprite _pip;
        [SerializeField] private Sprite _weaponSlot;
        [SerializeField] private Sprite _tomeSlot;
        [SerializeField] private Sprite _slotAccent;
        [SerializeField] private Sprite _abilityRing;
        [SerializeField] private Sprite _abilityCooldown;
        [SerializeField] private Sprite _bossNamePlate;
        [SerializeField] private Bar _hero;
        [SerializeField] private Bar _experience;
        [SerializeField] private Bar _boss;

        // Sliced: the forged frame of the top and bottom panels, and the pause button's plate.
        public Sprite Panel => _panel;
        public Sprite Button => _button;
        // Sliced and painted light, so the room's colour tints it (RunHud keeps setting it).
        public Sprite Pip => _pip;
        // Sliced: the loadout row's sockets, a weapon's warm and a tome's cool, and the accent strip under each, painted
        // light so the row's colours tint it.
        public Sprite WeaponSlot => _weaponSlot;
        public Sprite TomeSlot => _tomeSlot;
        public Sprite SlotAccent => _slotAccent;
        // The ring round the ability's icon, as large as the button; and the band of it the cooldown sweeps, painted
        // light so the scene's cooldown colour tints it.
        public Sprite AbilityRing => _abilityRing;
        public Sprite AbilityCooldown => _abilityCooldown;
        // A smoked band behind the boss's name, stretched to the name's rect.
        public Sprite BossNamePlate => _bossNamePlate;
        public Bar Hero => _hero;
        public Bar Experience => _experience;
        public Bar Boss => _boss;

        public bool IsValid
        {
            get
            {
                Texture2D sheet = _panel != null ? _panel.texture : null;
                return sheet != null && HasBorder(_panel) && On(_button, sheet) && HasBorder(_button) && On(_pip, sheet) &&
                    On(_weaponSlot, sheet) && On(_tomeSlot, sheet) && HasBorder(_weaponSlot) && HasBorder(_tomeSlot) &&
                    On(_slotAccent, sheet) && On(_abilityRing, sheet) && On(_abilityCooldown, sheet) &&
                    On(_bossNamePlate, sheet) && _hero != null && _hero.IsValid(sheet, true) && _experience != null &&
                    _experience.IsValid(sheet, false) && _boss != null && _boss.IsValid(sheet, true);
            }
        }

        private static bool On(Sprite sprite, Texture2D sheet) => sprite != null && sprite.texture == sheet;

        private static bool HasBorder(Sprite sprite) => sprite != null && sprite.border.x > 0f && sprite.border.z > 0f;
    }
}
