using System;

namespace Cryptforge.Combat
{
    // One enemy's slow, daze and heat (docs/57 sections 3.0-3.2): plain state beside its BurnState, ticked on combat time
    // by the scene's SlowStatus and by the foundry simulation alike. A skill never moves an enemy (lead decision 4): it
    // shortens the enemy's step toward the same point (PackMotion.SetSlow, SkillRule.Slowed), which PackMotion's spacing
    // invariant already allows, so the pack the scene and the simulation share stays the pack the parity tests pin.
    //
    // A slow is a share of the walk taken away for a while - Flame Sigil's 40 % inside its glyph, renewed at every tick of
    // the glyph's clock. A daze (Shield Rush) takes the whole walk and the swing for its seconds, the weapon's clock still
    // running. Heat is what a burn inside the glyph is multiplied by (SkillRule.SigilHeat), held as long as the slow it came
    // with. The stronger slow holds; a weaker one waits for it to run out.
    public sealed class SlowState
    {
        public float Slow { get; private set; }
        public float SlowLeft { get; private set; }
        public float DazeLeft { get; private set; }
        public float Heat { get; private set; } = 1f;
        public float HeatLeft { get; private set; }

        public bool IsSlowed => SlowLeft > 0f && Slow > 0f;
        public bool IsDazed => DazeLeft > 0f;
        public bool IsHeated => HeatLeft > 0f && Heat != 1f;
        // The share of the walk taken away now: all of it while dazed, the slow while it lasts, none otherwise.
        public float Current => IsDazed ? 1f : IsSlowed ? Slow : 0f;
        // What a burn tick deals now is multiplied by this.
        public float CurrentHeat => IsHeated ? Heat : 1f;

        // A slow of this share of the walk (in [0, 1)) for this many seconds: it replaces a slow that has run out or a weaker
        // one; an equal one renews the longer time.
        public void ApplySlow(float slow, float seconds)
        {
            if (float.IsNaN(slow) || slow < 0f || slow >= 1f)
                throw new ArgumentOutOfRangeException(nameof(slow));
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if (SlowLeft <= 0f || slow > Slow)
            {
                Slow = slow;
                SlowLeft = seconds;
            }
            else if (slow == Slow)
            {
                SlowLeft = Math.Max(SlowLeft, seconds);
            }
        }

        public void Daze(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            DazeLeft = Math.Max(DazeLeft, seconds);
        }

        // Burns tick at heat (>= 1) for this many seconds.
        public void ApplyHeat(float heat, float seconds)
        {
            if (float.IsNaN(heat) || float.IsInfinity(heat) || heat < 1f)
                throw new ArgumentOutOfRangeException(nameof(heat));
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            Heat = heat;
            HeatLeft = Math.Max(HeatLeft, seconds);
        }

        public void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (SlowLeft > 0f)
            {
                SlowLeft = Math.Max(0f, SlowLeft - deltaTime);
                if (SlowLeft <= 0f)
                    Slow = 0f;
            }
            if (DazeLeft > 0f)
                DazeLeft = Math.Max(0f, DazeLeft - deltaTime);
            if (HeatLeft > 0f)
            {
                HeatLeft = Math.Max(0f, HeatLeft - deltaTime);
                if (HeatLeft <= 0f)
                    Heat = 1f;
            }
        }

        public bool IsQuiet => SlowLeft <= 0f && DazeLeft <= 0f && HeatLeft <= 0f;

        public void Clear()
        {
            Slow = 0f;
            SlowLeft = 0f;
            DazeLeft = 0f;
            Heat = 1f;
            HeatLeft = 0f;
        }
    }
}
