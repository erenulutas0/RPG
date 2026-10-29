using UnityEngine;

namespace Cryptforge.UI
{
    // The pale chunk on a painted enemy bar (EnemyLookView): where the bar stood when a hit landed, held a moment and
    // drained to the fill (BarTrail), in whole texels of the fill as the fill is. Enabled only while a chunk shows, so a
    // standing crowd's bars cost no update and no allocation. Scaled time: a pause holds it as it holds the fight.
    internal sealed class EnemyBarTrail : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private float _texels;
        private BarTrail _trail;

        // The chunk's edge, as a fraction of the bar; the fill's when no chunk shows.
        internal float Shown => _trail.Shown;
        internal bool IsShowing => _renderer != null && _renderer.enabled;

        internal void Initialize(SpriteRenderer renderer, float texels)
        {
            _renderer = renderer;
            _texels = texels;
            _trail.Reset(1f);
            Show();
        }

        // The fill as it is now shown.
        internal void Follow(float fill)
        {
            _trail.Follow(fill);
            Show();
        }

        private void Update()
        {
            _trail.Tick(Time.deltaTime);
            Show();
        }

        private void Show()
        {
            bool moving = _trail.IsMoving;
            enabled = moving;
            if (_renderer == null)
                return;
            _renderer.enabled = moving;
            if (moving)
                _renderer.transform.localScale = new Vector3(Mathf.Ceil(_trail.Shown * _texels) / _texels, 1f, 1f);
        }
    }
}
