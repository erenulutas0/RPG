# Industrial atlas integration review

Independent code and contract review, 2026-10-11. Baseline: `2c3c836`.

Reviewed `FoundryRegionBakedBoiler.cs`, the changed `FoundryRegionTests.cs` assertions, shared mesh/depth/material lifetime code, atlas packing, and industrial capture tooling. This is a source review before final import and Unity validation, not final visual or device acceptance.

## Findings

- No runtime integration blocker found. Two shared atlas source materials retain four depth-band batches. Card sizes, vertical split, sorting ranks, foundation depth owners and cutaway flags remain unchanged. Near instances select the left tile; far instances select the right tile.
- Tests reject swapped tile coordinates, moved or resized cards, cross-card triangles and extra overlay geometry. Existing ownership checks cover source materials, cutaway variants and meshes while preserving borrowed Resources textures.
- Packing preserves each source tile's pixels without resizing. Observed horizontal alpha bounds in the reviewed 1024-square renders: tall variants `318..706`, short variants `169..855`. These provide substantial transparent separation at normal viewing scales. The final 1x1 mip inevitably averages the entire atlas; this review does not claim isolation at every possible scale.
- A 2048x1024 POT atlas correctly has 12 full mip levels. Final metadata/import still needs max2048 on applicable settings, unchanged GUIDs, clamp addressing, non-readable textures and loaded ASTC6x6 verification.

## Resolved tooling issue

The capture initially accepted a `before` phase while unconditionally asserting the new atlas dimensions and mip count. That could not reproduce the 1024-square baseline. The launcher now permits only `after`, and the capture asserts `after`. Baseline images are copied from the completed compact-gate proof with SHA-256 provenance rather than presented as a fresh capture.

## Pending at review time

Final source import, loaded texture observations, actual Unity images, complete tests/build and resource-budget confirmation remain root-owned pending work. No Unity or ADB command was run by this reviewer. No performance or physical-device claim is made.
