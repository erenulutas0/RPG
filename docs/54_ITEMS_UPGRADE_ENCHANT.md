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
| Efsun | Lines by rarity 1/2/3/4, a fifth by Binding Rune; 13-line pool fitted to this game; tiers 1-5; reroll all, then **keep old or new** | reroll 60/100/160/250 gold by rarity; a "good" 4-line roll 1 in 26 |
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

Modelled: careful 1.50, clumsy 0.60, auto-farmer 0.16 items a run (items are always kept on a death, section 17). The slot is drawn evenly from the three; the rarity
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
death, each booster dropped on the dying floor is kept or lost on a seeded coin, the gold's half in expectation). Items
are always kept (the lead, section 17).

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
| 2 | Rare (100) | 0.7 % | 141 | 14,085 |
| 3 | Epic (160) | 2.0 % | 50 | 8,002 |
| 4 | Gold (250) | 3.9 % | 26 | 6,488 |
| 5 | Gold + Rune (250) | 6.3 % | 16 | 3,983 |

The perfect roll (five power lines at tier 5) is out of reach by design (the five power stats among 13, each at tier 5: about 1 in 10^11); "good" is a few weeks' side
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

Built in phase 6 (section 21) with section 8's health and lower damage: Tempered x1.08 / x1.02, Inferno x1.15 / x1.10,
Abyssal x1.20 / x1.14.

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
- The result over the card for 1.2 s: success - sparks, the badge rolls up, a chime; failure - "Missed: +N kept" in
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

## 17. Lead decisions 2026-11-16

The lead's answers to section 16; the text above and the model follow them.

1. **The weapon slot is the forged weapon** (3.1).
2. **Basma belongs to the item**, with inheritance at 10 % of the level's expected cost (3.4).
3. **Weapon upgrades per weapon id** (3.1).
4. **The uneven pity and the hard ceiling** of 4.1: the owner wants high basma hard, but not punishing.
5. **Efsun keeps old or new**; no line lock for now (6.2).
6. **Forge Seals from elites and bosses only**; chests keep mend, gold and books (5).
7. **The owner's 39,491 gold stays**; no reset at the version bump (10).
8. **Gold find is dropped** from the line pool: the economy stays measurable. The pool is 13 lines (6.1); the efsun
   table above is re-run without it (a good 4-line roll 1 in 26, was 1 in 30).
9. **Items are always kept on a death** (they are rare, about 1.5 a run, and the owner asked for not-too-punishing);
   materials and boosters follow the gold's rule (half of the dying floor's in expectation). The model keeps items whole
   (careful 1.50 a run, clumsy 0.60, auto-farmer 0.16); the milestone tables do not read items and do not move.
10. **Gear-matched tiers** with docs/40's names and rewards x1.25 / 1.5 / 2 (8).
11. **The 30 s Warden floor** for +9 with good efsun on Forged (13).
12. **The async arena stays an outline** until the owner confirms it (14).

Next: phases 1-3 of section 15 are built in this worktree (section 18).

## 18. Built: phases 1-3 (2026-11-17)

No balance number moves: gear is not applied to any run (every run, every shipped test included, plays ungeared), loot is
never on the ground (nothing the fight or the autopilot walks to changes), and loot draws from its own stream
(`RunRandom.Loot`), so no offer, chest or crit draw moves. The Anvil has no screen yet (phase 4): its rules, save and
telemetry are built and tested, and a player can reach none of it but the loot line on the result screen.

**Phase 1, the pure core** (`Scripts/Progression`):
- `AnvilTable`: the lead's odds, the pity steps (+5/+5/+5/+4/+3/+2), the ceilings (2/3/4/6/8/10), gold and material per
  strike, the Seal's +10 (one a strike), the Hammer's +6 limit, inheritance at 10 %, the melt's gold and Ember Shards.
- `AnvilRule`: a strike's chance and outcome (a failure keeps the level, adds pity; the ceiling makes the next certain),
  the roll from the profile's anvil stream, and the expected strikes and gold in fixed point (`ExpectedStrikesMillionths`,
  `ExpectedGoldTo`, `InheritGold`), which `AnvilRuleTests` pins to the model's table (+9: 3.96 strikes, 16,073 gold).
- `EfsunRule` and `EfsunLine`: lines by rarity (1-4, a weapon 2, five at most), the 13-stat pool without gold find
  (`GearStat`, `GearStatIds`), tiers 40/30/18/9/3, values per tier, rerolls 60/100/160/250 by rarity, a rune's line.
- `GearStats`: the main stats by level, the per-stat caps and the power index in fixed point; `GearStatsTests` reads the
  model's sets (+15 / +21 / +31 / +42 / +51 %).
- `ItemInstance`, `ItemTemplate`, `GearSlot`, `MaterialKind`, `BoosterKind`; `Content/ItemDefinition` and nine assets
  (`Data/Items`: an armour, an amulet and a ring for each floor) listed by `PrototypeEconomy._items`.
- A change from section 6.1: the gear's stats are their own enum (`GearStat`) rather than new `UpgradeStat` values; they
  are mapped onto the run's stats in phase 5, when gear is applied, so nothing that switches on `UpgradeStat` moves now.

**Phase 2, the profile and the save**:
- `Core/ProfileGear`: the bag (30), each weapon's gear, the equipped armour/amulet/ring, materials, boosters, the anvil's
  seed and draws, the first armour's gift, the tiers (stored ahead of their build), a shown reroll; `Restore` repairs a
  damaged save (unknown or repeated stats and the main stat dropped from lines, tiers, levels and pity clamped, items
  without an id or in the weapon slot dropped, repeated uids renumbered, equipped uids that do not resolve unequipped,
  negative counts zeroed, an unresolvable reroll cleared) and never throws.
- `PlayerProfile.Gear`, `AddItem`, `AddMaterial`, `AddBooster`, `DepositLoot` (one change each).
- `Progression/Anvil`: strike (with a Seal or a Hammer, spent only where the strike is not already certain), reroll shown
  then kept old or new (a shown reroll blocks every other operation until resolved), Binding Rune, melt, inheritance,
  equip, and a weapon's gear made on first use (two lines); one `Changed` per operation; events for telemetry.
- `ProfileSaveData` version 4, `ProfileMigration` 3 -> 4 (gold untouched), `Save/ProfileGearSave` (fields <-> snapshot).

**Phase 3, the run's loot**:
- `Progression/LootRule`: section 3.2's and 5's drops per moment (floor clear, elite leader, gate boss, walk-out; the first
  floor-1 boss's armour), in parts of 10,000, items with rarity from `RarityTable` at no luck and lines from the same stream.
- `Economy/RunLoot`: the satchel; `Secure` at a Descend; `Settle` at the end (a defeat loses the floor of half the
  unsecured materials and a seeded coin per unsecured booster; items always kept).
- `Economy/RunBank`: items into the profile the moment they drop (a full bag melts the drop at once), secured materials
  and boosters at the Descend, the settled rest at the end.
- `CombatSetup`: `Loot`, `Anvil`; a gate boss or elite leader falling (`EncounterController.IsGateBossOfWave`,
  `IsEliteLeaderOfWave`, read-only), a floor clear and the walk-out feed the loot; the Descend secures it.
- Result screen: the loot as the build label's last line ("Loot: Ember Shard x4, Forge Seal x1, Cinder Ring (Rare)",
  "| lost: ..." after a death), invariant culture (`Economy/LootText`, `PrototypeTextDefinition.ResultLootFormat`).
