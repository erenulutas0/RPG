# Courtyard stone finish — 2026-09-29

## Intent

The preceding full-map and S23 furnace views showed strong, almost black outlines around every slab. Those outlines and the bright/dark bevel pair made the floor compete with actors and read as a uniform manufactured grid. The existing basalt texture and slab layout are retained, with a quieter surface treatment.

## Changes

- Joint inset narrows from 0.025 to 0.016 design units; underlying grout vertex value increases from 0.32 to 0.49.
- Bevel inset narrows from 0.045 to 0.035. Directional multipliers change from 1.12 / 0.68 to 1.035 / 0.84, making the stone read flatter and less glossy.
- A deterministic integer hash gives slabs a restrained 0.90–1.09 value range. It uses no random state.
- Smooth, broad vertex-colour patina spans slab boundaries at approximately five design units per lattice interval. Its 0.91–1.025 multiplier adds quiet uneven wear without cracks, decals, new obstacles or extra geometry.
- Furnace bounce radius broadens from 5.5 to 6.4 design units, with reduced peak source contribution. The court's cool bias is slightly more neutral. The effect remains authored static colour, not a physical light or an animated glow.

## Scope and verification

Only `Assets/_Project/Scripts/UI/Arena/FoundryRegionFinish.cs` was edited for this slice. No source texture, UV mapping, material asset, scene, collision or gameplay change. Existing bridge shading also receives the common patina/light calculation. Quad and vertex emission counts are unchanged by construction; this is not a measured frame-time or allocation result. Calculations occur while the region is built.

Reviewed the existing `foundry-atmosphere-01/after.png` and `device-plume.png` before implementation. Parent integration owns the new Unity renders, full tests and Android comparison. This slice has not independently run Unity, dotnet or ADB. Final visual acceptance should check that the courtyard still separates hero/enemy silhouettes and that warm furnace floor colour remains restrained at phone size.

No claim of final reference-quality stone, physically based lighting or new painted assets is made.
