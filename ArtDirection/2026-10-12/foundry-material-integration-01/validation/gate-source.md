# Gate material integration source review

2026-10-12. Implemented by the gate material helper. Root owns Unity validation and actual-image acceptance.

## Scope and observed problem

Read docs/06, docs/08 and the latest docs/38 handoff. Inspected the approved `2026-09-23/foundry-layout-01/foundry-v1.png` and the fresh baseline `before/gate-detail.png`.

The four shafts already had genuine blind recesses, but their backing read almost black and their tall surrounds stretched a small square grain patch. All new shaft surfaces also inherited a single top-corner light sample, extending the coping contact shade down their entire height. The painted door remained richer than those native stone surrounds.

Changed runtime file: `Assets/_Project/Scripts/UI/Arena/FoundryRegionGateRecesses.cs` only.

## Material treatment

- Each dressed stone maps a long, joint-free portion of the existing masonry texture using projected physical edge lengths. One common mapping covers its face and four bevels. The same physical grain scale is preferred for thin returns; large faces scale down uniformly to fit the known patch, without clamping away local grain.
- The backing's five triangles share one continuous mapping. Their material is darker toward the niche head and slightly cooler than the surround, while the sill side receives restrained reflected light. The darkest return remains distinct from the face.
- Generated surface colors interpolate the original shaft's four `LitFoundryTier` corner colors instead of using only its upper corner. This restores the existing floor-relative forge-light variation; it does not add a new light source or brighten the whole gate.
- Grain samples stay in UV x approximately .035-.398, y .438-.567 of FoundryMasonryV4's broad middle-row stone. Small per-stone origin offsets reuse that interior without a repeated painted mortar line.

## Cost and preserved contracts

No mesh positions, triangle indices, primitive emission order, material, texture, renderer, resource ownership or update loop is added or removed. The exact prior geometry remains 516 vertices per complete recessed shaft: two 240-vertex faces plus the retained 36-vertex chamfer/cap. Existing 550-vertex guards are unchanged. Work is limited to build-time UV/color calculation over those existing vertices.

The same wall footprints, silhouette, tapered prism, backing depth, pointed openings, frame contacts, door, header, banners, lock, fire and pipes remain. Existing projected coverage, open-throat and closed-corner/cap regression tests should therefore exercise identical geometry; no test oracle was weakened or new constant-mirroring test added.

## Validation ownership and limits

Assets were frozen and handed to root for isolated compilation, actual Unity captures and the complete required suite/build gates. This helper did not run Unity, ADB, Git or the phone. Source reasoning is not visual acceptance or performance evidence.

Inspect the actual close and portrait images for lighter backs still reading as recesses, visible texture grain without striped seams, consistent dressed surround/door values and clear actor space. The projected mapping is deliberately local to these static faces; it is not a global world-space stone shader. Existing cap/chamfer material is retained, so full stone-material and reference parity are not claimed.
