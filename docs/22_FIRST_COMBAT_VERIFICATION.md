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
7. The line under the title reads **Floor 1 | Room 1/6 | Ember Hall**, the status **Wave 1/2: combat is automatic**, with **Gold 0  (0 at risk)** in gold at the bottom left and **Level 0 | XP 0 / 10** at the bottom right. Enemies are spawned at runtime from their prefabs; the Hierarchy shows the current one by name.
8. On the fifth hit the Grunt shape disappears, XP reaches 10, **Gold 5  (5 at risk)**, the screen dims and **Level up! Choose one upgrade** shows **Tempered Edge** (+5 damage per hit) and **Quickened Grip** (+50% attack speed). Behind the overlay the status reads **Grunt defeated in 5 hits, 3.2s**. Combat is paused and nothing advances while the panel is open.
9. Tap **Tempered Edge**, including rapidly several times. Expect one application: **Sword | 15 damage every 0.80s**, **Level 1 | XP 10 / 20**. About one second later wave 2 brings a **Runner** (narrow yellow, 30 HP, 2 damage every 0.4 s) that dies in **2 hits, about 0.8s**.
10. The floor continues room by room: **Cinder Walk** (Runner, then Grunt), **Slag Gate** (a wide purple **Tank**, 120 HP, first slam only after 1.5 s), then **The Forge**. Every kill still opens an upgrade choice.
11. In **The Forge** no enemy spawns, the status reads **The forge is quiet. Choose your edge.** and **The forge: choose one** offers **Mend** (restore 40% of your health) or **Temper** (one extra upgrade choice). Mend heals immediately; Temper opens another upgrade choice at once. One second after choosing, the next room starts.
12. **Captain's Post** holds the elite **Grunt Captain** (dark red, 140 HP, 9 damage every 1.2 s after a 0.6 s windup). **Warden's Crucible** holds the boss **Forge Warden** (large grey, 300 HP, 10 damage every 1.8 s after a 1 s windup). At half health it turns red, the status reads **Forge Warden is enraged!** and it attacks twice as fast.
13. Killing the Warden clears the floor at once. After its level-up choice the panel reads **Checkpoint: extract or descend?** with **Extract / Bank all 96 gold and end the run** and **Descend / Secure 96 gold. Quicksilver Vaults: +20% enemy damage, +50% gold** (96 is the gold for killing everything on floor 1). **Extract** shows **Extracted**, **Vanguard escaped after clearing Ember Halls**, **Floor 1 | 6 rooms | Level 7 | XP 70**, **Gold banked: 96** and the build.
14. **Descend** instead secures the gold (**Gold 96  (0 at risk)**) and one second later starts **Floor 2 | Room 1/6 | Mercury Stair** with the hero's current health: descending does not heal. Every enemy has 40% more health, hits 50% harder (tier ×1.25, then Cursed Gold +20%) and pays 50% more gold, so the first Grunt has 70 HP, strikes for 9 and pays 8. The rooms are Mercury Stair (Grunt, Runner), Cold Crucible (Tank, Runner), Vault Gate (Grunt, Grunt), The Deep Forge (Mend or Temper), Sentry Hall (Grunt Captain, 196 HP) and Warden's Vault (Forge Warden, 420 HP).
15. Killing the floor 2 Warden ends the run: **Descent complete**, **Vanguard defeated the Forge Warden and cleared Quicksilver Vaults**, **Floor 2 | 12 rooms**, level, XP, **Gold banked: 250** and the build. The upgrade choice from that last kill is withdrawn. A hero death anywhere shows **Defeated** with **Vanguard fell to a {enemy} in {room}**, the floor and rooms cleared so far, and **Gold banked: n | lost: m** when gold was at risk: half of the gold earned since the last checkpoint, rounded down, is lost.
16. **Try again** is disabled for half a second, then tap it (rapidly is fine). Expect one reload into Ember Hall with 100 HP, Level 0, XP 0, Gold 0 and Sword 10 damage every 0.80s. Stopping and pressing Play again must give the same fresh run; only the saved profile (forge gold, relics, depth record) carries over.
17. The result screen also shows a gold line above the buttons naming the nearest relic: **Forge gold 0: Second Wind costs 80** after a first death with no gold, **Forge gold 96: Second Wind is ready to forge** after extracting from floor 1. Descending banks the secured gold in the profile at once, so a death on floor 2 still shows it.
18. Tap **Relic Forge** under **Try again** (after the half-second delay). The panel shows **Gold n | Deepest floor cleared: n**, a **Second Wind** card (80 gold) and a **Counterweight** card (150 gold) with their effects. An unaffordable card is dimmed and reads **80 gold: need 80 more**. Tapping an affordable card, even rapidly, spends its price once and marks it **Equipped**; tapping an owned card equips it for free. **Start run** begins a new run.
19. With Second Wind equipped the HUD shows **Relic: Second Wind** under the weapon line. The first time a hit leaves the hero at 25 HP or less, 25 HP returns and the line reads **Relic: Second Wind (1x)**; it does not trigger again in that run.
20. With Counterweight equipped every Grunt Strike is answered by a counter for 60% of the Sword's damage (6 at 10 damage, more after Tempered Edge), and the HUD counts the counters. The result build starts with the relic, for example **Build: Counterweight (7x), Tempered Edge x5**.
21. Progress is saved in `profile.json` under `Application.persistentDataPath`. In the Editor on Windows that is `%USERPROFILE%/AppData/LocalLow/CryptforgePrototype/Project Cryptforge/`. Stop and press Play again: forge gold, relics and the equipped relic remain. To start over, stop Play and delete `profile.json`, `profile.json.bak` and `profile.json.tmp` from that folder.

