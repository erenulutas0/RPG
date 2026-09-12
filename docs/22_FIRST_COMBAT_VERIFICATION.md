# First combat slice — setup and verification

Completed USB-device test and APK details: [Android device validation](23_ANDROID_DEVICE_VALIDATION.md). Unity Editor is installed at `E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe` and the prototype is installed on the Samsung SM-S911B.

## Validation status, 2026-09-12

- **Passed:** actual `DamageContext`, `HealthState`, and `WeaponRuntime` source compiled in C# 9 mode using the installed .NET SDK; all **26 NUnit calculation/state cases passed**, zero failures/skips.
- **Passed:** static metadata, JSON, GUID references, scene fileID references, and serialized script field checks using `Tools/Verify-Project.ps1`.
- **Passed in Unity 6000.0.65f1:** import and compilation after removing the invalid `com.unity.modules.textrendering` dependency; 26/26 EditMode cases and 5/5 PlayMode tests. Reports: `TestResults/editmode.xml` and `TestResults/playmode.xml` (ignored local artifacts).
- **Passed:** development APK build, ADB install/launch, rendered portrait layout on Samsung SM-S911B (1080x2340), five-hit death, and a short background/resume smoke test. Screenshots and app-process logs are listed in the device report. Extended performance/thermal tests and other devices remain pending.
- The standalone .NET run uses the same EditMode test source but does not substitute fake Unity APIs. Real Unity tests now additionally cover the actual authored scene and components.

## Open and run

