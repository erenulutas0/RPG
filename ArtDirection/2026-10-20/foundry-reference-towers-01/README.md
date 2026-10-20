# Foundry reference furnace towers (Unity integration)

Verified 2026-10-20. Baseline `c021c9a980b6cfcb6674c8558ba7f14a1df0fa70` (rim lantern package). Target: the approved concept
`ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`.

Tall furnace towers toward the concept. New native Blender renders (Tools/ArtReview/BuildBlenderReferenceTowers.py, source ArtDirection/2026-10-20/reference-towers-01) keep the original stone foundation, fire mouths and cornice and rebuild the upper towers as stacked blackened-iron drums ringed with small glowing arched windows, brass bands and a crenellated (near) or stepped ribbed dome (far) crown. Pipe contact (4.05), flue (0,0,7.2), footprint, camera and lights are unchanged; the atlas is repacked losslessly into the same two tiles, so runtime cards, depth bands, steam anchors and import settings (2048x1024 ASTC_6x6, 12 mips) are unchanged. Iron moved from the source's cyan-leaning forged blue (read teal in the court) to neutral-warm black.

Region 37 renderers / 154820 vertices: unchanged (texture-only). Editor texture memory 2,502,304 bytes unchanged. Static counts do not establish device performance.

## Verification

438 .NET / 711 EditMode / 160 PlayMode, zero failures/skips. One new test (atlas margins, dark upper iron below the rejected light-iron level, neutral-warm hue, lit window rings). Isolated compile 0/0. Fresh Before (25 views, baseline c021c9a, clean Assets) and final After Android-target captures passed. PASS: 648 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components. Android build exit 0. APK 36,392,292 bytes; SHA256 4AC6C3275E2A9518A631CA8D74086E2332A06953D8C6623DD097E52D1EBD204C; NOT INSTALLED.

All-Assets source freeze `eddaaa245a9de2690e35ee897baf3e0416c1047d02657f824b546d7da4750d2a` (1243 files including tests/metas) was taken before the final After capture and
matched after it and after the complete suites and APK build.

## Candidates (actual Unity captures)

- `candidates/01-light-iron`: first integrated render; the upper iron (~35 % lighter than before) and cream flue lips
  read beige and drew attention away from the gate.
- `candidates/02-blackened-iron`: darker iron and brass, iron flue lips; identical runtime to the final After.

`validation/comparison.png`: concept tower / Before / After at the full-map camera.

## Evidence boundary

Texture-only change to the two tall towers. The short causeway retorts (FoundryShortBoiler) are unchanged; the concept's
thick piped collars between towers are not added; no new windows light the floor. No gameplay, navigation or save
change. No phone operation; APK not installed.

## Reproduction

```powershell
& 'E:/MobileGame/TestResults/tools/blender-4.5.9-windows-x64/blender.exe' --background --python Tools/ArtReview/BuildBlenderReferenceTowers.py
python Tools/ArtReview/PackReferenceTowerAtlas.py
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryReferenceTowersProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```
