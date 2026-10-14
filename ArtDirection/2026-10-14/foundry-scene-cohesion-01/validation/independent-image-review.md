# Independent actual-image review

## Decision: ACCEPT bounded scene-cohesion improvement

Reviewed fresh matched Before/After full-map, court-detail, approach-20-1, forecourt-detail, bridge-detail, bridge-foot, stock-detail, casting-detail and yard-detail. The approved reference was independently inspected in the baseline review. These are actual native captures; no image generation, retouching or reconstructed scene was used for the judgment.

The floor pass is a bounded improvement, and the workyard additions have no observed blocking visual regression. The current combined candidate is accepted as an incremental scene improvement. This does not establish reference parity or phone acceptance.

## Corrected initial visual interpretation

The reviewer initially interpreted the changed adjacent masonry in bridge-detail and forecourt-detail as new overpaint on the right entrance keeper and temporarily rejected the candidate. Root challenged that observation with exact pixel comparisons. The reviewer then independently repeated the comparison with Pillow/NumPy on the actual Before/After PNGs. The broader hood and silhouette rectangle x829–896, y340–387 contains 3264 pixels and is completely identical: zero changed pixels and maximum RGB difference zero. Chest, body and pedestal core rectangles have maximum channel difference one. The original larger claimed rectangle includes changed background masonry and does not demonstrate keeper overpaint.

The keeper regression claim was wrong and is withdrawn. No runtime repair was required or justified by it. The accepted images are the same candidate, not a repaired recapture. Exact ranges, measurements and input hashes are preserved in `keeper-comparison-correction.md`. This correction supersedes the preliminary rejection message and earlier version of this note.

## Floor and lighting judgment

- Full-map and court-detail show a visibly narrower pit heat ribbon. The warm band still exists, but it occupies less court and sits nearer the pit collar. The approved reference remains more locally illuminated; this is progress rather than parity.
- Approach-20-1 shows quieter warm shoulders around the gate landing and a shorter scale of the directional mineral grain. The material remains visibly stratified because the existing raster is directional. It does not become a new stone texture.
- Court and forecourt grain is finer and less dominated by broad streaks. The regular flag layout remains apparent. No added dense joint pattern, random high-contrast checkerboard or visible UV reset was found in the inspected matched views.
- The hero, violet golem, chest and brass marking retain separation. The small patina midpoint lift does not create an observed static readability failure. The centre is visually retained in character, not bit-identical in final RGB.
- Yard-detail retains its clear transfer floor; the pit warmth no longer washes as far across it. Some continuous pit/collar warmth is still visible and should be documented as a remaining reference gap.

## Workyard and vertical architecture judgment

The new short stock billets are visible at casting-detail's lower-left, supported and lower than the adjacent crate/pipe group. They help connect the existing storage assembly without occupying the walking apron. The underbench storage reads chiefly as restrained dark layers behind the bench; its benefit is modest, and no unsupported floating stock or apparent new obstacle was observed. Stock-detail itself crops much of the added billet group beyond its right edge. Full-map group density remains far below the approved reference, so this cannot be described as a rebuilt or complete workyard.

The near-island/bridge material treatment is a modest change in the full map and more evident at close range. Bridge-foot retains the physical opening and its outer support outline in the static image. The material is still regular and simpler than the reference. The pixel verification above establishes that the supposed new keeper clipping is not a real regression; no blocking silhouette/overpaint issue remains from this review.

## New test review

`FloorSpillCanBeTightenedWithoutChangingWallOrIronIllumination` uses a one-lamp fixture, changes only FloorRadius, and requires real FloorLight response to change while ArchitectureLight and MetalLight stay equal. It restores the original cached lamp array in a finally block. This is a meaningful test of the new isolation boundary and does not merely restate the production colour formula. No source blocker was found in this new test. Execution belongs to root's recorded verification workflow.

## Resource and evidence boundary

Actual After runtime report: 36 renderers / 172735 region vertices, versus 36 / 169119 Before: +3616 vertices, about 2.14%. Distant meshes total 3572 (3428 + 144), included in the region total and unchanged. Retained ceilings are 174000 region / 3600 distant, leaving 1265 and 28 vertices. These counts do not establish physical-device frame time or performance.

The captures are paused static Editor output with hidden HUD. This review does not accept phone motion, crowd/HUD interaction, shimmer, installation or device performance. The current candidate is accepted only for the bounded static art outcome described above; full automated verification/build closure remains root's responsibility.

## Final capture verification after test-only repairs

Re-viewed the freshly captured final full-map, court-detail and bridge-detail images. The ACCEPT decision stands: narrower warm shoulders, finer continuous floor grain and retained actor/entrance readability are present in the final output. The After directory contains all 21 PNG views. The final runtime report remains 36 renderers / 172735 vertices, with distant total 3572 unchanged.

Compared the preserved initial source freeze in `rejected-tests/source-freeze.json` with the final `source-freeze.json`: the only differing Assets entries are FoundryCourtCompassTests.cs, FoundryFortressHierarchyTests.cs and FoundryWorkyardStockTests.cs. There are no runtime-script, runtime-art or material changes between those freezes. The final focused XML independently records 285/285 passing, zero failures/skips. Root reports isolated compilation with zero warnings/errors and successful fresh capture. Full-suite/build closure was still running when this review was refreshed and is not claimed complete here.

Final reviewed input SHA256:

| Input | SHA256 |
| --- | --- |
| After/full-map.png | `FE2EE703510A387B53C1B66B5BE05B82A35F6AD788AC43FC288D0E392B5C75F8` |
| After/court-detail.png | `363BE9748761344165A43D7CC226E34A81F6A77846AE716E261396364B261C9E` |
| After/bridge-detail.png | `65FB7DBA56585E2EBC3C99C33C8595F90C1AEA77CB6D0A415BAC71348DCE8B18` |
| Before/bridge-detail.png | `367E1759C4D1DB897C133C8AF26E40E52FC3BD4FAA03FC6BCE6F168B2B581FAB` |
| validation/source-freeze.json | `9D9E6E10B2A6B266B16D0297AEC04D3409D7856088401DE4AEA85D4C557E27BD` |

Repeated the keeper diagnostic against the final bridge capture: the full 3264-pixel hood/silhouette rectangle remains bit-identical to Before, while chest/body/pedestal core differences remain at most one channel level. The initial mistaken overpaint claim remains withdrawn. The earlier correction note's After hash identifies the first reviewed capture; the final capture hash above is the current acceptance input.
