using System;
using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Progression;

namespace Cryptforge.Core
{
    // Who the development autopilot plays as (docs/41 step 5's policies; docs/40 section 9.4's bot cohorts, where the
    // pilot's own habits can express them): a name and the habits it keeps. DevelopmentAutopilot, from the words of
    // development/autopilot.txt, and the foundry simulation's copy of it, from DescentSimulation.FoundryLevers.Cohort,
    // read the same habits in the same order and step through the same bodies here and in FootworkRule, so a cohort is
    // one player in the scene and the simulation (the reason FootworkRule is one body). A cohort only drops habits the
    // careful pilot has; none is invented for it.
    public sealed class AutopilotCohort
    {
        // One habit of the pilot's, kept or dropped. Append, never renumber.
        [Flags]
        public enum Habit
        {
            None = 0,
            // It kites any enemy within its kite reach, circling the crowd (docs/41, 2026-10-26).
            Kites = 1 << 0,
            // Its kite slides along a wall or turns away from it (FootworkRule.Kite); without it the stride runs straight
            // on, and against a wall the hero holds the stick into it and stands (FootworkRule.KiteStraight). 2026-11-04.
            KitesWithRoom = 1 << 1,
            // It kites each enemy just past that enemy's own reach (FootworkRule.ThreatReach); without it every enemy but
            // the gate boss at the kite reach, 1.6, which a Tank's 1.7 outreaches. 2026-11-04.
            KitesEachAtItsReach = 1 << 2,
            // It leaves the Warden's mark before the slam.
            EscapesMarks = 1 << 3,
            // ...away from the Warden, with room (FootworkRule.EscapeMark); without it the nearest of eight points round
            // the mark, whichever side the Warden stands (FootworkRule.EscapeMarkNearest). 2026-11-04.
            EscapesAwayFromTheWarden = 1 << 4,
            // It leaves the pit's band when it glows with a spill coming.
            Shelters = 1 << 5,
            // It keeps out of the glowing band: no step into it but to leave the Warden's mark, no lure to it, no kite or
            // escape across it, and a shelter that leaves it wherever the rim spot lies (FootworkRule.Shelter); without it
            // the shelter is the rim spot - a brazier or a wall in places - and the lure and the kite take it back in.
            // 2026-11-04.
            KeepsOutOfTheGlow = 1 << 6,
            // It draws a node fight to the pit's rim, so the spills burn the wave.
            Lures = 1 << 7,
            // It fires its ability at a crowd, or at once beside the gate boss.
            UsesTheAbility = 1 << 8,
            // It takes a card by docs/42's CardPicker order (CardScore); without it each offer's first card - the
            // autopilot's picker until 2026-10-26, docs/41 step 5's "first-card picker".
            ScoresCards = 1 << 9,
            // ...with a new weapon while a weapon slot is free; without it only a second, and it never held three.
            // 2026-11-04.
            FillsWeaponSlots = 1 << 10,
            // Against a single elite room's Captain, when its best weapon on that target is short (the Daggers) and it is
            // safe, it holds the target at that weapon's reach rather than kiting (FootworkRule.StepIn), as a player closes
            // in; without it the kite holds the Captain at 1.75, outside the Daggers' 1.8. Measured 2026-11-10 night, the
            // careful pilot's since 2026-11-11 (docs/41: the lead's decision with the Daggers' elite share).
            StepsIn = 1 << 11,
            All = (1 << 12) - 1,
            // Research, outside All - no cohort has it unless built with it: the step-in against the gate boss too. It takes
            // the Warden's fight under docs/41 step 4's 45 s (docs/41, status 2026-11-10 night).
            StepsInOnTheGateBoss = 1 << 12,
            Known = (1 << 13) - 1
        }

