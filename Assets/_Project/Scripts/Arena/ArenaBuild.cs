using System;
using System.Collections.Generic;

namespace Cryptforge.Arena
{
    // One efsun line of an open build's gear: a stat and its tier (1..5). The value is the table's, never stored (docs/54
    // section 10), so a retune moves every stored snapshot with it.
    public readonly struct ArenaGearLine
    {
        public readonly ArenaStat Stat;
        public readonly int Tier;

        public ArenaGearLine(ArenaStat stat, int tier)
        {
            if (tier < 1 || tier > ArenaGearTable.TierCount)
                throw new ArgumentOutOfRangeException(nameof(tier));
            Stat = stat;
            Tier = tier;
        }
    }

    // One piece of an open build's gear: its slot (the weapon, armour, amulet or ring, GearSlot's order), its + level and
    // its lines - ids and levels, never resolved values.
    public sealed class ArenaGearPiece
    {
        private readonly ArenaGearLine[] _lines;

        public int Slot { get; }
        public int Level { get; }
        public IReadOnlyList<ArenaGearLine> Lines => _lines;

        public ArenaGearPiece(int slot, int level, IEnumerable<ArenaGearLine> lines)
        {
            if (slot < ArenaGearTable.WeaponSlot || slot > ArenaGearTable.RingSlot)
                throw new ArgumentOutOfRangeException(nameof(slot));
            if (level < 0 || level > ArenaGearTable.MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(level));
            var kept = new List<ArenaGearLine>(ArenaGearTable.MaxLines);
            if (lines != null)
            {
                foreach (ArenaGearLine line in lines)
                {
                    if (kept.Count < ArenaGearTable.MaxLines)
                        kept.Add(line);
                }
            }
            Slot = slot;
            Level = level;
            _lines = kept.ToArray();
        }
    }

    // A build as a snapshot stores it (docs/55 section 3.6): ids and levels only - the hero, the weapon, the three skills,
    // the autopilot's policy, and either the efsun line TYPES the player picked (ranked) or the gear as the profile holds it
    // (open). Never a resolved stat: ArenaResolver re-resolves it under the rules of the fight, so a balance patch reaches
    // every stored defence at once. RulesVersion is the version it was made under (kept for audits); Hash identifies its
    // content.
    public sealed class ArenaBuild
    {
        private readonly string[] _skillIds;
        private readonly ArenaStat[] _lineTypes;
        private readonly ArenaGearPiece[] _gear;

        public ArenaMode Mode { get; }
        public string HeroId { get; }
        public string WeaponId { get; }
        public ArenaPolicy Policy { get; }
        public int RulesVersion { get; }
        public IReadOnlyList<string> SkillIds => _skillIds;
        // Ranked only: the line types picked, in the player's order.
        public IReadOnlyList<ArenaStat> LineTypes => _lineTypes;
        // Open only: the pieces the hero wears with this weapon.
        public IReadOnlyList<ArenaGearPiece> Gear => _gear;
        public ulong Hash { get; }

        private ArenaBuild(ArenaMode mode, string heroId, string weaponId, IEnumerable<string> skillIds, ArenaPolicy policy,
            IEnumerable<ArenaStat> lineTypes, IEnumerable<ArenaGearPiece> gear, int rulesVersion)
        {
            if (string.IsNullOrEmpty(heroId))
                throw new ArgumentException("A hero id is required.", nameof(heroId));
            Mode = mode;
            HeroId = heroId;
            WeaponId = weaponId ?? string.Empty;
            Policy = policy;
            RulesVersion = rulesVersion;
            _skillIds = skillIds != null ? new List<string>(skillIds).ToArray() : new string[0];
            _lineTypes = lineTypes != null ? new List<ArenaStat>(lineTypes).ToArray() : new ArenaStat[0];
            var pieces = new List<ArenaGearPiece>(ArenaGearTable.SlotCount);
            if (gear != null)
            {
                foreach (ArenaGearPiece piece in gear)
                {
                    if (piece != null)
                        pieces.Add(piece);
                }
            }
            _gear = pieces.ToArray();
            Hash = ComputeHash();
        }

        public static ArenaBuild Ranked(string heroId, string weaponId, IEnumerable<string> skillIds, ArenaPolicy policy,
            IEnumerable<ArenaStat> lineTypes, int rulesVersion = ArenaRules.ShippedVersion) =>
            new ArenaBuild(ArenaMode.Ranked, heroId, weaponId, skillIds, policy, lineTypes, null, rulesVersion);

        public static ArenaBuild Open(string heroId, string weaponId, IEnumerable<string> skillIds, ArenaPolicy policy,
            IEnumerable<ArenaGearPiece> gear, int rulesVersion = ArenaRules.ShippedVersion) =>
            new ArenaBuild(ArenaMode.Open, heroId, weaponId, skillIds, policy, null, gear, rulesVersion);

        // The same build in the other arena: a ranked build's identity with this gear, or an open build's identity with
        // these line types.
        public ArenaBuild AsOpen(IEnumerable<ArenaGearPiece> gear) => Open(HeroId, WeaponId, _skillIds, Policy, gear, RulesVersion);

        public ArenaBuild AsRanked(IEnumerable<ArenaStat> lineTypes) =>
            Ranked(HeroId, WeaponId, _skillIds, Policy, lineTypes, RulesVersion);

        public ArenaBuild WithPolicy(ArenaPolicy policy) =>
            new ArenaBuild(Mode, HeroId, WeaponId, _skillIds, policy, _lineTypes, _gear, RulesVersion);

        // FNV-1a over the stored ids and levels in a fixed order; the rules version is not part of the content.
        private ulong ComputeHash()
        {
            ulong hash = ArenaHash.Seed;
            hash = ArenaHash.Mix(hash, (int)Mode);
            hash = ArenaHash.Mix(hash, HeroId);
            hash = ArenaHash.Mix(hash, WeaponId);
            hash = ArenaHash.Mix(hash, (int)Policy);
            hash = ArenaHash.Mix(hash, _skillIds.Length);
            foreach (string id in _skillIds)
                hash = ArenaHash.Mix(hash, id);
            hash = ArenaHash.Mix(hash, _lineTypes.Length);
            foreach (ArenaStat stat in _lineTypes)
                hash = ArenaHash.Mix(hash, ArenaGearTable.Id(stat));
            hash = ArenaHash.Mix(hash, _gear.Length);
            foreach (ArenaGearPiece piece in _gear)
            {
                hash = ArenaHash.Mix(hash, piece.Slot);
                hash = ArenaHash.Mix(hash, piece.Level);
                hash = ArenaHash.Mix(hash, piece.Lines.Count);
                foreach (ArenaGearLine line in piece.Lines)
                {
                    hash = ArenaHash.Mix(hash, ArenaGearTable.Id(line.Stat));
                    hash = ArenaHash.Mix(hash, line.Tier);
                }
            }
            return hash;
        }
    }
}
