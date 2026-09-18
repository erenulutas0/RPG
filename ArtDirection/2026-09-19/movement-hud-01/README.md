# Movement HUD polish — 2026-09-19

Scope: visual hierarchy of the integrated movement budget. No movement rules, enemy data, authored density, camera, ability behaviour or top HUD changes. No new generated raster artwork. Existing UI images and legacy Text are retained.

## Presentation

At the existing 1080-wide reference resolution:

| Element | Position (bottom-left) | Size |
| --- | --- | --- |
| Health fill track | 64,210 | 698×22 |
| MOVE label | 64,168 | 94×28; 22px bold |
| Movement track | 168,175 | 594×14 |
| XP label | 62,126 | 700×32; 25px |
| XP track | 64,108 | 698×8 |

Full movement fill uses 60% of its normal alpha; partial fill retains full emphasis. Fill amount always equals the actual budget fraction. A nonempty-to-empty transition starts one 0.24-second scaled-time ember track pulse, then settles to the existing dull spent colour. Remaining empty does not repeat it; refilling cancels it. Pause freezes the pulse. MOVE does not intercept input. A separate label and spacing were chosen over brightening the whole panel so that health keeps visual priority.

## Evidence and reproduction

Seven actual Unity Camera.Render captures, losslessly converted from BMP to PNG: full, half, depletion instant, settled empty and refilling at 1080×2340; full and settled empty at 1080×1920. The gallery uses those stills, including CSS crops for the panel; it is not a live game or device recording. Test-local development profile and frozen ten-enemy room. The fixture changes only temporary runtime stamina state and framing for the sampled aspect ratios; it saves no scene or profile.

After coordinating exclusive Unity access:

```powershell
pwsh -File Tools/ArtReview/Style-MovementHud.ps1
pwsh -File Tools/ArtReview/Run-MovementHudProof.ps1
pwsh -File Tools/Run-UnityTests.ps1
pwsh -File Tools/Verify-Project.ps1
```

The first command deliberately saves the documented Gameplay HUD layout through a temporary Editor tool. The second stages and removes an isolated graphics test. Source: Tools/ArtReview/StyleMovementHud.cs and MovementHudCapture.cs. Runtime: HeroStaminaView; regression: ArtHudFlowTests.MovementEmphasisIsQuietWhenFullAndPulsesOnlyOnDepletion. Capture hashes are in sha256.txt.

Device review is pending coordination. No ADB interaction, APK installation or phone profile/telemetry mutation was performed in this slice. The latest installed movement-budget build remains the one documented in Docs/23, SHA-256 D77FFFA6D385BBB017F9EBCBA8C9D6A7271250F5318E4021A8FD9F7BFA6DEED2. Do not restore an older profile backup.

Next bounded art slice: ability range-ring artwork while preserving its exact gameplay radius and independent ground sorting. This work does not resolve low-density encounter pressure, remaining roster art, mirrored lighting or strict support-foot locking.

Verified: .NET 291/291; compile 0 warnings/errors; EditMode 306/306; PlayMode 108/108; isolated graphics capture 1/1; integrity 439 asset/folder GUIDs and 568 scene objects/components. Android build exited 0. Actual APK file: 29,151,985 bytes; SHA-256 `97A5F7511F0968E43AAF785BAAC2F0B8D654F91905CE4C858DECBACA3175A7AA`. Not installed or phone-tested in this slice. Full tests cover existing gameplay and the added depletion/pause regression; they do not establish physical-device readability or performance.

## Movement HUD installed and reviewed on S23 — 2026-09-19

Owner explicitly confirmed the phone was free and authorised continuation. Installed the already-verified movement-HUD APK with adb install -r; installed base.apk SHA-256 matches `97A5F7511F0968E43AAF785BAAC2F0B8D654F91905CE4C858DECBACA3175A7AA` (29,151,985 bytes). No new code or build in this device-only slice; prior gate remains .NET291 / EditMode306 / PlayMode108, clean compile, graphics1, integrity439/568. The new ability-ring proof is NOT integrated in this APK.

Before: Cinder Walk 2/6, level-3 upgrade choice, 74 HP, 13 unbanked run gold. The owner-authorised reinstall ended that active run, as announced. Fresh profile and telemetry were copied locally before installation. Profile revision150, banked gold11596, Daggers/Second Wind, both weapon/relic unlocks and deepest floor2 were preserved. Both primary and backup profile hashes match before install, immediately after install and after the test. Primary SHA256 `6CD3D4D338536FE77993A3D52E88E55C6577F7FD3C6FC32FFDE81AAE45BC8DE3`; backup `8617F6460C901005EFF3CEB0D6CFDFADD072C36D1068A3187002878287C39B41`. No restore, clear, purchase, equipment change or development-floor flag; telemetry naturally grew and was not rolled back.

Observed in ordinary authored combat: separated health/MOVE/XP layout; movement fill falling during a drag, partial fill returning after release, quiet full state, one brief ember depletion pulse fading into the spent track, empty state held while a level-up choice pauses play, refill after selection. First drag reached the rim before emptying; the longer reverse drag produced depletion. Fifteen-fps extracted frames show the pulse, not a duration/performance benchmark. Focus, awake and unlocked checks preceded each input/capture; captures were rechecked before keeping. Short filtered logcat check found no FATAL EXCEPTION, NullReferenceException, MissingReferenceException or HeroStaminaView binding error. This is not exhaustive log or frame-time validation. Pulse freeze mid-animation remains covered by the Unity regression, not demonstrated by the phone clip.

Final observed state: Cinder Walk 2/6, level-4 upgrade choice open, 56 HP, 23 at-risk run gold, XP55/70, full movement. Phone handed back to the owner; inspect fresh before further use. Local raw evidence/backups: TestResults/movement-hud-device-2026-09-19/. Committed selected screenshots and a labelled-by-documentation temporal HUD strip: ArtDirection/2026-09-19/movement-hud-01/device/. Full screenshots are unmodified; depletion-strip.png is an ffmpeg crop/scale/contact sheet, 18 sequential frames from 2.8s at 15fps, read left-to-right/top-to-bottom. Source videos remain local; no profile or telemetry data is committed.

This supersedes earlier pending-phone and D77 latest-installed statements. Next art task is the shared-lifetime integration of the preferred ability ring, not another movement-budget change.
