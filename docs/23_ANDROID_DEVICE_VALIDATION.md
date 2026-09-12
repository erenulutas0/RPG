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

The first combat slice, the kill → XP slice and the upgrade choice slice are running on the phone. The next encounter, floors and an in-game restart remain future slices.
