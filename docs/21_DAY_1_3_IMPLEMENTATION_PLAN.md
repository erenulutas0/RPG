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

## Day 3 — reward and upgrade slice (planned only)

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
