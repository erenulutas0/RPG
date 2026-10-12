# Connected working-apron source review

2026-10-12. Baseline: 2c2fa7c. Court helper; root owns Unity, tests and actual-image acceptance.

## Reference and intended improvement

Reviewed the approved foundry reference, the latest material-integration full map, court detail and pit-side portrait, and current docs/38 constraints. The western equipment reads as separate props on the same uninterrupted ceremonial floor. Two small isolated repair patches do not group the receiving area, turntable, trolley and northern stock approach at map scale.

## Implementation

`FoundryRegionServiceAprons.cs` replaces those two elliptical repairs with one connected selection field over already-packed paving. A full-strength north/south spine follows the existing stock/trolley rails at `-24 + FoundryMap.WorkyardShift`; its core is 7.2 units wide, from v -3.5 to 17.65. The receiving arm meets the spine across v 4.6–9.4 and reaches the existing west edge. A 1.8-unit soft shoulder admits the existing ordinary stones by a stable per-parent selection. The east shoulder ends before the pit-side passage. The central seal, gate and entrance are unaffected.

Only long parents at least two units across are subdivided, and both children retain at least a one-unit short side. All smaller parents stay whole; the former one-unit-square-to-half-unit-sett split is removed. Integer half-unit partitions preserve each original footprint exactly, including forced brass boundaries. Existing complete joint beds remain under the faces, with no new overlay, floor hole, raised kerb or movement change.

The existing cool repair-stone family is retained. Its per-slab value now follows the broad court quarry field; split flags no longer receive unrelated random values. Repair mortar changes from 0.80 to 0.85 to keep the larger working zone quiet. The existing world grain, light lattice and forge-light calculations are unchanged. No new texture, material, renderer, per-frame updater or depth owner.

## Verification and acceptance targets

Two new `FoundryWorkingApronTests` cases cover representative production-packed parent geometry, complete interior coverage, flush vertices, world UVs, opaque colors, unchanged small-parent geometry, a 4,000-additional-vertex allowance, the connected rail-contact core, and exclusion of ceremonial/gate zones and the pit-side passage. Existing paving-layout/contour and renderer guards are unchanged.

Isolated compile completed with exit 0, zero warnings and zero errors. Unity suites, actual resource count and image acceptance have not been run by this helper. No Unity, ADB or Git actions were performed.

Inspect the full map for a coherent western working field; it must not become a dark rectangular carpet or tiny checkerboard. Portrait views should retain normal walkability, actor feet and clear blue guides through the pit-side transition. If the whole-map difference is only a slight tint, describe it honestly and use actual review to decide whether the proposed hierarchy is sufficiently readable. Full reference parity and phone performance are not claimed.
