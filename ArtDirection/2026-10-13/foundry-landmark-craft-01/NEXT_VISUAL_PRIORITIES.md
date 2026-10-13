# Proposal: next visual priorities

Read-only art assessment, 2026-10-14. These are proposed tasks, not completed scope or phone acceptance.

Reviewed the final `after/full-map.png`, `gate-detail.png`, `approach-0-0.png`, `approach-20-1.png`, `approach--20-6.png`, `approach--29--29.png` and `approach--8--12.png` against `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`.

The major entrance, court, pit, workyard and gate relationships are now recognizable. The remaining difference is substantially source-art quality and material cohesion. The reference has shaped stone and metal surfaces with deliberate edges, deposits and reflected colour; the current native surfaces often read as smooth polygons with a shared grain laid across them. More tiny trim will not close that difference. The painted door, statues and boilers already demonstrate a richer surface treatment than nearby native stone and machinery.

## 1. Establish a better foreground stone surface benchmark

**Largest screen-area payoff; requires a source-art study.** Ordinary gameplay portraits contain much more paving than gate architecture. The current V6 floor is calm and actors remain readable, but its broad cloudy grain does not yet carry the stone character of the reference at this scale.

Produce a small A/B study of a deliberately authored, jointless slate diffuse with clearer mineral structure and restrained worn material, retaining V6 as the control. Start with one candidate in the actual central-court and bridge portraits. Retain the established stone footprints, geometry-owned joints, world UVs and light. Do not bake a second paving grid, directional cast shadows or a complete reference image into the texture.

Acceptance: a clear material improvement at normal portrait size without pale/dark checkerboards, repetitive scratches, crawling grain or weaker actor contrast. If the candidate is only distinguishable in an enlarged crop, keep V6 and stop tuning it. Existing tint changes alone are unlikely to supply this missing painted surface quality.

## 2. Bring the visible native machinery up to the painted prop family

**High local payoff; source art first, bounded material integration second.** `approach--20-6.png` places a formed pit hood and loaded trolley beside the hero. Their broad surfaces still look considerably simpler than the painted boiler fronts and gate. This contrast is visible during play; extra distant hardware is not.

Study one coherent cast-iron finish for the hood body/collar and trolley frame: broad cool metal planes, seated dark contacts, selective worked edges and restrained warmth on furnace-facing surfaces. Preserve their current shapes and useful negative spaces. An authored surface/atlas candidate may improve this more than another ring of rivets or another set of facets. Keep the existing material/batch contract where practical, but document any new source art and UV migration honestly. Do not repaint the complete prop as a flat card merely to hide its geometry.

Acceptance: the hood and cart belong to the same visual world as the existing painted props in the actual workyard portrait, while remaining subordinate to the hero. This is a source-quality target, not a promise that scalar shading alone can reproduce the reference.

## 3. Improve broad masonry visible beside the gate approach

**Feasible with current mesh/material tools; lower priority than the source studies.** Work on the lower piers and large sloping side shoulders visible in `approach-20-1.png`. Their broad plain planes still contrast with the detailed door. A few substantial fitted stones, coherent bearing joints and restrained directional face values can make these surfaces read as constructed masonry. Replace their current faces rather than append decorative layers. Preserve ramp clearance, exterior bounds and current camera.

Acceptance must use the gameplay approach portrait. The tops of the gate towers are largely cropped there, so further crown ornaments or minute niche dressing would mostly improve the detached close-up, not normal play.

## Deliberately defer

- More bolts, fine scratches, hairline cracks, repeated small chips or tiny distant windows.
- More global grime, brightness or saturation changes; the current readable floor and localized heat should remain the control.
- Additional gate micro-detail justified only by `gate-detail.png`, or another whole-map reshaping pass without a new composition problem.
- Claims of reference parity based on the full-map proof alone. The concept is a distant composed view; gameplay is a close portrait. Each proposed improvement needs matched gameplay evidence, then device motion/HUD/shimmer review when available.

Recommended next package: one controlled stone source study and one machinery source study, with the narrow masonry task only if its improvement is visible in the existing gameplay crop. Do not replace source-art work with another sequence of almost invisible numeric adjustments.
