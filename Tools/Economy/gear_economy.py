"""docs/54's economy model: items, basma (+0..+9) and efsun, paid for with forge gold, floor materials and boosters.

Pure arithmetic and a seeded Monte Carlo; nothing here touches the game. Income is measured, not assumed: every run of
Tools/Economy/income-2026-11-16.csv is one Descent of the shipped game in the simulation (GearEconomyResearch, seeds 1-100,
both heroes, five cohorts, typical human hands), with its forge gold banked, floors cleared, elites and bosses felled.
A simulated player plays runs drawn from its cohort's rows, banks the run's loot under the gold's rule (a death loses
half of what the dying floor paid), and spends at the Anvil after every run.

    python Tools/Economy/gear_economy.py                 # the tables docs/54 quotes
    python Tools/Economy/gear_economy.py --players 4000  # more players a cell (default 2000)

Every knob docs/54 names is a constant below, in one place.
"""

import argparse
import csv
import os
import random
from collections import defaultdict

HERE = os.path.dirname(os.path.abspath(__file__))
INCOME = os.path.join(HERE, "income-2026-11-16.csv")

# ---------------------------------------------------------------------------------------------------------------------
# Basma: the attempt from level L-1 to L, for L = 1..9 (index 0 is +1).
BASE_CHANCE = [100, 100, 100, 90, 75, 60, 45, 30, 20]       # percent, the lead's table
PITY_STEP = [0, 0, 0, 5, 5, 5, 4, 3, 2]                     # points added per failure at this level, reset on success
PITY_CEILING = [0, 0, 0, 2, 3, 4, 6, 8, 10]                 # after this many failures in a row the next strike holds
GOLD = [50, 80, 120, 180, 250, 400, 700, 1300, 2400]        # forge gold per attempt
# Floor materials per attempt: (material, count). E = Ember Shard (Foundry), Q = Quicksilver Bead (Vaults),
# S = Gloam Spore (Gardens).
MATERIAL = [None, None, None, ("E", 2), ("E", 3), ("E", 4), ("Q", 3), ("S", 4), ("S", 6)]

# ---------------------------------------------------------------------------------------------------------------------
# Drops (Forged tier, the shipped Descent). Materials per floor cleared and per elite felled on that floor.
MAT_PER_CLEAR = {1: ("E", 2), 2: ("Q", 2), 3: ("S", 2)}
MAT_PER_ELITE = {1: ("E", 1), 2: ("Q", 1), 3: ("S", 1)}
SEAL_PER_ELITE = 0.08        # Forge Seal: +10 points on one attempt, at most SEALS_PER_STRIKE on one strike
SEAL_PER_BOSS = 0.04
SEAL_BONUS = 10
SEALS_PER_STRIKE = 1
HAMMER_PER_BOSS = 0.04       # Master's Hammer: one attempt up to +6 cannot fail
HAMMER_MAX_LEVEL = 6
SCROLL_PER_CLEAR = 0.25      # Efsun Scroll: one reroll without gold (floors 1-3 and the finale)
RUNE_PER_BOSS = 0.02         # Binding Rune: adds a line to an item with fewer than five
RUNE_PER_FINALE = 0.03
ITEM_PER_BOSS = 0.25         # an armour, amulet or ring
ITEM_PER_ELITE = 0.05
ITEM_PER_FINALE = 0.60
RARITY_P0 = [0.60, 0.25, 0.12, 0.03]   # RarityTable at no luck: Common, Rare, Epic, Gold
LINES_BY_RARITY = [1, 2, 3, 4]
SALVAGE_GOLD = [20, 40, 80, 150]       # an item not kept is melted down at the Anvil
SALVAGE_EMBER = [1, 2, 3, 4]

# Efsun: reroll all lines of one item, priced by the item's rarity and never by its basma level (a price that grew with
# the level would teach rerolling at +0 first).
REROLL_GOLD = [60, 100, 160, 250]       # Common, Rare, Epic, Gold
TIER_WEIGHTS = [40, 30, 18, 9, 3]       # a line's tier 1..5

