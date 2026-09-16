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

## Runner and Tank slice on device (2026-09-13)

| Check | Result |
|---|---|
| Unity EditMode tests | 84 passed, 0 failed, 0 skipped |
| Unity PlayMode tests | 20 passed, 0 failed, 0 skipped |
| Static metadata/scene references | 91 project GUIDs, 178 scene objects/components resolved |
| Android build | Built only after both XML reports passed; SHA-256 `B6F62AB9CEC50AA5DA45E579E0233787DECC2D9338FF06FDC643B3EA00429500`; cold launch 523 ms |
| Result after an auto-played run | **Vanguard fell to a Grunt in encounter 15**, **Encounters cleared: 14 \| Level 14 \| XP 140**, **Tempered Edge x5, Quickened Grip x5** (`android-archetype-result.png`), matching the damage-first simulation |
| App log | 786 app-process lines in `android-archetype-logcat.txt`; no E/Unity, exception, fatal, null-reference, missing-reference or invalid-operation matches |
| Runner and Tank on screen, first attempt | Not captured: the phone was in use, so the app was in the background at the planned capture times |
| Focus-checked capture, 01:11 (same build) | Every tap and capture gated on `com.cryptforge.prototype` window focus and an awake display, re-checked after each capture |
| Encounter 2 Runner, after Quickened Grip | Narrow yellow enemy, **Runner \| 20 / 30 HP**, **Sword \| 10 damage every 0,64s**, **Hits this fight: 1**, hero **72 / 100** after the Grunt fight and two 2-damage slashes (`android-archetype-runner.png`) |
| Encounter 4 Tank, during windup | Wide purple enemy, **Tank \| 80 / 120 HP** after two 20-damage hits, hero unchanged at **56 / 100** (`android-archetype-tank-windup.png`) |
| Encounter 4 Tank, after first slam | **Tank \| 40 / 120 HP**, **Hits this fight: 4**, hero **44 / 100** (−12) (`android-archetype-tank-slam.png`) |
| Tank cleared, captured inside the 1 s advance delay | **Tank defeated in 6 hits, 3,3s**, hero still **44 / 100** (one slam), **Level 4 \| XP 40 / 50**, **Sword \| 25 damage every 0,64s** (`android-archetype-tank-cleared.png`) |

Procedure incident and change: the capture script sent `adb input tap` on a timer without checking which app had focus. While the device owner was using other apps, three captures recorded those apps instead of the game and some taps may have reached them. The captures were deleted right after the review that spotted the problem, were never shared or committed, and no game evidence is taken from them. Unity pauses in the background, so the game state and the result above remain valid. From now on every automated tap and capture first checks that `com.cryptforge.prototype` has window focus and aborts otherwise, and unattended device runs are announced to the owner first.

## Quickened Grip +50% on device (2026-09-13)

| Check | Result |
|---|---|
| Unity EditMode / PlayMode tests | 85/85 and 20/20 passed before the build |
| First attempt | Stopped before installing: another app had focus. Nothing was installed or tapped |
| Owner opened the game and confirmed | Focus, awake display and no keyguard verified before install, before and after every tap and capture |
| Install | APK SHA-256 `23DC82E0A7C8059CF044E11A4414328EC0386E51878C886444E0F2498ED4A567`, `Success`; game focused 1.0 s after launch |
| First choice | **Quickened Grip / +50% attack speed** (`android-speed-choice.png`) |
| Encounter 2 after Quickened Grip | **Sword \| 10 damage every 0,53s**, **Runner \| 20 / 30 HP** (`android-speed-runner.png`) |
| Runner cleared | **Runner defeated in 3 hits, 1,1s**, **Level 2 \| XP 20 / 30** (`android-speed-runner-cleared.png`) |

## Descent floor 1 (Ember Halls) on device (2026-09-13)

| Check | Result |
|---|---|
| Unity EditMode / PlayMode tests | 89/89 and 21/21 passed before the build |
| First attempt | Stopped before installing: another app had focus |
| Owner opened the game and confirmed | Focus, awake display and no keyguard verified before install and before and after every capture and tap |
| Install | APK SHA-256 `704F03BC55F6DC087D6E8F486E64C75C5F4D03F516A3964B7741D04B5EF06F24`, `Success` |
| Capture method | State-driven: each poll screenshot was classified locally by two pixels (choice card 43,56,82 at 540,1720; enabled Try again button 217,115,64 at 540,1905) and deleted; the left card was tapped on every panel, so upgrades went damage first and the forge chose Mend |
| Room 1, 2.5 s after launch | **Ember Halls \| Room 1/6 \| Ember Hall**, **Wave 1/2: combat is automatic**, Grunt 20/50, hero 88/100 (`android-floor-room1.png`) |
| Panel 6 at 31.5 s | **The forge: choose one**, **Mend / Restore 40% of your health**, **Temper / Gain 1 extra upgrade choice**; behind it **Room 4/6 \| The Forge**, **Sword \| 35 damage every 0,80s** (`android-floor-forge.png`) |
| Boss, 4.8 s after the Captain's level-up | **Room 6/6 \| Warden's Crucible**, **Forge Warden \| 90 / 300 HP** in the red enraged color, **Forge Warden is enraged!**, hero 52/100, **Sword \| 35 damage every 0,53s** (`android-floor-warden.png`) |
| Result at 46.4 s after 7 panels | **Floor cleared**, **Vanguard defeated the Forge Warden and cleared Ember Halls**, **Rooms cleared: 6/6 \| Level 7 \| XP 70**, **Build: Tempered Edge x5, Quickened Grip x1**, hero 42/100 (`android-floor-result.png`); the simulation predicted 38 HP for this path |

Limits: the whole floor took 46 s with instant automated choices, so a human run is roughly a minute. The victory cause line wraps to two lines and the forge status text shows faintly between the cards behind the overlay; both are cosmetic.

## Descent floor 2 (Quicksilver Vaults) and the checkpoint on device (2026-09-13)

