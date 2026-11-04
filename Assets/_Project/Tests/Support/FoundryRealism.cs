using System;
using System.Globalization;
using Cryptforge.Progression;

namespace Cryptforge.Tests
{
    // A human's hands on the foundry simulation's autopilot (research 2026-11-04, docs/cloud/2026-11-04-difficulty.md):
    // the cohorts (AutopilotCohort) choose what a player does, this chooses how late and how well. The autopilot reads
    // every threat the frame it appears and steps exactly where it means to; a player sees a threat, then answers it a
    // reaction later, misplaces the step, and sometimes looks away - at a card, a drop, the HUD. Simulation only: the
    // scene's DevelopmentAutopilot has none of it, and null (FoundryLevers.Realism's default) is the autopilot as it was.
    internal sealed class PilotRealism
    {
        public string Name = "human";
        // Seconds from a threat's appearing to the pilot's first answer to it, by the threat: the Warden's mark (and any
        // other mark on the floor, FoundryLevers' hazards), the pit's glow, and an enemy coming within the pilot's kite
        // reach - a fresh surge's pack, an elite closing, a runner. 0 is the autopilot's frame-perfect read.
        public float MarkReaction;
        public float GlowReaction;
        public float FoeReaction;
        // How long an enemy that stepped back out of the kite reach stays noticed: one that comes back within it is
        // answered at once, as a player tracks the enemy chasing it.
        public float FoeMemory = 1f;
        // Positioning error, floor units: each answer to a threat (an escape from a mark, a shelter from the glow, a kite
        // stride) aims at its point plus an offset drawn uniformly from a disc this wide. A kite redraws it each
        // KiteAimSeconds.
        public float AimError;
        public float KiteAimSeconds = .5f;
        // Attention: lapses at random (a Poisson rate a minute), each LapseSeconds long, in which the pilot holds the stick
        // where it was and notices nothing new; and after every upgrade or forge choice the panel closes on a floor the
        // player has to read again - CardLapseSeconds with the stick let go. A threat that appeared during a lapse is
        // answered a reaction after the lapse ends.
        public float LapsesPerMinute;
        public float LapseSeconds;
        public float CardLapseSeconds;

        public bool Notices => MarkReaction > 0f || GlowReaction > 0f || FoeReaction > 0f;
        public bool Lapses => LapsesPerMinute > 0f && LapseSeconds > 0f;

        // The reaction on every threat, a positioning error and the attention model scaled with the reaction: a player
        // reacting in r also misplaces a step by about r units and looks away more often the slower it is. Used for the
        // sweep; the parts are measured alone too.
        public static PilotRealism Human(float reaction, float aim, float lapsesPerMinute, float lapseSeconds,
            float cardLapse, string name = null) =>
            new PilotRealism
            {
                Name = name ?? Describe(reaction, aim, lapsesPerMinute, lapseSeconds, cardLapse),
                MarkReaction = reaction, GlowReaction = reaction, FoeReaction = reaction, AimError = aim,
                LapsesPerMinute = lapsesPerMinute, LapseSeconds = lapseSeconds, CardLapseSeconds = cardLapse
            };

        // The research's two humans (docs/cloud/2026-11-04-difficulty.md): a typical phone player - a quarter-second
        // reaction, half a unit off, a short lapse every fifteen seconds and half a second to read the floor after a card -
        // and a slower one.
        public static PilotRealism Typical() => Human(.25f, .5f, 4f, .4f, .5f, "typical human");
        public static PilotRealism Slow() => Human(.4f, .8f, 8f, .5f, .8f, "slow human");

        // FOUNDRY_REALISM's words: "typical", "slow", or a reaction in seconds on every threat alone; null for none.
        public static PilotRealism Named(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return null;
            string trimmed = word.Trim();
            if (trimmed == "typical")
                return Typical();
            if (trimmed == "slow")
                return Slow();
            if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out float reaction) && reaction > 0f)
                return new PilotRealism
                {
                    Name = "reaction " + reaction.ToString("0.00", CultureInfo.InvariantCulture), MarkReaction = reaction,
                    GlowReaction = reaction, FoeReaction = reaction
                };
            throw new ArgumentException("No such realism: " + word);
        }

        public static string Describe(float reaction, float aim, float lapsesPerMinute, float lapseSeconds, float cardLapse) =>
            string.Format(CultureInfo.InvariantCulture, "react {0:0.00} s, aim {1:0.0}, lapses {2:0}/min x {3:0.0} s, card {4:0.0} s",
                reaction, aim, lapsesPerMinute, lapseSeconds, cardLapse);

        public PilotRealism Copy(string name)
        {
            var copy = (PilotRealism)MemberwiseClone();
            copy.Name = name;
            return copy;
        }
    }

    // The draws a realistic pilot makes - its lapses and its misplaced steps - from the simulation's own seeded generator
    // (RunRandom), on a purpose of its own so that no draw of the game's (offers, chests, crits, the horde) moves: the floor
    // a careful pilot and a slow one play is the same floor, seed for seed.
    internal sealed class PilotDice
    {
        // RunRandom's purposes are 1-6 and the horde's 5; the pilot's own is far from them.
        public const int Purpose = 4101;
        private readonly RunRandom _random;

        public PilotDice(int seed)
        {
            _random = RunRandom.Stream(seed, Purpose, 0);
        }

        // Uniform in [0, 1).
        public float Unit() => (_random.Next() >> 8) * (1f / 16777216f);

        // A point uniform in a disc of this radius round the origin.
        public void Disc(float radius, out float x, out float y)
        {
            if (radius <= 0f)
            {
                x = y = 0f;
                return;
            }
            float r = radius * (float)Math.Sqrt(Unit());
            float a = Unit() * (2f * 3.14159274f);
            x = r * (float)Math.Cos(a);
            y = r * (float)Math.Sin(a);
        }

        // Seconds to the next event of a Poisson process of this rate a minute.
        public float Wait(float perMinute)
        {
            float u = Unit();
            return (float)(-Math.Log(1.0 - u) * 60.0 / perMinute);
        }
    }
}
