using UnityEngine;

namespace Cryptforge.Art
{
    // The run's growth on the hero, painted (RunFeedbackView, docs/43 item 1): the level-up's ring of light, the taken
    // card's ring and the bright core before it, the ember ring of an evolution and of a weapon picked up, and the sparks
    // the bursts send up. Tools/ArtReview/build_run_feedback_vfx.py paints the sheet they share and writes this asset;
    // each strip is a run of frames on it (SkillEffectArtSet's strips), which RunFeedbackView cuts into sprites at
    // startup. The view owns those sprites, this set the sheet. One sheet, so every ring on the floor batches.
    [CreateAssetMenu(menuName = "Cryptforge/Art/Run Feedback Set")]
    public sealed class RunFeedbackArtSet : ScriptableObject
    {
        [SerializeField] private SkillEffectArtSet.Strip _levelRing;
        [SerializeField] private SkillEffectArtSet.Strip _cardRing;
        [SerializeField] private SkillEffectArtSet.Strip _cardCore;
        [SerializeField] private SkillEffectArtSet.Strip _emberRing;
        [SerializeField] private SkillEffectArtSet.Strip _goldSpark;
        [SerializeField] private SkillEffectArtSet.Strip _emberSpark;
        // The width of the line a spark stands for at scale 1, in texels of the sheet as painted.
        [SerializeField] private float _sparkWidth;

        // Every ring is a unit floor ellipse, half as deep as it is wide, whose leading edge lies a world unit out at
        // scale 1; the view scales it to the outer edge of the line it stands for and shows the frame for the share of
        // its life it has lived. The level ring and the ember ring carry their own colours; the card's ring and core are
        // painted light and neutral, so the view tints them in the tier's colour.
        public SkillEffectArtSet.Strip LevelRing => _levelRing;
        public SkillEffectArtSet.Strip CardRing => _cardRing;
        public SkillEffectArtSet.Strip CardCore => _cardCore;
        public SkillEffectArtSet.Strip EmberRing => _emberRing;
        // Streaks facing +x with their heads at the pivot, in their own colours: the gold a level-up and a gold card send
        // up, the ember of an evolution and of a weapon picked up.
        public SkillEffectArtSet.Strip GoldSpark => _goldSpark;
        public SkillEffectArtSet.Strip EmberSpark => _emberSpark;
        // In world units.
        public float SparkWidth => _sparkWidth / _goldSpark.TexelsPerUnit;

        public bool IsValid
        {
            get
            {
                if (!Valid(_levelRing) || !Valid(_cardRing) || !Valid(_cardCore) || !Valid(_emberRing) ||
                    !Valid(_goldSpark) || !Valid(_emberSpark) || !(_sparkWidth > 0f))
                    return false;
                Texture2D sheet = _levelRing.Sheet;
                return _cardRing.Sheet == sheet && _cardCore.Sheet == sheet && _emberRing.Sheet == sheet &&
                    _goldSpark.Sheet == sheet && _emberSpark.Sheet == sheet;
            }
        }

        private static bool Valid(SkillEffectArtSet.Strip strip) => strip != null && strip.IsValid;
    }
}
