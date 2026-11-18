using System;
using System.Collections.Generic;

namespace Cryptforge.Arena
{
    // The arena's own save (P0's local arena, docs/55 section 6): versioned apart from the profile and never inside it, so
    // the profile's save, its version and its migrations stay untouched (two other changes are moving the profile to
    // versions 5 and 6). Plain fields, camelCase as the profile's save data, JSON through ArenaSaveJson (no engine
    // serializer: the class is pure and round-trips in the .NET tests). Nothing here is trusted on load: ArenaSaveJson
    // repairs what it reads, and every build is re-resolved under the fight's rules.
    [Serializable]
    public sealed class ArenaSaveData
    {
        public const int CurrentVersion = 1;
        public const int MaxHistory = 20;
        public const int MaxPastSnapshots = 5;

        public int saveVersion = CurrentVersion;
        // Raised by every save; a load takes the readable file with the highest (ArenaSaveStore).
        public int revision;
        // The rules the ladder last fought under.
        public int rulesVersion = ArenaRules.ShippedVersion;
        // The arena's own seed, drawn once when the save is made: the rivals' builds and every fight's seed derive from it.
        public int seed;
        // Fights played in both arenas; the next fight's seed is drawn at this index.
        public int fights;
        public ArenaLadderData ranked = new ArenaLadderData();
        public ArenaLadderData open = new ArenaLadderData();
        // The player's chosen builds (hero, weapon, policy; ranked: its line types). The open build's gear is read from
        // the profile at each fight.
        public ArenaSnapshotData rankedBuild;
        public ArenaSnapshotData openBuild;
        // The player's own past heroes (at most MaxPastSnapshots, oldest first); each also stands in its arena's ladder.
        public List<ArenaSnapshotData> pastSnapshots = new List<ArenaSnapshotData>();

        public ArenaLadderData Ladder(ArenaMode mode) => mode == ArenaMode.Ranked ? ranked : open;
    }

    [Serializable]
    public sealed class ArenaLadderData
    {
        public int rating = ArenaLadder.StartRating;
        public int wins;
        public int losses;
        public int draws;
        public List<ArenaRivalData> rivals = new List<ArenaRivalData>();
        // The latest fights, newest last, at most ArenaSaveData.MaxHistory.
        public List<ArenaMatchData> history = new List<ArenaMatchData>();
    }

    [Serializable]
    public sealed class ArenaRivalData
    {
        public string id;
        public string name;
        public int rating;
        public int wins;
        public int losses;
        // True for one of the player's own past heroes standing in the ladder.
        public bool pastSelf;
        public ArenaSnapshotData build;
    }

    // A build as stored: ids and levels only (ArenaBuild's fields), the rules version it was made under and its content
    // hash; for a past hero, the rating it had and the fight it was taken at.
    [Serializable]
    public sealed class ArenaSnapshotData
    {
        public string mode;
        public string heroId;
        public string weaponId;
        public List<string> skillIds = new List<string>();
        public string policy;
        public List<string> lineIds = new List<string>();
        public List<ArenaGearPieceData> gear = new List<ArenaGearPieceData>();
        public int rulesVersion;
        public string hash;
        public int rating;
        public int fight;
    }

    [Serializable]
    public sealed class ArenaGearPieceData
    {
        public int slot;
        public int level;
        public List<ArenaLineData> lines = new List<ArenaLineData>();
    }

    [Serializable]
    public sealed class ArenaLineData
    {
        public string stat;
        public int tier;
    }

    // A fight as the history keeps it: enough to re-run it (the seed, the rules, the rival's id) and to check a re-run
    // (the fingerprint).
    [Serializable]
    public sealed class ArenaMatchData
    {
        public int fight;
        public string rivalId;
        public int seed;
        public int rulesVersion;
        public string rulesHash;
        // "win", "loss" or "draw", the player's.
        public string result;
        public int roundsWon;
        public int roundsLost;
        public int ratingDelta;
        public string fingerprint;
    }

    // Between a stored snapshot and a build: names, not enum numbers, so a reordered enum cannot move a save.
    public static class ArenaSnapshots
    {
        public static string ModeId(ArenaMode mode) => mode == ArenaMode.Open ? "open" : "ranked";
        public static string PolicyId(ArenaPolicy policy) => policy == ArenaPolicy.Bold ? "bold" : "careful";

        public static ArenaSnapshotData ToData(ArenaBuild build, int rating = 0, int fight = 0)
        {
            if (build == null)
                throw new ArgumentNullException(nameof(build));
            var data = new ArenaSnapshotData
            {
                mode = ModeId(build.Mode),
                heroId = build.HeroId,
                weaponId = build.WeaponId,
                skillIds = new List<string>(build.SkillIds),
                policy = PolicyId(build.Policy),
                rulesVersion = build.RulesVersion,
                hash = ArenaHash.Hex(build.Hash),
                rating = rating,
                fight = fight
            };
            foreach (ArenaStat stat in build.LineTypes)
                data.lineIds.Add(ArenaGearTable.Id(stat));
            foreach (ArenaGearPiece piece in build.Gear)
            {
                var p = new ArenaGearPieceData { slot = piece.Slot, level = piece.Level };
                foreach (ArenaGearLine line in piece.Lines)
                    p.lines.Add(new ArenaLineData { stat = ArenaGearTable.Id(line.Stat), tier = line.Tier });
                data.gear.Add(p);
            }
            return data;
        }

        // The build a stored snapshot describes, repaired: unknown line ids and out-of-range pieces dropped, levels and
        // tiers clamped. Null when the snapshot names no hero. The resolver repairs the rest against the fight's rules.
        public static ArenaBuild ToBuild(ArenaSnapshotData data)
        {
            if (data == null || string.IsNullOrEmpty(data.heroId))
                return null;
            ArenaPolicy policy = data.policy == "bold" ? ArenaPolicy.Bold : ArenaPolicy.Careful;
            if (data.mode == "open")
            {
                var pieces = new List<ArenaGearPiece>();
                if (data.gear != null)
                {
                    foreach (ArenaGearPieceData p in data.gear)
                    {
                        if (p == null || p.slot < ArenaGearTable.WeaponSlot || p.slot > ArenaGearTable.RingSlot)
                            continue;
                        if (pieces.Exists(existing => existing.Slot == p.slot))
                            continue;
                        var lines = new List<ArenaGearLine>();
                        if (p.lines != null)
                        {
                            foreach (ArenaLineData line in p.lines)
                            {
                                if (line != null && ArenaGearTable.TryParse(line.stat, out ArenaStat stat) &&
                                    stat != ArenaGearTable.MainStatOf(p.slot) && !lines.Exists(l => l.Stat == stat))
                                    lines.Add(new ArenaGearLine(stat, Math.Max(1, Math.Min(ArenaGearTable.TierCount, line.tier))));
                            }
                        }
                        pieces.Add(new ArenaGearPiece(p.slot, Math.Max(0, Math.Min(ArenaGearTable.MaxLevel, p.level)), lines));
                    }
                }
                return ArenaBuild.Open(data.heroId, data.weaponId, data.skillIds, policy, pieces, data.rulesVersion);
            }
            var types = new List<ArenaStat>();
            if (data.lineIds != null)
            {
                foreach (string id in data.lineIds)
                {
                    if (ArenaGearTable.TryParse(id, out ArenaStat stat))
                        types.Add(stat);
                }
            }
            return ArenaBuild.Ranked(data.heroId, data.weaponId, data.skillIds, policy, types, data.rulesVersion);
        }
    }
}
