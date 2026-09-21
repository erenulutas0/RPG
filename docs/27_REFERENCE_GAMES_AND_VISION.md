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
| S5a | **Forge Pulse** (done 2026-09-21, `30`): one behavioural upgrade on the trigger contract agreed here (every *N*th strike pulses; pulse damage is not a strike; crits can trigger it but it never crits; area strikes count once; pulse kills chain nothing; relic damage is not a strike), shipped at one stack, radius 2.2, every fourth strike | **the claim below was measured and failed.** The Pulse was to favour the Daggers and instead favours the Sword at eleven of twelve settings, across two ways of scaling it: a per-strike burst inherits the weight of a strike, and the Daggers strike smallest. It shipped because it fixes a different real hole - the Sword is three times weaker than the Staff in a swarm - and because the Daggers need a card that keys on their crit rhythm instead, which is not this one | S1, S2 |
| S5b | **Evolutions, and what they cost** (done 2026-09-21, `32`): a card at its stack limit and a run past a Descent's length unlock an evolution, and every evolution raises a run-level `Difficulty` that scales enemy health and damage | **both claims answered.** Free evolutions send the Staff to the 200-wave cap wasting 59 levels; at 0.15 a run still goes 19 % deeper, so the second claim failed there; **0.20** is the smallest cost that satisfies it and takes wasted levels to zero. Taking *every* evolution now ends slightly shallower than taking none, which makes *some* the right answer - a build decision | S5a |
| S5c | **Splintering Crit** (done 2026-09-21, `31`): a critical strike also strikes every enemy within 2.0 units of its target for a share of its crit damage, gated on the Daggers' every-third-strike rhythm | **the claim held.** With crits held equal across the weapons, the Daggers gain most in nine settings out of nine - seven to ten times what the Sword gains, fifty times what the Staff gains - and the ordering does not turn on the radius. It takes them from 18 waves to 55 against the Staff's 67, so they are a choice rather than the best | S5a |
| S6 | **Floors below, swarm on top** (reframed 2026-09-21, decision 5): three or four floors re-authored as the descent to a single finale swarm after the last boss, the floors carrying the difficulty and the reasons to move, a Difficulty stat, floor targets of 4–6 minutes, a Descent of 15–20; the ramp re-measured against a hero entering at level eleven | the GDD's 3–4 minute floors, finally; distributions per seed; a finale whose depth is a score | S4 |
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
5. **The swarm is the finale, not a farm** (the owner, 2026-09-21, after playing it). Sections 2 and 8 above framed the
   swarm room as the place a run earns its experience; the owner's reading is Megabonk's actual structure - the Final
   Swarm that opens when the run's normal window ends, where leaving is extracting and depth is the score. The room
   itself is unchanged by this. What changes is where it goes and what it is for: it stands at the end of the last
   floor, after its boss, optional and endless; the floors below it are where a build is made and where the difficulty
   has to live; and the levels a swarm grants past a full build should turn into something - gold, or a deepest-wave
   record on the profile for S7. Two consequences for the measurements. `29`'s ramp was measured with a hero entering at
   level one; a finale is entered at level eleven with a build, so the ramp has to be measured again against that hero,
   and the target is not "it kills" but "depth is felt". And the open question about a bigger map belongs to the
   floors: an arena is the right shape for an endless crowd, a map with reasons to cross it is the right shape for the
   descent to it.
6. **The finale's difficulty grows exponentially** (the owner, 2026-09-21, after the evolution session). Linear escalation
   cannot catch a multiplicative build; the owner asked for the Megabonk shape, where staying is a real bet and even a
   very strong build has a last minute. Section 12 orders the work: calibrate first, then the ramp.
7. **In the finale, level-ups are offered together, not one at a time** (the owner, 2026-09-21, after the first run
   of the continuous swarm). A run that levels every three and a half seconds opened a card panel as often, and 27 %
   of its time went to choosing. Offered two ways - fewer levels, or the same levels shown less often - he chose the
   second: every card is still earned, and the swarm holds them back and offers them in one sitting every
   `SwarmRule.OfferEverySeconds` (`36`).
8. **The finale's crowd hardens with time, not with kills** (the owner, 2026-09-21, the same session). The ladder was a
   queue read one spawn at a time, so a hero who killed fast met the heaviest wave's Warden in every ten enemies within
   half a minute. Each of the pack's places now has a role in each wave, and the wave is the room's clock
   (`SwarmRule.LadderSeconds`): what stands on the floor at any moment is what that minute calls for (`36`).

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

