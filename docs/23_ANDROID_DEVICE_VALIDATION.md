# Android device validation — passed first combat smoke test

## Painted platform materials: S23 — 2026-09-18

**Gate:** .NET272, clean compile, EditMode284, PlayMode104, graphics1, Android exit0, integrity415/555. APK24,740,096 bytes; SHA-256 `64F49049A08DDB92396831F28FEC24DEFF42F902B8F849E6D87931F59F439475`. `adb install -r` succeeded and pulled installed base.apk hash matches. S23/1080x2340, serialRFCW20W2WFX. No project settings/package changes.

**Fresh state and installation.** The phone was awake/unlocked in the game, paused at floor2/room1, level8,72HP,149 run gold. Permanent profile revision134/9930 gold/Daggers/Second Wind (both unlockable weapons/relics, deepest2). The existing run resumed through its last speed upgrade and Mend, ending in victory with293 total run gold. Because the earlier floor checkpoint was already banked, only157 more was added: preinstall revision135/10087. Fresh profile/backup/telemetry were pulled; preinstall and immediate postinstall profile SHA-256 both `4E625F80711AD4BDF3F58208490C054CF0EBD9151DB4D7CE983B59E3365A4362`. No backup restoration, purchase or equipment change.

The first postinstall launch entered ordinary Ember Hall: the first temporary proof-file write was empty, then corrected. The owner also played during this normal run, confirmed when asked. Agent input stopped for coordination; the temporary flag was removed. The owner explicitly authorized restarting and handed control back. Before the controlled proof, the current saved state was revision136/10223 gold, same loadout/unlocks/deepest floor. The owner/agent mixed normal run is not controlled movement or balance evidence, and no old backup was restored over its progress.

**Controlled art smoke.** Verified `floor_density_proof` was written, then launched one ten-enemy room. Capture20 shows the actual two-Grunt/eight-Mite encounter after a southeast drag;22 shows combat and the near coping/wall corner after the damage choice and southwest drag. Ground materials, imported actors, health bars and ground ring render in the correct order, without the old pixel rim protruding. Lower keel/chains and chest/ring remain visibly provisional; projected crowd overlap persists. Capture21 was already an upgrade panel, so that attempted swipe does not prove movement. Capture23 was already the result, so its attempted northwest swipe is not four-direction evidence.

Telemetry for controlled session500ffe81a49b491ea0c12c19a2393baf records victory,10 kills, one damage upgrade,14.089 active seconds,65.022 choice seconds,65.224HP at completion and10 gold banked. This is an art smoke, not a balance comparison. Removed only the temporary start-floor file; its existing directory remains. Try again loaded normal floor_ember_halls, confirming a warm scene restart with the painted surface intact.

**Performance/limits.** Warm normal restart: StartWave0.59ms, frame16.7ms, main thread24.4ms,948.9KB whole-frame allocation. The surrounding five-second window averaged59.9fps and max17.1ms. Other windows mix active combat, choice pause and result screens (some33.4ms intervals), so no sustained dense-combat or mid-range claim. Current-process log search found no Exception, Error:, FATAL, NullReference or MissingReference matches. Runtime tests separately establish shared mesh/material/foundation identity across scene reload; the phone capture alone cannot measure ownership.

**Cleanup/state.** Final profile revision137/10233 gold/Daggers/Second Wind, same unlocks/deepest2; SHA-256 `3B63C73F70886712C22404478776D1C85A06C3BE1E75394AAAAB1F31594C5240`. The final controlled proof added10 to the owner's latest state. Telemetry and all new progress retained. Phone left at normal Ember Hall1/6 first upgrade choice, level1,XP10/22,5 at-risk gold,85HP. Inspect fresh before future input.

Every tap/swipe/capture gated on app focus, awake display and hidden keyguard, with post-capture focus recheck. Ignored evidence: TestResults/device-platform-runtime-01 (fresh backups, installed APK, logs, telemetry and captures). Two unchanged game-only screenshots are also in ArtDirection/2026-09-18/platform-runtime-01. Browser gallery desktop/390x844 controls/layout were checked.

## Slag Brute runtime art on S23 — 2026-09-17

**Gate:** .NET **272/272**, compile **0 warnings/errors**, EditMode **284/284**, PlayMode **100/100**, graphics capture **1/1**, Android exit **0**, integrity **404 GUIDs / 555 scene objects/components**. APK **24,739,917 bytes**, SHA-256 **`AC9D4FC627CD94484A2923DEBB7AEC2CD2894CF3E51537207123B1BA1A6814B0`**. `adb install -r` succeeded; pulled installed `base.apk` matches that hash. Same S23,1080x2340.

**Fresh state and preservation.** The phone initially showed another app; no off-game screenshot was taken. The owner explicitly answered that the phone was free for testing. Fresh backup was revision **131 /9658 gold /Daggers /Second Wind**, same unlocks and deepest floor2. Game launch showed an existing Ember Hall first-choice run. It was completed through the normal upgrade/Mend/Extract UI before installing, banking126 gold. Preinstall profile was **revision132 /9784 gold**. Preinstall and immediate postinstall SHA-256 both **`D2452610BF070D33D434D3BDF6AD7D7FE87D9C6AA0C62F4D305FEB0ED68C069A`**. No purchase, equipment change, reset or backup restoration.

**Physical art smoke.** An otherwise absent development/start-floor.txt selected one ten-enemy proof. Capture10 is startup only. Capture11 shows two newly painted Grunts among eight Mites after the southeast drag;13 shows the front Grunt near the lower rim after southwest movement;14 shows rear facing after northwest movement and an active damage number/bar. The body renders and bar clearance are readable at phone scale. Hero/Mite and chest/Grunt depth overlap remain visible. This is a brief movement/art smoke, not exhaustive four-direction animation or foot-contact acceptance. Automated graphics/tests cover all four facings and hit/death details.

Telemetry records one proof **victory**,10 kills, one damage upgrade, **15.741 active seconds**,49.338 choice seconds, **43.144 HP** at room completion and **10 gold banked**. It is not a balance comparison. The current-process log search found no Exception, Error:, FATAL, NullReference or MissingReference matches. No sustained-performance or mid-range-device claim.

**Cleanup.** Removed only the temporary start-floor file, leaving the existing directory. Try again returned to the ordinary Ember Hall; telemetry confirms floor_ember_halls. Final permanent profile **revision133 /9794 gold /Daggers /Second Wind**, same unlocks/deepest floor. SHA-256 **`DCDBB76F79A2348CF6303AD7B470226D3E08C88334CA2F8CF2BA6A1487325740`**. The136-gold increase is the prior run's126 plus the proof's10. Fresh backups and new progress/telemetry are retained; none restored over current progress.

All input/captures passed app-focus/awake/no-keyguard checks and captures rechecked focus before retention. Ignored evidence: TestResults/device-grunt-polish-01 (backups, installed APK, captures, log, telemetry). Three unchanged game-only captures are also in ArtDirection/2026-09-17/grunt-polish-01. Phone returned to normal Ember Hall1/6 first upgrade choice (level1,XP10/22,5 at-risk gold,85HP); inspect fresh before future input.

## Contact, sword grip and compact Mite bars: S23 — 2026-09-17

**Gate:** .NET **272**, EditMode **284**, PlayMode **96**, clean compile, targeted Vanguard/Mite **13**, graphics capture **1**, Android exit **0**, integrity **375/555**. APK **24,739,917 bytes**, SHA-256 **`1DCB931D6ECCA73424870687AB3AB32E21AC4E2C6BACD67A3C3B66492F4FB01C`**. Installed with `adb install -r`; installed `base.apk` hash matches. Same S23/1080x2340.

Fresh inspection found the game active on **Ember Hall 1/6, level-2 choice, XP25/36, 10 at-risk gold, 70 HP**, with stored profile **revision118 /8598 gold /Daggers /Second Wind**. The unattended review was announced. The existing run was completed through its normal upgrade/Mend/Extract choices, banking **116** gold. Sword was temporarily equipped through Relic Forge for art review, with no purchase. Immediately before installation the profile was **revision120 /8714 gold /Sword**; preinstall and immediate postinstall SHA-256 both equal **`52718643171FD01FA875169835B98518ECDE53E4F965C7D631222297E615FF9A`**. Fresh backups were retained; none were restored.

An otherwise absent `development/start-floor.txt` selected one ten-enemy proof run. Captures `11`–`16` show entry, southeast then southwest drags, compact Mite bars, front Sword presentation, silhouette flashes and a rear Sword strike. The moving front capture includes a large enemy overlapping the upper body, a remaining readability limitation. Telemetry records **victory /10 kills /13.355 active seconds /55.812 choice seconds /65.224 HP /10 gold banked**, with one damage upgrade. This is a short art smoke run, not a balance comparison or exhaustive physical four-direction acceptance.

Removed only the temporary start-floor file; its pre-existing empty directory remains. Re-equipped Daggers in Relic Forge and started the ordinary floor. Left at **Ember Hall 1/6, first upgrade choice, level1, XP10/22, 5 at-risk gold, 85 HP**. Final stored profile **revision122 /8724 gold /Daggers /Second Wind**, same unlocks and deepest floor2. Final SHA-256 **`8D9B091082AC58682E6C7B6F0FBA4FC2948BBA63898A8FEA380FE3559F7DA570`**. The 126-gold difference is the existing run's 116 plus the proof's 10; progress/telemetry were retained, not replaced.

Every input/capture checked app focus, awake display and no keyguard, and captures rechecked focus before retention. Evidence: ignored `TestResults/device-contact-grip-01/`; unchanged game-only captures also in the art gallery. Current-process log search found no Exception, Error:, FATAL, NullReference or MissingReference matches. Warm normal restart sampled **1000.4 KB** whole-frame allocation and **1.12 ms** StartWave for two enemies. Mixed combat/choice/result windows do not prove sustained performance; no middle-tier device or new performance gate. Final gait/lighting, large-enemy occlusion and matching other equipment remain open.


## Cinder Mite and hit effects: S23 review — 2026-09-17

**Gate:** .NET **272/272**, clean compile, EditMode **284/284**, PlayMode **94/94**, targeted Mite **5/5**, art/HUD **9/9**, graphics capture **1/1**, Android exit **0**, static integrity **367 GUIDs / 555 scene objects/components**. APK **24,739,942 bytes**, SHA-256 **`23EDF4B3D2FA6DC853A4B579254D5200D5483009D1B4B1A948EB6EDCC56BEED1`**. Installed with `adb install -r`; device `base.apk` hash matches. Same S23, 1080x2340.

**Fresh state:** the phone was awake/unlocked on its launcher and no game process was running, so there was no active run to discard. The owner had advanced since the preceding review: profile version 2, revision **117 / 8588 gold / Daggers / Second Wind**, both unlockable weapons and relics, deepest floor 2. Backups were pulled before installation; preinstall and immediate postinstall profile SHA-256 both equal **`12FAE1C6A2CB54AF0488157B299B4802698617033BE7221C72659D4514A66120`**. No reset, purchase, loadout change or backup restoration occurred.

The unattended run was announced. Every input/capture checked app focus, awake screen and hidden keyguard; captures checked again before retention. An otherwise absent `development/start-floor.txt` selected `floor_density_proof` for one run: two Grunts and eight Mites. `01-proof-entry` is only startup, not gameplay evidence. `02-proof-approach` shows the living pack and painted Mites arriving around the hero. After the first damage upgrade a 2.5-second southwest drag moved the hero toward the rim; `04-proof-moving` and `05-proof-after-drag` show the following Mites, hit feedback and compact collapsed remains. Two first-card damage upgrades were selected in total. Telemetry records **victory, 10 kills, 13.989 active seconds, 85.710 choice seconds, 62.464 HP, 10 gold banked**. This is a short art smoke run, not a controlled balance comparison.

**Cleanup:** deleted only the temporary start-floor file; its previously existing empty directory remains. Try again returned to **Ember Hall 1/6**. Left on the first upgrade choice, **level 1, XP 10/22, 5 at-risk gold, 85 HP**, Daggers/Second Wind. Final stored profile **revision 118 / 8598 gold** retains the same loadout, unlocks and deepest floor. The difference is exactly the proof victory's 10 gold. Final profile SHA-256 **`C29C7F70DA51B3CE02F97B6B3BE5B5422F0152E571C94488605F3225CB16CF5F`**. Telemetry was retained, not replaced.

**Limits and performance:** the short window covering choice release and movement held 59.7 fps with one 33.4 ms interval; the following mixed combat/choice window held 59.9 fps with max 16.7 ms. Much of the log is paused on choices or sitting on results; it cannot establish sustained dense-combat performance. Cold start included 2143.5 and 218.7 ms intervals. Normal warm restart sampled 1086.4 KB whole-frame allocation and StartWave .59 ms for two enemies. Current-process log search found no Exception, Error:, FATAL, NullReference or MissingReference matches. No middle-tier phone, GPU comparison or physical Sword run was performed; Sword direction/effect lifetime is covered by Unity tests and captures. Large health bars, earlier Grunt/chest art and some depth overlap remain visible.

Ignored full evidence: `TestResults/device-mite-runtime-01/` (fresh backups, all screenshots, current-process logs, final profile/telemetry). Two unchanged game screenshots are retained for review as `ArtDirection/2026-09-17/mite-runtime-01/device-crowd.png` and `device-kiting.png`.


## Four-direction Vanguard: S23 movement review — 2026-09-17

**Gate:** .NET **272/272**, compile **0 warnings/errors**, EditMode **284/284**, PlayMode **89/89**, targeted animation **6/6**, graphics capture **1/1**, Android exit **0**, integrity **333 GUIDs / 555 scene objects/components**. APK SHA-256 **`C793A3F11F58AB82049892BB939D9CEC606893D3ABCBEC2974FAA0F8173A329A`**; `adb install -r` succeeded and installed `base.apk` matched. Samsung S23, 1080x2340. No data clear, purchase, proof-floor flag or profile restoration.

**Fresh state and progress:** the phone was awake/unlocked on its launcher. Initial saved profile: version 2, revision **110**, **7891 gold**, Sword/Second Wind, both unlockable weapons/relics owned, deepest floor 2. The app cold-launched into a new run, so the preceding handoff's in-memory choice was no longer available; the first startup capture is only a splash screen. Before installing, this old-build run was completed with first offered upgrades, Mend and Extract, banking **116**, producing revision **111 / 8007 gold**. This old-build completion preserves progress; it is not evidence for new directional art.

The preinstall, immediate postinstall and final profile SHA-256 all matched **`10160EAC26F8E4FFF4670FD5E3D2BA229662D2BFDA564DD291EE8C9E52CC66F7`**. Fresh backups/evidence are under ignored `TestResults/device-vanguard-directions-01/`. No stale backup was restored. Telemetry was retained and grew normally.

The unattended session was announced. Each tap, drag and screencap checked game focus, awake display and hidden keyguard; captures checked again before pulling. Four diagonal drags were captured while their 2.2–2.5-second inputs were still held, following first-card choices where necessary:

| Capture | Observation |
| --- | --- |
| `front-left-moving.png` | Front mirrored body, sword/buckler attached, passing feet; hero moves toward the near-left rim. |
| `rear-right-moving.png` | Rear original body during return drag; ground marker/camera relationship changes. |
| `front-right-moving.png` | Front original body during Cinder Walk combat with an approaching enemy. |
| `rear-left-moving.png` | Rear mirrored body, sword on screen left, contact shadow under feet. |
| `left-for-owner.png` | Left paused on Cinder Walk **2/6**, level-4 upgrade choice, **23 at-risk gold**, **55 HP**, XP **55/70**. Stored profile still revision **111 / 8007 gold / Sword / Second Wind**. |

Current-process logcat search found no `Exception`, `NullReference`, `FATAL EXCEPTION` or `Error:` lines. This is a short Sword movement/art smoke check, not a sustained performance benchmark, mid-range test, new full-run balance test, or proof of perfect frame-by-frame foot contact. Staff/Daggers and lethal-hit/front-flash/reload coverage in this slice are Unity tests. Source gait is improved but still not foot-locked; mirrored light/handedness and individual finger occlusion remain.

## Imported Vanguard: installed and phone-reviewed — 2026-09-17

**Gate:** .NET **272/272**, compile **0 warnings/errors**, EditMode **284/284**, PlayMode **87/87**, graphics-enabled runtime capture **1/1**, Android exit **0**, integrity **317 GUIDs / 555 scene objects/components**. Development APK SHA-256 **`971FEF6BCEBF5EB8EAE06DEA51ECF08780833485D8FE80E1142511D016A90ADE`**. `adb install -r` returned Success; the installed `base.apk` SHA-256 matched. Same Samsung S23 (SM-S911B), 1080x2340. No app-data clear, purchase or profile restoration.