| Check | Result |
|---|---|
| Unity EditMode / PlayMode tests | 100/100 and 24/24 passed before the build |
| First attempts | Stopped before any tap or capture, twice: the game was not open (home screen focused, no game process), including a 3-minute read-only wait |
| Install | APK SHA-256 `567C672B40F18698A305AF57B0BE91639E85FA777C2DDCEAE038D6975862487D`, `Success` while the home screen had focus; nothing was launched |
| Owner opened the game | The game already showed a finished run played on the new build: **Descent complete**, **Floor 2 \| 12 rooms \| Level 15 \| XP 150**, **Gold banked: 250**, hero 31/100 (`android-descent-owner-result.png`) |
| Automated run | Focus, awake display and no keyguard verified before and after every tap and capture. One **Try again** tap started a fresh run. The lower card was tapped only on the first two-card panel after a one-card level-up (the checkpoint), the upper card everywhere else (damage first, Mend) |
| Checkpoint, panel 9 at 48.4 s | **Checkpoint: extract or descend?**, **Extract / Bank all 96 gold and end the run**, **Descend / Secure 96 gold. Quicksilver Vaults: +20% enemy damage, +50% gold**; behind it **Floor 1 \| Room 6/6 \| Warden's Crucible**, **Forge Warden \| 0 / 300 HP** (`android-descent-checkpoint.png`) |
| Floor 2, 2.4 s after Descend | **Floor 2 \| Room 1/6 \| Mercury Stair**, **Grunt \| 35 / 70 HP**, **Gold 96  (0 at risk)** in gold, hero 33/100: descending did not heal (`android-descent-floor2.png`) |
| Floor 2 forge, panel 13 at 70.0 s | **Floor 2 \| Room 4/6 \| The Deep Forge**, **Mend / Temper**, **Sword \| 35 damage every 0,23s** (`android-descent-floor2-forge.png`) |
| Boss, 5.4 s after Mend | **Room 6/6 \| Warden's Vault**, **Forge Warden \| 210 / 420 HP** in the enraged color, **Forge Warden is enraged!**, hero 21/100, **Gold 175  (79 at risk)** (`android-descent-floor2-warden.png`) |
| Result at 79.5 s after 13 panels | **Descent complete**, **Vanguard defeated the Forge Warden and cleared Quicksilver Vaults**, **Floor 2 \| 12 rooms \| Level 15 \| XP 150**, **Gold banked: 250**, **Build: Tempered Edge x5, Quickened Grip x5**, hero 21/100 (`android-descent-result.png`); the simulation predicted 17 HP for this path |
| App log | 603 app-process lines in `android-descent-logcat.txt`; no E/Unity, exception, fatal or missing-reference matches |

Limits: both floors took about 78 s with instant automated choices, so a human Descent is roughly two minutes, still short of the GDD's 3–4 minute floors. Only the Descend branch ran on device; Extract and a floor 2 death with gold loss are covered by PlayMode tests. The hero entered floor 2 with 33 HP here versus 38 in the simulation. The status line still shows faintly between the cards behind the overlay (cosmetic, as on floor 1).

## Forge meta layer (Relic Forge and local save) on device (2026-09-13)

| Check | Result |
|---|---|
| Unity EditMode / PlayMode tests | 119/119 and 28/28 passed before the build |
| Install | APK SHA-256 `32523A4D00FCC7142DBB59361FCEEB9208769A9B29C24F9BE553EE9BBE05628E`, `Success`, installed while the game was not in focus; nothing was launched |
| First attempts | Stopped before any tap or capture: the game was not in focus during a 3-minute read-only wait, then another app was in front |
| Owner opened the game | Focus, awake display and no keyguard verified before and after every tap and capture; the run the owner had opened was continued, never reinstalled or relaunched |
| Checkpoint, panel 9 at 46.8 s | **Extract** tapped |
| Result, 2.6 s later | **Extracted**, **Floor 1 \| 6 rooms \| Level 7 \| XP 70**, **Gold banked: 96**, **Build: Tempered Edge x5, Quickened Grip x2**, **Forge gold 96: Second Wind is ready to forge**, **Try again** above **Relic Forge** (`android-meta-result.png`) |
| Relic Forge | **Gold 96 \| Deepest floor cleared: 1**; **Second Wind** with **Forge for 80 gold**; **Counterweight** dimmed with **150 gold: need 54 more**; **Start run** (`android-meta-forge.png`) |
| One tap on Second Wind | **Gold 16**, Second Wind **Equipped**, Counterweight **150 gold: need 134 more** (`android-meta-forged.png`) |
| Start run, 3.7 s later | Fresh run: **Floor 1 \| Room 1/6 \| Ember Hall**, **Relic: Second Wind** in gold under the weapon line, **Gold 0 (0 at risk)**, **Level 0 \| XP 0 / 10** (`android-meta-relic-run.png`) |
| Saved profile | `/sdcard/Android/data/com.cryptforge.prototype/files/profile.json` (191 bytes): `saveVersion` 1, `revision` 3, `gold` 16, `ownedRelicIds` [`relic_second_wind`], equipped `relic_second_wind`, `deepestFloorCleared` 1. `profile.json.bak` holds revision 2 (96 gold, no relics); revision 1 was the floor 1 depth record. No temp file was left behind |
| App log | 505 app-process lines in `android-meta-logcat.txt`; no E/Unity, exception, fatal, missing-reference or profile-recovery matches |

Limits: Second Wind's heal and Counterweight's counters did not trigger in this short device run; both are covered by PlayMode tests. Owner feedback: the run starts immediately and there is no pause button; only choice panels and leaving the app pause it. Cosmetic: the result and Relic Forge backdrops are 92% opaque, so the HUD and result text show faintly behind the forge hint line and between the Relic Forge cards.

## Pause control on device (2026-09-13)

| Check | Result |
|---|---|
| First build (commit 70d35fd) | APK SHA-256 `07CDD30A40ADDD2DD1D0FAE38923DE995B043379E2368CA91D336AD6E3DCFC9B`, `Success`. The **II** button was found in the HUD and tapped after four upgrade choices, but the run never paused. Cause: the HUD canvas had no `GraphicRaycaster`, so real touches never reached the button, while the flow tests click buttons directly. The very first tap also landed just as the first Grunt died and a choice panel hid the button |
| Fixed build (commit 82631dd) | APK SHA-256 `15A602FB9EA45855429BF319088453A2C11E650050E7775EC43AD863290E80C5`, `Success`; the game the owner had open closed and was launched again. The saved profile survived the update (**Relic: Second Wind**) |
| Pause | Tapped right after choosing an upgrade, during the advance delay: **Paused**, **Combat waits until you resume**, **Resume**; behind it **Grunt 0 / 50 HP**, **Sword \| 15 damage**, **Gold 5 (5 at risk)** (`android-pause-overlay.png`) |
| Frozen | Two captures 3 s apart while paused: 0.00% of sampled pixels changed (`android-pause-frozen.png`) |
| Leave and return | **Resume**, then Home 0.8 s later during the Runner fight; the launcher was confirmed in front before the game was brought back. It showed **Paused** with **Runner \| 15 / 30 HP** and the hero at 72/100, and 0.00% changed over the next 2 s (`android-pause-after-home.png`) |
| Resume again | Combat continued; 1.2 s later the Runner's kill opened the next upgrade choice |
| App log | 631 app-process lines in `android-pause-logcat.txt`; no E/Unity, exception, fatal or missing-reference matches |

