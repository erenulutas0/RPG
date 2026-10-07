# Reference-led layout template — 2026-10-08

Actual Unity capture of the playable foundry after the owner requested an overall layout correction. Baseline: 7ea296c. This is geometry and placement work, not a new concept render.

## Changed
- Main court widened westward by 6 design units, from 32 to 38 units.
- Furnace opening enlarged from 8x8 to 12x12 (2.25x area), shifted toward the left workshop. Longer straight sides preserve the rounded-square silhouette. Molten surface lowered from 2 to 3.6 units; bed from 3.2 to 5. Existing lining and inner boiler drums extend with it.
- Gate causeway narrowed from 14 to 10 units, retaining a wider gate landing.
- Crane, supported anvil/stock shelf, loading clutter, west finials, bastions, chains and depth footprints relocated together. Bridge keeper feet adjusted to remain completely outside the new walking union.
- Shared collision, spawn placement, pursuit waypoints, pit light/soot and atmosphere follow the layout. No balance or progression changes; no new texture or renderer.

## Evidence
`before/overview.png` and `after/overview.png` use the identical overview camera (1,-1; orthographic size26;4:3). Detail cameras follow relocated structures and are not identical-framing comparisons. `after/map-export.png` is a clean actual map export; `after/walk.csv` records completed movement through bridge, west loop, central court, causeway and landing. `index.html` switches between old/new overview and shows the original concept separately.

390 pure tests and410 EditMode tests passed. Actual Unity graphic capture passed, including traversal and depth/motion checks. Compile check:0 warnings/errors. Region18 renderers /90,294 vertices versus84,160 baseline. Full PlayMode/build/device status is recorded below after completion.

## Still open
This establishes the template; it is not a finished match to the reference. Bevelled court silhouette, a distinct raised gate terrace, smaller apparent paving scale and more deliberate workshop grouping remain. All current walkable ground is on one gameplay plane. Existing artwork is reused rather than replaced wholesale.

Full gate passed:145/145 PlayMode and Android build exit0. APK SHA25649954D59713C50F530EF64EF9B8FFA74A1F6B8DEAB5CE12CC69B2B52BDF46D8E,35,962,080bytes. Device install awaits permission to close the owner's newly active Floor2 run. The initial read-only backup predates that run and must not be restored over it.
