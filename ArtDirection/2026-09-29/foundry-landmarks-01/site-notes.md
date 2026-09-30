# Bridge and shaft site finish — 2026-09-29

Owned source: FoundryRegionSite.cs only.

Four existing bridge gateposts receive tiny cast-iron brazier sockets and two dark inset panels with a restrained bronze lozenge outline. Stone silhouette, locations, bilateral symmetry and the brazier anchor at height 1.65 are unchanged. Panels stay inside the existing 0.6-unit feet; no walking obstruction is introduced.

The shaft collar now carries the same segmented masonry coping on all four inner sides instead of two. All masonry stays inside design bounds (-12,-4) x (2,10). Four narrow iron retaining saddles sit on the existing near ledges; their short anchor legs hang into the shaft. The opening stays clear and the original crossbeam/chains and corner beacons remain unchanged.

Reuses FoundryWall at logical -11, explicit existing FoundryIron overlay -10.5, and existing inset iron -15.4. No new batch keys, imported assets, gameplay, scene, collision or frame update. Source-count delta: 260 quads / 1,040 vertices / 520 triangles (72 bridge-panel/socket quads, 176 additional coping quads, 12 saddle/anchor quads). Root must record the actual final integrated census.

Pending root compile and real render review. Inspect bridge closeup for readable subtle panel/socket joins; shaft closeup for far coping not covering lining excessively and retained clear walk perimeter. No Unity or adb action performed by this subtask.

