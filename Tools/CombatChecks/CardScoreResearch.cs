using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cryptforge.Core;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The careful pilot's cards (docs/41, 2026-11-10 night): with a human's hands (PilotRealism.Typical) the first-card
    // cohort cleared more of floor 1 than the careful one, whose CardScore was tuned for a pilot that is never hit. This
    // lays the cohorts' cards side by side over a seed batch - what kind of card each took, which, at what tier and when,
    // the health and armour it walked to the Warden with - beside where and to what each lost health and died, and plays
    // the research's candidate pickers (FoundryLevers.Picker) on the same seeds. "careful" is the cohort's own score (since
    // 2026-11-10 the research's syn-dt31), "before" main 18715c94's. Explicit, so a plain `dotnet test` skips it:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~CardScoreResearch"
    //
    // Environment, all optional: FOUNDRY_SEEDS (200), FOUNDRY_FIRST_SEED (1), FOUNDRY_PARALLEL (the processors),
    // FOUNDRY_OUT (the repository's TestResults), CARDS_REALISM (comma separated: frame-perfect, typical, slow; both of the
    // first two by default), CARDS_PICKERS (comma separated names of Pickers below; careful, first-card and before by default),
    // CARDS_FLOORS (1). Writes cards-research.md.
    [Explicit("The careful pilot's cards research: run it by name.")]
    public sealed class CardScoreResearch
    {
        private int _parallel;
        private int[] _seeds;

        // A picker in the research: a name, the cohort that plays (its footwork; the picker replaces its card score), and
        // the picker itself (null: the cohort's own).
        private sealed class Picker
        {
            public string Name;
            public AutopilotCohort Cohort;
            public Func<DescentSimulation.FoundryPick, UpgradeService, int> Pick;
        }

        private static IEnumerable<Picker> Pickers()
        {
            yield return new Picker { Name = "careful", Cohort = AutopilotCohort.Careful };
            yield return new Picker { Name = "first-card", Cohort = AutopilotCohort.FirstCard };
            // main 18715c94's careful score (every line at 50) through the research's picker: its batch run for run as the
            // cohort played it before 2026-11-10 (the lever's check).
            yield return new Picker { Name = "before", Cohort = AutopilotCohort.Careful, Pick = Scored(CarefulScore) };
            // The first-card picker less one of its habits: a general card in the first slot passed over for the careful
            // score's best of the rest; a weapon line taken whenever offered; health first when low.
            yield return new Picker { Name = "fc-no-general", Cohort = AutopilotCohort.Careful, Pick = (p, u) =>
                p.Offer.Choices[0].Category != OfferCategory.General || p.Offer.Choices.Count < 2 ? 0 : 1 + Scored(CarefulScore)(Without(p, 0), u) };
            yield return new Picker { Name = "fc-lines", Cohort = AutopilotCohort.Careful, Pick = (p, u) =>
                FirstWhere(p, c => c.Category == OfferCategory.WeaponUpgrade) };
            yield return new Picker { Name = "fc-low-health", Cohort = AutopilotCohort.Careful, Pick = (p, u) =>
                p.Low ? FirstWhere(p, c => c.Stat == UpgradeStat.MaxHealth) : 0 };
            // Lines under tome levels but a weapon built toward its evolution (docs/42: the weapon at level 5 and its tome at
            // level 2): its lines while it is below level 5 with its tome held (a), its tome's levels to 2 too (b), with every
            // tome slot (c); its lines below level 5 whatever the tome (d).
            yield return new Picker { Name = "syn-a", Cohort = AutopilotCohort.Careful, Pick = Scored((p, u, s) => Synergy(p, u, s, false, false, 2)) };
            yield return new Picker { Name = "syn-b", Cohort = AutopilotCohort.Careful, Pick = Scored((p, u, s) => Synergy(p, u, s, true, false, 2)) };
            yield return new Picker { Name = "syn-c", Cohort = AutopilotCohort.Careful, Pick = Scored((p, u, s) => Synergy(p, u, s, true, false, 3)) };
            yield return new Picker { Name = "syn-d", Cohort = AutopilotCohort.Careful, Pick = Scored((p, u, s) => Synergy(p, u, s, false, true, 2)) };
            yield return new Picker { Name = "syn-e", Cohort = AutopilotCohort.Careful, Pick = Scored((p, u, s) => Synergy(p, u, s, true, true, 2)) };
            yield return new Picker { Name = "syn-dt", Cohort = AutopilotCohort.Careful, Pick = Scored((p, u, s) => SynergyDefence(p, u, s, 50, .5f)) };
            yield return new Picker { Name = "syn-dt-low70", Cohort = AutopilotCohort.Careful, Pick = Scored((p, u, s) => SynergyDefence(p, u, s, 50, .7f)) };
            // syn-dt31 is the score AutopilotCohort.CardScore ships (2026-11-10): the same floor run for run as "careful".
            yield return new Picker { Name = "syn-dt31", Cohort = AutopilotCohort.Careful, Pick = Scored((p, u, s) => SynergyDefence(p, u, s, 31, .5f)) };
            yield return new Picker { Name = "lines-under-tomes", Cohort = AutopilotCohort.Careful, Pick = Scored(LinesUnderTomes) };
            yield return new Picker { Name = "syn31", Cohort = AutopilotCohort.Careful, Pick = Scored((p, u, s) => SynergyDefence(p, u, s, 31, .5f, 0)) };
            foreach ((string name, Knobs knobs) in Candidates())
            {
                Knobs k = knobs;
                yield return new Picker { Name = name, Cohort = AutopilotCohort.Careful, Pick = Scored((p, u, s) => Tuned(k, p, u, s)) };
            }
        }

        // The candidates: the careful score with one or more of its weights moved (Knobs' defaults are the careful score).
        private static IEnumerable<(string, Knobs)> Candidates()
        {
            yield return ("knobs", new Knobs());
            yield return ("armour55", new Knobs { Armor = 55 });
            yield return ("health55", new Knobs { HealthGeneral = 55 });
            yield return ("speed55", new Knobs { Speed = 55 });
            yield return ("defence55", new Knobs { Armor = 55, HealthGeneral = 55 });
            yield return ("hero55", new Knobs { Armor = 55, HealthGeneral = 55, Speed = 55 });
            yield return ("defence-tiers", new Knobs { Armor = 25, HealthGeneral = 25, DefenceTierStep = 10 });
            yield return ("all-tiers", new Knobs { TierStep = 3, DefenceTierStep = 10 });
            yield return ("vitality", new Knobs { VitalityNew = 44, VitalityLevel = 52 });
            yield return ("vitality-new", new Knobs { VitalityNew = 44 });
            yield return ("no-evolution", new Knobs { Evolution = 5 });
            yield return ("low70", new Knobs { LowShare = .7f });
            yield return ("tomes3", new Knobs { TomesWanted = 3 });
            yield return ("tome-levels52", new Knobs { TomeLevel = 52, VitalityLevel = 52 });
            yield return ("defence55+vitality", new Knobs { Armor = 55, HealthGeneral = 55, VitalityNew = 44, VitalityLevel = 52 });
            yield return ("defence55+no-evolution", new Knobs { Armor = 55, HealthGeneral = 55, Evolution = 5 });
            // Ablations by card: the Daggers' lines, the Sword's range lines, the Daggers themselves.
            var daggerLines = new Dictionary<string, float> { ["line_daggers_attack_speed"] = 28, ["line_daggers_crit"] = 28, ["line_daggers_damage"] = 28 };
            yield return ("no-dagger-lines", new Knobs { ById = daggerLines });
            yield return ("no-sword-range", new Knobs { ById = new Dictionary<string, float> { ["line_sword_range"] = 28 } });
            yield return ("no-daggers", new Knobs { ById = new Dictionary<string, float> { ["weapon_daggers"] = 10 } });
            yield return ("no-staff-lines", new Knobs { ById = new Dictionary<string, float> { ["line_staff_damage"] = 28, ["line_staff_area"] = 28, ["line_staff_attack_speed"] = 28 } });
            yield return ("no-sword-lines", new Knobs { ById = new Dictionary<string, float> { ["line_sword_damage"] = 28, ["line_sword_area"] = 28, ["line_sword_range"] = 28 } });
            yield return ("lines29", new Knobs { Line = 29, Evolution = 29 });
            yield return ("lines35", new Knobs { Line = 35, Evolution = 35 });
            // Lines under tome levels, with one more weight moved.
            yield return ("l29-evolution50", new Knobs { Line = 29, Evolution = 50 });
            yield return ("l29-no-evolution", new Knobs { Line = 29, Evolution = 5 });
            yield return ("l29-defence-tiers", new Knobs { Line = 29, Evolution = 29, DefenceTierStep = 10 });
            yield return ("l29-vitality-new", new Knobs { Line = 29, Evolution = 29, VitalityNew = 44 });
            yield return ("l29-vitality", new Knobs { Line = 29, Evolution = 29, VitalityNew = 44, VitalityLevel = 32 });
            yield return ("l29-low70", new Knobs { Line = 29, Evolution = 29, LowShare = .7f });
            yield return ("l29-tomes3", new Knobs { Line = 29, Evolution = 29, TomesWanted = 3, ExtraTome = 15 });
            yield return ("l29-all-tiers", new Knobs { Line = 29, Evolution = 29, TierStep = 3 });
            yield return ("l29-armour", new Knobs { Line = 29, Evolution = 29, Armor = 29.5f });
            yield return ("l29-health", new Knobs { Line = 29, Evolution = 29, HealthGeneral = 29.5f });
            yield return ("l22", new Knobs { Line = 22, Evolution = 22 });
            yield return ("l29-weapon31", new Knobs { Line = 29, Evolution = 29, NewWeapon = 31, NewTome = 40 });
        }

        // The careful score's weights, each a knob (defaults: AutopilotCohort.CardScore as of main 18715c94).
        private sealed class Knobs
        {
            public float Line = 50, Evolution = 50, NewWeapon = 45, NewTome = 40, ExtraTome = 15, TomeLevel = 30;
            public float HealthGeneral = 25, Armor = 20, Speed = 20, Other = 20, LowHealth = 100;
            public float VitalityNew = 40, VitalityLevel = 30;
            // Added per tier above common (new weapons are always common): to every card, and again to the general health
            // and armour cards, whose tiers scale 1 / 2 / 4 / 5.3 where a weapon line's scale 1 / 1.25 / 1.5 / 2.
            public float TierStep, DefenceTierStep;
            public float LowShare = .5f;
            public int TomesWanted = 2;
            // A score by card id in place of its kind's, for the ablations.
            public Dictionary<string, float> ById;
        }

        private static float Tuned(Knobs k, DescentSimulation.FoundryPick pick, UpgradeService upgrades, int slot)
        {
            UpgradeOption card = pick.Offer.Choices[slot];
            int tier = (int)pick.Offer.RarityAt(slot);
            bool low = pick.Health < pick.Maximum * k.LowShare;
            bool vitality = card.Id == "tome_vitality";
            bool defence = card.Category == OfferCategory.General && (card.Stat == UpgradeStat.MaxHealth || card.Stat == UpgradeStat.Armor);
            float score;
            if (low && card.Stat == UpgradeStat.MaxHealth)
                score = k.LowHealth;
            else
                switch (card.Category)
                {
                    case OfferCategory.WeaponUpgrade: score = card.IsEvolution ? k.Evolution : k.Line; break;
                    case OfferCategory.NewWeapon: score = pick.WeaponsHeld < Cryptforge.Combat.WeaponLoadout.MaxSlots ? k.NewWeapon : 10; break;
                    case OfferCategory.NewTome: score = pick.TomesHeld < k.TomesWanted ? (vitality ? k.VitalityNew : k.NewTome) : k.ExtraTome; break;
                    case OfferCategory.TomeUpgrade: score = vitality ? k.VitalityLevel : k.TomeLevel; break;
                    default:
                        score = card.Stat == UpgradeStat.MaxHealth ? k.HealthGeneral : card.Stat == UpgradeStat.Armor ? k.Armor
                            : card.Stat == UpgradeStat.MoveSpeed ? k.Speed : k.Other;
                        break;
                }
            if (k.ById != null && k.ById.TryGetValue(card.Id, out float byId) && !(low && card.Stat == UpgradeStat.MaxHealth))
                score = byId;
            return score + tier * (k.TierStep + (defence ? k.DefenceTierStep : 0f));
        }

        // The slot of the best score, the first of equals, as AutopilotCohort.Pick takes it.
        private static Func<DescentSimulation.FoundryPick, UpgradeService, int> Scored(
            Func<DescentSimulation.FoundryPick, UpgradeService, int, float> score) =>
            (pick, upgrades) =>
            {
                int best = 0;
                float bestScore = float.MinValue;
                for (int i = 0; i < pick.Offer.Choices.Count; i++)
                {
                    float s = score(pick, upgrades, i);
                    if (s <= bestScore)
                        continue;
                    best = i;
                    bestScore = s;
                }
                return best;
            };

        // Lines under tome levels (29 against 30), but a line of a weapon on its way to its evolution at the old 50: below
        // EvolutionRule.WeaponEvolutionLevel, not evolved, and (unless anyTome) its evolution's tome held; with tomeToo,
        // that tome's levels below WeaponEvolutionTomeLevel at 50 as well while a held weapon waits on it.
        private static float Synergy(DescentSimulation.FoundryPick pick, UpgradeService upgrades, int slot, bool tomeToo, bool anyTome,
            int tomesWanted)
        {
            UpgradeOption card = pick.Offer.Choices[slot];
            if (pick.Low && card.Stat == UpgradeStat.MaxHealth)
                return 100;
            switch (card.Category)
            {
                case OfferCategory.NewWeapon: return pick.WeaponsHeld < Cryptforge.Combat.WeaponLoadout.MaxSlots ? 45 : 10;
                case OfferCategory.NewTome: return pick.TomesHeld < tomesWanted ? 40 : 15;
                case OfferCategory.TomeUpgrade:
                    if (tomeToo && upgrades.TomeLevel(card.Id) < EvolutionRule.WeaponEvolutionTomeLevel)
                        foreach (WeaponCard weapon in upgrades.WeaponCards)
                            if (weapon.EvolvesWithTomeId == card.Id && upgrades.Loadout.Owns(weapon.Id) && !upgrades.HasEvolved(weapon.Id))
                                return 50;
                    return 30;
                case OfferCategory.WeaponUpgrade:
                    if (card.IsEvolution)
                        return 50;
                    foreach (WeaponCard weapon in upgrades.WeaponCards)
                    {
                        if (!weapon.Lines.Any(line => line.Id == card.Id))
                            continue;
                        int at = upgrades.Loadout.IndexOf(weapon.Id);
                        bool toward = at >= 0 && weapon.Evolution != null && !upgrades.HasEvolved(weapon.Id) &&
                                      upgrades.Loadout.At(at).Level < EvolutionRule.WeaponEvolutionLevel &&
                                      (anyTome || upgrades.TomeLevel(weapon.EvolvesWithTomeId) > 0);
                        return toward ? 50 : 29;
                    }
                    return 29;
                default: return card.Stat == UpgradeStat.MaxHealth ? 25 : 20;
            }
        }

        // Synergy (a) with the health and armour cards' tiers (10 a tier), and the low share moved.
        private static float SynergyDefence(DescentSimulation.FoundryPick pick, UpgradeService upgrades, int slot, float toward, float lowShare, int tierStep = 10)
        {
            UpgradeOption card = pick.Offer.Choices[slot];
            if (pick.Health < pick.Maximum * lowShare && card.Stat == UpgradeStat.MaxHealth)
                return 100;
            if (card.Category == OfferCategory.General)
                return card.Stat == UpgradeStat.MaxHealth ? 25 + tierStep * (int)pick.Offer.RarityAt(slot)
                    : card.Stat == UpgradeStat.Armor ? 20 + tierStep * (int)pick.Offer.RarityAt(slot) : 20;
            float score = Synergy(pick, upgrades, slot, false, false, 2);
            if (card.Category == OfferCategory.WeaponUpgrade && score == 50 && !card.IsEvolution)
                score = toward;
            return score;
        }

        // The shipped score without its way to the evolutions: health when low; a new weapon while a slot is free; a new
        // tome while under two; the tomes' levels; every line and evolution at 28; then the hero's cards - health and armour
        // 10 a tier above common. It clears as many, but the careful pilot then evolves in about one clear in ten.
        private static float LinesUnderTomes(DescentSimulation.FoundryPick pick, UpgradeService upgrades, int slot)
        {
            UpgradeOption card = pick.Offer.Choices[slot];
            if (pick.Low && card.Stat == UpgradeStat.MaxHealth)
                return 100;
            int tier = (int)pick.Offer.RarityAt(slot);
            switch (card.Category)
            {
                case OfferCategory.NewWeapon: return pick.WeaponsHeld < Cryptforge.Combat.WeaponLoadout.MaxSlots ? 45 : 10;
                case OfferCategory.NewTome: return pick.TomesHeld < 2 ? 40 : 15;
                case OfferCategory.TomeUpgrade: return 30;
                case OfferCategory.WeaponUpgrade: return 28;
                default:
                    return card.Stat == UpgradeStat.MaxHealth ? 25 + 10 * tier : card.Stat == UpgradeStat.Armor ? 20 + 10 * tier : 20;
            }
        }

        // The first slot whose card matches, or the first slot.
        private static int FirstWhere(DescentSimulation.FoundryPick pick, Func<UpgradeOption, bool> match)
        {
            for (int i = 0; i < pick.Offer.Choices.Count; i++)
                if (match(pick.Offer.Choices[i]))
                    return i;
            return 0;
        }

        // The offer less one slot, as a pick (for a picker that passes a card over).
        private static DescentSimulation.FoundryPick Without(DescentSimulation.FoundryPick pick, int slot)
        {
            var cards = new List<UpgradeOption>();
            var tiers = new List<UpgradeRarity>();
            for (int i = 0; i < pick.Offer.Choices.Count; i++)
                if (i != slot)
                {
                    cards.Add(pick.Offer.Choices[i]);
                    tiers.Add(pick.Offer.RarityAt(i));
                }
            return new DescentSimulation.FoundryPick
            {
                Seconds = pick.Seconds, Level = pick.Level, Room = pick.Room, AtWarden = pick.AtWarden, Health = pick.Health,
                Maximum = pick.Maximum, Armor = pick.Armor, WeaponsHeld = pick.WeaponsHeld, TomesHeld = pick.TomesHeld,
                Offer = new UpgradeOffer(cards, pick.Offer.Index, tiers.ToArray())
            };
        }

        // AutopilotCohort.CardScore as of main 18715c94.
        private static float CarefulScore(DescentSimulation.FoundryPick pick, UpgradeService upgrades, int slot)
        {
            UpgradeOption card = pick.Offer.Choices[slot];
            if (pick.Low && card.Stat == UpgradeStat.MaxHealth)
                return 100;
            switch (card.Category)
            {
                case OfferCategory.WeaponUpgrade: return 50;
                case OfferCategory.NewWeapon: return pick.WeaponsHeld < Cryptforge.Combat.WeaponLoadout.MaxSlots ? 45 : 10;
                case OfferCategory.NewTome: return pick.TomesHeld < 2 ? 40 : 15;
                case OfferCategory.TomeUpgrade: return 30;
                default: return card.Stat == UpgradeStat.MaxHealth ? 25 : 20;
            }
        }

        [Test]
        public void TheCarefulPilotsCards()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            int count = Setting("FOUNDRY_SEEDS", 200);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            _parallel = Setting("FOUNDRY_PARALLEL", Environment.ProcessorCount);
            _seeds = FoundryBatch.Seeds(first, count);
            int floors = Setting("CARDS_FLOORS", 1);
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            string[] realisms = (Environment.GetEnvironmentVariable("CARDS_REALISM") ?? "frame-perfect,typical")
                .Split(',').Select(r => r.Trim()).Where(r => r.Length > 0).ToArray();
            string pickerNames = Environment.GetEnvironmentVariable("CARDS_PICKERS");
            List<Picker> pickers = (pickerNames ?? "careful,first-card,before").Split(',').Select(n => n.Trim()).Where(n => n.Length > 0)
                .Select(n => Pickers().FirstOrDefault(p => p.Name == n) ?? throw new ArgumentException("No such picker: " + n)).ToList();

            var heroes = new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() };
            var md = new StringBuilder();
            md.AppendLine("# The careful pilot's cards").AppendLine();
            md.AppendLine($"Seeds {_seeds[0]}-{_seeds[_seeds.Length - 1]} a hero, floor 1{(floors > 1 ? " and below" : string.Empty)}, " +
                          $"{DateTime.Now:yyyy-MM-dd HH:mm}. Every picker plays its cohort's footwork; the picker replaces the card score.").AppendLine();
            foreach (string realismName in realisms)
            {
                PilotRealism realism = realismName == "frame-perfect" ? null : PilotRealism.Named(realismName);
                var batches = new List<(Picker Picker, Run[] Runs)>();
                foreach (Picker picker in pickers)
                {
                    var runs = new Run[heroes.Length];
                    for (int h = 0; h < heroes.Length; h++)
                        runs[h] = Play(heroes[h], picker, realism, floors);
                    batches.Add((picker, runs));
                }
                md.AppendLine($"## {realismName}").AppendLine();
                md.Append(Section(batches, heroes.Length, Setting("CARDS_DETAIL", 1) != 0));
            }
            File.WriteAllText(Path.Combine(output, "cards-research.md"), md.ToString());
            TestContext.WriteLine(md.ToString());
            Assert.Pass();
        }

        private sealed class Run
        {
            public DescentSimulation.FoundryHero Hero;
            public DescentSimulation.FoundryResult[] Results;
        }

        private Run Play(DescentSimulation.FoundryHero hero, Picker picker, PilotRealism realism, int floors)
        {
            DescentSimulation.FoundryLevers levers = FoundryBatch.Shipped().Copy("cards " + picker.Name);
            levers.Cohort = picker.Cohort;
            levers.Picker = picker.Pick;
            levers.Realism = realism;
            levers.Floors = floors;
            levers.LogsPicks = true;
            levers.CountsHarmByPlace = true;
            var results = new DescentSimulation.FoundryResult[_seeds.Length];
            Parallel.For(0, _seeds.Length, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, _parallel) },
                i => results[i] = DescentSimulation.RunFoundry(hero, _seeds[i], levers));
            return new Run { Hero = hero, Results = results };
        }

        // ---------------------------------------------------------------------------------------------------------
        // The tables
        // ---------------------------------------------------------------------------------------------------------

        private static string Section(List<(Picker Picker, Run[] Runs)> batches, int heroes, bool detail)
        {
            var md = new StringBuilder();
            Run[] careful = batches[0].Runs;

            md.AppendLine("### Clears and deaths").AppendLine();
            md.AppendLine("Floor 1 cleared; both floors where the batch went down. Paired with the first picker: both / only it / only this.").AppendLine();
            md.AppendLine("| Picker | Hero | Cleared | Both floors | Paired | Deaths: where | Deaths: blow | Health lost a run: wave / horde / Warden / slams / pit | Lowest health P50 | Evolved clears |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
            foreach ((Picker picker, Run[] runs) in batches)
                for (int h = 0; h < heroes; h++)
                {
                    DescentSimulation.FoundryResult[] r = runs[h].Results;
                    var died = r.Where(x => x.Outcome == DescentSimulation.FoundryOutcome.Died).ToArray();
                    md.AppendLine($"| {picker.Name} | {runs[h].Hero.Name} | {r.Count(x => x.Cleared)} | {r.Count(Both)} | {Paired(careful[h].Results, r)} | " +
                                  $"{Group(died, Room)} | {Group(died, x => x.Killer ?? "?")} | " +
                                  $"{Mean(r, x => x.TakenFromWave)} / {Mean(r, x => x.TakenFromHorde)} / {Mean(r, x => x.TakenFromWarden)} / " +
                                  $"{Mean(r, x => x.TakenFromSlams)} / {Mean(r, x => x.TakenFromPit)} | " +
                                  $"{P(r.Select(x => x.LowestHealthShare * 100f), 50)}% | {r.Count(x => x.Cleared && x.Evolutions > 0)} |");
                }
            md.AppendLine();

            if (!detail)
                return md.ToString();
            md.AppendLine("### Health lost a run by place (mean), floor 1").AppendLine();
            md.AppendLine("| Picker | Hero | Place: health lost a run, largest blows |");
            md.AppendLine("|---|---|---|");
            foreach ((Picker picker, Run[] runs) in batches)
                for (int h = 0; h < heroes; h++)
                {
                    DescentSimulation.FoundryResult[] r = runs[h].Results;
                    var byPlace = r.SelectMany(x => x.TakenBy).GroupBy(kv => kv.Key.Split('|')[0])
                        .Select(g => (Place: g.Key, Sum: g.Sum(kv => kv.Value),
                            Blows: g.GroupBy(kv => kv.Key.Split('|')[1]).Select(b => (Blow: b.Key, Sum: b.Sum(kv => kv.Value)))
                                .OrderByDescending(b => b.Sum).Take(3).ToArray()))
                        .OrderByDescending(g => g.Sum).ToArray();
                    string text = string.Join("; ", byPlace.Select(g =>
                        $"{g.Place} {N(g.Sum / r.Length)} ({string.Join(", ", g.Blows.Select(b => $"{b.Blow} {N(b.Sum / r.Length)}"))})"));
                    md.AppendLine($"| {picker.Name} | {runs[h].Hero.Name} | {text} |");
                }
            md.AppendLine();

            md.AppendLine("### Cards taken a run (mean), floor 1").AppendLine();
            md.AppendLine("Kinds: weapon lines (and evolutions), new weapons, new tomes, tome levels, general cards. Health: cards on the hero's maximum health " +
                          "(Vitality, the health card, its evolution); armour: the armour card. Rare+: cards taken above the common tier.").AppendLine();
            md.AppendLine("| Picker | Hero | Offers | Weapon lines / new weapons / new tomes / tome levels / general | Health cards (of which Vitality) | Armour cards | Speed cards | Other general | Rare+ taken | Low (under half) picks | Health picks while low |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            foreach ((Picker picker, Run[] runs) in batches)
                for (int h = 0; h < heroes; h++)
                {
                    DescentSimulation.FoundryResult[] r = runs[h].Results;
                    var picks = r.SelectMany(x => x.PickLog).ToArray();
                    double n = r.Length;
                    string Kind(OfferCategory c) => (picks.Count(p => p.Card.Category == c) / n).ToString("0.0", CultureInfo.InvariantCulture);
                    string Count(Func<DescentSimulation.FoundryPick, bool> f) => (picks.Count(f) / n).ToString("0.0", CultureInfo.InvariantCulture);
                    md.AppendLine($"| {picker.Name} | {runs[h].Hero.Name} | {(picks.Length / n):0.0} | " +
                                  $"{Kind(OfferCategory.WeaponUpgrade)} / {Kind(OfferCategory.NewWeapon)} / {Kind(OfferCategory.NewTome)} / {Kind(OfferCategory.TomeUpgrade)} / {Kind(OfferCategory.General)} | " +
                                  $"{Count(p => p.Card.Stat == UpgradeStat.MaxHealth)} ({Count(p => p.Card.Id == "tome_vitality")}) | " +
                                  $"{Count(p => p.Card.Stat == UpgradeStat.Armor)} | {Count(p => p.Card.Stat == UpgradeStat.MoveSpeed)} | " +
                                  $"{Count(p => p.Card.Category == OfferCategory.General && p.Card.Stat != UpgradeStat.MaxHealth && p.Card.Stat != UpgradeStat.Armor && p.Card.Stat != UpgradeStat.MoveSpeed)} | " +
                                  $"{Count(p => p.Rarity != UpgradeRarity.Common)} | {Count(p => p.Low)} | {Count(p => p.Low && p.Card.Stat == UpgradeStat.MaxHealth)} |");
                }
            md.AppendLine();

            md.AppendLine("### What was on offer and passed over a run (mean), floor 1").AppendLine();
            md.AppendLine("Cards in an offer the picker did not take: health and armour cards by tier, Vitality as a new tome.").AppendLine();
            md.AppendLine("| Picker | Hero | Health passed: common / rare / epic / gold | Armour passed: common / rare / epic / gold | Vitality passed as a new tome | Tome levels passed |");
            md.AppendLine("|---|---|---|---|---|---|");
            foreach ((Picker picker, Run[] runs) in batches)
                for (int h = 0; h < heroes; h++)
                {
                    DescentSimulation.FoundryResult[] r = runs[h].Results;
                    var passed = r.SelectMany(x => x.PickLog).SelectMany(p => Enumerable.Range(0, p.Offer.Choices.Count).Where(i => i != p.Taken)
                        .Select(i => (Card: p.Offer.Choices[i], Rarity: p.Offer.RarityAt(i)))).ToArray();
                    double n = r.Length;
                    string Tiers(UpgradeStat stat) => string.Join(" / ", Enum.GetValues(typeof(UpgradeRarity)).Cast<UpgradeRarity>()
                        .Select(t => (passed.Count(c => c.Card.Category == OfferCategory.General && c.Card.Stat == stat && c.Rarity == t) / n).ToString("0.00", CultureInfo.InvariantCulture)));
                    md.AppendLine($"| {picker.Name} | {runs[h].Hero.Name} | {Tiers(UpgradeStat.MaxHealth)} | {Tiers(UpgradeStat.Armor)} | " +
                                  $"{(passed.Count(c => c.Card.Id == "tome_vitality" && c.Card.Category == OfferCategory.NewTome) / n):0.00} | " +
                                  $"{(passed.Count(c => c.Card.Category == OfferCategory.TomeUpgrade) / n):0.00} |");
                }
            md.AppendLine();

            md.AppendLine("### When, floor 1: cards a run by the floor's minute (weapon lines / new weapons / new tomes / tome levels / general)").AppendLine();
            md.AppendLine("| Picker | Hero | 0-1 min | 1-2 | 2-3 | 3-4 | 4+ (or at the Warden) |");
            md.AppendLine("|---|---|---|---|---|---|---|");
            foreach ((Picker picker, Run[] runs) in batches)
                for (int h = 0; h < heroes; h++)
                {
                    DescentSimulation.FoundryResult[] r = runs[h].Results;
                    var picks = r.SelectMany(x => x.PickLog).ToArray();
                    double n = r.Length;
                    var cells = new List<string>();
                    for (int m = 0; m < 5; m++)
                    {
                        var at = picks.Where(p => Math.Min(4, (int)(p.Seconds / 60f)) == m).ToArray();
                        cells.Add(string.Join(" / ", new[] { OfferCategory.WeaponUpgrade, OfferCategory.NewWeapon, OfferCategory.NewTome, OfferCategory.TomeUpgrade, OfferCategory.General }
                            .Select(c => (at.Count(p => p.Card.Category == c) / n).ToString("0.0", CultureInfo.InvariantCulture))));
                    }
                    md.AppendLine($"| {picker.Name} | {runs[h].Hero.Name} | {string.Join(" | ", cells)} |");
                }
            md.AppendLine();

            md.AppendLine("### The hero at the Warden, floor 1").AppendLine();
            md.AppendLine("Maximum health and health as the Warden walked out (P50 of the runs that got there); armour at the last offer before it; weapons, their levels and tomes held at the end (most common).").AppendLine();
            md.AppendLine("| Picker | Hero | Got there | Maximum P10 / P50 / P90 | Health P50 | Armour P50 / P90 | Tomes held at the end (top 4) | Vitality held | Weapon levels at the end (top 3) | Evolutions mean |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
            foreach ((Picker picker, Run[] runs) in batches)
                for (int h = 0; h < heroes; h++)
                {
                    DescentSimulation.FoundryResult[] r = runs[h].Results;
                    var gated = r.Where(x => x.HeroMaximumAtGate > 0f).ToArray();
                    float ArmorAtGate(DescentSimulation.FoundryResult x)
                    {
                        DescentSimulation.FoundryPick last = x.PickLog.LastOrDefault(p => !p.AtWarden);
                        if (last == null)
                            return 0f;
                        UpgradeOption card = last.Card;
                        return last.Armor + (card.Stat == UpgradeStat.Armor && card.Category == OfferCategory.General ? card.ModifierFor(last.Rarity).Amount : 0f);
                    }
                    string tomes = string.Join(", ", r.GroupBy(x => x.Tomes ?? "-").OrderByDescending(g => g.Count()).Take(4).Select(g => $"{g.Key} x{g.Count()}"));
                    string levels = string.Join(", ", r.GroupBy(x => x.Weapons ?? "-").OrderByDescending(g => g.Count()).Take(3).Select(g => $"{g.Key} x{g.Count()}"));
                    md.AppendLine($"| {picker.Name} | {runs[h].Hero.Name} | {gated.Length} | {P(gated.Select(x => x.HeroMaximumAtGate), 10)} / {P(gated.Select(x => x.HeroMaximumAtGate), 50)} / {P(gated.Select(x => x.HeroMaximumAtGate), 90)} | " +
                                  $"{P(gated.Select(x => x.HeroHealthAtGate), 50)} | {P(r.Select(ArmorAtGate), 50)} / {P(r.Select(ArmorAtGate), 90)} | {tomes} | " +
                                  $"{r.Count(x => (x.Tomes ?? string.Empty).Contains("vitality"))} | {levels} | {Mean(r, x => x.Evolutions, "0.00")} |");
                }
            md.AppendLine();
            return md.ToString();
        }

        // ---------------------------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------------------------

        private static bool Both(DescentSimulation.FoundryResult run) => run.Cleared && (run.NextFloor == null || run.NextFloor.Cleared);

        private static string Room(DescentSimulation.FoundryResult run) =>
            run.DeathRoom < 1 ? "descent"
            : run.DeathRoom <= run.Kinds.Length && run.Kinds[run.DeathRoom - 1] == Cryptforge.Combat.RoomKind.Boss ? "boss"
            : "room " + run.DeathRoom.ToString(CultureInfo.InvariantCulture);

        private static string Group(DescentSimulation.FoundryResult[] runs, Func<DescentSimulation.FoundryResult, string> key) =>
            runs.Length == 0 ? "-" : string.Join(", ", runs.GroupBy(key).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
                .Take(5).Select(g => $"{g.Key} {g.Count()}"));

        private static string Paired(DescentSimulation.FoundryResult[] a, DescentSimulation.FoundryResult[] b)
        {
            int both = 0, onlyA = 0, onlyB = 0;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                if (a[i].Cleared && b[i].Cleared)
                    both++;
                else if (a[i].Cleared)
                    onlyA++;
                else if (b[i].Cleared)
                    onlyB++;
            }
            return $"{both} / {onlyA} / {onlyB}";
        }

        private static string P(IEnumerable<float> values, double p)
        {
            double value = FoundryBatch.Percentile(values, p);
            return double.IsNaN(value) ? "-" : value.ToString("0", CultureInfo.InvariantCulture);
        }

        private static string Mean(DescentSimulation.FoundryResult[] runs, Func<DescentSimulation.FoundryResult, float> f, string format = "0") =>
            runs.Length == 0 ? "-" : runs.Average(f).ToString(format, CultureInfo.InvariantCulture);

        private static string N(float value) => value.ToString("0.0", CultureInfo.InvariantCulture);

        private static int Setting(string name, int fallback) =>
            int.TryParse(Environment.GetEnvironmentVariable(name), out int value) ? value : fallback;

        private static string RepositoryRoot()
        {
            string directory = TestContext.CurrentContext.TestDirectory;
            while (directory != null && !Directory.Exists(Path.Combine(directory, "Assets", "_Project")))
                directory = Path.GetDirectoryName(directory);
            return directory ?? TestContext.CurrentContext.TestDirectory;
        }
    }
}
