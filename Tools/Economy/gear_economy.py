"""docs/54's economy model: items, basma (+0..+9) and efsun, paid for with forge gold, floor materials and boosters.

Pure arithmetic and a seeded Monte Carlo; nothing here touches the game. Income is measured, not assumed: every run of
Tools/Economy/income-2026-11-16.csv is one Descent of the shipped game in the simulation (GearEconomyResearch, seeds 1-100,
both heroes, five cohorts, typical human hands), with its forge gold banked, floors cleared, elites and bosses felled.
A simulated player plays runs drawn from its cohort's rows, banks the run's loot under the gold's rule (a death loses
half of what the dying floor paid), and spends at the Anvil after every run.

    python Tools/Economy/gear_economy.py                 # the tables docs/54 quotes
    python Tools/Economy/gear_economy.py --players 4000  # more players a cell (default 2000)
    python Tools/Economy/gear_economy.py --video         # docs/58 section 4a: the x1.5 gold video on the tiers' payouts
        # --watch-rates 0,30,60,100 --multiplier 150 --min-gold 10 --from-run 2 --video-seconds 30
        # --policies forged,matched --cohorts careful,first-card,clumsy --multipliers 125,150,175,200

Every knob docs/54 names is a constant below, in one place; docs/58's video and docs/54 section 21's tiers likewise.
"""

import argparse
import csv
import os
import random
from collections import defaultdict
from functools import lru_cache

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
# Section 7.2's "good" 4-line efsun (damage, crit, health, move speed at tier 3): the +9g set Abyssal is matched to.
GOOD_LINES = [("damage", 2), ("crit", 0.8), ("health", 2), ("move_speed", 2)]

# ---------------------------------------------------------------------------------------------------------------------
# Difficulty tiers (docs/54 section 21, built). A tier opens once a run on the tier below walks out of the finale.
TIERS = ("forged", "tempered", "inferno", "abyssal")
# Gold: a careful run's measured banked gold on the tier over Forged's (seeds 5201-8400, both heroes: 498 / 620.5 /
# 760.5 / 998 a run); the per-kill rounding puts it a little above the reward. Materials and boosters: the reward
# exactly (LootRule: an amount times the reward, a fraction made whole by one draw; a chance times the reward).
# Items: Forged's chance on every tier (the tier pays in item luck, which the basma road does not read).
TIER_GOLD = {"forged": 1.0, "tempered": 1.25, "inferno": 1.53, "abyssal": 2.0}
TIER_REWARD = {"forged": 1.0, "tempered": 1.25, "inferno": 1.5, "abyssal": 2.0}

# ---------------------------------------------------------------------------------------------------------------------
# docs/58: the result screen's rewarded video (or the "Skip reward videos" product) adds the run's banked gold times
# (multiplier - 100) %, rounded up (RewardOfferRule.BonusGold), from the second completed run, when the run banked at
# least the minimum. Gold only: never materials, boosters or items.
VIDEO_MULTIPLIER_PERCENT = 150
VIDEO_MIN_GOLD = 10
VIDEO_FROM_RUN = 2
VIDEO_SECONDS = 30      # a watched video's time, counted in the hours (a skip owner's are the 100 % row's less this)


def bonus_gold(gold, multiplier_percent=VIDEO_MULTIPLIER_PERCENT, min_gold=VIDEO_MIN_GOLD):
    """RewardOfferRule: ceil(gold x (multiplier - 100) / 100) in integers; nothing below the minimum."""
    gold = int(gold)
    if gold < min_gold:
        return 0
    return (gold * (multiplier_percent - 100) + 99) // 100


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


def levels_power(levels):
    """Gear power of four items at these basma levels, worked in MAIN's order (weapon, armour, amulet, ring), main
    stats only: the model does not roll efsun lines."""
    totals = defaultdict(float)
    for (stat, curve), level in zip(MAIN.values(), levels):
        totals[stat] += curve[level]
    return gear_power(totals)[0]