        // What 2026-11-04's four fixes taught the pilot (the survival research): the careful pilot without them is the
        // pilot of main d6664d2, which cleared 74 (Vanguard) and 158 (Ilya) of seeds 1-200.
        public const Habit Fixes = Habit.KitesWithRoom | Habit.KitesEachAtItsReach | Habit.EscapesAwayFromTheWarden |
                                   Habit.KeepsOutOfTheGlow | Habit.FillsWeaponSlots;

        // The careful pilot, the default: every habit. docs/40 9.4's Skilled bot "reads telegraphs and manages its route",
        // as this one reads the Warden's mark and the pit's glow, kites with room, steps in on an elite with its short
        // weapon and builds by the CardPicker order.
        public static readonly AutopilotCohort Careful = new AutopilotCohort("careful", Habit.All);

        // docs/41 step 5's standing policy: no kiting, and no step-in - it stands where the fight finds it. Everything else
        // is the careful pilot's - it still leaves the Warden's mark, shelters from a spill and waits out the glow, and
        // draws node fights to the rim.
        public static readonly AutopilotCohort Standing = new AutopilotCohort("standing", Habit.All & ~(Habit.Kites | Habit.StepsIn));

        // docs/41 step 5's first-card picker with the careful pilot's footwork: every offer's first card.
        public static readonly AutopilotCohort FirstCard = new AutopilotCohort("first-card", Habit.All & ~Habit.ScoresCards);

        // The pilot before 2026-11-04's fixes, each undone, and without the step-in that came after them. docs/40 9.4's
        // Casual in shape: it gathers the drops, dodges simply - kites straight on at one reach, leaves the mark by the
        // nearest way, shelters on the rim spot and walks back into the glow - and takes a reasonable card.
        public static readonly AutopilotCohort Clumsy = new AutopilotCohort("clumsy", Habit.All & ~(Fixes | Habit.StepsIn));

        // docs/40 9.4's Auto Farmer: it moves - the careful pilot's walk between fights, to the altars and the next hall
        // with the drops beside the way - with no active skill and a simple upgrade, the first card. And no dodge, which
        // the report gives the Casual: no kite, no escape from the mark, no shelter from a spill, no wait for the glow;
        // nor the lure, a fight drawn to the pit on purpose. In a node fight it stands where the fight found it.
        public static readonly AutopilotCohort AutoFarmer = new AutopilotCohort("auto-farmer",
            Habit.All & ~(Habit.Kites | Habit.EscapesMarks | Habit.Shelters | Habit.KeepsOutOfTheGlow | Habit.Lures |
                          Habit.UsesTheAbility | Habit.ScoresCards | Habit.StepsIn));

        // Every cohort, the careful one first.
        public static readonly IReadOnlyList<AutopilotCohort> Cohorts = new[] { Careful, Standing, FirstCard, Clumsy, AutoFarmer };

        public string Name { get; }
        public Habit Habits { get; }

        public AutopilotCohort(string name, Habit habits)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("A cohort has a name.", nameof(name));
            Name = name;
            Habits = habits & Habit.Known;
        }

        public bool Has(Habit habit) => (Habits & habit) == habit;

        public bool Kites => Has(Habit.Kites);
        public bool EscapesMarks => Has(Habit.EscapesMarks);
        public bool Shelters => Has(Habit.Shelters);
        public bool Lures => Has(Habit.Lures);
        public bool UsesTheAbility => Has(Habit.UsesTheAbility);
        public bool StepsIn => Has(Habit.StepsIn);
        public bool StepsInOnTheGateBoss => Has(Habit.StepsIn | Habit.StepsInOnTheGateBoss);

        public override string ToString() => Name;

        // The cohort of this name, if there is one.
        public static bool TryNamed(string name, out AutopilotCohort cohort)
        {
            string trimmed = name?.Trim();
            for (int i = 0; i < Cohorts.Count; i++)
                if (string.Equals(Cohorts[i].Name, trimmed, StringComparison.Ordinal))
                {
                    cohort = Cohorts[i];
                    return true;
                }
            cohort = null;
            return false;
        }

