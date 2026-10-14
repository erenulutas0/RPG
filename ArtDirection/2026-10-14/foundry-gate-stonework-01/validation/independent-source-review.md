# Independent source review

Date: 2026-10-14. Reviewer: gate_stonework_review. Source review only; tests and visual captures must establish their own results.

## Scope inspected

- `FoundryRegionGateRecesses.cs` and revised `FoundryGateRecessTests.cs`.
- `FoundryRegionGateEnclosure.cs` and new `FoundryGateCurtainTests.cs`.
- Root's local material finish in `FoundryRegionGateButtress.cs`.

No blocking source defect was found in these changes.

The niche face is partitioned into a bounded number of broad stones: three pieces on each long jamb, two on each dressed pointed-head side, with the sill and broad outer head bearings retained. Body/frame course stations share their contact parameter. Real backing, returns, quarter-depth splay and original exterior stations remain. Indexed stone corners retain the existing face vertex budget. Arris interpolation changes because corners are shared, so the final image review must judge the resulting faceting. UVs remain continuous within each original physical face. Existing coverage, opening and lighting checks are preserved while old fixed vertex-offset assumptions are replaced with semantic frame spans; two new production cases inspect actual mortar and cross-joint UV witnesses.

The curtain ashlar now owns its full cell area through stone, arris and mortar. There is no old complete base wall underneath this ashlar. The closed chamfer and exact original cap are retained. Whole-face UV mapping removes the previous per-block atlas restart, and the original weathering/light formula remains. Broad stock reduces the old fine brick density. The separately authored `GateEnclosureRelief` decoration is unchanged; this package must not be described as removing every overlay from the whole curtain. New tests inspect emitted ownership, area, cap identity, bounded colour relationships and affine UVs. The representation guard includes an outward negative control.

The lower-buttress finish changes only sampled vertex colour after the face UV field. Its neutral gain is bounded approximately within 0.95–1.06, with a local washed body above the damp foot. It retains source forge hue and existing contact shadow. Global elevation ties adjoining courses together; the same face-edge value avoids an abrupt corner mismatch. Caps, geometry, UVs and triangle ownership are unchanged. This is not a general darkening or an extra overlay.

## Remaining acceptance

Actual Before/After images must show stone construction without excessive stripes, outline brightness or loss of niche depth. Source approval does not establish visual acceptance, suite success, Android build success, physical-device quality or reference parity.
