# Independent forecourt layout audit

Baseline: `4c86f7ae3dff5ab56e2f852080226bc696f41bd8`. Read-only source and image review; no Unity, device, Git, or Assets operations by this reviewer.

## Composition and scope

Inspected this package's fresh `before/full-map.png` and the approved `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`. The baseline concentrates too much floor west of the pit and leaves a long exposed entrance bridge. Reassigning four design units from the west yard to the south forecourt is a reasonable reference-shape experiment. It is a real change to the shared walking union, not a camera-only adjustment.

The rectangular court changes from 50 x 32 to 46 x 36 design units, or 1600 to 1656 before pit and corner subtraction. Its extreme isometric span is substantially unchanged: West + South stays -50 and the total diagonal horizontal range stays 82 before corner cuts. Describe the result as a deeper entrance forecourt and redistributed mass, not an increase in the entire map's screen width. Reference fidelity still requires actual comparison of compass-to-mouth spacing, forecourt shoulders, pit placement and gate approach.

Pit bounds, east/north boundary, gate landing, bridge endpoints and bridge width are preserved in the inspected candidate. West service assemblies follow WestAssemblyOffset; interior work islands move half that distance through WorkyardShift. The smaller yard therefore needs route clearance and machinery-contact review, not merely perimeter checks.

## Hidden dependencies identified and source resolution

- The original south CourtFinials at -14.5 would stand inside the new walkable floor. Candidate posts, CourtLantern selection, CourtLanternPosts and their registered depth footprints now follow South. Baked light and physical lamp move together.
- Two original south bastions ending at -14 would be rejected by MassClear after expansion while their unconditional inter-bastion chain remained visible. Both bastions and that chain now follow South; the exterior dressing chain previously at (5,-14) also follows it.
- The old hanging chain at (-4,-14) depended on the old major-pier modulo cadence. Translating that coordinate alone does not guarantee an iron socket. The candidate selects an actually emitted, unscreened major south pier and emits no fallback chain when none exists. Equal-distance ties are deterministic.
- BridgeHeadS changes from -36.3 to -44.3. Leaving lantern station -46 fixed would put it close to the keeper console. The candidate distributes two rows between the actual head and fixed far-mouth posts, restoring spacing. The three transverse support rows also follow the exposed bridge rather than remaining partly inside the expanded court foundation.
- Floor clipping, diagonal boundary walls, bridge side seams, parapet ends and inscribed parapet depth owners already derive from the shared South and require no independent invented boundary. Existing exact seam and owner tests should remain strict.
- Gate and pit architecture remain anchored. The remaining fixed -14 values in pit/gate/working-apron examples do not by themselves indicate stale south geometry; these locations have separate meanings.

No remaining source blocker was found in the reviewed candidate at this stage. This is not a claim of completed runtime or image acceptance.

## Test assessment

The revised FoundryMap tests use fixed points in the removed western strip and newly gained forecourt, not only assertions that constants equal themselves. They cover long movement to the real rim, explicit two-way bridge-to-shoulder traversal, four wave ordinals with ten separated spawns, and bidirectional FoundryPursuit around the changed yard toward bridge, pit loop and gate. The existing full interior grid sweep also adapts to the new rooms and includes four fractional cell offsets.

The new architecture guards inspect emitted post geometry, actual registered owners, lamp/light agreement, scenic bastion hulls, bridge-row spacing, full support return bounds and deterministic chain-socket selection. The existing emitted parapet tests independently verify both asymmetric joins, full cap geometry and owner exclusion from the physical floor. Keep these guards; do not loosen them to permit floor intrusion or float-boundary failures.

Remaining validation distinction: direct route tests and spawn separation do not prove crowd convergence after movement. The old ActualPackMotion case uses one enemy on the old pit-side route. A real PackMotion case through the new forecourt/bridge join would improve integration evidence; otherwise moving crowds and device circulation must explicitly remain pending.

## Actual-image acceptance checklist

1. Compare matched full-map and forecourt views. The entrance should open into a substantial court rather than a narrow long bridge; the crane yard must still have a separate usable silhouette.
2. Inspect both bridge join corners at walking scale. No transverse wall, hovering coping, buried keeper, overlapping lantern cage, unsupported bent or above-floor owner crossing is acceptable.
3. Inspect south post, bastion and chain contacts. The selected long chain must visibly originate at an emitted iron socket; no chain may emerge from empty air or an obsolete wall line.
4. Inspect the moved western shelves/crane and two interior work islands. No footing enters walkable floor without a shared obstacle; no baked card or stock rail becomes detached.
5. Inspect actor feet/guide at each new shoulder and along the west-pit passage. Rendering and navigation must agree, and the current camera must still present a useful play area.

Root owns isolated compilation, static checks, full test suites, Android-target captures, resource measurements and any later device work. No phone-performance or device-acceptance conclusion follows from this audit.

## Follow-up: built chain contact guard

Reviewed the root's `AssertSouthChainContacts` addition to the PlayMode region test. It reads the selected socket from the actual built FoundryRegionView, checks the current real major-pier location `(0, South)`, finds all four supporting plate corners in emitted iron meshes, and requires forged-link vertices near both the upper eye and the hanging end. The link UV filter excludes the full-face plate and rivet quads. These below-deck batches retain local z = 0, so the local-space distance comparisons are appropriate. This improves on selector-only unit coverage by proving that the selected support and the resulting chain were actually emitted. No source blocker found; actual rendering still needs to establish the visible contact and painter order.

The final reviewed architecture also keeps both parapet owners out of the shared union, gives the relocated keeper separate lantern rows, and contains complete support returns below the exposed bridge. A newly attempted ten-immortal-enemy convergence check was being investigated against baseline by the court agent at review time; this audit does not claim it passed or attribute a baseline crowd limitation to the layout.
