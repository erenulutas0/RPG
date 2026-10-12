# Grounded crane machinery — source scope

Baseline `c4f66e6`; root sealed sixteen fresh Before captures before GO. Compared the approved `2026-09-23/foundry-layout-01/foundry-v1.png` with the verified ceremonial package's full-map, crane-detail and yard-detail. The western crane exists but reads much lighter and more isolated than the reference's loading group.

## Changed files and active path

- `Assets/_Project/Scripts/UI/Arena/FoundryRegionCraneHousing.cs`: one native cast gearbox beneath the existing crane mast. A seated sole, broad sloping rear hood, joined front cheeks and header surround a real exposed cable drum. Far flange emits before the drum and near flange; the final cheeks and header retain a front service aperture. Three broad wound turns sit on the curved drum. The previous fallback mast-housing code is unchanged.
- `Assets/_Project/Scripts/UI/Arena/FoundryRegionDressing.cs`: one `BuildCraneMachineryBase(arena)` call after the baked/fallback branch. This is active with the current baked resource; the older `BuildCraneHousing` alone would only change the fallback.
- `Assets/_Project/Tests/EditMode/FoundryCraneMachineryTests.cs` and its `.meta`: three new tests for the complete emitted mesh inside the original scenic deck/height volume, actual sole contact outside the walking union, valid UVs/indices/alpha and bounded cost; a sampled last-visible-triangle comparison requiring a substantial part of the actual drum to survive final cheeks/header; and safe two-dimensional grain over actual nondegenerate triangles.

No ProductionYard, map, movement, gameplay, camera, texture, resource import or baked-card source edit is included.

## Physical and resource contract

New casing footprint is authored `u=-26.12..-24.18 + WestAssemblyOffset`, `v=11.22..14.70`, height `1.12..2.60`. It is seated on the existing iron deck and enclosed by the original crane footing `u=-27..-24 + WestAssemblyOffset`, `v=11..15`, depth height `8.6`. The current maximum new u is `-32.18`, outside walking West `-32`. The far barrel/crate corner ends at authored u `-26.22`; the new casting begins at `-26.12`, retaining a `.10` gap. The upper mast, pivot, jib, hanging chain/load endpoints and card framing remain fixed.

The new call uses the existing crane `MassDepth` footprint and existing `FoundryIron` order `-11` batch. It draws in front of the baked card at `-11.5`, intentionally covering the lowest mast with a broader native seat. It does not move or resize the card. No texture, material, renderer or per-frame updater is added.

Source topology estimate after the aperture repair: **760 added static vertices**, with the unchanged focused upper guard of 1100. This is not the measured whole-region resource result; root must record that from the actual capture. No device frame-time claim is made.

## Validation and limits

An early isolated compile encountered the court owner's still-in-progress `ReplaceCourtPavingBase` symbol in `FoundryRegionView.cs`; it reported no crane diagnostic. No repeated compile or Unity execution was performed by this helper. Root runs verification on frozen combined Assets.

Actual capture must confirm the native lower casing and baked mast form one seated assembly, the existing foreground lantern does not erase the whole new shape, and the visible service opening is not covered by the header or card. Compare full-map as well as crane detail: no claim of a dramatic whole-map improvement or reference parity is made before seeing After. The original lamp crossing the mast and the fixed small crane footprint remain limitations. Physical-device movement, crowd, shimmer and performance are separate pending acceptance work.

Independent source review reported no blocker after checking the active call, footprint/height, depth contract, drum axis and cheek/header contacts. It correctly distinguishes the sampled helper test from actual baked-card/lantern compositing: that remains an image gate. The two runtime files and new test/meta are frozen for root verification.

The first root geometry run then caught real aperture overpaint: only31 of338 drum samples remained exposed, below the unchanged35% requirement. The scoped runtime repair corrects far-bearing painter order and replaces the rectangular lintel with a genuine raised central soffit while retaining broad seated end bearings. See `crane-aperture-repair.md`. The test oracle and thresholds are unchanged; root must rerun actual geometry/capture gates.

## First actual finish rejected and corrected

After the geometry repair passed, root and independent image review rejected the first captured finish. The broad hood was conspicuously blue/bright and its roof/side grain collapsed into long straight corrugation-like streaks. The shared stock sampler anchored to deck floor drove these elevated vertices into its hard UV clamp; large faces consequently sampled a single texture row. The geometry passing did not establish material acceptance.

The final treatment is local to this new casting. `FinishCraneMachinery` uses a common centered projected field with `.105` UV units per projected unit, without any clamp, and converts the overly blue high response into restrained warm-neutral cast iron while keeping incoming form/light intensity variation. Every emitted face retains actual two-dimensional grain inside the iron atlas's unframed interior. Existing generic stock mapping, all textures/materials and the760-vertex geometry remain unchanged. The third test measures UV area against actual nondegenerate geometric triangles and checks atlas-interior containment, so line-collapse cannot silently return. The aperture35% and budget1100 guards remain unchanged.

Initial actual images are retained under `validation/rejected-crane-finish/` with their hashes and rejection note; root also keeps its first-capture evidence. New actual images are required before claiming the material mismatch is resolved. No Unity or additional combined compile was run by this helper during either repair.