## 9. S5a specification — the Forge Pulse (agreed 2026-09-21)

S5 holds two mechanisms that only share a purpose: a **behavioural upgrade**, which is a card that changes what an attack
does rather than a number it carries, and an **evolution at max stacks**, which is a weapon becoming another weapon. They
are built apart. This specifies the first, S5a; the evolution is S5b and the phone session argued for it - a swarm fills
all thirty-six stacks by wave thirty-three and every level after that is wasted, and an evolution is what a full card
turns into.

**The Pulse rides the stat pipeline rather than a second system.** `UpgradeStat.PulseDamage` is the share of the weapon's
damage a pulse deals, zero on every weapon until a card grants it, and `WeaponRuntime` holds it as a `ModifiableStat`
like the four it already holds. So stacking it is stacking a number, rarity tiers work unchanged, and the simulation
mirrors it by mirroring one stat. No new eligibility rule, no new offer kind, nothing that reads an offer changes.

**The contract, as agreed in the roadmap and unchanged by this specification.** Every *N*th strike pulses. Pulse damage
is **not a strike**: it advances no cadence, counts toward no rhythm and cannot pulse again, so a pulse never chains and
a kill it lands starts nothing. A crit **can** be the strike that triggers a pulse, but the pulse itself **never crits**
and its damage is never multiplied. An area strike counts **once** however many enemies it hits, because the strike is
the swing and not the landing. Relic damage is not a strike, which holds already: relics call `ApplyDamage` and never
`TryAttack`.

**The pulse is centred on the hero, not on the target**, over `PulseRule.Radius` floor units. That is what makes it a
short-reach weapon's card: the Daggers fight from 1.8 units and strike every half second, so they stand inside their own
pulse and trigger it twice as often as the Sword; the Staff already owns the crowd and gains least. The roadmap's test of
this design is that the Pulse favours the Daggers, and that if it favours the Sword the design is wrong - so the
measurement reports all three weapons and the claim can fail.

**Where the card sits.** Tenth in the authored order, behind Iron Skin, Swift Boots, Deep Lungs and Fortune, so
`UpgradePool.WidthAt` only opens it to a run that has levelled well past a Descent. Nothing about the Descent moves -
five cards at level eleven is five cards whatever stands behind them - and the Pulse arrives roughly where the phone
session ran out of build. Whether that is too late is a tuning question the measurement informs and the owner settles.

**Two constants, both measured** in the swarm room against all three weapons: how often a strike pulses
(`PulseRule.Every`) and how far it reaches (`PulseRule.Radius`), with the card's magnitude per tier read off the same
sweep. The claim to beat is that the Pulse changes which weapon goes deepest.

**Scene and simulation.** `AttackController` gathers the enemies within the pulse radius of its owner through the same
`Targeting.CollectNear` the Forge Burst uses, and hands them to `TryAttack` beside the splash list; `DescentSimulation`
gathers them with `WithinReachOfHero`, which is that call's mirror and already exists. `WeaponRuntime.Pulsed` is raised
for whoever draws it later. Telemetry counts pulses on `run_end`.

**Out of S5a.** The evolution at max stacks (S5b), any art for the pulse - it is silent, like a book, and is on the
visual-needs list - a second behavioural card, and any change to the three weapons' authored patterns.

**What was built, 2026-09-21** (`30`). The mechanism, whole, with its seven contract clauses each pinned by a test. The
design claim attached to it failed and is recorded rather than rewritten: the Pulse is the Sword's card. Two things worth
carrying forward. A per-strike effect cannot be made a light weapon's card by tuning its radius or its scaling, so the
Daggers' answer has to key on their every-third-strike crit rhythm and is a separate card. And the Sword's swarm baseline
- 15.5 waves against the Staff's 45 - is a balance hole nobody had measured before; the Pulse covers it by accident, and
covering a hole by accident is worth naming so that S6 fixes it on purpose.

**Answered 2026-09-21.** The crit-keyed card was built as S5c and it works (`31`): the Daggers gain most at every one of
nine settings, and unlike the Pulse the result is not sensitive to the radius, because the gate is in the weapon
definitions rather than in a number. Taken together, `30` and `31` say something worth keeping: a behavioural card aimed
at a particular weapon has to be gated on something that weapon owns in its own definition. Scale an effect to damage and
it belongs to the heaviest weapon whatever else you do to it.

