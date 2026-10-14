# Foundry gate masonry depth

Verified 2026-10-14. Baseline `94243b77ed59bb1c801ca3c7facc2fa11ecda2cb`.

The two outer gate buttresses now use an authored Blender masonry bake with broad fitted courses, bevel light and pointed recessed niches, retaining the original contact stations and native foundations. The two native upper header shoulders gain shallow pitched roofs and closed gables within their original bounds. Runtime cards share an unmirrored texture and calibrated 0.62 RGB gain, retain physical depth ownership and paint after their curtains; the south boiler feed body, collars and elbow paint strictly in front of the opaque near bay.

Region 36 renderers / 164672 vertices versus 34 / 167208: +2 renderers and -2536 vertices. Each header shoulder remains 136 vertices. Distant 3572 is included and unchanged; ceilings remain 174000 region / 3600 distant. Added runtime resource: one 512x1024 ASTC6 texture, 11 mips, nonreadable; Editor texture memory 630880 bytes and calculated compressed mip payload 314992 bytes, excluding other overhead. One scene-owned source material and one shared cutaway variant are added. The variant joins the existing focus-uniform update; no new updater component or shader. Scoped south-feed ordering reuses an existing iron batch. Geometry savings do not establish phone performance; texture sampling and transparent fragment work are added.

## Verification

438 .NET / 666 EditMode / 160 PlayMode = 1,264 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and final After Android-target graphics captures passed separately. PASS: 626 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,485 bytes; SHA256 649AB25FB971D8362CB0E08FA9EA60253531CEE2A3D7E834005B8AF10674D2A9; NOT INSTALLED.

Two helpers authored the masonry bake and native shoulder geometry/tests; root integrated runtime material, ordering and lifetime, while a third helper independently reviewed source and actual images. Six added EditMode cases cover production inclusion, original bearing/envelope, roof ownership, mortar and UV continuity. One added PlayMode case covers real imported texture, both card projections and depth ownership, south-feed opaque-overlap ordering, shared material gain and unload lifetime. Earlier focused Foundry 264/264 passed. Initial compile failed due to a local name collision and was repaired. First integrated graphics capture was visually rejected for curtain overpaint; second for excessive brightness. The first full PlayMode run passed159/160: the pre-existing other-batch cap counted the new gate cards and rejected25 against24. Gate cards are now counted separately (exactly2), the old other cap stays24, and the total cap explicitly rises34 to36 for the measured cost. Rejected XML/source evidence is retained. Final source freeze stayed unchanged through final capture, full suites and build.

## Evidence boundary

Twenty-one fresh Before/After pairs use the same actual Android-target Unity scene, cameras and actor staging. Combat is paused and HUD hidden. The reference is concept art. See validation/actual-image-review.md, independent-image-review.md and final-scope.md.

This is a gate-landmark improvement, not reference parity. Native inner piers, curtain/header grain and the simpler regular stone courses remain visible gaps. Existing approach-20-1 portrait crops the crown and most outer bays, so portrait benefit is limited. Map, camera, navigation, equipment placement, encounters and gameplay remain unchanged. Existing immortal-crowd waypoint congestion remains unresolved; see ArtDirection/2026-10-12/foundry-forecourt-layout-01/validation/crowd-diagnostic.md. Device motion, HUD, shimmer and midrange performance remain unverified. No phone control, installation or save/telemetry change was made. The prior concrete restart/install decision remains pending; later deployment must inspect current device state and preserve current original saves and telemetry, never restore historical snapshots. Regional progression remains deferred under the art-first direction.

## Reproduction

Run from E:/MobileGame with Unity closed:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryGateDepthProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

The Before launcher pins the baseline and requires unchanged Assets. Final source and image hashes are recorded under validation. Editable model and exact projection live in masonry-bay; its README documents reproduction. Next work is scoped in validation/next-visual-priorities.md.
