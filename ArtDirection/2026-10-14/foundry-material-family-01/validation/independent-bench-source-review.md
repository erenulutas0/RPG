# Independent moulding-bench source review

Decision: **no remaining source blocker; actual-image acceptance pending**. Reviewed FoundryRegionMouldingBench.cs, its production assembly in FoundryRegionProductionYard.cs, shared hull/face emission, structure-depth ownership and the current bench tests. No runtime/test edits or Unity operation were performed by the reviewer.

Final helper reviewed after the contact correction: SHA256 `138AC797CDD33FF3C71AFE47D1EAD82185A0EB55B9B72E9D27943759D9457987`.

## Corrected clamp contact

The first source version placed the long-flask screw at island-relative u=0.43..0.49 and height 1.40. Its oak rail spans u=0.32..0.44, but the 0.02 bevel narrows the flat cap to 0.34..0.42. Thus the screw did not touch the flat rim and its nearest edge remained approximately 0.0215 above the bevel. Both the bench author and this reviewer independently identified the mismatch.

The author moved the shaft and crossbar by -0.08 in u. The final shaft occupies u=0.35..0.41 at height 1.40, fully inside the cap with 0.01 margin. The shaft still passes through the L-arm's upper casting and overlaps its crossbar in height. This repairs the source contact with no geometry-count increase. The intermediate -0.05 shift already provided meaningful positive bearing; full seating is a refinement, not a reason to reject that intermediate candidate for lacking contact. No further clamp correction is requested.

## Cavities, support and drawing order

- The ingot sand top consists of four shoulders around its tapered opening. Its separately emitted floor and four walls connect to the mouth; no full sand quad fills the opening. The bearing top similarly surrounds an annular recess and retains a central sand core. Sixteen rectangle boundary stations include each real corner, so the outer shoulders reach the full non-square tray instead of leaving rectangular corner holes.
- The sand floors are emitted before cavity walls, and top shoulders/core follow them. Later oak rails mask the tray border. This is consistent with the fixed projected view; projected backwall coverage of a mouth sample is not evidence of a closed cavity. Actual captures must establish that the recess depth and ring remain readable rather than appearing as a painted symbol.
- Four leg feet bear at the unchanged skid top, height 0.10. Their raked heads reach the worktop underside at 0.99 and stay below real planks rather than gaps. The shelf, stacked flasks and packing boards retain their support levels. Main worktop boards end at 1.13, exactly where the mould bases and clamp legs start.
- Each near/far U-handle comprises two legs and a connected crossbar. Feet overlap their oak rail by 0.02 in v, and each crossbar shares a full edge with both feet. All three bars share the same vertical range. No visible support gap follows from the source coordinates.
- Frame members are emitted before the worktop and working moulds; foreground hardware follows the moulds. No extra renderer or material is introduced. The complete station retains its original WorkIsland 1 footprint, 1.55 height owner, FoundryIron batch and order -11. Its existing structure-depth assignment remains intact. No request is made to add irrelevant hidden box faces or change actor-depth rules.

The source composition is a coherent supported assembly. It predicts a more legible timber/sand/iron family, but does not by itself establish a visible gain at native portrait or full-map scale. Inspect approach--16-15 and yard-detail, with the neighbouring authored casting bench as the local material reference.

## Meaningful coverage and remaining evidence

The current tests isolate the real top plane, compare its area to rectangle minus mouth plus optional core, check nondegenerate faces and sample open versus solid regions. They also check cavity boundary connectivity and production inclusion, and verify leg contacts against actual plank extents. These are relevant contracts. Existing equipment tests retain owner envelope, UV/colour bounds, stored-flask support and timber grain checks. A concise hardware test should verify actual bevelled-cap bearing and connected handle feet/bar; testing every omitted invisible face would not address the observed issue.

Source accounting predicts 1389 station vertices versus 1784, recovering 395; the unchanged skid is separate. Treat that as pending actual combined-capture measurement, not device-performance evidence. Final source freeze, compilation, test execution and Android-target capture remain parent-owned. Phone motion, HUD/crowd readability, shimmer and physical-device timing remain outside this source review.