## 10. S5c specification — Splintering Crit, the Daggers' card (agreed 2026-09-21)

`30` closed the Forge Pulse with a falsified claim and a clear reason: a per-strike burst inherits the weight of a strike,
so it belongs to the weapon with the biggest strike, and no radius or scaling changes that. The Daggers are still the
weakest weapon in a swarm - twelve and a half waves against the Staff's forty-five - and what they lack there is not
damage but **area**: a single-target weapon cannot clear ten enemies pressing in, however fast it swings. This card is the
answer the measurement pointed at, keyed on the one thing the Daggers own outright.

**The gate is a crit, and crits are not evenly distributed.** The Daggers' authored pattern crits **every third strike**;
the Sword and the Staff author no rhythm at all and crit only from the chance Keen Edge grants. So the Daggers arrive
already critting a third of their strikes, twice a second, having spent nothing, while the Sword must buy three stacks of
Keen Edge to crit a quarter of the time at two thirds of the cadence. That is a structural edge rather than a tuned one,
which is exactly what the Pulse lacked.

**What it does.** `UpgradeStat.CritSplash` is the share of a **critical** strike's damage dealt to every other living
enemy within `CritSplashRule.Radius` of **the target**. Centred on the target, not on the hero, which is what makes it a
different card from the Pulse rather than a reskin of it: the Pulse is a drumbeat round the hero, this is a crit that
splinters where it lands.

**The contract**, in the same shape as the Pulse's and for the same reason - it is written down before the code so a later
change cannot quietly reinterpret it:

- Only a **critical** strike splinters, whether the crit came from the pattern's rhythm or from a rolled chance.
- A splinter is **not a strike**: no cadence, no rhythm, it cannot splinter again, and a kill it lands starts nothing.
- A splinter **never reports itself critical**, so a view that flashes crits flashes the strike and not its spray.
- It is dealt off the **critical** damage, unlike the Pulse, which reads the plain damage: the card is about crits, so it
  should grow with a build that invests in them.
- An **area** weapon's splash and a splinter are separate: the Staff's splash already hit those enemies, and both land.
  That is deliberate and is the thing the measurement has to watch, because it is how this card could turn out to be the
  Staff's instead.
- Relic and ability damage are not strikes and cannot splinter, which holds by construction.

**Where the card sits.** Eleventh in the authored order, behind the Forge Pulse, so `UpgradePool` opens it only to a run
that has levelled far past a Descent. Nothing about the Descent moves.

**Two constants, measured** in the swarm room against all three weapons, with a control arm that holds neither behavioural
card: the radius and the share, with the card's stack limit read off the same sweep. **The claim, stated so it can fail:
Splintering Crit must take the Daggers deeper, in relative terms, than it takes the Sword or the Staff.** If it favours
either of them, it is the wrong card for the same reason the Pulse was, and `30`'s lesson will have been that a strike-
scaled effect cannot be aimed at a light weapon at all.

**Out of S5c.** Weapon evolutions (S5b), art for either behavioural card - both are silent - a third behavioural card,
and any change to the three weapons' authored patterns.

**What was built, 2026-09-21** (`31`). All of it, at radius 2.0 and one stack, with six contract clauses each pinned by a
test. Two things to carry into S5b. The two behavioural cards were measured **separately**, and `UpgradePool` opens both
by level forty-two, so a finale can hold a Pulse and a splinter at once and nothing has measured that. And three
mechanics are now invisible - the book, the Pulse and the splinter - which is the largest thing waiting on Astra.

## 11. S5b specification — evolutions, and what they cost (agreed 2026-09-21)

The phone session of 2026-09-21 measured the hole this slice exists to close. Fifty-five swarm waves, fifty-one levels,
**thirty-eight** of them spent: the nine core cards were full by wave thirty-two, and from wave forty-one to wave
fifty-five the upgrade panel never opened again. Thirteen level-ups produced nothing at all, because nothing was eligible.
A room whose whole purpose is to pay for a long stay stops paying two thirds of the way through it.

**But the same session measured the opposite problem, for the third time running.** The owner left at 102 health, losing
2.1 health a wave; at that rate the swarm needed another fifty waves to kill him. Every session has ended with him
choosing to stop. So a slice that only adds power would close one hole by digging the other deeper, and this
specification refuses to do that.

