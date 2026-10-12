# Pit material integration — source review

Baseline: `3d85425`. Inspected the approved `foundry-layout-01/foundry-v1.png`, the fresh `before/shaft-detail.png`, the existing lining atlas and the prior combined image review.

## Problem and change

The four raised bearings stretched a narrow atlas border across their full height. This enlarged its rivets and repeated bronze ties, so the new geometry looked like decorated straps rather than formed iron. The bearing course sampled lower, warm stone and looked like a separate brown rail.

- Raised rib faces and their side returns now use a rivet-free cast-iron sample from the existing lining atlas. Height subdivisions of at most 0.84 design units keep the vertical grain near 60 texels per unit. The 0.33-unit half-faces carry about 19.5 horizontal texels. Neither source brass ties nor rivet heads enter this patch.
- Each outside arris is an integral 0.035-unit portion of the original face, with restrained unequal wear gains. The two half-faces keep their common central edge without another seam. All surfaces partition the original quads; there are no projected overlays or outward extrusions.
- The under-rail bearing course samples cool upper masonry, with UV aspect derived from the original chord width. A single 0.016-unit mortar bed per original segment joins the three existing profile surfaces. The upper stone stays shaded beneath the collar while the bottom remains warmer through the unchanged depth heat function.

## Preserved contracts

Only pit lining methods in `FoundryRegionMassing.cs` and the new partial `FoundryRegionPitSurface.cs` changed. Original stations, profile coordinates, deck contact, relief depths, lower wall, pool seam, wash, furnace apertures, vessels, hood geometry, material/batch assignment and renderer/depth ownership remain. No texture or material resource changed. No gameplay, camera, navigation, scene or persistence changes.

Calculated complete-lining cost is 1,932 vertices versus 1,264 before (+668), under the existing 2,124-vertex test ceiling; the guard was not relaxed. The existing five pit construction cases cover the full below-deck envelope, exact bearing joins, furnace-bay columns, finite mesh/UV/alpha, molten seam and cost. No new behavior or new geometric envelope required an additional test oracle.

## Validation status and visual acceptance needed

Source-only arithmetic and diff review completed by the author. Root owns compilation, Unity suites and actual capture; none are claimed here. Check the final close capture for cool masonry beneath the collar, restrained iron grain rather than smooth black bars, small worn edges, and the preserved pool-to-wall heat transition. Check the full-map and nearby actor portrait for unchanged pit prominence and occlusion. Device motion/shimmer remains separate and unverified.
