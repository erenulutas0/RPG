using System;
using Cryptforge.Progression;

namespace Cryptforge.Tests
{
    // docs/55's prototype (2026-11-17): a deterministic 1v1 hero duel, both sides played by a small autopilot, in integer
    // arithmetic only. Nothing in the game uses it. It exists to settle four questions of the PvP architecture with numbers
    // (ArenaDuelResearch): does an integer duel replay bit for bit, what does a re-simulation cost a server, how far does a
    // gear-power gap decide a build-against-build fight, and how many stored results does a small balance patch move.
    //
    // Integers, not floats, on purpose: RunRandom's note (docs/21) already paid once for two float copies that disagreed,
    // and a server re-simulating on x64 .NET what a phone played under IL2CPP on ARM64 is the same risk across machines
    // (Unity does not promise float determinism across architectures; Mono and IL2CPP have differed). Health and damage are
    // milli-points, distances milli-units, time 60 ticks a second, chances per mille. Randomness is RunRandom's, derived
    // per (seed, purpose, index), never System.Random.
    internal enum DuelSkillKind
    {
        // Damage now: Power milli-points (scaled by the fighter's damage), when the foe is within reach plus Radius.
        Strike,
        // A shield of Power per mille of maximum health for 3 s, used below 70 % health.
        Shield,
        // A heal of Power per mille of maximum health, used below 50 % health.
        Mend,
        // Attack speed +Power per mille for 3 s, used when the foe is in reach.
        Haste
    }

    internal sealed class DuelSkill
    {
        public string Name;
        public DuelSkillKind Kind;
        public int Power;
        public int CooldownTicks;
        public int Radius;

        public DuelSkill(string name, DuelSkillKind kind, int power, int cooldownSeconds, int radius = 0)
        {
            Name = name;
            Kind = kind;
            Power = power;
            CooldownTicks = cooldownSeconds * ArenaDuel.TicksPerSecond;
            Radius = radius;
        }
    }

    // A build snapshot as the duel reads it: what a player uploads, a hero, its weapon and the gear's sums. Everything is
    // an integer so the snapshot hashes and stores as it is.
    internal sealed class DuelFighter
    {
        public string Name;
        public int MaxHealth;
        public int Damage;
        public int IntervalTicks;
        public int Reach;
        public int SpeedPerTick;
        public bool Kites;
        // A run's build carries crit from its cards; the duel's heroes start with 10 % so two equal builds are not decided
        // by who swings first (ArenaDuel.BaseCrit).
        public int CritPermille = ArenaDuel.BaseCrit;
        public int CritPercent = 200;
        public int ArmourPermille;
        public int AttackSpeedPermille;
        // The weapon damage the skills' Power was written against (the hero's own, ungeared), so gear's damage scales
        // skills by the same share as strikes.
        public int SkillBase;
        public DuelSkill[] Skills = Array.Empty<DuelSkill>();

        public DuelFighter Copy()
        {
            var copy = (DuelFighter)MemberwiseClone();
            copy.Skills = (DuelSkill[])Skills.Clone();
            return copy;
        }

        // The Vanguard at the run's start as the foundry plays it (Hero_Vanguard: 100 health; the Sword on its category
        // base, 18 every 0.8 s at reach 2.1; Forge Burst 20 every 8 s, radius 2.5).
        public static DuelFighter Vanguard() => new DuelFighter
        {
            Name = "Vanguard", MaxHealth = 100_000, Damage = 18_000, SkillBase = 18_000, IntervalTicks = 48, Reach = 2_100, SpeedPerTick = 50,
            Skills = new[] { new DuelSkill("Forge Burst", DuelSkillKind.Strike, 20_000, 8, 400) }
        };

        // Ilya (Hero_CinderArcanist: 90 health; the Staff on its category base, 24 every 1.1 s at reach 2.4; Crucible Vent 8
        // every 14 s, radius 3). The staff keeps its reach (Kites).
        public static DuelFighter Ilya() => new DuelFighter
        {
            Name = "Ilya", MaxHealth = 90_000, Damage = 24_000, SkillBase = 24_000, IntervalTicks = 66, Reach = 2_400, SpeedPerTick = 50,
            Kites = true,
            Skills = new[] { new DuelSkill("Crucible Vent", DuelSkillKind.Strike, 8_000, 14, 600) }
        };
    }

