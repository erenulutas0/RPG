# Actual Unity image review

Root reviewed the final native capture after the 0.62 RGB / 1 alpha material calibration and scoped south-feed ordering fix. Compared the fresh sealed Before to final After, particularly gate-detail, gate-crown-detail, full-map and approach-20-1. These are the production Android-target Unity scene with paused combat and hidden HUD, not phone screenshots.

## Accepted changes

- Both outer bays show broader fitted stone courses, softened bevel light and a more legible recessed pointed niche. The normal native bay mesh is replaced, not layered under the baked card. The darker calibrated bake avoids the pale towers seen in the rejected second capture.
- The two native header shoulders have shallow pitched roofs and closed gables inside their existing bounds. This is a restrained construction improvement, most visible in the crown close-up. The central lock stays readable.
- Both niche apertures remain visible after separating the card from its curtain rank. The near boiler feed remains continuous over the bay; its body, collars and elbow now use a strict foreground rank instead of tying the new material. Gate iron inlays and unrelated mains remain at their prior ranks.
- The full-map composition retains its supported gate silhouette. The outer-bay change is visible but modest at full-map scale; this does not reconstruct the reference map.

## Limitations retained

The native inner piers and curtain/header surfaces have visibly different, coarser grain from the new baked outer stone and authored door. That is the next coherent landmark scope. Broad courses remain simpler and cleaner than the reference's irregular masonry. The existing approach-20-1 portrait crops the crown and most outer bays; it establishes lane readability and retained contacts, not a large new portrait-quality gain. No camera change was made to manufacture a better comparison.

The reference remains a target, not achieved parity. Phone motion, texture shimmer, HUD overlap, crowd motion and midrange frame-time are not covered by these static captures. No device installation or control occurred.

## Rejected iterations

The first integrated capture passed the graphics fixture but failed visual acceptance because separate equal-rank material batches let a curtain cover a niche. The second exposed the bays but was too pale. Their key images, graphics XML and source freezes are retained under first-integration and second-integration. The subsequent calibrated candidate exposed a source-level tie with an opaque south-feed witness; the scoped pipe repair is included in this final capture. Initial compile failure and repair are also retained. Passing automated checks did not substitute for image review.


## Budget-test correction and recapture

The first full PlayMode run rejected the old aggregate batch cap (159/160 passed). The new gate integration test passed. Gate cards are now separately counted as exactly two, preserving the old native-other cap24; total allowed renderers explicitly rises34 to36 for this measured package. Rejected XML and source freeze are retained in rejected-budget. Runtime production source did not change. The new capture was compared against the accepted images; changed PNG files: []. The visual verdict above still applies. Final full regression/build results remain a separate gate.
