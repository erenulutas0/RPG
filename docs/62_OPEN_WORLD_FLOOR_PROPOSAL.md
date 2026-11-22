# 62 - An open world before each floor's boss sanctum (proposal, 2026-11-22)

Design and research only. No runtime script, asset, scene, setting or test changed. Everything here is **proposed** for
the owner and the lead; numbers marked "target" are starting values to be measured, not results. (Since 2026-11-22
evening, section 12 records P0a as built behind a development switch and measured.)

**The owner's request (2026-11-22, translated from Turkish):** "The map is too ordinary. Every room has the same thing.
I want to make the current rooms like boss rooms. Before them, like Megabonk's different worlds, I want a bigger world:
with jumping and leaping, with capturing, longer, Megabonk-style. You kill bosses there and get out of the final swarm,
then exit into the current rooms. It's a lot of work. Can AI tools or other assets handle it?"

The owner also confirmed that the fiction is an **upward climb** up a tower, not a descent (docs/44 section 1.1: "the
player never goes down; always climbs up"). This document says "the climb"; the code still calls it the Descent
(`DescentSimulation`, `EncounterController.DescentFloor`).

## 0. The short version

- **Structure.** Each floor becomes three phases. First comes the **world**: an open, terraced map of about 2-3 times
  the foundry court, Megabonk-shaped, on a clock of about 3 minutes. Then the clock rings the **Bell**: the floor's swarm
  rises, and the hero fights up to the **Ascent Stair** at the top of the world. The stair is a portal into the
  **sanctum**: today's hand-built foundry region, re-used as the floor's boss sequence (the Forge, then the floor boss;
  from P1 an antechamber mini-boss first, drawn with a layout, a hazard and a rule). The checkpoint, extraction, the
  tiers and the map choice keep their rules.
- **What moves where.** The three combat halls leave the floor. Their job (fights, XP, gold, a reason to cross the map)
  moves into the world: the horde, **capture shrines**, **roaming Keepers** (mini-bosses), chests and jump pads. The
  Forge, the elite and the gate boss stay in the sanctum, where they become the floor's boss rooms.
- **The principle that keeps balance cheap: power parity at the boss.** The world must deliver the hero to the floor
  boss at the level and health today's floor delivers to the 3:30 gate. If it does, the Warden, floors 2 and 3, the
  tiers and the map choice's catch-up levels need no re-tuning, and the docs/56 rule still applies.
- **Pacing.** World 2:45-3:00, swarm escape 15-30 s, sanctum 70-105 s: a floor of 4:10-5:15. The climb is about
  14:50-16:05 with today's crown swarm, at the top edge of 13-16 minutes; section 4 gives two ways to keep it inside.
  A longer climb (17-19 minutes) is an option only after P0 shows the world is the fun.
- **Our camera favours a tall world.** The camera shows 9 floor units across (`Gameplay.unity`, `_visibleWidth: 9`). The
  floor is drawn at half height (`FoundrySurface.Project`: y x 0.5). So a portrait phone sees about 3 times further up
  the screen than across it. The world should be a tall strip of terraces that climbs up the screen, which is also the
  fiction.
- **Jumping without a new button.** Jump pads launch automatically. Narrow gaps take an auto-hop. Ledges drop one way
  only. Each jump is an arc with a shadow. Enemies cannot follow a jump: they must path round by the ramps, so a pad is
  an escape, as Megabonk's movement is.
- **Plan.** P0a is a 6-8 agent-day **mechanics probe in the existing court**: shrines, a Keeper, pads over the pit, and
  the Bell, the swarm and the gate. P0b is a ~21 agent-day **greybox world for floor 1**. P1 (art and depth for floor 1)
  is ~27 agent-days and P2 (the Vaults and Gardens worlds) ~33. The whole is about **90 agent-days**. AI tools and asset
  packs cut perhaps a third of the art days in P1-P2. They cut almost nothing in P0, which is code, simulation and
  measurement.
- **My opinion.** The direction is right: build-up in a varied space, then a boss sanctum, answers "every room is the
  same". A Megabonk-sized world is not right for a 9-unit-wide portrait view with one thumb. Make the world **medium,
  tall and dense with decisions**, and prove it with P0a before building any world.

## 1. The structure

### 1.1 A floor today, and a floor proposed

Today every floor plays on the same foundry map (`FoundryMap`, one immutable layout; `FoundryRegionView` builds it once,
~17,800 lines of code-built meshes, and no builder reads the floor number). Each floor is six rooms on four district
sites: three combat halls (Ember Hall, Cinder Walk, Slag Gate on floor 1), the Forge, the elite's post and the boss gate
(`Floor_EmberHalls.asset`: `_kind` 0, 0, 0, 1, 2, 3). The horde walks the map continuously (`HordeDirector`, scene cap
40), and the gate opens no sooner than 3:30 (`_foundryGateNotBeforeSeconds: 210`). That is what the owner calls "every
room the same": the halls differ only in their wave lists, and floors 2 and 3 re-theme the same ground.

| | Today | Proposed |
|---|---|---|
| Ground | One foundry court per floor, re-themed | **World** (new, per biome) + **sanctum** (today's region) |
| Before the boss | 3 halls at districts, horde between them, 2 altars, chests | Horde over the world, 3-8 capture shrines, 1-2 roaming Keepers, chests, greed altars, jump pads, later a vendor |
| Clock | Floor clock 300 s, then overrun (`HordeClock`); gate not before 3:30 | World clock ~180 s, then the **Bell**: the swarm (`SwarmDirector`) and the open stair |
| Boss | Warden walks out of the gate onto the same court | Portal → sanctum: Forge → floor boss (P1: antechamber mini-boss first, with variety) |
| After the boss | Checkpoint: extract or climb | Unchanged |
| Top of the tower | Final Swarm floor (`Floor_FinalSwarm`, 65 s) | Kept in P0; owner question 5 |

### 1.2 One floor, phase by phase (floor 1, targets)

1. **Arrival (0:00).** The hero lands on the bottom terrace of the world. The HUD's floor clock (`FloorClockView`)
   counts down 3:00. The Ascent Stair's light column stands at the top of the map, and its edge arrow is always shown.
2. **The world (0:00-3:00).** The horde fills by the world clock: a thin opening, then the crowd, then pressure. The
   hero chooses a route: shrines to capture, chests, greed altars, pads up the terraces. At 0:45 a **Keeper** (a roaming
   mini-boss with an escort) appears on the upper terraces. It carries the **Stair Key**.
3. **Early exit (optional).** With the Stair Key, the stair opens before the Bell. A strong hero can leave early and skip
   the swarm, giving up the rest of the world's experience. This is Megabonk's "the player decides when the build is
   ready" (docs/27 section 1; [W7]).
4. **The Bell (3:00; 2:45 on floor 1 if the climb runs long, section 4).** The swarm rises: the existing finale's
   ladder (`SwarmDirector`, `SwarmRule`), weighted to spawn
   between the hero and the stair. The stair opens for everyone. To leave, the hero holds the stair's plate
   (`ExitRule`'s hold, standing still) while the crowd closes in. This is the "get out of the final swarm" moment.
5. **The sanctum (about 3:15 onward).** The portal puts the hero on the foundry's arrival bridge. The court is quiet
   (`HordeState.Boss`, target 6). The Forge stands at the bridge's end (heal and spend), then the gate opens and the
   floor boss walks out, as today. From P1, an antechamber fight comes first (section 2.7).
6. **Checkpoint.** Extract with the gold, or climb to floor 2's world.

### 1.3 How it fits what is built

- **Tiers (docs/54 section 21).** No change of rule. The tier's health, damage and reward multipliers ride
  `FloorScaling.Tiered` at every spawn: world horde, Keepers, swarm and sanctum. The world's new rewards (shrine cards,
  chest gold, the Keeper's purse) must pay through `FloorScaling.Gold` and `LootRule.Roll` with the tier, as chests do
  today. Later, a tier may add structure (a second Keeper on Inferno, the Bell 15 s earlier on Abyssal); P0 is Forged
  only.
- **The map choice's deeper starts (docs/53).** A deeper start begins at the bottom of that floor's world, at the
  floor's arrival level, with the queued level-ups and Auto-pick as today. The unlock rule is unchanged
  (`MapUnlock.IsOpen`: a floor opens once a run on that tier clears the one below, which now means beating its sanctum
  boss). Until floors 2 and 3 have worlds (P2), they keep today's hall structure. A climb that mixes a world floor 1 with
  hall floors 2-3 is fine and is what P0 and P1 ship.
- **Extraction (docs/44 section 1.2).** Unchanged: the checkpoint after each sanctum boss offers the Quiet Anvil. There
  is no extraction from inside the world; a death there loses the floor as a death in the halls does today.
- **Power parity.** The careful pilot reaches level ~22 at 3:00 on today's floor 1 clock (docs/41, "the gate's wait").
  The world must hand the sanctum boss the same distribution of level and health that today's 3:30 gate does (read from
  the default batch's `foundry-gate.md`). That is P0's balance gate (section 5).

### 1.4 The lore

| Piece | Fiction (proposal, for docs/44's owner review) |
|---|---|
| **World = the Outworks** | Each floor of the Ascendant Kiln has an outer ring of open yards, cooling terraces and abandoned workers' quarters that climbs to the next sealed heart. The Foundry Outworks: slag yards, bellows vents, lava channels. The Vaults: frozen terraces and quicksilver pools. The Gardens: overgrown terraces on ash. |
| **Capture shrine = Tithe Anvil** | Where the Dawn Engine collected people's unspent tomorrows. Standing in its ring reclaims one, and the hero takes a stat. The Unfinished rush whoever tries. This ties the main mechanic to docs/44's central tragedy. |
| **Keeper** | The floor's lesser guardians (the Grunt Captain on floor 1, the Mirror Sentry on floor 2), carrying the key of the stair they guard. |
| **The Bell and the swarm** | When a floor's shift bell rings, that floor's Unfinished rise. The Forge Warden's spawn line, "Shift's not over.", already says it. The tower-top Final Swarm stays where all floors' echoes gather (docs/44 1.6). |
| **Ascent Stair** | The sealed stair from the Outworks into the floor's heart: an upward step in both senses. |
| **Sanctum** | The floor's machine heart, held by its boss: the Warden's Crucible, the Warden's Vault, the Warden's Grove. These are today's region and boss rooms. |

The halls' lore objects (docs/44 1.7 and 3.4) split between the two. The Two Little Boots and the Punch Ledger move into
the Outworks' workers' quarter, a natural place for found evidence. The Exit Plate stays at the sanctum's gate, where the
Warden's death reveals it.

**A tension with docs/44.** Section 1.6's production note advises against a "huge new open world" for floors 3-6, and
asks for one visual idea, 2-3 hazards, 3 enemies and a boss per floor. This proposal answers it by keeping the world
medium-sized, built from one modular kit per biome, with that same per-floor list. It is still the larger of the two
roads, and the owner should choose it knowingly.

## 2. Mechanics for a portrait, top-down phone

### 2.1 What our camera can see (the constraint behind every choice below)

- The camera is orthographic. It shows a fixed **9 world units across** (`ArenaCameraFollow._visibleWidth`, 9 in
  `Gameplay.unity`). The hero sits in the free band between the HUD blocks (`FollowFraming.Fit`).
- The floor is drawn at half height (`FoundrySurface.Project`: x, y x 0.5 + height). On a 19.5:9 phone the band
  therefore spans roughly **9 floor units across and 25-30 up the screen**.
- The hero walks at **2.5 units a second** (`HeroMovementInput._speed`). A screen's width is 3.6 s of walking; its
  height is 10-12 s.
- The horde walks in from 7-12 units out (`FoundryMap.HordeSpotNear/Far`): off the screen's sides, inside its height.

Megabonk's player sees the world from a 3D third-person camera over long distances, and slides, bunny-hops and jumps
through it ([W6], [G4]). A one-thumb portrait phone keeps perhaps a third of that. What carries over well: decisions
under pressure (which shrine, when to fight the Keeper, when to leave), positional risk (standing in a ring), escapes by
terrain, and the clock with its swarm. What carries over badly: long-range exploration and movement tech. The design
below leans on the first list.

### 2.2 Jumping and leaping

| Element | Rule (target values) | Why this way on a phone |
|---|---|---|
| **Jump pad** (F1 bellows vent, F2 quicksilver geyser, F3 seed catapult) | Walk onto it, and it launches along its fixed direction to a fixed landing spot 6-10 units away (often a higher terrace) in 0.7 s. The hero's weapons keep firing in the air. No contact damage while airborne; projectiles still hit. | No new button. One thumb stays on movement. The landing is authored, so it is always valid and deterministic for the simulation. |
| **Arc with a shadow** | The sprite rises up to 1.2 units on screen on a parabola. Its contact shadow (`ContactShadowView`) stays on the ground track. The camera follows the shadow, not the sprite, so the view does not bob. There is a dust ring on landing. | The player reads where the hero lands. The shadow is the hero's true floor position, as in combat code. |
| **Ledge** | One-way: walking off a high terrace's edge drops the hero (0.25 s hop) to the terrace below. Climbing up needs a ramp, stairs or a pad. Ledge edges carry a bright coping lip; ramps carry chevrons. | Creates escapes and routes with no input. Enemies obey the same rule. |
| **Gap over lava** | Lava channels block walking. A **narrow** gap (up to 1.5 units, marked with hop lips) takes an **auto-hop** (0.35 s) when the hero steers into it at full strength. A wide gap needs a pad or a dash skill. | Auto-vault rather than a jump button. The Vanguard's Shield Rush and the Riftblade's Phase Dash (`HeroDash`) also cross gaps, which gives the skills a traversal use. |
| **Downhill ramp** | +20 % walking speed going down a ramp. | Megabonk's "slide downhill to go faster" ([W6], [G5]) at zero control cost. |
| **Dodge-hop button** | **Not proposed** for P0. If the owner wants a jump button (owner question 4), it would be the HUD's fifth control, beside three skills and the Forge Burst. | The bottom HUD is already crowded on a portrait screen (docs/57 section 14: three skill buttons). |

The enemies' side of this: pursuit uses a **flow field** over the world's cells, with ledges as one-way edges, and it
never uses pads. A hero who takes a pad up a terrace buys 3-6 seconds while the crowd routes round by the ramp. The
horde's next spawns appear on the hero's new terrace anyway (section 3.4), so a pad buys breathing room, not safety.

### 2.3 Capture shrines (Tithe Anvils)

- **Charge.** Stand inside a ring of radius 2.2 for 5 s. Moving inside the ring is allowed, so a hero can kite in
  circles. Outside the ring the charge decays at half the fill rate, gentler than Megabonk's quick reset ([W2]), because a
  thumb slips. A ring of light fills on the ground.
- **The risk.** While a shrine charges, the horde's target near it rises by +6, a small surge from two sides. Charging a
  shrine is a fight. Megabonk's own shrines add no enemies ([W2], [G3]); ours do, because our crowd is thinner (40 at most
  against Megabonk's reported hundreds).
- **The reward.** A choice of 1 of 3 stat cards through the existing offer panel (`UpgradeService.CreateOffer`), in a
  shrine frame, with rarity drawn by `RarityTable.Draw` at the run's Luck. This is Megabonk's charge shrine ([W2]) on our
  card system.
- **Variants (P1).**
  - Gilded anvil: Epic or better only, about 1 in 6 shrines.
  - Challenge anvil: plays one of the old halls' authored waves on demand (the `RoomDefinition` wave lists are reused)
    and drops a chest on the clear. Megabonk's challenge shrine ([W6]).
  - Lodestone: pulls every experience drop on the floor to the hero. Megabonk's magnet shrine ([W6]).
  - Greed: the existing two altars (`AltarService`, `AltarRule`), unchanged.
- **Counts.** 3 in P0, 6-8 in P1. Megabonk places about 15 charge shrines in a 10-minute stage ([W5]), one every 40 s;
  six in our 3 minutes is one every 30 s.

### 2.4 Roaming elites and Keepers

- **Keeper (P0: one; P1: two).** It appears at 0:45 (and the second at 1:45), echoing Megabonk's minibosses at 3:00 and
  8:00 of 10 minutes ([W7]). It patrols a loop of 3-4 waypoints on the upper terraces at 0.7x the hero's speed, with an
  escort formation (at most `PackLayout.MaxFormationSize`, 10). It aggroes within 7 units or when hit, and once aggroed
  it follows. It drops the Stair Key, a chest and gold. Floor 1's Keeper is the Grunt Captain at a mini-boss health
  share; floor 2's is the Mirror Sentry (docs/50); floor 3's comes from docs/52's roster.
- **Elite packs.** The existing `EliteRule` packs walk the world as roamers rather than waiting in a hall.
- **Readability.** A Keeper carries a lantern: a warm light on the ground ahead of it, visible from off-screen through its
  edge arrow (section 2.6).

### 2.5 Chests, breakables and the vendor

- **Chests** (P0): 3-4 per world at seeded spots from the world's layout, free, opened as today (`ChestSpawner`,
  `ChestRule`: heal, books, items).
- **Breakables** (P1): slag crates and urns dropping small gold or experience, as Megabonk's 40 small and 15 big pots per
  map ([W5]).
- **Vendor** (P1, owner question 7): a "Shady Tinker" offering 1 of 3 items for **run gold**. Gold spent here is gold
  not extracted, which is a real decision under our extraction rule. Megabonk's chests cost escalating gold (30, 72,
  115, 159 ... the 10th 495; [W3]). Ours stay free until the owner decides on the economy, because the forge and the meta
  already spend run gold (docs/54, docs/58).

### 2.6 Navigation on a tall screen

Megabonk's most-criticised map problem was **finding the boss portal**. Players asked for a clearer cue, lost runs
never finding it, and installed a mod that shows the boss arrow from the start ([P2], [P3], [M1]). The developer added
a full map with fog of war in v1.0.17 ([S5], checked 2026-11-22 against the patch's text). Our rules learn from that:

1. **The objective is never hidden.** The Ascent Stair has an edge arrow from the first second and a light column
   visible up the screen.
2. **Edge arrows** for points of interest off the screen: uncaptured shrines within 25 units, the Keeper, chests within
   15 units, the open stair after the Bell. At most 4 at once, prioritised stair > Keeper > shrine > chest. Each sits on
   the band's edge with the target's icon, and none is placed over the HUD. `ExitSealView` already draws such an arrow
   for the finale's exit (`_arrow`, "Exit Direction"). Vampire Survivors' mobile "guides" do the same: an on-screen
   arrow with the item's icon pointing at each item ([VS1]).
3. **The world climbs up the screen.** The long axis runs up the screen, with terraces rising toward the stair. The
   camera's band shows 25-30 units up, so the next terrace and its pads are usually in view. Survivor.io's up-down
   scrolling chapters use the same shape on a phone ([SV1], [SV2]).
4. **Look-ahead.** The camera's band centre leads the hero by up to 2 units in the walking direction (a change to
   `ArenaCameraFollow.Goal`, which already offsets for the foundry's ramp). A zoom lever of 9 to 10.5 units wide in the
   world only goes on the S23 capture, since a wider view shrinks every sprite.
5. **The map** (P1). A full map of the world with fog of war on the pause panel, and as a strip on the level-up panel,
   which already pauses the run. That is Megabonk's v1.0.17 answer ([S5]) without a second always-on minimap competing
   with the HUD.
6. **Landmarks.** A small set of tall pieces per world (a furnace stack, a broken crane, the stair's column) so a player
   learns "the stair is past the crane". They are made from the region's existing baked boiler and crane.

### 2.7 Variety in the boss rooms (the sanctum)

The owner's complaint was sameness, so the sanctum must not be one fixed fight. A sanctum is drawn per run (seeded)
from three tables, all on today's region and its four district sites:

| Table | Floor 1 entries (P1) | Built from |
|---|---|---|
| **Layout** of the antechamber | *Crucible Ring*: around the pit, with eruptions. *Workyard*: among the work islands as cover. *Causeway*: the narrow gate landing, a lane fight. *North Court*: open ground. | The district sites (`FoundryMap.RoomSite`), the pit, the islands, the gate landing. Temporary barricades (crates, rails) reshape the court with no new geometry. |
| **Hazard** | Pit eruptions (exists: `PitEruptionRule`). Slag rain: telegraphed circles, the slam mark's art. Flame jets on a rhythm from the court's rim. Swinging crane hook. | Existing telegraph art (`WardenSlamView`, `PitEruptionView`) and one or two new rules. |
| **Edict** (a rule for the fight) | *Shift's Not Over*: the boss enrages at 40 s. *Escort*: two elites beside the boss. *Cold Iron*: no heals in the room. *Bell Ward*: the boss is armoured until the hero holds two anvils in the room, which turns the shrine mechanic into a movement-based boss fight. | `EnrageRule`, `EliteRule`, the shrine rule from 2.3. |

The sanctum's sequence per floor:
- **Antechamber.** A mini-boss with a layout, hazard and edict drawn. P1, and floors 2-3.
- **The Forge.** At the bridge's end: heal and the forge's choice. It is the breath between two bosses.
- **The floor boss.** Out of the gate: the Forge Warden, the Glass Regent, later Rootmother Myrr.

Floor 1 in P0 skips the antechamber, so P0's sanctum is today's Forge and Warden, measured as they are.

Three layouts, four hazards and four edicts give 48 combinations a floor. They come from roughly 6-8 new rules and no
new region. On floors 2 and 3 the hazards re-theme: slippery ice and mirror lanes on the Vaults, root snares and pollen
(`PollenRule` exists) in the Gardens.

### 2.8 Hazards in the world, per biome

| Biome | Hazards (2-3 each, docs/44's mobile note) | Exists today |
|---|---|---|
| Foundry Outworks | Lava channels (impassable; crossed by pads or a hop). Slag vents: telegraphed eruptions on a timer. Conveyor strips that carry actors. | The pit's eruption rule and art |
| Vault Terraces | Ice patches (steering lag). Quicksilver pools (x0.6 speed; Megabonk's water and mud are x0.4, [W6]). Mirror panels that block sight lines. | The ice materials, mirror panels, snow drifts (docs/49) |
| Gloam Terraces | Root snares (hold the hero 1 s unless dashing). Pollen clouds (vision). Thorn hedges that regrow. | `PollenRule`, `ThornlingRule`, the gloam growth (docs/51) |

## 3. Building the world

### 3.1 Hand-built, procedural, or a mix

| Option | For | Against |
|---|---|---|
| **Hand-built** (authored like the foundry region) | Full art control; readable | The foundry region took September to November and ~17,800 lines; one world a biome at that cost is out of reach. No run-to-run variety, which Megabonk's reviewers already faulted ([R1], [R2]). |
| **Fully procedural** (Megabonk: "randomly generated maps", [S1]) | Variety, cheap per run | Hard to make readable on a 9-unit view. Megabonk's own variation is "minor" ([R2]), and its maps drew the "everything looks similar" complaint ([P2]). Connectivity and fairness need proofs. |
| **Mix (recommended): a fixed skeleton, shuffled chunks, random objectives** | Players learn the shape (start at the bottom, stair at the top, three terraces). Each run differs in chunk choice, shrine, chest and pad spots and Keeper routes. The world is seeded from `RunSeeds`, so the scene and the simulation build the same one. | Needs a template library and a placement validator. |

**The mix in detail.**
- A world is a **skeleton**: 3 terraces stacked up the screen, each a band of chunk slots, with ramp and pad
  connections between bands.
- Each slot takes one of N authored **chunk templates**, 12 x 12 cells. A template is a small hand-made piece: floor,
  ledges, a lava channel, a pad pair, socket points for shrines, chests and props.
- Edge **sockets** (open, ledge, wall) must match between neighbours.
- A **validator** proves before play:
  - every cell is reachable from the start;
  - the stair is reachable on foot, not only by pads;
  - shrines are at least 12 units apart;
  - no spawn ring is empty.
  It rejects and redraws the seed's next candidate, as Megabonk players reset for a better map ([M2], [P3]).
- **Sizes.** P0 has 8 templates and 1 skeleton; P1 has 16 templates and 2 skeletons.

A library such as DeBroglie (an MIT-licensed C# wave-function-collapse library) could do the stitching. Our rules are
simple enough to write in-house in a day, and an in-house generator stays one pure body shared with the simulation (the
stamina lesson, docs/21).

### 3.2 Size against the budgets

| | Foundry court today | World, P0 | World, P1 target |
|---|---|---|---|
| Walkable cells | ~1,500-1,650 (design u -32..14, v -18..18, plus landing and bridge) | ~2,900 (30 x 96 floor units) | ~4,300 (36 x 120) |
| Crossing the long axis at 2.5 u/s | 18-23 s | ~38 s | ~48 s |
| Screens of area (9 x ~27 band) | ~6-7 | ~12 | ~18 |
| Built at once | Whole region: 36-41 renderers, 161-172k vertices against the 174,000 ceiling (docs/48 3.3, docs/51 8) | Every chunk, sliced over frames; only chunks in view render | Same |

**Budgets for the world (targets, measured on the S23):**
- **In view:** at most **130k vertices**, at most **60 renderers**, and **110 draw calls** with 40 enemies standing.
  Today's foundry draws 50-98 (docs/41's baselines). The 174k ceiling has applied to the whole region because all of it
  was in memory and potentially on screen. In a chunked world the same discipline applies to the part in view.
- **Total world mesh:** at most **450k vertices**, about 15 MB at today's vertex format. Low-end phones are checked
  before P2.
- **Crowd:** `HordeRule.SceneCap` stays **40**, the S23 density probe's measured top (59.9 fps, p95 16.7 ms, GPU under
  4.2 ms; docs/41 and docs/21). The world does not raise it.
- **Frame:** p95 at most 20 ms, no frame over 35 ms in a steady fight (docs/41 step 1), and the portal transition at most
  100 ms. The floor-2 theme change once cost a 279 ms hitch (docs/41, 2026-11-12) and was fixed by preparing the dress
  during the previous floor in 1 ms slices. The sanctum is prepared the same way during the world's last minute.

### 3.3 Streaming and culling

- **Chunks are renderers.** Each 12 x 12 chunk is one mesh per material (paving, wall and trim, lava, props), merged at
  build time as `FoundryRegionView` merges its batches. Unity's frustum culling then drops off-screen chunks for free. The
  band shows 2-4 chunks across and 3-4 up.
- **Build early, show late.** Every chunk is built during the map choice or the previous sanctum, in time slices. P0
  measures whether a whole-world build fits a loading moment (likely under 0.3 s); lazy per-chunk builds are the
  fallback.
- **Effects** (embers, flames, the lava's glow) run only in chunks within 1.5 screens.
- **Actors are leashed.** A horde enemy more than 18 units of path from the hero retires (into the body pool, docs/41
  2026-11-11) and the director spawns ahead. The horde travels with the hero, so 40 bodies are always 40 near ones.
- **Two regions, one scene.** The world root and the foundry root coexist. The portal disables the world's renderers,
  moves the hero to the arrival bridge and frames the camera. No scene load, so the run's state, pools and telemetry
  carry across.

### 3.4 What the code can reuse, and what is new

**The seam that must come first.** `FoundryMap` appears on 225 lines of 39 scripts and in 65 test files. Movement
(`HeroMotion.Move` → `FoundryMap.Move` when `geometry.IsFoundry`), spawn placement (`PlacePack`, `HordeSpot`,
`PlaceAtGate`), pursuit (`FoundryPursuit`, a fixed visibility graph), the camera's lift (`FoundrySurface.Lift`) and the
simulation's foundry run all call it as a static class. The world needs a small **`IFloorMap`** interface:
- walkable, segment-clear and move-with-slide (`Contains`, `Segment`, `Move`);
- spawn ring and pack placement (`HordeSpot`, `PlacePack`);
- pursuit target (`Pursue`);
- height (`Height`, for drawing).

`FoundryMap` implements it unchanged. Most of the 225 references are the region's rendering, which stays foundry-only.
The seam touches roughly ten runtime files and the simulation's foundry partial (15 direct calls).

| Reused as it is, or nearly | New |
|---|---|
| `HordeDirector` / `HordeRule` (pure). Its state comes from the world clock rather than halls cleared; spawn spots come from the map interface. | `IFloorMap` seam |
| `HordeClock` growth, rescaled to the world's 180 s | `WorldMap` (pure): cells, terrace heights, one-way ledges, ramps, gaps, pads, `Move`, flow-field pursuit, shared by scene and simulation as one non-inlined body |
| `SwarmDirector` / `SwarmRule` (the Bell's swarm), `ExitRule` (the stair's hold) | World generator: skeleton, templates, sockets, validator, seeded placement |
| `EliteRule`, `EnrageRule` (Keepers, edicts), `PackLayout`/`PackMotion` (escorts) | `WorldRegionView`: chunked meshes, greybox first |
| `ChestSpawner`, `ChestRule` (spots from the world), `AltarService`/`AltarRule` (greed) | Airborne state: pad arc, auto-hop, ledge drop, shadow |
| `UpgradeService` offers (shrine rewards), `RarityTable` | `ShrineRule` (pure) + its view; Keeper patrol and aggro |
| `HeroDash` (dashes cross gaps) | Edge-arrow manager (generalising `ExitSealView`'s arrow) |
| `EncounterController` rooms for the sanctum (`RoomKind` Forge, Elite, Boss), the gate (`OpenGateExit`, `GateBossArrived`), body pools | A floor phase before the rooms (world → Bell → portal) in the floor flow |
| `FloorScaling`, `ClimbScaling`, the tiers, `RunTelemetry`, `FloorClockView`, `ContactShadowView`, the theme system (`FloorTheme`, `ArenaView.ApplyFloorTheme`), `PlatformMaterialSet` materials, the paving, wall and coping builders (to be lifted out of `FoundryRegionView`) | Simulation partial for the world, the pilots' world habits per cohort, the batch's world tables |
| `PitEruptionRule` (slag vents), `PollenRule`, `ThornlingRule` (Gardens hazards) | Telemetry events: `world_start`, `shrine_start` / `shrine_capture` / `shrine_abandon`, `keeper_spawn` / `keeper_killed`, `pad_launch`, `bell`, `stair_enter` (time, level, health) |

## 4. Pacing

### 4.1 Where the minutes go (Forged, careful pilot, P50 targets)

| Phase | Floor 1 | Floor 2 | Floor 3 | Today, for comparison |
|---|---|---|---|---|
| World, to the Bell | 2:45 | 3:00 | 3:00 | Halls and horde until the 3:30 gate |
| Swarm escape to the stair | 0:15-0:30 | 0:15-0:30 | 0:15-0:30 | - |
| Portal and the sanctum's walk | 0:10 | 0:10 | 0:10 | - |
| Antechamber mini-boss | - (P1: 0:25) | 0:25 | 0:25 | - |
| Forge | 0:10 | 0:10 | 0:10 | (a node on the walk) |
| Floor boss | 0:50-0:55 (Warden today 50-56 s) | 0:45-0:50 (Regent 46-49 s) | 0:50-1:00 (docs/48: 47-62 s) | same |
| **Floor** | **4:10-4:40** | **4:45-5:05** | **4:50-5:15** | 4:24-4:36 (floors 1-3, docs/41 and docs/48) |

The climb: 13:45-15:00, plus today's crown Final Swarm (~65 s) for **14:50-16:05**. That is at the top edge of
docs/47's 13-16 minutes, and a slow player's world-and-escape runs past it. Three ways to keep it inside 16, in order
of preference:
1. Floor 1's world at 2:45 (it is the teaching floor) and the floors' antechambers at 0:20.
2. Fold the crown swarm into floor 3's Bell: the last swarm *is* the Final Swarm, and the stair leads to the last boss
   and the victory.
3. Shorter worlds (2:30). This is the wrong lever: the world is the new content.

**Lengthening instead (Budget B, 17-19 minutes: worlds of 4:00).**
- For it: Megabonk's arcs are 10 minutes a stage. A 3-minute world may feel like a corridor, and the owner asked for
  "longer".
- Against it:
  - docs/40 chose 13-16 minutes as the mobile session, with extraction after floor 1 at 4-6 minutes as the short session.
  - docs/47 measured that longer floors add deaths, not fun.
  - A 19-minute run is a longer thermal soak, and the S23 has not played even the 10-minute climb (docs/41's open device
    rows).
  - Each extra minute needs more shrines, templates and Keepers.

My advice: build Budget A and give the world clock a lever. Decide on B only after the owner has played P0 (owner
question 2).

### 4.2 Density and the horde director in open ground

- **State by the clock, not by halls.** Today `HordeRule.StateFor(nodesCleared, bossOpen)` moves Ramp → Horde →
  Pressure as halls clear. In the world there are no halls, so the state follows the world clock: Ramp 0:00-0:30
  (target 4), Horde 0:30-1:30 (10), Pressure 1:30-3:00 (16). The clocked growth and the reactive share
  (`EffectiveTarget`) are unchanged, and the Bell hands over to the swarm (cap 40). The targets are today's; the timings
  are new.
- **Local surges.** A charging shrine adds +6 to the target near it. A Keeper's escort is a formation, at most 10. Two
  world events at 1:00 and 2:00 send a 5 s surge from one side, like Megabonk's swarm events at 4:00 and 7:00 ([W7]).
- **Spawn only where the hero can be reached.** Spawn spots are cells within 7-12 units of path distance (not straight
  line) on the hero's connected region, weighted 2:1 ahead of the hero's walking direction. A ledge never hides a spawn
  that cannot climb to the hero.
- **Experience.** The halls' authored waves paid full experience; the horde pays 20 % (`HordeRule.ExperienceShare`).
  Without the halls, the world's experience comes from the horde, shrines, the Keeper and chests. P0 tunes one lever (the
  world horde's experience share, or a shrine's experience) to hit power parity at the boss. It does not re-tune the
  whole economy.
- **Gold.** The halls' kills paid gold; the horde pays none. The Keeper's purse and the chests replace it, tuned so the
  checkpoint's "Extract with N gold" matches today's P50 on floor 1.
- **What a minute looks like (targets):**
  - 12-20 standing enemies in the world, 30-40 at the swarm;
  - combat uptime of at least 75 % (docs/40; 0.95 measured today);
  - no more than 20 % of world time with no enemy within 6 units;
  - one decision every 10-15 s (a shrine, chest, pad, altar or Keeper).

## 5. A phased plan with measurable gates

Estimates are in **agent-days**: one agent's working day including the Unity gate (EditMode, PlayMode and the explicit
suites) and a review, the unit docs/48 used for floor 3. Measured in order: the simulation first, then the Editor, then
the S23. A gate that fails stops the phase until the lead decides.

### P0a - the mechanics probe in today's court (6-8 agent-days) - recommended first

The question: are capture, a roaming Keeper, pads and the Bell-swarm-stair fun on our phone, before any world exists?
- On floor 1's court, the three halls are replaced by three Tithe Anvils at the district sites.
- One Keeper (the Grunt Captain at a mini-boss share, with an escort) patrols the north court and the workyard.
- Two **pad pairs leap the lava pit**: the court already has its gap over lava.
- The 3:00 Bell starts the swarm, and the gate becomes the stair, held to enter; the Warden waits beyond it as today.
- Floors 2 and 3 are unchanged. It is behind a development file (`development/outworks.txt`) and off by default.

| Work | Days |
|---|---|
| `ShrineRule` + view + offer frame | 1.5 |
| Keeper patrol and key | 1 |
| Pads, airborne arc, shadow | 1.5 |
| Bell → swarm → held gate | 1 |
| Simulation mirror + pilot habits + batch rows | 2 |
| Measurement | 1 |

**Gates:**
- **Simulation** (both heroes, every playing cohort, typical hands; exploration on any seeds):
  - floor 1 P50 4:00-4:45;
  - level at the Warden within +-2 of today's P50;
  - careful captures at least 2 of 3 shrines (P50);
  - Keeper killed in at least 60 % of careful runs;
  - swarm deaths at most 3 % of Bell entries in every cohort.
- **Editor:** `HordePacingMeasurement` on 6 seeds within 10 % of the simulation.
- **S23:** p95 at most 20 ms during the swarm at 40 enemies.
- **The owner:** three runs each of today's floor 1 and the probe. Which one do you want to play again? Did you always
  know where to go?

**Kill or continue.** If the owner prefers today's floor, or the probe is not fun in the court, a bigger world will not
fix it. Fold whatever was liked (shrines, Keeper, pads) into the halls and stop. If the probe wins, P0b.

### P0b - the greybox world for floor 1 (~21 agent-days after P0a; ~25 without it)

The world as section 1.2 describes it:
- one skeleton, 8 templates, ~2,900 cells;
- 3 shrines, 3-4 chests, the 2 greed altars, 4 pad pairs, ledges, ramps and a lava channel;
- one Keeper, the world clock (2:45-3:00, a lever), the Bell, the swarm and the stair;
- the portal into the sanctum (rooms: Forge, then the Warden).

Greybox means the region's existing paving, wall and coping materials on simple shapes, coloured markers for pads and
anvils, and the baked boiler and crane as landmarks. Not grey boxes: the owner must be able to judge it on the phone.

| Work | Days |
|---|---|
| `IFloorMap` seam (movement, placement, pursuit, camera, simulation) | 3 |
| `WorldMap` + flow field + tests | 3 |
| Generator + validator + seeds | 2.5 |
| Greybox `WorldRegionView` + camera look-ahead and zoom lever | 2.5 |
| Horde in the world (reachable spawns, leash, surges), Bell, stair, portal and sanctum flow | 3 |
| Edge arrows + stair beacon | 1 |
| Simulation world partial, world habits per cohort, world tables, scene/simulation parity tests | 4 |
| Telemetry, map choice and deeper-start integration | 1 |
| Measurement | 2 |
| Review and fixes | 3 |
| **Less what P0a already built** | -4 |

**Gates:**
- **Parity:** the world's movement, spawn and pursuit arithmetic is one body; scene and simulation agree exactly on the
  pure parts (the existing parity tests' standard).
- **Simulation** (exploration seeds, then **one confirmatory read on a fresh block after the last used**, from 11601 or
  later, as docs/56 rule 1 requires):
  - world P50 2:45-3:15 including the escape;
  - sanctum P50 60-80 s;
  - level and health at the Warden within today's P25-P75;
  - combat uptime at least 75 %, idle at most 20 %;
  - four-floor completion change per cohort inside docs/56's thresholds against shipped (at least -1.0 point, CI lower
    bound at least -1.5), with an A/A probe on the world first (section 7.3).
- **Editor:** `HordePacingMeasurement` plays the world on 6 seeds within 10 % of the simulation; the portal transition's
  frame is under 50 ms in the Editor.
- **S23:** p95 at most 20 ms, no frame over 35 ms in steady fights, portal at most 100 ms, at most 110 draw calls and
  130k vertices in view, memory +60 MB at most, and a 15-minute soak with fps P50 at least 55 in its last minute.
- **The owner:** the A/B again, now against the world. Can you find the stair, the shrines and the Keeper without
  help?

### P1 - art and depth for floor 1, the Foundry Outworks (~27 agent-days)

| Work | Days |
|---|---|
| A module kit of about 30 pieces: paving variants, ledge walls, ramps and stairs, lava channel edges, bellows vents, the Tithe Anvil, the stair, crates, braziers, pipes, rails, scrap; Blender-scripted as the baked boilers were, with AI textures and props where section 6 says | 8-10 |
| 16 templates, a second skeleton, 6-8 shrines with variants, a second Keeper, breakables, the vendor (if the owner says yes) | 5 |
| The full map with fog of war; polish of the arrows | 2 |
| Sanctum variety for floor 1: 3 layouts, 4 hazards, 4 edicts, the antechamber | 5 |
| Lore objects moved, the portal's effect, audio hooks | 2 |
| Re-baseline on a fresh block (docs/56), the S23 perf and thermal pass, the owner's review | 4 |

**Gates:** P0b's, plus 30 seeds of the generator reviewed for readability on captures, and the owner's art review per
module family.

### P2 - more biomes: the Vault Terraces and the Gloam Terraces (~33 agent-days, ~16-17 each)

Per biome:
- the kit re-skinned through the theme system (the ice and gloam materials, props swapped 1:1 as docs/49 and docs/51
  did): 3-4 days;
- 2-3 hazards: 3 days;
- 8-12 templates: 2 days;
- Keepers from docs/50 and docs/52: 2 days;
- sanctum variety: 3 days;
- balance: 2 days (docs/56: **3,200** fresh seeds for a floor-2 change, 1,200 for floor 3);
- perf: 1 day.

**Gates:** P0b's, per floor.

**Total:** P0a 7 + P0b 21 + P1 27 + P2 33 ≈ **88-90 agent-days**. Parallel agents shorten the calendar, but the owner's
plays and reviews are the real clock, and the release path (docs/59) and the owner's chosen first production focus,
animation and effects, compete for the same weeks. P0a is code and can run beside the animation work.

## 6. Where AI tools and asset packs would cut the work

**A technical fact that decides most of this.** Our environment is not a 3D scene under a perspective camera. It is
code-built meshes **projected flat** (`FoundrySurface.Project` returns x, y x 0.5 + height, z = 0), drawn in sorting
order under an orthographic camera. Characters are pre-rendered sprites. A 3D asset pack therefore **does not drop
in**. It has to be either:
- **(a) re-rendered in Blender** at our 2:1 angle into sprites or tiles, a pipeline the project already runs (Ilya's
  frames, the baked boilers and the native Blender structures in docs/23's foundry entries); or
- **(b) converted by script** into our projected mesh format: project each vertex, assign its depth band. This works for
  simple low-poly props.

Switching the world to a true 3D camera is the third road, and it is not recommended: sorting sprites against 3D depth,
lighting and the whole region would change.

| Where | What would help | What it saves | Caution |
|---|---|---|---|
| **Tileable textures** (paving, ledge faces, lava crust, ice, ash) | AI image generation with tiling (local Stable Diffusion/Flux-class models, or a hosted tool with an API), then hand repaint to the palette | High: the world is mostly textured flat ground, and textures are where our painted look lives | Seams, palette drift, licence of the model and its outputs |
| **Props** (anvils, crates, statues, braziers, the Keeper's lantern) | Image-to-3D generators (examples to evaluate: Meshy, Tripo, Rodin, Hunyuan3D, TRELLIS), then decimation and a Blender bake | Medium: props are a third of the kit | Topology, triangle counts, painted-style fit, licence terms (some open-weight models exclude regions) |
| **Modular pieces** (ledges, ramps, stairs) | Asset packs (examples to evaluate: Synty POLYGON dark fantasy and dungeon sets; CC0 kits from KayKit, Kenney, Quaternius), re-rendered or converted | Medium: but kit pieces must match our cell size and socket edges | Style mismatch (flat-shaded packs against our painted look); licence on re-rendering and modification |
| **Procedural tools** | WFC libraries (DeBroglie, MIT), Tessera or Dungeon Architect (Asset Store) | Low for us: our generator is small, and these tools mostly speed up placing 3D prefabs in the Editor, which we do not use | A second generator that the simulation cannot share |
| **Unity's own AI** | Unity AI (formerly Muse) for textures and sprites | Unknown | It may need a newer Unity than 6000.0; the brief must check |

**Honest sizing.** P0 is about 90 % code, simulation and measurement; AI tools and packs save almost nothing there. In
P1-P2, art (the kits, re-skins, lore objects and effects) is about a third of the 60 days, and tools could save a
third to a half of that: roughly **6-10 agent-days** of the ~90. The owner's question "can AI tools or assets handle it?" is answered: they help the art, but the work is mostly
design, code and balance, and that is the agent's part either way.

**Questions for the owner's research brief** (docs/research/2026-11-22_ai_tools_brief.md, section I):
1. Which stylised, low-poly, dark-fantasy modular packs (foundry/industrial, ice/mirror crypt, garden on ash) allow
   **re-rendering into sprites and modification** in a commercial mobile game? Price, polygon counts, texture sizes.
2. Do any packs ship with an **isometric or 2:1 orthographic** variant (pre-rendered tiles)? That would skip our
   conversion step.
3. Which AI tools make **seamless tileable** textures in a painted style, with an API or a local model? Can they match
   a given palette or reference (image-to-material)?
4. Which image-to-3D tools give **clean, low-triangle** props (under 1,500 triangles) good enough to bake? What do
   their licences say about commercial use and ownership, and do they allow use from Turkey?
5. What GPU do local models (Flux/SDXL-class texture models, Hunyuan3D, TRELLIS) need? The brief's GPU line is still
   blank.
6. Has any shipped mobile game used AI-generated environment art at scale? Did the stores react (Google Play's
   AI-content policy)?
7. Does Unity AI work in **Unity 6000.0 with the Built-in RP**, and what does it cost?
8. Megabonk's maps (the brief's last bullet): any developer interview or devlog on how the stages are generated? This
   proposal found none (section 9).
9. Is there a Unity tool for **isometric terraced tilemaps** (heights, ledges, ramps) that a script can drive, and is it
   worth it over our own generator?
10. The monthly cost of the smallest useful set for environment work alone, against the animation and effects set the
    owner chose first.

## 7. Risks

### 7.1 Readability on a tall screen (high)

- **The view is narrow.** 9 units across means a shrine 10 units to the side is off the screen. Arrows carry
  navigation, and too many arrows become noise. Mitigation: at most 4, prioritised; the tall-world rule; landmarks; the
  look-ahead.
- **Heights in a flat projection.** In a 2:1 view a terrace can read as floor paint, so the walkable edge is unclear.
  Mitigation:
  - Terraces only rise **up the screen** (north). Every ledge face then faces the camera, and no high terrace stands in
    front of an actor.
  - Ramps carry chevrons; ledges carry a bright lip.
  - The region's hero cutaway (`FoundryVisibilityView`) applies to tall props.
- **Telegraphs on busy ground.** docs/49 7's rules hold in every biome: telegraphs stay warm, pickups stay warm, and
  frost or ash covers under 15 % of a fighting lane.

### 7.2 Performance (medium)

- More ground on screen and more effects, plus the swarm at 40 enemies. Chunked culling and the actor leash bound it.
- The S23 has not yet played the 10-minute climb (docs/41's open device rows). A 15-minute climb with a world is a
  longer thermal soak (docs/48 3.3).
- Building the world at the start must not hitch (the 279 ms lesson).
- Mitigation: P0b's device gates come before any art, and the low-end check comes before P2.

### 7.3 Balance (high): what docs/56 and the simulation assume, and what must be re-modelled

The simulation and the ship rule are built around **rooms on one fixed map**:
- `DescentSimulation.Foundry` plays the halls, the horde, the gate and the finale on `FoundryMap`.
- Four pilot cohorts (careful, standing, first-card, clumsy) carry habits calibrated to the owner (docs/33).
- docs/56 judges a **new creature** by the change in four-floor completion against shipped, on fresh seeds, with an A/A
  probe for noise.

What must change:
1. **Geometry.** The simulation's map calls go through `IFloorMap`, and `WorldMap` is shared with the scene, one body.
2. **The pilots' routing.** This is the largest new modelling, and the least certain. Today the pilots walk to a known
   next hall. In a world they choose: the nearest uncaptured shrine weighted by the crowd, the Keeper if their damage
   allows, chests within reach, the stair after the Bell. Each cohort needs world habits:
   - careful: captures when the crowd is thin, fights the Keeper;
   - standing: captures and stands;
   - first-card: beelines;
   - clumsy: misses shrines and is slow on pads.

   Human routing varies more than human fighting. `PilotRealism.Typical` needs a routing part (for example, missing 30 %
   of off-screen shrines), checked against the owner's own telemetry from P0a.
3. **Economy.** The halls' authored waves paid full experience and gold; the world's sources replace them. The parity
   principle limits the tuning to one or two levers.
4. **The horde's state** follows the clock rather than halls cleared (section 4.2).
5. **The rule itself.** docs/56 is built for small changes ("a creature"). A new floor structure is not one. Proposed
   handling:
   - **(a)** an A/A probe on the world floor first, because seeded layouts add noise (expect a wider band than docs/56's
     floor-2 numbers);
   - **(b)** the confirmatory read against shipped on a fresh block, as in rule 1, with the parity principle making
     "no worse than today" a fair target;
   - **(c)** once the owner accepts the world, it becomes the new shipped baseline, and the tiers' matched-cohort read
     (docs/54 section 21) and the deeper starts' catch-up levels (docs/53) are re-read on it once.
6. **Seed blocks.** Seeds 1-2000 are tuning seeds (docs/56), and later blocks were spent on the tiers and the skills
   (docs/41, 2026-11-19: the next read starts at 11601). P0b's confirmation takes the next unused block.

### 7.4 Scope (high)

- About 90 agent-days and two new pure systems (map and generator), plus a routing model, arrive while the release path
  (docs/59) waits on SDKs, accounts and the owner's store forms, and while the owner's chosen focus is animation and
  effects.
- The risk is a half-built world that blocks the release.
- Mitigation:
  - P0a and P0b are behind a development file;
  - floors 2-3 keep the hall structure until P2;
  - each phase has a kill gate;
  - the release does not wait for P1 (owner question 8).

### 7.5 Smaller risks

- **The Keeper may become the new "same room".** Mitigate with two Keepers with different behaviours per floor.
- **Shrine cards may flood the build.** Six shrines are six extra picks. Watch the level-up cadence, and the
  `UpgradePool.WidthAt` assumptions.
- **Jump pads may trivialise the swarm.** Pads close at the Bell, or the swarm spawns at landing spots.

## 8. My honest opinion

**The direction is right; the size is the question.** The owner's complaint is accurate and measurable: three floors on
one map, three halls of waves per floor, the same walk. Splitting the floor into a build-up phase full of choices and a
**boss sanctum** fixes the rhythm, and it gives the bosses the stage they lack today, where the Warden walks out onto the
court the player has crossed four times. Turning the current rooms into the boss sequence is a good use of the region
that took two months to build.

**But Megabonk's world is not Megabonk's fun on our phone.** Its pull comes from a far-seeing 3D camera and movement
tech: slides, hops and jumps over hills ([W6], [G4], TheGamer's praise of its verticality [R1]). Its maps are also its
most criticised part: few, similar, portal hard to find ([R1], [R2], [P2]). On a 9-unit portrait view with one thumb,
distance is cost, not fun. What transfers is **decisions under pressure**:
- stand in this ring with the crowd closing;
- fight the Keeper now or later;
- leave early, or stay for the Bell.

So the world should be **medium** (2-3x the court, not 10x), **tall** (it climbs up the screen like the fiction), and
**dense** (a decision every 10-15 s), and the boss rooms should get the variety tables of section 2.7.

**The smallest version that proves the fun is P0a**: shrines, one Keeper, pads over the pit and the Bell-swarm-stair, in
today's court, in about a week of agent work. If the owner plays it and wants more of it, the world is worth building,
and P0b's greybox answers the remaining question: does more ground help, or only cost? If P0a does not beat today's
floor, a bigger world will not save it, and we will have spent a week, not a season.

## 9. Research: Megabonk's stages, and big worlds on a tall screen

Facts below come from a research pass on 2026-11-22. The official wiki's domain no longer resolves, so its pages were
read from Wayback snapshots. Reddit, IGN and RPS could not be opened. The store page's "randomly generated maps" and
v1.0.17's full map and shrine colour were checked directly against Steam on 2026-11-22. Player opinion is marked as
such. docs/40 sections 2.2-2.9 hold the earlier (Turkish) summary; where they differ, this section is newer.

### 9.1 Megabonk

- **Maps and stages.**
  - Three maps: Forest, Desert, Graveyard ([W1]). The pre-launch plan was two maps of three stages each ([S0]).
  - Tier 1 is one stage; higher tiers chain stages, and Tier 3 ends in a separate final-boss arena ([W1], [S3]).
  - A stage runs **10 minutes** (Graveyard 16) ([W1]).
  - Reported events: first miniboss 3:00, swarm events 4:00 and 7:00, second miniboss 8:00, **Final Swarm 10:00** ([W7],
    a fan site; docs/40 2.2 marks the schedule unverified).
- **Size.** There are no published numbers. A guide calls the arena "a large square" ([G2]). Players report half a level
  spent running along the boundaries looking for the portal ([P2], player). One says a lap along the wall plus a pass
  through the centre reveals the whole level ([P3], player). That suggests a stage crossed in well under a minute that
  cannot be fully explored at a casual pace.
- **Terrain and movement.**
  - Forest is slopes and random cliffs with ramps so nothing is unreachable. Desert is wide dunes with tornadoes that
    launch the player ([W6]).
  - Slide, jump and bunny-hop are confirmed. Sliding downhill speeds you up ([W6], [G5]).
  - Extra jumps come from shrines and items ([W2], [S8]). Fall damage exists ([W6], [S5]). Water and mud slow to 0.4x
    ([W6]).
  - Falling out of bounds returns you to the map ([S3]). Hiding in caves stopped spawns until it was patched ([S6]).
  - **No jump pads** were found in any source; tornadoes are the only launchers.
- **Shrines and interactables** ([W2], [W3], [W5], [W6], [G1]):

  | Interactable | How it works |
  |---|---|
  | Charge shrine | Stand in a bubble for "several seconds" (about 5 by one guide); leaving resets quickly; pick 1 of 3 stat boosts; a rare gold variant gives only Legendary. About 15 per map. |
  | Greed shrine | +5 % difficulty for better rewards |
  | Boss curse | An extra boss |
  | Challenge shrine | An elite pack for a chest |
  | Magnet shrine | Pulls all experience to the player |
  | Moai statues | A free item pick |
  | Shady Guy | A vendor |
  | Chests | Escalating gold price: 30, 72, 115, 159 ... 495 at the 10th |
  | Microwaves | Duplicate an item |
  | Pots | 40 small and 15 big a map |
- **The boss portal.**
  - A boss spawner (a stone archway) sits at a random spot each run and can be used before 10:00. Using it summons the
    stage boss; killing the boss turns it into the teleporter onward ([W6], [W7], [G1]).
  - Players mostly wait, since boss health scales with time ([P4], player).
  - After it enters minimap range, a red edge arrow tracks it ([G1], [P1]).
- **The Final Swarm.**
  - It starts at 0:00 on the timer, or about 10 s after the boss dies. It is an escalating ghost horde meant to end runs
    ([W6], [P4]).
  - New ghosts were added to stop 5-hour runs ([S4]). Since the Spooky Update, ghosts speed up after 20 minutes ([S6]).
- **Procedural or hand-built.**
  - The store page says "randomly generated maps" (checked; released 18 September 2025, [S1]).
  - Terrain and objects are random, with ramps guaranteeing reachability ([W6]). A fan mod's notes suggest tile- or
    prop-based assembly ([M3]). A seed console exists for the Graveyard crypt ([S6]).
  - **No developer statement on the algorithm was found.**
  - Players reset runs for a better layout, and a popular mod rates maps ([M2], [P3]).
- **Praise and criticism.**
  - TheGamer praised the 3D terrain and verticality (2025-10-03, via [R1]).
  - IGN faulted the "lack of changing scenery" (2025-10-06, via [R1]).
  - Lords of Gaming: Forest uses verticality well, Desert is flat, procedural variation is minor ([R2]).
  - Players: the portal is hard to find and "everything looks similar" ([P2], [P3]).
  - The developer answered with a full map with fog of war ([S5]), after a minimap lock option and a wider camera zoom
    setting ([S3]).

### 9.2 Big worlds on a portrait phone

- **Vampire Survivors (mobile)** plays in portrait or landscape ([VS2], from a search excerpt). Stages scroll one way or
  both (the Inlaid Library horizontally, Gallo Tower vertically; [VS3]). Its navigation aid is a relic: a map of pickups
  plus optional on-screen arrows with the item's icon ([VS1]).
- **Survivor.io** mixes infinite maps, closed arenas and **up-down scrolling** chapters with no sideways scroll ([SV1]).
  Naavik notes the "infinitely scrollable in one or two axes" levels and the zoomed-out view ([SV2]). No off-screen
  arrows were documented.
- **Archero** fits each room to one phone screen, so the player sees every enemy at once ([AR1]). This is the opposite
  answer to the same constraint, and close to what our sanctum already is.
- **Brotato (mobile)**: landscape, a bounded arena slightly larger than one screen ([BR1], search excerpt).

The lesson for us: on phones, big worlds are either one-axis scrollers or bounded arenas. Navigation aids are arrows to
objectives or a map on pause. Nobody found relies on an always-on minimap on a portrait screen. Our tall world, edge
arrows and pause map follow that grain.

### 9.3 Sources

Accessed 2026-11-22 unless noted. Steam news posts are under `https://store.steampowered.com/news/app/3405340/view/<id>`.

- [S0] Megabonk dev post "Megabonk News", 2025-07-31, id 1806698490695551
- [S1] Steam store page, https://store.steampowered.com/app/3405340/Megabonk/ (released 2025-09-18; checked)
- [S3] Patch #2 (v1.0.4 + v1.0.7), 2025-09-28, id 1811772772356053
- [S4] v1.0.12, 2025-10-08, id 1813041031176676
- [S5] v1.0.17, 2025-10-19, id 1813674389045096 (checked: "There is now a full map with fog of war"; "Charge shrines
  are now a different color on the minimap")
- [S6] Spooky Update, 2025-12-14, id 1818752592131516
- [S8] "HATS" v1.0.64, 2026-01-25, id 1822556746165683
- [W1] megabonk.wiki "World", Wayback 2026-05-29:
  http://web.archive.org/web/20260529061934/https://megabonk.wiki/wiki/World
- [W2] megabonk.wiki "Charge_Shrine", Wayback 2026-05-15 (web.archive.org, megabonk.wiki/wiki/Charge_Shrine)
- [W3] megabonk.wiki "Chest", Wayback 2026-02-17 (web.archive.org, megabonk.wiki/wiki/Chest)
- [W5] Fextralife, Shrines and Interactables, edited 2026-09-17:
  https://megabonk.wiki.fextralife.com/Megabonk_Shrines_and_Interactables
- [W6] NamuWiki "Megabonk" (English machine translation), modified 2026-11-16: https://en.namu.wiki/w/Megabonk
- [W7] megabonk.org timer guide, undated (fan site): https://megabonk.org/guides/mechanics/timer/
- [G1] VGTimes, statues and map icons, 2025-10-29:
  https://vgtimes.com/guides/136827-all-statues-in-megabonk-full-guide-and-map-icons-explained.html
- [G2] Gamepressure, "Can't find portal", 2025-09-26:
  https://www.gamepressure.com/newsroom/cant-find-portal-in-megabonk-youre-definitely-not-alone-but-there/z8868b
- [G3] Gamepressure, Chaos Tome and shrines, 2025-10-15:
  https://www.gamepressure.com/newsroom/tips-on-how-to-get-chaos-tome-in-megabonk-charging-all-the-shrine/zd8795
- [G4] Gamer Guides, movement guide, updated 2026-10-26: https://gamer-guides.com/megabonk/movement-guide/
- [G5] dtgre, movement guide, 2025-10-10:
  https://www.dtgre.com/2025/10/megabonk-advanced-movement-bunny-hop-sliding-guide.html
- [R1] Wikipedia "Megabonk" (citing IGN 2025-10-06 and TheGamer 2025-10-03): https://en.wikipedia.org/wiki/Megabonk
- [R2] Lords of Gaming review, 2025-10-25: https://lordsofgaming.net/2025/10/megabonk-review-a-bonk-tastic-3d-bullet-heaven
- [P1] Steam thread "So it is just single 10 min runs?", 2025-10-02 (player):
  https://steamcommunity.com/app/3405340/discussions/0/600791249529706223/
- [P2] Steam thread "Boss portal needs to be more apparent", 2025-10-08 (player):
  https://steamcommunity.com/app/3405340/discussions/0/600791791480942835/
- [P3] Steam thread "make boss portals easier to find man", 2025-10-12 (player):
  https://steamcommunity.com/app/3405340/discussions/0/600792156684244494/
- [P4] Steam thread "Final swarm before Final boss is dead?", 2025-12-26 (player):
  https://steamcommunity.com/app/3405340/discussions/0/687493125920415796/
- [M1] Nexus mod "Always Show Boss Arrow": https://www.nexusmods.com/megabonk/mods/13
- [M2] Thunderstore, BetterMinimap: https://thunderstore.io/c/megabonk/p/wafuruns/MegabonkBetterMinimap/
- [M3] Fan multiplayer mod README: https://github.com/DeliriumPulse/MegaBonk.Multiplayer
- [VS1] Vampire Survivors wiki, Milky Way Map: https://vampire-survivors.fandom.com/wiki/Milky_Way_Map
- [VS2] TouchArcade, Vampire Survivors mobile review, 2022-12-09 (search excerpt; the page returned 403):
  https://toucharcade.com/2022/12/09/vampire-survivors-mobile-review-controller-cloud-save-sync-dlc-unlock-iphone-ipad-pro/
- [VS3] Vampire Survivors wiki, Inlaid Library: https://vampire-survivors.fandom.com/wiki/Inlaid_Library
- [SV1] AppGamer, Survivor.io chapter 4, updated 2022-08-29: https://www.appgamer.com/survivorio/chapter-4-financial-center
- [SV2] Naavik, "Survivor.io: In Archero's Footsteps", 2022-10-20:
  https://naavik.co/deep-dives/survivorio-archeros-footsteps/
- [AR1] Game Developer, "Finding the fun: Archero, part 1", 2019-07-03:
  https://www.gamedeveloper.com/design/finding-the-fun-archero-part-1---gameplay
- [BR1] minireview, Brotato Premium, undated: https://minireview.io/arcade/brotato-premium

## 10. Questions for the owner

Short and plain; the lead translates them.

1. Should the world **replace** the three combat halls on each floor (my proposal), or come **in addition** to them?
2. Should the full climb stay at **13-16 minutes** (worlds of about 3 minutes)? Or is **18-20 minutes** OK if the world
   turns out to be the fun part?
3. Should the world have the **same shape every run with shuffled pieces** (my proposal), or be fully random like
   Megabonk?
4. Jumping: are **automatic jump pads and hops** enough, or do you want a **jump button**?
5. Each floor now ends its world with a swarm. Should we **keep the separate Final Swarm** at the top of the tower, or
   let the last floor's swarm be the final one?
6. May a strong player **open the stair early** (by killing the Keeper) and skip the swarm? Or must everyone survive the
   swarm?
7. A **vendor and paid chests** would spend the run's gold, so less gold to extract. Yes, or keep chests free?
8. **Before or after the first release?** I recommend: the small probe (P0a) now, the full world after release.
9. Are you willing to **buy asset packs**, and what monthly budget is OK for environment tools?
10. When P0a is ready, can you play **three runs of today's floor 1 and three of the probe** on the S23 and tell us
    which one you want to play again?
11. Are the names OK as working names: **Outworks** (the world), **Tithe Anvil** (the shrine), **Keeper**, **the Bell**,
    **Ascent Stair**, **sanctum**? Turkish names go through docs/60.

## 11. The owner's answers and the lead's decisions (2026-11-22)

**The owner:**
1. **When:** P0a now; the full world after the first release.
2. **Halls:** the world comes **in addition to** the three halls, not in place of them. The floor runs as world, then
   halls, then the sanctum's boss.
3. **Jumping:** automatic. Pads launch the hero and narrow gaps take an auto-hop; there is no new button.
4. **Length:** the climb stays **13-16 minutes**. It is revisited after P0a if the world proves to be the fun.

Answers 2 and 4 together mean the floor's time is shared. In P0a the world segment gets about 1:30-2:00, and the halls'
time is trimmed so a floor stays about 4:30-5:15. The split is set by measurement in the simulation and confirmed on the
S23, not guessed. Power parity at the boss (section 4) still holds: the hero meets the Warden at today's level and
health.

**The lead** (gameplay calls are delegated):
- **Q3, the shape:** each run is a seeded arrangement of hand-authored pieces. The shapes are the same and their order is
  shuffled, so the world stays readable and is never pure noise.
- **Q5, the Final Swarm** stays at the top of the tower.
- **Q6, yes:** a Keeper's key opens the Ascent Stair early and skips the Bell. Skill is rewarded.
- **Q7, chests are free in P0a.** A vendor and paid chests wait for P1.
- **Q9, asset purchases:** decided when the verified tools list (docs/research/2026-11-22_ai_tools_verified.md) is in.
  Buying is the owner's.
- **Q10, the A/B on the S23:** asked when P0a is ready.
- **Q11, the working names stand** (Outworks, Tithe Anvil, Keeper, the Bell, Ascent Stair, sanctum). Their Turkish goes
  into the table with the owner's glossary review.


## 12. P0a built and measured (2026-11-22)

P0a is built behind `development/outworks.txt` (any text but "off"), read in the Editor and development builds as the
other development files are (`DevelopmentStart.Outworks`). Without the file nothing changes: the shipped floor, a release
build and the default batch play as before (section 12.6's byte-for-byte check). Measured in the simulation on the
exploration seeds 1-200 only (docs/56 rule 1: a probe spends no confirmation block). Nothing here is the S23's yet.

### 12.1 What is built

The floor is the owner's answer 2: **the Outworks phase, then the three halls as today, then the Warden** - on floor 1 of
a run that begins there (a higher start and floors 2-3 are untouched).

| Piece | As built | Where |
|---|---|---|
| The phase | Begins instead of floor 1's first hall; the floor's horde walks the court from the first frame, its state by the phase's clock (Ramp to 0:20, Horde to 1:00, Pressure from 1:00 and through the Bell). **One floor clock**: the horde's clock starts with the phase and runs on through the halls, so the gate's 3:30 hold and the 5:00 overrun read the whole floor. | `OutworksRule`, `OutworksPhase` (pure), `EncounterController.Outworks.cs` |
| 3 Tithe Anvils | Rings of 2.2 at the workyard's south, the workyard's north and the east court's south (two across the pit from the start). 5 s inside the ring captures; moving inside it is allowed; outside it the charge drains at half the fill rate. A capture is a level-up's offer of three cards (`RunState.GrantBonusUpgrade`, the panel an altar opens), without the altar's danger. | `AnvilField`; `CombatSetup.OnAnvilCaptured` |
| The Keeper | At 1:00 the floor's first elite room's wave (the Grunt Captain, a Grunt, two Cinder Mites) walks on at the waypoint of its loop farthest from the hero; the Captain at 12 times its health, its experience x3 and gold x2. It patrols a six-waypoint loop round the pit (north court, workyard, south rim) at 0.7 of its pace, and turns on the hero within 7 units or once its pack is struck. It is an elite wave (the Daggers' share, the elite's loot). Its Captain's fall drops the **Stair Key**: the stair opens before the Bell. A Keeper still roaming at the Bell leaves the court. | `OutworksPhase.KeeperGoal`, `SpawnKeeper` |
| Jump pads | Two pairs leap the lava pit, off the walks round it (section 12.9): west to east at v 5 and back at v 11, south to north at u -13 and back at u -7, each launch 2.8-4.5 units off the pit's edge. **Standing still on a pad's centre** (radius 0.35 inside the drawn 0.7 ring) for 0.3 s launches the hero - a walk across it never does - along a fixed arc to a fixed landing 17-18.5 units off in 0.9 s; the centre fills as the beat runs; the body rises up to 1.2 above the ground track while the shadow, the camera and every rule keep the track (`CombatantView.Lift`); no blow lands mid-arc (`AirborneGuard` on the hero's health; the simulation's `HeroDamaged`); the walls and the pit do not stop it. Each landing stands outside every pad, and a pad launches again only once the hero has stood off it. Enemies never use a pad. No new button. | `HeroLeap`, `HeroMovementInput.Leap`, `HeroMotion.Carry` |
| The Bell | At 1:45 on the phase's clock (frozen through choices): the stair opens for everyone, a roaming Keeper leaves, and the swarm rises in the wave's pack - `SwarmDirector` over a ladder of the floor's horde roles (4, 6, then 8 at a time, a new wave every 12 s, refills after 2 s), as fodder as the horde is (its health and damage shares, the floor clock's growth) hardened by the swarm's ramp. | `BeginBell`, `SpawnBellEnemy` |
| The Ascent Stair | At the court's north-east (design 10, 12), near the screen's top. Held as the opened gate is (`ExitDwell`: standing still on it for 1 s, radius 1.3). Sealed until the key or the Bell; the exit seal draws it once open. Held, the phase ends and floor 1's first hall forms round the hero there; the halls, the forge, the elites and the gate follow as today. | `OutworksPhase.StepStair`, `LeaveOutworks`, `ExitSealView` |
| Edge arrows | At most four on the band between the HUD blocks: the stair first (the exit seal's own arrow once open), then the Keeper, then the anvils nearest first, each labelled. Each label stands on its arrow's inner side, whole inside the safe area; an arrow pointing the way an earlier one does under the same name merges into it, and a different name that way stacks its label beside the earlier one's (section 12.9). A lantern ring marks the Keeper. | `OutworksView`, `EdgeLabel` |
| The clock | The HUD's floor clock counts to the Bell ("Bell in 1:05") and then says it rang; the floor's own clock takes over from the first hall. | `FloorClockView` |
| Text | Nine `outworks.*` rows through `Loc`, Turkish beside each (docs/60's rules: capitals keep their dots, no suffix on a placeholder, the short MERDİVEN on screen); the working names added to docs/60's glossary for the owner's review. | `Strings.txt` |
| The halls' trim | Knobs read instead of Floor_EmberHalls' own only while the switch is on (`OutworksRule.TrimmedHallSurges`, `TrimmedExtraElites`, `TrimmedGateSeconds`) - set to the asset's own values by the measurement below (section 12.5). | `EncounterController.HallSurgesOn` and kin |
| The parity lever | While the phase runs a horde enemy (the Bell's too) pays 40 % of its authored experience instead of 20 % (`OutworksRule.HordeExperienceShare`) - the doc's "one lever" (section 4.2). | `OutworksRule.HordeExperience` |
| The autopilot | `OutworksPilot` (pure, one body for the scene's `DevelopmentAutopilot` and the simulation): leaving - after the Bell, or with the key once done - the stair before anything else; otherwise after the kite the Keeper (when near, or the anvils done) or the nearest anvil, by a pad when the pad saves at least 3 units of walk, the drops beside the way; with nothing to go for, the altars, the drops in sight, else hold ground (never the pit's lure). A pad is walked onto and stood on (`OutworksPilot.PadStop`, inside its centre) for its beat. Per cohort: careful hunts and uses pads and leaves with the key once its anvils are done; standing never hunts; first-card leaves with the key at once; clumsy and auto-farmer miss anvils farther than 16 units, never take a pad and never hunt. A realistic pilot (typical hands) notices an off-screen anvil with 70 % chance until it comes within 9 units (section 7.3's routing part). | `OutworksPilot` |
| The simulation | `DescentSimulation.Outworks.cs`: the same frames, off by default (`FoundryLevers.Outworks`), with research levers for the Bell's time, the Keeper's health, experience and time, and the parity lever. `OutworksResearch` (explicit) measures it against the shipped climb. | Tests/Support, Tools/CombatChecks |
| The Editor's measurement | `TestResults/horde-outworks.txt` has `HordePacingMeasurement` play the probe; its header's mode reads "outworks" so the batch report never takes it for the shipped floor. | `HordePacingMeasurement` |

### 12.2 How it was measured

`OutworksResearch.TheOutworksProbe` (Tools/CombatChecks, explicit; it refuses a seed past 2000) plays every variant and
the shipped climb on the same seeds: **seeds 1-200**, both heroes, all five cohorts, typical hands
(`PilotRealism.Typical`), four floors (floors 1-3 and the Final Swarm), 12,000 runs of the shipped climb and five
variants in 23 minutes. On the built code the same table comes from:

    FOUNDRY_SEEDS=200 FOUNDRY_FLOORS=4 OUTWORKS_VARIANTS=p0a,p0a-xp30,p0a-xp50,p0a-keeper45,p0a-trim \
      dotnet test Tools/CombatChecks -c Release -p:NuGetAudit=false --filter "FullyQualifiedName~OutworksResearch.TheOutworksProbe"

It writes `outworks-p0a.md` and a row a run in `outworks-p0a.csv` into `FOUNDRY_OUT` (TestResults by default); a
one-seed trace is `OutworksResearch.OneOutworksRunTraced`. The switch as built is `p0a` (`DescentSimulation.OutworksLevers`).
The measurement was run before the Keeper's time moved - the Keeper at 0:45 as `p0a`, at 1:00 as the lever
`p0a-keeper60`; 1:00 was then made the rule, and `p0a` on the built code was checked to reproduce that lever's rows
exactly (100 of 100 runs, seeds 1-25, careful and clumsy, every column; the shipped rows likewise). Below, "P0a" is the
built switch. The playing cohorts are careful, standing, first-card and clumsy; the auto-farmer is reported but, as in
docs/48, is not one.

### 12.3 The split: the phase comes out of the gate's wait

| Hero | Cohort | Outworks P10 / P50 / P90 | Floor 1 P50: shipped / P0a | Climb P50 (P90): shipped / P0a |
|---|---|---|---|---|
| Vanguard | careful | 1:24 / 1:32 / 1:38 | 4:36 / 4:34 | 14:31 (15:02) / 14:34 (15:07) |
| Vanguard | standing | 1:38 / 1:48 / 1:52 | 4:25 / 4:23 | 13:57 (14:22) / 13:57 (14:16) |
| Vanguard | first-card | 1:25 / 1:32 / 1:40 | 4:33 / 4:31 | 14:16 (14:45) / 14:17 (14:40) |
| Vanguard | clumsy | 1:34 / 1:52 / 1:54 | 4:29 / 4:26 | 14:18 (14:43) / 14:10 (14:40) |
| Vanguard | auto-farmer | 1:36 / 1:51 / 1:54 | 4:12 / 4:07 | 13:17 (13:25) / 13:21 (13:55) |
| Ilya | careful | 1:24 / 1:31 / 1:35 | 4:31 / 4:30 | 14:17 (14:47) / 14:20 (14:48) |
| Ilya | standing | 1:39 / 1:48 / 1:52 | 4:22 / 4:20 | 13:51 (14:11) / 13:47 (14:06) |
| Ilya | first-card | 1:24 / 1:30 / 1:36 | 4:29 / 4:26 | 14:10 (14:27) / 14:06 (14:25) |
| Ilya | clumsy | 1:35 / 1:51 / 1:54 | 4:31 / 4:28 | 14:24 (15:01) / 14:17 (14:53) |
| Ilya | auto-farmer | 1:37 / 1:51 / 1:54 | 4:08 / 4:05 | 13:23 (13:32) / 13:24 (13:36) |

Outworks: from the floor's first frame to the stair held. Floor 1: the clears' active seconds. Climb: the four floors'
active seconds over the runs that completed it.

**The finding.** The owner's budget (1:30-2:00 for the world, the climb kept at 13-16 minutes) is met **without taking
anything from the halls**. Today the careful pilot clears floor 1's halls in about 1:40 and then waits about 1:53 for the
gate's 3:30 (docs/41; the default batch's gate row). With one floor clock, the phase spends that wait: the Warden walks out
at the clock's **3:30 in every variant, cohort and hero (P50; P90 3:30, the clumsy Vanguard's up to 3:37)**, floor 1 stays
**4:05-4:34** (P0a's gate: 4:00-4:45), and the climb stays **13:21-14:34 P50, 15:07 at its slowest P90**. The phase is
**1:30-1:52 P50** for every playing cohort: the careful and first-card pilots leave by the key at about 1:31, the
others mostly hear the Bell at 1:45.

The Keeper's time was the one lever the split needed: at 0:45 the careful pilot killed it by 1:05 and left at 1:13-1:15,
under the band; at 1:00 it falls at 1:21-1:22 and the phase lands at 1:31-1:32, with the same level at the Warden and no
more deaths (the careful Vanguard cleared floor 1 on 192 seeds at 1:00 against 189 at 0:45).

### 12.4 Power parity at the Warden, and completion

| Hero | Cohort | Level at the Warden P50 (P25-P75): shipped / P0a | Health share at the Warden P50: shipped band / P0a | Floor 1 cleared: shipped / P0a | Climb completed: shipped / P0a |
|---|---|---|---|---|---|
| Vanguard | careful | 25 (25-25) / 24 (23-25) | 0.80-0.92 / 0.85 | 195 / 192 | 190 / 191 |
| Vanguard | standing | 25 (25-26) / 25 (24-26) | 0.79-0.93 / 0.93 | 187 / 187 | 164 / 181 |
| Vanguard | first-card | 25 (25-26) / 24 (24-25) | 0.72-0.88 / 0.75 | 182 / 182 | 172 / 177 |
| Vanguard | clumsy | 25 (25-26) / 27 (26-27) | 0.46-0.71 / 0.75 | 78 / 94 | 61 / 80 |
| Vanguard | auto-farmer | 25 (25-25) / 28 (27-28) | 0.45-0.73 / 0.79 | 10 / 60 | 5 / 10 |
| Ilya | careful | 25 (25-26) / 24 (23-25) | 0.77-0.91 / 0.84 | 198 / 194 | 194 / 189 |
| Ilya | standing | 25 (25-26) / 25 (25-26) | 0.79-0.93 / 0.93 | 183 / 185 | 159 / 174 |
| Ilya | first-card | 25 (25-26) / 24 (24-25) | 0.72-0.88 / 0.74 | 198 / 192 | 184 / 186 |
| Ilya | clumsy | 25 (25-26) / 27 (26-27) | 0.45-0.71 / 0.73 | 131 / 163 | 99 / 141 |
| Ilya | auto-farmer | 25 (25-26) / 28 (27-28) | 0.51-0.77 / 0.80 | 29 / 58 | 6 / 11 |

Of 200 seeds. The Warden's fight stays where it was: P50 38-50 s for the playing cohorts against 39-51 s shipped.

- **The parity lever.** Without it (the phase's horde at the shipped 20 %), the hero met the Warden two to three levels
  down (22-23 against 25, an earlier 100-seed run of the same variants): the phase's thin opening farms less than the
  gate's wait at Pressure did. At 30 % the careful pilot was at 23 (the gate's edge), at 50 % the clumsy at 28. **40 %**
  puts every playing cohort within the gate's +-2: the careful and first-card at 24, the standing at 25, the clumsy at 27.
- **Not neutral for the weak.** The pilots who stay the whole phase (standing, clumsy, auto-farmer) farm more of it and
  take the anvils' three extra picks: the clumsy cohort meets the Warden two levels up and healthier, and clears floor 1
  on 16-32 more seeds of 200 and the climb on 19-42 more; the standing cohort's climb gains 15-17. The careful and
  first-card pilots, who fight the Keeper and leave the phase early with 50-57 % of their health, clear floor 1 on 0-6
  fewer seeds.
  docs/56 would flag the clumsy cohort's change for a creature; for a probe it is reported, not tuned: the A/B decides
  whether the world stays, and P0b re-reads parity on the world itself.
- **The phase's own numbers** (P0a, seeds 1-200): the careful pilot captures 2 anvils P50 (2.3 mean), kills the Keeper
  in 200 of 200 runs at 1:21-1:22 and leaves by the key in 197-199, takes 2-3 pads, and holds the stair at level 9 with
  56-57 % of its health. The first-card pilot kills it in 192 and 200, the standing pilot (which never hunts) in 95 and
  104 - the Keeper's loop finds it - and the clumsy in 49 and 56. The Bell rang in 910 runs of 2,000 and killed **no
  one**: its escape takes 1-7 s P50 and 7-18 s P90 in today's court, where the stair is never far. Deaths in the phase:
  11 clumsy Vanguards (the Keeper's Captain 5, the horde 6), 4 first-card Vanguards (the horde) and 10 auto-farmer
  Vanguards; none of Ilya's.

### 12.5 The proposed hall trim: none of the halls' content

The measured answer to "the halls' time is trimmed to fit" (section 11) is that the gate's wait is the trim: the phase
takes 1:30-1:52 out of the 1:40-1:53 the floor already waited, and the halls, their surges and the extra elite are
untouched. A real trim was measured and refused: one surge a hall and no extra elite (`p0a-trim`, measured beside the
Keeper at 0:45) moved floor 1's P50 by -1 to +2 s and the climb's by -3 to +14 s, and cost a level at the Warden (23
against 24 for the careful pilot) for health it did not need. The switch's knobs
(`OutworksRule.TrimmedHallSurges` 2, `TrimmedExtraElites` 1, `TrimmedGateSeconds` 210, read only on an Outworks run's
floor 1) therefore stand at Floor_EmberHalls' own values. If the owner's floor on the S23 runs past 5:15 - a human who
spends longer in the phase or the halls than the simulation's typical hands - the first knob to move is the extra elite
(`TrimmedExtraElites` 0), then one surge.

### 12.6 The gates

| Gate (section 5, P0a) | Target | Measured | Status |
|---|---|---|---|
| Floor 1 P50 | 4:00-4:45, both heroes, every playing cohort | 4:20-4:34 (auto-farmer 4:05-4:07) | met |
| Level at the Warden | within +-2 of today's P50 | careful 24, standing 25, first-card 24, clumsy 27 against 25 (auto-farmer 28) | met (clumsy at the edge) |
| Careful anvils | at least 2 of 3, P50 | 2 (mean 2.3) | met |
| Keeper killed | at least 60 % of careful runs | 100 % | met |
| Swarm deaths | at most 3 % of the Bell's entries, every cohort | 0 of 910 | met |
| The climb | 13-16 minutes (section 11) | 13:21-14:34 P50, 15:07 slowest P90 | met |
| Power parity, health | today's P25-P75 at the Warden | careful and first-card inside; standing at the top edge (0.93); clumsy above (0.73-0.75 against 0.71) | partly |
| The default batch unchanged | byte-identical with the switch off | FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_REALISM=typical FOUNDRY_FLOORS=2 on the base (lead-integrate-13) and on P0a: foundry-batch.csv, foundry-batch-floor2.csv, foundry-cohorts-floor2.csv, foundry-gate.md and foundry-cohorts.md identical; foundry-batch.md differs only in its date line and wall seconds | met |
| Editor | `HordePacingMeasurement` on 6 seeds within 10 % of the simulation | not run (no Unity in the agent's worktree): `TestResults/horde-outworks.txt` with any text plays it; compare with the research's CSV on seeds 11-66 | not measured |
| S23 | p95 at most 20 ms during the swarm at 40 enemies | the Bell's swarm stands beside the horde (cap 40 kept) | not measured |
| The owner | which floor to play again; always knew where to go | section 12.7 | open |

Unity's gates for the lead: EditMode `OutworksRuleTests` and `OutworksSimulationTests` (both also pass under Tools/CombatChecks,
24 tests since 12.9), PlayMode `OutworksFlowTests` (8 tests since 12.9; filter `OutworksFlowTests`), and the suites as usual.

### 12.7 What the owner's A/B on the S23 should look at

Install a development APK, then three runs of today's floor 1 (no file) and three with `development/outworks.txt`
holding "on" (`adb shell` into the profile's `development` folder, as for `autopilot.txt`). Forged, the Vanguard, floor 1
only (Extract at the checkpoint). Then:

1. **Which do you want to play again?** (docs/62 section 5's question; the kill-or-continue gate.)
2. **Did you always know where to go?** The anvils and the Keeper off the screen carry edge arrows; the stair always
   has one. Did an arrow ever point at something you could not find, or did you walk into the pit's rim by mistake?
3. **The pads.** Did a pad ever launch you when you did not mean it (since section 12.9 they stand off the walks round
   the pit and launch only a hero standing still on the centre for 0.3 s)? Was the beat too long in the Bell's swarm? Did
   the arc read - body up, shadow down - and did the landing feel safe?
4. **The anvils.** Is 5 s in a ring with the crowd closing a fight or a chore? Did you notice the charge draining when
   you stepped out?
5. **The Keeper.** Did you see it coming (the lantern, the arrow)? Was it a fight worth the key (about 20 s for the
   simulation's careful pilot at 12 times the Captain's health), or a wall? Did you leave early with the key?
6. **The Bell.** Did the swarm feel like an escape? The simulation says it is not yet a threat in today's small court
   (section 12.4): the stair is never far.
7. **The halls after it.** Did the floor feel longer (the simulation says no: the phase fills the gate's wait) and did
   the Warden feel as hard?
8. **Turkish.** One run in Turkish: do "ÖŞÜR ÖRSÜ", "MUHAFIZ", "MERDİVEN", "Çanın çalmasına 1:05" and "ÇAN ÇALDI -
   Merdivene ulaş" read, and do the working names stand (docs/60's glossary)?

What to pull afterwards (Tools/Device/Pull-DeviceEvidence.ps1): the frame times while the Bell's swarm stands with
the horde (P0a's S23 gate: p95 at most 20 ms at 40 enemies), and the run log's floor time beside the simulation's
(section 12.2). The run log's `outworks_step` events (anvil, keeper_killed, bell, stair_held, each with the phase's
clock, level and health) are the owner's own routing, to lay beside the pilots' habits (section 7.3).

### 12.8 Risks, gaps and questions

- **Unity has not run any of it.** The two .NET gates pass (Tools/CombatChecks: 1,106 tests passed; the Unity compile
  check: 0 errors); the EditMode and PlayMode suites, the capture (`pwsh Tools/ArtReview/Run-OutworksProof.ps1`, frames into
  `ArtDirection/2026-11-22/outworks-p0a/`, untracked) and the Editor's measurement are the lead's. The views are a greybox
  in line renderers and built-in-font labels, drawn as the altars are; the capture is their first look.
- **The body's lift** rides `CombatantView` (the body and the weapons it carries); if the Vanguard's puppet (PuppetAnimator)
  rewrites the body's position every frame, the arc will not show on it - only the capture will tell.
- **The Bell is not yet a threat** (0 deaths in 910 entries): the court is small and the stair never far. P0b's tall world
  puts the distance in; within P0a the levers are the stair's spot and the ladder (`OutworksRule.BellLadder`).
- **The pads stood on the paths round the pit** (P0a as first built): a walk round the rim could launch the hero unasked.
  Section 12.9 moved them off those walks and made the launch a deliberate stand on the centre.
- **The Keeper's Captain does the killing in the weak cohorts' phase deaths**, and the careful pilot leaves the phase with
  56-57 % of its health; a lower damage share with the same health is the lever if the owner finds it a wall.
- **The weak cohorts gain** (section 12.4): for a shipped change docs/56 would hold it; P0b re-reads parity on the world.
- **Not built in P0a:** chests in the phase (the halls' chests come with them), auto-hops over narrow gaps (the court has
  none; the pit is crossed by pads), one-way ledges, the full map, the sanctum. Floors 2 and 3 and a higher start keep
  today's floor. The telemetry logs `outworks_step` (anvil, keeper_killed, bell, stair_held with the phase's clock);
  `room_start` and the HUD's room line begin with the first hall.
- **Questions for the lead and the owner:**
  1. Is the gate's wait the trim the owner meant ("the halls' time is trimmed so a floor stays about 4:30-5:15")? The
     floor stays 4:20-4:34 with the halls whole.
  2. Keep the parity lever at 40 %, or take 30 % (the careful pilot at the gate's -2 edge, the weak cohorts' gain about a
     level smaller)?
  3. Should a pad close at the Bell (section 7.5's mitigation), or stay the escape it is? The lead's answer (12.9): open.
  4. The Turkish working names in docs/60's glossary (Öşür Örsü, Muhafız, Çan, Tırmanış Merdiveni, Dış Avlular) - and
     MERDİVEN as the short on-screen form?

### 12.9 The capture's polish (2026-11-23): the arrows' labels and the pads

The P0a captures (`ArtDirection/2026-11-22/outworks-p0a/`, 06 and 08) showed three faults; the lead's calls fixed them.

- **Labels stacked and cut.** Two anvils behind one edge drew two ANVIL labels on top of each other, and the open
  stair's ASCENT STAIR (the exit seal's label, centred under its arrow) ran off the screen's right edge. Now every edge
  label stands on its arrow's **inner side** and is clamped whole inside `Screen.safeArea` with a 3 % margin and inside
  the band between the HUD blocks (`EdgeLabel`, shared by `ExitSealView` and `OutworksView`); an arrow whose tip lies
  within 0.6 of an earlier one's under the same name **merges** into it (no slot taken), and a different name that way
  drops its chevron and **stacks** its label beside the earlier one's, the open stair's included. Still at most four.
- **Pads that launched unasked.** The pads stood 1.4 off the pit's rim, on the walks round it, and launched any hero
  whose feet came within 0.7. They now stand **2.8-4.5 off the pit's edge** (west to east at v 5, back at v 11; south to
  north at u -13, back at u -7), each landing 6 or more from its pair's other pad, and launch only a hero that has
  **stood still on the centre (0.35) for 0.3 s** - a walk across, at any pace, never does (`HeroLeap.TryLaunch`, one
  body for the scene and the simulation; the view fills the centre as the beat runs). The pilots stop on the centre
  (`OutworksPilot.PadStop`, 0.14). The pads stay open through the Bell.
- **Turkish.** The arrows already went through `Loc` (ÖRS, MUHAFIZ, MERDİVEN); `OutworksFlowTests` now checks it, and
  that every label stands inside the screen and on no other.

The default batch (switch off; FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_REALISM=typical
FOUNDRY_FLOORS=2) is byte-identical before and after: foundry-batch.csv, foundry-batch-floor2.csv,
foundry-cohorts-floor2.csv, foundry-gate.md and foundry-cohorts.md identical; foundry-batch.md only its date line and wall
seconds. `p0a` re-run on seeds 1-200 (both heroes, five cohorts, typical hands, four floors), before / after:

| Hero | Cohort | Outworks P50 | Floor 1 P50 | Climb P50 | Pads a run | Level at the Warden P50 | Keeper killed | Floor 1 cleared | Climb completed |
|---|---|---|---|---|---|---|---|---|---|
| Vanguard | careful | 1:32 / 1:26 | 4:34 / 4:34 | 14:34 / 14:28 | 2.9 / 1.4 | 24 / 24 | 200 / 199 | 192 / 193 | 191 / 191 |
| Vanguard | standing | 1:48 / 1:34 | 4:23 / 4:23 | 13:57 / 13:55 | 1.1 / 0.3 | 25 / 25 | 95 / 149 | 187 / 185 | 181 / 178 |
| Vanguard | first-card | 1:32 / 1:26 | 4:31 / 4:30 | 14:17 / 14:14 | 2.8 / 1.5 | 24 / 25 | 192 / 196 | 182 / 187 | 177 / 181 |
| Vanguard | clumsy | 1:52 / 1:52 | 4:26 / 4:26 | 14:10 / 14:10 | 0 / 0 | 27 / 27 | - | 94 / 94 | 80 / 80 |
| Ilya | careful | 1:31 / 1:26 | 4:30 / 4:29 | 14:20 / 14:19 | 2.8 / 1.5 | 24 / 25 | 200 / 200 | 194 / 197 | 189 / 196 |
| Ilya | standing | 1:48 / 1:35 | 4:20 / 4:21 | 13:47 / 13:50 | 1.1 / 0.3 | 25 / 25 | 104 / 140 | 185 / 191 | 174 / 182 |
| Ilya | first-card | 1:30 / 1:25 | 4:26 / 4:25 | 14:06 / 14:06 | 2.8 / 1.5 | 24 / 25 | 200 / 198 | 192 / 188 | 186 / 180 |
| Ilya | clumsy | 1:51 / 1:51 | 4:28 / 4:28 | 14:17 / 14:17 | 0 / 0 | 27 / 27 | - | 163 / 163 | 141 / 141 |

The floor and the climb barely move (P50 within -6 to +3 s; the Warden still walks out at the clock's 3:30, and the
level at the Warden holds or gains one). The phase itself gets shorter for the pilots who hunt and those who stand: the
careful and first-card pilots take half the pads (the old ones lay on their way and launched them anyway) and leave
about 5 s sooner with more health (0.57 to 0.69 at the stair, the careful Vanguard); the standing pilot, which never
hunts and took 1.1 unasked leaps a run, now stays where the Keeper's loop finds it - it kills the Keeper in 140-149 runs
of 200 against 95-104 and leaves by the key in 122-132 against 60-62, so its phase falls from 1:48 to 1:34-1:35 and the
Bell rings in 68-78 of its runs against 138-140. The auto-farmer is unchanged. Phase deaths: 25 before, 25 after (one
clumsy Ilya more, one first-card Vanguard fewer).
