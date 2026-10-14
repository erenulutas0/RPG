# Curtain stonework integration

## Selection

A UV-only edit would have retained the complete coplanar wall beneath each stone. The selected approach replaces that old two-layer path: each original non-dressed wall face now consists of explicit stone interiors, dressed arrises and narrow half-joints. All parts of this base ashlar surface occupy the same old plane exactly once. The existing narrow GateEnclosureRelief decoration remains its previous overlay; this change does not claim to remove all curtain overlays. Broad courses use approximately 0.82-unit height and 1.15-unit projected width, closer to the supporting tower stock and less finely divided than the previous wall.

A single bounded physical mineral field covers each entire original face, including its joints and arrises. Individual stones no longer restart the old atlas patch. The existing whole-face damp/mineral field, subtle per-stone value range and local forge-light family remain. The closed chamfer stays a single quiet face.

## Scope and preserved contracts

Only the non-dressed `GateEnclosureTier` path and its `GateEnclosureAshlar`, `GateAshlarStone` and new `GateAshlarPatch` helpers change. Already accepted dressed coping and shared `GateFaceLerp` / `GateStoneEdges` behavior remain untouched. Every old cap position, UV, colour and bearing is copied verbatim. Foundations and short tiers that did not use the ashlar path still use their prior path.

No texture, material, renderer, component or per-frame updater is added. The original map/camera/collision/encounters/progression/save contracts remain. Complete-region vertex cost must be taken from root's actual capture; the existing 174,000-vertex budget remains mandatory.

## Verification prepared

Five new EditMode cases in `FoundryGateCurtainTests.cs` invoke actual production `GateEnclosureTier` for both main curtain lifts and a small jamb support. Three profiles assert total projected area, exact original cap V/U/C, strict interior coverage and a one-ULP representable boundary guard with an eight-ULP outward counterexample. Two profiles additionally inspect referenced rendered face triangles for one affine mineral field, bounded noncollapsed UVs and restrained inherited light hue.

No compile, Unity, ADB or Git operation was performed by this helper. Root owns verification. Source review and actual matched-image acceptance remain pending when this handoff is written.


Independent source review found no blocker in the partition, retained caps, shared UVs or inherited lighting. It clarified the existing narrow relief overlay boundary documented above. Actual-image acceptance and root verification remain required.
