# Fortress and bridge independent source review

Read-only review of final frozen `FoundryRegionUndercroft.cs`, `FoundryRegionBridgeSupports.cs`, `FoundryRegionFortressSurface.cs` and `FoundryFortressHierarchyTests.cs`/metadata. No code changes, Unity, compile, ADB or Git operations were performed for this review.

No source blocker found in the reviewed scope.

- `BuildFortressArchFace` retains the original thirteen-piece intrados and backing. Primary arches keep the dressed ring; secondary bands are reduced inside the earlier ring envelope. Pier cadence and terminal support creation remain unchanged, so major-pier sockets and the selected south chain do not silently switch to another architecture rule.
- `BuildFortressRespond` keeps original foot depth, tapered outside face, bevel and shoulder extents. A broader central face replaces the old equal-width facets on primary supports. The secondary panel partitions the original projected face with frame, returns and backing; it is a projected inset treatment, not new 3D depth or an expanded footprint.
- Socket emission was separated into `BuildUndercroftSocket` without moving the existing plate or changing major/screening conditions. Its stone backing remains present through the revised primary respond.
- `BuildBridgeBent` retains the eleven-segment opening and returns. Only the fixed-mouth bent receives the broad upper bearing. The primary ring/spandrel is truncated at -3.03 before the new -3.03 to -2.7 bearing is emitted, so there is no old upper spandrel left spanning beneath that entire band. Rear bents keep the original opening and depth while receiving quieter faces.
- The new tests use actual emitted helpers, preserve independent original tapered-envelope/socket checks, sample the polygonal opening and deck contact, and reject triangles straddling the new bearing boundary. The new test metadata is a valid 32-character hexadecimal GUID. The asserted quiet UV region matches the existing bounded mineral helper.

The source establishes containment and construction intent, not visibility acceptance. Matching actual images must still show readable primary/secondary hierarchy without black strips, repeated arch overlap or a visually closed bridge opening. Root owns all actual Unity suites and resource totals; no test-execution result is claimed here.
