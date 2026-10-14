# Independent baseline review — gate cohesion

Baseline `3474e35`, reviewed read-only on 2026-10-14. Reopened the previous package's native gate detail, crown detail, full map and approach portrait, the approved `foundry-v1.png`, and the current masonry atlas/source. This is planning advice, not candidate acceptance. No Assets, Unity, phone or Git operations were performed.

## Strongest mismatch

The new outer bays have quiet dark stone, broad bevel light and convincing deep niches. Adjacent native inner piers and crown still show much coarser cloudy mineral variation. The inner jamb returns are comparatively thin, and the curtain/header surfaces have less consistent bearing construction. These three families sit directly against the detailed painted portal, making their different grain and edge treatment apparent. A further blanket brightness change would not resolve that difference.

The approved reference supports broad dark masses with restrained exposed arrises, a small number of readable construction breaks, and localized fire reflection. More equally bright trim, thinner courses or dense chisel noise would take this candidate away from that hierarchy. Native parts need their construction and grain to agree before additional ornament.

## Recommended bounded root treatment

1. Keep the existing texture/material/ranks and shared `GateRecessGrain` unchanged. It is used outside the gate, including distant works. Add a gate-local finishing path for the active inner-pier/header assembly and, if needed, the active curtain paths chosen by the enclosure helper. Avoid a change to every generic `LitFoundryTier` or every fallback body.
2. Reuse the established mortar-free atlas interior at a consistent physical grain scale, approximately the existing `.075` fine-stone density, across complete parent surfaces. Apply the field once after the final construction geometry. Do not restart it per block or thin bevel; that would repeat the same stain and restore the patchwork problem. Preserve the inherited face illumination, contact wash and mortar/stone value distinction rather than adding another global gain.
3. If a whole face exceeds the atlas interior, an ordinary UV affine remap cannot keep both fixed scale and atlas safety. Reusing `StoneDetailGrain` requires real fold subdivision, preserving original triangle order and interpolated colours. That has a real startup/geometry cost, despite no new texture or renderer. Use it only at the necessary complete-parent call sites and measure the merged result. Small faces that fit can remain affine. Watch for mirrored mineral motifs and excess fine grain after mip sampling.

The two geometry helpers should address broad returns and joined curtain supports within the current envelopes. Their contribution should remain visually apparent when grain is quiet: actual depth and bearing changes, not simply another set of thin seam lines. Root should avoid an additional fourth construction change before seeing these three together.

## Acceptance criteria

- In matched gate/crown details, inner piers, header and outer baked bays read as one dark stone family. Grain scale should stop changing conspicuously at their joins. Recesses remain darker than their surrounding face; the central door/emblem remains the focal object.
- Construction improvement is visible at the unchanged full-map scale without making the gate brighter than the workshop/flame hierarchy. Do not describe a close-up-only change as a large full-map improvement.
- The approach portrait crops the crown and most outer towers. Inspect its lower inner-pier/landing contacts and actor readability, and report this framing limitation honestly. Do not change the camera merely to demonstrate the candidate.
- Original footing, crown envelope, door/banner clearance, recess aperture, single surface ownership, pipe ordering and cutaway/depth rules remain intact. No duplicate base surfaces beneath partitioned geometry; no equal-rank material tie is reintroduced.
- Tests of UV continuity should cover actual emitted geometry and shared stations. If the contract becomes piecewise affine, replace only the obsolete global-affine oracle with a physical-field/fold oracle; preserve geometry tolerances, aperture and overlap checks. A helper calling itself as the sole expected-value oracle is insufficient.
- Baseline is 36 renderers / 164672 region vertices, including 3572 distant vertices; global ceilings are 174000 / 3600. No additional runtime texture or batch is planned. Record any subdivision cost, not a claim of free performance.

## Remaining boundary

This scope cannot by itself produce reference parity: the overall map still has sparser workshop grouping, regular paving and broad warm floor bands. Latest gate-depth APK is built but not installed. Device motion, HUD, shimmer, crowd behaviour and midrange performance remain unverified; the prior current-device/save/telemetry boundary still applies. Image and source acceptance must follow the actual merged capture, separately from parent-owned full tests/build.
