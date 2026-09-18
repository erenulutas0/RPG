# Experiments: why kiting wins, and what would change it — 2026-09-18

Part 1 measures a slower weapon cadence while the hero walks. Part 2 measures the two levers Part 1 pointed at:
enemy speed and enemy reach. Part 3 measures a graded speed set, where the heavy enemies stay slower than the
light ones, and separates what the speed change does from what the damage compensation does. Parts 1 to 3 change
nothing in the game. Part 4 builds the two development floors that let the candidate be *played* beside today's
game on the phone; it adds development-only content and still changes no shipped data.

## Part 1 — a slower weapon cadence while the hero walks

**Nothing in the game changed.** This is a measurement of a proposal (Fable's design review, "walking cadence"), run
entirely in a scratch copy of `DescentSimulation`. No runtime script, scene, asset, art, HUD, camera, profile, telemetry,
upgrade, quest or wave rule was touched, and no APK was installed. Read it as evidence for a decision that has not been
taken.

## The proposal, stated exactly

On a frame where the hero **really changed position**, its weapon's cooldown advances at a factor of real time:

```
weapon.Tick(moved ? deltaTime * cadence : deltaTime)
```

`moved` is displacement, not intent: a steer below the dead band, or one held into the rim, moves nothing and is
therefore not walking. Nothing else is scaled — the Forge Burst, the enemies' weapons and the relic all keep real time.
`cadence = 1` is the game as it is today.

## How it was measured

A copy of `Tests/Support/DescentSimulation.cs` in the scratchpad, compiled against the repository's own pure combat,
economy and progression sources (36 files), with three changes: the hero walks before its weapon ticks, so the frame
knows whether it moved; the tick takes the factor; and the result carries two more numbers (attacks made, seconds spent
having really moved). Moving the walk earlier is behaviour-neutral at cadence 1 — a tick only drains a cooldown and
nothing between the two reads the hero's position — and that is verified below, not assumed.

- **Encounters.** The authored Descent (Ember Halls → Quicksilver Vaults, Mend on floor 1, card slot 0, no relic, the
  Forge Burst fired whenever ready, as the balance baseline uses it) and the development proof floors: `DensityProof`
  **Trailing** (two Grunts then eight Cinder Mites) and **Mites** (ten Cinder Mites), both without the burst, as the
  scene parity cases use them.
- **Routes.** `stand` (no route at all), `kite` (`KiteRoute`), `stop-go` (kite for 1 s, stand for 1 s) and, beyond the
  asked scope, `backoff` (back away while a threat is inside the hero's reach, stand and fight once the nearest one is
  0.6 further away, with hysteresis) as a ceiling for adaptive play.
- **Cadences.** 1.00, 0.85, 0.70, 0.50. Everything else is identical in every cell; 144 runs plus 36 control runs.
- **Runner.** The pure .NET runner. `22` records that the Editor's Mono and this runner end 14 of 36 crowded or walking
  runs differently, so exact values below belong to this runtime; the trends do not depend on it.

### The 1.00 reference reproduces the game today

Every row this experiment shares with the measurement recorded for `c94856c` (three encounters × three weapons ×
{standing, kiting}, 18 rows) matches **bit for bit**: health left, damage taken, fight seconds, kills and gold. The
reordering is therefore neutral and the harness is measuring the current game, not a variant of it.

### The standing baseline cannot move

All 36 standing cells (3 encounters × 3 weapons × {0.85, 0.70, 0.50, control}) are identical to their 1.00 rows in
health, damage, fight seconds and attacks. A hero that never moves never meets the rule, so no pinned balance target in
`21`/`22` is at risk from this change — by construction, not by tuning.

### The noise floor, measured

A control pass re-ran every cell with **no rule change at all** and the hero's walking speed nudged from 2.5 to
2.5000005 (four parts in ten million — no player could feel it):

| Encounter | Routes that moved | Worst change in damage taken |
|---|---|---|
| Proof floors (Trailing, Mites) | `backoff` only | 7.2 (Sword), 1.7 (Daggers); every `stand`, `kite`, `stop-go` cell identical, one fight 0.03 s longer |
| Authored Descent | `stop-go`, `kite`, `backoff` | **28.5** (Daggers stop-go), 13.0 (Daggers backoff), 8.5 (Staff stop-go), 6.1 (Sword stop-go), 5.6 (Daggers kite) |

So a two-floor run with a walking hero is chaotic: a change nobody could perceive moves the damage it takes by up to 28
points. Descent numbers below are only meaningful as trends across cells; the proof floors reproduce exactly and carry
the detail.

## Results

Damage taken / fight seconds, at cadence 1.00 · 0.85 · 0.70 · 0.50. The hero starts every run at 100 health.

**Ten-enemy proof encounter** (two Grunts, eight Cinder Mites):

| Weapon | Route | 1.00 | 0.85 | 0.70 | 0.50 |
|---|---|---|---|---|---|
| Sword | stand | 48.6 / 11.2 | 48.6 / 11.2 | 48.6 / 11.2 | 48.6 / 11.2 |
| Sword | kite | 18.8 / 12.2 | 17.1 / 15.0 | 39.2 / 15.2 | 34.2 / 25.0 |
| Sword | stop-go | 45.8 / 13.0 | 49.7 / 13.8 | 43.6 / 13.9 | 55.8 / 15.1 |
| Sword | backoff | 24.3 / 12.8 | 28.7 / 15.6 | 33.1 / 18.5 | 23.2 / 26.1 |
| Staff | stand | 35.9 / 8.0 | 35.9 / 8.0 | 35.9 / 8.0 | 35.9 / 8.0 |
| Staff | kite | 19.3 / 7.3 | 12.7 / 8.3 | 16.6 / 9.8 | 26.5 / 13.1 |
| Staff | stop-go | 20.4 / 7.3 | 23.7 / 7.8 | 20.4 / 8.4 | 26.5 / 9.4 |
| Staff | backoff | 15.5 / 8.0 | 18.8 / 8.9 | 19.3 / 10.6 | 34.2 / 14.1 |
| Daggers | stand | 47.5 / 14.0 | 47.5 / 14.0 | 47.5 / 14.0 | 47.5 / 14.0 |
| Daggers | kite | 27.1 / 31.1 | 33.7 / 33.7 | 62.9 / 36.2 | 72.9 / 42.4 |
| Daggers | stop-go | 51.9 / 16.7 | 49.1 / 17.7 | 46.9 / 18.5 | 56.3 / 19.2 |
| Daggers | backoff | 48.6 / 17.0 | 35.9 / 17.4 | 33.7 / 30.3 | 70.1 / 42.1 |

**Authored Descent** (both floors; every cell cleared both floors unless marked):

| Weapon | Route | 1.00 | 0.85 | 0.70 | 0.50 |
|---|---|---|---|---|---|
| Sword | stand | 149.6 / 66.4 | 149.6 / 66.4 | 149.6 / 66.4 | 149.6 / 66.4 |
| Sword | kite | 55.0 / 66.5 | 48.1 / 104.5 | 82.3 / 89.3 | 107.4 / 117.6 |
| Sword | stop-go | 95.1 / 59.4 | 116.1 / 60.8 | 124.5 / 60.2 | 144.8 / 65.1 |
| Sword | backoff | 73.0 / 88.6 | 76.6 / 89.3 | 87.5 / 92.6 | 115.7 / 120.3 |
| Staff | stand | 151.1 / 69.9 | 151.1 / 69.9 | 151.1 / 69.9 | 151.1 / 69.9 |
| Staff | kite | 15.9 / 49.2 | 27.8 / 59.1 | 38.9 / 66.9 | 84.5 / 72.8 |
| Staff | stop-go | 95.3 / 57.3 | 99.5 / 57.9 | 102.8 / 62.4 | 137.2 / 68.2 |
| Staff | backoff | 36.0 / 66.2 | 50.4 / 68.7 | 55.4 / 71.7 | 137.0 / 91.6 |
| Daggers | stand | 146.3 / 65.1 | 146.3 / 65.1 | 146.3 / 65.1 | 146.3 / 65.1 |
| Daggers | kite | 107.5 / 132.4 | 115.7 / 129.7 | 128.5 / 151.4 | 131.3 / 192.2 |
| Daggers | stop-go | 148.3 / 77.5 | 163.7 / 73.5 | 169.3 / 75.6 | **died, room 6.1** / 81.6 |
| Daggers | backoff | 128.8 / 118.0 | 144.0 / 110.0 | 129.3 / 122.8 | **died, room 6.1** / 164.3 |

Kills, gold and level never changed: every one of the 142 surviving runs killed all ten (proof floors) or all sixty
(Descent), banked the same gold (10 / 0 / 273) and reached the same level (2 / 1 / 11). The Mites floor follows the same shape and is in the raw data, not repeated here.

### How much of kiting's advantage the penalty removes

Damage taken standing ÷ damage taken kiting, over the nine encounter × weapon cells (higher means kiting is safer):

| Cadence | Median | Mean | Cells where kiting is worse than standing |
|---|---|---|---|
| 1.00 | 1.89 | 2.84 | 0 of 9 |
| 0.85 | 2.27 | 2.43 | 0 of 9 |
| 0.70 | 1.24 | 1.63 | 1 of 9 |
| 0.50 | 1.11 | 1.20 | 2 of 9 |

### What it costs in time

Fight seconds as a multiple of the same cell at 1.00 (Sword / Staff / Daggers):

| Encounter | Route | 0.85 | 0.70 | 0.50 |
|---|---|---|---|---|
| Proof | kite | 1.23 / 1.13 / 1.08 | 1.25 / 1.33 / 1.16 | 2.06 / 1.78 / 1.36 |
| Proof | stop-go | 1.06 / 1.05 / 1.06 | 1.08 / 1.14 / 1.11 | 1.17 / 1.27 / 1.15 |
| Descent | kite | 1.57 / 1.20 / 0.98 | 1.34 / 1.36 / 1.14 | 1.77 / 1.48 / 1.45 |
| Descent | stop-go | 1.02 / 1.01 / 0.95 | 1.01 / 1.09 / 0.98 | 1.10 / 1.19 / 1.05 |

Standing is 1.00 everywhere by construction.

### Attacks and failures

Attacks made barely move: Descent Sword kiting 89 → 93 → 88 → 83, Staff 79 → 82 → 78 → 67, Daggers 123 → 126 → 121 →
112. The penalty mostly drains cooldown time the hero was spending with nothing in reach anyway, so it lands on fight
length and damage taken rather than on how often the hero swings.

Failed runs: **2 of 144**, both Daggers on the Descent at 0.50 (stop-go and back-off), both killed by the floor-1 boss
in room 6.1. Every other run finished.

## What the numbers say

1. **0.85 is not distinguishable from doing nothing.** The median ratio moves the wrong way (1.89 → 2.27) and four
   cells improve for the kiting hero. That is route chaos and control-level noise, not an effect.
2. **0.70 halves kiting's damage advantage** (median 1.89 → 1.24) and costs a kiting player 14–36% more time on the
   Descent. It leaves every run alive.
3. **0.50 nearly removes the advantage** (median 1.11, two cells where kiting is worse than standing), at 36–106%
   more time, and it is the only setting that killed runs.
4. **The ordering barely changes.** Kiting is still the lowest-damage route in all 12 Descent cells at every cadence,
   and in 7 of 12 proof cells. Even at 0.50 a Descent player is better off kiting. The penalty taxes kiting; it does not
   dethrone it, because kiting wins by not being hit at all — the hero outranges every enemy (1.8–2.4 against 1.5–1.7)
   and outruns all of them (2.5 against 0.9–2.2, the Mite being the fastest). Slowing the hero's output gives the
   enemies neither reach nor speed.
5. **One constant hits the three weapons very differently.** At 0.50 the Staff still takes 1.79× less damage kiting on
   the Descent, while the Daggers — the shortest reach — are pushed below standing on the proof floor and die on the
   Descent. A global number removes movement as an option for the short-range weapon first.
6. **Stop-and-go is not rewarded.** The rhythm the proposal hopes for ("drag, stop, hit") is worse at every cadence than
   pure kiting on the Descent, and its damage rises with the penalty as well. The adaptive back-off route wins only on
   the proof floor at 0.70–0.50, and it is one of the two runs that died.

## Recommendation

**Do not ship a global walking-cadence penalty now.** 0.85 buys nothing measurable; 0.50 is strong enough to matter but
makes movement a trap for the short-range weapon and kills runs; 0.70 is the only defensible value, and even there the
answer to "what should I do?" is still "keep kiting", only slower. The measurement says the dominance comes from the
hero's reach and speed advantage, so the cheapest honest next experiments are the ones that change that, each measurable
in this same harness without touching the game:

1. Enemy speed against the hero's 2.5 (the Mite is already 2.2; the Grunt at 1.6 is what makes kiting free).
2. Enemy reach against the hero's 1.8–2.4.
3. A reward for standing rather than a tax on walking (for example a damage bonus after a second without moving), which
   at least cannot make a weapon unplayable while moving.

Part 2 below measures 1 and 2. Number 3 is still untested.

If the owner still wants the tax tried in the game, the smallest defensible version is 0.70 **scaled per weapon or by
reach**, not one global constant, and it should be judged on the proof floors, where the simulation is exactly
reproducible, rather than on a two-floor run whose noise floor is ±6–28 damage.

## Assumptions about player feel — not measured

Kept separate from the numbers above on purpose. None of this is evidence.

- A hero that stops striking while the thumb drags may read as unresponsiveness rather than as a trade, especially with
  no animation or HUD cue for it. Nothing in this experiment can tell the two apart.
- The routes are scripts with perfect information, not players: `KiteRoute` knows every enemy's exact reach and re-plans
  every frame. A human would kite worse in some ways and better in others, and might find the policy that dodges the tax.
- Longer fights are not automatically worse: floors currently run 45–54 s against the GDD's 3–4 minutes (`23`), so the
  extra time a kiting player spends may read as pacing rather than padding — or as a slog. Only a play session tells.
- The proposal's hoped-for "stop to strike" rhythm assumes players notice the rule. With two upgrade cards and no
  feedback for the penalty, they may simply feel weaker while moving.

## Limits of this experiment

- **Existing parity tests prove nothing about this rule.** They pin the current behaviour; the scene has no equivalent
  change, and the walking-hero parity cases were written for cadence 1. Implementing this would need its own scene
  change and its own parity evidence.
- The harness is a copy: it does not run in the game, and the scene's frame order was mirrored by hand.
- One hero, three weapons, no relic, card slot 0, Mend on floor 1, the burst automatic on the Descent and absent on the
  proof floors. Other loadouts were not swept.
- Four routes, of which three are the asked ones; all are policies, not players.
- The .NET runner only; `22` records that crowded fights end differently on the Editor's Mono, and IL2CPP on the phone
  is a third runtime.
- Raw data: 144 experiment runs and 36 control runs, kept in the session scratchpad
  (`scratchpad/cadence/cadence.tsv`, with `build_harness.py`, `Program.cs` and `analyse.py` that produced it). It is not
  in the repository; rebuilding it from this description takes a few minutes.

---

## Part 2 — enemy speed and enemy reach

Same harness, same encounters, same routes, cadence back at 1.00. The hero is untouched: speed 2.5, weapon ranges 1.8 /
2.1 / 2.4, damage and cadence as they are. Two knobs move, one at a time:

- **Speed.** Every enemy's walking speed × a multiplier, or raised to a least value ("min 2.5" means no enemy walks
  slower than 2.5, the hero's own speed). Today: Tank 0.9, Warden 1.0, Captain 1.4, Grunt 1.6, Mite 2.2, Runner 3.0.
- **Reach.** A flat bonus on every enemy's reach. In the data one number is both how far an enemy strikes from and where
  it stops walking (`PackMotion` stops at reach − 0.1), so the bonus moves both, as it would in the game.

The routes' view of the world stays honest: a kiting hero reads each enemy's real reach, so `KiteRoute` keeps its
distance from the longer-reaching ones.

### Stage 1: what the levers cost as they are

The six standing Descent paths the balance tests pin (each cell is damage-card-first / speed-card-first, health left at
the end of floor 2, with the burst and Mend):

| Variant | Sword | Staff | Daggers |
|---|---|---|---|
| today | 30.4 / 29.2 | 28.9 / 39.2 | 33.7 / 22.4 |
| speed ×1.15 | 13.7 / 29.0 | **died 3.1** / 36.6 | 28.9 / 18.2 |
| speed ×1.30 | 11.7 / **died 3.1** | 9.8 / 14.2 | 27.2 / 12.5 |
| speed min 2.0 | 10.4 / 1.8 | **died 3.1** / **died 6.1** | 11.7 / **died 3.1** |
| reach +0.2 | 16.7 / 12.0 | **died 3.1** / 6.5 | **died 2.3** / **died 2.3** |
| reach +0.4 | 4.5 / **died 3.1** | **died 3.1** / **died 3.1** | **died 2.3** / **died 2.3** |

Unlike the cadence penalty, which a standing hero never meets, both of these levers change the authored balance on the
first step: ×1.15 already kills a pinned path. So the interesting question cannot be asked from these numbers — a harder
game is not the same as a game where movement matters less.

### Stage 2: the same difficulty, only the mobility changed

For each variant the enemies' damage was scaled back until the six standing paths land where they land today, searched
over a grid from ×0.40 to ×1.00 and scored by the worst path's distance from today's health, requiring all six to clear
both floors.

| Variant | Enemy damage | Worst standing path vs today | The other pinned claims |
|---|---|---|---|
| today | ×1.00 | 0.0 | hold |
| speed ×1.15 | ×0.94 | 8.9 | hold |
| speed ×1.30 | ×0.88 | 11.9 | **broken** (Temper now clears floor 2) |
| speed min 2.0 | ×0.80 | 13.9 | **broken** (both Temper paths clear) |
| speed min 2.5 | ×0.82 | 12.8 | hold |
| speed min 2.8 | ×0.82 | 10.2 | hold |
| light min 2.5 (only enemies at or under 60 health) | ×0.98 | 16.0 | hold |
| light min 2.8 | ×0.92 | 9.8 | hold |
| reach +0.2 | **none** | — | — |
| reach +0.4 | **none** | — | — |
| speed min 2.5 + reach +0.2 | **none** | — | — |

"The other pinned claims" are the rest of what `DescentTests` asserts, checked on the Sword: tempering on floor 1 must
still be the trap that cannot descend, and Counterweight and Second Wind must still rescue it.

**Enemy reach cannot be compensated at all.** No damage scale down to ×0.40 keeps the six standing paths alive, and the
reason is mechanical, not a matter of tuning: enemies stop walking at reach − 0.1, so at +0.2 the Tank and the Warden
stand exactly 1.8 from the hero — the Daggers' whole range — and at +0.4 they stand at 2.0, outside it. A standing
Daggers hero cannot answer them at all. Enemy reach is not a difficulty dial; it is a relation to each weapon's range,
and raising it globally breaks the weapon that has the least.

### With difficulty held, does movement still win?

Damage taken standing ÷ damage taken kiting, median over the three weapons (higher means kiting is safer). Every run in
this table cleared; none died.

| Variant | Proof floor | Authored Descent |
|---|---|---|
| today | 1.86 | 2.72 |
| speed ×1.15 | 2.23 | 2.33 |
| speed ×1.30 | 1.91 | 2.06 |
| speed min 2.0 | 2.29 | 2.58 |
| **speed min 2.5** | **1.69** | **1.76** |
| speed min 2.8 | 2.05 | 1.56 |
| light min 2.5 | 1.69 | 2.75 |
| light min 2.8 | 2.05 | 1.87 |

The ten-enemy proof floor holds only Grunts and Mites, so "light only" and "everything" are the same variant there; only
the Descent, which has the Tank, the Captain and the Warden, can tell them apart.

Two things stand out.

**Nothing below the hero's own speed matters.** ×1.15, ×1.30 and a floor of 2.0 move the Descent ratio from 2.72 to
2.06–2.58, which is inside the chaos band Part 1 measured, and two of them break the pinned Temper claims. A floor at
2.5 — the hero's own speed — is where kiting stops being free: 2.72 → 1.76. This is not a coincidence of tuning. Below
2.5 the hero can always open a gap and keep it; at 2.5 it cannot.

**The slow, heavy enemies are the ones being kited.** Raising only the light enemies to 2.5 leaves the Descent at 2.75,
no better than today, while raising everything gives 1.76. The Tank at 0.9, the Warden at 1.0 and the Captain at 1.4 are
exactly the enemies a player can walk away from for ever.

### What it costs in time, and where the hero is caught

Fight seconds, standing / kiting, on the authored Descent:

| Variant | Sword | Staff | Daggers |
|---|---|---|---|
| today | 66.4 / 66.5 | 69.9 / 49.2 | 65.1 / 132.4 |
| speed min 2.5 | 54.5 / 49.2 | 51.5 / 45.4 | 54.3 / 59.1 |
| speed min 2.8 | 52.5 / 44.8 | 49.9 / 43.2 | 52.7 / 55.3 |
| light min 2.5 | 62.7 / 81.9 | 59.8 / 46.9 | 62.6 / 98.2 |

Faster enemies make fights **shorter**, standing and kiting alike — the opposite of the cadence penalty, which stretched
a kiting Descent by 15–106%. The Daggers' 132-second kiting run, the worst pacing case in the whole project, becomes 59
seconds.

On the proof floor at min 2.5 the kiting Sword spends 60% of the fight moving (100% today) and the Daggers 66%: they are
forced to turn and fight. On the Descent the moving share stays at about 1.00 in every variant — the arena is wide and
authored packs are five or fewer, so the hero can still circle; it simply takes half as much of its advantage with it.

## Recommendation for Part 2

1. **Do not raise enemy reach.** It is not a difficulty knob. At +0.2 the Tank and the Warden already stand at the edge
   of the Daggers' range and a standing Daggers hero cannot fight back; no damage compensation rescues it. If enemy
   reach is ever touched, it has to move together with the weapon ranges, and the Daggers set the floor.
2. **Enemy speed is the lever that works, but only at the hero's own speed.** `speed min 2.5` with enemy damage ×0.82 is
   the one variant that cuts kiting's advantage (Descent 2.72 → 1.76, proof 1.86 → 1.69), keeps all six standing paths
   within 13 health of today, keeps the Temper and relic claims, kills no run, and shortens every fight.
3. **Its price is the heavy enemies' identity.** At that floor the Tank walks 2.5 instead of 0.9 and every enemy moves
   between 2.5 and 3.0; "slow and heavy" stops existing as a design idea, and the light-only version that preserves it
   does nothing (2.75). Whoever decides this is choosing between a kiting answer and an enemy archetype, not between two
   numbers.
4. **Against Part 1.** Enemy speed changes what the hero can *achieve* — it can no longer open a gap — while the cadence
   penalty taxes what it *does*. Speed shortens fights, cadence lengthens them; speed needs a damage re-tune and costs
   an archetype, cadence costs nothing in balance but does not change the answer. If one of the two is to be tried in
   the game, the evidence favours the speed floor.
5. **If the heavies must stay slow**, this experiment has no answer inside the "no new enemy systems" constraint: the
   next candidates are a closing dash, a ranged attack or ground the hero must leave, all of them new behaviour and all
   untested here.

## Limits of Part 2

- The compensation is coarse and was fitted on six standing Mend paths only: the worst path still sits 9–16 health from
  today, and the Temper and relic claims were checked on the Sword alone.
- Scaling every enemy's damage by 0.82 is itself a balance change; in the game it would be a data pass over the enemy
  assets, with its own numbers to re-pin.
- Part 1's control applies with more force here: an imperceptible speed nudge already moved a walking Descent's damage
  by up to 28 points, and speed is this part's variable. Treat single cells as illustration and the column trends as the
  finding.
- The proof floor cannot distinguish a light-only floor from a global one, because every enemy on it is light.
- Everything is the .NET runner, one hero, three weapons, no relic in the matrix, card slot 0, no chests, and scripted
  routes rather than players. The scene was not touched and nothing here was verified in Unity.

---

## Part 3 — a graded speed set, and speed separated from damage

Part 2 ended with a choice the owner has to make: the only speed lever that worked was a flat floor at the hero's own
speed, which costs the Tank, the Captain and the Warden their slowness. This part asks whether a **graded** set — the
heavy enemies still the slowest, but none of them far below the hero — does the same work, and it measures the speed
change and the damage compensation separately, so it is visible which half of the change carries the effect.

Hero untouched: speed 2.5, ranges 1.8 / 2.1 / 2.4, cadence 1.00, no relic in the matrix. Today's speeds are Tank 0.9,
Warden 1.0, Captain 1.4, Grunt 1.6, Mite 2.2, Runner 3.0.

| Variant | Tank | Warden | Captain | Grunt | Mite | Runner | Keeps the heavy-slow order |
|---|---|---|---|---|---|---|---|
| today | 0.9 | 1.0 | 1.4 | 1.6 | 2.2 | 3.0 | yes |
| graded 2.0 | 2.0 | 2.0 | 2.2 | 2.3 | 2.5 | 3.1 | yes |
| graded 2.3 | 2.3 | 2.3 | 2.4 | 2.5 | 2.6 | 3.2 | yes |
| graded 2.5 | 2.5 | 2.5 | 2.6 | 2.7 | 2.8 | 3.2 | yes |
| flat 2.5 | 2.5 | 2.5 | 2.5 | 2.5 | 2.5 | 3.0 | no |

Each variant was run twice: **speed only**, and **speed + damage**, where the enemies' damage is scaled back until the
six standing Descent paths land closest to where they land today (grid from ×0.40 to ×1.00, all six required to clear).

### Speed alone is only a difficulty increase

| Variant | Standing Descent, health left (damage card / speed card) | | |
|---|---|---|---|
| | Sword | Staff | Daggers |
| today | 30.4 / 29.2 | 28.9 / 39.2 | 33.7 / 22.4 |
| graded 2.0, speed only | 2.1 / 4.0 | **died / died** | 14.4 / **died** |
| graded 2.3, speed only | **died / died** | **died / died** | 7.8 / **died** |
| graded 2.5, speed only | **died / died** | **died / died** | **died / died** |
| flat 2.5, speed only | **died / died** | 9.4 / **died** | 1.0 / 2.3 |

Seven standing runs died across the matrix, all of them on the authored Descent, and every pinned claim broke — with
speed alone even Counterweight cannot rescue a tempered floor 1. Faster enemies without a damage pass do not make
movement optional; they make **standing** impossible, which is the opposite of the goal.

### The compensation pays for the speed; it does not change the answer

| Variant | Enemy damage | Standing Descent after compensation (Sword / Staff / Daggers, damage card first) | Pinned claims |
|---|---|---|---|
| today | ×1.00 | 30.4 / 28.9 / 33.7 | hold |
| graded 2.0 | ×0.80 | 37.7 / 27.1 / 42.0 | **broken** (Temper clears) |
| graded 2.3 | ×0.78 | 29.6 / 30.2 / 44.5 | hold |
| graded 2.5 | ×0.78 | 31.2 / 30.2 / 38.4 | **broken** (Temper clears) |
| flat 2.5 | ×0.82 | 25.3 / 40.1 / 33.2 | hold |

With no relic and no death, scaling every enemy's damage by a constant scales the hero's damage taken by exactly that
constant and changes nothing else: enemy health, positions and the hero's kills are untouched, and the hero's health
never feeds back into the fight. The measurement shows it exactly — on the proof floor the standing-to-kiting ratios are
**identical** in both modes (graded 2.3 Sword 1.54 either way; graded 2.0 Staff 2.62 either way), and the damage taken
falls by precisely the scale (73.4 → 58.7 is ×0.80). So: **the design effect comes entirely from the speed; the damage
cut is what makes it affordable.**

### What the graded set does to kiting, per weapon

Damage taken, standing / kiting / back-off, with the compensation applied, and the standing-to-kiting ratio.

**Authored Descent**

| Variant | Sword | Staff | Daggers |
|---|---|---|---|
| today | 149.6 / 55.0 / 73.0 — **2.72** | 151.1 / 15.9 / 36.0 — **9.50** | 146.3 / 107.5 / 128.8 — **1.36** |
| graded 2.0 | 142.3 / 68.4 / 95.5 — 2.08 | 152.9 / 60.5 / 94.9 — 2.53 | 132.4 / 97.5 / 105.5 — 1.36 |
| graded 2.3 | 150.4 / 75.3 / 87.2 — **2.00** | 149.8 / 72.4 / 90.1 — **2.07** | 134.3 / 105.4 / 110.4 — **1.27** |
| graded 2.5 | 148.8 / 80.4 / 96.7 — 1.85 | 149.8 / 94.7 / 121.7 — 1.58 | 141.6 / 113.8 / 123.8 — 1.24 |
| flat 2.5 | 154.7 / 82.8 / 78.0 — 1.87 | 139.9 / 79.6 / 116.0 — 1.76 | 146.8 / 102.3 / 109.3 — 1.43 |

**Ten-enemy proof encounter**

| Variant | Sword | Staff | Daggers |
|---|---|---|---|
| today | 48.6 / 18.8 / 24.3 — 2.59 | 35.9 / 19.3 / 15.5 — 1.86 | 47.5 / 27.1 / 48.6 — 1.75 |
| graded 2.0 | 58.7 / 26.1 / 33.1 — 2.25 | 39.3 / 15.0 / 20.3 — 2.62 | 70.7 / 40.6 / 38.4 — 1.74 |
| graded 2.3 | 54.2 / 35.3 / 34.9 — 1.54 | 36.6 / 14.6 / 17.6 — 2.50 | 54.2 / 34.4 / 34.9 — 1.58 |
| graded 2.5 | 58.1 / 30.6 / 31.9 — 1.90 | 36.6 / 22.4 / 22.8 — 1.63 | 65.5 / 43.9 / 36.2 — 1.49 |
| flat 2.5 | 62.9 / 47.5 / 36.7 — 1.32 | 38.5 / 15.4 / 23.5 — 2.50 | 69.7 / 41.2 / 40.3 — 1.69 |

Per weapon, which is how the owner asked for it:

- **Sword.** The clearest case. On the Descent 2.72 → 2.00 (graded 2.3); kiting's damage nearly doubles, 55 → 75, while
  standing stays where it was. Every graded step helps.
- **Staff.** The extreme case today: kiting a two-floor Descent costs it 15.9 damage against 151 standing, a ratio of
  9.5. Every variant collapses that to 1.6–2.5. This is the single largest change in the whole experiment, and it is the
  build the owner most often plays. On the ten-enemy floor the Staff goes the other way (1.86 → 2.50 at graded 2.3):
  enemies that converge faster bunch up, and an area weapon retreating from a tight bunch kills it sooner. Faster
  enemies do not punish an area weapon in a small room.
- **Daggers.** Barely moves: 1.36 → 1.27 on the Descent, 1.75 → 1.58 on the proof floor. It never had much kiting
  advantage to take away — reach 1.8 against enemy reaches of 1.5–1.7 leaves almost no safe band — so this lever neither
  helps nor hurts it. Whatever is done for the Daggers has to come from somewhere else.

### Are there still openings to stand and strike?

The back-off route stands whenever the nearest threat is further than reach + 0.6 and retreats when one is inside reach:
its moving share is a direct answer to "can I find a moment to stop, or am I running the whole fight?".

| Where | today | graded 2.3 | flat 2.5 |
|---|---|---|---|
| Descent, Sword / Staff / Daggers | 0.65 / 0.49 / 0.70 | 0.59 / 0.66 / 0.57 | 0.60 / 0.67 / 0.63 |
| Proof floor, Sword / Staff / Daggers | 0.89 / 0.76 / 0.49 | 0.70 / 0.84 / 0.61 | 0.70 / 0.84 / 0.56 |

The hero still spends a third to a half of every fight standing in all of them; no variant turns the run into constant
running. On the ten-enemy floor the *kiting* hero is the one that gets pinned: its moving share falls from 1.00 to 0.66
(Daggers) and 0.60–0.88 (Sword), meaning it is forced to turn and fight.

### Time

Fight seconds, standing / kiting, on the Descent, with compensation: today 66.4 / 66.5 (Sword), 69.9 / 49.2 (Staff),
65.1 / **132.4** (Daggers) → graded 2.3: 56.8 / 48.3, 58.8 / 46.2, 53.7 / **62.5**. Every variant shortens every fight,
and the Daggers' 132-second kiting run — the worst pacing case in the project — halves.

## Recommendation after Part 3

**`graded 2.3` with enemy damage ×0.78 is the candidate to feel on the phone.** It is the only variant that keeps the
heavy-slow ordering *and* the pinned balance claims, it takes the Staff's 9.5 down to 2.1, it shortens every fight, and
it leaves the hero a third to a half of each fight standing. `flat 2.5` is the stronger version of the same lever but
flattens the archetypes; `graded 2.0` and `graded 2.5` both break the Temper claim.

Two things this measurement cannot decide, and which the phone has to answer:

1. Whether enemies at 2.3–2.6 still *read* as different from one another, or whether the Tank stops being a Tank.
2. Whether being caught more often feels like pressure or like helplessness — the ratios say kiting is still worth doing
   (2.0 on the Descent), but a number cannot tell how it plays.

The damage pass (×0.78) is not a free rider: it is a data change over six enemy assets that would move every pinned
balance number, so if the phone likes the feel, the slice after it is a rebalance with its own numbers to re-pin.

## Limits of Part 3

- The same chaos band applies: Part 1's control moved a walking Descent's damage by up to 28 points with an
  imperceptible speed nudge, and speed is this part's variable. Single cells are illustration; the column trends and the
  per-weapon direction are the finding.
- The compensation is coarse (worst standing path 11.0–12.8 health from today) and was fitted on the six standing Mend
  paths; the Temper and relic claims were checked on the Sword alone.
- The graded sets are one guess each at a shape, not a search over the space; only the Runner keeps a speed above the
  hero in every one of them.
- Routes are scripted policies with perfect information, not players, and the back-off route's thresholds
  (reach, reach + 0.6) are a guess at what a player would do.
- The .NET runner, one hero, three weapons, no chests, and nothing verified in Unity or on a device.

---

## Part 4 — two arms to play, not to read

Part 3 ended with two questions a simulation cannot answer: whether enemies at 2.3–2.6 still read as different
archetypes, and whether being caught more often feels like pressure or like helplessness. Both are answered by playing,
so this part builds the two arms and states what to expect from them.

### What was built

Two development floors, reached the way the density proof floor is reached — a floor id in
`<profile folder>/development/start-floor.txt`, read only in the Editor and in a development build:

| | Arm A — `floor_kite_arm_a` | Arm B — `floor_kite_arm_b` |
|---|---|---|
| On screen | Kite Test A | Kite Test B |
| Walking speeds | today's: Tank 0.9, Warden 1.0, Captain 1.4, Grunt 1.6, Mite 2.2, Runner 3.0 | the candidate: 2.3, 2.3, 2.4, 2.5, 2.6, 3.2 |
| Enemy damage rate | 0.92, Ember Halls' own | 0.7176, which is 0.92 × the measured 0.78 |
| Everything else | identical | identical |

The arms share one shape — Approach, Heavy Ground, The Post, The Forge, Warden Gate — eighteen enemies over six waves,
1,135 enemy health, 99 experience (six level-ups), and a forge room offering only Mend, so the visit is not a decision
that can differ between the arms. Arm B's enemies are six new definitions that copy their authored twins and change one
field, `_moveSpeed`; they keep the same health, rewards, weapon assets and prefabs, so the arms cannot differ in reach,
rhythm, damage, look or experience. The compensation is carried by the floor's damage rate rather than by copied weapons,
because `FloorScaling` applies it as one uniform factor to every enemy — arithmetically the same ×0.78, in one authored
number instead of six.

Nothing shipped moved: `Data/Enemies`, `Data/Weapons`, the authored floors, the scene, the HUD, the camera and the art
are untouched, and a release build returns the authored floor before it reads the file at all.

### What the simulation expects from them

Same harness as Parts 1–3, the hero at 100 health with the burst and no relic, the left upgrade card taken at every
level-up and Mend taken at the forge. Damage taken, and health left at the end.

| | Arm A: stand / kite / back-off | Arm B: stand / kite / back-off | stand ÷ kite |
|---|---|---|---|
| Sword | 108.7 / 55.2 / 54.1 | 103.8 / 73.2 / 70.6 | **1.97 → 1.42** |
| Staff | 118.7 / 40.3 / 51.3 | 121.0 / 78.8 / 87.8 | **2.95 → 1.54** |
| Daggers | 98.3 / 59.6 / 94.9 | 97.3 / 69.3 / 80.5 | **1.65 → 1.40** |

Standing is as hard on both arms — health left 31.3 / 21.3 / 41.7 on A against 36.2 / 19.0 / 42.7 on B — which is what
the damage compensation is for, and it holds here even though it was fitted on the Descent, not on this floor. What
changes is the price of walking away: on arm A a kiting Staff finishes the whole floor at **99.7 health**, effectively
untouched; on arm B the same route ends at 61.2. The Daggers' pacing changes as much: a kiting run takes **104.6 s** on
arm A and **32.1 s** on arm B.

Both arms are survivable standing, and only because of the Mend: measured without the forge room, a standing Sword and
a standing Staff die to the Warden on both arms and the Daggers finish under 3 health. The back-off route, which stops
whenever the nearest enemy is further than reach + 0.6, still spends 14–34 % of the fight standing on both arms.

### How to play them

1. Back up `profile.json`, `profile.json.bak` and the two telemetry files; install; the install ends any open run.
2. Write `floor_kite_arm_a` into `development/start-floor.txt`, force-stop, launch, play the floor.
3. Write `floor_kite_arm_b`, force-stop, launch, play it again with **the same weapon, the same relic and the same
   upgrade card each level-up**. Both arms offer the same cards at the same moments, so this is possible exactly.
4. Delete the start-floor file, force-stop and restore the profile and telemetry.

The two questions, asked after each arm rather than at the end: **"Is running away the obvious move?"** and **"Do I find
openings to stop and strike, or am I forced to run the whole fight?"** A third is worth asking on arm B alone: **does
the Tank still read as a Tank** when it walks at 2.3 instead of 0.9?

### Limits of Part 4

- The expectations above come from scripted routes with perfect information, not from a player, and the chaos band of
  Parts 1–3 applies to them unchanged.
- The arm floor is not the Descent: one floor, no modifier, no chests, no relic in the numbers, and a forge that only
  heals. It was shaped to make the question visible in about a minute and a half, not to be balanced content.
- The arms were verified as assets and in the simulation, then installed and run twelve times on the S23 on
  2026-09-18; the record is in `23`. Three results bear on Part 3's recommendation. The **pacing claim holds**: with a
  moving hero, arm A's fight runs 9 to 16 seconds longer than arm B's, every time. The **compensation overshoots**:
  without a relic a standing hero dies on arm A and survives arm B with 22.5 health, so ×0.78 is too generous once the
  Forge Burst is not being fired. The claim that the candidate makes running away **less worthwhile is not
  reproduced**, but no device run kited: the scripted routes steer on a timer rather than away from the nearest enemy,
  so they understate kiting on the slow arm most. Device runs are deterministic, so each cell is one exact
  measurement rather than a sample.
- The feel questions are still unanswered, because nobody has played the arms by hand.
