# Foundry approach and production yard

## Intent

This combined package follows the owner's request to improve the actual map's major features toward the approved `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`. It builds on the wider west yard and connected foundation recorded in foundry-massing-01. The new slices address the entrance bridge, gate enclosure and an identifiable industrial working area.

## Production yard

`FoundryRegionProductionYard.cs` adds an embedded freight-transfer bay, called once from `BuildWorkshop`. A plain segmented annulus surrounds six broad transfer-table plates and a paired through-track from the salvage approach. A northbound pair of rails leads toward the existing pipe-stock shelf. The circle has no compass rays, luminous perimeter or interaction marker, distinguishing it from the ceremonial central seal.

Centre: design (-24,7); annulus radius 3.22–3.70. Complete planar bounds are u[-29.65,-20.30], v[3.30,17.68]. Every point is flush with the court projection. These are embedded channels and metal inlays, not raised tracks, sleepers or an invisible collision obstacle. Movement crosses the whole bay normally. No track extends into the pit or outside the west court.

All geometry uses the existing Coping atlas and FloorDecal batch. Existing local floor-light sampling applies to its subdivided plates and rail strips. There is no new texture, runtime update, material ownership, collider, reward or interaction. Review its contrast at the actual follow-camera scale before final acceptance.

## Companion features

- The entrance bridge extends and widens through the shared map contract. Paving, rails, supporting arches and endpoint assemblies follow the same bridge dimensions. The integration owner verifies movement and spawn/route behavior over the expanded area.
- Paired curtain-wall wings and broad buttressed towers enclose the existing gate. Their footprints remain outside the approach; the door and threshold retain their anchors.

These features are authored in the production scene-generation code. The gallery's reference image remains a concept, not a screenshot of the running game.

## Evidence and reproduction

The `before` folder was captured before this package. The `after` folder contains the combined production capture. Matching overviews and bridge/gate/workshop/court/shaft details allow direct comparison. The after capture additionally includes a yard close-up and staged hero positions in expanded areas. HUD is hidden for architecture inspection; these are Unity renders, not physical phone screenshots.

Reproduction: `Tools/ArtReview/Run-FoundryApproachProof.ps1 -Phase after`, using `FoundryApproachCapture.cs`. Do not regenerate the historical before folder from modified source. Raw static mesh counts are retained in each capture's `runtime-budget.txt`; those counts do not measure device frame time.

## Current verification status

Source implementation and local whitespace inspection are complete for the yard slice. Its actual Unity close-up shows a readable mechanical track and segmented transfer bed, with no overlap into the pit or raised-looking obstacle. The overview keeps this feature secondary to the central seal and furnace shaft. The shared FloorDecal batch owns every yard piece; no additional renderer is required by the yard.

The final combined capture records 25 region renderers and 113,622 vertices, versus 25 and 107,652 before (+5,970). The initial capture was 114,262 vertices. Review caught support painter ordering and an existing bridge-paving rectangle over the court; the final source draws arch returns before their faces, orders support rows far-to-near, and clips bridge paving at the actual court seam. Softer mortar and bevels reduce the diagonal grid contrast. These are mesh counts only; there is no new texture, material or renderer, and no performance improvement is claimed.

The longer approach and larger gate enclosure move the silhouette toward the approved reference. They do not establish reference-level fidelity: the western court remains quieter and flatter than the concept's populated industrial yard. This is intentional for the flush freight feature; future raised yard equipment requires an honest collision and navigation contract. All989 tests passed:409 .NET,429 EditMode,151 PlayMode. Android build passed, APK ready; installation/device review await owner-run restart permission. No frame-rate claim. Exact build/hash/device limits are in validation/verification.md.

## Bridge contract and inspection cameras

The shared bridge extent is now s = u+v in [-66,-22] and d = u-v in [-5,5], previously [-50,-24] and [-4,4]. Width increases 25%; complete deck extent increases about 69%, including its overlap with the court. Actual floor mesh, boundary handling, spawn sampling, pursuit and outboard decorations follow these constants. The original generic ArenaGeometry metadata is retained: changing its centre would also alter unrelated encounter placement, while foundry movement and camera follow already use the actual map.

Three below-deck masonry bents reuse the existing stone batch. Keeper seats, lanterns, flags, end posts, lights and chains follow the expanded bridge. The artwork is scenic; there is no bridge transition or new boss mechanic. Bridge paving no longer draws its rectangular overlap over court ashlar. Rendered-floor checks sample the new deck, court seam and nearby void; domain tests include far-end blocking and bidirectional pursuit.

The matched overview stays at orthographic size 32 and position (0,4). `after/full-map.png` is an additional wider size-37 inspection at (0,2) to show the entire new bridge and foundation. It is not a same-camera comparison or a change to gameplay framing. The after-only hero views at design (-31,-31) and (30,5) assert the actual hero edge margin and use the existing production follow width with HUD hidden.

The budget file's texture line describes the unchanged FoundryShortBoiler resource, not a new asset belonging to this package. The proof started on October 9 and verification continued after midnight on October 10.
