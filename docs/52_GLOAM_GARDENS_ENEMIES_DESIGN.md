# 52 - The Gloam Gardens' own enemies (design proposal, 2026-11-12)

A proposal for the lead's decision. Nothing here is built: no code, asset, scene or setting changed. Numbers cited as
"measured" name their source in docs/41, docs/48 or docs/50. Every other number is a starting value for the simulation
to measure, not a decision.

**Scope.** Floor 3 is the Gloam Gardens (docs/48, `Floor_GloamGardens.asset`): floor 2's six rooms under garden names,
climb-scaled at floor 3, the foundry's roster and the Forge Warden. docs/51 gives it a look (the foundry overgrown at
dusk, gloamlight `#C6EE6A` instead of lava, verdigris instead of brass) and re-colours the foundry enemies as charred bark
with an orchid `#D86BD0` bloom for now. docs/44 (section 1.6, F3 row) names the floor's family: **Cinder Bloom,
Thornling, Pollen Wraith**, and the boss **Rootmother Myrr** ("plants growing on ash, moving roots"; her telegraph line
"Be still. Be planted."). This document designs the three creatures so that each can be simulated, measured and built
one at a time, chooses the floor's elite, and decides the gate.

It is written after docs/50's two measured rounds, and it is shaped by what failed there (section 2.2). Like docs/50,
it is post-V0.1 content; the simulation research (section 8, step 1) can start at once because it touches nothing the
game ships.

## 0. The short version

| Creature | Role on floor 3 | Signature (one rule) | Counter-play | Separates | Weapon it favours |
|---|---|---|---|---|---|
| **Pollen Wraith** | Replaces the Cinder Mite in the horde (role 0) and in the three halls | Pollen settles on a hero who stands still near wraiths; full pollen blooms for a share of the hero's maximum health | Keep moving: kite the crowd | careful from **standing** (the `Kites` habit) | The Staff (the horde's weapon) |
| **Thornling** | Replaces the Runner in the horde (role 2) and in Ash Orchard's second wave | It dives into the ash and erupts under the hero's spot 0.8 s later | Read the ring and step off it; turn on the Thornling while it is stuck | **typical from slow hands** (`PilotRealism`), at equal cohort | The Staff in the horde; any weapon on the stuck body |
| **Cinder Bloom** (elite) | Replaces the Grunt Captain at Thorn Post | A bark-and-bloom cycle: closed 3.5 s (bark, takes 25 %, mauls), open 2.5 s (rooted, harmless, takes full damage) | Stay out of the closed maul's reach; strike the open bloom | careful from **standing** (kite / step-in reach against the maul) | The Daggers (the elites' weapon, `EliteRule` x2) |
| **Gate** | The Forge Warden stays (docs/48's choice), in docs/51's recolour | - | - | - | The Sword |

Rootmother Myrr is recommended as floor 3's own boss, but as a later design of its own, after the Glass Regent ships and
after these three are measured (section 6).

Three design rules carry the lessons of docs/50:
1. **No punishment rides on a horde body.** The two horde creatures punish with a share of the hero's maximum health
   that does not depend on the body's health or strike (horde bodies keep `HordeRule.HealthShare` 0.3 and die to the
   first splash, as the Wisp did).
2. **Every counter is a habit or a hand the cohort set already tells apart.** None of the three needs a new pilot habit
   (docs/50 needed three: flank, sidestep, back-liner). The Wraith reads `Kites`, the Bloom reads the kite and step-in
   reach, the Thornling reads `PilotRealism`'s reaction, aim and lapses.
3. **The elite's fight is bought with a clock, not with footwork.** The Bloom's windows set its length; the counter-play
   keeps the hero out of the maul without raising its damage, so a skilled fight is not a shorter one.

Rough size: simulation research about 3 days (5-6 h of runs); the three in the game about 6 days; art about 5-6 days.
About 14-16 working days in all, Myrr not included.

## 1. What floor 3 is today

Measured and shipped (docs/48, status 2026-11-12 round 2; `Floor_GloamGardens.asset`):

- **Rooms.** Ash Orchard (Grunt + 3 Mites; Runner + 3 Mites), Rootbound Walk (Tank + 3 Mites; 5 Mites), Bloom Gate
  (2 Grunts + 3 Mites), The Grafting Forge (Mend and Temper), Thorn Post (elite: Grunt Captain, Grunt, two Mites),
  Warden's Grove (gate: Forge Warden with two Cinder Mites). No surges, no extra elite, no modifier. The gate opens at
  3:00 on the floor's clock (180 s; floors 1 and 2 hold 210 s).
- **Tier.** `ClimbScaling` floor 3: enemy health x1.96, damage x1.8032; the gate boss x1.15 more. Horde enemies take
  `HordeRule.HealthShare` 0.3 and `DamageShare` 0.5 of that, and `HordeClock` grows them on the floor's clock (health
  +100 % and damage +50 % by 300 s, so x1.6 and x1.3 at the 180 s gate).
- **Horde.** Roles Mite, Grunt, Runner. `Weight(state, role) = max(0, 2 + pressure - role)`: the Mite (role 0) is 67 %
  of the Ramp, 50 % of the Horde state and 44 % of Pressure; the Runner (role 2) 0 / 17 / 22 %. Targets 4/10/16/6 grown
  by the clock (x1.3 at 180 s), scene cap 40.
