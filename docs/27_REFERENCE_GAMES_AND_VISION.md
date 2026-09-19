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
| S2a | **Stat sheet plumbing** (done 2026-09-19): six stats reachable by a card, pool unchanged so no number moves | each stat changes the thing that reads it, in scene and simulation alike | S1 |
| S2b | **The cards themselves** (done 2026-09-19): three tiers, luck, nine cards authored, five in the pool | a run's build differs from the next one's; and the pool's width is bounded by the run's length, measured | S2a |
| S2c | **A pool that grows with the run** (done 2026-09-20, with S4): all nine cards are authored in one order and `UpgradePool.WidthAt` takes as many as the run's level has opened - five until level twelve, one more every six after | Iron Skin, Swift Boots, Deep Lungs and Fortune are reachable by a run long enough to pay for them, and a Descent is offered the same five it always was, pinned seed for seed | S2b |
| S2 (was) | **Stat sheet**: `HeroStats` on the `ModifiableStat` pipeline (Range, MaxHP, Regen, Armor, MoveSpeed, Bar, Refill, CritChance, CritDamage, Luck), each an `UpgradeOption` target, mirrored in the simulation | a stat card changes the fight identically in both; standing baseline re-pinned where a stat moves it | S1 |
| S3 | **Books from chests** (done 2026-09-20): a chest keeps its reward and some hold a book as well, drawn by the seed and applied through the same service; the share is measured in `28`. The Descent simulation gained chests with it, which it had never had | a chest can now change a build, and the walk to one is worth taking; measured against mend and gold at six shares | S1, S2 |
| S4 | **Swarm room** (done 2026-09-20, `29`; measured first in `26` Part 6): `RoomKind.Swarm` with a spawn schedule (waves every *n* seconds, escalating, elites at marks), a survive-for-*T* clock, an alive cap of 10, and a checkpoint to leave early | XP per minute and damage per minute across weapons and routes; the budget's effect at real crowding | — (can run beside S1–S3) |
| S5 | **Forge Pulse and evolutions**: one behavioral upgrade with the trigger contract already agreed (every *N*th strike pulses; pulse damage is not a strike; crits can trigger it but it never crits; area strikes count once; pulse kills chain nothing; relic damage is not a strike), and an evolution at max stacks per weapon using Astra's existing ring and pulse art | the first synergies; Pulse should favour the Daggers, and if it favours the Sword the design is wrong | S1, S2 |
| S6 | **Floor length and difficulty**: three or four floors re-authored around swarm rooms, a Difficulty stat, floor targets of 4–6 minutes, a Descent of 15–20 | the GDD's 3–4 minute floors, finally; distributions per seed | S4 |
| S7 | **Meta tree**: banked gold into permanent nodes, respec, the Bones and Coins layer | the endgame Bones and Coins lacks | S1–S6 |

Why this order. S1–S3 give the player a build, which is the prompt's priority and Phase 2's list. S4 gives the game
pressure and length, which is what this session found missing and what no upgrade fixes; it does not depend on S1–S3,
so it can be measured in the simulation while they are built. S5 is the synergy layer and needs the stat sheet. S6 is
the re-authoring that turns rooms into maps and needs the swarm room to exist. S7 is meta and last.

**In the three days before Astra returns**: S1 and S2, with S3 if S2 lands early, and the S4 *measurement* in the
scratch harness without touching the game. Nothing in these needs new art.

**Visual needs to hand Astra**, none of them blocking: rarity on the choice cards, a book in the chest's opening frames
and whatever names the stack it grants (built 2026-09-20 and silent until then), a swarm clock and a "leave" prompt on
the HUD, a stat readout on the pause screen, and the evolution's presentation per weapon.

## 5. Decisions for the owner

1. BONK is Megabonk — confirm.
2. Books come from chests and cards from level-ups, two distinct reward moments, rather than books at level-up as
   Megabonk does it. Recommended: yes, it matches the owner's description and gives the chest a reason to exist.
3. The swarm room lets the player leave early with the XP earned so far, or stay for more — the Megabonk decision.
   Recommended: yes; it is the same shape as Extract/Descend and the game already has that checkpoint.
4. Run length target: 4–6 minutes a floor, 15–20 a Descent. Recommended as the S6 target; S4's measurement will say
   whether a swarm room can carry it.

