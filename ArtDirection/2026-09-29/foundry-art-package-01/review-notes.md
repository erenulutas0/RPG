# Independent art / implementation review — 2026-09-29

## Scope

Read-only review of the architecture, floor finish and workshop slices and their agent reports. No Unity, dotnet, ADB, gameplay edits, asset edits or commits were performed by this reviewer. The integration owner supplies compilation, actual rendering, mesh census and device evidence. Visual acceptance is pending those captures.

## Initial code findings

- No confirmed movement-footprint regression: workshop service shelves occupy u[-20.7,-18.45], v[5,8.4] and u[2,7], v[18.5,20.4], outside the existing walking union. Props are low and remain on the shelves. No collision or navigation change was introduced.
- Geometry helpers continue to merge by borrowed material and logical order. Workshop objects reuse existing groups; no new material instance, texture, frame update or independent mesh ownership was introduced. The existing region mesh disposal path still owns emitted meshes.
- Floor patina and value variation execute during mesh construction. No mutable random state, texture generation or frame work was added. Existing edge and heat calculations remain the larger build-time cost. This review does not establish startup allocation or device timing.
- Gate/boiler additions keep the existing footprints and chimney mouth anchors. More visible hardware does not imply a new interaction.

## Required visual check

New iron insets and stone architecture both use logical order -11, in distinct material batches. Both project to the same depth plane. The actual gate and boiler captures must show iron panels, collars and flue bands on top of their supporting stone; creation order alone is not a robust cross-material transparent ordering contract. Inspect this before accepting the integration. If hidden or inconsistent, resolve the intended ordering explicitly and rerun the relevant capture rather than increasing contrast blindly.

Check workshop silhouettes from the real crane-side and north-side cameras: outside-floor placement is correct, but projection can still visually overlap actors or hide small stock. The broad patina should reduce the old paving grid dominance at phone scale. New central seal and brazier changes are not yet included in this review.

## First actual Unity capture review

Inspected gate-detail.png, workshop-detail.png, pipe-storage-detail.png and point-2.png from the integration capture output in foundry-playable-01. Findings sent to the integration owner before finalization:

1. **Confirmed ordering defect:** the new gate iron details are hidden by the masonry. The owner is correcting their explicit sorting rank and plans another actual capture. This is a blocking visual defect for the advertised gate improvement.
2. **Central seal needs attenuation:** at portrait scale the solid dark radial pieces and stretched brass texture read as raised blades and dominate the hero's immediate floor. Suggested lower vertex alpha (approximately 0.30–0.40) so paving remains visible and the seal reads as an inlay. Actors remain discernible, but the current result does not meet the stated quiet-floor intention.
3. **Workshop material polish:** placement and silhouette work, but the grey bin bevels and ingot ends are uniformly flat compared with the detailed surrounding masonry. A material/UV or contrast refinement is desirable. This is a visual consistency issue, not a movement or sorting blocker. Pipe stock reads as industrial storage; the tongs rack remains peripheral detail at phone size.
4. **Brazier:** cup and prongs give the flame an identifiable support. The source sample renders predominantly dark red, rather than a bright fire core. This is a remaining art-quality limitation.

The first north-side capture also showed an incorrect cavern backdrop; the integration owner identified a capture camera framing omission and is correcting the tooling. No claim about final exported backgrounds is made from that first image.

## Final candidate visual verdict

Reviewed the refreshed gate-detail.png, workshop-detail.png, point-2.png and map-export.png after the corrective pass. **Accepted for integration verification and physical-device review; no remaining blocking visual finding in these views.**

- Gate insets, collars, central small forge mark and flue bands now appear over supporting stone. The explicit overlay rank resolves the observed masking defect; the added renderer group is justified by that actual defect.
- Workshop metal now shows surface variation and bevel continuity, without the former untextured grey faces. Shelf placement remains clear of the traversable floor.
- The attenuated seal now allows paving to show through and reads as a quiet floor mark. The hero, enemies, health bars, chest and range outline remain dominant in the portrait density capture.
- Braziers have a visible orange core at portrait scale. Cup and prongs continue to anchor them to the scenery.
- The full-map export contains the continuous cavern backdrop and shows the bridge, crane, shaft and gate composition coherently.

Acceptance is limited to this incremental package. The overall environment still has simplified repeated architecture and an intentionally open central court; it is not a claim of final reference-image quality. Full test results, final mesh count, installed build and physical phone status belong to the integration owner's report.
