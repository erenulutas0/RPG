# Reference games and the gameplay vision — 2026-09-19

The owner's direction, 2026-09-19: build Cryptforge's gameplay around what *Bones and Coins* and "BONK" do — stats and
builds, books that come out of chests, weapons that grow as the hero levels, several maps before a boss room, XP that has
to be earned by surviving swarms — and make runs longer and harder. Art stays with Astra and is not touched. This
document records what the two references actually do, maps that onto what Cryptforge already has, says what not to
copy, and redraws the roadmap. Nothing here is implemented.

**One assumption to confirm.** "BONK" is read as **Megabonk** (2025, the 3D auto-shooter roguelite with tomes, chests
and stage portals). The other candidate, *Ultra Bonk Survivors* (2026), also has books and chests but is a single-arena
survival game without staged bosses; the owner's description — several maps, a boss room at the end, swarms to farm XP
— matches Megabonk. If the owner meant the other, the mapping below changes only in the "maps" section.

## 1. What the references do

### Megabonk

- **A run is one to three stages, chosen by tier.** Tier 1 is one stage; tier 3 is Forest → Desert → a final boss
  arena. Each stage is an open map with its own ten-minute timeline: first miniboss at 3:00, swarm events at 4:00 and
  7:00, second miniboss at 8:00, and at 10:00 the normal window ends and the **Final Swarm** begins — an endless,
  escalating phase that ends the run when the player chooses or dies. The boss portal exists from the start at a random
  spot; **the player decides when the build is ready** and activates it. Killing the boss opens the next-stage portal
  if the tier has another stage.
- **Three reward channels.** Level-ups (from XP orbs) offer three choices among weapons, tomes and stat upgrades, each
  with a rarity. **Tomes** are run-long, single-stat upgrades (23 of them, four slots): Damage ×0.08 per level, XP
  +9 %, Size, Duration, Armor with diminishing returns, Agility, Luck, and so on. **Items** (86, unlimited, stackable)
  come from **chests that cost escalating gold**, from a shady dealer and from shrines. Weapons (31, four slots) level
  through chosen upgrades and some evolve.
- **A wide but shallow stat sheet** (about twenty-five stats): HP, regen, shield, armor, evasion, lifesteal, thorns;
  damage, crit chance and crit damage, attack speed, projectile count and speed; size, duration, knockback, damage to
  elites, move speed, pickup range, XP gain, gold and silver gain, luck, and a **Difficulty** stat that raises enemy
  HP, speed, damage and spawn pressure (from a Cursed Tome, from killing a boss under five minutes). Luck changes
  reward rarity only, never combat rolls.
- **Shrines and choices under pressure**: Charge (stat), Greed (harder for better loot), Bloody (extra boss),
  Challenge (a wave). Pots drop gold or XP.
- **Meta**: gold resets each run; silver persists and unlocks weapon and tome slots and shop refreshes. Twenty
  characters, 240 in-game quests, challenge modes that raise silver for penalties.
- **What reviewers hold against it**: few stages, reused enemy types, procedural layouts that vary only a little, and
  missing weapon evolutions — "repetition", inside a game they still call "a blast every run".

### Bones and Coins

- **An incremental auto-battler.** The player hovers a torch over skeleton packs while recruited heroes fight; a torch
  timer bounds each looting session; each of seven or eight named rooms has its own drop rates, skeleton cap and
  difficulty, and room XP unlocks progress.
- **Everything feeds a permanent skill tree** of 160–180 nodes split into income nodes and weapon nodes, plus a
  Blacksmith. Six heroes, five blessing slots each, party synergies ("Barbarian plus Shaman plus Ranger"), sixteen
  weapons forged from loot (copper, silver, bones). Expeditions are the challenge layer after the party is built.
- **What players hold against it** (docs/01, the reason Cryptforge exists): passive enemies, no failure state, no
  respec, no endgame. Mostly Positive, 72 % of 668.

### What the owner is asking for, restated as mechanics

