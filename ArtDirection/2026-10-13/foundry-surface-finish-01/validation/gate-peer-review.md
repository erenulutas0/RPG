# Gate surface source peer review

Reviewed by the material-bindings helper on 2026-10-13. This is a read-only source review plus retained failure-evidence inspection. The helper did not run Unity, edit Assets, or approve device performance. Final test and render results belong to the root verification record.

## Initial review

The three runtime changes keep the existing gate footprints and height envelope. Header courses replace the retained complete wall with cell partitions: mortar, dressed arrises and stone interiors have separate ownership. The buttress patch interpolates the original parent-face light before applying a shared face-coordinate contact treatment. `GateStoneContact` multiplies all RGB channels equally, so it preserves the incoming local forge-light ratio rather than introducing an independent painted glow.

`GateRecessGrain` applies one affine two-dimensional projection to each selected stone group. Its bounded origin/scale stay within the quiet atlas interior: U 0.035..0.398 and V 0.438..0.567. Header cap copying respects the original `LitFoundryTier` layout of three wall quads followed by cap quads. The existing recessed cap remains 0.022 below the raised coping. No blanket masonry-atlas replacement is made.

The initial source review found no actionable defect, but it did not establish numerical execution correctness. The subsequent Unity failures described below supersede that preliminary confidence.

## Failure and reviewed correction

`editmode-initial-failed.xml` records 567/569 passing EditMode cases, with two new gate grain cases failing. `editmode-diagnostic-failed.xml` retains the targeted witnesses. One witness reports a projected double-area of approximately 6.18509e-7 with exactly zero UV area at the lower boundary of a long header face.

The old `GateButtressBlock` mortar predicate tested whether either endpoint moved in any direction. At an outer boundary, a neighbouring joint can shorten the edge tangentially while its perpendicular inset remains zero. Emitting that four-point strip produces a mathematically zero-area boundary face. Float projection can make it appear as a tiny world-space sliver even while its UVs remain collinear.

The reviewed correction emits each mortar strip only when that edge has positive perpendicular inset in authored cell coordinates: lower bed.y > cell.y, right bed.z < cell.z, upper bed.w < cell.w, or left bed.x > cell.x. This removes nonexistent boundary mortar rather than weakening UV or area tolerances. Interior joints still emit their complete strip; dressed arrises and inner stone faces retain their existing partitions. The UV guard still skips only world double-area below 1e-7 and requires UV double-area greater than 1e-12; the diagnostic message was expanded, not its acceptance threshold.

No further actionable source finding was identified in this correction. Full execution results must still be checked by the root workflow.

## Limits and visual follow-up

Bounded UVs prove atlas-interior selection, not seamless visual tiling, ideal texel density, or absence of material repetition. The mapping can reduce its uniform scale to fit a long/tall face into the available atlas patch. It preserves the local aspect ratio but does not promise identical texture scale across every separate group. A mathematically affine map also cannot redeem source geometry that is itself degenerate; the failure above is why the retained execution test is necessary.

The newly used broad mineral patch is darker than the former narrow header sample (approximate measured source means 0.260/0.259/0.288 versus 0.300/0.288/0.308). The added crown contact treatment darkens locally as well. Actual gate closeups must confirm readable coping and supported mass; source review does not approve that visual result. No compensating global brightness change is recommended before inspecting the real render.

## Reviewed source hashes

- `Assets/_Project/Scripts/UI/Arena/FoundryRegionGateHeader.cs`: `2a76bcd86114e6642509e4919eec95be20e92dfdc3d2b42259baafd1aa894142`
- `Assets/_Project/Scripts/UI/Arena/FoundryRegionGateRecesses.cs`: `70c8613b44ca719e9879a40faaae37b20898d76a01b7b3a510c28cfd4d00e70a`
- `Assets/_Project/Scripts/UI/Arena/FoundryRegionGateButtress.cs`: `d2a1a2fdff60997946232552fc4160f670b6cfa0cbc7c80b1bb0cc9aa8803530`
- `Assets/_Project/Tests/EditMode/FoundryGateRecessTests.cs`: `f7c54aec9bfc24fc0edbdac5742b46cdf59d799330d1311815d87b78a4e234a9`
