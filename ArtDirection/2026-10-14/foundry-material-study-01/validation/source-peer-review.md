# Material study source peer review

Read-only review by the paving/material helper during the root-owned Unity capture. No Assets, tests, runtime source, Unity, ADB or Git operations were changed or run for this review. This is source assessment, not a claim of passed runtime verification or final artistic acceptance.

## Findings to resolve in the root-owned scene test

1. **Scene ranks were asserted in the wrong coordinate system.** `FoundryRegionView.SceneryOrder` maps authored orders above -14 to `RoundToInt((order + 13) * 2) - 12`. The hood's -10.5 becomes -7, and the trolley's -11 becomes -8. At review time `FoundryRegionTests` expected -10/-11 and used `renderer.sortingOrder == -10` to choose the hood bounds. Both the expected pair and the bounds selector must be corrected. The selector would otherwise classify the actual hoods as trolley geometry before the rank assertion. Production rank code is unchanged.

2. **The existing contact check excludes the migrated trolley.** `AssertWorkIslandContacts` collected only meshes whose texture equals `FoundryIron.mainTexture`, then demanded real grounded skid corners for both islands. Island 0 now uses `FoundryCastIron`, so its actual skid is excluded. Include the two intended iron bindings, or choose the material by island, while retaining the exact corner, depth, floor coverage and UV1 ownership assertions. This is a material-filter test migration, not a reason to weaken contact tolerances.

Both findings were sent to root before capture completed. Root owns their correction and verification.

Optional useful resource coverage: verify loaded cast texture Clamp mode and Android ASTC 6x6 alongside the already checked loaded dimensions, mip presence, Trilinear filter and non-readability. The boiler-only ASTC assertion does not cover the new cast texture.

## Production source assessment

No additional production blocker found in the reviewed scope:

- `PlatformMaterialSet` adds one borrowed optional material with fallback to existing iron. The production asset supplies the new GUID. Only the formed pit hoods and island-0 trolley select it; mould bench and remaining machinery retain the original atlas.
- The original hood profiles, vertices, colours, triangles, openings and local grain field are untouched. `BuildFurnacePit` changes only the material selection on the real `FormedPitHood` path.
- The trolley's final `YardTrolleyGrain` modifies only U. It sees the full builder including the existing skid. Its affine XY field uses the same 0.19 UV/world-unit density as the hoods; equal projected points get equal samples. There is no clamp operation or triangle reordering in this pass.
- The new trolley cases inspect actual constructed equipment and referenced nondegenerate triangles, plus exact V/T/C preservation when the UV operation is exercised with a different origin. Existing footprint/height and hood containment tests remain meaningful.
- The new opaque bitmap is copied byte-for-byte from its source. Its scalar is neutral and below one, alpha remains one, and the original Sprites/Default material is cloned by the existing cutaway cache. No new shader or updater is introduced.
- `Create` retains authored order and existing depth-scope ownership. `ForStructure` caches by source Material, so two ranks borrow one scene-owned variant. It enters the existing focus/reset/disposal loops, while imported material and texture remain borrowed. Root's lifecycle assertions exercise that ownership.
- The approved tradeoff is two extra static material/rank batches and one extra shared focus-vector update. No geometry budget increase was introduced.

## Visual and test limits

The local-field UVs are fixed-view projected sampling, not physically unfolded metal UVs. The opaque source grain and scalar therefore need actual portrait review for concrete-like noise, cap/body cohesion and strap/mouth separation. Mean matching controls average encoded luminance; it cannot promise equal light at every sampled patch.

Splitting material batches changes their aggregate renderer bounds. Equal-rank transparent colour rendering uses painter order, not structure-depth rejection, so actual crops still need to confirm no newly exposed overlap with the remaining iron batch. The internal mesh triangle order and declared ranks are unchanged; no definite overlap defect was found in source.

The new scene locality test uses projected axis-aligned bounds. It is useful to catch material migration into another landmark, but does not replace the existing tighter geometric envelope and contact assertions. No Android performance acceptance is implied by source review.


## Final source recheck after V2 correction

The root has corrected both reported test migration defects: emitted cast ranks now assert -7/-8, the hood bounds branch uses -7, and `AssertWorkIslandContacts` includes both old iron and cast iron textures while preserving its original depth/corner assertions. Loaded Clamp mode and conditional Android ASTC 6x6 assertions were also added. These corrections were inspected in source; test pass evidence remains root-owned.

The first source candidate was rejected after actual rendering because it read as coarse concrete. Final runtime binding is `FoundryCastIronV2`, GUID `8d6a4a1b535744f8bab4894000b6cd5e`, with RGB material gains `0.729285164 / 0.796827390 / 0.899652616` and alpha 1. Unlike V1's neutral luminance match, this authorized correction matches each baseline source channel mean to preserve the old cold iron family. No new geometry or shader change accompanies this correction. Final-image assessment is in `independent-final-material-review.md`.
