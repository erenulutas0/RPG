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

## 11. Lead decisions 2026-11-12

The owner made the lead the vision designer on 2026-11-12; these answer section 10.

1. **Order.** After the Mirror Sentry's build (docs/50 step 2). The Cinder Bloom's simulation research first (simulation only,
   it changes nothing the game ships), then the Pollen Wraith, then the Thornling. The art follows each creature's numbers.
2. **The gate.** The Warden stays on floor 3 until Rootmother Myrr has her own design, after the Glass Regent ships.
3. **Clumsy completion.** Hold the 40 % line on the whole Descent; no creature may cost the clumsy or first-card cohorts.
4. **Wraiths.** In the horde and the halls only; Thorn Post's and the Grove's Cinder Mites stay.
5. **The pollen ring.** An orchid meter under the hero that turns red-orange only in its last second: red-orange keeps
   meaning "this will hurt now" (docs/50 section 2 rule 3).
6. **Slow hands.** Accepted as the Thornling's read with the 0.8 s telegraph; if careful-slow loses more than 8 points of
   floor-3 clears, lengthen the telegraph to 1.0 s before anything else.
7. **Thorn Post** keeps its name.
8. **The closed Bloom's 25 % bark** is accepted as the elite's build counterplay: it blunts every weapon alike while
   closed, and the open phase rewards timing.
9. **Art.** Blender, as docs/50; the Pollen Wraith pale orchid, not ash-white, for Ilya's sake.
10. **Codex.** The three drafts go to the owner later, as C31-C33.

## Measured (simulation only, 2026-11-12 to 11-15): the Cinder Bloom

Section 9's step 1 for the Bloom alone (section 11, decision 1), as research levers off by default. Nothing the game
plays changed: no scene, prefab, asset or setting. The default batch is byte-identical before and after (end of this
section).

**What was built.**
- `Scripts/Combat/BloomRule.cs` (compiled by CombatChecks): the cycle in never-inlined bodies - `Open(age, closed,
  open)`, `WindowsBegun`, `DamageOn(amount, open, bark)`, `SpeedOf(speed, open)`, `SwingDone(since, windup)` - and a
  `BloomClock` (age, `IsOpen`, `Land`, `Speed`, `Reset`) for a later scene's `GloamService` and the simulation alike.
  `BloomRuleTests` hold it.
