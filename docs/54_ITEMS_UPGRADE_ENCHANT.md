# 54 - Items, basma and efsun: the gold sink and the long road (design proposal, 2026-11-16)

A proposal for the lead's decision. Nothing in the game is built: no code under `Assets/`, no asset, scene or setting
changed. Two tools are added so the numbers can be checked and re-run: `Tools/CombatChecks/GearEconomyResearch.cs` (the
Descent's income, measured in the simulation) and `Tools/Economy/gear_economy.py` (the economy model, with every knob of
this document as a constant). Numbers cited as "measured" come from the first; numbers cited as "modelled" from the
second, which reads the first's output (`Tools/Economy/income-2026-11-16.csv`). Every other number is a starting value.

**Owner decisions (2026-11-16, fixed).** Players grind gold in runs and spend it on **items**, on **basma** (Metin2-style
+0..+9 upgrades) and on **efsun** (Metin2-style random bonus lines, rerolled). A failed basma **never breaks the item**
("kırılmasın") and should not be too punishing, but high basma must be **hard**. Upgrade **boosters** drop in the game.

**The lead's frame** (refined here with numbers; where this document argues against it, it says so): 3-4 equipment slots
beside the hero's weapon choice; items drop in runs with the existing four rarities; the lead's success table with a
per-item pity; floor materials that tie high basma to depth; three boosters; efsun with 1-5 lines, reroll all for gold,
add-a-line rare; a power cap of about +40-50 % at full +9 with good efsun, and difficulty tiers as where that power goes.
Active skills and an async arena are outlined only.

## 0. The short version

| Piece | Proposal | Key numbers |
|---|---|---|
| Slots | **Weapon** (the forged weapon itself: Sword, Staff, Daggers), **Armour**, **Amulet**, **Ring**. Only the last three drop | 4 upgrade targets; relics stay as they are |
| Items | Drop from bosses, elites and the finale's walk-out; rarity by `RarityTable` (Common/Rare/Epic/Gold) | careful: **1.5 items a run** (modelled); a Gold item ~1 in 22 drops |
| Basma | +1..+3 100 %, +4 90, +5 75, +6 60, +7 45, +8 30, +9 20; a failure keeps the level and the item, spends the gold and materials, and adds **pity** (+5/+5/+5/+4/+3/+2 points) until a hard **ceiling** (the next strike holds after 2/3/4/6/8/10 failures in a row) | mean attempts +9: **3.96**, worst case 11 |
| Cost | Gold 50 / 80 / 120 / 180 / 250 / 400 / 700 / 1,300 / 2,400 an attempt; materials from +4: Ember Shard (Foundry) to +6, Quicksilver Bead (Vaults) for +7, Gloam Spore (Gardens) for +8 and +9 | one item to +9: **16,100 gold**, 12.5 Ember, 6.1 Quicksilver, 35 Spores (mean) |
| Boosters | **Forge Seal** (+10 points on one strike), **Master's Hammer** (one strike up to +6 cannot fail), **Efsun Scroll** (one reroll without gold), **Binding Rune** (adds a line) | careful a run: 0.43 Seals, 0.12 Hammers, 0.97 Scrolls, 0.09 Runes |
| Efsun | Lines by rarity 1/2/3/4, a fifth by Binding Rune; 14-line pool fitted to this game; tiers 1-5; reroll all, then **keep old or new** | reroll 60/100/160/250 gold by rarity; a "good" 4-line roll 1 in 30 |
| Power cap | Main stats by basma plus lines, clamped per stat in code; **gear power = DPS x EHP - 1** | +5 set **+15 %**, +9 set **+31 %**, +9 with good efsun **+42 %**, absolute ceiling **+51 %** |
| Curve | careful cohort, boosters, a quarter of gold to efsun | first +5 in **2 runs**, one +7 in 7, one +9 in **32**, full set +9 in **135 runs (P50; ~5 weeks at 4 runs a day)** |
| Tiers | Forged (today) - Tempered - Inferno - Abyssal, each matched to a gear power, each paying more | rewards x1 / 1.25 / 1.5 / 2 |
| Save | `ProfileSaveData` version 4 (items, weapon gear, materials, boosters, anvil stream, tier); migration 3 -> 4 adds empties | gold untouched |
| Balance | Every shipped balance test plays ungeared (`GearLoadout.None`), so the measured Descent is unchanged; new **geared cohorts** measure the rest | |

## 1. Why: today's economy

**What a run pays (measured).** `GearEconomyResearch` plays every cohort and both heroes down the shipped Descent (floors
1-3 and the finale), seeds 1-100 a hero, typical human hands (2026-11-16, this branch at main `0243c55d`). The simulation
keeps no gold of its own, so the research rebuilds it from what it does keep: each node-wave kill's enemy at its base
gold times the floor's modifier (`FloorScaling.Gold`; floor 2's Cursed Gold +50 %), nothing for a horde kill
(`EncounterController`: a horde enemy pays no gold), half the chests opened (`ChestRule`: odd rooms pay 10), and a
death loses half of the dying floor's gold (`RunState`'s `atRiskGoldLoss` 0.5, secured at each Descend).

| Cohort | Banked P25 / P50 / P75 (Vanguard) | Banked mean, both heroes | Completed (V / I) | Gold by floor 1 / 2 / 3 / finale | Elites felled | Bosses felled | Minutes P50 |
|---|---|---|---|---|---|---|---|
| careful | 500 / 507 / 515 | 497 | 97 % / 96 % | 180 / 159 / 106 / 61 | 3.9 | 2.9 | 14.4 |
| standing | 506 / 512 / 519 | 485 | 94 % / 85 % | 180 / 160 / 106 / 62 | 3.9 | 2.8 | 13.9 |
| first-card | 500 / 510 / 517 | 472 | 85 % / 90 % | 174 / 159 / 104 / 64 | 3.8 | 2.7 | 14.1 |
| clumsy | 21 / 62 / 350 | 224 | 22 % / 43 % | 128 / 138 / 100 / 61 | 2.2 | 1.2 | 7.2 (mean) |
| auto-farmer | 19 / 62 / 65 | 83 | 4 % / 4 % | 110 / 121 / 89 / 68 | 1.6 | 0.2 | 3.8 |

