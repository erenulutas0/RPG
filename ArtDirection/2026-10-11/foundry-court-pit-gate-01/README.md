# Court, furnace pit and gate architecture

2026-10-11. Baseline `66e68d34409f00441f1048ed2740bb809c4a93e0`.

## Result

The court compass now uses restrained paired brass facets and a continuous engraved ring over three fitted stone courses. The pit upper lining has a joined below-deck bearing course and four relief ribs with side returns, leaving all furnace bays, vessels and the molten seam unchanged. Four gate shafts receive genuine blind recesses with backing, returns and broad dressed surrounds; two connected header shoulders replace the former interrupted coping ends. Existing court/pit/gate footprints, door and lock sockets, ramps, navigation, camera, material batches, depth ownership, scene, gameplay and saves remain unchanged.

Three helpers owned court, pit and gate implementation with disjoint files; root integrated and alone ran Unity. Fresh before images were captured at baseline 66e68d3 before any runtime edits, then nine matching after views used the same camera/actor setup. One additional broad-yard view was captured only after; it is excluded from the paired comparison. Source review was crossed between helpers; root and helpers inspected actual images. Initial full EditMode passed 509/510: the new court annulus RGB gain also multiplied alpha to 1.08. The local ring tone now explicitly preserves alpha 1; the strict test was retained. Initial failed XML is saved. Full final suites passed before the APK build.

Region 32 renderers/165232 vertices versus 166468 (-1236); distant geometry 3572 unchanged. No new runtime texture, material, renderer or updater. Counts are resource observations, not device frame-time measurements.

See `validation/actual-image-review.md` for bounded visual acceptance and remaining reference differences. This package does not establish full reference parity.

## Verification

425 .NET / 510 EditMode / 159 PlayMode = 1,094 distinct suite cases, zero final failures/skips. Isolated compile zero warnings/errors; before and after Android-target graphics captures passed. PASS: 599 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0, no C# or shader diagnostics. APK 36,256,060 bytes; SHA256 3405AA3C2A261B803A36F7B25BCED0D1AEDDF64CB422BABB56EBC9E53F375501; NOT INSTALLED.

Full reference material richness and physical-device movement/crowd/HUD/shimmer/midrange acceptance remain unfinished. No ADB, install or fresh device observation in this package. The earlier concrete restart/install decision remains pending. Future deployment must inspect current state and preserve current original saves/telemetry, never restore historical snapshots. Regional encounter/boss progression stays deferred by the art-first direction.

## Reproduction

From E:/MobileGame with Unity closed:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryCourtPitGateProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

The Before capture requires clean baseline 66e68d3 Assets; it must not be recreated from changed runtime. The launcher temporarily imports its capture/meta into the PlayMode test assembly and removes both in finally. Nine views are matched: full-map, court-detail, shaft-detail, gate-detail, overview, approach-0-0, approach--20-6, approach--16-15 and approach-20-1. The broad-yard image is supplementary After-only evidence, excluded from the paired gallery. Combat is paused and HUD hidden for art inspection; these are actual Unity images, not phone screenshots. The approved reference is concept art. Graphics cases are separate from the suite total.
