using UnityEngine;

namespace Cryptforge.Art
{
    [CreateAssetMenu(menuName = "Cryptforge/Art/Strike Set")]
    public sealed class StrikeArtSet : ScriptableObject
    {
        [SerializeField] private Sprite[] _slash;
        [SerializeField] private Sprite[] _spark;
        // Optional: the Staff's painted impact (Tools/ArtReview/build_staff_impact_vfx.py), two strips on one sheet that
        // the view cuts into sprites as it cuts the skills' (SkillEffectArtSet.Strip). The ring is a unit-radius floor
        // ellipse, half as deep as it is wide, which the view scales to the part of the splash it shows; the burst stands
        // on the struck body. Without both, the Staff draws the procedural ring (StaffSplashArt); the slash and spark
        // never depend on them.
        [SerializeField] private SkillEffectArtSet.Strip _staffRing;
        [SerializeField] private SkillEffectArtSet.Strip _staffBurst;
        // Optional: docs/63's Cinder Saw painted (Tools/ArtReview/BuildBlenderCinderSaw.py renders the blade,
        // build_cinder_saw_vfx.py packs it and paints the ring), two strips on one sheet cut as the Staff's are. The blade
        // is a loop of the rendered blade turning, pinned at its axle; the ring a unit-radius floor ellipse like the
        // Staff's, which the view scales to the aura's edge. Without both, the Saw draws EffectArt's pixel blade and the
        // procedural ring StaffSplashArt draws, tinted.
        [SerializeField] private SkillEffectArtSet.Strip _sawBlade;
        [SerializeField] private SkillEffectArtSet.Strip _sawRing;
        public Sprite[] Slash => _slash;
        public Sprite[] Spark => _spark;
        public SkillEffectArtSet.Strip StaffRing => _staffRing;
        public SkillEffectArtSet.Strip StaffBurst => _staffBurst;
        public SkillEffectArtSet.Strip SawBlade => _sawBlade;
        public SkillEffectArtSet.Strip SawRing => _sawRing;
        public bool IsValid => Complete(_slash) && Complete(_spark);
        public bool HasStaffBlast => Valid(_staffRing) && Valid(_staffBurst);
        public bool HasCinderSaw => Valid(_sawBlade) && Valid(_sawRing);
        private static bool Complete(Sprite[] frames)
        {
            if (frames == null || frames.Length != 4) return false;
            foreach (var frame in frames) if (frame == null) return false;
            return true;
        }
        private static bool Valid(SkillEffectArtSet.Strip strip) => strip != null && strip.IsValid;
    }
}
