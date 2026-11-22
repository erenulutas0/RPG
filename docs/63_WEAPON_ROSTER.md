# 63 - The weapon roster: each hero its own weapon, a shared pool, three slots (2026-11-23)

Phase 1 of the owner's weapon request. Sections 1-4 are built and on by default (the rack's removal, the full-slot rule's
tests, Ilya's unlock rule); sections 5-8 are a design with two shared weapons built **behind a research lever that is off
by default** (`FoundryLevers.WeaponRoster` in the simulation, `development/weapon-roster.txt` in the scene). With the
lever off, everything a player meets is today's game and the default batch is byte-identical. Section 9 holds the
measurement on the exploration seeds 1-2000 (docs/56 rule 1); section 10 the candidate for a later confirmation read.

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
- **Byte-identical default.** With the lever off nothing the default batch reads changed (section 9.1).

## 9. Measurements (exploration seeds 1-2000)

Pending: filled in by the measurement commit.

## 10. The candidate for a confirmation read

Pending: filled in by the measurement commit.

## 11. Owner questions

Pending: filled in by the measurement commit.
