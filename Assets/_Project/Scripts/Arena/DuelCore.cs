using System;
using System.Collections.Generic;

namespace Cryptforge.Arena
{
    // One round of the duel as it ended. Winner: 0 the attacker (A), 1 the defender (B), -1 a draw. Health in milli-points
    // (not below zero), positions in micro-units, Credits per ArenaEffect in milli-points (DuelCredits).
    public sealed class DuelRoundResult
    {
        public int Round;
        public int Winner;
        public int Ticks;
        public int HealthA;
        public int HealthB;
        public int MaxHealthA;
        public int MaxHealthB;
        public int PositionA;
        public int PositionB;
        // The boons dealt (indices into the rules' boons), empty when none.
        public int[] BoonsA;
        public int[] BoonsB;
        public long[] CreditsA;
        public long[] CreditsB;
    }

    // A best of three as it ended: the rounds, the score, the fingerprint (the FNV-1a of every event the duel made, the
    // log's own fingerprint when one was kept), the credits summed over the rounds and the "why".
    public sealed class DuelMatch
    {
        public int Winner;
        public int WinsA;
        public int WinsB;
        public int Seed;
        public int RulesVersion;
        public ulong RulesHash;
        public ulong Fingerprint;
        public int Events;
        public List<DuelRoundResult> Rounds = new List<DuelRoundResult>(DuelRules.MaxRounds);
        public long[] CreditsA;
        public long[] CreditsB;
        public DuelWhy[] Why = new DuelWhy[0];

        public int TotalTicks
        {
            get
            {
                int ticks = 0;
                foreach (DuelRoundResult r in Rounds)
                    ticks += r.Ticks;
                return ticks;
            }
        }
    }

    // The arena's integer duel (docs/55 sections 2.2 and 3.2), grown from Tools/CombatChecks/ArenaDuel.cs: two resolved
    // builds in a lane, both played by the autopilot, 60 ticks a second, no float anywhere - so the Editor's Mono, the
    // phone's IL2CPP and a server's .NET replay a fight bit for bit (ArenaDuelCoreTests pins a fingerprint computed on
    // .NET and checked in Unity). Both sides decide on the tick's opening state and their blows land together: no
    // first-mover edge. Randomness is ArenaRandom's, derived per (seed, purpose, index).
    //
    // The heroes fight with their own three skills (docs/57 decision 8) at the rules' rank, read the hero's own maximum
    // health (never the arena's stretched one), and each round deals both sides boons of the same quality for variety.
    // Where the arena departs from the run, measured (docs/55 section 10): the walk goes by role (a kiter backs off, then
    // a chaser closes) with a kiter's backpedal slower than a chaser; and Ember Memory's burn grows with the build's
    // output bonus, where the run's is flat.
    public static class DuelCore
    {
        // Positions are kept in micro-units (a thousandth of the tables' milli-units) so a few per cent of walking speed is
        // not lost to rounding.
        public const int Precision = 1000;

        private const int OutCapacity = 2 + DuelRules.SkillSlots;

        private sealed class Blow
        {
            public ArenaEffect Source;
            public DuelEventKind Kind;
            public int Slot;
            public int Raw;
            public int CritExtra;
            public bool Rhythm;
            public int HeatExtra;
            public int Stacks;
            public bool Struck;
        }

