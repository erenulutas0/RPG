# Independent pit-metal source review - 2026-10-13

Read-only review of FoundryRegionPitHoods.cs, FoundryRegionPitMetal.cs, FoundryPitVesselTests.cs, the active BuildFurnacePit hook in FoundryRegionMassing.cs, shared MetalTone and FloorPatina, and the bound FoundryIronV2 texture. No Assets edits, Unity runs, ADB operations or git changes were performed by this reviewer.

## Active implementation and geometry

BuildFurnacePit emits FormedPitHood into the existing FoundryIron material and unchanged depth owner. PitHoodCourse now calls PitMetalPlate, so the change affects both real hood meshes rather than only an inactive legacy builder. Hardware and caps also call PitMetalGrain.

Upright plate courses are partitioned at a low seating row; sloping courses retain their original broad faces. The four original corners remain. The new points interpolate within the original projected face, so there is no intended enlargement or added coplanar plate. Original profile, straps, collars, flue mouth and depth envelope remain. Existing whole-hood envelope tests and new facet-area/ownership tests cover this contract. No material, renderer or update callback is introduced.

The lower contact function is sampled at bottom, cut and top vertices; the rubbed seating value is therefore actually present in the mesh. The angular reflection and crown soot also sample emitted angle/height stations, so their variation does not rely on an unsampled interior peak. Adjacent facet edges receive the same tone for equal physical stations. The low-frequency forging field samples emitted local positions and is subordinate to the broad form lighting.

## UV contract

PitMetalGrain uses one affine local screen-plane mapping across plate, straps, collars and caps. It does not clamp vertices onto a texture row. For a nondegenerate projected triangle, the affine mapping preserves nonzero UV area. The bounded hood footprint keeps samples well inside the broad plain interior of FoundryIronV2, away from the outer riveted/metal frame. The bound material GUID resolves to FoundryIronV2, which was inspected visually. New tests check the whole emitted mesh UV bounds, shared-position continuity and visible-triangle UV area.

## Finding from source plus first actual capture

The first candidate does not yet have a coherent exterior metal color contract. PitHoodCap retains the old literal blue tone (0.55, 0.61, 0.76), with only UVs updated, while the body now receives the neutral-to-warm PitMetalTone and world reflected forge light. The first After shaft-detail confirms the resulting defect: a conspicuous navy annulus around the warmer brown/olive neck and shell, particularly on the larger left hood. This is not merely a hypothetical source concern.

Request: put the exterior cap in the same sampled cast-iron tone/light family as the touching shell, while retaining the deliberately dark inner mouth bottom. The shell also reads rather bronze/wooden in this first capture; assess modest local cool compensation with the shared forge light applied, instead of a global exposure change. Do not erase the cool-upper/warm-lower structure or change the silhouette to conceal the seam.

The guard coping in this same capture is quieter and reads as fitted stone, with no observed new outline or contact defect. Its correction is independent of the hood color finding.

Final acceptance must be recorded after the repaired actual capture. Passing geometry tests alone does not close this material mismatch.

## Repair review

The exterior head now goes through PitMetalHeadCap. It retains the original MassDisk fan and exact perimeter, derives the real radial angle from each emitted position with FoundrySurface.ToDesign, and evaluates the same PitMetalTone as the shoulder. PitMetalGrain remains shared. The separate dark cap inside PitHoodMouth is unchanged. The new two-case regression inspects actual shared head/shoulder rim vertices on the whole manufactured hood and independently checks that the mouth centre remains dark. This addresses the reported color-contract defect without enlarging geometry or changing material binding.

Reviewed final source SHA256:

- FoundryRegionPitHoods.cs: EE941A19A3A3A83DB4071608511906E9D82D73D1A887BCCA86569F67040680F3
- FoundryRegionPitMetal.cs: FCB0AE140027E9AF16B5A6D76399CFDF5C0033BC53C0973C5E496C637FB9ACE9
- FoundryPitVesselTests.cs: FF576E461C4F010F8E5EFD1F397700FCC83C350C8EEC93C2417D9C88E745323C

No additional source correctness issue was found within this review scope. Actual-image acceptance and remaining art limits are recorded separately in independent-visual-review.md.