### An evolution is a card whose gate is a full card

`UpgradeOption` gains `EvolvesFromId`. A card that names one is offered only when the card it names is **at its stack
limit** - a stronger and more meaningful gate than `RequiresId`, which asks for a single stack. Everything else is reused:
pool order, rarity tiers, the seeded draw, the stack limits, the one eligibility list that serves both level-ups and
books.

**An evolution is not gated by `UpgradePool.WidthAt`.** The width takes cards from the front of the authored order, and a
run that reached level fifty-one only opens twelve of them; evolutions sitting at positions twelve to fourteen would never
arrive. Their gate is their base card being full, which a run cannot reach early anyway, so the width has nothing to add
and is deliberately not applied to them.

### Every evolution makes the world harder

`UpgradeStat.Difficulty` joins the sheet `HeroStats` holds - Megabonk keeps Difficulty on the character sheet for the same
reason - starting at 1.0 and multiplying **enemy health and damage** wherever the floor's own multipliers are applied, in
the scene and in the simulation alike. Taking an evolution raises it by `EvolutionRule.DifficultyPerStack`. Not as a
second stat on the card, which the one-stat model cannot carry, but as a property of being an evolution: the service that
applies the stack applies the cost.

That is what makes an evolution a **choice** rather than a reward for having played long enough, and it is the first thing
in the project that pushes back when the hero grows. It is also the honest shape for a finale whose depth is the score: a
player who evolves goes deeper per wave and meets a harder wave for it.

### What is authored

Three evolutions, on the three cards every measured run fills first: Tempered Edge, Quickened Grip and Vital Surge. Each
is a stronger version of the same stat with its own stack limit, so the balance question this slice asks is
one-dimensional and can be answered by measurement rather than taste. Fifteen extra stacks against thirteen wasted levels
is deliberate: a little more room than the hole, not a lot.

**Behavioural evolutions - a weapon changing its pattern, the Daggers' direct hit becoming a cleave - are not in this
slice.** They need the art the roadmap promised them and they are a second design question. The mechanism built here is
what they will use.

### What is measured

`EvolutionRule.DifficultyPerStack` and the evolutions' magnitudes, in the swarm room against all three weapons, against
the same backing-off player, with the control arm holding no evolutions. Two claims, stated so they can fail:

1. **Evolutions must close the hole**: a run that fills its build must keep being offered cards afterwards, and the count
   of level-ups that produce nothing must fall to near zero.
2. **Evolutions must not remove the ceiling further**: a run that takes every evolution must not go deeper, relative to
   its own control, than a run that takes none. If depth rises with every evolution taken, the difficulty cost is too
   small and the number is wrong.

The second is the one that matters, and it is allowed to send this slice back.

**Out of S5b.** Behavioural evolutions and any art, a Difficulty the player can raise on purpose (S6), evolutions for the
other six core cards, and any change to where the behavioural cards of S5a and S5c sit in the order - that is its own
small slice, and the phone session says it is needed.

**What was built, 2026-09-21** (`32`). The mechanism, the Difficulty stat, three evolutions, and the cost at 0.20. Two
things the specification did not foresee, both caught by pinned tests rather than by reading. A full base card alone let
an evolution into the **Descent**, because eleven level-ups spent on one card fill a five-stack card - so an evolution
needs the level gate as well, and the Descent is bit-identical again. And the measurement exposed a limit that now applies
to everything measured since `29`: **the scripted back-off player is about twice as fragile as the owner.** He wasted
thirteen levels at wave fifty-five; the policies waste zero with two weapons out of three, because they die at waves
twelve and fifteen. Every constant set against them is a floor rather than an answer, and re-calibrating the policy to his
measured depth is worth its own slice before S6 tunes anything else.

**Four silent mechanics now**: the book, the Pulse, the splinter and Difficulty. A card that says "and a harder world"
gives the player no way to see how much harder the world became.

## 12. The finale, reshaped, and the map below it — plan agreed 2026-09-21

Four phone sessions have now said the same thing in four ways: the owner leaves the swarm, it never kills him. The last
one said *why*. With every evolution taken the world stood at 4.5 times its authored strength by wave thirty-two, and his
health rose through it from 58 to 191. The world grows by **adding** - two per cent a wave, a fifth per evolution - and the
hero grows by **multiplying** - damage times cadence times crits. A linear ramp cannot catch a multiplicative build at
any rate; raising the rate only moves the moment it loses. That is a question of **shape**, not of a number.

