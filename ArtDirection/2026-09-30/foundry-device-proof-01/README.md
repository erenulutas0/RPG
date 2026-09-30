# Foundry depth proof and render stats on the S23 — 2026-09-30

Installed build: APK 32,305,422 bytes, SHA-256 `2B2EFFCDB8544F69A61594D1A27C54C122C1613B67EF0DB5CBC20F26276CCF76`
(installed base.apk hash matches). New in it: health bars stay on top of scenery (`FoundryVisibilityView`,
`EnemyLookView.Bar`), and two development-only tools, both off unless a flag file exists in a development build:
`Scripts/Core/DepthProofArrangement.cs` (`development/depth-proof.txt`) and `Scripts/Core/RenderStatsProbe.cs`
(`development/render-stats.txt`). The APK is smaller than the previous ones only because Gradle's incremental
packaging had left dead space in them; the packed contents are the same apart from the code (+22 KB `libil2cpp.so`).

## Silhouettes on the phone (first time)

`device-depth-proof.png`: with the depth-proof flag, a Grunt, the chest and a drop stand behind the south-west boiler,
the fight is stopped, the hero stands in front. On the S23 (Vulkan, Adreno 740) the parts covered by the boiler draw
as silhouettes exactly as in Unity - ember for the Grunt, gold for the chest and the drop - and the uncovered parts
draw normally; the Grunt's health bar stays on top. So the structure depth pass and the `ZTest Greater` silhouette pass
work on this GPU/API.

## Render stats (development build, 5 s windows)

- `render-stats-depth-proof.log` (static proof scene): 59.9 fps at the 60 Hz vsync cap, frame p50/p95 16.7 ms, GPU
  2.2-3.2 ms, 50 draw calls / 44 SetPass, 39k triangles, 127 MB total used, 16 MB textures, ~15 B GC per frame.
- `render-stats-combat.log` (a normal run left to fight on its own for ~45 s, `device-combat.png`): 59.9 fps, p95
  16.7 ms, worst frame ~17 ms after start-up, GPU 2.9-3.2 ms of the 16.7 ms budget, draw calls rising 50 -> 98 as the
  swarm grows, 127 MB, 27-428 B GC per frame.
- So today's depth pass, two-pass silhouettes, per-render moves and lighting leave large headroom on the S23. Not a
  sustained or thermal test; a larger late-game swarm was not measured.

## Phone state

Backups in `TestResults/foundry-proof-install-20260930` (profile revision 221, 33,370 gold; unchanged throughout).
Both flag files were removed afterwards (`development/` holds only `region-preview.txt` and `start-floor.txt.off` again).
The last test run was left paused via Home in Slag Gate at 16 HP; it banks nothing. Logcat: 0 exception/shader-error
matches.
