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
        // The open offer fought, when the fight was one (FightOffer); null for a ladder rival.
        public ArenaOpenOffer Offer;
        // Whether the rival was a training rival (IsTrainingRival).
        public bool TrainingRival;
    }

    // Which of the three open offers (docs/55 section 10.10): about 2 % of gear power below the player, level with it
    // (within half a per cent), about 2 % above.
    public enum ArenaOfferKind
    {
        Below = 0,
        Level = 1,
        Above = 2
    }

    // One open offer as its card shows it: a rival generated for this fight at a gear power near the player's, both
    // powers, and the expected chance read from the rules' fitted table (never a duel run to find it). Rating is the rating
    // at which Elo's expectation equals that chance, so the open rating moves by what the card promised.
    public sealed class ArenaOpenOffer
    {
        public ArenaOfferKind Kind;
        public int Index;
        public string Id;
        public string Name;
        public ArenaBuild Build;
        public int PlayerPower;
        public int RivalPower;
        public int ExpectedPermille;
        public int Rating;
    }

    // P0's local ladder (docs/55 section 6): 30 rivals a mode with ratings, a simple rating for the player (Elo, K 32, in
    // integers: an expectation table rather than a power of ten, so the save reads the same on every runtime), three
    // opponents offered near the player's rating, and the player's own past heroes standing in it. Pure: it changes the
    // ArenaSaveData it is given; ArenaSaveStore writes it.
    //
    // Since docs/55 section 10.10: the ranked ladder's generated rivals are training rivals, each carrying the rules'
    // handicap for its rung (Fill and Restamp stamp it into the rival's snapshot), and its rating is a training rating
    // (ArenaLadderData.training); the open arena offers three rivals generated at the player's own gear power, 2 % below,
    // level and 2 % above, each with its expected chance (OpenOffers, FightOffer).
    public static class ArenaLadder
    {
        public const int Size = 30;
        public const int StartRating = 1000;
        public const int LowRating = 700;
        public const int HighRating = 1500;
        public const int K = 32;
        public const int Offered = 3;
        // The open offers' gaps in the gear's power index (hundredths of a percent), and where their generator indices
        // start (the ladders' own rivals take 0 to 2 x Size - 1).
        public const int OfferGap = 200;
        public const int OfferIndexBase = 1000;

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
            ladder.training = mode == ArenaMode.Ranked;
            for (int rung = 0; rung < Size; rung++)
            {
                // The open ladder's rivals are drawn at their own indices, so the two ladders do not mirror each other.
                int index = mode == ArenaMode.Ranked ? rung : Size + rung;
                ArenaBuild build = mode == ArenaMode.Ranked
                    ? ArenaRivals.Ranked(seed, index, rules).WithHandicap(rules.TrainingHandicap(rung), rules.Version)
                    : ArenaRivals.Open(seed, index, ArenaRivals.TopGearPower * rung / (Size - 1), rules);
                ladder.rivals.Add(new ArenaRivalData
                {
                    id = (mode == ArenaMode.Ranked ? "ranked-" : "open-") + rung.ToString("00", System.Globalization.CultureInfo.InvariantCulture),
                    name = ArenaRivals.Name(index),
                    rating = LowRating + (HighRating - LowRating) * rung / (Size - 1),
                    rung = rung,
                    build = ArenaSnapshots.ToData(build)
                });
            }
        }

        // Stamps the rules' training handicaps into a save's training rivals (each by its rung, under the rules' version)
        // and marks the save as fought under those rules: after a rules change that moves the handicap table, and when a
        // version-1 save is migrated. Past heroes and the open ladder are left alone.
        public static void Restamp(ArenaSaveData save, ArenaRules rules = null)
        {
            if (save == null)
                throw new ArgumentNullException(nameof(save));
            rules = rules ?? ArenaRules.Current;
            ArenaLadderData ladder = save.ranked;
            if (ladder.training)
            {
                foreach (ArenaRivalData rival in ladder.rivals)
                {
                    if (rival.pastSelf || rival.rung < 0 || rival.build == null)
                        continue;
                    ArenaBuild build = ArenaSnapshots.ToBuild(rival.build);
                    if (build == null)
                        continue;
                    ArenaSnapshotData stamped = ArenaSnapshots.ToData(build.WithHandicap(rules.TrainingHandicap(rival.rung), rules.Version),
                        rival.build.rating, rival.build.fight);
                    rival.build = stamped;
                }
            }
            save.rulesVersion = rules.Version;
        }

        // A training rival: a generated rival of a training ladder (the ranked one until real players fill it).
        public static bool IsTrainingRival(ArenaLadderData ladder, ArenaRivalData rival) =>
            ladder != null && rival != null && ladder.training && !rival.pastSelf;

        // ---- The open offers (docs/55 section 10.10) ----

        // Three rivals generated for the save's next fight at the player's open gear power: 2 % below (never under no
        // gear), level, and 2 % above (never past the gear's ceiling, where it meets level). Each rival is drawn as the
        // ladder's are (a hero, a weapon it carries, a policy) from the save's seed at an index of its own, and wears gear
        // made as near its target power as the tables allow (ArenaRivals.GearNear); its expected chance is the rules' table at
        // the two powers. The same save and build always offer the same three.
        public static List<ArenaOpenOffer> OpenOffers(ArenaSaveData save, ArenaBuild player, ArenaRules rules = null)
        {
            if (save == null)
                throw new ArgumentNullException(nameof(save));
            if (player == null)
                throw new ArgumentNullException(nameof(player));
            if (player.Mode != ArenaMode.Open)
                throw new ArgumentException("Open offers are for an open build.", nameof(player));
            rules = rules ?? ArenaRules.Current;
            int power = ArenaResolver.Resolve(player, rules).GearPower;
            var offers = new List<ArenaOpenOffer>(Offered);
            for (int k = 0; k < Offered; k++)
            {
                var kind = (ArenaOfferKind)k;
                int target = Math.Max(0, power + (k - 1) * OfferGap);
                int index = OfferIndexBase + save.fights * Offered + k;
                ArenaBuild build = ArenaRivals.OpenNear(save.seed, index, target, rules);
                int rivalPower = ArenaResolver.Resolve(build, rules).GearPower;
                int expected = rules.OpenChance(power, rivalPower);
                offers.Add(new ArenaOpenOffer
                {
                    Kind = kind,
                    Index = index,
                    Id = "offer-" + save.fights.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-" + k.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Name = ArenaRivals.Name(index),
                    Build = build,
                    PlayerPower = power,
                    RivalPower = rivalPower,
                    ExpectedPermille = expected,
                    Rating = save.open.rating + GapFor(expected)
                });
            }
            return offers;
        }

        // The player's open build against an offer: a best of three on the save's next seed, the open rating moved by
        // K x (score - the offer's expected chance), the fight kept in the history with the rival's build and both powers.
        public static ArenaFightOutcome FightOffer(ArenaSaveData save, ArenaOpenOffer offer, ArenaBuild player,
            ArenaRules rules = null, DuelLog log = null)
        {
            if (save == null)
                throw new ArgumentNullException(nameof(save));
            if (offer == null)
                throw new ArgumentNullException(nameof(offer));
            if (player == null)
                throw new ArgumentNullException(nameof(player));
            if (player.Mode != ArenaMode.Open)
                throw new ArgumentException("An offer is fought with an open build.", nameof(player));
            rules = rules ?? ArenaRules.Current;
            int seed = ArenaRandom.Stream(save.seed, ArenaRandom.Fights, save.fights).NextSeed();
            ArenaFighter a = ArenaResolver.Resolve(player, rules);
            ArenaFighter b = ArenaResolver.Resolve(offer.Build, rules);
            DuelMatch match = DuelCore.BestOfThree(a, b, seed, log);
            int score = match.Winner == 0 ? 1000 : match.Winner == 1 ? 0 : 500;
            ArenaLadderData ladder = save.open;
            int before = ladder.rating;
            int diff = K * (score - offer.ExpectedPermille);
            int delta = diff >= 0 ? (diff + 500) / 1000 : -((-diff + 500) / 1000);
            ladder.rating = before + delta;
            if (match.Winner == 0)
                ladder.wins++;
            else if (match.Winner == 1)
                ladder.losses++;
            else
                ladder.draws++;
            var record = new ArenaMatchData
            {
                fight = save.fights,
                rivalId = offer.Id,
                seed = seed,
                rulesVersion = rules.Version,
                rulesHash = ArenaHash.Hex(rules.Hash),
                result = match.Winner == 0 ? "win" : match.Winner == 1 ? "loss" : "draw",
                roundsWon = match.WinsA,
                roundsLost = match.WinsB,
                ratingDelta = delta,
                fingerprint = ArenaHash.Hex(match.Fingerprint),
                rivalBuild = ArenaSnapshots.ToData(offer.Build),
                playerPower = a.GearPower,
                rivalPower = b.GearPower,
                expected = offer.ExpectedPermille
            };
            ladder.history.Add(record);
            while (ladder.history.Count > ArenaSaveData.MaxHistory)
                ladder.history.RemoveAt(0);
            save.fights++;
            save.rulesVersion = rules.Version;
            return new ArenaFightOutcome
            {
                Mode = ArenaMode.Open,
                Match = match,
                Player = a,
                Foe = b,
                Rival = new ArenaRivalData { id = offer.Id, name = offer.Name, rating = offer.Rating, build = record.rivalBuild },
                RatingBefore = before,
                RatingAfter = ladder.rating,
                RivalRatingBefore = offer.Rating,
                Record = record,
                Offer = offer
            };
        }

        // The rating gap (rival minus player) at which Elo's expectation for the player is this chance per mille: the
        // expectation table read backwards, linear between its 25-point steps.
        public static int GapFor(int expectedPermille)
        {
            int e = Math.Max(Expectation[Expectation.Length - 1], Math.Min(1000 - Expectation[Expectation.Length - 1], expectedPermille));
            bool favoured = e > 500;
            int m = favoured ? 1000 - e : e;
            int step = 0;
            while (step < Expectation.Length - 1 && Expectation[step + 1] >= m)
                step++;
            int gap = step >= Expectation.Length - 1
                ? 25 * step
                : 25 * step + 25 * (Expectation[step] - m) / Math.Max(1, Expectation[step] - Expectation[step + 1]);
            return favoured ? -gap : gap;
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
                Record = record,
                TrainingRival = IsTrainingRival(ladder, rival)
            };
        }

        // Keeps the player's build as a past hero (docs/55 section 6: "the player's past snapshots"): stored with the
        // player's rating, and standing in its arena's ladder - in place of the generated rival rated nearest it, or of the
        // oldest past hero once MaxPastSnapshots stand. False, and nothing kept, when the same build already stands: among
        // the kept snapshots, or on the ladder itself - the kept list holds MaxPastSnapshots across both arenas, so a build
        // it has let go of can still stand on its ladder, and keeping it again would put the same past hero (the same
        // "self-" id) there twice. Bookkeeping only: no duel rule, and nothing ArenaRules' hash covers, changes.
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
            foreach (ArenaRivalData standing in ladder.rivals)
            {
                if (standing.pastSelf && standing.build != null && standing.build.hash == hash)
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
