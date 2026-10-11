# Independent actual-image art review

Date: 2026-10-11. Reviewer: gate_actual_art_review. Final outcome: accepted after the second capture for the bounded gate-and-paving package. This review covers root-generated Unity images, not a phone session or a performance test.

## Reference and comparison scope

- Reference: `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`.
- Matched baseline: `ArtDirection/2026-10-11/foundry-industrial-01/after/`, copied into this package's `before/` directory with provenance recorded separately.
- Reviewed new views: `gate-detail`, `full-map`, `overview`, `gate-new-22`, `gate-new-26`, `approach-20-1`, `north-furnaces`, and `south-retorts`. Gate/detail/map/portrait views were reviewed again after the surface adjustment; furnace contacts were inspected in the first capture with the same architecture.
- Acceptance criteria: one coherent gate silhouette; seated masonry and pipe contacts; an actual readable paving improvement without a checkerboard or carpet; clear hero and feet in portraits; no obvious new foreground contact or cutaway defect.

## First capture: architecture accepted, new paving not accepted yet

The reduced outer towers and flatter upper parapet make the gate read as a broad, connected structure. The two outer masses no longer dominate the doorway as strongly as in the baseline. Masonry seats on the facade rather than appearing suspended, and the original emblem, blue banners and warm torch focus remain clear. The lower continuous cheek coping reads more coherently than the previous stepped fragment stack.

The north furnace and south retort views show the service pipes still meeting the gate-side masonry and retaining their furnace connections. No new visible detached support or floating pipe end was found. At the two gate portraits and approach view, the hero's blue silhouette, weapon and both feet remain readable against the floor. The changed architecture does not introduce an obvious new foreground overlap at those inspected positions.

The intended new four-course processional paving was not visually established in the first capture. The same oversized irregular shapes remained the dominant joints around the panel, at matching locations in the baseline and after views. Pixel comparison of three interior crops found low-amplitude differences, so the images were not byte-identical, but those differences did not demonstrate the intended new slab bond. Root was notified to investigate surface visibility before claiming this part complete. The later source diagnosis distinguished the retained octagonal border from the new, overly faint interior courses: this was an insufficient visual contrast result, not proof that the new geometry was absent. First-pass `gate-detail.png`, `gate-new-26.png`, graphics XML and the budget record remain under `validation/first-floor-pass/`.

## Second capture: combined package accepted

The adjusted joints, mortar bed and narrow bevel now make four staggered courses visible beneath the emblem in `gate-detail`, both gate portraits and `approach-20-1`. They read as assembled, flush stone. Their dark joints and restrained face variation do not create a bright checkerboard, luminous floor target or raised carpet. The original octagonal dressed border remains and the service-edge courses connect visually to the approach. No obvious new gap or height jump appears where the panel meets the surrounding floor in the inspected stills.

The hero's silhouette, sword and both feet remain clear. The brass emblem remains separate from the blue gameplay ring and stronger than the restrained paving. The second `full-map` and `overview` retain the broader gate hierarchy without introducing a new distracting floor patch. At those large views the benefit is chiefly the lower, connected gate silhouette; the paving improvement is chiefly visible at close and portrait scale.

The final images therefore support accepting this specific gate-and-stone package. They do not support a claim that the reference map is finished. Region metadata records 32 renderers and 129,812 vertices in the final capture; those are source/render-budget observations, not measured device performance.

## Limits and remaining reference gap

Even with the accepted architecture change, the gate remains simpler and more regular than the reference. The reference has denser small-scale stone articulation, supported recesses, weathered transitions and integrated industrial detail. This package should not be described as full reference fidelity. HUD is hidden in these proof views; static images cannot certify HUD interaction, motion shimmer, crowded play, device cutaway continuity or frame time.