        // The cohort a line of development/autopilot.txt names: the first of its words that is a cohort's name, beside the
        // measurement modes ("linger", "idle") or "on"; the careful pilot when none is.
        public static AutopilotCohort FromSwitch(string line)
        {
            if (string.IsNullOrEmpty(line))
                return Careful;
            string[] words = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
                if (TryNamed(words[i], out AutopilotCohort cohort))
                    return cohort;
            return Careful;
        }

        // While the pit's band glows (glowing): whether this cohort keeps out of it now.
        public bool Guards(bool glowing) => glowing && Has(Habit.KeepsOutOfTheGlow);

        // How near an enemy whose weapon reaches enemyReach comes before this cohort kites from it: FootworkRule.ThreatReach,
        // or, without KitesEachAtItsReach, kiteReach for every enemy but the gate boss.
        public float ThreatReach(float kiteReach, float enemyReach, bool gateBoss) =>
            gateBoss || Has(Habit.KitesEachAtItsReach) ? FootworkRule.ThreatReach(kiteReach, enemyReach, gateBoss) : kiteReach;

        // The kite's stride: with room, out of the glowing band where this cohort keeps out of it (FootworkRule.Kite; false
        // when no turn has room), or straight on (FootworkRule.KiteStraight).
        public bool Kite(float x, float y, float pushX, float pushY, float turn, float stride, bool glowing, out float gx,
            out float gy)
        {
            if (Has(Habit.KitesWithRoom))
                return FootworkRule.Kite(x, y, pushX, pushY, turn, stride, Guards(glowing), out gx, out gy);
            FootworkRule.KiteStraight(x, y, pushX, pushY, turn, stride, out gx, out gy);
            return true;
        }

        // The way out of the Warden's mark: away from the Warden with room, not across the glowing band where this cohort
        // keeps out of it (FootworkRule.EscapeMark), or the nearest way out (FootworkRule.EscapeMarkNearest).
        public bool EscapeMark(float x, float y, float markX, float markY, float radius, float wardenX, float wardenY,
            bool glowing, out float gx, out float gy) =>
            Has(Habit.EscapesAwayFromTheWarden)
                ? FootworkRule.EscapeMark(x, y, markX, markY, radius, wardenX, wardenY, Guards(glowing), out gx, out gy)
                : FootworkRule.EscapeMarkNearest(x, y, markX, markY, radius, out gx, out gy);

        // Out of the glowing band: FootworkRule.Shelter, or the rim spot depth out wherever it lies.
        public void Shelter(float x, float y, float depth, out float gx, out float gy)
        {
            if (Has(Habit.KeepsOutOfTheGlow))
                FootworkRule.Shelter(x, y, depth, out gx, out gy);
            else
                FootworkRule.RimSpot(x, y, depth, out gx, out gy);
        }

        // How many weapons this cohort takes a new one up to: every slot, or two.
        public int WeaponsWanted => Has(Habit.FillsWeaponSlots) ? WeaponLoadout.MaxSlots : 2;

        // docs/42's CardPicker order, as the autopilot scores a card at the tier it is offered (low: the hero under half its
        // health; towardEvolution: a line of a weapon on its way to its evolution, LeadsToEvolution): health when low; an
        // evolution; a new weapon while it holds fewer than it wants; two tomes; a line toward an evolution; the tomes'
        // levels; the other lines; then the hero's own cards, health and armour TierStep higher each tier above common.
        //
        // The tomes' levels come before the weapons' lines since 2026-11-10 (docs/41): a tome's level raises every weapon
        // held - Force, Tempo, Reach and Precision each weapon's damage, speed, circles or crits, Vitality the hero's health -
        // where a line raises one weapon. With every line at 50 the careful pilot spent two thirds of its cards on them, each
        // weapon to its last level, the Daggers too, and half its few tome levels after the fourth minute: frame-perfect it
        // cleared, but with a human's hands (the cohorts' PilotRealism) the horde reached the hero and the first-card cohort
        // outlived it, 181 to 154 Vanguards of 200. A line still comes before the tomes' levels while it builds toward its
        // weapon's evolution (the weapon below EvolutionRule.WeaponEvolutionLevel with its tome held), so the careful pilot
        // still evolves as often as before. And a health or armour card's tiers scale 1 / 2 / 4 / 5.3 where a line's scale
        // 1 / 1.25 / 1.5 / 2: a rare one outranks a line and a tome's level, an epic one a new tome, a gold health card a
        // new weapon.
        public const int TierStep = 10;

