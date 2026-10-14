# Foundry workyard finish

Verified 2026-10-14. Baseline `d4424447705aa85bf078ed5fb46ab11c92555b91`.

Existing service-tower exposed bearings gain restrained neutral quarry value while their sheltered fields remain dark. Native winch and mould-bench iron share a quieter cool finish, and workbench timber is less orange. Existing paving receives one soft equipment-contact albedo field that reduces the amber cast near supported stock/casting/salvage shelves and working islands, preserving the quiet centre and raised gate.

Region 34 renderers / 167208 vertices, unchanged from baseline; distant 3572 is included and unchanged. Global ceilings remain 174000 region and 3600 distant, leaving 6792 and 28 vertices. No new geometry, runtime textures, materials, shaders, renderer batches or per-frame updater components. Existing startup colour calculations change; counts alone are not device performance evidence. Floor geometry, UVs, ownership, contacts and the existing light function remain unchanged; the new contact albedo intentionally modifies the final RGB response near equipment.

## Verification

438 .NET / 660 EditMode / 159 PlayMode = 1,257 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed separately. PASS: 622 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,391 bytes; SHA256 A967EFA4B65337DD7F0B401611520600E47EC5D1172C8A416D8009676049774B; NOT INSTALLED.

Two helpers implemented disjoint service-stone and native iron/timber scopes; root implemented shared paving-contact albedo and integration. A third helper independently reviewed source and fresh native images. Fifteen additional EditMode cases cover actual emitted colour, exactly-once field application, shared-joint continuity, bounded shoulders, protected combat areas and unchanged geometry. Existing mineral/scalar-use isolators explicitly remove the new independent albedo; existing tolerances remain. The timber palette expectation intentionally changes to muted hardwood. Focused Foundry258/258 passed; no rejected run. All Assets remained identical to the freeze through accepted capture, full suites and build.

## Evidence boundary

Twenty-one fresh Before/After pairs use the same actual Android-target Unity scene, cameras and actor staging. Combat is paused and HUD hidden. The reference is concept art. See validation/actual-image-review.md, independent-image-review.md and final-scope.md.

This is a modest coordinated material improvement, not a workyard rebuild or reference parity. The existing warm floor band remains visible. Baked equipment, dense native iron grain, simpler prop silhouettes and broad native gate stone remain unfinished reference gaps. Map, camera, navigation, equipment placement, encounters and gameplay remain unchanged. Existing immortal-crowd waypoint congestion remains unresolved; see ArtDirection/2026-10-12/foundry-forecourt-layout-01/validation/crowd-diagnostic.md. Device motion, HUD, shimmer and midrange performance remain unverified. No phone control, installation or save/telemetry change was made. The prior concrete restart/install decision remains pending; later deployment must inspect current device state and preserve current original saves and telemetry, never restore historical snapshots. Regional progression remains deferred under the art-first direction.

## Reproduction

Run from E:/MobileGame with Unity closed:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryWorkyardFinishProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

The Before launcher pins the baseline and requires unchanged Assets. Final source and image hashes are recorded under validation. Next work is scoped in validation/next-visual-priorities.md.