        private sealed class Side
        {
            public int Index;
            public ArenaFighter F;
            // Fixed for the round.
            public int MaxHealth;
            public int OwnMaxHealth;
            public int Damage;
            public int IntervalTicks;
            public int AttackSpeed;
            public int CritPermille;
            public int CritEvery;
            public int RhythmPercent;
            public int Armour;
            public int BurnTick;
            public bool BurnOnHit;
            public bool Bold;
            public long Reach;
            public long Speed;
            public bool Kites;
            public ArenaSkill[] Skills;
            public int[] SkillPower;
            public int[] SkillSlow;
            public int[] SkillCooldown;
            // The fight's state.
            public long X;
            public long LastStep;
            public int Health;
            public long Progress;
            public int Strikes;
            public int[] Cooldowns;
            public ArenaRandom Dice;
            public int GuardTicks;
            public int GuardPermille;
            public int StunTicks;
            public int SlowTicks;
            public int SlowPermille;
            public int BurnStacks;
            public int BurnLeft;
            public int BurnClock;
            public int SigilTicks;
            public int SigilAge;
            public long SigilX;
            public long SigilRadius;
            public int SigilHeat;
            public int SigilSlow;
            public int Ward;
            public int WardTicks;
            public int WardStacks;
            // What the foe's act did to this side this tick, settled once both have acted and landed.
            public int PendingStacks;
            public bool PendingConsume;
            public int PendingStun;
            public int PendingSlowTicks;
            public int PendingSlowPermille;
            public long PendingX;
            public bool PendingMove;
            // The blows this side lands on the foe this tick.
            public readonly Blow[] Out = { new Blow(), new Blow(), new Blow(), new Blow(), new Blow() };
            public int OutCount;
            // Tallies for the credits.
            public readonly DuelTally Tally = new DuelTally();
        }

        // The event sink: every event goes into the fingerprint, and into the log when one is kept.
        private sealed class Sink
        {
            public DuelLog Log;
            public ulong Hash = ArenaHash.Seed;
            public int Count;
            public int Round;

            public void Emit(int tick, DuelEventKind kind, int side, ArenaEffect effect, int value, int extra, int aux)
            {
                var e = new DuelEvent(Round, tick, kind, side, effect, value, extra, aux);
                Hash = e.MixInto(Hash);
                Count++;
                Log?.Add(e);
            }
        }

        // Best of three under the fighters' rules: up to five rounds while rounds draw, each round's seed and boons derived
        // from the match's seed; still level after five, a draw. A log, when given, receives every event in order.
        public static DuelMatch BestOfThree(ArenaFighter a, ArenaFighter b, int seed, DuelLog log = null)
        {
            ArenaRules rules = CheckRules(a, b);
            var sink = new Sink { Log = log };
            var match = new DuelMatch
            {
                Seed = seed,
                RulesVersion = rules.Version,
                RulesHash = rules.Hash,
                CreditsA = new long[DuelCredits.Count],
                CreditsB = new long[DuelCredits.Count]
            };
            for (int round = 0; round < DuelRules.MaxRounds && match.WinsA < DuelRules.WinsNeeded && match.WinsB < DuelRules.WinsNeeded; round++)
            {
                int roundSeed = ArenaRandom.Stream(seed, ArenaRandom.Rounds, round).NextSeed();
                sink.Round = round;
                DuelRoundResult r = Play(a, b, roundSeed, Deal(rules, seed, round, 0), Deal(rules, seed, round, 1), sink);
                r.Round = round;
                match.Rounds.Add(r);
                if (r.Winner == 0)
                    match.WinsA++;
                else if (r.Winner == 1)
                    match.WinsB++;
                for (int i = 0; i < DuelCredits.Count; i++)
                {
                    match.CreditsA[i] += r.CreditsA[i];
                    match.CreditsB[i] += r.CreditsB[i];
                }
            }
            match.Winner = match.WinsA >= DuelRules.WinsNeeded ? 0 : match.WinsB >= DuelRules.WinsNeeded ? 1 : -1;
            match.Fingerprint = sink.Hash;
            match.Events = sink.Count;
            match.Why = DuelSummary.Explain(match);
            return match;
        }

        // One round with the boons given (none by default): the research's single duel.
        public static DuelRoundResult Round(ArenaFighter a, ArenaFighter b, int seed, DuelLog log = null, int[] boonsA = null,
            int[] boonsB = null)
        {
            CheckRules(a, b);
            var sink = new Sink { Log = log };
            return Play(a, b, seed, boonsA ?? new int[0], boonsB ?? new int[0], sink);
        }

        private static ArenaRules CheckRules(ArenaFighter a, ArenaFighter b)
        {
            if (a == null)
                throw new ArgumentNullException(nameof(a));
            if (b == null)
                throw new ArgumentNullException(nameof(b));
            if (a.Rules.Hash != b.Rules.Hash)
                throw new ArgumentException("Both fighters must be resolved under the same rules.", nameof(b));
            return a.Rules;
        }

