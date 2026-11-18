using System;
using System.Globalization;
using Cryptforge.Arena;
using Cryptforge.Localisation;
using Cryptforge.Progression;

namespace Cryptforge.UI
{
    // The arena's words in the game's language (docs/60): every string the arena screen shows goes through here, each key
    // with its English beside it and a row in Strings.txt. Pure (no engine), so the EditMode suite and Tools/CombatChecks
    // check that every summary effect, skill and boon has its words in both languages. The stat names, line values and
    // percentages are the gear's own (GearText), the heroes' and weapons' names their content's (<id>.name).
    public static class ArenaText
    {
        // The map choice's entry button and the screen's title.
        public static string Title => Loc.Text("arena.title", "Arena");

        public static string Close => Loc.Text("arena.close", "Close");
        public static string You => Loc.Text("arena.you", "You");

        // ---- The build ----

        public static string BuildTitle => Loc.Text("arena.build.title", "Your build");
        public static string HeroHeading => Loc.Text("arena.build.hero", "Hero");
        public static string WeaponHeading => Loc.Text("arena.build.weapon", "Weapon");
        public static string PolicyHeading => Loc.Text("arena.build.policy", "Autopilot");
        public static string Locked => Loc.Text("arena.build.locked", "Locked");
        public static string NotOwned => Loc.Text("arena.build.not_owned", "Forge it first");
        public static string Continue => Loc.Text("arena.build.continue", "Continue");
        public static string LinesNote =>
            Loc.Text("arena.build.lines_note", "Both sides fight at the same + level and line values: only the line types are yours.");

        public static string LinesHeading(int picked, int count) =>
            Loc.Format("arena.build.lines", "Efsun lines {picked} / {count}", ("picked", picked), ("count", count));

        public static string Policy(ArenaPolicy policy) => policy == ArenaPolicy.Bold
            ? Loc.Text("arena.policy.bold", "Bold")
            : Loc.Text("arena.policy.careful", "Careful");

        public static string PolicyNote(ArenaPolicy policy) => policy == ArenaPolicy.Bold
            ? Loc.Text("arena.policy.bold_note", "Uses each skill as soon as it can land")
            : Loc.Text("arena.policy.careful_note", "Waits for each skill's moment");

        // A ranked line type and its value, as the gear writes them: "Crit chance +3.25 pts".
        public static string Line(ArenaStat stat, int hundredths) =>
            TryGearStat(stat, out GearStat gear) ? GearText.StatName(gear) + " " + GearText.Value(gear, hundredths) : stat.ToString();

        public static string StatName(ArenaStat stat) =>
            TryGearStat(stat, out GearStat gear) ? GearText.StatName(gear) : stat.ToString();

        private static bool TryGearStat(ArenaStat stat, out GearStat gear) =>
            GearStatIds.TryParse(ArenaGearTable.Id(stat), out gear);

        // ---- Names ----

        // A hero's name from its content (<id>.name), the English given or the arena's own.
        public static string HeroName(string heroId, string english = null)
        {
            if (english == null)
                english = heroId == ArenaRules.Ilya ? "Cinder Arcanist" : ArenaRules.Current.Hero(heroId)?.Name ?? heroId;
            return Loc.Text(ContentKeys.Key(heroId, ContentKeys.Name), english);
        }

        public static string WeaponName(string weaponId) =>
            Loc.Text(ContentKeys.Key(weaponId, ContentKeys.Name), ArenaRules.Current.Weapon(weaponId)?.Name ?? weaponId);

        public static string SkillName(ArenaEffect effect)
        {
            switch (effect)
            {
                case ArenaEffect.ForgeBurst: return Loc.Text("arena.skill.forge_burst", "Forge Burst");
                case ArenaEffect.IronResolve: return Loc.Text("arena.skill.iron_resolve", "Iron Resolve");
                case ArenaEffect.ShieldRush: return Loc.Text("arena.skill.shield_rush", "Shield Rush");
                case ArenaEffect.CrucibleVent: return Loc.Text("arena.skill.crucible_vent", "Crucible Vent");
                case ArenaEffect.FlameSigil: return Loc.Text("arena.skill.flame_sigil", "Flame Sigil");
                case ArenaEffect.CinderWard: return Loc.Text("arena.skill.cinder_ward", "Cinder Ward");
                default: return string.Empty;
            }
        }

        // A rival's name: a generated rival's own (a proper name), or one of the player's past heroes by its fight.
        public static string RivalName(ArenaRivalData rival)
        {
            if (rival == null)
                return string.Empty;
            if (rival.pastSelf && rival.build != null)
                return Loc.Format("arena.rival.past_name", "You, fight {fight}", ("fight", rival.build.fight));
            return rival.name ?? string.Empty;
        }

        // ---- The ladder ----

