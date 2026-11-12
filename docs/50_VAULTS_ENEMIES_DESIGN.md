# 50 - The Quicksilver Vaults' own enemies (design proposal, 2026-11-12)

A proposal for the owner's decision. Nothing here is built: no code, asset, scene or setting changed. Numbers cited as
"measured" name their source in docs/41, docs/47 or docs/48. Every other number is a starting value for the simulation
to measure, not a decision.

**Scope.** Floor 2 is the frozen Quicksilver Vaults. Today it has an ice region (docs/49) and frosted copies of the
foundry's six enemies (`EnemyArtSet.Frosted`, `Tools/ArtReview/GenerateIceEnemyArt.py`: cold silver bodies with a violet
glow). It plays the foundry's roster and the Forge Warden. docs/44 (section 1.6, F2 row) gives the floor its own
family: **Mirror Sentry, Silver Wisp, Faceless Clerk**, and the boss **The Glass Regent** ("liquid mirror, copies,
narrow sight lines"). This document designs the four so that each can be built, simulated and measured one at a time.

Like docs/46 (the Riftblade) and docs/48 (a third floor), this is post-V0.1 content (docs/40 section 15.1: V0.1 is one
floor). Building it should wait for the V0.1 gate's device row (the S23's frame time at the chosen crowd). The
simulation research in step 1 of section 8 can start earlier, since it touches nothing the game ships.

## 0. The short version

| Creature | Role on floor 2 | Signature behaviour | Counter-play it asks for | Weapon it favours |
|---|---|---|---|---|
| **Silver Wisp** | Replaces the Runner (horde role 2 and the halls' Runner) | Blinks once to the mirrored spot ahead of the hero | Turn and fight; do not kite in a straight line | The Staff (the horde's weapon) |
| **Faceless Clerk** | Adds a back-line role the foundry lacks (docs/40 8.5's caster/disruptor), node waves only | Takes the hero's face: a copy appears where the hero stands, draws the pack, and shatters | Step off the copy, then walk to the Clerk | Option A: the Staff; option B ("faceless"): the Daggers and the Sword |
| **Mirror Sentry** | Replaces the Grunt Captain as floor 2's elite (Sentry Hall) | A mirror on its front turns slowly: hits from the front are blunted and partly reflected | Circle it and strike its flank | The Daggers (the elites' weapon) |
| **The Glass Regent** | Replaces the Forge Warden at floor 2's gate (staged, section 6) | Sight-line lanes instead of circles; at half health two glass copies cast lanes with it | Sidestep a lane; pick the real Regent out of three | The Sword (the gate boss's weapon) |

The three weapon jobs that docs/41 settled (Sword = gate boss, Staff = horde, Daggers = elites) carry over to floor 2
unchanged. Each creature's mechanic is shaped so that the job's weapon is the natural answer to it.

Recommended order: Sentry, then Wisp, then Clerk, then the Regent (section 8). Rough total: about 3-4 weeks of agent
work, about half of it art. Simulation research only: about 4 days.

## 1. What floor 2 is today

Measured and shipped (docs/41, 2026-11-10 to 2026-11-12; `Floor_QuicksilverVaults.asset`):

- **Rooms.** Mercury Stair, Cold Crucible, Vault Gate (three halls), The Deep Forge, Sentry Hall (elite: Grunt
  Captain, Grunt, two Mites), Warden's Vault (gate: Forge Warden with two Cinder Mites). No hall surges, no extra
  elite. The gate opens at 3:30 on the floor's clock.
- **Tier.** `ClimbScaling` floor 2: enemy health x1.4, damage x1.288, plus Cursed Gold (+20 % damage, +50 % gold).
  Horde enemies take `HordeRule.HealthShare` 0.3 and `DamageShare` 0.5 of that.
- **Horde.** Roles Mite, Grunt, Runner (lightest first). `HordeRule.Weight(state, role) = max(0, 2 + pressure - role)`,
  so the Runner (role 2) is 0 % of the Ramp, 17 % of the Horde state and 22 % of Pressure. Targets 4/10/16/6, grown by
  the clock; scene cap 40.
- **Warden.** `WardenRule`: a mark where the hero stands every 6 s (4 s enraged below 50 %), a 1.1 s telegraph, a
  2.2-radius blow for 20 % of the hero's maximum health. Its weapon is held from the mark until 1.5 s after the blow.
  The reveal holds the fight for 2.6 s. Floor 2's health share is 8.5 / 1.4 (`_foundryGateHealthShare` 6.071429):
  4,080 health at difficulty 1.6. The Sword deals x1.5 to the gate boss (`WardenRule.SwordShare`); the Daggers deal x2
  to an elite room's leader (`EliteRule.DaggersShare`).
- **Floor-2 Warden fight**, careful pilot, seeds 1-200 (docs/41, "the Daggers are the elites' weapon"): P50 58 s
  (Vanguard) and 52 s (Ilya) frame-perfect, 54 s and 49 s with human hands.
