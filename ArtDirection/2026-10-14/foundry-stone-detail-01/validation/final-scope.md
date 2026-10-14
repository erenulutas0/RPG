# Combined stone-detail scope

Baseline: `d9f33320bac6eb9fe9c5873e163337025f9ad747`. Fresh matched Before and After images are sealed in this package. This record includes the actual changed-runtime capture and static-image review. The frozen full regression/build run remains pending until the root records its result; no device acceptance is claimed.

## 1. Fixed-scale mineral detail

The new static construction helper `FoundryRegionStoneGrain.cs` maps the existing mortar-free atlas interiors as mirrored fields at **0.075 UV units per projected world unit**. It splits original triangles at the field's fold boundaries, so interpolation follows the repeat instead of stretching or shortcutting across it. Intersections are cached by edge/cut and use canonical endpoint ordering; adjacent triangles and separately authored coincident edges retain consistent cut positions without welding distinct colour owners.

The final complete-parent calls cover bridge bearings, complete undercroft bays/responds, service-shelf foundation and raised ranges, and the casting support append ranges. The bridge uses its retained deck datum; the other parent ranges use their declared world origin and orientation. Low-level face construction and `FortressMapGrain` remain available unchanged. The undercroft's nested spandrel does not subdivide independently before its parent pass.

Original input vertex positions and colours remain in their original slots. New cut vertices interpolate those colours, and the resulting triangles preserve their original painter-order groups. Unrelated append ranges remain outside each pass. Animated Flame/Cloth builders are explicitly rejected. This adds startup subdivision work and geometry; it is not a free texture-only adjustment. **No texture, material, renderer, GPU shader or per-frame update is added or changed by the mineral helper.**

`ShadeUndercroftDepth` still executes after construction. Its function and structural inputs are unchanged, but the new vertices sample that nonlinear field more finely. The rendered light interpolation therefore is not claimed to be bit-identical. Matched image review must consider the resulting stone value as well as its finer grain.

## 2. Entrance bridge haunch courses

`FoundryRegionBridgeSupports.cs` replaces the primary arch's eight non-empty flat spandrel fills with three broad common-height courses, staggered blocks and separately owned mortar cells. There is no retained flat parent face below those cells. Shared clipping stations preserve the old outline and avoid overlapping mortar/stone ownership. The completed parent still receives one common grain pass.

Original intrados stations, primary bearing/deck datums, responds, secondary bents, support depth, lanterns and sorting remain fixed. The helper's source-arithmetic estimate is +98 vertices / +48 triangles **before** the root's final mirrored-grain subdivisions. It must not be treated as the measured complete package cost. Two focused new cases inspect the actual primary spandrel output and whole bent.

## 3. Service-tower pier hierarchy

`FoundryRegionDistantWorks.cs` replaces each flat pier beside the service-tower arch with an eight-vertex indexed blind field. Four sloping bearings meet one recessed back, 0.12 design units behind the original face; the original outer rectangle stays in place. Side bearings retain 22% of the existing pier width, and head/sill bearings retain 0.85 units. The back is subordinate in value under the existing height response.

All eight replacements together add +32 vertices / +64 triangles / +192 indices. The captured distant total is **3,572** (3,428 masonry + 144 slit vertices), confirming the expected +32 vertex change. The original facade-wide masonry UV mapping continues across these new faces; this scope does not migrate the service-tower body to another texture field. Crown mouths/rims, opening and fire geometry, foundation feet, tower stations, shoulder/sill contacts and machine/portal relationships remain unchanged. Four focused new cases inspect production facades for contour closure, single ownership, exact area, UV continuity and value hierarchy.

## Actual capture and remaining visual gap

`after/runtime-budget.txt` reports **34 region renderers / 167,208 vertices**, versus 160,561 Before: **+6,647 vertices** (approximately 4.14%). The distant +32 is included in that region increase. The unchanged 174,000 region / 3,600 distant ceilings leave **6,792 region vertices and 28 distant vertices**. Future fine-detail work must account for this limited headroom rather than treating subdivisions as free material detail.

Native After views inspected for this record: full map, bridge foot, service shafts, stock, casting and entry portrait; matched bridge/stock/casting Before images and the approved reference were also reopened. The separate independent image review accepts the package as an incremental improvement.

- The bridge's former large cloudy patches now resolve into finer mottled stone. Broad spandrel courses fit the retained arch and improve the entrance view; the open aperture and supported deck remain readable. Some repeated texture rhythm remains noticeable in the enlarged view, without a dominant mirrored-chevron pattern in the inspected stills.
- Stock/casting supports have clearer stone-scale grain across their existing bearings and corbels. Equipment and rail contacts remain seated; the gain is primarily local finish, not a larger machinery silhouette.
- Service piers have more deliberate dark inset fields while retaining the arched openings and crown. They remain simple, dark architectural masses compared with the reference; their full-map contribution is modest.
- The overall map still has a more uniform court, sparse workyard and simpler foreground stone/iron finish than the reference. The painted cavern remains more richly detailed than much of the native foreground. The open fighting floor stays readable, but the reference gap is substantial and is not closed by this package.

## Verification boundaries and budgets

During focused verification, one run reported **239 passed / 4 failed out of 243**. Its remaining failures were the previous 760-vertex local shelf guard; intended fold subdivisions produced 925–991 vertices per completed support. The root explicitly updated that local cap from **760 to 1,024**, retaining all geometry/contact/ownership tolerances and the unchanged global ceilings. This is an intentional construction-cost budget allowance, **not an oracle correction**. That failed budget run/source is retained under `second-focused/`. Earlier failed output/source under `first-focused/` records a separate five-failure integration-oracle attempt; the two issues must not be conflated.

The focused source checks combine the existing physical-envelope/contact cases with new fold-area, field-interpolation, shared-edge, near-fold, parent-range/order and static-only tests. The support oracle checks the actual intended subdivided bearing triangles; it does not claim zero global overdraw across pre-existing hidden/crossing support returns. The full frozen Unity suite/build remains pending; the root must record its final outcome separately. Captured resource/region evidence above is not itself a passing test or Android performance result.

The package preserves camera, playable route and collision ownership, gameplay, scene configuration and save/telemetry behaviour. No device installation or phone verification belongs to this record. Later deployment still requires inspection of then-current device/run state and preservation of current original saves; historical snapshots must not replace newer progress. Resource and vertex counts are not phone performance measurements.

The reference remains unfinished. These changes address material scale and local architectural support; they do not establish full-map composition, painted-detail or final-quality parity. Device readability and moving-view acceptance also remain open despite the accepted static images.

## Final regression/build closure

438 .NET / 645 EditMode / 159 PlayMode = 1,242 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed separately. PASS: 622 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,391 bytes; SHA256 7B9DF62AAB90B43B35713D07A14181BC2111EB63DDEE4852ABDC70D6D70E5FA3; NOT INSTALLED.

The final frozen-source suite/build gate is complete. This closure supersedes the earlier pending full-suite/build statements in this document; phone acceptance remains pending.