## 6. S1 specification — the offer engine (agreed 2026-09-19)

The smallest missing slice. It changes nothing a player can see today, because the pool still holds two cards and the
game offers two; it is the foundation S2, S3 and S5 stand on, and it makes every offer reproducible from a recorded
number. Every existing pinned balance figure stays bit-identical by construction.

**Seed.** `RunState` carries an integer `Seed`, given at construction. The scene derives it in `CombatSetup` from a
development override when one exists — `<profile folder>/development/run-seed.txt`, read under the same rules as
`start-floor.txt` (Editor and development builds only, never throws, never logs) so two phone runs can be handed the
same offers — and otherwise from the profile revision, the run ordinal and the clock, hashed. It is recorded on
`run_start` as `seed`. The simulation takes a `seed` parameter, default 0.

**A random source of our own.** `RunRandom`, a pure SplitMix-style generator over 32-bit unsigned integers, integer
arithmetic only, so Mono and .NET cannot disagree. Streams are derived, not consumed in sequence:
`RunRandom.Stream(seed, purpose, index)` seeds a generator from all three, with `purpose` a fixed constant per consumer
(offers now, chests in S3) and `index` the consumer's own count. A chest opened between two level-ups therefore never
moves the next level-up's offer. `NextBelow(n)` is unbiased. Sample outputs are pinned by test.

**Offer rules.** An option is eligible when its stacks are below `MaxStacks` and, when it names a prerequisite, that
option has at least one stack. `UpgradeService.CreateOffer()` builds the eligible list in pool order; if it holds no
more options than the choice count it is offered whole, in pool order, consuming no randomness — which is today's game
and why no number moves — otherwise `choiceCount` distinct options are drawn without replacement from the stream
(seed, offers, offerIndex). `UpgradeOffer` carries its `Index`. Telemetry gains `offer_index` on `upgrade_offered` and
`upgrade_selected`. Rarity is not part of S1: a rarity that scales nothing is a lie on a card, and magnitudes belong to
S2's stat sheet where each card defines its tiers.

**Simulation.** `DescentSimulation.Run` keeps `cardSlot` and gains `preferIds`: when given, the first offered choice
whose id appears in the list is taken, so a policy such as "damage first" survives a pool larger than the choice
count. Parity tests pass the scene's `Run.Seed` into the simulation.

**Tests.** `RunRandomTests`: same inputs, same sequence; pinned sample values; streams with a different purpose or
index differ; `NextBelow` stays in range and is roughly uniform. `UpgradeTests`: over a synthetic pool of five and a
choice count of two, the same seed gives the same offers, different seeds differ, no offer repeats an option,
eligibility and stack limits hold, an eligible list no larger than the choice count is offered in pool order whatever
the seed, and a draw on the chest stream between two offers changes nothing. `RunTelemetryTests` re-pin the new fields.
`DevelopmentStart` tests cover the seed file. Every existing Descent and parity test passes unchanged.

**Out of S1.** New cards, rarity, stats beyond Damage and AttackSpeed, chests, and anything visible.

## 7. S3 specification — books from chests (agreed 2026-09-19)

The chest is the game's other reward moment and today it holds nothing a build cares about: a quarter of the hero's
health on even rooms, ten gold on odd ones. S3 lets a chest carry a **book** as well, which is an `UpgradeOption`
granted from a second source rather than a second system, exactly as section 2 decided. It is the small slice chosen
ahead of S4, and it also closes a gap that has been in the project since routes existed: the Descent simulation has
never modelled chests at all, so a walking hero heals in the scene and does not in the simulation.

**A book is additive, not a replacement.** A chest keeps the reward it has, and some chests hold a book on top of it.
Three reasons. The heal on room one is load-bearing for the pinned balance claims and taking it away would move every
number the project has measured. `ChestReward.Kind` stays Heal or Gold, so the chest's existing effects code needs no
change and cannot regress - the art owner adds the book's own presentation later. And a book that costs the player
nothing but the walk is the point: it gives the movement budget something to buy, which `26` Part 5 left it without.