        // The boons a side is dealt in a round: BoonsDealt distinct ones, drawn from the match's seed.
        public static int[] Deal(ArenaRules rules, int seed, int round, int side)
        {
            int count = rules.BoonsDealt;
            var dealt = new int[count];
            if (count == 0)
                return dealt;
            ArenaRandom dice = ArenaRandom.Stream(seed, ArenaRandom.Boons, round * 2 + side);
            var taken = new bool[rules.Boons.Count];
            for (int i = 0; i < count; i++)
            {
                int pick = dice.NextBelow(rules.Boons.Count);
                while (taken[pick])
                    pick = (pick + 1) % rules.Boons.Count;
                taken[pick] = true;
                dealt[i] = pick;
            }
            return dealt;
        }

        private static DuelRoundResult Play(ArenaFighter a, ArenaFighter b, int seed, int[] boonsA, int[] boonsB, Sink sink)
        {
            Side sa = Enter(a, boonsA, -DuelRules.Entry, seed, 0, sink);
            Side sb = Enter(b, boonsB, DuelRules.Entry, seed, 1, sink);
            int tick = 0;
            while (tick < DuelRules.LimitTicks)
            {
                // The walk, by role rather than by side: a kiting weapon backs off from the tick's opening positions,
                // then a side short of its reach closes on where the foe now stands - so a chaser keeps up with a kiter no
                // faster than itself, whichever side either is. Then both act on where they stand.
                long backA = Back(sa, sb);
                long backB = Back(sb, sa);
                sa.X += backA;
                sb.X += backB;
                long onA = Close(sa, sb);
                long onB = Close(sb, sa);
                sa.X += onA;
                sb.X += onB;
                Walked(sa, backA + onA, tick, sink);
                Walked(sb, backB + onB, tick, sink);
                sa.OutCount = 0;
                sb.OutCount = 0;
                Act(sa, sb, tick, sink);
                Act(sb, sa, tick, sink);
                if (sa.BurnStacks > 0)
                    Burn(sa, sb, tick, sink);
                if (sb.BurnStacks > 0)
                    Burn(sb, sa, tick, sink);
                // Both sides' blows land together.
                Land(sb, sa, tick, sink);
                Land(sa, sb, tick, sink);
                Settle(sa, tick, sink);
                Settle(sb, tick, sink);
                Expire(sa);
                Expire(sb);
                tick++;
                if (sa.Health <= 0 || sb.Health <= 0)
                    break;
            }

            int winner;
            if (sa.Health <= 0 && sb.Health <= 0)
                winner = -1;
            else if (sb.Health <= 0)
                winner = 0;
            else if (sa.Health <= 0)
                winner = 1;
            else
            {
                // The limit: the larger share of health left wins.
                long shareA = (long)sa.Health * sb.MaxHealth;
                long shareB = (long)sb.Health * sa.MaxHealth;
                winner = shareA > shareB ? 0 : shareB > shareA ? 1 : -1;
            }
            sink.Emit(tick, DuelEventKind.RoundEnd, winner < 0 ? 2 : winner, ArenaEffect.Weapon, Math.Max(0, sa.Health),
                Math.Max(0, sb.Health), tick);
            return new DuelRoundResult
            {
                Winner = winner,
                Ticks = tick,
                HealthA = Math.Max(0, sa.Health),
                HealthB = Math.Max(0, sb.Health),
                MaxHealthA = sa.MaxHealth,
                MaxHealthB = sb.MaxHealth,
                PositionA = (int)sa.X,
                PositionB = (int)sb.X,
                BoonsA = boonsA,
                BoonsB = boonsB,
                CreditsA = DuelCredits.Of(a, boonsA, sa.MaxHealth, sa.Tally),
                CreditsB = DuelCredits.Of(b, boonsB, sb.MaxHealth, sb.Tally)
            };
        }

