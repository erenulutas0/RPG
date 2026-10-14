# Complete native moulding bench

Baseline: `d4a3ecbab81759159633e27d65533c67d1a30f27`. Fresh matched Before views are sealed in this package. This scope implements item 1 of the preceding scene-cohesion package's `next-visual-priorities.md`.

## Station and intended visible result

The complete native moulding station on work island 1 is the single workyard target. It is clearly visible in `approach--16-15.png` and `yard-detail.png`, with the authored casting bench nearby for a useful material comparison. The approved reference remains `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`; the authored local source inspected is `Tools/ArtReview/BuildBlenderCastingBench.py`.

Four raked cast legs replace the square posts and four separate gussets. Their feet bear on the existing skid at 0.10, and their broad heads meet the existing worktop underside at 0.99. The two lower iron ties, lower shelf, two spare open flasks, two packing boards and three worktop boards remain. Both existing work setups are rebuilt with muted oak flask rails, a cool iron base, connected cast lifting handles, and actual recessed sand geometry. The long ingot opening has tapered walls and a lower floor; the bearing opening has a lower annular floor around a raised sand core. The sand top is partitioned around the openings, with no full top quad filling either cavity. Two connected L clamps retain their screw contact at the long flask rim. Small bevelled handle/clamp boxes are replaced by plain broad cast faces to fund the cavities.

The expected gain is a complete station that reads as supported oak, cast iron and packed sand alongside the authored casting bench. This is a source-stage expectation until actual After review; full-map improvement and cavity readability at phone scale must not be claimed from code alone.

## Geometry accounting

Exact counts below are calculated from the emitted component topology, before the unchanged 28-vertex island skid. These are source counts, pending independent execution/capture measurement.

| Component | Before vertices | Candidate vertices | Candidate composition |
| --- | ---: | ---: | --- |
| Lower shelf and two spare open flasks | 252 | 252 | 1 shelf hull and 8 rail hulls |
| Packing boards | 56 | 56 | 2 hulls |
| Cast support frame | 248 | 136 | 4 raked legs at 20 vertices and 2 lower ties at 28 |
| Three working planks | 84 | 84 | 3 unchanged hulls |
| Long ingot mould | 396 | 248 | 28 base, 36 sand, 112 oak rails, 72 handles |
| Bearing mould | 468 | 485 | 28 base, 273 sand, 112 oak rails, 72 handles |
| Clamps, end dog and divider | 280 | 128 | 2 L clamps at 52 and 2 plain hardware boxes at 12 |
| **Complete station** | **1,784** | **1,389** | **395 vertices recovered** |

The ingot sand consists of 4 shoulder quads, 4 tapered wall quads and 1 floor quad (36 vertices / 18 triangles). Bearing sand consists of 16 shoulder quads, one 17-vertex shared core fan, 16 outer wall quads, 16 floor quads and 16 core wall quads (273 vertices / 144 triangles). No degenerate fan quads are needed. The complete candidate station has 702 triangles by source accounting; the unchanged skid brings the island to 1,417 vertices / 716 triangles.

The existing `FoundryIron` material and sorting order -11 remain. No texture, material resource, renderer, batch, bake or Blender asset is added. Applying the isolated 395-vertex reduction to the sealed 172,735 region baseline predicts 172,340 vertices, subject to the combined capture measurement. The distant tier is outside this scope.

## Contact and envelope

All pieces remain within work island 1 (`u=-22.8..-19.6`, `v=11.8..14.0`) and its existing 0.10..1.55 height envelope. The long flask still occupies island-relative `u=0.32..1.42`, `v=0.31..(depth-0.27)`, bottom 1.13, rim 1.40. The bearing flask still occupies `u=1.70..(width-0.30)`, `v=0.40..(depth-0.47)`, bottom 1.13, rim 1.32. Sand tops are 0.04 below the rims; both recessed floors sit at 1.215, above the supporting iron plates. Front and rear U-handles overlap their oak rail at the two feet and share edges with their crossbar.

The shared `YardStoredFlask`, `YardBenchIronFinish`, `YardBenchTimberFinish`, `WorkyardCastTone`, trolley and other stock/casting groups are unchanged. No walkway, navigation, interaction, camera, collision footprint or depth owner change is included.

## Changed files and evidence status

- `Assets/_Project/Scripts/UI/Arena/FoundryRegionProductionYard.cs`: narrow station calls and removal of the superseded native mould/pattern helpers.
- `Assets/_Project/Scripts/UI/Arena/FoundryRegionMouldingBench.cs` and `.meta`: complete station-specific frame, oak rails, sand cavities and connected hardware.
- `Assets/_Project/Tests/EditMode/FoundryMouldingBenchTests.cs` and `.meta`: focused cavity/contact coverage prepared by the separate test owner. Three NUnit cases cover both casting variants and complete-station integration. The test owner reports isolated compilation passed with zero warnings/errors; Unity execution remains pending when this scope note is written.

Existing equipment tests already constrain the projected owner envelope, shared stored-flask contacts, working-plank grain and neutral iron family. New opening tests evaluate the actual sand-top plane separately from projected backwalls, then establish that the complete station emits the tested surfaces and their connected walls/floors. They also check all four leg contacts and the complete clamp screw feet on the actual flat oak cap. Both screw feet occupy island-relative `u=0.35..0.41` inside the `u=0.34..0.42` flat cap at height 1.40. An all-triangle screen coverage query is not a valid physical opening oracle.

This package still requires parent-owned actual matched Unity capture, measured loaded cost and independent image review. No phone build, physical-device frame time, crowd/busy-combat readability or sustained thermal result is provided by this station work. The approved reference remains a direction for overall scene quality; this bounded station change does not establish whole-scene parity with it.