Limits:
- **Label position:** the **II** label sat above its button, because the label was copied from the taller **Try again** button (`android-pause-hud.png`, first build). The scene was fixed afterwards with a regression test; the Relic Forge button label had a smaller version of the same offset.
  - **Fixed build (commit c46c841):** APK SHA-256 `0F8D8EB6E8F2821C2426774A215031285650C8D05BD8EBD9E9B34D0D62BBD6AD`, `Success`. **II** is centred in its button (`android-label-hud.png`).
  - **Fixed build, result screen:** after a floor 1 extraction, **Relic Forge** is centred in its button (`android-label-result.png`). The saved profile carried over the update and grew to **Forge gold 112: Counterweight costs 150**.
  - **App log:** 484 lines, no error matches.
- **Result build wording:** a relic that never triggered reads **Second Wind (0x)**, while the HUD omits the count until the first trigger.
- **Capture scripts:** the first pixel used to recognise the orange button fell on the **Resume** glyphs, so the scripts now sample the button's label-free left edge.

## Packs, Staff, Daggers and weapon unlocks on device (2026-09-13)

| Check | Result |
|---|---|
| Unity EditMode / PlayMode tests | 140/140 and 37/37 passed before the build (commit cc1a217, which includes the packs of commit 23e0de3) |
| Install | APK SHA-256 `8558B44BFB50FA307AA417882F646A6E4DF0B94D87F4EEE6AC8B51D14CAF48C5`, `Success` while the home screen had focus; nothing was launched |
| Saved profile before | Version 1, revision 4, 112 gold, Second Wind owned and equipped, deepest floor 1 |
| Owner opened the game | Focus, awake display and no keyguard verified before and after every tap and capture. The script always took the first card (damage first, Mend, Extract) |
| Sword, first wave | **Grunt defeated in 5 hits, 3,2s**, **Sword \| 15 damage every 0,80s**, sword and shield (`android-weapons-run1-1.png`) |
| Sword result, 10 panels at 55.6 s | **Extracted**, **Floor 1 \| 6 rooms \| Level 8 \| XP 108**, **Gold banked: 109**, **Build: Sword  \|  Second Wind (0x), Tempered Edge x5, Quickened Grip x3**, **Forge gold 221: Staff is ready to forge**, hero 56/100; the simulation predicted 54 (`android-weapons-result1.png`) |
| Relic Forge | **Gold 221 \| Deepest floor cleared: 1**. **Weapons: carry one**: Sword **Equipped**, Staff **Forge for 120 gold**, Daggers **Forge for 180 gold**. **Relics: equip one**: Second Wind **Equipped**, Counterweight **Forge for 150 gold**. Decimals follow the phone's Turkish locale (**0,8s**) (`android-weapons-forge.png`) |
| One tap on the Staff | **Gold 101**, Staff **Equipped**, Sword **Owned: tap to equip**, Daggers dimmed with **180 gold: need 79 more** (`android-weapons-forge-staff.png`) |
| Staff run | Brown staff with a violet orb, no shield. **Staff \| 23 damage every 1,20s** facing two full-health mites (`android-weapons-staff-2.png`); **Grunt \| 50 / 50 HP (+2 more)** at 38 damage (`android-weapons-staff-5.png`); **Tank \| 34 / 120 HP**, hero 39/100 (`android-weapons-staff-7.png`) |
| Staff result, 10 panels at 122.0 s | **Extracted**, **Build: Staff  \|  Second Wind (0x), Tempered Edge x5, Quickened Grip x3**, **Forge gold 210: Counterweight is ready to forge**, hero 38/100 against 52 simulated (`android-weapons-result-staff.png`) |
| One tap on the Daggers | **Gold 30**, Daggers **Equipped**, Sword and Staff **Owned: tap to equip**, Counterweight dimmed with **150 gold: need 120 more** (`android-weapons-forge-daggers.png`) |
| Daggers run | Two blades. **Grunt defeated in 7 hits, 3,3s** behind the first choice: 6 + 6 + 12 + 6 + 6 + 12 + 6 = 54, where 6-damage hits without crits would need 9 (`android-weapons-daggers-2.png`). **2 enemies defeated in 4 hits, 1,7s** at 11 damage (`android-weapons-daggers-4.png`), then three mites (`android-weapons-daggers-5.png`). The run was left to the owner |
| Saved profile after | Version 2, revision 8, **30** gold (112 + 109 − 120 + 109 − 180), relics unchanged, `ownedWeaponIds` [`weapon_staff`, `weapon_daggers`], equipped `weapon_daggers`. `profile.json` 313 bytes, `profile.json.bak` 286 bytes (the previous version 2 save), no temp file |
| App log | 584 app-process lines in `android-weapons-logcat.txt`, from process start at 19:16:58 to 19:21:46; no E/Unity, exception, fatal, missing-reference or profile-recovery matches |

Findings, both fixed afterwards (see `21`):
- **Staff health gap:** the Staff hero ended floor 1 at 38 HP against 52 simulated, while the Sword matched. The simulation readied the hero's weapon at every wave, but the scene's 1-second advance delay leaves a 1.2 s weapon still cooling down when the next wave arrives. The simulation now models the delay (the first Staff tuning then predicts 36), and the Staff was retuned to 10 damage every 0.9 s at full damage to everything in reach.
- **Forge backdrop:** the result screen's **Build:** line showed through the 92% Relic Forge backdrop, between the Daggers card and **Relics: equip one**. The backdrop is now opaque.

Limits: no still caught a gold crit flash; the PlayMode test checks it. A never-triggered relic still reads **Second Wind (0x)**. The retuned Staff and the opaque Forge were not yet on the phone when this section was written.

The first combat slice, kill → XP, upgrade choice, enemy attack with result/restart, Runner/Tank archetypes, attack speed tuning, Descent floor 1, gold with the Extract/Descend checkpoint, floor 2, the Forge meta layer with local save, pause control, packs and the three weapon behaviors with Forge weapon unlocks are running on the phone.

## Pixel-art arena on the phone (2026-09-14)

Build: commit 568a6e3 (walking packs, the rebalance, the framed arena and the pixel-art placeholders drawn from code).

