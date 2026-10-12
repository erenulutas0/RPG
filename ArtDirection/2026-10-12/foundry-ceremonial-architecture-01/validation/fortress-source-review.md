# Fortress and bridge hierarchy source slice

Baseline: `7c0f057`. Source completed and Assets frozen for root-owned verification. No Unity, ADB, Git, compile, or device operations performed by this helper.

## Files and behavior

- `FoundryRegionUndercroft.cs`: the production arch mesh now distinguishes each third bay as the primary dressed masonry order. Secondary bays have shallow projected arch reveals inside the existing curtain field, rather than the same prominent complete ring. Major responds retain their original outside taper, feet and shoulders but devote most of the face to three broad stone courses. Secondary responds receive one projected recessed panel. Socket emission is extracted unchanged; physical major/terminal metadata, screened-pier rules and selected south socket remain unchanged.
- `FoundryRegionBridgeSupports.cs`: all three support stations, complete aperture, return depth, outer limits and deck contacts remain. The mouth bent is the primary order; farther bents use projected reveal faces and recessed leg panels. A broad primary bearing course occupies -3.03..-2.7. The arch shoulders and spandrels terminate at -3.03, so the new bearing partitions the parent front rather than being painted over the old upper spandrel.
- `FoundryRegionFortressSurface.cs`: local `FortressRecessedRespond` partitions a tapered parent into four broad frame faces, four shaded returns and a coursed backing. These are projected 2D mesh surfaces; no literal 3D extrusion, collider or new depth owner is claimed. Existing bounded mineral UV mapping is reused.

No Massing, floor, gate, map, navigation, scene, material, texture or runtime updater changes. All mesh output uses existing batches and ranks. Major chain socket corners and hanging-chain selection remain governed by the prior guards.

## New verification source

`FoundryFortressHierarchyTests.cs` plus metadata (`cecf7652cf91484cad36b4ab33f6d137`, 32 hexadecimal characters) adds five EditMode cases using reflection into the actual production helpers:

1. Tapered projected panel stays inside its parent and covers sampled points without holes.
2. Major respond fits the independent original front/bevel/shoulder union and retains all baseline contacts plus stone backing behind the four socket corners.
3. Secondary respond fits and covers the same original envelope family.
4. Primary bridge bent retains the baseline eleven-segment intrados, exact upper deck line and no face crossing the replacement bearing partition.
5. Secondary bridge bent retains the same physical aperture and deck line.

All cases inspect emitted vertices, triangle indices, alpha and bounded local mineral UVs. Existing guards were not weakened or removed. Pending: root compilation/static checks, complete EditMode/PlayMode suites, actual matched-capture acceptance and resulting mesh/resource budget. Actual images must show a clearer broad support hierarchy; source correctness alone is not art acceptance.
