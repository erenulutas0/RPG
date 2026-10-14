# Foundry bearing architecture

Verified 2026-10-14. Baseline `cf511c38f325f43ec3f9d9005ed5b638c1bc0db1`.

Service-tower crowns now have joined recessed bearing bands and whole-head mineral sampling. Four working shelves and two casting cantilevers use raked stone knees beneath continuous bearing beds. The entrance bridge gains a carved inner return and shoulder while its front and soffit share one physical grain field; near-island arch bays and responds receive coherent parent-surface grain.

Region 34 renderers / 160561 vertices versus 34 / 161385 (-824 vertices). This region total includes the distant meshes, which are 3540 versus 3572 vertices (-32); do not add that saving again. No new runtime texture, material, renderer, updater or per-frame work is introduced. The existing startup mesh/UV generation performs the changes. Static counts do not establish device frame time.

## Verification

438 .NET / 631 EditMode / 159 PlayMode = 1,228 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed separately. PASS: 620 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,391 bytes; SHA256 6AF3C51E2A249A856391B4D5B214820C3E2E008AE78D4DDFDF842FAFD6B57DD4; NOT INSTALLED.

Two helpers implemented disjoint tower and shelf scopes; root implemented bridge/undercroft and completed integration when a helper hit its usage limit. The independent reviewer resumed after the limit and completed both nine-file source review and fresh image review. No source blocker remained. Ten additional EditMode cases and strengthened existing cases cover actual geometry, envelope contacts, single profile ownership, aperture preservation and connected material fields. All source remained identical to the freeze through capture, suites and build.

## Evidence boundary

Twenty-one fresh Before/After pairs use the same actual Android-target Unity scene, cameras and actor staging. Combat is paused and HUD hidden. The reference is concept art. See validation/actual-image-review.md, independent-image-review.md and final-scope.md.

This is a local bearing-architecture improvement. Map, camera, navigation, equipment placement, encounters and gameplay remain unchanged. Broader continuous grain reduces stamped repetition but is softer at close range than the reference; reference fidelity is unfinished. Existing immortal-crowd waypoint congestion remains unresolved; see ArtDirection/2026-10-12/foundry-forecourt-layout-01/validation/crowd-diagnostic.md. Device motion, HUD, shimmer and midrange performance remain unverified. No phone control, installation or save/telemetry change was made. The prior concrete restart/install decision remains pending; later deployment must inspect current device state and preserve current original saves and telemetry, never restore historical snapshots. Regional progression remains deferred under the art-first direction.

## Reproduction

Run from E:/MobileGame with Unity closed:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryBearingArchitectureProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

The Before launcher pins the baseline and requires unchanged Assets. Final source and image hashes are recorded under validation. Next work is scoped in validation/next-visual-priorities.md.
