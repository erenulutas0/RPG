using System;
using System.Collections.Generic;

namespace Cryptforge.Arena
{
    // A fight on the local ladder as it ended: the match, both fighters as they were resolved, the ratings around it and
    // the history line the save kept.
    public sealed class ArenaFightOutcome
    {
        public ArenaMode Mode;
        public DuelMatch Match;
        public ArenaFighter Player;
        public ArenaFighter Foe;
        public ArenaRivalData Rival;
        public int RatingBefore;
        public int RatingAfter;
        public int RivalRatingBefore;
        public ArenaMatchData Record;
    }

    // P0's local ladder (docs/55 section 6): 30 rivals a mode with ratings, a simple rating for the player (Elo, K 32, in
    // integers: an expectation table rather than a power of ten, so the save reads the same on every runtime), three
    // opponents offered near the player's rating, and the player's own past heroes standing in it. Pure: it changes the
    // ArenaSaveData it is given; ArenaSaveStore writes it.
    public static class ArenaLadder
    {
        public const int Size = 30;
        public const int StartRating = 1000;
        public const int LowRating = 700;
        public const int HighRating = 1500;
        public const int K = 32;
        public const int Offered = 3;

        // Elo's expectation per mille for a rating gap of 0, 25, ... 800 points: 1000 / (1 + 10^(gap / 400)), rounded.
        private static readonly int[] Expectation =
        {
            500, 464, 429, 394, 360, 327, 297, 267, 240, 215, 192, 170, 151, 133, 118, 104, 91, 80, 70, 61, 53, 46, 40, 35, 31,
            27, 23, 20, 17, 15, 13, 11, 10
        };

        // A fresh arena save: both ladders filled from the seed (0 is replaced, so a save always has one).
        public static ArenaSaveData NewSave(int seed, ArenaRules rules = null)
        {
            rules = rules ?? ArenaRules.Current;
            var save = new ArenaSaveData { seed = seed != 0 ? seed : 1, rulesVersion = rules.Version };
            Fill(save.ranked, ArenaMode.Ranked, save.seed, rules);
            Fill(save.open, ArenaMode.Open, save.seed, rules);
            return save;
        }

        // Thirty generated rivals, rated from LowRating to HighRating by rung; open rivals' gear rises with the rung to
        // ArenaRivals.TopGearPower.
        public static void Fill(ArenaLadderData ladder, ArenaMode mode, int seed, ArenaRules rules = null)
        {
            if (ladder == null)
                throw new ArgumentNullException(nameof(ladder));
            rules = rules ?? ArenaRules.Current;
            ladder.rivals.Clear();
            for (int rung = 0; rung < Size; rung++)
            {
                // The open ladder's rivals are drawn at their own indices, so the two ladders do not mirror each other.
                int index = mode == ArenaMode.Ranked ? rung : Size + rung;
                ArenaBuild build = mode == ArenaMode.Ranked
                    ? ArenaRivals.Ranked(seed, index, rules)
                    : ArenaRivals.Open(seed, index, ArenaRivals.TopGearPower * rung / (Size - 1), rules);
                ladder.rivals.Add(new ArenaRivalData
                {
                    id = (mode == ArenaMode.Ranked ? "ranked-" : "open-") + rung.ToString("00", System.Globalization.CultureInfo.InvariantCulture),
                    name = ArenaRivals.Name(index),
                    rating = LowRating + (HighRating - LowRating) * rung / (Size - 1),
                    build = ArenaSnapshots.ToData(build)
                });
            }
        }

        // The rivals offered: the count nearest the player's rating (ties to the lower index), strongest first.
        public static List<int> Opponents(ArenaLadderData ladder, int count = Offered)
        {
            if (ladder == null)
                throw new ArgumentNullException(nameof(ladder));
            var order = new List<int>(ladder.rivals.Count);
            for (int i = 0; i < ladder.rivals.Count; i++)
                order.Add(i);
            int rating = ladder.rating;
            order.Sort((x, y) =>
            {
                int dx = Math.Abs(ladder.rivals[x].rating - rating);
                int dy = Math.Abs(ladder.rivals[y].rating - rating);
                return dx != dy ? dx.CompareTo(dy) : x.CompareTo(y);
            });
            List<int> offered = order.GetRange(0, Math.Min(count, order.Count));
            offered.Sort((x, y) => ladder.rivals[y].rating != ladder.rivals[x].rating
                ? ladder.rivals[y].rating.CompareTo(ladder.rivals[x].rating)
                : x.CompareTo(y));
            return offered;
        }

