# Vanguard contact, sword grip and compact Mite bars

2026-09-17. A bounded presentation polish of the imported prototype, not final animation.

## Changes

- The Gameplay hero's old `CombatantView._attackNudge` is 0 rather than 0.22 world units. Actual attack events still select strike/recovery immediately; the feet and the root-owned contact shadow no longer separate vertically on each hit. Enemy nudges and gameplay movement are unchanged.
- Vanguard's displacement-driven four-pose cycle covers 0.8 rather than 1.2 floor units. This reduces the long contact hold. It does **not** lock a foot to world space: four discrete body poses still slide between samples, and attacks still temporarily replace the walking pose.
- Each front pose has an optional 11x8 grip sprite at 128 PPU, extracted from its already sampled body image. One hero-owned renderer draws those registered knuckles over the sword. Rear sword layering already places the weapon behind the hand. The overlay mirrors with the body, is disabled for Staff/Daggers and hides with the body on death. The imported hero's damage silhouette is above this overlay.
- Mite health-bar parent scale is `(0.65, 0.8, 1)`, reducing its 26/32-unit backing to 0.528125 units wide. The fill retains its 24-texel fraction and fixed left edge. Shared bar sprites, other enemy bars and health values are unchanged.

## Provenance and import

No new generated painting or prompt belongs to this slice. Body, equipment and source masters are unchanged from `../vanguard-directions-01/` and `../vanguard-runtime-01/`. `BuildVanguardAssets.cs` copies existing sampled front-body texels into eight small grip imports; it does not paint, regenerate or recolour the gauntlet.

For each front frame, convert its existing right-hand anchor to the sampled body's pixel coordinates, using pivot `(370/816 * 128, 16/1152 * 180)`. Crop origin is `floor(handPixel) - (5,4)`, size 11x8. The grip has a centre pivot and local offset `(cropOrigin + (5.5,4) - bodyPivot)/128`, preserving exact registration. Existing alpha is retained. Imports are bilinear, clamp, FullRect, no mips, no CPU read access; Android ASTC 4x4. Sprites are borrowed from the art set, with no per-frame pixel generation or sprite allocation.

## Reproduction and evidence

`pwsh -File Tools/ArtReview/Run-VanguardProof.ps1 -ContactGrip -Reimport` rebuilds and captures; omit `-Reimport` to capture the currently imported assets. It stages temporary Editor/PlayMode sources, removes them afterwards and never builds an APK. The actual scene/hero view supplies every pose, facing, attack and movement frame. Isolated test profiles prevent changes to the owner's Editor profile.

The 24 gait images are consecutive real frames at 60 Hz, displayed at 30 Hz in the browser for inspection. The fixed-size detail camera follows the hero; background motion shows actual displacement. The browser's separate pose selector is only a review sequence, not runtime timing. The portrait capture shows the initial proof-floor entry arrangement with attacks disabled; actors outside the camera remain present. `before-*.png` are unchanged captures from the preceding directional proof and retain its different effects timing.

`VanguardAnimationTests` checks all four facings, displacement/pause, hit timing, grounded attacks, registered mirrored grips through the strike/recovery/windup, flash layering, death/reload and the other loadouts. `MiteAnimationTests` additionally checks compact width, independent fill anchoring/ratio and unchanged large-enemy bars. Full verification and exact physical phone state are recorded in the newest Docs/21–23 entry.

## Verification and device

.NET 272/272; compile 0 warnings/errors; targeted Vanguard/Mite 13/13; graphics capture 1/1; full EditMode 284/284, PlayMode 96/96; Android exit 0; integrity 375 GUIDs / 555 scene objects/components. APK 24,739,917 bytes, SHA-256 `1DCB931D6ECCA73424870687AB3AB32E21AC4E2C6BACD67A3C3B66492F4FB01C`; installed S23 hash matched.

One Sword ten-enemy proof, southeast/southwest drags, 10 kills and victory were physically checked. `device-crowd.png` and `device-sword.png` are unchanged phone screenshots; full evidence is ignored under `TestResults/device-contact-grip-01/`. No sustained-performance or mid-range acceptance. The previous owner run was completed/extracted before installation; after the proof Daggers was restored through the UI and the development flag removed. Final stored profile revision122 /8724 gold /Daggers /Second Wind; phone left on Ember Hall1/6's first upgrade choice. See newest Docs/23 for exact hashes, progress accounting and limitations.

## Remaining art

Strict support-foot locking or additional gait poses; mirrored lighting/handedness; production finger articulation; matching Staff/Daggers; large enemies, chest and floor/prop material polish. This change does not alter boss geometry, enemy density, collision, balance, telemetry or progression.
