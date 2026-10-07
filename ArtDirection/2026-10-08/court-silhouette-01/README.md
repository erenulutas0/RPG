# Court silhouette and gate shoulders — 2026-10-08

The reference-led playable template now clips six design units from each of the two entrance-facing court corners. FoundryMap owns both half-planes; its exact segment interval union applies them to the court rectangles and preserves the diagonal bridge and gate rectangles. No stair-step approximation or nonlinear projection.

The existing ashlar lattice is clipped by convex triangle subtraction. UVs and baked light interpolate at cut vertices; bridge floor and flush decals are preserved outside the removed triangles. Straight edge walls, kerbs, rails and undercroft spans stop at the new outline. Diagonal coping, balustrades and arches close the new edges. Southeast foundation/chain positions and one finial move with the outline; the redundant old corner lantern is removed. Short diagonal rail owners stay outboard of the walking surface. Renderer count remains 18; total vertices 90,080 (previous 90,294).

Two stepped side shoulders frame the gate landing, wholly outside the walking lane. Their lower foundations use the below-floor batch; their raised portions use explicit depth owners. Flush arrival bands mark the wider landing. These are architectural side terraces, not a newly raised walkable floor.

## Evidence

- `before/overview.png`: previous reference-layout build.
- `after/overview.png`, `map-export.png`: actual Unity rendering using the same overview camera.
- `after/point-5.png`, `point-6.png`: actual hero traversal to both new corner lanes.
- `after/gate-detail.png`: actual Unity gate detail.
- `after/walk.csv`: bridge, western pit loop, centre, gate lane/landing, southeast and southwest waypoints.
- `after/static-budget.txt`: renderer/vertex count.

.NET 393 passed; compile 0 warnings/errors; final graphics capture 1 passed. EditMode 413 and PlayMode 145 passed. Android build exit0; integrity545 asset/folder GUIDs,1099 package GUIDs,568 scene objects/components. New tests reject both removed tips, stop long hero steps at the cut, preserve the bridge seam, and test actual rendered triangles against removed/retained floor samples. Existing exhaustive ten-enemy spawn/visibility sweeps remain enabled.

## Remaining reference differences

The two northern corners and western workshop ledge remain rectangular. Gate gameplay is still single-plane. Paving reads larger than the concept, and the workshop/crane composition needs further grouping. This package is not a claim of completed reference fidelity. No new textures, combat tuning, progression or save changes. No new device performance claim.


## Android closure

Installed APK B8896F6978B0642FD4F459938CA763E08B57D5C195E420583C8A5A7F1BBEC882,35,978,297 bytes; installed base.apk SHA256 matched. Fresh pre-install backup TestResults/court-silhouette-device-20261008-011106. S23 guarded southeast/southwest edge input, gate walk and normal Staff combat reviewed; device screenshots included. The near boiler discharge is exposed by the southeast cut. Gate side shoulders are partly cropped on the production phone view; their complete geometry was reviewed in Unity. Initial ADB interruption stopped inputs and cleaned the temporary flag; connection recovery was verified before repeating the unfinished southwest route.

Profile.json, profile.json.bak and all3 original development files SHA256-identical. Temporary depth-proof removed; original region-preview/gate-arrival/start-floor.txt.off retained. App stopped, test telemetry retained, no save restore or push. No new FPS or midrange benchmark.
