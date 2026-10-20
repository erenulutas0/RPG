# Foundry court bronze inlay network

Verified 2026-10-20. Baseline `94a925f20ee4874d575206b354d47b6a0ef2c754` (court flagstone package). Target: the approved concept
`ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`.

Court bronze inlay network after the concept: an octagon setting (R 7) around the seal, spokes from seven of its vertices to the rim and paired lane lines beside both processional ways (the eighth spoke would run down the bridge way and is replaced by its lane lines). Every line uses the seal's construction - a .14 stone channel with an .08 worn metal course on the coping rail, WeatherHeraldry and the ordinary LightFloorDecals response (<= 1.9) - and joins the existing single floor-decal batch. Lines are sampled every .25 units and clipped to open walking floor (FoundryMap.Contains with .55 clearance), outside the seal stone, boiler plinths and every forge-lamp box, so none runs into the pit, under equipment or past the kerb. First candidate (.085/.05) was too faint at map scale.

Region 37 renderers / 154393 vertices versus 152889: +1504 (thin strips). No new batch, material or texture. Distant 3572 unchanged. Ceilings 174000 / 3600; headroom 19607 / 28. Static counts do not establish device performance.

## Verification

438 .NET / 709 EditMode / 160 PlayMode, zero failures/skips. Two new inlay tests (open-floor clipping, seal and boiler clearance, metal course, bounded light; octagon edges, spokes kept off the processional stone, paired lane lines). Isolated compile 0/0. Fresh Before (25 views, baseline 94a925f, clean Assets) and final After Android-target captures passed. PASS: 646 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components. Android build exit 0. APK 36,391,867 bytes; SHA256 72B72988DFEF9F2CB7C2A587D4377D3FD137700A11887361EBC4A2B9E372F3CE; NOT INSTALLED. The APK is back near 36.39 MB, so the previous 31.2 MB build size was build-to-build variance, not this content.

All-Assets source freeze `32fa83c6b6ccb6f03aaff8c9f740a59690a71ab95c8604d14356093bfd17905a` (1239 files including tests/metas) was taken before the final After capture and
matched after it and after the complete suites and APK build.

## Candidates (actual Unity captures)

- `candidates/01-thin-lines`: .085 channel / .05 metal. Correct layout, but nearly invisible at map scale.
- `candidates/02-visible-lines`: .14 / .08; identical runtime to the final After (captured before the tests and freeze).

`validation/comparison.png`: concept / Before / After court, and the portrait game camera.

## Evidence boundary

Thin diagonal decals alias into pixel steps in the 16 px/unit full-map overview (as the existing seal ring does); at the
portrait game camera (~90 px/unit) they read as continuous ~7 px inlays. The concept's additional panel subdivisions are
not reproduced; the gate forecourt and work apron are unchanged. No gameplay, navigation or save change. No phone
operation; APK not installed.

## Reproduction

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryCourtInlayProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

Runtime: `Assets/_Project/Scripts/UI/Arena/FoundryRegionCourtInlays.cs` (called from `BuildLandmarks`). Tests:
`Assets/_Project/Tests/EditMode/FoundryCourtInlayTests.cs`.
