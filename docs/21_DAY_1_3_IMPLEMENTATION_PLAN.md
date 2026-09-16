# Day 1–Day 3: smallest mechanical prototype plan

## Stone/rim materials and actor contact shadows — 2026-09-16

`PlatformArt` now draws quiet upper bevels/lower lips, broad per-slab mineral patches and sparse chips, plus flush coping stones and brass clamps inside the existing rim. Surface dimensions, silhouette, central inlay/emblem, light frames and walkable geometry remain. Sidewall and lantern art are deliberately still the earlier prototype drawing. Actual centre/far/right Unity review renders at both portrait sizes and provenance are in `ArtDirection/2026-09-16/stone-contact-01/`.

`ContactShadowArt` supplies one 32x16 translucent ground patch and a pure conservative diamond-fit rule. `ContactShadowView` attaches below the hero/enemy root, independently of the animated body, sorts above the platform lights and below actors, follows movement, contracts at the edge and hides when health is dead. It borrows a lazily generated `ArenaSpriteCache` sprite across waves/reloads. Session reset/quit destroys sprite and texture. The extra texture costs 2,048 RGBA32 texel bytes; movement does not regenerate art. Chests have not received this actor component. There is no scene/prefab, gameplay, economy, telemetry, package-lock or project-settings edit.

Added `Art/ContactShadowArt.cs`, `UI/ContactShadowView.cs`, `Tests/EditMode/ContactShadowArtTests.cs` and new metadata. Changed `Art/PlatformArt.cs`, `Art/Unity/ArenaSpriteCache.cs`, `UI/ArenaView.cs`, `HeroLookView.cs`, `EnemyLookView.cs`, `Tests/PlayMode/ArenaArtCacheTests.cs` and `ArtHudFlowTests.cs`. Two pure tests validate the contact image's alpha/symmetry and sweep complete sprite bounds at three actor widths over symmetric and shifted arenas. One new scene test covers sorting, body-nudge independence, death, rim contraction and scene reload/unused-asset cleanup; the existing session-release test now includes the shadow resource. The temporary visual-staging fixture was removed before the full gate.

The first full gate caught `NullReferenceException` in `ContactShadowView.Start` when an existing test cloned a live enemy, including its generated child whose private state Unity does not serialize. `Attach` now rebinds the existing child/renderer to the new owner instead of leaving an orphan component and creating another patch. The clone test also verifies one shadow per clone and independent health/death visibility. The targeted nine-case `ArtHudFlowTests` passed after the fix; no APK was built from the failed gate.

**Verified:** .NET **262/262**, compile **0 warnings/errors**, EditMode **274/274**, PlayMode **80/80**, Android exit **0**, integrity **292 unique asset/folder GUIDs / 555 scene objects/components**. APK **24,739,335 bytes**, SHA-256 **`8DD7866A84537B9B4D91644C685F894717460A86DC1E8261BC506E2F3BEF5E3D`**, installed S23 hash matched. The fallback environment plus shadow cache retains **38 textures / 8,368,004 RGBA32 texel bytes**, excluding native overhead; the authored imported-cavern mode does not build unused void textures. Device observations and limits are in the corresponding `23` entry.

## Latest slice — wider portrait movement framing (2026-09-16)

Following the owner's normal-room/boss-arena direction, the gameplay camera width changes from 6 to 9. Actor scale, arena geometry, controls, HUD, balance and pack capacity are unchanged. Widths 6, 7.5 and 9 were compared with identical staged visuals at tall and short portrait target sizes; rationale and evidence limits are in `ArtDirection/2026-09-16/camera-review/README.md`. The furnace-cavern board in `arena-target/` is a future art target, not the current runtime scene.

Files: `Scenes/Gameplay/Gameplay.unity` (one camera field), `Tests/PlayMode/ArtHudFlowTests.cs` (extend existing guardian bounds coverage to width 9). The temporary offscreen render fixture was removed before the full gate. Live ten-enemy kiting, boss-specific geometry and new environment/actor art remain separate work.

**Verified:** .NET **239/239**, Unity compile **0 warnings/errors**, EditMode **251/251**, PlayMode **63/63**, Android build exit 0 and static integrity passed (272 project GUIDs; 555 scene objects/components). APK SHA-256 **`0E6CE9B82B47B6999B0AF68D8708B2882915AEDE0F16E861685755158FD1C4E2`**. The offscreen comparison has a separate final 1/1 capture-fixture pass; it is not included in the shipped suite count. Phone results are in the latest docs/23 entry.

## Latest slice — foundry result and Forge menus (2026-09-16)

The owner authorised extending the approved UI to the result and Relic Forge screens. This slice reuses the imported frame and two relic icons. It adds no artwork dependency, shop rule or save schema.

- Result: one opaque framed summary with outcome/cause, progress, banked/lost gold, build, next-unlock hint and two framed actions. Existing result strings and input-delay behaviour remain unchanged.
- Forge: separate weapon and relic sections, five full-row card targets, a persistent bottom Start Run action, and safe-area-relative vertical layout. Existing prices, effects and state text remain visible. Equipped/affordable/owned/too-expensive states have green/brass/blue/grey accents in addition to their written labels. Relic icons resolve from the current effect instead of being assumed from card order.
- Files: `Scenes/Gameplay/Gameplay.unity`, `Scripts/UI/RelicForgeView.cs`, `Tests/PlayMode/ArtHudFlowTests.cs`, `Tests/PlayMode/RelicForgeFlowTests.cs`. The one-shot Editor migration was removed after authoring; its local record is `TestResults/ApplyFoundryMenus.cs`. Art rationale and remaining work: `ArtDirection/2026-09-16/MENU_INTEGRATION.md`.
- Verification adds text-fit coverage at 1080x1760 and 1080x2232 safe-area sizes and extends the existing purchase/equip test with icon identity and the affordable-to-equipped accent change. Existing double-tap, price, persistence and touch-bound tests remain required.

**Verified:** .NET **239/239**, Unity compile check **0 warnings/errors**, EditMode **251/251**, PlayMode **63/63**, development APK build and static integrity passed (272 project GUIDs; 555 scene objects/components). APK SHA-256 **`B8176A69112EAFB7E8E7322B4768124102FACAE631F77E3B8BE6915D5B5D37E0`**. Device observations are recorded in `23_ANDROID_DEVICE_VALIDATION.md`.

## Latest slice — compact foundry UI (2026-09-15–16)

The owner approved the combined art direction and resumed Unity/device work after telemetry commit `127f4de`. Eight UI images from the approved art kits are now imported; no generated combat screenshot or replacement character sprite is used.

- Compact floor/room text, room pips, one gold counter, actual upgrade-stack badges, equipped relic state and a boss-only readout. Detailed weapon, target, relic and gold-at-risk text is available through Pause. Second Wind's badge shows remaining charges (1/0); Counterweight's number is triggers, not stacks or damage.
- A grouped lower HP/XP panel, a red health fill and blue XP fill. Forge Burst has separate glyph, circular mask, bezel, cooldown fill and countdown text. Imported assets survive scene reload; the procedural fallback owns only its own generated texture.
- Opaque, framed choice cards with live text and full-card hit areas. Upgrade icons resolve by stable content ID through the active offer, including when reaching a stack cap changes slot order. Forge, checkpoint and unillustrated future options do not inherit unrelated icons.
- A quieter local floor palette and fewer speckles in `PlatformArt`; dim inlays and a retained bright rim. Geometry, movement, characters, balance, telemetry and save rules are unchanged.
- Camera width 6 instead of 4.6. Full guardian body/bar bounds are checked analytically for both 6 and 7.5 on short/tall portrait layouts, with horizontal follow-lag allowance. The actual S23 review uses 6; a paired 6/7.5 phone comparison is not claimed.

Files: `Scripts/UI/RunHud.cs`, `RunChoiceView.cs`, `AbilityButtonView.cs`, `Scripts/Art/PlatformArt.cs`, `Data/UI/PrototypeText.asset`, `Scenes/Gameplay/Gameplay.unity`, eight textures and import metadata in `Art/UI/`, `Tests/EditMode/PlatformArtTests.cs`, and new `Tests/PlayMode/ArtHudFlowTests.cs`. Two temporary Editor migrations authored the scene/imports and corrected the first phone review's health colour and square icon corners; both were removed. Their local copies are under ignored `TestResults/`.

Art rationale, import limits and handoff: `ArtDirection/2026-09-15/UNITY_INTEGRATION.md`. Device evidence and any remaining limits: `23_ANDROID_DEVICE_VALIDATION.md`. Result/Relic Forge screens, the large pause title/resume control, per-badge tap details, walk/facing animation and shipping texture optimisation remain later art work.

**Verified:** after the device-driven corrections, .NET **239/239**, Unity compile check **0 warnings/errors**, EditMode **251/251**, PlayMode **62/62**, development APK build and static integrity passed (272 project GUIDs; 515 scene objects/components). APK SHA-256 **`8D1515975F8C3F1E0987FACC61379AFEF9B1E5239EA7D92C38DE23E0C517A149`**; the installed `base.apk` hash matches. S23 captures confirm final red HP/blue XP, circular ability artwork, cooldown response, readable choices and pause details; the earlier same-layout pass also reached the right-side guardian and Extract. See `23` for distinctions and remaining device coverage.

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

Verified: Unity EditMode 100/100, PlayMode 24/24, `Verify-Project.ps1`, .NET CombatChecks 100/100, development APK built only after both reports passed (SHA-256 `567C672B40F18698A305AF57B0BE91639E85FA777C2DDCEAE038D6975862487D`). On Samsung SM-S911B an automated Descend run cleared both floors in about 78 s with 21 HP left and banked 250 gold (see `23`).

### Implemented 2026-09-13: the Forge meta layer (local save, Relic Forge, two relics)

What the player sees:

- **Result screen:** a gold line names the nearest unlock, for example **Forge gold 96: Second Wind is ready to forge** or **Forge gold 0: Second Wind costs 80**. A **Relic Forge** button sits under **Try again**; both wait out the half-second input delay.
- **Relic Forge:** a panel over the result screen with **Gold n | Deepest floor cleared: n**, one card per relic (name, effect, then **Forge for n gold**, **n gold: need n more**, **Owned: tap to equip** or **Equipped**) and **Start run**.
  - Tapping an affordable relic forges and equips it; tapping an owned relic equips it. One relic is equipped at a time.
  - Unaffordable cards are dimmed. Every change briefly ignores taps, so rapid taps cannot buy twice.
- **During a run:** the HUD shows **Relic: name** under the weapon line and adds a trigger count once it fires. The result build lists the relic first, for example **Build: Second Wind (1x), Tempered Edge x5**.

| Relic (`Data/Relics`) | Price | Effect |
|---|---|---|
| Second Wind | 80 | Once per run, when a hit the hero survives leaves 25% health or less, restore 25% of maximum health |
| Counterweight | 150 | Every enemy hit the hero survives strikes that enemy back for 60% of the hero's current weapon damage |

Both change behavior rather than raw stats, as `03` and `24` §4.5 require. Counterweight fits the Vanguard's shield/counter identity and makes damage upgrades worth more; Second Wind is a one-time safety net that makes a greedier forge pick or descent survivable. A floor 1 extraction (96 gold) buys Second Wind; a full Descent (250) buys both.

**Gold flow:** `RunBank` deposits gold into the profile as soon as a checkpoint secures it, and the rest of the banked gold when the run ends, so a descent keeps its secured gold even if the app closes mid-floor. Clearing a floor raises the depth record. Every profile change is saved at once.

**Save:** `profile.json` in `Application.persistentDataPath`, versioned JSON (`saveVersion` 1) with a `revision` that grows on every save.

