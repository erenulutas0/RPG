# Gate and stone integration review

Date: 2026-10-11. Baseline: `857a99c`. Independent source and test review; this report does not claim device or visual acceptance.

## Reviewed scope

Final author changes in `FoundryRegionArchitecture.cs`, `FoundryRegionGateEnclosure.cs`, `FoundryRegionGateHeader.cs` and `FoundryRegionHeraldry.cs` were compared with the baseline and the latest docs21/docs38 contracts. No scene, camera, map, navigation, combat, save, texture, material ownership or per-frame code changed in this slice.

## Findings

No blocking source-level integration issue found.

- Outer gate buttresses retain their original plan footprint. The reduced coping terminates at height 7.35, matching the updated `MassDepth` envelope. Consecutive shaft, belt and coping tiers retain seated or overlapping bearings. Low landing shoulders stay outboard, terminate at 1.02 under the 1.05 depth envelope and retain their separate below-deck foundation batch.
- Inner cast terminals now terminate at 7.94 within the unchanged gate depth height 8.2. The old comment saying they remain below the central crest became inaccurate when the header changed; root identified and corrected that comment before capture. This is not a geometry defect.
- The lintel still overlaps the existing lower cornice. New side coping and the recessed central cap end at 7.66 for the live height 6.5. The cartouche and its iron lock both use the same vertical compression mapping, avoiding a lock left behind at the old height. Header tie stations remain unchanged.
- New processional flags and two apron courses are flush material faces, not obstacles. They use `CeremonialStoneFace`, world-continuous `PavingVertex` UVs and the shared half-unit light lattice. The current grade breaks at u25/u27 lie on that lattice. No new raised step or collision contract is implied.
- Existing PlayMode checks remain responsible for actual portal-card corners/UVs, fixed gate/landing footprints, ownership, draw order and full-region bounds/budgets. The review did not duplicate those checks or weaken their expectations.

## Added regression coverage

`FoundryGateStoneTests.cs` adds five EditMode cases:

1. Production-sized clipped processional field: complete mortar coverage, contour bounds, finite attributes, valid indices, positive triangle winding and continuous grain.
2. Off-grid field spanning the ramp foot: vertices and four interior barycentric samples per triangle remain on the shared surface; no triangle spans a grade break.
3. Equivalent field spanning the ramp crest, protecting the second seam even though the current processional panel ends before it.
4. The two production apron courses remain separate, entirely on the shared surface, fully covered through their boundary at the threshold, and flush through the ramp foot. No triangle can bridge the empty central band.
5. Every emitted compressed lock/pin/key-slot vertex remains within the actual projected stone backing, protecting coordinated stone/iron compression.

The floor coverage oracle rejects remote and degenerate triangles using bounding checks and double-precision orientations. Tests intentionally avoid freezing decorative vertex counts, slab counts or tonal values.

## Verification status at handoff

- Isolated compile check passed: zero warnings and zero errors. Command: `dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo -p:BaseIntermediateOutputPath=E:/MobileGame/TestResults/gate-stone-qa-obj/ -p:OutputPath=E:/MobileGame/TestResults/gate-stone-qa-bin/`.
- No Unity process or ADB command was run by this reviewer. Root must run the new cases/full suites, inspect actual captured views and record the final package identity.
- Files owned by this reviewer are frozen at handoff. Later gate results and visual acceptance belong in the root's final verification record.
