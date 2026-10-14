# Independent baseline art review

Reviewed 2026-10-14, before the grouped scene-cohesion changes. This is planning evidence, not acceptance of an implementation.

## Inputs inspected

- Latest sections of `docs/38_HANDOFF_2026-09-30_ART.md` and `docs/08_ART_AI_PIPELINE.md`.
- `foundry-gate-cohesion-01/validation/next-visual-priorities.md`.
- Actual final gate-cohesion captures: full-map, court-detail, forecourt-detail, approach-20-1, yard-detail and stock-detail.
- Approved `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`.
- Current paving/light sources, including FoundryRegionFinish, FoundryRegionPavingWear, FoundryRegionCourtSurface and FoundryGround; original FoundryPavingV7 pixels inspected read-only.

## Visible baseline findings

1. The pit and gate-apron illumination forms a broad connected amber ribbon. It attracts more area and attention than the individual fire sources. The approved reference has isolated warm contacts and a quieter cool court, especially beside the pit. Reducing broad shoulders is a larger scene-level opportunity than more gate trim.
2. The large processional and gate flags carry long diagonal mineral streaks. This is clearest in approach-20-1, where the stone looks stratified over multiple slabs, and in the native-size court/forecourt crops. Several low-frequency colour fields also modulate the stone, creating a cloudy wash. The joints are already sufficiently legible; adding dark joints, cracks or randomly contrasting flags would make combat noisier.
3. The workyard equipment is still separated and bare between its existing functional groups. Supported stock grouping can improve purpose and scale without occupying walking lanes. The near faces likewise remain more regular and simple than the reference. These are useful parallel scopes, but neither should consume the floor/readability budget.

## Concrete runtime recommendations to root

Keep the static floor-light architecture and existing world-continuous UVs. ForgeLamps also feeds architecture and metal lighting; a floor-specific falloff treatment is safer than globally changing all shared source radii.

As first capture hypotheses, narrow the floor pit radius from 3.8 toward 2.2–2.5 design units and the boiler floor spill from 4.2–4.6 toward 3.0–3.4, preserving the stronger tall-furnace fronts and a small credible collar glow. Point lantern pools could end nearer 2.7–3.0 while retaining their local warmth. Do not change CoolSlate/CourtAmbient or lower the whole court to achieve the effect. Avoid a hard cutoff, isolated bright discs or orange bands at every rail segment. Source strength and final actual-image response should determine the accepted values; these ranges are not asserted as tested settings.

V7 is visibly directional on its own, so vertex-colour changes alone cannot remove the long striation. Trial the common PavingGrainSpan at about 4.1–4.4 instead of 5.7, keeping PavingVertex and LitFloorQuad on the same mapping. This should put more of the existing mineral texture inside each flag rather than make a long shared wave. It requires actual close and portrait review for busy grain or repetition; it does not establish motion/shimmer acceptance. Avoid per-facet UV restart, arbitrary slab rotations or new seams.

Reduce the broad FloorLight patina amplitude from .91–1.025 toward .96–1.015 before adding more material variation. Keep the identity-sampled PavingMineralFamily restrained. High-contrast random stone values would reproduce a checkerboard; more low-frequency washes would reproduce the cloudy appearance. No raster asset edit is recommended in this scope.

## Acceptance criteria

- Full-map visibly separates the bright fire sources from their quieter surrounding floor; the pit no longer dominates as a continuous amber border.
- Court and forecourt retain the current blue hero, violet golem, chest and brass-route separation. Changes to the protected combat centre should be minimal.
- Approach-20-1 shows shorter, less dominant grain and distinct local warm contacts, without a new dense repeated texture pattern or obviously lighter entire landing.
- Yard, stock and casting reads as coherent supported workstations, with unchanged clear lanes and no floating stock or black contact plates that resemble new obstacles.
- Near-island fronts preserve the entrance aperture, support contacts and silhouettes; no false dark cutouts, new overpaint or bright outline bands.
- No new grain discontinuity at flag edges, grade breaks, shared court/bridge emitters or terminal surfaces.

Current reported baseline is 36 renderers / 169119 region vertices, including 3572 distant vertices. Ceilings remain 174000 / 3600. A tighter floor falloff may cause AshlarLightStep to request a half-unit lattice for more stones, so actual geometry must be measured even if the source adds no explicit new mesh primitive. Resource counts do not establish frame time or phone performance. Native static captures do not establish motion/HUD/shimmer acceptance.