# ---------------------------------------------------------------------------------------------------------------------
# Power. Main stat per basma level, by slot, in percent (crit in points); the line pool's per-tier values; the caps.
MAIN = {
    "weapon": ("damage", [0, 1, 2, 3, 4, 5, 6, 7, 8.5, 10]),
    "armour": ("health", [0, 1, 2, 3, 4, 5, 6, 7, 8.5, 10]),
    "amulet": ("attack_speed", [0, 0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4.25, 5]),
    "ring": ("crit", [0, 0.3, 0.6, 0.9, 1.2, 1.5, 1.8, 2.1, 2.55, 3]),
}
# Line pool: stat -> value per tier (tier 1..5). Power lines feed the gear power; utility lines have caps of their own.
LINES = {
    "damage": [1, 1.5, 2, 2.5, 3],
    "attack_speed": [0.5, 1, 1.5, 2, 2.5],
    "crit": [0.4, 0.6, 0.8, 1.0, 1.2],
    "health": [1, 1.5, 2, 2.5, 3],
    "armour": [0.4, 0.6, 0.8, 1.0, 1.2],
    "elite_damage": [2, 3, 4, 5, 6],
    "burn_damage": [3, 4.5, 6, 7.5, 9],
    "move_speed": [1, 1.5, 2, 2.5, 3],
    "pickup_radius": [4, 6, 8, 10, 12],
    "ability_cooldown": [2, 3, 4, 5, 6],
    "healing": [3, 4.5, 6, 7.5, 9],
    "luck": [1, 2, 3, 4, 5],
    "area": [1, 1.5, 2, 2.5, 3],
}
# The gear's total, main stat and lines together, never counts past these (percent; crit and armour in points).
CAPS = {"damage": 14, "attack_speed": 7, "crit": 4, "health": 14, "armour": 4, "elite_damage": 12, "burn_damage": 15,
        "move_speed": 6, "pickup_radius": 25, "ability_cooldown": 10, "healing": 15, "luck": 10, "area": 5}
CRIT_MULTIPLIER = 2.0


def gear_power(totals):
    """(DPS x EHP) - 1 against an ungeared hero, from capped totals; the docs/54 power index."""
    t = {k: min(v, CAPS[k]) for k, v in totals.items()}
    dps = (1 + t.get("damage", 0) / 100) * (1 + t.get("attack_speed", 0) / 100) * \
          (1 + t.get("crit", 0) / 100 * (CRIT_MULTIPLIER - 1))
    ehp = (1 + t.get("health", 0) / 100) / (1 - t.get("armour", 0) / 100)
    return dps * ehp - 1, dps - 1, ehp - 1


def set_totals(level, lines):
    totals = defaultdict(float)
    for slot, (stat, curve) in MAIN.items():
        totals[stat] += curve[level]
    for stat, value in lines:
        totals[stat] += value
    return totals


# ---------------------------------------------------------------------------------------------------------------------
def load_income(path=INCOME):
    by_cohort = defaultdict(list)
    with open(path, newline="") as f:
        for row in csv.DictReader(f):
            by_cohort[row["cohort"]].append({k: (v if k in ("cohort", "hero") else float(v)) for k, v in row.items()})
    return by_cohort


