# Development wide-region integration

Unlike the previous temporary-scene proofs, Gameplay now supports a development-only wide region through `development/region-preview.txt` containing exactly `wide` (surrounding whitespace allowed). Missing, unreadable or unknown values preserve authored hall geometry. Release builds short-circuit before filesystem reads. ArenaView resolves this once on first geometry request, including requests before Awake, and retains it until scene reload. No per-frame IO or mid-run geometry switching.

Wide geometry remains near/far=-27/+27, halfWidth18. FoundrySceneryView creates one furnace plus a foundation at the northern perimeter, borrows the imported sprite/floor material and owns/disposes only its mesh. The scene carries an optional furnace reference; no spawning per frame. No walkable extension, collision, forge interaction or local encounters added. Existing floor selection, combat, stamina, saves and progression remain their own authorities. This is a development traversal/combat preview, not a finished exploration loop or a replacement for the boss hall.

`RegionSceneryTests` covers missing/unknown flags, enabled geometry/art, frozen loaded geometry, disabling on reload, owned mesh destruction and borrowed art survival. Initial test compile referenced absent Physics2D; removed the unnecessary assertion without adding a package. Initial sprite YAML used an obsolete sliced subasset ID; corrected to the single-sprite21300000 reference and the real-scene test passed.

`Run-RegionRuntimeProof.ps1` stages only a capture fixture. RegionRuntimeCapture loads actual Gameplay with the development flag in isolated TestProfile and walks the hero to the northern landmark. It disables encounter progression/attacks only for the graphics inspection; the development flag itself does not disable combat. HUD graphics-fixture caveats from furnace-unity-01 still apply.

Device use: first preserve current app files and any existing development flag. Install only after the complete gate. Write the new flag only for the coordinated development review, restart the app, then remove only the test-created flag and return to the original scene. Never restore stale profile files or describe simulation parity on this geometry as established by the art capture.

## 2026-09-23 — Wide region and furnace development integration

Gameplay now binds Furnace.tga and ArenaView supports a once-resolved development/region-preview.txt=wide flag. Default hall and release behaviour preserved; wide preview is -27/+27/18. FoundrySceneryView borrows art/material and owns its foundation mesh. No local encounters, collision/elevation or new progression rules. Graphics capture now loads actual Gameplay, not an enlarged scene copy. See ArtDirection/2026-09-23/region-runtime-01/README.md for commands, proof and limits.

Verified: .NET380, compile0 warnings/errors, EditMode395/395, PlayMode130/130, targeted region2/2, graphics1/1, browser390px checked. Android exit0; actual APK29,252,655bytes, SHA256 C8B4592C03EF35A437BF7EB027B54D484DD333C48946EEF212D0B793D9F12704. APK is BUILT, NOT INSTALLED. USB device enumerated only; availability question unanswered, no launch/input/screenshot/profile access. Prior XP-only installed record remains current. No commit/push in this slice.

Next: coordinated fresh app-files backup, install and hash verification, temporary wide flag (preserve any existing value), physical traversal/visual review, remove only the created flag and return to normal hall. Playing normally may award/persist progress; never restore stale profile data. After that: local encounter-site gameplay and richer perimeter scenery.

## 2026-09-23 — Wide-region APK installed and device traversal

Owner authorized phone use. Fresh9-file backup in ignored TestResults/region-device-20260923-151946, app force-stopped, install --no-streaming -r succeeded. Installed base.apk SHA256 C8B4592C03EF35A437BF7EB027B54D484DD333C48946EEF212D0B793D9F12704 matched. All9 files byte-identical immediately after install. Fresh stored profile189/27099gold/defaultSword/SecondWind/deepest3 supersedes previous186 state.

Created only region-preview.txt=wide; original inactive start-floor.txt.off preserved. First walk was contaminated by owner inputs (owner confirmed), so no conclusions from its progression. After explicit handover restarted the wide run and sent a guarded25s north drag. Reached north furnace/rim; camera follow and fixed-scale paving visible on physical S23. Screenshot shows furnace overly occluded by hero at the exact tip: placement spacing needs polish, not final visual acceptance. Normal combat remained active and progressed to room2; no explicit upgrade selection or economy edit by assistant. This is a visual/traversal smoke test, not a performance benchmark.

Removed the test-created region flag, force-stopped and returned to normal Ember Hall1/6. Paused at100HP/level0/XP0/runGold0 for handback. Primary and backup profiles match the fresh preinstall files byte-for-byte; telemetry naturally grew, no rollback. All taps/swipes/captures checked focus/awake/unlocked before and capture guard after. Last1200 log lines had no FATAL EXCEPTION/NullReferenceException/MissingReferenceException matches. Evidence: region-runtime-01/device/north.png and handback.png. No code/build changes in this device slice.

## 2026-09-23 — Furnace clearance and physical exploration handoff

Moved furnace to an offset north-east ornamental foundation (x1.6, worldTop+.45); playable geometry unchanged. Actual Gameplay graphics now walk to the reachable tip and side using HeroMotion.EdgeMargin. An initial waypoint at raw corner27 failed as unreachable; fixed the fixture to use the existing0.6 margin rather than changing movement. Final captures show hero/furnace separation at both inspected limits. Fixture HUD overdraw remains an Editor capture limitation; this does not establish new device acceptance.

Verified: .NET380, compile clean, RegionSceneryTests2/2, graphics1/1. No full-suite/APK/device operation for this bounded layout correction; installed C8B4592C build still has original centred furnace. New OPUS_EXPLORATION_TASK.md covers two physical combat sites, optional single-use chest and unlocked boss entrance; drafted only, not sent or started. Existing region-map node task remains superseded. See region-runtime-01 updated screenshots and handoff.