- **Saving:** writes and flushes `profile.json.tmp`, moves the old `profile.json` to `profile.json.bak`, then moves the temp file into place.
- **Loading:** takes the readable file with the highest revision among the three. An unreadable main file is copied to `profile.json.unreadable`, and a file from a newer build is never read.
- **Repairs:** negative gold, duplicate relic ids and an equipped relic that is not owned are fixed on load.
- **When it loads:** on every Gameplay scene load, so no Bootstrap scene or persistent service object is needed yet (decision in `19`).

| Files | Change |
|---|---|
| `Scripts/Core/PlayerProfile.cs` | New, plain C#: gold, owned and equipped relic, deepest floor cleared; forging spends, owns and equips as one change; damaged saved values are repaired. |
| `Scripts/Economy/RunBank.cs` | New: deposits secured gold at the checkpoint and the remainder when the run ends. |
| `Scripts/Progression/RelicEffect.cs`, `RelicOption.cs`, `RelicRuntime.cs`, `RelicStatus.cs`, `RelicShop.cs` | New: relic data with validation; one run's relic reacting to hits the hero survives (a killing blow never triggers it); the Relic Forge rules (status, next unlock, forge or equip on tap). |
| `Scripts/Combat/IHealable.cs`, `RelicBehaviour.cs` | `IHealable` exposes `Current`. `RelicBehaviour` on the Vanguard reports each drop in hero health with the current enemy as the attacker. |
| `Scripts/Save/ProfileSaveData.cs`, `ProfileMigration.cs`, `ProfileLoadStatus.cs`, `ProfileStore.cs`, `ProfileLocation.cs` | New: schema, version gate, atomic save and newest-readable load, and a test-only folder override. |
| `Scripts/Content/RelicDefinition.cs`, `Data/Relics/Relic_SecondWind.asset`, `Relic_Counterweight.asset` | Relic definitions with price, effect, amount and threshold. |
| `Scripts/Core/CombatSetup.cs` | Loads the profile, logs recovered or reset saves, composes `RelicShop` and `RunBank`, saves on every profile change (a failed write is logged and retried on the next change), applies the equipped relic and records cleared floors. |
| `Scripts/UI/RunHud.cs`, `RunResultView.cs`, `RelicForgeView.cs`, `PrototypeTextDefinition.cs`, `Data/UI/PrototypeText.asset`, `Scenes/Gameplay/Gameplay.unity` | Relic HUD line, forge hint and Relic Forge button on the result screen, the Relic Forge panel. The scene was wired by a temporary builder, deleted afterwards. |
| Tests | EditMode `RelicForgeTests` (12 cases), `ProfileStoreTests` (6: first launch, round trip, corrupt main file, interrupted saves, newer version, repaired values) and a relic case in the `DescentTests` simulation. PlayMode `RelicForgeFlowTests` (4): the locked Forge with an empty profile; forging, re-equipping, saving and starting a run with the relic; Counterweight answering Grunt strikes; Second Wind healing once. Every scene test now uses its own temporary profile folder; `FloorFlowTests`, `DescentFlowTests` and `RunResultTests` check the saved gold and forge hint. |

