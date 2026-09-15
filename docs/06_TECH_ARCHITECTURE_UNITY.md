# Technical Architecture — Unity

## Engine decision

Use **Unity 6.x**, pinned to one stable production version.

Do not upgrade the editor/package stack in the middle of MVP without a reason.

Useful official packages for this project:
- 2D Aseprite Importer,
- 2D Pixel Perfect,
- Addressables (introduce when content scale justifies it),
- Unity Test Framework,
- optional analytics/monetization SDKs later.

## Architectural principles

- Data-driven content.
- Composition over inheritance.
- Core game rules are testable without scene dependencies where practical.
- MonoBehaviours should orchestrate Unity lifecycle/presentation, not own all business logic.
- Avoid “one GameManager that knows everything.”
- Avoid premature enterprise architecture.

## Suggested project folders

```text
Assets/
  _Project/
    Art/
      Characters/
      Enemies/
      Weapons/
      Environment/
      UI/
      VFX/
    Audio/
      Music/
      SFX/
    Data/
      Heroes/
      Weapons/
      Enemies/
      Upgrades/
      Encounters/
      Economy/
    Prefabs/
      Characters/
      Enemies/
      Projectiles/
      UI/
      VFX/
    Scenes/
      Bootstrap/
      MainMenu/
      Gameplay/
    Scripts/
      Core/
      Combat/
      Progression/
      Content/
      Economy/
      Save/
      UI/
      Audio/
      Analytics/
      Services/
      Utilities/
      Editor/
    Tests/
      EditMode/
      PlayMode/
    Settings/
  ThirdParty/
```

## Assemblies

Consider asmdefs once project grows:
- Project.Core
- Project.Gameplay
- Project.UI
- Project.Editor
- Project.Tests

Do not fragment into 20 assemblies on day one.

## Scene strategy

### Bootstrap
Persistent service setup:
- SaveService
- AudioService
- AnalyticsService
- ConfigService

### MainMenu
- play,
- loadout,
- progression.

### Gameplay
- combat,
- run state,
- HUD,
- encounter flow.

For very small MVP, Bootstrap can initialize then load menu/gameplay.

## Data model

Use ScriptableObjects for authoring static definitions:

```csharp
HeroDefinition
WeaponDefinition
EnemyDefinition
UpgradeDefinition
EncounterDefinition
EconomyConfig
```

Important:
ScriptableObjects are **definitions**, not runtime mutable save data.

Runtime state should be separate:

```csharp
HeroRuntimeState
RunState
PlayerProfile
InventoryState
```

## Combat architecture

Recommended components:

- `Health`
- `DamageReceiver`
- `Targeting`
- `AttackController`
- `WeaponRuntime`
- `StatusController`
- `MovementController` if needed
- `DeathHandler`
- `RewardEmitter`

Use interfaces/events where useful:
- `IDamageable`
- `ITargetable`
- `IStatusReceiver`

Do not build a huge inheritance tree:
`Character -> Enemy -> FlyingEnemy -> PoisonFlyingEnemy...`

## Weapon architecture

`WeaponDefinition` contains authoring data:
- base stats,
- tags,
- prefab refs,
- behavior type/config.

`WeaponRuntime` executes behavior.

Behavior modules:
- direct hit,
- projectile,
- area,
- periodic,
- summon.

Upgrades should modify runtime stat/effect containers rather than editing definitions.

## Modifier system

Build a controlled modifier pipeline:

```text
BaseStats
 -> additive modifiers
 -> multiplicative modifiers
 -> clamps
 -> derived stats
```

Behavior modifiers can be event hooks:
- OnHit
- OnCrit
- OnKill
- OnDamageTaken
- OnRoomStart
- OnBossStart

Avoid reflection-heavy “magic” systems in MVP.

## Event flow

Use C# events or a lightweight event bus for meaningful decoupling.

Good:
- EnemyDied
- UpgradeSelected
- RoomCompleted
- RunEnded

Bad:
- sending every frame position update through global event bus.

## Spawning and pooling

Pool:
- enemies if spawn volume is high,
- projectiles,
- hit VFX,
- floating damage numbers.

Do not pool everything blindly.

## Save system

MVP:
- versioned JSON or binary-ish serializable DTO,
- stored in `Application.persistentDataPath`,
- atomic-ish write pattern: temp → replace,
- default fallback if corrupted.

Example:

```json
{
  "saveVersion": 1,
  "softCurrency": 0,
  "unlockedHeroIds": ["hero_vanguard"],
  "unlockedWeaponIds": ["weapon_sword"],
  "settings": {}
}
```

Add migrations when schema changes.

Cloud save later, only after local loop is stable.

## Addressables

Do not introduce Addressables solely because it is “professional.”

Introduce when:
- content download size matters,
- remote content is needed,
- asset groups become large,
- seasonal content needs separate delivery.

Unity's Addressables system supports asynchronous local/remote loading and dependency management, making it a good scaling option later.

## Pixel art

Use:
- Pixel Perfect Camera,
- consistent Pixels Per Unit,
- point filtering where appropriate,
- carefully chosen reference resolution.

Aseprite files can be imported directly with Unity's official 2D Aseprite Importer.

## Performance budget

Prototype targets:
- stable 60 FPS on a representative mid-range device,
- optional 30 FPS fallback only if needed,
- no repeated GC spikes from spawn-heavy systems,
- low overdraw,
- atlas sprites where useful,
- limit transparent full-screen VFX.

Profile on real Android devices early.

Status 2026-09-15: a development build measures itself with `FrameTimeProbe` (logcat `[Perf]` lines, see `22`). On a Samsung SM-S911B, a flagship, a 150-second session held 60 fps through combat, choices and 20 wave spawns, with memory flat at 110–116 MB. Two costs will matter on a mid-range device. Each wave spends about 11 ms of main thread (2.7–16.6 ms) and 77–700 KB of garbage entering, because every enemy draws its own sprites. Each new run allocates 17.6 MB in one 50–67 ms frame: the frame in which the scene loads again and redraws its placeholder art from code. No mid-range device has been measured; see `23`.

## Dependency strategy

Keep third-party dependencies minimal.

Candidate later:
- Firebase Analytics / Crashlytics,
- Unity LevelPlay or another mediation stack,
- platform billing package.

Every SDK adds:
- binary size,
- initialization complexity,
- privacy/data-safety obligations,
- failure modes.

## Backend decision

### MVP
No custom backend.

### Add backend when one of these becomes real:
- account sync across devices,
- live economy configuration,
- server-validated purchases,
- anti-cheat with financial impact,
- timed live events,
- competitive leaderboards,
- remote content ownership.

Do not build microservices for a single-player prototype.
