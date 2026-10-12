# Independent service-skyline source review

2026-10-12. Reviewed by court helper, read-only. Scope: `FoundryRegionDistantWorks.cs`, `FoundryServiceShaftTests.cs` and the skyline source note. No blocking source defect found in the requested contacts, hollow crowns or cost controls.

- Actual production helpers retain foundation elevations -26/-25. The new top/sill offsets resolve to the original furnace sills -18.2/-20.2. Tests inspect emitted foot vertices and production helpers, with independently authored upper-envelope limits.
- Shoulder, leaning buttress head and neck share the same reduced rise. Resulting neck intervals remain positive (approximately 1.54/1.94 units before inset sill/header margins). Crown bases share the neck's upper bearing, and the hollow throats retain positive widths. Existing connected-surface and mouth edge-incidence tests remain.
- Arch endpoints resolve to -7.4/-6.8. Their springings at another -5.2 and bearing shoes between -6.3/-4.3 fit within the corresponding retained furnace-core height intervals. Endpoint and springing coverage tests exercise the actual service-tower meshes. The central missing segments and unequal fractured ends remain; no continuous new route is emitted.
- Six fire panes per shaft are deliberately repositioned inside shorter bays, not left at the former upper elevations. Both shortened variants retain positive pane heights and meaningful gaps. The exact test layout was updated for this intentional architecture change, while UV, count and containment guards remain.
- Existing primitive/tessellation counts are retained and the combined 3,600-vertex ceiling is unchanged. The expected 3,572 count still requires root's actual capture; this review does not establish runtime resource or performance results.

One wording nuance for final documentation: the material settings and color coefficients are unchanged, but `FinishDistantStone` samples its existing height-relative fade/heat with the new top values, so resulting vertex colors are not necessarily byte-identical. This is consistent with a lowered structure and is not a blocker.

Actual-image review must confirm that the lowered works and attached span improve the whole-map hierarchy without a new silhouette/contact issue. Assets were not edited, and no Unity, ADB, suite or Git action was performed during this review.
