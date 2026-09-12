# Code Standards & AI Coding Guardrails

## Goal

AI should make development faster without creating an unmaintainable Unity project.

## Mandatory rules

- C# nullable/defensive thinking where practical.
- One responsibility per class.
- No hidden scene searches in hot paths.
- Avoid `FindObjectOfType`/tag searches as architecture.
- No static global mutable state unless explicitly justified.
- No magic strings for content IDs in gameplay code.
- No hardcoded economy values outside config/data.
- No swallowing exceptions silently.
- No editing ScriptableObject definitions at runtime as save state.
- No “god manager.”

## Naming

- Classes: PascalCase
- Methods/properties: PascalCase
- private fields: `_camelCase`
- local vars: camelCase
- IDs: stable lowercase snake_case strings, e.g. `weapon_fire_staff`

## Comments

Comment:
- why,
- invariants,
- unusual performance choices,
- non-obvious math.

Do not comment obvious code.

## Public API

Prefer small explicit methods:
- `ApplyDamage(DamageContext context)`
- `AddModifier(StatModifier modifier)`
- `StartEncounter(EncounterDefinition definition)`

Avoid giant methods with many primitive parameters.

## Testing targets

Unit/EditMode:
- damage calculation,
- stat modifiers,
- reward calculation,
- economy curves,
- save migrations.

PlayMode:
- run starts,
- enemy spawns,
- upgrades apply,
- death ends run,
- save/load roundtrip.

## AI-generated code review checklist

Before accepting:
1. Does it compile?
2. Does it introduce a new architecture without request?
3. Is there duplicated logic?
4. Are allocations happening per frame?
5. Are subscriptions unsubscribed?
6. Can null references occur from scene setup?
7. Is state persisted correctly?
8. Did the AI change unrelated files?
9. Are new dependencies justified?
10. Are tests added for logic-heavy changes?

## Performance guardrails

Avoid in `Update()`:
- LINQ,
- repeated `GetComponent`,
- allocations,
- string formatting,
- scene searches.

This is not a ban on all LINQ; it is a hot-path rule.

## Git workflow

Suggested:
- `main` = releasable,
- feature branches,
- small commits,
- conventional-ish messages.

Examples:
- `feat: add weapon runtime`
- `fix: prevent duplicate death reward`
- `test: add save migration coverage`

## Definition of done

A feature is done when:
- behavior works,
- basic edge cases handled,
- no obvious console errors,
- relevant analytics event included if needed,
- data is configurable,
- tested on device if UI/performance-sensitive,
- docs updated if architecture changes.