The brief's "roughly 400-600 gold a run" is right for a run that completes: **about 500**, very tight (P25-P75 within
±10 gold), because the gold is the authored waves' and bosses' (the horde pays none). A run that dies on floor 1 banks
60-70.

**What there is to spend it on.** The Relic Forge sells two relics (80, 150) and two weapons (Daggers 180, Staff 120):
**530 gold in all, about one completed run.** After that, gold accumulates with nothing to buy; the owner's profile holds
39,491 (about 80 completed runs). There is no long-term progression past the depth record and two hero unlocks.

**What the sink has to do.**
1. Turn every run's gold into something felt, for weeks, without breaking the measured Descent (the 14-minute run, the
   lead's relative cohort rule, the bosses' 45-65 s bands).
2. Pay the weak cohorts most where they need it: the clumsy and auto-farmer cohorts bank 224 and 83 a run; gear is their
   ladder, so the first rungs must be cheap.
3. Tie the long tail to depth, so that banked gold alone cannot buy +9 (the owner's 39,491 should not finish the game).

## 2. What the references do (research, 2026-11-16)

| Game | Upgrade and odds | Failure | Pity / protection | Bonus lines | Lesson for Cryptforge |
|---|---|---|---|---|---|
| **Metin2** | +0..+9 at the blacksmith for yang and materials; per-server rates (`refine_proto`), falling with the level | the plain blacksmith **destroys the item**; a Blessing Scroll keeps it and **drops it one level** instead | scrolls and the guild/tower smiths' +10 % chance | 5 bonuses (four by reinforce item, the fifth by blessing marble) plus a separate 6th-7th segment; the switcher rerolls the segment's values, an RNG grind; a bonus cannot repeat the item's own | the +N fantasy and the reroll grind are the pull; the destruction is exactly what the owner ruled out |
| **Lost Ark** | honing, the success rate falls with the level | materials and gold spent, level kept | each failure raises the next attempt's chance; **Artisan's Energy** fills on failures and at 100 % the next hone is guaranteed | - | the cleanest "no too-punishing" design: a soft pity and a hard ceiling. Our ceiling is its energy bar |
| **Diablo Immortal** | Legendary Gem ranks 1-10 by duplicates and Gem Power; 5-star gems from Legendary Crests | - | 50 crests guarantee a 5-star gem | - | the pity there sits behind real money and the community called it pay-to-win; ours sits behind play only |
| **Archero / Archero 2** | gear levelled by coins plus slot-specific scrolls that drop from enemies; rarity raised by fusing three equal items | none | none needed | - | gold plus a dropped material per slot is the mobile norm; fusion returns the coins and scrolls spent, so an upgrade is never wasted |
| **Vampire Survivors** | PowerUps: each purchase raises the next price (10 % of base); refund returns everything | - | - | - | a meta shop with escalating prices is a gold sink with a visible ceiling; a full refund keeps experiments cheap |
| **Megabonk** (docs/40, 2.7) | silver persists and unlocks; gold resets every run | - | - | - | permanent power is small and bounded; the run's build is the game |

Sources at the end. The Metin2 wiki does not publish per-level rates (they are server data); the lead's table is in the
range players quote and is used as given.

Three lessons became rules here:
- **No destruction, no downgrade** (the owner; Metin2's Blessing Scroll still drops a level, ours does not).
- **Soft pity plus a hard ceiling** (Lost Ark): the soft pity makes a failure feel like progress; the ceiling bounds the
  worst case, which is what "not too punishing" means for a player who meets it.
- **The meta power is bounded and visible** (Megabonk, Vampire Survivors): a power index with per-stat caps in code, so
  the Descent stays the game and new content cannot creep past the ceiling.

## 3. Items

### 3.1 Slots: four targets, three that drop (argues with the lead's frame)

The lead's frame names weapon, armour, amulet, ring "on top of the hero's weapon choice". A dropped weapon item beside a
chosen weapon would be two weapons with one name: which one strikes? **Proposal: the weapon slot is the forged weapon
itself.** The Sword, Staff and Daggers (each owned weapon, the starting weapons included) carry their own basma level
and efsun lines; the Forge's weapon rack is where a weapon is bought, the Anvil is where it is upgraded. The other three
slots drop.

| Slot | Source | Main stat by basma (+0 -> +9) | Efsun lines |
|---|---|---|---|
| **Weapon** (per weapon id) | bought at the Forge (as today); the starting weapon is free | damage +0 -> **+10 %** (hero-wide `Damage`, so a second weapon picked in the run benefits too) | 2 to start (a forged weapon counts as Rare); Binding Runes add up to 5 |
| **Armour** | drops | max health +0 -> **+10 %** | by rarity |
| **Amulet** | drops | attack speed +0 -> **+5 %** | by rarity |
| **Ring** | drops | crit chance +0 -> **+3 points** | by rarity |

Armour, amulet and ring are **shared by every hero**; the weapon's upgrades belong to the weapon. Ilya always carries
the Staff (`WeaponShop.OnlyStartingWeapon`), so a player who plays both heroes upgrades the Sword (or Daggers) and the
Staff: five targets, the cost of choosing two heroes (question 3). The relic slot is unchanged: relics stay run-shaping
rules, not stat sticks.

### 3.2 Drops and rarity

| Source | Item chance | Measured careful frequency | Items a careful run |
|---|---|---|---|
| Floor boss (Warden, Regent, Warden) | 25 % | 2.9 a run | 0.73 |
| Elite leader (two on floor 1, one each on floors 2 and 3) | 5 % | 3.9 a run | 0.20 |
| The finale's walk-out | 60 % | 0.96 a run | 0.58 |
| **The first floor-1 boss a profile fells** | **100 %, an Armour** | once | the tutorial drop |

Modelled: careful 1.49, clumsy 0.58, auto-farmer 0.13 items a run. The slot is drawn evenly from the three; the rarity
from `RarityTable` (60 / 25 / 12 / 3 % at no luck; a difficulty tier adds luck, section 8). A Gold item is about 1 drop
in 33, one in 22 careful runs; each slot sees its first Gold item in about 65 careful runs, which is why basma can move
between items (3.4).

Item identity is small: a slot, a rarity, a name from a short list per slot and floor ("Foundry Mail", "Quicksilver
Locket", "Gloam Signet"; art and names are visual work, not this document's), a basma level, a pity count and its lines.
No item levels, no set bonuses, no item-specific effects: the run's cards and relics already carry the build's identity.

### 3.3 The bag, salvage

The bag holds **30 items**. An item can be **melted** at the Anvil for gold and Ember Shards (Common 20 + 1, Rare 40 + 2,
Epic 80 + 3, Gold 150 + 4): every drop is worth something even when it is not kept, and the melt is the bag's pressure
valve. A run's drops above a full bag wait in a 10-place overflow shown on the result screen, then melt automatically
(stated on the screen).

### 3.4 Inheritance: the level moves with the player

Archero returns the coins spent when items are fused; Metin2 does not, and a +7 Common ring that cannot become a Gold
ring is a reason to stop playing. **Inheritance**: the Anvil moves an item's basma level onto another item of the same
slot, for **10 % of the gold that level cost** (the expected cost, from the table in 4.1) and the source item, which is
consumed. The pity does not move; the lines do not move (the new item keeps its own). The weapon slot has no inheritance
(a weapon is not replaced, it is chosen).

## 4. Basma

### 4.1 The table

An attempt from +L-1 to +L costs gold and, from +4, a floor material; it succeeds with the shown chance. **A failure
keeps the item and its level** and spends what the attempt cost. Each failure adds the level's pity to the next attempt
on this item, until a success resets it; after the ceiling's count of failures in a row, the next attempt cannot fail.
Modelled with `expected_attempts` (exact, not sampled):

| Level | Base | Pity a failure | Ceiling (failures) | Attempts, mean | Worst case | Gold an attempt | Material an attempt | Gold from +0 (mean) | Materials from +0 (mean) |
|---|---|---|---|---|---|---|---|---|---|
| +1 | 100 % | - | - | 1.00 | 1 | 50 | - | 50 | - |
| +2 | 100 % | - | - | 1.00 | 1 | 80 | - | 130 | - |
| +3 | 100 % | - | - | 1.00 | 1 | 120 | - | 250 | - |
| +4 | 90 % | +5 | 2 | 1.10 | 3 | 180 | 2 Ember | 449 | Ember 2.2 |
| +5 | 75 % | +5 | 3 | 1.31 | 4 | 250 | 3 Ember | 776 | Ember 6.1 |
| +6 | 60 % | +5 | 4 | 1.59 | 5 | 400 | 4 Ember | 1,413 | Ember 12.5 |
| +7 | 45 % | +4 | 6 | 2.05 | 7 | 700 | 3 Quicksilver | 2,847 | + Quicksilver 6.1 |
| +8 | 30 % | +3 | 8 | 2.86 | 9 | 1,300 | 4 Spore | 6,568 | + Spore 11.4 |
| +9 | 20 % | +2 | 10 | 3.96 | 11 | 2,400 | 6 Spore | **16,073** | + Spore 35.2 |

Without pity or ceiling the same item to +9 would cost 19,339 gold. The chance the ceiling is ever reached is small (3.2 %
of +9 climbs, 1.5 % of +8, under 1.1 % below), so the ceiling is a promise, not a discount.

**Why +9 is hard.** One in five strikes holds at first; the mean +9 climb is four strikes, 9,500 gold and 24 Spores,
about **19 careful runs of gold for the last level alone**. "Hard" here is cost, not loss: the owner's rule takes loss
off the table, so the hardness has to be in the price and the materials. The lead's odds are kept as given.

**Why the pity is uneven (argues with "a few points").** A flat +5 would give +9 a 20 -> 25 -> 30 ... climb and cut its
mean to 3.3 strikes; +2 at +9 keeps the top hard, while +5 at +4..+6 makes the middle forgiving where new players live.

### 4.2 Floor materials: depth gates the top

| Material | Floor | Drops (Forged tier) | Careful a run (modelled) | Used for |
|---|---|---|---|---|
| **Ember Shard** | the Foundry (floor 1) | 2 for clearing the floor, 1 an elite | 3.95 | +4, +5, +6 |
| **Quicksilver Bead** | the Quicksilver Vaults (2) | 2 a clear, 1 an elite | 2.89 | +7 |
| **Gloam Spore** | the Gloam Gardens (3) | 2 a clear, 1 an elite | 2.89 | +8, +9 |

A player who never clears floor 1 can still reach +6 from the Foundry's elites and melted items (clumsy: 2.1 Ember a
run); +7 needs the Vaults, +9 the Gardens. That is the map progression the lead asked for, and it is what keeps banked
gold from buying the top: **the owner's 39,491 gold buys four items to +3 at once and then waits on Ember Shards** (12.5 an
item for +4..+6, three careful runs an item).

Map starts (docs/53) change nothing here: a run started on the Vaults earns Quicksilver and Spores but no Ember.

### 4.3 The roll is the profile's, not the moment's

Every attempt draws from a profile-owned stream: `anvilSeed` (set once at the profile's creation) and `anvilDraws` (the
number of draws so far), the same pattern as `RunRandom`. The spend, the roll and the result are one profile change and
one save. Killing the app before the save loses nothing and gains nothing: the next attempt draws the same number. The
chance shown on the button is the chance used, with the base, the pity and a Seal listed apart.

## 5. Boosters

All four are run loot: earned in the run, carried in the run's satchel, banked like gold (secured at each Descend; on a
death, each booster dropped on the dying floor is kept or lost on a seeded coin, the gold's half in expectation).

| Booster | Effect | Drops (Forged tier) | Careful a run | Clumsy a run | How the model spends it |
|---|---|---|---|---|---|
| **Forge Seal** | +10 points on one strike (stacks with pity; one a strike) | 8 % an elite, 4 % a boss | **0.43** (one in 2.3 runs) | 0.20 | on +9 strikes, and on +8 while three are kept for the +9s |
| **Master's Hammer** | one strike up to +6 cannot fail | 4 % a boss | **0.12** (1 in 8.5 runs) | 0.05 | on the +6 strike |
| **Efsun Scroll** | one efsun reroll without gold | 25 % a floor cleared (finale included) | **0.97** | 0.37 | rerolls before gold |
| **Binding Rune** | adds a line to an item with fewer than five | 2 % a boss, 3 % the finale's walk-out | **0.09** (1 in 11 runs) | 0.03 | on the equipped Gold items |

The lead's frame put Seals on elites and chests; chests here already carry mend, gold and books (docs/28), and a fourth
thing in a chest dilutes the book's moment, so Seals come from elites and bosses only (question 6). The Hammer is
capped at +6 as the lead framed it: on the +6 strike it saves 0.59 strikes on average, about 240 gold and two Ember
Shards, so it is a small gift, not a skip; a Hammer that worked at +9 would save about 7,100 gold and 18 Spores and would
decide the economy by luck.

Modelled effect (seed 54; the sensitivities at 800 players): with boosters the careful cohort's full set reaches +9 in
102 runs instead of 128 (P50), one +9 item in 24 runs instead of 30; boosters shorten the road by about a fifth and do not
replace it. Almost all of it is the Seal: without Seals the set takes 167 runs instead of 135 (a quarter of gold to
efsun), without Hammers 135 as with them. **The Seal's rate is not its knob, its bonus is**: a careful player earns about
58 Seals over the road against about 25 strikes at +8 and +9, so doubling the rate changes nothing (135 either way); below
about 0.2 a run the supply starts to bind. The lead's frame had Seals from elites and chests at a higher rate; at one a
run the Seal is simply +10 on every top strike, and a booster that is always there is a table change, not a drop.

## 6. Efsun

### 6.1 Lines

An item has as many lines as its rarity: **Common 1, Rare 2, Epic 3, Gold 4**; a forged weapon starts with 2. A Binding
Rune adds one, up to **5** (Metin2's first segment; the 6th-7th segment is not copied). A line is a stat from the pool
and a tier 1-5, drawn with weights 40 / 30 / 18 / 9 / 3; no stat twice on one item, and no line repeats the item's main
stat (Metin2's rule).

| Line | Tier 1 -> 5 | Game rule it rides | Cap (whole gear, main stats included) | Kind |
|---|---|---|---|---|
| Damage | +1 -> +3 % | `UpgradeStat.Damage` | 14 % | power |
| Attack speed | +0.5 -> +2.5 % | `AttackSpeed` | 7 % | power |
| Crit chance | +0.4 -> +1.2 points | `CritChance` | 4 points | power |
| Max health | +1 -> +3 % | `MaxHealth` | 14 % | power |
| Armour (damage taken) | -0.4 -> -1.2 points | `Armor` | 4 points | power |
| Damage vs elites and bosses | +2 -> +6 % | new: `EliteDamage` (`EliteRule`'s targets) | 12 % | power (situational) |
| Burn damage | +3 -> +9 % | new: `BurnDamage` (`BurnRule`) | 15 % | power (Staff) |
| Area | +1 -> +3 % | `Area` (the Reach tome's stat) | 5 % | power (weak) |
| Move speed | +1 -> +3 % | `MoveSpeed` | 6 % | utility |
| Pickup radius | +4 -> +12 % | new: `PickupRadius` (`GroundDrops.PickupRadius`) | 25 % | utility |
| Ability cooldown | -2 -> -6 % | new: `AbilityCooldown` (`AbilityRuntime`) | 10 % | utility |
| Healing received | +3 -> +9 % | new: `Healing` (chest mend, forge heals) | 15 % | utility |
| Luck | +1 -> +5 | `Luck` (`RarityTable`, card rarity only) | 10 | utility |
| Gold find | +2 -> +6 % | new: `GoldFind` (the run's gold, never materials) | 10 % | economy |

New stats are appended to `UpgradeStat` (append, never reorder, as `UpgradeRarity` does). **Excluded on purpose:**
experience gain (it moves the level pacing the Descent is tuned on, docs/41), difficulty, and any proc stat (pulse,
splinter, kill burst): those are the run's cards' identity.

### 6.2 Rerolling

"Efsun" rerolls **all** of an item's lines at once, for gold by rarity, **60 / 100 / 160 / 250** (Common to Gold), or one
Efsun Scroll. The price is the rarity's, not the basma level's: a price that grew with the level would teach every
player to efsun at +0 first.

**Keep old or new (a deviation from Metin2).** The new lines are shown beside the old; the player keeps one set. Metin2
replaces at once, and a good roll lost to a bad one is the moment players quit. With a choice, a reroll can only help,
so the sink is the price, not the loss; the owner's "not too punishing" applies here as much as to basma (question 5).

**How long "good" takes (modelled, `efsun_rerolls`).** "Good" = at least two of damage, crit, attack speed, health at
tier 3 or better:

| Lines | Rarity (gold a reroll) | Good a reroll | Rerolls to good | Gold to good, no scrolls |
|---|---|---|---|---|
| 2 | Rare (100) | 0.6 % | 162 | 16,234 |
| 3 | Epic (160) | 1.8 % | 57 | 9,156 |
| 4 | Gold (250) | 3.3 % | 30 | 7,484 |
| 5 | Gold + Rune (250) | 5.4 % | 19 | 4,654 |

The perfect roll (five power lines at tier 5) is out of reach by design (the five power stats among 14, each at tier 5: about 1 in 10^11); "good" is a few weeks' side
goal per item, paid with scrolls (about one a careful run) and gold. A line lock is not proposed (question 5).

## 7. The power cap

### 7.1 The index

Gear power is one number, computed the same way in the game, the simulation and the model (`gear_power`):

    DPS = (1 + damage) (1 + attack speed) (1 + crit x (crit multiplier - 1))      crit multiplier 2
    EHP = (1 + max health) / (1 - armour)
    gear power = DPS x EHP - 1

Each stat's total over the whole gear (main stats and lines) is **clamped** at its cap (6.1's table) before it reaches
`HeroStats`, in one non-inlined body shared by the scene and the simulation (`GearStats.Apply`, the stamina lesson of
docs/21: one body, or the parity breaks). Situational and utility lines are capped but not counted in the index; the
damage-vs-elites line is the one to watch at the bosses (section 9).

### 7.2 What a set is worth (modelled)

| Set | Damage | Attack speed | Crit | Health | Armour | DPS | EHP | **Gear power** |
|---|---|---|---|---|---|---|---|---|
| All four at +5, no lines | 5 | 2.5 | 1.5 | 5 | 0 | +9 % | +5 % | **+15 %** |
| All four at +7, no lines | 7 | 3.5 | 2.1 | 7 | 0 | +13 % | +7 % | **+21 %** |
| All four at +9, no lines | 10 | 5 | 3 | 10 | 0 | +19 % | +10 % | **+31 %** |
| +9, a "good" 4-line efsun on each (damage, crit, health, move speed at tier 3) | 14 (cap) | 5 | 4 (cap) | 14 (cap) | 0 | +24 % | +14 % | **+42 %** |
| +9, five perfect power lines on each | 14 | 7 | 4 | 14 | 4 | +27 % | +19 % | **+51 %** (the ceiling) |

The lead's "+40-50 %" is met: good efsun lands at +42 %, and nothing reaches past +51 % because the caps bind (a perfect
set wastes most of its lines on caps; that is the point of caps). The split, a little more DPS than EHP, keeps the
bosses' fights the place gear shows: a +9 set kills the Warden about a fifth faster.

## 8. Difficulty tiers: where the power goes

Gear that only makes the shipped Descent easier is a sink that empties the game. Tiers give the power somewhere to go,
and each tier is **matched to a gear power** so that a player geared for it meets the Descent the lead measured:

| Tier | Unlock | Enemy health / damage | Matched gear power (h x d - 1) | Gold, materials, boosters | Item luck (`RarityTable`) |
|---|---|---|---|---|---|
| **Forged** (today) | - | x1 / x1 | 0 | x1 | 0 |
| **Tempered** | complete the Descent on Forged | x1.08 / x1.06 | ~+15 % (a +5 set) | x1.25 | +10 |
| **Inferno** | complete Tempered | x1.15 / x1.13 | ~+30 % (+9, or +7 with lines) | x1.5 | +20 |
| **Abyssal** | complete Inferno | x1.20 / x1.18 | ~+42 % (+9 with good efsun) | x2 | +30 |

The names are docs/40's (section 10.1); its multipliers were skill tiers (Abyssal 1.8 / 1.5), these are gear tiers, and
the two can stack later. The multipliers ride `FloorScaling` (the modifier path floors already use) on node waves,
the horde and the bosses alike; the bosses' 45-65 s bands are measured per tier with the matched geared cohort (section
13). Map starts (docs/53) are per tier: a deeper start on a tier opens once that tier reached it.

## 9. The economy model: runs to each milestone

`python Tools/Economy/gear_economy.py --players 2000` (2,000 simulated players for careful and clumsy, 500 for the others;
seed 54). Each player plays runs drawn from its cohort's 200 measured runs, banks their loot under the gold's rule, and
works four items one at a time to +9 (the "one item first" habit), spending after every run. It does not feed gear back
into income, which makes the weak cohorts' rows pessimistic (gear raises their clears; section 13 measures it).

**With boosters, a quarter of every run's gold set aside for efsun** (the expected habit), runs P25 / P50 / P90:

| Cohort | First +5 | One +6 | One +7 | One +8 | One +9 | Set +7 | **Set +9** |
|---|---|---|---|---|---|---|---|
| careful | 2 / **2** / 3 | 3 / 4 / 5 | 6 / **7** / 12 | 12 / 15 / 24 | 24 / **32** / 56 | 92 / 108 / 145 | 117 / **135** / 178 |
| standing | 2 / 3 / 4 | 3 / 4 / 6 | 6 / 7 / 12 | 12 / 15 / 24 | 25 / 33 / 57 | 96 / 109 / 151 | 121 / 139 / 181 |
| first-card | 2 / 2 / 4 | 3 / 4 / 6 | 6 / 8 / 12 | 12 / 16 / 26 | 24 / 33 / 61 | 98 / 114 / 156 | 124 / 143 / 189 |
| clumsy | 4 / **5** / 9 | 6 / 8 / 13 | 12 / 16 / 26 | 25 / 33 / 56 | 52 / 71 / 122 | 200 / 234 / 322 | 253 / 294 / 391 |
| auto-farmer | 10 / 13 / 20 | 17 / 22 / 35 | 33 / 42 / 84 | 72 / 98 / 175 | 156 / 212 / 425 | 570 / 687 / 1,027 | 746 / 914 / 1,289 |

The other three variants (P50, careful / clumsy):

| Variant | First +5 | One +7 | One +9 | Set +9 |
|---|---|---|---|---|
| No boosters, all gold to basma | 2 / 4 | 6 / 13 | 30 / 67 | 128 / 282 |
| No boosters, a quarter to efsun | 2 / 5 | 7 / 17 | 40 / 89 | 171 / 381 |
| Boosters, all gold to basma | 2 / 4 | 6 / 13 | 24 / 54 | 102 / 225 |
| **Boosters, a quarter to efsun** | **2 / 5** | **7 / 16** | **32 / 71** | **135 / 294** |

**Against the targets.** First +5 within a few runs: **2 runs** for every competent cohort, 5 for clumsy. A full +9 set
over weeks: **135 careful runs**, about 32 hours of play; at 4 runs a day (an hour) that is **5 weeks** (P90 178 runs,
6 weeks), at 8 runs a day under 3 weeks. Clumsy takes about 10 weeks at 4 a day before gear lifts its clears. The auto-farmer's
row is the honest cost of a pilot that dies on floor 1 nineteen runs in twenty: it is why the first rungs are cheap, and
why section 13 must measure what +5 does for it.

**The knobs**, all constants at the top of `gear_economy.py`: `GOLD` (the strongest; the +8 and +9 prices are two thirds
of an item's cost), `BASE_CHANCE`, `PITY_STEP`, `PITY_CEILING`, `MATERIAL` (which floor gates which level),
`MAT_PER_CLEAR` / `MAT_PER_ELITE`, the booster rates, `ITEM_PER_*`, `REROLL_GOLD`, `TIER_WEIGHTS`, the main-stat curves
and `CAPS`. What moves the careful set's P50 (135 runs; boosters, a quarter to efsun; 800 players a variant):

| Change | One +9 | Set +9 |
|---|---|---|
| none | 32 | 135 |
| `GOLD` +25 % at +8 and +9 | 38 | 163 |
| `GOLD` -20 % at every level | 25 | 107 |
| no Forge Seals | 41 | 167 |
| Forge Seals at twice the rate | 33 | 136 |
| no Master's Hammers | 32 | 135 |
| Gloam Spores from elites only (1 a run instead of 3) | 32 | 136 |

**Gold sets the pace; depth is a gate, not a pace.** The materials never bind for a player who reaches floor 3 (careful
earns about 2.9 Spores a run against 27-35 an item, the fewer with Seals), so the Spore numbers are free to stay generous; for a player who never
clears floor 2 they are a wall at +7 and +8. That is the lead's "ties upgrades to map progression", read as a gate.

## 10. The save: version 4

`ProfileSaveData.CurrentVersion` 3 -> **4**; JSON keys stay camelCase and stable (06_TECH_ARCHITECTURE_UNITY);
`JsonUtility` serializes arrays of `[Serializable]` classes, so no dictionaries:

    public ItemSaveData[] items;          // the bag and the equipped items
    public WeaponGearSaveData[] weaponGear; // basma and lines per weapon id (owned or starting)
    public int[] equippedItemUids;        // armour, amulet, ring; 0 for an empty slot
    public int nextItemUid;               // uids start at 1
    public int emberShards, quicksilverBeads, gloamSpores;
    public int forgeSeals, mastersHammers, efsunScrolls, bindingRunes;
    public int anvilSeed;                 // set once when the profile is created (or migrated)
    public int anvilDraws;                // draws taken from the anvil's stream
    public int highestTierCompleted;      // 0 none, 1 Forged, ...
    public int selectedTier;              // 0 Forged

    [Serializable] class ItemSaveData { int uid; string itemId; int slot; int rarity; int level; int pity; LineSaveData[] lines; }
    [Serializable] class WeaponGearSaveData { string weaponId; int level; int pity; LineSaveData[] lines; }
    [Serializable] class LineSaveData { string stat; int tier; }   // the value is the table's, so a retune moves it

**Migration** (`ProfileMigration`, case 3): empty arrays, zero counts, `nextItemUid` 1, `anvilSeed` drawn once from the
revision and the clock, tiers 0; `saveVersion` 4. **Gold is untouched**, the owner's 39,491 included: materials gate it
(4.2), so no conversion is needed (question 7).

**Repair on load**, as `PlayerProfile` does today: levels clamped to 0..9, pity to 0..ceiling, lines past the rarity's
count (plus runes) dropped, unknown stats dropped, duplicate uids renumbered, equipped uids that are not in the bag or
are in the wrong slot unequipped, negative counts zeroed. A line stores its tier, never its value, so retuning a tier's
value moves every saved item with it.

**Run loot is not saved mid-run** beyond what the checkpoint secures: `RunBank` deposits secured materials and boosters
at a Descend as it does secured gold, so the app closing mid-floor costs what it costs today.

## 11. The UI on the phone

The Relic Forge panel (`RelicForgeView`, opened from the result screen) gains a **tab row** under its title: **Forge** (the
weapons and relics, unchanged), **Anvil**, **Bag**. Portrait, the 1080-wide canvas, thumb-reachable at the bottom.

**Anvil tab.**
- Top: the four slots as a row of cards (Weapon, Armour, Amulet, Ring), each with its icon, its rarity ground (docs/41:
  Common grey, Rare blue, Epic red, Gold gold) and a large "+7" badge. Tapping one selects it.
- Middle: the selected item's card, larger: name, main stat "now -> next" ("Damage +7 % -> +8.5 %"), its lines.
- The strike panel: the chance in large type, built up underneath ("Base 30 % + Anvil's memory 6 % + Seal 10 %"), the
  **Anvil's memory** bar (failures toward the ceiling, "holds in 5"), the cost with what is owned ("1,300 gold · 4 / 9
  Gloam Spores"), and toggles for a Forge Seal and a Master's Hammer when owned and allowed.
- **Strike**, wide, at the bottom; a hold of 0.4 s (a ring fills) so a stray tap never spends 2,400 gold.
- The result over the card for 1.2 s: success - sparks, the badge rolls up, a chime; failure - "The metal held." in
  grey, the memory bar fills a notch, no break, no downgrade, said every time until the player has seen it ten times.
- Below the slots, two small buttons: **Efsun** (opens the reroll sheet) and **Inherit** (when the bag holds a same-slot
  item).

**Efsun sheet.** The item's lines on the left, empty slots beside them on the right; **Reroll (250 gold)** or **Use a
scroll (3 left)**; the new lines fill the right; **Keep new** or **Keep old**. A line at or over its cap shows "capped"
in grey, so a player sees why a fourth damage line does nothing.

**Bag tab.** A grid of 30, filterable by slot; tap an item for equip / compare (the gear power before and after) /
melt. The result screen lists the run's loot (gold, materials, boosters, items) with what was lost on a death struck
through, as gold is today ("Gold 507 banked, 0 lost").

The gear power shows in one place: under the hero on the map choice (docs/53), "Gear +31 %", beside the tier picker.

## 12. Telemetry

Additions to the local telemetry (`RunTelemetry`); purchases happen between runs, so the Anvil's lines carry no run id,
as `currency_spent` does today.

| Event | Fields | Answers |
|---|---|---|
| `run_start` (+) | `tier`, `gear_power`, `gear_levels` (four ints) | who plays which tier with what |
| `run_end` (+) | `tier`, `gear_power`; loot banked and lost by kind | income by tier and cohort, the death's cost |
| `loot_dropped` | `kind` (item/material/booster), `id`, `rarity`, `source` (boss/elite/finale/salvage), `floor_index` | drop rates against section 3.2 and 5 |
| `currency_earned` (+) | currencies `ember_shard`, `quicksilver_bead`, `gloam_spore`, source `run_bank` / `salvage` | material income |
| `anvil_attempt` | `item_uid`, `slot`, `item_id`, `rarity`, `from_level`, `base`, `pity_bonus`, `seal`, `hammer`, `chance`, `success`, `gold`, `material`, `material_count`, `pity_after`, `ceiling_hit` | the curve: attempts a level, ceiling hits, where players stop |
| `efsun_reroll` | `item_uid`, `paid` (gold/scroll), `cost`, `lines_before`, `lines_after`, `kept` (old/new) | how often new wins, the reroll sink |
| `efsun_line_added` | `item_uid`, `stat`, `tier` | rune use |
| `item_equipped`, `item_melted`, `item_inherited` | uid, slot, rarity, level, what was paid or returned | bag behaviour |
| `currency_spent` (+) | sinks `anvil`, `efsun`, `inherit` | the existing gold report keeps working |

The first device review reads: runs to first +5 and +7 against section 9 (the model says 2 and 7 for a careful player),
the share of +9 attempts that reach the ceiling (3.2 % modelled), `kept=new` share, and gold banked minus spent a week.

## 13. The simulation and the balance tests

**One body.** `GearStats.Apply(HeroStats, GearLoadout)` (pure, `[MethodImpl(NoInlining)]`, in `Progression`) turns a
loadout into clamped modifiers. The scene calls it at run start; `DescentSimulation.FoundryHero` gains a `Gear`
(`GearLoadout.None` by default) and calls the same body.

**Nothing measured moves.** Every shipped balance test, `FoundryGate`, the relative cohort rule's batches and the
bosses' bands play with `GearLoadout.None` on Forged. They are the regression line: gear must not change them, and no
test needs a new expected value.

**Geared cohorts.** A cohort gains a gear level: `careful+5`, `careful+7`, `careful+9g` (+9 with section 7.2's good
lines), `clumsy+5`, `auto-farmer+5`. The research batch (seeds 1-200, both heroes) measures:

| Check | Target |
|---|---|
| Geared on Forged | completion never below the ungeared cohort's; Warden fight P50 at +9g not under **30 s** (gear shows, the boss is still a fight) |
| `clumsy+5` and `auto-farmer+5` on Forged | completion up by a measurable step (the weak cohorts' ladder works); report it |
| Each tier with its matched gear (Tempered/`careful+5`, Inferno/`careful+9`, Abyssal/`careful+9g`, and clumsy likewise) | inside the lead's relative rule against ungeared Forged: at most 1 completion in 800 lost for clumsy, careful within 3 floor-3 clears; the bosses in their 45-65 s bands |
| `GearEconomyResearch` re-run per tier and gear level | gold and loot a run, fed back into `gear_economy.py` (closes the model's gear-to-income gap) |
| A 14-minute Descent | unchanged on Forged; on a tier with matched gear within ±1 minute |

`GearEconomyResearch` already reads every cohort; it gains `GEAR` and `TIER` settings when the loadout exists.

## 14. Later (outline only)

**Three active skills per hero.** Today each hero has one ability (`Ability_ForgeBurst`, `Ability_CrucibleVent`). A second
opens at floor 2's first clear, a third at floor 3's; a skill bar of three on the right thumb. Gear touches them only
through the ability-cooldown line (capped at 10 %). Its own document, after V0.1's characters (docs/27 decision 12).

**An async arena (the lead's proposal; the owner has not confirmed it).** Builds against builds, driven by the existing
autopilot (`AutopilotCohort.Careful` and its card scoring): a player's snapshot (hero, weapon, gear, a card policy)
fights another snapshot in a small arena, each side played by the autopilot, the result seeded so a replay is exact.
Local rivals first (the player's own past snapshots, and generated rivals at fixed gear powers), online async later
(it needs a server and an anti-tamper story the game does not have). Matchmaking by gear power bands of ±10 %. Rewards
in cosmetics and Seals, never in gear power, so the arena does not become the economy. Its own document if confirmed.

## 15. Phased build plan

| Phase | Work | Done when |
|---|---|---|
| 0 (this branch) | Income research and the model | the tables above re-run from the repository |
| 1 | Pure core in `Progression`: `ItemDefinition` assets, `ItemInstance`, `AnvilRule` (odds, pity, ceiling, the stream), `EfsunRule`, `GearStats` (caps, the index), `UpgradeStat` appended; EditMode tests (also in CombatChecks) | odds, ceiling, caps and the index pinned by tests; the model's numbers reproduced in C# |
| 2 | Save version 4 and migration; `PlayerProfile` gains the bag, materials, boosters and the anvil's stream with the existing repair-on-load style | `ProfileStoreTests` round-trip v4, migrate v1-v3, repair damaged saves |
| 3 | Run loot: drops at bosses, elites and the finale; the satchel; `RunBank` secures and settles it like gold; the result screen's loot list; telemetry | a scene test banks a known seed's loot; a death loses the expected part |
| 4 | The Forge's tabs: Anvil, Efsun sheet, Bag (visual work is Claude's since 2026-09-30) | the owner strikes a +5 on the S23 |
| 5 | Geared cohorts in the simulation; section 13's checks | the table in 13 filled |
| 6 | Difficulty tiers (Tempered, Inferno, Abyssal) with their drop tables, measured with the matched cohorts | section 13's tier rows pass |
| 7 | Device week with the owner; tune the knobs from telemetry | runs to +5 / +7 inside section 9's quartiles |

Phases 1-3 touch no balance number; 5 must pass before 6 ships.

## 16. Questions for the lead

1. **Weapon slot = the forged weapon** (3.1), instead of a dropped weapon item beside the chosen one. Agree?
2. **Item-bound basma with inheritance** (3.4, 10 % of the level's cost) or **slot-bound basma** (the +7 belongs to the
   ring slot and any ring worn there gets it; simpler, but the "+7 ring" stops being a thing the player owns)?
3. **Weapon upgrades per weapon id**: a player of both heroes upgrades two weapons (five targets). Or share the weapon's
   basma across weapons of the same hero?
4. **Pity and ceiling** as in 4.1 (soft +5 -> +2, ceiling 2 -> 10 failures), or the lead's flat "a few points" without a
   ceiling? The ceiling is this document's answer to "not too punishing"; without it, 3 % of +9 climbs take 11+ strikes.
5. **Efsun "keep old or new"** (6.2) against Metin2's replace-at-once; and no line lock. Agree?
6. **Seals from elites and bosses, not chests** (5): chests keep mend, gold and books. Agree?
7. **The owner's 39,491 gold** stays as is (materials gate it, 4.2). Or reset dev profiles at the version bump?
8. **Gold find as a line** (capped 10 %): it compounds the economy slightly; drop it if the lead wants the economy pure.
9. **Item drops on a death**: the gold's rule (kept or lost on a seeded coin, half in expectation) for items too, or
   always keep items (only materials and boosters at risk)?
10. **Difficulty tiers matched to gear** (8) with docs/40's names, and rewards x1.25 / x1.5 / x2. Agree, or skill tiers
    (docs/40's 1.8 / 1.5) on top later?
11. **The 30 s Warden floor for +9g on Forged** (13): the line under which the boss stops being a fight.
12. **Async arena**: keep as an outline until the owner confirms (14)?

## Measured and modelled: how to re-run

    dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~GearEconomyResearch"
      # FOUNDRY_SEEDS=100 (default), FOUNDRY_REALISM=typical; writes TestResults/gear-economy.md and .csv
    python Tools/Economy/gear_economy.py --players 2000
      # reads Tools/Economy/income-2026-11-16.csv (the run of 2026-11-16, seeds 1-100 a hero, copied from TestResults)

## Sources

- Metin2 wiki, [Blacksmith](https://en-wiki.metin2.gameforge.com/index.php/Blacksmith) (a failed upgrade destroys the
  item; the guild and tower smiths' +10 %) and [Blessing Scroll](https://en-wiki.metin2.gameforge.com/index.php/Blessing_Scroll)
  (the item is kept, one level lower); [Bonuses](https://en-wiki.metin2.gameforge.com/index.php/Bonuses) (5 + 2 segments,
  reinforce and blessing marble, switching, no repeat of the default bonus).
- elitepvpers, [Metin2 upgrade chance](https://www.elitepvpers.com/forum/metin2-private-server/527622-help-upp-chance.html)
  (rates are server data in `refine_proto`).
- GameMarket, [Metin2 upgrading and bonus switching](https://gamemarket.gg/news/metin2/metin2-item-upgrading-bonus-switching-guide-build-endgame-gear-without-going-broke)
  (switching as an RNG grind).
- Maxroll, [Lost Ark gear honing](https://maxroll.gg/lost-ark/resources/gear-honing-system) and
  [ExitLag's honing guide](https://www.exitlag.com/blog/lost-ark-honing-guide/) (failure raises the next chance;
  Artisan's Energy at 100 % guarantees the next hone).
- [Mein-MMO on Diablo Immortal's legendary gems](https://mein-mmo.de/en/the-legendary-gems-make-diablo-immortal-completely-pay2win-this-is-how-they-work,834066),
  [Sportskeeda](https://www.sportskeeda.com/esports/how-legendary-gem-system-diablo-immortal-make-game-completely-pay-to-win)
  and [GameSpot's gem guide](https://www.gamespot.com/articles/diablo-immortal-guide-how-to-find-and-upgrade-legendary-gems/1100-6504038/)
  (the 50-crest guarantee, ranks by duplicates and Gem Power).
- [Pro Game Guides, Archero equipment and scrolls](https://progameguides.com/archero/archero-how-to-get-pets-equipment-scrolls/),
  [AllClash's fusion guide](https://www.allclash.com/archero-fusing-guide-how-to-get-legendary-equipment-fast/) and
  [Game Developer, Finding the fun: Archero progression](https://gamedeveloper.com/design/finding-the-fun-archero-part-2---progression)
  (coins plus dropped scrolls; fusion returns what was spent).
- [Vampire Survivors wiki, PowerUp](https://vampire-survivors.fandom.com/wiki/PowerUp?oldid=5168) and
  [Gamer Journalist](https://gamerjournalist.com/vampire-survivors-powerups/) (escalating prices, full refunds).
- docs/40 sections 2.7 and 10.1 (Megabonk's meta; the difficulty modes' names).
