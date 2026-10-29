using UnityEngine;

namespace Cryptforge.Art
{
    // The health bar over every enemy, painted (EnemyLookView): a forged backing round a channel, the fill and the pale
    // chunk a hit takes, and the Cinder Mite's compact bar, whose frame is painted thicker across so it stays even once
    // the bar is squeezed to the Mite's size. Tools/ArtReview/build_hud_art.py paints the one small sheet they share
    // (Art/UI/Hud/UI_EnemyBar.png, at the actors' 128 texels a unit) and writes this asset; every bar of a crowd draws
    // from it, so forty bars cost one texture and no allocation. Each backing is the pixel bar's size (26 by 5 of its
    // texels), so a painted bar takes the same room over a head.
    [CreateAssetMenu(menuName = "Cryptforge/Art/Enemy Bar Set")]
    public sealed class EnemyBarArtSet : ScriptableObject
    {
        [SerializeField] private Sprite _back;
        [SerializeField] private Sprite _fill;
        [SerializeField] private Sprite _trail;
        [SerializeField] private Sprite _compactBack;
        [SerializeField] private Sprite _compactFill;
        [SerializeField] private Sprite _compactTrail;

        // Centred; the fill and the chunk pin at their left edge's middle, centred in the backing's channel.
        public Sprite BackFor(bool compact) => compact ? _compactBack : _back;
        public Sprite FillFor(bool compact) => compact ? _compactFill : _fill;
        public Sprite TrailFor(bool compact) => compact ? _compactTrail : _trail;

        public bool IsValid => Valid(_back, _fill, _trail) && Valid(_compactBack, _compactFill, _compactTrail) &&
            _compactBack.texture == _back.texture;

        private static bool Valid(Sprite back, Sprite fill, Sprite trail)
        {
            if (back == null || fill == null || trail == null || fill.texture != back.texture || trail.texture != back.texture)
                return false;
            return fill.rect.size == trail.rect.size && fill.rect.width < back.rect.width &&
                fill.rect.height < back.rect.height && fill.pivot.x == 0f && trail.pivot.x == 0f;
        }
    }
}
