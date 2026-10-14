# Final source scope

This package groups three changes to the existing closed gate and its stone architecture.

## Profiled header stone

The continuous lintel now has a 0.09-unit recessed middle fascia between connected sloping returns. Directional vertex tones describe the carved depth in the existing unlit renderer. The three visible faces share their octagonal stations and one mineral field per complete face. The original cap, coping, outer bearing stations, corbels, door and iron contacts remain in place. The shared `GateCraftTier` behaviour is preserved; only the first lintel in `BuildGateHeaderFinish` uses the new profile.

## Fitted enclosure coping

Dressed curtain, tower-capital, jamb-belt and landing-plinth tiers use broad fitted stones with joints registered across their fascia and bearing caps. The original visible surfaces are partitioned once, replacing the former complete fascia beneath overlaid stone faces. Coplanar arrises retain the existing bearing plane. One bounded mineral field spans each complete tier. New vertices sample the original lit shell, with neutral joint and edge gains; non-dressed wall ashlar retains its existing path.

## Clearer closed-door threshold

The procedural rail excludes its central units at the landing's closed-portal edge. The existing door facade physically backs that interval; removing the visual fence exposes the door sill and its approach. Both explicit brazier supports and the outer rail returns remain. The movement boundary, collision/navigation geometry and closed-door behaviour are unchanged. This is a presentation opening, not a newly traversable route or a boss transition.

## Runtime cost and preserved contracts

The final actual scene capture reports 34 region renderers and 163,073 vertices, versus 34 renderers and 162,361 vertices before: +712 static vertices for the combined package. Distant geometry remains 3,572 vertices. No new texture, material, renderer, shader, component, owned resource type or per-frame updater is added. The earlier cast-iron material arrangement remains unchanged.

Map boundaries, camera framing, actors, combat, encounters, progression, saves, movement and collision contracts are unchanged. The existing region vertex budget is retained. These resource counts are not device frame-time measurements.

## Evidence boundary

Twenty-one matched Before and twenty-one matched After views were captured in the Android-target Unity Editor with the same camera and actor setup, paused combat and hidden HUD. The new gate-crown detail view is present in both phases. The final graphics test passed, including the actual merged-scene threshold presentation oracle. Floor ownership probes remain in the capture fixture.

Focused new tests inspect header depth and surface ownership, exact bearing/cap retention, continuous mineral UVs and rendered facet contrast; enclosure tests inspect fitted surface coverage, registered joints and original-light inheritance. Existing PlayMode coverage now checks the physical door backing, clear presentation interval, threshold paving ownership and absence of regenerated centre posts. Full suite/build results belong to the package verification record and must be established before commit acceptance.

No ADB operation, phone installation, save/telemetry operation or physical-device review was performed for this package. Reference fidelity, device motion/shimmer/crowd review and midrange performance remain unfinished. This package improves the existing gate's stone construction and threshold legibility; it does not complete the approved reference.
