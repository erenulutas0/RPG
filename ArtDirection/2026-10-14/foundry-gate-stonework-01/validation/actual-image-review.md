# Independent actual-image review

Date: 2026-10-14. Reviewer: gate_stonework_review.

## Evidence

I opened and visually compared these four matched Before/After pairs from this package: `gate-detail.png`, `gate-crown-detail.png`, `approach-20-1.png` and `full-map.png`. I also reopened the approved `../../2026-09-23/foundry-layout-01/foundry-v1.png` reference. These are actual Android-target Editor stills with the staging described by the capture record; they are not phone, HUD or live-combat evidence.

## Decision

Accept the final repaired-runtime captures visually. The first full EditMode run found two niche backing-overpaint failures at approximately 2e-7 area. Those failures were addressed by a bounded runtime split-edge interpolation correction, with the original strict area assertions unchanged. The targeted Unity recess suite then passed all 24 cases. I independently reviewed that correction and reopened the fresh final gate detail, crown detail, portrait approach and full-map captures after root's final recapture.

No visible crack, halo, seam or loss of niche depth appears after the correction. The findings below still apply to the final output. This is local visual acceptance; full regression/build success, device acceptance and complete reference fidelity require their own evidence. See `repair-peer-review.md` for the correction review.

The tall jambs now show a small number of fitted courses. In gate detail, the previously continuous vertical stone mass separates into broad constructed pieces without becoming a small-brick grid. The seams are restrained and remain subordinate to the door emblem, banners and fire. The pointed recesses still have a dark backing, visible sill and receding side/head planes. The added courses do not flatten the openings or produce distracting repeated horizontal stripes.

The crown view retains the prior cap and lintel silhouette. The new pointed-head joints are visible when inspected closely, while the large outer shoulder bearings still read as substantial stone. I see no newly detached cap, open join or luminous outline. Curtain stone is calmer than the finer previous block field, with less repeated per-block texture treatment; its exposed area is limited, so this improvement is secondary rather than a dramatic overall change.

The lower-buttress material change is subtle. I see no broad exposure shift or additional black band at the foot. Existing contact relationships and the gate threshold remain stable. The portrait approach retains the same clear hero silhouette, ability ring, floor cue, threshold and supported side braziers. Most upper stonework is outside that portrait crop, so it should not be used as the primary proof of the new jamb courses.

The full-map pair retains the same composition, dimensions and dominant landmarks. The gate work is barely perceptible at this distance, which is appropriate for restrained stone detail but limits how much this package closes the overall reference gap.

## Remaining limitations

The reference still has richer large-scale architectural variation, more convincing material transitions and a denser relationship between machinery, masonry and the surrounding void. The runtime court still reads comparatively broad, sparse and smooth. The gate's painted central facade and procedural flanks also retain different levels of surface detail. More fine seams alone will not solve those larger differences.

Static images cannot verify shimmer, moving occlusion, crowded combat readability or device frame time. The reported reduction in static vertices is a resource count, not a measured phone performance improvement. No device or gameplay acceptance is claimed here.
