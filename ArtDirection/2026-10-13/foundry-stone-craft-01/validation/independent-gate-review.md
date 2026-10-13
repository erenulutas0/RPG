# Gate visual review and independent floor source review

2026-10-13. This report is by the gate implementer. The gate image assessment is therefore an implementer review, not an independent acceptance claim. The floor source review was independent of its implementation; another helper separately reviewed the gate source. Root performs the combined acceptance and Unity verification.

## Images inspected

- before/gate-detail.png and after/gate-detail.png
- before/full-map.png and after/full-map.png
- before/approach-20-1.png and after/approach-20-1.png
- after/approach-0-0.png and after/shaft-detail.png
- Approved reference: ../../2026-09-23/foundry-layout-01/foundry-v1.png

The matched images retain their cameras and actor placement. These are paused Editor scene captures with hidden HUD, not physical-device observations.

## Gate result

The outer gate buttresses now read as heavier seated structures. In the close view, the far tower visibly replaces its thin low belt and long narrow bay with a broad coursed lower body, one sloping shoulder and a shorter upper recess. The near tower follows the same joined profile. Course edges remain quiet and the existing mineral field does not turn into a stretched stripe. The new shoulder meets the shaft without an obvious floating course, doubled ledge or exposed gap in the inspected views.

The door seal, painted banners, two inner jambs, flames and ramp remain in their former positions. The gate still anchors the end of the approach. The portrait approach leaves the hero and range guide readable, although it frames little of the outer buttresses: this portrait proves preserved approach readability more strongly than the complete new gate silhouette.

At whole-map scale the gate change is modest. It improves the load-bearing shape of its outer pieces; it does not recompose the gate, make the map larger, or match the reference by itself.

## Remaining material and composition gaps

- Large native tower faces and caps remain cleaner, more angular and less richly worked than the painted door and the reference. The seated lower mass is a foundation for more coherent material work, not finished reference-level masonry.
- The reference has a more integrated stepped gate silhouette, stronger architectural returns and varied stone edges. Adding more thin decorative strips would not solve that difference.
- The whole court still looks more geometrically regular and visually sparse than the reference. The larger quarry slabs help its scale, while the open combat space and map outline remain intentional and unchanged in this package.
- The portrait actor scale is appreciably larger than the whole-map overview suggests. The tiny overview actor is not sufficient evidence to change the gameplay camera.
- Motion shimmer, phone HUD overlap, physical-device load cost and midrange performance cannot be judged from these captures.

## Independent floor source review

Reviewed the changes in FoundryRegionFinish.cs, FoundryRegionFlagContours.cs, FoundryRegionPavingWear.cs, FoundryPavingLayoutTests.cs and FoundryFlagContourTests.cs. The gate crosswise slab bounds, exact packed-cell coverage, zone boundaries, world-space lighting and grain coordinates are retained. The larger court family has an actual packed-area share guard rather than only a catalogue test.

The initial QuarryEdgeAbrasion implementation used Unity Mathf.SmoothStep as though its first two arguments were input thresholds. That made edge abrasion remain nonzero across deep stone interiors. The floor author corrected the input normalization with InverseLerp followed by SmoothStep(0,1,t). The new regression exercises 41 seeds and requires zero broad-interior abrasion, zero unworked-edge abrasion, a present worked lip and bounded output. The corrected source has no remaining actionable finding from this review.

## Gate verification scope

Five added gate cases check original surface area and one-owner coverage, noncollapsed UV area, and support of every upper corner and midpoint on the actual chamfered lower cap. Existing complete-tower footprint, vertex bound and visible recessed-backing assertions remain. Their bay start changes intentionally from 1.84 to 2.85 because the new shoulder occupies the lower section. No door, ramp, camera or gameplay contract is changed by that update.
