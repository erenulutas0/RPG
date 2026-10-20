# Foundry reference retorts (Unity integration)

Verified 2026-10-20. Baseline `a8eae771f3c66297fab9fe8d8408a0709280e87b` (furnace tower package). Target: the approved concept
`ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`.

Causeway retorts joined to the reference towers' family. New native Blender renders (Tools/ArtReview/BuildBlenderReferenceRetorts.py, source ArtDirection/2026-10-20/reference-retorts-01) reuse the original retort constructors for the footing, masonry, fire doors and cornice and replace the blue rolled-steel hoods with blackened iron, brass bands and a ring of small glowing windows under a stepped ribbed dome (variant 0) or a merloned parapet (variant 1). Height <= 2.28, exhaust (0,0,2.28) and physical bounds equal the original (asserted in the generator); the atlas is repacked losslessly into the same tiles, so cards, depth bands, steam and import settings are unchanged.

Region 37 renderers / 154820 vertices: unchanged (texture-only). Static counts do not establish device performance.

## Verification

438 .NET / 712 EditMode / 160 PlayMode, zero failures/skips. The furnace-atlas test now covers both atlases (retorts: neutral-warm hue, > 250 lit window pixels per tile where the rejected flat hood had 149 and the old hoods 0). Isolated compile 0/0. Fresh Before (25 views, baseline a8eae77, clean Assets) and final After Android-target captures passed. PASS: 648 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components. Android build exit 0. APK 36,392,292 bytes; SHA256 3BDA245DF12816004B78D83D5E802290926D54011C9C5E7C567A6BA8AC80F8CE; NOT INSTALLED.

All-Assets source freeze `295370d9429824492ece0187133e34bcb010d1b635cc8b4a3433c5721e167a4c` (1243 files including tests/metas) was taken before the final After capture and
matched after it and after the complete suites and APK build.

## Candidates (actual Unity captures)

- `candidates/01-integrated`: identical runtime to the final After (captured before the test change and freeze).
- The rejected flat-hood render never reached Unity; it is kept in the source folder.

`validation/comparison.png`: Before / After retorts at the full-map camera.

## Evidence boundary

Texture-only change to the two causeway retorts. The concept's thick pipe collars between furnaces are still not added.
No gameplay, navigation or save change. No phone operation; APK not installed.

## Reproduction

```powershell
& 'E:/MobileGame/TestResults/tools/blender-4.5.9-windows-x64/blender.exe' --background --python Tools/ArtReview/BuildBlenderReferenceRetorts.py
python Tools/ArtReview/PackReferenceRetortAtlas.py
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryReferenceRetortsProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```