- **Warden.** `_foundryGateHealthShare` 5.102041 (effective 11.5 x 300 against floor 1's authored health). Fight P50,
  Vanguard / Ilya: careful 61 / 57 s, standing 48 / 47, first-card 57 / 53, clumsy 61 / 62: every playing cohort in
  docs/40 section 9.6's 45-65 s.
- **Floor 3, by cohort** (seeds 1-200, typical hands, Vanguard / Ilya):

| Cohort | Floor 3 cleared (of those who went down) | Floor 3 P50 (s) | Completed the Descent | Change from the two-floor Descent |
|---|---|---|---|---|
| careful | 99 / 99 % | 254 / 250 | 95 / 95 % | -1 |
| standing | 94 / 95 % | 243 / 242 | 85 / 81 % | -4.5 |
| first-card | 95 / 94 % | 248 / 245 | 82 / 90 % | -5 |
| clumsy | 94 / 92 % | 251 / 253 | 31 / 50 % | -3 |
| auto-farmer | 3 / 8 went down | 222 / 233 | 1 / 1 % | - |

- The Descent: P10 / P50 / P90 13.8 / 14.2 / 14.7 min. Floor 3 kills 62 runs of 2,000; no finale death.

**What this leaves room for.** Two numbers bind the design before any creature is drawn:
- **The clumsy cohort completes 40.5 % pooled** (31 / 50 %), on the lower edge of docs/40's Forged band (40-60 %) and of
  docs/50 section 7's "clumsy completion at least 40 %". Floor 3's creatures have no room to cost the clumsy cohort.
- **The first-card cohort is already 5 points below the two-floor Descent**, docs/48's own limit. Its footwork is the
  careful pilot's, so a creature read by footwork spares it; none of the three may read cards.

So the floor's skill separation has to come out of the **standing** cohort (and the auto-farmer, which is not a playing
cohort here) and out of **slow hands**, not out of the clumsy or first-card cohorts.

### Foundry roster, and what floor 3 makes of it

| Enemy | Health (authored) | Weapon: damage / interval / reach (initial delay) | Speed | Floor 3 in a hall | Floor 3 in the horde (at 0 s -> 180 s) |
|---|---|---|---|---|---|
| Cinder Mite | 15 | 0.6 / 1.5 s / 1.5 | 2.2 | 29.4 health, 1.08 a strike | 8.8 -> 14.1 health, 0.54 -> 0.70 |
| Runner | 30 | 1.2 / 0.4 s / 1.5 | 3.0 | 58.8, 2.16 | 17.6 -> 28.2, 1.08 -> 1.41 |
| Grunt | 50 | 3.6 / 1.0 s / 1.5 | 1.6 | 98, 6.5 | 29.4 -> 47.0, 3.2 -> 4.2 |
| Tank | 120 | 7.2 / 2.5 s / 1.7 (1.5) | 0.9 | 235, 13.0 | - |
| Grunt Captain (elite) | 140 | 5.4 / 1.2 s / 1.6 (0.6) | 1.4 | 274, 9.7 | - |
| Forge Warden (gate) | 300 | 6.0 / 1.8 s / 1.7 (1.0) | 1.0 | x1.15 boss; share 5.102041 | - |

## 2. Shared rules, and what docs/50 taught

### 2.1 The shared rules (docs/50 section 2, kept)

1. **One signature behaviour each**, driven by one pure rule: `PollenRule`, `ThornlingRule`, `BloomRule`. Arithmetic
   that both the scene and the simulation run lives in one never-inlined body, as `WardenRule.GateHealthShareOn` and
   `EliteRule.DamageOn` do. The stamina divergence's lesson applies: no distance, speed or clock arithmetic is copied
   across assemblies.
2. **No projectile objects** (docs/42 section 4). Pollen is a state on the hero, the Thornling's dive is a movement under
   the floor (a placement, as docs/50's Wisp blink), the Bloom's cycle is a state on its body.
3. **Colours.** Red-orange `#FF5A3C` only for what will hurt after a countdown, filling as the Warden's mark fills
   (`WardenSlamClock.TelegraphProgress`). **Orchid (hue 280-330) belongs to the enemies** and to their harmless effects
   (docs/51 rule 3). **Gloamlight is the environment's** and never sits on an enemy or an enemy effect (docs/51 rule 5:
   a chartreuse dot on the floor must never read as a drop). The rule in one line: **red-orange hurts on a timer,
   orchid is a creature, gloamlight is the garden.**
4. **Ordered with the Warden.** The three step in one service where `WardenSlamService` steps (script order -40: after
   `EncounterController`, before every `AttackController`), proposed `GloamService`, so the scene's order and the
   simulation's agree; `FoundrySimulationSceneValueTests.TheScenesScriptOrderIsTheSimulations` extends to it.
5. **Paused by the reveal and by panels.** Pollen, the Thornlings' dive clocks and the Bloom's cycle hold whenever the
   encounter holds, as the Warden's and the burns' clocks do since 2026-11-11.
