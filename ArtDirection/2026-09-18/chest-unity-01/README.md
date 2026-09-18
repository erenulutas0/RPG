# Chest Unity sampling proof — 2026-09-18

Eleven real Unity renders of the existing Gameplay scene with a temporary visual substitution. No authored asset, scene, gameplay, APK or device changes.

## Method

Unchanged RGBA masters in ../chest-proof-01 are losslessly staged, then sampled in Unity to 192-square bilinear sprites. Full 1254-square source canvas, common pivot (620/1254,174/1254), PPU = 192*788/(1254*.75), approximately 160.87. This makes the reference lower-box width approximately .75 world units; the second candidate scales uniformly by .9 to .675. Canvas width is larger than visible box width; these quantities must not be confused. Open lid extends right beyond the box and is not independently fitted.

The fixture uses TestProfile isolation and the ten-enemy density room, disables attacks, settles pursuit, freezes time and temporarily swaps only the chest sprite. It captures 1080x2340, 1080x1920 and close views. Hero front/behind positions are deliberately staged, not recorded gameplay. It verifies 10 enemies, common sprite bounds/pivot and unchanged chest-open count. Unloading the test scene disposes temporary sprites/textures; the isolated profile is removed. No device profile or telemetry touched.

## Review

- Painted brass and charcoal panels fit the existing floor and actors substantially better than the procedural chest.
- Prefer .75 as the initial runtime candidate: the .675 option saves little crowd area and weakens the small ember cue. This is visual judgment, not device acceptance.
- Open/spent silhouette and dark interior remain distinct. Three generated frames still have small base/rim changes; no smooth animation is proven.
- **Depth issue found:** in overlap-hero-front the chest still draws over the hero torso although the hero root is .25 units nearer. Existing center-based sprite sorting uses sprite bounds, while body/equipment have different local sorting orders. This proof leaves that behavior unchanged. A grounded depth grouping/pivot solution needs a bounded integration check; merely shrinking the chest does not solve it.
- The chest also overlaps a Grunt in the authored spot. This is preserved evidence, not a claim that crowd readability is finished.
- No painted contact shadow, compression acceptance, runtime opening animation or resource-reuse proof yet.

## Reproduce and checks

`pwsh -File Tools/ArtReview/Run-ChestProof.ps1`

The runner refuses an active Unity process/lock or an existing temporary fixture, runs pure tests and compile checks, installs only a temporary test source/meta, captures with graphics, and removes that temporary source/meta in finally. PNG masters are never modified.

Verified this slice: .NET **272/272**, compile **0 warnings / 0 errors**, targeted graphics PlayMode capture **1/1**, static integrity **415 GUIDs / 555 scene objects/components**. Full EditMode/PlayMode suites were not rerun; the preceding installed platform build remains the last full runtime gate. No APK was built or installed.

Next: resolve depth ordering for the imported chest/actor presentation, add fixed-pivot shared imports and pause-aware visual opening without changing reward timing, then full runtime gate and coordinated phone review.
