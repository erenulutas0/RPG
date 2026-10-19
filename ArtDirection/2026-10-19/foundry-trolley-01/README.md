# Foundry loading trolley

Verified 2026-10-19. Baseline `4e761e475826eac52fd866608de7eabf846b44b9` (docs-only after `0869689`; Assets identical).

The active loading trolley (work island 0) now joins the moulding bench and winch's neutral worked-iron family instead of reading as a blue cage over a pale blue tray: frame, yokes, axle saddles and wheels take WorkyardCastTone (darker toward the floor, lighter worn edges), the skid becomes a dark bearing plate, and the six billets take individual oxide mill-scale values. Two oak dunnage bearers at the band stations carry the load .06 above the deck; the folded bands still start and end on the deck. Yoke legs (.24 x .34) and crown are broader, wheels slightly larger (radius .31) with unchanged ground contact. The moulding bench's skid gets the same dark bearing plate. Footprint, load placement, rail relationship, open spaces, shared cast UV field, batches and depth ownership are unchanged. Bounded material/value integration, not reference parity: the yokes still read as frames and the wheels stay mostly hidden.

Region 36 renderers / 171940 vertices versus 171884: +56 (the two bearers). Distant 3572 included and unchanged. Ceilings 174000 / 3600 remain; headroom 2060 / 28. No new bitmap, material, shader, batch or updater. Static counts do not establish device performance.

## Verification

438 .NET / 701 EditMode / 160 PlayMode = 1,299 suite executions, zero failures/skips. Focused Foundry EditMode 299/299 passed separately (two new tests). Isolated compile 0 warnings/0 errors. Fresh Before (23 views, baseline 4e761e4, clean Assets) and final After Android-target graphics captures passed. PASS: 639 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components. Android build exit 0. APK 36,387,444 bytes; SHA256 43B67A2A5265B56601FC707B02DA3946D13766B714843005BECDC132E7C769A6; NOT INSTALLED.

All-Assets source freeze `bc6aa864d31ff4fb49ed1a4e20ac27006073ab6fec900f62e1d0dfb7b3ff7054` (1,225 files including tests/metas) was taken before the final After capture and
matched again after the capture and after the complete suites and APK build.

Two focused runs failed during development and were diagnosed before continuing (their XML was overwritten by the
next run; recorded in `validation/work-record.txt`): (1) a real defect - the billets' brightest faces (1.20) outshone
the iron's worn edges (1.16); the production billet tone was lowered ~6 %, the assertion unchanged; (2) a wrong oracle
in the new test, which looked for a bearer corner at the outer edge although the hull's top is inset by its .02 bevel;
the oracle now names the actual top corner. The new tests were not loosened.

## Images

`index.html` compares Before / After / reference across 23 matched actual Unity views (identical cameras and actor
staging, paused combat, hidden HUD). `trolley-detail` and `trolley-route` are new views for this package.
Pixel differences outside the trolley and the bench skid are animation noise (at most ~1,400 pixels per view)
(`validation/actual-image-review.md`).

## Evidence boundary

Bounded workyard material/value improvement. Native yokes remain frame-shaped; wheels are mostly hidden by the deck at
the real camera; the moulding bench is unchanged apart from its skid; the broad gate crown/header material mismatch
(second priority) was not addressed. Map, camera, navigation, collision footprints, gameplay, encounters, energy
deferral and saves unchanged. No phone operation, install, save or telemetry change; the restart/install decision
remains pending. Crowd congestion (ArtDirection/2026-10-12/foundry-forecourt-layout-01/validation/crowd-diagnostic.md)
remains open.

## Reproduction

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/Run-UnityTests.ps1 -Platform EditMode -Filter Foundry -SkipApk
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryTrolleyProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

Runtime source: `Assets/_Project/Scripts/UI/Arena/FoundryRegionProductionYard.cs`. Tests:
`Assets/_Project/Tests/EditMode/FoundryYardEquipmentTests.cs`. Capture: `Tools/ArtReview/FoundryTrolleyCapture.cs`,
`Run-FoundryTrolleyProof.ps1` (Before pinned to the baseline HEAD and clean Assets).