## Data edits and edge cases

Perform data edits outside Play Mode; ScriptableObject Inspector edits during Play Mode can persist in Unity.

- Select `Data/Weapons/Weapon_Sword.asset`: Damage **10**, Interval **0.8**, Range **3**. Change Damage to **25**, enter Play Mode, and expect two hits. Stop and restore **10**.
- Change Interval to **1.5** and expect visibly slower hits. Stop and restore **0.8**. Runtime snapshots intentionally do not hot-reload asset edits.
- Change Range to **1**; the center-to-center separation is 2.4, so no hits should occur. Stop and restore **3**. There is no movement system to close this gap.
- Select `Data/Weapons/Weapon_GruntStrike.asset`: Damage **6**, Interval **1**. Set Damage to **40** and expect death on the third strike in Ember Hall and an immediate **Defeated** result with **Floor 1 | 0 rooms**, **Gold banked: 0** and no upgrades. Stop and restore **6**. Clearing `_weapon` on `Enemy_Grunt.asset` must throw `Room 0 wave 0 needs an enemy definition with a weapon and a prefab carrying Health, Targeting and AttackController.`
- Select `Data/Floors/Floor_EmberHalls.asset`: rooms are edited inline (name, kind, waves, forge options). Move the Forge Warden into Ember Hall's waves to fight it first. Stop and restore. A Forge room with waves, or a combat room without waves, must throw at startup with the room index.
- Select `Data/Floors/Floor_QuicksilverVaults.asset`: Enemy Health Multiplier **1.4**, Enemy Damage Multiplier **1.25**, Modifier **Modifier_CursedGold**, no Next Floor. Set Enemy Health Multiplier to **2**, descend, and expect a 100 HP Grunt. Stop and restore. Clearing Next Floor on `Floor_EmberHalls.asset` makes floor 1 final again: the Warden's kill shows **Descent complete** without a checkpoint. Pointing floor 2's Next Floor back at Ember Halls must throw `Floor Floor_EmberHalls links back into the descent.` Restore both.
- Select `Data/Floors/Modifier_CursedGold.asset`: Enemy Damage Percent **0.2**, Gold Percent **0.5**. Set Gold Percent to **1** and the floor 2 Grunt pays 10 gold. Stop and restore.
- Select `Data/Economy/PrototypeEconomy.asset`: At Risk Gold Loss **0.5**. At **1** a death loses all gold earned since the checkpoint, at **0** none. Stop and restore. Enemy definitions author Gold Reward: Grunt **5**, Runner **3**, Tank **10**, Grunt Captain **20**, Forge Warden **50**.
- Select `Data/Relics/Relic_SecondWind.asset`: Amount **0.25**, Threshold **0.25**, Price **80**. Set Price to **0** to forge it with no gold. Stop and restore. `Relic_Counterweight.asset`: Amount **0.6**, Price **150**; with Amount **5** the first counter (50 damage) kills the Grunt outright. Stop and restore.
- With Play stopped, damage `profile.json` (for example delete its closing brace) and press Play. The Console warns that the profile was **Recovered**, the previous save from `profile.json.bak` loads, and the damaged file is kept as `profile.json.unreadable`.
- Select `Data/Forge/Forge_Mend.asset`: Amount **0.4**. Set **1** to heal to full. Stop and restore.
- Select the **Forge Warden** prefab: `EnrageBehaviour` Health Fraction **0.5**, Attack Speed Bonus **1**. Set the fraction to **0.9** to see the enrage early. Stop and restore.
- Select `Data/Weapons/Weapon_TankSlam.asset`: Initial Delay **1.5**. Set it to **0** and the Tank slams on its first frame. Stop and restore **1.5**.
- Select the **Encounter** object: Advance Delay **1**. Set it to **3**, choose an upgrade, and expect a three-second gap before the next wave or room. Stop and restore **1**. Prefab edits to `Grunt.prefab` also reach its variants (Runner, Tank, Grunt Captain, Forge Warden) unless they override the same property.
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