Balance chosen with the Descent simulation (first upgrade card every time, Mend at floor 2's forge, always descending; health after floor 1 → end of floor 2):

| Floor 1 path | No relic | Counterweight 60% | Second Wind 25%/25% |
|---|---|---|---|
| Damage first, Mend | 38 → 17 | 60 → 39 | 38 → 42 |
| Damage first, Temper | 17 → dies in floor 2 room 3 | 39 → 18 | 42 → 21 |
| Speed first, Mend | 34 → 13 | 42 → 21 | 34 → 38 |
| Speed first, Temper | 4 → dies in floor 2 room 1 | 12 → dies in floor 2 room 1 | 29 → dies in floor 2 room 3 |

Each relic rescues the damage-first Temper descent but not the greediest path. Rejected in the same simulation:

- Counterweight at 50% rescued no path.
- Second Wind once per floor let every path survive, including speed first with Temper.

Known gaps:

- After both relics (230 gold) gold has no sink; weapon unlocks from the next slice are the planned sink.
- The analytics events `currency_spent` and `meta_upgrade` wait for an analytics service.
- There is no in-game progress reset.
- Closing the app mid-floor forfeits gold not yet secured.

Verified: Unity EditMode 119/119, PlayMode 28/28, `Verify-Project.ps1`, .NET CombatChecks 113/113, development APK built only after both reports passed (SHA-256 `32523A4D00FCC7142DBB59361FCEEB9208769A9B29C24F9BE553EE9BBE05628E`).

### Implemented 2026-09-13: pause control

Owner feedback from the Relic Forge device run: a run starts at once and there is no way to pause it.

- **Pause button:** a **II** button in the HUD's top-right corner pauses the run. Combat, the advance delays and all timers freeze, and a **Paused** overlay (**Combat waits until you resume**) offers **Resume**. Resume ignores taps for 0.3 s after the overlay appears.
- **Leaving the app:** Home, a call or switching apps pauses the run too, so returning shows **Paused** instead of combat carrying on. A choice panel that is already open stays in charge: it already waits, so no second overlay appears.
- **When the button shows:** only during live combat; it is hidden while a choice is open, while paused and after the run ends.

| Files | Change |
|---|---|
| `Scripts/Core/RunPause.cs` | New, plain C#: the single owner of "is the run frozen?". An open choice and the player's pause each hold the run; pausing is refused during a choice or after the run ends, and the end releases a pause. |
| `Scripts/Core/CombatSetup.cs` | Applies `RunPause` to `Time.timeScale` (the choice handler no longer sets it directly), reports choice state to it, and pauses on `OnApplicationPause(true)`. |
| `Scripts/UI/PauseView.cs`, `PrototypeTextDefinition.cs`, `Data/UI/PrototypeText.asset`, `Scenes/Gameplay/Gameplay.unity` | HUD pause button and a **Pause Canvas** (sorting order 30, above choices and results) with the overlay. The scene was wired by a temporary builder, deleted afterwards. |
| Tests | EditMode `RunPauseTests` (4). PlayMode `PauseFlowTests` (4): the button freezes combat until Resume; leaving the app pauses the run while an open choice stays in charge; the button hides when the run ends; every button's canvas can receive touches. |

Verified first: Unity EditMode 123/123, PlayMode 31/31, `Verify-Project.ps1`, .NET CombatChecks 117/117, development APK built only after both reports passed (SHA-256 `07CDD30A40ADDD2DD1D0FAE38923DE995B043379E2368CA91D336AD6E3DCFC9B`).

**Device finding and fix:** on the phone the pause button ignored real touches.
- **Cause:** the HUD canvas had never needed input, so it had no `GraphicRaycaster`. The flow tests click buttons with `ExecuteEvents` directly and therefore bypass raycasting.
- **Fix:** a temporary builder added the raycaster (deleted afterwards).
- **Guard:** `PauseFlowTests.EveryButtonSitsOnACanvasThatReceivesTouches` now checks that every button's root canvas has one. Run against the unfixed scene it failed with **Pause Button is on HUD Canvas, which has no GraphicRaycaster**.

Verified after the fix: Unity EditMode 123/123, PlayMode 32/32, `Verify-Project.ps1`, development APK built only after both reports passed (SHA-256 `15A602FB9EA45855429BF319088453A2C11E650050E7775EC43AD863290E80C5`). On the phone the button now pauses the run, and leaving the app pauses it too (see `23`).

**Second device finding:** the **II** label floated above its 120-unit button. It had been copied from the 260-unit **Try again** label, which is anchored to the bottom with a fixed height; the Relic Forge button label was 30 units high for the same reason. Both labels now stretch to fill their buttons. `PauseFlowTests.EveryButtonLabelSitsInsideItsButton` guards every button; run against the scene before this fix it failed with **Forge Button Label extends outside Forge Button**.

Verified after both fixes: Unity EditMode 123/123, PlayMode 33/33, `Verify-Project.ps1`, development APK built only after both reports passed (SHA-256 `0F8D8EB6E8F2821C2426774A215031285650C8D05BD8EBD9E9B34D0D62BBD6AD`). The corrected label was later captured on the phone (see `23`).

### Implemented 2026-09-13: packs, the Cinder Mite and the Sword's cleave (weapon behaviors, part 1)

The GDD's weapon set (Sword cleave, Bow pierce, Staff AoE) only matters against several enemies at once, so the owner chose to add packs and three weapons together. This first part adds packs and the Sword's cleave; Staff, Daggers and weapon unlocks follow.

- **Packs:** a wave now spawns one to three enemies in a row in front of the hero, centre first, then left and right, 1.7 units apart. Targeting breaks distance ties in favour of the earlier slot, so the hero always fights the first living enemy in slot order. The HUD bar follows that enemy, and the label counts the rest, for example **Cinder Mite | 15 / 15 HP  (+2 more)**. A wave clears when its last enemy falls: **3 enemies defeated in 6 hits, 2.4s**.
- **Cinder Mite:** a new fodder enemy, a small ember-red variant of the Grunt prefab. It has 15 HP, deals 1 damage every 1.5 s, and gives 1 gold and 3 XP. The named enemies keep their stats and now give 10 XP each.
- **Sword cleave:** each swing also strikes the nearest other living enemy within 2 units of the target, for 60% of the damage.
- **Hits carry their attacker:** `DamageContext.Source` is set by `WeaponRuntime`, and health raises `Damaged` before `Changed`. Counterweight strikes back at the actual attacker in a pack, and a defeat names the last enemy whose hit landed.
- **Experience:** XP per kill moved from `EconomyConfig` onto each enemy. Each level now costs one more than the last (10, 11, 12, ...; `_experienceGrowth` 1), so frequent small kills do not use up the ten upgrade stacks on floor 1.

| Floor | Room | Waves |
|---|---|---|
| Ember Halls | Ember Hall | Grunt · 2 Mites · 3 Mites |
| | Cinder Walk | Runner + Mite · 3 Mites · Grunt + 2 Mites |
| | Slag Gate | Tank · 3 Mites |
| | Captain's Post | Grunt Captain + 2 Mites |
| | Warden's Crucible | Forge Warden |
| Quicksilver Vaults | Mercury Stair | Grunt + Mite · 3 Mites |
| | Cold Crucible | Tank + Mite · Runner + 2 Mites |
| | Vault Gate | Grunt + 2 Mites |
| | Sentry Hall | Grunt Captain + Mite |
| | Warden's Vault | Forge Warden |

Ember Halls now pays 109 gold (a floor 1 extraction still buys Second Wind) and a full Descent 270.

| Files | Change |
|---|---|
| `Scripts/Combat/PackLayout.cs`, `WeaponBehavior.cs` | New: pack slot offsets; DirectHit and Cleave. |
| `Scripts/Combat/WeaponRuntime.cs`, `AttackController.cs`, `Targeting.cs` | Cleave with splash radius and fraction; attacks pass their owner as the source and gather splash candidates, nearest first, only when ready; earlier candidates win distance ties. |
| `Scripts/Combat/DamageContext.cs`, `HealthState.cs`, `Health.cs`, `RelicBehaviour.cs` | Damage source and the `Damaged` event; the relic reacts to the actual attacker. |
| `Scripts/Combat/EncounterController.cs`, `Scripts/Content/WaveDefinition.cs`, `RoomDefinition.cs` | Waves of one to three enemies, per-enemy gold, `WaveEnemyAt`, `DefinitionOf`, `GoldRewardOf`, validation of pack sizes. |
| `Scripts/Core/RunState.cs`, `CombatSetup.cs`, `Scripts/Economy/RewardService.cs`, `Scripts/Content/EnemyDefinition.cs`, `EconomyConfig.cs`, `WeaponDefinition.cs` | Experience growth; per-enemy experience and gold; the last attacker; weapon behavior data. |
| `Scripts/UI/RunHud.cs`, `RunResultView.cs`, `PrototypeTextDefinition.cs`, `Data/UI/PrototypeText.asset` | Pack health line and pack victory status; the defeat names the last attacker. |
| `Data/Enemies/Enemy_CinderMite.asset`, `Data/Weapons/Weapon_MiteBite.asset`, `Prefabs/Enemies/CinderMite.prefab`, both floors, every enemy and weapon asset, `PrototypeEconomy.asset`, `Gameplay.unity` | Content and data; the prefab variant and scene re-save came from a temporary builder, deleted afterwards. |
| Tests | New `DescentSimulation` shared by `FloorTests` and `DescentTests`: packs in slot order, cleave, enrage and relics through the real services. New `PackTests` (6). PlayMode tests describe waves by what spawned; the hero's own cleaves may finish enemies in any order. New `FloorFlowTests.PacksSpawnSideBySideAndTheHeroFightsThemInSlotOrder`. |

Balance was chosen with a scored search over mite stats, experience growth, cleave strength and floor 2 packs. A single-target Sword against these packs died in room 2 on every path. Results (first upgrade card every time, Mend on floor 2, always descending; health after floor 1 → end of floor 2):

| Path | No relic | Counterweight | Second Wind |
|---|---|---|---|
| Damage first, Mend | 54 → 40 | 75 → 61 | — |
| Damage first, Temper | 24 → dies in floor 2 room 3 | 35 → 21 | 49 → 35 |
| Speed first, Mend | 33 → 13 | — | — |
| Speed first, Temper | 12 → dies in floor 2 room 1 | 19 → dies in floor 2 room 3 | 37 → 20 |

- **Level-ups:** 8 upgrades by the end of floor 1, and the tenth on floor 2.
- **Counterweight:** cuts the damage-first Mend fight time from 22 s to 15 s.
- **Second Wind:** now also carries the greediest path.
- **Pacing:** simulated fight time for floor 1 is still only about 16 s, so pacing remains the open gap; longer rooms and more waves are the lever.

Verified: Unity EditMode 129/129, PlayMode 34/34, `Verify-Project.ps1`, .NET CombatChecks 123/123, development APK built only after both reports passed (SHA-256 `AA38FA1DD344F8E50E0567397F60362DD0BF813F48983302FD3ECF13EE0D1863`). Not captured on the phone before part 2.

### Implemented 2026-09-13: Staff, Daggers and weapon unlocks (weapon behaviors, part 2)

The second half of the owner's choice: two weapons bought with gold in the Relic Forge, each with its own behavior.

- **Staff (120 gold):** area damage. Every 0.9 s it deals 10 damage to its target and to every other living enemy within 3.5 units, which covers a whole pack. It clears mite packs fastest and is the slowest weapon against lone enemies.
- **Daggers (180 gold):** fast strikes with crits. They deal 6 damage every 0.55 s to one enemy, and every third strike is critical for double damage. They are slowest against packs and fastest against the Wardens.
- **Crit rhythm:** crits follow a fixed rhythm (every Nth attack) instead of a chance, so fights stay readable and the simulation stays exact. A critical hit, splash included, carries `DamageContext.IsCritical`, and the struck body flashes gold instead of white.
- **Carrying a weapon:** the Relic Forge now shows **Weapons: carry one** (Sword, Staff, Daggers) above **Relics: equip one**. Forging a weapon spends its price once and equips it; an owned weapon is equipped again for free. The Sword is the starting weapon: it is always owned and is never stored in the save. The next run creates the hero's weapon from the equipped definition. The HUD, the result build (**Build: Staff  |  ...**) and the hero's placeholder loadout (sword and shield, a staff with a violet orb, or two blades) follow it.
- **Nearest unlock:** the result hint names the cheapest unowned relic or weapon; a relic wins a tie, and the final hint reads **everything is forged**.
- **Save version 2:** `profile.json` gains `ownedWeaponIds` and `equippedWeaponId`. `ProfileMigration` upgrades a version 1 file in place: no weapons, the starting weapon carried. This is the first real migration.

| Files | Change |
|---|---|
| `Scripts/Combat/AttackPattern.cs` (new), `WeaponBehavior.cs`, `WeaponRuntime.cs`, `AttackController.cs`, `DamageContext.cs` | The behavior, splash and crit rhythm move into one validated `AttackPattern` value passed to `WeaponRuntime`; new `Area` behavior; `AttacksMade` counts the rhythm; `IsCritical` on hits. |
| `Scripts/Content/WeaponDefinition.cs`, `Data/Weapons/Weapon_Staff.asset`, `Weapon_Daggers.asset` (new), every weapon asset | Crit rhythm fields, a Forge description format and a Forge price. Enemy weapons keep zeros and are never sold. |
| `Scripts/Core/PlayerProfile.cs`, `Scripts/Save/ProfileSaveData.cs`, `ProfileMigration.cs`, `ProfileStore.cs` | Owned and equipped weapons alongside relics; schema version 2 with the version 1 step. |
| `Scripts/Progression/WeaponOption.cs`, `WeaponShop.cs` (new), `UnlockStatus.cs` (was `RelicStatus.cs`), `RelicShop.cs` | The weapon rack's rules: the starting weapon is always owned and is the fallback for a missing or unowned saved weapon; card states shared with relics. |
| `Scripts/Core/CombatSetup.cs`, `Scripts/UI/RelicForgeView.cs`, `HeroWeaponView.cs` (new), `CombatantView.cs`, `RunHud.cs`, `RunResultView.cs`, `PrototypeTextDefinition.cs`, `Data/UI/PrototypeText.asset` | The Forge catalog on Combat Setup, `HeroWeapon` for the run, weapon cards and section headers, placeholder loadouts, the gold crit flash, the weapon in the HUD and result build, and the nearest unlock across both shops. |
| `Scenes/Gameplay/Gameplay.unity` | A temporary builder, deleted afterwards, added the loadouts under **Hero Body** and three weapon cards and two headers to the Forge panel, and re-laid out all cards at 220 units so everything fits a 1920-unit canvas. |
| Tests | EditMode `WeaponBehaviorTests` (6): area splash, the crit rhythm and a critical splash, crit validation, weapon identity and sidegrade balance in the Descent simulation. `WeaponForgeTests` (4): profile slots, the weapon rack, catalog validation, the version 1 migration. `ProfileStoreTests` adds a version 1 file loading and saving as version 2. PlayMode `RelicForgeFlowTests` adds forging the Staff into the next run, Daggers crits flashing gold, and a layout check that the Forge panel is opaque and fits 1920 units without overlaps. |

Values were chosen with a search over damage, interval, area fraction and crit rhythm. The hard targets were the relic slice's: Mend descents survive, Temper descents fall on floor 2 without a relic, and both relics rescue damage-first Temper. On top of that, no weapon may be a straight upgrade: Mend-path health stays within 15 of the Sword's, the Staff must clear mite packs faster, and the Daggers must kill the Wardens faster. The Staff ends up the weakest against a single target (11.1 damage per second against the Sword's 12.5) and the slowest against the Wardens.

Results with the 1-second advance delay between waves (health after floor 1 → end of floor 2; D = damage first, S = speed first; boss and mite-pack times are the simulated fight seconds on the D + Mend path):

| Weapon | D + Mend | S + Mend | D + Temper | S + Temper | D + Temper + Counterweight | D + Temper + Second Wind | S + Temper + Counterweight | S + Temper + Second Wind | Wardens | Mite packs |
|---|---|---|---|---|---|---|---|---|---|---|
| Sword | 54 → 40 | 33 → 13 | 24 → dies F2 room 3 | 12 → dies F2 room 1 | 35 → 21 | 49 → 35 | 19 → dies F2 room 3 | 37 → 20 | 5.8 s | 4.4 s |
| Staff | 63 → 38.5 | 34 → 9.5 | 23 → dies at the F2 Warden | 3 → dies F2 room 1 | 43 → 18.5 | 48 → 23.5 | 11 → dies F2 room 2 | 28 → 3.5 | 6.5 s | 0 s |
| Daggers | 60 → 37 | 44 → 21 | 31 → dies F2 room 3 | 6 → dies F2 room 1 | 44 → 21 | 31 → 33 | 24 → dies F2 room 3 | 31 → dies F2 room 3 | 3.6 s | 5.8 s |

- **Staff:** after one Tempered Edge a single blast kills a mite pack, so mite waves cost it no time and floor 1 fights shrink to about 14 s. Its small hits keep Counterweight's counters small; only Second Wind carries speed-first Temper, with 3.5 HP left.
- **Daggers:** Counterweight answers with 60% of a 6-damage hit, and neither relic carries speed-first Temper.

Known gaps:
- Crit upgrades and a crit chance wait for the upgrade pool to grow.
- Weapon placeholders are flat shapes; a visible area blast or crit number would read better.
- Mid-run weapon swaps do not exist by design.

The first Unity run failed two of the new PlayMode tests: this Editor formats decimals in Turkish, so the HUD read **0,80s**, not **0.80s**. The HUD follows the device language on purpose, so the tests now format expected numbers in the current culture.

Verified (commit cc1a217, first Staff tuning): Unity EditMode 140/140, PlayMode 37/37, `Verify-Project.ps1`, .NET CombatChecks 133/133, development APK built only after both reports passed (SHA-256 `8558B44BFB50FA307AA417882F646A6E4DF0B94D87F4EEE6AC8B51D14CAF48C5`).

**Device run and fixes (see `23`):**
- **Staff health gap:** the first Staff tuning (18 damage every 1.2 s, 75% to the rest of the pack) ended floor 1 at 38 HP on the phone, against 52 in the simulation; the Sword matched (56 against 54). The simulation had readied the hero's weapon at every wave. In the scene the weapon only cools down during the 1-second advance delay, so a weapon slower than that delay starts each wave late.
- **Simulation fix:** `DescentSimulation` now ticks the advance delay between waves. The Sword and Daggers results are unchanged. The first Staff tuning then showed 36 HP after floor 1 and died on floor 1 on the damage-first Temper path.
- **Staff retune:** a repeated search found 8 of 540 candidates meeting every target. The Staff became 10 damage every 0.9 s, with full damage to everything in reach (the values above).
- **Forge backdrop:** the result screen's build line showed through the 92% Relic Forge backdrop, between the weapon and relic cards. The backdrop is now opaque, and `ForgePanelIsOpaqueAndFitsTheShortestPortraitScreenWithoutOverlaps` checks it.

Verified after the fixes: Unity EditMode 140/140, PlayMode 37/37, `Verify-Project.ps1`, .NET CombatChecks 133/133, development APK built only after both reports passed (SHA-256 `5949CEDE5117510EFA1BBA815FD18AF497BFAD6C5AC73AB7A8AADA7C1FF147FB`).

### Implemented 2026-09-14: scene and simulation parity tests

The Staff gap showed that the balance simulation can drift from the scene without any test failing. `SimulationParityTests` (PlayMode) now play whole Descents in the real scene the way `DescentSimulation` plays them, and compare the results.
- **Choices:** the same upgrade card every time, Mend or Temper at the first forge, Mend afterwards, Descend at the checkpoint.
- **Time:** `Time.captureDeltaTime` fixes game time at 1/60 s per frame, like the simulation, and the frame-rate cap is lifted, so a two-floor Descent takes about a second of real time.
- **Cases:** each weapon on the damage-first Mend path; the Sword's speed-first Temper death; Counterweight and Second Wind carrying damage-first Temper; the Staff dying on floor 2 with Counterweight.
- **Result:** all seven match exactly: outcome, floor, death room, kills, gold banked and lost, upgrades applied, relic triggers, and health after floor 1 and at the end. The tolerance is 1 HP.

To make this possible, `DescentSimulation` and the relic mirrors moved into `Tests/Support` (`Project.TestSupport`, compiled only with the test framework). Both test assemblies reference it; the Android build does not include it.

Verified: Unity EditMode 140/140, PlayMode 44/44, `Verify-Project.ps1`, .NET CombatChecks 133/133, development APK built only after both reports passed (SHA-256 `C06E064B7F24BE0F8601B01860D491AC35739FD8A25D9B701B365F7AC0266495`; no game code changed since the build installed on the phone).

### Implemented 2026-09-14: Forge catalog checks at startup

A code review of the packs and weapon commits found one robustness gap. A bad Forge entry threw an exception halfway through `CombatSetup.Awake`, after the profile loaded but before the run existed. Examples: a weapon listed twice, a description format with an unknown placeholder, or crit data the weapon runtime rejects. Every view then logged its own missing-reference error, and nothing named the bad asset.

`Content/ForgeCatalog` now builds the relic and weapon options before the profile loads. It checks every entry: empty slots, duplicate ids, relic amounts, description formats, and each weapon's combat data (by creating a runtime, so a broken Daggers asset fails even while the Sword is carried). Combat Setup logs one error naming the asset and disables itself, as it already did for missing references. EditMode `ForgeCatalogTests` (3) cover each case.

Verified: Unity EditMode 143/143, PlayMode 44/44, `Verify-Project.ps1`, .NET CombatChecks 133/133, development APK built only after both reports passed (SHA-256 `8AF1C71DF37E3703A9B21A3DA55BD281DFDB6EE3F5F132DDDB4B6C735B1EAA6F`).

### Implemented 2026-09-14: truncated save files

`13` and `18` rate save corruption as critical. Two EditMode tests now cut a profile file at every character and reload it:
- **Temp file:** a save killed mid-write can leave a partial temp file with a higher revision. At every cut length the last complete save still loads (70 gold, the equipped Staff), and only the complete newer file wins.
- **Main file:** storage damage can cut `profile.json` itself. At every cut length the backup loads instead.

Unity's JSON parser rejected every partial file, so no change to `ProfileStore` was needed; the tests keep it that way. Verified: Unity EditMode 145/145 (no game code changed).

### Implemented 2026-09-14: the arena floor and walking packs (isometric arena, part 1)

The first slice of the astral foundry direction in `19`. The owner chose enemies that walk in toward the hero over enemies that appear in place. This part moves combat onto a two-dimensional floor and rebalances the Descent; the floating platform, backdrop and camera follow in part 2.

- **Arena floor:** every position is a point on a flat floor with the hero at the origin. The screen shows depth at half length (`ArenaFloor.DepthScale` 0.5), the way an isometric view foreshortens it. Reach, cleave and area radii are floor distances, so an enemy straight ahead is as far away as one to the side. The Vanguard now stands at the world origin, and the camera moved up to keep it framed.
- **Formations:** a wave holds up to seven enemies. `PackLayout` fills the front row first and puts later slots behind and to the sides, never more than 2 floor units off the centre line.
- **Walking in:** a wave enters 6 floor units from the hero (`_entryDepth`). `PackMotion` walks each enemy toward its own point on an arc just inside its weapon's reach (0.1 inside, so rounding never leaves it on the edge). Enemies from the centre slots come straight on; the widest slots approach from 60° off the centre line. Enemies step nearest first and wait whenever a step would bring them closer than 0.9 units (`_bodySpacing`) to a nearer enemy, so the pack queues instead of stacking.
- **Combat order:** the hero strikes the nearest living enemy within its reach, the earlier slot on equal distance; an enemy strikes once the hero is within its own reach. Nothing walks on the frame a wave spawns, while time is frozen, or after the hero falls. `EncounterController` runs before other scripts, so enemies move before anyone attacks, the same order the Descent simulation uses.
- **Victory names the boss:** Warden's Vault now holds two mites with the Warden, and the result named whichever enemy fell last. It now names the final wave's toughest enemy.

| Enemy | Speed (units/s) | Reach | Damage |
|---|---|---|---|
| Grunt | 1.6 | 1.2 | 3.6 every 1 s |
| Runner | 3 | 1.2 | 1.2 every 0.4 s |
| Tank | 0.9 | 1.4 | 7.2 every 2.5 s |
| Grunt Captain | 1.4 | 1.3 | 5.4 every 1.2 s |
| Forge Warden | 1 | 1.4 | 6 every 1.8 s |
| Cinder Mite | 2.2 | 1 | 0.6 every 1.5 s |

| Floor | Room | Waves |
|---|---|---|
| Ember Halls | Ember Hall | Grunt + Mite · 3 Mites · Grunt + 3 Mites |
| | Cinder Walk | Runner + 2 Mites · 4 Mites · Tank + 2 Mites |
| | Slag Gate | Grunt + Runner + 2 Mites · Tank + 2 Mites |
| | Captain's Post | Grunt Captain + Grunt + 2 Mites |
| | Warden's Crucible | Forge Warden |
| Quicksilver Vaults | Mercury Stair | Grunt + 3 Mites · Runner + 3 Mites |
| | Cold Crucible | Tank + 3 Mites · 5 Mites |
| | Vault Gate | 2 Grunts + 3 Mites |
| | Sentry Hall | Grunt Captain + Grunt + 2 Mites |
| | Warden's Vault | Forge Warden + 2 Mites |

**Rebalance.** With walking packs the old values failed four balance tests. The Staff's 3.2 reach blasted packs well before they arrived, so it outclassed the other weapons. A flat +5 Tempered Edge added 83% to the Daggers' 6 damage but 50% to the other weapons' 10, and the search found no Daggers tuning around that. The new values:
- **Enemies:** every enemy weapon deals 60% of its old damage; floor 2 hits 1.4× instead of 1.25× before Cursed Gold.
- **Cinder Mites:** 1 XP and no gold instead of 3 XP and 1 gold, and each level costs two more than the last (10, 12, 14, ...; `_experienceGrowth` 2), so the larger packs do not use up the ten upgrade stacks early.
- **Tempered Edge:** +50% damage instead of +5. That is the same for 10-damage weapons and proportional for the Daggers.
- **Weapons:** the Sword reaches 1.8; the Staff deals 11 every 1.1 s from 2.1 units away and 75% of that to the rest of the pack within 3.5 units; the Daggers strike every 0.45 s at 1.5 units.
- **Warden:** reach 1.4 instead of 1.5. It stopped exactly at 1.4, one of the Daggers reaches the search tried, where float rounding would decide whether they connect.

The search kept the hard targets of the relic and weapon slices (Mend descents survive; Temper descents fall on floor 2 without a relic; both relics rescue damage-first Temper; every weapon within 15 HP of the Sword on the Mend paths; the Staff fastest on mite packs and the Daggers fastest on the Wardens). Small changes flipped outcomes, so it preferred candidates whose one-step neighbours also met every target.

Ember Halls pays 116 gold and a full Descent 273, because only named enemies pay. A Descent now has 60 kills and about 83 s of simulated fighting with the Sword (54 s on floor 1, up from about 16 s), with 7 upgrades by the end of floor 1 and all 10 on floor 2.

| Weapon | D + Mend | S + Mend | D + Temper | S + Temper | D + Temper + Counterweight | D + Temper + Second Wind | S + Temper + Counterweight | S + Temper + Second Wind | Wardens | Mite packs |
|---|---|---|---|---|---|---|---|---|---|---|
| Sword | 46 → 37 | 45 → 23 | 12 → dies F2 room 2 | 11 → dies F2 room 2 | 33 → 30 | 37 → 34 | 27 → 18 | 36 → 26 | 15.2 s | 9.1 s |
| Staff | 46 → 42 | 41 → 34 | 12 → dies F2 room 2 | 7 → dies F2 room 2 | 40 → 37 | 37 → 33 | 23 → 16 | 32 → 25 | 16.2 s | 6.5 s |
| Daggers | 55 → 38 | 51 → 33 | 21 → dies F2 room 3 | 18 → dies F2 room 2 | 35 → 20 | 46 → 29 | 29 → dies F2 room 3 | 43 → 24 | 13.9 s | 10.4 s |

(Health after floor 1 → end of floor 2; D = damage first, S = speed first; Warden and mite-pack times are simulated fight seconds on the D + Mend path.)

- **Counterweight now carries speed-first Temper** for the Sword and the Staff. The relic decision in `19` wanted that path to fail; it still fails for the Daggers. Counterweight still favours damage: on the Sword's Mend paths it adds 16 HP to damage first and 5 to speed first. `DescentTests` now checks that difference, and that Second Wind carries speed-first Temper.
- **Parity:** the Staff's speed-first Temper death with Counterweight became a victory, so that parity case now plays the Daggers' death on the same path. All seven cases match the simulation exactly.

| Files | Change |
|---|---|
| `Scripts/Combat/ArenaFloor.cs`, `PackMotion.cs` (new), `PackLayout.cs`, `Targeting.cs` | Floor distances; walking in; two-dimensional slots for up to seven enemies; targeting and splash by floor distance. |
| `Scripts/Combat/EncounterController.cs`, `Scripts/Content/EnemyDefinition.cs`, `Scripts/UI/RunResultView.cs` | Entry depth, formation and body spacing; moving the pack each frame; move speed per enemy; the toughest enemy of the wave names a victory. |
| Both floors, every enemy and weapon asset, `Enemy_CinderMite.asset`, `Upgrade_Damage.asset`, `PrototypeEconomy.asset`, `Gameplay.unity` | Packs, speeds, reaches and the rebalance; the hero at the origin, the camera and the encounter's arena settings. |
| Tests | EditMode `PackMotionTests` (5): walking at speed and stopping inside reach, approach angles, waiting behind a nearer enemy, validation, floor distances from world positions matching floor coordinates exactly. `PackTests` checks the slots and plays the Sword's cleave on mites that walked in. `DescentSimulation` walks packs through `PackMotion` with the new data. PlayMode tests follow walking packs: formation positions at spawn and mirrored paths, the first pack falling to six swings for 11 XP, enemies striking only in reach, pause and a dead hero freezing movement, Counterweight answering whichever enemy strikes, Staff splash on the Grunt beside the mite, Daggers crits across the mite and the Grunt, and the victory naming the Warden although its mites fell last. |

Before the Unity runs, a frame-by-frame replay of single waves with the simulation's classes predicted the scene counts the new PlayMode tests assert (six swings and eight hits for the first pack; three swings for three mites after Tempered Edge, four after Quickened Grip). All held on the first run.

Verified: Unity EditMode 150/150, PlayMode 44/44, `Verify-Project.ps1`, .NET CombatChecks 138/138. The development APK is built after part 2.

### Implemented 2026-09-14: the floating platform and camera framing (isometric arena, part 2)

The placeholder look of the astral foundry arena. No image or art asset is added; final art replaces one component.

- **Arena view:** `ArenaView` builds one vertex-coloured mesh at startup and draws it with the built-in Sprites-Default material.
  - **Void:** a violet gradient, four nebula glows, 220 stars with a few sparkles, and six small floating rocks.
  - **Platform top:** a rhombus on the arena floor, from 3 floor units behind the hero to 14 in front and 3.3 to either side. It has a brass rim, 8 × 8 stone tiles that darken toward the far corner under the top HUD, a brass inlay at the centre, and ember lanterns at the near and side corners.
  - **Below the top:** the near faces with seams and ember slits, and a keel with ember cracks and a glowing seam.
- **Depth:** the camera sorts sprites along the screen's vertical axis (`TransparencySortMode.CustomAxis`). An enemy higher on the screen stands farther away on the floor, so it is drawn behind nearer enemies and the hero. The arena mesh draws at sorting order −20, below every sprite.
- **Framing:** `ArenaCameraFraming` fits the arena into the rows between the HUD's enemy bar and hero label, and frames again when the screen size or safe area changes. The fitted area runs from 0.7 below the hero to 4.5 above it (beyond the deepest pack slot) and 2.6 to either side.
  - **1080 × 2340 phone:** the widest pack slots set the zoom (orthographic size 5.63, down from 6).
  - **1080 × 1920:** the free band sets it (5.96). Part 1's fixed camera left the hero's feet 28 rows inside the bottom HUD block at this size.

Why a rhombus, not a 2:1 isometric diamond: a square platform turned 45° with the same near corner that holds every pack slot would be 13.6 units wide, 2.6 times the phone's visible width. Stretching the rhombus along the floor's depth keeps the mockup's tall diamond silhouette and holds every slot.

| Files | Change |
|---|---|
| `Scripts/UI/ArenaView.cs`, `ArenaCameraFraming.cs`, `ArenaFraming.cs` (new) | The placeholder arena mesh and depth sorting; framing between the HUD blocks; the framing arithmetic as a pure function. |
| `Scenes/Gameplay/Gameplay.unity` | A temporary builder, deleted afterwards, added the **Arena** object and the framing component on **Main Camera**, and set the camera's clear colour to the void's. |
| Tests | EditMode `ArenaFramingTests` (4): the pack width setting the zoom on a tall phone with the arena centred in the free band, a shorter screen zooming out to fill the band, the whole-screen fallback when the HUD leaves too little, validation. PlayMode `ArenaViewTests` (3): depth sorting with the arena behind every sprite, every slot of every pack size entering on the platform, and on a 1080 × 2340 phone the hero's feet and every slot fitting between the HUD blocks and inside the screen width. |

Before the tests, a temporary Editor script (not committed) rendered the scene with two walking packs at 1080 × 2340 and 1080 × 1920. After the first render the floating rocks were shrunk, because they competed with the platform, and ember cracks were added to the keel.

Placeholder limits: combatants are still flat shapes; both floors share one platform, although `19` gives Quicksilver Vaults cool silver tones; nothing in the void moves.

Verified: Unity EditMode 154/154, PlayMode 47/47, `Verify-Project.ps1`, .NET CombatChecks 138/138, development APK built only after both reports passed (SHA-256 `A95A3A66D91892792DCA8DC801AC8C76E2072620E69536C4B6B9C6FE4CCF481A`).

### Implemented 2026-09-14: pixel-art placeholders for the astral foundry (isometric arena, part 3)

The owner asked for the arena to reach the look of `ArtDirection/2026-09-14/mockups/cosmic-v1.png`, then the characters and the combat effects. Everything is still placeholder art, now **pixel art generated from code**: no image file enters `Assets`, and final art (doc `08`) replaces single components later.

- **Pixel canvas:** `Cryptforge.Art` is engine-independent and compiled by the .NET checks: `Rgba`, `PixelCanvas` (rects, ellipses, triangles, lines, dither, string pixel maps, outline, silhouette), `PixelNoise` (deterministic hash noise, so the art is identical on every run), `PixelPalette` (sampled from the mockup), `ArenaGeometry`. `PixelSpriteFactory` turns a canvas into a point-filtered sprite at **32 texels per world unit**; every sprite is drawn at its final size, never scaled.
- **Void backdrop** (`VoidArt`, `VoidLayout`, `VoidBackdropView`): a violet gradient sky mesh with nebula glows, two star layers, nine twinkling sparkles, a spiral galaxy, three haze clouds, four floating rock islands with lantern flames, orbital rings and hanging chains, and eight pieces of drifting rubble. Islands drift ±0.05 units over 8 s and flames flicker; pausing freezes it. Everything stays outside the platform and lower in contrast than the combat.
- **Forge platform** (`PlatformLayout`, `PlatformArt`, `ForgePlatformView`): one 217×403 texel sprite for the whole platform: 8×8 two-tone stone tiles with grout, a brass rim, brass lines corner to corner with a nested-diamond emblem where they cross, corner towers with lanterns, stacked stone faces with lava seams, a keel narrowing to a lava crystal, and chains. A second overlay sprite flickers the flames, seams and crystal on a 2-frame swap and breathes its colour.
- **Hero** (`HeroArt`, `HeroLookView`): the Vanguard as a 32×44 blue-steel knight seen from behind with a cape, three body frames (two idle breaths, an attack), and the weapons of the carried loadout drawn as their own sprites: sword and shield, staff with a violet orb, two daggers. The existing loadout objects keep their names, so `HeroWeaponView` is unchanged. On each attack the sword sweeps ±40° about its grip, the daggers thrust and the orb lights.
- **Enemies** (`EnemyArt`, `EnemyLookView`): six looks with a 1-texel outline, feet on the pivot and a translucent ground shadow — Grunt (slag brute, 31×30), Runner (21×32), Tank (45×36), Grunt Captain (brass pauldron, 35×40), Forge Warden (quicksilver golem with one violet visor line, 49×56), Cinder Mite (15×16 ember with a flame crown). Two idle frames (lava cracks pulse, walking bobs) and an attack frame. Each enemy carries a 26×5 health bar above its head from the moment it spawns, hidden on death.
- **Hit flash:** a white tint means nothing on a textured sprite, so `CombatantView` now flashes a **silhouette overlay** (`ILookSprites.SilhouetteOf`) tinted white, or crit gold, for the feedback duration; scenes without a look view keep the old tint. `IsFlashing` and `FlashColor` expose it to tests.
- **Effects** (`EffectArt`, `CombatEffectsView`): sword crescents, staff blast rings, dagger double slashes with a gold star on crits, ember sparks on every hit, smoke and coins on a kill that pays gold, floating 3×5-pixel damage numbers (crit numbers gold at 2×), and a 0.1 s camera shake when the hero is hit. Sprites and a 24-effect, 12-number pool are built once; nothing allocates per frame. `AttackController` gained a `Struck(Health)` event beside `Attacked`.
- **Reach:** with real sprites, an enemy stopping 0.9 floor units in front of the hero stood hidden behind the hero's helmet. Every reach grew (Cinder Mite 1.0 → 1.5; Grunt and Runner 1.2 → 1.5; Grunt Captain 1.3 → 1.6; Tank and Forge Warden 1.4 → 1.7) and the hero weapons with them (Sword 1.8 → 2.1, Staff 2.1 → 2.4, Daggers 1.5 → 1.8), so melee happens 0.7–0.8 world units above the hero's feet and the enemy's face stays visible. A +0.4 step for every enemy failed one balance target (the Daggers with Counterweight died on floor 2); this step keeps every target.

Balance after the reach change (health after floor 1 → end of floor 2; Wardens and mite packs are fight seconds on the D + Mend path):

| Weapon | D + Mend | S + Mend | D + Temper | S + Temper | D + Temper + Counterweight | D + Temper + Second Wind | S + Temper + Counterweight | S + Temper + Second Wind | Wardens | Mite packs |
|---|---|---|---|---|---|---|---|---|---|---|
| Sword | 46 → 37 | 43 → 21 | 12 → dies F2 room 2 | 9 → dies F2 room 1 | 29 → 26 | 37 → 34 | 13 | 23 | 14.6 s | 9.5 s |
| Staff | 46 → 36 | 40 → 32 | 12 → dies F2 room 2 | 6 → dies F2 room 2 | 40 → 37 | 37 → 34 | 21 | 30 | 15.6 s | 6.1 s |
| Daggers | 54 → 34 | 52 → 30 | 20 → dies F2 room 2 | 18 → dies F2 room 2 | 33 → 15 | 45 → 26 | dies F2 room 3 | 21 | 13.3 s | 10.0 s |

How it was made: the drawing was split across five parallel agents (backdrop, platform, hero, enemies with the flash and bars, effects), each owning disjoint files, verified with a dotnet compile of the gameplay scripts against the Editor's UnityEngine assemblies plus the .NET tests; Unity itself ran once for the wiring (a temporary Editor builder, deleted afterwards, added `HeroLookView` to the Vanguard, `CombatEffectsView` to the Arena and `EnemyLookView` to every enemy prefab, and set the bodies to white at scale 1) and once for the tests. A temporary PlayMode test rendered the running scene to PNG frames for review; after the first render the platform faces were darkened (the warm brown competed with the ember enemies) and the enemy bars were shown from spawn.

| Files | Change |
|---|---|
| `Scripts/Art/*.cs` (new) | Rgba, PixelCanvas, PixelNoise, PixelPalette, ArenaGeometry, VoidArt, PlatformArt, HeroArt, EnemyArt, EffectArt; `Art/Unity/PixelSpriteFactory.cs`. |
| `Scripts/UI/Arena/` (new) | MeshBuilder, VoidBackdropView, ForgePlatformView; `ArenaView` coordinates them. |
| `Scripts/UI/HeroLookView.cs`, `EnemyLookView.cs`, `ILookSprites.cs`, `CombatEffectsView.cs` (new), `CombatantView.cs`, `Scripts/Combat/AttackController.cs` | Looks, flash overlay, health bars, effects, the `Struck` event. |
| `Scenes/Gameplay/Gameplay.unity`, `Prefabs/Enemies/*.prefab`, every weapon asset | The wiring above; white bodies at scale 1; the new reaches. |
| Tests | EditMode `PixelCanvasTests` (6), `ArenaGeometryTests` (2), `VoidArtTests` (6), `PlatformArtTests` (7), `HeroArtTests` (6), `EnemyArtTests` (7), `EffectArtTests` (6). PlayMode: `ArenaViewTests` checks the drawn platform and the sorting of backdrop, platform and combatants; the Daggers crit test reads the flash from `CombatantView`; `UpgradeFlowTests` expects the Sword's new range. |

Known limits: one facing for every figure (the hero always faces away, enemies always toward the hero); the sword swing rotates the sprite off the texel grid for 0.12 s; the staff ring shows half the splash radius so it does not ring the whole platform; both floors share one platform and backdrop, although `19` gives Quicksilver Vaults cool silver tones; the HUD is still the old text HUD (the icon HUD is the next slice after the ability).

Verified: Unity EditMode 194/194, PlayMode 47/47, `Verify-Project.ps1`, .NET CombatChecks 178/178, development APK built only after both reports passed (SHA-256 `5FF7C1FF5FA33E6A96EA120D4EC6EDBF5EA838BF0C4F561B0C6E463D09238908`; installed and checked on the phone the next morning, see `23`).

### Implemented 2026-09-14: the Forge Burst (the hero's active ability)

The first player action inside a fight. The owner chose a burst around the hero over the mockup's touch-aimed ring (`19`), because the coming slice puts the finger on movement.

- **Ability:** `AbilityDefinition` (`Data/Abilities/Ability_ForgeBurst.asset`: 20 damage, 2.5 floor units, 8 s cooldown) on `HeroDefinition`; `AbilityRuntime` (pure) holds the cooldown and strikes every living target once; `AbilityController` on the Vanguard collects the enemies within the radius through `Targeting.CollectNear` around the hero, fires them, and ticks the cooldown on scaled time, so a pause or an open choice holds it. `CombatSetup` creates the runtime with the weapon and exposes it as `Ability`.
- **Button:** `Ability Button` on the HUD canvas at the bottom right (220 units, the built-in round knob sprite, above the hero's health line): a blue burst glyph drawn by `AbilityArt`, a radial fill that drains through the cooldown, and `AbilityButtonView`, which answers only while the run is live, no choice is open and the run is not paused. A tap fires; a burst with nobody in reach is still spent, as the player chose the moment.
- **Ring:** `AbilityRingView` lays a dashed isometric ring (`AbilityArt.DrawRangeRing`, twice as wide as tall, 2.5 units) on the floor around the hero's feet, sorted between the platform and the combatants: bright with a slow breath while ready, faint while cooling. `CombatEffectsView` flashes the blast ring at the burst's full radius on use; the struck enemies get their sparks and numbers from their own damage events.
- **Simulation:** `DescentSimulation.Run` takes an optional `HeroAbility`; the simulation fires it whenever it is ready and an enemy stands within the radius, after the hero's swing of that frame. The scene never fires it by itself, so the parity tests stay exact. Fired that way the burst is the player's edge, not the balance (health after floor 1 → end; D + Mend, S + Mend, S + Temper):

| Weapon | Without the burst | With the burst (uses) |
|---|---|---|
| Sword | 46 → 37, 43 → 21, dies F2 room 1 | 60 → 52 (9), 48 → 27 (10), dies F2 room 2 (7) |
| Staff | 46 → 36, 40 → 32, dies F2 room 2 | 57 → 47 (9), 45 → 44 (9), dies F2 room 2 (7) |
| Daggers | 54 → 34, 52 → 30, dies F2 room 2 | 62 → 48 (10), 58 → 42 (10), dies F2 room 3 (8) |

- **Smoke:** the death puff is drawn at 55% so it no longer hides the hero standing beside a fallen enemy (seen on the phone).

| Files | Change |
|---|---|
| `Scripts/Combat/AbilityRuntime.cs`, `AbilityController.cs`, `Scripts/Content/AbilityDefinition.cs`, `Data/Abilities/Ability_ForgeBurst.asset` (new); `HeroDefinition.cs`, `Hero_Vanguard.asset`, `Core/CombatSetup.cs` | The ability and its wiring into the run. |
| `Scripts/Art/AbilityArt.cs`, `Scripts/UI/AbilityButtonView.cs`, `AbilityRingView.cs` (new); `CombatEffectsView.cs`; `Gameplay.unity` | Glyph, ring, button and effects; a temporary builder, deleted afterwards, added the controller and ring to the Vanguard and the button to the HUD. |
| Tests | EditMode `AbilityTests` (3: strikes and cooldown, validation, the simulated edge without carrying greed), `AbilityArtTests` (2). PlayMode `AbilityFlowTests` (3): the button bursts both enemies of the first pack once they stand in the ring and then cools down; the button is dead behind a choice; the floor ring lies under the hero and fades while cooling. |

Known limits: one ability, no upgrades touch it, and the simulation's automatic use is a stand-in for the player's timing.

Verified: Unity EditMode 199/199, PlayMode 50/50, `Verify-Project.ps1`, .NET CombatChecks 183/183, development APK built only after both reports passed (SHA-256 `ECF52FA70B7EF17684C9CF7D76C81DA2D2AA0AD52C9CF9CFAFBF4BEA6B8BEC70`).

### Implemented 2026-09-14: the walkable arena (packs from every corner, a walking hero, chests, a following camera)

The owner's request after seeing the reference game again (`19`): a wider platform in the middle, the hero walkable, packs from left and right and from all four sides, chests to survive by, the art larger on screen.

- **Platform:** `ArenaGeometry(-9, 9, 9)`: a diamond 18 world units wide and 9 tall on screen, the hero at its centre; `PlatformArt` draws it as before with 16 tiles per edge (581×419 texels). The backdrop's islands, rubble, sparkles, haze and galaxy moved beyond the corners and the keel (`VoidLayout`), the star field grew to 24×27 units.
- **Corners:** `EntrySide` and `EntrySides`: a wave's slots go round the far, right, near and left corners in turn, starting one corner further for each wave of a floor, so a pack of four comes from every side at once. `PackLayout` still forms the enemies that share a corner. Packs enter 5 floor units from the centre (`_entryDepth`).
- **Motion:** `PackMotion` takes the hero's position each step (`Step(dt, heroX, heroY)`); each enemy heads for its own point on the arc round the hero, turned by its corner and its lateral slot, stops just inside its reach, and walks again when the hero leaves. `EncounterController` feeds it the hero's floor position (`HeroFloorX/Y`) and picks the wave's nearest enemy relative to it.
- **Hero:** `HeroMotion` (pure): 2.5 floor units per second, a steer with a dead band, the rim as a wall with sliding along the edge (0.6 units inside it). `HeroMovementInput` on the Vanguard: a touch or mouse press anywhere that is not a button starts a drag; past 24 px the hero walks, at 110 px at full speed; screen up is floor depth, so a diagonal drag walks the same diagonal on screen; `Hold`/`Release` steer it without a touch (tests, and later an auto-walk option). It runs before the encounter so the packs chase this frame's position.
- **Chests:** `ChestRule` (pure) puts one chest per combat room on a spot circling the centre (right, far, left, near) 1.8 units out, inside the edge of what the camera shows; even rooms mend 25% of maximum health, odd rooms pay 10 gold at the floor's rate; the hero opens it by standing within 0.75 units. `ChestSpawner` draws it (`ChestArt`, closed and open with a coin glow) and applies the reward; `CombatEffectsView` shows the heal amount or pops coins with the sum.
- **Camera:** `ArenaCameraFollow` replaces the fixed framing: 4.6 world units across the screen (`FollowFraming`, pure), the hero on the middle row between the HUD blocks, gliding after the hero at 8 per second and re-framed on screen or safe-area changes.
- **Balance:** the simulation's hero stands still at the centre and fires the burst whenever it is ready with an enemy in reach, which is now the baseline for every balance test (`Burst(...)` helpers); chests are not simulated. With every corner striking at once the old numbers fell on floor 1, so floor 1 scales enemy damage by 0.92 (`_enemyDamageMultiplier`), floor 2 by 1.288 (its ×1.4 kept), and the Daggers strike every 0.5 s instead of 0.45 (they alone carried a greedy Temper descent at 0.92). Every earlier target holds:

| Weapon | D + Mend | S + Mend | D + Temper | S + Temper | D + Temper + Counterweight | D + Temper + Second Wind | Wardens | Mite packs |
|---|---|---|---|---|---|---|---|---|
| Sword | 60 → 30 | 55 → 29 | dies F2 | dies F2 | survives | survives | 12.1 s | 7.2 s |
| Staff | 42 → 29 | 48 → 39 | dies F2 | dies F2 | survives | survives | 13.6 s | 5.8 s |
| Daggers | 69 → 34 | 59 → 22 | dies F2 | dies F2 | survives | survives | 11.6 s | 8.3 s |

- **Parity:** unchanged in spirit; the scene and the simulation share `EntrySides` and `PackMotion`, the scene's hero never moves in the parity tests, and all seven cases match exactly.

| Files | Change |
|---|---|
| `Scripts/Combat/EntrySide.cs`, `HeroMotion.cs`, `ChestRule.cs`, `ChestSpawner.cs` (new), `PackMotion.cs`, `EncounterController.cs`, `ArenaFloor.cs` | Corners, the walking hero's rules, chests, the moving target. |
| `Scripts/UI/HeroMovementInput.cs`, `ArenaCameraFollow.cs`, `Scripts/Art/FollowFraming.cs`, `ChestArt.cs` (new); `ArenaCameraFraming.cs`, `ArenaFraming.cs` (removed); `VoidArt.cs`, `PlatformArt.cs`, `CombatEffectsView.cs` | Drag input, the following camera, chest art and effects, the wider void and 16 tiles. |
| `Gameplay.unity`, `Floor_EmberHalls.asset`, `Floor_QuicksilverVaults.asset`, `Weapon_Daggers.asset` | The 9-unit diamond, entry depth 5, the new components (a temporary builder, deleted afterwards), the damage rates and the Daggers' interval. |
| Tests | EditMode `PackMotionTests` (corners, a walking hero), `HeroMotionTests` (2), `ChestRuleTests` (2), `ChestArtTests` (1), `FollowFramingTests` (3); the balance tests run with the burst. PlayMode `ArenaWalkTests` (3: walking, chasing and the rim; the mending chest; the paying chest), `ArenaViewTests` (corners on the platform, the following camera), `FloorFlowTests` (corner entries), and the first-pack counts (7 swings, 7 hits; 3 swings and 4 hits after Tempered Edge). |

Known limits: every figure keeps one facing, so enemies from the near corner walk with their backs to the camera; blocked enemies wait rather than step round each other; the hero has no walk animation; the chests and the hero's position are not part of the balance.

Verified: Unity EditMode 205/205, PlayMode 53/53, `Verify-Project.ps1`, .NET CombatChecks 193/193, development APK built only after both reports passed (SHA-256 `16FCB13983917E04C9D854609893CCFC7B2878BDD7010120F659DF98F96DE69C`).

### Implemented 2026-09-14: packs stay on the platform when the hero stands at the rim

A read-through of the walkable arena found two rules that only held while the hero stood near the centre. Packs entered round the arena's centre, so a hero at the right corner had the right corner's pack appear on its left and the far corner's pack behind it. And an enemy's stopping point beside the hero was never checked against the platform: the hero keeps 0.6 units inside the rim and enemies stop up to 1.6 away, so a Grunt that followed the hero to a corner stopped over the void.

- **Entry:** `EntrySides.Place` lays each wave out round the hero's position when it enters, 5 floor units out as before. An enemy whose spot lies beyond the rim, or inside the 0.6-unit margin the hero keeps, starts short of the rim on the line from the hero, as long as that leaves it at least 3 floor units away (`MinimumEntryDistance`, beyond every enemy's and weapon's reach). A corner with an enemy that has no such room closes for that wave and the open corners take its enemies in turn (`Formation` over an open-corner mask). So halfway to the right corner all four corners still send their enemies, the right, far and near ones starting nearer; from about 5 units out (5.4 for a lone enemy per corner, 4.9 for pairs), and at the corner itself, the whole wave comes from the left; on an edge, from the two corners that have floor. If every corner closes, which only a platform too small for the wave can cause, each enemy is drawn in toward the hero until it stands inside.
- **Found on the phone:** the first version closed a corner as soon as any of its spots lay beyond the rim, without drawing it in. On the phone the level-up froze the hero halfway to the right corner, and every later wave of the room came from the left; the rule above keeps the four sides the owner asked for.
- **Stopping:** `PackMotion` takes the platform when a scene or the simulation creates it. An enemy whose own point beside the hero lies outside the margin turns round the hero in 5-degree steps, both ways, to the first point on the platform, and takes the way nearer to where it stands, so it never crosses in front of the hero. The hero always stands inside the same margin, so the half of the circle facing the platform's centre always qualifies.
- **Nothing changes near the centre:** from the centre every corner fits every pack of up to seven and no point at reach is near the rim, so each enemy enters and stops exactly where it did before; a test compares every slot of every pack size bit for bit, and the balance and parity numbers are unchanged. `DescentSimulation.Platform` mirrors the scene's arena, checked by `ArenaViewTests`.
- **Checked and left alone:** a press that starts on the burst or pause button does not walk the hero. The event system runs at execution order -1000, before `HeroMovementInput` at -60, so a press is already known to be on a button in its first frame, and the HUD labels do not catch raycasts. An earlier note in `23` had claimed otherwise; a drag from the burst button on the phone confirmed the input was right, and the note is corrected.

| Files | Change |
|---|---|
| `Scripts/Combat/EntrySide.cs` | `EntryPlacement`, `EntrySides.AllSides` and `MinimumEntryDistance`, `Formation` over open corners, `Place` round the hero with spots drawn in short of the rim and closing corners. |
| `Scripts/Combat/PackMotion.cs` | A platform-bounded constructor with the hero's position, `Add` from a placement, turning onto the platform. |
| `Scripts/Combat/EncounterController.cs`, `Gameplay.unity` | The encounter holds the `ArenaView` and places each wave round the hero; the scene reference was added to the encounter's serialized fields. |
| `Tests/Support/DescentSimulation.cs` | The simulation places and walks its packs with the same calls on the same platform. |
| Tests | EditMode `PackMotionTests` (+3): packs from the centre form up as before bit for bit, with open-corner formations and validation; halfway to the right corner every corner still sends enemies, the right corner's drawn in short of the rim; most of the way there, at a corner and on an edge the corners without room close; every enemy on the platform, at least 3 units away and apart from the others, and a too-small platform drawing them in; a Grunt that followed the hero to the right corner turns onto the platform where it would have stood over the void. PlayMode `ArenaWalkTests` (+1): with the hero at the right corner the next wave enters from the platform's side, out of reach, and every enemy walks in, stays on the platform every frame and strikes; `ArenaViewTests` places packs of up to seven from the centre and checks the simulation's platform. |

Known limits: enemies still wait behind a nearer one instead of stepping round it, so at a corner a pack can queue; a hero who walks along the rim while an enemy is turning can make it walk a longer arc.

Verified: Unity EditMode 208/208, PlayMode 54/54, `Verify-Project.ps1`, .NET CombatChecks 196/196, development APK built only after both reports passed (SHA-256 `58E9C943607C34E1C997403ABD4DD35684BBA267700B0D30C09B7DE054BBC31A`; the first version, which only closed corners, was `080FF9A7F5FA79FCABBD38AFB1A122C7540728A9634864B2195AB4E17BBD90F3`).

### Implemented 2026-09-15: a frame-time probe for phone sessions

No frame rate had been measured on a device (`22`, `23`), and doc 06's budget asks for stable 60 fps and no GC spikes from spawn-heavy systems. The probe measures a development build on the phone itself, without a profiler connection and without drawing anything.

- **Probe:** `FrameTimeProbe` installs itself after the scene loads in a development player only (`Application.isEditor` or a release build leaves it out, so tests and the Editor never see it) and survives scene reloads. Every 5 s of real time it logs a window through `FrameStats`: frames, fps, average, p50, p95, p99, the longest frame, frames over 20 ms and over 35 ms. It adds the longest main-thread frame, the managed memory allocated and the GC runs, and the managed and total memory in use (Unity `ProfilerRecorder` counters). It logs a line for every wave that enters, with the enemy count and the `Cryptforge.StartWave` marker's time, and one for any other frame past 35 ms. Lines carry `[Perf]`, use the invariant culture and skip stack traces.
- **Stats:** `FrameStats` (pure) keeps up to 4096 samples per window for nearest-rank percentiles and counts every frame for the average and the longest; it sorts into its own buffer, so summaries allocate nothing.
- **Marker:** `PerformanceMarkers.StartWave` wraps `EncounterController.StartWave`: instantiating, drawing and setting up every enemy of a wave and everything that answers it. It costs nothing outside development builds.
- **First session:** see `23`. On the S23 the game held 60 fps; a wave costs about 11 ms of main thread and up to 700 KB of garbage, and a new run 17.6 MB in one frame, the costs a mid-range phone will feel first.

| Files | Change |
|---|---|
| `Scripts/Core/FrameStats.cs`, `FrameTimeProbe.cs`, `PerformanceMarkers.cs` (new) | Window statistics, the development-only probe, the wave marker. |
| `Scripts/Combat/EncounterController.cs` | `StartWave` runs its body (`SpawnWave`) inside the marker. |
| Tests | EditMode `FrameStatsTests` (2): a window's average, percentiles and missed deadlines; frames past the capacity, invalid durations and validation. The probe itself runs only on a device; its output is checked in `23`. |

Verified: Unity EditMode 210/210, PlayMode 54/54, `Verify-Project.ps1`, .NET CombatChecks 198/198, development APK built only after both reports passed (SHA-256 `03443B303B76A1E97E09239A460B768021B66CC34A640115C91023BE4C025605`), then a 150-second session on the phone.

### Implemented 2026-09-15: a local telemetry log

Handoff item D (`25`), specified in the owner's prompt through Astra's shared roadmap: a bounded local event log for device sessions, with no backend, SDK or network, beside the profile and separate from it. Built by a five-agent workflow in dependency order: the log core and the gameplay events in parallel, then the recorder, the Unity bridge and an adversarial review. The review found one real defect and fixed it: an old scene disposed after a new run began wrote `run_interrupted` under the new run's id.

- **File:** `<persistentDataPath>/telemetry/events.jsonl`; on Android `/sdcard/Android/data/com.cryptforge.prototype/files/telemetry/`. The format is JSON Lines, schema 1, UTF-8 without a byte-order mark. At 256 KB the file rotates into `events.1.jsonl` and `events.2.jsonl`, never more than three files, and a line never splits across files.
- **Envelope:** each line carries, in order:
  - `v`: the schema version.
  - `seq`: strictly increasing within a session, across runs and restarts.
  - `t` and `st`: UTC time and seconds since the session started.
  - `session`: a random id per app launch.
  - `run`: `<session>-<ordinal>`, only while a run is open.
  - `event`, then the event's fields.

  Numbers use the invariant culture.
- **Events:**
  - Session and run: `session_start`, `run_start`, `run_end`, `run_interrupted`. `run_end` is written exactly once, with result, cause (`killed_by:<enemy_id>`), floors, rooms, level, kills, upgrades, gold, banked and lost gold, and active, choice, pause and background seconds. `run_interrupted` marks a run whose scene unloaded before it ended; no end is ever fabricated.
  - Progress: `room_start` and `room_complete`, `boss_start` and `boss_end`, `floor_complete`.
  - Play: `first_kill`, `chest_opened`, `ability_used`.
  - Choices: `upgrade_offered`, `upgrade_selected`, `forge_selected`, `extract_choice`.
  - Currency: `currency_earned` (run gold per room, by source: kills or chest; forge gold banked at run end) and `currency_spent` (Relic Forge purchases, outside any run).
  - App and log: `app_background`, `app_foreground`, `telemetry_dropped`.

  Not logged: frames, ordinary hits, the device model, hardware ids or any account.
- **Time:** active seconds come from scaled time while no choice is open and the player has not paused: fighting, walking and the delays between waves. Choice and pause seconds come from unscaled time. Background seconds come from a monotonic clock between leaving and returning, with the resume frame skipped. All are kept per room, per floor and per run.
- **Writes:** lines wait in a buffer of at most 512 (the oldest are dropped and reported). They are written on the main thread only when a choice opens, at a run end or interruption, when the app goes to the background, at quit, and every 64 lines or 10 seconds.
- **Failures:** only IO and access errors are caught. The lines stay buffered, combat goes on, nothing reaches the console and the profile is never touched. After a failure only the 10-second timer retries.
- **Composition:** `CombatSetup` adds `RunTelemetryBridge` at runtime, so the scene is unchanged. It is attached before `CombatSetup` subscribes to the checkpoint choice, kills and floor clears, so each cause is logged before its effect (Extract ends the run inside `Checkpoint.Chosen`); that subscription moved below it, and `CombatSetup` is its only subscriber.
- **Bridge and session:** the bridge forwards encounter progress (compared on each hit, logged only on a change), kills, floor clears, the ability and the scene's chest spawner, and disposes the recorder on scene unload. `TelemetryRuntime` keeps one session per folder across restarts.
- **Gameplay events:** new `UpgradeService.Selected`, `ForgeService.Selected`, `RelicShop.Forged` and `WeaponShop.Forged`, with no behaviour change. `Selected` fires after the choice applied and before the offer that follows it.

| Files | Change |
|---|---|
| `Scripts/Analytics/TelemetryFields.cs`, `TelemetryRecord.cs`, `TelemetryJson.cs`, `ITelemetryClock.cs`, `StopwatchTelemetryClock.cs`, `ITelemetryStore.cs`, `FileTelemetryStore.cs`, `TelemetryLog.cs`, `TelemetrySession.cs` (new) | The pure log: fields, lines, the rotating store, the bounded buffer, the session. |
| `Scripts/Analytics/RunTelemetry.cs`, `RunTelemetryContext.cs` (new) | The pure recorder: every event, the ordering rules, time accounting, room currency. |
| `Scripts/Analytics/Unity/RunTelemetryBridge.cs`, `TelemetryRuntime.cs`, `TelemetryLocation.cs` (new); `Scripts/Core/CombatSetup.cs` | The Unity side and the wiring. |
| `Scripts/Progression/UpgradeService.cs`, `ForgeService.cs`, `RelicShop.cs`, `WeaponShop.cs` | The four events. |
| `Tools/CombatChecks/CombatChecks.csproj` | The pure telemetry sources and tests. |
| Tests | EditMode (+41): `TelemetryJsonTests` (5), `TelemetryStoreTests` (5), `TelemetryLogTests` (8), `TelemetrySessionTests` (5), `RunTelemetryTests` (12), and 6 event tests in `UpgradeTests`, `FloorTests`, `RelicForgeTests`, `WeaponForgeTests`. PlayMode `TelemetryFlowTests` (4): the first pack and level-up logged in order beside a valid profile; a defeat and restart with one `run_end`, a second run id and no duplicated selection; a telemetry folder blocked by a file, with combat going on and no console errors; the first chest walk logged with its room and healed health. |

Known limits:
- Chest gold picked up while no room is open (during the descent after Descend) appears only in `chest_opened`, not in a room's `currency_earned`.
- A flush that spans a rotation and fails part-way can repeat whole lines, with the same `seq`, on the retry.
- The monotonic clock stops during deep sleep, so `background_sec` can undercount with the screen off.
- `Application.quitting` is unreliable on Android; the other flush points cover it.
- Telemetry is not part of the balance simulation.

Verified: Unity EditMode 251/251, PlayMode 58/58 (the seven parity cases unchanged), `Verify-Project.ps1`, .NET CombatChecks 239/239, development APK built only after both reports passed (SHA-256 `BAA8E568E0B8D3D9DFB00A3FBB243311F45916AAD17C7D73CA80FF6E90623A97`), then one run on the phone whose log matched its result screen (`23`).

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

## Movement and density proof: ten enemies and a kiting route — 2026-09-16

One bounded slice that answers a single question with measurements instead of opinion: what does a normal room cost, and what does it cost differently, once ten enemies enter and the hero walks away from them. It adds no boss attack, no upgrade, no seed replay, no currency and no art, and it does not touch `Gameplay.unity`, the camera or the HUD.

**Capacity.** `PackLayout.MaxPackSize` goes from 7 to 10 with formation rows for 8, 9 and 10; rows 1-7 are byte-identical, so every authored wave forms up exactly where it always did. The new rows stay symmetric about their centre line, within the half-width of 2 and the depth limit of 4, and their closest pair is 1.5 spacing units against the required 1.1.

**Spawning ten without overlap.** `EntrySides.Place` gains an overload taking a `minimumSeparation`, and `EncounterController.SpawnWave` passes the encounter's body spacing (0.9) to it, so the scene and the Descent simulation place a wave identically. A spot that lands within a body of an earlier one closes its own corner exactly as a spot over the void does, and the open corners take the wave again. Swept over every hero spot on a 0.3 grid, four waves and pack sizes 1-10: a pack of six or fewer never moves at all, whichever corner the hero stands in, and every authored wave in `Data/Floors` holds five or fewer. From seven up, a layout moves if and only if the plain one would have started two bodies overlapping, and every layout it moves it also separates.

**A hero that walks.** `Scripts/Combat/HeroRoute.cs` defines `RouteView` and `IHeroRoute.Steer`; `StationaryRoute` holds still, `ScriptedRoute` follows an authored list of legs, and `KiteRoute` steers away from whatever is about to reach the hero while keeping clear of the rim. All three are pure, allocation-free and deterministic, and none can return a steer that is NaN, longer than one, or that walks the hero off the platform.

**The same rules in the simulation.** `Tests/Support/DescentSimulation.Run` takes an optional route and a hero speed, drives one `HeroMotion`, asks the route for a steer at the end of a frame and applies it at the start of the next - the scene's one-frame input lag - and measures every distance from the hero's own position with the scene's operand order. A run without a route is bit-identical to the old stationary baseline, which a dedicated test pins over the whole Descent. New result fields record hits and damage taken, frames off the platform, strikes landed while moving, the earliest strike after a spawn, the closest hero-to-enemy and enemy-to-enemy gaps and the hero's final floor position.

**Reaching the proof encounter without editing the scene.** `Scripts/Core/DevelopmentStart` lets the Editor or a development build start the run on a floor from `Resources/Development` when `<profile>/development/start-floor.txt` names its id. A release build returns the authored floor before touching the file system, a missing or unreadable file is the authored floor, and nothing logs in any case. `Resources/Development/Floor_DensityProof.asset` is that floor: one combat room, one wave of two Grunts and eight Cinder Mites at Ember Halls' damage rate, no next floor. Only development floors may live there.

**A rim that every runtime agrees on.** An entry pulled in short of the rim used to converge onto the rim line exactly, where whether the spot counts as on the platform is decided by the last bit of a float - the Editor's Mono and the pure .NET runner disagreed about 40 of the 56 grid spots that sit on that line, and put the same pulled-in spot on opposite sides of it by one bit (measured: 1.14e-7 in the normalized measure, about a millionth of a floor unit). `EntrySides` now gives up a ten-thousandth of the way in (`RimRelief`), which moves an entry by a thousandth of a unit at most and puts it hundreds of bits clear of the line. No authored wave is pulled in at all, so no existing number moved.

Files added: `Scripts/Combat/HeroRoute.cs`, `ScriptedRoute.cs`, `KiteRoute.cs`, `Scripts/Core/DevelopmentStart.cs`, `Resources/Development/Floor_DensityProof.asset`, `Tests/EditMode/HeroRouteTests.cs`, `Tests/EditMode/DensityProofTests.cs`, `Tests/PlayMode/DensityProofParityTests.cs`. Files changed: `Scripts/Combat/PackLayout.cs`, `EntrySide.cs`, `EncounterController.cs`, `Tests/Support/DescentSimulation.cs`, `Tests/EditMode/PackTests.cs`, `PackMotionTests.cs`, `Tools/CombatChecks/CombatChecks.csproj`.

**What this slice does not do.** It does not raise the capacity of any floor a player can reach: every authored wave still holds five enemies or fewer, and the ten-enemy encounter exists only on the development floor. It does not give the hero a body that blocks enemies, does not model between-wave frames for a routed hero, and does not close the scene's upgrade-withdrawal gap (both recorded in `22`). It measures; it does not retune. The balance question it surfaced - kiting the Staff through the authored Descent - is left for a later slice.

## Shared enemy sprites across waves and restarts — 2026-09-16

`Art/Unity/EnemySpriteCache` lazily draws each enemy look once per application session. Three body frames and three matching flash silhouettes per look are shared; the two bar textures are shared across all looks. Six looks therefore retain at most 38 sprites/textures. `EnemyLookView` borrows this art and still owns its animation clocks, renderers, flash state through `CombatantView`, and health-fill scale. Destroying an enemy or reloading Gameplay does not destroy the shared art. Application quit and subsystem registration release both native sprites and their textures; the latter also prevents stale state with domain reload disabled. The ownership exception and alternatives are recorded in `19`.

Four added PlayMode cases cover the six-look art contract and repeated borrowing, release of every native resource and fresh recreation, scene restart followed by `Resources.UnloadUnusedAssets`, and independent health/critical flash state when another enemy is destroyed. Existing movement, economy, telemetry and simulation parity cases remain unchanged. The first Unity attempt exposed missing friend-assembly access (`CS0122`), which the single-assembly compile check cannot detect; this was fixed before the full passing gate. No APK was built from that failed attempt.

The Windows Unity runner now waits for the Editor process itself with `WaitForExit`, rather than waiting for its long-lived licensing/helper descendants, and starts it hidden. This fixes the observed post-exit stall without weakening either suite or the APK gate. One stale lock from the failed compiler run was archived under ignored `TestResults/` only after confirming no Unity process remained.

Files added: `Scripts/Art/Unity/EnemySpriteCache.cs`, `Tests/PlayMode/EnemySpriteCacheTests.cs` and their new GUID metadata. Changed: `Scripts/UI/EnemyLookView.cs`, ownership comments in `PixelSpriteFactory.cs`, `Tests/PlayMode/ArtHudFlowTests.cs`, `Tools/Run-UnityTests.ps1`, documentation and the arena production-plan status. No scene, prefab, art canvas, gameplay rule, package lock or project setting changed.

**Verified:** .NET **257/257**, Unity compile **0 warnings/errors**, EditMode **269/269**, PlayMode **74/74**, Android build exit **0**, static integrity **284 unique asset/folder GUIDs / 555 scene objects/components**. Development APK **24,738,586 bytes**, SHA-256 **`4A2F37C48A7DC451776772479780F9D9C16D2D488DA33F54365A40A4B92AF4CD`**. The installed S23 package hash matches. Device measurements and their whole-frame attribution limits are recorded in the corresponding entry in `23`.

## Shared platform and backdrop sprites across restarts — 2026-09-16

Following the enemy cache measurement, `ArenaSpriteCache` retains the current platform's surface and two light frames, plus the fixed void backdrop's sprites. `VoidSprites` generates haze, galaxy, star layers, sparkle/flame poses, islands, rings, chains and rubble once; it retains only sprites, layout and small attachment coordinates, not the temporary CPU canvases. Renderers, sky mesh, drifter transforms, flicker phases and platform animation clocks still belong to the scene. Existing drawing functions, palette, pivots, sorting, geometry, camera and gameplay are preserved.

Platform sharing checks near corner, far corner and half-width exactly. The first geometry is retained; a different geometry receives private art that its view destroys, keeping the session cache bounded and avoiding incorrect dimensions. Application quit and subsystem registration release both sprites and textures. The architecture choice and the limitation for future multi-room art are recorded in `19`.

Four added PlayMode cases cover repeated builds reusing every visible sprite and attachment/sorting state, destroying one view while another survives unused-asset cleanup, three different geometry variants releasing only their private art, all native resources including hidden frames being released on session reset, and actual Gameplay reload followed by pause/resume animation. The build-cost diagnostic records CPU-side Editor timings and real `GC.Alloc` event counts, never treating the unsupported zero-byte Mono counter as a valid measurement. The phone probe gains separate `Cryptforge.BuildBackdrop` and `Cryptforge.BuildPlatform` markers and labels reported bytes as whole-frame allocation.

Added: `Scripts/Art/Unity/ArenaSpriteCache.cs`, `VoidSprites.cs`, `Tests/PlayMode/ArenaArtCacheTests.cs` and metadata. Changed: `UI/Arena/ForgePlatformView.cs`, `VoidBackdropView.cs`, `UI/ArenaView.cs`, `Core/PerformanceMarkers.cs`, `FrameTimeProbe.cs`, ownership comments in `PixelSpriteFactory.cs`, `ArtHudFlowTests.cs`, and documentation. Scene YAML, existing metadata, imported assets, combat/economy/profile/telemetry logic, project settings and package lock remain untouched.

**Verified:** .NET **257/257**, Unity compile **0 warnings/errors**, EditMode **269/269**, PlayMode **78/78**, Android build exit **0**, static integrity **287 unique asset/folder GUIDs / 555 scene objects/components**. Development APK **24,738,586 bytes**, SHA-256 **`6DE8F2A9A2B92B3CA15A50FA5EA71980ACB8E91C60CF832C7A126F1D8CC39ED6`**; installed S23 package hash matches.

The cache holds **37 textures**, **8,365,956 RGBA32 texel bytes** (about 7.98 MiB, excluding native object overhead). In the full Editor suite, repeated platform/backdrop builds took **0.06/0.65 ms**, with **6/234 allocation events**, versus the preceding uncached diagnostic's **18.08/27.51 ms**, **82/1033 events**. Three S23 warm restart frames allocated **1008.5–1131.4 KB**, versus **17,620.8–17,742.4 KB** in the preceding enemy-cache session (approximately 94% less). Platform/backdrop markers measured **0.03–0.04 / 0.97–1.06 ms**. Device frame/main-thread limits and cold-start exclusions are recorded in `23`. This completes the bounded environment reuse slice, not all restart/startup optimisation or production environment art.

## Room-bound framing and the first furnace layer — 2026-09-16

`FollowFraming.ConstrainToArena` moves the free HUD band's centre toward the room interior near the rim, while reserving space around the hero for nearby enemy bodies/bars. `ArenaCameraFollow` reads the scene's actual `ArenaView` geometry and origin; width 9 and smooth movement remain. It no longer promises the hero's feet always sit exactly at the band's centre. The rule adapts to the available band height, including the existing thin-band fallback.

The scene now assigns `ENV_Furnace_Cavern_v1.png`, an original generated 1024 x 1536 distant environment layer. `VoidBackdropView` uses that imported asset with aspect-preserving cover and bounded parallax, leaving the existing procedural sky mesh underneath and skipping unused stars/islands. Without the imported sprite/camera it still builds the session-cached procedural void. Platform, actors, HUD, collision and all gameplay/progression systems remain independent. No full-island boss room, new encounter density, seed or upgrade was implemented.

Changes: `FollowFraming.cs`, `UI/ArenaCameraFollow.cs`, `UI/ArenaView.cs`, `UI/Arena/VoidBackdropView.cs`, two new scene references, the environment PNG/import metadata, `FollowFramingTests`, `ArenaViewTests`, the reload/animation case in `ArtHudFlowTests`, and docs/art provenance. Three new pure cases sweep the platform at two portrait heights and exercise shifted/small/large geometry. One new scene case checks actual hero body bounds, room binding and background coverage at the centre and all four corners on both aspect ratios. Reload testing checks imported sprite identity, skipped procedural stars, mesh cleanup and platform pause/resume.

The temporary Editor setup changed only two scene references; it and the offscreen capture fixture were removed before the full suite/build. Seven actual Unity renders compare centre/far/right at 1080x2340 and 1080x1920, plus unbounded far follow. Their ten frozen visual actors are staging without combat/bar controllers, using real spawn placements. Safe insets are simulated. See `ArtDirection/2026-09-16/furnace-layer-01/README.md` for source/import settings and visual limits; these renders are not device evidence.

**Verified:** .NET **260/260**, compile **0 warnings/errors**, EditMode **272/272**, PlayMode **79/79**, Android exit **0**, static integrity **289 unique asset/folder GUIDs / 555 scene objects/components**. Development APK **24,739,335 bytes**, SHA-256 **`3704897005DA235B5C23DD70761DD2D784387489D5A896091F9241735CCB6F6F`**, installed S23 hash matched. Live far-rim and right-corner drags, choice overlays, proof-floor warm restart and normal-floor cleanup passed the short phone review in `23`. One warm proof restart allocated **1005.5 KB** across the whole frame, with backdrop/platform build **0.18/0.06 ms**. This is not a new sustained-combat or mid-range benchmark.
