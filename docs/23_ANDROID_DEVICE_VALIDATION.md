# Android device validation — passed first combat smoke test

Date: 2026-09-12. The user authorized building and running the prototype on their USB-connected phone.

## Delivered

- App: **Project Cryptforge**, package `com.cryptforge.prototype`.
- Device: Samsung SM-S911B, Android 16, ARM64, 1080x2340.
- APK: `E:/MobileGame/Builds/Android/Cryptforge-Prototype.apk`.
- APK size: **15,442,511 bytes** (development build, debug signing, ARM64/IL2CPP).
- SHA-256: `371B4FDA69FDE71272333C9943A3A4C992EB592F59AD07B0A7C62E84D8637324`.
- ADB installation returned `Success`. Activity `com.unity3d.player.UnityPlayerActivity` launched successfully. The app was left open on the phone.

## Verified results

| Check | Result |
|---|---|
| Unity import and full compilation | Passed after correcting the package manifest |
| Unity EditMode tests | 26 passed, 0 failed, 0 skipped |
| Unity PlayMode tests | 5 passed, 0 failed, 0 skipped |
| Static metadata/scene references | 46 GUIDs and 32 scene objects/components resolved |
| Android build | Succeeded; Unity exited with code 0 |
| Portrait rendering on device | Hero, enemy, weapon, labels and health bars visible without overlap |
| Automatic combat | Live screenshot shows Grunt at 40/50 HP with one hit; later screenshot shows 0/50 with five hits |
| Enemy death | Enemy shape disappears and `Grunt defeated` appears |
| Stop after death | Hit count remains at five; hero remains at 100/100 HP |
| Background/resume | Backgrounded for three seconds during combat; returned to a living enemy at 20/50 HP and three hits, then finished at five without a catch-up kill |
| App log | Final app-process log contains no E/Unity entries or managed/fatal exceptions matching the checked patterns |

This is a short device smoke test. Sustained FPS, battery/thermal behavior, other devices, and alternate resolutions have not been profiled. Vulkan capability probing emitted Android hardware-buffer errors during initialization; rendering worked and no Unity error/crash was observed. The standard Unity splash appears briefly before gameplay.

## Local evidence

Ignored test artifacts under `TestResults/`:

- `editmode.xml`, `editmode.log`
- `playmode.xml`, `playmode.log`
- `android-build.log`
- `android-combat.png`: living Grunt at one hit
- `android-resume.png`: resumed combat at three hits
- `android-final.png`: defeated Grunt at five hits
- `android-final-logcat.txt`: app-process log from the final run

The APK and local test artifacts are intentionally ignored by Git. Unity-generated `ProjectSettings/` defaults and `Packages/packages-lock.json` are retained for reproducible project setup.

## Installed tooling and fixes

- Editor: `E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe`. The actual Editor reports revision `a18e2220bd50`; Unity updated `ProjectVersion.txt` accordingly. The earlier hand-authored `f34bf41fecc5` revision was incorrect; the requested version number did not change.
- Android Build Support plus bundled OpenJDK 17, NDK r27c, SDK/platform tools, API 34/35/36 and CMake installed successfully.
- Unity CLI 1.0.0-beta.9 is available at `E:/UnitySetup/unity.exe`; account sign-in and Unity Personal licensing are active. Installation followed Unity's [CLI workflow](https://docs.unity.com/en-us/unity-cli/use-unity-cli).
- The first launcher failed from the desktop. A relative executable path, colocated verified CLI binary and CRLF batch file let the administrator prompt run. The user approved it. No Windows security settings were changed.
- The elevated main install omitted Android modules, so they were installed separately with `install-modules` and child modules enabled.
- Removed nonexistent `com.unity.modules.textrendering` dependency when real import detected it. Text rendering is provided by the Editor.
- Aligned Test Framework manifest with the Editor's effective built-in version **1.6.0** (NUnit **2.0.5**) and retained the generated lockfile.
- Added `Scripts/Editor/AndroidPrototypeBuild.cs` with an Editor-only assembly: development APK from enabled scenes, portrait, ARM64/IL2CPP, explicit build-failure reporting.

## License lapse and recovery (later on 2026-09-12)

Batch-mode runs exited with code 198 (`No valid Unity Editor license found`, `Access token is unavailable`). The account was restored with the Unity CLI: `E:/UnitySetup/unity.exe auth login` (browser sign-in by the user), then `unity license activate --personal --accept-eula` with the user's explicit consent to the Personal terms. `unity license status` now reports Unity Personal assigned. Run the Editor outside restricted sandboxes; it needs its AppData caches.

## Kill → XP slice on device (2026-09-12)

