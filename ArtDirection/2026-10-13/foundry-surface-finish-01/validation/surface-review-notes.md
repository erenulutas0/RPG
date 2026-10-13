# Independent material review - 2026-10-13

Baseline: 525d759, foundry-stone-craft-01. Read-only review of the bound materials and actual full-map, court, pit, gate, and approach-0-0 images against foundry-layout-01/foundry-v1.png.

## Main finding

The geometry is now doing useful work: substantial gate bases, broader varied paving, and a strong pit boundary. The remaining mismatch is not solved by adding another object. The floor still reads as evenly peppered concrete beneath geometric joint lines; the reference floor reads as broad dressed stone with restrained mineral and wear variation. The current photographic aggregate competes with the painterly door, background and character materials.

MAT_FoundryPaving is bound to FoundryPavingV5 (c926e4cc19bf4ad28b54285a48d4935d), material multiplier 0.8. Its dense pale aggregate and small black pits remain visually uniform across a large area. Existing geometry already supplies stone contours, joints, selected worn edges, and world-space reflected warmth. A replacement should supply grain and broad mineral variation only.

MAT_FoundryMasonry is bound to FoundryMasonryV4 (4293c349ead2486cb02d69a42a9747dd), a five-course block atlas, not an unstructured grain image. Replacing it with a seamless floor grain image would erase authored masonry and break callers sampling specific atlas regions. Any masonry treatment must retain the original course/edge ownership or use a separately scoped face sampler.

## Quarry texture acceptance

1. Orthographic, seamless continuous material; no slab edges, mortar grid, patterned cracks, diamond shapes, symbols or borders. Geometry owns these.
2. Matte quarried slate, visibly mineral rather than smooth plastic. Broad softly changing mineral masses should survive actual game downsampling; pores and short flakes stay sparse and subordinate.
3. No bright salt-and-pepper aggregate, wet patches, specular highlights, metallic streaks, baked top light, black occlusion halo, orange fire or vignette. Existing vertex shading owns directional and forge light.
4. Neutral charcoal slate with restrained cool grey and slightly warm quarry variation. Avoid saturating the current mauve/purple floor and avoid bleaching the court into pale grey.
5. No obvious repeating landmark when mapped at the existing 5.7 grain span. The actual design-space repeat period is about 4.03 units, so each large 3 by 2 slab samples a substantial portion of a repeat.
6. At portrait capture, the blue hero, small mite, chest, health bars and pale attack circle must remain more salient than material texture. Existing joint and brass hierarchy must remain readable.
7. At full-map capture, improvement should be a calmer, richer stone field. It is not enough for the bitmap to look impressive at native resolution while disappearing or becoming noise in the scene.
8. Pit warmth should remain local; gate and wall returns should retain distinct face values. Texture should not introduce false lighting.

## Other current material clashes

- Gate: the detailed painted door has richer edge work than surrounding simple dark crowns and monolithic returns. A coarse material adjustment alone cannot make the upper silhouette match the reference, but lowering photographic noise and establishing coherent stone grain is a worthwhile next step.
- Pit: stone lining already reads more naturally than large floor faces. Keep it as a consistency target rather than globally brightening it. The fluorescent molten edge is conspicuous but outside a quarry-grain edit.
- Metal: large furnace caps and connecting pipes still have broad clean shading and visible faceting, while the painted furnace fronts are richer. If this package includes metal finish, restrict it to material variation on those real native faces rather than adding surface ridges everywhere.
- Whole-map actors are tiny because that capture is a review overview. Actual portrait actors are readable; do not change camera to compensate for an overview image.

## Suggested acceptance images

Compare matched before/after full-map, court-detail, shaft-detail, gate-detail, approach-0-0, approach--8--12 and bridge-detail at identical camera/brightness. View the texture repeated as imported if a seam-free claim is made. Reject a candidate that only adds contrast or that recreates the old aggregate grain at a larger scale.

No Assets, docs, Unity, ADB, or git changes performed by this reviewer.
