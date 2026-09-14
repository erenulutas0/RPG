# Economy & Balancing

## Economy goals

The economy exists to:
- pace discovery,
- create goals,
- reward play,
- support monetization later without requiring it.

It should not exist to create arbitrary suffering.

## MVP currencies

Use at most two:

### Gold
- earned in runs,
- used for basic permanent progression / unlocks.

### Shards (optional, later)
- rarer,
- used for hero/weapon unlock progression.

Do not add premium currency until IAP design is actually being implemented.

## Power curve

Separate:
- **run power**: explosive, temporary,
- **meta power**: slower, controlled.

This prevents permanent upgrades from destroying run balance.

## Basic cost model

For level-based upgrades, start with a geometric model:

`cost_n = base_cost * growth^(n-1)`

Example:
- base 100
- growth 1.18–1.35 depending on system

Do not hardcode constants across scripts.
Store curves/data in ScriptableObjects or balancing tables.

## Enemy scaling

Prototype:
`HP_wave = base_hp * hp_growth^wave`
`DMG_wave = base_dmg * dmg_growth^wave`

With the accepted floor structure, `wave` is the floor index for the base curve; rooms inside a floor add a small linear step, and the floor modifier applies last. Elites and bosses use their own multipliers on top.

Use different curves for:
- normal enemies,
- elites,
- bosses.

### At-risk rewards

Rewards earned since the last floor checkpoint are at risk when the player chooses Descend. Start with **50%** lost on death and tune from playtests; keep the fraction in `EconomyConfig`, never in code.

Boss difficulty should come partly from mechanics, not only HP inflation.

### Run experience

Each enemy carries its own experience and gold reward, so fodder in a pack can pay less than a named enemy. Levels cost progressively more:

`xp_to_reach_level_n = xp_per_level * n + growth * n * (n - 1) / 2`

The prototype uses 10 and 2 (10, 12, 14, ... per level). Named enemies give 10 XP; the fodder Cinder Mite gives 1 XP and no gold. Packs of up to five enemies would otherwise use up the ten upgrade stacks on floor 1. Both constants live in `EconomyConfig`.

## Upgrade valuation

Create a normalized “power budget” for upgrades.

Example only:
- common: ~1.0 unit
- rare: ~1.6 units
- epic: ~2.4 units

Behavioral upgrades may require simulation rather than arithmetic.

Scale damage upgrades by percent. The prototype's Tempered Edge adds +50% damage per stack: a flat +5 was worth 50% on a 10-damage weapon but 83% on the 6-damage Daggers. Percent bonuses add together before they multiply, so five stacks give +250%, not ×7.6.

## Synergy danger

Multiplicative effects can explode.

Example:
- attack speed × projectile count × crit × status spread.

Add:
- caps where necessary,
- diminishing returns for selected stats,
- deterministic test simulations.

But preserve “broken build” excitement. The goal is controlled absurdity, not perfect symmetry.

## Loot philosophy

MVP recommendation:
- no Diablo-like gear inventory,
- no dozens of affixes.

Use:
- weapon unlocks,
- run upgrades,
- simple end-run rewards.

Add inventory only if testers ask for deeper collection.

## Offline progression

Do not implement in first mechanical prototype.

When added:
- cap offline duration,
- calculate from server time only if cheating becomes economically relevant,
- before monetization, local time may be acceptable for prototype testing,
- reward should help return but not replace active play.

## Balancing workflow

1. define intended time-to-kill,
2. define intended upgrade cadence,
3. simulate simple curves,
4. playtest,
5. instrument analytics,
6. change values in data, not code.

## Spreadsheet fields to eventually maintain

### Weapons
- id
- base damage
- interval
- range
- behavior (direct hit, cleave, area), splash radius and fraction
- crit rhythm (every Nth attack) and crit multiplier
- projectile count
- tags
- scaling coefficients
- rarity
- unlock condition (prototype: Forge price in gold)
- which simulated fight it wins (packs, bosses)

### Enemies
- id
- HP
- damage
- speed
- gold reward
- experience reward
- tags
- spawn weight

### Upgrades
- id
- rarity
- effect
- max stacks
- prerequisites
- exclusions
- tags
- power estimate

### Floors and modifiers
- floor id
- biome
- scaling tier
- modifier pool
- boss id
- modifier id, effects, power estimate

### Relics (meta unlocks)
- id
- effect (behavior, not raw stat)
- amount / threshold
- price in gold
- which simulated path it rescues

## Anti-patterns

- 20 currencies,
- hidden soft caps,
- paywalling basic experimentation,
- exponential costs without meaningful new mechanics,
- upgrades that only increase numbers forever,
- economy tuned around ad watching before retention is proven.
