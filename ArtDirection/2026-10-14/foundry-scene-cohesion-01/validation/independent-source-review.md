# Independent source review

## Root paving and floor-light pass

Reviewed the current FoundryRegionFinish implementation after the baseline was sealed, with the existing dependent weathering, court, bridge, service-apron and test code. This is read-only source review; no Unity execution or actual After acceptance is claimed here.

No correctness blocker found in the root change:

- All ForgeLamp instances are created by Lamp, which initializes FloorRadius. The explicit pit/boiler values and 2.8 point-source default therefore cannot leave a zero radius in the present call graph.
- FloorLight uses FloorRadius for the early cutoff, heat falloff and ambient reach. SmoothFall retains a smooth boundary.
- ArchitectureLight and MetalLight still use the original Radius, Strength, Height and source positions. Their light model is isolated from the new floor-specific radius.
- PavingVertex and LitFloorQuad share PavingGrainSpan=4.3; service-apron inverse design decoding uses the same constant. Court and fitted emitters route through PavingVertex. No new per-face texture reset is introduced.
- Colour-only treatment preserves all explicit geometry/ownership code, but the changed light can change AshlarLightStep refinement and therefore actual vertex count. Final resources must be measured.

Existing FoundryBridgePaving, FoundryCourtDressedStone, FoundryCourtCompass, FoundryFlagContour, FoundryCourtMaterial, FoundryPavingLayout, FoundryWorkingApron and FoundryElevation coverage already exercises the relevant geometry, UV and material-field contracts. Existing architecture/metal tests check height attenuation and ramp continuity. A new test merely asserting the cosmetic grain-span or patina constants is unnecessary.

The one meaningful new coverage gap is radius separation. Current material tests divide emitted colour by production FloorLight, so they cannot independently detect an accidental return to shared Radius. A single isolated fixture can save/restore the lamp array, change only FloorRadius, and require actual floor output to change while architecture/metal output stays equal. This covers the new behavioral boundary without duplicating the lighting formula. Any fixture mutation must be restored even on failure.

The broad patina amplitude is reduced, but its midpoint rises from .9675 to .9875, about 2.1%. The combat centre remains protected from the existing use/contact fields; its final floor RGB is not bit-identical. This small lift is an actual-image judgment, not a source blocker. Do not describe it as an unchanged rendered centre before checking the captures.

Workyard and lower-facade helpers were still implementing at this review point. This note does not accept their final source or images. Actual full-map, court/forecourt, approach-20-1, yard/stock/casting and near-island captures remain required for visual acceptance. Static captures cannot establish phone performance, motion or shimmer.