- **Clears, both floors** (the same table; careful / first-card / standing / clumsy / auto-farmer of 200): Vanguard
  200 / 196 / 180 / 99 / 2 frame-perfect and 192 / 177 / 162 / 65 / 3 with human hands; Ilya 200 / 200 / 176 / 164 / 6
  and 195 / 192 / 156 / 109 / 3.
- **The Descent** (docs/47 `gate210-chest`, human hands): P10 / P50 / P90 9.8 / 10.1 / 10.5 min. Completion
  96 / 87 / 91 / 44 % (careful / standing / first-card / clumsy).
- **What the measurements could not get.** docs/41 found that neither form of the Warden echo separated skill
  (2026-11-11, "Neither echo separates skill"), and that the Staff leads the elite rooms' whole waves. Floor 2's new
  creatures are a chance at both: mechanics that a careful player reads and a standing one does not, and one enemy the
  Staff does not solve alone.

### Foundry roster, for reference (authored values)

| Enemy | Health | Weapon: damage / interval / reach (initial delay) | Speed | Gold / XP |
|---|---|---|---|---|
| Cinder Mite | 15 | 0.6 / 1.5 s / 1.5 | 2.2 | 0 / 1 |
| Runner | 30 | 1.2 / 0.4 s / 1.5 | 3.0 | 3 / 10 |
| Grunt | 50 | 3.6 / 1.0 s / 1.5 | 1.6 | 5 / 10 |
| Tank | 120 | 7.2 / 2.5 s / 1.7 (1.5) | 0.9 | 10 / 10 |
| Grunt Captain (elite) | 140 | 5.4 / 1.2 s / 1.6 (0.6) | 1.4 | 20 / 10 |
| Forge Warden (gate) | 300 | 6.0 / 1.8 s / 1.7 (1.0) | 1.0 | 50 / 10 |

## 2. Shared rules for all four

1. **One signature behaviour each.** It is driven by one pure rule (proposed names: `WispRule`, `ClerkRule`,
   `MirrorGuardRule`, `RegentRule`). Arithmetic that both the scene and the simulation run lives in one never-inlined
   body, as `WardenRule.GateHealthShareOn` and `EliteRule.DamageOn` do. The stamina divergence's lesson applies: never
   copy distance or angle arithmetic across assemblies.