**Preserving progress.** Fresh initial backup: version 2, revision **107**, **7659 gold**, Staff/**Second Wind**, both unlockable weapons/relics owned and deepest floor 2. The existing old-build run was paused on the Forge choice with 41 at-risk gold and 48 HP. It was continued with Mend, the first offered upgrades and Extract before installation, banking **116**; preinstall backup therefore records revision **108**, **7775 gold**, same equipment. This deliberately avoids discarding an already-open run or restoring the older revision-104 handoff.

The unattended device run was announced. Every tap/swipe/capture passed the focus, awake and hidden-keyguard check; captures were checked again before pulling. One capture failed as Unity's completed build stopped ADB; the daemon was restarted and a newly gated capture succeeded. The immediate post-launch capture was blank during startup and is not evidence of a rendered scene; subsequent live captures are.

| Evidence under ignored `TestResults/device-vanguard-01/` | Observation |
| --- | --- |
| `03-staff-moving.png`, `04-staff-after-drag.png` | Imported hero and procedural Staff visible in live combat; a five-second right drag reaches the rim with the camera following. The first capture is near drag start, not proof of a passing frame. |
| `06-staff-walk-left.png`, `07-staff-release.png` | Leftward drag shows a passing-leg pose with equipment attached; a later level-up choice interrupts further motion. Single rear-facing body remains obvious while backing away. |
| `08-staff-next.png` | Imported hero fighting the existing procedural enemies at the rim. Mixed art fidelity remains. |
| `11-staff-extracted.png` | New-build Staff/Second Wind run completes six rooms, level 7, XP 121, **116 banked gold**. Telemetry: 31 kills, 7 upgrades, 56.743 active seconds. This was a movement/art smoke run, not a balance baseline. |
| `12-forge.png`, `13-sword-restart.png`, `14-sword-walk.png` | Equip the already-owned free Sword, then Start run. Imported sword/buckler/body survive scene restart; walking and damage are visible. No purchase. |
| `15-sword-combat.png` | Left in Ember Hall 1/6 on the first upgrade choice: level 1, 11 XP, 5 at-risk gold, 86 HP, Sword/Second Wind. |

**Final saved state:** revision **110**, **7891 gold**, **Sword / Second Wind**, same unlocks and deepest floor. The increase from 7659 is the old run's 116 plus the new Staff run's 116; the third revision increment equips Sword. Both backups and final telemetry/profile were retained; no development start-floor flag was created. Inspect fresh state before the next phone test, since the owner can resume this run.

**Limits and logs.** Current-process `final-unity.log` contains **0** matches for the checked E/Unity, exception, missing/null-reference and fatal patterns. Logs are retained, but this is not a controlled performance benchmark: the first cold window includes a **2116.3 ms** maximum and the Sword warm restart window includes **50.1 ms / 1077.5 KB** maximum whole-frame allocation. No before/after attribution, cold-start acceptance, ten-enemy density remeasurement or mid-range claim is made. Physical checks covered Staff and Sword; Daggers, death/flash and imported-asset lifetime were covered by Editor tests/captures, not a separate new Daggers/death device run.

Sources, actual Unity renders, source hashes/prompts, import contract and incomplete visual work are in `ArtDirection/2026-09-17/vanguard-runtime-01/`. Other facings, planted-foot cleanup, finger occlusion and matching Staff/Daggers/enemy artwork remain. The imported hero is an integrated prototype, not finished character art.


## Stone/contact slice: installed and observed live; controlled device pass pending — 2026-09-16

Development APK **24,739,335 bytes**, SHA-256 **`8DD7866A84537B9B4D91644C685F894717460A86DC1E8261BC506E2F3BEF5E3D`**. Full gate after fixing the live-enemy clone fault: .NET **262**, EditMode **274**, PlayMode **80**, clean compile, Android exit 0 and integrity **292 GUIDs / 555 scene objects/components**. Installation used `adb install -r`; hashing the installed package matched the local APK. No app data was cleared.

A fresh backup of the app's files, including both profile generations and telemetry, preceded installation under ignored `TestResults/device-stone-contact-01/before/`. The phone already held more progress than the preceding handoff: profile revision **97**, **6594 gold**, Staff/Counterweight, both purchased weapons/relics and deepest floor 2. Nothing was restored from an older backup. The observed profile remained byte-identical afterwards (SHA-256 `4D6EA529458141A44283A1692C1E7BC7748BA29B0F08DB2F45751AC25502B6EE`).

After explicit `am start -W`, focus/unlocked/awake gates guarded every screenshot. `01-installed.png` caught startup too early to assess art. Despite its provisional filename, `02-normal-choice.png` shows **live combat**, with the new floor, rim, contact patches, body hit flash, damage numbers and a blue ability effect. `03-current.png` and `04-observed.png` show the level-4 choice in **Cinder Walk, floor 1 / room 2 of 6**, 33 at-risk run gold and 81 displayed HP. Telemetry records owner-driven upgrade selections, an ability and a chest; the agent sent **no taps or swipes** during this observed run.

Live touches were apparent before automatic input began, so the owner was asked who should control the phone. The active run was left at its choice instead of force-stopping it to enter the proof floor. **Agent-controlled rim drags and ten-enemy warm-restart checks for this APK remain pending that coordination.** No temporary density-floor flag was created; the development directory was confirmed empty. Do not claim the phone was returned to Ember Hall, that this run finished, or that this build received a ten-enemy device pass. Those checks remain covered only by the preceding device build and current automated scene/parity tests until repeated here.

`normal.log`/`observed.log` contain no matched `Exception`, `MissingReference`, `Error:` or `FATAL` messages. Recorded post-startup 5-second windows 2–7 report **59.9 fps**, maximum intervals **16.7–17.1 ms**, zero intervals over 20 ms; these mix combat and paused choices and are **not sustained-combat acceptance**. Observed 3–4-enemy wave markers: **1.42–6.28 ms** StartWave, **17.0–496.1 KB whole-frame allocation**. Cold window 1 includes a **2149 ms** startup interval and **196.5 ms** maximum main-thread work; do not merge it into the warm summary or attribute it to isolated floor drawing. The new shadow texture is 2 KB by construction, but no isolated mobile rendering-cost claim is made. Only the S23 was observed; short portrait review is from Unity renders.

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

## Shared enemy sprite cache on S23 — 2026-09-16

The owner authorised USB testing and the run was announced. Backed up `profile.json`, `profile.json.bak` and telemetry after force-stopping the app; no start-floor flag existed. The installed old APK hash matched `8D73EE69683BCDB1FA061F04AB15B6303C87B86477C806912D722CACA7A0653A`. The existing `floor_density_proof` flag enabled the same two-Grunt/eight-Mite room without a scene change. A fresh old-build restart was recorded before installing the cache APK with `adb install -r`.

**Verified build:** .NET **257/257**, compile **0 warnings/errors**, EditMode **269/269**, PlayMode **74/74**, Android exit **0**, static integrity **284 GUIDs / 555 scene objects/components**. APK **24,738,586 bytes**, SHA-256 **`4A2F37C48A7DC451776772479780F9D9C16D2D488DA33F54365A40A4B92AF4CD`**; install returned Success and the installed `base.apk` hash matches.

Every input/capture used the focus/awake/keyguard gate; captures were rechecked before being pulled. The first attempt stopped because NotificationShade had focus (awake, unlocked); collapsing it and launching the app restored focus. An ADB daemon restart during the Android build caused one old-build restart input to fail; the following capture still showed the result panel. That attempt is not counted as a restart. The successful retry produced the baseline below.

### Measured restart frames

| Build / sample | `StartWave` | Frame | Main thread | Whole-frame allocation |
|---|---:|---:|---:|---:|
| Old build, fresh sample | 8.10 ms | 66.7 ms | 67.2 ms | 18,284.1 KB |
| Cache, warm restart 1 | 3.05 ms | 50.0 ms | 54.8 ms | 17,620.8 KB |
| Cache, warm restart 2 | 2.68 ms | 66.7 ms | 68.2 ms | 17,742.4 KB |
| Cache, warm restart 3 | 2.39 ms | 50.0 ms | 56.9 ms | 17,741.6 KB |

These are live samples, not a controlled benchmark: baseline used Daggers/Counterweight; the owner switched to Staff during the new-build session and also used Forge Burst. The spawn marker shows a clear reduction consistent with removing repeated drawing, but a precise across-loadout performance percentage is not claimed. Three new-build warm restarts in one process confirm the cache survives scene reloads. The first launch window includes a 2148.3 ms application-start interval and a 182.2 ms startup frame; the probe starts after scene load and does not isolate cold enemy generation. Those samples are excluded from the warm comparison.

**Correction to the preceding attribution:** the 18 MB figure describes the entire frame, which includes rebuilding platform/backdrop/hero/effects and other scene work. It was not 18 MB of enemy drawing, nor a measurement of every ordinary wave. Enemy sharing removes approximately 542–663 KB in these whole-frame samples, while the restart spike remains 50–67 ms. Do not mark scene-restart optimisation complete. Arena/backdrop caching and more specific allocation markers are the next performance slice.

The new log contains many 59.9 fps windows, but these include result screens at `time scale 1`. Time scale alone does not prove combat is active. Staff and burst clear the proof quickly, and owner input was interleaved, so this pass does **not** replace the earlier sustained-combat benchmark with a new one. No mid-range phone was measured.

### Visual evidence and retained data

Local evidence: `TestResults/device-sprite-cache-01/` (ignored), including `baseline.log`, `cache.log`, `final-unity.log`, backups and pulled telemetry.

- `baseline-07-restart.png`: old-build crowded fight, separate enemy bars and damage numbers.
- `cache-01-cold.png`: new-build Grunts/Mites with mixed white hit silhouettes and independent health bars; appearance retained.
- `cache-04-warm-combat.png`: despite the filename, this capture is already the **result** screen; not combat evidence.
- `cache-05-warm-entry.png`: warm-restart combat with Staff/burst, hit silhouettes, effects and separate bar fills; shared sprites remain valid.
- `cache-06-proof-end.png`: proof cleared and 10 gold banked; current Staff/Counterweight loadout retained.
- `cache-07-normal-floor.png`: after deleting only `development/start-floor.txt`, Try again returns to **Ember Hall 1/6** with valid Grunt/Mite art. The empty development directory is harmless and contains no start flag.
- `final-unity.log`: no matches for the checked Unity exception/error/fatal patterns.

Before: version 2, revision **77**, **5525 gold**, Daggers/Counterweight. At normal-room return: version 2, revision **83**, **5575 gold**, Staff/Counterweight; both weapons/relics and deepest floor 2 retained. Five proof completions added 50 gold and one loadout selection accounts for the six revision increments. The owner interacted during the session, so the backup was **not restored over newer progress**. Telemetry was retained, including its normal rotation to two files, and pulled for evidence. No application data clear, purchase or profile reset occurred.

## Platform and backdrop reuse on S23 — 2026-09-16

The owner deferred the next Opus task and authorised this performance follow-up. The USB measurement was announced; every tap/capture passed the focus, awake and keyguard gate, and captures were checked again before pulling. The fresh profile/backup and telemetry were saved before installation, and no start-floor flag existed. The preceding APK was preserved locally with its `4A2F37C4...` hash. Installed the new APK with `adb install -r`; Success and exact installed `base.apk` hash were verified. The temporary proof flag selected the existing two-Grunt/eight-Mite floor, without editing scene data.

**Gate:** .NET **257/257**, compile **0 warnings/errors**, EditMode **269/269**, PlayMode **78/78**, Android exit **0**, static integrity **287 GUIDs / 555 scene objects/components**. APK **24,738,586 bytes**, SHA-256 **`6DE8F2A9A2B92B3CA15A50FA5EA71980ACB8E91C60CF832C7A126F1D8CC39ED6`**.

### Three warm restarts in the same application process

All used Staff/Counterweight. Scripted input was limited to Try again; no movement, ability, upgrade, equip or purchase input was injected. Telemetry records ten kills, zero upgrades applied, ten gold banked and 75.712 HP remaining for each warm proof completion. Each took about 4.525 seconds of active combat.

| Sample | Backdrop build | Platform build | StartWave | Probe frame interval | Main thread | Whole-frame allocation |
|---|---:|---:|---:|---:|---:|---:|
| Warm restart 1 | 1.02 ms | 0.04 ms | 1.81 ms | 16.7 ms | 32.0 ms | 1131.4 KB |
| Warm restart 2 | 0.97 ms | 0.03 ms | 1.54 ms | 16.7 ms | 24.8 ms | 1008.5 KB |
| Warm restart 3 | 1.06 ms | 0.04 ms | 1.59 ms | 16.7 ms | 28.0 ms | 1012.0 KB |

The preceding enemy-cache-only session measured 17,620.8–17,742.4 KB for three warm restart frames. The current range is approximately **94% lower**. This is a comparison with recorded earlier live samples, not a fresh controlled A/B benchmark; the earlier session included owner interaction. Bytes describe **all work in that frame**, not the allocation of an individual marker. The small stage times and resource-identity tests support that environment redrawing has been removed from repeated builds.

Probe intervals were 16.7 ms in these samples, but the main-thread recorder still measured 24.8–32.0 ms. Do not call restart work free or claim perfect presented frame pacing. Initial launch included a 2110.2 ms application-start interval and an 83.6 ms startup frame; the next window included a 50.1 ms frame with 325.9 KB allocated. Those are excluded from the warm comparison and were not isolated by stage markers, which start after initial scene loading. Cold generation remains a separate measurement problem.

Warm restart windows mix the short fight and result screen, which also runs at time scale 1. For example window 57 reports 59.9 fps, max 16.9 ms and no interval above 20 ms, but this is **not** a sustained dense-combat benchmark. Total recorded memory stayed around 124.4–124.7 MB during this short session; that does not establish a long-term leak test. No mid-range device was measured.

### Evidence and cleanup

Ignored evidence: `TestResults/device-arena-cache-01/`, including the old APK, backups, `arena-cache.log`, `final-unity.log`, final profile and telemetry. Editor diagnostic before the change: `TestResults/arena-art-baseline.xml`; final diagnostics and native lifetime checks: `TestResults/playmode.xml`.

- `01-cold-combat.png`: live proof fight with valid platform, backdrop and ten enemy actors.
- `02-state.png`, `03-warm-1.png`, `04-warm-2.png`, `05-warm-3.png`: completed proof runs; these are result captures, not active-combat evidence. Environment remained visible behind the overlay.
- `06-normal-floor.png`: after deleting only `development/start-floor.txt` and tapping Try again, **Ember Hall 1/6** with platform, props, chest, hero and enemies intact. The pre-existing empty development directory remains.
- `final-unity.log`: no matches for the checked exception, missing-reference, error or fatal patterns.

Fresh backup: profile version 2, revision **89**, **6211 gold**, Staff/Counterweight. At normal-floor return: version 2, revision **93**, **6251 gold**, same equipment, both purchased weapons/relics and deepest floor 2 retained. Four proof completions explain the 40 gold and four revisions. No backup was restored over that progress; no data clear, purchase or profile reset occurred. Telemetry remained in its two files and was pulled; it confirms the fifth run starts on `floor_ember_halls`.

The bounded environment cache is complete. The reference furnace-cavern assets, room-aware framing, separate boss geometry, hero blocking and kiting balance remain future work. Seeded offers and Forge Pulse were not started.

## First furnace layer and bounded follow on S23 — 2026-09-16

**Gate:** .NET **260/260**, compile **0 warnings/errors**, EditMode **272/272**, PlayMode **79/79**, Android exit **0**, integrity **289 GUIDs / 555 scene objects/components**. APK **24,739,335 bytes**, SHA-256 **`3704897005DA235B5C23DD70761DD2D784387489D5A896091F9241735CCB6F6F`**. Installed with `adb install -r`; Success and the installed `base.apk` hash were verified. Phone: the same S23, 1080x2340. The session was announced, and all taps, drags and captures passed the focus/awake/keyguard gate, including the recheck before retaining each capture. The ADB daemon restarted after Unity's build; subsequent commands succeeded.

Backed up the current profile, its backup and both telemetry files before installation; there was no development start-floor flag. The existing APK was preserved locally. Started the normal authored floor with the owner's Staff/Counterweight. Three Tempered Edge selections let the short movement review continue through its choice interruptions. No equipment change, purchase, ability tap or profile edit was made.

### Live observations

- `01-launch.png` is the early startup capture; it is not evidence of the completed scene.
- `02-current.png`: the new cavern behind the existing floor and choice panel. No procedural star field or floating background islands remained.
- `03-far-drag.png`: a 4.5-second upward drag reached the far tip. The camera retained much more escape floor below the hero, and the hero/threats stayed clear of the top HUD. The hero is white in this frame because of its existing damage flash; damage digits and enemy bars are live.
- `04-state.png`, `05-diagonal.png`: upgrade overlays. The first diagonal gesture was interrupted by a choice, so its full requested duration was not active movement.
- `06-live-rim.png`: choice dismissed near the upper/right rim, with a readable hero, live loot/damage effects and the cavern behind the platform.
- `07-side-drag.png`: a second diagonal gesture reached the right corner. The hero remained visible with nearby enemy bodies/bars, and the background covered the view without an exposed texture boundary. Far entry positions can still be offscreen; this is intentional at width 9.

This normal-floor review was force-stopped before its run ended to switch to the small development proof floor. It is not a completed Descent or a claimed graceful interruption callback. The in-run upgrades/rewards from that test were temporary and not banked. Existing permanent progress was preserved.

Pushed `floor_density_proof` to the documented temporary start flag, launched, completed the proof, then used Try again once without exiting. `08-proof-result.png` and `10-warm-result.png` show two completions at 76/100 displayed HP, each banking 10 gold. `09-warm-combat.png` shows the reloaded cavern/platform and the entering pack with independent bars. These are actual phone images, unlike the frozen review renders in ArtDirection.

| Warm proof restart | Measured |
|---|---:|
| Backdrop build | 0.18 ms |
| Platform build | 0.06 ms |
| StartWave (10 enemies) | 1.92 ms |
| Probe frame interval | 16.7 ms |
| Main-thread time | 29.7 ms |
| Whole-frame allocation | 1005.5 KB |

This single live smoke sample retains the preceding cache slice's roughly 1 MB restart allocation; it is not a controlled performance comparison. Ordinary authored-wave samples in the movement log measured 2.30–9.42 ms StartWave and 21.0–495.6 KB whole-frame allocation, including first-use enemy looks. The movement session contains many paused choice windows and a 33.4 ms paused interval. The proof's cold launch includes a 2144.9 ms application-start interval and a 107.1 ms startup frame. Do not describe all these windows as combat or claim all hitches are eliminated. No sustained dense-combat, GPU-specific or mid-range-device acceptance is claimed.

### Cleanup and evidence

Deleted only `development/start-floor.txt`; the pre-existing empty directory remains. Try again returned to **Ember Hall 1/6**, captured in `11-normal-floor.png`; final telemetry confirms `floor_ember_halls`. Profile before: version 2, revision **93**, **6251 gold**. At return: version 2, revision **95**, **6271 gold**, Staff/Counterweight, both purchased weapons/relics and deepest floor 2 retained. The two proof rewards explain the 20 gold and two revisions. No backup restoration, app-data clear, reset or purchase occurred.

Ignored evidence: `TestResults/device-furnace-01/`, including backup, preceding APK, captures, `movement.log`, `proof.log`, `final-unity.log`, final profile and telemetry. Movement/final logs contained no matches for the checked Unity exception, missing-reference, error or fatal patterns. Offscreen test/capture fixture copies remain in `TestResults/FurnaceSetupTemporary.cs` and `FurnaceCaptureTemporary.cs`; they were removed from Assets before the full gate.

The furnace plate and bounded camera are the completed art slice. Stone/rim/prop material work, contact shadows, production actors and a distinct boss arena remain open. Short portrait framing was rendered/tested at 1080x1920, but only the S23 was physically reviewed. Seeded offers and Forge Pulse remain deferred.

## Two-sided enemy spacing on S23 — 2026-09-16

The owner handed over USB for the check. Development APK 24,739,335 bytes, SHA-256 `24F1FD800362ACBDFB518C8F86D78367E3068FDF11271BF1C071F1A3476CABDA`; `adb install -r` returned Success and the installed `base.apk` hash matches. Every tap, drag and capture was preceded by the focus/awake/keyguard check and every capture was checked again before it was kept.

**Before.** The owner's run was open on a level-up choice in Cinder Walk 2/6 (33 at-risk gold, 67 HP; `00-before-install.png`); the install ended it, with the owner's go-ahead. The stored profile - version 2, revision **104**, **7361** gold, Staff and Counterweight equipped, deepest floor 2 - and both telemetry files were pulled first. An empty `development` folder already existed from an earlier session.

**Runs on the proof floor** (`development/start-floor.txt` holding `floor_density_proof`):

| Run | Control | What was seen |
|---|---|---|
| Staff + Counterweight, hero standing | no input sent from this session; the hero stood at the centre in every capture | `01-standing-1..4`: the ten enemies enter from all four corners, ring the hero and keep apart; the Staff's area hits clear them in a few seconds. Victory at 86 HP, 10 gold. |
| Daggers, meant to stand | **not controlled** | The owner was also using the phone - telemetry shows an upgrade taken and, afterwards, the loadout set back to Staff and Counterweight and further Staff runs started by hand. `06-daggers-standing-1..6` are kept as observation only. |
| Daggers + Second Wind, turning round | scripted after the owner stopped touching the phone: `input motionevent` full steer left and right, switching about every 1.5 s, a card taken at the level-up, then the same again | `10-daggers-turning-1..4` and `11-daggers-turning-1..4`: the hero crosses the centre marker both ways and the pack turns with it in a ring, enemies apart. Telemetry: victory, 15.45 s of fighting, 10 kills, 10 gold, 78.08 HP left. |

What overlaps on screen is depth, not spacing: the floor's depth is drawn at half length, so two enemies 0.9 apart along the depth axis draw their sprites partly over one another, and the hero has no body, so an enemy at its reach behind the hero is drawn over it. No two enemies were seen stacked on the same spot, stuck in place with room beside them, or shaking back and forth. Captures are about 1.5 s apart, so they cannot show a single-frame flicker; the per-frame guarantees are proven in PlayMode, not here.

**Frame cost, `[Perf]`.** In the controlled Daggers run every 5 s window after the entry frame - through the fight, the choices and the result screen - held **59.9 fps**, p50 and p95 16.7 ms, p99 at most 17.1 ms, longest frame 17.3 ms, main thread at most 22.2 ms, with 3–180 KB allocated per window. The one long frame was the run's first: 83.5 ms (main thread 82.8 ms) when Start run in the Relic Forge reloads the scene and the ten enemies enter (`StartWave` 2.59 ms, 1127 KB). Across the uncontrolled runs the windows held 59.7–59.9 fps with at most one 33.4 ms frame in a window, and a wave entered in 2.72 ms (1127 KB). So walking ten enemies under the two-sided rule costs nothing measurable on this device; the entry frame after a Forge start is worth a look on its own and is not caused by movement.

**After.** The start-floor file was deleted (the pre-existing empty folder remains), the app force-stopped, and `profile.json`, `profile.json.bak`, `telemetry/events.jsonl` and `events.1.jsonl` restored from the backup; all four SHA-256 hashes match it on the device, so the proof runs' banked gold and events are gone and revision 104 / 7361 gold / Staff / Counterweight stand as before. On launch the game started on **Ember Hall, Room 1/6** with the Staff (`12-return-hud.png`) and was left running for the owner.

Limits: one controlled walking run, one weapon walking, on a flagship; outcomes on IL2CPP were not compared with the Editor tables in `22` and may differ in a crowd. Local evidence: `TestResults/device-spacing-01/` (captures, contact sheets, the three `logcat-*.txt` files and the backup).

## Integrated foundry chest — 2026-09-18

Three shared imported 192-square ASTC4x4 sprites (closed/opening/spent), a ChestArtSet and one Gameplay scene reference replace the procedural prop when valid art is present. Common full-source registration, bilinear filtering, .75-unit reference box width; generated originals/prompts unchanged. Contact reward remains immediate/exactly once; the cosmetic .18-second opening respects pause. Reload borrows existing sprite/texture assets. GroundedSorting keeps each actor body/equipment/flash together at the floor root, while bars, shadows and ability ring retain independent global groups. This also changes actor/actor overlap presentation; no physical movement/spacing/balance rules changed.

Files: ChestSpawner, ChestArtSet, GroundedSorting, HeroLookView, EnemyLookView, ContactShadowView, AbilityRingView, imported Art/Props/FoundryChest, one scene reference, ChestPresentationTests/ArenaWalkTests and Tools/ArtReview chest import/capture scripts. See `ArtDirection/2026-09-18/chest-runtime-01/README.md` for reproduction and limits. Remaining: small generated frame drift, smooth lid animation, crowd occlusion and physical-device review; ability-ring artwork itself is unchanged.

Verified: .NET 272/272; compile 0 warnings/errors; EditMode 284/284; PlayMode 106/106; graphics capture 1/1; integrity 424 GUIDs / 555 scene objects/components; Android build exit 0. APK 24,751,592 bytes, SHA-256 `BA1F79545C8B3D70779A0380B2BD22A26BA0A8B88A3C09187B019DDCD852D814`. Two presentation tests cover immediate single reward, pause/resume, import format, common registration, independent ground/overlay groups and reload ownership. The first targeted test exposed an unsaved null art reference; importer now reloads the saved asset in the scene context and explicitly marks the scene dirty. A graphics check exposed the range ring inheriting the hero group; it now sorts independently.

Device status: NOT installed or phone-tested. An asynchronous device-availability question remains unanswered; no ADB input/capture/install or profile/telemetry mutation occurred in this slice. The previously installed platform APK is still the latest verified phone record. Do not claim this build's depth, performance or opening was verified on S23.

## Kite Test arms on the S23 — 2026-09-18

The owner handed over USB and asked for the two development arms of `26` Part 4 to be installed and measured.
Development APK **24,752,344 bytes**, SHA-256 `48504C0A81369E4F4B575D98438B80505A6310C7DC37EFFD0D5145A5B4D26E60`;
`adb install -r` returned Success and the installed `base.apk` hash matches. Focus, an awake display and no keyguard
were checked before and after every capture and before every tap.

**Before.** Nothing was open: the launcher had focus, so no run was interrupted. `profile.json` (revision **144**,
**10985** gold, Second Wind and Counterweight owned, Second Wind equipped, no weapon equipped so the hero carries the
Sword), `profile.json.bak` and all three telemetry files were pulled first.

**Four runs**, arm chosen by writing the floor id into `development/start-floor.txt` and tapping Try again, which
reloads the scene and re-reads the file. The hero was never steered by hand: a standing run received no touch at all
except on a choice panel, and a walking run held one drag from (540, 1600) that flipped 220 px left and right every
1.5 s. The first card was taken at every level-up and Mend at the forge. **The Forge Burst was never fired**, so these
are ability-free runs, unlike the simulation's.

| Run | Arm | Hero | Fight | Health after each room | Left | Damage taken |
|---|---|---|---|---|---|---|
| 1 | A, today | standing | 49.8 s | 72.4 / 35.4 / 20.1 / 60.1 / 32.5 | 32.5 | **132.5** |
| 2 | A, today | walking | 45.5 s | 98.0 / 65.4 / 73.3 / 100.0 / 77.9 | 77.9 | **98.8** |
| 3 | B, graded 2.3 | standing | 46.2 s | 75.9 / 40.6 / 29.0 / 69.0 / 47.5 | 47.5 | **117.5** |
| 4 | B, graded 2.3 | walking | 43.2 s | 76.3 / 48.8 / 47.5 / 87.5 / 66.0 | 66.0 | **74.0** |

Damage taken is health lost, so the heals are added back: Mend in every run, Second Wind once in both standing runs
(the result screen reads `Second Wind (1x)`; it did not fire in either walking run), and in run 2 two chests the walking
hero happened to touch, each healing 25. All four runs ended in victory with 18 kills, level 6, 99 experience and 128
gold, exactly as the arms were authored, and both arms opened the same six choices.

**What the device agrees with.** The arms behave as built. Standing difficulty is close between them, which is what the
×0.78 compensation is for: 132.5 against 117.5, arm B 11 % easier rather than the intended nil. Every fight is shorter
on arm B, in both policies: 49.8 → 46.2 s standing and 45.5 → 43.2 s walking.

**What it does not show.** The predicted collapse of the kiting advantage did not appear. Walking saved 25 % of the
damage on arm A and 37 % on arm B, so on these runs movement paid *more* on the candidate, not less, against a
simulation that expected 49 % to fall to 29 %. Three reasons not to read that as a refutation, and not to read it as
support either:

- The simulation's kiting route keeps the greatest distance it can with perfect information. A drag that flips left and
  right every 1.5 s is not that; it walks into the pack as often as away from it, and it is a much weaker kite on the
  slow arm A than on the fast arm B, where enemies close in whatever the hero does.
- Neither device run used the Forge Burst, which the simulation fires whenever it is ready. Every device fight is
  therefore longer and takes more hits than its predicted counterpart.
- Run 2 is off-protocol: one tap landed on the second card, so that run carries Tempered Edge ×4 and Quickened Grip ×1
  where the other three carry Tempered Edge ×5. Its damage and its 45.5 s are not strictly comparable. The driver now
  waits 500 ms after releasing the drag before touching a card.

**After.** The session ended early: the owner opened another app, the focus check refused the next tap and no input was
sent after that. The app was force-stopped, `development/start-floor.txt` deleted and `profile.json`, `profile.json.bak`
and all three telemetry files restored from the backup, each with a matching SHA-256 on the device, so the four runs'
512 banked gold and their events are gone and revision 144 / 10985 gold stands as before. The next launch starts on
Ember Halls.

Local evidence: the session's telemetry and the end-of-run captures in the session scratchpad; nothing was copied into
the repository. The owner then asked for the remaining runs, which follow and supersede the damage figures above.

### Second pass, relic-free — the same evening

Two things had to be fixed first. **The runs are deterministic**: repeating a cell reproduced it to the decimal, 49.8 s
and 32.5 health twice for arm A standing, 43.8 and 43.9 s at 87.1 health for arm B kiting. So a single run per cell is
the measurement, not a sample, and repeats buy nothing. **Second Wind was the real confound**: it fires once at a
quarter health and heals a quarter, which is 25 damage of uncertainty in cells that never say whether it fired. The
relic was unequipped by pushing an edited save while the app was stopped, into *both* `profile.json` and
`profile.json.bak`, because the store loads the newest readable revision of the two and the first attempt, which wrote
only the main file, was overridden by a newer backup.

Eight runs then, each a cold launch, no relic, no Forge Burst, all five upgrades Tempered Edge, Mend at the forge.
Damage taken is health lost with the Mend and any chest heals added back.

| Hero | Arm A, today | | Arm B, graded 2.3 | |
|---|---|---|---|---|
| | damage | fight | damage | fight |
| standing | **died in The Post** | 37.9 s | 117.5 | 46.2 s |
| sweeping left and right, 1.5 s | 118.1 | 49.4 s | 105.5 | 40.3 s |
| sweeping left and right, 3 s | 109.8 | 58.4 s | 107.2 | 45.4 s |
| orbiting the arena, 6 s a lap | 98.3 | 59.5 s | 94.3 | 43.2 s |

**Arm B is easier to stand in, not equal.** Without the relic a standing hero dies on arm A in the third room and
survives arm B with 22.5 health. The ×0.78 compensation, fitted on the authored Descent with the burst, overshoots on
this floor without it. That is a finding about the compensation, not about the speeds.

**Movement pays about the same on both arms.** The best scripted route takes 98.3 on arm A against a standing death,
and 94.3 on arm B against 117.5 standing, roughly a fifth saved on each. The simulation's claim that the candidate
makes running away less worthwhile is **not reproduced**, and the reason is visible in the table: none of these routes
is a kite. They steer on a timer, not away from the nearest enemy, so on arm A they walk back through a pack the hero
could simply have outrun. A player kiting arm A would do better than 98.3; how much better is exactly what is unknown.

**The pacing claim is reproduced, clearly.** Whenever the hero moves, arm A's fight runs 9 to 16 seconds longer than
arm B's: 49.4 against 40.3, 58.4 against 45.4, 59.5 against 43.2. Slow enemies make a moving hero wait for the fight;
the graded set ends it. This is the same effect the simulation reported as the Daggers' 104.6 s kiting run falling to
32.1 s, and it is the strongest evidence the candidate produces the intended change.

**After.** `development/start-floor.txt` deleted, the app force-stopped, and `profile.json`, `profile.json.bak` and all
three telemetry files restored from the backup with matching SHA-256 hashes on the device: revision 144, 10985 gold,
Second Wind equipped again, and every test run's gold and events gone. The next launch starts on Ember Halls.

### The owner plays both arms — the same evening

The arms were then set up for the owner to play, one at a time, without being told which was which; the app was closed
each time and no input was sent from this session. Their own loadout stood: the Sword, Second Wind equipped, and their
own choice of upgrade cards.

| | Arm A, today | Arm B, graded 2.3 |
|---|---|---|
| Result | victory | victory |
| Fight | 45.7 s | 42.8 s |
| Health after each room | 78.5 / 41.5 / 43.3 / 83.3 / 61.2 | 84.5 / 56.5 / 64.3 / 100.0 / 95.7 |
| Damage taken | 128.8 | 115.0 |
| Chests reached | 2 | 3 |
| Upgrade cards | damage ×3, speed ×2 | damage ×4, speed ×1 |
| Time spent on choices | 44.2 s | 5.2 s |

Arm B was 11 % cheaper in damage and 2.9 s shorter, and the hero was never in danger on it: its lowest health at a room
boundary was 56.5 against arm A's 41.5. Second Wind cannot have fired in either run, since neither dropped to a quarter
health at any room boundary.

**Two things make the 11 % soft.** The second run took one more damage card than the first, which kills faster and so
takes fewer hits, and it reached one more chest. Both favour arm B. A repeat with the same five cards in both arms
would settle it; the scripted runs above, which did hold the cards identical, put the same gap at 11 % as well, so the
direction is at least consistent.

**The owner's impression, in their own words:** on the second arm they felt they ran away *more*, and on both arms they
could kill by kiting and striking. That is the answer the numbers could not give, and it points the opposite way from
the candidate's purpose. See the verdict in `26`.

## The movement budget on the S23 — 2026-09-19

The owner asked for the build carrying the movement budget and its HUD bar to be installed so they could play it.
Development APK **24,752,344 bytes**, SHA-256 `D77FFFA6D385BBB017F9EBCBA8C9D6A7271250F5318E4021A8FD9F7BFA6DEED2`;
`adb install -r` returned Success at 22:01:34 UTC and the installed `base.apk` hash matches. Focus, an awake display and
no keyguard were checked before the one capture taken, and **no input was sent from this session at all**: the app was
force-stopped after installing and handed over closed.

**Before.** A run was open on the old build - Ember Halls, Cinder Walk, room 2 of 6, a level-up panel showing, 53 health,
23 gold not yet banked - and the install ended it, which the owner had asked for. `profile.json`, `profile.json.bak` and
all three telemetry files were pulled first; the profile itself was untouched and still reads revision **144**, **10985**
gold, Second Wind equipped, no weapon equipped so the hero carries the Sword. No `development/start-floor.txt` exists,
so the game starts on the authored Descent rather than on either Kite Test arm, which remain in the build unreachable.

**The run.** The owner played Ember Halls with the Staff, clearing the first three rooms and the forge with health
83.4 / 70.7 / 86.4 / 100.0 and fights of 17.5, 14.9 and 9.8 seconds. Telemetry does not record the bar, so nothing here
measures the budget itself; the runs before 22:01 UTC are on the previous build and are not evidence about it.

**What the owner reported.** The bar is good, and they notice clearly when it empties.

That answers the question this slice was least sure of. The empty state had to be readable from the track alone, because
an empty bar has no fill left to look at, and a dull ember track is a quiet signal; it turns out to be enough. It also
means the mechanic is not invisible, which is what made shipping it without a bar a bad idea.

**Then they played it properly.** A whole two-floor Descent with the Staff: twelve rooms, sixty kills, 92.7 s of
fighting, 313 gold, finishing at 73.1 health and never once below 70.2. Their words, on the design question this slice
exists for: they can comfortably run away, kite and rest; the waves hold few enemies anyway; it may get hard once the
difficulty goes up; for now it is playable.

**That is the measurement, felt.** `26` Part 5's own tables say the budget reverses the answer on the ten-enemy proof
floor - standing becomes as cheap as running, and the best route becomes stop-and-go - while on the authored Descent it
only softens it, because the authored waves hold one to five enemies and a hero with room to run can spend its bar,
stand for a second and spend it again. The owner reached that conclusion from play in two runs, and the health line of
theirs agrees: a whole Descent without dropping below 70.

So the budget did what it was built to do and no more. It changed the **rhythm** of a fight from running throughout to
running in bursts, which is what the simulation predicted and what the owner describes as resting. It did not make the
authored Descent harder, and it was never going to: the same tables show the best play costing about what it cost
before (Sword 55.0 against 59.3 damage on the Descent). Pressure has to come from the fight, not from the budget.

**What this points at, untested.** The budget bites in proportion to how crowded the fight is. The project already has
the ten-enemy proof floor where it bites hard and `PackLayout.MaxPackSize` of 10 against authored waves of one to five.
Raising authored density has been out of scope in every slice so far and stays a decision for the owner, not a plan this
record makes.

## 2026-09-19 — Movement HUD polish, desktop verification

The labelled movement-row and depletion-pulse candidate was rendered in Unity at two portrait sizes. These images are Editor Camera.Render captures, not phone screenshots. No ADB input, capture, install, profile write or telemetry restoration occurred in this slice: the phone/Unity availability question had no answer. The installed D77FFFA6D385BBB017F9EBCBA8C9D6A7271250F5318E4021A8FD9F7BFA6DEED2 build and owner-play evidence above remain the latest device record. Read fresh device state before future review. New desktop build identity and validation are recorded in ArtDirection/2026-09-19/movement-hud-01/README.md; do not equate a built APK with an installed one.

Verified: .NET 291/291; compile 0 warnings/errors; EditMode 306/306; PlayMode 108/108; isolated graphics capture 1/1; integrity 439 asset/folder GUIDs and 568 scene objects/components. Android build exited 0. Actual APK file: 29,151,985 bytes; SHA-256 `97A5F7511F0968E43AAF785BAAC2F0B8D654F91905CE4C858DECBACA3175A7AA`. Not installed or phone-tested in this slice. Full tests cover existing gameplay and the added depletion/pause regression; they do not establish physical-device readability or performance.

## 2026-09-19 — Ability-ring visual proof; phone still pending

ADB enumeration detected the attached S23 (RFCW20W2WFX). No input, screencap, install, app launch or profile/telemetry operation followed; the exclusive-use question is unanswered. New ring comparisons are isolated Unity captures, not device evidence. No new APK was built, and the previously built movement-HUD APK remains uninstalled by Astra. The installed D77FFFA build record above remains latest. See ArtDirection/2026-09-19/ability-ring-01/README.md for scope and next integration checks.

## Movement HUD installed and reviewed on S23 — 2026-09-19

Owner explicitly confirmed the phone was free and authorised continuation. Installed the already-verified movement-HUD APK with adb install -r; installed base.apk SHA-256 matches `97A5F7511F0968E43AAF785BAAC2F0B8D654F91905CE4C858DECBACA3175A7AA` (29,151,985 bytes). No new code or build in this device-only slice; prior gate remains .NET291 / EditMode306 / PlayMode108, clean compile, graphics1, integrity439/568. The new ability-ring proof is NOT integrated in this APK.

Before: Cinder Walk 2/6, level-3 upgrade choice, 74 HP, 13 unbanked run gold. The owner-authorised reinstall ended that active run, as announced. Fresh profile and telemetry were copied locally before installation. Profile revision150, banked gold11596, Daggers/Second Wind, both weapon/relic unlocks and deepest floor2 were preserved. Both primary and backup profile hashes match before install, immediately after install and after the test. Primary SHA256 `6CD3D4D338536FE77993A3D52E88E55C6577F7FD3C6FC32FFDE81AAE45BC8DE3`; backup `8617F6460C901005EFF3CEB0D6CFDFADD072C36D1068A3187002878287C39B41`. No restore, clear, purchase, equipment change or development-floor flag; telemetry naturally grew and was not rolled back.

Observed in ordinary authored combat: separated health/MOVE/XP layout; movement fill falling during a drag, partial fill returning after release, quiet full state, one brief ember depletion pulse fading into the spent track, empty state held while a level-up choice pauses play, refill after selection. First drag reached the rim before emptying; the longer reverse drag produced depletion. Fifteen-fps extracted frames show the pulse, not a duration/performance benchmark. Focus, awake and unlocked checks preceded each input/capture; captures were rechecked before keeping. Short filtered logcat check found no FATAL EXCEPTION, NullReferenceException, MissingReferenceException or HeroStaminaView binding error. This is not exhaustive log or frame-time validation. Pulse freeze mid-animation remains covered by the Unity regression, not demonstrated by the phone clip.

Final observed state: Cinder Walk 2/6, level-4 upgrade choice open, 56 HP, 23 at-risk run gold, XP55/70, full movement. Phone handed back to the owner; inspect fresh before further use. Local raw evidence/backups: TestResults/movement-hud-device-2026-09-19/. Committed selected screenshots and a labelled-by-documentation temporal HUD strip: ArtDirection/2026-09-19/movement-hud-01/device/. Full screenshots are unmodified; depletion-strip.png is an ffmpeg crop/scale/contact sheet, 18 sequential frames from 2.8s at 15fps, read left-to-right/top-to-bottom. Source videos remain local; no profile or telemetry data is committed.

This supersedes earlier pending-phone and D77 latest-installed statements. Next art task is the shared-lifetime integration of the preferred ability ring, not another movement-budget change.

## Integrated ability ring on S23 — 2026-09-19

Following the owner's phone handover and continuation, installed the verified eight-arc ring APK with adb install -r. APK29,152,854 bytes; installed/base hash matched `300759ECC845E8CB12FA9988E6BC6FF481E9C96ABF703CB63DBE1E3A30D76ED6`. Gate: .NET291, clean compile, EditMode306, PlayMode110, graphics1, integrity440/568, Android0.

Fresh before-install screenshot: Cinder Walk2/6, level4 choice,56HP,23 at-risk run gold. Announced reinstall ended that active run. Fresh profile/backup/telemetry copied to local TestResults/ability-ring-device-2026-09-19/before. Primary/backup profile SHA256 remained identical before/after installation and after play: `6CD3D4D338536FE77993A3D52E88E55C6577F7FD3C6FC32FFDE81AAE45BC8DE3` and `8617F6460C901005EFF3CEB0D6CFDFADD072C36D1068A3187002878287C39B41`. Revision150, banked11596gold, Daggers/Second Wind, both weapon/relic unlocks and deepest2 unchanged. No save restore, equipment change, purchase or development flag. Telemetry was not rolled back.

Normal Ember Hall: selected Tempered Edge, dragged toward the right rim and observed the bright new contour centred under the hero. Fired the ability; next capture shows the dim contour and cooldown8. No apparent floor/body sorting error in these samples. Focus, awake and unlocked checks preceded each input/capture and followed captures before keeping. Filtered last1500 logcat lines contained no FATAL EXCEPTION, NullReferenceException, MissingReferenceException or AbilityRingView binding error; not an exhaustive log/performance check. Session reuse and paused breathing are covered by Unity tests, not a separate phone restart/performance experiment. The cast burst itself is still the old pixel effect.

Final observed state: Ember Hall1/6, level2 choice open,71HP,10 at-risk run gold,XP25/36, full movement, ability countdown4 held by the choice. Phone handed back; inspect fresh before future use. Selected unmodified screenshots are in ArtDirection/2026-09-19/ability-ring-runtime-01/device; raw captures and private backups remain local under TestResults. This supersedes earlier proof-only ring and97A5 latest-installed statements. Next art: match the cast/impact effect to the new contour without delaying damage.

## Ability cast pulse installed on S23 — 2026-09-19

Owner's device permission/continuation remained in effect; announced install and short recording. Installed verified APK29,153,822 bytes, SHA256 `66CE2B7B2CB39E273C2DDC35B5795EEA70E518B65FF40BD50F0A00CD87D94165`; base.apk matched. Gate291/306/111, clean compile, graphics1, integrity440/568, Android0. New pulse uses the shared contour and existing effect pool; damage/cooldown/total0.21s duration unchanged.

Before: Ember Hall1/6, level2 choice,71HP,10 at-risk gold; announced reinstall ended that active run. Fresh primary/backup profile and telemetry copied under TestResults/ability-pulse-device-2026-09-19/before. Profile hashes match before, after install and after play: primary `6CD3D4D338536FE77993A3D52E88E55C6577F7FD3C6FC32FFDE81AAE45BC8DE3`, backup `8617F6460C901005EFF3CEB0D6CFDFADD072C36D1068A3187002878287C39B41`. Revision150/gold11596/Daggers/Second Wind, unlocks/deepest2 unchanged. No restore, purchase, equipment change or development flag; telemetry not rolled back.

Selected Tempered Edge and fired ability in ordinary Ember Hall. A six-second screenrecord, reviewed as a20fps contact sheet, shows the small bright contour expanding to the outer indicator and disappearing, leaving the dim range marker. No thick pixel burst remains on ability use. It is a visual check, not a timing/performance benchmark; pause/origin/expiry are also covered by Unity regression. Focus/unlocked/awake checks preceded inputs/capture/recording and followed capture/recording before retention. Filtered last1500 log lines contained no FATAL EXCEPTION, NullReferenceException or MissingReferenceException; not exhaustive.

Final observed: Ember Hall1/6 level2 choice,71HP,10run gold,XP25/36, full movement. Phone handed back; inspect fresh before further use. Raw recording/screens and private backups remain in TestResults; committed device/pulse.mp4 is a540-pixel-wide H264 transcode of the unaltered-speed screenrecord, with selected original PNGs and hashes under ArtDirection/2026-09-19/ability-pulse-01/device. No private saves/telemetry committed. The previous installed300759 build is now superseded. Staff splash still uses its old ring; that separate presentation, coins and remaining equipment art are future work.

## 2026-09-19 — Staff splash proof; no device operation

Tools/ArtReview/Run-StaffSplashProof.ps1 rendered an isolated Staff effect study in Unity only (ArtDirection/2026-09-19/staff-splash-01). No ADB input, screenshot, installation, loadout change or save/telemetry access occurred. Latest installed APK remains ability-pulse66CE2B7B2CB39E273C2DDC35B5795EEA70E518B65FF40BD50F0A00CD87D94165; previous device state is historical, not inspected this slice. Staff remains pixel-art in the installed app. Runtime integration/full gate and physical Staff review are next.

## Staff impact installed on S23 — 2026-09-19

## Verification and S23 installation

Verified: .NET291/291; compile0 warnings/errors; EditMode306/306; PlayMode113/113; graphics capture1/1; integrity442 asset/folder GUIDs /568 scene objects/components. Android exit0; APK29,153,541 bytes, SHA256 `7A107BD2F871D2EC8AE660BFC7ABE565D25095EF47BB8E15B6F95CB49AD42279`. Installed base.apk hash matched. Six actual Unity captures cover real Staff attack start/expansion/fade/expiry and two portrait ratios. No full damage-area claim: candidate retains impact-only shown radius.

The first streamed ADB install returned a blank failure; the retry using `install --no-streaming -r` succeeded. No uninstall/data clear. Initial phone was awake/unlocked at launcher; focus guard correctly refused a screenshot there. Fresh complete local files backup was taken before install (TestResults/staff-runtime-device-2026-09-19/before). Primary/backup hashes were identical before and after install: `6CD3D4D338536FE77993A3D52E88E55C6577F7FD3C6FC32FFDE81AAE45BC8DE3` / `8617F6460C901005EFF3CEB0D6CFDFADD072C36D1068A3187002878287C39B41`; revision150/gold11596/Daggers/Second Wind. No old backup restored.

Temporarily selected the existing floor_density_proof using development/start-floor.txt (previously absent). Completed one Daggers run, banked10, opened Relic Forge and equipped the already-owned Staff through UI. An8-second live screenrecord shows successive four-arc purple impacts expanding/fading under the actors, distinct from the blue range ring; reviewed2–5s at20fps as a contact sheet. No active ability was used during the Staff recording/run. Completed Staff proof, banked another10, restored Daggers through UI. Development flag removed and empty directory verified. Normal Ember Hall1/6 then launched with Daggers/Second Wind; last captured at100HP,level0,XP0/10,0run gold (live state can advance).

Final profile revision154/gold11616; same weapon/relic unlocks, Second Wind and deepest floor2. Primary SHA256 `F1C37216FCEA2EF18C36B72317502CC976FF91D862EBED5DFB00A0538C199BBF`; backup `18AA83037FC0DA94BF0DE2CF41021327AAA9D4BFFC38E1745B5FED2A25B52867`. Expected changes are two10-gold wins and two equipment saves. No purchases, save edits or rollback; telemetry naturally grew. Every tap/capture/recording was preceded by focused/awake/unlocked checks, with post-capture/recording checks. Last1500 log lines filtered for FATAL EXCEPTION/NullReferenceException/MissingReferenceException had no matches; not exhaustive. This is a short visual test, not a sustained performance benchmark or mid-range-device result.

Committed device/staff.mp4 is a540px-wide H264 transcode at unchanged speed; device/normal.png is the original screencap. Raw video/contact sheet and private backups remain in ignored TestResults. Phone handed back to owner after normal return; inspect fresh before more input. Staff equipment sprite, Daggers art, coins and remaining roster are still separate work.

## Staff weapon art-only study — 2026-09-19

No device operation or new APK in this slice. Source artwork/browser proof in ArtDirection/2026-09-19/staff-weapon-01. Prior installed Staff-impact7A107BD2 and prior observed profile154/11616gold/Daggers/Second Wind remain historical; inspect fresh before future use.

## Painted Staff APK installed; live review pending — 2026-09-19

S23 RFCW20W2WFX was connected, awake and unlocked, with a non-game app focused. Asked whether the phone would be available for a live test; no reply at time of installation. Announced update without opening the game, then `adb install --no-streaming -r` succeeded. No launch, tap, swipe, screenshot, recording, proof flag, save edit or restore. Existing app use was left alone. Installed base.apk SHA-256 matches `4A73EE71112FE26005D97A9AD4AE0E986C9A6EF4AD7F8E4ED434D1AB86EF2E42` (local APK 24,752,344 bytes). Gate: .NET291, compile clean, EditMode306, PlayMode114, Android exit0.

Fresh preinstall primary/backup pulled to ignored TestResults/staff-weapon-device-2026-09-19/before. Stored state is revision163 /12570 gold /Staff /Counterweight, both weapons/relics owned, deepest floor2. After-install hashes exactly match before: primary `F00D6D5854672A14D6A32FAED9E62BB7C6C3689C77750647941A9F7BF5FC3B29`, backup `C09418B3C0FED76D75432B2E6D8584D707B07D83104809DAA804CA6148E41C09`. Development folder was empty. These fresh values supersede older profile154/Daggers entries; no rollback.

Next: when the phone is available, launch Cryptforge and review the already-equipped Staff in motion/cast/front/rear, using focus/awake/keyguard guards. This slice establishes installation and Editor visuals, not physical appearance/performance. Daggers and remaining art stay separate.

## Staff live S23 review completed — 2026-09-19

Owner explicitly handed over the phone after the pending-availability entry. Launcher was awake/unlocked; launched Cryptforge with am start. The first capture guard rejected the startup transition, then a fresh check passed. Normal Ember Hall1/6 opened at level1 upgrade choice, 90HP, 5run gold, XP11/22. Selected Quickened Grip once and made two guarded diagonal drags (southeast then northwest). A 12-second live screenrecord and original front/rear screenshots confirm the imported staff, mirrored rear presentation, grip retention and single crystal. Reviewed the full recording as a contact sheet and 12 native-size cropped frames around a cast: the whole staff leans and returns, with the purple impact distinct from the blue range ring. No visible separation in this short sample; not a sustained performance or all-device test.

All inputs/captures used focused/awake/unlocked guards, including post-capture/recording checks. No reinstall, purchase, loadout change, proof flag, save edit or restore. Profile primary/backup remain byte-identical to the prior installation hashes: revision163,12570gold,Staff/Counterweight, same unlocks/deepest2. Telemetry naturally advances. Last1200 log lines contained no FATAL EXCEPTION/NullReferenceException/MissingReferenceException matches; not exhaustive. Left at normal Ember Hall1/6 level2 upgrade choice,80HP,10run gold,XP27/36. Phone returned to owner; inspect fresh next time.

Evidence: ArtDirection/2026-09-19/staff-weapon-runtime-01/device contains original front/rear screenshots and a 540px-wide H264 transcode at unchanged speed. Raw video/contact sheets remain ignored in TestResults/staff-weapon-device-2026-09-19. Installed APK remains 4A73EE71112FE26005D97A9AD4AE0E986C9A6EF4AD7F8E4ED434D1AB86EF2E42. No code change or suite rerun in this device-only follow-up.

Daggers source-only follow-up (2026-09-19): no device operation, new APK or loadout change. staff-weapon-runtime-01/device remains latest physical evidence; Daggers source in daggers-weapon-01 is not in the installed app.

Daggers Unity proof follow-up (2026-09-19): no phone operation or new APK. Actual Editor renders exposed a hilt/contact issue in existing thrust; runtime integration remains pending. Staff4A73EE71 remains installed; previous device state is historical.

## Daggers equipment APK installed; live review pending — 2026-09-19

Owner authorised continuation. Connected S23 was unlocked with another app focused; asked whether it would be available for testing, with no reply before installation. Announced background app update and installed with `adb install --no-streaming -r`; success. No game launch, UI input, screenshot, recording, proof flag, loadout change or save restoration. Installed base.apk matched SHA256 `62D5A2707299AB8865B581ED3B5FD69F135EB727E8D562362F997B7F6055D603`, local APK24,752,344bytes. Gate291/306/115, compile clean, graphics1, integrity444/568, Android0.

Fresh primary/backup pulled before install to ignored TestResults/daggers-runtime-device-2026-09-19/before. Revision165,12696gold,empty equippedWeaponId (default Sword),Counterweight,both unlocks/deepest2. After-install hashes exactly matched those fresh files: primary `B491EE6E287087787E54F5B1B8C82EF256F329CFDA09028886FFDE0DDA920900`, backup `DF2B5FD5EEFD90882ED9D8007160EAE15778811D44A7DBB21C42759C762B4224`. Development folder empty. Supersedes previous revision163/Staff state; do not restore it. Phone remains with owner. Next: coordinated live test of owned Daggers via UI; installation is not physical visual acceptance.

## Ground reward APK ready; phone review pending — 2026-09-22

Integrated shared reward art and pooled presentation passed .NET380, EditMode395, PlayMode124, compile clean, graphics1 and integrity482/568. Android build exit0, APK24,752,438bytes, SHA-256 `9DD2A9A758C95DCB88F91E4204B0207D61E30389787AF674414A0805B19A0F28`. Player data was rebuilt (scene hash changed), and both GroundDropView IL2CPP metadata and Reward_ExperienceGold shared assets are present in the APK. Eight controlled Unity renders are in ArtDirection/2026-09-22/reward-runtime-01; they are not phone captures.

Asked whether USB/phone were available for a live run; no answer arrived during the slice. Only `adb devices -l` ran, listing RFCW20W2WFX / SM_S911B. No install, launch, touch, screencap, recording, profile/telemetry read/write, development flag or restoration. Do not infer the current installed app/profile from the older September19 entries. Before next install, inspect fresh device state, preserve the current profile, and coordinate a short run. Verify resting drops, one-unit pickup, choice pause/resume and floor sweep; performance has not been measured for the new view.

## Ground reward build installed; screen unlock needed — 2026-09-22

Owner requested continuation. S23 RFCW20W2WFX was connected, Dozing with keyguard showing. Requested that the owner unlock it for live review. No launch, input, screen capture or unlock attempt was made.

Backed up all nine files under /sdcard/Android/data/com.cryptforge.prototype/files to ignored TestResults/reward-device-20260922-233551/before/files. Fresh profile revision186,26321gold,defaultSword,SecondWind,deepestFloorCleared3; both paid weapons and both relics owned. Installed via adb install --no-streaming -r: Success. Installed base.apk SHA-256 matches 9DD2A9A758C95DCB88F91E4204B0207D61E30389787AF674414A0805B19A0F28. Post-install pull and hash comparison confirm all nine app-files are byte-identical, including primary/backup profiles and all three telemetry logs. Primary hash40F8FA69CEA9B53ED1D6FB924D70A3D708C7800E2A6B1501C57D9CA1B872D2DE; backup E79CC1322A981A2A1B97D0BA295DBBF279374149AC0BA7682EA0B6C99085B00B.

No restoration or save edit, no proof flag/loadout changes. Existing development/start-floor.txt.off is inactive and untouched. Phone remained locked/dozing on the last check. This supersedes previous not-installed statements; live reward/pickup/choice-pause and performance checks remain pending owner unlock. No suites rerun for installation alone.

## Ground rewards first live inspection — 2026-09-22

Owner unlocked the device; awake/unlocked launcher check passed, launched Cryptforge with am start. Two screencaps and one pause tap used game-focus/awake/keyguard guards. First normal-room capture shows Cinder Walk2/6,XP0/10,gold0,57HP and visible ground rewards. Found a presentation gap: CinderMite's authored XP1/gold0 means the common XP-only drop uses provisional pixel art; the earlier assumption that single-currency rewards were rare was wrong. Next reward-art work should provide a matching painted XP-only sprite, retaining the correct absence of gold.

Between observations the run advanced to room5/6,level6,XP90/112,gold51,84/115HP without agent-driven choices/movement. Asked owner to coordinate inputs; paused the game with a guarded tap. These captures cannot establish controlled pickup or flight timing. Originals committed to the artifact folder as files (not a git commit): ArtDirection/2026-09-22/reward-runtime-01/device/first-ground-rewards.png and review-paused.png. Primary profile hash stayed40F8FA69CEA9B53ED1D6FB924D70A3D708C7800E2A6B1501C57D9CA1B872D2DE. No save restore/flag/loadout change. Installed APK9DD2A9A7 remains; live acceptance is partial, not complete. Phone last observed paused; ask/check current use before inputs.

## Controlled phone follow-up — 2026-09-22

Owner explicitly handed over the idle phone. Guarded capture confirmed the previous paused room5/6 state. Recorded 25 seconds, resumed through the visible Resume button and performed one downward drag. The XP-only drop disappeared on approach and XP increased90->91 while gold stayed51. Frame samples show its short approach/disappearance, but actor overlap prevents an exact flight-duration assessment. A combined painted cyan/brass drop is clearly visible beside the hero after the remaining room5 enemy dies and room6 begins. The normal boss then died automatically; a level-up panel appeared with gold126, level7, XP121/136 and44/115HP. No upgrade was chosen; the phone is left safely waiting at that panel. Choice frames remain visually stationary; this recording does not isolate a visible in-flight reward behind the panel, so the precise pause/resume flight contract remains supported by Unity tests, not independently proven on-device.

Evidence: device/controlled-pickup.png, device/combined-drop.png, device/controlled-choice.png. Full original recording and sampled contact sheets remain under ignored TestResults/reward-device-20260922-233551/live. Primary profile hash remains40F8FA69CEA9B53ED1D6FB924D70A3D708C7800E2A6B1501C57D9CA1B872D2DE (revision186/gold26321). No save edits/restoration, active development flag, purchase or loadout change. No performance claim. No code changes or repeat build/tests in this device-only follow-up.

Device smoke check now confirms visible drops and proximity collection with payment; reward art remains incomplete because the frequent XP-only fallback still needs matching painted artwork. Next art priority: XP-only crystal, followed by the exit seal. Inspect fresh device state before further inputs.

## XP-only APK ready — 2026-09-23

APK1F00CF65D105882E8976A0B3167B5A600A57022B38D9A0B9ED2D1FC997BBA168 passed full gate380/395/124 and graphics1. Owner asked for game integration. Phone availability asked but unanswered: no adb access, install, launch, input, capture or profile access in this slice. Device still has the previous build according to the latest historical record; inspect fresh before next interaction. Source and actual Unity captures: ArtDirection/2026-09-22/xp-reward-runtime-01/.

## XP-only installation and device presence check — 2026-09-23

Owner explicitly handed over the idle phone and requested installation. S23 was awake/unlocked with game focus. Force-stopped the app, backed up all9 external app-files into ignored TestResults/xp-device-20260923-001139/before, installed with --no-streaming -r (Success), pulled after-files and verified all9 byte-identical. Installed base.apk SHA256 matches1F00CF65D105882E8976A0B3167B5A600A57022B38D9A0B9ED2D1FC997BBA168. Stored profile revision186/gold26321/defaultSword/SecondWind/deepest3; no save restore, purchase, loadout or flag change.

Launched normally. Guarded capture shows painted XP-only crystals on the actual phone at Ember Hall1/6, level0/XP0,0run gold,84/100HP. A guarded pause tap followed; handback capture saved. Evidence: ArtDirection/2026-09-22/xp-reward-runtime-01/device/installed.png and handback.png. This verifies installation, launch and actual sprite appearance; no new collection-timing or performance claim. The force-stop/install restarted the active run, not the persistent profile. Phone handed back paused. Previous not-installed statements are historical and superseded.

## Finale seal APK ready — 2026-09-23

Full gate380/395/127 passed; APK28B658D4AFC4FBBFB68623F0EA97A3F2F3AD1CA9ECB747B10930ED57B95E0B41 ready, NOT installed. No device operations this turn. Actual Unity proof: ArtDirection/2026-09-23/exit-seal-runtime-01; physical exit hold/cue remains pending. Inspect fresh before device review.

## 2026-09-23 — Wide region and furnace development integration

Gameplay now binds Furnace.tga and ArenaView supports a once-resolved development/region-preview.txt=wide flag. Default hall and release behaviour preserved; wide preview is -27/+27/18. FoundrySceneryView borrows art/material and owns its foundation mesh. No local encounters, collision/elevation or new progression rules. Graphics capture now loads actual Gameplay, not an enlarged scene copy. See ArtDirection/2026-09-23/region-runtime-01/README.md for commands, proof and limits.

Verified: .NET380, compile0 warnings/errors, EditMode395/395, PlayMode130/130, targeted region2/2, graphics1/1, browser390px checked. Android exit0; actual APK29,252,655bytes, SHA256 C8B4592C03EF35A437BF7EB027B54D484DD333C48946EEF212D0B793D9F12704. APK is BUILT, NOT INSTALLED. USB device enumerated only; availability question unanswered, no launch/input/screenshot/profile access. Prior XP-only installed record remains current. No commit/push in this slice.

Next: coordinated fresh app-files backup, install and hash verification, temporary wide flag (preserve any existing value), physical traversal/visual review, remove only the created flag and return to normal hall. Playing normally may award/persist progress; never restore stale profile data. After that: local encounter-site gameplay and richer perimeter scenery.

## 2026-09-23 — Wide-region APK installed and device traversal

Owner authorized phone use. Fresh9-file backup in ignored TestResults/region-device-20260923-151946, app force-stopped, install --no-streaming -r succeeded. Installed base.apk SHA256 C8B4592C03EF35A437BF7EB027B54D484DD333C48946EEF212D0B793D9F12704 matched. All9 files byte-identical immediately after install. Fresh stored profile189/27099gold/defaultSword/SecondWind/deepest3 supersedes previous186 state.

Created only region-preview.txt=wide; original inactive start-floor.txt.off preserved. First walk was contaminated by owner inputs (owner confirmed), so no conclusions from its progression. After explicit handover restarted the wide run and sent a guarded25s north drag. Reached north furnace/rim; camera follow and fixed-scale paving visible on physical S23. Screenshot shows furnace overly occluded by hero at the exact tip: placement spacing needs polish, not final visual acceptance. Normal combat remained active and progressed to room2; no explicit upgrade selection or economy edit by assistant. This is a visual/traversal smoke test, not a performance benchmark.

Removed the test-created region flag, force-stopped and returned to normal Ember Hall1/6. Paused at100HP/level0/XP0/runGold0 for handback. Primary and backup profiles match the fresh preinstall files byte-for-byte; telemetry naturally grew, no rollback. All taps/swipes/captures checked focus/awake/unlocked before and capture guard after. Last1200 log lines had no FATAL EXCEPTION/NullReferenceException/MissingReferenceException matches. Evidence: region-runtime-01/device/north.png and handback.png. No code/build changes in this device slice.

## 2026-09-23 — Playable foundry installed and handed back

Owner explicitly requested playable integration and confirmed phone free. Existing paused finale completed through UI/exit, banking373gold; fresh preinstall profile194/27795gold/defaultSword/SecondWind/deepest3. Backups: TestResults/foundry-device-20260923-171628. New APK7FB249A4E8EB25560113ACF417521C25AB6809DD9B8A1EA118812D367B7C4CBD installed/hash matched; initial ADB response interrupted, subsequent hash proved installation already complete. All9 files unchanged immediately after install.

Created region-preview.txt=foundry. First idle startup ended in0gold defeat before controlled input. UI restart, guarded8s northwest drag demonstrated map/camera/pursuit toward furnace passage; pause/resume and5s return collected5gold/13XP and opened level1 choice at80HP. Left there for owner play, FLAG RETAINED intentionally. Profile primary/backup byte-identical after review, telemetry advanced; no restore/loadout change. Old normal-hall handback entries are superseded.

Captured Unity process log has no matched NullReferenceException/MissingReferenceException/InvalidOperationException/FATAL EXCEPTION. Sample59.7–59.9fps includes paused windows; one active/restart window max33.4ms. Short flagship smoke, not broad performance validation. Photos and exact limits in foundry-playable-01/device and README. New map uses existing automatic room progression; local-site unlocks/separate boss-hall transition remain pending.

## 2026-09-23 — Rough map APK ready; installation deferred for active run

APK648EA6B0CB5283F8FF8AA3687DF4035A3C8BE5BE9E3476B5B3350B67B3FBDE75 (33,755,809bytes), Android exit0 after402/131 gate. Not installed. Fresh read-only S23 inspection: awake/unlocked/game focused; floor3 finale upgrade choice,348 run gold,129/175HP,level13. Stored profile196/28118gold/defaultSword/SecondWind/deepest3. Backup TestResults/foundry-rough-20260923-180733/before; region-preview=foundry retained. Owner question pending: finish personally, controlled exit by assistant, or abandon/install. No game input or phone data modification. Prior7FB installation remains current until a later verified install.

## 2026-09-23 — Rough map installed after owner closed game

Owner confirmed the game was closed and authorized continuation. Fresh backup TestResults/foundry-install-20260923-182322. Installed APK648EA6B0CB5283F8FF8AA3687DF4035A3C8BE5BE9E3476B5B3350B67B3FBDE75 with adb install -r; Success and installed base.apk SHA256 matched. All10 files byte-identical immediately after installation. Fresh profile196/28118gold/defaultSword/SecondWind/deepest3; primary and backup still byte-identical after test. No restore or manual save edit; telemetry naturally advanced. Previous open348-gold run was already closed by owner; assistant did not bank it.

Launched normal authored Ember Hall1/6 with retained region-preview=foundry. Guarded pause/resume,8-second northwest drag, pause: camera moved across square toward side/furnace junction;3 enemies defeated, XP crystals visible. Left paused at85/100HP,level0,XP0/10,runGold0. No upgrade or loadout selection. This is a short device movement/render smoke, not full map traversal/performance acceptance. All five paths were verified in Unity in the previous slice. Current-process log had no matched FATAL EXCEPTION/NullReferenceException/MissingReferenceException/InvalidOperationException. Evidence in foundry-playable-01/device-rough. Latest installed648 supersedes7FB and pending-install entries. Device ready for owner Resume.


## Photograph-led build ready — installation pending active-run reply

Final gate: pure388; Unity EditMode403 /PlayMode131; graphics1; compile clean; integrity494 GUIDs/568 scene objects. Android exit0, APK33,758,335bytes, SHA256 A8ED8E1C8F0DEFCF341CA998237E32A19E354794A9873D9B130EDC4B1C9C64A3. Built but NOT installed. Device still has648EA6 rough linked-map build.

Read-only screenshot during this slice found a new owner run at Warden's Crucible1/6's room6,116 run gold,21/100HP,level7 upgrade choice. Asked whether owner will finish or authorizes abandoning for install; no reply yet. No tap/swipe/install/force-stop/save edit performed in this slice. Previous last-known profile196/28118 is not freshly verified here; inspect again before install. Screenshot TestResults/foundry-install-20260923-182322/live/before-layout-update.png. Owner's earlier closed-game authorization applied to the completed648 install, not this newly observed active run.


## 2026-09-23 — Photograph-led map installed on S23

Owner said continue; fresh inspection found launcher focused, game closed. Fresh backup TestResults/foundry-photo-install-20260923-195830. Profile197/28234gold/defaultSword/SecondWind/deepest3: the previous116gold had been banked before this session. Installed APK A8ED8E1C8F0DEFCF341CA998237E32A19E354794A9873D9B130EDC4B1C9C64A3 via install-r; Success and installed base.apk hash matched. All10 files identical immediately after installation; profile and backup identical after smoke. No restore or save edit. Foundry flag retained.

First launch request failed during an ADB daemon connection interruption; game remained on launcher and input guards rejected taps. Rechecked device and retried launch only, not installation. Successful start paused at100HP. Guarded resume /12-second leftward drag /pause confirmed camera movement, combat and visible new court/shaft area. Left Ember Hall1/6,wave3/3,77HP,level0,XP0/10,runGold0,paused. No loadout or upgrade choice. Current-process log had no matched FATAL EXCEPTION/NullReferenceException/MissingReferenceException/InvalidOperationException. Short S23 smoke only, not complete physical-device traversal or performance proof. Earlier five-region Unity walk remains the traversal evidence. Screenshots device-photo. Supersedes pending-install and648 device status. No code or rebuild in this device turn.


## Final build status — 2026-09-23

Full gate403 EditMode /131 PlayMode passed; Android exit0. APK48AB4DFA3F0AC71D8B9B6AF56A25F95B8E57B5D1FDADB897805FBFCD49050CA5,29,253,015bytes, built NOT installed. Device read-only check found NotificationShade/keyguard showing; guarded screenshot refused as designed. Asked owner to unlock and leave game closed or continue only on computer. No answer yet; no taps, installation, force-stop or profile operation this slice. Installed phone remains A8ED8E1C8F0DEFCF341CA998237E32A19E354794A9873D9B130EDC4B1C9C64A3. Last recorded profile197/28234 is historical until freshly read again.


## 2026-09-23 — Installed; USB lost before gate-position review

Owner unlocked and requested continuation. Fresh inspection launcher/unlocked. Backup TestResults/gate-install-20260923-211101/before: profile197/28234gold/defaultSword/SecondWind/deepest3. Installed48AB4DFA3F0AC71D8B9B6AF56A25F95B8E57B5D1FDADB897805FBFCD49050CA5, adb Success and base.apk hash matched. All10 files byte-identical immediately after installation. Foundry flag retained. No code/build changes this turn.

Launched and paused100HP. Guarded resume and28s northeast drag ended at level1 upgrade choice,86/100HP,XP11/22,5 run gold. Gate not yet visible. Next guarded input sequence was rejected before dispatch because device RFCW20W2WFX disappeared; adb devices confirmed empty. No further taps/swipes succeeded. Final profile/log pull could not be performed, so only prelaunch installation preservation is verified. No restore/save edit. Last screenshot gate-walk.png is the last observed state, not a guarantee of current phone state. Installation is complete; gate-specific physical-device review remains incomplete. Actual gate render remains verified in Unity. Inspect fresh on reconnection; do not reinstall blindly.


## 2026-09-23 - Furnace/masonry APK installed and reviewed

Owner confirmed phone free. Fresh launcher/unlocked inspection and backup TestResults/surfaces-install-20260923-215604. Profile revision199/28507gold/defaultSword/SecondWind/deepest3. Installed APK0CC7AD1F0FED3C145E7A5A62B6A48123DD3E07A9AEA0CAC7AF978D0596CC4750 (29,253,375bytes); adb Success, installed base.apk hash matched, all10 files byte-identical immediately after install. Region foundry flag retained.

Guarded launch, northeast movement and two in-run upgrades (Quickened Grip, Tempered Edge) reached the gate approach. Physical S23 screenshot shows imported furnace faces, exposed chimneys, masonry supports and gate together. Combat and camera follow remained active. Left PAUSED in Slag Gate room3/6, wave2/2,42/100HP,level2,XP30/36,15 run gold; Second Wind triggered during review. Primary and backup profiles byte-identical after test; telemetry advanced normally, no restore/save/loadout edit. Current process log has0 matches for FATAL EXCEPTION/NullReferenceException/MissingReferenceException/InvalidOperationException. Initial capture during activity startup rejected null focus; no input was sent until focus resolved.

Evidence: ArtDirection/2026-09-23/foundry-surfaces-01/device/surfaces-furnaces.png and handback.png. Short rendering/movement smoke, not a performance benchmark or full-device map traversal. Full Unity403/131, graphics1, clean compile, integrity500/568 recorded in docs22. This supersedes48AB installed status. Remaining art: crane, pipe connections, shaft dressing, roof/material refinement. No boss transition or new gameplay mechanics.


## 2026-09-23 - Combined foundry dressing installed

Under owner authorization to complete and integrate the entire package, fresh inspection showed the assistant's previous paused test run unchanged (42HP/15runGold/room3). Update restarted that unbanked test run; no user run was progressed or banked. Fresh backup TestResults/complete-install-20260923-223213; stored profile199/28507gold/defaultSword/SecondWind/deepest3. Install-r Success, installed base.apk SHA256 7EE642D10E16A21588E40318D70CCC2F03C3ADFB61FE6CD27BA43851855E42EB matched local. All10 files byte-identical immediately after install.

Normal authored run with retained foundry flag. Guarded10-second west drag and12-second northwest drag, with pauses between, reached shaft then crane. Actual S23 captures show heated shaft panels/inner rail/slag basin and complete crane with basket. Movement, combat and camera follow worked. No upgrade/loadout selections in this run. Handed back PAUSED at crane, Cinder Walk room2/6,wave3/3,64/100HP,level0,XP1/10,0runGold. Profile and backup byte-identical after review; telemetry grew normally, no save edit/restore. Current-process log0 matches for FATAL EXCEPTION/NullReferenceException/MissingReferenceException/InvalidOperationException.

Active sampled windows after startup:59.9fps,max16.9-17.1ms; short S23 smoke only. First startup window contains2147.2ms maximum, not excluded from evidence or claimed resolved; paused windows are not gameplay benchmarks. No midrange-device measurement. All five regions traversed in Unity, only shaft/crane physically reviewed this turn. Screenshots foundry-complete-01/device. Supersedes prior0CC7 installed build. No commit/push.


## 2026-09-23 - Energy-free APK ready, device install pending

Built APK F0105B61C8454C4867D6CCA47D35E0CF592E4BCA85CEF7748271FAF2037EA67A after390/405/130 gate. NOT INSTALLED. Fresh read-only device screenshot showed owner progress beyond the assistant handback: paused boss room6/6,35runGold,38/100HP,level4,XP57/70. Asked whether to restart/install or let owner finish; no answer at completion. No tap/swipe/force-stop/install or profile change in this turn. Installed device remains7EE642D10E16A21588E40318D70CCC2F03C3ADFB61FE6CD27BA43851855E42EB, with energy still active. Screenshot TestResults/complete-install-20260923-223213/live/energy-before.png. Inspect fresh before installation; do not assume old profile199 is current. Decision deferred in docs15/19/26, proof in energy-deferred-01.

## 2026-09-24 - Foundry lighting and energy-free movement installed

Owner explicitly authorized restarting the previous run, installing and testing. Fresh phone launcher/unlocked. Backup TestResults/foundry-lighting-20260924/before. install-r Success; installed SHA256259EA288CC447CE38B8371DA694B7D43B57777A80AB8A6B6B2C5A63C57B394F1 matches local; all10 stored files byte-identical immediately after install. Includes energy deferral previously pending installation. Profile revision199/28507gold/defaultSword/SecondWind/deepest3, SHA256FBCE902BD0202A5F83C74D0BE4194F0D37EDBCD329D10120F48F6F6C0165E90D unchanged after review. No restore/save edit.

Guarded normal run: two12-second northeast drags and one Tempered Edge choice reached furnace/gate; actual physical screenshot in ArtDirection/2026-09-24/foundry-lighting-01/device-gate.png. Warm floor pools, cooler stone faces/cap highlights and absent movement-energy HUD reviewed. Final Forge room4/6 choice,14HP,level1,XP20/22,5runGold. A pause tap landed after the Forge choice opened; it did not dismiss the choice. No Forge selection made. Inspect fresh before further input.

Current process log0 matches for FATAL EXCEPTION/NullReferenceException/MissingReferenceException/InvalidOperationException. Normal sample windows59.9fps; first startup max2211.5ms unresolved. Short visual/movement smoke, not sustained crowded combat or midrange performance evidence. Full405/130 Unity gate and graphics1 passed. No commit/push.

## 2026-09-24 - Gate and boiler architecture installed

Fresh phone launcher/unlocked, previous owner install/test permission continued. Backup TestResults/foundry-architecture-20260924/before, profile208/31166gold/Staff/SecondWind/deepest3. install-r Success; installed SHA2560B23B12F7C9EB5D4CDB38802820E8FD5CA8BBA7BA43ADF768371BC4F41FF95A1 matches local and all10 stored files byte-identical immediately after install. Profile SHA256D3D4FF563175BDF50EC2E9D32A772A015D7544E2C4F9C7704374DBD441BB7BCD unchanged after review. No backup restore/save/loadout changes.

First normal run died in Slag Gate with0 banked gold. Try again then guarded18-second northeast drag reached gate; unchanged device-gate.png in foundry-architecture-01 shows new pillars and open-mouth boiler stacks with live Staff combat. Last screen Cinder Walk2/6,level2,XP22/36,45HP,8runGold at upgrade choice. Pause tap was issued but final captured screen is the choice, not pause menu; no deliberate agent upgrade selections. Do not use intervening progression as controlled balance evidence. Inputs ended; inspect fresh before any further interaction.

Current process log0 matches for FATAL EXCEPTION/NullReferenceException/MissingReferenceException/InvalidOperationException. First startup max2149.6ms remains. Brief physical visual/movement review; no sustained density or midrange evidence. Full405/130 Unity and graphics1 passed. Proof README has complete scope/limits. No commit/push.

## 2026-09-24 - Combined courtyard package verification

Final five-waypoint Unity graphics proof passed (1/1). Full gate: EditMode 405/405, PlayMode 130/130, Android build exit 0. Project integrity: 511 unique project GUIDs, 1099 package GUIDs, 568 scene objects. Static scenery remains 15 renderers; vertices increase from 23,160 to 39,740. No combat rules changed; pure .NET combat suite was not rerun.

APK Builds/Android/Cryptforge-Prototype.apk: 29,999,777 bytes, SHA256 460BD079412E658692CEA1EF32FF79CD5BF54C803E86158B84D326BC45CA1261. Installed on the S23 with install-r Success; installed base.apk hash matched. All 10 stored files byte-identical immediately after installation. Fresh backup and logs: TestResults/foundry-courtyard-20260924.

Owner unlocked and left phone free. Guarded 15-second bridge movement and 12-second return to courtyard reviewed new paving, bridge piers, inlays and live Staff combat. Physical screenshots: device-bridge.png, device-courtyard.png, device-handback.png in ArtDirection/2026-09-24/foundry-courtyard-01. Left PAUSED in Cinder Walk, room 2/6, wave 3/3, 68/100 HP, level 0, XP 4/10, 0 run gold. No upgrades or loadout changes selected. Profile revision 208, gold 31166, Staff/Second Wind/deepest 3 unchanged after review; SHA256 D3D4FF563175BDF50EC2E9D32A772A015D7544E2C4F9C7704374DBD441BB7BCD matches fresh pre-install copy. No save restoration or editing.

Current process log has 0 matches for FATAL EXCEPTION, NullReferenceException, MissingReferenceException or InvalidOperationException. Startup still includes 2185.3 ms and 222.8 ms frames; these remain unresolved and are not hidden by the short visual review. This is not a sustained density or midrange-device benchmark. Crane and shaft details were verified in Unity; physical traversal this turn covered bridge and courtyard only. Energy deferral remains active. No commit/push.


## 2026-09-24 - Undercroft APK ready; device coordination pending

## Final verification

Full Unity gate passed: EditMode 405/405; PlayMode 130/130; Android build exit 0. Final project integrity passed: 511 unique project GUIDs, 1099 package GUIDs, 568 scene objects/components. Five-point graphics proof passed 1/1 before the full gate. Existing region batching/sorting/reload tests passed unchanged. Pure combat suite not rerun for this presentation-only change.

APK Builds/Android/Cryptforge-Prototype.apk: 34,532,412 bytes; SHA256 FD0D282C3E39746AC89E01FECB73E662136703E439FF80092AE34BE12A6DE4F6. READY, NOT INSTALLED in this turn. Physical smoke remains pending owner device coordination. Initial guarded capture refused because game focus was absent; no capture or input dispatched. Later read-only window check showed launcher/unlocked/awake. No tap, swipe, force-stop, app launch, install or save modification performed. Last verified installed package remains the preceding courtyard build 460BD079412E658692CEA1EF32FF79CD5BF54C803E86158B84D326BC45CA1261; do not mistake new Unity screenshots for device evidence. Do not assume the previous paused run remains current; inspect fresh before installation. No commit/push.

## 2026-09-24 - Device install and handback (supersedes pending status above)

The owner explicitly requested installation for hands-on review. Fresh inspection found the S23 connected, unlocked, awake and on the launcher. A backup of the existing 10 app files was pulled before install. `adb install -r` returned Success. The first whole-directory after-install pull lost USB transport partway through the large IL2CPP metadata file, so that partial local copy is not evidence of data loss; the device reconnected automatically. Installed base.apk SHA256 FD0D282C3E39746AC89E01FECB73E662136703E439FF80092AE34BE12A6DE4F6 matches the built APK. A fresh individual pull verified profile, backup profile, foundry preview flag, disabled floor flag and all three telemetry files byte-for-byte against their preinstall copies. The stored profile remains revision 208, 31,166 gold, Staff/Second Wind, deepest floor 3; profile hash still matches after launch.

The game launched explicitly and a guarded S23 screenshot captured live Ember Hall room 1/6 with the character and enemies on the updated courtyard. No movement or purchase was performed by the assistant; the game was left open and running for the owner to try. Current-process log has zero matches for FATAL EXCEPTION, NullReferenceException, MissingReferenceException or InvalidOperationException. This is an installation and startup smoke, not a physical traversal of the new undercroft or a performance benchmark. Unity five-point graphics proof covers undercroft visibility. Evidence: TestResults/foundry-undercroft-20260924, including before/, after-install/ critical files, 01-start.png and game.log. No save restore/edit. No commit/push.

## 2026-09-25 - Atmosphere verified and installed

Compile check: 0 warnings/errors. Full Unity gate: EditMode 405/405, PlayMode 130/130, Android exit 0. Final five-point graphics walk plus sampled plume and same-frame effect-on/off captures passed 1/1. Final integrity after temporary capture cleanup: 512 project GUIDs, 1099 package GUIDs, 568 scene objects/components. Pure combat suite not rerun for this presentation-only change. Preview: ArtDirection/2026-09-25/foundry-atmosphere-01.

APK A03DF2201C2FBABBEF337C84CDE6FB2DD7E2C9C83D38D3263CFC5FCE3AACA707 (39,340,606 bytes) installed on S23; install-r Success and installed base.apk hash matched. Owner explicitly approved restarting the paused room-2 run and left device free. Fresh backup in TestResults/foundry-atmosphere-20260925/before. All 7 saved profile/backup/development/telemetry files byte-identical immediately after install. Profile revision 209, 31,166 gold, default Sword/Second Wind/deepest 3; SHA256 C2A33F051D75465E7AFAA1FBF720E3EF18AE3F3044794D87F3A20D7488964DAE unchanged after review. No save restoration/edit or loadout change.

Guarded northeast traversal, one Vital Surge in-run choice, then short north movement reached the furnace/gate approach. device-plume.png shows the actual phone plume and embers. Pause confirmed: 06-stop.png and 07-paused-repeat.png were byte-identical. Final handback PAUSED in Cinder Walk room 2/6, wave 3/3, 58/130 HP, level 1, XP 18/22, 5 run gold. Screenshots and process log retained. A pause tap during the earlier level-up choice did not dismiss it; the later photographed pause is the actual final state.

Current process log: zero matches for FATAL EXCEPTION/NullReferenceException/MissingReferenceException/InvalidOperationException. Early active windows 2-5 measured 59.9 fps, max frame 17.1 ms. Startup window retained 2181.8 ms and 375.8 ms spikes. This short S23 sample is not sustained density, isolated allocation or midrange acceptance. Code reuses arrays; no assertion that all game allocations disappeared. No commit/push.

## 2026-09-29 - Combined foundry art package verified and installed

Presentation-only package: quieter paving, gate/flue hardware, peripheral workshop stock, engraved court seal and supported braziers. Compile check passed with 0 warnings/errors. Static integrity passed: 514 project GUIDs, 1099 package GUIDs, 568 scene objects/components. Full Unity gate: EditMode 405/405; PlayMode 130/130; Android build exit 0. Final graphics capture passed before the full gate and was independently visually reviewed. Environment static census 17 renderers / 46,476 vertices (previous 16 / 44,716), plus the unchanged atmosphere 1 / 780. Existing sorting, reload and walking checks pass; renderer cap is now 17 to allow the necessary explicit gate iron overlay group. Pure combat suite not rerun for presentation-only edits.

APK SHA256 BF87942E8C9EAE76AA5DDCB9F575493F1B3C9C260E5EA41E82F4FFD69FC196CC, actual file length 39,340,606 bytes. S23 RFCW20W2WFX install-r succeeded; installed base.apk hash matched. Fresh device inspection showed launcher, awake and unlocked. Before/after-install individual pulls confirmed all 7 profile/backup/development/telemetry files byte-identical. Profile remained revision 215, 32,776 gold, Staff/Second Wind, deepest floor 3; post-review profile hash also matched. No profile edit/restore or loadout change.

Guarded short movement reached the north pipe-storage shelf and showed the new floor and supported flame in actual portrait combat. Handback confirmed PAUSED in Cinder Walk room 2/6, wave 2/3, 67/100 HP, level 0, XP 1/10, 0 run gold. Phone evidence: TestResults/foundry-art-package-20260929/03-west.png (historical filename; actual view is north pipe shelf), 04-paused.png, game.log, install.txt and save copies. No FATAL EXCEPTION, NullReferenceException, MissingReferenceException or InvalidOperationException in the current-process log. Active sample windows 2-3: 59.9 fps, max 16.9 ms; startup window retains 2165.4 ms spike. This short S23 smoke does not establish sustained-density or midrange performance, nor fix startup stalls. Whole-map, gate, workshop and shaft review additionally used real Unity captures, not phone captures.

Preview: ArtDirection/2026-09-29/foundry-art-package-01. The reference art target remains unfinished, especially architectural silhouette variety and material richness. No commit/push.

## 2026-09-29 - Crane and facade silhouette package ready; device install pending

Completed crane mounting/cradle/bearing/counterweight/basket refinement and faceted undercroft piers/terminal supports/iron anchors. Runtime changes limited to FoundryRegionDressing.cs and FoundryRegionUndercroft.cs; walking, collisions, gameplay and scene data unchanged. Compile check 0 warnings/errors; integrity 514 project GUIDs / 1099 package GUIDs / 568 scene objects/components. Actual five-point graphics traversal/capture 1/1, reviewed crane, overview and bridge; independent facade cross-review accepted. Full Unity gate: EditMode 405/405, PlayMode 130/130, Android build exit 0. Static census 17 renderers / 47,096 vertices; existing atmosphere separately 1 / 780. No new materials or raster assets.

APK E51DEC6448AD4012B43E0C906FD72DD9270769120617BCF0E0690AC0D9016645, actual APK file 39,340,606 bytes, built 2026-09-29 13:54:53 local. NOT INSTALLED. Fresh guarded read-only screenshot found the owner had advanced to Forge room 4/6, 41 run gold, 54/100 HP, level 5, XP 76/90. Restart/install versus finish question is unanswered. No device tap/swipe/force-stop/launch/install/profile edit in this turn. Preserve that run; installed package remains BF87942E8C9EAE76AA5DDCB9F575493F1B3C9C260E5EA41E82F4FFD69FC196CC. Screenshot TestResults/foundry-silhouette-20260929/00-before.png. Reinspect device before any later approved install; do not treat old captured state as current.

Proof and before/after: ArtDirection/2026-09-29/foundry-silhouette-01. Reference-quality art remains unfinished. No commit/push.

## 2026-09-29 - Combined gate, boiler, bridge and shaft landmarks

Four integrated art tasks completed: shallow gate pediment; boiler plinth/corner support; bridge socket/inset panels; four-side inner shaft coping/retaining saddles. Includes the preceding crane/undercroft work. Only Architecture/Site runtime presentation sources changed this slice. No gameplay, scene, collision, camera or energy edits; no new textures/materials. Compile 0 warnings/errors; final integrity 514 project GUIDs, 1099 package GUIDs, 568 scene objects/components. Final actual five-position graphics capture 1/1 passed after removing floating gate flashing. Root and independent reviewer inspected final views; no blocker. Boiler plinth is visually heavy: do not increase it further. Static 17 renderers / 49,396 vertices; unchanged atmosphere separately 1 / 780.

Full Unity gate passed: EditMode 405/405, PlayMode 130/130, Android build exit 0. APK SHA256 4576CC04844ABB7F733AB1F354F8CBFD82A9962A3CAB386CE8EADCCE93B38994, actual file size 39,340,606 bytes, local build time 2026-09-29 14:25:34. BUILT, NOT INSTALLED. Owner stated they were using the phone and asked to continue development; no adb/device action performed this slice. Previous installed BF87942E8C9EAE76AA5DDCB9F575493F1B3C9C260E5EA41E82F4FFD69FC196CC remains. Do not assume the earlier Forge screenshot is current. Reinspect/coordinate before restarting the owner's run. No device performance/profile claims for this uninstalled package.

Proof: ArtDirection/2026-09-29/foundry-landmarks-01 (before is the previous uninstalled silhouette package). Final reference-image quality remains unfinished. No commit/push.

## 2026-09-29 - Landmark package installed (supersedes pending status)

Owner explicitly requested installation. Seven critical files backed up in TestResults/foundry-landmarks-install-20260929/before. adb install returned a blank failure, but immediate installed base.apk hash inspection confirmed the exact intended 4576CC04844ABB7F733AB1F354F8CBFD82A9962A3CAB386CE8EADCCE93B38994 package. No second install attempted. Device became unlocked/launcher; game launched successfully. Post-launch verification: profile, backup, two development flags and rotated telemetry files byte-identical; current telemetry retains its exact preinstall prefix and appends launch events. Profile revision215/32776gold/Staff/SecondWind/deepest3 unchanged. No save edit/restore.

Startup capture and pause confirmed room1/6, wave1/3,93HP,level0,XP0/10,0runGold. Game left PAUSED for owner Resume. Current process log has no fatal/null-reference/missing-reference/invalid-operation matches. Installation/startup smoke only, not a new physical landmark traversal/performance benchmark. Evidence 01-start.png,02-paused.png,game.log,install.txt and before/after-install files. Initial verification script had stale LASTEXITCODE and then compared active telemetry after launch; explicit final six-file hash and telemetry-prefix verification resolved those tooling issues. No code/build changes or commit/push.

## 2026-09-29 - Local masonry light/depth package ready

Static RGB-only furnace/brazier bounce on boiler/gate stone tiers and deck-relative darkening of undercroft stone/anchor plates. Geometry/UV/alpha/materials unchanged; 17 static renderers / 49,396 vertices, unchanged atmosphere1/780. Compile0warnings/errors, integrity514/1099/568, actual five-point graphics1/1. Root and independent visual review accepted furnace/undercroft treatment. Full gate passed EditMode405/405, PlayMode130/130, Android exit0.

APK374F0327B734E3DC2009E5B7F047370CA44AFC9392D6AA92E58092BB8DDA65A3, actual39,340,606 bytes, built2026-09-29 15:01:43 local. READY NOT INSTALLED. No adb/device operations this turn; installed4576CC04844ABB7F733AB1F354F8CBFD82A9962A3CAB386CE8EADCCE93B38994 remains. The prior startup/pause snapshot is historical, recheck before operating the phone. No new device-performance/profile claims. Proof ArtDirection/2026-09-29/foundry-depth-01. No commit/push.

## 2026-09-29 - Workshop anvil and local soot verified

Added a supported anvil and resting hammer on an outward-expanded west shelf; retained original stock and court-facing edge. Added smooth local floor soot near shaft/storage without geometry or alpha changes. Includes preceding masonry/depth shading. Static region: 17 renderers / 49,556 vertices; atmosphere unchanged at 1 / 780. Actual five-point graphics capture 1/1; root and independent visual/code review found no blocker. Compile check 0 warnings/errors; integrity 514 project GUIDs / 1099 package GUIDs / 568 scene objects/components. Full gate passed: EditMode 405/405, PlayMode 130/130, Android build exit 0.

APK SHA256 E933DC1CED20FEC4AB984C2B04E83A36F910A3DCF6C3611BAED240329D2C81B4, actual file size 39,340,606 bytes, local build time 2026-09-29 15:23:57. READY, NOT INSTALLED. No device/adb actions in this turn; installed landmark build remains 4576CC04844ABB7F733AB1F354F8CBFD82A9962A3CAB386CE8EADCCE93B38994. Fresh device review needed before any later install; no new phone performance or save claims. Proof: ArtDirection/2026-09-29/foundry-use-01. No gameplay, scene, collision, camera or energy changes; no commit/push.

## 2026-09-29 - Three-agent visual closeout verified

Exactly three helper agents used. Flue band/slot placement now conforms to tapered chimney faces; anvil working face/horn existing vertices receive steel tint; capture-only HUD order corrected to match production overlay semantics. No runtime HUD/scene/gameplay/collision changes. Final actual graphics traversal1/1, furnace/workshop/point3 visual review passed. Static17renderers/49,556vertices unchanged, atmosphere1/780. Compile0warnings/errors, integrity514/1099/568. Full gate passed EditMode405/405, PlayMode130/130, Android build exit0.

APK SHA25618DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535, actual39,340,606bytes, localbuild2026-09-29 15:49:27. READY NOT INSTALLED. Includes uninstalled material-depth/workshop-use slices. No device operations this turn; last verified installed4576CC04844ABB7F733AB1F354F8CBFD82A9962A3CAB386CE8EADCCE93B38994 remains. No new phone performance/save claims; inspect fresh before next installation. Proof ArtDirection/2026-09-29/foundry-finish-01. Named findings closed, final reference fidelity remains future work. No commit/push.

## 2026-09-29 - Foundry finish installed on S23

Supersedes the ready/not-installed status above. Owner explicitly requested installation. Backed up profile, profile backup, two development flags, and three telemetry files under TestResults/foundry-finish-install-20260929/before/. The prior Forge choice run was restarted without selecting an option. adb install -r succeeded; installed base.apk SHA256 matches the built APK: 18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535. Profile, profile backup, development flags, and rotated telemetry files were byte-identical after installation. Active events.jsonl retained its full prior prefix and gained new launch/run events. The new app launched into Ember Hall room 1/6; a short unattended smoke run reached Slag Gate and ended in defeat. Started another run and left it paused in Ember Hall room 1/6 with 100/100 HP, ready for the owner to resume. Evidence: TestResults/foundry-finish-install-20260929/01-start.png and 04-ready.png. No save editing, commit, or push.
## 2026-09-29 - Segmented undercroft arch masonry

Replaced continuous arch faces with 13 individually separated stone faces over recessed mortar, a projecting central key, narrow lit upper arrises, and dark inner returns. Existing static wall material and local depth shading reused. Static budget: 17 renderers / 51,348 vertices (previous 49,556); atmosphere remains separately 1 / 780. No gameplay, collision, scene, camera, or raster asset changes. Actual Unity five-waypoint graphics traversal passed; undercroft, bridge, and full-map renders inspected. This is a subtle close-range refinement, not reference-art completion.

Compile: 0 warnings/errors. Integrity: 514 project GUIDs / 1,099 package GUIDs / 568 scene objects/components. Full gate: EditMode 405/405, PlayMode 130/130, Android build exit 0. APK actual size 39,340,606 bytes, built 2026-09-29 16:26:53 local; SHA256 F5723AB3A6204118B349301B3834043B215C5BFD9134664B066A2DF99DBB499F. READY, NOT INSTALLED. Last verified installed package remains 18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535. No phone operations or new device-performance/save claims. Proof: ArtDirection/2026-09-29/foundry-arches-01. No helper agents, commit, or push this slice.
## 2026-09-29 - Court, approach, and workshop paving

Added three flush paving regions in FoundryRegionFinish/FoundryRegionView: central 2x2 seal flags, crosswise staggered 1x2 gate-approach flags, and darker 1x1 workshop stones. General court remains staggered 2x1, with half flags at zone ends. Existing shared texture, static lighting, and batches retained. No walking-union, collision, scene, gameplay, camera, or raster asset change. Includes the prior uninstalled arch masonry slice. Static budget 17 renderers / 51,780 vertices (previous 51,348); atmosphere separately 1 / 780.

Actual five-waypoint Unity graphics traversal passed. Reviewed overview and center/approach/workshop portraits; comparison in ArtDirection/2026-09-29/foundry-paving-01. Compile 0 warnings/errors; integrity 514 project GUIDs / 1,099 package GUIDs / 568 scene objects/components. Full gate EditMode 405/405, PlayMode 130/130, Android exit 0. APK actual size 39,340,606 bytes, local build 2026-09-29 16:43:29, SHA256 EED797DA0FFA60A586E6523759A3261A6F35B6B53F91455DFBABB484C68C5BFB. READY, NOT INSTALLED. Last verified installed remains 18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535. No phone actions, new device measurements, helper agents, commit, or push. Reference-quality art is still in progress.
## 2026-09-29 - Gate finials and boiler shoulder braces

Added two supported gate finials with cast sockets, bronze collars, and tapered tips, plus two visible roof-mounted shoulder braces on each of four boiler chimneys. Existing mouth/smoke anchors, painted facade, footprints, and static iron batches retained. Capture-only gate framing expanded; baseline recaptured with the same new review framing before runtime edits. Gameplay camera unchanged. Root reviewed matched gate/furnace details, overview, and gate portrait. Finials sit above the close-follow gate portrait; their main impact is the wide silhouette. Proof: ArtDirection/2026-09-29/foundry-crowns-01.

Static 17 renderers / 52,556 vertices (previous 51,780); atmosphere separately 1 / 780. Graphics traversal 1/1 passed, compile 0 warnings/errors, integrity 514/1099/568. Full gate EditMode 405/405, PlayMode 130/130, Android build exit 0. APK actual size 39,340,606 bytes, local build 2026-09-29 16:57:43, SHA256 6DBD8583D538C3C77458F1DBFA8823D20146FEDF3D5E0C284E241BE373158EC8. READY, NOT INSTALLED. Includes the prior uninstalled arch and paving slices. Last verified installed remains 18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535. No phone actions, save changes, helper agents, commit, or push. Reference-quality art remains in progress.
## 2026-09-29 - Combined material, architecture, and atmosphere package

Owner requested several art tasks in one delivery. Integrated: unchanged generated honed basalt v2 (versioned source and Assets file, old v1 retained), 8 gate tower return louvre panels, iron roof aprons on 4 boilers, irregular soft smoke outlines with source-to-upper colour transition. Final review lifted panel edges and smoke visibility. Two implementation helpers plus one independent reviewer used (maximum 3). Final actual Unity images accepted with no blocking placement/readability issue. Smoke remains subtle; device motion/performance not claimed. Proof/source/prompt: ArtDirection/2026-09-29/foundry-integrated-01/README.md and PROMPTS.md. No raster editing; built-in image_gen source SHA256 63ED64CB8CFA2A6C450ED6EEE2C13B3869E4DD18D7414F6479DAE4DC89049FBA.

Static 17 renderers / 52,908 vertices; atmosphere 1 / 780 with 288 cached radius floats and existing 30 Hz/pause/ownership behavior. No walking-union, collision, scene, gameplay, or playable camera edits. Graphics traversal 1/1, compile 0 warnings/errors, integrity 515/1099/568. Full gate: EditMode 405/405, PlayMode 130/130, Android exit 0. APK actual size 39,340,606 bytes, local build 2026-09-29 17:21:08, SHA256 FD05B1749FB79D9E1EF7F992DBB61BF0B9FB83CA6E253FD9B74DC938D9F32BEC. READY, NOT INSTALLED. Includes prior uninstalled arches, paving, and crowns. Last verified installed remains 18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535. No phone actions, save edits, commit, or push. Named combined package complete; final reference-image quality remains open.
## 2026-09-29 - Combined foundry service, shaft hardware and floor-light pass

Completed in one visual package: supported north-shelf quenching basin, reserve billets and hollow strip coils; 32 recessed-looking shaft coping trays and three chain clevis/saddle connections; subtle gate/boiler contact shade, worn approach and outer bridge torch pools. Bridge floor is subdivided across its width for localized lighting. Reuses existing materials/textures and static batches; no gameplay, walkable bounds, collision or balance change.

Evidence: ArtDirection/2026-09-29/foundry-service-01/index.html and README.md. Before/after and closeups are real Unity captures. Graphics capture 1/1 passed; 405 EditMode + 130 PlayMode passed; static integrity 515 project GUIDs / 1099 package GUIDs / 568 scene objects/components passed. Final static budget 17 renderers / 54,840 vertices (+1,932 from prior package); unchanged separate atmosphere renderer. Three helpers covered workshop, shaft hardware and visual review. No blocking visual defect found; reference-image fidelity is still a work in progress.

Android build succeeded. APK Builds/Android/Cryptforge-Prototype.apk: 39,340,606 bytes, 2026-09-29 17:45:55 local; SHA-256 DFF2E84065DA0CCA3B80C8580B89814C30107D2AC889B5514D3E6176ED62529D. NOT installed or device-tested in this session. Last known installed phone APK remains 18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535; do not infer current device state.
## 2026-09-29 - Combined perimeter and service hardware finish

Eight exterior posts received stepped feet/collars, tapered stone crowns, bronze terminals and inset panels. Existing exterior pipe runs received four paired bolted unions and two valve bodies. Gate apron received eight flush metal wear-strip frames. Initial dark threshold plates were rejected in graphics review because they resembled holes; final strips leave stone visible and reuse the coping batch. Modified FoundryRegionSite, FoundryRegionDressing and FoundryRegionArchitecture only. No new bitmap/material, gameplay, collision, walking union, scene, camera or per-frame work. No helper agents used.

Actual Unity five-position capture passed 1/1; gate/furnace/undercroft/portrait/full-map reviewed. Static budget 17 renderers / 56,816 vertices (+1,976); existing atmosphere 1 / 780 separately, unchanged. Compile check passed 0 warnings/errors before final threshold adjustment; final Unity gate passed EditMode405/405, PlayMode130/130, Android exit0. Integrity515/1099/568 passed. Preview references11/11 resolved. Proof ArtDirection/2026-09-29/foundry-perimeter-finish-01/index.html and README.md.

APK Builds/Android/Cryptforge-Prototype.apk: actual39,340,606bytes, localtime2026-09-29 18:02:11, SHA2561A55731F755FC6C42D72392474DBBC2E7A696D243300AC47E0227E19AFF3386A. READY NOT INSTALLED. Includes previous uninstalled service/material/architecture changes. Last verified installed remains18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535. No device operations, save edits, new phone performance claims, commit or push. Reference-image fidelity remains in progress.
## 2026-09-29 - Combined court stonework finish

Added a flush 24-flag stone annulus beneath the existing central engraved seal (radius3.97, inside the +/-4 inset), restrained gate-road shoulder shading, sparse in-slab corner repairs, softer joints and reduced slab value variation. FoundryRegionFinish and FoundryRegionView only; existing paving batch and continuous UVs reused. No new material/texture, per-frame behavior, walking/collision, scene, gameplay or camera edits. No helpers used.

Actual Unity five-position capture1/1 passed; center combat and gate views reviewed. Compile0warnings/errors, integrity515/1099/568, full EditMode405/405 and PlayMode130/130 passed, Android exit0. Static17renderers/57,432vertices (+616), atmosphere1/780 unchanged. Preview HTTP200 and7/7 local references verified. Before/after and production notes: ArtDirection/2026-09-29/foundry-court-finish-01.

APK actual39,340,606bytes, localtime2026-09-29 19:11:24, SHA256103811AA22B982D4363C509FA4BBA8EBD6ADD7DDD2ED3D5D7567355D70E5FCE3. READY NOT INSTALLED. Includes all prior uninstalled art packages. Last verified installed remains18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535. No phone operations, save edits, new device performance claims, commit or push. Reference-image fidelity remains unfinished.
## 2026-09-29 - Exterior post lanterns and warm floor pools

Four existing exterior post tips now carry enclosed iron lanterns with two visible window faces, opaque cage bars, supported caps and hipped roofs. Four matching static floor-light sources added. FoundryRegionSite and FoundryRegionFinish only; existing texture reused without editing. No gameplay, walking/collision, scene, camera, save, per-frame light or particle changes. No agents used.

First full gate caught a renderer regression: 18 static groups exceeded the existing <=17 test. Kept the test unchanged; split panes into inset windows around bars and reused the existing flame batch. Final actual Unity capture1/1 passed with 17renderers/57,560vertices (+128 from prior), atmosphere1/780 unchanged. Final closeup and overview reviewed; cap UVs corrected after dark first render. Final full gate EditMode405/405, PlayMode130/130, Android exit0. Earlier integrity515/1099/568 and compile0warnings/errors passed. Proof ArtDirection/2026-09-29/foundry-lanterns-01.

APK actual39,340,606bytes, localtime2026-09-29 19:36:46, SHA2561C4A1D885F27C7A5B2DFDA74FF16B990D24BC0193887DD27229C34C3EECA6688. READY NOT INSTALLED. Includes previous uninstalled art passes. Last verified installed remains18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535. No phone operations, save edits, new device performance claim, commit or push. Reference-image quality remains in progress.
## 2026-09-29 - Combined art installed and reviewed on S23

Owner confirmed phone free and authorized installation/review. Fresh backup TestResults/foundry-lanterns-install-20260929/before. adb install-r succeeded; installed base.apk SHA256 matches1C4A1D885F27C7A5B2DFDA74FF16B990D24BC0193887DD27229C34C3EECA6688. All7 profile/backup/development/telemetry files byte-identical immediately after install. Supersedes prior not-installed status for included art passes.

Physical review: center stone ring visible in combat; guarded southeast/northeast movement reached exterior lantern, warm paving, masonry and boiler/pipe details (07-lantern.png). Initial startup run ended in Slag Gate defeat,0gold banked; restarted through Try again. Left PAUSED in Ember Hall1/6,wave3/3,86HP,level0,XP3/10,0runGold,Staff/SecondWind. No upgrade, equipment purchase, save edit or restoration.

After review profile/backup,both development flags and rotated telemetry remain byte-identical. Profile revision215/32776gold/Staff/SecondWind/deepest3 unchanged. Current telemetry retained full preinstall prefix and appended3919bytes. Current-process log0matches for FATAL EXCEPTION,NullReferenceException,MissingReferenceException,InvalidOperationException. Short install/movement/art smoke only, not sustained performance or exhaustive traversal. Inputs/captures gated on focus,awake,no-keyguard; captures rechecked. Device image copied unchanged to ArtDirection/2026-09-29/foundry-lanterns-01/device-lantern.png. No code/build changes,commit,push. Reinspect fresh before further phone input.
## 2026-09-29 - Pressure reservoirs integrated and verified

Two exterior tanks at (22.3,-5.8)/(22.3,15.8), each on supported2x2 masonry feet, now connect existing boiler service mains. Added faceted metal hulls, retaining belts, tapered shoulders, relief stems/wheels and short wall-to-main branches. All feet outside walking union. FoundryRegionDressing only; existing materials/static batches. No gameplay, collision, scene, camera, per-frame work or new raster assets. No agents used.

First visual review caught belt caps painting over the upper hull; corrected bottom-to-top course order. Near tank now emits after pipes, far tank before; branches begin on tank wall. Final actual five-point Unity capture1/1 passed; furnace closeup/overview reviewed. Static17renderers/59,584vertices (+2,024), atmosphere1/780 unchanged. Full gate EditMode405/405,PlayMode130/130,Android exit0. Compile/integrity earlier passed0warnings/errors and515/1099/568. Preview HTTP200,7/7 local links. Proof ArtDirection/2026-09-29/foundry-reservoirs-01.

APK actual39,340,606bytes, localtime2026-09-29 20:15:24, SHA25679B4C59EE5E4D7AA44BA53663228BA9D198BD7920DCCD1F004B7F0CDB7E62DC1. READY NOT INSTALLED. Last verified installed phone package1C4A1D885F27C7A5B2DFDA74FF16B990D24BC0193887DD27229C34C3EECA6688 remains; prior paused phone snapshot is historical until reinspected. No device operations/save edits/commit/push. Reference-image fidelity remains unfinished.
## 2026-09-29 - Reservoir package installed on S23

Owner explicitly authorized restarting the advanced Forge-choice run (40HP,5unbanked gold). Fresh backups under TestResults/foundry-reservoirs-install-20260929/before. adb install-r Success; installed base.apk SHA25679B4C59EE5E4D7AA44BA53663228BA9D198BD7920DCCD1F004B7F0CDB7E62DC1 matches local. All7 saved files byte-identical immediately after install. Supersedes pending installation above.

Startup run ended in Slag Gate defeat with0banked gold. Restarted through Try again; guarded northeast drags reached the boiler/tank group. Actual08-tank-detail.png confirms near reservoir hull, base, pipe connections and surrounding furnace art on S23. Movement/camera follow worked. A visual limitation is especially apparent here: foreground actor/ring rendering can make the hero look on top of projected boiler/pipe silhouettes while the walkable bounds remain unchanged. Record this as the next depth/readability issue; it was not fixed or claimed accepted in this device-only turn.

Left PAUSED in Cinder Walk2/6,wave1/3,77HP,level0,XP3/10,0runGold,Staff/SecondWind. Profile revision215/32776gold/deepest3 unchanged; profile,backup,two development flags,rotated telemetry byte-identical after review. Active telemetry retains exact prior prefix. Current-process log0matches for FATAL EXCEPTION/NullReferenceException/MissingReferenceException/InvalidOperationException. No save restore/edit/equipment purchase. Short art/movement smoke, not sustained performance or full-map acceptance. All input/capture gated by focus/awake/no-keyguard; post-capture focus rechecked. Evidence copied unchanged to ArtDirection/2026-09-29/foundry-reservoirs-01/device-tank.png. No code/build change,commit,push. Reinspect before future phone actions.
## 2026-09-29 - Foundry ground readability and hero cutaway

The foundry ability range ring and pooled ability/Staff pulses now use the arena ground-effect sorting rank (-13), instead of painting above every structure. Ordinary halls retain their prior effect rank. FoundryVisibilityView and the Resources FoundryCutaway shader reveal the existing floor through above-floor architecture in a soft ellipse around the living hero. Scene-owned material variants reuse source textures, update one uniform per variant, disable cleanly, and are destroyed on reload; shared imported materials survive. Static mesh construction and batches remain unchanged. This is a hero-centred readability cutaway, not physical depth sorting; distant enemies still render above structures.

Matched actual Unity captures at walkable design point (22,0), traversal portraits and full-map review: ArtDirection/2026-09-29/foundry-ground-readability-01. Both matched images use the new ring sorting; only cutaway enable differs. Five waypoint traversal1/1 passed. Static17renderers/59,584vertices and separate atmosphere1/780 unchanged. Gameplay, movement/collision, balance, scene, camera and save rules unchanged.

Targeted first test caught the old assertion that every material is borrowed; updated ownership checks require new variants to be destroyed and imported assets to survive. Final full gate EditMode405/405,PlayMode130/130,Android exit0. Final compile0warnings/errors; integrity518projectGUIDs/1099packageGUIDs/568sceneobjects passed. Proof HTTP200/local links resolved. Added checks cover cutaway focus/disable/reload ownership, grounded ring SortingGroup and real ability-event pulse rank.

APK Builds/Android/Cryptforge-Prototype.apk: actual39,340,731bytes, local2026-09-29 20:55:31, SHA25644566462D0203760FF3EA8B918175EEE553636DF6F35E7FDB565BA3E32780288. READY NOT INSTALLED. Last verified installed remains79B4C59EE5E4D7AA44BA53663228BA9D198BD7920DCCD1F004B7F0CDB7E62DC1. Device read-only check found S23 awake/unlocked at launcher; asked whether it is free, no reply yet. No phone inputs/install/save writes or device-performance claims. New shader cost and appearance on Android remain to be tested. No helper agents, commit or push.
## 2026-09-29 - Ground-readability package installed on S23

Owner said to continue after the pending installation question. Fresh guarded screenshot showed the same paused Cinder Walk run (77HP,0runGold). Backed up profile/backup/development flags/telemetry before adb install-r. Success; installed base.apk SHA25644566462D0203760FF3EA8B918175EEE553636DF6F35E7FDB565BA3E32780288 matches tested local build. All7 saved files byte-identical immediately after installation. Supersedes READY NOT INSTALLED above.

Actual phone inspection reached near boilers/reservoirs through controlled northeast drags. Cutaway renders on S23 and reveals floor around feet; range ring remains beneath solid architecture. Chose Long Reach at first level-up to continue traversal. Short live movement/combat smoke only, not sustained performance acceptance. Remote-enemy occlusion remains open. Current-process log: no FATAL EXCEPTION, NullReferenceException, MissingReferenceException, InvalidOperationException or shader error/unsupported matches.

Left PAUSED room3/6,wave2/2,28HP,level1,XP16/22,5at-risk gold,Staff/SecondWind. Profile revision215/32776gold/deepest3 unchanged; backup,development flags and rotated logs unchanged. Active telemetry preserves pre-test prefix. No profile restore/purchase. Evidence TestResults/foundry-visibility-install-20260929; unchanged actual screenshot at ArtDirection/2026-09-29/foundry-ground-readability-01/device.png. Inspect fresh before future phone inputs. No new code/build/commit/push this installation turn.

## 2026-09-30 - Foundry court floor installed on S23

Fresh read-only check first: S23 awake, unlocked, launcher focused, game not running. One launch showed a fresh Ember Hall run, which was sent Home at once to pause it. Backed up profile, profile backup, development flags and telemetry to `TestResults/foundry-floor-install-20260930`, then `adb install -r` of `Builds/Android/Cryptforge-Prototype.apk` (39,340,731 bytes, SHA-256 59892F56D5BDE8A82A3514AC92A4D49A9897505E175878D0E327F93EAAE964EB): Success. Profile and backup SHA-256 identical before and after installation. Focus was checked before and after the one screenshot (`ArtDirection/2026-09-30/foundry-floor-01/device.png`, court start); the new flags and compass render on device as in Unity. The run was then sent Home to pause. Short visual check only, not a performance measurement.

## 2026-09-30 - Foundry depth ordering and landmark mass installed on S23

Fresh read-only check: S23 awake, no keyguard, launcher focused, game not running. Backed up profile, profile backup, development flags and telemetry to `TestResults/foundry-depth-mass-install-20260930`, then `adb install -r` of `Builds/Android/Cryptforge-Prototype.apk` (39,340,731 bytes, SHA-256 AFB6E0079767DF61EB7C97869873F19FF2A288078EF7CF0FF09331CB0B160EC4): Success; installed base.apk hash matches. Profile and backup SHA-256 identical before and after installation (d3645ca6..., e82c127c...). One launch went straight into a live Ember Hall court run with enemies attacking (81/100 HP at capture); focus checked before and after the one screenshot (`ArtDirection/2026-09-30/foundry-depth-mass-01/device.png`), then Home to pause; profile hash unchanged afterwards. Logcat 0 matches for FATAL EXCEPTION / NullReferenceException / MissingReferenceException / InvalidOperationException / shader error. The screenshot shows the court only: structure-over-actor occlusion and the new boilers were not yet seen on the phone. Short visual check, not a performance measurement.

## 2026-09-30 - Hidden enemy silhouettes installed on S23

Fresh read-only check: S23 awake, no keyguard, launcher focused; the game process held only the earlier paused test run, which the region-preview flag does not persist (profile last written 2026-09-28). Backed up profile, backup, development flags and telemetry to `TestResults/foundry-xray-install-20260930`; `adb install -r` of APK 39,340,240 bytes, SHA-256 BE991765AE0BB13AE6B72BF416A78379F1BD71EDE02353F6F633E5303AB6ED86: Success, installed base.apk hash matches, profile and backup unchanged by the installation. Then a short live run with focus-gated drags and taps (one level-up choice, Tempered Edge; one forge choice, Mend) to the south-west corner of the boiler compound; `ArtDirection/2026-09-30/foundry-xray-01/device.png` shows the new boilers, balustrade and lantern post on the phone. No enemy passed behind a structure during the captures, so the silhouette was NOT observed on device. The run ended in defeat to the Forge Warden (Warden's Crucible), which banked 3 gold through the normal save: profile revision 215 -> 216, gold 32,776 -> 32,779, previous profile rotated into the backup; not restored, as it is newer progress. App sent Home afterwards. Logcat 0 matches for FATAL EXCEPTION / NullReferenceException / MissingReferenceException / InvalidOperationException / shader error. Not a performance measurement.

## 2026-09-30 - Foundry rim trim built, NOT installed

APK `Builds/Android/Cryptforge-Prototype.apk` 39,340,240 bytes, SHA-256 DDBD7DAA49E8E4C7AA3D882824EB4F45A43AFD9648702E5034F53464CADEB9A0. READY NOT INSTALLED. Read-only check found the game in the foreground with a run that was not the test run (Room 4/6 The Forge, level 3, 21 run gold, forge choice open), so the owner appears to be playing: no install, no input; one focus-gated screenshot only. Last verified installed build remains BE991765AE0BB13AE6B72BF416A78379F1BD71EDE02353F6F633E5303AB6ED86.

## 2026-09-30 - Foundry parallel pass built, NOT installed

APK 39,339,970 bytes, SHA-256 C1E500CD0301A2AACEECA4588F611D1972443B4FC98A8F7D446CDC1A2D7CECBE. READY NOT INSTALLED. Build report: GateFacade 2048x1024 ASTC 1.2 MB, textures 20.5 MB total. Read-only check: S23 dozing with keyguard, the game process from the owner's open run still alive; no install and no input, since installing would end that run. Last verified installed build remains BE991765AE0BB13AE6B72BF416A78379F1BD71EDE02353F6F633E5303AB6ED86 (the rim build DDBD7DAA... was never installed either).

## 2026-09-30 - Foundry chest/light pass built, NOT installed

APK 39,339,790 bytes, SHA-256 A18311F4904C9190488C6957A320BF707E67D6F3BD0C043368F032D059CD0BA0. READY NOT INSTALLED. Build report: FurnaceFacade and GateFacade 1.2 MB each (ASTC), textures 18.1 MB. Read-only check: S23 awake with the notification shade focused and the owner's game process still alive, so the phone was in use; no install, no input. Last verified installed build remains BE991765AE0BB13AE6B72BF416A78379F1BD71EDE02353F6F633E5303AB6ED86.

## 2026-09-30 - Forged UI panel import built, NOT installed

APK 39,339,430 bytes, SHA-256 CB66934314CD7CC7079E857236F752525A2CC28BE2F9D72ABA0118C357E339CE. READY NOT INSTALLED. Read-only check: S23 dozing with keyguard; the owner's game process still alive; profile.json and its backup were rewritten at 16:17-16:18 by the owner's own play (not by this session). No wake, input, screenshot or install. Last verified installed build remains BE991765AE0BB13AE6B72BF416A78379F1BD71EDE02353F6F633E5303AB6ED86; four newer builds are pending, the latest being this one.

## 2026-09-30 - Foundry art packages installed on S23

Owner said the phone was free. Fresh check: awake, no keyguard, game in the foreground; backups in `TestResults/foundry-final-install-20260930` (profile revision 218, 33,082 gold). The pre-install screenshot showed an open owner run (Floor 3, level 12, 303 unbanked gold, level-up choice); `adb install -r` ended it and its unbanked gold was lost; the saved profile was not affected. APK 39,339,430 bytes, SHA-256 CB66934314CD7CC7079E857236F752525A2CC28BE2F9D72ABA0118C357E339CE: Success, installed base.apk hash matches; profile and backup hashes identical before, after installation and after the checks. Seen on device: brass star seal, forge light and cool court, forged panel at 1024 ASTC 4x4 with continuous gold lines, boiler/balustrade corner. Two short runs lost to a Tank in Slag Gate, 0 gold banked. Not seen: any actor behind a structure, so the silhouettes remain verified in Unity only; no false silhouettes in the open court. Logcat 0 exception/shader-error matches. App sent Home. Proof `ArtDirection/2026-09-30/foundry-device-01/`. Supersedes the READY NOT INSTALLED entries above.

## 2026-09-30 - Foundry effect/light pass built, NOT installed

APK 39,339,430 bytes, SHA-256 DEFA17127136C731A90059BCF016C33911AFA0E0C42B6CCEFC9C87F797AE43B5. READY NOT INSTALLED. Read-only check and one focus-gated screenshot: the owner was in a run (Floor 2 room 4/6, level 7, 116 unbanked gold, forge choice open; profile last written 18:23 by that play). Installation deferred until the owner confirms the run can be ended or has finished. Installed build remains CB66934314CD7CC7079E857236F752525A2CC28BE2F9D72ABA0118C357E339CE.

## 2026-09-30 - Foundry effect/light pass installed on S23

The owner said the run was over; the fresh check showed another open run (Floor 3, level 13, 303 unbanked gold), and the owner chose "install now". Backups in `TestResults/foundry-fx-install-20260930` (profile revision 221, 33,370 gold). `adb install -r` of APK DEFA17127136C731A90059BCF016C33911AFA0E0C42B6CCEFC9C87F797AE43B5: Success, installed base.apk hash matches; profile and backup identical before and after. One launch, one short drag toward the pit: forge-orange pools around the pit and a cool court on the phone (`ArtDirection/2026-09-30/foundry-fx-light-01/device-*.png`). Logcat 0 exception/shader-error matches. App sent Home. Supersedes the NOT INSTALLED entry above.

## 2026-09-30 - Silhouettes seen on S23; first render stats

Fresh check: game not running, no open run. Backups in `TestResults/foundry-proof-install-20260930` (profile revision 221, 33,370 gold). `adb install -r` of APK 32,305,422 bytes, SHA-256 2B2EFFCDB8544F69A61594D1A27C54C122C1613B67EF0DB5CBC20F26276CCF76: Success, hash matches (smaller only through Gradle incremental-packaging dead space in earlier APKs; contents equal apart from code). With `development/depth-proof.txt` and `render-stats.txt` pushed: the covered parts of a Grunt, the chest and a drop behind the south-west boiler drew as ember/gold silhouettes on the S23 (Vulkan, Adreno 740), matching Unity. Render stats: proof scene 59.9 fps, GPU 2.2-3.2 ms, 50 draws; a normal run fighting ~45 s 59.9 fps, p95 16.7 ms, GPU 2.9-3.2 ms, draws 50-98, 127 MB used. Not a sustained/thermal test. Both flags removed afterwards; profile unchanged; test run left paused in Slag Gate at 16 HP; logcat clean. Proof `ArtDirection/2026-09-30/foundry-device-proof-01/`.

## 2026-09-30 - Balustrade/review build installed on S23 (not yet viewed)

Fresh check: the owner was in another app (Instagram) and the game was not running, so no run could be lost. Backups in `TestResults/foundry-rail-install-20260930` (profile revision 221). `adb install -r` of APK 37,011,610 bytes, SHA-256 14612D369B9334EB6F479D54F916A546343697A1A006A21BAB07A0F172462A29: Success, hash matches, profile and backup unchanged; the owner's app stayed in front. No launch, input or screenshot: the depth-proof look (balustrade, hidden/visible sparks) waits until the owner hands over the phone.

## 2026-09-30 - Balustrade and effect depth seen on S23

Owner handed over the phone (launcher, game not running). Pushed `development/depth-proof.txt`, launched, four focus-gated screenshots: the new stone balustrade reads with its balusters; the hidden Grunt, chest and drop show as silhouettes behind the south-west boiler; hit flashes/sparks appear on the Grunt's uncovered part and on the Mite in the open court, and nothing is drawn over the boiler where it covers the Grunt - effect depth confirmed on device. Flag removed, app force-stopped (the proof run banks nothing); profile and backup unchanged; logcat 0 exception/shader-error matches. Proof `ArtDirection/2026-09-30/foundry-rail-01/device-proof*.png`.

## 2026-09-30 - Foundry reference pass installed on S23

Owner said the phone was free; fresh check: Chrome in front, game not running. Backups in `TestResults/foundry-refpass-install-20260930` (profile revision 221). `adb install -r` of APK 37,022,536 bytes, SHA-256 8F4C2C76A123C4E22293B4DF610BA0B224B3F78367F526423197CA1B90451F42: Success, hash matches; profile and backup unchanged. One launch, one short drag: violet-grey court with lit brass compass/lozenges and the bridge-head brazier seen on device. Logcat 0 exception/shader-error matches; app sent Home; development folder unchanged. Proof `ArtDirection/2026-09-30/foundry-reference-pass-01/device-*.png`.

## 2026-10-01 - Foundry night pass installed on S23

Owner reconnected the phone and asked to continue. Fresh check: launcher in front, game not running. Backups in `TestResults/foundry-night-install-20261001` (profile revision 221). `adb install -r` of APK 32,374,835 bytes, SHA-256 81299787DDBDEFEE455560EF17F8102873C1CCC931787417A7B14A687487497D: Success, hash matches; profile and backup unchanged. With `development/render-stats.txt`: one launch, two short drags (ashlar court, rounded pit collar seen); 59.9 fps, GPU 3.0-3.3 ms, 52-81 draws, 61k triangles, 128 MB. Flag removed, app stopped; logcat 0 exception/shader-error matches. Proof `ArtDirection/2026-10-01/foundry-night-01/`.

## 2026-10-01 - Foundry day pass installed on S23 (not viewed)

Fresh check found the game in front with a paused owner run (Floor 1 room 3/6, level 3, 11 unbanked gold); the owner chose "install now". Backups in `TestResults/foundry-day-install-20261001` (profile revision 221). `adb install -r` of APK 37,158,301 bytes, SHA-256 F67765262EB7CBA3FE53285DB4288059375BD813E652B389891E04A7D1B67E67: Success, hash matches; profile and backup unchanged. After the launch the keyguard came up, so the guarded drags and screenshots were skipped (one focus-only screenshot taken by mistake while the keyguard showed was deleted unused). Logcat 0 exception/shader-error matches. The new pit and bridge lanterns are not yet seen on the phone.

## 2026-10-04 - Pending foundry work completed

Finished the inherited pit atmosphere (20 rising sparks, heat wisps and glow), gate-brazier support checks, bridge coping/kerb corrections, arch-before-pier ordering, screened anchor plates and framed molten discharge. Extended the west bridge paving margin to d=-4 for s=-32..-24, covering the bare joint bed after the kerb ends. No gameplay, movement bounds, save or balance changes. Proof: ArtDirection/2026-10-04/foundry-completion-01 (actual Unity before/after).

Graphics capture1/1; .NET390/390; compile0warnings/errors; EditMode410/410; PlayMode134/134; Android exit0. Static17renderers/73,244vertices; atmosphere832vertices. Integrity526/1099/568 checked while gate was in progress. APK37,175,012bytes SHA25623BB5C7B4C2A51BA12A6609193B6D358E867270DD278A755486CFA2858ED0058. Built, NOT installed; latest known installed remains F6776526... from October1. Fresh read-only phone check found launcher foreground and game not running. No device inputs. New painted masters are being prepared separately; this package contains no texture changes. Commit locally only, no push.
## 2026-10-04 - Jointless painted court (P1-b)

Integrated generated FoundryPavingV3 on the existing paving material; geometric ashlar joints, bevels and calibrated .8 tint retained. Raw master and prompt preserved in ArtDirection/2026-10-04/painted-surfaces-02-masters. Matched proof: ArtDirection/2026-10-04/painted-paving-01. Graphics 1/1, EditMode 410/410, PlayMode 134/134, Android exit 0; integrity 526/1099/568. Static budget unchanged: 17 renderers, 73,244 vertices. Import uses POT rescale, 1024 max, mipmaps, Repeat and Android ASTC6x6; Android packed-assets report lists FoundryPavingV3 at 611.7 KB (compressed mip-chain footprint, not raw RGBA). APK 37,175,012 bytes, SHA256 C68DA0594EAE850A99CA45F3D6EA71BA2F2FA37EEB4AC6DE48774C27FD74C2B3. Built, not installed. No gameplay changes or push.
## 2026-10-04 - Painted foundation masonry (P3)

Integrated FoundryMasonryV2, preserving geometry and the original asset/GUID. Recalibrated the sampled masonry texel mean used by clutter so wood/cloth tones remain correct. Matched Unity proof: ArtDirection/2026-10-04/painted-masonry-01. Graphics 1/1; compile 0 warnings/errors; EditMode 410/410; PlayMode 134/134; Android exit 0; integrity 527/1099/568. Static 17 renderers / 73,244 vertices. Android build log now explicitly reports actual target textures: both P1-b and P3 are 1024x1024 ASTC_6x6, 11 mips. APK 32,374,835 bytes SHA256 7C72EA9421300E14655AAB8FE6EB9D8CB4D42818521C9CA497812127CA57F423. Built, not installed. USB phone no longer appears in adb devices; owner was asked to reconnect while desktop work continues. No push.
## 2026-10-04 - Dedicated painted pit lining (P4)

Integrated FoundryPitLining with its own shared material and preserved 17-renderer batching. Adapted UV/heat to the current molten pool rather than the obsolete pre-pool prompt-pack layout. Repeated facade windows removed; molten pool/discharge and actor depth preserved. Proof ArtDirection/2026-10-04/painted-pit-01. Graphics 1/1; compile 0 warnings/errors; integrity 529/1099/568; EditMode 410/410; PlayMode 134/134; Android exit 0. New shared-surface checks cover pit independence and POT dimensions. Static 17 / 73,244 vertices. Android actual 256x512 ASTC_6x6, 10 mips; packed texture 78.0 KB. APK 32,374,835 bytes SHA256 F3512B910F9AA85B14AAA2F769B74E3063AD99D017DCA82D452B4668BB10CF7E. Built, not installed (ADB device list empty). No push.
## 2026-10-05 - Painted boiler wrap (P5) and combined package complete

Integrated FoundryBoilerWrap on the existing court boiler slot; dedicated pit material unchanged. Matched actual Unity proof ArtDirection/2026-10-04/painted-boiler-01; combined reference/before/after page ArtDirection/2026-10-04/painted-foundry-01. Session started October 4; final gate completed after midnight October 5. Graphics 1/1; EditMode 410/410; PlayMode 134/134; Android exit 0; integrity 531/1099/568. Static budget remains 17 renderers / 73,244 vertices. Android actual formats: paving and masonry 1024x1024 ASTC_6x6 (11 mips), pit 256x512 ASTC_6x6 (10), boiler 512x512 ASTC_6x6 (10). Packed boiler 154.8 KB, pit 78.0 KB. Final APK 32,374,745 bytes SHA256 B6B4BFD0DCA089A0FF514767BC94E356CA58AEE68A1B9649A60F17C9810BE551.

Generated, integrated, captured and built; NOT installed at this record. Phone reappeared after disconnecting; another app is foreground and Cryptforge is not running. Asked owner whether phone is free before launching/testing. Read-only backup TestResults/painted-final-backup-20261005 includes profile, profile backup, development and telemetry; profile revision 221. No device inputs or flags changed. No push. All four raw masters and exact prompts/hashes preserved in painted-surfaces-02-masters; original imported assets/GUIDs retained. The target's full lighting/volume/detail fidelity is still future work; P2 deliberately skipped without adding a renderer.
## 2026-10-05 - Painted package installed before surface-weathering work

Owner confirmed the phone was free. Installed B6B4BFD0DCA089A0FF514767BC94E356CA58AEE68A1B9649A60F17C9810BE551 via adb install -r: Success. Fresh backup TestResults/painted-install-20261005 (profile revision221); profile and backup SHA256 unchanged immediately after install. Ordinary launch reached the Forge selection; then a separate development depth-proof baseline was used to see the boiler, floor, stone and actor occlusion unobstructed. Guarded device-before.png in ArtDirection/2026-10-05/foundry-weathering-01 shows the painted package. Baseline steady windows: 59.9 fps, GPU3.7ms,57draws,62.0ktriangles,127.3MB. The brief notification-panel interruption caused a guard failure: no capture or tap sent until game focus was restored. Depth-proof/render-stats flags are temporary for the ongoing paired test; remove after the final package review. No profile restore.
## 2026-10-05 - Architectural weathering package verified and installed

Combined work: static vertical masonry deposits, localized ledge shade, height-aware fire spill on stone faces/caps, soot around iron roof/vent centres, and variable rubbed paving bevels. Existing UVs and depth ownership retained; no gameplay, collision, movement bounds, profile logic, telemetry logic or energy changes. A small vertical subdivision resolves the shallow ledge shadow: static budget 17 renderers / 76,420 vertices (+3,176, 4.3%). All treatment runs at map construction, with no new material/texture or per-frame update.

Proof: ArtDirection/2026-10-05/foundry-weathering-01 (matched Unity before/after, matched device boiler views, device walk and raw RenderStats lines). Compile 0 warnings/errors; graphics capture 1/1; EditMode 410/410; PlayMode 135/135 including the new height-falloff/ledge check; Android exit0; integrity532/1099/568; diff check clean. APK32,374,745 bytes SHA256 AFEEE1AF9D8065F240DD15C5AAF4BD96EA51D3753320208E3001B57FF8763E40.

Installed on S23 after owner confirmed it was free; device base.apk hash equals that APK. Fresh backup TestResults/weathering-install-20261005. Profile and profile.json.bak hashes match before install, after install and after the test (revision221); no older backup restored. Guarded before/after and 3.5-second leftward drag confirm rendered surfaces, camera following and the existing enemy/loot x-ray. Stable matched depth-proof view: previous59.9fps/GPU3.7ms/57draws/62.0ktriangles/127.3MB; new59.9fps/GPU3.7-3.8ms/57draws/63.8ktriangles/~127.6MB. This is an S23 static depth-review arrangement, not a combat-load or midrange-device benchmark. Current-process Unity exception/shader-error matches0.

App stopped after review. Both temporary depth-proof.txt and render-stats.txt removed. Remaining development files match the pre-test filenames and hashes exactly (region-preview.txt and start-floor.txt.off). Ordinary foundry preview is ready on next launch. Generated, integrated, built, installed and seen on device are now all complete for the painted package and this weathering pass. Local commit only; no push.
## 2026-10-05 - Gate and site finish verified on S23

Gate/boiler tier lighting now uses actual elevation and the shared architecture sources; bridge/court stone follows the same treatment. Added compact gate header ties and inset mullions, retaining existing depth scope, materials and geometry footprints. Proof: ArtDirection/2026-10-05/foundry-gate-finish-01 (matched Unity views, device static views and two-drag approach). Static17 renderers/76,492 vertices (+72). Compile0/0,graphics1/1,Edit410/410,Play135/135,Android0,integrity532/1099/568. APK32,374,745 bytes SHA256 76E4B83DFD613A939EDF5DD03899832181D9822FBB5A64769B7E93A99863FDB4, installed base.apk hash matched. Static S23 before/after59.9fps,GPU3.7ms,57draws; no current-process Unity exceptions/shader errors. Not a combat or midrange benchmark. Fresh backup TestResults/gate-install-20261005; profile and backup hashes unchanged (revision221), temporary flags removed, original development files hash-matched. Game stopped, ordinary preview ready next launch. No push. See proof README for limits and full evidence.
## 2026-10-05 - Masonry V3 verified and installed

Built-in generated painted masonry V3 now drives the existing stone material: slate/charcoal/warm-grey blocks, mineral facets, chips, restrained cracks/runoff. Original V2 preserved; P3b import stage records exact prompt/raw master. Clutter patch mean recalibrated. Proof ArtDirection/2026-10-05/masonry-variety-01. Static17/76,492 unchanged; compile0/0,graphics1/1,Edit410/410,Play135/135,Android0,integrity533/1099/568. Android imported1024x1024 ASTC6x6/11mips. APK32,374,745 bytes SHA2560D719910B89CAEF4590EFF4CCA102C563D86B941C52EC26D5B49E2795966D8F6; installed S23 hash matched. Matched static review59.9fps/GPU3.7-3.8ms/57draws, guarded walk seen, Unity error matches0. Not a combat/midrange benchmark. Profile/backup hashes unchanged, temporary flags removed, original development names/hashes matched; app stopped. No push. See proof README for exact generation provenance and evidence.
## 2026-10-05 - Forged metal V2 build verified, device pending

P6 built-in image_gen edit/refinement preserves border/rivet atlas locations and replaces stone-like damage with a calmer forged iron field. Sibling FoundryIronV2 feeds existing MAT_FoundryIron; original retained, shared clutter patch mean recalibrated. Proof/prompt/master: ArtDirection/2026-10-05/forged-metal-01. No geometry/gameplay/depth changes. Static17/76,492. Compile0/0,graphics1/1,Edit410/410,Play135/135,Android0,integrity534/1099/568. Actual imported1024ASTC6x6/11mips,packed611.7KB. APK32,374,745 bytes SHA2564E8ED3A10C2215EA4AA61AE795A1E4985BEE8D5BBAED68C48A1739DF073353CA.

NOT installed/seen on device. Phone in another app; availability question unanswered at closure. Read-only backup TestResults/iron-install-20261005; no app/input/flags/install changes. Latest installed remains masonry V3; do not report its59.9fps as an iron V2 measurement. Next authorized device step: recheck state/back up, install, guarded capture/walk/stats, clean only test-owned flags. No push.
## 2026-10-05 - Forged metal V2 device closure

Supersedes pending install. Owner continuation, phone launcher/unlocked/no game process. Fresh backup TestResults/iron-device-closure-20261005 preserves revision224. Installed verified4E8ED3A10C2215EA4AA61AE795A1E4985BEE8D5BBAED68C48A1739DF073353CA; base.apk hash matched. Guarded before/after and3-second walk seen, current-process Unity error matches0. Same initial facing/view59.9fps/GPU3.8ms/57draws/63.8ktriangles/~127.5-127.6MB; initial different-facing58draw sample repeated, retained under TestResults. Static depth-proof review only. Profile/backup unchanged by hash; no restore. Test flags removed, original development names/hashes matched; app stopped. Proof ArtDirection/2026-10-05/forged-metal-01. Prior545-test gate applies to this exact unchanged APK. No push.
## 2026-10-05 - Workshop props built; phone locked

Combined crate/board/brace, barrel/stave/hoop/lid and sack/fold/tie geometry completed. Proof ArtDirection/2026-10-05/workshop-props-01. Static17 renderers/78,232 vertices (+1,740). Compile0/0,graphics1/1,Edit410/410,Play135/135,Android0,integrity534/1099/568. APK32,374,745 bytes SHA2563051F09D7E7E1932CC4F35D2487EB0CC04E82EEA10FAC06DBEB2C90AABA86050.

NOT installed: phone keyguard showing, unlock request pending. Read-only state queries only; no input, install, app launch or development-flag mutation. Latest installed remains forged-metal-01 (4E8ED3A10C2215EA4AA61AE795A1E4985BEE8D5BBAED68C48A1739DF073353CA). No new device performance claim. Next step after unlock: fresh backup/current-state inspection, install this verified APK, guarded static capture and walk, compare profile hashes, remove only test-owned flags and verify original development files. No push.
## 2026-10-05 - Workshop props device closure

Owner authorised install. Fresh backup TestResults/workshop-device-20261005. APK3051F09D7E7E1932CC4F35D2487EB0CC04E82EEA10FAC06DBEB2C90AABA86050 installed and base.apk hash matched. Guarded static capture and 3-second walk inspected, current-process Unity error matches0. Matched S23 static review:59.9fps/GPU3.8ms/57draws;65.6k triangles versus63.8k before. Not a combat/midrange benchmark. Profile and backup unchanged by hash; original two development files unchanged, test flags removed; app stopped. Proof workshop-props-01 now includes device evidence. Supersedes pending-device status. No push.
## 2026-10-05 - Paving contrast integrated and installed

Court/apron joint-bed tone .50->.66, lit bevel1.10->1.055 and shadow bevel.74->.86 soften the dark grid seen during the workshop device review. Seal inset/setts keep their existing contrast. Colour-only change in FoundryRegionFinish: same geometry, UVs, light sampling, materials, gameplay and walking union. Static17/78,232 unchanged. Proof ArtDirection/2026-10-05/paving-softness-01 contains matched Unity views and guarded phone capture/walk.

Compile0/0,graphics1/1,Edit410/410,Play135/135,Android0,integrity534/1099/568. APK32,374,745 bytes SHA2562AE2ACAC31F1D126CCE099F176922C24444575FDCBD851FF6D428239A9F165AC installed and base.apk hash matched. Matched S23 static59.9fps/GPU3.8ms/57draws/65.6ktriangles. No current-process Unity exceptions/shader errors. Not a combat or midrange benchmark. The capture includes the depth-proof actors' periodic white hit flash; this is not a texture change.

Fresh backup TestResults/paving-device-20261005. Profile/bak unchanged by hash; original development files unchanged, test flags removed, app stopped, normal preview ready next launch. No push. This corrects floor emphasis; it does not close the broader reference-image fidelity gap.
## 2026-10-05 - Three-agent foundry identity package installed

Three explicitly requested agents edited disjoint files: gate tympanum/bevel frame/keystone (FoundryRegionArchitecture), boxed crane jib/sheave/coupling/weight seams/basket brace (FoundryRegionDressing), and worked anvil face/distinct tongs-hammer/tank ribs (FoundryRegionWorkshop). Parent integrated, compiled, captured, ran the full gate and device review. Static geometry/material batches retained:17 renderers,78,820 vertices (+588 from78,232). No gameplay/collision/camera/scene/new asset changes.

Proof ArtDirection/2026-10-05/foundry-identity-01 pairs actual Unity overview/gate/crane/workshop/pipe-shelf views plus phone start and gate approach. Owning agents reviewed captures read-only; no blocking visual regression. Anvil horn remains darker than the working face; fine details are subtle at overview scale. Existing portrait HUD partly hides the high gate pediment at close approach: full gate shape was checked in Unity, not falsely claimed fully visible on the phone. Crane/workshop closeups are Unity evidence, not device closeups.

Compile0/0,graphics1/1,Edit410/410,Play135/135,Android0,integrity534/1099/568. APK32,374,745 bytes SHA25646D65F0CA8A2007F87B26B749BE8EFF9014EF141121CE84E46CE3E7DD9BB077F installed and base.apk hash matched. Matched S23 static59.7-59.9fps/GPU3.8ms/57draws/66.2ktriangles. One observed sample reached33.4ms max; do not claim zero hitches. No current-process Unity exception/shader-error matches. Not a combat/midrange benchmark.

Fresh backup TestResults/identity-device-20261005. Guarded four-drag gate route recorded in proof/device-route.txt; timing yields slight position differences. Profile/bak and original two development files unchanged by hash; temporary flags removed, app stopped. Normal preview ready next launch. No push.
## 2026-10-05 - Three-agent structural package 02 installed

Three agents in disjoint files refined bridge support courses (Site), undercroft arch/pier face shading (Undercroft), and active pit hood collars/neck bands/straps (Massing). Dead legacy BuildShaft was identified and left untouched; pit work was reassigned to the actual BuildFurnacePit path. Parent inspected captures and ran integration. Static17renderers/80,316vertices (+1,496,+1.90%). Existing materials/depth ownership/walking boundaries preserved; no new assets, scene, gameplay or camera changes.

Actual matched Unity and phone evidence: ArtDirection/2026-10-05/foundry-structure-02. Bridge/undercroft reviewed in Unity closeups and overview; pit also inspected after a guarded two-drag approach on S23. Fine changes are subtle at overview scale and parts of the undercroft remain obscured by existing bastions. Phone route timing yields slight framing differences; this is not an exact pixel-aligned comparison or a reference-fidelity claim.

Compile0/0,graphics1/1,Edit410/410,Play135/135,Android0,integrity534/1099/568. APK32,374,745bytes SHA2567DA6010C1C646642EE5B12F22EDD7347D68CF6360CC32FBEEA00E9BA7F4EAEEC installed and base.apk hash matched. S23 static59.9fps/GPU3.8ms/57draws/67.2ktriangles; captured settled samples max16.9ms. No current-process Unity error matches. Not a combat/midrange benchmark.

Owner explicitly approved restarting their Forge run (2HP,40unbanked gold). Fresh backup TestResults/structure-device-20261005 taken first. Profile/bak and original development files unchanged by hash after validation; test flags removed, app stopped. Persistent progress retained; the in-flight run was restarted as authorised. No push.
## 2026-10-06 - Visual closure APK ready, phone unavailable

Owner explicitly could not provide the phone; no ADB access or device mutation was performed. Final desktop graphics capture and full gate passed:Edit410,Play135,Android0. APK Builds/Android/Cryptforge-Prototype.apk is32,374,745bytes, SHA25674297A6B721A4251F7C0E07E06AD3F8E1CD03848B984DD54955CDD7F4680B978. Built but NOT installed. Last installed baseline remains October5 structure02, SHA2567DA6010C1C646642EE5B12F22EDD7347D68CF6360CC32FBEEA00E9BA7F4EAEEC.

Proof ArtDirection/2026-10-06/visual-closure-01 contains actual Unity comparisons, not phone captures. Pending: fresh current-state backup, guarded install/hash check, lantern/steel/background appearance while moving and fighting, gate approach review, profile/development-file verification and session-owned flag cleanup. Do not restore an old backup or reuse the prior package's S23 FPS as a measurement of this APK. No push.


## 2026-10-06 - Surface composition supersedes pending APK

Latest APK B2D5A6F5000A1A7EF4099E595E19DA35BB1BF33921F65557ED9F722937B2E42B,32,374,745bytes, includes preceding visual-closure package plus workshop deposit/masonry cohesion corrections. Compile/graphics/fullgate passed:Edit410,Play135,Android0. Phone still unavailable: NO ADB access, installation or profile modification. Latest installed remains October5 structure02 (7DA6010C1C646642EE5B12F22EDD7347D68CF6360CC32FBEEA00E9BA7F4EAEEC).

Install this final cumulative APK when device available, with fresh run-state/profile/development backup and guarded visual review. Inspect surfaces and prior lantern/anvil/background changes while moving and fighting; production camera is unchanged. Portrait lens experiments in surface-composition-01 are test-only and not shipped. Previous S23 timing is not evidence for this build. No push.


## Device closure - surface composition (device logs 2026-10-07)

APK B2D5A6F5000A1A7EF4099E595E19DA35BB1BF33921F65557ED9F722937B2E42B installed and base.apk hash matched after fresh backup TestResults/surface-device-20261007. Supersedes pending device status. Guarded foundry gate walk and normal Staff room2, pickups, upgrade choice and enemy-contact captures reviewed; proof ArtDirection/2026-10-06/surface-composition-01/device. No new code/rebuild. Gate top/HUD crop remains; no new visual blocker in these samples.

S23 settled depth59.9fps/GPU3.8ms, initial226.5ms max; normal active samples59.4–59.9fps/GPU4.0–4.4ms, initial57.3ms max. Paused-choice samples excluded. Short smoke review, not dense-wave/midrange benchmark. No current-process error matches. ADB server interrupted before input; focus guard aborted then recovered. Profile/bak and original development files SHA256-identical; temporary depth/render flags removed, app stopped. Telemetry test events retained, no backup restore. Latest cumulative build ready for normal launch. No push.


## 2026-10-07 - Gate arrival runtime preview and device closure

Implemented opt-in development foundry entry vista (non-empty development/gate-arrival.txt plus foundry region preview, depth-proof wins). EncounterController defers the first wave; GateArrivalView presents the full gate for about 4 unscaled seconds, consumes skip until release, restores camera/HUD/input, then starts once. Cancellation/re-enable, pre-Start interruption, scene unload and pause ownership are covered. Telemetry active time excludes the deferred encounter. No proximity boss trigger, scene/art geometry, save schema or balance change; static art remains 17 renderers / 80,316 vertices.

Compile 0 warnings/errors; final EditMode 410/410, PlayMode 143/143, Android exit 0. APK 520E96960DDE255A99D13D32E668E9EE5BB9F6D0EEEAF885367C1CE672424EDD (31,113,605 bytes) installed; base.apk matched. Proof: ArtDirection/2026-10-07/gate-arrival-runtime-01. Device natural completion and ability-position tap skip reviewed, ability remained ready. Natural telemetry first room at 4.183s, first kill at 1.118 active seconds. Short skip session was stopped before flush; do not claim persisted skip telemetry verification.

Fresh TestResults/arrival-device-20261007 backup; profile/bak and both original development files hash-identical. New arrival preview flag intentionally remains enabled for next owner launch. Awake setting restored to original 2, app stopped, no depth/render diagnostics. Android AHardwareBuffer messages present; no managed exception/crash found in sampled process logs. No new performance benchmark. Next work should evaluate arrival pacing in play and broad visual fidelity; avoid treating more tiny trim as major progress. No push.


## 2026-10-07 - Yard surface hierarchy installed

Proof ArtDirection/2026-10-07/yard-surface-01. FoundryRegionFinish only: broader workshop apron deposits, soft boiler service grime, stronger existing gate-lane wear; original pit deposit preserved. Small material-value refinement, not a map redesign or concept-fidelity completion. Same textures/materials and 17 renderers; adaptive light mesh 80,344 vertices (+28). Same-camera before/after captures reviewed; central court remains substantially unchanged.

Graphics1, Edit410, Play143, Android0, integrity536/1099/568. Installed APK CAC7489FB29F0AF91781D85731B3BC154B31236154B2DF3D01E8B8C85E2B3170 (35,934,327 bytes); base.apk hash matched. Guarded S23 staged boiler/gate walk reviewed, no current-process managed exception/crash match. Not a fresh combat/performance benchmark. Fresh backup TestResults/yard-device-20261007; profile/bak and all original development files hash-identical. Temporary depth-proof removed, previous arrival opt-in retained, app stopped, no push.

Further minor floor-value tuning has diminishing benefit. Next substantive art slice should target a clearly visible asset/material limitation with an explicit before/after reference, or dynamic combat readability; do not advertise another small contrast adjustment as a major map upgrade.


## 2026-10-07 - Masonry V4 cohesion installed

New built-in image_gen edit of original FoundryMasonryV3: V4 removes repeated large diagonal fracture facets while preserving the five-course bond and dark grey-violet value. MAT_FoundryMasonry points at the new versioned texture; V3/GUID untouched. Full prompt/provenance and Unity gate/portrait/bridge/pit/overview comparisons: ArtDirection/2026-10-07/masonry-cohesion-01. Source1254square, Unity POT scaling, Android max1024 ASTC6x6, mipmapped/trilinear/repeat/non-readable. No geometry, lighting, camera or gameplay edits; static17/80,344 unchanged. Improvement is surface coherence, not concept completion.

Graphics1, Edit410, Play143, Android0, integrity537/1099/568. Installed APK59FCABA6D557886D68D2E0E1230CB2D8C1076F2C3FC776A48F18E67D203C0A2E (35,934,327 bytes); base.apk matched. Guarded S23 full gate reveal and natural Staff combat handback inspected. No managed exception/crash pattern in sampled process log; no new performance benchmark. Fresh backup TestResults/masonry-device-20261007; profile/bak and original development files SHA256-identical. No test flags added, arrival opt-in retained, app stopped. No push.


## 2026-10-07 - Open brazier flame readability installed

FoundryRegionDressing: open beacons/braziers now sample the existing smooth hot atlas patch rather than a wider coal-crusted seam. Layered gold/orange vertex tones and less opaque cooler tips remove black wedges from the flame interior. Coal mounds, boiler-window and undercroft mapping untouched. No new texture/material/geometry/update logic; static17/80,344. Static angular silhouettes remain; this is not a flame-animation package. Proof ArtDirection/2026-10-07/brazier-fire-01 (same-camera gate/bridge/portrait/overview and actual device).

Graphics1, Edit410, Play143, Android0, integrity537/1099/568. APK350870192B69AE45BB80E3AC8F1518C8E1A7F9233A2759DC50DED3A48295724C,31,113,605bytes installed; base.apk matched. Guarded S23 gate reveal and normal combat handback captured; no managed exception/crash pattern in sampled process log. No new FPS benchmark. Fresh TestResults/fire-device-20261007 backup; profile/bak and development files hash-identical. No test flags added, existing arrival opt-in retained, app stopped. Comment-only corrections followed build. No push.

## 2026-10-07 - Open flame motion installed

FoundryFlameMotion animates tagged upper flame corners in the existing emission batch at up to 30Hz, using scaled time and cached vertex arrays. Bases, untagged vertices and depth stay fixed; pause freezes, disable restores, re-enable resumes. Fixed bounds padding covers authored displacement. Region owns the mesh. No added materials/renderers/geometry; 17 renderers /80,344 vertices retained, one existing batch now dynamic. Full batch vertex upload is an explicit cost, not shader-only motion.

Proof: ArtDirection/2026-10-07/flame-motion-01 (six actual Unity poses plus guarded S23 entry still). Graphics check asserts exactly one animated batch, actual motion and pause freeze. Compile 0 warnings/errors; EditMode410/410, PlayMode144/144, Android exit0; integrity539/1099/568. APK8C0B5FA725943CDE6BF86039EF73E9A7BC7FD23862A3991EFA0EC839CAC0AB5B,31,113,605bytes installed and base.apk hash matched.

Fresh TestResults/flame-motion-device-20261007 backup; profile/bak and all three original development files SHA256-identical after review. Temporary depth-proof/render-stats flags removed, original arrival opt-in retained, app stopped. Sampled process log had no managed exception/crash pattern. Short staged S23 gate sample before recording:59.7-59.9fps, p50/p95 16.7ms, one window maximum33.4ms; GPU about4.2ms. This is not a dense-combat or midrange-device benchmark. Entry still confirms on-device flame appearance; real-map automated captures verify motion. Local first device video crops the open flames and is not promoted as visual motion evidence. No push.

## 2026-10-07 - Bridge banner motion installed

Two bridge-head banners now use six rows per texture column for gentle cloth curvature. FoundryBannerMotion tags cloth through the existing facade batch, keeps rods/untagged faces/structure Z fixed and uses identical drop/phase at duplicated seams. Scaled time freezes on pause; disable restores, re-enable resumes. Cached arrays upload the existing facade vertex buffer at up to30Hz. Max sway0.115 /vertical0.025, padded bounds0.15 per side. Region owns mesh. No new texture/material/renderer:17 renderers /80,504vertices (+160). Two existing region batches now dynamic (flame emission and cloth facade); gate-painted banners remain static. This is local entrance motion, not broad reference-fidelity completion.

Proof ArtDirection/2026-10-07/banner-motion-01: rest +8 actual Unity frames, two guarded S23 bridge poses. Compile0warnings/errors; graphics1passed (one actual cloth batch, motion and pause assertions); Edit410/410, Play145/145, Android0; integrity541/1099/568. Installed APK376E38C2E1F9F53B06F2DB00DB6B1EAA3ACFF43420F717B7C44BECD05755D8A8,31,113,605bytes; base.apk SHA256 matched. Short staged S23 bridge windows t92-107:59.9fps, p50/p95 16.7ms, max17.1ms, GPU3.6ms. Not dense combat, midrange testing or an isolated before/after cost measurement. No managed exception/crash pattern in sampled process log.

Fresh TestResults/banner-device-20261007 backup; profile/bak and three original development files SHA256-identical. Temporary depth-proof/render-stats removed, owner arrival opt-in retained, app stopped. No push. Further work should address broad silhouette/material/readability limits rather than presenting small ambient motion as reaching the concept image.


## 2026-10-07 - Basalt paving V4 ready; device install pending

Built-in image_gen edited FoundryPavingV3 into versioned Assets/_Project/Art/Props/Foundry/FoundryPavingV4.png. The new continuous stone surface reduces directional diagonal streaks and adds irregular wear/pitting. Source1254square RGB; mean87.76/80.85/84.14 versus V3 90.22/83.65/88.06. Slightly darker, not brightness-identical. MAT_FoundryPaving references V4, all other settings unchanged; V3/GUID preserved. No raster postprocessing, new geometry, layout, lighting, camera or gameplay change. Same17 region renderers /80,504vertices. Actual Android import1024square ASTC6x6,11mips. Real Unity before/after portrait/overview/pit/gate/bridge: ArtDirection/2026-10-07/basalt-surface-01, prompt.txt records built-in provenance. Accepted as a surface refinement, not a major reference-fidelity milestone.

Graphics1passed. First full gate Edit410pass, Play144/145: ExitSealPresentationTests compared pre-LateUpdate view state to post-Update encounter state;0.02053 difference narrowly exceeded0.02. Fixed test synchronization by freezing the encounter then yielding through LateUpdate. Tightened value comparison to0.00001 and pause comparison to exact equality; no runtime behavior change. Targeted3/3passed, final full Edit410/410 and Play145/145 passed. Initial failing XML/log retained in TestResults/basalt-initial-playmode.*. Integrity542/1099/568; Android exit0. Ready APK DD3E70BCF9E6835632E1A81D0DF0301D2B9D54087DEE1875E61A8780169D4F38,31,113,605bytes.

NOT installed or reviewed on device. ADB showed the app had reopened (PID2763) after the previous package was stopped. Asked whether the active run can be restarted; no answer received by closure. Do not kill it or install until this is resolved. This turn performed read-only device checks; no flags, profiles, backups or app lifecycle changes. Last package installed/verified by this assistant remains banner build376E38C2. No new FPS claim. On approval: fresh backup, guarded install, texture/gameplay visual review, diagnostic cleanup and persistent-file verification. No push.

Additional original concept, not in APK: ArtDirection/2026-10-07/forge-keeper-proof-01/forge-keeper-source.png (1024x1536 RGBA, transparent exterior, alpha0..254). Built-in image_gen; full prompt.txt, no raster edits. Browser size/background study and README explain the frontal-view/projection, paired-lighting, depth/cutaway and renderer-budget questions. Candidate for the missing large bridge guardian silhouettes; no statue implementation or final-art approval implied. Next substantial visual study should test that silhouette in the actual camera, instead of more minor floor tuning.


## 2026-10-07 - Basalt V4 device closure

The owner's continuation resolved the restart/install question. Fresh backup TestResults/basalt-device-20261007-182726, then installed ready APK DD3E70BCF9E6835632E1A81D0DF0301D2B9D54087DEE1875E61A8780169D4F38; base.apk SHA256 matched. Guarded S23 gate-entry and natural Staff combat screenshots reviewed. Hero, ember enemies and cyan XP remain legible against the more irregular paving. Proof includes device-entry.png and device-combat.png in ArtDirection/2026-10-07/basalt-surface-01. No new code/build/test run; uses the already-passed final410Edit/145Play gate. No managed exception/crash pattern in sampled process log. Short visual smoke check, not a new FPS or dense-wave benchmark.

Profile, profile backup and all three original development files SHA256-identical after review. No diagnostic flags added; existing region/arrival settings retained, telemetry test events retained, app stopped. No backup restoration or push. Supersedes the preceding pending-install status. Forge-keeper concept remains outside Assets and the APK; its actual-camera placement and depth/budget proof are the next substantive art task.


## 2026-10-07 - Guardian package device attempt: USB disappeared before installation

Owner explicitly gave phone access while AFK. Initial USB serial RFCW20W2WFX awake/unlocked, launcher focused, game stopped. Read-only pull of all11 app files succeeded to TestResults/guardian-device-20261007-205856/before. No phone inputs, app lifecycle or flag writes performed. At APK completion adb devices was empty; daemon restart/discovery and Windows device listing did not restore the Samsung connection. Owner reconnect question pending.

Ready guardian APK93380BACB4BFAE6BB17BFCF1FB1814E153E9524DEFAA8071F0C83959E5C49470; full410Edit/145Play passed. NOT INSTALLED; installed verified DD3E70BC remains current. No device screenshots/FPS/profile-after proof for this package. On reconnection recheck live profile and refresh backup if it changed before using the prepared TestResults/guardian-device-review.ps1. Device script has not run. Preserve original region-preview.txt, gate-arrival.txt and start-floor.txt.off; remove only newly added diagnostics after review.


## 2026-10-07 - Guardian package installed and reviewed

USB returned on owner continuation. Fresh backup TestResults/guardian-device-20261007-212948. Installed ready APK93380BACB4BFAE6BB17BFCF1FB1814E153E9524DEFAA8071F0C83959E5C49470, base.apk hash matched. First guarded launch stopped because NotificationShade remained foreground; temporary flags cleaned in finally. Collapsed the panel without screen capture/input to other apps, relaunched game and verified normal Staff combat. Then staged guarded bridge walk verified keeper, stone console and relocated banner on S23. Short bridge windows t32-52:59.9fps,p50/p95=16.7ms,max16.8-16.9ms,GPU3.7ms; not dense combat or a midrange benchmark. No managed exception/crash pattern in sampled staged-process log.

Profile,bak and all3 original development files SHA256-identical after review. Temporary depth-proof/render-stats removed; original arrival/region settings retained. App stopped. Proof device-bridge.png and device-normal.png in ArtDirection/2026-10-07/guardian-runtime-01. Supersedes pending USB/install status. No additional build for this installation; prior full410Edit/145Play verified package used. No push.


## 2026-10-07 - Crane structure package installed

Code-native crane silhouette revision in FoundryRegionDressing.cs and crane-only FoundryRegionSite.cs: broader cast mast/jib members with plain iron sampling, sparse alternating braces and end joints, stronger hoist chain, larger open scrap basket/solid lower tray, filled mast bearing and winch hubs, plain drum with sparse winding bands. Existing anchors, footing, generic wheels and gameplay/collision/camera remain unchanged. No new texture/material/renderer/update loop. Region18renderers /79,288vertices, down544 from guardian baseline79,832; existing flame/banner batches remain dynamic.

Actual same-camera Unity before/after and production portrait: ArtDirection/2026-10-07/crane-structure-01. Portrait stages a reachable court point with HeroMotion.EdgeMargin and demonstrates local cutaway; restores hero position before square closeup so the drum remains visible. At the close production camera the jib extends behind HUD and the outboard basket approaches the left edge; no camera change was made to fit the complete crane. This improves local structure/readability, not full painted-reference fidelity.

Verified: .NET390/390, compile0warnings/errors, final graphics1passed including map traversal, EditMode410/410, PlayMode145/145, Android exit0, integrity544/1099/568. No new tests mirroring visual constants; existing region depth/ownership/budget guards pass. Installed S23 APK24BCDD64BD851636A0DCB4948ADAC7FCBACE2D19753A32A04497B3F56EC4E4AD (35,958,239bytes); base.apk SHA256 matched.

Fresh TestResults/crane-device-20261007-215052 backup. Guarded staged route reached bridge, then pit corner and west court beside crane; changed routing to go around the shaft, no off-map teleport/camera change. Phone court/edge captures show main members, bearing, drum and partial outboard basket; full basket verified in square Unity capture. Final staged windows t332-347:59.9fps,p50/p95=16.7ms,max16.7-16.9ms,GPU2.3-2.4ms. Not dense combat, midrange hardware or an isolated performance comparison. Sampled staged/normal process logs had no managed exception/crash pattern. After removing depth-proof/render-stats, normal Staff battle reopened and active combat was captured. Background flush then app stopped.

Profile,bak and all3 original development files SHA256-identical after review; no restoration, loadout change or permanent test flags. Existing region-preview/gate-arrival/start-floor.txt.off retained; test telemetry retained. Earlier guardian package93380BAC was also installed/verified this turn before the crane revision; this24BC package is now latest. No push. Next art work should address another visible material/silhouette limitation with a real comparison, not claim the concept is finished.

## 2026-10-08 - Court silhouette device closure

Installed S23 APK B8896F6978B0642FD4F459938CA763E08B57D5C195E420583C8A5A7F1BBEC882 (35,978,297bytes), base.apk hash matched. Fresh backup TestResults/court-silhouette-device-20261008-011106. Guarded southeast/southwest edge inputs, gate passage and ordinary Staff combat reviewed; screenshots in ArtDirection/2026-10-08/court-silhouette-01. One transient ADB daemon interruption stopped inputs; cleanup was checked on reconnection, and the unfinished route was repeated. No other-app captures or inputs.

Profile,bak and original3 development files SHA256-identical; temporary depth-proof removed, existing region-preview/gate-arrival/start-floor.txt.off retained, app stopped. Test telemetry retained; no restore/push. No selected managed exception/crash patterns in sampled logs and no new FPS claim. .NET393, Edit413, Play145, graphics1, Android0; integrity545/1099/568. Device portrait crops parts of the new gate shoulders; full geometry checked in Unity.

## 2026-10-08 - North/workshop/paving device closure

Installed APK CD9510549BA6F748145AE7CF7A4D8ABBFF538A4B57948991692FD2C3FE2814C6 (35,978,062bytes); installed base.apk SHA256 matched. Owner unlocked and gave phone for this test. Fresh preinstall backup TestResults/north-workshop-device-20261008-153149 (earlier151657 snapshot retained, not restored). Guarded depth-proof route traversed west court to crane/northwest edge, stock-station front and northeast edge. Phone captures show supported stock grouping, character clearance and new rail/stone treatment. Complete silhouette also inspected in Unity because the close portrait crops structure tops/outboard portions.

After diagnostic removal the first ordinary capture was paused; it is not evidence of active combat. A fresh normal launch captured gate entry and then ordinary Slag Gate3/6 with Staff,2HP and40 at-risk gold/visible XP drops. No equipment/upgrade/forge selection or extraction performed. Background flush, app stopped. Final profile.json, backup and all3 original development files SHA256-identical to fresh preinstall backup; depth-proof absent, region-preview/gate-arrival/start-floor.txt.off retained. Test telemetry retained; no save restoration. Sampled staged and normal logs contain no selected managed exception/crash patterns. No new FPS, sustained combat or midrange-device benchmark.

.NET395/Edit415/Play145, compile clean, graphics1, Android0; final integrity545/1099/568. Region18renderers /92,951vertices. Screenshots and full evidence: ArtDirection/2026-10-08/north-workshop-paving-01. No push.

## 2026-10-08 - Gate processional device review

Installed S23 APK C5C78DF97A5090830159C295D262DE935F8540E39C60920740EB8D5E34D6E0F7 (35,981,571bytes); base.apk hash matched. Fresh backup TestResults/gate-procession-device-20261008-155803. Phone awake/unlocked, game stopped before installation. Collapsed notification shade before guarded game inputs; no other-app screenshot/input. Temporary depth-proof walk reached gate lane, medallion and dressed threshold. First start capture was loading; later actual game images are the accepted evidence. Normal restart captured gate entry and Staff battle/loot (Ember Hall1/6,90HP,4at-riskgold). No upgrade/loadout/extraction actions.

Profile.json,bak and all3 original development files SHA256-identical. Diagnostic removed, region-preview/gate-arrival/start-floor.txt.off retained; telemetry test events retained. Background flush then app stopped. Sampled staged/normal logs have no selected managed exception/crash pattern. No new FPS or midrange measurement. Full gate395.NET/415Edit/145Play/1graphics, compileclean, Android0, integrity545/1099/568. Proof ArtDirection/2026-10-08/gate-procession-01. No restore or push.


## 2026-10-08 — Gate elevation installed and reviewed

Owner explicitly authorized closing the Forge run (2 HP, 40 unbanked gold). Background flush, force-stop and fresh backup preceded installation: `TestResults/gate-elevation-device-20261008-165908`. Installed APK/base.apk SHA-256 matched `42916B306B07732DF42D4AFB11D5C7DB0037FEB67D135BD6D1F9CD4E50734617` (31,183,755-byte APK).

Guarded actual ADB drags checked ramp entry, crest, upper gate threshold and descent, including elevated feet/ring and follow-camera alignment. Normal restart and Staff combat were reviewed. Profile/profile backup and all three original development files had unchanged hashes after both device runs. Temporary depth-proof.txt/render-stats.txt removed; original region-preview.txt, gate-arrival.txt and start-floor.txt.off retained. Telemetry retained; no old-save restoration. App stopped, phone released. No selected fatal/reference/shader/index exception patterns appeared.

A capture-interleaved route showed one 166.9 ms frame with cause unresolved. Fresh-process traversal without screenshots during measurement did not reproduce it: post-startup 59.5–59.9 fps, maximum 33.3 ms. Do not generalize to dense combat, midrange devices or cold-driver behavior. Device images and complete selected measurement windows: `ArtDirection/2026-10-08/gate-elevation-01/device/`. Local commit only; no push.


## 2026-10-08 — Stone, metal and cavern package installed

Verified: .NET 395 passed; compile 0 warnings/errors; final actual graphics capture 1 passed; EditMode 415 passed / PlayMode 149 passed (including new owned-material lifecycle test); Android exit 0; final integrity 552 asset/folder GUIDs /1,099 package GUIDs /568 scene objects/components. Static region 18 renderers /95,622 vertices (+832, about0.9%); largest mesh27,373. APK36,061,418 bytes; SHA-256 `BA39FFFAB4EB3D650CBC0609B7211DF6BCDA3990EDCCE1655EE45519B8CFCA67`; installed S23 base.apk hash matched.

Phone was on Home with the game stopped and no keyguard before installation. Fresh backup: `TestResults/surface-cohesion-device-20261008-233826`. Guarded actual drags traversed the approach, gate ramp and threshold, then descended. Device captures show new boiler hood seams/material, quieter paving, graded distant scenery in the margins, and enemy/loot silhouettes through the near boiler. Complete far/near hood surfaces are checked in Unity detail; the close phone HUD crops the north hood. Normal Staff combat was captured separately (Room1,90HP,4unbanked gold, damage and XP/gold drops). Profile.json, profile.json.bak and all3 original development files had identical SHA-256 hashes before/after. Temporary depth-proof flag removed; original gate-arrival,region-preview,start-floor.txt.off and test telemetry retained. No save restoration. App background-flushed, stopped and phone released. Sampled process logs contained no selected fatal/reference/invalid-operation/index/shader error patterns. No new FPS, sustained combat or midrange benchmark; earlier gate-elevation first-use hitch remains unisolated.

Proof: `ArtDirection/2026-10-08/surface-cohesion-01/`; local commit only, no push.


## 2026-10-09 — Forge metal light installed

Verified: .NET 395 passed; compile 0 warnings/errors; actual graphics capture 1 passed; EditMode 415 passed / PlayMode 150 passed; Android exit 0. Final integrity: 552 unique asset/folder GUIDs, 1,099 package GUIDs and 568 scene objects/components; references and JSON valid. Static region: 18 renderers /96,558 vertices (+936, about 0.98%); largest mesh 27,373. No new renderer, material, texture or update loop. APK 36,063,810 bytes; SHA-256 `43BD507FD72AE0D91093D96BA86771C172B0DB7C00006BBE993EAE6A11743CDD`; installed S23 base.apk hash matched.

Fresh pre-install backup: `TestResults/forge-metal-light-device-20261009-001630`. Phone was on Home, unlocked, with the game stopped; no owner run interrupted. Guarded actual drags checked the gate approach, threshold and ramp descent. Warm near-boiler hood and pressure-main reflections were visible on S23; the HUD crops the north hood, whose complete geometry is checked in Unity. Normal Staff combat captured separately: Ember Hall Room1, 90 HP, 4 unbanked gold, damage7 and XP/gold drops. Profile.json, profile.json.bak and all three original development files were SHA-256-identical before/after. Temporary depth-proof removed; original gate-arrival, region-preview and start-floor.txt.off retained. Test telemetry retained; no save restoration or loadout/upgrade/extraction selection. App background-flushed and stopped; phone released. Selected fatal/reference/invalid-operation/index/shader patterns absent from sampled process logs. No new FPS, sustained or midrange benchmark; earlier first-use gate-elevation hitch remains unisolated.

Proof: `ArtDirection/2026-10-09/forge-metal-light-01/`. Local commit only; no push.


## 2026-10-09 — Worked stone package installed

Verified: .NET 395 passed; compile 0 warnings/errors; final actual graphics capture 1 passed; EditMode 415 passed / PlayMode 150 passed, including the strengthened triangle-interior elevation assertion. Android exit 0. Final integrity: 552 asset/folder GUIDs /1,099 package GUIDs /568 scene objects/components, resolved references and valid JSON. Static region:18 renderers /99,828 vertices (+3,270, about3.4%); largest mesh30,443. No new material, texture, renderer or per-frame work. APK 36,067,857 bytes; SHA-256 `209FC325C9E98DAA292EE10AE9F16AE350D53590774FB61BEA6CB8E7C24D5D70`; installed S23 base.apk hash matched.

Fresh pre-install backup: `TestResults/stone-relief-device-20261009-004818`. The phone was on Home, unlocked, with the game stopped; no owner run interrupted. Guarded actual drags checked the lane, gate threshold and ramp descent. Slate flakes and within-stone variation were seen on the S23; full long bastion recesses were reviewed in the Unity overview, not claimed fully visible through the phone HUD. Separate normal Staff run produced Room1,90HP,4 unbanked gold and visible XP/gold/chest rewards; the retained normal-combat image is the post-fight loot view, not an active attack frame. Profile.json, profile.json.bak and all3 original development files remained SHA-256-identical. Temporary depth-proof removed; original gate-arrival,region-preview,start-floor.txt.off and test telemetry retained. No save restoration. App background-flushed, stopped and phone released. No selected fatal/reference/invalid-operation/index/shader patterns in sampled process logs. No new FPS, sustained or midrange benchmark; the earlier elevation first-use hitch remains unisolated.

Proof: `ArtDirection/2026-10-09/stone-relief-01/`. Local commit only; no push.


## 2026-10-09 — Architecture-cohesion APK ready, not installed

Verified: .NET395 passed; compile0warnings/errors; actual graphics capture1 passed; EditMode415 passed / PlayMode150 passed; Android exit0. Integrity:552 unique asset/folder GUIDs /1,099 package GUIDs /568 scene objects/components; references/JSON valid. Static region18renderers /102,016vertices (+2,188/about2.2%), largestmesh30,443. APK36,068,501bytes; SHA-256 `0CA694C76762E3F5D855AADF43BDF69E38144FB85DC4B4E075A10FCC0677380C`.

Device installation and review are pending. USB is connected and keyguard was not showing, but the phone was on NotificationShade. Root asked whether the device would be free for installation/traversal and short performance measurement; no reply arrived during this work. No installation, app launch, gameplay input, save writes or device captures were performed for this package. Latest verified installed art remains the preceding stone-relief package; do not label this package phone-verified. Once availability is confirmed, recheck for an active owner run, take a fresh backup, install/hash-match the ready APK, run guarded gate/normal checks, then optionally the short no-capture traversal measurement. Prepared local scripts: TestResults/architecture-cohesion-device-review.ps1, -normal.ps1 and -perf.ps1. No fresh device backup exists yet; never reuse the previous session snapshot. No new FPS claim; prior first-use hitch and sustained/midrange acceptance remain open.

Actual Unity proof: `ArtDirection/2026-10-09/architecture-cohesion-01/`. Local commit only; no push.


## 2026-10-09 — Architecture-cohesion installed after owner availability confirmation

Owner explicitly made the phone available and authorized pushing completed commits. Installed/hash-matched ready APK `0CA694C76762E3F5D855AADF43BDF69E38144FB85DC4B4E075A10FCC0677380C` (36,068,501 bytes); no rebuild. Fresh backup `TestResults/architecture-cohesion-device-20261009-133914`, with app stopped on unlocked Home before install. Guarded gate approach, threshold and descent reviewed; normal Staff run separately showed Room1,90HP,4unbanked gold, damage7 and loot. Profile, backup and three original development flags hash-identical. Temporary depth-proof/render-stats removed, test telemetry retained, app stopped. No selected exception/crash pattern in sampled logs.

Short no-capture traversal measurement: after the startup window,59.5–59.9fps, one50.0ms frame; GPU averages3.9–4.6ms. Initial startup window contained2272.6ms, which is not a steady-state frame claim. The50ms traversal hitch remains unisolated; this does not close cold-first-use, sustained crowded-combat or midrange acceptance. Captures/log in `ArtDirection/2026-10-09/architecture-cohesion-01/device/`. Supersedes pending-device status above.


## 2026-10-09 — Material readability verified and installed

Completed together: broader continuous iron sampling and restrained stock deposits; curved pit lining/contact shadow; matte brass and quieter stone grooves. Files FoundryRegionWorkshop/Massing/Heraldry.cs. Existing source textures, floor/layout/depth/camera/gameplay/save behavior retained. Three-agent source and actual-image cross-review found no blocker; before/after gain is restrained, not painted-reference completion.

Verified .NET395, compile0warnings/errors, actual graphics1, EditMode415, PlayMode150, Android exit0. Integrity552unique asset/folderGUIDs /1099packageGUIDs /568sceneobjects, valid refs/JSON. Static region18renderers /102,100vertices (+84/about0.08%), largestmesh30,443. Installed/hash-matched S23 APK36,072,971bytes; SHA-256 `1B79022F776AA523259193DD64532CFB0D369EF4A63620B1B3A3A2FF0BE287C6`.

Fresh backup `TestResults/material-readability-device-20261009-135248`. App was stopped/unlocked before install; no owner run interrupted. Guarded real drags reviewed the west stock shelf, pit collar/wall and gate route; separate normal Staff run showed Room1,90HP,4unbanked gold, damage7 and XP/gold/chest. First route image named pit-front shows central court instead and is excluded from pit proof; a subsequent targeted route reached the actual pit wall. No teleport/camera override. Profile.json,bak and three original development files SHA256-identical after normal run and both performance passes. Temporary depth-proof/render-stats removed, original owner flags and test telemetry retained, app stopped/released. Selected crash/reference/index/invalid-operation/shader error patterns absent in sampled logs.

Performance limitation: first short no-capture pass had one216.9ms frame (main214.5ms) near first ramp entry; other post-startup windows59.9fps. Startup contained2259.5ms and is explicitly separate. A fresh-process control plus same-process full descent/reclimb had first-entry33.4ms; the repeat phase remained59.7–59.9fps with maximum33.4ms, no repeat of the217ms stall. Control startup also had300.1ms and2309.2ms, and reported initial refresh30Hz; do not treat the two launches as identical cold-driver conditions.

Read-only source diagnosis: FoundryGroundProjection.Apply lazily creates a material and16x16 sprite grids at first ramp overlap. Grid arrays total5384bytes, compatible with window allocation peaks, but the actual217ms frame reported only0.3KB, so allocation, shader/driver initialization or external stall causation is NOT established. All new art code runs once at region construction. Next focused engineering task: profiler markers around material/resource creation, grid creation/OverrideGeometry and first draw; compare first/warm crossings before selecting controlled-loading prewarm. The earlier first-use hitch remains open; no sustained crowd or midrange acceptance claimed.

Proof `ArtDirection/2026-10-09/material-readability-01/` contains matched Unity views, actual device stock/pit/normal views and both raw performance logs. Broad surface fidelity, actual encounter/boss transition, actor articulation/roster and extended acceptance remain open.

## 2026-10-09 — Single baked north boiler integrated; installed, device visual review pending

Replaced only north boiler index0 at design(18.39,15.81) with the Blender study02 bake. Original source PNG retained byte-for-byte in Resources/FoundryBoiler.png; imported1024square ASTC6x6,11mips,non-readable,Clamp. Calculated block payload626,288bytes including mips, excluding engine/native overhead. FoundryRegionBakedBoiler emits two static bands at the existing drum/iron ranks inside the unchanged MassDepth footprint. Scene owns source material and cutaway variant; shared texture is asset-owned. Foundation, pipe endpoints, smoke anchor, other boilers, movement/layout/gameplay/saves/camera remain unchanged.

Initial full PlayMode run149/150caught a second floor-decal batch violating existing ordering. Fixed by merging contact shade into the existing Coping/floor-decal batch, preserving the original test. Final .NET395passed, EditMode415passed, PlayMode150passed; actual graphical capture1passed; Android exit0; static integrity554asset/folderGUIDs,1099packageGUIDs,568sceneobjects/components with valid references/JSON. Static region20renderers /101,392vertices (prior18 /102,100). Two added renderers,708fewer vertices; this is not an FPS claim. No new per-frame component. Graphics show matched old/new construction, intact pipe/base contacts and four validated walkable approach points with production follow width and HUD hidden. No temporary courtyard prop in these views.

APK36,074,875bytes; SHA256 DA3961080FB0823D60C230DDC0C883C39F89AE31C15A24D9FA405773375FA5D3. Owner authorized installation/testing. Later read-only window check showed YouTube in front, so root asked whether opening the game could interrupt use; no further answer before this record. Background installation proceeded under the existing installation authorization while Cryptforge was stopped; no input, app launch or screenshot. Fresh backup TestResults/baked-boiler-runtime-device-20261009-181315. adb install-r succeeded, installed base.apk hash matched; all11files in fresh before/after-install snapshots SHA256-identical. Final foreground was Launcher. No diagnostic flags written. Phone visual/traversal/performance acceptance remains pending; do not claim this package was played or benchmarked on S23. The prior ramp first-use hitch remains open.

Proof: ArtDirection/2026-10-09/baked-boiler-runtime-01. Resume with fresh device focus/keyguard/active-run check and the pending availability reply; installed APK does not need rebuilding. Then guarded north-boiler route, normal combat, cleanup/save hashes and optional short no-capture measurement. Prepared ignored device scripts are starting points only: device-review currently includes reinstall/backup, so reuse or split carefully rather than blindly replacing the known install snapshot. Full painted-reference fidelity and other furnace variants remain unfinished.

## Single baked boiler device acceptance completed — 2026-10-09

Supersedes the pending-device status above. On the next continuation, the phone was unlocked on Home and Cryptforge was stopped. Fresh review backup: TestResults/baked-boiler-review-20261009-181855. Installed APK hash still matched DA3961080FB0823D60C230DDC0C883C39F89AE31C15A24D9FA405773375FA5D3; no reinstall. Guarded real movement reached the new boiler; front and step-back captures show arches, foundation, pipe connection and foreground ordering. HUD partly crops the hood; complete form is covered by Editor capture. An earlier route overshot toward the old second boiler and is excluded from the selected first-boiler proof.

Separate normal Staff run showed Room1,90HP,4unbanked gold and XP/gold/chest rewards; normal-combat.png is a post-kill reward view. Profile.json, profile.json.bak and all three original development files remained SHA256-identical. Temporary depth-proof/render-stats flags removed, telemetry retained, no save restoration, app stopped. Selected crash/reference/index patterns absent from sampled process logs.

Targeted route ended with short quiet windows at59.9fps, maximum17.1ms and GPU average3.8ms. Startup separately contained2305.7ms and the first sampled window39.1ms. This is a short S23 check, not sustained crowded-combat or midrange acceptance; no matched old/new GPU benchmark. The earlier first-ramp hitch remains open. Selected screenshots and process logs are in device/.


## 2026-10-09 — Shared north boiler pair verified and installed

Accepted the first boiler on S23, then extended the same study02 bake to the second tall north boiler. FoundryRegionBakedBoiler now lazily owns one source material shared by both; two existing depth-band batches each contain8vertices with independent mass depths. Original foundations, service pipes, smoke anchors, short-boiler variants and gameplay remain unchanged. Runtime region20renderers /100,684vertices (previous single replacement20 /101,392); no additional texture/material/renderer or per-frame component.

Verified .NET395passed, graphical capture1passed, EditMode415passed, PlayMode150passed, Android exit0, no C# warning/error matches in Android log. Final static integrity554asset/folderGUIDs /1099packageGUIDs /568sceneobjects, valid references/JSON. Existing ordering and renderer-budget assertions retained; batch test now verifies both original footprint depths. APK36,074,650bytes, SHA256 `64DC9D243D84BBF414B7F39E0F2B30118EDBD441B12B7445076282BC07FB43D5`; installed S23 base.apk hash matched.

Fresh backup TestResults/baked-boiler-pair-device-20261009-183514; before installation phone unlocked on Home, game stopped. Guarded actual movement checked both north boilers and their foundation/pipe/foreground contacts. Phone HUD crops much of the second boiler's upper form; complete pair is separately verified in actual Unity graphics, not claimed fully visible on phone. During the first targeted return attempt ADB daemon connection failed, the guard stopped input and finally cleanup removed flags/stopped app. Rechecked Home/unlocked, absent flags and stopped app; repeated that targeted route successfully. The failed attempt is not acceptance evidence.

Separate normal Staff run: Room1,90HP,4unbanked gold, damage7 and XP/gold/chest visible in post-kill reward capture. Profile.json,bak and all3original development files hash-identical; temporary depth-proof/render-stats removed, telemetry retained, no save restoration. App background-flushed/stopped, phone released. Selected fatal/reference/index/invalid-operation/shader-error patterns absent from sampled process logs.

Short quiet route windows59.9fps with maximum17.2ms, GPU averages around3.9–4.0ms; first launch separately contained2330.4ms and249.2ms startup intervals. No matched GPU baseline, sustained crowded combat or midrange acceptance; earlier ramp first-use hitch remains open.

Proof: ArtDirection/2026-10-09/baked-boiler-runtime-02, actual pair before/after, four validated walkable portrait approaches, full-map view and selected actual device captures/logs. Same-frame legacy comparison reconstructs both old boilers and keeps new contact shades in both views. Current capture tool reproduces pair02; use e901a33 for historical single01. Full reference fidelity remains unfinished. Next substantial art candidate is a distinct short-boiler variant, then pipe-material coherence; these are not implemented by this slice.


## 2026-10-09 — Short boiler pair verified and installed

Completed a distinct low retort design in Blender and integrated both foreground boilers. Shared study02 materials/light, new segmental fire arches, broad pressed hood and short vent. Source Tools/ArtReview/BuildBlenderShortBoiler.py loads the committed tall study scene and replaces its mesh geometry. Editable source/bake: ArtDirection/2026-10-09/short-boiler-01. Model6,704vertices/13,036triangles is authoring-only; Unity uses two shared depth bands and the original runtime service sockets. Existing low height2.28, foundations, smoke anchors, layout/gameplay/camera/save behavior retained.

One new1024ASTC6x6,mipmapped,non-readable texture,626,288calculated payload bytes including mips. Both shorts share one source material and two batches, explicit scene cleanup. Old furnace-wrap batch is now absent (legacy resource/constructor retained for fallback/comparison); region21renderers /99,492vertices vs previous20 /100,684. No new per-frame component. Regression test verifies original independent short depths, both bands, retired wrap absence and21renderer limit; existing floor ordering/lifecycle assertions retained.

Verified .NET395passed, actual graphics1passed, EditMode415passed, PlayMode150passed, Android exit0 and no C#warning/error matches in Android log. Static integrity555asset/folderGUIDs /1099packageGUIDs /568sceneobjects, resolved refs/JSON. Initial capture compilation found a test-local name collision, corrected before successful graphics and full gate; its stale Unity lock was removed only after confirming Editor exit. APK36,076,056bytes; SHA256 `A3630A5F5249B7D486BBA8406DCDAE0CEA8DAA1F2062F421B7512C0A89F6D54C`; installed S23 base.apk hash matched.

Owner explicitly authorized closing the background run and using the phone. Fresh pre-stop/install backup TestResults/short-boiler-device-20261009-201142. Guarded real drags checked the foreground boiler and behind route; the first route did not reach the second boiler closely enough, so a separate longer targeted route captured the actual east boiler beneath the gate ramp. Both are visible in the retained device images; pipe/socket and foundation alignment intact, hero readable. Proof route contains staged enemies/outlines and is not normal encounter/balance evidence.

Separate normal Staff run showed Room1,90HP,4unbanked gold, damage7 and XP/gold/chest in post-kill reward view. Profile.json,bak and all3original development files SHA256-identical after normal run and final targeted review. Temporary depth-proof/render-stats removed, telemetry retained, no save restoration. Game stopped; phone released. Selected fatal/reference/index/invalid-operation/shader-error patterns absent in sampled process logs.

First route's late quiet windows59.9fps,max16.9ms,GPUaverage4.2–4.3ms; startup separately2289.9ms and216.3ms. This short S23 check is not matched prior/new GPU testing, sustained crowded combat or midrange acceptance. Earlier first-ramp hitch remains open.

Actual Unity before/after, four validated walkable portrait approaches, overview and device evidence: ArtDirection/2026-10-09/short-boiler-runtime-01. Legacy comparison reconstructs both old shorts; new shared contact shade/runtime sockets remain in both views, with coincident legacy socket geometry only in before. Fire/light are baked static, smoke remains animated. Foreground pipe shading and central pressure vessel still look coarser than the new furnace bakes; next focused art work is their material/shape coherence. Full-reference fidelity, encounter/actor art and long-device acceptance remain unfinished.

## 2026-10-09 — Combined pressure and architecture package installed

Five chained art tasks completed with three helper agents: rolled service pipes/high feeds, two baked pressure reservoirs, formed pit hood pair, gate roof/header continuity, and keeper mounting slabs. One coordinated full gate and APK after source/image reviews. Pressure proof21/99,492 ->24/99,444; pit/header24/100,548; final keeper seats24renderers/100,572vertices. New reservoir runtime512ASTC6x6/10mips/non-readable,158,432calculated mip block bytes; authoring source remains1024. Runtime has two explicit2x2mass footprints and separated far/main/near ranks; source material cleanup and near relief ordering verified. Other changes reuse existing materials and add no per-frame components. Layout/gameplay/camera/profile behavior retained.

First full PlayMode run149/150failed the natural arrival duration: the old loop counted the preceding scene-loading frame toward the four-second reveal. Fixed GateArrivalView to start a monotonic unscaled clock after capture. Added an actual1.2-second preceding-frame regression; old natural3..6second assertion unchanged. Final .NET395passed, EditMode415passed, PlayMode151passed; Android exit0, no C#warning/error matches. Six successful graphics passes cover actual source-state before/after pressure, pit/header and keeper comparisons. Static integrity561unique asset/folderGUIDs,1099packageGUIDs,568sceneobjects/components, valid references/JSON. Initial keeper capture selected a point without the hero margin; corrected tool to assert EdgeMargin and use three established walkable points before successful captures. No gameplay bounds were relaxed.

Installed S23 APK31,209,737bytes; SHA256 `871C3772368CFBC082F537CC39DBC9862C449BAD686BAFB8CAEB425892C9E4F7` matched installed base.apk. Fresh backup `TestResults/pressure-finish-device-20261009-210229`, taken before closing the owner's explicitly authorized Forge run (2HP/40unbanked gold). All11files hash-identical immediately after installation. Phone was temporarily locked; later read-only check confirmed unlocked, awake Launcher before any game input.

Guarded real drags reviewed the near tank, stem/wheel/socket and boiler service connections, gate approach, pit hood/rim and right keeper/base on the bridge. HUD crops the gate header and portions of tall equipment; full structure/other tank acceptance comes from actual Editor captures, not a claim that every detail was visible on device. First keeper route hit the court edge, so a second established route reached the actual bridge; the misleading first-route images are excluded from selected proof. Separate normal Staff run showed Room1,90HP,4unbanked gold, damage7, XP/gold/chest in post-kill reward view.

Final profile.json,bak and all3original development files SHA256-identical; temporary depth-proof/render-stats removed, telemetry retained, no save restoration. App stopped and phone released. Selected fatal/reference/index/invalid-operation/shader-error patterns absent in sampled process logs. Late quiet windows59.9fps,max17.0ms,GPUaverage4.2..4.3ms on the first route; its first sample included229ms. Second route initial33.8ms, laterquiet16.7..16.9ms. These are short S23 checks with captures, not a matched GPU benchmark, cold-ramp diagnosis, sustained crowd or midrange acceptance. Earlier first-ramp hitch remains open.

Proofs: ArtDirection/2026-10-09/pressure-assembly-01 (includes combined device/validation), foundry-finish-01, keeper-seat-01. Editable Blender reservoir source in reservoir-01. The inherited capture budget's texture line describes unchanged FoundryShortBoiler; gallery READMEs identify this explicitly rather than treating it as reservoir data. Gate and keeper gains are small; pit metal is smoother but simpler. Full painted-reference richness, encounter/boss transition and actor articulation/roster remain unfinished. Next work should target a substantial reference-facing surface or encounter presentation, rather than another barely visible bevel pass.

Source provenance correction: forge-keeper-proof-01/prompt.txt did not retain the full original generation prompt (literal undefined plus generic note); its README is corrected without inventing a missing prompt.