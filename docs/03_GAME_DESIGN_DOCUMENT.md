# Game Design Document (GDD)

## Working title

Use a temporary codename until visual identity is stable.

Recommended codename: **Project Cryptforge**

Do not spend days naming the game during prototype.

## Genre

- Incremental RPG
- Auto-battler
- Dungeon crawler
- Buildcraft / roguelite-lite
- Mobile portrait

## Camera / orientation

**Default recommendation: portrait.**

Why:
- one-handed usability,
- strong fit for check-in play,
- easier bottom-screen upgrade interactions,
- distinctive versus PC-derived landscape layouts.

Re-evaluate only if multi-character battlefield readability fails.

## Core loop

1. Start run
2. Hero auto-attacks
3. Kill enemies
4. Gain XP / gold / temporary resources
5. Choose upgrades
6. Build becomes mechanically stronger
7. Encounter elite/boss
8. End run/checkpoint
9. Convert rewards into permanent progress
10. Unlock new possibilities
11. Start another run with a different target/build

## First prototype combat

### Hero
One melee or short-range hero.

Base stats:
- HP
- attack damage
- attack interval
- attack range
- crit chance
- crit multiplier
- move speed only if movement is actually used

Avoid adding 20 stats.

### Enemies
Prototype:
- Grunt: baseline
- Runner: fast, low HP
- Tank: slow, high HP
- Ranged: later
- Elite: later
- Boss: one readable mechanic

### Weapon prototype set
1. Sword — direct cleave
2. Bow — projectile / pierce
3. Staff — slow AoE
4. Daggers — fast attacks / crit
5. Optional: cursed orb — orbit/periodic damage

Only 3 are necessary for earliest build.

## Upgrade taxonomy

### Numeric
- +damage
- +attack speed
- +crit
- +HP

### Behavioral
- projectiles +1
- attacks pierce
- crits trigger bleed
- kills explode
- burning spreads
- every N attacks launches special attack

### Synergy
- poison can crit
- bleed duration converts to burst
- fire + knockback creates explosion
- low-HP enemies execute

Target ratio for interesting runs:
- 30–40% basic numeric,
- 40–50% behavioral,
- 10–20% synergy/capstone.

## Run structure

Prototype option:

- 5 rooms/checkpoints
- room 1–3 normal escalation
- room 4 elite
- room 5 boss

Alternative continuous model:
- timed waves,
- milestone bosses.

Recommendation: use **room/checkpoint structure** initially because:
- easier pacing,
- clean reward screens,
- easier analytics,
- natural ad/revive hooks later.

## Progression layers

### In-run
Temporary.
- level-ups,
- weapon enhancements,
- status synergies.

### Meta
Permanent.
Keep small:
- unlock heroes,
- unlock weapons,
- tiny account upgrades,
- codex/collection.

Avoid large permanent raw-stat multipliers early because they can trivialize balancing.

## Hero future roster

### Vanguard
- melee
- shield / counter identity
- beginner-friendly

### Ranger
- ranged
- projectile count / pierce
- positioning-lite identity

### Arcanist
- spell cycles
- elemental interactions

### Necromancer
- summons
- kill-based scaling

### Rogue
- crit / execute / combo

### Paladin
- defense transformed into offense

MVP: **Vanguard only**.  
Vertical slice: Vanguard + one mechanically opposite hero (Ranger or Necromancer).

## Boss design principles

Bosses should test a build, not demand twitch controls.

Good:
- shield phase that rewards burst timing,
- summons that reward AoE,
- enraged low-HP phase,
- alternating armor weakness.

Bad for this product:
- precision bullet hell,
- tiny dodge windows,
- complex manual positioning.

## Failure

Failure should:
- end the run,
- grant some progress,
- clearly show why,
- immediately offer another attempt.

Never punish with long reloads.

## Session targets

- first meaningful action: <10 sec
- first kill: <15 sec
- first upgrade choice: 30–60 sec
- first visible “power spike”: <3 min
- first boss/checkpoint: ~5–8 min for early prototype

## UX priority

The battle screen should answer at a glance:
- hero health,
- enemy threat,
- current weapon,
- current build effects,
- progress to next upgrade,
- current room/boss progress.

Do not flood the screen with currencies during combat.

## Audio
Prototype:
- hit,
- crit,
- enemy death,
- coin/loot,
- upgrade,
- boss cue.

A few satisfying sounds matter more than a full soundtrack during prototype.

## Accessibility baseline
- readable font,
- color is not the only status indicator,
- vibration toggle,
- sound/music controls,
- reduce flash option later,
- large touch targets.