| Owner's words | Reference mechanic | Meaning for Cryptforge |
|---|---|---|
| "statüler" | Megabonk's stat sheet, grown by tomes | a hero stat sheet the run can grow, not only two weapon stats |
| "sandıklardan kitaplar çıkıyordu" | tomes (Megabonk gives them at level-up; Ultra Bonk drops books in the field) | books as a **chest** reward, run-long, single-stat |
| "build yapıyordu" | items and tomes with rarity, weapon evolutions | a pool wide enough that two runs differ, and synergies |
| "level atladıkça silahlar gelişiyordu" | weapon upgrades chosen at level-up, evolutions | the existing upgrade path, plus an evolution at max stacks |
| "birden fazla bölüm / boss odasına gelmek için mapler" | stages with a timeline, boss portal at the end | more and longer rooms per floor, a boss the player reaches when ready |
| "XP kasmak için swarm'dan canlı çıkmak" | swarm events and the Final Swarm | a room type where XP is earned by surviving escalating spawns |
| "zorluk ve uzunluk" | ten-minute stages, difficulty stat, tiers | 4–6 minute floors, three or four of them, a difficulty dial |

## 2. What Cryptforge already has, and where each piece lands

The game today: two floors of six rooms each in a fixed order (combat, combat, combat, forge, elite, boss), every room a
fixed list of one-to-five-enemy waves, one chest per room (mend on even rooms, gold on odd), a level-up every few kills
offering two cards from a pool of two stat upgrades, one active ability, three weapons and two relics bought with banked
gold, a movement budget, a Descent simulation that mirrors all of it frame for frame, and local telemetry. Floor 1 is
about 43 s of fighting; a whole Descent is 2–3 minutes.

| Reference piece | Cryptforge today | Landing |
|---|---|---|
| Stage | `FloorDefinition` (six rooms) | keep; re-author rooms and add a room type |
| Timeline events, swarms | none; waves are fixed lists | a new `RoomKind.Swarm`: timed, escalating spawns from a schedule |
| Boss portal, "go when ready" | boss room is simply the last room | the swarm room ends with a checkpoint: leave early with what you have, or stay for more XP |
| Tomes / books | `UpgradeOption` is already a run-long single-stat upgrade with `MaxStacks` | **books are `UpgradeOption`s from a different source**; widen the stat set, do not build a second system |
| Level-up rarity | none; two cards, pool order | rarity = magnitude tier of the same stat, rolled from the run seed |
| Items from chests | `ChestRule` gives mend or gold | add a book reward drawn by the seed |
| Stat sheet | `WeaponStat` has Damage and AttackSpeed only | a `HeroStats` set the existing `ModifiableStat` pipeline can hold: Range, MaxHP, Regen, Armor, MoveSpeed, Bar, Refill, CritChance, CritDamage, Luck |
| Weapon evolution | none; `AttackPattern` is fixed per weapon | an evolution at max stacks (the synergy layer), the same mechanism as a behavioral upgrade |
| Difficulty dial | floor multipliers only | a run-level Difficulty stat: enemy health, damage, speed and spawn pressure |
| Silver / skill tree | Relic Forge with two relics and two weapons | later: banked gold into permanent nodes, the Bones and Coins tree |
| Failure state, risk | death, Extract/Descend with gold at risk | keep and lean on it; this is what Bones and Coins lacks |

Two design consequences fall straight out of this table:

1. **Books and level-up cards are one system.** `UpgradeOption` already is a tome. The work is the stat set behind it,
   the eligibility and stack rules, and a second *source* (chests). The seed and rarity engine serves both.
2. **The swarm room is where length and pressure come from.** This session's measurement (`26` Part 5, `23`) found
   the movement budget bites in proportion to how crowded the fight is and that authored waves of one to five enemies
   leave room to run. A swarm room is crowding by design, bounded by `PackLayout.MaxPackSize` (10 alive) and by the
   measured spawn cost (a 10-enemy entry frame is 50 ms and 18 MB).

## 3. What not to copy

- Not the 3D verticality, jumps, or open procedural maps; the arena is a diamond and the art is Astra's.
- Not thirty-one weapons and eighty-six items; Phase 2's target is 10–20 upgrades and 3–5 synergies. Reviewers' main
  complaint about Megabonk is repetition despite that width, so width is not what makes it work.
- Not unlimited item stacking and not a twenty-five-stat sheet; a stat the player cannot read on this HUD is noise.
- Not Bones and Coins' passivity. Enemies that walk in, a budget that runs out and a swarm that does not stop are the
  point.
- Not exact run replay from the seed; the finger is not seeded (`26`, the handoff prompt).

## 4. The roadmap, redrawn

Each slice keeps the standing rules: scene and simulation on one rule in the same commit, the pinned balance claims
(Temper fails floor 1, Counterweight rescues it) untouched unless a slice says it is re-pinning them, the verification
gate in order, distributions over seeds rather than one run, phone use coordinated and the profile backed up.

