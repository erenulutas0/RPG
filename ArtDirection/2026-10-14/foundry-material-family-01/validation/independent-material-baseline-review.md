# Independent material baseline and integration review

Status: baseline recommendations only. No candidate material or final image acceptance is implied. Read-only review of current production source, imports and actual scene-cohesion captures after `d4a3ecb`; no Unity, phone or Git operation, runtime/test/bitmap modification, or image reconstruction was performed by this reviewer.

## Visible problem and useful replacement

Re-viewed the actual scene-cohesion full-map, court-detail and approach-20-1 images, V7 source bitmap, and approved `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`. Latest primary handoff/pipeline notes and scene-cohesion next priorities were also read.

The current floor is readable, but its common directional mineral ridges run across every independently laid flag. The raised approach makes this particularly obvious: stone reads as a uniformly combed surface. More joints, higher contrast, new subdivisions or random slab colour would not address that material character. The reference combines quiet cool stone with short, irregular wear and material inclusions; its broad floor does not acquire one dominant grain direction.

The strongest bounded V8 study is continuous worn fine-grained stone with short, discontinuous mineral flecks/chips and subdued local bedding in varied directions. Preserve a quiet midtone and natural irregularity at scales that survive the imported 1024 image and mip chain. Exclude painted laid-stone joints, bevels, large cracks, long parallel ridges, large cloudy patches, directional illumination, vignette and orange fire staining. Geometry already defines the flag layout and vertex colours already provide regional wear, cool ambient light and local fire. A neutral/slightly warm source is consistent with that existing system; a conspicuously blue source would double the current cool tint.

Acceptance should be evident at native approach/court portrait scale: less common diagonal striation while individual flags, brass, hero and violet enemy still separate. Full-map must retain a quiet combat field and coherent stone identity; high-contrast speckle, large repeated stains and a smooth plastic-looking floor are regressions. Assess a real Android-target import and the matched captures rather than the raw generated source alone. A pleasing isolated texture is insufficient.

## Measured V7 baseline

Source: `Assets/_Project/Art/Props/Foundry/FoundryPavingV7.png`, SHA256 `57E938EB1426675C002374C0E3E769349D7BCDF94E36102432F5226F704A29A5`, 1254 x 1254 RGB, 3,456,555 bytes. Measurements use decoded PNG channel values divided by 255. The luma proxy below is `0.2126R + 0.7152G + 0.0722B` on encoded sRGB values, not linear luminance or scene luminance.

| Measure | Value |
| --- | --- |
| Mean encoded R / G / B | 0.477054 / 0.467065 / 0.448469 |
| Encoded luma mean / standard deviation | 0.467846 / 0.054426 |
| Encoded luma p1 / p5 / p10 | 0.345633 / 0.383444 / 0.401935 |
| Encoded luma p50 / p90 / p95 / p99 | 0.465235 / 0.537527 / 0.561838 / 0.610071 |
| Mean linear luminance after sRGB decode | 0.188936 |
| Actual material RGB scalar | 0.604573943 |
| Encoded luma mean times material scalar, before vertex light/tint | 0.282847 |

`ProjectSettings.asset` currently sets Gamma colour space. The multiplication above is a useful first calibration reference, not an exact prediction of rendered pixel values: vertex colours, mip filtering, import resizing/compression and blending still apply. A different new source mean can be compensated deliberately through the shared material scalar, but scalar compensation cannot correct a hue change or excessive contrast. Preserve effective mean/chroma first, then assess matched images. The current `.358 mean` and `_Color .8` comments in FoundryRegionFinish are historical and no longer describe the actual V7 source/material; do not use them as the new calibration baseline.

Pillow Lanczos resizing, used only as a CPU diagnostic rather than a simulation of Unity ASTC or mip generation:

| Diagnostic image size | Encoded luma mean | Standard deviation | p5 / p50 / p95 |
| --- | --- | --- | --- |
| 1024 | 0.467846 | 0.052742 | 0.385698 / 0.465503 / 0.558782 |
| 256 | 0.467854 | 0.033085 | 0.415954 / 0.467202 / 0.523205 |
| 64 | 0.467825 | 0.020323 | 0.435546 / 0.467469 / 0.502213 |
| 16 | 0.467904 | 0.010714 | 0.451445 / 0.467469 / 0.484210 |