| Check | Result |
|---|---|
| Unity EditMode / PlayMode tests | 194/194 and 47/47 passed before the build |
| Install | APK SHA-256 `5FF7C1FF5FA33E6A96EA120D4EC6EDBF5EA838BF0C4F561B0C6E463D09238908`, `Success` while the home screen had focus |
| Launch | The owner was connected over USB and asked to open the game; the script launched it and verified focus, an awake display and no keyguard before and after every capture and the one tap |
| First pack, about 3 s in | The stone-and-brass platform with its lantern towers, lava-seamed faces and keel, the violet void with rock islands, all behind the HUD text. The Grunt (slag body, lava cracks, ember eyes) fights beside the blue-steel knight while the mite it walked in with dies in a smoke puff; damage numbers **6** and **10** float above them; health bars over the enemies (`android-art-wave1-02.png`) |
| First pack cleared | **2 enemies defeated in 8 hits, 5,8s**, **Level 1 \| XP 11 / 22**, the Grunt's smoke and coins, hero 89/100, as the simulation and `22` predict (`android-art-wave1-05.png`) |
| Level-up | **Level up! Choose one upgrade** over the dimmed arena: **Tempered Edge +50% damage per hit** and **Quickened Grip +50% attack speed** (`android-art-levelup.png`) |
| Cinder Walk | Four mites with full health bars walk in from the far end: **Sword \| 25 damage every 0,80s**, **Cinder Mite \| 15 / 15 HP (+3 more)**, hero 73/100 (`android-art-wave2-02.png`); three seconds later **4 enemies defeated in 4 hits, 3,4s** (`android-art-wave2-04.png`) |
| App log | No Unity error, exception or missing-reference line |

Notes:
- The first level-up was taken on the phone by hand while the script was capturing (the script only tapped the second one), so the run reached **25 damage** with three Tempered Edges by Cinder Walk.
- The framing held on the phone: the hero's feet sit just above **Vanguard | HP** and the far end of the platform, where packs enter, stays below the enemy bar.
- Readability at phone size is good for the hero, the Grunt and the mites; the platform's far half and the islands sit under the top HUD text without hurting it.
- Seen, not fixed: the death smoke is as large as the hero and briefly covers him; a Grunt fighting at the hero's shoulder overlaps the cape; no frame rate was measured.
## Forge Burst on the phone (2026-09-14)

Build: commit 094d84c, APK SHA-256 `ECF52FA70B7EF17684C9CF7D76C81DA2D2AA0AD52C9CF9CFAFBF4BEA6B8BEC70`, installed over USB while the game had focus; the script relaunched the game, tapped the burst button once at 3.4 s (906, 1570) and captured before and after, focus-gated as always.

| Check | Result |
|---|---|
| Before the tap | The dashed blue ring around the hero's feet spans the platform's width at the hero's row; the round button with the blue burst glyph sits at the bottom right above **Vanguard \| 100 / 100 HP**; the Grunt and the mite have just walked into the ring (`android-burst-before.png`) |
| The tap | **20** floats over the Grunt beside the Sword's **10**, the mite is gone (**XP 1**), the ring has faded to a faint dash line and a blue radial fill drains around the button (`android-burst-burst-00.png`) |
| Later | The ring is bright again once the 8 s passed; the owner's own level-up tap took Tempered Edge meanwhile (`android-burst-cooldown-half.png`) |
| App log | No Unity error or exception line |

Seen, not fixed: the button's dark disc overlaps the platform's lower-right edge and the faces below it, which is where the mockup puts it too; the ring is drawn at the hero's feet, so its far half passes behind enemies standing in front of the hero.

## The walkable arena on the phone (2026-09-14)

Build: commit 011328e (the 9-unit diamond, drag-to-walk, packs from every corner, chests, the following camera), APK SHA-256 `16FCB13983917E04C9D854609893CCFC7B2878BDD7010120F659DF98F96DE69C`, installed over USB while the game had focus; the install closed the game and the script relaunched it. Focus, an awake display and no keyguard were verified before and after every capture, tap and drag. Drags are `adb shell input swipe` presses started in the background so captures could run while the finger was down.

| Check | Result |
|---|---|
| Unity EditMode / PlayMode tests | 205/205 and 53/53 passed before the build |
| First pack, 5.1 s after launch | The diamond platform fills the screen with its brass seams crossing under the hero; the hero stands at the centre inside the burst ring, the Grunt walks in from the far corner at the top, the mite's health bar enters at the right edge, and the closed chest waits 1.8 units to the right, fully on screen. **Grunt \| 50 / 50 HP (+1 more)**, **Wave 1/3**, **Vanguard \| 100 / 100 HP** (`android-walk-start.png`) |
| 7.1 s | The mite has fallen (**XP 1 / 10**); the Grunt stands behind the hero and strikes: the hero flashes white, **Vanguard \| 93 / 100 HP**, **Grunt \| 40 / 50 HP** with a **10** above it (`android-walk-fight.png`) |
| Drag right from 7.6 s (380 px over 1.4 s), captured 1.9 s after the press | The hero walked onto the chest: it stands open under his feet, **25** rises and the bar reads **100 / 100 HP**; the camera followed him, so the platform's centre mark is now at the left edge and the right rim with the stone faces below it is in view; the Grunt chased and stands at his shoulder at **10 / 50** (`android-walk-chest.png`) |
| 10.9 s | **2 enemies defeated in 7 hits, 6,1s** behind **Level up! Choose one upgrade** (`22` predicts 7 hits and 6.2 s for a hero who never moves); the Grunt's smoke and coin beside the open chest (`android-walk-levelup.png`) |
| The owner's play | The panel closed within 1.3 s, before the script's polling saw it: the owner was playing by hand. When the second script looked, the run stood at **Floor 1 \| Room 3/6 \| Slag Gate**, **Sword \| 30 damage**, **Level 5**, **Gold 41 (41 at risk)**, on a level-up with room 3's closed chest beside the hero and two mites' bars at the ring (`android-walk-room3-levelup.png`) |
| Tempered Edge tapped, +1.6 s | **Wave 2/2**, **Sword \| 35 damage every 0,80s**, **Tank \| 120 / 120 HP (+2 more)**; a mite walks in from the far corner under the top HUD text, the burst button's blue fill is draining, hero **73 / 100** (`android-walk-wave2.png`) |
| Drag up-left (280 px left, 400 px up over 1.5 s), 0.7 s in | The hero walks toward the far-left corner, the camera glides after him (the centre mark now sits below-left of his feet), the far corner of the platform and the void beyond it come into view (`android-walk-up.png`) |
| 1.9 s | The Tank reached him from the left and took a **35** (**85 / 120 HP**); it stands in front of room 3's chest, sorted by its feet; hero **72 / 100**; the lower-left rim and faces are in view (`android-walk-after-up.png`) |
| App log | No Unity error, exception or missing-reference line during either script |

