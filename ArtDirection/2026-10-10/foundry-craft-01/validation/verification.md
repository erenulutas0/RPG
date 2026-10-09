# Foundry craft verification - 2026-10-10

## Changes and review

Three reference-facing tasks integrated: varied court/workyard ashlar with restrained mineral and wear treatment; broad blind arches/dressed gate masonry; substantial stock loading hoist within existing shelf/depth ownership. Root and three helper reviews accepted actual before/after court, portrait, stock, gate and full-map images. No walking footprint, gameplay, camera or scene changes. Fine stone grain/edge irregularity and machinery material richness still fall short of the reference. This is not final map-art acceptance.

## Executed checks

- Pure model: `dotnet test Tools/CombatChecks/CombatChecks.csproj --no-restore --nologo --verbosity minimal` - 409 passed.
- `pwsh -NoProfile -File Tools/Run-UnityTests.ps1` - EditMode432 passed; PlayMode151 passed; zero failures/skips. Total992 tests. Three new packer cases verify coverage, zones, seal bond, forced brass joints and varied workyard sizes across deterministic seeds. Unity6000.0.65f1.
- Graphics: one baseline before editing and one combined after capture passed. XML retained under each phase. Phase chooses destination only; baseline a4bdc7d source is required to reproduce before.
- `pwsh -NoProfile -File Tools/Verify-Project.ps1` -571 unique asset/folder GUIDs,1099 package GUIDs,568 scene objects/components; resolved references and valid JSON. Executed with Unity closed after build.
- Android build exit0. No C# warning/error matches in build log. Gallery references resolve; whitespace review passed.

Region geometry:25 renderers unchanged;113,622 ->117,754 vertices (+4,132;3.64%). No new texture/material/update component. This is a static geometry count, not FPS or memory benchmarking. Budget texture line describes unchanged FoundryShortBoiler.

## APK and phone

Built `Builds/Android/Cryptforge-Prototype.apk`:36,153,404 bytes; SHA256 `035EFC2FCED469CBA94F1129051A46EC608F1091947C8A6A232D3A6A0F3944BE`.

NOT INSTALLED. Earlier owner-run restart/install question remains unanswered. Last observed state from the previous package was paused Room2/74HP/8unbanked gold; no fresh phone observation is claimed. No ADB input, install, stop, device flag or profile writes were performed this turn. Last recorded installed package remains021b95f. Take a fresh preflight/backup after permission, install latest APK, then guarded walk/combat checks and original-file hash/flag cleanup. Never restore an older save over owner progress.

Physical-device acceptance, cold-first-ramp hitch investigation, sustained crowded combat and midrange performance remain open. Latest owner direction is map art first; local encounter and boss progression work stays deferred.