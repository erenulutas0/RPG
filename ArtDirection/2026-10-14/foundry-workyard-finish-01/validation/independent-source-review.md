# Independent source review

Baseline `d442444`. Read-only review of the frozen material scope and focused tests. No Assets/Docs edits, Unity, build, Git or device actions by this reviewer. No source blocker found; visual acceptance and complete regression results remain separate gates.

## Paving contact albedo

`PavingWorkshopTint` combines distance to the three existing service shelves and two actual work-island boxes by minimum distance. Overlapping influence therefore does not multiply several dark masks. Smooth shoulders reach 4.5 design units, with the seal protected through radius 7 and a smooth transition out to radius 11. The raised gate family at u >= 12 remains neutral. Its colour range is white to (0.84, 0.90, 0.94); alpha stays one.

The field is applied once in `PavingVertex`, after the existing scalar `PavingCourtUse`, before final colour emission. Searching runtime call sites found no other application. It changes no vertices, indices, UVs, floor ownership, shader, material or lamp source. Existing continuous `FloorLight`, individual quarry identity and local repair/apron use remain separate. This is additional startup arithmetic in the existing mesh construction, not zero CPU cost. There is no new per-frame call path.

Seven new cases inspect actual emitted ordinary/fitted paving. They recover the new albedo from the prior source-light/scalar recipe without calling `PavingWorkshopTint`, so missing/doubled application can fail. The saturated witness values are independently declared; a broad sample checks bounds, soft shoulders and neutral centre; a real shared boundary checks exact colour agreement. Geometry/indices and UV-derived design coordinates are also checked. Legacy mineral/scalar isolation helpers explicitly divide the new field, while the independent new cases own its verification. No geometry/colour tolerance was loosened for those isolation changes.

The main outstanding visual risk is cumulative darkening: the existing apron use, soot and local repair finish already reduce working-area value. Even with a bounded single new field, dark rail/bench feet could become indistinct. Evaluate actual portrait shadows and whether the amber band is more naturally absorbed, not merely replaced with another flat stain.

## Workshop iron and timber

The bench and native winch share `WorkyardCastTone`, which retains source luminance ordering and positive warm bias while narrowing the cool-grey ratio. Local foot/shoulder response is evaluated in the existing finish loops. Timber retains its directional UV field and receives a deliberately less orange palette with modest upper worktop wear. The baked stock/casting/crane art and service pipes are unchanged comparison anchors; this is not a rewrite of all equipment.

Source mapping formulas and emission geometry are unchanged. Two additional tests exercise the finishing functions on actual emitted mesh coordinates with controlled source colours, checking neutral chromaticity, light ordering, warm reflection, broad contact separation and stable geometry/index/UV output. This isolates finish behavior rather than proving every call range by itself; the existing complete station/timber and geometry tests plus inspected call sites cover integration. The actual timber palette assertion now has bounded revised channel ratios, representing the declared palette change rather than an unexplained weakened lower bound.

## Service-tower stone

Only selected RGB construction values change. Exposed pier contours, outer spandrels and supported corner/shoulder blocks gain a modest neutral value; inset backs, deep returns, projecting dressed arch, fire panes, head and final atmosphere/weather/fade functions retain their prior values. Geometry, UV fields, alpha, openings and stations remain unchanged. The existing distant budget is not increased.

Six new production cases inspect both bay orientations and authored proportions, exact expected gains and unchanged sheltered values, UVs, alpha, complete shaft cost, supported block/taper geometry and the unchanged head. They calculate old source recipes independently of the new quarry helper and compare actual emitted buffers. Existing opening/contact/envelope checks remain authoritative. No source blocker found.

## Acceptance boundary

Root reports focused Foundry tests 258/258 and pure tests 438 passed, with isolated compilation clear; this reviewer did not execute them. Full-suite/build outcome is not asserted here. Fresh capture must confirm unchanged 34 renderers / 167208 total region vertices, with 3572 distant vertices already included. Static counts are not phone performance. The final image gate must reject flattened/crushed contacts, excessive blue-grey metal, orange timber, or tower exposure that competes with the gate. Reference fidelity remains incomplete.
