# Foundry arch masonry - 2026-09-29

## Delivered scope

`FoundryRegionUndercroft.cs` replaces each continuous 12-segment arch face with 13 individually separated stone faces over a continuous recessed mortar backing. The middle stone projects slightly above/below the neighboring faces. Narrow upper arrises and dark inner returns add depth, with restrained deterministic stone variation. Existing local depth shading is applied afterward.

Existing shared wall texture and static mesh batching are reused. No new bitmap, material, runtime update, scene asset, walkable geometry, collision, balance, or camera change. No helper agents used for this slice. This is a subtle close-range masonry refinement, not completion of the reference-art target.

## Evidence

- `before.png`: previous finish package's full-map export.
- `before-detail.png`: same-camera undercroft capture preserved before the change.
- `after.png`, `undercroft-detail.png`, `bridge-detail.png`, `point-0.png`, `point-3.png`: actual Unity captures after the change.
- `walk.csv`: successful five-waypoint traversal through the existing map.
- `static-budget.txt`: 17 static renderers, 51,348 vertices (previous 49,556; +1,792). Existing atmosphere remains separate at 1 renderer / 780 vertices.

Compile check: 0 warnings/errors. Static integrity: 514 project GUIDs, 1,099 package GUIDs, 568 scene objects/components. Actual Unity graphics traversal: 1/1 passed. Root inspected undercroft, bridge, and full-map output. Full test/build outcome is appended below when available.

The phone was not operated during this slice. Last installed APK remains `18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535`; the prior pause snapshot is historical and must be rechecked before device input. No commit or push.

## 2026-09-29 - Segmented undercroft arch masonry

Replaced continuous arch faces with 13 individually separated stone faces over recessed mortar, a projecting central key, narrow lit upper arrises, and dark inner returns. Existing static wall material and local depth shading reused. Static budget: 17 renderers / 51,348 vertices (previous 49,556); atmosphere remains separately 1 / 780. No gameplay, collision, scene, camera, or raster asset changes. Actual Unity five-waypoint graphics traversal passed; undercroft, bridge, and full-map renders inspected. This is a subtle close-range refinement, not reference-art completion.

Compile: 0 warnings/errors. Integrity: 514 project GUIDs / 1,099 package GUIDs / 568 scene objects/components. Full gate: EditMode 405/405, PlayMode 130/130, Android build exit 0. APK actual size 39,340,606 bytes, built 2026-09-29 16:26:53 local; SHA256 F5723AB3A6204118B349301B3834043B215C5BFD9134664B066A2DF99DBB499F. READY, NOT INSTALLED. Last verified installed package remains 18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535. No phone operations or new device-performance/save claims. Proof: ArtDirection/2026-09-29/foundry-arches-01. No helper agents, commit, or push this slice.