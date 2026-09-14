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