The owner compared it to Megabonk's Final Swarm, which does three things this room does not: difficulty that rises
**exponentially with time**, enemies that **never stop** coming, and an exit that is **a place on the map** you walk to
through the crowd. He approved the exponential ramp (decision 6, below) and asked for the rest to be planned now, with the
art that two of them need arriving with Astra.

### The order, and why it is this order

**S6a — calibrate the measurement to the owner.** Every constant since `29` was set against a scripted back-off player,
and four sessions say that player is about half as durable as the owner: the policies die at waves twelve to forty-five,
he left at fifty and fifty-five. Anything measured against it is a floor rather than an answer, and an exponential ramp
tuned against a player half his strength would be tuned for nobody. The telemetry holds his runs wave by wave - health,
level, every card he took - and each run's seed. So his exact build can be **replayed** in the simulation, which isolates
the one thing the simulation gets wrong. Pure measurement; nothing in the game changes.

**Done 2026-09-21 (`33`), and the hypothesis first written here was wrong.** This paragraph said the thing the simulation
gets wrong is how he moves. Replaying his builds exactly, a back-off player with a slightly wider release fits his health
within six to ten points a wave; the simulation was never wrong about moving. It was wrong about **choosing** - every
measurement since `29` took the card in slot zero, and he takes evolutions, then damage, then cadence, every time. The
calibrated player is his card ranking plus that movement, and with the Staff it goes about half as deep again as the
reference player the ramp and the evolution cost were set against. Replayed with his own builds, his runs would have died
at waves fifty-eight to ninety-six; he left each with that margin in hand.

**S6b — the exponential ramp.** `SwarmRule.DifficultyAt` becomes geometric - each wave a fixed factor harder than the one
before - and the factor is measured against the calibrated policy. The target is not "it kills" but the Megabonk shape:
the strongest measured build dies somewhere, a weaker build dies clearly earlier, and the gap between them is the score.

**Done 2026-09-21 (`34`), at 1.02 a wave.** Four criteria were written before the sweep. Against the calibrated player,
1.02 takes the Staff's median from 53.5 to 41 and the runs past wave eighty from nine in a hundred and forty-four to one,
and leaves the early game within noise of where it was; 1.025 took that last run too but made the first fifteen waves
measurably deadlier (z = 2.11), and 1.03 and above are walls. Two criteria were not met as written and are recorded as
such: the ceiling was stated as a maximum, which grows with the sample, and the early-game test as a strict inequality
that a non-significant difference of four runs misses.

**S6c — a continuous swarm and a walked exit.** The wave goes: as enemies die, others enter, up to the ten the arena holds.
The per-wave question goes with it - it was a third of the owner's last session spent in menus - and leaving becomes a
**place**, like a chest: the hero walks onto it through the crowd, and the movement budget finally prices an escape. The
rule is gameplay and is built here with the exit at a provisional spot; how the exit looks is Astra's.

**Done 2026-09-21 (`35`).** A step of three seconds and a respawn of two, measured at the end of the Descent rather than
at level zero - where no setting met the criteria, because a level-zero Sword or Daggers dies inside a minute in a crowd
that never clears whatever the clocks say, and decision 5 says the finale is never met that way. The Staff's median is
202 seconds, the ninety-fifth percentile 236, two runs in 139 die in the first minute. The exit stands five units toward
the far corner, and its distance turned out not to be the lever: at three, five and seven units a hero who decides by
ninety seconds always gets out and one who decides at four minutes almost never does. The measurement also found that a
stand-in blind to the exit left by accident in thirteen runs of 144, just by stopping to fight on it; the measuring player
now avoids it as a player who sees it would, and until Astra draws it a real player can make the same mistake.

**S6d — the map below.** The descent to the finale, re-authored around reasons to move: rewards on the ground (experience
and gold the hero walks to collect, so a kill is no longer the only source of either), and **shrines** - places that grant
a buff in exchange for difficulty, the same bargain an evolution makes, chosen on the map rather than on a card. Then the
map's size, which the owner raised, measured once there is something on it worth crossing. Ground rewards are recommended
first: they give the hero a reason to move everywhere rather than to one spot, and they are pure simulation.

