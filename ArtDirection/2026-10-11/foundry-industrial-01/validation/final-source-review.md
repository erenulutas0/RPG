# Independent industrial furnace source review

Date: 2026-10-11. Reviewer: industrial_source_qa, separate from both model authors.

Status: **Accepted for Unity integration after the two tall-source corrections below.** This is source geometry and baked-image acceptance, not runtime, phone, or complete reference-fidelity acceptance.

## Evidence inspected

- Approved composition reference: `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`.
- Previous actual Unity composition: `ArtDirection/2026-10-11/foundry-compact-gate-01/after/full-map.png`.
- Both final 1024-square RGBA renders in `industrial-furnace-01` and `industrial-retort-01`.
- `BuildBlenderIndustrialFurnaces.py`, `BuildBlenderIndustrialRetorts.py`, original `BuildBlenderFurnace.py`, both model-budget records, and the retort original-contract comparison.

## Findings resolved before acceptance

1. The first tall renders showed solid grey exhaust lids: the intended soot interior was below a capped solid lip. The final square mouth uses four rim walls around its visible recessed interior, and the round mouth uses a true annulus. Final renders show both openings. The soot floors remain below the rim, whose maximum design height stays 7.2.
2. The new tall receiver initially began at 3.62 above the retained cornice ending at 3.60. Both the receiver and its foot ring now begin at 3.58, giving 0.02 positive overlap rather than an unsupported gap. Final source and renders agree.

## Contacts and projection

Tall source retains the original matched camera with orthographic scale 10.8, target design height 3.6, image bottom -1.8, centred outlet and maximum design height 7.2. The complete old lower foundation, arches and their existing overhang are retained. Reported maximum radius 2.375079 is the retained decorative overhang, not a new base expansion. New shoe/body, crown, chamber, shoulder and rib contacts are joined; the upper silhouette remains inside the existing lower overhang envelope.

Short source uses the original scale 6, target height 1.14 and image bottom -1.86. Both centred outlets end at 2.28. Evaluated physical bounds match the archived short-boiler source exactly: approximately [-2.296214, -2.296214, 2.0, 2.0]. The preserved fire-mouth projection exceeds the circular radius-2 plinth; it is not new geometry growth. Both variants remain inside the image.

The first retort has a visible, low inspection hatch with gasket, hinges, feet and handle seated on its flat deck. The second uses a distinct pressed hood, supported ribs and a front service plate evaluated against the actual polygon face. Neither adds the formerly rejected tall table-like hatch. Hood and flue contacts are coherent. Source-level service-socket contracts stay at the existing locations; actual runtime pipe/card contact still requires Unity evidence.

## Art assessment and limits

The angular pressure chest, ribbed exhaust tower and two manufactured retort hoods provide clearer industrial identities than the previous repeated smooth kettle silhouettes. Restrained bronze and retained lower fire openings keep them within the approved foundry palette. The variants differ in complete silhouettes and manufactured panels, not only colour.

No remaining source-image blocker was found after the corrections. The reference still has finer material wear, richer architecture and more irregular functional machinery. These models improve that direction but do not complete it. Hatch visibility, inter-building separation, smoke attachment, pipe contacts, imported compression and actor readability must be checked in actual Unity portraits and map views. Native Blender vertex counts are authoring statistics; the game will use baked cards, so these numbers establish no runtime performance claim.