| # | Slice | What it proves | Depends on |
|---|---|---|---|
| S1 | **Offer engine**: run seed on `RunState` and in `run_start`, a small PRNG of our own (not `System.Random`, which differs between Mono and .NET), rarity tiers per stat, explicit eligibility and stack limits, pool of N ≥ 3, `offer_index` and rarity in telemetry | two runs with different seeds differ; the same seed gives the same offers in scene and simulation | — |
| S2 | **Stat sheet**: `HeroStats` on the `ModifiableStat` pipeline (Range, MaxHP, Regen, Armor, MoveSpeed, Bar, Refill, CritChance, CritDamage, Luck), each an `UpgradeOption` target, mirrored in the simulation | a stat card changes the fight identically in both; standing baseline re-pinned where a stat moves it | S1 |
| S3 | **Books from chests**: `ChestRule` gains a book reward drawn by the seed, a run-long option applied through the same service | a chest can now change a build; measured against mend/gold | S1, S2 |
| S4 | **Swarm room**: `RoomKind.Swarm` with a spawn schedule (waves every *n* seconds, escalating, elites at marks), a survive-for-*T* clock, an alive cap of 10, and a checkpoint to leave early | XP per minute and damage per minute across weapons and routes; the budget's effect at real crowding | — (can run beside S1–S3) |
| S5 | **Forge Pulse and evolutions**: one behavioral upgrade with the trigger contract already agreed (every *N*th strike pulses; pulse damage is not a strike; crits can trigger it but it never crits; area strikes count once; pulse kills chain nothing; relic damage is not a strike), and an evolution at max stacks per weapon using Astra's existing ring and pulse art | the first synergies; Pulse should favour the Daggers, and if it favours the Sword the design is wrong | S1, S2 |
| S6 | **Floor length and difficulty**: three or four floors re-authored around swarm rooms, a Difficulty stat, floor targets of 4–6 minutes, a Descent of 15–20 | the GDD's 3–4 minute floors, finally; distributions per seed | S4 |
| S7 | **Meta tree**: banked gold into permanent nodes, respec, the Bones and Coins layer | the endgame Bones and Coins lacks | S1–S6 |

Why this order. S1–S3 give the player a build, which is the prompt's priority and Phase 2's list. S4 gives the game
pressure and length, which is what this session found missing and what no upgrade fixes; it does not depend on S1–S3,
so it can be measured in the simulation while they are built. S5 is the synergy layer and needs the stat sheet. S6 is
the re-authoring that turns rooms into maps and needs the swarm room to exist. S7 is meta and last.

**In the three days before Astra returns**: S1 and S2, with S3 if S2 lands early, and the S4 *measurement* in the
scratch harness without touching the game. Nothing in these needs new art.

**Visual needs to hand Astra**, none of them blocking: rarity on the choice cards, a book in the chest's opening
frames, a swarm clock and a "leave" prompt on the HUD, a stat readout on the pause screen, and the evolution's
presentation per weapon.

## 5. Decisions for the owner

1. BONK is Megabonk — confirm.
2. Books come from chests and cards from level-ups, two distinct reward moments, rather than books at level-up as
   Megabonk does it. Recommended: yes, it matches the owner's description and gives the chest a reason to exist.
3. The swarm room lets the player leave early with the XP earned so far, or stay for more — the Megabonk decision.
   Recommended: yes; it is the same shape as Extract/Descend and the game already has that checkpoint.
4. Run length target: 4–6 minutes a floor, 15–20 a Descent. Recommended as the S6 target; S4's measurement will say
   whether a swarm room can carry it.

## Sources

- Megabonk on Steam: https://store.steampowered.com/app/3405340/Megabonk/
- Megabonk maps and tiers: https://megabonk.org/guides/maps/
- Megabonk stage timer: https://megabonk.org/guides/mechanics/timer/
- Megabonk stats: https://megabonk.org/guides/stats/
- Megabonk tomes: https://megabonk.org/database/tomes/
- Megabonk review (Lords of Gaming): https://lordsofgaming.net/2025/10/megabonk-review-a-bonk-tastic-3d-bullet-heaven/
- Megabonk progression and Final Swarm: https://megabonk.org/guides/progression/
- Ultra Bonk Survivors: https://www.metacritic.com/game/ultra-bonk-survivors/details/
- Bones and Coins on Steam: https://store.steampowered.com/app/4134650/Bones_and_Coins/
- Bones and Coins guides: https://bones-and-coins.wiki/guides/
