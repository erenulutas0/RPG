# Foundry material integration

2026-10-12. Baseline `3d854256d88863793b9094a71e6017c32ef82a3f`.

Gate recesses now map the existing stone grain by real block dimensions, continue it over bevels and retain inherited corner lighting plus a shaded-head/reflected-foot gradient. Pit ribs use a quiet cast-iron atlas interior at bounded grain scale with restrained integral worn arrises; the under-rail bearing uses cool masonry and actual narrow block beds. The medallion ring and fitted centre flags gain flush dressed shoulders and share the surrounding court quarry family, reducing the separate warm checker pattern. Existing footprints, silhouette, pit fire and vessels, door/lock, navigation, camera, scene, gameplay and saves remain unchanged.

Three helpers owned gate, pit and court source; source reviews were crossed and root alone ran Unity. Nine Before images were copied from committed baseline 3d854256d88863793b9094a71e6017c32ef82a3f and verified byte-for-byte against Git objects. Nine matching actual After views retain the same camera/actor setup. Root and helpers inspected actual images. Two new court geometry cases guard dressed-face area, containment, noncollapsed shoulders and continuous world grain; existing guards were retained. Initial graphics launch exited 198 during license initialization. After browser sign-in succeeded, the owner explicitly authorized accepting the Unity Personal terms and activating the license; activation exited 0 and the license became active. Successful Unity results were obtained only after this renewal. The initial sanitized failure and recovery record are retained.

Region 32 renderers/166467 vertices versus 165232 (+1235); distant geometry 3572 unchanged. No new runtime texture, material, renderer or per-frame updater. These resource observations are not device-performance measurements.

## Verification

425 .NET / 512 EditMode / 159 PlayMode = 1,096 distinct suite cases, zero failures/skips. Isolated compile zero warnings/errors; Android-target actual graphics capture passed. PASS: 602 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0, no C# or shader diagnostics. APK 36,260,221 bytes; SHA256 993B2DA7B72A5A7D6A0FF2923DF29EB41A55917D33B45AF2EE105760DB08D8D0; NOT INSTALLED.

Full reference material richness and physical-device motion/crowd/HUD/shimmer/midrange acceptance remain unfinished. No ADB, installation or fresh phone observation. The earlier concrete restart/install decision remains pending. Before future deployment inspect current device state and preserve current original saves/telemetry; never restore historical snapshots. Regional encounter/boss progression stays deferred under the art-first direction.

See validation/actual-image-review.md for bounded visual acceptance and remaining differences.

## Reproduction

Run with Unity closed from E:/MobileGame:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryMaterialIntegrationProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

The matched Before files are committed baseline evidence, retained with Git-object/SHA256 provenance; do not recreate them from changed runtime. The capture helper temporarily imports its PlayMode fixture and removes it afterward. Actual scene captures pause combat and hide HUD; they are not phone screenshots. Graphics cases are separate from the suite count. The approved reference is concept art.
