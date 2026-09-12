# Product Vision

## Working vision

Build a **mobile-first, portrait, one-thumb-friendly incremental auto-battler RPG** where the fun comes from discovering strong combinations of heroes, weapons and upgrades—not merely watching numbers rise.

The fantasy should be:

> “I entered weak, discovered a clever build, watched it become absurdly strong, beat a boss, brought permanent progress home, and immediately want to try a different build.”

## Product pillars

### 1. Readable chaos
Combat can become visually exciting, but a player must always understand:
- who is attacking,
- what upgrade just mattered,
- why damage increased,
- why they died.

### 2. Builds, not only stats
A +10% damage upgrade is acceptable as filler. The memorable upgrades should change behavior:
- projectile splits,
- poison can crit,
- burning enemies explode,
- shield breaks deal AoE,
- attacks chain,
- summons inherit weapon effects.

### 3. Fast reward cadence
Something meaningful should happen frequently:
- enemy kill,
- coin burst,
- loot drop,
- upgrade choice,
- room clear,
- mini-boss,
- boss,
- unlock.

Avoid long dead travel sections.

### 4. Mobile-native convenience
- portrait orientation by default,
- playable with one hand,
- no precision controls,
- sessions can be interrupted,
- fast resume,
- optional offline progression later,
- no mandatory ad interruptions in the early product.

### 5. Expandable content system
The codebase should make a new hero/weapon/enemy mostly a **data + art + behavior module**, not a rewrite.

## What we are deliberately NOT making

For MVP:
- no multiplayer,
- no PvP,
- no guilds,
- no chat,
- no open world,
- no complex inventory grid,
- no server-authoritative economy,
- no gacha,
- no 50-currency economy,
- no 100-level campaign,
- no “AI-generated everything” visual identity.

## Differentiation thesis

The opportunity is not “Bones and Coins on mobile.”

The opportunity is:
- the simplicity and clarity of idle/incremental games,
- the satisfaction of survivor-like build escalation,
- the collectability of light RPG progression,
- real hero/weapon synergy,
- mobile session design.

Possible future identity directions:
1. **Dark cute necro-fantasy** — compact readable pixel art, bones/crypts, comedic loot.
2. **Guild of misfits** — each hero has mechanically extreme identity.
3. **Alchemy/combo focus** — status interactions are the core strategy.
4. **Dungeon economy** — loot can be equipped, salvaged or invested into permanent unlocks.
5. **Party tactics-lite** — 2–3 heroes with formation/synergy, still auto-controlled.

Do not choose all of these at once. Prototype should validate #2 + #3 first.

Originality note (2026-09-12): the reference game's identity is skeletons, catacombs and coins in an old-school Diablo style. Direction #1 and any "bones/crypt/coins" theming sit on top of it. Treat #1 as excluded unless a style board proves clear separation; see `24_RESEARCH_ROUND_2_REFERENCE_AND_FUTURE.md`.

## Player promise

Within the first 3 minutes:
- player sees combat,
- makes at least one choice,
- becomes visibly stronger,
- defeats something meaningful.

Within the first 10 minutes:
- player completes a run or boss checkpoint,
- unlocks or previews another build path,
- understands the next goal.

Within the first day:
- player has at least one “I discovered this combo” moment.

## Commercial hypothesis

The project can work commercially if:
- core gameplay retains players before monetization,
- creative marketing communicates escalation clearly,
- production cost stays low through a controlled content pipeline,
- rewarded ads are optional and high-value,
- IAP is convenience/cosmetic/value-pack oriented rather than mandatory power.

## Kill criteria

Stop or redesign before scaling if:
- testers describe gameplay as “watching numbers” with no decisions,
- build choices are obviously solved,
- first-session completion is poor because UI is unclear,
- players churn before first meaningful upgrade,
- art pipeline consumes more time than gameplay iteration,
- UA creatives cannot explain the fantasy in 3–5 seconds.
