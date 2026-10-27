# The Forge Warden: one Blender model, pre-rendered to the painted roster's sprites

2026-10-27. The Foundry's gate boss (bible docs/44 sections 1.6 and 1.8) gets art that reads as the boss it is. Until
now `ForgeWarden.prefab` either drew the code-drawn pixel Warden (`EnemyArt.DrawWarden`, 49x56 texels at 32 a unit,
a silver block with a violet visor) or - what the latest import actually showed - the painted Grunt's frames: the
Warden is a variant of `Tank.prefab`, itself a variant of `Grunt.prefab`, and nothing overrode the Grunt's
`_paintedArt`, so every Grunt variant (Runner, Tank, Grunt Captain, Warden) inherited `SlagBruteArtSet` at a Grunt's
size. The Warden now overrides it with its own set; the other three are another painter's work in progress.

Like Ilya (`../arcanist-blender-01`), the Warden is a stylised 3D model built entirely by a script and rendered to
sprite frames, so every frame, both facings and both phases come from one model and cannot drift.

## Sources

Everything is authored in `Tools/ArtReview/BuildBlenderWarden.py`: lofted rings and primitives, every plate riding one
bone rigidly (plate armour does not bend), one toon shading node group, an inverted-hull contour, two-bone IK, and
every pose as a function. No generated imagery, no external textures, no paid service. The comparison floor in the
review images is cropped from `ArtDirection/2026-10-24/foundry-pit-eruption-01/pit-spill.png`; the molten patch is
synthesised in the pit's sampled colours.

- `forge-warden.blend` - the model, materials, rig and one action per pose (scrub them on the rig `Forge Warden`).
  The node group `Warden state` drives the furnace: Heat 0/1 for the phases, Flare for the windup, Life for death,
  Glow pass for the bloom render. The second phase's cracks are hidden (hide_render) in the saved first phase.
- `frames.json` - every packed sprite: file, GUID, size, pivot, and the memory figures.

## Design

Who it is: the first warden, forced to seal the workers' exit in the uprising; it cannot step away from the door.
Tragic, not evil: "Shift's not over." / "Stand clear of the mark." / "Door... was never locked for you."

- A towering foundry gatekeeper, about 1.79x a painted Grunt's figure (1.56 units to the pauldrons against the Grunt's
  0.88): a barrel chest on thick legs, the helm sunk between three-lame slab pauldrons, long arms with heavy fists at
  the hip, a little stooped, as if at the end of a shift that never ends.
- Blackened forge-iron plate (#3C3A45) whose planes turned to the key flash cool steel (#8B9DBE) - the steel is what
  separates it from the orange lava - with brass rivets, brow band, buckle and hammer bands.
- The furnace that drives it shows through every seam: the waist, the breastplate's lower edge and the joints that set
  its keel plate off, the knees, elbows and shoulders, a grate of three burning slots on the back, and one narrow
  burning slit across a heavy riveted bucket helm (no face, no eyes, three small breathing holes on the cheek).
- A long-hafted forge hammer (1.2 units, a 0.5-unit head with two hot striking faces), held like a sentry's halberd:
  the head down at the floor by the right foot, the fist round the haft at mid-length. Every pose but the windup keeps
  the hammer low, so the telegraph - the hammer hauled high over the helm - is the tallest silhouette it has.
- The tragic detail: on the left wrist (the side turned to the camera in both facings) the worn shackle that chained it
  to the door, and five links of chain hanging from its eye, the last one broken open.
- No text anywhere.

Values: on the dark stone floor (mean 66,61,69) the figure separates by its outline, the steel planes, a cool rim and
the seams' bloom; on lava it reads at once as a dark armoured mass. The ember bounce from below is kept faint so the
lower plates do not read as rust.

## Camera, light and render

As Ilya: orthographic, looking north and down 30 degrees (the 2:1 floor), heights modelled in screen units and the rig
stretched by 1/cos 30, the key light fixed to the screen's upper left. Two facings, both looking left (the game
mirrors them): front 36 degrees off the camera toward the left, rear 40 degrees off straight away. The model is built a
little large and the rig scaled 0.96. Cycles CPU, emission-only toon materials, rendered at 4x (1344x1664 for a
336x416-texel canvas) and box-filtered down in linear light. Plates are flat-shaded and chamfered by a bevel, so each
plane takes one painted value (with a small per-face shift) and every edge catches the key in a line.

Each pose renders twice: the beauty, and a glow pass in which only the burning parts show. `PackWardenSprites.py` lays
the glow pass's bloom (a tight and a wide halo, stronger in the second phase) over the downsampled frame after the
one-texel outline, so the visor and seams glow like the Grunt's lava cracks and never get a dark ring.

## Frames (EnemyArtSet)