**Which chests hold one.** `ChestRule.HoldsBook(chestIndex, seed)` draws from `RunRandom.Stream(seed, Chests,
chestIndex)`, where `chestIndex` counts chests placed across the whole run, not rooms within a floor. The share is a
single constant, `ChestRule.BookPercent`, measured rather than chosen: a Descent places about twelve chests against
roughly seven level-ups, so a share of one hands a build more from chests than from levels, and a share of zero is the
game as it stands. The measurement reports survival and build width per share and the constant is set from it.

**What a book gives.** `UpgradeService.TryGrantBook(chestIndex)` draws one option, uniformly, from the same eligibility
the level-up offer uses - below its stack limit, prerequisite held - and one tier from `RarityTable` at the run's luck,
both from `RunRandom.Stream(seed, Books, chestIndex)`, a fourth purpose so the "does this chest hold a book" draw and
"which book" draw cannot correlate. It applies the modifier through the same two branches `TrySelect` uses, counts a
stack, records an applied upgrade, and raises `BookGranted`. It grants nothing when nothing is eligible.

One race is worth naming rather than defending against: a chest cannot open while a choice panel is up, because an open
choice freezes time and `ChestSpawner` ignores a frame of zero length, but a level-up granted earlier in the same frame
leaves a one-frame window in which a book could fill a card the open offer still shows. Taking that card would put it
one stack past its limit. The alternative - hiding offered cards from the book draw - makes the draw depend on whether
an offer happens to be open, which is true in the scene and never true in the simulation, and that is a parity
divergence rather than an edge case. One stack over the cap on one card, once, is the cheaper cost.

**Telemetry.** `chest_opened` gains `book` (the option id, or none) and `book_rarity`. Nothing else moves.

**Simulation.** `DescentSimulation` places a chest in every combat room on `ChestRule.SpotFor`, checks
`ChestRule.IsWithinReach` once a frame after the hero has moved and the pack has stepped, and applies the same three
effects through the same calls. `RouteView` gains the chest's spot and whether it is still closed, so a policy can
decide to fetch it; the scene's PlayMode driver fills the same fields from `ChestSpawner`. `Result` gains
`ChestsOpened` and `BooksTaken`. A standing hero never reaches a chest, so every pinned balance number is unchanged by
construction, and the parity tests that walk now cover the chest as well.

**Tests.** `RunRandomTests`: the books stream differs from the chest stream at the same index. `ChestRuleTests`: the
share is what it claims over many chests, the same seed gives the same chests, and a reward keeps its kind.
`UpgradeTests`: a book respects stack limits and prerequisites, grants nothing when the pool is full, and a book drawn
between two level-ups leaves both offers untouched. `DescentSimulation` tests: a walking hero opens the chest it walks
onto, and a standing run is bit-identical to what it was. `SimulationParityTests`: a routed Descent that opens chests
matches the scene. `RunTelemetryTests` re-pin `chest_opened`.

**Out of S3.** A book's own art and any HUD for it (Astra's, and listed in the visual needs above), a separate book
pool distinct from the card pool, books that offer a choice rather than granting, and chests that cost gold.

**What was built, 2026-09-20.** All of the above, with the share set to 25 % from the sweep in `28`: the lowest share at
which every measured run found at least one book and a hero who fetches them can still die. Two things the
specification did not foresee. The simulation had never modelled chests at all, so a walking scene hero healed where the
simulation's did not - a parity gap that had been open since routes existed and is now closed, with a scene case that
walks onto a chest. And a book has no presentation yet: a chest that holds one still shows only its heal or its coins,
so the stack arrives silently. That is Astra's to draw and is on the visual-needs list; until then the telemetry names
it and the pause screen's stat readout will show it.

## 8. S4 and S2c specification — the swarm room and a pool that grows with the run (agreed 2026-09-20)

`26` Part 6 measured a swarm before it was built and came back with three things that fix this slice's shape. A swarm
grants seventy-plus level-ups where five cards hold twenty-two stacks, so **S2c is a dependency of S4 rather than a
follow-up**. The arena caps alive enemies at ten, so past the twelfth wave the only way a swarm can keep hardening is
for each enemy to harden, and a flat multiplier fails both ways - at 1.0 an area weapon never dies, at 1.5 every weapon
dies at wave seven - so the ramp has to **rise with the wave**, which the floor format cannot express. And the movement
budget barely touches a player who moves only when threatened, even in a crowd, so a swarm is where fetching a chest
stops being free.

### S2c — the pool widens with the run, rather than all at once