6. **Born on floor 3.** The three are drawn in the gloam palette from the start: their `EnemyArtSet` has no gloam
   variant (docs/51 step 4's `ForTheme` lookup returns null, so the set is its own look). The foundry's Grunt and Tank,
   the Cinder Mites at Thorn Post and the Grove, and the Warden stay in docs/51's bark-and-orchid recolour.
7. **Floor scaling as everyone.** Climb scaling, the horde's shares and the horde clock apply to the bodies below
   exactly as to the foundry's. The two punishments that are a share of the hero's maximum health (the bloom of pollen,
   the eruption) do not scale with the floor, as the Warden's slam (20 %) and docs/50's Clerk copy do not. Nothing here
   adds a scaling path.
8. **One for one.** Each creature replaces one foundry enemy in place: no wave grows, no horde role or target changes.

### 2.2 What docs/50 taught, and the rule each lesson became

| docs/50 measured | Why it failed | Rule here |
|---|---|---|
| The Silver Wisp: 40 blinks a floor, 0.1-0.2 health a minute taken, no reading moves; `wisp-kite` neither | A horde Wisp has 10 health and strikes for 0.8: it dies to the first splash and cannot punish what it reads | The horde creatures punish with a **share of the hero's maximum health** that does not need the body alive (the pollen stays on the hero; the Thornling cannot be struck while it is under the ash) |
| The Faceless Clerk's copy: shatter hits 0.1 a floor for every cohort that leaves a mark; `clerk-reach` hits every cohort about once a floor | The standing cohort keeps `EscapesMarks`: a mark separates nobody but the auto-farmer, and a mark nobody can leave is a cost everybody pays | Read **what the cohorts differ in**: standing lacks only `Kites` and `StepsIn`; the clumsy cohort lacks the 2026-11-04 fixes; the hands differ in reaction, aim and lapses. A mark is used only where the hands are the read (the Thornling), with a telegraph short enough for the hands to matter |
| The Mirror Sentry: the flank made it the Daggers' elite (+81-97 %) and separated careful from standing (+15 / +7), but no variant passed a 4.3 s elite fight | The counter-play (the flank) raised the damage dealt, so it shortened the very fight it was meant to fill | The elite's length comes from **a cycle** (closed / open windows), and the counter-play keeps the hero out of harm without adding damage |
| The Sentry cost the clumsy cohort (completion 36-39 %), which never flanks and died in the Sentry Hall | The read was a habit the clumsy cohort lacks (`StepsIn`) | With clumsy completion at 40.5 % (section 1), every read here is one the clumsy cohort **passes**: it kites (straight), leaves a circle by the nearest way, and kites any enemy within 1.6 |
| `all`: the four together cancelled out (the Sentry's cost and the Clerk's and Regent's ease met in the middle) | Some creatures eased the floor | No creature here eases the floor: the Wraith keeps the Mite's strike, the Thornling the Runner's, and the Bloom is the only one whose numbers change |
| The Regent needed a new sidestep habit before it could ship | A read the pilots did not have | No new habit (rule 2 of section 0) |

### 2.3 The cohort map

What each cohort keeps (`AutopilotCohort`), against the three reads. "Hands" are `PilotRealism`: typical (0.25 s
reaction, 0.5 aim error, 4 lapses a minute of 0.4 s, 0.5 s after a card) and slow (0.4 s, 0.8, 8 of 0.5 s, 0.8 s).

| Cohort | Kites | Kites each at its reach | Steps in | Escapes marks | Pollen Wraith | Thornling | Cinder Bloom |
|---|---|---|---|---|---|---|---|
| careful | yes | yes | yes | away from the source | spared | spared with typical hands | spared (step-in at 1.65, kite at 1.65) |
| standing | **no** | - | **no** | away | **pays** | spared with typical hands | **pays** (stands in the maul) |
| first-card | yes | yes | yes | away | spared | spared with typical hands | spared |
| clumsy | yes (straight) | no (1.6 for all) | no | nearest of eight | spared, but where its straight kite stalls on a wall | spared with typical hands (a circle has no wrong way out) | spared (kites at 1.6, the maul reaches 1.5) |
| auto-farmer | no | - | no | **no** | pays | pays | pays |
| any cohort, **slow hands** | | | | | a little (later kites) | **pays** | a little |

## 3. Pollen Wraith

**Role.** Replaces the Cinder Mite on floor 3 in the horde (role 0: `_foundryHorde` becomes Wraith, Grunt, Thornling)
and in the three halls' waves (Ash Orchard 6, Rootbound Walk 8, Bloom Gate 3). Thorn Post's two Mites and the Grove's
escort stay Cinder Mites, so the elite and the gate are read on their own mechanics. docs/51: "a pale drifting mote, the
Mite's slot". docs/44's clue for the floor is trees that feed on memory instead of light: the wraiths are what the trees
shed, and they settle on whoever stands still long enough to be remembered.

**Signature: pollen settles on stillness.** The hero carries pollen, a number from 0 to 1. Each frame:
- the hero is **still** when its average speed over the last 0.5 s is under 1.0 unit a second (one smoothed speed,
  `PollenRule.Still`);
- while still, pollen rises by 0.10 a second for each living Wraith within 1.6 of the hero, counting at most three;
- while moving, pollen falls by 0.5 a second;
- at 1.0 the pollen **blooms**: 6 % of the hero's maximum health, and pollen returns to 0.

One Wraith beside a still hero blooms it in 10 s; three in 3.3 s. A hero that walks for 2 s sheds a full load. Killing a
Wraith removes its pollen rate, not the pollen already carried. The Wraith keeps the Cinder Mite's body, strike and speed
unchanged: pollen is its one addition, so a hero that keeps moving meets exactly today's floor.

**Why it matters.** It moves cost from the hero that moves to the hero that stands. The standing cohort, which has no
kite, stands where the fight finds it with the Mite-heavy halls and the Ramp's crowd at its feet. The careful,
first-card and clumsy cohorts kite any enemy within their threat reach (`FootworkRule.ThreatReach`: max(1.6, 1.5 + 0.15)
= 1.65 for a Wraith; the clumsy cohort's 1.6 for every enemy), and the pollen radius, 1.6, is inside both: a Wraith close
enough to pollinate is close enough to be kited from. It is the "standing still does not pay" read that docs/41's echo
and docs/50's Clerk could not produce, because it is not a mark: there is nothing to leave, only a habit to have.

The punishment does not ride on the body (section 2.2): a horde Wraith has 8.8-14.1 health and dies to the first splash,
but the hero's pollen does not care how long any one Wraith lived, only how long the hero stood among them.

**Counter-play.** Keep moving: kite the crowd, turn, come back. The Staff's splash thins the Wraiths at range, so the
Staff is still the horde's weapon, and it is still the answer to a crowd you cannot leave.

**Cohort separation (target).** Standing against careful: blooms a floor careful at most 0.5, standing at least 4; the
careful-minus-standing gap in floor-3 clears grows by at least 5 points (today 99 / 99 against 94 / 95 %). Careful,
first-card and clumsy floor-3 clears within 2 of today. The clumsy cohort's straight kite stalls against walls
(`FootworkRule.KiteStraight` holds the stick into the wall and stands), which is the one place the Wraith reads it: hence
"within 2", not "unchanged".

**Numbers (starting values).**

| | Cinder Mite (today) | Pollen Wraith | On floor 3 |
|---|---|---|---|
| Health | 15 | 15 | 29.4 in a hall; 8.8 -> 14.1 in the horde |
| Weapon | 0.6 / 1.5 s / 1.5 | 0.6 / 1.5 s / 1.5 (unchanged) | 1.08 a strike in a hall, 0.54 -> 0.70 in the horde |
| Speed | 2.2 | 2.2 | |
| Gold / XP | 0 / 1 | 0 / 1 | the horde's 0 gold and 0.2 XP |
| Pollen | - | radius 1.6; +0.10 a second a Wraith (at most 3) while the hero's 0.5 s average speed is under 1.0; -0.5 a second while moving; bloom at 1.0 for 6 % of the hero's maximum health | |

Swept in step 1: rate 0.07 / 0.10 / 0.15, share 4 / 6 / 8 %, radius 1.4 / 1.6.

**Readability.** The Wraith: a small pale-orchid mote (`#F6C2F0` bloom light round a `#5A1F66` seed core) with a dark
outline. It must **not** be ash-white: Ilya's robe (`#D8CFC2`) is already near the ash drift's value (docs/51 rule 2), and
a pale white mote at her feet would hide her. The pollen on the hero: orchid motes cling to the hero's feet while pollen
is under 0.5 (harmless yet); from 0.5 a red-orange ring under the hero fills with the pollen to the bloom (it will hurt,
on a clock the hero controls). Red-orange under the hero then means one thing on every floor: move. The bloom: a short
orchid burst at the hero's feet and the usual hit flash. No gloamlight anywhere on the Wraith or the pollen.

**Art.** The horde contract the Mite uses (`BuildMiteAssets.cs`, `MiteRuntimeCapture.cs`): front and rear idle, step A,
step B, strike, pass A, pass B (12 bodies), a strike windup (2), remains (2), its own death (5 front, 5 rear): about 26
drawings and 14 flashes. Blender-rendered like `BuildBlenderFoundryEnemies.py` (a seed-pod mote trailing a few filaments,
the core a separate emissive part so its value can be tuned without re-rendering). Effects: the pollen motes (one
4-frame sprite, pooled with the drops' effects), the ring under the hero (the Warden mark's material at radius 0.8), the
bloom burst (4 frames).

**Simulation.** Cheap.
- The hero gains `Pollen` and a smoothed speed; the pack loop already measures every foe's distance to the hero for the
  kite and the threat, so the count within 1.6 costs one comparison a foe.
- `PollenRule.Step(pollen, stillSpeed, wraithsNear, deltaTime)` and `PollenRule.Still(...)`: never-inlined, called by the
  scene's `GloamService` and the simulation's floor step.
- No pilot habit: the counter is `Kites`, which the cohorts already have or lack.
- Levers, off by default: `GloamWraiths`, `WraithRate`, `WraithShare`, `WraithRadius`, `WraithStillSpeed`.
- Metrics: blooms a floor by cohort and room; health lost to pollen; seconds the hero stood still within 1.6 of a Wraith;
  floor-3 horde kills a second by weapon; floor-3 clears by cohort.

## 4. Thornling

**Role.** Replaces the Runner on floor 3: horde role 2 (0 % of the Ramp, 17 % of the Horde state, 22 % of Pressure)
and the Runner leading Ash Orchard's second wave. docs/51: "the Runner's thorned cousin". docs/44's "moving roots":
the Thornlings run the ash between the trees and come up where the climber stands.

**Signature: it dives and erupts under the hero.** A Thornling that has walked at least 3 s (since it walked in or last
erupted) and stands 2.5-5 units from the hero **dives**: it sinks into the ash (0.2 s), and while under it cannot be
targeted or struck. As it dives, a red-orange ring of radius 1.2 appears at the hero's spot and fills for **0.8 s**. When
it fills, the Thornling erupts at the ring's centre: **5 % of the hero's maximum health** if the hero is inside. Then it
is **stuck** for 1.0 s (thorns caught in the ash): it cannot walk or strike, and it takes damage as usual. Floor-wide, at
most one ring is down at a time, and a new ring waits 2.5 s after the last eruption. Between dives it is the Runner: the
same body, strike and speed.

**Why it matters, and why the hands.** A ring at the hero's feet is a mark, and docs/50 measured that every cohort but
the auto-farmer leaves marks (the Clerk's 1.1 s copy: 0.1 hits a floor). The Thornling does not try to separate cohorts.
It separates **hands**, which `PilotRealism` models and the cohorts do not: from the centre, the walk (3.5 units a second)
leaves a 1.2-radius ring in 0.34 s.
- Typical hands: 0.25 s reaction + 0.34 s = 0.59 s, 0.21 s to spare inside the 0.8 s; the 0.5 aim error and a lapse
  (4 a minute of 0.4 s, about an 8 % chance of overlapping a ring) catch it now and then.
- Slow hands: 0.40 + 0.34 = 0.74 s, 0.06 s to spare; the 0.8 aim error often leaves the escape point inside the ring, and
  lapses (8 a minute of 0.5 s) overlap about 17 % of rings.
- A kiting hero is already moving: 0.8 s at 3.5 units a second is 2.8 units, and it leaves the ring without reading it.
  So the kiting cohorts pay least, but the standing cohort, which keeps `EscapesMarks`, leaves the ring with the same
  hands. The ring is a circle, so the clumsy cohort's nearest-of-eight escape is as good as any (docs/50's Regent lanes
  were the clumsy cohort's trouble, not circles).

The telegraph is 0.8 s, docs/40 section 8.9's lower bound (0.8-1.2 s), not under it. docs/41's 0.8 s Warden echo was a
cliff because it was a second escape straight after the slam's; the Thornling's ring is a single escape, at most one at a
time, from a 1.2 radius, for 5 %. And the punishment does not ride on the body: a horde Thornling has 17.6-28.2 health,
but under the ash it cannot be splashed, so the Staff's first splash, which ended every Wisp blink, cannot cancel it.

**Counter-play.** Read the ring, step off it, then turn on the stuck Thornling (1.0 s in which it cannot strike). Do not
stand reading a card's afterimage: the 0.5-0.8 s after a panel closes is when rings land.

**Cohort separation (target).** Typical against slow hands, the same cohort: eruption hits a floor, careful typical at
most 1.5, careful slow at least twice that; careful slow floor-3 clears no more than 8 points below slow shipped (a read,
not a cliff). With typical hands, careful, standing, first-card and clumsy floor-3 clears within 2 of today.

**Numbers (starting values).**

| | Runner (today) | Thornling | On floor 3 |
|---|---|---|---|
| Health | 30 | 30 | 58.8 in a hall; 17.6 -> 28.2 in the horde |
| Weapon | 1.2 / 0.4 s / 1.5 | 1.2 / 0.4 s / 1.5 (unchanged) | 2.16 a strike in a hall, 1.08 -> 1.41 in the horde |
| Speed | 3.0 | 3.0 | |
| Gold / XP | 3 / 10 | 3 / 10 | the horde's 0 gold and 0.2 XP |
| The dive | - | walked 3 s; 2.5-5 units out; ring r 1.2 at the hero's spot; 0.8 s; 5 % of the hero's maximum health; stuck 1.0 s; one ring on the floor, 2.5 s between | |

Swept in step 1: telegraph 0.8 / 0.9 / 1.0 s, share 4 / 5 / 6 %, the floor-wide gap 2.5 / 4 s.

**Readability.** The Thornling: a low, dark bark body (`#4A3B33` with the dark outline) with orchid thorn tips. docs/51
found the Runner's recolour reads too pale (near Ilya's robe); the Thornling takes the lower body value from the start.
The dive: a grey ash puff where it stood (harmless, so not red-orange and not orchid-bright). The ring: red-orange, the
Warden mark's fill, a third of the Warden's radius, so the two read apart by size. The eruption: a burst of dark thorns
with orchid tips. Stuck: a ring of disturbed ash at its feet until it walks.

**Art.** The horde contract the Runner uses (about 26 drawings, 14 flashes), plus a dive pair (crouch, half-sunk; 2)
used in reverse for the eruption. Effects: the ash puff (4 frames, shared by the dive and the eruption) and the thorn
burst (4 frames), pooled. The ring reuses the slam mark's material at radius 1.2.

**Simulation.** Cheap to moderate.
- `Foe` gains `Burrowed` and `StuckUntil`; the floor gains the time the next ring may start.
- The ring is a `HazardMark` with the slag's shape (a mark at the hero's spot landing a telegraph later), which the
  research levers already have; the pilots' `EscapesMarks` and `PilotRealism`'s mark reaction already answer it.
- A burrowed foe is skipped by `Targeting.Acquire` and `CollectNear` (as the dead are); the eruption places its slot at
  the ring's centre with the placement docs/50's Wisp added to the simulation (`PackMotion`).
- `ThornlingRule.Dives(age, sinceLast, distance)` and `ThornlingRule.InRing(...)`: never-inlined.
- One rule interaction: `FootworkRule.StepIn` is not safe while a mark is down, so a ring pauses the careful pilot's
  step-in on an elite. That is the scene's rule today; measure it, do not change it.
- Levers: `GloamThornlings`, `ThornlingTelegraph`, `ThornlingRadius`, `ThornlingShare`, `ThornlingGap`,
  `ThornlingWalk`, `ThornlingStuck`.
- Metrics: rings a floor; eruption hits a floor by cohort and hands; health lost to eruptions; rings that landed in a
  lapse or within 0.8 s of a panel; strikes landed on stuck Thornlings.

## 5. Cinder Bloom (floor 3's elite)

**Role.** Replaces the Grunt Captain at Thorn Post: Bloom, Grunt, two Cinder Mites. `EliteRule` treats the first enemy of
an elite room's wave as the elite, so the Bloom gets the Daggers' x2 with no change. docs/51: "the foundry's golems with
ash-bark bodies and orchid blooms where the lava was". docs/44's floor clue: it is a foundry golem left standing when the
fires went out, and the garden grew a bloom in its cold furnace that opens to feed on the memories near it.

**Signature: the bark-and-bloom cycle.** From the moment it walks in, the Bloom repeats one cycle:
- **Closed, 3.5 s.** Its bark takes **25 %** of every landing. It walks and strikes with its maul.
- **Open, 2.5 s.** The bloom unfolds and it **roots**: it stops walking, holds its maul, and takes **100 %**.

One body, `BloomRule.Open(age)` and `BloomRule.DamageOn(amount, open)`, applied before `EliteRule.DamageOn` (the
Daggers' x2 multiplies what the bark lets through). No mark, no ring: the open bloom is a window, not a hazard.

**Why it fixes docs/50's elite row.** The Sentry's flank raised the damage dealt, so the fight shortened as the read
worked. Here the cycle quantises the fight. With D the hero's damage a second on the elite while in reach (all weapons,
the Daggers' x2 included) and the Bloom's 549 health on floor 3, a full cycle takes D x (3.5 x 0.25 + 2.5) = 3.375 D:

| D (damage a second in reach) | Windows it takes | Fight (s) |
|---|---|---|
| 60 | 3 | about 17 |
| 100 | 2 | about 10.5 |
| 150 | 1, then falls through the bark in the next closed phase | about 7 |
| 200 | 1 | about 5.5 |

No D brings the fight under 3.5 s plus what the first window needs, and the counter-play below does not raise D (the
Daggers' step-in, the Sword and the Staff all strike from 1.65-2.4, outside the maul either way). Health buys windows:
`BloomHealthScale` is the lever that sets "two windows for the careful pilot", whatever its footwork. Step 1 first reads
D on floor 3's Captain today, which docs/48 did not report, and sets the health from it.

**Why it is a skill read that spares the clumsy cohort.** The maul reaches 1.5, the Grunt's reach:
- the careful pilot's step-in (the Daggers) holds at 1.8 - 0.15 = 1.65, outside it, and its kite holds each enemy at
  its reach + 0.15 = 1.65: it is mauled only when crowded or when a Thornling ring pauses the step-in;
- the clumsy cohort kites every enemy within 1.6: also outside the maul;
- the standing cohort stands where the Bloom finds it, inside the maul for every closed phase (about 58 % of the fight).
The open bloom is harmless, so every cohort, the standing one too, gets its damage window; what the standing cohort
pays is the maul in between.

**Counter-play.** Keep out of the closed Bloom's reach (kite it, or hold the Daggers' step-in), kill the escort while it
is closed, and spend the open window on it.

**Cohort separation (target).** Health lost at Thorn Post: standing at least twice careful's; clumsy at most 1.3 times
careful's. Careful and standing elite fights both P50 6-12 s (the same length: the cycle, not the footwork, sets it).
Thorn Post deaths: clumsy no more than 2 above today. The Daggers lead the Bloom by more than 10 % (the Captain on floor
2: +19 %, docs/50).

**Numbers (starting values).**

| | Grunt Captain (today) | Cinder Bloom | On floor 3 |
|---|---|---|---|
| Health | 140 | 280 (2x; docs/40 8.5: the elite's health is its own budget) | 549; effective 2,195 against the closed bark |
| Weapon | 5.4 / 1.2 s / 1.6 (0.6) | Bark Maul 10.8 / 1.8 s / **1.5** (0.6), closed only | 19.5 a blow, 10.8 a second while closed |
| Speed | 1.4 | 1.2; rooted while open | |
| Gold / XP | 20 / 10 | 20 / 10 | |
| The cycle | - | closed 3.5 s (25 % taken), open 2.5 s (rooted, no strike, 100 % taken); closed when it walks in | |

The maul is 6.0 damage a second authored against the Captain's 4.5 (docs/40 8.5: an elite at 1.5-1.8x a Grunt's 3.6),
in fewer, heavier blows, and only for 58 % of the fight. Swept in step 1: `BloomHealthScale` 0.8 / 1.0 / 1.25, maul 8.1
/ 10.8 / 13.5, open 2.0 / 2.5 / 3.0 s.

**Readability.** Closed: a golem in charred bark plates (`#4A3B33`, a step lighter than the floor, dark outline) with a
thin orchid seam where the chest bud is shut. Open: the bloom unfolds in the chest, the brightest orchid on the floor
(`#D86BD0` to `#F6C2F0`), and bark-coloured roots run from its feet into the loam. The open bloom says "strike here", so
it is orchid, never red-orange: it does not hurt. The maul's windup is its strike windup pair (melee, no ground mark, as
the Captain's). A struck closed Bloom shows a dull bark chip in place of the hit flash's full white, so a player sees
that closed landings are blunted.

**Art.** The elite contract the Captain uses: 12 bodies, strike windup (2), remains (2), own death (5 + 5); about 26
drawings. Plus the open set: open idle front and rear (2), opening half and full (2; closing plays them in reverse); about
4 more, the role the Warden's "hot" frames play. Blender-rendered with the bloom as a separate emissive part (as docs/50
proposed for the Sentry's mirror), so its value can be tuned without re-rendering. No new effects beyond the bark chip
(3 frames).

**Simulation.** Cheap.
- `Foe` gains its age on the floor (the cycle reads it); `BloomRule.DamageOn` where `FoeDamaged` applies a landing,
  ahead of `EliteRule.DamageOn`; in the scene, a component ahead of `Health` on the Bloom's prefab, as docs/50 proposed
  for the Sentry's guard.
- Rooted: speed 0 and the weapon held while open, through the hold the Warden already uses (`WardenSlamClock`'s
  weapon hold).
- No pilot habit. `FootworkRule.StepIn` already names "an elite room's Captain": it must read "the elite room's
  leader" (the `EliteRule` definition), or the careful pilot would kite the Bloom where it stepped in on the Captain.
  That is a renaming of the target, not a new habit; check it in step 1.
- Levers: `GloamBloom`, `BloomHealthScale`, `BloomClosed`, `BloomOpen`, `BloomBark`, `BloomMaulScale`.
- Metrics: the elite fight P50 / P90 (walk-in to fall) and windows used (1, 2, 3+); health lost at Thorn Post by source
  (maul, escort, horde) and by cohort; each weapon's damage a second on the Bloom (the floor-3 roles row's elite job);
  Thorn Post deaths.

### 5.1 Why the Bloom is the elite

- **The slots docs/51 gives them.** The Wraith is the Mite's slot and the Thornling the Runner's: horde bodies. The
  Bloom is the golems', and the Captain is a golem.
- **A cycle needs a body that lives through it.** At floor 3 only an elite's or a boss's health lasts two windows; a
  horde body lasts one strike, a hall body about a second (docs/40 8.6).
- **The elites' weapon.** The Daggers' x2 (`EliteRule`) applies to the elite room's leader with no change, and the cycle
  keeps it a single-target fight, where the Daggers lead.
- **docs/40 10.3's elite identity:** "build counterplay: a temporary area resistance, not immunity". The closed bark
  is that: 25 %, not 0, and it ends on a clock.
- **The alternative,** a Thornling elite ("Thorn Post" names thorns): a fast flanker that dives is a fight the hero's
  reaction shortens or lengthens, the Sentry's problem in another form, and its read is the hands, which an elite room
  met once a floor measures poorly. The room can keep its name (the bark is thorned) or be renamed (question 7).

## 6. The gate: the Warden stays now; Rootmother Myrr later

**Decision: floor 3 keeps the Forge Warden for this design's build** (docs/48's choice: share 5.102041, two Cinder
Mites, the gate at 180 s), in docs/51's bark-and-orchid recolour. **Floor 3 does need its own boss, Rootmother Myrr, as
a separate design after the Glass Regent ships.** Reasons:

1. **The band is measured.** The floor-3 Warden holds every playing cohort in 45-65 s (47-62 s). docs/50's Regent took
   two rounds and a share past its own sweep (7.0 against 4.6-5.8) to reach the band. A new boss buys the band again.
2. **The creatures move what the hero brings to the gate.** Pollen and the maul change the standing cohort's health at
   the Grove, the Thornlings everyone's with slow hands. A boss share tuned before them would be tuned on the wrong
   floor. Build the creatures, re-read the Warden's band with them on, then design Myrr against that floor.
3. **One boss at a time.** The Regent (docs/50: share 7.0, the sidestep habit still to build) is the boss work in
   flight. Both bosses use the boss contract (about 48 drawings and flashes, one Blender model each) and the same
   reveal, hold and parity tests; building them at once doubles the riskiest part of both.
4. **But the Warden three times is too many.** Today it is the gate boss of floors 1, 2 and 3. With the Regent on floor
   2 it is still met twice: docs/48 section 1.4's "same boss again" is halved, not solved. docs/40 section 3.2 calls
   floor 3 the "evolution / strong boss" floor, and docs/44 names Myrr. So Myrr is a yes, in order.

**What Myrr's design should start from** (constraints learned on floors 1-2, not a design):
- **Keep the Warden's clock** (mark, 1.1 s telegraph, the weapon held until 1.5 s after the blow), as the Regent did:
  the fight's pace and damage window are already measured.
- **One new read that is not "leave the circle"**, which every cohort but the auto-farmer does. The floor's grammar
  offers one: stillness. A candidate to measure: her mark reads the hero's pollen (`PollenRule`), radius 2.2 at none
  and 3.0 at full, so "Be still. Be planted." costs the still hero a longer run and nothing new is asked of the moving
  one. One rule reading another, no new body.
- **A reinforcement that threatens, not one to break.** docs/40 8.9's third phase and docs/48 1.4's reinforcement:
  docs/50's Regent copies (6 % of its health, shattered in about a second by every cohort) "add little". If Myrr calls
  help at half health, it should be Thornlings (rings), not bodies to break.
- **The Sword stays the gate boss's weapon** (`WardenRule.SwordShare` x1.5 on Myrr alone).

## 7. Lore and codex

docs/44's mobile production note asks each later floor for 3 new enemies, 1 boss and 3-5 Codex entries: this design
gives the three enemies; the boss is section 6's. The Codex runs to C30; three entries after it, as drafts for the
owner's pen:

| Unlock | Draft title | Draft line |
|---|---|---|
| First Cinder Bloom felled | Cold Furnace, Open Flower | "The fires went out. The furnace kept opening, as if something inside still waited to be fed." |
| First eruption dodged | Root Post | "The roots carried messages between the trees. Now they carry thorns, and they still arrive." |
| First pollen bloom taken | Kept Moments | "Every mote is a moment someone stood still for. The garden keeps them all." |

The Wraith's rule is the lore's line made a mechanic: the garden plants whoever stays. Myrr's telegraph, "Be still. Be
planted.", is then the floor's warning in her voice.

## 8. The measurement plan and acceptance

**What is built for it** (step 1, simulation only, every lever off by default):
- `Assets/_Project/Tests/Support/DescentSimulation.Gloam.cs`: the three as `Enemy` definitions with sections 3-5's
  numbers; `GloamChain(levers)` (floors 1 and 2 as shipped above a floor 3 with the levers' creatures in place: the
  Wraith for the Mite in the horde's role 0 and the three halls, the Thornling for the Runner in the horde's role 2 and
  Ash Orchard's second wave, the Bloom for Thorn Post's Captain; Thorn Post's and the Grove's Cinder Mites stay); the
  levers of sections 3-5; the rules in never-inlined bodies (`PollenRule`, `ThornlingRule`, `BloomRule`).
- `DescentSimulation.Foundry.cs` hooks that do nothing unless floor 3 holds one of the three (as docs/50's did).
- `Tools/CombatChecks/GloamEnemiesResearch.cs` (explicit): every cohort, both heroes, the whole Descent (floors 1-3 and
  the finale, `FinaleFloor` 4), with `GLOAM_OPTIONS` picking options; it writes `gloam-enemies.md` and `.csv`. No
  `GloamHabit`: none of the three asks the pilot for one.

**The runs.**
- Seeds 1-200, both heroes, all five cohorts, `FOUNDRY_REALISM=typical`: each creature alone against `shipped`, then
  the three together (`all`).
- **Slow hands**: the careful and standing cohorts again with `FOUNDRY_REALISM=slow`, `shipped` and each option. This is
  the Thornling's read, and the check that the Wraith and the Bloom are not hands reads in disguise.
- The careful cohort frame-perfect (no realism), as the reference row docs/41's tables use.
- The roles: the careful Vanguard started with the Staff and with the Daggers (typical), beside its Sword: a floor-3
  roles row (the horde, the Bloom, the Warden), reported as docs/50's floor-2 row was.
- The chosen values again on the holdout, seeds 201-400.
- About 6-7 minutes an option on 6 threads for the typical cohorts (docs/50's 4 minutes for two floors and the finale,
  plus floor 3), 3 more for the slow pair: the three single options, `all` and the three sweeps (about 25 options) are
  about 5-6 hours.
- **The default batch stays byte-identical** after the merge (`FOUNDRY_FLOORS=2`, and `FOUNDRY_FLOORS=4`'s floor 1-3 and
  finale rows with the levers off), as after every research merge since docs/41.

**Baseline readings `shipped` must report first** (docs/48 did not): the floor-3 Captain's fight P50 and its D (the
damage a second in reach, section 5), health lost at Thorn Post by cohort, the floor-3 clears and completion with slow
hands, and the standing cohort's seconds standing still within 1.6 of a Mite on floor 3 (the Wraith's opportunity).

