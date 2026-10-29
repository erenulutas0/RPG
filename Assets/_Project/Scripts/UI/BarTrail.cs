using UnityEngine;

namespace Cryptforge.UI
{
    // The chunk a hit takes off a painted bar: it holds where the bar stood for a moment, then eases down to the fill. A
    // heal, a new target or a fresh fight lifts it with the fill at once. A hit landing while a chunk drains joins it
    // without restarting the hold, so a stream of hits never keeps the chunk up for longer than the hold and the ease.
    // Plain arithmetic on the fill's fraction, shared by the HUD's bars (HudArtView) and the enemies' (EnemyBarTrail).
    public struct BarTrail
    {
        public const float HoldSeconds = .25f;
        // The ease's rate, per second: about a third of a second to close on the fill.
        public const float Rate = 9f;
        // Within this of the fill the chunk is gone.
        public const float Settle = .002f;

        private float _hold;

        // Where the chunk's edge stands, and the fill it drains to, as fractions of the bar.
        public float Shown { get; private set; }
        public float Target { get; private set; }
        // A chunk shows (and needs ticking) until it has drained.
        public bool IsMoving => Shown > Target;

        public void Reset(float fraction)
        {
            Shown = Target = fraction;
            _hold = 0f;
        }

        public void Follow(float fraction)
        {
            if (fraction >= Shown)
            {
                Shown = fraction;
                _hold = 0f;
            }
            else if (!IsMoving)
            {
                _hold = HoldSeconds;
            }
            Target = fraction;
        }

        public void Tick(float deltaTime)
        {
            if (!IsMoving || deltaTime <= 0f)
                return;
            if (_hold > 0f)
            {
                _hold -= deltaTime;
                return;
            }
            Shown = Mathf.Lerp(Shown, Target, 1f - Mathf.Exp(-Rate * deltaTime));
            if (Shown - Target < Settle)
                Shown = Target;
        }
    }
}
