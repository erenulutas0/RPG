using UnityEngine;

namespace Cryptforge.Art
{
    // The combat feedback's painted effects, drawn to match the painted actors: the Daggers' double slash, the crit star,
    // the smoke of a death without painted remains and of a horde arrival, the gold coin, and the numerals of the
    // floating numbers. Tools/ArtReview/build_combat_feedback_vfx.py paints the sheet they share and writes this asset;
    // each strip is a run of frames on it (SkillEffectArtSet's strips), which CombatEffectsView cuts into sprites at
    // startup. The view owns those sprites, this set the sheet. One sheet, so everything it draws batches together, every
    // number on screen included.
    [CreateAssetMenu(menuName = "Cryptforge/Art/Combat Feedback Set")]
    public sealed class CombatFeedbackArtSet : ScriptableObject
    {
        // A font is a numeral for each digit, 0 to 9 in order.
        public const int NumeralCount = 10;

        [SerializeField] private SkillEffectArtSet.Strip _doubleSlash;
        [SerializeField] private SkillEffectArtSet.Strip _critStar;
        [SerializeField] private SkillEffectArtSet.Strip _smoke;
        [SerializeField] private SkillEffectArtSet.Strip _coin;
        [SerializeField] private SkillEffectArtSet.Strip _digits;
        [SerializeField] private SkillEffectArtSet.Strip _critDigits;
        // Left edges of consecutive numerals are this far apart, in texels of the sheet as painted.
        [SerializeField] private float _digitStride;
        [SerializeField] private float _critDigitStride;

        public SkillEffectArtSet.Strip DoubleSlash => _doubleSlash;
        public SkillEffectArtSet.Strip CritStar => _critStar;
        // Painted in the pixel smoke's unit frame, so the view sizes the death and arrival puffs as it sizes that.
        public SkillEffectArtSet.Strip Smoke => _smoke;
        // One whole turn of the spin, which the view loops.
        public SkillEffectArtSet.Strip Coin => _coin;
        // Every numeral of a font is one cell pinned at its bottom-left corner, so a number is laid out by stride.
        public SkillEffectArtSet.Strip Digits => _digits;
        public SkillEffectArtSet.Strip CritDigits => _critDigits;
        // The strides in world units.
        public float DigitStride => _digitStride / _digits.TexelsPerUnit;
        public float CritDigitStride => _critDigitStride / _critDigits.TexelsPerUnit;

        public bool IsValid => Valid(_doubleSlash) && Valid(_critStar) && Valid(_smoke) && Valid(_coin) &&
            Font(_digits, _digitStride) && Font(_critDigits, _critDigitStride);

        private static bool Valid(SkillEffectArtSet.Strip strip) => strip != null && strip.IsValid;

        private static bool Font(SkillEffectArtSet.Strip strip, float stride)
        {
            if (!Valid(strip) || strip.Length != NumeralCount || strip.Pivot != Vector2.zero || !(stride > 0f))
                return false;
            foreach (SkillEffectArtSet.Cell cell in strip.Frames)
            {
                if (cell.Width != strip.Frames[0].Width || cell.Height != strip.Frames[0].Height)
                    return false;
            }
            return true;
        }
    }
}
