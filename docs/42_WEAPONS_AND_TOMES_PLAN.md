# 42. Weapons and tomes - an engineering plan (proposed, 2026-10-26)

Everything below the first section is **proposed**. It expands step 3 of `41` (the report's PR-03, `40` section 17.2)
into small PRs against the code as it stands on `82daaf8`. Names that exist in the repository are written as they are;
names introduced here are marked *(proposed)*. Tuning numbers are starting points for the simulation, not decisions.
Decisions 9-12 in `27` say why; the report's rules are `40` sections 5.1, 5.3, 5.4, 6.1, 6.2 and 9.2.

## 1. How it works today

**The weapon at runtime.** `Combat/WeaponRuntime.cs` is a per-attacker snapshot built from a definition and never
written back to it. It holds six `ModifiableStat`s - `_damage`, `_attackSpeed` (base 1, `Interval => _baseInterval /
_attackSpeed.Value`), `_range`, `_critChance`, `_pulseDamage`, `_critSplash` - plus an immutable `AttackPattern`
(`Behavior`, `SplashRadius`, `SplashFraction`, `CritEvery`, `CritMultiplier`). `AddModifier(UpgradeStat, StatModifier)`
routes a card to one of the six and raises `StatsChanged`. `TryAttack(target, source, nearby, inPulseRadius,
inCritSplashRadius)` consumes the cadence, counts `AttacksMade`, decides a crit (the pattern's rhythm
`AttackPattern.IsCritical` first, then `RollsCritical`, drawn from `RunRandom.Stream(Seed, RunRandom.Crits,
AttacksMade)` at the fixed `RolledCritMultiplier` 2), deals the hit, the splash (`WeaponBehavior.Cleave` stops at the
first living neighbour, `Area` hits all), then `Splinter` (`CritSplashRule`, radius 2, off the crit damage) and
`Pulse` (`PulseRule`, every 4th strike, radius 2.2 round the attacker, flat). `ModifiableStat.AddModifier` computes
`(Base + sum of flat) * (1 + sum of percent)`; percents are summed, never compounded.

**The controller and targeting.** `Combat/AttackController.cs` holds exactly one runtime: `Initialize(WeaponRuntime)`
throws on a second call, `Weapon` returns it, and `Update` ticks it, asks `Targeting.Acquire(_weapon.Range)` for the
nearest living candidate, gathers `_nearby` (`Pattern.SplashRadius`), `_inPulseRadius` (only when `PulseDamage > 0`)
and `_inCritSplashRadius` (only when `CritSplash > 0`) through `Targeting.CollectNear`, then raises `Attacked` and
`Struck(Health)`. Every enemy uses the same component (`EncounterController` calls `definition.Weapon.CreateRuntime()`
and `AttackController.Initialize`), so any change here runs on up to 40 horde enemies. `Combat/Targeting.cs` is a
linear scan over candidates handed in by `EncounterController` (`_heroTargeting.SetCandidates`), measured through
`ArenaFloor.FloorDistanceSquared` so the simulation's `DescentSimulation.Acquire` ties the same way.

**Definitions and assets.** `Content/WeaponDefinition.cs` serializes damage, interval, range, initial delay, behaviour,
splash, crit rhythm and the forge text/price; `CreateRuntime()` and `CreateForgeOption()` are its only products. It has
no per-weapon upgrades. `Data/Weapons`: Sword 10 dmg / 0.8 s / 2.1 range / Cleave r2 x0.6 (price 0, the starting
weapon); Staff 11 / 1.1 s / 2.4 / Area r3.5 x0.75 (120); Daggers 6 / 0.5 s / 1.8 / DirectHit, crit every 3rd x2 (180).
The other six assets are enemy weapons.

**How the hero weapon is chosen and bound.** `Core/CombatSetup.Awake` builds `WeaponShop(Profile, weapons,
startingWeapon)` from `_weapons` (Sword, Staff, Daggers) and `HeroDefinition.StartingWeapon`, takes
`HeroWeapon = _weapons[...Weapons.Equipped]`, creates `Weapon = HeroWeapon.CreateRuntime()`, binds it to the one
scene-serialized `_attack`, sets `Weapon.Seed = Run.Seed` and hands the same runtime to `UpgradeService`. `WeaponShop.
Equipped` is the saved `PlayerProfile.EquippedWeaponId` while owned, else the starting weapon; the save is
`Save/ProfileSaveData` v2 (`ownedWeaponIds`, `equippedWeaponId`, empty for the starting weapon).

**Cards.** `Content/UpgradeDefinition.CreateOption()` makes an immutable `Progression/UpgradeOption` (one `Stat`, a
`StatModifier` per `UpgradeRarity` Common/Rare/Epic, `MaxStacks`, `RequiresId`, `EvolvesFromId`). The scene's
`CombatSetup._upgrades` and `DescentSimulation.Pool()` hold the same 14 in the same order: Tempered Edge (Damage +50%),
Quickened Grip (AttackSpeed +50%), Long Reach (Range +0.3), Keen Edge (CritChance +0.08), Vital Surge, Iron Skin, Swift
Boots, Deep Lungs, Fortune, Forge Pulse (PulseDamage 0.4), Splintering Crit (CritSplash 0.4), and the evolutions Ruinous
Edge, Blurred Grip, Ironblood. `UpgradeStat` is serialized by value (append only); `HeroStats.Owns(stat)` decides
whether `UpgradeService.TrySelect` writes to `HeroStats` or to `_weapon` - there is exactly one weapon to write to.

**Offers, books, evolutions.** `UpgradeService.Eligible()` filters by stack limit, `RequiresId`, the stamina switch,
and `UpgradePool.WidthAt(level)` (5 cards until level 12, +1 per 6 levels). `CreateOffer()` hands the eligible list
whole when it fits `_choiceCount`, else draws distinct cards and then rarities from one `RunRandom.Offers` stream per
offer index (`RarityTable.Draw`, 70/25/5 shifted by `HeroStats.Luck`). `TryGrantBook(chestIndex, ...)` draws one card
and tier from `RunRandom.Books` over the same `Eligible()` list; `ChestRule.HoldsBook` (25 %) decides whether a chest
holds one and `ChestSpawner` calls it. An evolution (`IsEvolution`) skips the width but needs `EvolutionRule.Unlocked
(level >= 12, base card full)`, and `ApplyEvolutionCost` adds `EvolutionRule.DifficultyPerStack` 0.2 to `HeroStats.
Difficulty` (kept by decision 11).

**Presentation.** `UI/RunHud.RefreshWeapon` prints `HeroWeapon.DisplayName` and the one runtime's damage/interval into
`_weaponLabel`/`_weaponName` and listens to `_setup.Weapon.StatsChanged`; `RefreshBadges` shows two badges
(`_badgeDefinitions` = Tempered Edge, Quickened Grip) with stack counts. `UI/RunChoiceView.IconFor` resolves a card icon
by `UpgradeDefinition.Id` from `_upgradeIcons`. `UI/RunResultView.DescribeBuild` lists `HeroWeapon.DisplayName` and every
stacked card. `UI/HeroWeaponView` activates the one `Loadout` whose `Weapon == _setup.HeroWeapon`; `UI/VanguardAnimator`
infers `_loadout` from which part is active and reads `_attack.Weapon.Interval`/`Range`; `UI/HeroLookView` animates on
`_attack.Attacked`/`Struck`. `UI/CombatEffectsView.OnHeroStruck` picks the strike via `EffectArt.StrikeFor(weapon.
Pattern.Behavior)` (Cleave -> SwordSlash, Area -> StaffBlast with `StaffSplashArt`'s shared ring scaled by `RingScale`
from `Pattern.SplashRadius`, else DaggerSlash) and pairs it with a single `_critPending` flag. `Combat/RelicBehaviour`
passes `_attack.Weapon.Damage` to `RelicRuntime.OnHeroDamaged`.

**Telemetry.** `Analytics/RunTelemetry`: `run_start.weapon_id` (from `RunTelemetryContext.WeaponId = HeroWeapon.Id`),
`run_end.pulses` (`context.Pulses = Weapon.PulsesFired`), `upgrade_offered` (`choice_ids`, `choice_rarities`,
`offer_index`), `upgrade_selected` (`upgrade_id`, `stacks`, `rarity`), the chest's `book`/`book_rarity`, and
`WeaponShop.Forged` -> `ReportSpent("weapon", ...)`.

**The simulation.** `Tests/Support/DescentSimulation.cs` re-states the three weapons by hand as `HeroWeapon` (`Sword()`,
`Staff()`, `Daggers()`, with test-only `PulseShare`/`CritSplashShare`/`CritChance` starters). `Run(..., heroWeapon,
..., pool, choiceCount, ...)` builds one `WeaponRuntime`, one `UpgradeService` and a `CardPicker` (slot, `preferIds`,
`replay`) and threads the single `weapon` through `RunFloor`, `Linger` and `RunSwarmRoom`, which call `weapon.TryAttack`
at two sites mirroring `AttackController.Update`. Pins that name a weapon: `WeaponBehaviorTests`
(`StaffClearsMitePacksFastestAndDaggersFellTheWardensFastest`, `EveryWeaponSurvivesTheMendingDescentBut...`, the pulse
and splinter mechanism tests), `DescentSeedTests` (`Weapons()` loops, `TodaysDescentIsOfferedTheSameFiveCardsItAlwaysWas`),
`DescentTests` (`APulseReachesTheCrowd...`, `ASplinteringCritGivesTheDaggersTheAreaTheyLack`, `Evolutions...`),
`SimulationParityTests.PlayDescent` per weapon id, plus `DensityProofParityTests`, `AbilityTests`, `HeroStaminaTests`,
`GroundDropsTests`, `OfferEngineTests`, `UpgradeTests`.

## 2. Target model

**Loadout.** A pure `WeaponLoadout` *(proposed, Combat)*: an ordered list of up to `MaxSlots = 3` entries, each a
weapon id, its `WeaponRuntime` and a `Level` 1-8. Slot 0 is the starting weapon (`WeaponShop.Equipped`); a weapon is
never owned twice (a repeat pick is a level-up). `AttackController` keeps `Initialize(WeaponRuntime)` and gains
`AddWeapon(WeaponRuntime)` *(proposed)*; `Update` walks the runtimes in slot order inside the one `Update` (Unity does
not order `Update` across components, and the simulation needs one fixed order), each with its own `Acquire(range)` and
gathered lists. `Weapon` keeps returning slot 0, so every existing reader keeps compiling. Enemies keep a list of one.
`Struck` gains a sibling `StruckWith(Health, WeaponRuntime)` *(proposed)* so views know which weapon hit.

**Per-weapon upgrades live on the definition.** `WeaponDefinition` gains `_levelUps` *(proposed)*, an array of
`UpgradeDefinition` lines (stat, operation, three tier amounts, description) - reusing the card type means rarity,
descriptions and `ModifierFor(rarity)` come free. An owned-weapon card is "weapon X: one line from X's table", drawn when
the offer is made so the card can show its effect (`40` 9.2 step 9); taking it applies the line to X's runtime only and
raises X's level. Seven picks take a weapon from Lv1 to Lv8 (`40` 5.1). `WeaponDefinition` also gains `_evolution` and
`_evolvesWithTome` *(proposed)*.

**Tomes are global.** A tome is an `UpgradeDefinition` with `MaxStacks = 8` and a weapon stat; `UpgradeOption` gains a
`Category` *(proposed enum `OfferCategory`, appended-only: General, NewWeapon, WeaponUpgrade, NewTome, TomeUpgrade)*,
General by default so every existing card is unchanged. At most 3 distinct tomes (`40` 6.2). A tome pick goes to
`WeaponLoadout.AddGlobal(stat, modifier)` *(proposed)*, which records it, applies it to every current runtime and
replays the record onto any weapon acquired later.

**Why this reproduces `40` 5.3 with no new arithmetic.** `ModifiableStat` already computes `(Base + sum flat) * (1 +
sum percent)`. If per-weapon damage lines are authored **Flat** and Force is **Percent**, a weapon's damage is
`(d0 + sum of its own increments) * (1 + sum of all Force levels)` - the report's per-weapon-first, global-bucket-second
order, and Force +8 % twice is 1.16x, not 1.08 x 1.08 (`40` 6.2 rule 7). Tempo is Percent on `_attackSpeed`, so attack
speed bonuses add (`AS_bonus`). The rule to enforce: a weapon line on Damage must be Flat (a content test over
`Data/Weapons`), otherwise it would fall into the global bucket.

**Stat mapping.** Force -> `UpgradeStat.Damage` Percent; Tempo -> `AttackSpeed` Percent; Precision -> `CritChance` Flat
(pp); Reach -> a new `UpgradeStat.Area = 12` *(proposed, appended)*: a seventh `ModifiableStat` on `WeaponRuntime`
(base 1) with `SplashRadius => Pattern.SplashRadius * area` and the pulse and splinter radii scaled the same, so Reach
reaches the Daggers once they splinter (`40` 6.2 rule 6). Later tomes map onto what exists: Fortune -> `HeroStats.Luck`,
Vitality -> `MaxHealth`; Magnet needs `GroundDrops.PickupRadius` (a const today) to become a stat, Knowledge needs an
experience multiplier on `RunState.AddExperience` - both out of V0.1.

**Offers.** `UpgradeService.CreateOffer` gains a category path *(proposed)* used only when the service was given a
loadout and a weapon/tome catalogue: per card slot draw a category from `OfferWeights` *(proposed static, like
`RarityTable`)* - New Weapon 27/15, Owned Weapon 28/35, New Tome 25/15, Owned Tome 10/25, General 10/10 (early/late,
`40` 9.2) - renormalised over eligible categories (slots full, Lv8, three tomes held, nothing left), then a distinct card
within it. Early/late switches on run level, not the clock, so scene and simulation agree without sharing a timer:
`LateFromLevel = 6` *(proposed; `40` 9.1 puts level 3-4 at 1 min, 11-13 at 5)*. Category and card draws come from a new
`RunRandom.Categories = 5` *(proposed, appended)* stream; rarities stay on `RunRandom.Offers`, so "which card" and
"which tier" never share draws (`40` 9.2). Without a loadout the current `CreateOffer`/`Eligible()` run unchanged.

**Evolutions.** A weapon evolution unlocks at weapon Lv >= 5 and its tome Lv >= 2 (`40` 5.4) through
`EvolutionRule.WeaponUnlocked(weaponLevel, tomeLevel)` *(proposed)* and still pays `ApplyEvolutionCost` (decision 11).
It changes behaviour by granting an existing behavioural stat to that weapon's runtime only: Daggers + Precision ->
`CritSplash` (Splintering Crit's 0.4/0.6/1.0; this is the report's Rift Echo and docs/31's own conclusion); Sword +
Force -> `PulseDamage` (Forge Pulse's 0.4/0.65/1.0; docs/30 measured the pulse as the Sword's card; the report pairs
the Sword with Bastion, not in V0.1). The Staff's evolution needs a new behaviour and is left open. Forge Pulse and
Splintering Crit leave the general pool in category mode, which settles the "card that assumes one weapon" problem.

**Books** draw, in category mode, from Owned Tome and General only (a chest never hands a weapon), on the same
`RunRandom.Books` stream - *proposed, owner's call*.

## 3. Migration in small PRs

Each PR compiles, keeps every existing test green unless it says otherwise, and leaves the scene on the single-weapon
path until PR 7.

1. **Area stat.** `UpgradeStat.Area = 12`; `WeaponRuntime._area`, `SplashRadius`, `PulseRadius`, `CritSplashRadius`
   *(proposed)*; `AttackController`, `DescentSimulation` (both attack sites) and `CombatEffectsView.RingScale` read the
   runtime's radii instead of `Pattern.SplashRadius` / the constants. Area starts at 1, so the values are unchanged.
   Test: an Area modifier widens the Staff's splash; all pins unchanged.
2. **Loadout, one slot.** `WeaponLoadout` with `TryAdd`, `Primary`, `AddGlobal`; per-slot crit seed - slot 0 keeps
   `Run.Seed`, slot k derives its own (today two weapons with one `Seed` would crit on the same attack numbers);
   `AttackController.AddWeapon` + slot-ordered `Update`; `DescentSimulation` loops over the loadout at both
   `TryAttack` sites, a later slot skipping a target an earlier slot killed this step (the scene's `IsValid` already does).
   With one slot every number is identical. Tests: new two-weapon cases in `WeaponBehaviorTests` (own cadences, slot
   order, dead target skipped, separate crit streams); all pins unchanged; `SimulationParityTests` unchanged.
3. **Data.** `WeaponDefinition._levelUps`, `_evolution`, `_evolvesWithTome`; `UpgradeOption.Category`; four tome
   assets; per-weapon lines on Sword/Staff/Daggers; a pure `WeaponCard` *(proposed: id, name, runtime factory, lines,
   evolution)* built by `WeaponDefinition.CreateCard()` *(proposed)* and, in the simulation, from `HeroWeapon`, so
   `UpgradeService` stays free of ScriptableObjects. A content test that sim and assets agree (the stamina lesson:
   duplicated numbers drift). Nothing reads the data yet.
4. **Category offers.** `OfferWeights`, `RunRandom.Categories`, the category path in `CreateOffer`/`TrySelect`, slot
   and level limits, a `WeaponAcquired` event *(proposed)* that `CombatSetup` will use to call `AddWeapon`; `CardPicker`
   learns to prefer categories. Tests (`OfferCategoryTests`, proposed): weights sum to 100 in both phases; closed
   categories renormalise; no NewWeapon when 3 slots are held; a Lv8 weapon and a Lv8 tome leave; never a 4th tome; no
   repeated id in an offer; same seed same offers. `OfferEngineTests`, `UpgradeTests` and
   `TodaysDescentIsOfferedTheSameFiveCardsItAlwaysWas` unchanged because they never pass a loadout.
5. **Weapon evolutions.** `EvolutionRule.WeaponUnlocked`, the two V0.1 evolutions, cost kept. Tests: offered at Lv5 +
   tome Lv2 and not before; applies to its own weapon only; Difficulty rises 0.2.
6. **Books in category mode.** Tests: never a weapon from a chest; legacy book tests unchanged.
7. **Scene switch-on.** `CombatSetup` builds the loadout from `Weapons.Equipped` (the save's `equippedWeaponId` becomes
   the starting weapon; **no `ProfileSaveData` change, no version bump**), subscribes `WeaponAcquired` ->
   `_attack.AddWeapon`, and enables category mode when its new `_tomes` *(proposed)* array is non-empty, so the switch
   is one serialized field. Explicit re-pins here: `SimulationParityTests` gains category-mode cases (sim vs scene, one
   per starting weapon); `DescentSeedTests` gains a category-mode cohort; the legacy pins stay and keep measuring the
   legacy path, which stays callable in the simulation.
8. **Telemetry.** Keep `run_start.weapon_id` (now the starting weapon). Add `weapon_acquired` (`weapon_id`, `slot`,
   `run_level`), `upgrade_offered.choice_categories`, `upgrade_selected.category` and `weapon_level`, and on `run_end`
   `weapon_ids`, `weapon_levels`, `tome_ids`, `tome_levels`, `evolutions`; `pulses` becomes the sum over slots.
   `RunTelemetryTests` and `TelemetryFlowTests` re-pinned for the new fields only.
9. **UI and art.** HUD badges, card labels and icons, result screen, strike effects per weapon (section 4).

## 4. Risks

- **Several weapons on one hero.** All three take the nearest target, so the Sword and the Staff overkill the same
  enemy; nearest stays for V0.1, a per-weapon targeting mode is later. Body animation must follow slot 0 only
  (`HeroLookView`, `VanguardAnimator` on `Attacked`), or the hero swings three times; `HeroWeaponView` keeps showing the
  starting weapon in hand and the others show only as effects. `CombatEffectsView._critPending` pairs a crit with the
  next `Struck` - with three weapons in a frame that pairing breaks; it needs `StruckWith` and the crit per hit.
  `RelicBehaviour` keeps reading the primary's damage (state it in the relic text).
- **Cards that assume one weapon.** `TrySelect` and `TryGrantBook` write to `_weapon`; Tempered Edge, Quickened Grip,
  Keen Edge and Long Reach would buff only slot 0 and double-dip with the tomes - they leave the pool in category mode.
  Forge Pulse and Splintering Crit become evolutions. `context.Pulses` and `PulsesFired` / `SplintersFired` are
  per-runtime and must be summed.
- **Balance.** Today one Tempered Edge is +50 % and five are +250 %; Force Lv8 at Common is +64 %. Category mode makes
  each pick much smaller while the weapons' bases (10/11/6) are about half the report's (18/24/7). The first category
  run must be measured on the horde floor before any tuning is believed. Precision Lv8 plus the Daggers' crit lines can
  pass 80 %: apply the report's 80 % hard cap in `RollsCritical` *(proposed, category mode only)*.
- **Parity.** The simulation restates weapon numbers by hand; the new lines and tomes must be built from one source or
  pinned equal by a test. Slot order must be identical in `AttackController.Update` and the simulation loop.
- **In-flight work.** `CombatSetup`, `RunTelemetry`, `RunTelemetryBridge` and `EncounterController` carry uncommitted
  boss changes (`WardenSlamService`); PR 7 and 8 must wait for them to land.
- **Portrait HUD.** Six badges (3 weapons, 3 tomes) with a level digit in one row of 40-48 px icons under the XP bar,
  weapons left, tomes right, replacing `_upgradeBadges`; names and numbers only in the paused `_details` panel. Cards
  need a "NEW" or "Lv n -> n+1" line: `ChoiceCard` gains category and level *(proposed)*; `RunChoiceView.IconFor`
  resolves weapon and tome icons by id.
- **Mobile performance.** Sword, Staff and Daggers are instant hits: no projectile objects in V0.1. Cost is three
  `Acquire` scans and `CollectNear` insertion sorts per ready attack (about 4 attacks a second over 40 enemies, cheap),
  but more damage numbers and Staff rings each frame; measure with `RenderStatsProbe` at the horde cap, and pool any
  projectile weapon added later.

## 5. The minimal V0.1 slice

- **Weapons:** Sword (start, or the forged `Equipped`), Staff, Daggers, all three in the pick-up pool regardless of
  forge ownership (the forge then picks the starting weapon - *owner's call*). Lines (Common, x1.25 Rare, x1.5 Epic,
  `40` 6.1 multipliers): Sword +2 Damage flat, +12 % Area, +0.3 Range; Staff +2 Damage flat, +12 % Area, +8 % Attack
  Speed; Daggers +1 Damage flat, +5 pp Crit, +8 % Attack Speed.
- **Tomes** (`40` 6.1 green/orange/red, Lv1-8): Force +8/10/12 % Damage, Tempo +6/7.5/9 % Attack Speed, Reach
  +8/10/12 % Area, Precision +4/5/6 pp Crit.
- **General:** Vital Surge, Iron Skin, Swift Boots, Fortune, Ironblood (Deep Lungs stays out while stamina is off).
- **Evolutions:** Daggers Lv5 + Precision Lv2 (splinter), Sword Lv5 + Force Lv2 (pulse); cost +0.2 Difficulty.
- **Weights:** `40` 9.2 as above, late from level 6; three cards an offer; rarity keeps today's three tiers until PR-04.

**Acceptance tests.** Slots never exceed 3 weapons / 3 tomes and levels never exceed 8 over 1,000 seeded category-mode
simulation runs; no ineligible card is ever offered; weights renormalise when a category closes; a seed replays its
offers and picks (`CardPicker.Taken` / `ReplayMisses` = 0); one Force level raises every owned weapon's damage by the
same share and a weapon picked up later receives it; `(d0 + flat) * (1 + Force)` matches a hand-computed value; an
evolution appears only at Lv5 + Lv2; with one slot every legacy pin is unchanged; category-mode `SimulationParityTests`
match for each starting weapon; on the horde floor `40` 9.3's ">= 2 weapons and >= 2 tomes by minute 5" holds in at
least 85 % of simulated runs; every weapon has a viable run without a Luck pick; and the device holds step 1's frame
time with three weapons firing.

## 6. Owner decisions (2026-10-26)

- All three weapons (Sword, Staff, Daggers) drop as cards in a run whatever the forge owns; the forge picks the starting
  weapon.
- Chest books draw from Owned Tome and General only: a chest never hands a weapon.
- Rarity is already four colours (`RarityTable`, green/orange/red/gold); tomes and weapon lines use the report's
  1/1.25/1.5/2 tier multipliers, while today's general cards keep their own tier values.
