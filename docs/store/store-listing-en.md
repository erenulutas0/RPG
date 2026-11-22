# Store listing, English (draft, 2026-11-19)

The English Play listing, a translation added to the Turkish default (docs/59 decision 1: Turkish and English at
release). The game's English words are used as the game shows them (the Anvil, the Forge, efsun, basma as the game's
own word for a strike, the floors' and enemies' names). Counts are Unicode characters, line breaks included, measured
with Python's `len` on the exact text of each block; limits: name 30, short description 80, full description 4,000
([store listing limits](https://support.google.com/googleplay/android-developer/answer/9859152)). The metadata rules
are the same as in store-listing-tr.md (no "best", "#1", "new", prices or calls to action; no capitals for emphasis;
no emojis in the short description; no keyword stuffing; no competitors' names).

## 1. App name (30 at most)

| Option | Count | Note |
|---|---|---|
| **Cryptforge** (recommended) | 10 | The same name in both listings |
| Cryptforge: Horde and Anvil | 27 | Only if the Turkish listing takes "Cryptforge: Sürü ve Örs" |

## 2. Short description (80 at most)

| Option | Count |
|---|---|
| **Hold against the horde and strike your gear to +9 between runs. It never breaks.** (recommended) | 80 |
| Hold against the horde, bank your loot, strike your gear to +9. It never breaks. | 80 |
| Survive the horde in portrait, then strike your gear up to +9 between runs. | 75 |

## 3. Full description (4,000 at most)

**Counts:** the base text is **2,201** characters; the skills paragraph adds 148 and the arena paragraph 159. With
both: **2,508**. (2026-11-22: the first paragraph says the hero climbs, "As you climb floor by floor", where it said
"Deep in the forge": the hero always climbs, docs/44 section 1 and docs/60 section 10.)

### 3.1 Base text

```text
Cryptforge is a single-player action roguelite played in portrait. As you climb floor by floor, the horde closes in: you hold, grow stronger and try to get your loot out alive. Then you strike your gear up to +9 at the Anvil. Your item never breaks.

The run
- Your hero attacks on their own. Your left thumb steers, your right thumb fires your skill. The game is played with two hands.
- Start in the lava-lit halls of the Foundry, cross the Quicksilver Vaults and the Gloam Gardens, and face the Final Swarm.
- At every level-up, pick your upgrade from a choice of cards: new weapons, tomes, evolutions. Build your own way with the Sword, the Staff and the Daggers.
- Bosses hold the floors' gates, among them the Forge Warden and the Glass Regent.
- At the checkpoint, choose: bank your gold and extract, or climb to the next floor. Fall, and part of that floor's gold is lost; the items you found always stay with you.

The Anvil, basma and efsun
- Bosses, elites and the walk-out drop items: weapon, armour, amulet, ring, in four rarities from Common to Gold.
- Strike your item up to +9 at the Anvil. The chance is written on the button.
- A strike that misses never breaks the item and never lowers its level. Up to +3, every strike holds.
- Every miss fills the anvil's memory. When it is full, the next strike holds for certain.
- Reroll the efsun lines: the new ones appear beside the old. Keep the old or take the new; nothing is lost until you choose.
- Melt items, move a level to another piece, forge relics at the Relic Forge.

The heroes
- The Vanguard holds the front line. Forge Burst blasts every foe close by.
- Ilya Kest, the Cinder Arcanist: every hit sets a foe alight. Crucible Vent erupts the burning.

Fair play
- The game is free.
- If you like, watch a short video at the end of a run to multiply that run's gold by 1.5. Skip it and you lose nothing. There are no other ads.
- The store sells one thing: Skip reward videos. It gives the same bonus without the video.
- Power is never sold. Gold, materials, boosters, efsun and items are earned by playing.
- No account needed. Plays offline; the video bonus is offered only when you are connected.
- In English and Turkish.
```

### 3.2 Only if hero skills 2-3 ship - add as the last line of "The heroes" (+148)

```text
- Each hero has three skills. The second opens on your first clear of floor 2, the third on your first clear of floor 3, and the Forge forges them.
```

With it, line one of "The run" can say "fires your skills" (+1).

### 3.3 Only if P0's arena has a screen in the release - add before "Fair play", after a blank line (+159)

```text
The arena
- Test your hero in automatic duels against your own past heroes and rivals the game makes up. The arena is played on your phone; it is not online.
```

The sources behind each claim are in store-listing-tr.md section 3.4.

## 4. The in-app product

| Field | Text | Count |
|---|---|---|
| Name | Skip reward videos | 18 |
| Description | Get the end-of-run bonus (1.5 times the run's gold) without watching a video. Bought once; it comes back on a new device with the same Google account. Gives no power. | 166 |

## 5. Screenshots

Same specs, method and order as store-listing-tr.md section 5 (1080 x 1920 portrait, release build, at least three
for recommendation eligibility), captured in English.

| # | Frame | Caption (count) |
|---|---|---|
| 1 | **Real vertical combat** in the Foundry | Hold against the horde (22) |
| 2 | **"Your item never breaks"**: the Anvil right after a miss, "Missed: +7 kept / The anvil remembers: +1 memory" | Your item never breaks (22) |
| 3 | **The efsun choice**: old and new lines side by side, "Keep old" / "Take new" | Keep the old efsun or take the new (34) |
| 4 | The level-up cards | Build your run, level by level (30) |
| 5 | A boss mid-fight | Topple the boss at the gate (27) |
| 6 | The checkpoint, "Checkpoint: extract or climb?" | Extract or climb (16) |
| 7 | The hero choice | Two heroes, two ways to play (28) |
| 8 | The arena (if it ships) or the map choice | Test your hero in the arena (27) / Three floors and the Final Swarm (32) |

## 6. Feature graphic

The brief in store-listing-tr.md section 6; one file serves both listings if it has no tagline.

**Alt text (112):** A knight stands against a crowd of stone golems in a lava-lit foundry; beside him a +9 sword glows on the anvil.

## 7. Keywords

Woven into the copy: roguelite, action, single-player, horde, boss, loot, gear, +9, strike, Anvil, efsun, portrait,
offline, hero, sword, staff, daggers. Not in the copy: roguelike, action RPG, survivor, dungeon; add one naturally
only if search data shows it matters. No competitors' names.

## 8. Category, tags and contact details

As store-listing-tr.md section 8 (Game > Action; the same tags; {{CONTACT_EMAIL}}, {{WEBSITE_URL}}, {{POLICY_URL}}).