        // The fighter's numbers for this round, its boons laid on its totals.
        private static Side Enter(ArenaFighter f, int[] boons, int entry, int seed, int index, Sink sink)
        {
            ArenaRules rules = f.Rules;
            int Total(ArenaStat stat)
            {
                int total = f.Total(stat);
                foreach (int boon in boons)
                {
                    if (rules.Boons[boon].Stat == stat)
                        total += rules.Boons[boon].Hundredths;
                }
                return total;
            }

            ArenaHero hero = f.Hero;
            ArenaWeapon weapon = f.Weapon;
            int damage = Total(ArenaStat.Damage);
            int own = (int)((long)hero.Health * (10000 + Total(ArenaStat.MaxHealth)) / 10000);
            var s = new Side
            {
                Index = index,
                F = f,
                OwnMaxHealth = own,
                MaxHealth = (int)((long)own * DuelRules.HealthScale * hero.HealthPermille / 1000),
                Damage = (int)((long)weapon.Damage * hero.DamagePermille * weapon.DamagePermille / 1000 * (10000 + damage) / 10000000),
                IntervalTicks = weapon.IntervalTicks,
                AttackSpeed = Total(ArenaStat.AttackSpeed),
                CritPermille = DuelRules.BaseCritPermille + Total(ArenaStat.CritChance) / 10,
                CritEvery = weapon.CritEvery,
                RhythmPercent = weapon.RhythmPercent,
                Armour = Math.Min(9000, Total(ArenaStat.Armour)),
                BurnTick = rules.BurnScales
                    ? BurnTickOf(hero, damage, Total(ArenaStat.AttackSpeed), Total(ArenaStat.CritChance), Total(ArenaStat.BurnDamage))
                    : BurnTickOf(hero, 0, 0, 0, Total(ArenaStat.BurnDamage)),
                BurnOnHit = hero.BurnOnHit,
                Bold = f.Policy == ArenaPolicy.Bold,
                Reach = (long)weapon.Reach * Precision,
                Speed = (long)hero.SpeedPerTick * Precision * (10000 + Total(ArenaStat.MoveSpeed)) / 10000,
                Kites = weapon.Kites,
                X = (long)entry * Precision,
                Dice = ArenaRandom.Stream(seed, ArenaRandom.Crits, index)
            };
            s.Health = s.MaxHealth;
            int cooldown = Math.Min(ArenaGearTable.Cap(ArenaStat.AbilityCooldown), Total(ArenaStat.AbilityCooldown));
            int count = f.Skills.Count;
            s.Skills = new ArenaSkill[count];
            s.SkillPower = new int[count];
            s.SkillSlow = new int[count];
            s.SkillCooldown = new int[count];
            s.Cooldowns = new int[count];
            for (int i = 0; i < count; i++)
            {
                ArenaSkill skill = f.Skills[i];
                s.Skills[i] = skill;
                int power = ArenaSkill.AtRank(skill.Power, rules.SkillRank);
                // A skill that strikes scales with the build: the hero's coefficient and its damage bonus, never the
                // weapon's coefficient (docs/57 section 3.0: the skill's base x the weapon's damage over its base).
                if (skill.Kind == ArenaSkillKind.Burst || skill.Kind == ArenaSkillKind.Rush || skill.Kind == ArenaSkillKind.Vent)
                    power = (int)((long)power * hero.DamagePermille * (10000 + damage) / 10000000);
                s.SkillPower[i] = power;
                s.SkillSlow[i] = ArenaSkill.AtRank(skill.SlowPermille, rules.SkillRank);
                s.SkillCooldown[i] = (int)((long)skill.CooldownTicks * (10000 - cooldown) / 10000);
            }
            // An opening offset so two equal builds do not swing in lockstep: up to one interval, and each skill ready at
            // half its cooldown plus up to a second, seeded per side.
            ArenaRandom opening = ArenaRandom.Stream(seed, ArenaRandom.Openings, index);
            s.Progress = (long)opening.NextBelow(s.IntervalTicks) * 10000;
            for (int i = 0; i < count; i++)
                s.Cooldowns[i] = s.SkillCooldown[i] / 2 + opening.NextBelow(DuelRules.TicksPerSecond);
            int boonAux = boons.Length > 0 ? boons[0] + 1 : 0;
            sink.Emit(0, DuelEventKind.RoundStart, index, ArenaEffect.Weapon, s.MaxHealth, (int)s.X, boonAux);
            return s;
        }