1. Select **EditMode → Run All**: expect 119 passing cases covering the profile (forging, equipping, repairs, depth record), the Relic Forge rules, checkpoint and end-of-run banking, relic triggers, the save file (first launch, round trip, corrupt main file, interrupted saves, newer version, repaired values), a Descent simulation with each relic, gold securing and settlement by outcome, kill gold, floor scaling, the checkpoint decision and its withdrawal, the boss level-up before the checkpoint, a two-floor Descent simulation, floor stepping, healing, run outcomes, bonus upgrades, forge visits, the combined choice prompts, the enrage rule, simulations of the authored floor with Mend and Temper, weapon windups, Runner/Tank fight simulations, damage validation, health clamping, duplicate/reentrant death, cadence, pause, missing/dead targets, independent weapon state, the five-hit balance fixture, kill-reward idempotency, stat modifier order and clamps, level thresholds, single/stale/re-entrant upgrade selection with stack limits, encounter progress rules, upgraded next-fight simulations, run end rules, enemy strike damage and a full-run death simulation.
2. Select **PlayMode → Run All**: expect 28 passing tests. Each test uses its own temporary profile folder, never the Editor's saved progress. They cover the Relic Forge locked with an empty profile, forging and re-equipping relics with the saved profile carried into the next run, Counterweight answering every Grunt Strike, Second Wind healing once, saved gold after extraction, descent and victory, the authored Ember Halls rooms and waves ending at the checkpoint and an extraction banking 96 gold, descending with secured gold and a 70 HP Grunt that hits for 9 on floor 2, a floor 2 death losing half the unsecured gold, both floors to **Descent complete** with 250 gold, Mend healing 40%, Temper opening an extra upgrade, the Warden's enrage and the checkpoint after its kill, the full automatic kill with 10 XP, a single experience and gold award under repeated lethal damage, range/disable/reacquisition, destroyed targets, pause/resume, dead-owner behavior, the upgrade panel opening and pausing combat, rapid taps applying once, attack-speed selection updating runtime stats and the HUD, taps before the input delay being ignored, no encounter advance while choosing, kill → XP → choose → next Grunt chains for both upgrades, Grunt strike damage, death to a Grunt with the result explaining it, the result build list, and a single Try again reload into a fresh run.
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

The implemented run is a two-floor Descent: six rooms per floor ending in a boss, a forge choice on each floor, gold with an Extract/Descend checkpoint after floor 1, a scaled floor 2 with the Cursed Gold modifier, a Descent complete, Extracted or Defeated result and a one-tap restart. Banked gold is saved locally and spent in the Relic Forge on two relics. Additional weapon behaviors (the next gold sink), analytics and an in-game progress reset are still pending.
