# Independent baseline review

Baseline `cf511c3`, reviewed 2026-10-14. Reopened the preceding package's actual After full map, service-shaft, casting, stock and bridge views beside the approved `2026-09-23/foundry-layout-01/foundry-v1.png`. Read the previous next-visual-priorities and the bridge/undercroft/fortress surface source. Read-only review; no Unity, device, source edits or implementation acceptance.

The main silhouettes are established, but the native secondary structures remain visibly simpler than the reference. This package should improve how existing large structures are assembled and supported, rather than add unrelated small props. It will not close the remaining layout/detail gap by itself.

1. **Service towers:** the square hollow crowns and very regular tall wall fields behind the casting pocket are conspicuous beside painted machinery. Give the existing crown, shoulders and major bays a coherent bearing hierarchy. Keep openings genuinely open, respect the broken-span contacts, and retain their atmospheric recession. Avoid turning the shafts into bright gate towers or adding tiny brick rows to every plane.
2. **Working shelves:** the stock and casting equipment has more detail than its thin cap-to-wall transition. A broad load-bearing course and readable underside haunch would make the shelf look supported. Keep the authored equipment and rail fixed; judge the complete pocket. Do not hide the rail, create a floating trim line, or consume the playable route with a larger scenic envelope.
3. **Near island and bridge:** improve the existing bearing course, arch return and major support faces. Retain the current deck contact, aperture contour, support stations and foreground silhouette. The primary support should read more clearly than recessed rear bents. At full-map scale, the desired improvement is a constructed masonry edge carrying the court, with depth visible under it.

## Root's bridge/undercroft hooks and pitfalls

- `BuildBridgeBent` already distinguishes the primary dressed mouth from the two recessed rear rows; it preserves an open intrados and a straight bearing at -2.7. Keep that hierarchy and every opening vertex. New bearing subdivisions should replace part of the original parent face instead of drawing another coplanar wall over it.
- `BuildFortressArchFace` and `UndercroftSpandrel` already provide backing and a narrow deck contact. Extra dark strips are unlikely to help. Broad plane relationships and consistent mineral scale matter more than additional outlines.
- `FortressMapGrain` currently restarts at each face's local minimum and selects the longest edge as its axis. If adjoining parent-plane subdivisions are changed, map the whole original plane with one stable orientation where possible; otherwise their individual starts can become patchwork. Stay within the existing stone-interior atlas patch. Preserve the original dressed-face/frame topology unless an actual defect justifies replacing it.
- `ShadeUndercroftDepth` already attenuates deeply recessed stone to 0.64 with a cool bias. Do not multiply another uniform darkness pass through that entire family. Bring exposed bearing stone into relation with the foreground edge while leaving the deep structure quiet.
- Preserve transparent-batch ordering: returns before front silhouettes, distant bridge rows before near rows, and the existing separate iron-anchor rank and screened-pier rule. A source-level geometric improvement is not accepted if a front pier or anchor paints across an opening incorrectly.

## Acceptance evidence

Compare the same full-map composition, both service towers, stock/casting pocket, bridge/forecourt and actor approaches. Require unchanged playable corridor and deck edges; no sealed apertures, floating loads, repeated bright courses, checkerboard mineral patches or excessive darkness. Review production-emitted geometry, contact/coverage tests, UV bounds and the original ownership/resource ceilings. Test success alone does not establish visual improvement; final image and device claims must remain separate.