        public static string TrainingTab => Loc.Text("arena.tab.training", "Training");
        public static string OpenTab => Loc.Text("arena.tab.open", "Open arena");
        public static string EditBuild => Loc.Text("arena.ladder.edit", "Edit build");
        public static string Fight => Loc.Text("arena.fight", "Fight");
        public static string TrainingRival => Loc.Text("arena.rival.training", "Training rival");
        public static string PastHero => Loc.Text("arena.rival.past_self", "Your past hero");
        public static string OpenNote => Loc.Text("arena.ladder.open_note", "Your gear counts in full here. Pick the risk.");
        public static string TrainingNote =>
            Loc.Text("arena.ladder.training_note", "Training rivals hold back a little low on the ladder and less as you climb.");
        public static string GearCounts => Loc.Text("arena.ladder.gear_counts", "Your equipped gear");

        public static string Gear(int power) =>
            Loc.Format("arena.ladder.gear", "Your gear +{power}%", ("power", GearText.Number(power)));

        // The player's rung on the training ladder, 1 the bottom: where its rating stands among the rivals' (700 to 1500).
        public static int RungOf(int rating)
        {
            int span = ArenaLadder.HighRating - ArenaLadder.LowRating;
            int rung = (int)Math.Round((rating - ArenaLadder.LowRating) * (double)(ArenaLadder.Size - 1) / span, MidpointRounding.AwayFromZero);
            return Math.Max(0, Math.Min(ArenaLadder.Size - 1, rung)) + 1;
        }

        public static string Rung(int rung, int rungs) =>
            Loc.Format("arena.ladder.rung", "Rung {rung} of {rungs}", ("rung", rung), ("rungs", rungs));

        public static string Rating(ArenaMode mode, int rating) => mode == ArenaMode.Ranked
            ? Loc.Format("arena.ladder.training_rating", "Training rating {rating}", ("rating", Number(rating)))
            : Loc.Format("arena.ladder.open_rating", "Open rating {rating}", ("rating", Number(rating)));

        public static string Record(int wins, int losses, int draws) =>
            Loc.Format("arena.ladder.record", "Won {wins}  Lost {losses}  Drawn {draws}", ("wins", wins), ("losses", losses),
                ("draws", draws));

        public static string RivalRating(int rating) =>
            Loc.Format("arena.rival.rating", "Rating {rating}", ("rating", Number(rating)));

        public static string Offer(ArenaOfferKind kind, int gapHundredths)
        {
            string gap = GearText.Number(gapHundredths);
            switch (kind)
            {
                case ArenaOfferKind.Below: return Loc.Format("arena.offer.below", "{gap}% below you", ("gap", gap));
                case ArenaOfferKind.Above: return Loc.Format("arena.offer.above", "{gap}% above you", ("gap", gap));
                default: return Loc.Text("arena.offer.level", "Level with you");
            }
        }

        public static string Powers(int mine, int theirs) =>
            Loc.Format("arena.offer.powers", "Your gear +{mine}%  Theirs +{theirs}%", ("mine", GearText.Number(mine)),
                ("theirs", GearText.Number(theirs)));

        public static string Chance(int permille) =>
            Loc.Format("arena.offer.chance", "Chance to win {chance}%", ("chance", DuelNumbers.ChancePercent(permille)));

        // ---- The playback ----

        public static string Round(int round) => Loc.Format("arena.round", "Round {round}", ("round", round));
        public static string RoundTo(string name) => Loc.Format("arena.round_to", "Round to {name}", ("name", name));
        public static string RoundDrawn => Loc.Text("arena.round_drawn", "Round drawn");
        public static string Skip => Loc.Text("arena.skip", "Skip");
        public static string Dazed => Loc.Text("arena.dazed", "Dazed");

        public static string Speed(int speed) => "x" + speed.ToString(CultureInfo.InvariantCulture);

        // The boon a round dealt a side, with its value: "Force: +6% damage".
        public static string Boon(ArenaBoon boon)
        {
            if (boon == null)
                return string.Empty;
            string value = GearText.Number(boon.Hundredths);
            switch (boon.Stat)
            {
                case ArenaStat.Damage: return Loc.Format("arena.boon.force", "Force: +{value}% damage", ("value", value));
                case ArenaStat.AttackSpeed: return Loc.Format("arena.boon.tempo", "Tempo: +{value}% attack speed", ("value", value));
                case ArenaStat.MaxHealth: return Loc.Format("arena.boon.vitality", "Vitality: +{value}% health", ("value", value));
                case ArenaStat.CritChance: return Loc.Format("arena.boon.keen", "Keen: +{value} crit points", ("value", value));
                default: return boon.Name;
            }
        }

        // ---- The result ----

        public static string Victory => Loc.Text("arena.result.victory", "Victory");
        public static string Defeat => Loc.Text("arena.result.defeat", "Defeat");
        public static string Draw => Loc.Text("arena.result.draw", "Draw");
        public static string WatchAgain => Loc.Text("arena.result.again", "Watch again");
        public static string NextFight => Loc.Text("arena.result.next", "Next fight");
        public static string NoWhy => Loc.Text("arena.result.no_why", "Nobody won: the rounds came out even.");

        public static string Outcome(int winner) => winner == 0 ? Victory : winner == 1 ? Defeat : Draw;

        public static string Rounds(int mine, int theirs) =>
            Loc.Format("arena.result.rounds", "Rounds {mine} - {theirs}", ("mine", mine), ("theirs", theirs));