# The gear power each tier was tuned on (section 21's matched sets): a +5 set, a +9 set, +9 with good efsun.
TIER_MATCHED_POWER = {"forged": 0.0, "tempered": gear_power(set_totals(5, []))[0],
                      "inferno": gear_power(set_totals(9, []))[0], "abyssal": gear_power(set_totals(9, GOOD_LINES * 4))[0]}


# ---------------------------------------------------------------------------------------------------------------------
def load_income(path=INCOME):
    by_cohort = defaultdict(list)
    with open(path, newline="") as f:
        for row in csv.DictReader(f):
            by_cohort[row["cohort"]].append({k: (v if k in ("cohort", "hero") else float(v)) for k, v in row.items()})
    return by_cohort


def _whole(amount, rng):
    """LootRule's tier amount: a fraction is made whole by one draw. A whole amount (every Forged one) draws nothing."""
    base = int(amount)
    if amount == base:
        return base
    return base + (1 if rng.random() < amount - base else 0)


def run_loot(row, rng, tier="forged"):
    """One run's banked loot under the gold's rule: a floor cleared keeps all; the floor died on keeps half.
    On a tier above Forged the gold, materials and booster chances carry the tier's reward (Forged draws as it did)."""
    reward = TIER_REWARD[tier]
    cleared = int(row["floors_cleared"])
    completed = int(row["completed"]) == 1
    died_on = None if completed else cleared + 1
    loot = defaultdict(float)
    loot["gold"] = row["banked"] if TIER_GOLD[tier] == 1 else float(round(row["banked"] * TIER_GOLD[tier]))
    for floor in (1, 2, 3):
        played = floor <= cleared or floor == died_on
        if not played:
            continue
        keep = 1.0 if floor <= cleared else 0.5
        elites = int(row["elites_f%d" % floor])
        bosses = int(row["bosses_f%d" % floor])
        if floor <= cleared:
            m, n = MAT_PER_CLEAR[floor]
            loot[m] += _whole(n * reward, rng)
            if rng.random() < min(1.0, SCROLL_PER_CLEAR * reward):
                loot["scroll"] += 1
        m, n = MAT_PER_ELITE[floor]
        dropped = n * elites if reward == 1 else sum(_whole(n * reward, rng) for _ in range(elites))
        loot[m] += int(dropped * keep)
        for _ in range(elites):
            if rng.random() < min(1.0, SEAL_PER_ELITE * reward) * keep:
                loot["seal"] += 1
            # Items are always kept on a death (the lead, 2026-11-16); materials and boosters follow the gold's rule.
            if rng.random() < ITEM_PER_ELITE:
                loot["items"] += 1
        for _ in range(bosses):
            if rng.random() < min(1.0, SEAL_PER_BOSS * reward) * keep:
                loot["seal"] += 1
            if rng.random() < min(1.0, HAMMER_PER_BOSS * reward) * keep:
                loot["hammer"] += 1
            if rng.random() < min(1.0, RUNE_PER_BOSS * reward) * keep:
                loot["rune"] += 1
            if rng.random() < ITEM_PER_BOSS:
                loot["items"] += 1
    if completed:
        if rng.random() < min(1.0, SCROLL_PER_CLEAR * reward):
            loot["scroll"] += 1
        if rng.random() < min(1.0, RUNE_PER_FINALE * reward):
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


def spend(p, items):
    """Strike at the Anvil while the purse allows: the highest item until +9, then the next (the "one item first" habit)."""
    progressed = True
    while progressed:
        target = next((j for j in range(items) if p.levels[j] < 9), None)
        if target is None:
            break
        progressed = p.attempt(target)


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
            spend(p, items)
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


VIDEO_MILESTONES = ("one +6", "one +9", "set +6", "set +9")


@lru_cache(maxsize=None)
def _levels_power_cached(levels):
    return levels_power(levels)


