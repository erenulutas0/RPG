# Gate depth package scope

Baseline: `94243b77ed59bb1c801ca3c7facc2fa11ecda2cb`. Status at this handoff: final image acceptance, full regression suites and APK build are **pending**. No phone installation or device test has occurred for this package.

## Construction and integration

- **Paired outer buttresses:** an editable Blender model and transparent fixed-view bake replace the two original above-deck native bodies. Broad fitted stone, recessed beds and pointed niche returns retain the original footprint, height range and contact stations. Foundations remain native. Both placements share the same unmirrored source, correct projected anchor and one application of the raised landing lift. The original native body remains a missing-resource fallback; it is not emitted beneath the normal baked path.
- **Native header shoulders:** the two upper shoulders now have a ridge, a 0.12-unit fall to their eaves, closed visible end gables and three broad roof rows with separately owned mortar. The flat backing roof is removed. One mineral field crosses each whole shoulder. Lower cornice, central backing/lock, corbels and iron contacts remain unchanged; shared craft helpers retain their original behavior.
- **Runtime ownership:** two generated card meshes retain their existing mass-depth owners. A reserved half-rank places each replacement after its adjacent curtain. The imported texture is borrowed; one scene-owned source material and its shared cutaway variant are disposed by their existing owners. The new scene test covers actual placement, ordering, shared material/depth registration, import properties and unload lifetime.

The first integrated capture was rejected because the curtain painted over the niche. The second exposed the bay correctly but was too pale. The current source uses a 0.62 RGB material gain; its final matched capture is still awaiting acceptance. These rejected iterations are retained in `first-integration/` and `second-integration/`. Source correctness alone does not establish the final material match.

## Measured cost and verification

| Measurement | Baseline | Candidate |
| --- | ---: | ---: |
| Region renderers | 34 | 36 |
| Region vertices | 167,208 | 164,672 |
| Distant vertices, included in region | 3,572 | 3,572 |
| Each native shoulder | 136 | 136 |

The complete package is **2,536 vertices lower**, with two additional renderers. The shoulder-specific change is measured as **zero vertices** for each side in `focused-editmode.xml`, where all six shoulder cases passed. No whole-scene index-count delta was measured. The region/distant ceilings remain 174,000/3,600, leaving 9,328 region and 28 distant vertices; the distant count must not be added again.

The extra runtime texture is 512×1024, ASTC 6×6, with 11 mip levels and nonreadable data. The captured Editor texture memory is **630,880 bytes**. Its independently summed compressed mip payload is **314,992 bytes**, excluding Unity/material/driver overhead. The 1024×2048 master remains an external editable-source output. This trade removes native vertex construction while adding texture/material ownership and transparent fragment work. The additional cutaway material participates in the existing focus update; no new updater component or shader was introduced. These counts are not a frame-time or phone-performance measurement.

The earlier focused run passed **264/264** cases, including the six new shoulder cases. Their checks cover actual production inclusion, original support/envelope, lock exclusion, complete roof ownership, real mortar, continuous noncollapsed UVs and restrained cap values. That result predates the final sorting/value candidate and does not replace its full frozen-source gate. Root will close final capture, full suites and APK status separately.

## Limits

The bake represents one fixed camera direction with baked lighting, not a real-time volumetric replacement. Inner piers, curtain walls and parts of the header remain native and coarser than the authored portal. Reference parity is unfinished. Static captures do not establish mobile motion, alpha shimmer, crowd readability or midrange performance. Gameplay, navigation, camera, door opening and save/telemetry behavior are unchanged.

## Final regression/build closure

438 .NET / 666 EditMode / 160 PlayMode = 1,264 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and final After Android-target graphics captures passed separately. PASS: 626 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,485 bytes; SHA256 649AB25FB971D8362CB0E08FA9EA60253531CEE2A3D7E834005B8AF10674D2A9; NOT INSTALLED.

Two helpers authored the masonry bake and native shoulder geometry/tests; root integrated runtime material, ordering and lifetime, while a third helper independently reviewed source and actual images. Six added EditMode cases cover production inclusion, original bearing/envelope, roof ownership, mortar and UV continuity. One added PlayMode case covers real imported texture, both card projections and depth ownership, south-feed opaque-overlap ordering, shared material gain and unload lifetime. Earlier focused Foundry 264/264 passed. Initial compile failed due to a local name collision and was repaired. First integrated graphics capture was visually rejected for curtain overpaint; second for excessive brightness. The first full PlayMode run passed159/160: the pre-existing other-batch cap counted the new gate cards and rejected25 against24. Gate cards are now counted separately (exactly2), the old other cap stays24, and the total cap explicitly rises34 to36 for the measured cost. Rejected XML/source evidence is retained. Final source freeze stayed unchanged through final capture, full suites and build.

Final images accepted after calibrated gain and the scoped south-feed ordering repair. Region remains 36/164672 because the feed joins an existing iron batch. This closure supersedes earlier pending image/full-suite/build statements above; phone acceptance remains pending.
