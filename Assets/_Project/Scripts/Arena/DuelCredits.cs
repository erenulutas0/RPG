using System;
using System.Collections.Generic;
using System.Globalization;

namespace Cryptforge.Arena
{
    // What one side did in a round, in milli-points of the foe's health it took (dealt) or of its own it kept (armoured,
    // guarded, warded), by source. The duel fills it as blows land; DuelCredits turns it into credits per effect.
    public sealed class DuelTally
    {
        public long Weapon;
        // The extra of random crits (weapon and skills) and of the weapon's rhythm.
        public long Crit;
        public long Rhythm;
        public long Burst;
        public long Rush;
        public long Vent;
        public long Burn;
        // The Sigil's heat on the burn inside the glyph.
        public long Heat;
        public long Armoured;
        public long Guarded;
        public long Warded;
        public long Taken;

        internal void AddSkill(ArenaEffect source, long amount)
        {
            switch (source)
            {
                case ArenaEffect.ForgeBurst:
                    Burst += amount;
                    break;
                case ArenaEffect.ShieldRush:
                    Rush += amount;
                    break;
                case ArenaEffect.CrucibleVent:
                    Vent += amount;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(source));
            }
        }
    }

    // Credits per ArenaEffect, in milli-points: the health swing each effect gave its side. Damage the side dealt is split
    // between its sources and the bonuses that scaled it - the share of a damage bonus in the damage it multiplied, the
    // share of an attack speed bonus in the weapon's strikes, the share of a crit bonus in the crits' extra - so the
    // bonuses' credits are carved out of the sources', and the credits of a side add up to what it dealt and kept. Its own
    // build's bonuses (ArenaFighter.Credited) and the round's boons are credited apart; the common + level of ranked stays
    // with the sources. Health: the extra health a health bonus gave, as far as the side was hurt into it.
    public static class DuelCredits
    {
        public const int Count = 18;

        public static long[] Of(ArenaFighter f, int[] boons, int maxHealth, DuelTally t)
        {
            var c = new long[Count];
            ArenaRules rules = f.Rules;
            int Boon(ArenaStat stat)
            {
                int total = 0;
                foreach (int boon in boons)
                {
                    if (rules.Boons[boon].Stat == stat)
                        total += rules.Boons[boon].Hundredths;
                }
                return total;
            }

            // The damage bonus on everything it scaled: the weapon's strikes and their crits, the striking skills, the burn.
            int boonDamage = Boon(ArenaStat.Damage);
            long whole = 10000 + f.Total(ArenaStat.Damage) + boonDamage;
            long own = f.Credited(ArenaStat.Damage);
            // (Under the research's flat burn the burn takes no share of the bonuses.)
            bool burnScales = rules.BurnScales;
            long scaled = t.Weapon + t.Crit + t.Rhythm + t.Burst + t.Rush + t.Vent + (burnScales ? t.Burn + t.Heat : 0);
            c[(int)ArenaEffect.Damage] = scaled * own / whole;
            c[(int)ArenaEffect.Boon] += scaled * boonDamage / whole;
            long keep = whole - own - boonDamage;
            long weapon = t.Weapon * keep / whole;
            long crit = t.Crit * keep / whole;
            long rhythm = t.Rhythm * keep / whole;
            c[(int)ArenaEffect.ForgeBurst] = t.Burst * keep / whole;
            c[(int)ArenaEffect.ShieldRush] = t.Rush * keep / whole;
            c[(int)ArenaEffect.CrucibleVent] = t.Vent * keep / whole;
            long burn = burnScales ? t.Burn * keep / whole : t.Burn;
            c[(int)ArenaEffect.FlameSigil] = burnScales ? t.Heat * keep / whole : t.Heat;

            // The attack speed bonus on the weapon's strikes (crits included).
            int boonSpeed = Boon(ArenaStat.AttackSpeed);
            long fast = 10000 + f.Total(ArenaStat.AttackSpeed) + boonSpeed;
            long ownSpeed = f.Credited(ArenaStat.AttackSpeed);
            long strikes = weapon + crit + rhythm;
            c[(int)ArenaEffect.AttackSpeed] = strikes * ownSpeed / fast;
            c[(int)ArenaEffect.Boon] += strikes * boonSpeed / fast;
            long keepSpeed = fast - ownSpeed - boonSpeed;
            weapon = weapon * keepSpeed / fast;
            crit = crit * keepSpeed / fast;
            rhythm = rhythm * keepSpeed / fast;

            // The crit bonus on the random crits' extra (the rhythm is the weapon's own).
            int boonCrit = Boon(ArenaStat.CritChance) / 10;
            long chance = DuelRules.BaseCritPermille + (f.Total(ArenaStat.CritChance) + Boon(ArenaStat.CritChance)) / 10;
            long ownCrit = f.Credited(ArenaStat.CritChance) / 10;
            long critOwn = crit * ownCrit / chance;
            long critBoon = crit * boonCrit / chance;
            c[(int)ArenaEffect.Crit] = critOwn;
            c[(int)ArenaEffect.Boon] += critBoon;
            c[(int)ArenaEffect.Crits] = crit - critOwn - critBoon + rhythm;
            c[(int)ArenaEffect.Weapon] = weapon;

            // The burn grows with the whole output bonus (DuelCore's burn tick): its attack speed and crit shares go to those
            // bonuses, then its own burn bonus, the rest Ember Memory's; the glyph's heat is the Sigil's.
            long burnSpeed = burnScales ? burn * ownSpeed / fast : 0;
            long burnSpeedBoon = burnScales ? burn * boonSpeed / fast : 0;
            c[(int)ArenaEffect.AttackSpeed] += burnSpeed;
            c[(int)ArenaEffect.Boon] += burnSpeedBoon;
            burn -= burnSpeed + burnSpeedBoon;
            long critWhole = 10000 + f.Total(ArenaStat.CritChance) + Boon(ArenaStat.CritChance);
            long burnCrit = burnScales ? burn * f.Credited(ArenaStat.CritChance) / critWhole : 0;
            long burnCritBoon = burnScales ? burn * Boon(ArenaStat.CritChance) / critWhole : 0;
            c[(int)ArenaEffect.Crit] += burnCrit;
            c[(int)ArenaEffect.Boon] += burnCritBoon;
            burn -= burnCrit + burnCritBoon;
            long burnWhole = 10000 + f.Total(ArenaStat.BurnDamage);
            c[(int)ArenaEffect.BurnDamage] = burn * f.Credited(ArenaStat.BurnDamage) / burnWhole;
            c[(int)ArenaEffect.EmberBurn] = burn - c[(int)ArenaEffect.BurnDamage];

            // What it kept off itself.
            c[(int)ArenaEffect.Armour] = t.Armoured;
            c[(int)ArenaEffect.IronResolve] = t.Guarded;
            c[(int)ArenaEffect.CinderWard] = t.Warded;
            int boonHealth = Boon(ArenaStat.MaxHealth);
            long healthWhole = 10000 + f.Total(ArenaStat.MaxHealth) + boonHealth;
            long extraOwn = maxHealth * (long)f.Credited(ArenaStat.MaxHealth) / healthWhole;
            long extraBoon = maxHealth * (long)boonHealth / healthWhole;
            c[(int)ArenaEffect.Health] = Math.Min(extraOwn, t.Taken);
            c[(int)ArenaEffect.Boon] += Math.Min(extraBoon, Math.Max(0, t.Taken - c[(int)ArenaEffect.Health]));

            // The open arena's cooldown line: the share of the skills' work its shorter cooldowns bought.
            long cooldown = f.Credited(ArenaStat.AbilityCooldown);
            if (cooldown > 0)
            {
                long skills = c[(int)ArenaEffect.ForgeBurst] + c[(int)ArenaEffect.ShieldRush] + c[(int)ArenaEffect.CrucibleVent] +
                              t.Heat + t.Guarded + t.Warded;
                c[(int)ArenaEffect.AbilityCooldown] = skills * cooldown / 10000;
            }
            return c;
        }
    }

    // One line of the "why": an effect of the winner's, what it gave the winner over the match (Amount, milli-points), and
    // how much more that is than the same effect gave the loser (Net).
    public readonly struct DuelWhy
    {
        public readonly int Side;
        public readonly ArenaEffect Effect;
        public readonly long Amount;
        public readonly long Net;

        public DuelWhy(int side, ArenaEffect effect, long amount, long net)
        {
            Side = side;
            Effect = effect;
            Amount = amount;
            Net = net;
        }
    }

    // The fight's summary (docs/55 section 9, answer 5): the two or three effects that won it. The winner's effects ranked
    // by what each gave it beyond what the same effect gave the loser - so the weapon both swing is named only when it
    // outdid the other's - ties broken by the effect's id; the third is kept only when it is at least a quarter of the
    // first. When nothing of the winner's outdid the loser's (a narrow match), its two largest credits. A draw has none.
    public static class DuelSummary
    {
        public const int MaxLines = 3;

        public static DuelWhy[] Explain(DuelMatch match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            if (match.Winner < 0)
                return new DuelWhy[0];
            long[] mine = match.Winner == 0 ? match.CreditsA : match.CreditsB;
            long[] theirs = match.Winner == 0 ? match.CreditsB : match.CreditsA;
            var ranked = new List<DuelWhy>(DuelCredits.Count);
            for (int i = 0; i < DuelCredits.Count; i++)
            {
                long net = mine[i] - theirs[i];
                if (mine[i] > 0 && net > 0)
                    ranked.Add(new DuelWhy(match.Winner, (ArenaEffect)i, mine[i], net));
            }
            if (ranked.Count == 0)
            {
                for (int i = 0; i < DuelCredits.Count; i++)
                {
                    if (mine[i] > 0)
                        ranked.Add(new DuelWhy(match.Winner, (ArenaEffect)i, mine[i], mine[i] - theirs[i]));
                }
                ranked.Sort((x, y) => x.Amount != y.Amount ? y.Amount.CompareTo(x.Amount) : x.Effect.CompareTo(y.Effect));
                return ranked.GetRange(0, Math.Min(2, ranked.Count)).ToArray();
            }
            ranked.Sort((x, y) => x.Net != y.Net ? y.Net.CompareTo(x.Net) : x.Effect.CompareTo(y.Effect));
            int keep = Math.Min(MaxLines, ranked.Count);
            if (keep == MaxLines && ranked[2].Net * 4 < ranked[0].Net)
                keep = 2;
            return ranked.GetRange(0, keep).ToArray();
        }

        // The line as the result screen shows it, in English (the view localises): "Iron Resolve turned away 1,140".
        public static string Line(DuelWhy why, ArenaFighter winner)
        {
            string amount = (why.Amount / 1000).ToString("N0", CultureInfo.InvariantCulture);
            bool open = winner != null && winner.Mode == ArenaMode.Open;
            string Bonus(string stat) => open ? stat + " from gear" : stat + " lines";
            switch (why.Effect)
            {
                case ArenaEffect.Weapon:
                    return $"{(winner != null ? winner.Weapon.Name : "Weapon")} strikes dealt {amount}";
                case ArenaEffect.Crits:
                    return $"Crits dealt {amount} extra";
                case ArenaEffect.ForgeBurst:
                    return $"Forge Burst dealt {amount}";
                case ArenaEffect.IronResolve:
                    return $"Iron Resolve turned away {amount}";
                case ArenaEffect.ShieldRush:
                    return $"Shield Rush dealt {amount}";
                case ArenaEffect.CrucibleVent:
                    return $"Crucible Vent dealt {amount}";
                case ArenaEffect.FlameSigil:
                    return $"Flame Sigil's heat dealt {amount}";
                case ArenaEffect.CinderWard:
                    return $"Cinder Ward absorbed {amount}";
                case ArenaEffect.EmberBurn:
                    return $"Ember Memory's burn dealt {amount}";
                case ArenaEffect.Damage:
                    return $"{Bonus("Damage")} added {amount}";
                case ArenaEffect.AttackSpeed:
                    return $"{Bonus("Attack speed")} added {amount}";
                case ArenaEffect.Crit:
                    return $"{Bonus("Crit")} added {amount}";
                case ArenaEffect.Health:
                    return $"{Bonus("Health")} held {amount} more";
                case ArenaEffect.Armour:
                    return $"{Bonus("Armour")} turned away {amount}";
                case ArenaEffect.BurnDamage:
                    return $"{Bonus("Burn")} added {amount}";
                case ArenaEffect.MoveSpeed:
                    return Bonus("Move speed");
                case ArenaEffect.AbilityCooldown:
                    return $"Shorter cooldowns added {amount}";
                case ArenaEffect.Boon:
                    return $"Boons added {amount}";
                default:
                    return why.Effect.ToString();
            }
        }

        // The stable id of an effect, as telemetry writes it.
        public static string Id(ArenaEffect effect)
        {
            switch (effect)
            {
                case ArenaEffect.Weapon: return "weapon";
                case ArenaEffect.Crits: return "crits";
                case ArenaEffect.ForgeBurst: return ArenaRules.ForgeBurst;
                case ArenaEffect.IronResolve: return ArenaRules.IronResolve;
                case ArenaEffect.ShieldRush: return ArenaRules.ShieldRush;
                case ArenaEffect.CrucibleVent: return ArenaRules.CrucibleVent;
                case ArenaEffect.FlameSigil: return ArenaRules.FlameSigil;
                case ArenaEffect.CinderWard: return ArenaRules.CinderWard;
                case ArenaEffect.EmberBurn: return "ember_memory";
                case ArenaEffect.Damage: return "damage";
                case ArenaEffect.AttackSpeed: return "attack_speed";
                case ArenaEffect.Crit: return "crit";
                case ArenaEffect.Health: return "health";
                case ArenaEffect.Armour: return "armour";
                case ArenaEffect.BurnDamage: return "burn_damage";
                case ArenaEffect.MoveSpeed: return "move_speed";
                case ArenaEffect.AbilityCooldown: return "ability_cooldown";
                case ArenaEffect.Boon: return "boon";
                default: throw new ArgumentOutOfRangeException(nameof(effect));
            }
        }
    }
}
