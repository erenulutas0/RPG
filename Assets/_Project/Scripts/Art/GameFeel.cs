using System;
using Cryptforge.Combat;

namespace Cryptforge.Art
{
    // The small motion a weapon fired from a later slot plays on the hero (docs/43 section 1, the game-feel pass of
    // 2026-11-22): the body swings with slot 0 alone (docs/42, risks), so each weapon after it shows a cue of its own - the
    // Sword an off-hand flourish arc, the Staff a raised glow pulse at its tip, the Daggers a quick double flick.
    public enum SlotCue
    {
        Flourish,
        Pulse,
        Flick
    }

    // The game-feel pass's timings and curves (docs/43 section 1, 2026-11-22), pure so they are tested without Unity:
    // the slot cues, the hero's recoil from a blow and its pop on a level, the bosses' tell through a telegraph, and how
    // many motes a death throws as a crowd falls. Visual only - nothing here is read by a rule, a weapon or the simulation.
    public static class GameFeel
    {
        // The hero's weapons by how they strike, as CombatEffectsView draws their strikes (EffectArt.StrikeFor): the
        // Sword cleaves, the Staff strikes an area, the Daggers strike one target.
        // docs/63 phase 2's Cinder Saw, an aura, shows the Staff's glow when it is picked up.
        public static SlotCue CueFor(WeaponBehavior behavior) =>
            behavior == WeaponBehavior.Cleave ? SlotCue.Flourish
            : behavior == WeaponBehavior.Area || behavior == WeaponBehavior.Aura ? SlotCue.Pulse
            : SlotCue.Flick;

        // Whether a weapon in a later slot cues on the hero at each strike. An aura strikes five times a second and its saw
        // already circles the hero (CombatEffectsView), so a cue on every strike would only flicker: it cues once, when it
        // is picked up.
        public static bool CuesEachStrike(WeaponBehavior behavior) => behavior != WeaponBehavior.Aura;

        // Each cue's length; never more than CueShare of its weapon's interval, so a quick weapon's cue ends before its next
        // strike, and never less than MinimumCueSeconds, so it is seen at all. The Daggers' 0.3 s category interval keeps
        // its 0.18 s whole (0.24 s allowed).
        public const float FlourishSeconds = .24f;
        public const float PulseSeconds = .28f;
        public const float FlickSeconds = .18f;
        public const float CueShare = .8f;
        public const float MinimumCueSeconds = .1f;

        public static float CueSeconds(SlotCue cue, float interval)
        {
            float own = cue == SlotCue.Flourish ? FlourishSeconds : cue == SlotCue.Pulse ? PulseSeconds : FlickSeconds;
            if (!(interval > 0f) || float.IsInfinity(interval))
                return own;
            return Math.Max(MinimumCueSeconds, Math.Min(own, interval * CueShare));
        }

        // The flourish's blade, in degrees for a hero facing right (90 is up): from behind the off shoulder over the top
        // and down toward the struck enemy, quick out of the shoulder and settling at its end.
        public const float FlourishFrom = 155f;
        public const float FlourishTo = -25f;

        public static float FlourishAngle(float t) => Lerp(FlourishFrom, FlourishTo, EaseOutCubic(Clamp01(t)));

        // A cue's strength over its life: in over its first InShare, held, then out over the rest - so it pops in with
        // the strike and never lingers past it.
        public const float InShare = .15f;

        public static float CueEnvelope(float t)
        {
            t = Clamp01(t);
            if (t < InShare)
                return t / InShare;
            float out01 = (t - InShare) / (1f - InShare);
            return 1f - out01 * out01;
        }

        // A blow on the hero: the recoil rises over its first HitAttack share and falls back to nothing by HitSeconds.
        public const float HitSeconds = .18f;
        public const float HitAttack = .2f;

        public static float HitEnvelope(float t)
        {
            if (!(t >= 0f) || t >= 1f)
                return 0f;
            if (t < HitAttack)
                return t / HitAttack;
            float back = (t - HitAttack) / (1f - HitAttack);
            return (1f - back) * (1f - back);
        }

        // At a full blow the figure gives this share along the blow's line and bulges half as much across it, and is shoved
        // this far (world units) away from it: about 2 px and 4 px on the S23's 80 px hero.
        public const float SquashAlong = .12f;
        public const float SquashAcross = .06f;
        public const float ShoveDistance = .07f;