| Check | Result |
|---|---|
| Unity EditMode tests | 34 passed, 0 failed, 0 skipped |
| Unity PlayMode tests | 6 passed, 0 failed, 0 skipped |
| Static metadata/scene references | 53 GUIDs and 32 scene objects/components resolved |
| Android build | Succeeded; APK 19,017,824 bytes, SHA-256 `A675F306D09761C46A7E318CA154D37F9CAC6FDFB739A10D56D74A1510E7F2B0` |
| Install and cold launch | `Success`; activity launched in 748 ms |
| Combat, 5.7 s after launch | Grunt 20/50 HP, **Hits landed: 3**, **XP: 0** (`android-xp-combat.png`) |
| After death, 12.4 s | Grunt 0/50, **Grunt defeated**, **Hits landed: 5**, **XP: 10**, hero 100/100 (`android-xp-final.png`) |
| App log | 465 app-process lines in `android-xp-logcat.txt`; no E/Unity, exception or fatal matches |

The narrow grey shape at the right edge of the combat screenshot is the Samsung Edge panel handle, not game UI. This remains a short smoke test.

## Upgrade choice slice on device (2026-09-13)

| Check | Result |
|---|---|
| Unity EditMode tests | 53 passed, 0 failed, 0 skipped |
| Unity PlayMode tests | 10 passed, 0 failed, 0 skipped |
| Static metadata/scene references | 69 project GUIDs, 1,099 package GUIDs indexed, 144 scene objects/components resolved |
| Android build | Built only after both XML reports passed; APK 24,709,937 bytes, SHA-256 `2326B14D7D6BB954C883A0508B5FC8C2A2C342A98CF2DC8AE8BBB3E9E5A7BFAA` |
| Install and cold launch | `Success`; activity launched in 553 ms |
| Combat, 6.4 s | uGUI HUD inside the safe area (cutout inset 98 px); Grunt 10/50, **Hits landed: 4**, **Level 0 \| XP 0 / 10** (`android-upgrade-combat.png`) |
| Kill, 11.6 s | Dimmed overlay with **Level up! Choose one upgrade**, **Tempered Edge / +5 damage per hit** and **Quickened Grip / +25% attack speed** (`android-upgrade-panel.png`) |
| Two `adb input tap` commands on Tempered Edge | Panel closed; **Sword \| 15 damage every 0,80s** (one application), **Level 1 \| XP 10 / 20**, Grunt defeated (`android-upgrade-after.png`) |
| App log | 461 app-process lines in `android-upgrade-logcat.txt`; no E/Unity, exception, fatal or missing-reference matches |

Limits: the two adb taps run as separate shell commands, so the second landed after the panel had closed; truly simultaneous taps are covered by the PlayMode test rather than this device run. Numbers use the device's Turkish decimal comma. The dimmed "Grunt defeated" status line is visible between the cards; it is behind the overlay and harmless, but should move when the result screen replaces it.

## Rebuild and launch

Close any Editor using this project, then run:

```powershell
& 'E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe' -batchmode -quit -projectPath E:/MobileGame -buildTarget Android -executeMethod Cryptforge.Editor.AndroidPrototypeBuild.Build -logFile E:/MobileGame/TestResults/android-build.log
adb devices -l
adb install -r E:/MobileGame/Builds/Android/Cryptforge-Prototype.apk
adb shell am start -W -n com.cryptforge.prototype/com.unity3d.player.UnityPlayerActivity
```

Select a specific device with `adb -s <serial>` if more than one is attached. The commands above assume exactly one authorized device. The existing first slice has no in-game restart button: fully close the app and reopen it to start a fresh fight, or use `adb shell am force-stop com.cryptforge.prototype` before launching it again.

## Next encounter slice on device (2026-09-13)

| Check | Result |
|---|---|
| Unity EditMode tests | 64 passed, 0 failed, 0 skipped |
| Unity PlayMode tests | 13 passed, 0 failed, 0 skipped |
| Static metadata/scene references | 75 project GUIDs, 140 scene objects/components resolved |
| Android build (tested content) | Built only after both XML reports passed; APK SHA-256 `57CFE61F1B2F551A83E15959BB3A20989D4538508F5C8FBECEECD39886180BFE` |
| First choice, 12.0 s | Dimmed status **Grunt defeated in 5 hits, 3.2s**; Sword 10 damage (`android-encounter-choice1.png`) |
| Tempered Edge tapped; +3.5 s | **Encounter 2**, **Sword \| 15 damage every 0,80s**, fresh Grunt at 5/50 after **3 hits**, **Level 1 \| XP 10 / 20** (`android-encounter-fight2.png`) |
| Second choice, +7.0 s | Dimmed status **Grunt defeated in 4 hits, 2.4s** (`android-encounter-choice2.png`) |
| Tempered Edge tapped again, captured inside the 1 s advance delay | **Grunt defeated in 4 hits, 2,4s**, **Hits this fight: 4**, **Sword \| 20 damage**, **Level 2 \| XP 20 / 30** (`android-encounter-cleared2.png`) |
| App log | 463 app-process lines in `android-encounter-logcat.txt`; no E/Unity, exception, fatal, null-reference or missing-reference matches |
| Subtitle fix | The new subtitle wrapped and was cut off at the top of the screen. Shortened the text asset only, rebuilt (SHA-256 `A21D6DCD0BE8522DF8BA37C3B0B373A549AF38DF3F4DCAA74B5E7AA6E09F0099`), reinstalled and confirmed **A lone vanguard. Foes keep coming.** on one line during Encounter 1 (`android-encounter-fight1.png`). Unity tests were not re-run for this text-only change; static verification passed. |

