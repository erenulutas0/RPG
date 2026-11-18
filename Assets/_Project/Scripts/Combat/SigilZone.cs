using System;

namespace Cryptforge.Combat
{
    // Flame Sigil's glyph (docs/57 section 3.2): a stationary floor zone of Radius round where Ilya drew it, live for
    // Duration seconds and read on Burn's clock (BurnRule.TickSeconds) - one pooled decal in the scene, no movement, no
    // collision, one CollectNear a tick. Every tick, from the one as it is drawn to the last before it fades, each enemy
    // inside is slowed by Slow (half on a gate boss, SkillRule.SlowOn) and heated (its burn x SkillRule.SigilHeat) until the
    // next tick (HoldSeconds), and an unburnt one catches SkillRule.SigilStacks direct stacks. Plain state: the scene's
    // AbilityController and the simulation tick it and do what its ticks say.
    public sealed class SigilZone
    {
        private const float Slack = 1e-4f;
        // How long a tick's slow and heat hold on an enemy: to the next tick, and a little past it, so an enemy that stays
        // inside never walks one frame unslowed between two ticks whatever the frame's rounding.
        public const float HoldSeconds = BurnRule.TickSeconds + .05f;

        private float _elapsed;
        private float _nextTick;

        public float X { get; private set; }
        public float Y { get; private set; }
        public float Radius { get; private set; }
        public float Duration { get; private set; }
        public float Slow { get; private set; }
        public int Ticks { get; private set; }
        public bool IsLive => Duration > 0f && _elapsed < Duration - Slack;

        // A glyph drawn at (x, y): its first tick comes with the next Tick.
        public void Begin(float x, float y, float radius, float duration, float slow)
        {
            if (!(radius > 0f) || float.IsInfinity(radius))
                throw new ArgumentOutOfRangeException(nameof(radius));
            if (!(duration > 0f) || float.IsInfinity(duration))
                throw new ArgumentOutOfRangeException(nameof(duration));
            if (float.IsNaN(slow) || slow < 0f || slow >= 1f)
                throw new ArgumentOutOfRangeException(nameof(slow));
            X = x;
            Y = y;
            Radius = radius;
            Duration = duration;
            Slow = slow;
            _elapsed = 0f;
            _nextTick = 0f;
            Ticks = 0;
        }

        // Advances the glyph by deltaTime of combat time; how many of its ticks fell in the step (each one a CollectNear).
        // The first is the tick at the moment it was drawn; the last falls before it fades.
        public int Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (!IsLive)
                return 0;
            _elapsed += deltaTime;
            int ticks = 0;
            while (_nextTick <= _elapsed + Slack && _nextTick < Duration - Slack)
            {
                ticks++;
                _nextTick += BurnRule.TickSeconds;
            }
            Ticks += ticks;
            return ticks;
        }

        // Whether a floor point lies inside the glyph (SkillRule.Within: the squared radius cast to float).
        public bool Contains(float x, float y) => SkillRule.Within(x, y, X, Y, Radius);

        public void End() => Duration = 0f;
    }
}
