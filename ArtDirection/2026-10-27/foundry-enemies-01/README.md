# The Runner, the Tank and the Grunt Captain join the painted Grunt's family

2026-10-27. Three of the Foundry's enemies (docs/44, The Foundry: Grunt, Cinder Mite, Runner, Grunt Captain, Tank) were
code-drawn pixel looks (`EnemyArt`, 32 texels a unit). They now have painted-quality sprites at the painted Grunt's
128 texels a unit, in its exact sprite contract (`EnemyArtSet`: front and rear facings looking left, six poses each
with a white flash silhouette, two remains frames), so the game needs no new code. Each prefab variant overrides
`_paintedArt` on the base `EnemyLookView`, as `CinderMite.prefab` does; nothing else in the prefabs changed.

Before this change the three variants inherited the Grunt's own `_paintedArt` from `Grunt.prefab`, so in the game
they actually walked as painted Grunts, not as their pixel looks. `pixel-before/` keeps the pixel looks (`EnemyArt.Draw`
output, which this change does not touch) for the before/after comparison.

## The method per enemy, and why

The brief offered a stylised Blender model or a 2D kitbash of the painted Grunt's frames. Each enemy got the one that
matches the Grunt better (the review's `approach-*` images show both):

- **Grunt Captain: kitbash of the painted Grunt's masters** (chosen). An elite Grunt should *be* the Grunt, a size up,
  and the kitbash keeps every painted rock. The rendered candidate (`--enemy captain`, the Grunt-proportioned golem with
  a modelled helm and bell) lost the painted rocks and proportions and read as a different, lankier creature.
- **Runner and Tank: rendered from Blender models** (chosen). Their silhouettes must differ from the Grunt's; squeezing
  or stretching the Grunt's frames to their proportions only gives a thin or a fat Grunt. The models share the
  painted Grunt's material instead: its value bands, violet basalt, ember seams, outlines and top-left light.

## Sources

No generated imagery and no external textures. Everything is authored by:

- `Tools/ArtReview/BuildBlenderFoundryEnemies.py` (Blender 4.5 LTS): the Runner, the Tank, the calibration golem
  (`--enemy grunt`), the rejected rendered Captain (`--enemy captain`) and the Captain's brass props
  (`--enemy captain-brass`). Rocks are convex hulls of points on rounded boxes with near-flat facets merged and edges
  chamfered; the lava is emissive geometry showing through the gaps; one toon node group shades everything (no lights);
  each part carries an inverted-hull contour, so every boulder is outlined as the painted ones are. A small FK/IK
  skeleton of empties places the rocks for each pose.
- `Tools/ArtReview/build_foundry_enemies.py`: the pack step. Blender frames are box-filtered from 4x in linear light
  with premultiplied alpha, their lava mask (a shader AOV) blooms an ember light over the stone round every seam (a wider
  second bloom from the Runner's heart), and a one-texel outline closes the silhouette. The Captain is built here from
  `ArtDirection/2026-09-17/grunt-polish-01/registration.json` and its master sheets.
- `Tools/ArtReview/review_foundry_enemies.py`: the review material, from the imported TGA pixels.

Files here: `runner.blend`, `tank.blend` (each pose keyed along the timeline with a named marker; scrub the markers),
`captain-brass.blend` (the helm and the bell), `sprites.json` (every texture, GUID, size, pivot, the set values and the
memory), `pixel-before/` (the pixel looks).

## Calibration against the painted Grunt

The Blender material was tuned on a Grunt-proportioned golem rendered beside the painted Grunt's own sprite. The
basalt's five values are the imported `CHR_Grunt_front-idle.tga`'s value bands (top tenth, 75-90th, 50-75th, 25-50th,
lowest quarter), pushed a little more violet after measuring saturation (the first renders were greyer: 0.26 against
the sprite's 0.32). The key light is fixed to the screen's upper left; the lava is orange with a yellow core only where
it faces out, and blooms about two texels over the stone.

## Designs

- **Runner** (fast, fragile): a lean sprinter of basalt shards on long reverse-jointed legs, leaning 30 degrees into
  its run even standing, the wedge head thrust out low, three fins swept back along the spine. An open ribcage holds a
  white-hot heart that outshines every seam, with cracks running from it; it glows through a seam between the shoulder
  blades from behind. Its facings turn 40 degrees from the camera (230 / 130), so the lean and the zig-zag legs read as
  a diagonal. Strike: a pounce, both claws thrown at the target. Light, long strides: a one-unit walk cycle.
- **Tank** (slow, heavy): a slab of basalt as wide as it is tall, the head sunk between two humps, arms as thick as
  the body ending in fists near the floor, short legs in a wide stance. Soot-black foundry iron is bolted over it:
  banded chest and back plates bent round the body, a furnace grille glowing in the belly plate, cast iron caps on
  the shoulders, an iron brow over the eyes. It rolls onto each planted foot and sinks into each contact. Strike: both
  fists hammered on the floor ahead. A 0.6-unit cycle at 0.9 units a second: a step every sixth of a second.
- **Grunt Captain** (the leader): the painted Grunt's fourteen frames registered exactly as the Grunt's sprites were
  (checked: silhouette overlap 0.97 to 0.995 with the shipped Grunt sprites), scaled by 1.15 about the floor root,
  the basalt turned from violet to a dark ember red in OKLab (every painted value step kept), the seams hotter and the
  stone round them lit red from within. Brass is laid over each frame: a crested captain's helm on the head, and the
  Warning Bell (docs/44: "Hold the gate until the bell") hanging on a chain across the chest; from behind, the chain
  across the neck. In death the helm lies fallen on the heap and the bell on the floor beside it. Its walk is the
  Grunt's own gait at a 0.8-unit cycle (the Grunt's 0.7 scaled).

## Camera, light and render (Blender)

As `BuildBlenderArcanist.py`: orthographic, looking north and down 30 degrees (the game's 2:1 floor), heights stretched
by 1/cos 30 so a design unit of height is a world unit on screen. The rig turns for the two facings; the camera and the
key light never move. Cycles CPU, 16 samples, emission-only materials. Rendered at 4x (512 texels a unit).

## The sprites

| Enemy | Canvas (texels) | Pivot | VisualTop | Shadow | Walk cycle | ASTC 4x4 |
|---|---|---|---|---|---|---|
| Runner | 100 x 152 | 50, 20 | 1.05 | 0.44 | 1.0 | 395,200 bytes |
| Tank | 212 x 164 | 106, 26 | 1.09 | 0.93 | 0.6 | 903,968 bytes |
| Grunt Captain | 168 x 176 | 84, 18 | 1.26 | 0.88 | 0.8 | 768,768 bytes |
| **Total** | 78 textures | | | | | **2,067,936 bytes (1.97 MB)**, budget 3 MB |

Each enemy's frames share one canvas (the union of all its frames plus two transparent texels, symmetric about the
pivot so the body's bounds centre on the feet, every side a whole number of ASTC blocks) and one pivot at the floor
root. Imports copy the Grunt's: a single sprite, 128 texels a unit, bilinear, clamp, no mipmaps, not readable, ASTC
4x4 on Android (max size 256, never downscaled). Frame order in each set: idle, contact A, contact B, strike, passing
A, passing B (the Grunt's; `HasPassingFrames`, `WalkingFrame` walks 1, 4, 2, 5). VisualTop clears every living pose by
the Grunt's margin (0.05 over its tallest); the shadow is the Grunt's 0.78 of the idle body's width.

Sizes against the pixel looks they replace (idle figure, front): Runner 0.56 x 0.91 units (pixel 0.59 x 0.94), Tank
1.19 x 1.01 (pixel 1.34 x 0.97), Captain 1.13 x 1.06, 1.21 from behind (pixel 1.03 x 1.22).

## Unity

`Runner.prefab`, `Tank.prefab` and `GruntCaptain.prefab` each override `_paintedArt` with their set. Two things this
does not change and the lead should know:

- `ForgeWarden.prefab` is a variant of `Tank.prefab`, so until it overrides `_paintedArt` itself the Forge Warden
  inherits the Tank's set (before this change it inherited the Grunt's).
- The three variants keep their old procedural attack nudges (`CombatantView._attackNudge`: Runner -0.3, Tank -0.12,
  Captain -0.25), which drop the body during the 0.14 s strike frame. The Grunt's was set to 0 when it got painted
  frames (`BuildGruntAssets.cs`); the same is probably right here.

## Reproduce

```powershell
$blender = 'E:/MobileGame/TestResults/tools/blender-4.5.9-windows-x64/blender.exe'
& $blender --background --python Tools/ArtReview/BuildBlenderFoundryEnemies.py -- --enemy runner --samples 16
& $blender --background --python Tools/ArtReview/BuildBlenderFoundryEnemies.py -- --enemy tank --samples 16
& $blender --background --python Tools/ArtReview/BuildBlenderFoundryEnemies.py -- --enemy captain-brass --samples 16
python Tools/ArtReview/build_foundry_enemies.py                  # TGAs, metas, the three sets, sprites.json
# The comparison candidates (not shipped): the calibration golem and the rendered Captain.
& $blender --background --python Tools/ArtReview/BuildBlenderFoundryEnemies.py -- --enemy captain --blend TestResults/captain.blend
python Tools/ArtReview/review_foundry_enemies.py --out <dir>     # line-ups, crowd, GIFs, before/after, approaches
```

Raw frames go to `TestResults/foundry-enemies-render` (ignored). Each Blender run takes seconds, and a rebuild from
nothing reproduces the TGAs byte for byte. Re-packing keeps every GUID, and refuses a frame its render canvas clips. `python Tools/ArtReview/build_foundry_enemies.py --captain-work <dir>` writes the Captain's registered, recoloured
frames with a placement grid, for moving the brass (`CAPTAIN_DRESS`).

## Limits and next steps

- The rendered stone is cleaner than the painted Grunt's: broad flat facets with a light brush texture where the
  painting has gradients and small fractures. At the phone's scale the three read as one family; zoomed in, the Runner
  and the Tank are visibly rendered.
- The Captain's brass is placed per frame by eye; it can sit a texel or two off the painted head from frame to frame.
  Its front frames stand lower than its rear ones because the painted Grunt's head sits lower in its front sheet.
- Mirroring for the right-facing directions flips the light, as it does for the Grunt.
- The Runner's strike frame is a pounce that reads best from the front; from behind the claws are half hidden.
- Four poses and a strike per facing, as the Grunt: no windup frames and no world-space foot locking.