## Enemy attack and result screen slice on device (2026-09-13)

| Check | Result |
|---|---|
| Unity EditMode tests | 71 passed, 0 failed, 0 skipped |
| Unity PlayMode tests | 17 passed, 0 failed, 0 skipped |
| Static metadata/scene references | 79 project GUIDs, 178 scene objects/components resolved |
| Android build | Built only after both XML reports passed; APK 24,716,834 bytes, SHA-256 `6BA30D96AC30097CBCB6C5FB9DF6821FC88BAAE14803A003FA3A5D8E58030FF7`; cold launch 431 ms |
| Full run | `adb input tap` on the first card position every 0.7 s from 8 s after launch (the position is empty HUD space outside the choice panel and above the Try again button). First choice was already open at 5.7 s (`android-run-strike.png`) |
| Result, captured at 35 s and again at 85 s | **Defeated**, **Vanguard fell to a Grunt in encounter 11**, **Encounters cleared: 10 \| Level 10 \| XP 100**, **Build: Tempered Edge x5, Quickened Grip x5**, Sword 35 damage every 0,36s behind the overlay; identical at both times, so nothing advanced after death (`android-run-midway.png`, `android-run-result.png`). Matches the EditMode full-run simulation exactly |
| Try again (two taps in one shell command), +3.0 s | Same app process; **Encounter 1: combat is automatic**, **Level 0 \| XP 0 / 10**, **Sword \| 10 damage every 0,80s**, Grunt 10/50 flashing white, **Vanguard \| 82 / 100 HP** after three strikes (`android-run-restarted.png`) |
| App log | 657 app-process lines across the run and restart in `android-run-logcat.txt`; no E/Unity, exception, fatal, null-reference or missing-reference matches |

Limits: the device run proves one reload visually; the exactly-once reload under repeated taps is asserted by the PlayMode test. The whole run lasted about 30 seconds, much shorter than the GDD's 5–8 minute boss checkpoint target; that is expected until enemy scaling, healing and floors exist.

## Runner and Tank slice on device (2026-09-13) — partial

| Check | Result |
|---|---|
| Unity EditMode tests | 84 passed, 0 failed, 0 skipped |
| Unity PlayMode tests | 20 passed, 0 failed, 0 skipped |
| Static metadata/scene references | 91 project GUIDs, 178 scene objects/components resolved |
| Android build | Built only after both XML reports passed; SHA-256 `B6F62AB9CEC50AA5DA45E579E0233787DECC2D9338FF06FDC643B3EA00429500`; cold launch 523 ms |
| Result after an auto-played run | **Vanguard fell to a Grunt in encounter 15**, **Encounters cleared: 14 \| Level 14 \| XP 140**, **Tempered Edge x5, Quickened Grip x5** (`android-archetype-result.png`), matching the damage-first simulation |
| App log | 786 app-process lines in `android-archetype-logcat.txt`; no E/Unity, exception, fatal, null-reference, missing-reference or invalid-operation matches |
| Runner and Tank on screen | **Not captured.** The phone was in use during the run, so the app was in the background at the planned capture times |

Procedure incident and change: the capture script sent `adb input tap` on a timer without checking which app had focus. While the device owner was using other apps, three captures recorded those apps instead of the game and some taps may have reached them. The captures were deleted right after the review that spotted the problem, were never shared or committed, and no game evidence is taken from them. Unity pauses in the background, so the game state and the result above remain valid. From now on every automated tap and capture first checks that `com.cryptforge.prototype` has window focus and aborts otherwise, and unattended device runs are announced to the owner first.

The first combat slice, kill → XP, upgrade choice, next encounter, enemy attack with result/restart, and Runner/Tank archetype slices are running on the phone. Visual on-device confirmation of the Runner and Tank is pending. Enemy scaling, floors, healing and Extract remain future slices.