    // The gear's sums over the four slots (docs/54 section 7.2), per mille and crit in tenths of a point.
    internal readonly struct DuelGear
    {
        public readonly string Name;
        public readonly int Damage;
        public readonly int AttackSpeed;
        public readonly int CritPermille;
        public readonly int Health;
        public readonly int Armour;

        public DuelGear(string name, int damage, int attackSpeed, int critPermille, int health, int armour)
        {
            Name = name;
            Damage = damage;
            AttackSpeed = attackSpeed;
            CritPermille = critPermille;
            Health = health;
            Armour = armour;
        }

        public static readonly DuelGear None = new DuelGear("none", 0, 0, 0, 0, 0);

        // docs/54 7.1's index, DPS x EHP - 1, with the crit multiplier 2 (reported, never used by the duel).
        public double Power =>
            (1 + Damage / 1000.0) * (1 + AttackSpeed / 1000.0) * (1 + CritPermille / 1000.0) *
            ((1 + Health / 1000.0) / (1 - Armour / 1000.0)) - 1;

        public DuelFighter On(DuelFighter fighter)
        {
            DuelFighter geared = fighter.Copy();
            geared.Damage = (int)((long)fighter.Damage * (1000 + Damage) / 1000);
            geared.AttackSpeedPermille = fighter.AttackSpeedPermille + AttackSpeed;
            geared.CritPermille = fighter.CritPermille + CritPermille;
            geared.MaxHealth = (int)((long)fighter.MaxHealth * (1000 + Health) / 1000);
            geared.ArmourPermille = fighter.ArmourPermille + Armour;
            return geared;
        }
    }

    internal readonly struct DuelResult
    {
        // 0 the first fighter, 1 the second, -1 a draw.
        public readonly int Winner;
        public readonly int Ticks;
        public readonly int HealthLeftA;
        public readonly int HealthLeftB;
        public readonly ulong Hash;
        public readonly int Events;

        public DuelResult(int winner, int ticks, int healthLeftA, int healthLeftB, ulong hash, int events)
        {
            Winner = winner;
            Ticks = ticks;
            HealthLeftA = healthLeftA;
            HealthLeftB = healthLeftB;
            Hash = hash;
            Events = events;
        }
    }

    internal static class ArenaDuel
    {
        public const int TicksPerSecond = 60;
        // The arena's own health scale: a run's hero dies to a level-1 foe's sword in 5 s, which no duel should be. A
        // multiplier on both sides is how PvP modes stretch a fight (the knob the arena owns, not the hero).
        public const int HealthScale = 8;
        public const int LimitTicks = 90 * TicksPerSecond;
        // A straight lane, the walls at +-6 units, the fighters entering at +-4.
        public const int Wall = 6_000;
        public const int Entry = 4_000;
        public const int KiteSlack = 400;
        public const int EffectTicks = 3 * TicksPerSecond;
        public const int BaseCrit = 100;

        // RunRandom purposes of the arena, apart from the run's (RunRandom's constants are below 100).
        public const int Crits = 101;
        public const int Openings = 102;
        public const int Rounds = 103;
        public const int Draws = 104;

        // A pool of same-quality skills a round can deal both sides (Archero 2's arena deals three random skills of one
        // quality to each side every round): docs/54 section 14's second and third active skills, drawn instead of chosen.
        public static readonly DuelSkill[] Pool =
        {
            new DuelSkill("Cleaving Arc", DuelSkillKind.Strike, 30_000, 9, 300),
            new DuelSkill("Quick Jab", DuelSkillKind.Strike, 14_000, 4, 200),
            new DuelSkill("Bulwark", DuelSkillKind.Shield, 180, 12),
            new DuelSkill("Second Wind", DuelSkillKind.Mend, 200, 16),
            new DuelSkill("Bellows", DuelSkillKind.Haste, 400, 11)
        };

        private sealed class Side
        {
            public DuelFighter F;
            public int X;
            public int Health;
            public int Shield;
            public int ShieldTicks;
            public int HasteTicks;
            public int HastePermille;
            public long Progress;
            public int[] Cooldowns;
            public RunRandom Dice;
        }

