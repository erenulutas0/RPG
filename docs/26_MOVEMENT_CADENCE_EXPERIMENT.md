# Experiments: why kiting wins, and what would change it — 2026-09-18

Part 1 measures a slower weapon cadence while the hero walks. Part 2 measures the two levers Part 1 pointed at:
enemy speed and enemy reach. Neither part changes the game.

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
