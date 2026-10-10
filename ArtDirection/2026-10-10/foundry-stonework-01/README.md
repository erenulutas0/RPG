# Foundry stonework — matched Unity proof

Started 2026-10-10, completed review after midnight on 2026-10-11. Baseline: `22e32ea`. Before images and graphics XML were copied unchanged from accepted `foundry-gate-court-01/after` captures of that runtime. The new runner retains identical cameras, image dimensions and staged actor positions. Approved reference: `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`.

## Implementation

Four runtime partials: FoundryRegionFinish, FoundryRegionFlagContours, FoundryRegionArchitecture and FoundryRegionGateHeader. The ordinary ashlar family now favours 1x1 and 1x1.5 stones, with occasional larger replacements. Narrower, quieter joints and less per-stone value contrast avoid a dense checkerboard. The redundant per-slab cloud wash is removed; world-coherent quarry variation, soot and the forge light field remain. The new shared PavingGrainSpan (5.7 instead of 8.55) increases continuous texture density 1.5x, including ceremonial borders and ramp pieces. Two existing geometry tests use the updated UV contract; projection/coverage assertions are unchanged.

The gate adds bonded dressed courses to its lintel, crown faces and inner returns with physically scaled grain, sparse clipped arrises and restrained damp variation. Blind niches now map continuously instead of restarting the mineral patch on each triangle. Coping blocks use narrow joints and shallow geometric bevels within the original octagonal cap footprint and top height. Original door, banners, silhouette and depth owners remain.

## Generated material and provenance

The first actual capture showed that UV changes alone did not remove the broad cloudy surface. MAT_FoundryPaving actually referenced V4; the older BuildFoundryPavingAsset importer still points to V2 and was not used. Built-in image_gen edited V4 into the versioned `Assets/_Project/Art/Props/Foundry/FoundryPavingV5.png`, producing finer mineral pores/flecks. Generated pixels were copied unchanged into the repository; no raster postprocessing. Full prompt: prompt.txt. Previous versions and GUIDs are preserved. MAT_FoundryPaving is the only changed material binding.

Both sources are 1254x1254 RGB. Mean RGB: V4 87.76/80.85/84.14; V5 85.92/79.70/82.13, slightly darker rather than brightness-identical. Unity Android import is 1024x1024 ASTC6x6, 11 mips, non-readable. Existing material tint/filter/wrap/mips settings remain. No new renderer/material instance/per-frame component.

## Actual review

First capture rejected by root and independent reviewer: floor still cloudy, gate upper coping became blank. The rejected views/XML/budget are retained under validation/first-*. Final V5 and actual bevelled cap blocks resolve those blockers. Root and independent review accept final court/gate portraits and details. Grain is visible at gameplay scale; hero, enemies and ability effects retain priority. Overview improvement is subtle. Regular paving, simpler central gate housing and the wider reference's architectural richness remain limits, not completed goals.

Region stays at 32 renderers; 115,944 to 118,082 vertices (+2,138/about 1.84%). These counts do not establish frame-time, startup-allocation or phone performance. Map, ramp, camera, navigation, combat, scene and saves are unchanged.

## Verification and phone

409 .NET tests and compile check (0 warnings/errors) passed. Two new after graphics captures passed; the before capture is reused and not counted as a new execution. Static integrity passed 585 asset/folder GUIDs, 1099 package GUIDs and 568 scene objects/components. Full gate passed: 446 EditMode and 154 PlayMode, bringing the distinct suite total to 1,009, zero failures/skips. Android build exit 0; APK 31,210,207 bytes, SHA256 1C0C01119360CEE32312ADDD9FA97C1202F8FBDB0857F5C1D4D04CDBB9AC3196. Final logs have no C# or shader diagnostics. See validation/verification.md.

No ADB operation or fresh phone observation. The earlier concrete owner-run restart/install decision remains pending. Historical installed package 021b95f and Room2 / 74HP / 8 unbanked gold must not guide inputs. On authorization use fresh preflight/current-files backup; preserve current original saves/telemetry, never restore an older snapshot. Physical-device, crowd, midrange and first-ramp acceptance remain open. Regional encounter/boss progression remains deferred by the art-first direction.

## Reproduction

With Unity closed: `pwsh -NoProfile -File Tools/ArtReview/Run-FoundryStoneworkProof.ps1 -Phase after`. Phase chooses the output directory only; it does not restore historical runtime code. Temporary capture source is removed in the runner's finally block. Portraits use production camera width with staged actors and hidden HUD; these are Unity captures, not phone screenshots.
