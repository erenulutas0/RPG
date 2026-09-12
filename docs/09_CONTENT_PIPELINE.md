# Content Production Pipeline

## Goal

New content should be cheap to add without destabilizing code.

## Content types

- Hero
- Weapon
- Enemy
- Upgrade
- Encounter
- Boss
- Biome
- Reward

## Golden rule

A new content item should usually require:
1. definition data,
2. visual/audio assets,
3. optionally one behavior module,
4. tests/playtest.

It should not require editing five unrelated managers.

## Hero checklist

- stable ID
- role
- base stats
- starting weapon
- passive
- art
- animation
- SFX
- unlock rule
- analytics dimension

## Weapon checklist

- ID
- tags
- damage model
- cadence
- targeting
- prefab
- SFX/VFX
- upgrade pool compatibility

## Upgrade design schema

```text
id
display_name
rarity
tags
max_stacks
prerequisites
exclusions
effect_type
effect_values
description_template
```

## Tags

Use tags to enable synergy without hardcoding every pair.

Examples:
- Fire
- Poison
- Projectile
- Melee
- Crit
- Summon
- Shield

Then upgrades can target categories:
“Projectile attacks gain +1 pierce.”

## Encounter authoring

EncounterDefinition:
- enemy pools,
- spawn weights,
- wave count,
- intensity,
- elite chance,
- reward,
- boss reference.

## Content validation tools

Eventually add editor validators:
- duplicate IDs,
- missing icons,
- invalid prerequisites,
- impossible upgrade exclusions,
- missing localization keys.

## Localization readiness

Even if MVP launches only in English/Turkish:
- no user-facing strings hardcoded in scripts,
- stable localization keys,
- UI supports text expansion.

## Scope budget

Before making a new asset, classify:

**Core** — required for prototype.
**Proof** — required for vertical slice.
**Growth** — only if metrics justify.
**Nice-to-have** — backlog.

If everything is “Core,” scope is broken.