        // The player's build against a rival of the mode's ladder: the fight's seed drawn from the save at its fight count,
        // both builds re-resolved under the rules, a best of three, the ratings moved (zero-sum), the history kept.
        public static ArenaFightOutcome Fight(ArenaSaveData save, ArenaMode mode, int rivalIndex, ArenaBuild player,
            ArenaRules rules = null, DuelLog log = null)
        {
            if (save == null)
                throw new ArgumentNullException(nameof(save));
            if (player == null)
                throw new ArgumentNullException(nameof(player));
            if (player.Mode != mode)
                throw new ArgumentException("The build is for the other arena.", nameof(player));
            rules = rules ?? ArenaRules.Current;
            ArenaLadderData ladder = save.Ladder(mode);
            if (rivalIndex < 0 || rivalIndex >= ladder.rivals.Count)
                throw new ArgumentOutOfRangeException(nameof(rivalIndex));
            ArenaRivalData rival = ladder.rivals[rivalIndex];
            ArenaBuild foeBuild = ArenaSnapshots.ToBuild(rival.build);
            if (foeBuild == null || foeBuild.Mode != mode)
                throw new InvalidOperationException($"Rival '{rival.id}' holds no build for this arena.");

            int seed = ArenaRandom.Stream(save.seed, ArenaRandom.Fights, save.fights).NextSeed();
            ArenaFighter a = ArenaResolver.Resolve(player, rules);
            ArenaFighter b = ArenaResolver.Resolve(foeBuild, rules);
            DuelMatch match = DuelCore.BestOfThree(a, b, seed, log);

            int score = match.Winner == 0 ? 1000 : match.Winner == 1 ? 0 : 500;
            int before = ladder.rating;
            int rivalBefore = rival.rating;
            int delta = Delta(before, rivalBefore, score);
            ladder.rating = before + delta;
            rival.rating = rivalBefore - delta;
            if (match.Winner == 0)
            {
                ladder.wins++;
                rival.losses++;
            }
            else if (match.Winner == 1)
            {
                ladder.losses++;
                rival.wins++;
            }
            else
            {
                ladder.draws++;
            }
            var record = new ArenaMatchData
            {
                fight = save.fights,
                rivalId = rival.id,
                seed = seed,
                rulesVersion = rules.Version,
                rulesHash = ArenaHash.Hex(rules.Hash),
                result = match.Winner == 0 ? "win" : match.Winner == 1 ? "loss" : "draw",
                roundsWon = match.WinsA,
                roundsLost = match.WinsB,
                ratingDelta = delta,
                fingerprint = ArenaHash.Hex(match.Fingerprint)
            };
            ladder.history.Add(record);
            while (ladder.history.Count > ArenaSaveData.MaxHistory)
                ladder.history.RemoveAt(0);
            save.fights++;
            save.rulesVersion = rules.Version;
            return new ArenaFightOutcome
            {
                Mode = mode,
                Match = match,
                Player = a,
                Foe = b,
                Rival = rival,
                RatingBefore = before,
                RatingAfter = ladder.rating,
                RivalRatingBefore = rivalBefore,
                Record = record
            };
        }

        // Keeps the player's build as a past hero (docs/55 section 6: "the player's past snapshots"): stored with the
        // player's rating, and standing in its arena's ladder - in place of the generated rival rated nearest it, or of the
        // oldest past hero once MaxPastSnapshots stand. False, and nothing kept, when the same build already stands.
        public static bool KeepSnapshot(ArenaSaveData save, ArenaBuild build)
        {
            if (save == null)
                throw new ArgumentNullException(nameof(save));
            if (build == null)
                throw new ArgumentNullException(nameof(build));
            ArenaLadderData ladder = save.Ladder(build.Mode);
            string hash = ArenaHash.Hex(build.Hash);
            foreach (ArenaSnapshotData kept in save.pastSnapshots)
            {
                if (kept.hash == hash)
                    return false;
            }
            ArenaSnapshotData data = ArenaSnapshots.ToData(build, ladder.rating, save.fights);
            save.pastSnapshots.Add(data);
            while (save.pastSnapshots.Count > ArenaSaveData.MaxPastSnapshots)
                save.pastSnapshots.RemoveAt(0);

            int place = -1;
            int selves = 0;
            int oldest = -1;
            for (int i = 0; i < ladder.rivals.Count; i++)
            {
                ArenaRivalData r = ladder.rivals[i];
                if (!r.pastSelf)
                {
                    if (place < 0 || Math.Abs(r.rating - data.rating) < Math.Abs(ladder.rivals[place].rating - data.rating))
                        place = i;
                    continue;
                }
                selves++;
                if (oldest < 0 || r.build.fight < ladder.rivals[oldest].build.fight)
                    oldest = i;
            }
            if (selves >= ArenaSaveData.MaxPastSnapshots || place < 0)
                place = oldest;
            if (place < 0)
                return true;
            ladder.rivals[place] = new ArenaRivalData
            {
                // The build's hash, not the fight count: two builds kept between the same two fights stay apart.
                id = "self-" + data.hash.Substring(0, 8),
                name = "You, fight " + save.fights.ToString(System.Globalization.CultureInfo.InvariantCulture),
                rating = data.rating,
                pastSelf = true,
                build = data
            };
            return true;
        }

        // Elo's expectation for a against b, per mille, interpolated in the table.
        public static int Expected(int rating, int foeRating)
        {
            int gap = foeRating - rating;
            int magnitude = Math.Min(800, Math.Abs(gap));
            int step = magnitude / 25;
            int within = magnitude % 25;
            int e = step >= Expectation.Length - 1
                ? Expectation[Expectation.Length - 1]
                : Expectation[step] - (Expectation[step] - Expectation[step + 1]) * within / 25;
            return gap >= 0 ? e : 1000 - e;
        }

        // The rating change for a score per mille (1000 a win, 500 a draw, 0 a loss), rounded half away from zero.
        public static int Delta(int rating, int foeRating, int scorePermille)
        {
            int diff = K * (scorePermille - Expected(rating, foeRating));
            return diff >= 0 ? (diff + 500) / 1000 : -((-diff + 500) / 1000);
        }
    }
}