**Acceptance (proposed, the three together, against floor 3 as shipped on the same seeds, typical hands unless named).**

| Row | Target |
|---|---|
| Clears, careful, floor 3 | no more than 2 of 200 below today, both heroes |
| Clears, first-card and clumsy, floor 3 | no more than 2 points below today |
| Clears, standing, floor 3 | 5-10 points below today: the floor's skill read |
| Skill separation | the careful-minus-standing gap in floor-3 clears grows by at least 5 points |
| Hands separation | careful, slow hands: floor-3 clears 3-8 points below slow shipped; careful typical within 2 |
| Completion | careful within 1 point of today; first-card within 2; **clumsy at least 40 % pooled** (today 40.5 %); standing may fall up to 8 |
| Descent | P50 within 13.8-14.8 min, all completed runs inside 13-16; no finale death |
| Floor 3 time P50 | 240-300 s (the 180 s gate binds; the Bloom's longer fight may add about 5 s) |
| Floor-3 Warden | every playing cohort's P50 still in 45-65 s with the creatures on; if not, the share moves, not the creatures |
| Pollen Wraith | blooms a floor: careful at most 0.5, standing at least 4; health lost to pollen reported by cohort |
| Thornling | eruption hits a floor: careful typical at most 1.5, careful slow at least twice that; no hit within 0.8 s of a ring's start (the telegraph holds) |
| Cinder Bloom | elite fight P50 6-12 s for careful **and** standing; health lost at Thorn Post: standing at least 2x careful, clumsy at most 1.3x |
| Floor-3 roles | the Staff leads the horde, the Daggers the Bloom, the Sword the Warden, each by more than 10 % (reported) |
| Parity | each rule's scene-and-simulation parity test; the Editor's `HordePacingMeasurement` with horde-floors.txt "4", 6 seeds a hero, within about 6 s of the simulation on floor 3 |
| Device | floor 3 on the S23 at 40 alive with a ring down and the pollen ring showing: p95 at most 20 ms, no frame over 35 ms in a steady fight |

**Fallbacks if a row fails** (each a lever value, not new work):
- Wraith too harsh on standing: share 4 %, or rate 0.07. No separation: rate 0.15, or radius 1.8 (beyond the clumsy
  kite reach, so check the clumsy row). Careful pays: lower `WraithStillSpeed` to 0.6, so a step-in's small moves count
  as moving.
- Thornling a cliff for slow hands: telegraph 0.9-1.0 s, or the floor-wide gap 4 s. No hands separation: radius 1.4.
- Bloom too short: `BloomHealthScale` 1.25 (a third window); too long: open 3.0 s. Standing separation too small: maul
  13.5. The clumsy cohort mauled: maul reach 1.4.

## 9. Phased build plan

Sizes are rough, for one agent; measurement runs are wall time on this machine.

| Step | What | Size |
|---|---|---|
| 0 | The lead answers section 10 | - |
| 1 | **Simulation research**: `DescentSimulation.Gloam.cs`, the three as levers, `GloamEnemiesResearch`; the baseline readings, each creature alone, the sweeps, `all`, slow hands, the holdout; a docs/52 Measured section with the tables | 2.5-3 days, about 5-6 h of runs |
| 2 | **Cinder Bloom in the game**: `BloomRule`, the cycle in `GloamService`, the bark component ahead of `Health`, rooted-while-open through the Warden's hold, `Enemy_CinderBloom` and its weapon, Thorn Post's leader, the step-in's target read from `EliteRule`; parity and flow tests (as `EliteStepInFlowTests` on a proof floor) | 1.5-2 days |
| 3 | **Pollen Wraith in the game**: `PollenRule`, the hero's pollen in `GloamService` (reset at each checkpoint), the ring under the hero, `Enemy_PollenWraith`, floor 3's horde role 0 and the halls' Mites; tests | 1.5-2 days |
| 4 | **Thornling in the game**: `ThornlingRule`, the floor-wide ring gate, untargetable while under the ash (`EncounterController`'s candidates), the slot placement in `PackMotion`, `Enemy_Thornling`, horde role 2 and Ash Orchard's Runner; tests | 2-2.5 days |
| 5 | **Art for the three** (Blender, the contracts above, the Build and Capture tools, proofs on the gloam floor once docs/51 steps 1-4 are in; pollen motes, ring, bloom burst, ash puff, thorn burst, bark chip) | 5-6 days |
| 6 | **The phone**: a new APK; floor 3 on the S23 (frame time with the rings and the crowd, the owner's play) - the owner's go needed, as for every install | 0.5 day with the owner |
| later | **Rootmother Myrr**: its own design after the Regent ships, against the floor these three make (section 6) | a design, then about 8-10 days |

About 13-16 working days for steps 1-6, a little under half of it art. Steps 2-4 can each ship on their own once step 1
has its numbers. Recommended order: the Bloom (the least code, and the elite row docs/50 could not reach), then the
Wraith (the floor's main skill read), then the Thornling (the riskiest: a hands read can turn into a cliff).

What stays unchanged throughout: floors 1 and 2 and every row of the default batch; the Warden's, the Daggers' and the
Sword's rules; the horde director; the region's renderers and vertices (each creature is one body sprite and shadow, as
the enemy it replaces; the rings, motes and bursts are pooled quads in the existing effects).

## 10. Questions for the lead

1. **When, and in which order** beside docs/50's build (the Sentry, the Regent) and docs/51's art steps? Simulation
   research now, the build after floor 2's creatures?
2. **The gate.** Keep the Warden on floor 3 until Myrr gets her own design after the Regent ships (section 6), or bring
   Myrr into this document's scope now?
3. **The clumsy line.** Hold clumsy completion at 40 % on the three-floor Descent (which aims every read here at the
   standing cohort and at slow hands), or accept a lower line for floor 3?
4. **Wraiths where?** In the horde and the three halls only (Thorn Post's and the Grove's Cinder Mites stay), or at
   Thorn Post and the Grove too?
5. **The pollen ring under the hero.** Red-orange from half pollen (the floor's "move" colour, on the hero rather than on
   a spot), or an orchid-only meter that turns red-orange only in its last second?
6. **The Thornling's read is the hands.** Is "slow hands lose more" an acceptable skill separation, with a 0.8 s
   telegraph at docs/40's lower bound?
7. **Thorn Post's name** with a Bloom leading it: keep it (the bark is thorned) or rename (for example "The Bloom Bed")?
8. **The bark.** Is a closed Bloom taking 25 % (docs/40 10.3's area resistance, which blunts the Staff on the elite as
   much as any weapon) acceptable as the elite's build counterplay?
9. **Art route** as docs/50: Blender-rendered, no generated imagery; the Wraith pale orchid, not ash-white, for Ilya's
   sake. Agreed?
10. **Codex.** Take the three drafts of section 7 to the owner, and under which IDs after C30?