        public int CardScore(UpgradeOption card, UpgradeRarity tier, bool low, int weaponsHeld, int tomesHeld,
            bool towardEvolution)
        {
            if (low && card.Stat == UpgradeStat.MaxHealth)
                return 100;
            switch (card.Category)
            {
                case OfferCategory.WeaponUpgrade: return card.IsEvolution ? 50 : towardEvolution ? 31 : 28;
                case OfferCategory.NewWeapon: return weaponsHeld < WeaponsWanted ? 45 : 10;
                case OfferCategory.NewTome: return tomesHeld < 2 ? 40 : 15;
                case OfferCategory.TomeUpgrade: return 30;
                default:
                    if (card.Stat == UpgradeStat.MaxHealth)
                        return 25 + TierStep * (int)tier;
                    return card.Stat == UpgradeStat.Armor ? 20 + TierStep * (int)tier : 20;
            }
        }

        // Whether a card is a line of a weapon the hero holds on its way to its evolution (docs/42, EvolutionRule): the
        // weapon has one, has not taken it, stands below WeaponEvolutionLevel, and the tome it evolves with is held. False
        // for anything else, and in the legacy engine.
        public static bool LeadsToEvolution(UpgradeOption card, UpgradeService upgrades)
        {
            if (card == null || card.Category != OfferCategory.WeaponUpgrade || card.IsEvolution || upgrades == null ||
                upgrades.Loadout == null || upgrades.WeaponCards == null)
                return false;
            for (int w = 0; w < upgrades.WeaponCards.Count; w++)
            {
                WeaponCard weapon = upgrades.WeaponCards[w];
                for (int l = 0; l < weapon.Lines.Count; l++)
                {
                    if (weapon.Lines[l].Id != card.Id)
                        continue;
                    int slot = upgrades.Loadout.IndexOf(weapon.Id);
                    return slot >= 0 && weapon.Evolution != null && !upgrades.HasEvolved(weapon.Id) &&
                           upgrades.Loadout.At(slot).Level < EvolutionRule.WeaponEvolutionLevel &&
                           upgrades.TomeLevel(weapon.EvolvesWithTomeId) > 0;
                }
            }
            return false;
        }

        // The slot this cohort takes from the run's upgrade offer, as the scene's autopilot and the simulation's take it:
        // what the hero holds read from the offers' own service.
        public int Pick(UpgradeOffer offer, bool low, UpgradeService upgrades) =>
            Pick(offer, low, upgrades.Loadout != null ? upgrades.Loadout.Count : 1, upgrades.TomesHeld, upgrades);

        // The slot this cohort takes from an upgrade offer: the best scored at its tier, the first of equals; without
        // ScoresCards, the first. Without the service no line leads to an evolution.
        public int Pick(UpgradeOffer offer, bool low, int weaponsHeld, int tomesHeld, UpgradeService upgrades = null)
        {
            if (!Has(Habit.ScoresCards))
                return 0;
            int best = 0, bestScore = int.MinValue;
            for (int i = 0; i < offer.Choices.Count; i++)
            {
                UpgradeOption card = offer.Choices[i];
                int score = CardScore(card, offer.RarityAt(i), low, weaponsHeld, tomesHeld, LeadsToEvolution(card, upgrades));
                if (score <= bestScore)
                    continue;
                best = i;
                bestScore = score;
            }
            return best;
        }
    }
}