def run_loot(row, rng):
    """One run's banked loot under the gold's rule: a floor cleared keeps all; the floor died on keeps half."""
    cleared = int(row["floors_cleared"])
    completed = int(row["completed"]) == 1
    died_on = None if completed else cleared + 1
    loot = defaultdict(float)
    loot["gold"] = row["banked"]
    for floor in (1, 2, 3):
        played = floor <= cleared or floor == died_on
        if not played:
            continue
        keep = 1.0 if floor <= cleared else 0.5
        elites = int(row["elites_f%d" % floor])
        bosses = int(row["bosses_f%d" % floor])
        if floor <= cleared:
            m, n = MAT_PER_CLEAR[floor]
            loot[m] += n
            if rng.random() < SCROLL_PER_CLEAR:
                loot["scroll"] += 1
        m, n = MAT_PER_ELITE[floor]
        loot[m] += int(n * elites * keep)
        for _ in range(elites):
            if rng.random() < SEAL_PER_ELITE * keep:
                loot["seal"] += 1
            # Items are always kept on a death (the lead, 2026-11-16); materials and boosters follow the gold's rule.
            if rng.random() < ITEM_PER_ELITE:
                loot["items"] += 1
        for _ in range(bosses):
            if rng.random() < SEAL_PER_BOSS * keep:
                loot["seal"] += 1
            if rng.random() < HAMMER_PER_BOSS * keep:
                loot["hammer"] += 1
            if rng.random() < RUNE_PER_BOSS * keep:
                loot["rune"] += 1
            if rng.random() < ITEM_PER_BOSS:
                loot["items"] += 1
    if completed:
        if rng.random() < SCROLL_PER_CLEAR:
            loot["scroll"] += 1
        if rng.random() < RUNE_PER_FINALE:
            loot["rune"] += 1
        if rng.random() < ITEM_PER_FINALE:
            loot["items"] += 1
    return loot


def expected_attempts(level_index, seal=0, trials=200000, rng=None):
    """Mean attempts for one level with pity and ceiling, analytically (seal adds to every attempt when given)."""
    p_fail_so_far = 1.0
    mean = 0.0
    k = 0
    while True:
        chance = 100 if k >= PITY_CEILING[level_index] and PITY_CEILING[level_index] > 0 else \
            min(100, BASE_CHANCE[level_index] + PITY_STEP[level_index] * k + seal)
        mean += p_fail_so_far  # this attempt happens with the probability every earlier one failed
        p_fail_so_far *= 1 - chance / 100
        k += 1
        if p_fail_so_far < 1e-12:
            return mean, k


class Player:
    def __init__(self, rng, boosters=True):
        self.rng = rng
        self.boosters = boosters
        self.res = defaultdict(float)
        self.levels = []           # basma level per target item, in the order they are worked
        self.pity = []

    def attempt(self, i):
        L = self.levels[i]          # current level; the attempt targets L + 1
        li = L                      # index into the tables
        cost_gold = GOLD[li]
        mat = MATERIAL[li]
        if self.res["gold"] < cost_gold or (mat and self.res[mat[0]] < mat[1]):
            return False
        self.res["gold"] -= cost_gold
        if mat:
            self.res[mat[0]] -= mat[1]
        k = self.pity[i]
        if PITY_CEILING[li] and k >= PITY_CEILING[li]:
            chance = 100
        else:
            chance = BASE_CHANCE[li] + PITY_STEP[li] * k
            # The Hammer on the +6 strike (the dearest it may serve); the Seals on +9, and on +8 while
            # three or more are left for the +9s.
            if self.boosters and L + 1 == HAMMER_MAX_LEVEL and chance < 100 and self.res["hammer"] >= 1:
                self.res["hammer"] -= 1
                chance = 100
            elif self.boosters and chance < 100:
                seals = 0
                if L + 1 == 9:
                    seals = int(min(SEALS_PER_STRIKE, self.res["seal"]))
                elif L + 1 == 8 and self.res["seal"] >= 3:
                    seals = 1
                self.res["seal"] -= seals
                chance += SEAL_BONUS * seals
        if self.rng.random() * 100 < chance:
            self.levels[i] += 1
            self.pity[i] = 0
        else:
            self.pity[i] += 1
        return True


