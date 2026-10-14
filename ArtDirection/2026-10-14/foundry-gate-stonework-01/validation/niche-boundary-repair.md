# Niche split-edge failure and bounded runtime repair

## Rejected first suite

The first full Unity EditMode run completed 612 cases with 610 passes and two failures in the existing `ProductionSplayedFramesConsumeExistingDepthAndMeetTheirUnmovedBacking` cases. The independent double-precision projected-area oracle found later frame triangles entering the unchanged visible backing by 2.4125643462766567e-7 and 1.8570346099215434e-7 square world units. Its requirement remains strictly less than 1e-8. The first PlayMode run passed 159 cases; the APK was correctly skipped after the EditMode failures.

Root retained the failed suite XML and source freeze under `first-suite/`. The rejected niche runtime and tests are retained there as text. `first-suite/niche-diagnostic/` holds four actual managed geometry dumps and per-triangle clipping evidence. Those standalone managed dumps are diagnostic evidence, not Unity capture or device acceptance.

## Cause

New intermediate course boundaries along the splayed frame's inner contour were produced with float `Vector3.Lerp`. Rounding placed some points slightly on the opening side of the line joining the original unchanged throat end stations. The original complete unsplit contour did not have those intermediate vertices. Actual overlapping triangles belonged to the new inner arrises and mortar; backing, original body and original returns were not implicated.

For the first profile's first side, examples are triangle 190 (indices 190/191/195), triangle 224 (222/223/227), and triangle 236 (234/235/239). The per-triangle evidence identifies all implicated owners. The second profile's managed dump differs slightly from Mono in accumulated overlap, reinforcing why a passing standalone compile cannot replace the actual Unity oracle.

## Repair

Only the splayed frame's split inner edge uses `GateRecessOpeningPoint`. It computes an intermediate point in double precision, then examines the rounded float point against the canonical throat edge in double precision. If rounding enters the opening, it chooses the closest valid adjacent x or y float on the stone side. A bounded fallback may adjust both coordinates by one adjacent float. Original end stations are returned exactly for t=0 and t=1. Stone and mortar request the same corrected station. Body/frame shared contacts and all original backing/return/cap stations remain unchanged.

The correction is at most one representable step per coordinate from the nearest float representation of the double interpolation. It does not enlarge the aperture by a world-space epsilon or change any test tolerance. The original full projected-area oracle remains authoritative. Existing production-course cases additionally inspect emitted vertices for the stone-side half-plane, identical stone/mortar copies and the one-step coordinate bound; no new test cases were added for this correction.

## Diagnostic bound

`niche-boundary-repair/boundary-repair-math.json` records a mathematical replay against the preserved four actual faces. Among 48 distinct course/mortar boundary stations, 24 failed-source stations entered the throat. The maximum inward distance was 7.977337041279871e-7 world units. The repaired interpolation produces zero inward stations and requires at most one adjacent representable step per coordinate. Its largest displacement from the rejected mesh is 2.132480599880018e-6 world units.

This replay is a numerical diagnosis, separate from the actual Unity result. No runtime budget, renderer, resource, gameplay or collision change results from the repair.

## Targeted Unity result

The repaired source passed all 24 `FoundryGateRecessTests` cases in actual Unity EditMode: 24 passed, zero failed or skipped. `recess-repair-targeted.xml` retains the result. This includes both previously failing strict projected-backing area cases and both production-course cases with the new emitted-coordinate bounds. The full package capture and complete verification remain root-owned; the targeted pass does not establish the full gate, APK or device result.