        // A burn stack's tick, milli-points: BurnRule's 1.5 a stack every 0.5 s x the hero's coefficient, grown by the
        // build's whole output bonus - damage, attack speed and crit (each crit point a per cent, crits striking x2), the
        // terms of the gear's DPS (docs/54 section 7.1) - and by its burn bonus. The run's burn is flat; in the lane it is a
        // third of Ilya's damage, and flat it moved the hero balance with the gear: with the coefficients tuned at ranked's
        // + level, Ilya won 80 % of open fights at no gear against the Vanguard's Sword and lost at the ceiling (measured,
        // section 10 of docs/55). Fixed point at a thousandth of a milli-point, one rounding at the end.
        private static int BurnTickOf(ArenaHero hero, int damage, int attackSpeed, int crit, int burn)
        {
            long tick = (long)DuelRules.BurnTickMilli * hero.DamagePermille;
            tick = tick * (10000 + damage) / 10000;
            tick = tick * (10000 + attackSpeed) / 10000;
            tick = tick * (10000 + crit) / 10000;
            tick = tick * (10000 + burn) / 10000;
            return (int)(tick / 1000);
        }

        private static long Distance(Side s, Side foe) => Math.Abs(s.X - foe.X);

        private static long Speed(Side s) => s.SlowTicks > 0 ? s.Speed * (1000 - s.SlowPermille) / 1000 : s.Speed;

        // A kiting weapon's step back once the foe is inside its reach (less the slack), at the rules' backpedal share of
        // its speed; nothing while dazed. The walls stop it.
        private static long Back(Side s, Side foe)
        {
            if (s.StunTicks > 0 || !s.Kites || Distance(s, foe) >= s.Reach - (long)DuelRules.KiteSlack * Precision)
                return 0;
            int away = foe.X > s.X ? -1 : 1;
            return Clamp(s.X + away * (Speed(s) * s.F.Rules.BackpedalPermille / 1000)) - s.X;
        }

        // A step toward the foe while it stands beyond the weapon's reach, never past the reach; nothing while dazed.
        private static long Close(Side s, Side foe)
        {
            long distance = Distance(s, foe);
            if (s.StunTicks > 0 || distance <= s.Reach)
                return 0;
            int toward = foe.X > s.X ? 1 : -1;
            return Clamp(s.X + toward * Math.Min(Speed(s), distance - s.Reach)) - s.X;
        }

        private static long Clamp(long x)
        {
            long wall = (long)DuelRules.Wall * Precision;
            return Math.Max(-wall, Math.Min(wall, x));
        }

        private static void Walked(Side s, long step, int tick, Sink sink)
        {
            if (step != s.LastStep)
            {
                s.LastStep = step;
                sink.Emit(tick, DuelEventKind.Move, s.Index, ArenaEffect.Weapon, (int)s.X, (int)step, 0);
            }
        }

