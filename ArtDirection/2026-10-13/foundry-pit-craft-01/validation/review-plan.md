# Pit craft - independent review plan, 2026-10-13

Scope: read-only art-direction review against the approved foundry-layout-01/foundry-v1.png reference and actual foundry-surface-finish-01 After images: shaft-detail, court-detail, full-map. The latest surface-finish and stone-craft sections of docs/38_HANDOFF_2026-09-30_ART.md and the surface-finish README were read. Relevant existing PitGuard, PitHoods and PitSurface entry points were located. No Assets, Unity, ADB or git changes were made.

## Gain 1: make the guard read as fitted stone

The current stone guard already has a useful weight and visible openings. Its top and front bands still carry repeating diagonal/bright patches and dense little texture resets that can read as a narrow polished metal rail. This clashes with the quieter V6 floor and the reference's broad masonry coping.

Keep the curved boundary, original height, posts, sill, negative openings and actual depth. The highest-impact treatment is broad stone stock with purposeful end joints, a quiet upper arris and subdued mineral surface. The underside and the recessed bays should stay darker than the top. Local pool-facing returns can retain weak reflected warmth; the court-facing upper boundary remains cool. Do not add a new row of ornaments, fasteners, or bright wear to every block.

The proposed implementation from gate_surface is appropriate: remove repeated small-segment coping texture restarts, emit along curve chords with roughly 1.2-unit fitted pieces, partition the original face into stone/joint/arris, and use selected rather than universal edge abrasion. Actual rendered proof must establish that these cues survive without creating a bright dashed outline.

## Gain 2: give both existing hoods coherent cast-iron surfaces

In shaft-detail the two upper vessel hoods read as clean blue-green polygon stripes against richer warm painted vessel fronts. Their existing ribs and collars are enough physical construction. Added fasteners or extra ridges would not solve the surface mismatch.

Give the broad plates a restrained dark cast-iron value with low-frequency oxidation and grain variation. The seams should be subordinate to the overall shape and the collars should read as seated, with contact at the shell rather than disconnected rings. Preserve the two existing profile identities, mouths and sightline to the furnace shaft. The cool upper body should receive only limited material variation; warmth belongs mainly to lower or pool-facing surfaces. Do not bake an orange stripe into every plate or globally lift all hood brightness.

## Acceptance cues

- shaft-detail: broad guard cap and front read as worked stone, not streaked metal; cap joints and wear are limited, not an every-segment pattern.
- shaft-detail: actual guard openings remain visible, posts remain seated, and there is no new near/far overlap, glowing slit or floating cap.
- shaft-detail: the two hood shells read as coherent cast iron and remain distinguishable; broad material variation is visible without obvious repeated stripes, compressed UV rows or glitter.
- court-detail and full-map: the pit remains one readable landmark. Guard treatment must not become a bright outline around it, and hood surface changes must not steal attention from the pit depth/fire or the combat figures.
- portrait approach near pit: hero, enemies, chest and attack guide remain more salient than stone and metal microdetail.
- gate-detail/other machinery comparison if shared helpers change: no unintended value or atlas-contract change to unrelated native machinery.

Reject only observed defects in the actual paired images, not hypothetical demand for more detail. Root should capture a fresh untouched Before and compare the same views after source freeze.

## Limits and next gaps

This package will not close the larger difference in atmospheric richness and architectural craft between the whole-map render and the concept. Current map geometry is visibly simpler and its floor intentionally quiet for combat. Static acceptance does not verify phone shimmer, compression in motion, frame time, or interactive readability.

Advice was sent to gate_surface and material_bindings before implementation. No demand for global exposure changes or new scene clutter is part of this review.