        public static DuelResult Fight(DuelFighter a, DuelFighter b, int seed)
        {
            Side sa = Enter(a, -Entry, seed, 0);
            Side sb = Enter(b, Entry, seed, 1);
            ulong hash = 14695981039346656037UL;
            int events = 0;
            int tick = 0;
            for (; tick < LimitTicks; tick++)
            {
                // Both decide on the tick's opening state, then both blows land together: no first-mover edge.
                Move(sa, sb);
                Move(sb, sa);
                int toB = Act(sa, sb, tick, 0, ref hash, ref events);
                int toA = Act(sb, sa, tick, 1, ref hash, ref events);
                Land(sa, toA, tick, 0, ref hash, ref events);
                Land(sb, toB, tick, 1, ref hash, ref events);
                Expire(sa);
                Expire(sb);
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
                long shareA = (long)sa.Health * sb.F.MaxHealth;
                long shareB = (long)sb.Health * sa.F.MaxHealth;
                winner = shareA > shareB ? 0 : shareB > shareA ? 1 : -1;
            }
            hash = Mix(hash, tick, 9, winner, 0);
            return new DuelResult(winner, tick, Math.Max(0, sa.Health), Math.Max(0, sb.Health), hash, events);
        }

        // Best of three: each round deals both sides `drawn` skills from the pool (seeded per round and side) beside the
        // hero's own ability. Up to five rounds when rounds draw; still level, a draw.
        public static int BestOfThree(DuelFighter a, DuelFighter b, int seed, int drawn, out ulong hash)
        {
            int winsA = 0, winsB = 0;
            hash = 14695981039346656037UL;
            for (int round = 0; round < 5 && winsA < 2 && winsB < 2; round++)
            {
                int roundSeed = (int)RunRandom.Stream(seed, Rounds, round).Next();
                DuelResult r = Fight(Deal(a, roundSeed, 0, drawn), Deal(b, roundSeed, 1, drawn), roundSeed);
                hash = Mix(hash, round, 10, r.Winner, (long)r.Hash);
                if (r.Winner == 0)
                    winsA++;
                else if (r.Winner == 1)
                    winsB++;
            }
            return winsA == 2 ? 0 : winsB == 2 ? 1 : -1;
        }

        public static DuelFighter Deal(DuelFighter fighter, int seed, int side, int drawn)
        {
            if (drawn <= 0)
                return fighter;
            DuelFighter dealt = fighter.Copy();
            var skills = new DuelSkill[fighter.Skills.Length + drawn];
            Array.Copy(fighter.Skills, skills, fighter.Skills.Length);
            RunRandom dice = RunRandom.Stream(seed, Draws, side);
            bool[] taken = new bool[Pool.Length];
            for (int i = 0; i < drawn; i++)
            {
                int pick = dice.NextBelow(Pool.Length);
                while (taken[pick])
                    pick = (pick + 1) % Pool.Length;
                taken[pick] = true;
                skills[fighter.Skills.Length + i] = Pool[pick];
            }
            dealt.Skills = skills;
            return dealt;
        }

        private static Side Enter(DuelFighter f, int x, int seed, int side)
        {
            var s = new Side
            {
                F = f,
                X = x,
                Health = f.MaxHealth * HealthScale,
                Cooldowns = new int[f.Skills.Length],
                Dice = RunRandom.Stream(seed, Crits, side)
            };
            // An opening offset so two equal builds do not swing in lockstep: up to one interval, seeded per side.
            RunRandom opening = RunRandom.Stream(seed, Openings, side);
            s.Progress = (long)opening.NextBelow(f.IntervalTicks) * 1000;
            for (int i = 0; i < s.Cooldowns.Length; i++)
                s.Cooldowns[i] = f.Skills[i].CooldownTicks / 2 + opening.NextBelow(TicksPerSecond);
            return s;
        }

        private static int Distance(Side s, Side foe) => Math.Abs(s.X - foe.X);

        private static void Move(Side s, Side foe)
        {
            int distance = Distance(s, foe);
            int toward = foe.X > s.X ? 1 : -1;
            int step = 0;
            if (distance > s.F.Reach)
                step = toward * Math.Min(s.F.SpeedPerTick, distance - s.F.Reach);
            else if (s.F.Kites && distance < s.F.Reach - KiteSlack)
                step = -toward * s.F.SpeedPerTick;
            s.X = Math.Max(-Wall, Math.Min(Wall, s.X + step));
        }