Seen, not fixed:
- A lone enemy from the far corner stops straight behind the hero, so its body hides behind him and his hit flash (`android-walk-fight.png`); only enemies from the sides and the near corner are fully visible. A sideways stop for the far corner, or a sort that keeps the hero's attacker visible, is a follow-up.
- ~~A drag that starts on the burst button also starts a walk on that frame (the input reads the pointer before the event system has raycast the new touch); a tap does not move the hero, so this only shows when dragging out of the button.~~ Corrected the same day: this was deduced from code, never seen, and wrong. The event system runs at execution order -1000 (set in its script's meta file), before `HeroMovementInput` at -60, so a press is already known to be on a button in its first frame. A drag from the burst button on this build left the hero and the camera in place (see the next section).
- No walk animation: the hero slides. The upgrade cards still show the wave line faintly between them.

## Packs at the rim on the phone (2026-09-14)

Three builds, each installed over USB; focus, an awake display and no keyguard were verified before and after every capture, tap and drag. The game was launched with `am start` rather than `monkey`, which also injects one random event. To hold a drag, the scripts sent `adb shell input motionevent DOWN`, a `MOVE` 160 px to the right and, in a `finally` block, `UP`. Hero positions below are read off the platform's rim lines on screen (235 px per world unit) and are approximate.

| Check | Result |
|---|---|
| Button drag, build 16FCB139 (commit 011328e) | Relaunched; about 3 s in, a press on the Forge Burst button (906, 1570) dragged 220 px left over 0.9 s and released off the button. The seam cross stayed under the hero's feet and the chest at the right edge: the camera never moved, so the press never started a walk (`android-drag-button-before.png`, `android-drag-button-after.png`, the second behind the level-up that opened meanwhile, which also hides the button). This settles the wrong note in the previous section |
| First rim rule, build 080FF9A7 (commit 111e500) | Unity EditMode / PlayMode 208/208 and 54/54 before the build. Held right from 2 s; the first pack fell at 4.5 s (**2 enemies defeated in 8 hits, 4,5s**) and the level-up froze the hero about halfway to the right corner, at x ≈ 4.5 (`android-rim-v1-levelup.png`) |
| Its next waves | After Tempered Edge, wave 2's three mites entered only from the left, off screen, and fought on the hero's left (`android-rim-v1-wave2.png`, **Cinder Mite \| 15 / 15 HP (+2 more)**); wave 3's Grunt and mites did the same (`android-rim-v1-wave3.png`, **Grunt \| 26 / 50 HP**). Every enemy stood on the platform, but halfway out the rule had already closed three corners, against the owner's "from all four sides". The amendment draws such spots in short of the rim |
| Amended rule, build 58E9C943 (commit 4595add) | Unity EditMode / PlayMode 208/208 and 54/54 before the build. Held right for 1.7 s from 0.6 s: the Grunt from the far corner followed the hero to about x ≈ 4.9 (`android-rim-halfway.png`) |
| Wave 2 halfway out | 1.3 s after Tempered Edge, one mite fights on the hero's left and one on his right on the seam row, a **9** over the right one; **Cinder Mite \| 6 / 15 HP (+1 more)** (`android-rim-halfway-wave2.png`). 1.1 s later: **3 enemies defeated in 5 hits, 2,3s** (`android-rim-halfway-cleared.png`). At about the same spot the first rule had sent every mite from the left |
| On to the corner | Held right for 2.2 s: the hero reached the right corner's tip, with the void beyond it; the enemies of wave 3 fell on his left, on the platform, behind **4 enemies defeated in 10 hits, 3,1s** (`android-rim-corner-levelup.png`) |
| The owner's play | The owner took the next two level-ups by hand and walked the hero back to about x ≈ 6. Room 2's mites entered from the left and fell on the platform's side of the hero (`android-rim-room2-cleared.png`, **3 enemies defeated in 5 hits, 1,4s**); the next mite came the same way (`android-rim-room2-wave2.png`, **Wave 2/3**, **Sword \| 25 damage**) |
| App log | No Unity error or exception line in any of the three runs |

Not seen on the phone: an enemy turning round a hero at the rim. With Counterweight carried (9 counters by room 2) the enemies that reached the corner fell within a second, before a capture caught them standing; the turn is covered by `PackMotionTests` and by the PlayMode test that stands the hero at the right corner and follows every enemy of the next wave frame by frame. No enemy stood beyond the rim in any capture.

## Performance on the phone (2026-09-15)

Build: the frame-time probe (`21`), APK SHA-256 `03443B303B76A1E97E09239A460B768021B66CC34A640115C91023BE4C025605`, installed while the game had focus. The script relaunched the game and streamed `adb logcat -v time -s Unity:V` to a file (`device-perf/logcat.txt`, the `[Perf]` lines in `device-perf/perf.txt`). For the first 20 s it sent nothing to the phone. Then, every 2.5 s, it checked focus (one `dumpsys window` and one `dumpsys power` call) and tapped the first card position (540, 1720) and the **Try again** position (540, 1905). That took every first card: damage upgrades, Mend, Extract. A finished run was restarted in one tap; a tap on the arena never walks the hero. 106 taps over 150 s; two runs through floor 1 and the start of a third.

| Measure | Result |
|---|---|
| Frame rate | 30 windows, 8,854 frames. 28 windows at 59.9 fps with p50, p95 and p99 all at 16.7 ms, paused choices included; the other two held 59.3 and 59.5 fps around a run start. The probe read the display mode as 30 Hz while frames arrived every 16.7 ms, so that figure is not trusted |
| Frames over 20 ms / over 35 ms | 7 / 4 in 150 s. Of the four over 35 ms: the launch (a 2,118 ms first frame, then 213 ms) and the two run starts. The three others over 20 ms were single 33.4 ms frames with no wave, load or probe line; they may come from the script's `dumpsys` calls |
| Wave spawns in combat | 20 waves of 1, 3 and 4 enemies. `StartWave` took 2.7–16.6 ms, median 10.9 ms; not one spawn frame ran past 16.7 ms. Garbage per wave: 77–538 KB for three enemies, 103–644 KB for four, 700 KB for a single elite or boss; each such window ran 3–6 GC passes |
| A new run (**Try again**) | One frame of 66.8 ms and one of 50.1 ms, with 17.6 MB allocated in that frame and 20–24 GC passes in its window. It is the frame in which the scene loads again and redraws its placeholder art from code |
| Between spawns | 3–12 KB allocated per 5 s window, the probe's own log lines included, and no GC pass: combat allocates next to nothing per frame |
| Memory | Total in use 110.5–116.3 MB and managed 1.0–1.4 MB across both restarts: no growth, so enemy and arena textures are released |
| App log | No Unity error or exception line |

Reading: the S23 has room to spare. The two costs a mid-range phone will feel first are the wave spawn, about 11 ms of main thread and up to 0.7 MB of garbage per wave because every enemy draws its own sprites as it enters, and the 17.6 MB, 50–67 ms run start. On a phone two to three times slower the spawn alone could miss a frame or two per wave; that is an estimate, as no mid-range device was measured. Both costs sit in the placeholder art's view code (`EnemyLookView`, the arena views): caching drawn sprites per enemy look and across run restarts would remove most of both without changing how anything looks.

## Art sprint 01 — reference capture only (2026-09-15)

The owner authorized the first art round and USB/ADB inspection. An announced reference capture on the S23 (RFCW20W2WFX) was saved as `TestResults/art-sprint-01/current-01.png`, 1080x2340. Before screencap, dumpsys confirmed `com.cryptforge.prototype` focus, `mWakefulness=Awake` and `isKeyguardShowing=false`; the same checks passed again before retaining/pulling the capture.

The existing level-up state shows the blue-steel hero, chest, large combat title/stat lines and two navy upgrade cards; a faint old status line remains visible between the cards. This is reference evidence for art work, not a pass of revised UI or a new performance measurement. No new APK was built/installed, no profile reset and no fresh installed-APK hash verification occurred in this art round.

Generated gameplay camera alternatives and a themed choice concept are under `ArtDirection/2026-09-15/sprint-01/`, with exact prompts and integration limitations. They have not been applied to the phone. Record actual UI/device acceptance in a later integration entry, and update the old card-colour detection coordinates then.

## Local telemetry on the phone (2026-09-15)

Build: the local telemetry log (`21`), APK SHA-256 `BAA8E568E0B8D3D9DFB00A3FBB243311F45916AAD17C7D73CA80FF6E90623A97`, installed with `adb install -r` while the launcher had focus, keeping app data. The session was announced to the owner first.

The script:
1. Backed up `profile.json` and `profile.json.bak`.
2. Relaunched the game with `am start`.
3. Took a focus-gated screenshot about once a second. It tapped the first card whenever the choice-panel pixel (1000, 1720) showed, and stopped at the result screen's **Try again** pixel (540, 1905). It never opened the Relic Forge.
4. Pulled the telemetry folder (`TestResults/device-telemetry/`).

| Check | Result |
|---|---|
| Before | No `telemetry` folder on the device; profile version 2, revision 42, forge gold 2723 |
| Run | 9 panels in 73.6 s: seven level-ups (Tempered Edge ×5, then Quickened Grip ×2 once Tempered Edge was full), Mend, Extract |
| Result screen | **Extracted**, **Floor 1 \| 6 rooms \| Level 7 \| XP 121**, **Gold banked: 116**, **Build: Staff \| Counterweight (26x), Tempered Edge x5, Quickened Grip x2**, **Forge gold 2839** (`device-telemetry/result.png`) |
| Log file | `events.jsonl`, 11,081 bytes, 41 lines. One session and one run, `seq` 1 to 41 without a gap. LF line ends, UTC timestamps, a decimal point in every number |
| `run_start` against the screen | `weapon_staff`, `relic_counterweight`, `forge_gold` 2723, `deepest_floor` 2 |
| `run_end` against the screen | `extracted`, 1 floor, 6 rooms, level 7, 31 kills, 7 upgrades, gold 116, banked 116, lost 0 |
| Choices against the panels | Seven `upgrade_selected` (five `upgrade_damage`, two `upgrade_attack_speed`), one `forge_selected` (`forge_mend`) and one `extract_choice` (`extract`): the nine panels |
| Gold against the screen | Room kill gold 10 + 13 + 18 + 0 + 25 + 50 = 116; 2723 + 116 = the 2839 on screen |
| Order at the start | `session_start`, `run_start`, `room_start` 1, `first_kill` (a Cinder Mite at 1.6 active s), then each upgrade offer before its selection |
| Order per room | the room's `currency_earned`, then its `room_complete`, then the next `room_start`; `boss_start` names the Forge Warden |
| Order after the boss | its kill's `upgrade_offered`, the boss room's `room_complete`, `boss_end` (defeated), `floor_complete`, then `upgrade_selected`, `extract_choice`, the banked `currency_earned` and `run_end` |
| Time, run | 46.8 active s, 22.8 choice s, no pause, no background |
| Time, rooms | Rooms 1–3: 13.1, 12.4 and 9.3 active s; the forge 1.0; the elite 4.7; the boss 6.3. The boss's level-up and the checkpoint came after the floor closed, so they count toward the run only |
| Not in this run | No `chest_opened` or `ability_used`: the script never walked the hero or tapped the burst. No `app_background` |
| Profile after | Version 2, revision 43, forge gold 2839, the same keys as before |

Reading:
- Floor 1 took 46.8 s of active play against the GDD's 3–4 minute floors.
- Choice panels took 22.8 s, most of it the script's screenshot polling rather than thinking time. A human session's log would show real decision time.

## Foundry UI integration on S23 — 2026-09-15–16

The owner authorised resumed runtime work and USB testing after the telemetry worker finished. Device runs were announced before installation/control. Both installs used `adb install -r`, preserving data; launch used `am start`, not `monkey`. Every tap/swipe/capture checked Cryptforge focus, awake display and hidden keyguard; captures were checked again before pulling them. Local helper: `TestResults/art-device-review.ps1`. Captures, profile backups and pulled logs: `TestResults/device-art-ui-01/`.

### Builds and checks

- Initial integration APK: `BD861ACCED1A2C6C3149FF2FC5ABB3C07EF611A07E814C2AE0EBC9F85DA075AF`. Full gate passed: .NET 239, EditMode 251, PlayMode 62. Actual device review exposed a retained blue HP colour and square corners of the opaque ability icon covering the circular bezel.
- Final corrected APK: **`8D1515975F8C3F1E0987FACC61379AFEF9B1E5239EA7D92C38DE23E0C517A149`**, 24,738,453 bytes. HP changed to red, XP to blue, and the icon is circularly masked beneath the bezel. The full gate passed again: .NET **239/239**, compile **0 warnings/errors**, EditMode **251/251**, PlayMode **62/62**, build exit 0 and static integrity (272 project GUIDs, 515 scene objects/components). `sha256sum` of the installed package's `base.apk` matches the local APK exactly.

### Observations

| Evidence | Observed result |
|---|---|
| `01-initial.png`, `02-choice.png` (the latter is live combat despite its filename) | Compact HUD, live weapon/gold/upgrade/relic counts, quiet floor, current procedural actors and following camera at width 6 |
| `03-state.png` | Two framed upgrade cards; readable names/effects; icons match; no old status text in the gaps |
| `04-pause-details.png` | Pause exposes detailed weapon/relic/target/gold information, including Turkish decimal commas, without overlapping Resume |
| `05-movement.png` and later sequence | Resume and a gated horizontal drag work; camera follows the displaced hero |
| `review-09.png` | Right-side Forge Warden's full body and overhead HP bar fit inside the view; boss-only top readout is visible |
| `review-12.png` | Extract/Descend descriptions fit; these text options do not display an unrelated upgrade icon |
| `review-13.png` | Extraction succeeds: floor 1, six rooms, level 7, XP 121, 126 gold banked; Staff/Counterweight, damage x5 and speed x2 |
| `final-01-ready.png` | Final installed build: red HP, coherent circular ability face, no exposed square icon corners |
| `final-02-cooldown.png` | Final build: a gated ability tap produces damage, dims the icon and displays 8 seconds; XP fill is blue |
| `final-03-state.png` | Final build's choice state after resumed play |

The owner also played during this session, including loadout changes and selections; the sequence is not an isolated deterministic playthrough. Before review, the profile was version 2, revision 44, gold 2839, both unlockable weapons and both relics owned, deepest floor 2, Daggers/Counterweight equipped. After review it retained those unlocks, deepest floor and equipped items with gold 3132 earned during play. No profile reset or backup restore was performed over the owner's live progress.

Final process log `final-unity.log` had **0** matches for Unity errors/exceptions. A sampled 5-second window held 59.9 fps, 16.7 ms p95, 124 MB total memory; that window was paused (`time scale 0`), so it is not a new combat-performance benchmark. Existing telemetry continued recording; logs are pulled to `telemetry` and `telemetry-final`, including final-build upgrade events.

### Updated S23 touch/detection coordinates

At the current 1080x2340 layout: Pause about `(988,178)`, ability `(923,2108)`, first choice `(540,1300)`, second choice about `(540,1590)`, Resume/Try again `(540,1905)`. The choice cards are lower-middle, no longer at the old `1720` card-colour sample. The review script detected a visible first card from the right edge `(980,1250)` (RGB approximately 102,80,68) plus dark left interior `(98,1250)` before tapping; it never blindly tapped on a timer. Re-measure after any layout, safe-area or texture change, and keep the focus/awake/keyguard gate before every input/capture. A pixel match identifies a card, not its meaning; select the desired option from the current prompt.

### Limits

Phone evidence is S23 at width 6. Guardian-left/rim extremes and widths 6/7.5 at 1080x1920/2340 are covered analytically, not by a paired phone capture of every case. No manual short-screen UI pass or mid-range-device validation was performed. Result/Relic Forge styling was open at this checkpoint and is covered by the following entry. The large pause heading/resume button and individual badge tap explanations remain open. These are the first integrated HUD/card assets, not completion of all game art.

## Foundry result and Relic Forge menus on S23 — 2026-09-16

The owner authorised continuing the art integration at medium reasoning effort. The unattended device run was announced. Installation used `adb install -r`; no data clear or profile restoration was performed. Every input/capture used the existing focus/awake/keyguard gate, with a second check before retaining captures. One pre-capture check stopped when those conditions were briefly unavailable; no capture was taken on that attempt. A fresh check found Cryptforge focused, awake and unlocked before resuming.

**Build:** APK SHA-256 `B8176A69112EAFB7E8E7322B4768124102FACAE631F77E3B8BE6915D5B5D37E0`, 24,738,453 bytes. Installation returned Success; the installed `base.apk` SHA-256 matches. .NET **239/239**, compile **0 warnings/errors**, EditMode **251/251**, PlayMode **63/63**, Android build exit 0; static integrity resolved 272 project GUIDs and 555 scene objects/components.

The first full PlayMode run passed 62/63 and blocked the APK: the old layout guard asserted `Forge Surface — Expected: 0, But was: 0.015` because it required every safe-area child to be bottom-anchored. The guard now checks actual bounds of all ten functional rows at safe heights 1760, 1920 and 2232, preserves opacity/containment/non-overlap checks, and excludes the decorative background. Its targeted rerun passed, followed by the full passing gate above. Result/Forge text fit at short and tall safe-area heights is also covered automatically.

Local evidence: `TestResults/device-foundry-menus/` (ignored).

| Evidence | Observed result |
|---|---|
| `05-result.png` | Extracted after floor 1, six rooms, level 7, XP 121; 116 gold banked. Staff, Counterweight (26 triggers), damage x5 and speed x2 remain readable inside one framed summary. Both result actions fit. |
| `06-forge.png` | All three weapons, two relics and Start run visible within the S23 safe area; descriptions readable, relic art correct, Staff and Counterweight marked Equipped in green. Opaque Forge background hides the result screen. |
| `07-equipped-alternative.png` | Tapping already-owned Daggers and Second Wind updates their text and accents to green; prior items become blue Owned. Gold stays 3531. |
| `08-restored-loadout.png` | Staff and Counterweight restored through the normal cards before leaving the Forge. |
| `09-new-run.png` | Start run reloads into floor 1, room 1 with Staff/Counterweight, a fresh run counter and the existing HUD/artwork intact. |
| `final-unity.log` | No matches for checked Unity error/exception/fatal patterns in the app-process log. |

The before profile was version 2, revision 53, gold 3415. After extraction and four owned-item selections it was version 2, revision 58, gold 3531; both weapon unlocks, both relic unlocks, deepest floor 2 and Staff/Counterweight loadout were retained. The 116-gold difference matches the result; no purchase was needed.

Current S23 touch centres (1080x2340): result Try again approximately `(540,1565)`, Relic Forge `(540,1728)`; Forge weapon rows approximately `(540,665)`, `(540,945)`, `(540,1225)`, relic rows `(540,1638)` and `(540,1915)`, Start run `(540,2175)`. Re-measure after layout/safe-area changes. Combat/choice coordinates from the previous entry are unchanged.

Limits: this phone pass exercised extraction and owned/equipped states. Defeat/victory, insufficient-gold/purchase logic and persistence remain covered by automated flow tests, not a fresh physical-device capture of every state. Short/tall safe-area coverage is automated; no second physical device, localization audit or new sustained performance benchmark was run. Pause-menu styling, badge detail interactions, weapon illustrations, character animation and audio remain separate work.

## Wider portrait camera on S23 — 2026-09-16

The owner retained portrait and confirmed the proposed Opus ten-enemy task was not started. This slice only changes the camera's visible width from 6 to 9; it does not increase pack capacity or replace the cosmic backdrop/actors with the new cavern concept.

**Verified:** .NET **239/239**, compile **0 warnings/errors**, EditMode **251/251**, PlayMode **63/63**, build exit 0, static integrity **272 GUIDs / 555 scene objects/components**. Development APK: 24,738,453 bytes, SHA-256 **`0E6CE9B82B47B6999B0AF68D8708B2882915AEDE0F16E861685755158FD1C4E2`**. `adb install -r` returned Success and the installed `base.apk` hash matches. The unattended run was announced; each input/capture used the focus/awake/keyguard checks, including the second capture check before pulling. The original profile was backed up locally, never cleared or restored over live progress.

Local evidence: `TestResults/device-camera-09/`.

| Evidence | Observed result |
|---|---|
| `01-start.png` | Current Staff/Counterweight profile starts at width 9. More floor and distant structures visible; HUD and ability button keep their previous size. A small enemy is visible near the right edge. |
| `03-during-left-drag.png` | Capture taken during the scripted 2600 ms horizontal drag: the floor centre has shifted relative to the hero and a Staff area hit/damage number is visible. The gesture does not open Pause or activate the ability. |
| `04-after-left-drag.png` | A level-up panel opens during the gesture; simulation pauses as before. The gesture is not an uninterrupted controlled kiting benchmark. |
| `05-during-diagonal-drag.png`, `06-after-diagonal-drag.png` | A 2300 ms diagonal drag moves toward the far rim. The following camera retains the hero, nearby mites and the chest. |
| `combat-17.png` | Guardian to the hero's left near the far rim: full body and overhead bar visible, plus boss-only HUD bar. |
| `07-result.png` | Extracted after floor 1, six rooms, level 7, XP 121; 126 gold banked, Staff/Counterweight (22 triggers), damage x5 and speed x2. |
| `final-unity.log` | No matches for the checked Unity error/exception/fatal patterns in the app-process log. |

The profile went from version 2 / revision 58 / 3531 gold to version 2 / revision 59 / 3657 gold. Owned weapons/relics, deepest floor 2 and Staff/Counterweight loadout were retained. The 126-gold difference agrees with the result; no Forge purchase or equip operation was performed. This is a short live smoke review with possible owner interaction, not a deterministic isolated run.

Remaining issue: near the far rim, the unbounded follow camera spends substantial upper-screen area on the void. Width 9 improves surrounding threat visibility, but room-aware camera limits and a separately sized boss arena remain necessary for the reference composition. The seven visual actors in `ArtDirection/2026-09-16/camera-review/` are offscreen staging, not this live phone encounter; the six staged render variants assume safe insets and use converted canvases. No physical short-screen device, paired three-width phone test, sustained combat benchmark or ten-enemy kiting acceptance is claimed. Touch coordinates remain those of the preceding menu/HUD slice.

## Ten enemies on the S23 — 2026-09-16

The USB session was announced before it began. Every input and capture was preceded by the focus/awake/keyguard check and the app held focus throughout. The device profile and the telemetry log were pulled to a local backup first, and both were restored afterwards and verified by SHA-256 against that backup; the `development` folder was deleted from the device at the end.

APK 24,738,586 bytes, SHA-256 `8D73EE69683BCDB1FA061F04AB15B6303C87B86477C806912D722CACA7A0653A`, `adb install -r` returned Success. The proof floor was reached by pushing one line, `floor_density_proof`, into `<persistentDataPath>/development/start-floor.txt` - the scene file was not touched.

| Evidence | Observed result |
|---|---|
| `01-launch.png` | The HUD reads **Floor 1 / Room 1/1, Crowded Floor**: the development floor is live on the device. |
| `02-state.png`, `04-dense.png` | Ten enemies - two Grunts and eight Cinder Mites - stand in a ring round the hero, each with its own bar and damage numbers. This is the density the slice set out to show. |
| `03-now.png` | **Descent complete**, ten kills, level 2, 10 gold banked, hero at 54/100 HP with Daggers and Counterweight. |
| `05-flag-removed.png` | With the file deleted the app starts on **Ember Hall, Room 1/6** again: the development hook leaves nothing behind. |

**Frame cost of a ten-enemy room, from the `[Perf]` probe.**

| What | Measured |
|---|---|
| Steady dense combat, ten enemies fighting | 59.5-59.9 fps; average 16.7-16.8 ms; p50 16.7, p95 16.7, p99 <= 17.0 ms; longest frame 17.1 ms; **no frame over 20 ms**; 3-138 KB allocated per 5 s window |
| The frame ten enemies enter on (four spawns measured) | `StartWave` **8.7-10.9 ms**; the frame itself **50.0-50.1 ms**; main thread 56.0-63.0 ms; **18.2-18.3 MB allocated in that one frame**; 20-22 GC runs in the window that contains it |
| Restart: result screen -> Try again -> scene reload and respawn | one 50.1 ms frame and nothing else over 35 ms; the scene reload itself costs under a frame |

So ten enemies fighting are free on this device - the encounter holds 60 fps with no frame even reaching 20 ms - and the whole cost is the single frame the pack enters on, which drops about three frames. That frame is dominated not by time but by allocation: **18 MB of managed garbage per wave**, from the actor sprites being generated per enemy as they spawn. It is the same 18 MB whether the wave enters at the start of a run or after a restart. A sprite cache keyed by enemy id is the obvious remedy and would remove almost all of it, but it lives in the art code, so it is handed to the art owner rather than done here.

Caveats. This is a flagship device; no low or mid-range phone has been measured. The measurement covers one wave of ten in an otherwise empty room, not ten enemies plus a boss, chests and effects. The runs were live, with owner interaction possible at the phone, so the health and gold figures in the captures are a smoke reading, not the deterministic numbers in `22`. Kiting was not driven by script on the device: the scene's agreement with the routed simulation is proved in PlayMode instead, which is the stronger statement.

Local evidence: `TestResults/device-density-01/` (captures and the two `[Perf]` logs).