        private static void Act(Side s, Side foe, int tick, Sink sink)
        {
            long distance = Distance(s, foe);
            if (s.StunTicks > 0)
            {
                // Dazed: no swing and no skill; the clocks still run.
                for (int i = 0; i < s.Cooldowns.Length; i++)
                {
                    if (s.Cooldowns[i] > 0)
                        s.Cooldowns[i]--;
                }
                Glyph(s, foe);
                return;
            }

            long need = (long)s.IntervalTicks * 10000;
            s.Progress += 10000 + s.AttackSpeed;
            if (s.Progress >= need)
            {
                if (distance <= s.Reach)
                {
                    s.Progress -= need;
                    s.Strikes++;
                    bool rhythm = s.CritEvery > 0 && s.Strikes % s.CritEvery == 0;
                    bool crit = s.Dice.NextBelow(1000) < s.CritPermille;
                    int extra = rhythm ? s.Damage * (s.RhythmPercent - 100) / 100 :
                        crit ? s.Damage * (DuelRules.CritPercent - 100) / 100 : 0;
                    Queue(s, ArenaEffect.Weapon, DuelEventKind.Strike, -1, s.Damage + extra, extra, rhythm, 0, 0, true);
                    if (s.BurnOnHit)
                        foe.PendingStacks++;
                }
                else
                {
                    s.Progress = need;
                }
            }

            for (int i = 0; i < s.Cooldowns.Length; i++)
            {
                if (s.Cooldowns[i] > 0)
                {
                    s.Cooldowns[i]--;
                    continue;
                }
                if (Use(s, foe, i, distance))
                {
                    s.Cooldowns[i] = s.SkillCooldown[i];
                    ArenaSkill skill = s.Skills[i];
                    int lasting = skill.Kind == ArenaSkillKind.Guard || skill.Kind == ArenaSkillKind.Sigil || skill.Kind == ArenaSkillKind.Ward
                        ? skill.DurationTicks : 0;
                    sink.Emit(tick, DuelEventKind.Skill, s.Index, skill.Effect, i, lasting,
                        skill.Kind == ArenaSkillKind.Sigil ? (int)s.SigilX : 0);
                }
            }
            Glyph(s, foe);
        }

        // Whether skill i is used this tick by the side's policy; what it does to the foe waits in the foe's Pending
        // fields until Settle.
        private static bool Use(Side s, Side foe, int i, long distance)
        {
            ArenaSkill skill = s.Skills[i];
            long radius = (long)skill.Radius * Precision;
            bool atHand = distance <= foe.Reach + (long)DuelRules.AtHand * Precision;
            bool hurt = (long)s.Health * 1000 < (long)s.MaxHealth * DuelRules.CarefulHealthPermille;
            switch (skill.Kind)
            {
                case ArenaSkillKind.Burst:
                    if (distance > radius)
                        return false;
                    QueueSkill(s, skill, i, s.SkillPower[i]);
                    return true;
                case ArenaSkillKind.Guard:
                    // Careful: under 70 % with the foe at hand. Bold: whenever the foe is at hand.
                    if (!atHand || (!s.Bold && !hurt))
                        return false;
                    s.GuardTicks = skill.DurationTicks;
                    s.GuardPermille = ArenaSkill.AtRank(skill.Power, s.F.Rules.SkillRank);
                    return true;
                case ArenaSkillKind.Rush:
                    if (distance > s.Reach + radius)
                        return false;
                    if (distance > s.Reach)
                    {
                        int toward = foe.X > s.X ? 1 : -1;
                        s.PendingX = s.X + toward * (distance - s.Reach);
                        s.PendingMove = true;
                    }
                    QueueSkill(s, skill, i, s.SkillPower[i]);
                    foe.PendingStun = Math.Max(foe.PendingStun, skill.StunTicks);
                    return true;
                case ArenaSkillKind.Vent:
                    // Careful: four stacks on the foe; bold: any.
                    if (distance > radius || foe.BurnStacks < (s.Bold ? 1 : DuelRules.VentStacks))
                        return false;
                    QueueSkill(s, skill, i, s.SkillPower[i] * foe.BurnStacks);
                    foe.PendingConsume = true;
                    return true;
                case ArenaSkillKind.Sigil:
                    // Careful: down as the foe comes on; bold: whenever the foe is in the lane's half.
                    if (s.SigilTicks > 0 ||
                        distance > (s.Bold ? (long)DuelRules.SigilBoldReach * Precision : s.Reach + (long)DuelRules.SigilCarefulReach * Precision))
                        return false;
                    s.SigilTicks = skill.DurationTicks;
                    s.SigilAge = 0;
                    s.SigilX = s.X;
                    s.SigilRadius = radius;
                    s.SigilHeat = s.SkillPower[i];
                    s.SigilSlow = s.SkillSlow[i];
                    return true;
                case ArenaSkillKind.Ward:
                    // As the guard. The ward is a share of the hero's own maximum health, not of the arena's stretched one
                    // (docs/57 section 3.0): an earlier draft that read the stretched health won every duel alone.
                    if (!atHand || (!s.Bold && !hurt))
                        return false;
                    s.Ward = (int)((long)s.OwnMaxHealth * ArenaSkill.AtRank(skill.Power, s.F.Rules.SkillRank) / 1000);
                    s.WardTicks = skill.DurationTicks;
                    s.WardStacks = skill.Stacks;
                    return true;
                default:
                    return false;
            }
        }

