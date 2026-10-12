# Fortress and bridge masonry — source handoff

2026-10-12. Baseline `2c2fa7c`. Inspected the approved foundry reference, prior material-integration full map and fresh `before/bridge-detail.png` plus `before/gate-detail.png` for adjacent painted boiler/door stone. Root captured the baseline before authorizing these edits.

## Material and construction change

- Lower fortress curtain has three broad main courses rather than four; original two lower skirt courses remain. Major/minor pier fronts have five/four broad courses rather than seven/six. Bridge support legs have three rather than four. Each surface retains its exact original outer quad, closed mortar backing, arch openings and depth. Fewer courses reduce repetitive bands and geometry rather than adding decoration.
- Local fortress stone faces use a joint-free cool interior of the existing V4 masonry atlas. One isotropic projected grain mapping spans each block and its dressed rim; thin/tapered returns no longer stretch the same square. The undercroft spandrel's existing thirteen construction segments share one mineral field and the unchanged contact-shadow strip, avoiding artificial seams at those segments.
- Cooler, modestly clearer curtain, arch and pier fronts separate the platform mass from the background. Dark soffits/side returns and the original depth cooling remain. No new orange wash, global palette change or light source was introduced.
- Bridge parapets preserve their original geometry, exact coping/bevel height, three-unit joint rhythm, small depth owners and keeper sockets. Their grain now spans the entire block across owner boundaries, including the cap and its bevels. The existing lower plinth gains restrained contact shade; no mesh overlay was added.

## Ownership and preserved boundaries

Changed `FoundryRegionUndercroft.cs`, `FoundryRegionBridgeSupports.cs`, and added isolated `FoundryRegionFortressSurface.cs/.meta`. Existing `UndercroftCoursedFace`, `UndercroftMineralFace`, `ShadeUndercroftDepth`, `GateStonePatch` and other shared helpers remain unchanged for the gate, workshop, casting bench, upper court edge and unrelated callers. New wrappers are used only by the selected fortress/bridge path.

No map, navigation, camera, renderer/material/texture resource, depth sorting or per-frame update change. Exact outer envelopes, grade joins, transparent emission order, pipe/drain contacts, arch apertures, wall contractions and bridge passage width remain. The production geometry should decrease from fewer fitted courses; root capture must record actual counts.

## Validation

Added four cases in `FoundryFortressSurfaceTests.cs`: two exercise the actual new production `FortressCoursedFace` on tapered/sloping faces (complete original underlay, containment, nondegenerate triangles, finite alpha/UVs, isotropic mapping and continuous grain over each dressed rim), and two compare emitted bridge-cap UVs across adjacent fine depth owners on both sides. Existing tests and ceilings remain unchanged; old shared-helper tests alone are not claimed as coverage of the new production path.

Root owns compile, complete test suites, Android-target captures and APK validation. The author did not run Unity, ADB, Git or compilation. Final image acceptance must check broad stone readability without pale blocks, missing mortar/edge contact, visible texture restarts or a smooth blank spandrel. The former floor/coping/neighboring boiler material work is retained; this local package does not claim full reference parity or device performance.
