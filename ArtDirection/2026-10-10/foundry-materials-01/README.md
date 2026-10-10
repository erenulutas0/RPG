# Foundry materials package

## Intent

Continue toward the approved reference's material richness within the established map. No layout, camera, movement or gameplay change belongs to this package. The concept remains a visual target, not a runtime screenshot or a claim of completed fidelity.

## Authored stock hoist

`Tools/ArtReview/BuildBlenderStockHoist.py` authors the current gantry in Blender with the existing study02 lighting and materials. Editable source, transparent 1024 render and projection metadata live in `../stock-hoist-01/`. Rounded bearing housings, continuous flanged beams, wound lifting cable, actual chain links and subtle bronze bearings replace the procedural striped surface treatment. The crosshead remains 3.44 high, with its original upright feet and shelf anchors. Existing tank, pipes, coils, tools and baked salvage remain separate.

`FoundryRegionBakedStockHoist.cs` uses one quad and one owned material. The texture imports at 512, ASTC6x6, mipmapped and non-readable using the established salvage profile. Calculated block payload is 158,432 bytes including mips, excluding Unity overhead. The card uses the original stock-shelf depth footprint with height3.6 as an explicit non-landmark owner. Sorting rank -11.5 maps to -9, behind existing stock at -8. The old geometry remains as a missing-resource fallback. Scene destruction releases the mesh, cutaway material variant and source material while retaining the shared texture resource.

Runtime projection: anchor stock-local(4.5,22.08), image bounds x[-2.7,2.7], y[-0.9,4.5]. Blender's height compensation matches the existing fixed isometric view. No image generation, post-render bitmap editing, additional runtime animation or new collider is used.

## Verification status

Blender authoring/render completed and the isolated image was visually reviewed. Source whitespace inspection completed. FoundryRegionTests gained assertions for texture import, one-card batch count, sorting, explicit depth ownership and cleanup; execution results belong to the coordinator's verification record. Matched captures and side-by-side portrait views belong to the gallery; they do not constitute physical-phone or timing evidence.

## Actual hoist visual review

Compared `before/stock-detail.png`, `after/stock-detail.png` and `after/full-map.png`. The dark rolled beam, rounded housings and wound cable replace the earlier parallel stripe treatment with coherent metal. Drum and crosshead remain legible in context, while the existing shelf contents remain visible. This is a material-quality improvement within the existing silhouette, not reference-level completion for the whole map.

The crane now correctly passes in front of portions of the hoist chain and yoke. Its foot is design(-31.5,13) and jib tip(-32,20), while the hoist is around(-21.5,22.08); the crane lies nearer the fixed isometric camera. The preceding shared-batch emission order drew the later hoist over the earlier crane. Promoting the new card would restore that misleading overlap and could cover existing foreground stock. Current rank is accepted: partial chain/yoke occlusion is the result of their established placement, rather than a missing mesh or an acceptance blocker. No Assets were changed during this visual review.

## Stone surfaces

The floor replaces broad flat dark chips with narrow five-segment fracture strips and smaller faceted corners. Sparse mineral grains stay inside selected breaks; shallow internal incisions occur on approximately1/24 eligible flags. The slab packer, mineral palette, complete mortar coverage and ramp/brass/seal/repair exclusions remain unchanged. Root removed the now-unused old edge-wear helper. Actual court, yard and portrait review found no visible gaps/brass spill or actor-contrast regression. The main gain is removing pasted-on polygon artifacts; fine grit is intentionally subtle at portrait scale.

Existing gate bodies now use finer staggered ashlar over a narrow mortar bed, with coherent darkening below belts, damp runs and mineral variation. Exact tapered corners, caps, recesses, quoins, pipes and painted doorway remain. The painted door retains the highest local detail. Matched gate-detail review shows clearer stone scale; the full-map change is subdued rather than a large silhouette change.

## Capture and combined cost

Run `pwsh -NoProfile -File Tools/ArtReview/Run-FoundryMaterialsProof.ps1 -Phase after` with Unity closed. Before was captured from7e0c860 before this package; the phase flag selects output only, never reconstructs historical source. Portrait captures use production camera width with staged actors/hidden HUD. Full-map is a separate wider camera.

Actual combined region:117,754 ->118,902vertices (+1,148),25 ->26renderers. The baked hoist replaces its procedural mesh with one card; wall/floor refinements add static geometry. New hoist texture512ASTC6x6/10mips/non-readable confirmed in actual Unity output. These counts do not establish frame-time performance. Budget short-boiler texture line is inherited and describes that unchanged asset.

Selected matched before/after overview, court, yard, gate, stock, workshop and central portrait, plus final full-map, are retained with verification files. Additional local diagnostic angles can be regenerated from the corresponding source. Reference-level material richness, physical-device acceptance and sustained/midrange performance remain unfinished.