**First part done 2026-09-21: the finale behind the last boss.** `Floor_FinalSwarm` holds the proof floor's ladder and
multipliers - the room `35` and `36` measured at the end of the Descent - and Quicksilver Vaults leads to it, so the
Warden's fall no longer ends the run: its checkpoint offers Extract, which banks everything as the old victory did, or
Descend, which secures the Descent's gold and walks into the finale with only what the finale pays at risk. Walking out
of it is the victory. The owner had asked whether it should wait for a strength threshold; measured (`36`), the
threshold is one floor's worth of cards and the second floor adds nothing against the swarm, so the end of the Descent
is past it with a floor to spare. That second finding is the brief for the rest of S6d.

### S6c specification — a continuous swarm and a walked exit (agreed 2026-09-21)

Two changes to the finale, one rule each, and the owner chose the faithful version of the first over a cheaper one. The
cheap version kept waves and removed the gap between them; it would have left a two-second lull every time a fresh pack
walked in from five units out. The faithful version removes waves.

**The pack never ends.** A swarm room holds one pack for its whole life, at most the ten the arena holds. It opens with
the ladder's first wave, as it does now, and it never clears: when an enemy dies its place waits `SwarmRule.RespawnSeconds`
and a fresh enemy walks in, placed by the same `EntrySides` rule a wave uses. Which enemy is the ladder read as a queue -
the second wave's enemies after the first's, then the third's, through the twelfth, whose list then repeats for as long
as the room lasts - so the crowd hardens in composition exactly as the ladder always meant it to. How many stand at once is
the size of the ladder wave the queue has reached: three at the start, ten by the time the queue is into the twelfth.

**The ramp runs on the clock.** With no waves there is no wave ordinal to harden by. `SwarmRule.DifficultyAtSeconds` reads
the seconds the hero has spent in the room and applies the geometric ramp of `34` one step per `StepSeconds`, and each
enemy takes the difficulty of the moment it spawns - so an enemy that entered early stays the enemy it was, and the crowd
around a hero who stays keeps getting worse. The owner's swarm waves took a median of seven seconds including the choice
panels (296 intervals across four runs); with the panels gone the step is the constant this slice measures, against the
same criteria `34` used, with the calibrated player of `33`.

**Leaving is a place.** `ExitRule` puts an exit on the arena's far side, well beyond the chest, and the hero leaves by
**standing on it** for `ExitRule.DwellSeconds`. A second, not a touch, so a hero kiting across it by accident does not end
its run - and a second of standing still in a crowd is exactly the price the owner described: to take what the finale paid
you, you have to walk through it and hold your ground. The per-wave question goes, all of it: the prompt, the event that
opened it and the two checkpoint kinds it used. It was a third of the owner's last session spent in menus.

**The simulation mirrors all of it**, on the same frames: the pack, the refill, the queue, the clock, the exit. It also
needs a player who decides to leave, because a scripted player otherwise never does: a leave-after-*T*-seconds policy,
after which the route walks to the exit, holds it, and either leaves or dies trying. That is the measurement the exit
exists for - how the cost of leaving grows the longer a hero stays.

**Constants.** Two measured, two fixed with their reasons stated. `StepSeconds` is measured against `34`'s four criteria.
The exit's distance is measured against one criterion written now: **a calibrated hero who decides to leave early in the
finale almost always gets out, and one who decides late often does not** - leaving has to become a real bet, or the exit
is a menu with extra walking. `RespawnSeconds` is fixed at one second, because continuity is the point and anything much
shorter only collapses into "always ten". `DwellSeconds` is fixed at one second for the reason above.

*As built (`35`), three things differ from this specification and are recorded rather than rewritten above:*
`RespawnSeconds` was measured with the step, because the first sweep showed it shapes the opening as much as the step
does, and ships at two seconds; the hold counts only while the hero is **on the exit and not moving**, because the first
build had a kiting hero leave twelve seconds in by running across it; and the movement bar fills once, as the room opens,
since there are no wave starts left to refill it.

**Out of S6c.** The exit's art and any swarm clock (Astra), a swarm room anywhere in the authored Descent (S6d), and any
change to the ladder itself.

### What is Astra's, and waits for her

A difficulty readout - four mechanics are silent now, and Difficulty is the one the owner said he could not feel - the
swarm's clock, the exit, shrines, ground rewards, and marks for a book, a pulse and a splinter. None of S6a to S6c waits on
her: each is a rule that can be measured before it can be seen.

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