        private static void QueueSkill(Side s, ArenaSkill skill, int slot, int damage)
        {
            bool crit = s.Dice.NextBelow(1000) < s.CritPermille;
            int extra = crit ? damage * (DuelRules.CritPercent - 100) / 100 : 0;
            Queue(s, skill.Effect, DuelEventKind.Hit, slot, damage + extra, extra, false, 0, 0, true);
        }

        private static void Queue(Side s, ArenaEffect source, DuelEventKind kind, int slot, int raw, int critExtra, bool rhythm,
            int heatExtra, int stacks, bool struck)
        {
            if (s.OutCount >= OutCapacity)
                throw new InvalidOperationException("More blows in one tick than a fighter can land.");
            Blow blow = s.Out[s.OutCount++];
            blow.Source = source;
            blow.Kind = kind;
            blow.Slot = slot;
            blow.Raw = raw;
            blow.CritExtra = critExtra;
            blow.Rhythm = rhythm;
            blow.HeatExtra = heatExtra;
            blow.Stacks = stacks;
            blow.Struck = struck;
        }

        // The caster's glyph: a foe standing in it is slowed while it stands there and catches a burn stack every
        // BurnTickTicks of the glyph's life.
        private static void Glyph(Side s, Side foe)
        {
            if (s.SigilTicks <= 0)
                return;
            s.SigilAge++;
            if (Math.Abs(foe.X - s.SigilX) > s.SigilRadius)
                return;
            if (s.SigilSlow > 0)
            {
                // Two ticks: Settle sets it and Expire takes one before the foe's next step reads it.
                foe.PendingSlowTicks = Math.Max(foe.PendingSlowTicks, 2);
                foe.PendingSlowPermille = Math.Max(foe.PendingSlowPermille, s.SigilSlow);
            }
            if (s.SigilAge % DuelRules.BurnTickTicks == 0)
                foe.PendingStacks++;
        }

        // The burning side's burn this tick (BurnState.Tick in ticks), landed as a blow of the burner's: stacks x the
        // burner's tick every 0.5 s of burning, nothing for a part tick when the burn runs out; hotter inside the burner's
        // glyph.
        private static void Burn(Side s, Side burner, int tick, Sink sink)
        {
            if (++s.BurnClock >= DuelRules.BurnTickTicks)
            {
                s.BurnClock -= DuelRules.BurnTickTicks;
                int damage = s.BurnStacks * burner.BurnTick;
                int heat = 0;
                if (burner.SigilTicks > 0 && Math.Abs(s.X - burner.SigilX) <= burner.SigilRadius)
                    heat = (int)((long)damage * burner.SigilHeat / 1000);
                Queue(burner, ArenaEffect.EmberBurn, DuelEventKind.Burn, -1, damage + heat, 0, false, heat, s.BurnStacks, false);
            }
            if (--s.BurnLeft <= 0)
            {
                s.BurnStacks = 0;
                s.BurnClock = 0;
                sink.Emit(tick, DuelEventKind.Stacks, s.Index, ArenaEffect.EmberBurn, 0, 0, 0);
            }
        }

