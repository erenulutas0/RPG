# 63 - The weapon roster: each hero its own weapon, a shared pool, three slots (2026-11-23)

Phase 1 of the owner's weapon request. Built and on by default: the full-slot rule's tests (section 4), the forge's
weapon rack removed (section 5) and Ilya's unlock rule (section 6). Built **behind a research lever that is off by
default** (`FoundryLevers.WeaponRoster` in the simulation, `development/weapon-roster.txt` in the scene): the roster of
sections 2-3 and the two shared weapons of section 7. With the lever off, everything a player meets is today's game and
the default batch is byte-identical. Section 9 holds the measurement on the exploration seeds 1-2000 (docs/56 rule 1);
section 10 why no candidate is ready for a confirmation read, and the next round's two.

**The owner's request (2026-11-22, translated from Turkish):** "Like Megabonk: weapons distributed to characters, each
character with its own weapon; characters pick 3 weapons as they level up, and once they have 3, only those 3 appear as
weapon options."

**Megabonk, as the lead checked it on the web (2026-11-22):** each character starts with one unique weapon and a passive;
weapon slots start at 2 and grow to 4 through meta progression; there are about 29 weapons. **We keep three slots,
fixed** (docs/40 section 5.1): no slot meta-progression.

## 0. The short version

- **Signature weapons.** Each hero's starting weapon is its own, and **no other hero ever finds it as a card**: the
  Vanguard's Sword, Ilya's Staff; planned, the Riftblade's Daggers, the Bastion's Iron Halo, the Voltwright's Arc
  Conduit, the Sanguine's Sanguine Sickle (docs/40 section 4, docs/44 section 2). The rule is one line of pure code: a
  weapon is a signature exactly when it is a roster hero's starting weapon (`WeaponRoster`).
- **A shared pool** every hero draws from: today the Daggers (until the Riftblade ships, section 3.3), and from docs/40
  section 5.2 the **Magma Mortar** and the **Forge Bow**, built behind the lever (section 7). Later: Cinder Saw, Gravity
  Anvil, Ember Mines (section 3.4).
- **Three slots, fixed, and the full-slot rule:** slot 1 is the signature weapon, slots 2-3 are picked from the shared
  pool as the hero levels; once three are held, no level-up, bonus offer or chest offers a new weapon - only levels of
  the three, beside the tomes and the general cards. It was already the code's rule; its tests could not see it (section
  4).
- **The forge's weapon rack is gone:** every hero is bound to its own weapon, so the rack sold nothing (section 5).
- **Ilya's unlock** stays "Clear The Foundry with the Staff" with today's catalogue, where the Vanguard can take the
  Staff as a card (97-100 % of its floor-1 clears carry it); under the roster no card can hand it over, and the clear
  alone unlocks (section 6).
- **Measured (section 9): the roster as asked fails docs/56's rule by 9-18 points a cohort, and the whole cost is the
  Vanguard's** (careful 96 % -> 60 % of full climbs; Ilya 96 % -> 98 %). The Staff did two thirds of the Vanguard's damage;
  under the roster only the Mortar holds a crowd, it is held in about 55 % of runs, and the runs without it almost never
  finish. Stronger Mortar numbers, an area-first pilot and a Sword sweep each close part of the gap, none enough. No
  candidate is ready for a confirmation read; section 10 proposes the next round's two (a crowd weapon guaranteed, or a
  second shared crowd weapon).

## 1. Where the weapons stood on 2026-11-22

- Three hero weapons (`Assets/_Project/Data/Weapons`): Sword (Cleave), Staff (Area), Daggers (DirectHit, a crit every
  third strike, double damage to elites). The other nine assets there are enemy weapons.
- Every hero starts with its own weapon only: Ilya its Staff since 2026-10-29, the Vanguard its Sword since 2026-11-22
  (`HeroDefinition._startsWithItsOwnWeaponOnly`, removed on 2026-11-23 with the rack: section 5).
- On the foundry a run builds by category (docs/42): slot 0 is the starting weapon, and every weapon of the catalogue
  (`CombatSetup._weapons`: Sword, Staff, Daggers) can come as a card, so the Vanguard picks up the Staff and the Daggers
  and Ilya the Sword and the Daggers. Three slots, three weapons: every careful run ends holding all three.