        // The scale a blow from the floor direction (dx, dy) gives at amount (0 to 1): a blow from the side squeezes the
        // width and lifts the height, one from in front or behind squeezes the height. (1, 1) with no amount or direction.
        public static void Squash(float amount, float dx, float dy, out float scaleX, out float scaleY)
        {
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (!(amount > 0f) || !(length > 1e-6f))
            {
                scaleX = 1f;
                scaleY = 1f;
                return;
            }
            float side = Math.Abs(dx) / length;
            float along = SquashAlong * amount;
            float across = SquashAcross * amount;
            scaleX = 1f - along * side + across * (1f - side);
            scaleY = 1f - along * (1f - side) + across * side;
        }

        // A level gained: the figure stretches up and settles over PopSeconds, a quick rise and a softer fall.
        public const float PopSeconds = .32f;
        public const float PopStretch = .09f;

        public static float PopEnvelope(float t)
        {
            if (!(t >= 0f) || t >= 1f)
                return 0f;
            return (float)Math.Sin(Math.PI * Math.Sqrt(t));
        }

        // A boss's body through its telegraph (WardenSlamClock.TelegraphProgress, 0 at the mark, 1 at the blow): it rises
        // and leans back fast as the mark goes down, then holds and gathers - an ease-out, so the body has told the blow
        // well before it lands.
        public static float TellEase(float progress)
        {
            progress = Clamp01(progress);
            float inverse = 1f - progress;
            return 1f - inverse * inverse * inverse;
        }

        // The glow over the boss's body through the telegraph: already on at the mark (TellGlowFloor) and gathering to
        // TellGlowPeak at the blow, beating with the mark's own edge (pulse from 0 to 1, WardenSlamView's pulse), so the body
        // and the floor's mark throb together.
        public const float TellGlowFloor = .16f;
        public const float TellGlowPeak = .5f;

        public static float TellGlow(float progress, float pulse) =>
            (TellGlowFloor + (TellGlowPeak - TellGlowFloor) * Clamp01(progress)) * (.75f + .25f * Clamp01(pulse));

        // How many ash motes a fallen enemy throws, given how many fell within the budget's window (itself included): six
        // for a few, three through a busy fight, and one once more than CrowdDeaths are falling at once, so a crowd's
        // death never costs the frame that a crowd's spawn did.
        public const int FewDeaths = 10;
        public const int CrowdDeaths = 30;

        public static int MotesPerDeath(int recentDeaths) =>
            recentDeaths <= FewDeaths ? 6 : recentDeaths <= CrowdDeaths ? 3 : 1;

        // A horde arrival's embers, likewise: three for a trickle, one when a crowd walks in together.
        public static int EmbersPerArrival(int recentArrivals) => recentArrivals <= FewDeaths ? 3 : 1;

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        private static float Clamp01(float t) => t < 0f ? 0f : t > 1f ? 1f : t;

        private static float EaseOutCubic(float t)
        {
            float inverse = 1f - t;
            return 1f - inverse * inverse * inverse;
        }
    }

    // How many things happened within the last Window seconds - deaths, arrivals - read without allocating: a ring of the
    // last Capacity moments. A count past the ring's size reads as the ring's size, which is past every budget's line.
    public sealed class FeelBudget
    {
        private readonly float[] _times;
        private int _next;
        private int _count;

        public FeelBudget(float window, int capacity)
        {
            if (!(window > 0f) || float.IsInfinity(window))
                throw new ArgumentOutOfRangeException(nameof(window));
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            Window = window;
            _times = new float[capacity];
        }

        public float Window { get; }
        public int Capacity => _times.Length;

        public void Record(float now)
        {
            _times[_next] = now;
            _next = (_next + 1) % _times.Length;
            if (_count < _times.Length)
                _count++;
        }

        // Moments recorded no more than Window before now (and none after it).
        public int Recent(float now)
        {
            int recent = 0;
            for (int i = 0; i < _count; i++)
            {
                float age = now - _times[i];
                if (age >= 0f && age <= Window)
                    recent++;
            }
            return recent;
        }

        public void Clear()
        {
            _next = 0;
            _count = 0;
        }
    }
}
