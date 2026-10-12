# Gate actual-image review

Inspected the actual matching Before/After `gate-detail.png`, `approach-20-1.png` and `full-map.png` after root confirmed the successful graphics capture. Compared them directly with the approved `2026-09-23/foundry-layout-01/foundry-v1.png`. No Assets edits or runtime operations were performed for this review.

## Visible findings

- The two outer piers now have a visibly longer uninterrupted blind bay. The lowered bearing removes the former square upper-storey / tall block-base split, most clearly on the left side of `gate-detail`. Its plinth remains connected to the landing shoulder and the pipe connection; the existing capital and roof stay seated on the taller bay.
- The inner stone returns read as broad vertical members with three large courses and a shaded longitudinal edge. This is more substantial than the old narrow rectangular strip. The added darker face is conspicuous in the close view, especially beside the far-side banner; it reads as a recessed/turned stone face rather than a missing image, but still has a simple planar finish.
- Painted banners, gold seal, door leaves and portal header remain intact. Neither the new return nor the longer outer niche visibly crosses the painted door area. No floating cap, open silhouette seam or detached base is apparent in the inspected views.
- The actor approach remains clear. That portrait mostly shows the landing and lower door, so the tower improvement is subtle there and should not be presented as a dramatic change to the immediate play view.
- At full-map scale the gate has cleaner vertical grouping, but the improvement is modest. The whole-map silhouette, gate dimensions and its relation to the boilers remain essentially the same, as intended by this bounded architecture slice.

## Remaining reference gaps

The gate still uses broad squared roofs and comparatively plain procedural stone faces. The reference has more varied corner construction, finer support transitions and more integrated roughness/wear; a taller bay alone does not provide that material richness. The painted door remains on its original plane, so this package should not be described as physically recessing the door itself. The existing low rail in front of the door also continues to read differently from the reference's unobstructed threshold; it was not part of this approved change.

No new blocking visual defect was found in the three requested pairs. This is acceptance of a contained structural improvement, not a claim of completed reference fidelity. Paused static captures do not establish device motion, shimmer, crowds or performance.

## Verification context

The first Unity launch caught a test-only `CS0122`: the new gate test accessed internal `FoundrySurface.ToDesign` directly. Root changed that test access to reflection without changing production geometry or the assertion, re-froze Assets and obtained the actual successful capture reviewed here. The isolated compile had not caught the separate Unity test-assembly boundary. Full suite/APK results are recorded by root separately; this note does not claim those results.

The first subsequent EditMode suite found two failing new jamb checks. Source investigation identified both an incorrect containing-tier oracle and a small real square-corner overhang beyond the base's chamfered top. The authorized correction trims the jamb body `.04` inward on each side and independently tests the actual base cap; details and measured clearances are in `gate-source.md`. The images reviewed above predate that repair. Their visual observations remain a record of the first capture, not final acceptance of the repaired geometry. A new root capture and suite run are required.