- So "choose three" chose nothing: with three weapons a full loadout is the whole catalogue. A roster bigger than three
  is what makes the choice, and the full-slot rule, real.

## 2. The rules

1. **Three weapon slots, fixed** (docs/40 section 5.1; Megabonk's 2-to-4 meta growth is not taken). Slot 0 is the
   hero's signature weapon, always; slots 1 and 2 fill from the cards.
2. **A signature weapon is its hero's alone.** It is never offered to another hero, not as a new weapon and so never as
   a level. The hero's own is in its catalogue for its levels and its evolution.
3. **The shared pool** is every hero weapon that is no roster hero's signature. Each hero's catalogue is its signature,
   then the shared pool, in the authored order (`CombatSetup._weapons` then `_rosterWeapons`; the simulation the same).
4. **The full-slot rule.** With three weapons held the new-weapon kind closes; its offer share goes to the other kinds
   (`OfferWeights`); weapon cards are levels (and the evolution) of the three held. Every bonus offer (an altar's, an
   Outworks anvil's, the forge's) is the same offer, and a chest's book never hands a weapon (docs/42 section 6, the
   owner's 2026-10-26 decision).
5. **Tomes stay global** (three tome slots, eight levels), so a shared weapon picked up at minute five gets every tome
   level taken before it (`WeaponLoadout.AddGlobal`).

The rule in code (`Assets/_Project/Scripts/Progression/WeaponRoster.cs`), one body for the scene's `CombatSetup` and the
simulation's `FoundryRun`:

```text
Offers(own, weapon, signatures) = weapon == own  or  weapon not in signatures
signatures = { hero.StartingWeapon.Id for every hero on the roster }    // CombatSetup._heroes; the simulation's RosterSignatures
own        = the weapon the run starts with (HeroWeapon: the hero's own, or development/start-weapon.txt's)
```

## 3. The roster

### 3.1 Signature weapons

| Hero | Signature | Status | Behaviour today / planned | Notes |
|---|---|---|---|---|
| Vanguard (Edda Vale) | **Sword** | shipped | Cleave: 18 / 0.8 s (category base), the nearest second enemy within 2 of the target for 60 %; x1.5 on the gate boss (WardenRule) | Lines: +2 damage, +12 % area, +0.3 reach. Evolution with Force: Resonant Edge (a pulse every 4th strike). |
| Cinder Arcanist (Ilya Kest) | **Staff** | shipped | Area: 24 / 1.1 s, every enemy within 3.5 of the target for 75 % | Lines: +2 damage, +12 % area, +8 % attack speed. Evolution with Reach: Cinder Nova (kills burst). Ember Memory lights every hit of every weapon Ilya carries. |
| Riftblade (Nix Vane) | **Daggers** | planned (docs/46; simulated only) | DirectHit: 7 / 0.3 s, a crit every third strike x2, x2 on elites | In the shared pool until the Riftblade ships (section 3.3). |
| Bastion (Orr Bramm) | **Iron Halo** | planned | docs/40: 11 / 0.65 s, pulses round the hero (+12 % radius, +8 % guard) | A pulse round the attacker exists (PulseRule); the guard does not. |
| Voltwright (Tess Coil) | **Arc Conduit** | planned | docs/40: 16 / 0.85 s, chains between targets (+1 bounce, +8 % shock) | Needs a chain target rule. |
| Sanguine (Serel Morrow) | **Sanguine Sickle** | planned | docs/40: 21 / 1.0 s, bleed and lifesteal | Needs a heal on hit. |

### 3.2 The shared pool

| Weapon | Status | Behaviour | Category base | Lines (green; x1.25 / x1.5 / x2 rare, epic, gold) | Evolution hook | Visual / effect |
|---|---|---|---|---|---|---|
| **Daggers** | shipped; leaves with the Riftblade | DirectHit, reach 1.8, crit every 3rd x2, x2 on elites | 7 / 0.3 s | +1 damage, +5 pp crit, +8 % attack speed | Rift Echo with Precision (crits splinter) | the double slash and the crit star |
| **Magma Mortar** | **built, lever** | Area: lobbed at reach 4.0; the target and every enemy within 2.5 of it take the full blast (fraction 1.0) | 34 / 1.8 s | +6 damage, +15 % area (blast radius), +8 % attack speed | proposed: Magma Pool with Force (a burning pool where it lands: SigilZone and BurnRule for any hero) | an ember shell streak dropping onto the target and an ember floor ring at the blast's radius |
| **Forge Bow** | **built, lever** | DirectHit at reach 5.0, a crit every 4th shot x2; Precision's chance on top | 25 / 1.2 s | +4.5 damage, +7 pp crit, +0.5 reach | proposed: Piercing Volley with Precision (a crit pierces the line behind its target) | a hot tracer from the hero to the target, the hit spark, the crit star |
| Cinder Saw | later | a saw orbiting the hero: hits all within a short radius every 0.2 s | 5 / 0.2 s | +0.8 damage, +10 % duration, +8 % bleed | - | needs an aura strike (the pulse's circle round the attacker, on every strike) |
| Gravity Anvil | later | Area at the target with a knockback | 42 / 2.0 s | +7 damage, +12 % size, +10 % knockback | - | needs a knockback on the pack (PackMotion) |
| Ember Mines | later | mines laid on the ground that burst when an enemy steps on them | 28 / 1.5 s | +5 damage, +15 % area, +12 % duration | - | the first weapon with objects: pooled, a capped count (docs/42's projectile rule, section 7.3) |

docs/40's other designs are signatures (Iron Halo, Arc Conduit, Sanguine Sickle). Every number in this table is a
starting value for the simulation (docs/40 section 5.2), not a decision; section 9 measures the two built ones.

### 3.3 The Daggers' transition

The Daggers are docs/46's Riftblade's signature. Until the Riftblade ships, no roster hero starts with them, so the rule
of section 2 puts them in the shared pool with no special case. The day the Riftblade's `HeroDefinition` joins
`CombatSetup._heroes` (with the Daggers as its starting weapon), the Daggers become a signature by the same rule and leave
the Vanguard's and Ilya's catalogues; nothing in `WeaponRoster` changes. The simulation's `RosterSignatures` gets the
Daggers' id on the same day (a scene-value test holds it to the scene's heroes). Then the shared pool is the Mortar and the
Bow alone - two weapons for two slots, no choice left - so **a third shared weapon must ship with the Riftblade** (Cinder
Saw is the cheapest: section 3.2).

### 3.4 What a larger roster asks for

Each new shared weapon: a `WeaponDefinition` with its three lines (a Damage line Flat, docs/42), the simulation's
`HeroWeapon` and lines (WeaponContentTests holds them equal), the strings (docs/60), an icon and its socket icon
(`Tools/ArtReview/build_upgrade_icons.py`), its strike in `CombatEffectsView` (by weapon id, `EffectArt.StrikeFor`), and a
measured read (section 9). A weapon whose behaviour the combat code lacks (an aura, a chain, a knockback, mines) also
needs its rule in `Combat` as one pure body for the scene and the simulation.

## 4. The full-slot rule, verified

- **Code.** `UpgradeService.WeaponOpen`: a new weapon is open while a slot is free and the hero does not carry it; a
  carried one's upgrade while it is below level 8 or its evolution waits. `CreateCategoryOffer` draws a kind from
  `OfferWeights` over the kinds still open, so with three held the new-weapon kind's share goes to the others.
  `TryGrantCategoryBook` draws only tome levels and general cards. Every other source of a weapon - an altar, an Outworks
  anvil, the forge's upgrade - grants a level-up (`RunState.GrantBonusUpgrade`) answered by the same offer. The scene and
  the simulation build the same `UpgradeService`.
- **The gap was in the tests.** With three weapons in the catalogue a full loadout holds them all, so
  `OfferCategoryTests.NoNewWeaponIsOfferedOnceThreeSlotsAreHeld` passes with the slot check removed. `FullSlotRuleTests`
  hands the service five weapons: no fourth weapon from level-ups and bonus offers over 30 seeds, both extra weapons
  reachable before the slots fill, full offers still three cards, no book ever a weapon. With the slot check removed all
  three fail. The roster's own tests repeat it on the roster's catalogue (section 8).

## 5. The forge's weapon rack is gone (built, on)

With every hero bound to its own weapon the rack's only card was the hero's own, "Equipped", and two painted empty
places. Removed on 2026-11-23:

- **Code.** `WeaponShop` and its flow (`PlayerProfile.TryForgeWeapon`, `EquipWeapon`, `UnequipWeapon`;
  `CombatSetup.Weapons`, `FollowNextHero`; telemetry's "weapon" forge spend; the result screen's weapon hint, now relics
  alone; `HeroDefinition._startsWithItsOwnWeaponOnly`).
- **UI.** The forge's weapons' place shows the next run's hero's own weapon, read only: its name, what it does and
  "Signature weapon" (TR "İmza silahı"), under the header "Weapon: the hero's own" (TR "Silah: kahramanın kendi
  silahı"), following the hero chooser (`CombatSetup.NextWeapon`). Gameplay.unity lost Weapon Cards 2 and 3 (42 objects,
  no reference left) and the relics' header and cards moved up two card places; the band they leave above the start
  button is empty. The Anvil's weapon slot reads the same next weapon.
- **Save.** `PlayerProfile.OwnedWeaponIds` and `EquippedWeaponId` stay in the profile and the save, unchanged and read by
  no run (the arena's build still reads them, `ArenaProfileBuild`). No migration, no version bump
  (`ProfileStoreTests.TheRetiredRacksWeaponsSurviveALoadAndASave`). A player who bought the Staff or the Daggers keeps the
  record; the gold is not refunded (owner question 2).

## 6. Ilya's unlock

Ilya unlocks by clearing The Foundry carrying the Staff (`HeroDefinition._unlockWeapon`, docs/45). With the Vanguard
bound to its Sword the Staff can only come as a card. **With today's catalogue that still works:** in the default batch
(seeds 1-200, typical hands) a Vanguard clearing floor 1 carries the Staff in 195 of 195 careful clears, 187/187
standing, 182/182 first-card, 76 of 78 clumsy and 10/10 auto-farmer.

**Under the roster** the Staff is Ilya's signature and never offered to the Vanguard, so the condition can never be met.
The closest fair rule keeps everything but the impossible part: **the clear alone unlocks** when the run's cards could
never have handed the weapon over (`HeroUnlock.UnlocksOnFoundryClear(carried, cardsCouldOfferIt)`; `CombatSetup` asks
whether this run's catalogue holds it). With today's catalogue the rule is the one it always was; in the hall arena (the
legacy engine, no catalogue) the weapon must still be carried. When the roster ships, the hint becomes "Clear The
Foundry" (TR "Dökümhane katını temizle") and `_unlockWeapon` can go.

## 7. The two shared weapons built

### 7.1 Why these two

The two that best fit what the combat code already does, so each is data plus a strike effect, with no new combat rule:

- **Magma Mortar** is `WeaponBehavior.Area` - the Staff's path, every enemy within the splash radius of the target - at a
  long reach, a slow cadence and the full blast (fraction 1.0). The ring is the Staff's floor ring, tinted ember. It is
  also the Vanguard's area weapon once the Staff is Ilya's alone: without it the roster's Vanguard would hold the Sword
  and the Daggers, two single-target weapons, on a horde floor.
- **Forge Bow** is `WeaponBehavior.DirectHit` with the crit rhythm (the Daggers' path) and the rolled crit chance a tome
  or a line grants (`WeaponRuntime.RollsCritical`), at the longest reach in the game. It gives Ilya, whose Staff and
  Ember Memory are all area, a single-target weapon for the elites and the gate boss, and every hero a weapon that keeps
  striking while kiting.
- The others need a rule the code lacks: Cinder Saw an aura strike, Gravity Anvil a knockback, Ember Mines objects
  (section 3.2).

### 7.2 What was built

| | Magma Mortar | Forge Bow |
|---|---|---|
| Asset | `Data/Weapons/Weapon_MagmaMortar.asset`, id `weapon_magma_mortar` | `Data/Weapons/Weapon_ForgeBow.asset`, id `weapon_forge_bow` |
| Behaviour | Area, reach 4.0, blast radius 2.5, fraction 1.0 | DirectHit, reach 5.0, crit every 4th x2 |
| Base | 34 / 1.8 s (legacy and category alike) | 25 / 1.2 s |
| Lines (`Data/Upgrades/WeaponLines`) | `line_magma_mortar_damage` +6 flat; `line_magma_mortar_area` +15 %; `line_magma_mortar_attack_speed` +8 % | `line_forge_bow_damage` +4.5 flat; `line_forge_bow_crit` +7 pp; `line_forge_bow_range` +0.5 |
| Shares | 1 on the gate boss and the elites | 1 and 1 |
| Evolution | none yet (hook: Magma Pool, section 3.2) | none yet (hook: Piercing Volley) |
| Strike (`CombatEffectsView`, `EffectArt.StrikeFor(id, behaviour)`) | an ember shell streak falling onto the target and the Staff's floor ring tinted ember at the blast's radius (Reach and the area line widen it) | a hot tracer from the hero to the target (`EffectArt.TracerFrames`), stretched along the shot and fading in 0.12 s; the crit star on a crit |
| Icon (`UI_UpgradeIcons.png`, `build_upgrade_icons.py`) | a squat iron mortar with an ember shell; its lines the mortar small with the stat's mark | a recurve bow with a glowing forged arrow; its lines likewise |
| Strings (docs/60) | "Magma Mortar" / "Magma Havanı" | "Forge Bow" / "Ocak Yayı" |

The shared math is the existing bodies: the splash and area radius are `WeaponRuntime.SplashRadius` (one non-inlined body,
the area factor applied once), every reach comparison squares the radius into `ArenaFloor.FloorDistanceSquared`'s float as
the targeting always has, and the crit roll is `RollsCritical`'s. Nothing new is restated in the simulation but the two
weapons' numbers, which WeaponContentTests holds to the assets.

### 7.3 docs/42's projectile rule

docs/42 section 4: "no projectile objects in V0.1". **Both weapons respect it:** each is an instant hit - the damage lands
on the strike's frame, as the Sword's and the Staff's do - and the shell and the arrow are drawn, not simulated: a pooled
sprite in `CombatEffectsView`'s strike pool (24 shared slots), alive for 0.12-0.2 s, with no collider, no update of its
own and nothing the simulation must follow. The first weapon that needs objects is Ember Mines (section 3.2); it should
come with the exception argued then: a pooled, capped count, stepped by the encounter as the pit's eruptions are.

## 8. The lever

- **Simulation:** `DescentSimulation.FoundryLevers.WeaponRoster` (off). On, a run's catalogue is
  `DescentSimulation.RosterCatalogue(own)`: the five hero weapons (Sword, Staff, Daggers, Magma Mortar, Forge Bow)
  filtered by `WeaponRoster` with `RosterSignatures` (the Sword and the Staff). Off, it is `WeaponCatalogue()` as shipped.
- **Scene:** `development/weapon-roster.txt` (any text but "off"; the Editor and a development build only):
  `CombatSetup` adds `_rosterWeapons` (Magma Mortar, Forge Bow) to `_weapons` and filters them with `WeaponRoster`
  against `_heroes`' starting weapons. `development/start-weapon.txt` may also name a roster weapon, so a scene test can
  start a run with it.
- **Research only:** `FoundryLevers.RosterPool` and `RosterSignatureIds` hand the roster another pool or other signatures
  (section 9's decomposition and candidates), and `WeaponRosterResearch`'s `pref-area` reading uses `FoundryLevers.Picker`.
- **Byte-identical default.** With the lever off nothing the default batch reads changed (section 9.1).

## 9. Measurements (exploration seeds 1-2000)

### 9.1 The default batch, the lever off

`FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_REALISM=typical FOUNDRY_FLOORS=2`, filter
`FullyQualifiedName~FoundryBatchReport.TheFoundryFloorOverASeedBatch`, on the merged base (b525281c), on the rack's
removal (77f660bf) and on the roster's commit (4775964c), each from a clean snapshot: every CSV byte-identical
(foundry-batch.csv, foundry-batch-floor2.csv, foundry-cohorts-floor2.csv), and the three reports identical once the two
things that change from run to run are masked (the header's timestamp and the floor-time table's wall-clock seconds).

### 9.2 The roster as built, against today (seeds 1-2000, all five cohorts, typical hands, the whole climb)

`WeaponRosterResearch` (`ROSTER_OPTIONS=shipped,roster`, 40,000 runs). Completion: the finale walked out of. The change
in points is paired on the seeds, its 90 % CI from `Tools/BalanceRule/paired_bootstrap.py` (10,000 resamples of seeds,
both heroes of a seed together):

| Cohort | Shipped | Roster | Change (points) | 90 % CI | SD per 400 seeds |
|---|---|---|---|---|---|
| careful | 95.9 % | 79.0 % | **-16.88** | -17.93 to -15.82 | 11.4 |
| standing | 81.6 % | 68.5 % | **-13.10** | -14.43 to -11.78 | 14.3 |
| first-card | 82.2 % | 63.9 % | **-18.25** | -19.55 to -16.95 | 14.3 |
| clumsy | 37.6 % | 28.9 % | **-8.75** | -10.32 to -7.17 | 17.4 |
| auto-farmer | 0.9 % | 0.8 % | -0.10 | - | - |

**It fails docs/56's rule by a wide margin, and the whole cost is the Vanguard's.** By hero (completion, change):

| Cohort | Vanguard shipped | Vanguard roster | Ilya shipped | Ilya roster |
|---|---|---|---|---|
| careful | 96.0 % | 60.0 % (-36.1) | 95.7 % | 98.0 % (+2.4) |
| standing | 81.0 % | 48.3 % (-32.7) | 82.2 % | 88.7 % (+6.5) |
| first-card | 77.5 % | 36.4 % (-41.2) | 86.8 % | 91.5 % (+4.7) |
| clumsy | 26.4 % | 6.3 % (-20.1) | 48.9 % | 51.5 % (+2.6) |

Floor 1 alone tells the same story: the careful Vanguard clears it in 70.8 % of runs under the roster against 98.5 % today.

**Why.** Today the Vanguard's damage is Ilya's Staff's: of the weapons' damage in the Vanguard's runs that held it, the
Staff dealt **64.8 %**, the Sword 29.8 %, the Daggers 5.9 % (all cohorts but the auto-farmer). Under the roster the Staff
is gone, and of the three shared weapons only the Mortar fights a crowd:

| Hero, roster | Sword / Staff (own) | Daggers | Magma Mortar | Forge Bow |
|---|---|---|---|---|
| Vanguard: held | 100 % | 59 % | 55 % | 58 % |
| Vanguard: share of the weapons' damage among holders | 29.7 % | 13.2 % | **55.2 %** | 27.5 % |
| Ilya: held | 100 % | 65 % | 60 % | 63 % |
| Ilya: share among holders | 53.9 % | 6.2 % | 39.0 % | 25.6 % |

The Vanguard's runs that took the Mortar completed **65.5 %** (4,384 runs); the runs that were offered it and passed it
**2.6 %** (2,725 runs). The Sword, the Daggers and the Bow hold no crowd on the horde floor. The autopilot takes new weapons
in the order the offer shows them (the first of equal scores, `AutopilotCohort.Pick`), so it passes the Mortar in two
runs of five that see it; with the Staff in the pool there was no wrong pick to make, since the Staff and the Daggers were
the only two to take.

Ilya gains a little: it loses the Sword (a quarter of its damage today) and takes the Mortar and the Bow instead; its own
Staff and Ember Memory already hold the crowd.

**No weapon dominates in the sense of crowding the others out.** The Mortar's 55 % of the Vanguard's damage is less than
the Staff's 65 % today; the Bow takes a quarter wherever it is held; the Daggers are the weak pick for both (6-13 %) and
were already today (6 %). Pick rates are the autopilot's order, not a preference: each shared weapon is offered in about
90 % of runs and held in 55-65 %. What does dominate is the **need**: a Vanguard without an area weapon cannot finish.

### 9.3 Follow-up reads (seeds 1-1000, the four docs/56 cohorts)

Each reading's design reason was written in `WeaponRosterResearch` before it was read. Paired change against `shipped`
on the same seeds, points, 90 % CI; the roster's own row on these seeds for comparison:

| Option | What | careful | standing | first-card | clumsy |
|---|---|---|---|---|---|
| roster | as built | -16.25 (-17.75 to -14.75) | -11.35 (-13.15 to -9.55) | -17.85 (-19.75 to -15.95) | -10.15 (-12.45 to -7.80) |
| exclusive | each hero's own weapon, no new weapon (Vanguard: Sword + Daggers) | -50.65 | -45.80 | -43.45 | -15.40 |
| open | the two new weapons for both, no exclusivity (five weapons each) | -6.70 (-7.95 to -5.45) | -1.85 (-3.60 to -0.10) | -5.35 (-7.00 to -3.70) | +1.35 (-0.95 to +3.60) |
| pref-area | the roster, the pilot taking the Mortar first among new weapons | -10.45 (-11.85 to -9.05) | -5.80 (-7.55 to -4.05) | -17.85 (unchanged: it scores nothing) | -7.90 (-10.25 to -5.55) |
| c-mortar15 | the roster, the Mortar's blast at 3.0 every 1.5 s | -12.20 (-13.70 to -10.70) | -3.80 (-5.75 to -1.85) | -7.55 (-9.60 to -5.50) | -2.40 (-4.90 to +0.05) |
| c-sweep | the roster, the Vanguard's Sword sweeping (2.5 round the hero, 60 %, reach 2.5; simulation only) | -14.00 (-15.40 to -12.55) | -10.95 (-12.75 to -9.15) | -14.65 (-16.55 to -12.65) | -8.70 (-11.00 to -6.35) |
| c-sweep-m15 | c-sweep and c-mortar15 together | -10.35 (-11.80 to -8.90) | -3.25 (-5.15 to -1.35) | -5.75 (-7.75 to -3.70) | -1.65 (-4.15 to +0.85) |

The Vanguard alone (completion, careful / clumsy; shipped 96.0 % / 28.1 % on these seeds, the roster 60.7 % / 7.2 %):
exclusive 2.8 / 0.1, pref-area 72.1 / 9.2, c-mortar15 68.4 / 19.0, c-sweep 65.2 / 10.1, c-sweep-m15 72.1 / 20.5, open
80.0 / 20.5. Ilya never loses: careful 98-99 % in every roster reading (95.5 % shipped), clumsy 51-60 % (50.1 %).

What the reads say:

- **The Vanguard must hold a crowd weapon, and its own Sword is not one.** Exclusivity alone leaves it the Sword and the
  Daggers: 2.8 % careful. A sweep on the Sword (the 2026-11-04 sword-role research's "2.5 round the hero, 60 %") adds about
  two points; the Vanguard runs without the Mortar still finish 4.5 %.
- **The Mortar's strength matters when it is held**: the Vanguard's runs holding it finish 65.5 % as built, 88.2 % with
  c-mortar15; but it is held in about 57 % of runs whatever its numbers, and the other 43 % finish 2 %.
- **Picking it on sight is not enough** (pref-area): the careful pilot takes the first new weapon it is offered while a
  slot is free, and a third of its runs fill both slots before the Mortar shows.
- **More weapons dilute the old answer too** (open): with five weapons and no exclusivity the Vanguard holds the Staff in
  43 % of runs instead of 85 %, and the careful cohort loses 6.7 points - even "adding weapons" costs the Vanguard until
  its picks reliably include a crowd weapon.
- **Dominance.** c-mortar15 makes the Mortar 65 % of the Vanguard's weapon damage among holders (the Staff's 65 % today)
  and 51 % of Ilya's - a sign that at those numbers it would crowd out Ilya's own Staff (46 %). The Bow stays at a quarter
  wherever it is held in every reading; the Daggers stay the weak pick (5-13 %).

How to rerun (exploration seeds only; the probe refuses a seed past 2000):

```text
# seeds 1-2000, every cohort (40,000 runs, about an hour on 12 threads)
FOUNDRY_SEEDS=2000 ROSTER_OPTIONS=shipped,roster FOUNDRY_OUT=<out>/a dotnet test Tools/CombatChecks -c Release -p:NuGetAudit=false --filter "FullyQualifiedName~WeaponRosterResearch"
# seeds 1-1000, the four docs/56 cohorts (about 10 minutes an option)
FOUNDRY_SEEDS=1000 ROSTER_COHORTS=careful,standing,first-card,clumsy ROSTER_OPTIONS=exclusive,open,pref-area,c-mortar15,c-sweep,c-sweep-m15 FOUNDRY_OUT=<out>/b dotnet test ...
python Tools/BalanceRule/paired_bootstrap.py <out> 1 2000    # or 1 1000 for the follow-ups
```

## 10. The candidate for a confirmation read

**None yet; no confirmation block should be spent on what is measured here.** Every reading of the roster fails docs/56's
rule for the careful cohort by 10 points or more, and the cost is the Vanguard's alone. The reads point at one design
fact, not at numbers: on the horde floor a hero must hold a crowd weapon, the Vanguard's own Sword is not one, and today's
answer was another hero's weapon.

**Proposed for the next design round (exploration first, then at most two to the confirmation block):**

1. **C1, "a crowd weapon guaranteed"** - the roster with c-mortar15's Mortar (34 damage every 1.5 s, reach 4, blast 3.0 at
   the full damage; lines as built), and an offer rule: while a hero holds no crowd weapon (an Area weapon or a sweep) and
   a slot is free, a new-weapon card it is shown is a crowd weapon when the pool holds one (docs/40 section 9.2's step 3,
   "early-game guarantee"). The reason: the Vanguard's runs that hold the strong Mortar finish 88 % (shipped 96 %), and
   the 43 % that never hold it finish 2 %; the guarantee removes exactly that failure. Ilya is unaffected (its Staff is
   its crowd weapon).
2. **C2, "a second shared crowd weapon"** - the roster with c-mortar15's Mortar and docs/40's Cinder Saw (an aura round
   the hero: 5 every 0.2 s within a short radius) in the shared pool, so two of the Vanguard's four shared choices hold a
   crowd. The reason: the same failure, removed by the pool's shape instead of a rule. It needs the aura strike built in
   `Combat` (section 3.2).
3. A guard against the Mortar crowding out Ilya's Staff at c-mortar15's numbers (51 % of Ilya's damage): read Ilya's
   shares beside the completion, and if needed keep the Mortar's damage at 30 for Ilya's sake (a number, not a rule).

The confirmation read for whichever candidate passes on exploration seeds: the fresh block from **seed 11601** (the lead's
note), 3,200 seeds (a change from floor 1 diverges over all four floors, docs/56 rule 1), both heroes, typical hands, the
four cohorts, paired against `shipped` on the same code.

## 11. Owner questions

1. **The roster as asked costs the Vanguard its finish today.** Keeping each hero's own weapon to that hero takes Ilya's
   Staff from the Vanguard, and the Staff did two thirds of the Vanguard's damage. With the two new shared weapons the
   careful player's full climb drops from 96 % to 60 % for the Vanguard (Ilya goes from 96 % to 98 %). Do you still want
   the Staff kept to Ilya alone, knowing the Vanguard then needs a crowd weapon guaranteed (C1) or a second crowd weapon in
   the shared pool (C2) before this can ship?
2. **The old weapon purchases.** The forge no longer sells weapons. A save that bought the Staff or the Daggers keeps the
   record but the gold is not given back. Refund it, or leave it (there are no players yet)?
3. **Names.** "Magma Mortar" (TR "Magma Havanı") and "Forge Bow" (TR "Ocak Yayı") - keep, or rename?
4. **Ilya's unlock.** Under the roster the Vanguard can never carry the Staff, so the closest fair rule is "Clear The
   Foundry" with any weapon (built, behind the roster's switch). Is that the rule you want, or something harder (for
   example clearing The Foundry on a higher tier)?
5. **The Riftblade's Daggers.** When the Riftblade ships, the Daggers become its own and leave the shared pool; the pool
   then needs a third shared weapon. Is the Cinder Saw the one to build first?
6. **Evolutions for the new weapons** (Magma Pool for the Mortar, Piercing Volley for the Bow): in the next phase, or
   after the roster's balance is settled?
7. **The forge's empty space.** With the rack gone, the forge shows the hero's weapon and the relics, and the band above
   the start button is empty. Leave it, or put something there (the hero's passive, or the skills)?
