# Foundry inner piers

Verified 2026-10-14. Baseline `e6f681792a53d9c3e3204291e67d51738f2c1abd`.

The complete native inner-pier family now has a substantial raked lower haunch, a shorter recessed shaft and a single seated capital below the preserved sloping head. The shaft gently flares back to the previous upper contact planes so the fixed iron collar remains seated. Original footing, lower collar, inner returns, portal and 7.25 terminal bearing remain fixed. A family-local .11 quarry field replaces the .075 shared field only on these piers, using the same clean masonry atlas patch and preserving inherited face lighting. Header, returns and cartouche retain their existing finish and painter order. Matched images show a stronger lower bearing and a less elongated recess; the grain is finer but still visibly different from the smoother authored outer bays. This is a bounded gate-form improvement, not reference parity or a new map layout.

Region 36 renderers / 171884 vertices versus 171888: -4. Distant 3572 is included and unchanged. Ceilings remain 174000 / 3600, leaving 2116 / 28. The inner-pier family uses the existing masonry atlas/material with its own fine quarry field, preserving source lighting and the original painter/depth order. No new runtime bitmap, material, shader, renderer batch or updater. The preceding outer-bay resource retains its measured 512x1024 ASTC6 / 11-mip / non-readable budget. Static counts do not establish device performance.

## Verification

438 .NET / 699 EditMode / 160 PlayMode = 1,297 accepted suite executions, zero failures/skips. Focused Foundry 297/297 passed separately. Isolated compile zero warnings/errors. Fresh Before and final After Android-target graphics captures passed separately. PASS: 639 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 36,387,202 bytes; SHA256 C6EDF8C6975B336866423D848649FC3CC812F860E88697936A0F324E2A2839FC; NOT INSTALLED.

Three helpers contributed disjoint native pier geometry, six meaningful geometry/finish tests, and independent source/image review plus the Claude handoff audit. Root sealed 21 fresh Before views at e6f6817 before any Assets edit, integrated the family-local finish and original-order append, and coordinated the final captures, test suites, build, gallery and handoff.

Source review checked the intentional negative shaft inset, haunch/capital bearings, exposed recesses, unchanged foot/head and fixed iron contact. The six new cases check actual geometry, collar overlap, visible bay ownership, family UV/light isolation and the exact final append. The whole finished near/far pier pair changes from 3563 to 3559 vertices; the region measurement independently confirms -4.

An unsupported legacy-cap-tail assumption was removed from the new test fixture while retaining exact legacy footing/head checks and direct checks of both new indexed-cap bearings. The first focused run passed 297/297, but a freeze comparison then found that test source had changed after the initial freeze. That passing run and initial freeze are retained as superseded evidence; the initial successful graphics XML is retained as pre-final-freeze evidence. No runtime source changed in this correction. Root reran the isolated compile, froze all Assets including tests/metas, reran focused 297/297 and regenerated the complete final After capture before starting full suites/build. A passing superseded result is not counted as final acceptance.

The final source freeze must match after the complete suites and APK build. No phone operation, install, profile or telemetry change was made. The complete Claude handoff is finalized after this package's measured closure.

## Evidence boundary

Twenty-one matched actual Unity views use identical camera/actor staging, paused combat and hidden HUD. Reference is concept art.

This is a bounded inner-pier form and finish improvement, not reference parity or a new layout. Approach portrait crops most upper architecture; full-map gain is localized to the gate. Native and baked materials still differ, and the inherited curtain remains largely occluded. Map bounds, camera, navigation, equipment footprints, gameplay and encounters remain unchanged. Existing immortal-crowd waypoint congestion remains unresolved (ArtDirection/2026-10-12/foundry-forecourt-layout-01/validation/crowd-diagnostic.md). No phone control, installation or save/telemetry change. Device motion, HUD, shimmer and midrange performance remain unverified. The earlier concrete restart/install decision remains pending; later deployment must inspect current state and preserve current original saves/telemetry, never restore historical snapshots.

## Reproduction

Run with Unity closed:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryInnerPiersProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

Before pins exact baseline HEAD and unchanged Assets. Runtime native source is FoundryRegionGateInnerPiers.cs and FoundryRegionGatePierFinish.cs. The preceding Blender outer-bay resource remains unchanged.
