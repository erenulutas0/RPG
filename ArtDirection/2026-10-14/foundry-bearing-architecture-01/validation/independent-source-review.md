# Independent source review

Reviewed the frozen nine-file package on 2026-10-14 against baseline `cf511c3`: `FoundryRegionFortressSurface`, `FoundryRegionBridgeSupports`, `FoundryRegionUndercroft`, `FoundryRegionDistantWorks`, `FoundryRegionWorkshop`, `FoundryRegionCastingBench`, and their fortress hierarchy, service shaft and loading support test files. Read-only review; no Unity, build, device or source edits were performed by this reviewer.

**Source disposition: no blocking defect found.** This is permission to proceed with root's actual verification and image review, not a claim that either has passed.

## Bridge and near-island structure

- `FortressDressedArch` replaces the original wedge with an inner return, shoulder and dressed outer face. The original four corners remain; its opening is not moved or covered by another parent plane. The additional eight vertices per wedge are a bounded partition, not a second arch.
- `BridgeBearingGrain` uses the same origin and scale for the complete front and separate soffit buffer. The return's 1.3 design-unit offset raises its projected position by only about 0.460 units (`Axis / 2`); its highest sample remains below the -2.7 bearing line. The complete original front/return envelope therefore fits the chosen .04-.39 by .44-.564 atlas interior. It preserves one affine, noncollapsed field at coincident boundaries.
- `BuildFortressArchFace` and `BuildFortressRespond` remap their complete emitted range rather than restarting the image on each small face. Existing geometry, face colours, socket backing and screened-anchor logic remain. `ShadeUndercroftDepth` is unchanged, so this does not compound the existing darkness treatment.
- Existing complete-bent tests retain the open intrados and exact deck-contact checks. The new production-buffer UV test covers both batches. The wedge partition test checks total triangle area, original corners, interior coverage and colour hierarchy. Review identified an initially unclipped outer test corner; root corrected both outer corners to the actual -3.03 primary bearing before this final review. No tolerance was relaxed.

## Service towers

- Each crown's two visible outer planes are replaced by three connected profile bands. Shared inset ring coordinates keep the corner joined. Top/bottom exterior rings, annulus, inner walls and recessed throat remain; the .075 inset leaves at least .145 units before the existing throat.
- The profile does not fold over itself at either authored crown size. An independent arithmetic check of both projected profiles found all 12 triangle orientations consistent and one owner at every triangle centroid. This was a source-math check, not a Unity run.
- The whole head receives one bounded physical mineral field. The variant 1/2 bearing response is a neutral scalar layered with the retained ambient, weather and local heat response. Other distant variants are excluded. Existing foot fade and scenery order remain.
- New tests exercise both actual crown sizes and complete heads, not merely helper constants: shared corner incidence, unchanged mouth edge incidence, connected geometry, UV bounds/scale/noncollapsed triangles, source geometry retention and bounded material gain. Existing fire panes, skyline, foundation and broken-span contacts remain authoritative. The expected -32 distant vertices requires root's actual scene measurement.

## Workshop and casting supports

- All four existing `BuildServiceShelf` consumers use the reviewed 2.8 or 3.0 depths, so the new ordered corbel stations stay inside their prior depth intervals. Corbel knees retreat no farther than .25 into the .28 recessed infill, retaining positive thickness; their foot and -.42 header remain fixed. Returns are emitted before the corresponding front face.
- The shelf bearing replaces the old strip, sharing the near corner across both faces. The casting shoe uses the same profile while retaining its original four foot, shoulder and top contacts. The cap, rail, equipment placement, depth owners and rank split remain unchanged.
- Independent projected-profile arithmetic for both bearing proportions found positive triangle areas, consistent orientation and one centroid owner. The recessed facets therefore do not fold or duplicate the old flat bed at those authored dimensions. Final scene occlusion still needs image inspection.
- `ServiceShelfGrain` uses one isotropic affine field per complete supported piece and stays inside the existing mineral patch. Foundation and raised apron are separate physical pieces; no continuity across that material joint is claimed. Casting cantilevers each keep their own complete field.
- `FoundryLoadingSupportTests` now checks emitted profile triangles in the actual foundations, the original scenic envelope/deck corners and below-floor split, casting foot/shoulder/cap contacts, complete-piece UV continuity and an independent single-owner check on the profile. These are meaningful checks beyond a vertex budget. They do not replace final painter-order inspection against the unchanged rail and painted equipment.

## Remaining acceptance work

Root must confirm actual resource counts, all required suites and matched After images. Inspect the full map as well as tower crowns, stock/casting undersides and the bridge mouth: reject overly bright bands, faint/flattened returns, mineral scale mismatch, concealed equipment, closed apertures or a darker undercroft without improved structure. This package addresses existing secondary architecture; it does not establish reference parity, change the playable map, or constitute phone acceptance.
