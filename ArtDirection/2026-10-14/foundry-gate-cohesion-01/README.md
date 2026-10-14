# Foundry gate cohesion

Verified 2026-10-14. Baseline `3474e356e0535e145f94e824eadaeda57231875f`.

Inner gate jambs now use four fitted stone courses and separately owned mortar beds instead of full backing faces with overlaid blocks. Active curtain wings gain real blind pointed bays with connected 0.18-unit returns and closed shallow pitched coping. A gate-local fixed-scale mineral field joins the native inner piers, crown, curtains and landing shoulders, using a clean area of the existing atlas and preserving the original face light, door opening and bearing stations.

Region 36 renderers / 169119 vertices versus 36 / 164672: +4447 vertices (2.70%). Distant 3572 is included and unchanged. Retained ceilings are 174000 region / 3600 distant, leaving 4881 and 28 vertices. No new runtime texture, material, shader, renderer batch or updater. Each fitted jamb uses 272 vertices/280 triangles versus 392/196 before the final grain finish; each curtain uses 2032 versus 2372 vertices before finishing. Final grain adds construction-time tessellation and indices. Local vertex savings are not a performance claim; physical-device timing and shimmer remain unmeasured.

## Verification

438 .NET / 681 EditMode / 160 PlayMode = 1,279 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and final After Android-target graphics captures passed separately. Focused Foundry 279/279 passed. PASS: 631 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,485 bytes; SHA256 E7B574E2F7B9CA149DC978C338A3A669D92E906BD8A32C1379BB760D21A3C64C; NOT INSTALLED.

Two helpers implemented disjoint jamb and curtain construction/tests; root integrated the gate-local material field, and a third helper independently reviewed source and actual images. Fifteen added EditMode cases cover exact bearings, aperture/overpaint, one-owner faces/roofs, connected UV fields and fold interpolation. Initial Unity test compilation rejected access to internal FoundrySurface; the test now uses independent public projection. First executable focused run passed278/279; the far-curtain oracle used an unrounded intermediate coordinate. Explicit Vector3 storage repaired the oracle with unchanged runtime and strict1e-8 clipping checks. A first graphics candidate was rejected for a newly exposed dark atlas drip on the crown; its wider U patch(.04-.39) was replaced by the clean .04-.21 interior at the same .075 density. The superseded full run passed681EditMode, then PlayMode was intentionally stopped for that visual repair and no APK was built. Rejected source, images, XML and diagnostic evidence remain under validation. Final accepted Assets freeze remained unchanged through final capture, full suites and build.

## Evidence boundary

Twenty-one fresh Before/After pairs use the same actual Android-target Unity scene, cameras and actor staging. Combat is paused and HUD hidden. The reference is concept art. See validation/actual-image-review.md, independent-image-review.md and final-scope.md.

This is a bounded gate construction/material improvement. The whole-map difference is modest; full reference parity remains unfinished. Native stone is still more regular and visibly textured than the smooth baked outer bays. The existing approach-20-1 portrait crops most crown/curtain detail; its benefit is limited to lower jamb and landing contacts. Broad warm paving bands, sparse working groups and simpler near-island faces remain larger scene-level gaps. Map, camera, navigation, equipment placement, encounters and gameplay remain unchanged. Existing immortal-crowd waypoint congestion remains unresolved; see ArtDirection/2026-10-12/foundry-forecourt-layout-01/validation/crowd-diagnostic.md. No phone control, installation or save/telemetry change was made. The prior concrete restart/install decision remains pending; later deployment must inspect current device state and preserve current original saves and telemetry, never restore historical snapshots. Regional progression remains deferred under the art-first direction.

## Reproduction

Run from E:/MobileGame with Unity closed:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryGateCohesionProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

The Before launcher pins the baseline and requires unchanged Assets. Final source and image hashes are recorded under validation. Next work is scoped in validation/next-visual-priorities.md.