def simulate_paths(rows, tier_policy="forged", watch_rate=0.0, multiplier_percent=VIDEO_MULTIPLIER_PERCENT,
                   min_gold=VIDEO_MIN_GOLD, from_run=VIDEO_FROM_RUN, video_seconds=VIDEO_SECONDS, items=4, players=2000,
                   boosters=True, efsun_share=0.25, seed=58, max_runs=1500):
    """docs/58 section 4a: each player's road to a full +9 set, with the result screen's gold video and a tier policy.

    tier_policy "forged" stays on Forged. "matched" moves up one tier after a run once this profile has walked out of the
    Descent on its tier (the unlock chain) and its gear power has reached the next tier's matched set (TIER_MATCHED_POWER,
    main stats only). A tier's run is the cohort's measured run with the tier's rewards: section 21 measured a matched
    set's completion on its tier +0.3..+2.9 points above the ungeared cohort's on Forged, and its minutes within 0.1.
    watch_rate: the chance a player takes an offered video, run by run (1.0 also stands for the skip owner).
    Each player has its own streams, seeded by its index (the runs drawn, their loot, the anvil, the watch), so calls that
    differ only in the video or the policy play the same runs and the same strikes as far as their paths agree.
    One record a player: milestone -> (runs, hours); the longest stretch between two levels gained (runs, hours); the run
    each tier was first played; videos taken and their gold."""
    records = []
    for i in range(players):
        tag = "%d-%d-" % (seed, i)
        rows_rng = random.Random(tag + "rows")
        loot_rng = random.Random(tag + "loot")
        watch_rng = random.Random(tag + "watch")
        p = Player(random.Random(tag + "anvil"), boosters)
        p.levels = [0] * items
        p.pity = [0] * items
        tier = 0
        walked_out = set()
        rec = {"hit": {}, "moved": {}, "videos": 0, "bonus": 0, "gap": (0, 0.0)}
        hours = 0.0
        last_gain = (0, 0.0)
        for run in range(1, max_runs + 1):
            row = rows_rng.choice(rows)
            loot = run_loot(row, loot_rng, TIERS[tier])
            hours += row["minutes"] / 60.0
            if int(row["completed"]) == 1:
                walked_out.add(tier)
            bonus = bonus_gold(loot["gold"], multiplier_percent, min_gold) if run >= from_run else 0
            if bonus and (watch_rate >= 1 or (watch_rate > 0 and watch_rng.random() < watch_rate)):
                loot["gold"] += bonus
                rec["videos"] += 1
                rec["bonus"] += bonus
                hours += video_seconds / 3600.0
            for k, v in loot.items():
                p.res[k] += v * (1 - efsun_share) if k == "gold" else v
            before = sum(p.levels)
            spend(p, items)
            if sum(p.levels) > before:
                if run - last_gain[0] > rec["gap"][0]:
                    rec["gap"] = (run - last_gain[0], hours - last_gain[1])
                last_gain = (run, hours)
            best, low = max(p.levels), min(p.levels)
            for name, ok in (("one +6", best >= 6), ("one +9", best >= 9), ("set +6", low >= 6), ("set +9", low >= 9)):
                if ok and name not in rec["hit"]:
                    rec["hit"][name] = (run, hours)
            if "set +9" in rec["hit"]:
                break
            if tier_policy == "matched" and tier + 1 < len(TIERS) and tier in walked_out and \
                    _levels_power_cached(tuple(p.levels)) >= TIER_MATCHED_POWER[TIERS[tier + 1]] - 1e-9:
                tier += 1
                rec["moved"][TIERS[tier]] = run + 1
        for name in VIDEO_MILESTONES:
            rec["hit"].setdefault(name, (max_runs + 1, hours))
        records.append(rec)
    return records


