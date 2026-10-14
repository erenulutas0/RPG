# Foundry court and machinery cohesion

Verified 2026-10-14. Baseline `722b3b23a18a3df2af89751da70ae81193ece511`.

Broad material use now distinguishes the scrubbed entry from the settled working apron while preserving the quiet combat centre. The moulding bench stores supported open spare flasks and timber packing; its worked iron agrees with the crane casting. Exposed gate stone has a bounded face-dependent quarry value lift, and rolled service pipes use cooler iron beneath existing reflected fire.

Region 34 renderers / 161385 vertices versus 34 / 161161 (+224 vertices); distant geometry remains 3572 vertices. No new runtime texture, material, renderer, updater or per-frame work is introduced. Existing startup paving and material generation performs the added bounded colour sampling. Static counts are not device performance evidence.

## Verification

438 .NET / 621 EditMode / 159 PlayMode = 1,218 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed separately. PASS: 620 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,391 bytes; SHA256 4F696238FBB970EC4965E71B4D92E0210EBB453DBB5D9B5E231487E664BD390E; NOT INSTALLED.

The first full run passed 620 of 621 EditMode cases and all 159 PlayMode cases; APK production was skipped. A legacy mineral-isolation oracle removed world light but retained the new intentional broad use field. Its test helper now removes both fields, retaining all original tolerances and mineral/shoulder limits. Seven new actual-emission cases independently recover gain without invoking the new field function, preserving detection of missing, doubled or discontinuous use. Runtime source did not change for this repair. Root reran pure checks, isolated compile, matched After capture and the complete regression/build gate from a refreshed source freeze. The rejected run and original test source remain in first-suite/.

## Evidence boundary

Twenty-one fresh Before/After pairs use the same actual Android-target Unity scene, cameras and actor staging. Combat is paused and HUD hidden. The reference is concept art. See validation/actual-image-review.md and final-scope.md for the extent of acceptance.

This is a modest material-cohesion improvement, not a new layout or completed reference reproduction. Camera, map bounds, navigation, gameplay and encounters remain unchanged. Existing immortal-crowd waypoint congestion remains unresolved; see ArtDirection/2026-10-12/foundry-forecourt-layout-01/validation/crowd-diagnostic.md. Device motion, HUD, shimmer, crowd and midrange performance remain unverified. No ADB operation, installation or save/telemetry change was made. The prior concrete restart/install decision remains pending; later deployment must inspect current device state and preserve current original saves and telemetry, never restore historical snapshots. Regional progression remains deferred under the art-first direction.

## Reproduction

Run from E:/MobileGame with Unity closed:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryCourtCohesionProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

The Before launcher pins the baseline and requires unchanged Assets. Final source hashes and image hashes are recorded under validation. Next work is scoped in validation/next-visual-priorities.md.
