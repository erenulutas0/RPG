# Service-shaft pier integration

Baseline: `d9f33320bac6eb9fe9c5873e163337025f9ad747`. Scope is limited to `FoundryRegionDistantWorks.cs` and `FoundryServiceShaftTests.cs`.

The two flat pier rectangles beside each service bay are replaced by actual shallow blind fields. Each retains the original outer rectangle and adds one inset contour: four sloping bearing surfaces surround one recessed back. The back sits 0.12 design units behind the face, with side bearings occupying 22% of the existing pier width and head/sill bearings 0.85 units high. The broad back has a restrained 0.78 value response under the unchanged height gradient. There is no original flat pier face beneath the replacement. Existing arched frames, core and their intentional independent geometry remain unchanged.

Each replacement uses eight shared indexed vertices and ten triangles instead of four vertices and two triangles. Eight actual pier fields across the two service towers therefore add **32 vertices and 64 triangles / 192 indices**. Expected distant total: **3,572**, within the existing 3,600 ceiling. This is a source-derived delta; the root's fresh capture will supply the measured full-region and distant totals. Distant vertices are already included in the region count.

The existing facade-wide world-coordinate masonry mapping covers all new surfaces. There is no per-field texture restart, material, renderer, resource or per-frame work. Crown geometry, open mouths, arch/fire openings, outer shoulder/sill contacts, fixed tower stations, foundation feet, background fade and machine/portal contacts are unchanged. No global stone-grain helper was edited.

Four focused test cases inspect the actual production `DistantRecessFace` at both authored tower dimensions and both visible orientations. They check the eight-vertex indexed ownership, closed inner/outer boundary incidences, exact projected-area partition, positive triangle winding, dense strictly interior one-owner samples, original footprint containment, continuous existing facade UV and subordinate recessed-field value. Existing full service-tower opening, contact, head and budget tests remain unchanged.

A separate float32 Python arithmetic diagnostic reproduced the eight authored pier contours and the same 81 interior samples per pier: all 648 samples had one owner, every triangle had positive projected area (minimum twice-area 0.3250994), and each partition's total area equalled its original rectangle exactly in the diagnostic. This is a numerical cross-check only, not a Unity test result.

Assets frozen after implementation. Root compile, Unity suites and matched image review remain pending. No Unity, build, Git, ADB, phone or save actions were run by this helper. Visual acceptance must judge whether the broad pier structure reads at full-map and portrait scale; this source change does not establish reference parity.