        // What this side deals the foe this tick, in milli-points before the foe's armour and shield.
        private static int Act(Side s, Side foe, int tick, int side, ref ulong hash, ref int events)
        {
            int dealt = 0;
            int distance = Distance(s, foe);
            long need = (long)s.F.IntervalTicks * 1000;
            s.Progress += 1000 + s.F.AttackSpeedPermille + (s.HasteTicks > 0 ? s.HastePermille : 0);
            if (s.Progress >= need)
            {
                if (distance <= s.F.Reach)
                {
                    s.Progress -= need;
                    dealt += Roll(s, s.F.Damage);
                    hash = Mix(hash, tick, 1, side, dealt);
                    events++;
                }
                else
                    s.Progress = need;
            }

            int maxHealth = s.F.MaxHealth * HealthScale;
            for (int i = 0; i < s.Cooldowns.Length; i++)
            {
                if (s.Cooldowns[i] > 0)
                {
                    s.Cooldowns[i]--;
                    continue;
                }
                DuelSkill skill = s.F.Skills[i];
                bool used = false;
                switch (skill.Kind)
                {
                    case DuelSkillKind.Strike:
                        if (distance <= s.F.Reach + skill.Radius)
                        {
                            int scaled = (int)((long)skill.Power * s.F.Damage / Math.Max(1, s.F.SkillBase));
                            int strike = Roll(s, scaled);
                            dealt += strike;
                            used = true;
                        }
                        break;
                    case DuelSkillKind.Shield:
                        if ((long)s.Health * 10 < (long)maxHealth * 7 && distance <= foe.F.Reach + 500)
                        {
                            s.Shield = (int)((long)maxHealth * skill.Power / 1000);
                            s.ShieldTicks = EffectTicks;
                            used = true;
                        }
                        break;
                    case DuelSkillKind.Mend:
                        if ((long)s.Health * 2 < maxHealth)
                        {
                            s.Health = Math.Min(maxHealth, s.Health + (int)((long)maxHealth * skill.Power / 1000));
                            used = true;
                        }
                        break;
                    case DuelSkillKind.Haste:
                        if (distance <= s.F.Reach)
                        {
                            s.HasteTicks = EffectTicks;
                            s.HastePermille = skill.Power;
                            used = true;
                        }
                        break;
                }
                if (used)
                {
                    s.Cooldowns[i] = skill.CooldownTicks;
                    hash = Mix(hash, tick, 2 + (int)skill.Kind, side, skill.Power);
                    events++;
                }
            }
            return dealt;
        }

        private static int Roll(Side s, int damage)
        {
            if (s.Dice.NextBelow(1000) < s.F.CritPermille)
                return (int)((long)damage * s.F.CritPercent / 100);
            return damage;
        }

        private static void Land(Side s, int damage, int tick, int side, ref ulong hash, ref int events)
        {
            if (damage <= 0)
                return;
            int taken = (int)((long)damage * (1000 - Math.Min(900, s.F.ArmourPermille)) / 1000);
            if (s.Shield > 0)
            {
                int absorbed = Math.Min(s.Shield, taken);
                s.Shield -= absorbed;
                taken -= absorbed;
            }
            s.Health -= taken;
            hash = Mix(hash, tick, 8, side, taken);
            events++;
        }

        private static void Expire(Side s)
        {
            if (s.ShieldTicks > 0 && --s.ShieldTicks == 0)
                s.Shield = 0;
            if (s.HasteTicks > 0)
                s.HasteTicks--;
        }

        // FNV-1a over the event's integers: the replay's fingerprint.
        private static ulong Mix(ulong hash, int tick, int kind, int side, long value)
        {
            unchecked
            {
                hash = (hash ^ (uint)tick) * 1099511628211UL;
                hash = (hash ^ (uint)kind) * 1099511628211UL;
                hash = (hash ^ (uint)side) * 1099511628211UL;
                hash = (hash ^ (ulong)value) * 1099511628211UL;
                return hash;
            }
        }
    }
}
