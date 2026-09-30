# Foundry court floor at the gameplay camera — 2026-09-30

## Why this package

Ranked against the approved reference (`ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`) at the camera the
player actually uses, the largest gap was the floor: in the portrait view the court paving fills about four fifths of
the screen, and its per-flag variation was ±6 %, which does not read on a phone. The screen was one grey sheet with a
single ring. The reference's court is mottled flagstones with brass inlay laid out around a compass.

## What changed

- **Flags read one by one.** Per-flag tone now has a skewed distribution (about one flag in six a darker replacement
  stone, one in ten paler and warmer), a subtle warm/cool tint on some flags, a darker joint (0.64 → 0.50) that is
  wider (0.016 → 0.028 design units), and a stronger lit/shaded bevel (0.035 → 0.06 wide, 1.10 / 0.74). Flush vertex
  colour only; no new floor geometry that could read as an obstacle.
- **Court compass.** Eight brass rays leave the forge seal and meet an outer ring of radius 8.6, with a lozenge where
  each cardinal ray meets it. Every piece is flush and clipped to the walking union, so the ring breaks where the shaft
  cuts the court. Narrow worn channel either side of a brighter inlay, so it reads as metal rather than a crack.
- **Cutaway bug fixed.** Flush floor decals (coping trim, borders, inlays, seal, bridge joints, gate threshold) sorted at
  the same rank as raised parapet sides and so received the hero cutaway: brass lines vanished in a disc around the
  hero's feet as he walked over them. They now form their own `Foundry floor decals` batch on the plain material, at the
  same sorting rank. Renderer count unchanged.

Files: `Scripts/UI/Arena/FoundryRegionFinish.cs` (flags), `FoundryRegionHeraldry.cs` (compass),
`FoundryRegionView.cs`, `FoundryRegionSite.cs`, `FoundryRegionArchitecture.cs` (decal rank), and
`Tests/PlayMode/FoundryRegionTests.cs` (the decal batch must exist once and must not use the cutaway shader).

## Evidence

Matched actual Unity captures, same code except this package, same camera and state: `before/` and `after/`
(`start.png` is the portrait start in the court, `overview.png` the whole map, `ground-readability-on.png` the
cutaway point near the boilers). `device.png` is the installed build on the Samsung S23 at the court start.

- Static budget: 17 renderers (unchanged), 59,584 → 60,768 vertices (+1,184, the compass).
- Compile 0 warnings/errors; EditMode 405/405; PlayMode 130/130; Android build exit 0.
- Integrity: 517 project GUIDs / 1,099 package GUIDs / 568 scene objects. The last record said 518; no asset was
  removed in this package and git shows no deletion.
- APK `Builds/Android/Cryptforge-Prototype.apk` 39,340,731 bytes, SHA-256
  `59892F56D5BDE8A82A3514AC92A4D49A9897505E175878D0E327F93EAAE964EB`, **installed** on the S23 with `adb install -r`.
  Profile and backup byte-identical before and after (SHA-256 recorded in
  `TestResults/foundry-floor-install-20260930`, with the pulled development flags and telemetry).
- On device: one launch to the court start, one screenshot, then Home to pause. Not a performance test.

## Limits

- This improves the floor at the gameplay camera. It does not close the distance to the reference's architecture:
  landmarks are still small against the court, the parapet is thin, the shaft is square, and there are no chains or
  heavy foundations.
- The cutaway still dissolves structures on every side of the hero, including behind him, and distant enemies still
  render above structures. Both need real depth ordering of tall structures against actors, which is the next package.
- The brass lines are darker than the reference's lit gold; they match the court's existing inlays.
