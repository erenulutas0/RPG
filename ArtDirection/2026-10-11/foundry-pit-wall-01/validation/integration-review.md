# Independent source integration review

2026-10-11. Baseline `c549f06ebfffeee990ea21bb2fd7abd1f8082047`. Reviewer: pit_wall_proof helper. Read-only review of `FoundryRegionPitHoods.cs`, the pit-only changes in `FoundryRegionMassing.cs`, `FoundryRegionUndercroft.cs`, their projection/mesh helpers and new tests. No Unity or ADB was run by this reviewer.

## Finding corrected after review

The initial `JoinedFoundationSpans` joined spans using design-plane collinearity alone, but `Project` is piecewise linear across the gate ramp at u=25 and u=27. The old individual-cell foundation follows those elevation changes. A merged landing edge from (24,-2) to (28,-2) projected only the endpoints and became a single slope; at u=25 the resulting shoulder was .3125 units above the old line, and at u=27 it was .3125 below it. The lower skirt uses contracted endpoints whose projection crosses the ramp at different design stations, so protecting only the deck endpoints was insufficient.

The repaired source adds `FoundationProjectionStraight`, which requires collinearity at the old shared point for both the projected deck and the contracted/projected foot. Its double-precision cross-product check preserves the existing grade-break vertices. Independent reread accepts this repair. `JoinedGateCurtainKeepsOriginalRampAndContractedFootBreaks` checks every original landing edge against the emitted joins and includes the 25..27 counterexample where the deck is straight but the contracted foot is not. The flat-origin gap/corner test is retained. The repaired actual capture completed and final visual review accepted it. Root suite/APK verification was still running at this review update; final execution results belong in the package verification record.

## Other inspected contracts

- Hood variants retain the original radius 1.1 and height 1.3 owner. Profile joins are continuous; the .18 start is seated on the existing stone foot. The original flue terminal and smoke attachment remain.
- New head straps interpolate real facet endpoints rather than an ideal circular radius, and their saddles overlap the supporting flange. Hardware emission stays within the existing iron batch.
- Service panels and retaining belts remain below deck and within the old head envelope; they use existing pit lining/iron resources. Full runtime ownership and contact acceptance still requires actual Unity inspection.
- The new mouth uses an annular lip and recessed dark inner return; no full disk is emitted at the top opening.
- Undercroft dressed faces retain exact incoming quad envelopes. The solid underlay closes the joints, and the inherited edge helper partitions its face rather than enlarging the outline. Existing material, ranks, pier-after-arch emission, bastion screening and lower shading are retained.
- Span collection still excludes the pit opening and bridge join. No new actor-reachable obstacle, navigation input, camera, runtime updater, resource or scene mutation is present in the reviewed diff.

This is source review, not final visual acceptance, full test evidence or device performance evidence.
