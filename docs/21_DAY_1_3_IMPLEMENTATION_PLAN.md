# Day 1–Day 3: smallest mechanical prototype plan

Date: 2026-09-12. Scope follows `16_FIRST_PROMPT.md`, with `01`, `03`, and `06` as primary constraints. All 22 original Markdown documents were read before implementation. The repository initially contained only `docs/`, with no Unity project, source code, or Git repository.

The first requested implementation stops after **one hero automatically attacks one enemy until the enemy dies**. Day 3 is planned, not implemented. This is not the full mechanical milestone or Gate A.

## Project and package setup

- Project root: `E:/MobileGame`; Git initialized on `main`, with no commit or remote created.
- Pin Unity **6000.0.65f1**, actual installed revision `a18e2220bd50`. Unity corrected the initially hand-authored revision during first import; the Editor version number is unchanged. This is a fixed prototype baseline, not a claim that it is the newest release. See [official release notes](https://unity.com/releases/editor/whats-new/6000.0.65f1).
- Built-in renderer, orthographic 2D camera, portrait. No render pipeline dependency is needed for solid-color SpriteRenderers.
- Pin Unity Test Framework **1.6.0** in `Packages/manifest.json`, matching the version bundled with the installed 6000.0.65f1 Editor. The initial documentation-based choice was 1.4.3, but the actual Editor resolves its built-in 1.6.0; the manifest now states that effective version explicitly. Unity generated `packages-lock.json`, resolving built-in NUnit 2.0.5 plus IMGUI and JSON serialization modules. Both test suites passed with this resolved stack.
- The built-in IMGUI module supplies the temporary, read-only HUD. Text rendering types ship with the Editor and do not have a separate `com.unity.modules.textrendering` package in this version; the first real import exposed and removed that invalid dependency. Before Day 3's interactive upgrade choice, add uGUI and replace this HUD with a Canvas using the package version resolved for the pinned Editor. No input package is needed for automatic combat.
- Android-first player settings: portrait, ARM64/IL2CPP, minimum API 26, target API automatic. These are prototype build settings; store submission policy validation is later work.
- Install Android Build Support, SDK/NDK, and OpenJDK through Unity Hub when doing the first Android build. Desktop Play Mode does not require these modules.
- Point-filtered 16x16 solid white placeholder texture, tinted/scaled in scene. This does not lock final character resolution or art style. Defer Pixel Perfect and Aseprite packages until an actual pixel-art pipeline is being validated.

## Day 1 — project and authored scene

Implementation order:

1. Add `.gitignore`, `.gitattributes`, project version, package manifest, portrait PlayerSettings, text serialization/visible meta settings, and build scene list.
2. Author `Assets/_Project/Scenes/Gameplay/Gameplay.unity` with this hierarchy:

```text
Main Camera                   orthographic; dark solid background
Vanguard                      Health, Targeting, AttackController, CombatantView
  Hero Body                   blue SpriteRenderer
    Sword Placeholder         narrow light rectangle
    Shield Placeholder        dark blue rectangle
Grunt                         Health, CombatantView
  Enemy Body                  orange SpriteRenderer
Combat Setup                  definition-to-runtime composition
Prototype HUD                 read-only labels and health bars
```

3. Add one shared shape asset and three static gameplay definitions, then wire every reference explicitly in the scene.

Acceptance: project imports without errors; the gameplay scene contains one hero and one enemy; both are legible at 9:16 and 9:19.5; Gameplay is the only enabled build scene. No bootstrap/menu scene is needed before there are persistent services.

## Day 2 — first playable combat slice (implemented)

Implementation order:

1. `DamageContext`, `IDamageable`, and `HealthState`: validate damage, clamp health, and emit one death transition.
2. `Health`: component adapter for per-instance health and events.
3. `WeaponDefinition` and `WeaponRuntime`: author stats and snapshot them into per-attacker state.
4. `Targeting`: scan an explicitly serialized candidate list for the nearest active living target within range. No scene searches, physics, or global registry.
5. `AttackController`: tick cadence, acquire target, apply direct damage, and emit attack feedback. First hit is immediate; no accumulated burst after a long frame.
6. `CombatSetup`: initialize both health instances and one weapon in `Awake`. Views bind events separately; combat begins in `Update` after initialization.
7. `CombatantView` and `PrototypeHud`: show hits, HP, death, and attack count. HUD string formatting occurs on events rather than every frame.
8. Add calculation/state tests, authored-scene tests, static reference checks, and this verification guide.

Acceptance:

- With no input: five hits of 10 damage defeat the 50 HP Grunt in about 3.2 seconds at normal frame rates (well within the documented 15-second first-kill target).
- Enemy HP follows 50 → 40 → 30 → 20 → 10 → 0; the first update may already show 40.
- Death fires once, the Grunt body disappears, and attacks remain at five afterward.
- Outside a 3-unit center-to-center range or while disabled/destroyed, the target is not attacked. Returning a living target to range resumes attacks.
- Pausing scaled time stops attacks; a dead/disabled hero cannot attack.
- Combat does not modify any ScriptableObject. A fresh Play Mode session has fresh health and weapon state.
- Both Unity test suites pass (26 EditMode and five PlayMode cases). The Android development APK also passed a short smoke test on Samsung SM-S911B; see `23_ANDROID_DEVICE_VALIDATION.md` for evidence and remaining profiling limits.

## Day 3 — reward and upgrade slice (implemented: kill → XP, upgrade choice, next encounter)

### Implemented 2026-09-12: kill → XP once

| Files | Change |
|---|---|
| `Scripts/Core/RunState.cs` | New. Plain C# run XP with `ExperienceChanged`; rejects negative amounts. Level, phase and upgrade state are not added yet. |
| `Scripts/Economy/RewardService.cs` | New. `TryAwardKill(IDamageable)` rewards a dead victim once, tracked in a set, so repeated or re-entrant death callbacks cannot duplicate XP. |
| `Scripts/Content/EconomyConfig.cs` | New. Only `_experiencePerKill`; the level threshold arrives with the upgrade slice. |
| `Data/Economy/PrototypeEconomy.asset` | New. 10 XP per kill. |
| `Scripts/Core/CombatSetup.cs` | Requires `EconomyConfig`, creates `RunState` and `RewardService`, awards on `Grunt.Died`, unsubscribes on destroy; exposes `Run`. |
| `Scripts/UI/PrototypeHud.cs`, `Scripts/Content/PrototypeTextDefinition.cs`, `Data/UI/PrototypeText.asset` | XP label from `prototype.experienceFormat`. The HUD listens to `ExperienceChanged`, because `Health.Changed` fires before `Died` and would refresh before the award. |
| `Scenes/Gameplay/Gameplay.unity` | Two references: `CombatSetup._economy` and `PrototypeHud._setup`. |
| `Tests/EditMode/RewardTests.cs`, `Tests/PlayMode/CombatSceneTests.cs`, `Tools/CombatChecks/CombatChecks.csproj` | Eight reward/run-state cases; the kill test asserts 10 XP; a new test applies lethal damage twice and expects one award. |

Deliberate deviation from the table below: the tests live in `RewardTests.cs` rather than `RewardAndUpgradeTests.cs` until upgrade tests exist.

Verified: Unity EditMode 34/34, PlayMode 6/6, `Verify-Project.ps1`, .NET CombatChecks 34/34, development APK on Samsung SM-S911B showing `XP: 0` during combat and `XP: 10` after the fifth hit (see `23`).

### Implemented 2026-09-13: XP threshold → choose one of two upgrades

| Files | Change |
|---|---|
| `Scripts/Progression/ModifierOperation.cs`, `StatModifier.cs`, `ModifiableStat.cs`, `WeaponStat.cs` | New. `Value = (base + Σflat) × (1 + Σpercent)`, clamped to a safety minimum. Percents are summed, not compounded, per `05`. Enum values are serialized, so they are append-only. |
| `Scripts/Progression/UpgradeOption.cs`, `UpgradeOffer.cs`, `UpgradeService.cs` | New, plain C#. One offer per pending level, deterministic pool order, stack limits. `TrySelect(offer, slot)` closes the offer before side effects and rejects stale or repeated offers by reference, so double taps and re-entrant callbacks apply one upgrade. |
| `Scripts/Content/UpgradeDefinition.cs`, `Data/Upgrades/Upgrade_Damage.asset`, `Upgrade_AttackSpeed.asset` | New. Tempered Edge: +5 flat damage. Quickened Grip: +25% attack speed (0.8 s → 0.64 s). Five stacks each. `CreateOption()` snapshots data like `WeaponDefinition.CreateRuntime()`. |
| `Scripts/Combat/WeaponRuntime.cs` | Damage and attack speed are `ModifiableStat`s; `Interval = baseInterval / attackSpeed`. `AddModifier` raises `StatsChanged`. A running cooldown keeps its remaining time. |
| `Scripts/Core/RunState.cs`, `Scripts/Content/EconomyConfig.cs`, `Data/Economy/PrototypeEconomy.asset` | Level, pending and applied upgrade counts; `_experiencePerLevel: 10` (linear until encounters exist) and `_upgradeChoiceCount: 2`. |
| `Scripts/Core/CombatSetup.cs` | Builds upgrade options from `_upgrades` and composes `UpgradeService`. Sets `Time.timeScale` to 0 while an offer is open and restores it on close or destroy. uGUI input uses unscaled time. |
| `Scripts/UI/RunHud.cs`, `UpgradeChoiceView.cs`, `SafeAreaFitter.cs` | New uGUI Canvas HUD replacing IMGUI (`PrototypeHud.cs` removed). The weapon label now reads runtime stats. Two 320-unit touch cards on a dimmed overlay; cards disable on the first tap and stay disabled for 0.25 s unscaled after showing. Content is kept inside `Screen.safeArea` because the Android player renders behind the cutout. |
| `Scripts/UI/CombatantView.cs` | Feedback uses unscaled time so a hit flash settles during the pause. |
| `Scenes/Gameplay/Gameplay.unity` | HUD Canvas, Upgrade Canvas and EventSystem, generated by a temporary Editor builder that was deleted after saving. The scene is now Unity-serialized. |
| `Packages/manifest.json`, `packages-lock.json`, asmdefs | `com.unity.ugui` 2.0.0 (built into this Editor); lock gained only `com.unity.ugui` and `com.unity.modules.ui`. Gameplay, Editor and PlayMode test assemblies reference `UnityEngine.UI`. |
| `Tests/EditMode/UpgradeTests.cs`, `Tests/PlayMode/UpgradeFlowTests.cs`, `CombatSceneTests.cs` | 19 cases: modifier order, percent summing, clamps, validation, cooldown behaviour, level thresholds, single/stale/re-entrant selection, stack limits. 4 PlayMode tests: kill opens two choices and pauses, rapid taps apply once, attack speed changes only runtime stats and the HUD, taps before the input delay are ignored. The source Sword asset is asserted unchanged. Existing waits after a kill use real time. |
| `Tools/Verify-Project.ps1`, `Tools/CombatChecks/CombatChecks.csproj` | Package script GUIDs resolve from `Library/PackageCache` or the Editor's built-in packages; the .NET mirror compiles `Scripts/Progression`. |

Deviations from the planned table below: no `UpgradeChoicePanel.prefab` yet (one panel, authored in the scene; extract when a second screen reuses it); `ModifiableStat`, `UpgradeOption` and `UpgradeOffer` were added so the service stays plain C# and runs in the .NET mirror; offers are deterministic rather than random.

Verified: Unity EditMode 53/53, PlayMode 10/10, `Verify-Project.ps1`, .NET CombatChecks 53/53, development APK on Samsung SM-S911B (see `23`).

### Implemented 2026-09-13: next Grunt encounter after the choice

| Files | Change |
|---|---|
| `Prefabs/Enemies/Grunt.prefab` | New. The authored Grunt (Health, CombatantView, Enemy Body) extracted by a temporary Editor builder, which was deleted afterwards. The scene no longer contains a Grunt. |
| `Scripts/Combat/EncounterProgress.cs` | New, plain C#. Encounter number, hits taken this fight, scaled clear time (elapsed stops at the clear), and `Tick(deltaTime, canAdvance)` that counts the advance delay only while progression is allowed. |
| `Scripts/Combat/EncounterController.cs` | New. Spawns every encounter from the prefab at its own position with fresh `Health`, sets the hero's targeting candidates, raises `EncounterStarted`, `ProgressChanged` and `EnemyDefeated(Health)`. Starts the next encounter 1 s (scaled) after a clear, only when no upgrade offer is open; unsubscribes from and destroys the previous enemy first. |
| `Scripts/Combat/Targeting.cs` | `SetCandidates(Health[])`; the serialized list is empty in the scene. |
| `Scripts/Core/CombatSetup.cs` | No longer owns an enemy. Subscribes rewards to `EnemyDefeated` before `EncounterController.Initialize(Upgrades)` spawns the first Grunt. |
| `Scripts/UI/RunHud.cs`, `Scripts/Content/PrototypeTextDefinition.cs`, `Data/UI/PrototypeText.asset` | HUD follows the current encounter: `Encounter {0}: combat is automatic`, `Hits this fight: {0}`, and `{0} defeated in {1} hits, {2:0.0}s`. The enemy name now comes from the definition instead of a fixed "Grunt defeated" string. Subtitle shortened to fit one line. |
| `Scenes/Gameplay/Gameplay.unity` | `Encounter` object at (0, 1.2, 0) with the controller; `CombatSetup._encounters` and `RunHud._encounters` wired. |
| `Tests/EditMode/EncounterTests.cs`, `Tests/PlayMode/UpgradeFlowTests.cs` | 11 cases: numbering and reset, hit and clear-time counting, single clear, delay gated by `canAdvance`, validation, an earlier victim never rewarded again, and 60 Hz simulations showing +5 damage kills in 4 hits (~2.4 s) and +25% attack speed in 5 hits (~2.56 s vs 3.2 s). 3 PlayMode tests: no next encounter while the choice is open; damage upgrade → next Grunt is a new instance with 50 HP, targeted, killed in 4 hits, XP exactly 20 and a second offer; attack-speed upgrade → next clear at least 0.4 s faster. |

Unchanged by design: enemy stats do not scale between encounters (floors and scaling belong to the Descent slices), and every encounter is a Grunt.

Verified: Unity EditMode 64/64, PlayMode 13/13, `Verify-Project.ps1`, .NET CombatChecks 64/64, development APK on Samsung SM-S911B (see `23`).

### Implemented 2026-09-13 (Day 4 item): enemy attacks, hero death, result screen and restart

| Files | Change |
|---|---|
| `Data/Weapons/Weapon_GruntStrike.asset`, `Scripts/Content/EnemyDefinition.cs`, `Data/Enemies/Enemy_Grunt.asset` | New enemy weapon: 6 damage every 1.0 s, range 3. Enemies reference a `WeaponDefinition` and attack through the same `WeaponRuntime` path as the hero. |
| `Prefabs/Enemies/Grunt.prefab` | Gains `Targeting` and `AttackController` (owner and targeting wired inside the prefab); `CombatantView` lunges the Grunt 0.22 units toward the hero on each strike. Edited by a temporary builder that was deleted afterwards. |
| `Scripts/Combat/EncounterController.cs` | Requires the hero `Health`; each spawned enemy targets the hero and gets a fresh weapon runtime. The next encounter only starts while the hero is alive. Validates that the prefab is armed. |
| `Scripts/Core/RunState.cs` | `End()` raises `Ended` once; experience after the end is ignored. |
| `Scripts/Progression/UpgradeService.cs` | `Pool` for the result build list; ending the run withdraws an open offer and rejects further selections. |
| `Scripts/Combat/EncounterProgress.cs` | `EncountersCleared`. |
| `Scripts/Core/CombatSetup.cs` | Hero death ends the run. `RestartRun()` restores time scale and reloads the Gameplay scene once, rebuilding every runtime object from definitions. |
| `Scripts/UI/RunResultView.cs`, `Scripts/Content/PrototypeTextDefinition.cs`, `Data/UI/PrototypeText.asset` | Result canvas above the upgrade panel: **Defeated**, `{hero} fell to a {enemy} in encounter {n}`, encounters cleared / level / XP, the build (`Tempered Edge x5, Quickened Grip x5` or `no upgrades`), and a 260-unit **Try again** button that stays disabled for 0.5 s unscaled and disables itself on the first tap. |
| `Scenes/Gameplay/Gameplay.unity` | `Result Canvas` (sorting order 20) with the view; `Encounter._hero` wired. |
| `Tests/EditMode/RunEndTests.cs`, `Tests/PlayMode/RunResultTests.cs`, `CombatSceneTests.cs` | 7 cases: single end and ignored late XP, offer withdrawal, no reward after the end, pool stacks, cleared count, strike damage against the hero (76 HP after the first fight, 58 after an upgraded second), and a full-run simulation that must die within 5–20 encounters. 4 PlayMode tests: strikes deal authored damage; the Grunt kills a wounded hero and the result explains it while all attacks and encounters stop; the result lists cleared encounters and the build; Try again ignores early taps and reloads a fresh run exactly once. The first-kill scene test now expects the hero alive but damaged. |

Balance consequence, recorded for tuning: every encounter starts with a guaranteed Grunt strike, so a run always ends. Taking the first card every time dies in encounter 11 after 10 clears with both upgrades maxed, in about 30 seconds on device. Healing (the planned forge room), enemy scaling and Extract belong to the Descent slices.

Verified: Unity EditMode 71/71, PlayMode 17/17, `Verify-Project.ps1`, .NET CombatChecks 71/71, development APK on Samsung SM-S911B (see `23`).

### Implemented 2026-09-13 (Day 4 item): Runner and Tank archetypes

| Enemy | HP | Weapon | Windup | Prefab |
|---|---|---|---|---|
| Grunt | 50 | Grunt Strike 6 / 1.0 s | 0 | `Grunt.prefab`, orange 1.1 × 0.75 |
| Runner | 30 | Runner Slash 2 / 0.4 s | 0 | `Runner.prefab` variant, yellow 0.55 × 0.95, lunge 0.3 |
| Tank | 120 | Tank Slam 12 / 2.5 s | 1.5 s | `Tank.prefab` variant, purple 1.7 × 1.25, lunge 0.12 |

Encounters follow `Data/Encounters/PrototypeEncounters.asset`: Grunt, Runner, Grunt, Tank, repeating. XP per kill stays 10 for every enemy.

| Files | Change |
|---|---|
| `Scripts/Combat/WeaponRuntime.cs`, `Scripts/Content/WeaponDefinition.cs` | Optional `initialDelay` (windup before the first attack only); `_initialDelay` defaults to 0, so the Sword and Grunt Strike are unchanged. |
| `Scripts/Content/EnemyDefinition.cs` | `_prefab` (Health with Targeting and AttackController). |
| `Scripts/Content/EncounterSequenceDefinition.cs`, `Scripts/Combat/EncounterSchedule.cs` | Ordered, repeating enemy list; plain `IndexFor(encounterNumber, count)`. A stand-in for the Descent floor/room definitions. |
| `Scripts/Combat/EncounterController.cs` | Spawns `sequence.EnemyFor(n)` from its own prefab; validates every entry at startup; exposes `CurrentDefinition` (used by the HUD and result screen). |
| `Prefabs/Enemies/Runner.prefab`, `Tank.prefab`, `Data/Enemies/Enemy_Runner.asset`, `Enemy_Tank.asset`, `Data/Weapons/Weapon_RunnerSlash.asset`, `Weapon_TankSlam.asset` | New content. Variants override only body color, body scale and lunge distance; silhouettes differ so archetypes are readable without color. Created by a temporary Editor builder, deleted afterwards. |
| `Tests/EditMode/EnemyArchetypeTests.cs`, `Tests/PlayMode/EncounterSequenceTests.cs`, `UpgradeFlowTests.cs` | 13 cases: schedule order and validation, windup only delaying the first attack, Runner fight (3 hits, 4–5 chip strikes), Tank fight (12 hits, first slam at 1.5 s, 3 slams, 64 HP left), mixed run lasting longer than Grunts alone. 3 PlayMode tests: authored order with per-archetype prefabs and HP, Runner dying to two upgraded hits while chipping, Tank holding its first slam for the windup. Next-encounter tests became data-relative because encounter 2 is now a Runner. |

Balance measured with the 60 Hz simulation before choosing values (hero always takes the first card):

| Variant | Damage-first run | Speed-first run |
|---|---|---|
| Grunts only (previous slice) | dies in encounter 11 | dies in encounter 7 |
| Chosen Grunt/Runner/Grunt/Tank values | dies in encounter 15 | dies in encounter 7 |
| Same enemies, Quickened Grip +35% | 19 | 11 |
| Same enemies, Quickened Grip +50% | 19 | 15 |

Finding: +25% attack speed was clearly weaker than +5 damage, so choosing it first halved the run. The on-device run (damage first) died in encounter 15, matching the simulation.

**Tuning applied 2026-09-13 (approved by the owner):** `Upgrade_AttackSpeed.asset` is now **+50%** (0.8 s → 0.53 s on the first stack; percents still sum, so five stacks give +250% and 0.23 s). Simulated runs now die in encounter 19 when taking damage first and 15 when taking attack speed first. `EnemyArchetypeTests.EitherFirstPickSurvivesAComparableNumberOfEncounters` guards that neither card becomes a trap again (speed first must reach encounter 10 and at least 70% of the damage-first run). PlayMode expectations read the authored interval.

Verified: Unity EditMode 84/84, PlayMode 20/20, `Verify-Project.ps1`, .NET CombatChecks 84/84, development APK on Samsung SM-S911B including Runner, Tank windup and slam captures (see `23`).

### Implemented 2026-09-13: Descent floor 1 (Ember Halls)

| Room | Kind | Content |
|---|---|---|
| 1 Ember Hall | Combat | Grunt, Runner |
| 2 Cinder Walk | Combat | Runner, Grunt |
| 3 Slag Gate | Combat | Tank |
| 4 The Forge | Forge | **Mend** (restore 40% of maximum health) or **Temper** (one extra upgrade choice) |
| 5 Captain's Post | Elite | **Grunt Captain**: 140 HP, 9 damage / 1.2 s, 0.6 s windup |
| 6 Warden's Crucible | Boss | **Forge Warden**: 300 HP, 10 damage / 1.8 s, 1 s windup; at 50% health +100% attack speed |

| Files | Change |
|---|---|
| `Scripts/Combat/FloorProgress.cs`, `FloorStep.cs`, `FloorStepKind.cs`, `RoomKind.cs` | New, plain C#. Walks rooms and waves; zero-wave rooms are non-combat steps; tracks rooms cleared and the final wave. |
| `Scripts/Combat/IHealable.cs`, `HealthState.cs`, `Health.cs` | `Heal(amount)` clamps to maximum and never revives. |
| `Scripts/Combat/EnrageRule.cs`, `EnrageBehaviour.cs`, `AttackController.cs` | One-time threshold rule (never on the killing blow) and a boss behaviour module that adds an attack speed modifier to the enemy's runtime weapon. `AttackController.Weapon` exposes that runtime. |
| `Scripts/Core/RunState.cs`, `RunOutcome.cs` | `End(RunOutcome)` records Victory or Defeat once; `GrantBonusUpgrade()` and `PendingUpgradesChanged` let the forge add upgrade choices without levelling. |
| `Scripts/Progression/ForgeEffect.cs`, `ForgeOption.cs`, `ForgeOffer.cs`, `ForgeService.cs` | New. One selection per visit, same stale/re-entrant guards as upgrades; withdrawn when the run ends. |
| `Scripts/Progression/RunChoices.cs`, `ChoicePrompt.cs`, `ChoiceCard.cs`, `ChoiceKind.cs` | New single source for "the player is choosing": combines upgrade and forge offers into prompts for the panel, the pause and encounter gating. |
| `Scripts/Content/FloorDefinition.cs`, `RoomDefinition.cs`, `ForgeOptionDefinition.cs`, `Data/Floors/Floor_EmberHalls.asset`, `Data/Forge/*.asset`, `Data/Enemies/Enemy_GruntCaptain.asset`, `Enemy_ForgeWarden.asset`, `Data/Weapons/Weapon_CaptainCleave.asset`, `Weapon_WardenHammer.asset` | Floor data with rooms authored inline; forge options and the elite/boss content. |
| `Prefabs/Enemies/GruntCaptain.prefab`, `ForgeWarden.prefab` | Variants of Grunt and Tank; the Warden adds `EnrageBehaviour`, and its `CombatantView` switches to a red resting color when enraged. Created by a temporary builder, deleted afterwards. |
| `Scripts/Combat/EncounterController.cs` | Now runs the floor: validates every room at startup, spawns waves, opens the forge, waits for choices, and clears the floor immediately on the final wave's kill. Replaces the looping `EncounterSequenceDefinition`/`EncounterSchedule`, which were removed with their asset and tests. |
| `Scripts/Core/CombatSetup.cs` | Composes `ForgeService` and `RunChoices`; pauses on any open choice; hero death → Defeat, floor cleared → Victory. |
| `Scripts/UI/RunChoiceView.cs` (renamed from `UpgradeChoiceView`, same GUID), `RunHud.cs`, `RunResultView.cs`, `CombatantView.cs`, `PrototypeTextDefinition.cs`, `Data/UI/PrototypeText.asset` | Panel shows upgrade or forge prompts with the matching title; HUD shows `Ember Halls | Room n/6 | room`, wave or forge status and the enraged warning; result screen shows Floor cleared or Defeated with the room, rooms cleared n/6, level, XP and build. |
| Tests | EditMode `FloorTests` (12) with a floor simulation through the real run, reward, upgrade, forge and choice services; `EnemyArchetypeTests` keeps its fight cases. PlayMode `FloorFlowTests` (4): authored order to victory, Mend, Temper, Warden enrage and kill. `RunResultTests` and `UpgradeFlowTests` follow the new texts and view name. |

Balance chosen with the floor simulation (hero always takes the first upgrade card):

| | Damage first | Speed first |
|---|---|---|
| Mend at the forge | cleared, 38 HP left | cleared, 34 HP left |
| Temper at the forge | cleared, 17 HP left | cleared, 4 HP left |

Mend is the safer pick when hurt, Temper when healthy; the Warden threatens every path. XP stays at one level per kill for this floor. The measured fight time is only about 22 s plus choices and delays, so a first playthrough takes roughly a minute; floor 2 scaling and longer fights are the next lever toward the GDD's 3–4 minute floors.

Verified: Unity EditMode 89/89, PlayMode 21/21, `Verify-Project.ps1`, .NET CombatChecks 89/89, development APK on Samsung SM-S911B: a full floor cleared in 46 s with 42 HP left (see `23`).

### Implemented 2026-09-13: gold, the Extract/Descend checkpoint and Descent floor 2 (Quicksilver Vaults)

Clearing Ember Halls no longer ends the run. After the Warden's level-up the panel asks **Checkpoint: extract or descend?**

- **Extract** banks all gold and ends the run as **Extracted**.
- **Descend** secures the gold earned so far and, after the one-second advance delay, starts floor 2 with the hero's current health: no heal.
- A death banks the secured gold plus half of the gold earned since the last checkpoint, rounded down (`PrototypeEconomy.asset` At Risk Gold Loss 0.5). Extract and the final victory bank everything. Gold is shown and settled but not yet spent or saved; that is the Forge meta slice.

Gold per kill: Grunt 5, Runner 3, Tank 10, Grunt Captain 20, Forge Warden 50. Ember Halls pays 96. Floor 2 applies its tier (enemy health ×1.4, enemy damage ×1.25) and then the **Cursed Gold** modifier (+20% enemy damage, +50% gold, rounded away from zero), so it pays 154 and a full Descent 250.

| Room | Kind | Floor 2 content |
|---|---|---|
| 1 Mercury Stair | Combat | Grunt (70 HP, 9 damage / 1 s, 8 gold), Runner (42 HP, 3 damage / 0.4 s, 5 gold) |
| 2 Cold Crucible | Combat | Tank (168 HP, 18 damage / 2.5 s after 1.5 s, 15 gold), Runner |
| 3 Vault Gate | Combat | Grunt, Grunt |
| 4 The Deep Forge | Forge | Mend or Temper |
| 5 Sentry Hall | Elite | Grunt Captain (196 HP, 13.5 damage / 1.2 s after 0.6 s, 30 gold) |
| 6 Warden's Vault | Boss | Forge Warden (420 HP, 15 damage / 1.8 s after 1 s, enrages at 50%, 75 gold) |

| Files | Change |
|---|---|
| `Scripts/Core/RunState.cs`, `RunOutcome.cs` | Gold, secured gold and settlement on `End`: `GoldBanked`, `GoldLost`; new outcome `Extracted`; the loss fraction is validated to 0–1 and an ended run earns nothing more. |
| `Scripts/Economy/RewardService.cs` | `TryAwardKill(victim, gold)` pays experience and gold once per victim. |
| `Scripts/Combat/FloorScaling.cs` | New, plain C#: health = base × tier × (1 + modifier); damage bonus = tier × (1 + modifier) − 1, applied as a percent modifier on the enemy's runtime weapon; gold rounds away from zero. |
| `Scripts/Progression/CheckpointKind.cs`, `CheckpointOption.cs`, `CheckpointOffer.cs`, `CheckpointService.cs`, `RunChoices.cs`, `ChoiceKind.cs` | New checkpoint decision with the same stale and re-entrant guards, withdrawn when the run ends. `RunChoices` priority is upgrade, forge, then checkpoint, so the boss level-up comes first. |
| `Scripts/Content/FloorDefinition.cs`, `FloorModifierDefinition.cs`, `EnemyDefinition.cs`, `EconomyConfig.cs`; `Data/Floors/Floor_QuicksilverVaults.asset`, `Modifier_CursedGold.asset`; enemy, floor 1 and economy assets | Floors link through Next Floor with a scaling tier and an optional modifier; enemies author Gold Reward. |
| `Scripts/Combat/EncounterController.cs` | Validates the whole floor chain at startup (rejects cycles), scales every spawn, tracks the floor number and total rooms, and `DescendToNextFloor()` starts the next floor after the advance delay. |
| `Scripts/Core/CombatSetup.cs` | Floor cleared → checkpoint when a next floor exists, otherwise Victory. Extract → Extracted; Descend → secure gold, then descend. |
| `Scripts/UI/RunHud.cs`, `RunResultView.cs`, `RunChoiceView.cs`, `PrototypeTextDefinition.cs`, `Data/UI/PrototypeText.asset`, `Scenes/Gameplay/Gameplay.unity` | HUD: `Floor n \| Room n/6 \| room`, and a gold **Gold n (n at risk)** label replaces the attack counter. Result: Descent complete, Extracted or Defeated; `Floor n \| rooms \| Level \| XP`; a gold line with any loss. The scene was wired by a temporary builder, deleted afterwards. |
| Tests | EditMode `DescentTests` (11 cases), including a two-floor simulation through the real run, reward, upgrade, forge, choice and scaling code. PlayMode `DescentFlowTests` (3): descending with secured gold and scaled enemies, death on floor 2 losing half the unsecured gold, and both floors to Descent complete with 250 gold. `FloorFlowTests` now ends floor 1 at the checkpoint and extracts; `RunResultTests` and `CombatSceneTests` check the gold. |

Balance chosen with the Descent simulation (first upgrade card every time, Mend at floor 2's forge, always descending):

| Floor 1 forge | Damage first | Speed first |
|---|---|---|
| Mend | 38 HP after floor 1; clears floor 2 with 17 HP | 34 HP; clears floor 2 with 13 HP |
| Temper | 17 HP; dies in Vault Gate, floor 2 room 3 | 4 HP; dies in Mercury Stair, floor 2 room 1 |

Descending is a real bet: a healthy hero makes it, a hero who tempered on floor 1 should extract. Rejected alternatives in the same simulation: a 30% heal on descend let every path survive; health ×1.6 with the same ×1.5 damage killed the speed-first Mend path and left damage-first Mend at 2 HP.

Verified: Unity EditMode 100/100, PlayMode 24/24, `Verify-Project.ps1`, .NET CombatChecks 100/100, development APK built only after both reports passed (SHA-256 `567C672B40F18698A305AF57B0BE91639E85FA777C2DDCEAE038D6975862487D`).

### Original Day 3 plan (kept for reference)

Keep the existing gameplay scene. Implement one repeatable Grunt encounter and a two-choice numeric upgrade proof before adding enemy types or weapon behaviors.

Exact additions planned under `Assets/_Project/`:

| Files | Purpose |
|---|---|
| `Scripts/Core/RunState.cs` | Plain C# run XP, level, phase, and selected upgrade state |
| `Scripts/Economy/RewardService.cs` | Award a configured kill reward once, from death events |
| `Scripts/Content/EconomyConfig.cs` | XP-per-kill and level threshold data |
| `Scripts/Content/UpgradeDefinition.cs` | Stable ID, display text, stat, operation, amount, stack limit |
| `Scripts/Progression/StatModifier.cs` | Additive then multiplicative stat modification with clamps |
| `Scripts/Progression/UpgradeService.cs` | Apply a selected definition to runtime stats; prevent double selection |
| `Scripts/Combat/EncounterController.cs` | Own one active encounter and supply targeting candidates; continue after selection |
| `Scripts/UI/UpgradeChoiceView.cs` | Two large touch choices on a uGUI Canvas |
| `Scripts/UI/RunHud.cs` | Replace the temporary IMGUI HUD; show HP and XP |
| `Data/Economy/PrototypeEconomy.asset` | Configured XP reward and threshold |
| `Data/Upgrades/Upgrade_Damage.asset` | Numeric damage choice |
| `Data/Upgrades/Upgrade_AttackSpeed.asset` | Numeric attack cadence choice |
| `Prefabs/Enemies/Grunt.prefab` | Extract current enemy setup for sequential spawning |
| `Prefabs/UI/UpgradeChoicePanel.prefab` | Reusable touch choice panel |
| `Tests/EditMode/RewardAndUpgradeTests.cs` | Reward idempotency, stack limits, modifier order, definition immutability |
| `Tests/PlayMode/UpgradeFlowTests.cs` | Kill → award → choose → stronger next encounter |

Modify `WeaponRuntime`, `Targeting`, `CombatSetup`, and `Gameplay.unity` only to integrate the new slice. Remove `PrototypeHud` when replacing it, and migrate its text data to the new UI.

Order: reward/state tests → death-to-XP flow → data/modifier calculation → choice UI and pause → choice application → next Grunt encounter.

Acceptance: kill awards XP once; a configured threshold opens two choices and pauses encounter progression; one tap applies exactly one upgrade; rapid repeated taps cannot duplicate it; the next encounter uses modified runtime stats; the source Sword asset is unchanged. Tune the first-choice time toward 30–60 seconds once sequential encounter pacing exists. No persistence is required for this temporary run state.

## Actual first-slice file inventory

All Unity assets and asset directories include committed-ready `.meta` files with stable GUIDs. No empty future-system folders or placeholder abstractions are created.

```text
.gitignore
.gitattributes
README.md
Packages/manifest.json
Packages/packages-lock.json          generated by the Editor; retained
ProjectSettings/
  ProjectVersion.txt
  ProjectSettings.asset
  EditorSettings.asset
  VersionControlSettings.asset
  EditorBuildSettings.asset
Assets/_Project/
  Art/Placeholders/Square.png
  Data/
    Heroes/Hero_Vanguard.asset
    Enemies/Enemy_Grunt.asset
    Weapons/Weapon_Sword.asset
    UI/PrototypeText.asset
  Scenes/Gameplay/Gameplay.unity
  Scripts/
    Project.Gameplay.asmdef
    Core/CombatSetup.cs
    Combat/
      DamageContext.cs
      IDamageable.cs
      HealthState.cs
      Health.cs
      WeaponRuntime.cs
      Targeting.cs
      AttackController.cs
    Content/
      HeroDefinition.cs
      EnemyDefinition.cs
      WeaponDefinition.cs
      PrototypeTextDefinition.cs
    UI/
      CombatantView.cs
      PrototypeHud.cs
    Editor/
      Project.Editor.asmdef
      AndroidPrototypeBuild.cs
  Tests/
    EditMode/
      Project.EditModeTests.asmdef
      CombatStateTests.cs
    PlayMode/
      Project.PlayModeTests.asmdef
      CombatSceneTests.cs
Tools/
  Verify-Project.ps1
  CombatChecks/CombatChecks.csproj
docs/
  21_DAY_1_3_IMPLEMENTATION_PLAN.md
  22_FIRST_COMBAT_VERIFICATION.md
  23_ANDROID_DEVICE_VALIDATION.md
```

`19_DECISION_LOG_TEMPLATE.md` also gains an actual decision entry after the original template. All other original documentation remains unchanged.

## Deliberate tradeoffs and next boundary

- One runtime assembly plus two test assemblies supports tests without fragmenting the gameplay code.
- `HealthState` is the small testable rule object; `Health` bridges it to Unity. No damage-receiver or death-handler class is necessary yet.
- Sword uses one direct hit. Cleave, Bow projectiles, Staff AoE, Runner, Tank, room escalation, boss, result screen and one-tap restart belong to later mechanical slices. The Day 2 victory label is only combat feedback, not a run result system.
- The stationary hero and enemy start within range. Movement and enemy retaliation are not needed to prove this first slice.
- No pooling for two authored entities, no singleton/GameManager, no save, no backend, no SDK integration, and no final art.
- UI text lives in `PrototypeText.asset`; stable localization keys are `prototype.<fieldNameWithoutUnderscore>` and `<content_id>.name`. No localization framework is needed yet.
- The temporary IMGUI HUD deliberately trades production layout/performance for a small read-only prototype. Device performance has not been measured; replace it for the interactive upgrade slice.
