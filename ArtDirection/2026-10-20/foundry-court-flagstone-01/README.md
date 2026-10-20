# Foundry court flagstones

Verified 2026-10-20. Baseline `aa1f3904d506270118b0be31eda5ead741fa1560` (gate crown package). First package after the owner handed visuals and gameplay
to Claude (2026-10-20); target is the approved concept `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`.

Court flagstone package. The open court (packer zones 0 and 3, minus the claimed work-apron repairs) is now laid as irregular hand-fitted flags like the approved concept instead of rectangular ashlar. Joints, worn arrises, quarry value and grain live in a new seamlessly tiling 1024 raster FoundryPavingV9 (procedural source Tools/ArtReview/GenerateFoundryFlagstones.py: relaxed periodic Voronoi, ~60 flags per 12-unit tile, mean luma .520 = ~8 % above the V8 calibration, faintly cool). The mesh keeps only the packer footprints as tessellation for the continuous forge-light, use and contact field (no per-footprint tint, so no rectangular checker); a new MAT_FoundryFlagstone batch at the paving order. Ceremonial seal/ring, processional stones, bridge, gate apron and work-apron repairs keep their fitted V8 geometry; the V8 material gains one restrained cool balance (_Color .5717/.5894/.6252, luma unchanged) so they sit in the same stone family. Measured court value moves toward the concept: full-map (74,67,71) -> (76,71,80) against the concept's (83,79,89); actors stay readable.

Region 37 renderers / 152889 vertices versus 36 / 172160: one new material batch, -19271 vertices (the laid ashlar's joint beds, bevel strips and contours). Distant 3572 unchanged. Ceilings 174000 / 3600; headroom now 21111 / 28. The PlayMode renderer guard moves 36 -> 37 for this batch. Static counts do not establish device performance.

## Verification

438 .NET / 707 EditMode / 160 PlayMode, zero failures/skips. Three new flagstone tests (continuous field and exact coverage; seamless calibrated raster and import; one cool stone family across both materials); the compass ownership test now builds both base layers. Isolated compile 0/0. Fresh Before (25 views, baseline aa1f390, clean Assets) and final After Android-target captures passed. PASS: 644 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components. Android build exit 0. APK 31,210,485 bytes; SHA256 D0441EF3D7721C1A61D986D31BEB91A507D488160213425FA8E2626125A2A857; NOT INSTALLED. V9 ships as 611.7 KB ASTC_6x6 (build report); the APK is ~5 MB smaller than the previous 36,387,952-byte build for reasons this package does not establish.

All-Assets source freeze `248b716102385ddbf52ab890bda1ff113f3d00f688759b7fc17639ef70a09109` (1235 files including tests/metas) was taken before the final After capture and
matched after it and after the complete suites and APK build.

## Candidates (all actual Unity captures)

- `candidates/00-shared-material-prototype`: V9 on the shared paving material, laid court only. Proved the look and the
  vertex saving, but every fitted stone (seal, paths, bridge) drew the raster's flags under its own geometric joints.
  Captured with a launcher that briefly overwrote the committed 2026-10-10 FoundryFlagstoneCapture tools (restored from
  git before the clean Before; see work record).
- `candidates/01-neutral-v9`: separate flagstone material, neutral raster at the V8 mean. Correct ownership, but the court
  stayed ~13 % darker than the concept.
- `candidates/02-calibrated-v9`: raster lifted ~8 %, faintly cool, softer mortar. The fitted V8 stone beside it read brown.
- `candidates/03-cooled-v8`: plus the restrained V8 cool balance; identical runtime to the final After.

`validation/comparison.png` puts concept / Before / After side by side (full map and portrait game camera).

## Evidence boundary

Bounded floor-material package. The concept's brass inlay network (octagon ring, spokes, lane lines) is NOT added yet;
the gate forecourt and work apron remain rectangular dressed stone by choice; the raster is procedural, not painted, and
has a 12-unit repeat (not visible in the reviewed views). The laid-ashlar court branch of `AshlarFlagFace`
(contours/wear for zones 0/3) is now unreachable in production; its tests still pass and it is left for a later cleanup.
Map, camera, navigation, collision, gameplay and saves are unchanged. No phone operation; APK not installed.

## Reproduction

```powershell
python Tools/ArtReview/GenerateFoundryFlagstones.py Assets/_Project/Art/Props/Foundry/FoundryPavingV9.png 1024 60 11 1.0942
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryCourtFlagstoneProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```