2. **No projectile objects** (docs/42 section 4). Every attack is a melee strike, a mark on the floor that lands after a
   telegraph (as the Warden's slam), or a blink. Lanes and copies are marks, not flying objects.
3. **Telegraphs stay red-orange** (`#FF5A3C`, docs/49 rule 4). Any spot or lane that will hurt after a countdown is
   red-orange and fills with the same progress as the Warden's mark (`WardenSlamClock.TelegraphProgress`). Violet and
   silver belong to the creatures and to harmless effects (a blink's shimmer, a mirror's glint). The rule is simple:
   **red-orange means "this floor will hurt"; violet never does.**
4. **Ordered with the Warden.** Each behaviour steps where `WardenSlamService` steps (script order -40: after
   `EncounterController`, before every `AttackController`), in one service, so the scene's order and the simulation's
   agree. `FoundrySimulationSceneValueTests.TheScenesScriptOrderIsTheSimulations` extends to it.
5. **Paused by the reveal and by panels.** Their clocks hold whenever the encounter holds (the boss reveal, a level-up
   choice), as the Warden's and the burns' do since 2026-11-11.
6. **Born on floor 2.** The four are drawn in the ice palette from the start. Their `EnemyArtSet` has no `_frost`
   (`Frosted` returns null, so the set is its own floor-2 look). The foundry's Grunt, Mite and Tank stay on floor 2 in
   their frosted copies.
7. **Floor scaling as everyone.** Climb scaling, Cursed Gold, the horde's shares and the horde clock apply to the
   numbers below exactly as they do to the foundry's enemies. Nothing in this document adds a scaling path.

## 3. Silver Wisp

**Role.** Replaces the Runner on floor 2: horde role 2 (`_foundryHorde` becomes Mite, Grunt, Wisp) and the Runner in
Mercury Stair's second wave. It is the floor's fast flanker (docs/40 8.5 "Runner: fast flank"), but it flanks by
moving across the floor, not by outrunning the hero. docs/49 already used the Wisp's violet for the frosted palette.

**Signature: the mirrored blink.** Once in its life, a Wisp that has stood at least 1.5 s and is 3-7 units from the hero
fades out. A violet shimmer shows where it will land: 3 units from the hero, on the opposite side from where the Wisp
stood (the Wisp's spot mirrored through the hero, pulled in to 3 units). 0.6 s later it forms there. It cannot strike
for 0.5 s after it lands. If the landing spot is off the walkable floor (the same `ArenaFloor` / `FoundryMap` test both
sides use), the blink is skipped and does not come back.

**Why it matters.** A hero that kites in a straight line, away from the pack, finds Wisps landing in front. That is the
"surges that cut off the kite" idea the difficulty research measured as `AmbushSurges` (FOUNDRY_LEVERS=7), now as an
enemy you can see coming. A hero that turns and fights, or circles, gets Wisps at its side in reach of its weapon.

**Counter-play.** Read the shimmer, turn before it forms, and fight the arrivals. Do not run straight.

**Weapon.** The Staff. Wisps that blink near the same time arrive on one 3-unit ring ahead of the hero, inside one
3.5-radius splash. The horde job stays the Staff's. Each Wisp is one strike for any weapon (10 health as a horde body).

**Numbers (starting values).**

| | Runner (today) | Silver Wisp | On floor 2 (x1.4 health, x1.546 damage), horde |
|---|---|---|---|
| Health | 30 | 24 (0.8x) | 33.6 in a hall, 10.1 in the horde |
| Weapon | 1.2 / 0.4 s / 1.5 | 1.0 / 0.5 s / 1.4 | about 1.5 a strike in a hall, 0.8 in the horde |
| Speed | 3.0 | 2.4 (the blink is its flank) | |
| Gold / XP | 3 / 10 | 3 / 10 | the horde's 0 gold and 0.2 XP |
| Blink | - | once; age at least 1.5 s; 3-7 units out; lands 3 units ahead; 0.6 s shimmer; 0.5 s before its first strike | |

**Readability on the slate floor.** A small, bright silver body with a violet core and a dark outline (docs/49 rule 3).
It must not be tinted the floor's slate. The shimmer is violet, not red-orange, because the landing spot does not hurt.
It is a ring of motes about 0.6 units wide, at the ground, under the hero's height. A Wisp that has already blinked
loses its trailing motes, so the player can see which Wisps are spent.

**Art.** The horde sprite contract that the Runner uses: front and rear idle, step A, step B, strike, pass A, pass B
(12 bodies), a strike windup (2), the two remains (`_death`) and its own death (5 front, 5 rear); flashes generated as
white silhouettes. About 26 drawings and 14 flashes. The blink itself needs no frames: the body fades through its own
flash silhouette tinted violet, scaled down. One 4-frame shimmer effect, pooled with the drops' effects. Blender-rendered
like `BuildBlenderFoundryEnemies.py` (a small faceted quicksilver mote with an ice shell), packed with a `Build*Assets.cs`
like `BuildMiteAssets.cs`, checked by a runtime proof like `MiteRuntimeCapture.cs`.

**Simulation.** Cheap.
- `Foe` gains `Blinked` and `BlinkAt`.
- `PackMotion` gains a placement of one slot at a spot (or reuses `Replace` with an `EntryPlacement`), used by both
  the scene and the simulation.
- Levers, off by default: `VaultsWisps` (the Wisp replaces the Runner on floor 2), `WispBlinkMin` / `WispBlinkMax`,
  `WispLandDistance`, `WispShimmerSeconds`, `WispArrivalHoldSeconds`.
- Metrics: blinks a floor; hits taken from Wisps a minute by cohort; arrivals within the hero's reach before the
  shimmer ended (should be 0); floor-2 horde kills a second by weapon; floor-2 clears by cohort.

## 4. Faceless Clerk

**Role.** Adds the role the foundry lacks: docs/40 8.5's back-line caster or disruptor (1.4-1.8x a Grunt's health,
"telegraphed ranged" or "area denial"). It appears in node waves only, never in the horde, with at most two alive.
Proposed waves: it leads Mercury Stair's first wave and replaces a Grunt in Vault Gate's wave. That is two Clerks a
floor, met in the first and third halls. docs/44's lore: the archivists who keep stolen names and faces.

**Signature: it takes the hero's face.** Every 5 s (the first 1.5 s after it walks in), the Clerk "writes" for a
moment, then a silver copy of the hero appears exactly where the hero stands. A red-orange ring of radius 1.6 sits under
the copy and fills for 1.1 s. While the copy stands, **every pack (the horde and the standing node wave, except Clerks,
elites and the gate boss) walks to the copy instead of the hero**. When the ring fills, the copy shatters: 10 % of the
hero's maximum health if the hero is inside the ring. Then the packs chase the hero again. Floor-wide, at most two copies
stand at once.

**Why it matters.** It is the one double-edged mechanic on the floor. A hero who stands takes the shatter with the pack
on top of it. A hero who steps off gains about a second of separation and a pack bunched on one spot. That is the
standing-versus-careful gap docs/41's echo research could not produce.

**Counter-play.** Step off the copy, then turn on the bunched pack, then walk to the Clerk and kill it. The Clerk stops
2.8 units from the hero, outside every weapon's reach (Daggers 1.8, Sword 2.1, Staff 2.4), so the hero must close in.
It does not strike in melee.

**Weapon.** Two options for the measurement to choose between:
- **A (plain).** The Clerk takes damage like any enemy. The Staff's 3.5 splash reaches it behind its pack, so it is the
  Staff's, and the pack bunched on the copy feeds the splash too.
- **B ("faceless").** Area finds no face: splash, cleave, pulse, splinter and kill bursts pass through the Clerk, and
  only a weapon's own strike lands. One damage rule like `EliteRule.DamageOn`, applied in `WeaponRuntime.TryAttack`'s
  proc landings. The Clerk then belongs to the single-target weapons: the Daggers (highest single-target rate) and the
  Sword. It is the one place on floor 2 where the Staff, which today leads the horde and the elite rooms' whole waves,
  has a weakness. B is the lead's preference if the clears hold (section 7).

**Numbers (starting values).**

| | Grunt (today) | Faceless Clerk | On floor 2 |
|---|---|---|---|
| Health | 50 | 70 (1.4x, docs/40's caster) | 98 |
| Weapon | 3.6 / 1.0 s / 1.5 | none in melee; it stands off at 2.8 | |
| Speed | 1.6 | 1.2 | |
| Gold / XP | 5 / 10 | 8 / 15 | x1.5 gold (Cursed Gold) |
| The copy | - | every 5 s (first at 1.5 s); 1.1 s telegraph; radius 1.6; 10 % of the hero's maximum health; packs drawn while it stands; at most two copies on the floor | |

Target: the Clerk's time under fire P50 1-2.5 s (docs/40 8.6, "Ember Caster"). Its 98 health against a floor-2 hero's
roughly 90-105 health a second of focused damage (docs/41 2026-11-03 table, at the gate's levels) is about a second
once reached.

**Readability.** docs/49 rule 3: a dark robe with a silver mask, readable by value against frost. It has no face: the
mask is a blank mirror oval that catches the light. The copy is the hero's silhouette in pale silver at about 60 %
opacity, standing still, so it cannot be mistaken for the hero (which moves and keeps its colours). The ring under it
is the floor's only warm shape, red-orange, filling as the Warden's mark fills. The Clerk has no spoken line; docs/44's
"Hold still. I need your face." stays the Regent's.

**Art.**
- The Clerk: the horde contract (12 bodies). Its strike windup pair (2) becomes the writing pose: a ledger raised,
  a quill at the mask. Its own death (5 + 5): the robe empties and the mask falls. About 26 drawings.
- The copy: one standing silhouette per hero (Vanguard, Ilya; Nix later), front only, pale silver. It is drawn once,
  not puppeted, because the heroes are cut-out puppets and copying one live is a renderer per part. Plus a 4-frame
  shatter effect.
- The ring reuses the Warden mark's material at radius 1.6.

**Simulation.** Cheap.
- The copy is a `HazardMark` (the echo, slag and stomp levers already have them) with the slag's shape: a mark where
  the hero stands, landing a telegraph later. "Range does not help, standing still does not either" (FOUNDRY_LEVERS=7's
  slag) is the same rule with a body to kill.
- The pack draw costs nothing new: `PackMotion.Step(deltaTime, heroX, heroY)` already takes its goal as arguments, so
  the packs are stepped toward the copy instead of the hero while it stands.
- Levers: `VaultsClerks`, `ClerkEvery`, `ClerkTelegraph`, `ClerkRadius`, `ClerkShare`, `ClerkStandOff`,
  `ClerkDrawsPacks` (on / off), `ClerkFaceless` (option B).
- The careful pilot already escapes marks (`EscapesMarks`). It needs one more habit, "walk to the back-liner once its
  pack is down", in both `DevelopmentAutopilot` and the simulation's pilot.
- Metrics: copies a floor; shatter hits by cohort; the Clerk's time under fire; the seconds the packs spent drawn; the
  Staff's floor-2 clears under A and B.

## 5. Mirror Sentry

**Role.** Replaces the Grunt Captain as floor 2's elite: Sentry Hall becomes Sentry, Grunt, Mite, Mite. Because
`EliteRule` treats the first enemy of an elite room's wave as the elite, the Sentry gets the Daggers' x2 with no change.
docs/44's room note for Sentry Hall: "guards with different faces but the same voice". The codex entry C06 "Liquid
Names" already unlocks at the first Mirror Sentry (docs/44).

**Signature: the mirror guard.** The Sentry carries a mirror on its front and turns toward the hero at a limited
rate, 90 degrees a second. A landing made while the hero stands inside its front arc (120 degrees, centred on its
facing) deals 35 % to it, and reflects 20 % of the landing's full amount back to the hero. The reflection is capped at
4 % of the hero's maximum health a second for each Sentry. From the flanks and the rear it takes full damage and
reflects nothing. The test uses the hero's position, not the strike's: a Staff splash that reaches the Sentry is
blunted if the hero stands in front of it.

**Why it matters, and why the Daggers.** The walk is 3.5 units a second on the foundry. Circling at radius r, the hero
turns round the Sentry at 3.5 / r radians a second:
- at the Daggers' step-in (1.65, `FootworkRule.StepInMargin` inside 1.8): about 121 degrees a second, 31 more than the
  Sentry turns;
- at the Sword's 2.0-2.1: about 100 degrees a second, 10 more;
- at the Staff's 2.4: about 84 degrees a second, which is too slow to get round it.

So close weapons win the flank, and the Daggers win it fastest (about 2 s of circling to leave the front arc, against
6 s for the Sword). Add the x2, and the Sentry is the Daggers' elite, as the Captain is today. A standing player trades
from the front and pays the reflection. The cap ties the reflection to the hero's health, not its damage, so it cannot
grow into a one-shot as the hero's build grows.

**Counter-play.** Circle close and strike the flank. Or kill the escort first and let the Sentry walk into a kite that
turns.

**Numbers (starting values).**

| | Grunt Captain (today) | Mirror Sentry | On floor 2 |
|---|---|---|---|
| Health | 140 | 160 (effective 457 from the front) | 224 |
| Weapon | 5.4 / 1.2 s / 1.6 (0.6) | Mirror Bash 5.4 / 1.4 s / 1.6 (0.6) | about 8.3 a strike |
| Speed | 1.4 | 1.1; turns 90 degrees a second | |
| Gold / XP | 20 / 10 | 20 / 10 | |
| The guard | - | front arc 120 degrees; 35 % taken from the front; 20 % reflected; cap 4 % of the hero's maximum health a second | |

Targets: the elite fight (the Sentry's time under fire) P50 6-12 s for the careful pilot. docs/40 8.6 asks 7-14 s of
exposure; the foundry's elite fights fell to 5-6 s with the Daggers (docs/41, 2026-11-11). The share of landings on the
Sentry made from the front: careful pilot at most 40 %, standing cohort at least 80 %. The Daggers lead the floor-2
elite job by more than 10 %.

**Readability.** Silver-and-ice armour with a dark outline. The mirror is the brightest shape on the body, a pale
silver-violet slab. The art faces four ways (front and rear, mirrored for left and right), which cannot show a
90-degree-a-second turn. So the Sentry wears one more quad: a **mirror glint**, a short violet arc on the ground at its
feet, rotated to the rule's exact facing and as wide as the front arc. It is violet because standing in front does not
hurt on a timer. A reflected hit draws a 3-frame violet glint from the mirror to the hero, and the hero flashes as for
any hit.

**Art.** The elite contract that the Captain uses: 12 bodies, strike windup (2), remains (2), own death (5 + 5); about
26 drawings. Plus the glint arc (1 sprite) and the reflect glint (3 frames). A taller figure than the Captain, rendered
in Blender like the Runner and Tank, with the mirror as a separate emissive plane, so its value can be tuned without
re-rendering.

**Simulation.** Moderate.
- `Foe` gains `Facing` (one float), turned each frame toward the hero at the rule's rate.
- The guard is one never-inlined body, `MirrorGuardRule.DamageOn(amount, heroX, heroY, foeX, foeY, facing)`, called
  where a landing is applied: `FoeDamaged` in the simulation, and a component ahead of `Health` on the Sentry's prefab
  in the scene. `DamageContext` gains the attacker's position, or the rule reads the hero's position directly.
- The reflection is a hit on the hero with the Sentry as its source; the cap is a per-Sentry accumulator.
- The careful pilot needs a **flank** habit: when its best weapon on an elite is short and it is safe, orbit at that
  reach instead of holding still. It extends `FootworkRule.StepIn` and goes into `DevelopmentAutopilot` and the
  simulation together. Without it, the frame-perfect careful pilot trades from the front, and the measurement should
  report both.
- Levers: `VaultsSentry`, `SentryTurnDegrees`, `SentryArcDegrees`, `SentryFrontShare`, `SentryReflectShare`,
  `SentryReflectCap`, and the habit.
- Metrics: front share by cohort; health reflected an elite fight; the elite fight's length; health a second on the
  Sentry by weapon (the floor-2 roles row's elite job); floor-2 clears.

## 6. The Glass Regent

**Who.** docs/44 section 1.8: the archivist who, wanting to protect identities, came to value the copies above their
owners. Spawn line "Which one of you is original?", telegraph line "Hold still. I need your face.", death line "I kept
the wrong one...". A tall figure of liquid mirror in an archivist's robe, crowned, with a violet heart.

**How the fight differs from the Forge Warden.**

| | Forge Warden (floor 1, today on floor 2 too) | Glass Regent (proposed) |
|---|---|---|
| The read | A circle where the hero stands, radius 2.2 | A **sight line**: a lane 1.2 wide from the Regent through where the hero stands, 10 units long |
| The dodge | Run out of the circle (about 2.2 units) | **Sidestep** 0.6-1 unit across the lane; running along it does not help |
| Second phase (50 %) | Marks every 4 s; the horde thickens round it | **Two glass copies** step out; every body casts a lane on the same beat; marks every 4.5 s |
| Target priority | None: one body | Three bodies; only the Regent takes the Sword's x1.5; the hero should kill the copies or walk to the Regent |
| Damage window | Weapon held from the mark until 1.5 s after the blow | The same clock (`WardenSlamClock` reused), the same hold |
| Reveal, heal chest, escort | 2.6 s reveal; the room's heal chest; two Mites | The same; escort unchanged at first |

**Phase 1 (100 % to 50 %).** Every 6 s the Regent marks a lane (red-orange, filling over 1.1 s; its first lane of a
fight carries the line "Hold still. I need your face."): from the Regent,
through the hero's spot, to 10 units. When it fills, glass shards run down the lane: 18 % of the hero's maximum health
inside it. The Regent holds its weapon from the mark until 1.5 s after the lane lands. Between lanes it walks and swings
its Glass Rod.

**Phase 2 (at 50 %).** "Which one of you is original?" Two **glass copies** step out of the Regent and take places
where its position, turned 120 degrees either way round the hero, would be. The three stand round the hero at about the
same distance. The copies walk at its speed, do not swing, and each casts a lane on the Regent's beat, aimed through the
hero's spot. The beat is now every 4.5 s. Three lanes from three sides cross at the hero's spot, 60 degrees apart. A
step along any one lane stays in it. A step of about 1.2 units between two lanes (across the third) clears all three,
and the walk covers 3.85 units in a telegraph. A beat's lanes hit at most
once (the hero takes one lane's share however many it stands in). Each copy has 6 % of the Regent's maximum health and
takes every weapon's damage at its authored value, with no Sword x1.5 (they are not the gate boss). A shattered copy
stays shattered. Both shatter when the Regent falls.

**Why this shape.** The lane keeps the Warden's proven clock (telegraph, hold, recovery; docs/41 2026-11-03 and
2026-11-04), so the fight's pace and its damage window are already measured. What changes is the dodge (across, not
out) and the second phase's question (which body to strike). The Sword stays the gate boss's weapon, because only the
real Regent takes its x1.5: a Sword player gains most by walking to the real one.

**Keeping the 45-65 s band.**
- The health share is the lever, as for the Warden. Starting value **5.2** (floor 2's Warden: 6.071429). At difficulty
  1.6 that is 300 x 5.2 x 1.4 x 1.6 = 3,494 health, plus two copies of 210 each: 3,914 in all, 96 % of the Warden's
  4,080. The remaining seconds come from the sidesteps and the walk between bodies.
- The sweep: share 4.6 to 5.8 in steps of 0.2, both heroes, careful pilot, frame-perfect and human hands, seeds 1-200,
  holdout 201-400. Choose the share that puts both P50s in 45-65 s with clears no more than 2 points below the
  Warden's on the same seeds, as the Warden's 2026-11-03 choice was made.
- Secondary levers if health alone cannot: the copies' share (4-8 %), the phase-2 beat (4-5 s), the recovery
  (1.5-2 s). The lane's 18 % and the copies' count stay fixed during the sweep, so the band is bought with one number.
- The overrun row (clock at clear past 300 s) is read as on 2026-11-03: no more than 5 % of clears newly past it.

**Does it replace floor 2's Warden?** Yes, recommended, but staged, and only after it is measured:
1. The Regent as **data and art on the Warden's rule**: the lane in place of the circle, no copies. This proves the
   sidestep habit, the lane telegraph and the band.
2. **Add the copies.** Measure again.
3. **Switch floor 2's gate room** to the Regent once both are in band. The Forge Warden stays floor 1's boss, and the
   switch is one enemy reference in `Floor_QuicksilverVaults.asset`, so it can be reverted in one line.

The Forge Warden is floor 1's identity, and docs/44 names the Regent as floor 2's. Fighting the same boss twice is
what docs/48 section 1.4 warned reads as "the same boss again". The room name "Warden's Vault" and docs/44's quests that
name it (Sanguine's "F2 Warden's Vault" Return Mark) are the owner's call (question 2).

**Numbers (starting values).**

| | Forge Warden | Glass Regent |
|---|---|---|
| Health (authored), floor-2 share | 300, 6.071429 | 300, **5.2** (swept 4.6-5.8) |
| Weapon | Warden Hammer 6.0 / 1.8 s / 1.7 (1.0) | Glass Rod 5.0 / 1.8 s / 1.7 (1.0) |
| Speed, march | 1.0; the gate march as today | the same |
| Mark | circle r 2.2, 20 %, every 6 s / 4 s | lane 1.2 x 10, 18 %, every 6 s / 4.5 s |
| Telegraph, recovery | 1.1 s, 1.5 s | 1.1 s, 1.5 s |
| Phase 2 | at 50 %: faster marks, the horde thickens | at 50 %: two copies, 6 % of its maximum health each, lanes on its beat |
| Gold / XP | 50 / 10 | 50 / 10 |

**Readability.**
- Lanes are red-orange stripes with the mark's fill. They are the only warm shapes in the hall, and they must read on
  docs/49's mirror-ice panels: the panels stay dark-valued and the lane is checked on `IcePavingV1` first.
- The real Regent: the violet heart and the crown. The copies: paler silver at about 70 % opacity, no heart, no crown.
  The difference has to read at 80 pixels on the phone, so it is a shape difference (crown), not only a colour.
- The boss banner names the Regent, not an escort (as it does for the Warden since 2026-11-12).
- Damage numbers keep white with a dark stroke (docs/49 rule 5).

**Art.** The boss contract that the Warden uses:
- front and rear idle, step A, step B, strike, pass A, pass B (12); windup (2); windup at a walk (8);
- phase-2 variants of all of those (22), the role the Warden's "hot" frames play, here a brighter heart and cracks in
  the glass;
- a fall (about 4 front and 3 rear, like the Warden's fall-struck / fall-knee / fall-topple).

About 48 drawings and their flashes. The copies are a script recolour of the phase-1 frames (no heart, no crown, paler),
like `GenerateIceEnemyArt.py` (about 14 drawings, no fall: a copy shatters with an effect). Effects: the lane telegraph
(a stretched quad on the slam mark's material), the shard run along the lane (4 frames), and the copy shatter (shared
with the Clerk's). Built in Blender as `BuildBlenderWarden.py` is: one model renders every frame, both facings and both
phases. docs/49 phase 4 (the hall of ice mirrors) is the arena.

**Simulation.** Moderate to high, the largest of the four.
- The lane is a `HazardMark` with a direction and half-width; the hit test is a point-to-segment distance, one
  never-inlined body.
- The clock is `WardenSlamClock` as it is.
- The copies are two more `Foe`s that are not the gate boss. They are placed by a rotation round the hero, have no
  weapon, and their lanes ride the Regent's beat.
- The pilot needs a **sidestep** habit for lanes. Today `EscapesMarks` takes the nearest of eight points at one distance
  round a circle; a lane needs the nearer of the two points across it, avoiding the other lanes. It goes into
  `DevelopmentAutopilot` and the simulation together.
- Levers: `VaultsRegent`, `RegentHealthShare`, `RegentLaneWidth`, `RegentLaneLength`, `RegentLaneShare`,
  `RegentEvery`, `RegentEveryPhaseTwo`, `RegentCopies` (0 = stage 1), `RegentCopyShare`.
- Metrics: the fight's P50 / P90; felled within 5 s; lane hits a fight by cohort; seconds in phase 2; copies shattered
  before the Regent; health a second on the Regent by weapon (the Sword's lead); deaths at the gate boss and their
  killing blows.

## 7. The measurement plan and acceptance

**How.** The foundry simulation (`DescentSimulation.RunFoundry`), `FOUNDRY_FLOORS=3` (both floors and the finale),
every cohort, both heroes, seeds 1-200 and a holdout of 201-400, frame-perfect and `FOUNDRY_REALISM=typical`. The Vanguard
is also started with each weapon for the roles reading. A new research set, `FoundryBatch.VaultsLevers` on the next free
`FOUNDRY_LEVERS` number, runs each creature alone, then all four together. Every lever is off by default, and the
default batch must stay byte-identical after each merge, as for the Daggers' and the Riftblade's levers.

**A floor-2 roles row.** The gate's rows read floor 1 today. Add a floor-2 reading of the same three jobs: the horde
(floor 2's horde, the Wisp included), the elites (the Sentry) and the gate boss (the Regent or the Warden). It is
reported, not gated, until the owner adopts it.

**Acceptance (proposed, all four together, against today's floor 2 on the same seeds).**

| Row | Target |
|---|---|
| Floor-2 gate boss fight P50, careful, both heroes, both hands | 45-65 s; P90 at most about 75 s; felled within 5 s: none |
| Floor-2 roles | The Staff leads the horde, the Daggers the Sentry, the Sword the Regent, each by more than 10 % |
| Clears, careful, both floors, human hands | no more than 3 of 200 below today |
| Clears, first-card | no more than 5 points below today |
| Clears, standing and clumsy, floor 2 | may fall up to 10 points; this is the skill separation the floor is meant to add |
| Skill separation | the careful-minus-standing gap in floor-2 clears grows by at least 5 points (reported) |
| Descent completion, clumsy | at least 40 % (docs/40's Forged band's lower edge; today 44 %) |
| Floor-2 time P50 | 240-300 s (the 3:30 gate binds); the Descent P50 within 9.8-10.6 min |
| Wisp | shimmer-to-hit with no reaction time impossible (no hit within 0.5 s of landing); hits taken from Wisps a minute reported by cohort |
| Clerk | time under fire P50 1-2.5 s; shatter hits a floor: careful at most 1, standing at least 3 |
| Sentry | elite fight P50 6-12 s; front share careful at most 40 %, standing at least 80 % |
| Regent | lane hits a fight, careful, at most 0.3; the Sword's lead on the Regent over 10 % |
| Parity | each rule's scene-and-simulation parity test; the Editor's `HordePacingMeasurement` down the whole Descent, 6 seeds a hero, within about 6 s of the simulation on floor 2 (as on 2026-11-11) |
| Device | floor 2 on the S23 at 40 alive with two copies and three lanes up: p95 at most 20 ms, no frame over 35 ms in a steady fight |

**Fallbacks if a row fails.** The Sentry: drop the reflection and keep the guard (`SentryReflectShare` 0). The Wisp:
land at 3.5 units, or raise the arrival hold. The Clerk: option A instead of B, or turn off the pack draw. The Regent:
stage 1 (no copies) with the Warden's enrage. Each fallback is a lever value, not new work.

## 8. Phased build plan

Sizes are rough, for one agent; measurement runs are wall time on this machine.

| Step | What | Size |
|---|---|---|
| 0 | The owner answers section 9 | - |
| 1 | **Simulation research**: the four as levers in the simulation only (Foe fields, the lane mark, the pack draw, the blink placement, the flank and sidestep habits); `VaultsLevers`; one creature at a time, then together; a docs/50 status with the tables | 3-4 days, about 6-8 h of runs |
| 2 | **Mirror Sentry in the game**: `MirrorGuardRule`, the guard component, the flank habit in `DevelopmentAutopilot`, `Enemy_MirrorSentry` and its weapon, Sentry Hall's leader; parity and flow tests (as `EliteStepInFlowTests` on a proof floor) | 2-3 days |
| 3 | **Silver Wisp in the game**: `WispRule`, the slot placement, `Enemy_SilverWisp`, floor 2's horde roles and the hall's Runner; tests | 1.5-2 days |
| 4 | **Faceless Clerk in the game**: `ClerkRule`, the copy mark, the pack draw in `EncounterController`, the back-liner habit, option A or B; tests | 2-3 days |
| 5 | **Art for the three** (Blender, the six-pose contract, the Build and Capture tools, proofs on the ice floor; the glint, shimmer, copy and shatter effects) | 5-6 days |
| 6 | **The Glass Regent, stage 1** (the lane on the Warden's clock, the sidestep habit, `Enemy_GlassRegent`, its art); the share sweep; the Editor beside the simulation | 4-5 days |
| 7 | **The Regent, stage 2** (the copies, their recolour, the phase-2 beat); the sweep again; switch floor 2's gate room | 2-3 days |
| 8 | **The phone**: a new APK; floor 2 on the S23 (frame time with copies and lanes, the owner's play) - the owner's go needed, as for every install | 0.5 day with the owner |

About 20-26 working days in all, a little under half of it art. Steps 2-4 can each ship on their own once step 1 has
its numbers; step 1 can start before the V0.1 device row because it changes nothing the game ships.

What stays unchanged throughout: floor 1, its Warden and every floor-1 row of the batch; the Daggers', the Sword's and
the Warden's rules; the horde director; the region's renderers and vertices (each creature is one body sprite and
shadow, as the enemy it replaces; the glint, shimmer, lanes and copies are pooled quads within the existing effects).

## 9. Questions for the owner

1. **When?** Simulation research now, and the build after the V0.1 device row (as docs/46 and docs/48 wait)? And in
   which order beside the Riftblade (docs/46) and a third floor (docs/48)?
2. **The Regent replaces floor 2's Warden.** Yes, after it measures in band? Does the gate room keep the name "Warden's
   Vault" (docs/44's rooms and the Sanguine quest name it), or become, for example, "The Mirror Gallery"?
3. **The Clerk: option A or B?** Should one floor-2 enemy be one the Staff cannot solve with its splash ("faceless")?
4. **The Sentry's reflection.** Keep a capped reflection (4 % of maximum health a second), or the guard alone?
5. **The Wisp in place of the Runner**, all of it on floor 2, or Runners and Wisps mixed?
6. **Skill separation.** Is it acceptable that the standing and clumsy cohorts lose up to 10 points of floor-2 clears if
   the careful cohort holds?
7. **Art route.** Blender-rendered like the foundry enemies (no generated imagery), the copy of the hero as one still
   silhouette per hero. Agreed?
8. **Colour rule.** Red-orange only for spots and lanes that hurt after a countdown; violet for the Wisp's shimmer and
   the Sentry's glint, which never hurt on a timer. Agreed?
9. **Floor 3 (docs/48)** plays foundry enemies at first. Once floor 2 has its own, should floor 3's stand-in reuse the
   Vaults' creatures or the foundry's?