1. Install/open Unity Hub and install **Unity 6000.0.65f1** from the [official release page](https://unity.com/releases/editor/whats-new/6000.0.65f1). Activate the appropriate Unity license. Add Android Build Support with its SDK/NDK and OpenJDK if testing Android.
2. In Hub, use **Add project from disk** and select `E:/MobileGame`. Open it with the pinned Editor; allow package resolution/import to finish. This folder is already the project; do not create a nested template project.
3. Open `Assets/_Project/Scenes/Gameplay/Gameplay.unity`. In the Console, clear old messages and confirm there are no compiler errors, missing scripts, missing sprites, or missing reference errors.
4. Select the Game tab. Add a **540 × 960** fixed resolution or **9:16** aspect ratio. Also repeat at **1080 × 2340** (9:19.5). Verify the title, weapon stats, both health bars, hero, and enemy are visible without overlap.
5. Press **Play**. No click/tap is required. The blue Vanguard attacks the orange Grunt; the hero body and its sword/shield nudge upward; the Grunt flashes on hits. The Grunt strikes back every second for 6 damage: it lunges down, the hero flashes, and **Vanguard | HP** drops (about 76 / 100 after the first fight).
6. Watch the Grunt drop from 50 HP to zero in five 10-damage hits, spaced roughly 0.8 seconds apart. The first hit occurs on the first gameplay update, so the first visible HP may be 40. Expected time to death is about 3.2 seconds plus frame quantization.
7. During combat the status reads **Encounter 1: combat is automatic** with **Hits this fight** counting up and **Level 0 | XP 0 / 10**. The Grunt is spawned at runtime from `Prefabs/Enemies/Grunt.prefab`; the Hierarchy shows it as **Grunt**.
8. On the fifth hit the Grunt shape disappears, XP reaches 10, the screen dims and **Level up! Choose one upgrade** shows two cards: **Tempered Edge** (+5 damage per hit) and **Quickened Grip** (+25% attack speed). Behind the overlay the status reads **Grunt defeated in 5 hits, 3.2s**. Combat is paused and no new Grunt appears while the panel is open, however long you wait.
9. Tap **Tempered Edge**, including rapidly several times. Expect the panel to close, **Sword | 15 damage every 0.80s**, **Level 1 | XP 10 / 20**, and no second application. About one second later **Encounter 2** starts with a **Runner**: a narrow yellow enemy with 30 HP that slashes every 0.4 s for 2. It dies in **2 hits, about 0.8s**, and the second choice opens at **XP 20 / 30**.
10. Encounter 3 is a Grunt again. Encounter 4 is a **Tank**: a wide purple enemy with 120 HP. It does not attack for the first 1.5 s, then slams for 12 every 2.5 s. Encounter 5 starts the Grunt, Runner, Grunt, Tank order over. In a new session choose **Quickened Grip** first: expect **10 damage every 0.64s** and the Runner cleared in **3 hits, about 1.3s**.
11. Keep choosing. Each encounter costs HP, so the hero eventually dies (encounter 15 when always taking the first card; about encounter 7 when taking attack speed first). The hero shape disappears and **Defeated** appears with **Vanguard fell to a Grunt in encounter N**, encounters cleared, level, XP and the build. Enemy and hero attacks stop and no new Grunt spawns.
12. **Try again** is disabled for half a second, then tap it (rapidly is fine). Expect one reload into Encounter 1 with 100 HP, Level 0, XP 0 and Sword 10 damage every 0.80s.
13. Stopping and pressing Play again must give the same fresh state. Keep default domain/scene reload enabled as saved in Editor settings.

## Data edits and edge cases

Perform data edits outside Play Mode; ScriptableObject Inspector edits during Play Mode can persist in Unity.

- Select `Data/Weapons/Weapon_Sword.asset`: Damage **10**, Interval **0.8**, Range **3**. Change Damage to **25**, enter Play Mode, and expect two hits. Stop and restore **10**.
- Change Interval to **1.5** and expect visibly slower hits. Stop and restore **0.8**. Runtime snapshots intentionally do not hot-reload asset edits.
- Change Range to **1**; the center-to-center separation is 2.4, so no hits should occur. Stop and restore **3**. There is no movement system to close this gap.
- Select `Data/Weapons/Weapon_GruntStrike.asset`: Damage **6**, Interval **1**. Set Damage to **40** and expect death on the third strike of the first encounter and an immediate result screen showing encounter 1 and no upgrades; set it to **0.01** for a practically endless run. Stop and restore **6**. Clearing `_weapon` on `Enemy_Grunt.asset` must throw `EncounterController needs an enemy prefab, an armed definition, the hero and hero targeting.`
- Select `Data/Encounters/PrototypeEncounters.asset`: reorder the list to put Tank first and expect a purple 120 HP enemy in Encounter 1. Stop and restore Grunt, Runner, Grunt, Tank. Clearing an entry must throw `Encounter sequence entry N needs an enemy definition with a weapon and a prefab carrying Health, Targeting and AttackController.`
- Select `Data/Weapons/Weapon_TankSlam.asset`: Initial Delay **1.5**. Set it to **0** and the Tank slams on its first frame. Stop and restore **1.5**.
- Select the **Encounter** object: Advance Delay **1**. Set it to **3**, choose an upgrade, and expect a three-second gap before the next Grunt. Stop and restore **1**. Prefab edits to `Grunt.prefab` apply to every encounter.
- During Play Mode, move the spawned Grunt to X **20**; HP must stop falling. Return it to `(0, 1.2, 0)` and attacks resume if it is still alive.
- Disable Grunt in the Hierarchy while alive, wait, then re-enable it. HP should remain unchanged while disabled and attacks should resume after re-enable.
- Pause using the Editor pause button, then resume. Combat must freeze and continue without a backlog of burst attacks.
- Select `Data/Upgrades/Upgrade_Damage.asset`: Amount **5**. Change it to **20**, run, choose it and expect **30 damage**. Stop and restore **5**. After any session, `Weapon_Sword.asset` must still read Damage 10 and Interval 0.8.
- Set Experience Per Level on `PrototypeEconomy.asset` to **20**: the kill no longer opens a choice (XP 10 / 20). Stop and restore **10**.
- Select `Data/Economy/PrototypeEconomy.asset`: Experience Per Kill **10**. Change it to **25**, enter Play Mode, and expect **XP: 25** after the kill. Stop and restore **10**. Clearing the `Economy` field on Combat Setup must log `CombatSetup is missing required scene or definition references.`
- Check the definition assets after running: their authored values remain unchanged by gameplay.

## Automated checks

From a PowerShell terminal at `E:/MobileGame`:

```powershell
pwsh -File Tools/Verify-Project.ps1
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release --logger 'trx;LogFileName=combat-state.trx' --results-directory TestResults
```

The .NET test project targets .NET 9 and accepts SDK 9 or newer with the .NET 9 runtime installed. Its packages are development-only and are outside `Assets`; they do not enter the Unity player. Reports and build outputs are ignored by Git.

In Unity, open **Window → General → Test Runner**:

1. Select **EditMode → Run All**: expect 84 passing cases covering encounter scheduling, weapon windups, Runner/Tank fight simulations, damage validation, health clamping, duplicate/reentrant death, cadence, pause, missing/dead targets, independent weapon state, the five-hit balance fixture, kill-reward idempotency, stat modifier order and clamps, level thresholds, single/stale/re-entrant upgrade selection with stack limits, encounter progress rules, upgraded next-fight simulations, run end rules, enemy strike damage and a full-run death simulation.
2. Select **PlayMode → Run All**: expect 20 passing tests covering the authored Grunt/Runner/Grunt/Tank order, Runner chip damage, the Tank windup, the full automatic kill with 10 XP, a single award under repeated lethal damage, range/disable/reacquisition, destroyed targets, pause/resume, dead-owner behavior, the upgrade panel opening and pausing combat, rapid taps applying once, attack-speed selection updating runtime stats and the HUD, taps before the input delay being ignored, no encounter advance while choosing, kill → XP → choose → next Grunt chains for both upgrades, Grunt strike damage, death to a Grunt with the result explaining it, the result build list, and a single Try again reload into a fresh run.
3. Reopen Gameplay after tests; tests load/unload the scene for isolation.

Optional Unity batch commands after installing the Editor (close any Editor using this project first). Batch mode requires an active Unity license: when the account session or Personal license has lapsed, Unity exits with code 198 and the log reads `No valid Unity Editor license found`; sign in again through Unity Hub before retrying. The Editor on this machine is installed at the path below, not under the default Hub location.

```powershell
$unityEditor = 'E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe'
& $unityEditor -batchmode -nographics -projectPath E:/MobileGame -runTests -testPlatform EditMode -testResults E:/MobileGame/TestResults/editmode.xml -logFile E:/MobileGame/TestResults/editmode.log
& $unityEditor -batchmode -nographics -projectPath E:/MobileGame -runTests -testPlatform PlayMode -testResults E:/MobileGame/TestResults/playmode.xml -logFile E:/MobileGame/TestResults/playmode.log
```

Create `TestResults` first if it does not exist. Do not add `-quit` to test commands; the test runner exits after completion. Batch tests do not establish visual correctness.

## Android smoke check (passed on Samsung SM-S911B)

1. Open **File → Build Profiles**, select/add Android, and switch platform. Confirm **Gameplay** is the only enabled scene in the scene list.
2. In Player settings, confirm portrait, ARM64, and IL2CPP. Use the SDK/NDK/JDK supplied with the Editor. The application identifier is a temporary prototype identifier.
3. Enable Development Build and use **Build And Run** to a connected Android device. Output to ignored `Builds/`.
4. Confirm direct entry into combat, readable text around safe areas, five-hit death, and no errors in the device log. Background and resume the app during combat; verify no burst of catch-up attacks.
5. Observe frame pacing and memory on an actual low/mid Android device before claiming the documented 60 FPS target. No device performance measurement has been made yet.

## Scope remaining

The implemented run repeats kill → XP → choose → next identical Grunt until the hero dies, then shows the result and restarts in one tap. Enemy scaling, healing, Extract, additional enemies/weapons, escalating encounters, a boss, result screen, and in-game restart are deliberately still pending. The implementation plan assigns the next reward/choice slice to Day 3.