        public static string RatingChange(ArenaMode mode, int before, int after)
        {
            string delta = (after - before).ToString("+0;-0;0", Loc.Numbers);
            return mode == ArenaMode.Ranked
                ? Loc.Format("arena.result.training_rating", "Training rating {before} -> {after} ({delta})",
                    ("before", Number(before)), ("after", Number(after)), ("delta", delta))
                : Loc.Format("arena.result.open_rating", "Open rating {before} -> {after} ({delta})",
                    ("before", Number(before)), ("after", Number(after)), ("delta", delta));
        }

        public static string WhyHeading(string winner) => Loc.Format("arena.result.why", "Why {name} won", ("name", winner));
        public static string WhyYouWon => Loc.Text("arena.result.why_you", "Why you won");
        public static string Against(string rival) => Loc.Format("arena.result.against", "Against {name}", ("name", rival));

        // One line of the fight's summary (DuelSummary, docs/55 section 10.2) by its effect: the localised DuelSummary.Line.
        // weaponId names the winner's weapon; open says the bonuses came from gear rather than ranked line types.
        public static string Why(DuelWhy why, string weaponId, bool open)
        {
            string amount = (why.Amount / 1000).ToString("#,0", Loc.Numbers);
            switch (why.Effect)
            {
                case ArenaEffect.Weapon:
                    return Loc.Format("arena.why.weapon", "{weapon} strikes dealt {amount}", ("weapon", WeaponName(weaponId)),
                        ("amount", amount));
                case ArenaEffect.Crits:
                    return Loc.Format("arena.why.crits", "Crits dealt {amount} extra", ("amount", amount));
                case ArenaEffect.ForgeBurst:
                    return Loc.Format("arena.why.forge_burst", "Forge Burst dealt {amount}", ("amount", amount));
                case ArenaEffect.IronResolve:
                    return Loc.Format("arena.why.iron_resolve", "Iron Resolve turned away {amount}", ("amount", amount));
                case ArenaEffect.ShieldRush:
                    return Loc.Format("arena.why.shield_rush", "Shield Rush dealt {amount}", ("amount", amount));
                case ArenaEffect.CrucibleVent:
                    return Loc.Format("arena.why.crucible_vent", "Crucible Vent dealt {amount}", ("amount", amount));
                case ArenaEffect.FlameSigil:
                    return Loc.Format("arena.why.flame_sigil", "Flame Sigil's heat dealt {amount}", ("amount", amount));
                case ArenaEffect.CinderWard:
                    return Loc.Format("arena.why.cinder_ward", "Cinder Ward absorbed {amount}", ("amount", amount));
                case ArenaEffect.EmberBurn:
                    return Loc.Format("arena.why.ember_memory", "Ember Memory's burn dealt {amount}", ("amount", amount));
                case ArenaEffect.Damage:
                    return open
                        ? Loc.Format("arena.why.damage_gear", "Damage from gear added {amount}", ("amount", amount))
                        : Loc.Format("arena.why.damage_lines", "Damage lines added {amount}", ("amount", amount));
                case ArenaEffect.AttackSpeed:
                    return open
                        ? Loc.Format("arena.why.attack_speed_gear", "Attack speed from gear added {amount}", ("amount", amount))
                        : Loc.Format("arena.why.attack_speed_lines", "Attack speed lines added {amount}", ("amount", amount));
                case ArenaEffect.Crit:
                    return open
                        ? Loc.Format("arena.why.crit_gear", "Crit from gear added {amount}", ("amount", amount))
                        : Loc.Format("arena.why.crit_lines", "Crit lines added {amount}", ("amount", amount));
                case ArenaEffect.Health:
                    return open
                        ? Loc.Format("arena.why.health_gear", "Health from gear held {amount} more", ("amount", amount))
                        : Loc.Format("arena.why.health_lines", "Health lines held {amount} more", ("amount", amount));
                case ArenaEffect.Armour:
                    return open
                        ? Loc.Format("arena.why.armour_gear", "Armour from gear turned away {amount}", ("amount", amount))
                        : Loc.Format("arena.why.armour_lines", "Armour lines turned away {amount}", ("amount", amount));
                case ArenaEffect.BurnDamage:
                    return open
                        ? Loc.Format("arena.why.burn_gear", "Burn from gear added {amount}", ("amount", amount))
                        : Loc.Format("arena.why.burn_lines", "Burn lines added {amount}", ("amount", amount));
                case ArenaEffect.MoveSpeed:
                    return open
                        ? Loc.Text("arena.why.move_speed_gear", "Move speed from gear")
                        : Loc.Text("arena.why.move_speed_lines", "Move speed lines");
                case ArenaEffect.AbilityCooldown:
                    return Loc.Format("arena.why.ability_cooldown", "Shorter cooldowns added {amount}", ("amount", amount));
                case ArenaEffect.Boon:
                    return Loc.Format("arena.why.boon", "Boons added {amount}", ("amount", amount));
                default:
                    throw new ArgumentOutOfRangeException(nameof(why));
            }
        }

        // A whole number in the language's format: "1,032" in English, "1.032" in Turkish.
        public static string Number(int value) => value.ToString("#,0", Loc.Numbers);
    }
}