At 256 pixels, mean absolute adjacent luma change is 0.020391 along X versus 0.027089 along Y. Wrapped autocorrelation at a four-pixel lag is 0.2583 along X versus 0.0680 along Y, consistent with directional local structure in the source. These descriptive metrics are not automatic art pass/fail thresholds. A new material should reduce a persistent shared direction, rather than merely reduce every texture feature. Check repeat seams at multiple mip sizes and in real floor repeats; matching the average alone does not establish seamlessness.

## Production invariants and intentional V8 integration

- Keep `PavingGrainSpan = 4.3` and the world-continuous mapping `(1 + 2 * Axis * u / span, 1 + 2 * Axis * v / span)`. Both `PavingVertex` and `LitFloorQuad` use this contract. Do not restart UVs per flag or change the geometric floor, ownership, topology, depth or walking union for this material proof.
- Keep `FloorLight`, `PavingCourtUse`, `PavingWorkshopTint` and existing per-stone mineral tint unchanged to isolate the new raster's contribution. A texture/material-only change should not trigger different adaptive floor-light geometry.
- The existing `MAT_FoundryPaving.mat` GUID is `bb0e7a77a8343c145bb92c982d7526df`. `PlatformMaterials.asset` references that shared material. Only its `_MainTex` reference needs to move to the intended newly imported V8 asset; keep the existing material/shader, texture scale `(1,1)` and offset `(0,0)` unless an explicitly measured scalar calibration is needed.
- Runtime consumers are `Layout surface` in FoundryRegionView, `Gate dressed threshold paving` in FoundryRegionArchitecture and `Gate dressed stone border` in FoundryRegionHeraldry. There is no runtime V7-name lookup. Confirm all three use the replacement and no duplicate V7 runtime texture/material is introduced.
- Retain Default texture type, sRGB, alpha ignored, Repeat on all axes, trilinear filtering, anisotropy 2, mipmaps enabled, non-readable, no streaming, and Android override 1024 / ASTC 6x6. V7 actual Android-target capture reports 1024 x 1024, 11 mips, ASTC_6x6, readable False. V7's source is NPOT; the importer uses ToNearest. Do not equate raw master size with loaded texture dimensions.
- Retain one loaded paving resource at the same budget. A source file replacement does not by itself prove equal loaded memory or compression; log the actual runtime texture format/dimensions/mips and material usage again. Keep original generated source and provenance in the proof package.

## Existing coverage and necessary verification

No explicit V7-name assertion was found in Assets tests or Tools source. Therefore no old identity assertion has to be weakened merely to accept V8. The existing FoundryRegionTests surface loop checks non-null, non-readable, power-of-two dimensions and real scene use, but does not explicitly pin the paving's loaded dimensions, mip count, Repeat/trilinear state or ASTC format. A concise extension in that existing integration test can verify the intentional V8 texture identity plus 1024 x 1024, 11 mips, non-readable, Repeat, trilinear, and ASTC_6x6 under UNITY_ANDROID. Verify the shared material survives scene teardown as the existing lifecycle test already requires.

Existing floor tests meaningfully cover continuous UVs, fitted/ordinary shared light/use, exact floor ownership, projected routes, gate and bridge floor coverage. They should continue passing unchanged because the geometry/UV/light contract is unchanged. In particular, do not alter geometry budgets or oracle tolerances to accommodate a bitmap swap. Run the relevant loaded-scene test under the actual Android target and retain the usual focused/full verification evidence. Source tests cannot establish material attractiveness, mip shimmer or on-device readability.

## Remaining boundaries

Baseline is 36 region renderers / 172735 vertices, including 3572 distant vertices. Ceilings remain 174000 / 3600, leaving 1265 / 28. The new workyard and near-island changes share that existing headroom; no extra floor subdivisions are justified. Static counts or a same-format texture replacement do not establish device timing.

This review does not accept phone motion, HUD overlap, shimmer, installation or performance. Existing immortal-crowd waypoint congestion remains unresolved. Preserve map/navigation/camera/actor-depth contracts and the pending device decision; any later authorized install must preserve current original saves and telemetry rather than restoring historical snapshots.

Current scene-cohesion images re-viewed for this baseline: full-map SHA256 `FE2EE703510A387B53C1B66B5BE05B82A35F6AD788AC43FC288D0E392B5C75F8`; court-detail SHA256 `363BE9748761344165A43D7CC226E34A81F6A77846AE716E261396364B261C9E`. Final material-family acceptance requires its own fresh matched inputs and exact hashes.
