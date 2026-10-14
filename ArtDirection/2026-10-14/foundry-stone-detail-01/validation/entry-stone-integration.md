# Entrance haunch stone integration

Source scope: `FoundryRegionBridgeSupports.cs` and two additional cases in `FoundryFortressHierarchyTests.cs`. Root owns compilation, captures, regression/build execution and shared material changes. No phone operation belongs to this work.

The actual previous `bridge-foot` view showed an articulated primary arch beneath broad, unjointed triangular spandrels. The new emitter partitions only those existing primary-spandrel faces into three large horizontal masonry courses, with two shared bed heights and staggered broad blocks. Dark mortar occupies its own narrow polygon cells; no flat parent face remains underneath. Subtle top-to-bottom colour varies within the real courses. The existing final parent-grain pass still owns the assembled UV coordinates.

The eleven original intrados stations, all outer parent corners, primary bearing boundary at -3.03, deck contact at -2.7, side responds, rear arches, support depth, lanterns, scenery ownership, sort ranks and movement remain unchanged. No new resource, material, renderer, updater or per-frame work is introduced. Shared clipping/material helpers were not edited.

The local clipping helper snaps its cut axis to the authored joint station and interpolates the other coordinate in double precision before storing float vertices. Adjacent clipped cells share cuts. It removes exact duplicate vertices and excludes only zero-area polygons/triangles. This is a partition within the old silhouette, not an expansion of the arch envelope.

Two additional cases exercise all eight non-empty original primary-spandrel faces: independent original-polygon area, retained boundary corners, nondegenerate triangles and single ownership at double-precision triangle centroids; the full production bent is also sampled across old wedge boundaries at both shared mortar beds. Existing aperture, deck contact, upper-bearing separation and material-field cases were retained unchanged in this helper's edits. Root may separately update the existing field oracle for its coordinated material revision.

Source-arithmetic diagnostic predicts 130 vertices/64 triangles replacing 32 vertices/16 triangles in the eight primary spandrels: +98 vertices and +48 triangles for the one primary bent. The diagnostic was a JavaScript float32 arithmetic reconstruction, not a Unity execution. The focused test retains the declared +400-vertex maximum. Actual integrated region counts and verification results must come from root's capture/regression gate.

Source implementation is ready for root verification. No compile, Unity test, APK build, capture or device result is claimed here. These changes address one local masonry transition; the reference remains a broader unfinished visual target.