- `PackMotion.SetSpeed(index, speed)`: the rooting (an open Bloom walks at 0). Unused by the scene today.
- `Tests/Support/DescentSimulation.Gloam.cs`: `CinderBloom` (280 health, Bark Maul 10.8 every 1.8 s at reach 1.5, 0.6 s
  before the first, speed 1.2, 20 gold, 10 experience: 549 health and 19.5 a blow at floor 3's tier) and
  `GloamChain(levers)` (floors 1-2 as `VaultsChain` makes them, floor 3 with the Bloom at the head of Thorn Post in the
  Captain's place, its Grunt and two Cinder Mites unchanged). Levers: `GloamBloom`, `BloomHealthScale`,
  `BloomMaulScale`, `BloomMaulReach`, `BloomClosed`, `BloomOpen`, `BloomBark`, and past section 5's list `BloomSpeed`
  and `BloomWindup`. Its clock is stepped where the scene's service would step it (after the Warden's slam and the
  Sentry's guard, before every attacker) and holds under a panel and the reveal. Every elite leader's fight is now read
  for the report (walk-in to fall, seconds in the hero's longest reach, D, windows, the health lost while an elite wave
  stands, by source); the reading changes nothing a run does.
- `Tools/CombatChecks/GloamEnemiesResearch.cs` (explicit): every cohort (or `GLOAM_COHORTS`), both heroes, the four-floor
  Descent, `GLOAM_OPTIONS`, `GLOAM_ROLES`; it writes `gloam-enemies.md` and `.csv`. `FoundryFloorThreeTests` holds the
  lever off as the shipped run and reads the Bloom's fight over several seeds.

**Where the model departs from section 5.**
- The bark takes its share of every landing that reaches the Bloom's health while closed: weapons, procs, the ability and
  a burn's tick alike. It is applied where the landing meets the body, after the attacker's `EliteRule` share; the
  product is the same, so the Daggers' x2 multiplies what the bark lets through, as section 5 says.
- While open the maul neither ticks nor strikes (the Warden's weapon hold) and the Bloom's walk is 0.
- Its age runs from its first stepped frame (not the frame it walks in).

**Floor 2 in every run below is the shipped Mirror Sentry, `sentry-r0-f70`** (main 78f70a4d, merged), unless marked:
rounds 1-4 of the exploration (2026-11-12) ran on the Sentry's earlier `f50`. All runs: typical hands unless named, both
heroes, the four-floor Descent, and every cell against `shipped` (the Grunt Captain at Thorn Post) on the same seeds.

### Baselines (docs/48 did not report them)

Floor 3's Thorn Post with the Captain, seeds 1-200; 201-400:

| Cohort | Elite fight P50 (walk-in to fall), V / I | In reach P50 | D (health a second in reach) P50 | Thorn Post health lost (leader / escort / horde) | Floor-3 clears V / I | Completed (of 400) |
|---|---|---|---|---|---|---|
| careful | 2.5 / 2.4; 2.6 / 2.5 s | 2.2-2.4 s | 220-235 | 17.0-19.2 (12.2-13.1 / 4.0-4.8 / 0.6-1.2) | 189 / 190; 193 / 191 | 379; 384 |
| standing | 4.3 / 4.3; 4.4 / 4.3 s | 2.3-2.5 s | 220-228 | 12.7-14.5 (11.6-13.0 / 0.5-0.8 / 0.3-0.5) | 165 / 162; 164 / 168 | 327; 332 |
| first-card | 2.4-2.5 s | 2.2 s | 233-243 | 17.8-21.3 | 164 / 179; 146 / 159 | 343; 305 |
| clumsy | 5.6 / 5.3; 5.9 / 5.2 s | 2.7-3.8 s | 150-218 | 21.1-31.2 (14.5-20.9 / 1.2-1.8 / 0.5-0.7) | 62 / 98; 56 / 90 | 160 (40.0 %); 146 (36.5 %) |
| careful, slow hands (1-200) | 2.6 / 2.7 s | 2.4-2.5 s | 217-218 | 18.8-20.3 | 137 / 141 | 278 |
| standing, slow hands (1-200) | 4.6 / 4.4 s | 2.3-2.5 s | 212-233 | 13.5-13.7 | 129 / 125 | 254 |

- **D is about 230, not section 5's 60-200.** It counts every weapon and the Daggers' x2 over the seconds in reach (the
  careful Vanguard's Sword alone deals 37 a second to the leader over its fight). With D = 230 the starting Bloom's 549
  falls in its first open window (3.5 s x 0.25 x 230 + 2.5 s x 230 = 776 a cycle), so section 5's quantisation reads as
  "one window, or two", not "two by health".
- The standing cohort loses *less* than careful at Thorn Post (0.75x): the kite takes the careful hero through the
  escort (4-5 health), and a 2.5 s fight leaves the Captain's blows no time to differ.
- The Captain kills almost nobody at Thorn Post (2 and 6 deaths of the four cohorts' 1,600 runs a range).

### The starting values (`bloom`)

| Seeds | Careful F3 V / I (vs Captain) | Completed careful / standing / first-card / clumsy (vs Captain) | Elite fight P50 careful / standing | Thorn Post lost careful / standing / clumsy | Standing / careful | Thorn Post deaths |
|---|---|---|---|---|---|---|
| 1-200 | 186 / 187 (-3 / -3) | 373 (-6) / 309 (-18) / 309 (-34) / 154 (-6) | 9.8 / 10.8 s | 49.8 / 52.5 / 62.8 | 1.05x | 17 |
| 201-400 | 193 / 189 (+0 / -2) | 382 (-2) / 326 (-6) / 276 (-29) / 141 (-5) | 9.8 / 10.9 s | 46.2 / 46.1 / 65.3 | 1.00x | 32 |

The fight is in band and the Daggers lead it (+87 %); everything else misses. The clumsy cohort loses 11 completions in
800, first-card 63, and **the careful cohort is mauled as hard as the standing one** (42 of its 50 from the maul). Why,
read from the cells:
- **The kite does not keep a hero out of a 1.5 maul.** The careful pilot kites an enemy within its reach + 0.15 = 1.65,
  a reaction (0.25 s) after it comes in; the Bloom closes 0.3 in that time, inside 1.5, and its maul, ready while it
  walked, lands on contact. Section 5's "outside the maul either way" holds for a frame-perfect pilot only.
- **The clumsy cohort is mauled like the standing one** (40-49 from the leader): its straight kite stalls, and its
  fights are the longest (12-16 s P50, 2-3 windows), every second of them also a second at the pit's rim (the health it
  loses at Thorn Post that is neither the leader's, the escort's nor the horde's: 14-21 a fight, against 4-7 beside
  the Captain).
- What a hero loses at Thorn Post it carries into the Warden's Grove (no forge between): the new deaths are at the gate.

### The sweep (seeds 1-200)

One lever at a time off the starting values, then combinations; against the Captain on the same seeds (careful floor-3
clears 189 / 190; four-floor completions standing 327, first-card 343, clumsy 160).

| Option | Careful F3 V / I | Completed standing / first-card / clumsy | Elite fight P50 careful / standing | Thorn Post lost careful / standing / clumsy | Standing / careful |
|---|---|---|---|---|---|
| `bloom-h80` (health 224) | +0 / -3 | -16 / -23 / -4 | 7.4 / 10.3 s | 41.4 / 47.0 / 54.9 | 1.13x |
| `bloom-h125` | -3 / -2 | -22 / -36 / -6 | 10.9 / 11.6 s | 57.8 / 59.3 / 72.2 | 1.03x |
| `bloom-m75` (maul 8.1) | -5 / -1 | -12 / -18 / -5 | 9.8 / 10.8 s | 39.6 / 40.1 / 51.9 | 1.01x |
| `bloom-o3.0` | -2 / -3 | -14 / -29 / -4 | 9.5 / 11.2 s | 44.7 / 49.1 / 57.5 | 1.10x |
| `bloom-o2.0` | -4 / -3 | -22 / -41 / -10 | 9.9 / 10.6 s | 54.0 / 56.2 / 69.1 | 1.04x |
| `bloom-c3.0` | -4 / +1 | -12 / -26 / -4 | 9.2 / 10.4 s | 44.9 / 42.4 / 56.6 | 0.94x |
| `bloom-c4.0` | +0 / -3 | -15 / -32 / -7 | 10.6 / 11.1 s | 53.8 / 52.6 / 64.4 | 0.98x |
| `bloom-r1.2` (maul reach 1.2) | +0 / +0 | -15 / -6 / -5 | 9.8 / 10.7 s | 18.3 / 44.8 / 56.9 | 2.45x |
| `bloom-m100-r1.0` | -1 / +2 | -8 / +3 / -6 | 9.8 / 10.7 s | 11.8 / 41.2 / 51.6 | 3.48x |
| `bloom-m75-r1.2` | -1 / +0 | -8 / -3 / -4 | 9.8 / 10.7 s | 15.9 / 34.3 / 47.8 | 2.16x |
| `bloom-m50-r1.2` | -1 / +1 | -4 / +1 / +0 | 9.8 / 10.7 s | 13.5 / 23.8 / 38.7 | 1.76x |
| `bloom-m35-r1.2` | -1 / +1 | -2 / +3 / +2 | 9.8 / 10.7 s | 12.1 / 17.5 / 33.3 | 1.45x |
| `bloom-m50-r1.0` | +0 / +2 | -1 / +4 / +0 | 9.8 / 10.7 s | 10.3 / 22.0 / 35.1 | 2.13x |
| `bloom-m35-r1.0` | +0 / +2 | -1 / +5 / +0 | 9.8 / 10.7 s | 9.9 / 16.3 / 30.2 | 1.65x |
| `bloom-m100-h80-r1.2` | -3 / +0 | -13 / -5 / -4 | 7.4 / 10.2 s | 15.7 / 40.5 / 47.5 | 2.59x |
| `bloom-m75-h80-r1.2` | -3 / +0 | -6 / -2 / -4 | 7.4 / 10.2 s | 13.8 / 31.1 / 39.9 | 2.26x |
| `bloom-m50-h80-r1.2` | -3 / +0 | -1 / -2 / -3 | 7.4 / 10.2 s | 11.9 / 21.6 / 32.3 | 1.82x |
| `bloom-m35-h80-r1.2` | -3 / +0 | +0 / -1 / -2 | 7.4 / 10.2 s | 10.7 / 15.9 / 27.8 | 1.48x |

Read on the Sentry's earlier `f50` floor 2 (rounds 2-4, against the Captain on those runs; clumsy of 400): `bloom-r1.4`
-7; `bloom-s0.9` (a closed walk of 0.9) -11 at 0.78x; `bloom-w0.5` and `bloom-w0.7` (a maul windup that whiffs on a hero
who has left its reach) -8 and -9 at 1.44x and 1.56x. The windup and the slower walk spare the careful pilot less than a
shorter reach does, and cost the clumsy one as much.

What the sweep says:
- **The cycle levers move nothing that matters.** Closed, open, health and (round 1) the bark each cost the clumsy cohort
  4-10 and leave standing / careful at 0.9-1.1x. The fight's length is the cycle's, as section 5 meant: P50 9.2-10.9 s
  at full health, 7.2-7.4 s at 80 %.
- **The maul's reach is the skill read.** At 1.2 the careful pilot's kite (held at 1.6-1.65, less the 0.3 a reaction
  lets the Bloom close) stays out of it: its maul damage falls from 42 to 9-10 at the full maul, to about 3 at 1.0.
  The standing cohort, whom the Bloom walks up to (it stops at its reach less 0.1), takes the maul at any reach.
- **The maul's weight is the clumsy cost.** With the reach short the clumsy cohort still takes 30-50 at Thorn Post (its
  kite stalls, its fight is 12-15 s); only a maul of 35-50 % (3.8-5.4 authored, 6.8-9.7 a blow on floor 3) brings that
  back near the Captain's 21-31 plus the longer fight's pit.
- **Two rows pull against each other.** The standing cohort's maul is the separation, and the clumsy cohort pays the same
  maul. A maul light enough to cost the clumsy cohort nothing leaves the standing one 15-22 at Thorn Post against
  careful's 8-10 - and careful's 8-10 is mostly the escort and the horde (5 + 3-4), which no Bloom lever touches: 1.45-1.74x
  by section 8's row. Read on the maul alone (the leader's own damage), standing loses 4-13x careful's.
- Health at 80 % shortens the fight to 7.4 s (in band) and buys no completion: each h80 combination is level with its
  full-health form or below it.

### Under the lead's relative rule (seeds 1-400)

The rule (2026-11-12): over seeds 1-400 the creature may cost the clumsy cohort at most 1 four-floor completion in 800
against the shipped floor on the same seeds; careful within 3 floor-3 clears on each range; the other rows as section 8
lists them. The options read on both ranges:

| Option | Clumsy 1-200 / 201-400 (of 160 / 146) | Cost in 800 | Careful F3 V / I: 1-200; 201-400 | Standing / careful lost at Thorn Post | Elite fight P50 careful / standing | Rule |
|---|---|---|---|---|---|---|
| `bloom` (starting) | -6 / -5 | 11 | -3 / -3; +0 / -2 | 1.05x / 1.00x | 9.8 / 10.8 s | fails |
| **`bloom-m35-r1.0`** | +0 / +0 | **0** | +0 / +2; +0 / +0 | 1.65x / 1.74x | 9.8 / 10.7 s | **passes** |
| `bloom-m35-r1.2` | +2 / -1 | **0** (gains 1) | -1 / +1; +0 / +1 | 1.45x / 1.52x | 9.8 / 10.7 s | **passes** |
| `bloom-m50-r1.0` | +0 / -2 | 2 | +0 / +2; +0 / +0 | 2.13x / 2.23x | 9.8 / 10.7 s | fails by 1 |
| `bloom-m35-h80-r1.2` | -2 / +0 | 2 | -3 / +0; +0 / +0 | 1.48x / 1.58x | 7.4 / 10.2 s | fails by 1 |
| `bloom-m50-r1.2` | +0 / -3 | 3 | -1 / +1; +0 / +1 | 1.76x / 1.85x | 9.8 / 10.7 s | fails |
| `bloom-m50-h80-r1.2` | -3 / +0 | 3 | -3 / +0; +0 / -1 | 1.82x / 1.93x | 7.4 / 10.2 s | fails |
| `bloom-m75-h80-r1.2` | -4 / +0 | 4 | -3 / +0; +0 / -2 | 2.26x / 2.38x | 7.4 / 10.2 s | fails |

A completion is a coin of about 0.4 for the clumsy cohort, so 400 runs carry a binomial SD of about 10; paired seeds take
most of it away, but neighbours still swap by 2-3 (`bloom-m35-h80-r1.2` and `bloom-m35-r1.2` differ by 4 at 1-200 for a
change of health alone). A 1-in-800 bar sits inside that noise: "passes" means "no cost these 800 runs can see", and
`bloom-m50-r1.0` (2) is not measurably different from the two that pass.

### Slow hands, roles, the gate (the passing variants)

**Slow hands** (careful and standing, `FOUNDRY_REALISM=slow`, against the Captain with slow hands on the same seeds):

| Option | Seeds | Careful F3 V / I (vs Captain) | Completed careful / standing (vs Captain) | Elite fight P50 careful / standing | Thorn Post lost careful (maul) / standing (maul) |
|---|---|---|---|---|---|
| `shipped` | 1-200; 201-400 | 137 / 141; 151 / 150 | 278 / 254; 301 / 275 | 2.6 / 4.5; 2.7 / 4.4 s | 19.5 / 13.6; 17.3 / 13.5 |
| `bloom` | 1-200; 201-400 | -7 / -8; -10 / -1 | -15 / -17; -11 / -10 | 10.4 / 11.1; 10.4 / 10.9 s | 57.4 / 47.6; 51.1 / 44.5 |
| `bloom-m35-r1.2` | 1-200; 201-400 | +1 / -2; +0 / +2 | -1 / -7; +2 / -1 | 10.4 / 10.8 s | 17.1 (8.3) / 17.3 (13.4); 16.0 (8.3) / 16.5 (12.5) |
| `bloom-m35-r1.0` | 1-200; 201-400 | +1 / -2; +1 / +5 | -1 / +2; +6 / +3 | 10.3 / 10.8 s | 12.1 (3.7) / 16.2 (12.3); 11.4 (3.7) / 15.6 (11.8) |

The slow pilot's maul at reach 1.0 is 3.3-4.0 a fight against the typical pilot's 1.0-1.1, and at 1.2 about 8 against
3: the reach read is a hands read too, but a small one, and no slow cell leaves careful's clears or completions more
than 2 below the Captain's. The starting values cost the slow careful pilot up to 10 clears and 11-15 completions a
range; the passing variants nothing.

**Roles** (careful Vanguard started with each weapon, seeds 1-200; damage a second on the horde, the elite leader, the
gate boss; Sword / Staff / Daggers):

| Option | Horde | Elite leader | Gate boss | Roles | Staff on the horde | Daggers on the leader | Sword on the gate |
|---|---|---|---|---|---|---|---|
| `shipped` (Captain) | 27.8 / 86.9 / 3.8 | 37.3 / 53.1 / 77.3 | 41.3 / 35.8 / 5.7 | met | +212 % | +46 % | +15 % |
| `bloom` | 27.7 / 87.4 / 3.4 | 26.9 / 29.2 / 54.6 | 41.5 / 35.6 / 5.2 | met | +216 % | +87 % | +17 % |
| `bloom-m35-r1.2` | 27.3 / 87.8 / 3.7 | 26.9 / 28.6 / 54.4 | 41.0 / 36.1 / 5.7 | met | +222 % | +90 % | +14 % |
| `bloom-m35-r1.0` | 27.3 / 87.4 / 3.6 | 27.1 / 29.0 / 55.5 | 41.0 / 35.1 / 5.3 | met | +220 % | +91 % | +17 % |

The bark blunts the Sword and the Staff on the leader by a quarter to a half; the Daggers' x2 and their step-in keep them
the Bloom's weapon by about 90 % (section 8 asks more than 10 %).

**The gate and the Descent** (`bloom-m35-r1.0`, every playing cohort, both ranges): the floor-3 Warden's fight P50 47-63 s
against the Captain floor's 47-63 s (in docs/40's 45-65 s); floor 3 P50 242-255 s against 241-254; the Descent P50
13.9-14.6 min against 13.9-14.6. The auto-farmer: 3 / 2 floor-3 clears of 5 / 7 against 4 / 2. Thorn Post deaths of the
four playing cohorts: 0 and 2 against the Captain's 2 and 6.

### Recommendation

**Build `bloom-m35-r1.0` in step 2:** health 280 (549 on floor 3), the cycle as section 5 has it (closed 3.5 s taking
25 %, open 2.5 s rooted and taking all), and the **Bark Maul at 3.8 every 1.8 s, reach 1.0, 0.6 s before the first**
(35 % of 10.8; 6.8 a blow on floor 3), speed 1.2. Against the Captain on seeds 1-400:

| Row (section 8, the lead's rule) | `bloom-m35-r1.0` | |
|---|---|---|
| Clumsy completions, 1-400 | 306 against 306 (+0 / +0): cost 0 in 800 | met |
| Careful floor-3 clears within 3, each range | +0 / +2; +0 / +0 | met |
| Elite fight P50 6-12 s, careful and standing | 9.8 / 10.7 s on both ranges (P90 11.5-13.5 / 12.1-13.3) | met |
| Standing at least 2x careful at Thorn Post | 1.65x / 1.74x (16.3 / 9.9; 14.6 / 8.4); the maul alone 13.4 / 1.1 = about 12x | **missed** (see below) |
| Clumsy at most 1.3x careful | 3.0x / 3.8x (30.2 / 9.9) | **missed** |
| Daggers' lead on the leader above 10 % | +91 % (the Captain: +46 %) | met |
| First-card completion | +5 / +10 | met |
| Floor-3 Warden in 45-65 s, Descent P50 13.8-14.8 | 47-63 s; 13.9-14.6 min | met |
| Slow hands: careful within 3 | +1 / -2; +1 / +5 | met |

**Runner-up: `bloom-m35-r1.2`** (the same maul at reach 1.2): cost 0 in 800 (+2 / -1), careful -1 / +1 and +0 / +1,
standing / careful 1.45x / 1.52x, the Daggers +90 %. The longer reach lets a careful pilot's lapses through (its maul 3 a
fight at typical hands, 8 at slow, against 1 and 4 at 1.0) and buys nothing back. If the lead would rather have the
separation row than the strict clumsy bar, **`bloom-m50-r1.0`** (maul 5.4) meets 2x (2.13x / 2.23x) at a cost of 2 in 800
(+0 / -2), every other row as `m35-r1.0`: one completion over a bar that sits inside the sample's noise.

**Why the separation row is missed, and what it would take.** Section 8's "standing at least 2x careful at Thorn Post"
reads all the health lost while the wave stands. Careful's 8-10 of it is the escort's and the horde's (it kites through
them), which no Bloom lever touches; the maul is the only thing the levers move, and the clumsy cohort pays it as the
standing one does (its straight kite stalls, and its fight is 12-15 s, mostly beside the pit). So the bar a maul must
pass for the clumsy cohort (about 15 a fight) caps standing's total at 15-22. Two readings for the lead:
- Read the row on the maul alone (standing about 12x careful at `m35-r1.0`), which is what section 5 means by "standing
  pays the maul". Then `m35-r1.0` meets it.