        // The striker's queued blows on the victim, in the order they were made: the victim's armour, then its guard,
        // then its ward; every blow the ward absorbs gives the striker the ward's stacks.
        private static void Land(Side victim, Side striker, int tick, Sink sink)
        {
            for (int i = 0; i < striker.OutCount; i++)
            {
                Blow blow = striker.Out[i];
                if (blow.Raw <= 0)
                    continue;
                int armoured = (int)((long)blow.Raw * victim.Armour / 10000);
                int after = blow.Raw - armoured;
                int guarded = victim.GuardTicks > 0 ? (int)((long)after * victim.GuardPermille / 1000) : 0;
                after -= guarded;
                int warded = 0;
                if (victim.Ward > 0 && after > 0)
                {
                    warded = Math.Min(victim.Ward, after);
                    victim.Ward -= warded;
                    after -= warded;
                    if (blow.Struck)
                        striker.PendingStacks += victim.WardStacks;
                }
                victim.Health -= after;

                DuelTally t = striker.Tally;
                long share = blow.Raw > 0 ? (long)after * (blow.CritExtra + blow.HeatExtra) / blow.Raw : 0;
                switch (blow.Source)
                {
                    case ArenaEffect.Weapon:
                        t.Weapon += after - share;
                        if (blow.Rhythm)
                            t.Rhythm += share;
                        else
                            t.Crit += share;
                        break;
                    case ArenaEffect.EmberBurn:
                        t.Burn += after - share;
                        t.Heat += share;
                        break;
                    default:
                        t.Crit += share;
                        t.AddSkill(blow.Source, after - share);
                        break;
                }
                victim.Tally.Armoured += armoured;
                victim.Tally.Guarded += guarded;
                victim.Tally.Warded += warded;
                victim.Tally.Taken += after;

                int flags = (blow.CritExtra > 0 ? 1 : 0) | (blow.Rhythm ? 2 : 0);
                int aux = blow.Kind == DuelEventKind.Burn ? blow.Stacks : flags;
                sink.Emit(tick, blow.Kind, striker.Index, blow.Source, after, warded, aux);
            }
        }

        // What the foe's act did to this side this tick: a Vent spends the stacks before new ones land; a new stack
        // refreshes the burn's 3 s (a fresh burn starts its tick clock); a daze; a slow; the rusher's own leap.
        private static void Settle(Side s, int tick, Sink sink)
        {
            int stacksBefore = s.BurnStacks;
            if (s.PendingConsume)
            {
                s.BurnStacks = 0;
                s.BurnLeft = 0;
                s.BurnClock = 0;
                s.PendingConsume = false;
            }
            if (s.PendingStacks > 0)
            {
                if (s.BurnStacks == 0)
                    s.BurnClock = 0;
                s.BurnStacks = Math.Min(DuelRules.BurnMaxStacks, s.BurnStacks + s.PendingStacks);
                s.BurnLeft = DuelRules.BurnTicks;
                s.PendingStacks = 0;
            }
            if (s.BurnStacks != stacksBefore)
                sink.Emit(tick, DuelEventKind.Stacks, s.Index, ArenaEffect.EmberBurn, s.BurnStacks, 0, 0);
            if (s.PendingStun > 0)
            {
                s.StunTicks = Math.Max(s.StunTicks, s.PendingStun);
                sink.Emit(tick, DuelEventKind.Daze, s.Index, ArenaEffect.ShieldRush, s.PendingStun, 0, 0);
                s.PendingStun = 0;
            }
            if (s.PendingSlowTicks > 0)
            {
                s.SlowPermille = s.SlowTicks > 0 ? Math.Max(s.SlowPermille, s.PendingSlowPermille) : s.PendingSlowPermille;
                s.SlowTicks = Math.Max(s.SlowTicks, s.PendingSlowTicks);
                s.PendingSlowTicks = 0;
                s.PendingSlowPermille = 0;
            }
            if (s.PendingMove)
            {
                long wall = (long)DuelRules.Wall * Precision;
                s.X = Math.Max(-wall, Math.Min(wall, s.PendingX));
                s.PendingMove = false;
                sink.Emit(tick, DuelEventKind.Move, s.Index, ArenaEffect.ShieldRush, (int)s.X, (int)s.LastStep, 1);
            }
        }

        private static void Expire(Side s)
        {
            if (s.GuardTicks > 0)
                s.GuardTicks--;
            if (s.StunTicks > 0)
                s.StunTicks--;
            if (s.SlowTicks > 0 && --s.SlowTicks == 0)
                s.SlowPermille = 0;
            if (s.SigilTicks > 0)
                s.SigilTicks--;
            if (s.WardTicks > 0 && --s.WardTicks == 0)
                s.Ward = 0;
        }
    }
}
