# 50 - The Quicksilver Vaults' own enemies (design proposal, 2026-11-12)

**Status 2026-11-12:** the Mirror Sentry is built and **cleared to ship as `sentry-r0-f70`** (step 2 of section 8;
"Lead decisions 2026-11-12" and "Shipped: `sentry-r0-f70`" at the end). It takes 70 % of a landing from its front and
reflects nothing; the reflection is a violet glint only. The Wisp, the Clerk and the Regent stay simulation-only research.

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
| Sentry | elite fight P50 **reported** (the lead's decisions 2026-11-12: no variant passes 4.3 s; the flank that makes it a read also shortens it); front share careful at most 40 %, standing at least 80 % |
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

## Measured (2026-11-12, simulation only)

Section 8's step 1, as research levers off by default. Nothing the game ships changed: no runtime script, scene, asset
or setting. The default batch (`FOUNDRY_FLOORS=2`, seeds 1-200, cohorts) is byte-identical before and after: every CSV,
`foundry-gate.md` and `foundry-cohorts.md` (only `foundry-batch.md`'s clock and wall seconds differ).

**What was built.**
- `Assets/_Project/Tests/Support/DescentSimulation.Vaults.cs`: the four as `Enemy` definitions with section 3-6's
  numbers, `VaultsChain(levers)` (floor 1 as shipped above a floor 2 with the levers' creatures in place: the Sentry for
  Sentry Hall's Captain, the Wisp for the Runner in the horde's third role and Mercury Stair's second wave, a Clerk at
  the head of Mercury Stair's first wave and in place of a Grunt in Vault Gate's, the Regent for the Warden), the
  levers (`VaultsSentry`, `VaultsWisps`, `VaultsClerks`, `VaultsRegent` and each creature's numbers; the Regent's share
  is `LaterWardenHealthShare`), the shared arithmetic in never-inlined bodies (`VaultsRule.InLane`, `Turn`, `InFront`)
  and the research habits `VaultsHabit.Flanks` and `VaultsHabit.Sidesteps`, outside `AutopilotCohort.Habit.All` (the
  runtime enum is untouched): the flank extends the step-in (so only a cohort that `StepsIn`, careful and first-card,
  and only when its best single-target weapon is short, the Daggers); the sidestep extends `EscapesAwayFromTheWarden`
  (careful, standing, first-card; the clumsy pilot keeps the nearest-of-eight escape, the auto-farmer none).
- `DescentSimulation.Foundry.cs` gains hooks that do nothing unless a floor holds one of the four (`Foe` is partial).
- `Tools/CombatChecks/VaultsEnemiesResearch.cs` (explicit): every cohort, both heroes, the whole Descent, typical
  hands; for the options that read roles, the Vanguard also started with the Staff and with the Daggers in the careful
  hands. `VAULTS_OPTIONS` picks options; it writes `vaults-enemies.md` and `.csv`.

**Where the model departs from sections 3-6.**
- The Regent's phase-2 beat is the Warden's enraged 4 s (`WardenSlamClock` as it is), not 4.5 s. The horde still
  thickens in phase 2 as for the Warden.
- A shimmering Wisp keeps walking until it forms at the landing spot (the spot is fixed as the shimmer starts).
- The Clerk walks with its pack to the copy (one formation); it never strikes and the pilot does not kite from it.
- Every cohort walks to a wave's last Clerks (section 4's back-liner habit), or a hall holding a Clerk would never
  clear when no splash reaches it.
- The sidestep picks the nearest point (rings 0.5-3.2 units, 24 bearings) clear of every lane by 0.35 and on the floor.

**The run.** Seeds 1-200, both heroes, all five cohorts, `FOUNDRY_REALISM=typical`, the whole Descent. Each option is
one creature alone against `shipped` on the same seeds (floor 1 is the same run in every option). About 4 minutes an
option on 6 threads, 8 with the roles row.

### Floor-2 clears (deaths) by cohort, Vanguard / Ilya

Of the runs that cleared floor 1 (went down: careful 195 / 198, standing 187 / 183, first-card 182 / 198, clumsy 78 /
131, auto-farmer 10 / 29), with the floor's deaths in brackets.

| Option | careful | standing | first-card | clumsy | auto-farmer | Descent P50 min, careful V / I |
|---|---|---|---|---|---|---|
| `shipped` | 191 (4) / 191 (7) | 180 (7) / 170 (13) | 172 (10) / 192 (6) | 66 (12) / 108 (23) | 3 (7) / 8 (21) | 10.3 / 10.2 |
| `sentry` (flank) | 188 (7) / 186 (12) | 162 (25) / 158 (25) | 169 (13) / 185 (13) | 61 (17) / 91 (40) | 1 (9) / 5 (24) | 10.3 / 10.1 |
| `wisp` | 192 (3) / 193 (5) | 173 (14) / 174 (9) | 177 (5) / 193 (5) | 67 (11) / 108 (23) | 3 (7) / 6 (23) | 10.3 / 10.2 |
| `clerk` (option A) | 192 (3) / 191 (7) | 182 (5) / 177 (6) | 180 (2) / 195 (3) | 65 (13) / 113 (18) | 5 (5) / 5 (24) | 10.3 / 10.2 |
| `sentry-noflank` | 184 (11) / 185 (13) | 162 (25) / 158 (25) | 170 (12) / 183 (15) | 61 (17) / 91 (40) | 1 (9) / 5 (24) | 10.3 / 10.2 |
| `regent-lanes` (stage 1, 5.2) | 194 (1) / 195 (3) | 185 (2) / 176 (7) | 179 (3) / 194 (4) | 63 (15) / 104 (27) | 5 (5) / 13 (16) | 10.1 / 10.0 |
| `regent-5.2` (copies, sidestep) | 194 (1) / 194 (4) | 187 (0) / 176 (7) | 180 (2) / 195 (3) | 62 (16) / 103 (28) | 5 (5) / 13 (16) | 10.2 / 10.0 |
| `all` (the four, Regent 5.2) | 190 (5) / 194 (4) | 180 (7) / 174 (9) | 176 (6) / 188 (10) | 56 (22) / 98 (33) | 2 (8) / 8 (21) | 10.2 / 10.0 |

The Descent time barely moves: shipped P50 9.9-10.3 minutes across the cohorts that complete it, every option 9.7-10.3
(the Regent's shorter fight takes 0.1-0.2 minutes off). Completion follows the floor-2 clears above one for one: no
option changes the finale.

### Floor 2's roles row (careful hands, the Vanguard started with each weapon)

Damage a second: the horde (a second held), the elite room's leader (a second of the elite fight), the gate boss (a
second of its fight). Sword / Staff / Daggers.

| Option | Horde | Elite leader | Gate boss | Roles (`FoundryGate.LeadsJob`) | Daggers' lead on the leader | Sword's lead on the gate boss |
|---|---|---|---|---|---|---|
| `shipped` (Captain, Warden) | 19.2 / 54.8 / 1.1 | 35.1 / 42.8 / 51.0 | 36.0 / 28.9 / 4.8 | met | +19 % | +25 % |
| `sentry` | 18.8 / 53.8 / 1.2 | 14.5 / 17.0 / 30.8 | 34.8 / 29.2 / 4.7 | met | **+81 %** | +19 % |
| `wisp` | 18.1 / 52.4 / 1.7 | 31.6 / 40.6 / 55.0 | 36.5 / 29.1 / 4.5 | met | +35 % | +25 % |
| `clerk` | 18.8 / 54.3 / 1.2 | 32.8 / 42.1 / 46.2 | 35.2 / 29.1 / 4.8 | not met (Daggers +10 %) | +10 % | +21 % |
| `sentry-noflank` | 18.3 / 55.2 / 1.2 | 13.8 / 15.6 / 25.3 | 34.9 / 29.1 / 4.7 | met | +62 % | +20 % |
| `regent-5.2` (Regent) | 18.7 / 54.1 / 1.1 | 35.1 / 42.8 / 51.0 | 38.4 / 31.1 / 4.5 | met | +19 % | +23 % |
| `all` | 17.9 / 51.9 / 1.6 | 14.3 / 16.9 / 30.2 | 40.3 / 32.3 / 4.4 | met | +79 % | +25 % |

The `wisp` and `clerk` rows change neither the Captain nor the Warden, so their elite and gate columns (Daggers +10 % to
+35 % on the same Captain) show the row's noise: a difference under about 15 points between rows is not a reading.

### Each creature

**Mirror Sentry** (flank habit on; it acts only through the step-in, so only when the best single-target weapon is the
Daggers). The Daggers lead the Sentry by 81 % (the Captain: 19 %): the Sentry is the Daggers' elite. Floor-2 clears
fall 3 / 5 (careful), 18 / 12 (standing), 3 / 7 (first-card), 5 / 17 (clumsy). The careful-minus-standing gap grows
from 11 / 21 to 26 / 28 clears (+15 / +7: the skill separation docs/41's echo could not make). Most of the
new deaths are not at the Sentry but at the gate (standing: boss deaths 24 / 21 against 7 / 9): the reflection, about
25-31 health a fight, is carried into the Warden. The clumsy pilot dies to the Sentry itself (room 5: 6 / 6). The elite
fight is short: P50 3.6-4.4 s (careful, standing), time under fire 3.1-3.9 s, under section 7's 6-12 s. Front share:
careful 95-96 %, standing 100 %. Careful Ilya's -5 misses the "no more than 3 below" row by 2.

The flank habit is worth little at 90 degrees a second. Without it (`sentry-noflank`) careful clears are 184 / 185 (4 /
1 fewer), the Daggers' lead 62 % instead of 81 %, and every start lands 100 % from the front instead of 95 %. Its own
reading (`TheSentryFlankWithTheDaggers`, the careful Vanguard started with the Daggers, seeds 1-60, typical hands), front
share and time under fire with / without the habit:

| Sentry | Front share | Under fire P50 | Reflected a fight |
|---|---|---|---|
| 90 deg/s, 160 health (docs/50) | 93 % / 100 % | 3.6 / 4.0 s | 27 / 32 |
| 45 deg/s | **72 %** / 93 % | 2.9 / 3.9 s | 20 / 29 |
| 60 deg/s, 240 health | 78 % / 96 % | 4.1 / 5.1 s | 28 / 40 |
| 90 deg/s, 280 health | 91 % / 100 % | 5.2 / 6.0 s | 38 / 48 |

The step-in holds the Daggers' reach about 2.7 s of a 3.6 s fight, and at a net 24-31 degrees a second the hero needs
2-2.5 s to leave a 120-degree arc: the Sentry is down first. The turn rate is the lever that makes the flank matter;
health alone lengthens the fight without opening the flank. Section 7's "careful at most 40 % front" is out of reach at
any of these; 72 % is the best seen.

**Silver Wisp.** No reading moves beyond noise: clears within -7 to +4, about 40 blinks a floor, 4-5 Wisps a floor
form within their reach of the hero and none strikes inside the 0.5 s hold, and the hero takes 0.1-0.2 health a minute
from Wisps. A horde Wisp has 10 health and dies to the first splash; the Staff still leads the horde (52.4 against
54.8, the same lead). As specified the Wisp is a look, not a mechanic.

**Faceless Clerk** (option A, area damage lands). About 4 copies a floor, the packs drawn for 4.3-4.9 s a floor, the
Clerk's time under fire P50 1.0-1.5 s (in section 7's 1-2.5 s). Shatter hits a floor: 0.1 for every cohort that leaves
a mark (careful, standing, first-card, clumsy) and 1.2-1.4 for the auto-farmer. Clears rise slightly (standing 182 /
177 against 180 / 170): a copy pulls the packs off the hero. No skill separation: docs/41's standing cohort keeps
`EscapesMarks`, so it steps off a copy as the careful one does; section 7's "standing at least 3" cannot be met by this
cohort set.

**Glass Regent** (lanes, two copies at half health, the sidestep habit for careful, standing and first-card). The fight
band, P50 / P90 seconds of floor 2's gate fight (from the gate wave to the fall, the reveal included), careful hands:

| Gate boss, share | Careful P50 V / I (in 45-65 s) | Careful P90 V / I | Careful clears V / I | Standing P50 V / I | Standing clears V / I |
|---|---|---|---|---|---|
| Forge Warden, 6.071 (shipped) | 51 / 45 (69 % / 50 %) | 61 / 56 | 191 / 191 | 40 / 38 | 180 / 170 |
| Regent, lanes only, 5.2 | 42 / 38 (31 % / 16 %) | 51 / 47 | 194 / 195 | 32 / 30 | 185 / 176 |
| Regent, 4.6 | 39 / 36 (21 % / 7 %) | 48 / 44 | 194 / 195 | 30 / 29 | 187 / 176 |
| Regent, 4.8 | 40 / 37 (27 % / 11 %) | 48 / 45 | 193 / 195 | 31 / 30 | 187 / 176 |
| Regent, 5.0 | 42 / 38 (31 % / 14 %) | 49 / 46 | 193 / 195 | 32 / 30 | 187 / 175 |
| Regent, 5.2 | 43 / 39 (37 % / 18 %) | 51 / 47 | 194 / 194 | 32 / 31 | 187 / 176 |
| Regent, 5.4 | 44 / 40 (42 % / 22 %) | 53 / 48 | 193 / 195 | 32 / 32 | 185 / 176 |
| Regent, 5.6 | 44 / 40 (46 % / 25 %) | 54 / 49 | 194 / 195 | 33 / 32 | 184 / 175 |
| Regent, 5.8 | 45 / 41 (50 % / 25 %) | 54 / 50 | 192 / 194 | 34 / 33 | 184 / 176 |
| Regent, 6.2 (past the sweep) | 47 / 43 (60 % / 34 %) | 56 / 52 | 193 / 194 | 34 / 34 | 183 / 176 |
| Regent, 6.6 (past the sweep) | 49 / 45 (63 % / 46 %) | 59 / 54 | 192 / 195 | 36 / 34 | 183 / 177 |

None of 4.6-5.8 puts both careful P50s in the band: the Vanguard reaches 45 s at 5.8, Ilya stays at 41. Both first
stand in it at 6.6 (49 / 45; Ilya on the band's edge, 46 % of its fights inside), past the sweep and above the Warden's
own 6.071. Each 0.2 of share buys about 1 s. At about the Warden's share (6.2 against 6.071) the Regent's fight is 2-4 s
shorter: the sidestep is a step of about a unit, so the hero stays in reach, where the Warden's circle sends it 2.8
units out; and the copies add little (6 % each falls in about a second: 2.0 of 2 shattered a fight by every cohort but
the auto-farmer). Nothing falls within 5 s, and P90 stays under 75 s. Clears do not suffer: careful 192-194 / 194-195
at every share against 191 / 191, standing up by 3-7 / 5-7. Lane hits a fight: careful 0.3-0.5 (section 7 asks at most
0.3), standing 0.1-0.3, clumsy 0.9-1.3 (its nearest-of-eight escape often runs along the lane), auto-farmer 3.4-4.0.
The Sword leads the Regent by 23 % (the Warden: 25 %): the roles hold. The Descent's P50 falls 0.1-0.2 minutes.

The sidestep habit carries the Regent. Without it (`regent-5.2-nosidestep`: the shipped escape out of a circle, away
from the boss, which runs along the lane) careful clears fall to 163 / 170 (against 194 / 194 with it and 191 / 191
shipped), standing to 158 / 159, first-card to 146 / 166, nearly every new death at the gate; lane hits are 3.5-3.9 a
fight. The clumsy pilot, which never sidesteps, is the same in both (62 / 103). The Regent cannot ship before
`DevelopmentAutopilot` and the scene's development pilot learn the sidestep, and the lane telegraph has to read as
"step across" to a player.

### Recommendation

Section 7's rows, each creature alone against `shipped`:

| Row | Sentry | Wisp | Clerk | Regent (copies, 6.6) |
|---|---|---|---|---|
| Careful clears no more than 3 below | V -3 met; I -5 missed | met (+1 / +2) | met (+1 / 0) | met (+1 / +4) |
| First-card no more than 5 points below | met (-1.5 / -3.5) | met | met | met |
| Standing and clumsy may fall up to 10 points | clumsy Ilya -13 missed; the rest met | met | met (they rise) | met |
| Skill separation +5 points (careful-minus-standing clears) | **met: +15 / +7** | +8 / -2 (noise) | -1 / -7 | -2 / -3 |
| Clumsy completion at least 40 % | **missed: 38 %** (shipped 43.5 %) | 44 % met | 44.5 % met | 40.5 % met |
| The creature's own row | elite fight 3.6-4.4 s and front share 95 % missed; Daggers +81 % met | no hit inside the hold met | under fire 1.0-1.5 s met; standing shatter hits 0.1 missed | in band only past the sweep; lane hits 0.4-0.5 missed; Sword +23 % met |
| Descent P50 9.8-10.6 min | met | met | met | met (0.1-0.2 shorter) |

All four together (`all`, the Regent at 5.2) cancel out: careful 190 / 194, standing 180 / 174, the careful-minus-standing
gap 10 / 20 against 11 / 21, clumsy completion 38.5 %. The Sentry's cost and the Clerk's and Regent's ease meet in the
middle and the skill separation disappears.

**Build the Mirror Sentry first** (section 8's order holds), after one more simulation round on the levers already in
place. It is the only one of the four that moves a reading the floor needs: the Daggers' elite job (+81 % on the Sentry
against +19 % on the Captain) and the skill separation docs/41's echo could not make (+15 / +7). As specified it costs
too much in the wrong place (careful Ilya -5, clumsy completion 38 %, most of the new deaths at the gate after the
reflection) and its fight is too short for its own mechanic. The next round: `SentryTurnDegrees` 45-60 (the flank's
front share falls to 72-78 %), `SentryHealthScale` about 1.5 (the fight toward 6 s) and `SentryReflectCap` 0.02-0.03.

**The Glass Regent second**, as section 6 stages it. Its lanes, copies and the sidestep keep the clears and the Sword's
lead; the lead's condition (in 45-65 s before it replaces the Warden) is met only at a share of about 6.6, outside the
4.6-5.8 sweep, because the sidestep keeps the hero in reach. Next: confirm 6.4-7.0 on the holdout seeds 201-400, and
raise the copies to 8-12 % so that phase 2 asks the target question for more than a second.

**Do not build the Wisp or the Clerk as specified**: neither moves any reading. A horde Wisp has 10 health and dies to
the first splash, and its 0.5 s hold means its blink never lands a hit. The Clerk's copy is stepped off by every cohort
but the auto-farmer (docs/41's standing cohort keeps `EscapesMarks`), and its pack draw makes the halls easier. Both need
a redesign before art: for the Wisp, a share of a Runner's health in the horde or a shorter hold; for the Clerk, a
wider or faster copy (radius 2.4, telegraph 0.8 s) or no pack draw, measured against a cohort that does not leave marks.

### Round 2 (2026-11-12, the lead's follow-up)

Same harness and hands (`VaultsEnemiesResearch`, typical, all five cohorts, both heroes, the whole Descent). The default
batch stays byte-identical at the round-2 code. New levers, all off by default: `SentryHealthScale` (the Sentry's rules
read its name), `WispKiteBlink` / `WispKiteSeconds` / `WispKiteDegrees`, `ClerkReachBlast` / `ClerkTouch` /
`ClerkBlastRadius` / `ClerkFadeSeconds`.

**1. The Mirror Sentry grid** (seeds 1-200, the flank habit on): turn 45 / 52 / 60 degrees a second x health 125 / 150 %
x reflection cap 2 / 3 %, and one variant past the grid with half of a front landing taken (docs/50: 35 %). Shipped:
careful 191 / 191, standing 180 / 170 (gap 11 / 21), clumsy completion 43.5 %, the Daggers' lead on the Captain 19 %.

| Variant | Careful V / I (vs shipped) | Standing V / I | Gap V / I | Clumsy V / I | Clumsy completion | Elite fight P50, careful V / I | Front share, careful | Daggers' lead |
|---|---|---|---|---|---|---|---|---|
| `sentry-t45-h125-c2` | 190 / 190 (-1 / -1) | 165 / 162 | 25 / 28 | 54 / 101 | 38.8 % | 3.7 / 3.6 s | 72 % / 73 % | +88 % |
| `sentry-t45-h125-c3` | 191 / 189 (+0 / -2) | 161 / 161 | 30 / 28 | 54 / 96 | 37.5 % | 3.7 / 3.6 s | 72 % / 73 % | +88 % |
| `sentry-t45-h150-c2` | 190 / 189 (-1 / -2) | 164 / 160 | 26 / 29 | 56 / 95 | 37.8 % | 4.1 / 4.0 s | 70 % / 71 % | +87 % |
| `sentry-t45-h150-c3` | 187 / 189 (-4 / -2) | 157 / 154 | 30 / 35 | 54 / 91 | 36.2 % | 4.1 / 4.0 s | 70 % / 71 % | +87 % |
| `sentry-t52-h125-c2` | 190 / 191 (-1 / +0) | 165 / 162 | 25 / 29 | 54 / 101 | 38.8 % | 3.8 / 3.6 s | 75 % / 76 % | +85 % |
| `sentry-t52-h125-c3` | 188 / 189 (-3 / -2) | 161 / 161 | 27 / 28 | 54 / 96 | 37.5 % | 3.8 / 3.6 s | 75 % / 76 % | +85 % |
| `sentry-t52-h150-c2` | 188 / 189 (-3 / -2) | 164 / 160 | 24 / 29 | 56 / 96 | 38.0 % | 4.1 / 4.0 s | 73 % / 74 % | +92 % |
| `sentry-t52-h150-c3` | 185 / 188 (-6 / -3) | 157 / 154 | 28 / 34 | 54 / 92 | 36.5 % | 4.1 / 4.0 s | 73 % / 74 % | +92 % |
| `sentry-t60-h125-c2` | 190 / 190 (-1 / -1) | 165 / 162 | 25 / 28 | 54 / 101 | 38.8 % | 3.9 / 3.7 s | 78 % / 80 % | +87 % |
| `sentry-t60-h125-c3` | 188 / 190 (-3 / -1) | 161 / 161 | 27 / 29 | 54 / 96 | 37.5 % | 3.9 / 3.7 s | 79 % / 80 % | +87 % |
| `sentry-t60-h150-c2` | 190 / 190 (-1 / -1) | 164 / 160 | 26 / 30 | 56 / 96 | 38.0 % | 4.3 / 4.1 s | 77 % / 78 % | +96 % |
| `sentry-t60-h150-c3` | 186 / 187 (-5 / -4) | 157 / 154 | 29 / 33 | 54 / 92 | 36.5 % | 4.3 / 4.1 s | 77 % / 79 % | +97 % |
| `sentry-t45-h125-c2-f50` | 192 / 189 (+1 / -2) | 164 / 165 | 28 / 24 | 62 / 101 | 40.8 % | 3.3 / 3.2 s | 73 % / 74 % | +83 % |

Read against the lead's five conditions:
- **Careful within 3 of shipped:** every variant at cap 2 % holds it (-3 to +1); at cap 3 % the 150 % variants miss
  (-4 to -6).
- **Clumsy completion at least 40 %:** no variant of the grid reaches it (36.2-38.8 %). The clumsy pilot never flanks
  (100 % of its landings from the front) and dies in the Sentry Hall itself (room 5: 8 / 5 of its floor-2 deaths at
  `t45-h125-c2`), so turn and cap cannot help it; it fights the front's effective health, 160 x 1.25 / 0.35. Taking
  half a front landing instead of 35 % (`f50`) brings it to 40.8 %.
- **The Daggers' lead well above the Captain's 19 %:** +83 % to +97 % everywhere.
- **The elite fight in 6-12 s: not reachable.** The careful elite fight P50 is 3.3-4.3 s at every variant (under fire
  2.8-4.0 s): the flank that a slower turn opens shortens the fight as much as the health lengthens it. Only the clumsy
  pilot's fight passes 5 s. The row needs health well past 150 % or a lower front share with more health; it is the one
  condition this grid cannot meet.
- **A clear careful-minus-standing gap:** 24-34 / 24-34 clears everywhere (shipped 11 / 21).

**Top 3**, all with the flank habit:
1. **`sentry-t45-h125-c2-f50`** (45 deg/s, 125 %, cap 2 %, half a front landing): careful +1 / -2, clumsy completion
   40.8 %, Daggers +83 %, gap 28 / 24. The only variant that meets four of the five; the elite fight is 3.3 s.
2. **`sentry-t45-h125-c2`**: careful -1 / -1, Daggers +88 %, front share 72 %, gap 25 / 28; clumsy completion 38.8 %.
3. **`sentry-t60-h150-c2`**: careful -1 / -1, the Daggers' lead +96 %, the longest careful fight of the grid (4.3 /
   4.1 s), gap 26 / 30; clumsy completion 38.0 %.

**2. The Glass Regent on the holdout** (seeds 201-400, copies, the sidestep habit). Shipped on these seeds: careful
194 / 194, standing 179 / 178, first-card 164 / 186, clumsy 62 / 99; the Warden's fight P50 50 / 46.

| Gate boss (seeds 201-400) | Careful P50 / P90 V; I (in band) | Careful | Standing | First-card | Clumsy | Lane hits careful / standing / clumsy (V; I) | Sword's lead |
|---|---|---|---|---|---|---|---|
| Forge Warden (shipped) | 50 / 60; 46 / 55 (69% / 58%) | 194 / 194 | 179 / 178 | 164 / 186 | 62 / 99 | - | 20 % |
| Regent 6.4 | 46 / 57; 44 / 53 (55% / 45%) | 196 / 194 | 186 / 187 | 162 / 186 | 56 / 92 | 0.5 / 0.2 / 1.2; 0.4 / 0.2 / 1.4 | 27 % |
| Regent 6.7 | 48 / 59; 45 / 55 (61% / 53%) | 195 / 195 | 184 / 184 | 163 / 184 | 58 / 94 | 0.6 / 0.2 / 1.3; 0.5 / 0.2 / 1.4 | 28 % |
| Regent 7.0 | 49 / 60; 46 / 56 (69% / 60%) | 196 / 193 | 183 / 183 | 165 / 186 | 56 / 91 | 0.5 / 0.3 / 1.3; 0.5 / 0.2 / 1.5 | 27 % |

**3. One alternative each for the Wisp and the Clerk** (seeds 1-200, against shipped on the same seeds).

- **`wisp-kite`**: the blink comes only after 1.5 s of straight walking (heading within 20 degrees), lands 3 units ahead
  along the walk and strikes on landing (no hold). Blinks fall from about 40 to 2.2-2.9 a floor: the pilots rarely walk
  1.5 s straight inside a fight, mostly between halls. Clears (V / I): careful 193 / 192, standing 179 / 171, first-card
  177 / 193, clumsy 65 / 107, against 191 / 191, 180 / 170, 172 / 192, 66 / 108. The hero takes 0.1-0.2 health a
  minute from Wisps. **No skill separation, and no cost**: a 10-health horde body that strikes for 0.8 cannot punish a
  kite even when it lands in front of it.
- **`clerk-reach`**: the copy has no ring and no timer; it draws the packs and bursts as the first pack body comes
  within 1.8 of it (a pack halts at its reach, 1.4-1.6, from its goal; a first run with 1.0 never burst), hitting the
  hero within 2.4 for 10 %; untouched for 3 s it fades. A cohort that leaves marks does not see it: only walking away
  saves the hero, which the kiting pilots do and the standing one does not.
  About 4 copies a floor: 2.6-2.9 burst, 1.2-1.5 fade, the packs drawn 8 s a floor. Clears (V / I): careful 188 / 189,
  standing 172 / 163, first-card 175 / 191, clumsy 65 / 104 (shipped 191 / 191, 180 / 170, 172 / 192, 66 / 108). The
  halls are not easier: careful, standing and clumsy lose 1-8 clears (first-card +3 / -1). The careful-minus-standing
  gap grows to 16 / 26 (+5 / +5), but
  not through the copy: every cohort is hit about once a floor (careful 1.0-1.1, standing 1.1, clumsy 1.1-1.3). The
  packs halt round the copy at 1.4-1.6, and a kite holds the hero 1.6-2.5 from them, inside the 2.4 blast, so the kite
  does not save the hero either; the standing cohort simply carries the 10 % into the gate worse (its boss deaths 14 /
  15 against 7 / 9). **A cost, not a read.** For the copy to separate, the blast has to be smaller than the distance a
  kite keeps (about 1.6) or the burst has to wait for the pack to close on the copy, with the hero's step off it as the
  counter.

### Recommendation after round 2

- **Mirror Sentry: build `sentry-t45-h125-c2-f50`** - 45 degrees a second, 200 health (125 % of 160), half of a front
  landing taken, the reflection capped at 2 % of the hero's maximum a second, the flank habit in `DevelopmentAutopilot`
  and the simulation together. It keeps careful within 3, clumsy completion at 40.8 %, the Daggers' lead at +83 % and a
  gap of 28 / 24 clears. Section 7's elite-fight row (6-12 s) should be rewritten or dropped for the Sentry: no variant
  of the grid passes 4.3 s for the careful pilot, because the flank that makes the Sentry a read also shortens it.
- **Glass Regent: share 7.0.** On the holdout its careful P50 is 49 / 46 s with 69 % / 60 % of the fights in band,
  against the shipped Warden's 50 / 46 and 69 % / 58 % on the same seeds; 6.7 is the lowest share in band but puts Ilya
  on the edge at 45 s. Clears stay within 1 of shipped or above for the careful, standing and first-card cohorts (careful
196 / 193 against 194 / 194), and the Sword
  leads by +27 % (the Warden: +20 % on these seeds). Two misses to carry into the build: careful lane hits are 0.4-0.6 a
  fight (section 7 asks at most 0.3), and the clumsy cohort, which never sidesteps, loses 4-8 clears. It still needs the
  sidestep habit in `DevelopmentAutopilot` before it can replace the Warden.
- **Silver Wisp: do not build the kite-punisher either.** Neither form separates skill, because a horde Wisp is a
  10-health body that strikes for 0.8; the blink's geometry is not the problem, its body is. Keep the frosted Runner on
  floor 2, or give the Wisp a Runner's health in the horde before measuring a blink again.
- **Faceless Clerk: not yet.** The ringless copy is a cost every cohort pays about once a floor; it widens the gap
  only because the standing cohort carries the damage into the gate worse. Next round, if wanted: blast radius 1.4
  (inside a kite's distance) with the burst waiting for the pack to close.

## Lead decisions 2026-11-12 (the owner delegated the gameplay calls to the lead)

**The Mirror Sentry was built as `sentry-t45-h125-c2-f50`** (section 8, step 2). The rounds below replaced its tuning; it
ships as `sentry-r0-f70` ("Shipped: `sentry-r0-f70`", the last section). The rest of this list stands, except where that
section says otherwise:
- It replaces the Grunt Captain as floor 2's elite. Sentry Hall is Sentry, Grunt, Mite, Mite. `EliteRule` treats the first
  enemy of an elite room's wave as the elite, so the Daggers' x2 applies unchanged. Floor 1 and floor 3 (Gloam Gardens'
  Thorn Post) keep their Captain.
- The guard (`MirrorGuardRule`): the mirror turns toward the hero at 45 degrees a second. Its front arc is 120 degrees,
  centred on its facing and judged by the hero's position, not the strike's. A landing of the hero's made from the front
  deals half to it, and reflects 20 % of the landing's full amount to the hero. The reflection is capped at 2 % of the
  hero's maximum health a second for each Sentry: a budget that starts full and refills at that rate. Flanks and the rear
  take the landing whole and reflect nothing. A burn's tick (`DamageContext.IsTick`), the pit's spill and a relic's
  counter are not landings and pass untouched.
- `Enemy_MirrorSentry`: health 200 (125 % of 160; the floor's scaling applies as for every enemy). `Weapon_MirrorBash`:
  5.4 every 1.4 s, reach 1.6, 0.6 s before the first. Speed 1.1, gold 20, experience 10.
- **The reflection waits while the encounter holds** (an open choice panel, the boss's reveal, the player's pause).
  What the landings owe is banked and paid as one hit, from the last Sentry that reflected, on the next frame the fight
  runs (`MirrorReflectionDue`), so the hero never loses health while it chooses. Round 1 and round 2 paid it the next
  frame even under a panel. The guard's clock (turn, budget) holds too, as the Warden's slams and the burns do.
- **The flank** (`FootworkRule.Flank`) goes into `DevelopmentAutopilot` and the simulation together. A cohort that steps in
  (careful, first-card) and whose best single-target weapon is short (the Daggers) turns the step-in's hold point round the
  Sentry toward its rear, at most 40 degrees a goal, at the same reach.
- Section 7's elite-fight row (6-12 s) is **reported** for the Sentry, not a target. No variant of round 2 passes 4.3 s
  for the careful pilot, because the flank that makes the Sentry a read also shortens its fight.
- The codex entry C06 "Liquid Names" (docs/44) has nothing to unlock yet: the game has no codex.

**How it is built.** One pure rule, `Scripts/Combat/MirrorGuardRule.cs` (its turn, arc, front share, reflection and
refill in never-inlined bodies, a `MirrorGuardState` for each Sentry and the `MirrorReflectionDue` bank), stepped by the
scene and by the simulation alike:
- Scene: `MirrorGuard` on the Sentry's prefab sits ahead of its `Health` (`Health.Guard`). `MirrorGuardService` (script
  order -39, after `WardenSlamService`'s -40 and before every attacker) turns it, refills its budget and pays what is owed.
  It works on the foundry only, so the hall arena's Sentry fights as authored, as that arena's elites and Warden do.
  `MirrorGlintView` draws a violet (#8B6CE8) ground arc as wide as the front arc and turned to the guard's exact facing,
  and a three-frame violet glint from the mirror to the hero for each reflection paid. It never uses red-orange (section 2,
  rule 3). The art is the Blender-rendered `MirrorSentryArtSet`; it has no frosted copy, so the set is its own floor-2 look.
- Simulation: `DescentSimulation.MirrorSentry` leads the shipped floor 2's Sentry Hall. Its guard is stepped where the
  service steps it (`StepVaults`, after `StepWarden`). The research levers are kept: `VaultsCaptain` puts the Captain back,
  and `SentryTurnDegrees`, `SentryHealthScale`, `FlanksTheSentry` and the rest move the tuning.
- Tests: `MirrorGuardRuleTests`, `FootworkRuleTests` (the flank), `FoundrySimulationSceneValueTests` (the script order and
  `TheMirrorSentryIsTheSimulations`), and the PlayMode `MirrorSentryFlowTests` on `floor_sentry_proof`.
- `VaultsEnemiesResearch` now plays the four-floor Descent. Its finale read floor 3 after docs/48's merge, so its
  completion column was empty. `shipped` is the Sentry and `captain` is floor 2 before 2026-11-12.

**Measured** (seeds 1-200, both heroes, `FOUNDRY_REALISM=typical`, the four-floor Descent):

`VAULTS_OPTIONS=shipped,captain VaultsEnemiesResearch` at the shipped code, with the reflection held under the panels.
`captain` is floor 2 before today; it reproduces round 1's `shipped` row and the default batch's cohort section before the
change, cell for cell.

| Floor 2, Vanguard / Ilya | `captain` (before) | `shipped` (the Sentry) | Round 2's `sentry-t45-h125-c2-f50` |
|---|---|---|---|
| Careful clears (deaths) | 191 (4) / 191 (7) | **192 (3) / 189 (9)** | 192 / 189 |
| Standing clears | 180 / 170 | **164 / 165** | 164 / 165 |
| Careful-minus-standing gap | 11 / 21 | **28 / 24** | 28 / 24 |
| First-card clears | 172 / 192 | 175 / 185 | - |
| Clumsy clears (deaths in Sentry Hall) | 66 (0) / 108 (0) | 62 (4) / 101 (3) | 62 / 101 |
| Auto-farmer clears | 3 / 8 | 4 / 7 | - |
| Sentry front share, careful / standing | - | 73 % / 74 %; 100 % / 100 % | 73 % / 74 % |
| Health reflected per elite fight, careful / standing / clumsy | - | 10.7 / 9.5; 14.7 / 13.4; 19.0 / 15.2 | - |
| Elite fight P50, careful (reported) | - | 3.3 / 3.2 s | 3.3 / 3.2 s |
| The Daggers' lead on the elite leader (roles row) | +19 % | **+83 %** | +83 % |
| The Sword's lead on the gate boss | +25 % | +25 % | - |
| Roles (`FoundryGate.LeadsJob`) | met | met | met |

Floor 2's numbers are round 2's `f50` cell for cell, so holding the reflection under a panel moves nothing measurable.
The Descent now has floor 3 (docs/48), so completion is not round 2's three-floor figure. Completed, of 400 runs:

| Four-floor Descent | `captain` | `shipped` | Change |
|---|---|---|---|
| Careful | 379 (94.8 %) | 372 (93.0 %) | -7 |
| Standing | 332 (83.0 %) | 312 (78.0 %) | -20 |
| First-card | 344 (86.0 %) | 339 (84.8 %) | -5 |
| Clumsy | 161 (40.3 %) | **147 (36.8 %)** | -14 |
| Descent P50, careful V / I | 14.5 / 14.3 min | 14.5 / 14.3 min | - |

**What this leaves open.** In the four-floor Descent, clumsy completion falls under docs/40's 40 % edge: 36.8 %, against
40.3 % before the Sentry. With floor 3 the edge was already at 40.3 %, so any floor-2 cost now crosses it. The three-floor
round 2 put the same Sentry at 40.8 %. The levers are section 7's fallbacks: `SentryReflectShare` 0, the guard alone; or a
lower `SentryHealthScale`. The standing cohort carries the reflection into the gate: its floor-2 boss deaths are 23 / 14
against 7 / 9.

**The default batch** was run before and after the change, with the same environment: `FOUNDRY_SEEDS=200
FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_REALISM=typical FOUNDRY_PARALLEL=1 FOUNDRY_FLOORS=4`. The runs were at
b85969c2 and at the Sentry's simulation port. The port ran before the reflection hold; the research run above was at the
final code and gives the same floor-2 cells.
- Floor 1 is byte-identical. `foundry-gate.md` and `foundry-batch.csv` match byte for byte. In `foundry-batch.md` only
  the clock in its header and floor 1's wall-seconds column differ. In `foundry-cohorts.md` every floor-1 line is the same.
- Only the floor-2 rows and those that follow from them move: floor 2, floor 3, the finale and the whole Descent.
- The cohorts' floor-2 clears move from 191 / 191, 180 / 170, 172 / 192, 66 / 108 and 3 / 8 to 192 / 189, 164 / 165,
  175 / 185, 62 / 101 and 4 / 7, in the order careful, standing, first-card, clumsy, auto-farmer. These are round 2's
  `f50` row. The port is exact.
- The shipped frame-perfect careful pilot clears floor 2 on every seed for both heroes, before and after.

### The 40 % rule and the sweep (2026-11-12, later)

**The lead's decision.** docs/40's 40 % line for clumsy completion holds for the whole four-floor Descent (docs/52 takes
the same rule): no creature may bring the clumsy cohort below it. The Sentry ships only in a variant that keeps clumsy
completion at 40 % or more. Of those, it must keep careful clears on floor 2 within 3 of the Captain and the Daggers'
lead above +50 %. The largest careful-minus-standing gap then wins.

**The sweep.** One lever at a time off `t45-h125-c2-f50`, the flank on. The run used `VaultsEnemiesResearch` on the
final code: seeds 1-200, both heroes, every cohort, typical hands, the four-floor Descent. Clumsy completion is of 400
runs; floor-2 cells are Vanguard / Ilya.

| Option | Clumsy completion | Careful F2 | Standing F2 | Gap | Daggers' lead | Standing F2 boss deaths | Clumsy deaths in Sentry Hall | Careful front share | Careful elite fight P50 | Reflected a fight, careful |
|---|---|---|---|---|---|---|---|---|---|---|
| `captain` (before) | 161 (40.3 %) | 191 / 191 | 180 / 170 | 11 / 21 | +19 % | 7 / 9 | 0 / 0 | - | - | - |
| `shipped` (`f50`) | 147 (36.8 %) | 192 / 189 | 164 / 165 | 28 / 24 | +83 % | 23 / 14 | 4 / 3 | 73 % / 74 % | 3.3 / 3.2 s | 10.7 / 9.5 |
| `sentry-f60`: 60 % of a front landing taken | 145 (36.3 %) | 189 / 190 | 168 / 168 | 21 / 22 | +81 % | 19 / 11 | 3 / 6 | 74 % / 74 % | 3.1 / 3.0 s | 10.2 / 9.0 |
| `sentry-c1`: reflection capped at 1 % a second | 153 (38.3 %) | 193 / 189 | 169 / 167 | 24 / 22 | +83 % | 18 / 12 | 3 / 3 | 73 % / 74 % | 3.3 / 3.2 s | 5.3 / 4.8 |
| `sentry-h100`: health 160 | 149 (37.3 %) | 188 / 191 | 168 / 167 | 20 / 24 | +77 % | 19 / 12 | 2 / 3 | 75 % / 76 % | 2.9 / 2.9 s | 9.8 / 8.5 |
| `sentry-r0`: no reflection, the guard alone | 157 (39.3 %) | 193 / 189 | 175 / 167 | 18 / 22 | +83 % | 12 / 12 | 0 / 2 | 73 % / 73 % | 3.3 / 3.2 s | 0 |

**No variant reaches 40 %, so nothing ships yet.** The code keeps `f50`, and the Sentry waits for a variant that passes.
- The closest is `r0`, at 157 of 400; it needs 160.
- The reflection is the larger cost. Removing it gives back 10 completions (147 to 157) and the standing cohort's gate
  deaths (23 / 14 to 12 / 12).
- The front share and the health move clumsy completion little (-2 to +2). The clumsy pilot fights the front's
  effective health whichever way the turn goes.
- The Captain's floor itself stands at 40.3 %, one completion above the line. A floor-2 elite therefore has to cost the
  clumsy cohort at most one completion in 400. That is within this sample's noise: the binomial SD at 40 % of 400 is
  about 2.4 points.
- The Daggers' lead stays at +77 % to +83 % and careful within 3 of the Captain in every variant: the flank, not the
  reflection, makes the Sentry the Daggers' elite.

**Recommended next round.**
- Keep `r0` and soften the front for the cohort that never flanks: `r0-f70` (effective health from the front 286,
  against the Captain's 140 and `f50`'s 400), `r0-f60-h100` (267) and `r0-f80` (250).
- Or keep a small reflection only for the flanking cohorts' benefit: `c1-f70`.
- Each costs 3 minutes on 8 threads. Read them against `captain` on the same seeds and, given the noise, on the holdout
  seeds 201-400 too, before any of them ships.

### Rounds 3 and 4 (2026-11-12, night): the ship rule on two seed ranges

**The lead's rule, since the line sits inside the noise.** A variant ships only if all four hold:
- clumsy completion of the four-floor Descent is at least 40.0 % on seeds 1-200;
- it is at least 39.0 % on the holdout, seeds 201-400;
- careful clears on floor 2 stay within 3 of the Captain's;
- the Daggers' lead stays above +50 %.

Among the variants that pass, `c1-f70` is preferred, because it keeps the reflection. Otherwise the largest
careful-minus-standing gap wins.

**The runs.** Each variant was played on both seed ranges against `captain` on the same seeds, with
`VaultsEnemiesResearch` on the shipped code: both heroes, every cohort, typical hands, the flank on. Round 3 is `r0-f70`,
`r0-f60-h100`, `r0-f80` and `c1-f70`. Round 4 is `c1-f80` and `r0-f90`. Completion is of 400 runs; floor-2 cells are
Vanguard / Ilya.

| Option | Clumsy completion, 1-200 | Clumsy completion, 201-400 | Both (of 800) | Careful F2, 1-200 / 201-400 | Gap, 1-200 / 201-400 | Daggers' lead, 1-200 / 201-400 | Standing F2 boss deaths, 1-200 / 201-400 | Reflected a fight (careful), 1-200 |
|---|---|---|---|---|---|---|---|---|
| `captain` | 161 (40.3 %) | 146 (36.5 %) | 307 | 191 / 191; 194 / 194 | 11 / 21; 15 / 16 | +19 %; +31 % | 7 / 9; 10 / 14 | - |
| `f50` (built) | 147 (36.8 %) | - | - | 192 / 189 | 28 / 24 | +83 % | 23 / 14 | 10.7 / 9.5 |
| `r0-f70` | **160 (40.0 %)** | 146 (36.5 %) | 306 | 191 / 194; 195 / 194 | 17 / 22; 18 / 15 | +74 %; +58 % | 13 / 7; 12 / 13 | 0 |
| `r0-f60-h100` | 159 (39.8 %) | 148 (37.0 %) | 307 | 191 / 193; 192 / 193 | 18 / 22; 11 / 16 | +69 %; +77 % | 14 / 8; 8 / 15 | 0 |
| `r0-f80` | 155 (38.8 %) | 144 (36.0 %) | 299 | 190 / 191; 193 / 193 | 11 / 18; 11 / 15 | +63 %; +62 % | 8 / 6; 7 / 14 | 0 |
| `c1-f70` | 153 (38.3 %) | 142 (35.5 %) | 295 | 190 / 193; 194 / 194 | 16 / 23; 17 / 17 | +76 %; +58 % | 13 / 9; 12 / 15 | 4.9 / 4.3 |
| `c1-f80` | 148 (37.0 %) | 136 (34.0 %) | 284 | 190 / 190; 192 / 194 | 12 / 21; 12 / 17 | +63 %; +62 % | 9 / 10; 9 / 15 | 4.7 / 4.1 |
| `r0-f90` | 159 (39.8 %) | 141 (35.3 %) | 300 | 191 / 191; 194 / 192 | 15 / 20; 14 / 14 | +62 %; +58 % | 11 / 8; 9 / 14 | 0 |

**No variant passes the absolute rule** (superseded by the lead's relative rule in the next section).
- Only `r0-f70` meets the bar on seeds 1-200, with 160 of 400, exactly 40.0 %.
- No variant can meet the holdout bar. On seeds 201-400 the Captain itself completes 36.5 % (146 of 400), under the
  39.0 % line. A Sentry that is no easier for the clumsy pilot than the Captain cannot pass it.
- Careful clears within 3 of the Captain and the Daggers' lead above +50 % hold for every variant on both ranges.

**The noise.** At 40 % of 400 runs the binomial SD is about 10 completions (2.4 points).
- The readings move by that much between neighbours that should be ordered. `c1-f80`, softer than `c1-f70`, completes
  148 against 153. `r0-f80`, softer than `r0-f70`, completes 155 against 160.
- The two seed ranges differ by 15 completions for the same Captain (161 and 146).
- An absolute bar reads the seeds as much as the Sentry.

**Recommendation for the lead.** Read the bar against the Captain on the same seeds, for example "the clumsy cohort
loses at most 1 completion in 800 against the Captain". On the pooled 800 runs:
- `r0-f60-h100` costs nothing (307 against 307);
- `r0-f70` costs 1 (306);
- `c1-f70`, the one that keeps the reflection, costs 12 (1.5 points), and `f50` about 14 on seeds 1-200 alone.

Of the two that meet such a bar, `r0-f70` has the larger careful-minus-standing gap (72 against 67 over both ranges).
Its Daggers' lead is +74 % / +58 %. Shipping it gives up the reflection, and with it the violet reflect glint; the
front arc and the turn remain the creature's read. Keeping the reflection costs the clumsy cohort about 1.5 points
over 800 runs. The choice is the lead's or the owner's.

### Shipped: `sentry-r0-f70` (cleared, 2026-11-12)

**The lead's rule, replacing the absolute bars.** The holdout seeds showed that a fixed bar reads the seeds, not the
creature: on seeds 201-400 the Captain itself completes 36.5 %. The lead also notes the rule in docs/52. Over seeds
1-400 combined, a new creature may cost the clumsy cohort at most 1 completion in 800, against the shipped floor on the
same seeds. Careful clears on floor 2 must stay within 3 of it on each range, and the Daggers' lead must stay above +50 %.

**`sentry-r0-f70` passes:**

| `r0-f70` against `captain` | Seeds 1-200 | Seeds 201-400 | Both |
|---|---|---|---|
| Clumsy completion (of 400) | 160 against 161 | 146 against 146 | **306 against 307 (-1 of 800)** |
| Careful F2 clears, V / I | 191 / 194 against 191 / 191 | 195 / 194 against 194 / 194 | within 3 on each |
| Careful-minus-standing gap, V / I | 17 / 22 against 11 / 21 | 18 / 15 against 15 / 16 | - |
| The Daggers' lead on the elite | +74 % (Captain +19 %) | +58 % (Captain +31 %) | above +50 % |
| Standing F2 boss deaths, V / I | 13 / 7 against 7 / 9 | 12 / 13 against 10 / 14 | - |
| Careful front share | 74 % / 74 % | 74 % / 77 % | - |
| Careful elite fight P50 (reported) | 2.9 / 2.8 s | 2.9 / 2.8 s | - |

`r0-f60-h100` passes too (307 of 800, no cost), but its gap is smaller (67 clears against 72 over both ranges). `c1-f70`
keeps a reflection and costs 12 of 800, so it fails.

**What ships.**
- `MirrorGuardRule`: turn 45 degrees a second, front arc 120 degrees, **70 %** of a front landing taken
  (`FrontShare` 0.7), **nothing reflected** (`ReflectShare` 0; `ReflectCap` stays 0.02, unused).
- Health 200, `Weapon_MirrorBash`, speed 1.1, gold 20 / experience 10, as built.
- The flank habit, as built.
- The research levers keep the reflection path (`MirrorReflectionDue` and its hold under panels) for any later tuning.
- **The reflection as feedback only.** A landing the mirror blunted flashes the violet reflect glint from the mirror
  toward the hero: `MirrorGuardService.Blunted`, drawn by `MirrorGlintView`.
  - It is rate-limited to one every 0.8 s (`BluntGlintSeconds`), so the glint shows for some blunted landings and
    a fast weapon never spams it.
  - It deals no damage: violet never hurts (section 2, rule 3).
- `MirrorSentryFlowTests` holds this: the front share, whole rear landings, no health reflected, and the glint shown but
  never twice within 0.8 s.

**Floor 1 and the batch.** The default batch was run again at the shipped code, with the "before" run's environment
(`FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_REALISM=typical FOUNDRY_FLOORS=4`). Only the thread count
differed, and runs are seeded, so threads change no result.
- Floor 1 is byte-identical to the batch before the Sentry. `foundry-gate.md` and `foundry-batch.csv` match byte for
  byte. In `foundry-batch.md` only the header's clock and floor 1's wall seconds differ. `foundry-cohorts.md` keeps every
  floor-1 line.
- The cohorts' floor-2 clears are 191 / 194, 174 / 172, 175 / 189, 64 / 107 and 5 / 7, in the order careful, standing,
  first-card, clumsy, auto-farmer. They were 191 / 191, 180 / 170, 172 / 192, 66 / 108 and 3 / 8 with the Captain.
- The cohorts that reach the finale number 189 / 190, 165 / 162, 164 / 179, 62 / 98 and 4 / 2. The clumsy cohort's 160 of
  400 is `r0-f70`'s reading above.