| Field | Content |
|---|---|
| `_front`, `_rear` | idle, step A (right foot's contact), step B, strike, passing A, passing B - the Grunt's order (`HasPassingFrames`) |
| `_windup` (optional) | front, rear: the hammer high over the helm, the visor and seams flaring, standing in the walk's contact B (since 2026-10-27's second pass, below) |
| `_enragedFront`, `_enragedRear`, `_enragedWindup` (optional) | the same poses in the second phase, wearing the first phase's flashes |
| `_windupWalk`, `_enragedWindupWalk` (optional, second pass) | front then rear: the raised hammer carried at a walk in the walk's four quarters - contact A, passing A, contact B (the standing windup's own drawing), passing B - in each phase, the second wearing the first's flashes |
| `_fallFront`, `_fallRear`, `_fallSeconds` (optional, second pass) | the boss's own fall in each facing - struck, down on a knee, toppling, down - and how long each holds: 0.16, 0.44, 0.16, 0.56 s (1.32 s); the last two drawings are shared by both facings |
| `_death` | the two remains every painted enemy leaves: the fall's front kneel and the body down |
| `_visualTop` 1.642, `_shadowWidth` 1.12, `_walkCycleDistance` 1.075 | the bar's anchor (pauldron top + 0.08), the contact shadow, a heavy stride |

The strike's slam is aimed per facing so the head lands toward the screen's left in both: a forward slam in the rear
facing would land hidden behind the body.

Second phase: hotter colours in every ember (pale yellow seams, a white-hot slit, a glowing hammer face), a stronger
ember bounce, a wider bloom, and cracks that open only then - across both pauldrons, down the helm's cheek, out from the
keel across the chest, up the greaves.

## Second pass (2026-10-27): the walking windup and the boss's fall

**The glide.** The Warden keeps walking toward the hero through its 1.1 s telegraph (its movement is gameplay and stays
as it is), and with one windup drawing a facing it slid along the floor with the hammer up. Now the raised hammer is
carried at a walk: `windup_walk(phase)` is the walk's own legs at the walk's own phase - the same stride, so a planted
foot keeps its place on the floor from one drawing to the next as it does in the walk - under the windup's upper body,
the hammer riding the body's rise and fall, the feet coming down flatter under its weight. `EnemyLookView` steps it by
the distance travelled exactly as it steps the walk (`WalkingQuarter`, `_walkCycleDistance` 1.075), carrying on from
the walk's own quarter when the mark catches it mid-stride, and shows the standing windup while the Warden stands.
The standing windup is redrawn as the walk's contact B (the left foot forward, as the old braced stance had it): one
figure standing or stepping, and one drawing fewer.

**The death.** Every painted enemy falls in two drawings over 0.4 s. The Warden now falls in its own beat of 1.32 s,
in the facing it fell in, its second phase's furnace dying as it goes: the killing blow rocks it back on its heels in
a last white-hot flare, the knees buckling and the hammer's head slipping to the floor in a fist that will not let go;
it drops onto its right knee, slumped over itself as if still on guard - the held beat (0.44 s); it twists toward the
screen's left as it folds and topples; and it lies on its face at full length, rolled a little so the back's grate and
the helm turn to the camera, the fist still round the haft, the seams nearly out - and fades and settles as every
painted enemy's remains do. Both facings turn toward the screen's left as they fall (the way both drawings look), so
from the topple on they come down to the same pose on the floor and share those two drawings; the rear's hammer swings
round behind the body to get there. The fall is drawn in the second phase only (the enrage fires on the way down
through half health; only a single blow from above half could skip it), and each drawing carries the contact shadow
the live one leaves when it hides at the death - ContactShadowArt's own stepped ellipse at its own texels under the
root while the body still stands on it, and the same steps under whatever of the body lies on the floor.

## Sprites and memory

`Assets/_Project/Art/Characters/ForgeWarden/`: 66 PNGs, one sprite each, imported as the painted Grunt's TGAs (sprite,
single, 128 texels a unit, bilinear, no mipmaps, not readable, clamp, ASTC 4x4 on Android, Android max size 512 so no
frame is ever downscaled). Each body pose's two phases and its flash share one rect, trimmed to their texels,
symmetric about the pivot (x 0.5: the bounds centre is the root, where the hit sparks and burn flames stand) and in
whole 4x4 blocks; the pivot is the floor origin under the feet. The fall is drawn by the pooled remains, not the body,
so its drawings are trimmed tight wherever the body lies (rendered on a wider 544x384 canvas), the pivot still the floor
origin where the boss stood; they have no flash.

As ASTC 4x4 (8 bits a texel), against the 3.3 MB budget:

| Part | Textures | Bytes | MB |
|---|---|---|---|
| idle, walk, strike - first phase, second phase, flashes (unchanged) | 36 | 1,617,456 | 1.54 |
| standing windup, both phases and flash (redrawn as the walk's contact B) | 6 | 383,520 | 0.37 |
| walking windup: contact A, passing A, passing B, both phases and flashes (new) | 18 | 1,131,936 | 1.08 |
| the fall's own drawings: struck x2, the rear's kneel, the shared topple (new) | 4 | 204,272 | 0.19 |
| the two remains: the fall's front kneel and the shared body down (redrawn) | 2 | 96,384 | 0.09 |
| **in all** (before the second pass: 44, 2,097,088, 2.00) | **66** | **3,433,568** | **3.27** |

The savings that made room: the standing windup is one of the walk's quarters, the second phase wears the first's
flashes, both facings share the fall from the topple on, the two remains are the fall's own drawings, and the fall is
trimmed tight. A cheaper second phase (a recolour of the first) would not look the same: its cracks are new shapes.

## Unity

- `ForgeWarden.prefab` adds one modification: `_paintedArt` on the Tank's `EnemyLookView` (fileID 5758630739228300961)
  = `ForgeWardenArtSet`, as `CinderMite.prefab` overrides it on the Grunt's.
- `EnemyArtSet`: the optional `_windup` pair and `_enraged*` frames, `HasWindup`, `HasEnragedFrames`,
  `GetFrame(index, rear, enraged)`, `GetWindup(rear, enraged)`; `FlashOf` also finds the optional bodies. A partly
  filled optional part counts as absent. `IsValid` is unchanged. Second pass: `_windupWalk` / `_enragedWindupWalk`
  (`HasWalkingWindup(enraged)`, `GetWalkingWindup(quarter, rear, enraged)`, `WalkingQuarter(distance)`, the quarter
  `WalkingFrame` steps), and `_fallFront` / `_fallRear` / `_fallSeconds` (`HasFall`, `HasRearFall`, `GetFall(rear)`,
  `FallHold`, `FallSeconds`, `FallFrameAt`, and `FallTimeline(rear)`, the fall as frames `FallStep` 0.02 s apart). A
  second phase without its walking drawings stands to raise the hammer in that phase; a set without the rear's fall
  falls in the front's, mirrored.
- `EnemyLookView`: `WindUp(target)` / `Slam(target)` / `CancelWindup()` (internal), `IsWindingUp`, `ShowsEnraged`,
  `WindupFrame` (6), `WindupStep` (the walking windup's quarter, or -1 standing). The raised hammer holds through
  walking and the boss's own swings, facing the mark, stepped by the distance travelled in a set that draws it at a
  walk; the slam shows the strike frame for 0.35 s; the enrage (`EnrageBehaviour.Enraged`) swaps every pose for the
  second phase's, keeping the stride's quarter. Enemies without the optional frames behave exactly as before.
- `CombatEffectsView.OnEnemyDefeated`: a painted set with its own fall plays it (`FallTimeline` in the facing it fell
  in, flipped as it was, fading and settling as the remains do) from a slot reserved past the bursts (`FallSlots`), so
  a crowd's sparks, falls and coins never cut it short; every other painted enemy keeps its two remains from the shared
  pool. Nothing waits on it: the rewards, the open gate and the next wave come on the blow.
- `WardenSlamView` drives it: on `Marked` the gate boss's look winds up toward the mark, on `Slammed` it slams, and a
  mark withdrawn without a blow cancels it. The slam's clock, timing and damage (`WardenRule`, `WardenSlamService`) are
  untouched.
- `CombatantView` skips its enrage tint (0.95, 0.35, 0.2) for a look that draws its own second phase: multiplied over
  the painted steel it turned the Warden a dark rust-brown that lost its silhouette on the lava.

## Reproduce

```powershell
& '<Blender 4.5.9>/blender.exe' --background --python Tools/ArtReview/BuildBlenderWarden.py   # about two minutes
python Tools/ArtReview/PackWardenSprites.py                                                  # sprites, metas, set
python Tools/ArtReview/ReviewWardenSprites.py --out <dir> [--pixel <dir>] [--before <dir>]    # GIFs, line-ups, mock
```

The raw 4x frames go to `TestResults/warden-render` (ignored). The render is deterministic (a rebuild reproduces every
sprite byte for byte; the second pass left the first pass's 36 walk, idle and strike sprites untouched) and re-packing
keeps every GUID. `--pixel` takes raw dumps of `EnemyArt.Draw(EnemyLook.Warden, pose)` (a few lines over the
CombatChecks build) for the before/after; without it that column is left out. `--before` takes the Warden's sprites
from before the second pass (`git archive <commit> Assets/_Project/Art/Characters/ForgeWarden`, unpacked) for the
walking telegraph's and the death's before/after.

## Limits and next steps

- The surfaces are cleaner than the painted Grunt's boulders: one value per plane, little texture. A painted grain on
  the plates would bring it closer.
- The walking windup faces the mark, as the standing one does: if the Warden ever walks away from where its blow will
  land (the hero running past it mid-telegraph) its legs step the wrong way for that drawing. Each drawing holds for a
  quarter stride, about 0.27 s at its speed, so the root glides within it as it does in the walk.
- The fall's rear facing gets there by a turn: its hammer swings from the right of the kneeling body to the left of
  the toppling one in one drawing. Its knee and topple at 0.44 and 0.16 s are the beats that carry it.
- The fall is the second phase's (a one-blow kill from above half health would show its cracks opening at the death),
  plays from the reserved slot on scaled time (an upgrade choice opened by the boss's experience pauses it with the
  game), and the remains sort over the combatants as every painted enemy's do: a hero standing in front of the
  falling boss may be drawn under it for the beat.
- Mirroring the left-looking drawings for the right moves the hammer and the shackle to the other hand, as the game
  does for every painted enemy.
- The windup's hammer stands about 3 units tall: at the top of the view it may pass under the top HUD for the
  telegraph.