- Telemetry: `loot_dropped` (kind, id, amount, rarity, source, floor); `currency_earned` per material and booster banked
  (source `run_bank`, after the gold's, before `run_end`); `loot_lost`; between runs `anvil_attempt`, `efsun_reroll`,
  `efsun_kept`, `efsun_line_added`, `item_melted`, `item_inherited`, `item_equipped`, and `currency_spent` with sinks
  `anvil`, `efsun`, `inherit`. A run without loot writes none of them.

**Tests.** New EditMode: `AnvilRuleTests`, `EfsunRuleTests`, `GearStatsTests`, `ItemContentTests` (assets),
`ProfileGearTests`, `AnvilTests`, `RunLootTests`; extended: `ProfileStoreTests` (v4 round trip, a v3 file, a damaged v4
file), `RunTelemetryTests` (loot and Anvil lines; the log-shape rule lets the bank's lines chain before `run_end`),
`WeaponForgeTests` (version 4). All but `ItemContentTests` and `ProfileStoreTests` also run in CombatChecks.

    Unity, EditMode:  pwsh -File Tools/Run-UnityTests.ps1 -Platform EditMode -SkipApk -Filter "AnvilRuleTests;EfsunRuleTests;GearStatsTests;ItemContentTests;ProfileGearTests;AnvilTests;RunLootTests;ProfileStoreTests;WeaponForgeTests;RunTelemetryTests"
    Unity, PlayMode:  pwsh -File Tools/Run-UnityTests.ps1 -Platform PlayMode -SkipApk -Filter "RunResultTests;DescentFlowTests;FloorFlowTests;FoundryDescentFlowTests;FoundryRunEndFlowTests;RelicForgeFlowTests;TelemetryFlowTests;MultiRunSoak"

The whole EditMode suite is the safer read (CombatSetup changed); PlayMode's flows above are the ones that end a run,
read the result screen or the telemetry log, or play several runs on one profile.

**The default batch.** Run before (main 0243c55d) and after (phase 3's code, a6b4e955) with `FOUNDRY_SEEDS=200
FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_REALISM=typical FOUNDRY_FLOORS=4`: every CSV, `foundry-gate.md`,
`foundry-cohorts.md` and `foundry-finale.md` match byte for byte; in `foundry-batch.md` only the clock in its header and the
wall-seconds column differ. CombatChecks: 877 green (`Başarılı`); the Unity compile check green; `Verify-Project.ps1` passes.

**Risks the build leaves open.**
- **Not run in Unity.** Compile check and CombatChecks only; the Unity suites above are the lead's to run.
- **A full bag melts drops.** Until the Bag tab (phase 4) the player cannot melt or equip, so after about 20 careful runs
  the bag is full and every drop melts at once for gold and Ember Shards (said on the result line as "(Rare, melted)").
- **Profile saves mid-run.** An item is banked the moment it drops (always kept), so a boss or elite drop writes the
  profile mid-floor, as a checkpoint's secured gold already does; a few more writes a run.
- **The result line can overflow the build label** on a long run (many boosters and items); the label is 174 units tall.
- **`EncounterController` gained two read-only queries** and nothing else; the simulation does not model loot, so loot
  cannot move any number it measures.

## 19. Built: phase 4, the Anvil, Efsun and Bag tabs (2026-11-17)

Section 11 on the phone, built in code like the map choice (`UI/AnvilView`, its own overlay canvas, sorting order 90) and
drawn in the choice cards' look (`ChoiceCardPainter` with `RunChoiceView`'s `ChoiceCardArtSet`: the dim, the surface, the
title plate; each piece on its rarity's card and item ground - Common grey, Rare blue, Epic red, Gold gold) with the
forge's painted plates on every button (`ScreenArtSet` primary and secondary, the pressed and disabled plates by sprite
swap); plain colours where either set is missing. Legacy uGUI, no scene or asset change.

- **Entry.** The Relic Forge's title row gains two buttons, **Bag** left and **Anvil** right of the 600-wide nameplate
  (190 x the row's height, about 100 px on the S23). Each opens the panel on its tab; the panel's tab row is Forge (back),
  Anvil, Efsun, Bag (221 x 104 each).
- **Header.** The wallet (gold, Ember, Bead, Spore, in their colours) and the boosters, then the equipped pieces: the
  weapon the next run carries, the armour, the amulet, the ring - each a 220 x 236 card on its rarity with its level badge.
  A tap picks the piece the Anvil and Efsun tabs work on; the equipped weapon is picked first. An empty slot sends the
  player to the Bag.
- **Anvil.** The piece's card, its main stat now and next ("Damage +7% > +8.5%"), the chance in large type coloured by how
  likely it is, the breakdown ("Base 20% + memory 6% + Seal 10% (a miss keeps the level)", or why the strike is certain),
  the anvil's memory bar towards the ceiling ("2 / 10: holds for certain in 8"), the cost (gold and material, red where
  short, the material in its colour with what is held), the Seal and Hammer toggles shown only where they change the
  strike, and **Hold to strike +N** (`UI/HoldToStrike`: a 0.4 s fill; lifting or sliding off cancels; a tap is not a
  strike). The moment after, for 1.4 s over the chance: "+9: the strike held!" in gold on the nameplate plate, which
  settles from a slight swell, or "Missed: +8 kept / The anvil remembers: +1 memory" in grey. The button says why
  it cannot strike ("Need 120 more gold", "Need 2 more Gloam Spore", "Keep or discard the reroll first", "+9: nothing
  more to strike"). A weapon without gear yet strikes at +0 and its gear (two lines) is made by that strike; opening the
  tab makes nothing.
- **Efsun.** The piece's lines on recessed plates with their tiers as five diamonds (grey, blue, red, gold); a reroll
  (its rarity's gold, or an Efsun Scroll) splits the list into Current and New side by side with **Keep old** and **Take
  new**; a Binding Rune adds a line up to five.
- **Bag.** "Bag 12 / 30" and thirty 146-unit places on their rarity's frame and ground, each with its icon and level; a
  mark on the equipped and the picked. A tap picks an item (its name, rarity, slot, level, main stat and lines below);
  **Equip**, **Melt** (the first tap says what it pays, the second melts), **Inherit** (the picked item is the source;
  only lower items of its slot stay tappable; the button then reads "Move +5 / 78 gold"; with no target it cancels) and
  **Upgrade** (the Anvil tab on that item). A full bag says a new drop melts at once.
- **Icons.** No item art exists yet: a piece shows the painted card icon of its weapon or of the stat it carries
  (`UpgradeIconSet`: armour, attack speed, crit). Dedicated armour, amulet, ring, material and booster icons are an art
  pass of their own.
- **Words and numbers.** `Progression/GearText`: stat names, values with units ("+2.5%", "+0.8 pts", "-4%"), lines,
  levels, gold with its separator ("12,345"), all in the invariant culture; `GearTextTests` reads them on tr-TR. Every
  label best-fits down to half its size.
- **The result's loot line** is now one short line at the build label's foot ("Loot: Ember 3, Seal 1, 1 item | lost:
  Bead 1", `LootText.Compact`); if it would overflow the 174-unit label at its full size it gives way to "Loot: N pieces
  (see the Bag)", and that to nothing (`RunResultView.LootLine`).

**Tests.** EditMode `GearTextTests` (also in CombatChecks), `RunLootTests` (the compact line). PlayMode `AnvilFlowTests`:
the tabs open from the forge and close; a missed strike keeps the item, its level and saves the pity, and a tap is not a
strike; a held strike with a Seal reaches +9; a weapon's gear is made on its first strike; a reroll kept old, a scroll's
taken new, a rune's line; the bag equips, melts on the second tap and inherits for its 10 %; a full bag shows thirty.

    Unity, EditMode:  pwsh -File Tools/Run-UnityTests.ps1 -Platform EditMode -SkipApk -Filter "GearTextTests;RunLootTests"
    Unity, PlayMode:  pwsh -File Tools/Run-UnityTests.ps1 -Platform PlayMode -SkipApk -Filter "AnvilFlowTests;RelicForgeFlowTests;PauseFlowTests;ArtHudFlowTests;PaintedScreensFlowTests;RunResultTests;DescentFlowTests"
    Capture:          pwsh -File Tools/ArtReview/Run-AnvilUiProof.ps1     (frames in ArtDirection/2026-11-17/anvil-ui-01, untracked)

`PauseFlowTests` (every button on a raycast canvas, every label inside its button) and `ArtHudFlowTests` (every forge
label fits at its minimum size on 1760- and 2232-unit safe areas) read the two new forge buttons.

**Risks.** Not seen in Unity: the layout is laid out by the numbers above and compiled, not looked at; the capture is for
that look. Item icons are stand-ins (the stat cards).

**Fix after the first capture (2026-11-17).** The panel drew as a 100 x 100 thumbnail inside the forge: its canvas was
parented under the forge's view, which sits on the result's canvas, so it was a nested canvas (scaler ignored, a default
100 x 100 rect). It is now a root canvas in the scene, under nothing; `AnvilFlowTests` asserts it is a root canvas
covering the screen's width and that its surface fills the width (or, on a short screen, the height), and that every
label on each tab fits at its minimum size. The piece cards stand 30 apart (the gold glow reaches 14 past a card) and
carry no crest (the gold clasp would stand into the row above). The forge's title is narrowed to the nameplate's middle
608 units so the Bag and Anvil buttons sit clear of it; `RelicForgeFlowTests`' layout test checks them on the title's
row, clear of the title and at least 44 units tall.

## 20. Built: phases 5 and 5b, the gear on the run (2026-11-17)

The equipped gear reaches the run, every line of it, and the simulation measures geared cohorts. Nothing measured moves:
every shipped balance test, the gate and the default batch play ungeared (`GearLoadout.None`), and an ungeared run
touches nothing - no modifier, no factor, no share, heal, cooldown or reach changed - so it is today's run to the bit
(the batch below is byte-identical). No balance constant changed; the difficulty tiers (phase 6) are not built.

**The lead's decisions of 2026-11-17 (phase 5b).**
1. **Gear is its own multiplicative layer.** An item's "Damage +14 %" is 14 % more whatever the tomes hold: damage,
   attack speed, area, max health and move speed multiply the final stat as a factor of their own
   (`ModifierOperation.Multiply`, appended), never summed with the cards' and tomes' percents. Crit, armour and luck
   stay points added as the cards add theirs. (Phase 5 had them as percents summed with the tomes', which diluted a
   +10 % to +6.8 % on a run holding +48 % of tomes.)
2. **No dead lines.** The five lines phase 5 left inert each reach a rule of the run (below).
3. **Telemetry.** `run_start` logs the gear a run wears, and device pulls set geared runs apart.
4. **The auto-farmer's ladder** is measured to +9 and +9g; its floor-clear step counts as its measurable step.

**One body** (section 7.1, the stamina lesson of docs/21). `GearStats.Apply(GearLoadout, HeroStats, WeaponLoadout,
WeaponRuntime, HealthState, AbilityRuntime, GroundDrops)` is `[MethodImpl(NoInlining)]` in `Progression` and is the only
place gear reaches a run; `GearStats.RunAmount` and `GearStats.Factor` (the float conversions), `ModifiableStat`'s
factor, `WeaponLoadout.ShareOf`, `AbilityRuntime.ShortenCooldown`, `HealthState`'s heal scaling, `BurnRule.Scaled` and
`DropRule.IsWithinReach` at a radius are non-inlined bodies shared by the scene and the simulation, and none of them runs
without gear. `CombatSetup.Awake` calls `Apply` once, after the ground drops are made (the hero's stat sheet bound, the
weapon loadout, the `UpgradeService` and the ability made); `DescentSimulation.FoundryRun` calls it at the same point of
its copy of that Awake (it now makes its `AbilityRuntime` just before, a pure construction). `GearLoadout` is what a run
wears: the carried weapon's own gear and the equipped armour, amulet and ring, its clamped totals taken once, so nothing
changed later moves a run already started. The scene builds it from `PlayerProfile.Gear.EquippedPieces(HeroWeapon.Id)`:
the weapon slot is per weapon id (section 17, decision 3), so the Vanguard carrying the Sword wears the Sword's basma
and lines and never the Staff's; Ilya always carries the Staff and wears its. `CombatSetup.Gear` exposes it.

**What maps where.** Each stat's total over the whole gear, main stats and lines, is clamped at its cap (6.1) first.

| Gear stat (`GearStat`) | Run path | How | At the cap |
|---|---|---|---|
| Damage (the weapon's main) | `UpgradeStat.Damage` on every weapon held and every weapon picked up later (`WeaponLoadout.AddGlobal`, the tomes' path); the hall arena's one weapon | factor (`Multiply`) | x1.14 |
| Attack speed (the amulet's main) | `UpgradeStat.AttackSpeed`, the same way | factor | x1.07 |
| Area | `UpgradeStat.Area` (the Reach tome's), the same way | factor | x1.05 |
| Max health (the armour's main) | `HeroStats.MaxHealth` (the gain arrives filled) | factor | x1.14 |
| Move speed | `HeroStats.MoveSpeed` | factor | x1.06 |
| Crit chance (the ring's main) | `UpgradeStat.CritChance` on every weapon; a rolled crit strikes for 2x (`WeaponRuntime`) | points / 100, added | +0.04 |
| Armour (damage taken) | `HeroStats.Armor`: the pool that takes exactly that share off an unarmoured hero, 100 r / (1 - r) | points, added | 4.17 armour, 4 % less |
| Luck | `HeroStats.Luck` (`RarityTable`, the cards' rarity only) | points, added | +10 |
| Damage vs elites and bosses | every weapon's elite and gate-boss shares (`WeaponLoadout.SetTargetShareFactor`, the path the Daggers' elite share and the Sword's gate-boss share ride; weapons picked up later too; the hall's one weapon directly): every landing of a weapon - strike, splash, burst, splinter, pulse - on an elite (`EncounterController.Elites`) or the gate boss | factor on the shares | x1.12 |
| Burn damage | every tick of the hero's burns (`BurnState.DamageScale`, set as a burn is lit and passed on with a spread: `CombatSetup.IgniteStruck` and `BurnStatus`, the simulation's `Ignite`) | factor on the ticks | x1.15 |
| Pickup radius | `GroundDrops.PickupRadius`, which the scene's `EncounterController` and the simulation both collect through (`CollectNear`) | factor on the radius | x1.25 (1.25 units) |
| Ability cooldown | `AbilityRuntime.ShortenCooldown`, once at the start: every cooldown of the run's ability | factor below 1 | x0.90 (the 10 % cap) |
| Healing received | `HealthState.HealingReceived` on the hero: every heal - the chest's mend, the forge's heal, Second Wind, the simulation's research heals - comes through `HealthState.Heal`, the one place it applies | factor on the heal | x1.15 |

Where a rule line has nothing to act on:
- **Burn damage is Ilya's alone.** Burns exist only through Ember Memory (docs/45), Ilya's passive, which lights every
  hit of every weapon she carries; the Vanguard (and the research Riftblade) never burn anything, so the line does
  nothing for them - a Vanguard run wearing only burn lines is its ungeared run (`GearApplyTests` pins it). Crucible
  Vent's burst is the ability's damage per spent stack, not a tick, and is not scaled.
- **Damage vs elites and bosses is the weapons' landings.** The ability's damage, burns and the pit's damage on an
  elite or the gate boss are not weapon landings and are not scaled, as the Daggers' share never scaled them. The hall
  arena has neither elites nor a gate boss.
- **Pickup radius is the ground drops'.** The chest's reach (`ChestRule.PickupRadius`) and the autopilot's stopping
  distance are not drops and do not move.
- **Ability cooldown** has no ability to shorten for the research Riftblade (its active keeps its own clock).
- **Healing received** scales heals, not a raised maximum: health gained from a card or the gear's own +% arrives filled
  as before (`SetMaximum`), unscaled. The game has no regeneration or lifesteal to scale.

**Armour joins the cards' armour pool.** On an unarmoured hero four points take exactly 4 % off; beside Iron Skin's 20
the same 4.17 armour takes about 2.8 points more. Crit likewise adds its points to the cards' and the Precision tome's.

### Telemetry: `run_start` and the device pulls

A run whose loadout changes it (`GearLoadout.IsEmpty` false) writes two keys on `run_start`, after the keys every run
writes: `gear_power`, the index in hundredths of a percent (4190 is +41.9 %), and `gear_levels`, the carried weapon's,
the armour's, the amulet's and the ring's basma ("9,7,-1,9"; the weapon 0 when the Anvil never touched it, -1 an empty
slot). An ungeared run's line keeps its keys exactly (`RunTelemetryContext.Gear`, `RunTelemetry.GearLevels`).
`DevicePull` reads both (`RunRecord.GearPower`, `GearLevels`) and leaves a geared run out of the owner's floor rows,
counted apart (`DeviceEvidence.GearedRuns`, named in the gate's "left out" line), since the simulation those rows are read
against plays ungeared - as a run begun on a deeper map already is.

### The geared cohorts and the research

`FoundryGear` (`Tests/Support`) holds section 13's cohorts, each an autopilot cohort wearing section 7.2's modelled set
(the hero's own weapon at the level, a Gold armour, amulet and ring at the level; for +9g each piece carries damage,
crit, health and move speed at tier 3, less its own main stat): `careful+5` (+15 %), `careful+7` (+21 %), `careful+9`
(+31 %), `careful+9g` (+42 %), `clumsy+5`, `auto-farmer+5` (+15 %), `auto-farmer+9` (+31 %) and `auto-farmer+9g` (+42 %).
`DescentSimulation.FoundryHero.Gear` is `GearLoadout.None` unless a report wears one. `GearedCohortsResearch`
(`Tools/CombatChecks`, explicit) plays the named geared cohorts (`FOUNDRY_GEAR`, all by default) and the ungeared cohorts
they are drawn from, both heroes, down the whole Descent on the same seeds with the batch's levers and typical hands, and
writes `gear-cohorts.md` and `gear-cohorts.csv` (one row a run; `gear-economy.csv`'s columns first, so
`gear_economy.py` can read geared income).

**Measured: section 13's Forged rows, with the factor layer and every line wired** (2026-11-17, phase 5b's code; seeds
1-200 a hero, typical human hands, the whole Descent; `FOUNDRY_PARALLEL=4`, 12 minutes). The ungeared rows match the
default batch's cohorts to the seed (careful 190 / 194 completed, clumsy 61 / 100, auto-farmer 5 / 6). Completed and
paired are of the 200 seeds; paired with the ungeared cohort on the same seeds: both / only ungeared / only geared.
Floors cleared: floor 1 / 2 / 3 (the finale cleared is the completion). The bosses: P50 of the felled fights (floor 1's
Warden from walking out to falling, as the gate's 45-65 s band reads it; floor 2's Regent and floor 3's Warden their own
fights). Minutes: the Descent's active minutes, P50 of the completed runs. Banked: forge gold a run, mean, rebuilt as
`GearEconomyResearch` does.

| Cohort | Hero | Gear | Completed | Paired | Floors cleared 1 / 2 / 3 | Warden (f1) P50 / P90 | Regent (f2) | Warden (f3) | Minutes | Banked |
|---|---|---|---|---|---|---|---|---|---|---|
| careful | Vanguard | - | 190 (95.0 %) | - | 195 / 191 / 190 | 51.3 / 62.4 s | 49.5 s | 61.6 s | 14.5 | 489 |
| careful | Ilya | - | 194 (97.0 %) | - | 198 / 195 / 194 | 46.0 / 55.8 s | 46.0 s | 56.7 s | 14.3 | 501 |
| careful+5 | Vanguard | +14.7 % | 199 (99.5 %) | 190 / 0 / 9 | 199 / 199 / 199 | 47.8 / 60.0 s | 46.8 s | 58.4 s | 14.4 | 507 |
| careful+5 | Ilya | +14.7 % | 197 (98.5 %) | 192 / 2 / 5 | 198 / 198 / 197 | 44.1 / 53.1 s | 43.7 s | 54.6 s | 14.2 | 506 |
| careful+7 | Vanguard | +21.0 % | 199 (99.5 %) | 189 / 1 / 10 | 199 / 199 / 199 | 47.3 / 58.3 s | 45.6 s | 58.4 s | 14.4 | 509 |
| careful+7 | Ilya | +21.0 % | 197 (98.5 %) | 192 / 2 / 5 | 198 / 197 / 197 | 42.1 / 53.2 s | 43.2 s | 53.5 s | 14.2 | 507 |
| careful+9 | Vanguard | +30.9 % | 199 (99.5 %) | 189 / 1 / 10 | 200 / 199 / 199 | 45.6 / 57.9 s | 44.4 s | 55.2 s | 14.2 | 512 |
| careful+9 | Ilya | +30.9 % | 197 (98.5 %) | 191 / 3 / 6 | 198 / 198 / 197 | 41.2 / 51.2 s | 42.1 s | 51.9 s | 14.1 | 510 |
| careful+9g | Vanguard | +41.9 % | 200 (100 %) | 190 / 0 / 10 | 200 / 200 / 200 | **44.9** / 55.5 s | 44.0 s | 53.4 s | 14.1 | 513 |
| careful+9g | Ilya | +41.9 % | 200 (100 %) | 194 / 0 / 6 | 200 / 200 / 200 | **40.5** / 51.4 s | 41.0 s | 51.2 s | 14.0 | 515 |
| clumsy | Vanguard | - | 61 (30.5 %) | - | 78 / 65 / 61 | 47.8 / 58.1 s | 46.2 s | 60.1 s | 14.3 | 196 |
| clumsy | Ilya | - | 100 (50.0 %) | - | 131 / 106 / 100 | 47.9 / 61.5 s | 48.6 s | 61.7 s | 14.4 | 312 |
| clumsy+5 | Vanguard | +14.7 % | 74 (37.0 %) | 54 / 7 / 20 | 91 / 79 / 74 | 45.0 / 54.3 s | 43.4 s | 56.8 s | 14.1 | 230 |
| clumsy+5 | Ilya | +14.7 % | 120 (60.0 %) | 86 / 14 / 34 | 153 / 131 / 120 | 46.1 / 59.6 s | 46.6 s | 59.2 s | 14.3 | 365 |
| auto-farmer | Vanguard | - | 5 (2.5 %) | - | 10 / 6 / 5 | 34.0 / 37.1 s | 30.2 s | 37.1 s | 13.3 | 58 |
| auto-farmer | Ilya | - | 6 (3.0 %) | - | 29 / 12 / 6 | 28.7 / 35.4 s | 35.0 s | 35.6 s | 13.4 | 99 |
| auto-farmer+5 | Vanguard | +14.7 % | 4 (2.0 %) | 3 / 2 / 1 | 17 / 9 / 4 | 30.1 / 33.8 s | 34.1 s | 45.1 s | 13.5 | 71 |
| auto-farmer+5 | Ilya | +14.7 % | 11 (5.5 %) | 3 / 3 / 8 | 53 / 34 / 11 | 28.6 / 33.5 s | 32.8 s | 35.5 s | 13.3 | 139 |
| auto-farmer+9 | Vanguard | +30.9 % | 7 (3.5 %) | 4 / 1 / 3 | 30 / 17 / 7 | 27.7 / 33.1 s | 32.5 s | 36.9 s | 13.4 | 92 |
| auto-farmer+9 | Ilya | +30.9 % | 19 (9.5 %) | 6 / 0 / 13 | 67 / 50 / 19 | 26.6 / 31.5 s | 31.8 s | 33.6 s | 13.2 | 168 |
| auto-farmer+9g | Vanguard | +41.9 % | 11 (5.5 %) | 4 / 1 / 7 | 41 / 31 / 11 | 28.0 / 32.0 s | 33.3 s | 32.6 s | 13.2 | 118 |
| auto-farmer+9g | Ilya | +41.9 % | 29 (14.5 %) | 4 / 2 / 25 | 84 / 57 / 29 | 27.0 / 32.4 s | 31.0 s | 32.2 s | 13.2 | 194 |

Against section 13's Forged targets:

| Check | Target | Measured | Read |
|---|---|---|---|
| Geared completion | never below the ungeared cohort's | careful at every level, both heroes: 190 -> 199-200 (Vanguard), 194 -> 197-200 (Ilya) | **met** |
| Warden fight at +9g | P50 not under 30 s | floor 1's Warden 44.9 s (Vanguard), 40.5 s (Ilya); the Regent 44.0 / 41.0 s; floor 3's Warden 53.4 / 51.2 s | **met**, with room |
| `clumsy+5` | completion up by a measurable step | Vanguard 61 -> 74 (+6.5 points), Ilya 100 -> 120 (+10 points); floor 1 cleared 78 -> 91 and 131 -> 153 | **met** |
| The auto-farmer's ladder (+5, +9, +9g) | a measurable step; the floor-clear step counts (the lead, 2026-11-17) | floor 1 cleared: Vanguard 10 -> 17 / 30 / 41, Ilya 29 -> 53 / 67 / 84; every floor cleared, summed (floors 1-3 and the finale): Vanguard 26 -> 34 / 61 / 94, Ilya 53 -> 109 / 155 / 199; completion: Vanguard 5 -> 4 / 7 / 11, Ilya 6 -> 11 / 19 / 29 | **met** by floor clears at every rung; completion moves measurably from +9 (Ilya +13 / +23, the Vanguard +2 / +6) |
| The 14-minute Descent on Forged | unchanged | careful 14.5 -> 14.1-14.4 min (Vanguard), 14.3 -> 14.0-14.2 (Ilya); clumsy 14.3-14.4 -> 14.1-14.3 | **met** (at most 0.4 min shorter at +9g) |

What the rows say:
- **The factor layer changes the bosses little, the late floors a little more.** Against phase 5's additive reading
  (the same seeds), careful+9g's floor-1 Warden is 44.9 s (was 44.7) for the Vanguard and 40.5 s (was 41.6) for Ilya,
  floor 3's Warden 53.4 s (was 55.1) and 51.2 s (was 51.8): floor 1's Warden comes at about four and a half minutes,
  before the tomes have stacked, where adding and multiplying differ least. At +9g (index +42 %, DPS +24 %) the floor-1
  Warden falls 12 % faster for both heroes, not section 7.2's "about a fifth": the fight carries the 2.6 s reveal and the
  slams' dodging, which no DPS shortens. The 30 s floor is far away; the cost is the other side: Ilya's floor-1 Warden P50
  sits under the gate's 45 s band from +5 (44.1 s) and the Vanguard's at +9g (44.9 s), the gap phase 6's tiers exist to
  close.
- **The careful cohort saturates**: its few ungeared losses (10 and 6 of 200) almost all go at +5, and more gear buys
  speed.
- **clumsy's ladder works**: +5 adds 13 and 20 completions, with churn (20 and 34 seeds now complete, 7 and 14 no longer
  do - geared runs crit on other swings and see other offers).
- **The auto-farmer's ladder works by floors, slowly by completions.** Every rung clears more floors (Ilya's summed clears
  nearly quadruple at +9g, the Vanguard's more than triple); it still dies on floors 2 and 3, where it plays without
  dodging, the ability or a card score (docs/40 9.4's Auto Farmer). Its completion first moves measurably at +9 (Ilya
  6 -> 19) and at +9g reaches 14.5 % (Ilya) and 5.5 % (the Vanguard).
- **Income with gear (the model's gap).** careful banks 3-5 % more geared (489 -> 507-513, 501 -> 506-515: fewer deaths
  lose half a floor); clumsy+5 **+17 %** for both heroes (196 -> 230, 312 -> 365); the auto-farmer's ladder 58 -> 71 / 92 /
  118 (Vanguard) and 99 -> 139 / 168 / 194 (Ilya). `gear_economy.py` plays every run at ungeared income, so its clumsy row
  (first +5 in 5 runs, set +9 in 294) is pessimistic by about a sixth after the first +5, and its auto-farmer row by up to
  half; feeding `gear-cohorts.csv`'s rows to a player as its gear reaches each rung would close it (the CSV keeps
  `gear-economy.csv`'s columns; the model is not changed here).

### Tests

- **EditMode, also in CombatChecks: `GearApplyTests`.** An empty loadout (None, no pieces, a +0 set) touches nothing -
  no modifier, no change event, the shares, the heal factor, the cooldown, the reach and the burn factor as they were; a
  known loadout gives known numbers; the caps bind (the sheet's and the five rule lines'); section 7.2's +15 / +21 / +31 /
  +42 / +51 % read back off the run's own weapon and hero; **the factor layer**: over a build of +48 % Force, +5 flat
  damage, +30 % attack speed, flat health and +20 % move speed, a +9 set is exactly 10 % more damage, 5 % more attack
  speed and 10 % more health, applied before or after the build, and its crit adds three points; the sheet's mapping
  (Multiply for five, points for three, a rule for five); **damage vs elites and bosses** on an elite, the gate boss and
  a weapon picked up later, not on a grunt, and on the hall's one weapon; **burn** ticks x1.09, an ungeared burn as
  authored to the bit, the Vanguard's simulated run unchanged and Ilya's changed; **pickup** reach 1.12 in `GroundDrops`
  (a drop 1.1 away) and in the simulated run; **ability cooldown** 6 % and capped at 10 %, and in the simulated run;
  **healing** x1.15 at the cap, Second Wind included, a raised maximum unscaled; a weapon picked up later wears the gear;
  the hall gears its one weapon; the weapon's gear is the carried weapon's own; the simulation's heroes are ungeared by
  default; the eight cohorts' indices.
- **EditMode, also in CombatChecks: `RunTelemetryTests.ARunWearingGearSaysSoOnRunStart`** (an ungeared run's keys
  exactly; a geared run's two keys, their values; a +0 armour without lines is ungeared) and **`DevicePullTests.
  AGearedRunIsLeftOutOfTheOwnersFloors`**.
- **PlayMode: `GearRunStartTests`.** A fresh profile starts the hall and the foundry with the weapon exactly as its
  definition makes it and the hero at its authored health; a +9 Sword starts the hall's run at 1.1x damage and nothing
  else; on the foundry a +9 Sword (with the Staff at +9, not carried), a +9 armour and a +9 ring give the first weapon
  1.1x damage and three points of crit and the hero 1.1x health; a +9 armour starts the hall's hero full at its gain; a
  Gold ring of the four rule lines reaches the foundry's loadout shares (x1.06), the hero's heals (x1.09), the ability's
  cooldown (x0.94) and the drops' reach (x1.12), the sheet untouched.

      Unity, EditMode:  pwsh -File Tools/Run-UnityTests.ps1 -Platform EditMode -SkipApk -Filter "GearApplyTests;GearStatsTests;RunTelemetryTests;HeroStatsTests;UpgradeTests;EliteRuleTests;WeaponLoadoutTests;BurnRuleTests;AbilityTests;GroundDropsTests;RelicForgeTests;FoundrySimulationTests;FoundryCohortTests;CombatStateTests"
      Unity, PlayMode:  pwsh -File Tools/Run-UnityTests.ps1 -Platform PlayMode -SkipApk -Filter "GearRunStartTests;AnvilFlowTests;TelemetryFlowTests;CinderArcanistTests;BurnEffectsTests;AbilityFlowTests;ChestPresentationTests;UpgradeFlowTests;RunResultTests;CombatSceneTests;DescentFlowTests;FoundryDescentFlowTests"

The whole EditMode suite is the safer read: `ModifiableStat`, `HealthState.Heal`, `BurnState`, `AbilityRuntime`,
`GroundDrops`, `WeaponLoadout` and `CombatSetup` changed (each only where gear is worn). In PlayMode, `AnvilFlowTests`'
fixture profile wears gear (a +7 Sword, +3 armour, +7 amulet with a move-speed line, +9 ring with a luck line, and so on),
so its runs start geared and its `run_start` carries the gear keys - it kills the hero with 100,000 damage and reads the
forge, so nothing it asserts should move; `TelemetryFlowTests` reads ungeared runs, whose `run_start` keeps its keys.

### The regression line

Against main b0fa98c2 (its sources exported to a clean folder and run there), `FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0
FOUNDRY_COHORTS=1 FOUNDRY_REALISM=typical FOUNDRY_PARALLEL=4`, each run into its own empty output folder with the same
empty `FOUNDRY_EDITOR` and `FOUNDRY_DEVICE=none`, on phase 5's code and again on phase 5b's:
- `FOUNDRY_FLOORS=4`: `foundry-gate.md`, `foundry-batch.csv`, `foundry-batch-floor2.csv`, `foundry-cohorts.md`,
  `foundry-cohorts-floor2.csv`, `foundry-cohorts-floor3.csv`, `foundry-finale.md` and `foundry-finale.csv` match byte for
  byte; in `foundry-batch.md` only the clock in its header and the wall-seconds column differ.
- `FOUNDRY_FLOORS=2`: the same - every file byte-identical but `foundry-batch.md`'s clock and wall seconds.

CombatChecks: 899 green (`Başarılı`; 881 on main, 16 in `GearApplyTests`, one each in `RunTelemetryTests` and
`DevicePullTests`); the Unity compile check green; `Verify-Project.ps1` passes (a fresh worktree lacks the untracked
`Assets/_Project/Editor` folder its tracked `.meta` names - main's state, not this branch's; the check passes with the
empty folder present).

### Risks the build leaves open

- **Not run in Unity.** The compile check, CombatChecks and `Verify-Project.ps1` only; the EditMode and PlayMode suites
  above are the lead's to run. `GearRunStartTests` reads the foundry's start one frame after the load; the hall's tests
  also read the hero full, which holds while the first pack walks in from the far end.
- **Geared parity is by construction, not measured.** The scene and the simulation call the same bodies at the same
  point of the same Awake, and an ungeared run is byte-identical; but no Editor measurement (`HordePacingMeasurement`)
  has played a geared profile, so the parity table has nothing geared to read. A pacing run with a +9g test profile would
  measure it.
- **The owner's device rows thin out as the owner gears up.** Geared runs are now told apart and left out of the gate's
  owner floor rows; every profile receives an armour at its first floor-1 boss (section 3.2), so ungeared owner runs will
  become rare, and the owner's floor rows will need a geared reading (phase 6's tiers with their matched cohorts, or a
  geared batch) rather than the ungeared one.
- **The factor layer is a new operation on every stat.** `ModifiableStat` multiplies only when a `Multiply` modifier has
  been added (the batch is byte-identical), and no card carries one; a future card given `Multiply` in its asset would
  compound with the gear, which is the operation's meaning but not yet a designed card.
- **`AnvilFlowTests`' fixture starts geared runs.** Its asserts read the forge after the hero is killed with 100,000
  damage; a geared start should not move them, but it has not been run.

## 21. Built: phase 6, the difficulty tiers (2026-11-17/19)

Forged, Tempered, Inferno and Abyssal (section 8), each a harder Descent that pays more, each matched to a gear power. A
Forged run is today's run to the bit: Forged multiplies nothing, rounds no gold of its own, draws no loot word it did
not draw, and writes no telemetry key it did not write (the default batch is byte-identical, below). No Forged balance
constant changed.

### The tiers

| Tier | Opens when | Enemy health / damage | Matched gear (the cohort read) | Gold, materials, boosters | Item luck (`RarityTable`) |
|---|---|---|---|---|---|
| **Forged** | always | x1 / x1 | none (ungeared) | x1 | 0 |
| **Tempered** | the Descent completed on Forged | **x1.08 / x1.02** | +5 set, +14.7 % (`careful+5`, `clumsy+5`, `first-card+5`) | x1.25 | +10 |
| **Inferno** | the Descent completed on Tempered | **x1.15 / x1.10** | +9 set, +30.9 % (`+9`) | x1.5 | +20 |
| **Abyssal** | the Descent completed on Inferno | **x1.20 / x1.14** | +9 with good efsun, +41.9 % (`+9g`) | x2 | +30 |

Section 8's health stands; the damage is lower than section 8's (1.06 / 1.13 / 1.18), under the matched set's own health
(+5 %, +10 % and +14 % at the cap): the tier's health times damage is +10.2 %, +26.5 % and +36.8 % against the sets'
+14.7 %, +30.9 % and +41.9 %. The reasons are measured below.

**Where the multipliers ride** (`DifficultyTiers`, `FloorScaling.Tiered`). The tier's enemy health and damage multiply
the floor's own tier - the climb's (`ClimbScaling`), a boss room's growth included, or an authored floor's - in
`EncounterController.FloorHealthMultiplier` and `FloorDamageMultiplier`, which every node wave, elite, gate boss, horde
enemy and finale swarm enemy reads, before the run's difficulty and the horde's shares. `FloorScaling.Tiered` is one
`[MethodImpl(NoInlining)]` body; the scene reaches it through `TierHealth` / `TierDamage`, the Descent simulation
(`DescentSimulation.FoundryRun`: `SpawnWave`, the horde's spawn, the finale's `SpawnSwarmFoe`) calls it at the same
point with the same operands, so the two round the product alike (the stamina lesson). A multiplier of exactly 1 returns
the floor's own, so Forged is untouched. What the tier does not touch: the Warden's slam (20 % of the hero's maximum)
and the Regent's lanes (10 %), which take a share of the hero's health on every floor and never rode the floors'
multipliers; the pit's eruptions; the Mirror Sentry's reflection (the hero's own blows).

**Rewards.**
- **Gold**: `FloorScaling.Gold(base, modifier, tier)` - the floor's gold times the tier's reward, rounded once (away
  from zero, as the floor's is) - at each kill (`EncounterController`'s spawn) and each chest (`ChestSpawner`). The
  run's gold, the checkpoint's "Extract with N gold", the result's banked gold and telemetry's kill and chest gold all
  read the tier's. Per kill: a 3-gold grunt pays 4 on Tempered, 5 on Inferno, 6 on Abyssal; a 10-gold chest 13 / 15 /
  20.
- **Materials** (`LootRule.Roll` with the tier): the amount times the reward; a fraction is made whole by one word of
  the moment's loot stream drawn last (so the moment's other draws stay where they were): a floor clear's two Ember
  Shards are 2 or 3 (2.5 on average) on Tempered, 3 on Inferno, 4 on Abyssal.
- **Boosters**: each chance (parts of 10,000) times the reward, never above certain: a Seal from an elite 8 % -> 10 / 12
  / 16 %. Each chance still draws exactly one word, so the same moments drop the same items on every tier.
- **Items**: their chance is Forged's on every tier (the tier pays in luck): the rarity is drawn at the tier's item luck
  (`RarityTable.Draw(stream, 10 / 20 / 30)`, one word as at no luck). At +30 a Gold item is about 6 % of drops (3 % on
  Forged).
- The first floor-1 boss's armour is the profile's gift once, on whatever tier.

**Unlock and map starts** (`ProfileTiers`). A tier opens once a run on the tier below walks out of the Descent's finale
(`PlayerProfile.RecordDescentCompleted`, from `CombatSetup.OnFloorCleared` on the last floor when it is the finale,
floor 4 - not a development or proof floor that ends a run alone - before `Run.End(Victory)`). A walk-out from a deeper
start counts: a deeper start on a tier is open only once that tier reached it. Each tier keeps its own record of the
deepest floor cleared (`RecordFloorCleared(floor, tier)` raises it and the profile's own depth record), and a deeper map
is a start on a tier once a run on that tier reached it (`ProfileTiers.IsMapOpen` = `MapUnlock.IsOpen` with the tier's
record; docs/53). Forged's record is the profile's depth record at the least: every run before the tiers was a Forged
run (`Reconcile`), so a profile's Forged map cards read as they did, and a depth record through the finale (4) completed
Forged. No development exception: the owner sees the locks as a player does (the development start files still start a
floor without the panel, on Forged or the profile's tier).

### How the multipliers were found (exploration seeds 1-2000)

`DifficultyTiersResearch` (Tools/CombatChecks, explicit): each tier played by its matched cohorts - the clumsy, careful
and first-card autopilots wearing `FoundryGear`'s set at the tier's level (the hero's own weapon and a Gold armour,
amulet and ring; +9g with good efsun) - against the same cohort ungeared on Forged (`shipped`), both heroes, the whole
Descent with typical human hands, on the same seeds. The read is docs/56's: the change in four-floor completion in
points of rate, both heroes of a seed pooled, with `Tools/BalanceRule/paired_bootstrap.py`'s seed-level 90 % CI (it now
reads `difficulty-tiers.csv` and the first-card cohort). `FOUNDRY_PARALLEL=4`.

**Round 1, seeds 1-400** (section 8's values, and a harder Tempered to see the slope):

| Option (health / damage, gear) | clumsy | careful | first-card |
|---|---|---|---|
| Tempered 1.08 / 1.06, +5 | -0.25 (-2.88 to +2.38) | +1.25 (+0.12 to +2.50) | +0.12 (-2.00 to +2.12) |
| Inferno 1.15 / 1.13, +9 | +0.38 (-2.38 to +3.12) | +0.88 (-0.38 to +2.12) | -0.62 (-2.62 to +1.38) |
| Abyssal 1.20 / 1.18, +9g | -0.75 (-3.62 to +2.00) | +0.88 (-0.25 to +2.00) | -2.12 (-4.25 to +0.00) |
| Tempered 1.12 / 1.09, +5 | -5.00 (-7.88 to -2.25) | +0.00 (-1.25 to +1.25) | -5.62 (-7.75 to -3.50) |

Section 8's multipliers were matched to the sets' power almost exactly - every cohort within about a point of Forged -
and the slope is steep: 6.6 % more health times damage cost the clumsy and first-card cohorts five points. (Two harder
probes, Inferno 1.22 / 1.18 and Abyssal 1.28 / 1.25, were stopped unread once the slope was plain.)

**Why not ship "exactly matched".** A tier changes every enemy from floor 1 and the gear changes every crit and offer,
so a tier's paired runs diverge from the first wave: the paired change's SD per 400 seeds came out **12.7-13.5
completions for the clumsy cohort, about 10 for first-card and 6 for careful** (a floor-2 creature's: 6-8 for clumsy,
docs/56 section 2). At 3,200 seeds that is an SE of **0.60, 0.44 and 0.27 points**: a tier that changes nothing at all
fails the rule's lower bound (-1.5) about one time in five for the clumsy cohort alone. So the multipliers were set to
leave every gated cohort about a point ahead of Forged on these seeds - an easing the rule passes and reports - rather
than on its edge. Health stayed at section 8's (it sets the bosses' fights, and the sets' DPS meets it: below), and the
damage came down under the sets' own health.

**Round 2, seeds 1-2000** (the shipped `shipped` once; Tempered at two damages, chosen by a rule written before its
second read: the hardest whose weakest gated cohort is at least +0.75, else the easiest):

| Option (health / damage, gear) | clumsy | careful | first-card | Completed: clumsy / careful / first-card |
|---|---|---|---|---|
| Forged, ungeared (`shipped`) | - | - | - | 37.8 % / 95.9 % / 82.3 % |
| Tempered 1.08 / 1.04, +5 | +0.33 (-0.82 to +1.52) | +0.30 (-0.30 to +0.93) | +1.38 (+0.45 to +2.30) | 38.1 / 96.2 / 83.7 % |
| **Tempered 1.08 / 1.02, +5** | **+1.32 (+0.17 to +2.50)** | **+0.70 (+0.12 to +1.27)** | **+2.17 (+1.25 to +3.12)** | 39.1 / 96.6 / 84.5 % |
| **Inferno 1.15 / 1.10, +9** | **+0.75 (-0.42 to +1.95)** | **+0.97 (+0.42 to +1.52)** | **+1.62 (+0.72 to +2.55)** | 38.6 / 96.9 / 83.9 % |
| **Abyssal 1.20 / 1.14, +9g** | **+2.15 (+0.90 to +3.40)** | **+1.40 (+0.88 to +1.95)** | **+1.20 (+0.28 to +2.12)** | 40.0 / 97.3 / 83.5 % |

The shipped multipliers are in bold. Tempered at 1.04 left the clumsy cohort +0.33 (about a one-in-five chance of
failing the confirmation's lower bound); at 1.02, +1.32. Inferno at 1.10 met the +0.75. Abyssal at 1.14 has its weakest
cohort (first-card) at +1.20 and was not made harder: a step back towards 1.16 would put first-card near zero.

**The bosses** (P50 of the felled fights, seconds, Vanguard / Ilya, seeds 1-2000; the gate's band 45-65 s):

| Cohort | Tier | Warden (floor 1) | Regent (floor 2) | Warden (floor 3) |
|---|---|---|---|---|
| careful | Forged | 51.1 / 45.8 | 49.4 / 46.0 | 60.8 / 57.2 |
| careful | Tempered (+5) | 51.1 / 45.9 | 49.1 / 45.8 | 59.9 / 57.3 |
| careful | Inferno (+9) | 50.6 / 45.8 | 48.2 / 45.8 | 59.7 / 57.3 |
| careful | Abyssal (+9g) | 50.2 / 45.8 | 48.3 / 45.8 | 59.7 / 57.3 |
| clumsy | Forged / Tempered / Inferno / Abyssal | 47.0-48.2 / 48.5-49.2 | 45.6-47.0 / 48.2-48.7 | 60.2-61.8 / 61.4-62.6 |
| first-card | Forged / Tempered / Inferno / Abyssal | 48.2-49.2 / **44.1-44.2** | 47.4-48.0 / 44.8-45.3 | 55.9-57.0 / 53.4-54.1 |

Every boss holds its Forged length on every tier, within a second (the tier's health against the set's DPS: section 20's
+9g Warden at 44.9 / 40.5 s is back at 50.2 / 45.8). Every careful boss is inside 45-65 s on every tier. The one P50
under the band is first-card Ilya's floor-1 Warden, at 44.1-44.2 s on every tier - and at 44.2 s on Forged itself: the
tiers neither cause it nor fix it (as first-card Ilya's floor-2 Regent, 44.8-45.3 s, Forged's 45.3).

**The Descent's minutes** (P50 of the completed runs): Forged 14.37 / 14.40 / 14.21 (clumsy / careful / first-card);
every tier within **-0.07 to +0.01 minutes** of it (target +-1).

**What a run pays** (forge gold banked a run, mean, careful, rebuilt as `GearEconomyResearch` does with the tier's
gold): Forged 496 / 497 (Vanguard / Ilya), Tempered 618 / 622 (x1.25), Inferno 759 / 760 (x1.53), Abyssal 997 / 999
(x2.01).

### The confirmation (seeds 5201-8400)

The fresh block assigned to this phase, read once with the shipped multipliers above (303b28a7), the same research and
cohorts, both heroes, typical hands, `FOUNDRY_PARALLEL=4`; one candidate a tier, none changed after the read. Recorded
in docs/56's "Blocks used". The read was begun on 2026-11-17 and stopped by the machine's shutdown in its last tier
(Abyssal); it was read again from scratch on the same code on 2026-11-18/19 (5 h 45 m beside another agent's batches),
and the rows the stopped read had written - Forged, Tempered and Inferno, 57,600 runs - came back byte for byte.

docs/56's bar for clumsy, careful and first-card: the point change at least **-1.0** and the 90 % CI's lower bound at
least **-1.5** (`paired_bootstrap.py <folder> 5201 8400`; 6,400 runs a cohort):

| Tier (gear) | Cohort | Forged ungeared | Tier | Change (points) | 90 % CI | SD per 400 | Verdict |
|---|---|---|---|---|---|---|---|
| Tempered (+5) | clumsy | 2394 (37.4 %) | 2560 (40.0 %) | **+2.59** | +1.61 to +3.56 | 13.4 | passes; an easing |
| Tempered (+5) | careful | 6169 (96.4 %) | 6191 (96.7 %) | **+0.34** | -0.09 to +0.78 | 6.0 | passes |
| Tempered (+5) | first-card | 5246 (82.0 %) | 5345 (83.5 %) | **+1.55** | +0.78 to +2.30 | 10.3 | passes; an easing |
| Inferno (+9) | clumsy | 2394 (37.4 %) | 2452 (38.3 %) | **+0.91** | -0.05 to +1.88 | 13.3 | passes |
| Inferno (+9) | careful | 6169 (96.4 %) | 6204 (96.9 %) | **+0.55** | +0.09 to +1.03 | 6.3 | passes; an easing |
| Inferno (+9) | first-card | 5246 (82.0 %) | 5326 (83.2 %) | **+1.25** | +0.52 to +1.98 | 10.0 | passes; an easing |
| Abyssal (+9g) | clumsy | 2394 (37.4 %) | 2578 (40.3 %) | **+2.88** | +1.91 to +3.84 | 13.3 | passes; an easing |
| Abyssal (+9g) | careful | 6169 (96.4 %) | 6219 (97.2 %) | **+0.78** | +0.33 to +1.23 | 6.2 | passes; an easing |
| Abyssal (+9g) | first-card | 5246 (82.0 %) | 5306 (82.9 %) | **+0.94** | +0.19 to +1.69 | 10.2 | passes; an easing |

**All three tiers pass**, no cohort's point change below zero. Seven of the nine CIs lie wholly above 0: by docs/56 rule
3 these are easings, reported to the lead - the matched set plays its tier a little easier than an ungeared player plays
Forged, by design (the margin above); the largest are the clumsy cohort's on Tempered (+2.6) and Abyssal (+2.9), a
little over the exploration seeds' +1.3 and +2.2. The weakest reads are Inferno's clumsy (+0.91, lower bound -0.05) and
Tempered's careful (+0.34, lower bound -0.09), both well inside the bar.

**The bosses** (P50, seconds, Vanguard / Ilya, the careful cohort; the gate's band 45-65 s):

| Tier | Warden (floor 1) | Regent (floor 2) | Warden (floor 3) | Descent P50, completed (min) | Banked a run |
|---|---|---|---|---|---|
| Forged | 51.1 / 45.9 | 49.0 / 46.0 | 60.1 / 56.8 | 14.48 / 14.31 | 497 / 499 |
| Tempered | 51.1 / 45.9 | 48.9 / 46.1 | 60.0 / 56.8 | 14.48 / 14.30 | 619 / 622 |
| Inferno | 50.3 / 45.7 | 48.1 / 45.9 | 59.5 / 56.6 | 14.45 / 14.30 | 759 / 762 |
| Abyssal | 50.7 / 45.9 | 48.2 / 45.7 | 59.4 / 56.7 | 14.40 / 14.25 | 999 / 997 |

Every careful boss inside its band on every tier and within a second of Forged's; first-card Ilya's floor-1 Warden at
44.1-44.2 s on every tier as on Forged (44.1), outside the band before the tiers. The Descent's minutes, pooled, within
**-0.08 to -0.01** of Forged's for every cohort and tier (target +-1).

### The save: version 5

`ProfileSaveData.CurrentVersion` 4 -> **5**:

    public TierProgressSaveData[] tierProgress;   // a record per tier
    [Serializable] class TierProgressSaveData { string tier; int deepestFloorCleared; bool completed; }   // "forged", "tempered", ...
    public int selectedTier;          // kept from version 4: the tier chosen for the next run (0 Forged)
    public int highestTierCompleted;  // kept from version 4: now written from the records (0 none, 1 Forged, ...), read only by the migration

- **Migration** (`ProfileMigration`, case 4): every run so far was Forged's, so Forged's record is
  `deepestFloorCleared`, completed when that is 4 (the finale) or when version 4's `highestTierCompleted` counted it;
  the tiers above start empty; `selectedTier` stays (Restore lets only an open tier stand). Gold, gear and everything
  else untouched. The owner's profile (deepest 4) opens Tempered on its first load.
- **Repair on load** (`ProfileTiers.Restore`, never throws): unknown or empty tier ids dropped, a repeated tier's later
  records dropped (the first wins), negative depths zeroed, a completed tier raised to the finale, Forged raised to the
  profile's depth record; a tier that is not open (the tier below it not completed) keeps no record, so a damaged save
  never opens what was not earned; a chosen tier that is unknown or not open falls back to Forged.
- `ProfileGear` no longer holds the two tier fields it stored ahead of their build; `ProfileTiersSave` copies the fields
  to a `TierSnapshot` and back, as `ProfileGearSave` does the gear's.
- **docs/57's skills take version 6** (its section 11, item 9, now says so; its build plan's save row too).

### The UI

**The tier row on the hero and map choice** (`MapChoiceView`, docs/53). Between the hero row and the maps: four cards
(216 x 190 on the 1080-wide canvas), Forged, Tempered, Inferno, Abyssal, each with its name, "Rewards x1.25" and a state
line. Drawn with `ChoiceCardPainter` in the run's choice cards' look: the chosen tier lit in its own tier's card - the
cards' ladder, Forged green, Tempered orange, Inferno red, Abyssal gold (no crest: the gold clasp would stand into the
hero row) - "Selected" in gold; an open tier on the neutral card, "Tap to choose"; a locked tier on the neutral card,
not interactable (the disabled shade), its hint in the locked grey worded as a locked hero's: "Locked: Complete the
Descent on Forged". Under the row, two lines on the chosen tier: "Tempered: foes +8% health, +2% damage; rewards x1.25;
item luck +10 / Built for gear +15% (a +5 set). Your gear: +14.7%" - the chosen hero's own gear power
(`CombatSetup.GearPowerFor`, the weapon it would carry). A tier card chooses the tier and the profile remembers it
(`PlayerProfile.SelectTier`, as the hero); the map cards redraw for it (each tier its own record); the Foundry with the
scene's hero and tier begins the run there, and a tier other than the scene's reloads the scene with the map
(`RunStartRequest` carries the floor, the hero and the tier; `CombatSetup.Tier` takes the request's tier when open, else
the profile's). The panel is 1900 units tall with the hero row (was 1644): whole on the S23's 2340, scaled to 99 % on a
16:9 phone's 1920.

**The tier in the run.** Above Forged, the HUD's floor line begins with the tier's name in its colour ("Tempered  ·
Floor 1  /  Room 2/5"), the pause screen's run title reads "CRYPTFORGE  ·  Tempered", and the result's title "Defeated
· Tempered" (`TierText`); a Forged run's labels are exactly as they were.

**Capture.** `pwsh -File Tools/ArtReview/Run-TierUiProof.ps1` (installs `TierUiCapture.cs` for one PlayMode run, removes
it): 1080x2340 on a tr-TR culture, frames in `ArtDirection/2026-11-17/tier-ui-01` (untracked) with `frames.txt`: a fresh
profile (Forged lit, three locked), a geared profile that completed Forged (Tempered open), Tempered chosen (the deeper
maps locked on Tempered), that run's HUD and its result, and every tier open with Abyssal chosen. `MapChoiceView.FitNow`
fits the panel to the phone's safe area after the canvases are redrawn at its size, as `AnvilView.FitNow` does for the
Anvil (the batch run's own screen is small). Not yet run: the lead's.

### Telemetry

`run_start` writes `tier` ("tempered", "inferno", "abyssal") on a run above Forged, after the gear's keys; a Forged
run's line keeps its keys exactly (`RunTelemetryContext.Tier`). `DevicePull` reads it (`RunRecord.Tier`, null for Forged
and every older run) and leaves a tiered run out of the owner's floor rows, counted apart (`DeviceEvidence.TieredRuns`,
named in the gate's "left out" line), as a geared or deeper-started run already is.

### Tests

- **EditMode, also in CombatChecks: `DifficultyTierTests`.** Forged changes nothing - `TierHealth` / `TierDamage` /
  `Tiered(x, 1)` return the floor's multiplier to the bit, Forged gold is `Gold(base, modifier)`, chances and amounts
  are Forged's and draw nothing, and every Forged loot roll is the old roll word for word; the table (rewards, luck,
  matched power, ids, hints, each tier harder than the last); the enemies' layer on the floor's own (a Warden at 8.5 x
  1.6 is the tier's health more); gold per kill and chest; materials (x1.25 averages 2.5, x1.5 and x2 whole), boosters
  x1.25 / 1.5 / 2, items as often on every tier and rarer at the tier's luck, the satchel rolling on its run's tier; the
  unlock chain and the choice, map starts per tier, every run before the tiers Forged; the round trip through the save's
  fields and a damaged snapshot repaired; the version 4 -> 5 migration; the simulation's floor-1 Warden at the tier's
  health.
- **EditMode, Unity: `ProfileStoreTests`** - `TiersRoundTripThroughVersionFive` (the JSON keys, every record and the
  choice back), `AVersionFourProfileCarriesItsDepthToForged` (the owner's 39,491 gold and deepest 4: Tempered open,
  written back as 5; deepest 2 with a tier chosen ahead of its build: nothing opens, Forged chosen, the Gardens still
  open on Forged), `DamagedTiersInAReadableSaveAreRepaired`; the version strings moved 4 -> 5. `MapStartFloorTests` -
  the tier rides `RunStartRequest` once with its map; the tier line reads the same on tr-TR. `WeaponForgeTests` - the
  version is 5.
- **EditMode, also in CombatChecks:** `RunTelemetryTests.ARunOnATierSaysSoOnRunStart` (Forged's keys exactly, `tier`
  above it), `DevicePullTests.ATieredRunIsLeftOutOfTheOwnersFloors`, `ProfileGearTests` (the tier fields left the gear).
- **PlayMode: `TierChoiceFlowTests`** - a fresh profile: four tier cards, Forged selected, the three locked with their
  hints and refusing a tap and `StartOnMap`, every tier label fitting at its minimum size, Forged's floor line as it
  was; a profile that completed Forged: Tempered chosen and saved, the deeper maps locked on Tempered, the Foundry
  reloading on Tempered with every enemy at the tier's health and damage (`FloorHealthMultiplier`,
  `FloorDamageMultiplier`), the loot on the tier, the HUD's floor line and the result's title naming it; a Tempered
  profile whose runs cleared floor 1: the Vaults open on Tempered and the Gardens not, both open on Forged, the Vaults
  starting a Tempered run on floor 2 with its boss's health on the climb's.

      Unity, EditMode:  pwsh -File Tools/Run-UnityTests.ps1 -Platform EditMode -SkipApk -Filter "DifficultyTierTests;ProfileStoreTests;ProfileGearTests;WeaponForgeTests;MapStartFloorTests;RunTelemetryTests;RunLootTests;DescentTests;ClimbScalingTests;FoundrySimulationTests;GearApplyTests;AnvilTests"
      Unity, PlayMode:  pwsh -File Tools/Run-UnityTests.ps1 -Platform PlayMode -SkipApk -Filter "TierChoiceFlowTests;MapChoiceFlowTests;HeroAndMapChoiceFlowTests;RunResultTests;RunHudProgressTests;HudTextTests;LabelAllocationTests;TelemetryFlowTests;ChestPresentationTests;DescentFlowTests;FoundryDescentFlowTests;FoundryRunEndFlowTests;SimulationParityTests;GearRunStartTests;AnvilFlowTests;RelicForgeFlowTests;MultiRunSoak;PauseFlowTests"
      Capture:          pwsh -File Tools/ArtReview/Run-TierUiProof.ps1     (frames in ArtDirection/2026-11-17/tier-ui-01, untracked)

The whole EditMode suite is the safer read: `EncounterController`, `CombatSetup`, `PlayerProfile`, `LootRule`,
`RunLoot`, the save and `RunTelemetry` changed (each only where a tier above Forged is played, or where the save gained
its field). In PlayMode the flows above end a run, read the HUD's floor line, the result, the telemetry or the profile,
or play several runs on one profile (`MultiRunSoak`: a run walked out now completes Forged, which opens Tempered but
chooses nothing).

### Risks the build leaves open

- **Not run in Unity.** The compile check, CombatChecks and the batches only; the EditMode and PlayMode suites above and
  the capture are the lead's. The tier row is laid out by the numbers above, not looked at: four 216-wide cards hold a
  locked tier's three lines of hint at 22 units only by the generator's arithmetic (`TierChoiceFlowTests` asserts every
  tier label fits at its minimum size).
- **The panel grew to 1900 units** with the hero row; a 16:9 phone shows it at 99 %, and a shorter safe area (a 16:9
  phone with a notch) scales it further, the map cards' 300 shrinking with it.
- **A walk-out from a deeper start completes the tier.** A deeper start on a tier is open only once that tier reached
  it, so a player cannot skip a tier's floors 1-2 without having walked them once on it; but the tier's completion can
  come from a run begun on its Gardens. If the lead wants a whole Descent (floor 1 to the finale) for the unlock,
  `CombatSetup` records it only when `StartFloor` is 1 - a one-line change.
- **The rule's read is noisier for a tier than for a creature.** A tier changes every enemy from floor 1, and the gear
  changes every crit and offer, so the paired change's SD per 400 seeds is about 12.7-13.5 completions for the clumsy
  cohort and 10 for first-card (a floor-2 creature's: 6-8); at 3,200 seeds a cohort's SE is 0.60 points, so a tier that
  changes nothing fails the rule's lower bound about one time in five for the clumsy cohort. The tiers were tuned to
  come out a point or so ahead of Forged on the exploration seeds (an easing of that size passes and is reported) rather
  than on the rule's edge; on the fresh block they came out +0.3 to +2.9 ahead, seven of nine cells an easing. If the
  lead wants the tiers closer to "exactly matched", the damage multipliers are the knob (about 0.8 points of the clumsy
  and first-card cohorts per percent of health times damage), and a new candidate needs a fresh block.
- **The standing and auto-farmer cohorts were not read** (the rule gates clumsy, careful and first-card); the
  auto-farmer meets a tier only through its slow ladder (section 20).
- **Matched means the modelled set.** A real profile's +5 is rarely four +5 pieces with no lines; the line under the
  tier row shows the hero's own gear power beside the tier's, and the owner's device week (phase 7) will read who plays
  which tier with what (`run_start`'s `tier`, `gear_power`).
- **The owner's device rows thin out further.** Tiered runs are left out of the gate's owner rows as geared runs are; a
  tiered reading of the device (the tier's matched cohort against the owner's tiered runs) is not built.
- **Per-kill gold rounding.** A tier's gold is rounded at each kill: a 3-gold grunt pays 4 on Tempered (x1.33), a 5-gold
  one 6 (x1.2); over a run the reward lands near x1.25-1.3, x1.5 and x2 (the research's banked gold, above).

### The lead's decisions (2026-11-19)

1. **A walk-out from a deeper start completes the tier, as built.** A tier's deeper start opens only after that tier
   reached the floor, so its floors 1-2 have been walked on it once. Asking for a whole Descent again would only lengthen
   the grind.
2. **The easings are accepted.** A player in the matched gear finds a tier a point or two easier than an ungeared player
   finds Forged. That is the reward for the gear, and the tier's rewards pay for the time. The damage multipliers stay;
   phase 7's device week reads who plays which tier with what.
3. **The standing and auto-farmer cohorts** are read when the skills go live (docs/57 13.8), on the same fresh block as
   that read, so the two whole-Descent changes are seen together.
4. **The tier row's layout** is judged on the capture (`Run-TierUiProof.ps1`) after the merge, and with the Turkish
   strings of localisation pass 2.

### The regression line

Against main 408f95fa (its sources exported to a clean folder and run there), `FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0
FOUNDRY_COHORTS=1 FOUNDRY_REALISM=typical FOUNDRY_PARALLEL=4`, each run into its own empty output folder with the same
empty `FOUNDRY_EDITOR` and `FOUNDRY_DEVICE=none`, on this branch's tuned code (303b28a7, run 2026-11-18; the later
commits change docs only):
- `FOUNDRY_FLOORS=4`: `foundry-gate.md`, `foundry-batch.csv`, `foundry-batch-floor2.csv`, `foundry-cohorts.md`,
  `foundry-cohorts-floor2.csv`, `foundry-cohorts-floor3.csv`, `foundry-finale.md` and `foundry-finale.csv` match byte
  for byte; in `foundry-batch.md` only the clock in its header and the wall-seconds column differ.
- `FOUNDRY_FLOORS=2`: the same - every file byte-identical but `foundry-batch.md`'s clock and wall seconds.

CombatChecks: 929 green (`Başarılı`; 917 on main, 10 in `DifficultyTierTests`, one each in `RunTelemetryTests` and
`DevicePullTests`); the Unity compile check green, the capture fixture included (copied into PlayMode for the check and
removed).

## Measured and modelled: how to re-run

    dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~GearEconomyResearch"
      # FOUNDRY_SEEDS=100 (default), FOUNDRY_REALISM=typical; writes TestResults/gear-economy.md and .csv
    python Tools/Economy/gear_economy.py --players 2000
      # reads Tools/Economy/income-2026-11-16.csv (the run of 2026-11-16, seeds 1-100 a hero, copied from TestResults)
    python Tools/Economy/gear_economy.py --video
      # docs/58 section 4a: the result screen's gold video (watch rates, multipliers) on the tiers' payouts
    dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~GearedCohortsResearch"
      # section 20's geared cohorts: FOUNDRY_GEAR (all), FOUNDRY_SEEDS=200, FOUNDRY_REALISM=typical, FOUNDRY_PARALLEL;
      # writes TestResults/gear-cohorts.md and .csv (phase 5b's run of 2026-11-17, eight geared cohorts and the three
      # ungeared ones: FOUNDRY_PARALLEL=4, 12 minutes)
    dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~DifficultyTiersResearch"
      # section 21's tiers with their matched gear: TIER_OPTIONS (shipped,tempered,inferno,abyssal), TIER_CANDIDATES
      # ("tempered@1.10/1.08": a tier's set and rewards at other enemy multipliers, through the same body), TIER_COHORTS
      # (clumsy,careful,first-card), FOUNDRY_FIRST_SEED, FOUNDRY_SEEDS (200), FOUNDRY_PARALLEL, FOUNDRY_REALISM (typical);
      # writes TestResults/difficulty-tiers.md and .csv (a row a run, rewritten after each option). Seeds 1-2000, the four
      # options: 1 h 59 m at FOUNDRY_PARALLEL=4; seeds 5201-8400: 5 h 45 m beside another agent's batches.
    python Tools/BalanceRule/paired_bootstrap.py <folder> <first-seed> <last-seed>
      # docs/56's read of every FOUNDRY_OUT=<folder>/<name> under <folder>, difficulty-tiers.csv included; each read's own
      # folder (a candidate's option names are its multipliers; a shipped option's are not, so two rounds of `tempered` at
      # different multipliers must not share a folder)

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
