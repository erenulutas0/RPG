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
| `_windup` (new, optional) | front, rear: the hammer high over the helm, the visor and seams flaring |
| `_enragedFront`, `_enragedRear`, `_enragedWindup` (new, optional) | the same 14 poses in the second phase, wearing the first phase's flashes |
| `_death` | the knees go and the hammer hits the floor; kneeling, slumped, the slit dark and the hammer let go |
| `_visualTop` 1.642, `_shadowWidth` 1.12, `_walkCycleDistance` 1.075 | the bar's anchor (pauldron top + 0.08), the contact shadow, a heavy stride |

The strike's slam is aimed per facing so the head lands toward the screen's left in both: a forward slam in the rear
facing would land hidden behind the body.

Second phase: hotter colours in every ember (pale yellow seams, a white-hot slit, a glowing hammer face), a stronger
ember bounce, a wider bloom, and cracks that open only then - across both pauldrons, down the helm's cheek, out from the
keel across the chest, up the greaves.

## Sprites and memory

`Assets/_Project/Art/Characters/ForgeWarden/`: 44 PNGs, one sprite each, imported as the painted Grunt's TGAs (sprite,
single, 128 texels a unit, bilinear, no mipmaps, not readable, clamp, ASTC 4x4 on Android, Android max size 512 so no
frame is ever downscaled). Each pose's two phases and its flash share one rect, trimmed to their texels, symmetric
about the pivot (x 0.5: the bounds centre is the root, where the hit sparks and burn flames stand) and in whole 4x4
blocks; the pivot is the floor origin under the feet.

As ASTC 4x4 (8 bits a texel): **2.00 MB** in all, against the 3 MB budget (RGBA32 would be 8.00 MB) - first phase
0.64 MB, second phase 0.64 MB, flashes 0.64 MB, remains 0.09 MB. Frames run from 140x228 (rear idle) to 184x388
(front windup) and 280x224 (front strike).

## Unity

- `ForgeWarden.prefab` adds one modification: `_paintedArt` on the Tank's `EnemyLookView` (fileID 5758630739228300961)
  = `ForgeWardenArtSet`, as `CinderMite.prefab` overrides it on the Grunt's.
- `EnemyArtSet`: the optional `_windup` pair and `_enraged*` frames, `HasWindup`, `HasEnragedFrames`,
  `GetFrame(index, rear, enraged)`, `GetWindup(rear, enraged)`; `FlashOf` also finds the optional bodies. A partly
  filled optional part counts as absent. `IsValid` is unchanged.
- `EnemyLookView`: `WindUp(target)` / `Slam(target)` / `CancelWindup()` (internal), `IsWindingUp`, `ShowsEnraged`,
  `WindupFrame` (6). The windup holds through walking and the boss's own swings, facing the mark; the slam shows the
  strike frame for 0.35 s; the enrage (`EnrageBehaviour.Enraged`) swaps every pose for the second phase's. Enemies
  without the optional frames behave exactly as before.
- `WardenSlamView` drives it: on `Marked` the gate boss's look winds up toward the mark, on `Slammed` it slams, and a
  mark withdrawn without a blow cancels it. The slam's clock, timing and damage (`WardenRule`, `WardenSlamService`) are
  untouched.
- `CombatantView` skips its enrage tint (0.95, 0.35, 0.2) for a look that draws its own second phase: multiplied over
  the painted steel it turned the Warden a dark rust-brown that lost its silhouette on the lava.

## Reproduce

```powershell
& '<Blender 4.5.9>/blender.exe' --background --python Tools/ArtReview/BuildBlenderWarden.py   # about a minute
python Tools/ArtReview/PackWardenSprites.py                                                  # sprites, metas, set
python Tools/ArtReview/ReviewWardenSprites.py --out <dir> [--pixel <dir>]                     # GIFs, line-ups, mock
```

The raw 4x frames go to `TestResults/warden-render` (ignored). The render is deterministic (a rebuild reproduced all
44 sprites byte for byte) and re-packing keeps every GUID. `--pixel` takes raw dumps
of `EnemyArt.Draw(EnemyLook.Warden, pose)` (a few lines over the CombatChecks build) for the before/after; without it
that column is left out.

## Limits and next steps

- The surfaces are cleaner than the painted Grunt's boulders: one value per plane, little texture. A painted grain on
  the plates would bring it closer.
- One windup drawing per facing: if the Warden walks during the 1.1 s telegraph it glides with the hammer raised (at
  its fighting speed, one unit a second, about 1.1 units).
- The remains are the shared two-frame, 0.4 s fall of every painted enemy; a boss death worth the fight (a longer
  collapse, the door, the line) would need its own effect.
- Mirroring the left-looking drawings for the right moves the hammer and the shackle to the other hand, as the game
  does for every painted enemy.
- The windup's hammer stands about 3 units tall: at the top of the view it may pass under the top HUD for the
  telegraph.
