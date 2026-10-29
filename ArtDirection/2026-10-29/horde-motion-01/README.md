# The horde's anticipation and deaths (art round 3)

2026-10-29. The Foundry's five horde enemies - the Grunt, the Cinder Mite, the Runner, the Tank and the Grunt Captain -
used to snap into their strike, so a blow could not be read coming, and all fell in the same two-frame 0.4 s remains.
Each now draws back in a short windup before every blow on a target in its reach, and dies in a way of its own. Both
are optional parts of `EnemyArtSet` (`IsValid` unchanged; a set without them behaves exactly as before). The Forge
Warden's telegraph and fall are untouched.

## What each one does

| Enemy | Windup (beat) | Death (length) |
|---|---|---|
| Grunt | the fist its strike throws, raised (0.20 s) | cracks with its seams flaring white, crumbles into its own painted rocks, which fall onto a heap and cool (0.60 s) |
| Cinder Mite | coiled back and low over its feet, seams and flame running hot (0.18 s) | swells white-hot and bursts: its rocks flung round the spot, embers rising off them (0.54 s) |
| Runner | sunk on its back leg, both claws cocked behind the shoulders (0.15 s) | struck upright, its legs give way and it pitches forward onto its face, its heart going out (0.60 s) |
| Tank | reared back, both fists up ahead of it (behind: flung wide at the shoulders) (0.25 s) | rocked back on its heels, it topples forward like a felled slab and lies with its grille dimming (0.66 s) |
| Grunt Captain | the Grunt's raised fist in ember red, helm and bell worn (0.22 s) | crumbles as the Grunt does while the bell's chain snaps and the bell drops and tips over, and the helm is knocked off onto the heap (0.66 s) |

The windup shows while `AttackController.StrikeDue` reports a blow due within the beat (never more than half the
blow's interval) on a target already in reach, turned toward it; the strike frame takes over when it lands. Deaths
play in the facing the enemy fell in, a drawing every 0.06 s, the last held while it fades and sinks (as the remains
did), from 48 pooled slots of their own (`CombatEffectsView.DeathSlots`).

## Method, per enemy (each keeps the one it was made with)

- **Grunt** - its own painted masters (`ArtDirection/2026-09-17`). The front windup is the front sheet's windup cell
  (`grunt-motion-01/front-sheet-v2.png`, the image-right fist its strike uses). The rear one is the only rear windup
  drawn with the right arm: the rejected `grunt-polish-01/rear-sheet-v3.png`, its alpha haze matted off (its figures
  are opaque to 247 of 255, the haze fainter) and its colours fitted by least squares to the v2 sheet the shipped rear
  frames come from (residual 4.6 of 255). Both are registered by their feet against the idle and sampled the way
  `BuildGruntAssets` sampled the shipped frames (a box mip chain of straight colours, trilinear): that reproduces the
  shipped idles to about one level of 255. The death cuts the idle itself into its rocks along its outlines and lava
  seams (a rock half hidden behind another is merged with it), and drops them under gravity onto a heap, each turned,
  outlined in the painted ink, its seams cooling; a few stone flakes kick up as the heap lands.
- **Cinder Mite** - its own painted masters (`mite-runtime-01`), sampled as `BuildMiteAssets` did. The windup squashes
  and leans the idle back about its floor root and heats its seams. The death swells it, then cuts its rocks (the
  flame removed: it goes up in the flash) and flings them out with a flash and rising embers.
- **Runner, Tank** - their Blender models (`BuildBlenderFoundryEnemies.py --motion`, poses keyed in `runner.blend` and
  `tank.blend` after the walk), packed exactly as their walk was (`build_foundry_enemies.process_render`).
- **Grunt Captain** - the kitbash of the Grunt's masters (`build_foundry_enemies.captain_frame`): the Grunt's windups
  scaled 1.15, turned red-hot and dressed in the rendered brass (placed by eye, `CAPTAIN_WINDUP_DRESS`); the death is
  the Grunt's crumble in ember red with the brass props on their own paths.

Windups share their enemy's walking canvas and pivot exactly (the pivot line is copied from the walk's own meta).
Death drawings are cropped to what they show plus the floor root, in whole ASTC blocks, the pivot on that root.

## Memory (ASTC 4x4, one byte a texel in whole blocks)

| Enemy | Windups (4) | Death drawings | Total |
|---|---|---|---|
| Grunt | 129,600 | 12: 204,688 | 334,288 |
| Cinder Mite | 21,760 | 14: 136,944 | 158,704 |
| Runner | 60,800 | 10: 110,448 | 171,248 |
| Tank | 139,072 | 10: 294,992 | 434,064 |
| Grunt Captain | 118,272 | 14: 308,528 | 426,800 |
| **All five** | **469,504** | **60: 1,055,600** | **1,525,104 (1.45 MiB)** |

`sprites.json` lists every texture with its GUID, size, pivot and bytes, each death's steps and timing.

## Reproduce

```powershell
$blender = 'E:/MobileGame/TestResults/tools/blender-4.5.9-windows-x64/blender.exe'
& $blender --background --python Tools/ArtReview/BuildBlenderFoundryEnemies.py -- --enemy runner --motion --samples 16
& $blender --background --python Tools/ArtReview/BuildBlenderFoundryEnemies.py -- --enemy tank --motion --samples 16
& $blender --background --python Tools/ArtReview/BuildBlenderFoundryEnemies.py -- --enemy captain-brass --samples 16
python Tools/ArtReview/build_horde_motion.py              # TGAs, metas, the sets' new parts, sprites.json (about 4 min)
python Tools/ArtReview/build_horde_motion.py --preview DIR # strips only, nothing written to Assets
python Tools/ArtReview/review_horde_motion.py --out DIR [--stone PHONE_CAPTURE.png]
```

A rerun writes every file byte for byte the same (checked: 166 files). The renders go to `TestResults` (ignored); the
Blender renders are pixel-identical run to run (only Blender's PNG metadata differs).

## Limits

- A blow whose cooldown had already run out when its target stepped into reach - most often the first blow of an
  enemy walking in - lands with no windup: presentation cannot hold a blow back.
- The windup holds through walking, as the strike frame always has; there is no walking windup.
- The painted masters were never drawn in pieces: a rock half hidden behind another tumbles as a crescent, and a cut
  between rocks shows its lava edge rather than painted stone. At the phone's scale these are a few pixels.
- The rear Grunt windup comes from a rejected, re-matted generation: its colours sit within about five levels of
  the shipped rear frames, its edge a little softer.
- The Tank's rear windup flings its arms wide rather than raising them, because raised ahead of it from behind they
  leave the walk's canvas and cover the health bar. The Runner's fall reads as a collapse more than a pitch at
  phone scale.
- Mirroring for the right-hand facings flips the light, as it does for every enemy.
