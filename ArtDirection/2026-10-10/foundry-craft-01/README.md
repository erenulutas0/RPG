# Foundry craft package

## Intent and evidence

The owner asked to prioritize the approved foundry reference's visual quality before more gameplay work. This package develops the existing courtyard surfaces, gate and industrial stock station without expanding the walking contract. The reference remains a concept image, not a runtime screenshot.

Matched Unity captures live in `before/` and `after/`: overview, stock, workshop, gate and court details. The stock-detail camera targets design (-22,20), with an upward offset of 1 and orthographic size 5. These staged architecture images do not constitute live phone or frame-time evidence. See the integration owner's final validation record for tests, build and device results.

The gallery also places matched `approach-0-0.png` portrait captures side by side to make the courtyard's surface change assessable at phone proportions. No layout or gameplay behavior changes belong to this package.

## Stock loading gantry

`FoundryRegionStockHoist.cs`, called before the existing tank/stock/rack from `BuildWorkshop`, adds a coherent lifting assembly on the existing non-walkable stock shelf. It has a broad flanged crosshead, two uprights, diagonal knee braces, paired bearing housings, a wound drum and suspended lifting yoke. The original billets, coils, pipes, tank and tools remain; baked salvage and boilers are untouched by this slice.

Stock-local base bounds are u[2.05,6.95], v[22.10,22.40], inside the existing shelf u[2,7], v[18.5,22.4]. Rear upright feet start at v22.13, beyond the quenching tank's v22.10 end. Crosshead height is 3.44; a dedicated depth scope uses the existing shelf footprint and height 3.6, so upper parts cannot escape the old 1.6-high landmark's ownership. This is an explicit non-landmark owner, avoiding unintended ownership of unrelated geometry.

All pieces share existing FoundryIron/-11 geometry batches. No texture, new material, per-frame component, collision, reward or interaction is introduced. Painter order emits the gantry before existing foreground stock.

## Actual visual review

Reviewed the combined `after/stock-detail.png`, `after/overview.png` and matched before/after `approach-0-0.png`. The hoist's drum, bearing housings, knee braces and two chain drops read as a coherent lifting machine. Existing pipes, coils, tank and tools remain visible. No obvious painter-order slicing or intrusion into the court is visible in those views. The full-map silhouette gains a purposeful upper industrial shape without competing with the gate.

At portrait scale the courtyard's broad slab breakup is more orderly than before, while the hero, enemies and chest remain the stronger visual subjects. The visual delta is restrained rather than a complete style transformation. The gantry's procedural metal treatment is simpler than the baked props at close range; the overall reference's surface richness and industrial density remain unfinished. These findings accept the current visual slice, not reference-level completion.

## Status

Source, whitespace and actual capture review are complete for the stock-hoist slice. Automated verification completed:409 .NET,432 EditMode and151 PlayMode tests passed; Android built successfully. Device validation remains pending owner-run restart approval. See validation/verification.md for the full record. No physical-device acceptance or frame-rate measurement is asserted by this review.

## Court stone and gate masonry

The workyard now uses the mixed half-unit ashlar packer rather than forced one-unit squares. Occasional 2.5-by-1 slabs and a weaker matching-course preference interrupt repeated rows. Broad mineral families span neighbouring stones; contained chipped corners and selective worn edges stay flush. The seal retains regular unit flags, brass lines remain forced joints and the complete mortar base covers every floor point. Three regression cases exercise the production packer across deterministic seeds: exact non-overlapping coverage, zone boundaries, brass joints and varied workyard sizes.

Gate buttresses receive broad pointed blind recesses on both visible faces, matching curtain recesses and four large lower quoin courses. Dressed caps and belts sample continuous stone interiors instead of repeated brick rows. Relief placement follows existing tapered faces. Footprints, heights, doorway artwork, depth owners and passage remain unchanged. Actual gate-detail and full-map review found no visible relief spill or broken pipe connection. Mixed slabs improve yard-detail most; the central court delta is moderate. Fine stone grain and irregular edges still fall short of the concept.

## Reproduction and cost

Run `pwsh -NoProfile -File Tools/ArtReview/Run-FoundryCraftProof.ps1 -Phase after` with Unity closed. Phase selects an output directory; it does not reconstruct old source. Historical baseline was captured at a4bdc7d before these edits. Full-map camera is wider than the matched overview; portraits retain production camera width with staged actors and hidden HUD.

Region geometry changes from 113,622 to 117,754 vertices (+4,132, approximately 3.64%), retaining 25 renderers. Combined package counts are not per-slice attribution or frame-time measurements. No new texture or material. The inherited budget texture line describes unchanged FoundryShortBoiler.

Selected before/after overview, court, yard, gate, stock, workshop and central portrait images are retained in Git, along with the final full-map and verification files. Other locally generated diagnostic angles can be regenerated from the corresponding source state.