- Or keep the row and accept 2 in 800 (`m50-r1.0`), or give the clumsy cohort a counter the sim does not model (its kite
  stalls at walls; docs/41's fixes), which is a pilot change, not a creature lever.

The "clumsy at most 1.3x careful" row is missed for the same reason and cannot be met by any lever here: the clumsy
cohort loses 21-31 at Thorn Post to the Captain already (1.3-1.6x careful's), and 30-34 to the Bloom. Its completions,
not its health at Thorn Post, are what the lead's rule protects, and they hold.

**Risks for the build.**
- **Reach 1.0.** The Bloom stops 0.9 from the hero (its reach less `PackMotion.ReachMargin`), closer than any foundry
  enemy (1.4-1.6). Its body sprite will overlap the hero's at that distance on the phone; check the read in the art pass,
  and keep 1.0 in the rule only if it still reads as "in the maul". 1.2 (the runner-up) stops at 1.1.
- **The read is the pilot model's reaction.** Reach 1.0 works because a 0.25 s reaction lets a 1.2-speed Bloom close
  0.3 from the kite's 1.6-1.65. A player who stands to trade blows takes the maul as the standing cohort does (12-13 a
  fight, 6.8 a blow); nothing tells that player to step back except the windup frames. The maul's windup pair and the
  bark chip (section 5's readability) carry the whole read.
- **D is 230, not 60-200.** The Bloom falls in its first window in 39 % of careful fights, in its second in 55 %, later
  in 5 %; 62-70 % fall while open. A stronger build at floor 3 (the evolutions) shortens the fight toward 6 s; the cycle
  keeps it above 3.5 s plus the walk-in whatever the build.
- **Noise.** A 1-in-800 bar sits inside the sample: `m35-r1.2` and `m35-h80-r1.2`, one lever apart, differ by 4
  completions on seeds 1-200. Re-read the chosen variant on seeds 401-600 if the lead wants a third range before the art.

### What the scene build (step 2) needs

- `BloomRule` and `BloomClock` are in `Scripts/Combat` already; the scene adds a `GloamService` at script order -38
  (after `WardenSlamService` -40 and `MirrorGuardService` -39, before every attacker), stepping each standing Bloom's
  clock while the encounter runs and holding it under the reveal and a panel, as the simulation does.
- A bark component on the Bloom's prefab ahead of `Health` (`IDamageGuard`, as `MirrorGuard`), returning
  `BloomClock.Land(amount)` for **every** landing, a burn's tick included (unlike the mirror, which passes ticks).
- Rooting and the held maul: `PackMotion.SetSpeed(slot, BloomClock.Speed(speed))` each step (the method exists, unused);
  the Bloom's `AttackController` held while open, as `WardenSlamService` holds the Warden's (no tick, no strike).
- `Enemy_CinderBloom` (280 health, speed 1.2, gold 20, experience 10) and `Weapon_BarkMaul` (3.78 every 1.8 s, reach 1.0,
  0.6 s initial delay), leading `Floor_GloamGardens`' Thorn Post; `EliteRule` takes it as the room's first enemy with no
  change. The step-in already targets the elite room's leader (the simulation's `_wave[0]`), so the careful pilot steps
  in on the Bloom with the Daggers; `DevelopmentAutopilot` needs no new habit.
- Then: `DescentSimulation.GloamGardens`' Thorn Post holds `CinderBloom` at these numbers (the levers stay for research),
  `FoundrySimulationSceneValueTests` holds the asset to it and the script order, and the default batch moves on floor 3
  and the finale only (the cells above). A PlayMode flow test on a proof floor (closed bark, open window, rooted, maul
  held) as `MirrorSentryFlowTests` does.

### The default batch

Run before (main 78f70a4d, the Sentry `r0-f70`) and after (this branch's head) with `FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0
FOUNDRY_COHORTS=1 FOUNDRY_REALISM=typical`, `FOUNDRY_FLOORS=4` and `FOUNDRY_FLOORS=2`: every CSV, `foundry-gate.md`,
`foundry-cohorts.md` and `foundry-finale.md` match byte for byte; in `foundry-batch.md` only the clock in its header and
the wall-seconds column differ. The same held at the first research commit against b85969c2's floor 2 (2026-11-12).
CombatChecks: 830 green (`Başarılı`); the Unity compile check green.
## Cinder Bloom art, pass 1 (2026-11-14)

In Assets, not yet imported by Unity, not yet worn by any prefab (the runtime build, section 9 step 2, wires it).
- **Pipeline.** `Tools/ArtReview/BuildBlenderCinderBloom.py` (Blender 4.5, the Mirror Sentry's rig, solver, camera and
  aux-pass method), `PackCinderBloomSprites.py`, `review_cinder_bloom.py`. The bloom, the furnace seam and rear grate and
  the eyes render black in the beauty pass; the aux pass writes their coverage, tone and glow, so the packer lays the
  orchid ramp (`#5A1F66` -> `#D86BD0` -> `#F6C2F0`) and its value without rendering again.
- **Look.** A squat foundry golem in charred bark plates over a cold iron furnace barrel, a small head sunk under two dead
  branches, a cold chimney stack with a twig on its back, verdigris hoops; a bark maul (a log bound in verdigris).
  CLOSED: two bark shutters over the furnace with a thin orchid seam, the maul on the right shoulder. OPEN: feet planted,
  roots into the loam, the maul's head grounded, the shutters swung wide and the bloom out of the chest, about a third of
  the figure in orchid. No red-orange and no gloamlight anywhere (rule 3): warm and chartreuse texels counted, 0.
- **Contract.** `Art/Characters/CinderBloom/CinderBloomArtSet.asset` is GruntCaptainArtSet line for line (44 textures),
  `_frost` and `_gloam` empty (rule 6: no themed copy, so EnemyThemeArtTests' painted list is unchanged). The open state
  (unfold-0, unfold-1, open in each facing, each with a flash) and a three-frame bark chip lie beside the set; closing
  plays the unfold back. `CinderBloomArtSetTests` (Editor) checks the contract, the open state, the imports and the budget.
- **Budget.** 59 textures, 1,307,392 bytes as ASTC 4x4 (closed 0.70 MB, open 0.30, death 0.30, chip 0.01) against the
  Sentry's 1.07 MB; drawn at 0.88 of the model, 1.21 units tall (the Captain 1.06), about 145 px on the S23.
- **Review.** `ArtDirection/2026-11-14/cinder-bloom-01/` (untracked): states, cycle, motion, the phone-scale lineup with
  its value map, the squint test, the bark chip, checks.json and sprites.json (the runtime's list of the open drawings).
- **Polish (the lead's look, 2026-11-15).** Approved; orchid only (no gloamlight stamen). The petals are smooth-shaded
  and finer, in two layers that read as a lotus: six broad pointed outer petals laid wide with curling tips, five narrow
  inner petals standing up as a lighter cup in front, each creased down its middle. The closed rear grate is deep plum,
  near the bark, so only the open grate reads pink; the open halo a little stronger. Open front orchid mean luminance
  0.51, open rear 0.50; the pass-1 sheets are kept beside the new ones as `*-pass1.png`.

## Lead decisions 2026-11-15 (on the Bloom's measurement)

1. **Ship `bloom-m35-r1.0`.** It passes the relative rule (clumsy 306 against 306 over seeds 1-400, careful within 3 on
   each range), and the maul alone separates the standing cohort from the careful one by about 12x.
2. **"Standing at least 2x careful at Thorn Post" becomes a reported row, not a gate.** Careful's share of it is the
   escort's and the horde's, which no Bloom lever touches. **"Clumsy at most 1.3x careful" likewise** (the Captain
   already misses it: 1.3-1.6x).
3. **The rule is not relaxed for `bloom-m50-r1.0`** (a cost of 2 in 800).
4. **Fallback:** if the Bloom's look fails at reach 1.0 (it stands 0.9 from the hero), ship `bloom-m35-r1.2`.
5. **The bark blunts burns too**: accepted; the scene and the simulation stay equal.
6. **Step 2 (the scene) now**, as "What the scene build (step 2) needs" lists, with the art of 2026-11-14
   (`CinderBloomArtSet` and the open-state sprites the runtime holds itself).

Later the same day, as the integrated code and the capture came in (Step 2 below holds the readings):

7. **Reach 1.2** for readability (the Thorn Post capture), not the measurement's 1.0.
8. **The rule is kept after a failed reading**: no "noise" exemption. The maul was swept at reach 1.2, then the health on
   the shipped floor 2 (the Glass Regent at 10 % / 1.7 s, f22dc7ad, settled).
9. **Ship `bloom-m20-h70-r1.2`** (196 health, the maul 2.16 at reach 1.2): cost 0 in 800, careful within 3. Its careful
   elite fight, 5.9 / 6.0 s, is recorded as at the floor of section 8's 6-12 s band, not a fail.
10. **The lesson**: the 1-in-800 tolerance sits inside run-divergence noise; the next creature's research reads seeds
    1-800 (paired, the same bar scaled) before deciding.

## Step 2: the Cinder Bloom in the game (2026-11-15)

Built on main 40a7df7d (the Glass Regent as shipped at 10 % / 1.7 s, the map unlock, the Bloom's art and this research).
**It ships `bloom-m20-h70-r1.2`**: 196 health (70 % of docs/52's 280; 384 on floor 3), the Bark Maul 2.16 every 1.8 s
(20 % of docs/52's 10.8; 3.9 a blow on floor 3) at reach 1.2, the cycle as section 5 has it (closed 3.5 s taking 25 %,
open 2.5 s rooted and taking all). How it got there is the next section.

### Why reach 1.2, a maul of 20 % and 70 % health: three readings at the integrated code

The rule (the lead's, 2026-11-12, kept after a failed reading - no noise exemption): over seeds 1-400 the creature may cost
the clumsy cohort at most 1 four-floor completion in 800 against the shipped floor on the same seeds (here the Grunt
Captain at Thorn Post), careful within 3 floor-3 clears on each range. Every reading below: seeds 1-200 and 201-400, the
careful, standing, first-card and clumsy cohorts, typical hands, both heroes, the four-floor Descent; "maul only" is the
leader's own damage at Thorn Post, standing over careful.

**Reach 1.2, for the look.** The Thorn Post capture (`Run-ThornPostProof.ps1`, the lead's run on verify-1115) put the
reach-1.0 Bloom 0.9-1.04 from the hero: from the camera's side the hero covered much of the Bloom, from the other the
Bloom covered the hero. The lead took the fallback's reach 1.2 (it stops 1.1 away; 1.25 walked up in the capture's second
run).

**Reading 1, at 4608af42** (the Regent's lanes made to reach 4 past the hero; Captain clumsy 153 / 148). The measurement's
pick and its fallback failed, so the maul was swept at reach 1.2:

| Thorn Post | Careful F3 V / I vs Captain, 1-200; 201-400 | Clumsy 1-200 / 201-400 | Cost in 800 | Standing / careful at Thorn Post | Maul only | Elite fight careful / standing |
|---|---|---|---|---|---|---|
| Captain | 190 / 192; 193 / 193 | 153 / 148 | - | 0.83x / 0.78x | 1.1x / 1.0x | 2.5 / 4.3 s |
| `bloom-m35-r1.0` (the measurement's pick) | +1 / +0; +0 / +0 | 156 / 142 | 3 | 1.75x / 1.89x | 11.6x / 9.1x | 9.7 / 10.7 s |
| `bloom-m35-r1.2` | +2 / +0; +1 / +0 | 153 / 143 | 5 | 1.58x / 1.62x | 4.8x / 4.1x | 9.7 / 10.7 s |
| `bloom-m30-r1.2` | +2 / +0; +1 / +0 | 154 / 143 | 4 | 1.45x / 1.50x | 4.8x / 4.1x | 9.7 / 10.7 s |
| `bloom-m25-r1.2` | +2 / +0; +1 / +0 | 154 / 144 | 3 | 1.30x / 1.36x | 4.8x / 4.1x | 9.7 / 10.7 s |
| `bloom-m20-r1.2` | +2 / +0; +1 / +0 | 155 / 145 | **1, passes** | 1.14x / 1.21x | 4.8x / 4.1x | 9.7 / 10.7 s |

**Reading 2, at 6c9bb2ca** (the Regent's lane at 6 %): `bloom-m20-r1.2` against the Captain, clumsy 158 / 146 against 156 /
149 - **a cost of 1, passes**; careful +2 / +0 and +1 / +0; maul only 4.8x / 4.1x.

**Reading 3, at f22dc7ad** (the Regent as it ships, 10 % / 1.7 s; the lead: no further floor-2 re-tunes). `bloom-m20-r1.2`
**failed** (clumsy 158 / 147 against 159 / 150, a cost of 4). The lead's decision: attack the cost driver, the fight's
length, with the Bloom's health:

| Thorn Post, at f22dc7ad | Careful F3 V / I vs Captain, 1-200; 201-400 | Clumsy 1-200 / 201-400 | Cost in 800 | Elite fight careful / standing | Standing / careful at Thorn Post | Maul only |
|---|---|---|---|---|---|---|
| Captain | 190 / 193; 195 / 192 | 159 / 150 | - | 2.5 / 4.3 s | 0.85x / 0.78x | 1.0x / 1.0x |
| `bloom-m20-r1.2` (100 % health) | +0 / -2; +0 / -1 | 158 / 147 | 4 | 9.7 / 10.7 s | 1.24x / 1.15x | 4.6x / 3.9x |
| `bloom-m35-h80-r1.2` | +0 / +1; +0 / -1 | 161 / 146 | 2 | 7.3 / 10.2 s | 1.75x / 1.58x | 5.4x / 4.6x |
| `bloom-m30-h80-r1.2` | +0 / +1; +0 / -1 | 161 / 146 | 2 | 7.3 / 10.2 s | 1.61x / 1.45x | 5.4x / 4.6x |
| `bloom-m25-h80-r1.2` | +0 / +1; +0 / -1 | 161 / 146 | 2 | 7.3 / 10.2 s | 1.45x / 1.31x | 5.4x / 4.6x |
| `bloom-m20-h80-r1.2` | +0 / +1; +0 / -1 | 161 / 146 | 2 | 7.3 / 10.2 s | 1.29x / 1.16x | 5.4x / 4.6x |
| `bloom-m30-h70-r1.2` | +0 / +1; -1 / +0 | 161 / 146 | 2 | 5.9 / 9.9 s | 1.62x / 1.44x | 5.5x / 4.7x |
| **`bloom-m20-h70-r1.2`** | +0 / +1; -1 / +0 | 161 / 148 | **0, passes** | 5.9 / 9.9 s (6.0 / 10.0 on 201-400) | 1.30x / 1.15x | 5.5x / 4.7x |

`bloom-m20-h70-r1.2` ships (the lead's decision): it passes the rule, and its careful elite fight, 5.9 / 6.0 s, sits at the
floor of section 8's 6-12 s band - recorded as at the band's floor, not a fail. Over both ranges at f22dc7ad first-card
gains 15 completions, standing 4, careful none; Thorn Post deaths of the four cohorts 1 and 3 against the Captain's 2 and
4.

**Why health moved: the fight's length drives the cost.** At reach 1.2 the 201-400 clumsy completions are 143-145 for
every maul from 35 % down to 20 % (4608af42), and 146 for every 80 % variant at f22dc7ad: the maul's weight barely moves
them. What moves them is how long Thorn Post lasts - 9.7-10.7 s at full health against the Captain's 2.5-5.9 s, every
second of it beside the pit and the horde - and 80 % health (7.3 s) gains about 2 on 1-200, 70 % (5.9 s) the rest. The
standing cohort, whom the Bloom walks up to, still pays its maul 4.6-5.5x what the careful one does at every weight; by
the whole of Thorn Post it is 1.15-1.30x (the escort and the horde, a reported row since the lead's decisions).

**The seed-level finding: the cost is divergence at the gate.** Seed for seed (`bloom-m25-h80-r1.2` against the Captain,
clumsy, 201-400), the Bloom loses 7 runs and gains 3, and every one of the ten ends at the Warden's Grove - four to the pit,
three to the horde in the losses. Some of the lost runs left Thorn Post far worse off (70 health against 41, 95 against
48); others left it exactly as the Captain's did (8.1 against 8.1) and still died at the gate: once the elite fight
changes, the rest of the run is a different run, and the Grove's pit and horde decide it differently. The same holds for
every variant: a creature that changes a fight on floor 3 reshuffles which clumsy runs survive the gate, and the net of
that reshuffle is a few completions either way.

**A lesson for the next creature.** The 1-in-800 tolerance sits inside that run-divergence noise: a variant passed at two
bases and failed at a third that changed only floor 2's boss (`bloom-m20-r1.2`: -1, -1, then -4), and neighbouring variants
one lever apart read 2-4 completions apart. A pass or a fail can flip with any upstream change. **The next creature's
research should read seeds 1-800** (paired, the same bar scaled: at most 2 completions in 1,600) before deciding, and
re-read on the shipped base after each upstream re-tune that lands before it ships.

### What was built

- `Scripts/Combat/CinderBloom.cs`, on the Bloom's prefab ahead of its `Health` (`IDamageGuard`, as `MirrorGuard`): while
  closed it returns `BloomClock.Land` for every landing - weapons, procs, the ability, a burn's tick, the pit's spill -
  and the whole landing while open. Until `GloamService` has stepped it (the frame it walks in, a hall arena) it guards
  nothing.
- `Scripts/Core/GloamService.cs` at script order -38 (after `WardenSlamService` -40 and `MirrorGuardService` -39, before
  every attacker), foundry only: each running frame it steps every standing Bloom's clock (not on the frame it walks in),
  roots it while open (`EncounterController.SetWalkShare`, through `PackMotion.SetSpeed`, from the pack's next step), holds
  its maul (`AttackController.IsHeld`: no tick, no strike) and hands it the hero (`CinderBloom.Hero`). It holds whenever
  the encounter holds: the reveal, an open choice, the pause. The simulation steps the same `BloomClock` at the same point
  of its frame.
- `Scripts/UI/CinderBloomView.cs`: the closed Bloom is `CinderBloomArtSet` on `EnemyLookView`, facing as it walks; while
  open the view lays the open-state frames over the body after the look has drawn (unfold-0 and unfold-1 0.08 s each as
  the window opens, open, then unfold-1 and unfold-0 in its last 0.16 s; `BloomRule.OpenFor`) and turns the rooted Bloom
  to the hero: mirrored when the hero is to its right, its rear only when the hero stands clearly farther from the camera
  (the walk's thresholds), its open front otherwise - the open bloom is the read. The open frames' flashes go to
  `EnemyLookView.AddFlash`. A landing the closed bark blunted tints the hit flash 65 % toward bark `#65574C`
  (`CombatantView.FlashTint`) and plays the three-frame bark chip at the chest (0.46 up, 0.08 s a frame).
- `Enemy_CinderBloom` (196 health, speed 1.2, 20 gold, 10 experience) and `Weapon_BarkMaul` (2.16 every 1.8 s, reach 1.2,
  0.6 s before the first); `CinderBloom.prefab`, a variant of the Grunt's wearing `CinderBloomArtSet` with the Captain's
  look and the two components; `Floor_GloamGardens`' Thorn Post led by it (its Grunt and two Cinder Mites unchanged);
  `Resources/Development/Floor_BloomProof` (the Bloom alone in an elite room).
- The simulation ships it: `DescentSimulation.CinderBloom` (the asset's numbers) leads `GloamGardens`' Thorn Post, and its
  clock steps wherever a Bloom stands. `FoundryLevers.GloamCaptain` puts the Captain back for comparisons;
  `BloomHealthScale` is read against docs/52's 280 (the shipped 0.7), and the research's `bloom-*` options start from
  docs/52's 280 health and 10.8 at 1.5 as before, so every cell above keeps its meaning (`bloom-m20-h70-r1.2` is the
  shipped Bloom, run for run).
- Tests: `TheCinderBloomIsTheSimulations` and the script order in `FoundrySimulationSceneValueTests`, floor 3's elite in
  `ClimbFloorContentTests` and `FoundryFloorThreeTests`, `BloomRule.OpenFor` in `BloomRuleTests`, and the PlayMode
  `CinderBloomFlowTests` on `floor_bloom_proof`, read in game seconds: through a whole open window the closed landings are
  a quarter of the Daggers' landing and the open ones whole, no maul and the open frames while open; rooted for the rest of
  a window with the hero 3 away, turned to the hero's side, walking again once closed; no maul on a hero kept 1.5 away and
  every maul within 1.2; a level-up panel holding the clock and the Bloom's place. CombatChecks 838 green ("Başarılı"),
  the Unity compile check green; the lead's Unity runs at the reach-1.2 wip: EditMode 41 / 41, PlayMode 6 / 6.
- `Tools/ArtReview/Run-ThornPostProof.ps1` (with `ThornPostCapture.cs`): the Bloom at its stopping distance on the proof
  floor dressed as the garden, the hero left, right, below and above it, each closed, opening and open, close up and at
  the portrait framing; `overlap.txt` gives the two bodies' sprite boxes (the Bloom's body renderer, the hero's puppet
  parts) and their overlap, the facing and which body is nearer the camera (`ArtDirection/2026-11-15/thorn-post-01`).

### The default batch

Against f22dc7ad (the Glass Regent as shipped; main 40a7df7d adds only docs), `FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0
FOUNDRY_COHORTS=1 FOUNDRY_REALISM=typical`: with `FOUNDRY_FLOORS=2` every file is byte-identical but `foundry-batch.md`'s
clock and wall-seconds column; with `FOUNDRY_FLOORS=4`, `foundry-gate.md`, `foundry-batch.csv`, `foundry-batch-floor2.csv`
and `foundry-cohorts-floor2.csv` are byte-identical and every floor-1 and floor-2 line of the reports is the same; floor 3
and the finale move:

| Cohort, Vanguard / Ilya | Floor 3 cleared, Captain | Bloom | Completed the Descent, Captain | Bloom |
|---|---|---|---|---|
| careful | 190 / 193 | 190 / 194 | 383 | 384 |
| standing | 175 / 167 | 177 / 166 | 342 | 343 |
| first-card | 168 / 178 | 172 / 184 | 346 | 356 |
| clumsy | 59 / 100 | 61 / 100 | 159 | 161 |
| auto-farmer | 5 / 4 | 5 / 6 | 9 | 11 |

These are GloamEnemiesResearch's seeds-1-200 cells at f22dc7ad, the Captain's and `bloom-m20-h70-r1.2`'s, run for run. For
the playing cohorts floor 3's Warden P50 is 46-62 s before and after, the Descent P50 13.8-14.5 min both.

**Risks the build leaves open.**
- **A thin margin, inside the noise.** The rule passes by 0 at f22dc7ad; the lesson above says what that is worth. Re-read
  `GLOAM_OPTIONS=shipped,captain` on both ranges after any change to floors 1-3 before the next APK.
- **The elite fight at the band's floor.** 5.9-6.0 s for the careful pilot; a stronger build or a later damage change
  would take it under 6 s.
- **The read is light.** The standing cohort pays the maul 4.6-5.5x what the careful one does, but it is about 7 a fight;
  the Bloom's weight on floor 3 is its cycle. If the owner's play finds Thorn Post toothless, the maul is the lever, with a
  re-read.
- The closed Bloom faces as it walked (as every enemy does when it stands); only the open one turns to the hero.
- The open bloom's frames are drawn by a second view over the look's (`LateUpdate`); a look change that draws later would
  hide them. The flow test reads them through a window.

## Pollen Wraith and Thornling art, pass 1 (2026-11-15)

In Assets, not yet imported by Unity, not yet worn by any prefab (the runtime builds, section 9's Wraith and Thornling
steps, wire them).
- **Pipeline.** `Tools/ArtReview/BuildBlenderGloamHorde.py --creature wraith|thornling` (Blender 4.5, the Bloom's toon
  group, solver, camera and aux-pass method), `PackGloamHordeSprites.py`, `review_gloam_horde.py`. The Wraith's seed
  core and the Thornling's thorn tips and eyes render black in the beauty pass; the aux pass writes their coverage, tone
  and glow, so the packer lays the plum and orchid ramps without rendering again. No red-orange and no gloamlight on
  either body (warm and chartreuse texels counted on every drawing: 0).
- **Pollen Wraith** (`Art/Characters/PollenWraith`). A small pale-orchid mote hovering about 0.14 over the loam: a
  teardrop of pollen-down (`#F6C2F0` lit, plainly orchid in its mid and shade, never ash-white), a dark plum seed core
  with a wet glint on its face, six orchid bracts cupping it, a crown of seven pappus wisps, three filaments trailing
  behind. It drifts (step A/B bob it), draws back and snaps (bracts flared), and bursts: the puff pops, the wisps fly,
  the husk falls and greys, shedding pollen motes. `PollenWraithArtSet` is CinderMiteArtSet line for line (36 textures:
  4 bodies a facing and the windup with flashes, 7+7 deaths listed nine steps, 2 remains), with its own visual top
  (0.61), shadow (0.34: it hovers) and drift (0.9); `_frost`/`_gloam` empty. 148,720 bytes as ASTC 4x4 against the
  Mite's 256,624; 0.28 wide and 0.51 tall (the Mite 0.51 and 0.50). Effects beside it: three pollen motes and a
  four-frame pollen bloom (49,920 bytes).
- **Thornling** (`Art/Characters/Thornling`). A lean, hunched runner of dark bark (mean luminance 0.23 against the
  loam's 0.30 and the Runner recolour's 0.32), a thorn ridge down its back, crown, collar, shoulder, elbow and forearm
  thorns with orchid tips, three thorn claws a hand, a root tail; it lopes, rakes, dies flat into the ash.
  `ThornlingArtSet` is RunnerArtSet line for line (40 textures), visual top 1.01, shadow 0.5, stride 1.0. Beside it, in
  each facing: dive-0..2 (gathering, head first, nearly gone), erupt-0..2 (breaking the ash, half out, airborne),
  stuck-0..1 (claws caught, heaving), a holdout plane at the floor cutting what is under the ash and a ring of disturbed
  ash painted where the body meets it; those above the ash have flashes (dive-1, dive-2, erupt-0 none). Effects: the
  ash puff (3), the thorn burst (3), and the ring as a radial pair at a radius of one world unit (scale by 1.2; the
  pollen meter reuses it at 0.8) - edge (128 texels a unit) and fill (64) - in orchid and in warning `#FF5A3C` for its
  last second. 910,752 bytes in all (set 474,304, beside it 293,088, effects 143,360).
- **Tests.** `PollenWraithArtSetTests` and `ThornlingArtSetTests` (Editor: the contract against the Mite's and the
  Runner's sets, no themed copy, the drawings and effects beside the set, the imports, the Wraith's set at or under the
  Mite's memory, the Thornling's budget); `EnemyThemeArtTests.FloorThreesOwnEnemiesHaveNoThemedCopies` names the Bloom,
  the Wraith and the Thornling. Neither file matches CombatChecks' `*ArtTests.cs` glob. The Unity compile check is
  green; the EditMode suite is unrun (no Unity in a worktree).
- **Review.** `ArtDirection/2026-11-15/gloam-horde-01/` (untracked): the two sheets, the dive strip with the ring, the
  phone-scale crowd (32 Wraiths, Thornlings, Grunts, the Bloom, the Vanguard, Ilya among Wraiths), its value map, the
  squint, checks.json, and `pollen-wraith/sprites.json`, `thornling/sprites.json` (the runtime's list of everything
  beside the sets); the first look kept beside as `*-pass1.*`, the improvement pass before the stuck claws sank as
  `*-pass2.*`.
