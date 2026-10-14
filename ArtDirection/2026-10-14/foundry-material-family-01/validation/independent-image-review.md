# Independent actual-image review

## Decision: ACCEPT the bounded material-family improvement

Independently compared matched Before/After full-map, court-detail, approach-0-0, approach-20-1, approach--16-15, yard-detail, forecourt-detail and bridge-foot, and re-viewed the approved `foundry-layout-01/foundry-v1.png` reference. The package contains all 21 After PNGs. Judgment uses the actual capture files, with no image reconstruction or retouching. Exact Before/After input hashes, dimensions and pixel diagnostics are preserved in `independent-image-inputs.json`.

The new paving is a clear improvement in the native court and approach views. The complete moulding bench has a visible material/form improvement, while the broad stone bearings gain restrained separation. No blocking static visual regression was found in the reviewed views. This accepts the combined incremental art outcome, not reference parity, phone behavior or full verification/build closure.

## Paving: less common direction, retained readable floor

V8 substantially reduces the long shared diagonal streaks seen across V7 flags. The court, medallion field and raised gate approach now read as subdued fine stone instead of a uniformly combed mineral sheet. Its extra fine source detail filters to a restrained granular surface in the actual captures; it does not produce an observed bright speckle field, repeated hard texture seam, new joint pattern or false obstacle boundary.

The calibration to material scalar 0.587603232 preserves the practical value level. The court still separates the blue hero, violet golem, small fire enemy, gold chest and brass markings. The source's slight warmth remains measurable but does not turn the central floor orange or defeat those silhouettes. There is no source-only need for another coloured material correction after this actual comparison.

The following rectangles contain floor and its existing joints, not actor silhouettes. Pixel bounds use the original files with exclusive right/bottom endpoints; the encoded luma proxy is 0.2126R + 0.7152G + 0.0722B on 0..255 channels.

| Actual floor sample | Before luma mean / std | After luma mean / std | Mean RGB delta |
| --- | --- | --- | --- |
| court-detail (230,100)..(620,325) | 66.244 / 4.437 | 66.410 / 4.172 | +1.441 / -0.078 / -1.168 |
| approach-0-0 (120,210)..(760,720) | 62.783 / 4.349 | 62.946 / 4.143 | +1.396 / -0.077 / -1.094 |

These bounded samples support the visual result: the effective floor mean is nearly held, with a modest warmer shift and slightly quieter variation. They are not a global luminance guarantee or a motion/shimmer measurement. At full-map scale the gain is subtle: the broad floor becomes cleaner, rather than dramatically more detailed. Regular geometry and repetitive per-flag layout remain obvious. The reference has more varied wear and local craftsmanship, so this is a material-character improvement, not a finished reference match.

## Moulding station: an assembly-level change

Approach--16-15 and yard-detail clearly show the complete station change. The two working moulds now use timber rims that relate to the nearby authored casting bench instead of the former blue iron rim family. The long opening has a readable recessed interior, and the circular setup retains a raised core within a dark annular recess. Raked supports and the retained worktop, lower storage and skid read as one supported object. The repaired clamps and handles show no visible detached hardware or distracting overpaint in these views.

The gain is not dependent on a magnified tiny fastener: the tray material and recesses occupy the visible working surface, and the support silhouette changes. It is strongest in the portrait and yard detail. The station is small at full-map scale and does not significantly fill the sparse workyard composition. Its sand remains very dark, especially in the ingot recess, and the surrounding native timber/iron remains simpler than the neighbouring authored bench. The new station therefore improves local family consistency without establishing authored-prop parity or a complete workyard rebuild.

## Broad near-island bearings and protected dark areas

Forecourt and bridge-foot show a modest, coherent increase in exposed-bearing readability. The primary bridge arch/front course and the nearby island's framed stone faces separate more clearly from their returns and recesses, while remaining darker than the walking surface and main landmarks. Full-map shows the same bounded hierarchy change; it does not transform the native masonry's regular, angular construction.

The bridge opening remains open, and its dark background/recesses are retained. Exact pixel checks in bridge-foot found zero changed pixels in the recessed-back rectangle (430,440)..(750,500), 19,200 pixels, and in the open-void rectangle (450,750)..(750,950), 60,000 pixels. These are bounded evidence of preserved protected regions, not a claim that every scene pixel outside bearings is identical. Keeper/entrance silhouettes and the walking route show no new visible obstruction in the inspected forecourt view.

## Actual resources, verification evidence and residual limits

Actual After report is **36 renderers / 172340 region vertices**, versus 36 / 172735 Before: **395 fewer vertices**, matching the station replacement accounting. Distant meshes remain 3428 + 144 = 3572, included in the region total. Retained ceilings 174000 / 3600 leave 1660 / 28 vertices. This is resource accounting, not a frame-time or performance claim.

The real capture reports FoundryPavingV8 at **1024 x 1024 ASTC_6x6, 11 mips, non-readable, grain span 4.3**. The focused XML independently records **289/289 passed**, zero failures/skips. Broader suite and build closure remain parent-owned and are not asserted complete by this note.

The largest residual visual gaps remain the regular laid-stone rhythm, continuous warm pit/edge ribbons, sparse equipment composition, dark/simple native moulding detail and native-versus-authored architectural material/form differences. The new paving fixes the conspicuous common streak direction; it does not fix those separate issues.

These are paused Android-target Editor captures with hidden HUD. Phone motion, HUD overlap, temporal shimmer, crowded combat and physical-device timing remain unverified. Existing immortal-crowd waypoint congestion is unresolved. No installation, phone operation, save/telemetry change or gameplay/navigation/camera acceptance is inferred from this visual review. Preserve the pending device decision and current-data safeguards.

Representative accepted After SHA256:

| Input | SHA256 |
| --- | --- |
| full-map.png | `BA935A167DC45DD41D1836D1CFEEB772BF75C3816E8383E1E9F29AC15C49D5EA` |
| court-detail.png | `9D4FC7EC9C94721BE59D050C26FF743ABEC339607AFFAD2D74310FE2BB7C7D33` |
| approach--16-15.png | `688AE87186A143F600E90C88793468F785197D95CAAD3064CC9B3E125374739D` |
| bridge-foot.png | `CBF308EE0B08518F01F6C4F1667B7310FCDFBD24BB96E88E4EB13325A6C61FF3` |
| validation/source-freeze.json | `2EB60D8C175D9B6001179991BC7152282C31DF964AEBA84AB5015D57E796BC01` |
