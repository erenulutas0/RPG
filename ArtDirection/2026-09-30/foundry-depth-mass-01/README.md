# Foundry depth ordering and landmark mass — 2026-09-30

## Why this package

After the floor package (`../foundry-floor-01/`), the next two gaps against the approved reference
(`ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`) at the gameplay camera were:

- **Depth.** Structures and actors only had sorting orders, so every enemy drew over every structure and the hero
  cutaway dissolved structures on all sides of the hero, including those behind him.
- **Mass.** Landmarks were small and thin against the court: square furnace towers, a thin parapet, a square shaft, no
  heavy foundations or chains.

## What changed

- **Real depth for tall structures** (`FoundryRegionDepth.cs`, `FoundryVisibilityView.cs`, `FoundryCutaway.shader`,
  `GroundedSorting.cs`, `FoundryRegionView.cs`). Structure vertices carry z = 50 + their ground-diagonal position;
  the cutaway shader gains a depth-only pass that writes that z. Actor sprites are moved to their feet depth only while
  the camera renders (Camera.onPreCull / onPostRender) and put back afterwards, so gameplay transforms are unchanged.
  An actor behind a structure is now hidden by it; an actor in front of it draws over it.
- **Cutaway only in front.** The fade now applies only to structures that are in front of the hero's depth and over
  his body, so nothing behind the hero is cut open.
- **Landmark mass** (`FoundryRegionMassing.cs`). Four round boilers replace the square furnace towers (the north pair
  tall with domes and flues, the south pair squat so the hero never appears to stand on a boiler roof at the gameplay
  camera), a pierced balustrade with posts and eight lanterns, a round furnace pit with collar, lining, ember basin
  and inner boilers, boiler foundations, six bastion piers and hanging chains. The old square shaft, shaft collar,
  tower caps, shaft-corner beacons and short elbow drops were removed; the atmosphere's smoke sources follow the new
  boiler flues.

Collision, movement bounds, balance, progression, saves and telemetry were not touched.

## Evidence

Actual Unity captures, same camera and state:

- `../foundry-floor-01/after/` — before this package.
- `depth-only/` — depth ordering alone.
- `after/` — depth ordering plus massing.
- `after/depth-hero-before-tower.png` / `-old.png` — hero in front of a north boiler, with and without the new
  visibility view. `after/depth-enemy-behind-tower.png` / `-old.png` — an enemy behind the south boiler: hidden now,
  drawn over the boiler before. HUD canvases are hidden in these closeups.
- `device.png` — the installed build on the Samsung S23.

Gate:

- Static budget 17 renderers (limit unchanged), 60,768 → 54,168 vertices.
- EditMode 405/405, PlayMode 130/130, Android build exit 0; the build log shows the `Cryptforge/Foundry Cutaway`
  shader compiled into the player.
- Integrity PASS: 519 project GUIDs (+2 new scripts' metas) / 1,099 package GUIDs / 568 scene objects.
- APK `Builds/Android/Cryptforge-Prototype.apk` 39,340,731 bytes, SHA-256
  `AFB6E0079767DF61EB7C97869873F19FF2A288078EF7CF0FF09331CB0B160EC4`, **installed** on the S23 with `adb install -r`.
  Profile and backup SHA-256 identical before and after (`d3645ca6…`, `e82c127c…`); pulled profile, backup,
  development flags and telemetry are in `TestResults/foundry-depth-mass-install-20260930`.
- On device: one launch, one screenshot, then Home to pause. The launch went straight into a live court run with
  enemies already attacking (81/100 HP at capture). The screenshot shows the court start only; it does not show a
  structure occluding an actor on the phone. Not a performance test.

## Limits

- Hidden actors have no silhouette: an enemy fully behind a boiler is invisible until it steps out.
- Chains read thin at the gameplay camera; the six piers are uniform.
- Combat effects stay at z = 0 and are not depth-sorted against structures.
- Still far from the reference's painted richness: surfaces are vertex-coloured procedural meshes. See
  `../painted-surfaces-01/PROMPTS.md` for the painted-texture brief.