S2b measured a nine-card pool and it collapsed the game: standing cleared one Descent in ten, kiting a third to a half.
The reason is dilution - a Descent grants about eleven level-ups, and every card past a point thins the chance of the
one the hero needs. That measurement stands, so the four held-back cards cannot simply be added.

What changes instead is that the pool is **a function of the run's level rather than a constant**. Cards are authored in
one order - Tempered Edge, Quickened Grip, Long Reach, Keen Edge, Vital Surge, then Iron Skin, Swift Boots, Deep Lungs,
Fortune - and `UpgradePool.WidthAt(level)` says how many of them are eligible, starting at the five that ship today and
adding one card every so many levels up to all nine. A Descent that reaches level eleven sees close to the pool it sees
now; a swarm that reaches level seventy sees all nine and has somewhere to put them. The thresholds are measured against
two claims at once: the Descent's standing clear rate must not fall below what S2b pinned (7, 6 and 10 of 24 seeds), and
a swarm must reach the full nine.

### S4 — the swarm room

**A swarm room is a room whose waves do not run out.** `RoomKind.Swarm` authors its waves as a **ladder**, the ordinary
`WaveDefinition[]` every combat room already has: wave one is the ladder's first, and once the ladder is spent its last
wave repeats for as long as the player stays. That is exactly the shape `26` Part 6 measured - the pack grows from three
to the ten the formations hold over about twelve waves and then cannot grow again - and it needs no new content type.

**The ramp is a per-wave multiplier.** `SwarmRule.DifficultyAt(ordinal)` is 1.0 on the first wave and rises with the
wave; it multiplies the floor's own enemy health and damage multipliers, so it composes with `FloorScaling` rather than
going round it. The rate is the measured constant of this slice, held against the Staff, which is the build that breaks
a swarm: at a flat 1.0 an area weapon cleared all eighty waves in twelve of twelve seeds, so a ramp that leaves the
Staff immortal has not ramped.

**Leaving is a choice, and it is the choice the game already has.** After each swarm wave the room offers **Press on** or
**Leave** through `CheckpointService`, the same service, the same `ChoiceKind.Checkpoint` and the same panel that asks
Extract or Descend after a boss. Two new `CheckpointKind` values and no new UI: the visual treatment a swarm deserves - a
wave counter, a clock - is Astra's and is on the visual-needs list. Offering it after every wave is the plainest reading
of "leave when you like" and is the thing to ask the owner about after playing it.

**`FloorProgress` gains endless rooms.** It is built today from a wave count per room; a swarm room is marked endless,
`Advance()` keeps handing out waves with a rising ordinal and never clears it, and `LeaveEndlessRoom()` is what the
Leave choice calls to let the floor move on.

**Where a swarm room lives, for now.** Nowhere in the authored Descent. Putting one into Ember Halls or Quicksilver
Vaults would move every pinned balance number in the project, and S6 is the slice that re-authors floors. S4 ships a
**development swarm floor** reached through `development/start-floor.txt`, the same hook the density proof floors use,
so it can be measured and played on the phone without touching the Descent. S6 decides where swarm rooms belong.

**Simulation.** `DescentSimulation` mirrors all of it: endless rooms, the ladder, the per-wave multiplier, and a leave
policy given as a parameter (leave after N waves) so a measurement can ask what N is worth. `Result` gains the deepest
swarm wave reached.

**Telemetry.** `swarm_wave` with the ordinal and the difficulty at it; the checkpoint event names the two new kinds.

**Out of S4 and S2c.** Any swarm room in the authored Descent (S6), a clock or wave counter on the HUD (Astra), elites
at marks, shrines, and a Difficulty stat the player can raise (S6).

**What was built, 2026-09-20** (`29`). All of the above. The ramp is 2 % a wave, measured: flat leaves a backing-off
hero immortal in half of forty-eight runs, and anything from 5 % up cuts the room down to what a Descent already grants.
The pool's first extra card arrives at level twelve, one above the longest run the game has, so the Descent is
bit-identical seed for seed. Two things the specification did not foresee. A swarm room has **one chest**, because
chests are per room, so S3's books barely reach it - the median swarm run took none, and chests that arrive with the
waves are a gap for S6. And the exit is offered after every wave, which is one constant and a question about feel that
only playing it can answer.

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