def simulate(rows, items=4, players=2000, boosters=True, seed=54, max_runs=1500, efsun_share=0.0):
    """Runs (P25 / P50 / P90) until milestones: first item +5, one item +6/+7/+9, every item +9.
    Items are worked one at a time (the player's best item first). efsun_share of each run's gold is set aside for efsun."""
    milestones = {"first +5": [], "one +6": [], "one +7": [], "one +8": [], "one +9": [], "set +7": [], "set +9": []}
    rng = random.Random(seed)
    for _ in range(players):
        p = Player(random.Random(rng.random()), boosters)
        p.levels = [0] * items
        p.pity = [0] * items
        hit = {}
        for run in range(1, max_runs + 1):
            loot = run_loot(rng.choice(rows), p.rng)
            for k, v in loot.items():
                p.res[k] += v * (1 - efsun_share) if k == "gold" else v
            # The first item drops at the first floor-1 boss (guaranteed; docs/54 section 3); the model starts every item
            # at +0 once the first run is played.
            progressed = True
            while progressed:
                progressed = False
                order = sorted(range(items), key=lambda j: p.levels[j])
                # work the highest item until +9, then the next (the "one item first" habit)
                target = next((j for j in range(items) if p.levels[j] < 9), None)
                if target is None:
                    break
                progressed = p.attempt(target)
            best = max(p.levels)
            for name, ok in (("first +5", best >= 5), ("one +6", best >= 6), ("one +7", best >= 7), ("one +8", best >= 8),
                             ("one +9", best >= 9), ("set +7", min(p.levels) >= 7), ("set +9", min(p.levels) >= 9)):
                if ok and name not in hit:
                    hit[name] = run
            if "set +9" in hit:
                break
        for name in milestones:
            milestones[name].append(hit.get(name, max_runs + 1))
    out = {}
    for name, values in milestones.items():
        values.sort()
        n = len(values)
        out[name] = (values[n // 4], values[n // 2], values[int(n * 0.9)])
    return out


def efsun_rerolls(lines=4, wanted=("damage", "crit", "attack_speed", "health"), need=2, min_tier=3, trials=200000, seed=7):
    """Chance one reroll of an item with this many lines gives at least `need` of the wanted stats at tier >= min_tier."""
    rng = random.Random(seed)
    pool = list(LINES)
    good = 0
    for _ in range(trials):
        stats = rng.sample(pool, lines)
        hits = 0
        for s in stats:
            tier = rng.choices(range(1, 6), weights=TIER_WEIGHTS)[0]
            if s in wanted and tier >= min_tier:
                hits += 1
        if hits >= need:
            good += 1
    return good / trials


def mean_loot(rows, n=20000, seed=3):
    rng = random.Random(seed)
    total = defaultdict(float)
    for _ in range(n):
        for k, v in run_loot(rng.choice(rows), rng).items():
            total[k] += v
    return {k: v / n for k, v in total.items()}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--players", type=int, default=2000)
    args = parser.parse_args()
    income = load_income()
    cohorts = ["careful", "standing", "first-card", "clumsy", "auto-farmer"]

    print("## Income a run (mean, Forged tier, the shipped Descent)\n")
    print("| Cohort | Gold | Ember | Quicksilver | Spore | Forge Seal | Hammer | Efsun Scroll | Binding Rune | Items |")
    print("|---|---|---|---|---|---|---|---|---|---|")
    for c in cohorts:
        m = mean_loot(income[c])
        print("| %s | %.0f | %.2f | %.2f | %.2f | %.2f | %.3f | %.2f | %.3f | %.2f |" % (
            c, m.get("gold", 0), m.get("E", 0), m.get("Q", 0), m.get("S", 0), m.get("seal", 0), m.get("hammer", 0),
            m.get("scroll", 0), m.get("rune", 0), m.get("items", 0)))

    print("\n## One item, level by level (expected attempts with pity and ceiling; cost to reach from +0)\n")
    print("| Level | Base | Pity / fail | Ceiling | Attempts (mean) | Worst case | Gold / attempt | Material / attempt | Gold to here | Materials to here |")
    print("|---|---|---|---|---|---|---|---|---|---|")
    gold_sum = 0.0
    mats = defaultdict(float)
    for i in range(9):
        mean, worst = expected_attempts(i)
        gold_sum += mean * GOLD[i]
        if MATERIAL[i]:
            mats[MATERIAL[i][0]] += mean * MATERIAL[i][1]
        mat = "%d %s" % (MATERIAL[i][1], MATERIAL[i][0]) if MATERIAL[i] else "-"
        print("| +%d | %d %% | +%d | %s | %.2f | %d | %d | %s | %.0f | %s |" % (
            i + 1, BASE_CHANCE[i], PITY_STEP[i], PITY_CEILING[i] or "-", mean, worst, GOLD[i], mat, gold_sum,
            " / ".join("%s %.1f" % (k, v) for k, v in sorted(mats.items())) or "-"))
    no_pity = sum(GOLD[i] * 100.0 / BASE_CHANCE[i] for i in range(9))
    print("\nWithout pity or ceiling the same item to +9 costs %.0f gold." % no_pity)

    print("\n## Runs to each milestone (P25 / P50 / P90), four items worked one at a time\n")
    for boosters in (False, True):
        for share in (0.0, 0.25):
            label = ("with boosters" if boosters else "no boosters") + (", 25 % of gold set aside for efsun" if share else "")
            print("### %s\n" % label)
            print("| Cohort | First +5 | One +6 | One +7 | One +8 | One +9 | Set +7 | Set +9 |")
            print("|---|---|---|---|---|---|---|---|")
            for c in cohorts:
                r = simulate(income[c], players=args.players if c in ("careful", "clumsy") else args.players // 4,
                             boosters=boosters, efsun_share=share)
                print("| %s | %s |" % (c, " | ".join("%d / %d / %d" % r[k] for k in
                                                       ("first +5", "one +6", "one +7", "one +8", "one +9", "set +7", "set +9"))))
            print()

    print("## Efsun: chance a reroll is 'good' (at least two of damage / crit / attack speed / health at tier 3+)\n")
    print("| Lines | Rarity | Good a reroll | Rerolls to good (mean) | Gold to good (no scrolls) |")
    print("|---|---|---|---|---|")
    for lines, rarity in ((1, "Common"), (2, "Rare"), (3, "Epic"), (4, "Gold"), (5, "Gold + Binding Rune")):
        p = efsun_rerolls(lines=lines)
        price = REROLL_GOLD[min(lines, 4) - 1]
        print("| %d | %s (%d a reroll) | %.2f %% | %s | %s |" % (lines, rarity, price, p * 100, "%.0f" % (1 / p) if p else "-",
                                                                 "%.0f" % (price / p) if p else "-"))

    print("\n## Gear power (DPS x EHP - 1), capped\n")
    print("| Set | Damage | Attack speed | Crit | Health | Armour | DPS | EHP | Power |")
    print("|---|---|---|---|---|---|---|---|---|")
    good4 = [("damage", 2), ("crit", 0.8), ("health", 2), ("move_speed", 2)]
    perfect5 = [("damage", 3), ("attack_speed", 2.5), ("crit", 1.2), ("health", 3), ("armour", 1.2)]
    for name, level, lines in (("+5, no lines", 5, []), ("+7, no lines", 7, []), ("+9, no lines", 9, []),
                               ("+9, 'good' 4-line efsun on every item", 9, good4 * 4),
                               ("+9, perfect 5-line efsun on every item", 9, perfect5 * 4)):
        t = set_totals(level, lines)
        power, dps, ehp = gear_power(t)
        print("| %s | %.1f | %.1f | %.1f | %.1f | %.1f | +%.0f %% | +%.0f %% | **+%.0f %%** |" % (
            name, min(t["damage"], CAPS["damage"]), min(t["attack_speed"], CAPS["attack_speed"]), min(t["crit"], CAPS["crit"]),
            min(t["health"], CAPS["health"]), min(t.get("armour", 0), CAPS["armour"]), dps * 100, ehp * 100, power * 100))


if __name__ == "__main__":
    main()