def quartiles(values):
    """P25 / P50 / P90 as simulate reads them."""
    values = sorted(values)
    n = len(values)
    return values[n // 4], values[n // 2], values[int(n * 0.9)]


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


def mean_loot(rows, n=20000, seed=3, tier="forged"):
    rng = random.Random(seed)
    total = defaultdict(float)
    for _ in range(n):
        for k, v in run_loot(rng.choice(rows), rng, tier).items():
            total[k] += v
    return {k: v / n for k, v in total.items()}


def _cell(runs, hours):
    return "%d (%.1f h)" % (runs, hours)


def _sooner(base, other):
    """Runs and hours saved against the 0 % player (P50s), and the share of the runs."""
    runs, hours = base[0] - other[0], base[1] - other[1]
    share = 100.0 * runs / base[0] if base[0] else 0.0
    return "%.0f %% (%d run%s, %.1f h)" % (share, runs, "" if runs == 1 else "s", hours)


def video_report(income, args):
    """docs/58 section 4a's tables: the x1.5 video at several watch rates, on Forged and up the tiers."""
    rates = [int(x) for x in args.watch_rates.split(",")]
    policies = args.policies.split(",")
    cohorts = args.cohorts.split(",")
    label = {"forged": "stays on Forged", "matched": "moves up when matched"}
    knobs = dict(multiplier_percent=args.multiplier, min_gold=args.min_gold, from_run=args.from_run,
                 video_seconds=args.video_seconds, players=args.players)

    print("## Income a run on each tier (mean; the bonus is what a taken video adds, x%.2f, minimum %d)\n" % (
        args.multiplier / 100.0, args.min_gold))
    print("| Cohort | Tier | Completed | Minutes | Gold | Video bonus | Ember | Quicksilver | Spore | Forge Seal | Hammer |")
    print("|---|---|---|---|---|---|---|---|---|---|---|")
    for c in cohorts:
        rows = income[c]
        done = sum(r["completed"] for r in rows) / len(rows)
        minutes = sum(r["minutes"] for r in rows) / len(rows)
        for t in TIERS:
            m = mean_loot(rows, tier=t)
            gold = [r["banked"] if TIER_GOLD[t] == 1 else round(r["banked"] * TIER_GOLD[t]) for r in rows]
            bonus = sum(bonus_gold(g, args.multiplier, args.min_gold) for g in gold) / len(gold)
            print("| %s | %s | %.1f %% | %.1f | %.0f | +%.0f | %.2f | %.2f | %.2f | %.2f | %.3f |" % (
                c, t, done * 100, minutes, m.get("gold", 0), bonus, m.get("E", 0), m.get("Q", 0), m.get("S", 0),
                m.get("seal", 0), m.get("hammer", 0)))

    results = {}
    for c in cohorts:
        for pol in policies:
            for rate in rates:
                results[(c, pol, rate)] = simulate_paths(income[c], tier_policy=pol, watch_rate=rate / 100.0, **knobs)

    def p50(recs, name):
        return quartiles([r["hit"][name][0] for r in recs])[1], quartiles([r["hit"][name][1] for r in recs])[1]

    print("\n## Runs and hours to each milestone (P50: runs (hours of play, videos included); boosters, a quarter of "
          "gold to efsun)\n")
    print("| Cohort | Tiers | Watch | One +6 | One +9 | Set +6 | Set +9 | Set +9, P90 runs | First Tempered run | "
          "Videos to set +9 |")
    print("|---|---|---|---|---|---|---|---|---|---|")
    for c in cohorts:
        for pol in policies:
            for rate in rates:
                recs = results[(c, pol, rate)]
                cells = [_cell(*p50(recs, name)) for name in VIDEO_MILESTONES]
                moved = [r["moved"].get("tempered") for r in recs]
                moved_text = "-"
                if pol == "matched":
                    reached = sorted(m for m in moved if m is not None)
                    moved_text = "%d (%d %% of players)" % (reached[len(reached) // 2], 100 * len(reached) // len(recs)) \
                        if reached else "never"
                print("| %s | %s | %d %% | %s | %d | %s | %d |" % (
                    c, label.get(pol, pol), rate, " | ".join(cells), quartiles([r["hit"]["set +9"][0] for r in recs])[2],
                    moved_text, quartiles([r["videos"] for r in recs])[1]))

    base_rate = rates[0]
    print("\n## How much sooner than the %d %% player (P50 runs and hours; share of the runs)\n" % base_rate)
    print("| Cohort | Tiers | Watch | One +6 | One +9 | Set +6 | Set +9 |")
    print("|---|---|---|---|---|---|---|")
    for c in cohorts:
        for pol in policies:
            base = results[(c, pol, base_rate)]
            for rate in rates[1:]:
                recs = results[(c, pol, rate)]
                print("| %s | %s | %d %% | %s |" % (c, label.get(pol, pol), rate, " | ".join(
                    _sooner(p50(base, name), p50(recs, name)) for name in VIDEO_MILESTONES)))

    print("\n## The longest stretch between two levels gained, on the road to a +9 set (runs P50 / P90 (hours P50))\n")
    print("| Cohort | Tiers | %s |" % " | ".join("%d %%" % r for r in rates))
    print("|---|---|%s" % ("---|" * len(rates)))
    for c in cohorts:
        for pol in policies:
            cells = []
            for rate in rates:
                recs = results[(c, pol, rate)]
                q = quartiles([r["gap"][0] for r in recs])
                cells.append("%d / %d (%.1f h)" % (q[1], q[2], quartiles([r["gap"][1] for r in recs])[1]))
            print("| %s | %s | %s |" % (c, label.get(pol, pol), " | ".join(cells)))

    mults = [int(x) for x in args.multipliers.split(",")]
    sens_rates = [r for r in rates if r not in (0,)]
    sens_rates = [sens_rates[0], sens_rates[-1]] if len(sens_rates) > 1 else sens_rates
    pol = "matched" if "matched" in policies else policies[0]
    print("\n## Other multipliers (%s; P50 runs, and how much sooner than the 0 %% player)\n" % label.get(pol, pol))
    print("| Cohort | Multiplier | Watch | One +9 | Set +9 | One +9 sooner | Set +9 sooner |")
    print("|---|---|---|---|---|---|---|")
    for c in cohorts:
        base = results.get((c, pol, 0)) or simulate_paths(income[c], tier_policy=pol, watch_rate=0.0, **knobs)
        for mult in mults:
            for rate in sens_rates:
                if mult == args.multiplier and (c, pol, rate) in results:
                    recs = results[(c, pol, rate)]
                else:
                    recs = simulate_paths(income[c], tier_policy=pol, watch_rate=rate / 100.0,
                                          **dict(knobs, multiplier_percent=mult))
                one, full = p50(recs, "one +9"), p50(recs, "set +9")
                print("| %s | x%.2f | %d %% | %d | %d | %s | %s |" % (
                    c, mult / 100.0, rate, one[0], full[0], _sooner(p50(base, "one +9"), one),
                    _sooner(p50(base, "set +9"), full)))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--players", type=int, default=2000)
    parser.add_argument("--video", action="store_true",
                        help="docs/58 section 4a's tables instead of docs/54's: the result screen's gold video on the tiers")
    parser.add_argument("--watch-rates", default="0,30,60,100", help="percent of offered videos taken; 100 = skip owner")
    parser.add_argument("--multiplier", type=int, default=VIDEO_MULTIPLIER_PERCENT, help="the video's gold, percent")
    parser.add_argument("--min-gold", type=int, default=VIDEO_MIN_GOLD)
    parser.add_argument("--from-run", type=int, default=VIDEO_FROM_RUN)
    parser.add_argument("--video-seconds", type=float, default=VIDEO_SECONDS)
    parser.add_argument("--policies", default="forged,matched", help="forged (stays) and/or matched (moves up)")
    parser.add_argument("--cohorts", default="careful,first-card,clumsy")
    parser.add_argument("--multipliers", default="125,150,175,200", help="the sensitivity table's multipliers, percent")
    args = parser.parse_args()
    income = load_income()
    if args.video:
        video_report(income, args)
        return
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
