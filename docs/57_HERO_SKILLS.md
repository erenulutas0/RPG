# 57 - Hero skills: three active skills a hero (design proposal, 2026-11-17)

A proposal for the lead's and the owner's decision. The owner asked for "karakterlerin daha çok skilleri olsun": more
skills for the heroes, the PvP arena first in mind. docs/54 section 14 outlined it in one paragraph ("three active
skills per hero: a second opens at floor 2's first clear, a third at floor 3's, a skill bar of three on the right
thumb"); docs/55 built an integer duel core whose "dealt skills" stood in for the second and third. This document
designs them.

**Status 2026-11-18: phases 1 and 2 are built and measured (section 13).** The skills are in the core, the scene and the
simulation, off by default; measured with every playing cohort, three of section 3's details changed on the spent seeds
(the Rush stops on impact, the timing pilot leaves the Warden to its weapons, a gate boss takes half the glyph's heat),
and confirmed once on the fresh block 8401-11600 under docs/56's rule: every cohort gains (four-floor completion: clumsy
**+15.0** points, 90 % CI +14.0 to +16.0; careful **+1.5**, +1.1 to +1.8; first-card **+3.9**, +3.3 to +4.5; standing
+4.3), the careful pilots' gate fights stay in 45-65 s and the Descent at 14.3-14.5 minutes. The rest of this paragraph
and sections 0-12 are the proposal of 2026-11-17, with the measured numbers marked where phase 2 replaced the modelled
ones.

**Status 2026-11-19: phases 3 and 4 are built (section 14).** The skills are playable: floor 2's first clear opens skill 2
and floor 3's opens skill 3, the Forge's new Skills tab forges each per hero for 300 and 500 gold, and the HUD shows
three buttons at section 5.2's anchors (a skill locked until forged). Save version 6 holds what was forged. The
simulation keeps its skills off by default, so the default batch is byte-identical; the easing re-read with the unlock as
built (13.8, decision 1) is the lead's next read.

Nothing in the game is built: no code under `Assets/`, no asset, scene, setting or package changed. One research file
is added and the duel core grows: `Tools/CombatChecks/HeroSkillsResearch.cs` (the six skills as numbers, a cheap pinned
test in every plain `dotnet test`, and two reports run by name) and six skill kinds, Ember Memory's burn, a bold policy
and a cooldown line appended to `Tools/CombatChecks/ArenaDuel.cs`, which leave every docs/55 fight bit-identical
(pinned, and docs/55's report re-runs byte for byte but its clocks). Numbers cited as **measured** come from those (2026-11-17, main `b0fa98c2`, this
12-thread Windows machine): the duel's on seeds 1-2000, the Descent's on seeds 1-200 with typical human hands. Numbers
cited as **modelled** are arithmetic on measured inputs, shown. Every other number is a starting value for the
simulation to measure, not a decision.

## 0. The short version

| Question | Proposal | Key numbers |
|---|---|---|
| The Vanguard's three | **Forge Burst** (shipped, 20 within 2.5, 8 s) - **Iron Resolve** (+100 armour for 4 s: half damage on a bare hero, 20 s) - **Shield Rush** (4 units in 0.3 s along the stick, 15 to what it passes, a 0.5 s daze, 12 s) | melee and armour: hit the crowd, hold the line (a forgiving 4 s), cross the floor |
| Ilya's three | **Crucible Vent** (shipped, 8 a stack within 3, 14 s) - **Flame Sigil** (a 2.5-unit glyph at its feet for 5 s: enemies inside walk 40 % slower and burn double, 20 s) - **Cinder Ward** (absorbs 20 % of its maximum health, about one slam, for 2 s; strikers catch 2 burn stacks; 20 s) | the caster: cash the fire, make the crowd wade through it, ward the glass cannon (a timed 2 s) |
| What a skill is worth in the run today | the shipped actives are **1-3 % of the Vanguard's damage and under 1 % of Ilya's** (measured), and their share **falls floor by floor** because their damage is flat while the build grows | new damage skills **scale with the build**; the defensive and control skills carry the PvE value, read in deaths, not DPS |
| Unlock | **the floor's first clear opens the skill, the Forge forges it for gold**: skill 2 at 300, skill 3 at 500, per hero | a floor-2-clearing run banks ~340, a completed one ~500 (docs/54 1): each is bought with the run that opened it |
| Ranks | later, after the skills are measured: **five ranks, +6 % of the skill's own number each, gold and floor materials, no failure**; cooldowns only from docs/54's efsun line (capped at 10 %) | about 6,750 gold a hero for all three to rank V; **+2-4 %** on the hero's PvE power index (modelled) |
| Input | **three buttons in an arc on the right thumb**: skill 1 stays the 234-unit corner button (14 mm on the S23), skills 2 and 3 at 176 units (10.5 mm) on its arc; a locked slot is hidden | rejected: one cycling button (fires the guard as an attack), a smart button (removes the decision), gestures (unreadable under pressure) |
| PvP | the hero's own three replace docs/55's five stand-in skills; Archero 2's variety comes from **dealt boons**, not more buttons; ranked plays **all three at a fixed rank, no cooldown line** | each new skill alone wins **70-93 %** of a mirror; the -10 % cooldown line **83 %** of a Vanguard mirror; with Ember Memory's burn (which docs/55's Ilya lacked) **Ilya beats the Vanguard 98-100 %**: an arena coefficient near **x0.8** on Ilya's damage evens it |
| Balance | the cohorts measure every skill; **skills off, the default batch stays byte-identical**; skills on, the Descent stays inside the relative ship rule (docs/56's study of that rule is pending) | section 6; **measured (section 13)**: skills on, clumsy +15.0 / careful +1.5 / first-card +3.9 points of four-floor completion, 90 % CIs wholly above 0, on the fresh block 8401-11600; the careful bosses in 45-65 s, the Descent's P50 within 0.03 minutes |
| Build | phases 1-5: **12-14 days** (core and simulation 5, measurement 2-3, HUD and art 3, unlock and Forge 2, device 1); ranks +2-3 later; the arena's share inside docs/55's P0 | section 8 |

**The recommendation in one paragraph.** Give each hero two more one-tap skills that need no aim: the Vanguard an
armour guard and a charge, Ilya a slowing, burning glyph and an ash ward. Skill 2 deepens the hero's identity, skill 3
covers its weakness - the Vanguard's weaker players die in the crowd, Ilya's at the boss (measured). Open them at the
first clears of floors 2 and 3, as docs/54 outlined, and let the Forge sell them so the gold the run brought home buys
the button the run earned. Make the new damage scale with the build, because the shipped actives measure as flavour by
floor 3. Put three buttons in an arc on the right thumb, the new two smaller, hidden until opened. Measure every skill in
the simulation with the cohorts before it ships, keep the shipped Descent byte-identical with skills off, and give the
arena the same three skills for everyone at a fixed rank, since an auto-duel turns any skill edge into most of the wins.

## 1. References: how the genre structures active skills

| Game | What the player presses | Where the skills come from | Do they level | Lesson for Cryptforge |
|---|---|---|---|---|
| **Vampire Survivors** (poncle) | movement only: there is no attack button, weapons fire on their own | weapons and passives picked at level-ups | in the run only | the genre's core needs no button; build choices replace aiming |
| **Megabonk** (docs/40 2.3) | movement and the game's movement tech (jump, slide); characters have no ability button in the guides read | a starting weapon and a passive per character; weapons and tomes from level-ups | in the run only | docs/40's own lesson: "Cryptforge burada ayrışabilir: karaktere özel aktif yetenek mastery" - the actives are where Cryptforge can differ |
| **Brotato** | movement; weapons aim themselves (manual aim is an option) | characters are stats, a starting weapon and sometimes a unique rule; items between waves | no | identity through rules, not buttons; the skill is where you stand |
| **Archero / Archero 2** (Habby) | movement; heroes carry level boosts, some periodic with an internal cooldown | run skills picked at level-ups and kept for the run; in the arena the skills are **dealt automatically in groups** and the fight is watched | hero levels | an auto-battler's variety can be dealt picks rather than buttons; heroes with steady passive buffs do best in auto fights |
| **Hades** (Supergiant) | **Attack, Special, Cast, Dash**, and **Call** once the God Gauge holds a charge | boons from the gods modify each of those fixed verbs | boons rise in rarity and level in the run | a few fixed verbs, each changed by the run; on touch (the Netflix iOS port) the layout is customisable and a reviewer kept hitting Cast for Dash on a preset |
| **Brawl Stars** (Supercell) | attack (an aim stick), **Super** (charged by hits), **Gadget** (few uses) on the right | per brawler, unlocked | gadgets and star powers unlock | the mobile standard: one large primary and smaller secondaries beside it; layout guides put the Super near the attack and larger |
| **Metin2** (Gameforge; the owner's reference for basma and efsun) | a hotbar of the class's skills | the skills of the class's chosen group | **yes, long**: skill points through the normal levels, then **Master M1-M10 by skill books** (20,000 experience to read one, **18-30 hours between reads**, an Exorcism Scroll skips the wait, Concentrated Reading makes a read succeed), **55 successful reads from M1 to G1**, then **Soul Stones from Grand Master to Perfect** | ranks are a weeks-long goal and a sink for every currency; the waits and the failures are what the owner turned down for basma ("kırılmasın", not too punishing) |

What this says for a mobile survivor-like with heroes:
1. **The core loop asks for no button** (Vampire Survivors, Megabonk, Brotato). A hero's skills must be optional power
   and readable choices, never a requirement for the floor: docs/40's skill-dependence scale (Vanguard 1, Ilya 3) and
   the auto-farmer cohort, which presses nothing, keep that honest.
2. **Few fixed verbs, changed by the run** (Hades) reads better than many. Three is the most docs/40 14.4 and the
   thumb allow; each is one tap with no aim.
3. **On a phone the secondaries are smaller and beside the primary** (Brawl Stars), and adjacent buttons get mis-hit
   (the Hades port's review): telemetry must count mis-taps on the device.
4. **In an auto-fight, variety is dealt, not pressed** (Archero 2's arena): the arena keeps the hero's three and deals
   boons on top (section 7).
5. **Ranks are a long sink** (Metin2), and the owner already chose the friendly side of Metin2 once: no failure, no
   wait (section 4.3).

## 2. What a skill is today, and what the shipped two are worth

**In the code.** `AbilityDefinition` (a ScriptableObject: id, name, damage, radius, cooldown, `AbilityKind` Burst or
Vent, icon) makes an `AbilityRuntime` per run; `AbilityController` fires it (`TryUse`: a Burst strikes every enemy within
the radius; a Vent spends every burning enemy's stacks first, then erupts each for `stacks x 8` on it and its 1.2-unit
neighbours) and holds it while the boss's reveal holds the fight (`FightHold`). `HeroDefinition` carries one ability.
`AbilityButtonView` is the round button at the bottom right: 234 x 234 canvas units, anchored bottom-right at (-40, 68)
on the 1080-wide canvas (14 mm on the S23), a radial cooldown fill and a countdown, the hero's own icon; a press
anywhere that is not a button becomes the movement stick (`HeroMovementInput`). `RunTelemetry.RecordAbilityUsed` writes
`ability_used` with the run's one `ability_id`. The development autopilot and the simulation fire the ability when three
enemies stand in reach (burning ones for a Vent) or any one beside the gate boss (`PilotAbilityCrowd`); the auto-farmer
cohort never does (`AutopilotCohort.Habit.UsesTheAbility`).

**What the shipped two do in the run (measured).** `HeroSkillsResearch.TheShippedDescentAsTheSkillsWouldMeetIt`, seeds
1-200, the whole Descent, typical hands:

| Cohort, hero | Ability uses a minute, floors 1 / 2 / 3 | Ability's share of the hero's damage | Damage a use | Hero's damage a second | Burn's share | Enemies standing (mean) | Damage taken a minute |
|---|---|---|---|---|---|---|---|
| careful Vanguard | 3.1 / 2.0 / 2.7 | **2.7 / 0.9 / 0.9 %** | 30 / 28 / 29 | 59 / 94 / 148 | - | 8.0 / 7.6 / 7.3 | 24 / 16 / 23 |
| careful Ilya | 0.7 / 0.7 / 1.0 | **0.6 / 0.4 / 0.3 %** | 29 / 28 / 27 | 59 / 93 / 148 | 3.1 / 1.8 / 1.7 % | 7.9 / 7.5 / 7.3 | 22 / 15 / 21 |
| clumsy Vanguard | 4.4 / 1.8 / 2.9 | 5.0 / 0.9 / 1.0 % | 31 / 28 / 32 | 49 / 92 / 164 | - | 9.0 / 7.5 / 7.6 | 78 / 36 / 71 |
| clumsy Ilya | 0.7 / 0.7 / 1.1 | 0.5 / 0.3 / 0.3 % | 26 / 27 / 28 | 58 / 91 / 167 | 2.8 / 1.9 / 1.7 % | 7.8 / 6.8 / 7.4 | 44 / 34 / 72 |
| auto-farmer Vanguard | (none) | - | - | 46 / 86 / 134 | - | 8.7 / 7.7 / 7.9 | 73 / 25 / 72 |
| auto-farmer Ilya | (none) | - | - | 56 / 88 / 134 | 1.8 / 1.2 / 1.0 % | 8.2 / 7.7 / 7.8 | 48 / 27 / 62 |

P50 over the runs that played the floor. The floors last 250-276 s for the careful pilot and its gate bosses 46-62 s.
A Burst deals 20 to each enemy in reach, so 30 a use is at least 1.5 enemies (overkill hides more).

| Cohort | Hero | Completed of 200 | Deaths: floor 1 / 2 / 3 | Deaths: at the boss / in a hall's fight / elsewhere |
|---|---|---|---|---|
| careful | Vanguard | 190 | 5 / 4 / 1 | 9 / 1 / 0 |
| careful | Ilya | 194 | 2 / 3 / 1 | 3 / 3 / 0 |
| clumsy | Vanguard | 61 | 122 / 13 / 4 | 33 / **94** / 12 |
| clumsy | Ilya | 100 | 69 / 25 / 6 | **69** / 27 / 4 |
| auto-farmer | Vanguard | 5 | 190 / 4 / 1 | 100 / 78 / 17 |
| auto-farmer | Ilya | 6 | 171 / 17 / 6 | **184** / 7 / 3 |

Three things follow, and they shape every number in section 3:
1. **The shipped actives are flavour in the run's damage.** The Forge Burst is 2.7 % of the careful Vanguard's damage on
   floor 1 and 0.9 % after; the Vent 0.3-0.6 % of Ilya's, since three burning enemies in reach come about once a minute.
   Ember Memory's burn itself is only 2-3 % of Ilya's damage: enemies die to the Staff before it ticks. docs/46's R_skill
   read the same on the Warden: 1.04x for the Forge Burst, 1.01x for the Vent.
2. **Their share falls floor by floor** because their damage is flat (20, 8 a stack) while the hero's grows with every
   card: the careful Vanguard deals 59 a second on floor 1, 94 on floor 2 and 148 on floor 3, and a Burst stays near 30
   a use. A new damage skill sized like the Burst would not show; a flat one would vanish by floor 3. So **the new
   skills' damage scales with the build** (section 3.0); whether skill 1 should too is a measured lever, not part of this
   proposal (lead question 1).
3. **The run's value of a defensive or control skill is read in deaths, not damage**, and the deaths say which. The
   careful pilots rarely die (10 and 6 of 200). The clumsy Vanguard dies **in the halls' fights** (94 of its 139 deaths,
   122 of them on floor 1): a crowd problem - a guard and a way out. The clumsy and auto-farmer Ilya die **at the boss**
   (69 of 100, 184 of 194): a 90-health caster under the Warden's blows - a guard first. Laid over the same runs, a
   guard pressed by the careful rule is worth about +4-5 % effective health and gives a third to a half of the clumsy
   cohort's deaths 5 s or more at the killing moment (section 6.3).

## 3. The six skills

### 3.0 Rules every skill keeps

- **One tap, no aim.** Direction, where one is needed, is the stick's heading (or, with no finger down, toward the
  nearest enemy), as docs/46's Phase Dash. A one-thumb player cannot point at a target.
- **Skill 2 deepens the hero's identity, skill 3 covers its weakness.** The Vanguard gets more armour, then a way across
  the floor and out of a surround; Ilya more fire and a slowed crowd, then a ward for a 90-health caster at the boss.
- **No projectile objects** (docs/42: "Sword, Staff and Daggers are instant hits: no projectile objects in V0.1"). Every
  strike is an instant `Targeting.CollectNear` (a circle, or a capsule along a path); the one lasting effect, the Sigil,
  is a stationary floor zone read on a clock like the pit's band - one pooled object, no movement, no collision.
- **The new damage scales with the build**: a skill's damage is its base x the hero's first weapon's damage over that
  weapon's base (what the Force tome and the damage lines have added), read at the press - the duel core already scales
  skills this way (`Scaled`). Burn stays flat, as `BurnRule` has it.
- **Gate bosses are not dazed, are slowed by half the slow and take half the glyph's heat** (the heat since phase 2,
  section 13.2): the Warden's and the Regent's telegraphs stay the
  bosses' clocks (docs/41 step 4, docs/50).
- **Cooldowns are the skill's own**, shortened only by docs/54's efsun line (`GearStat.AbilityCooldown`, capped at 10 %).
- **A skill that reads maximum health reads the hero's own**, also in the arena, whose x8 health is the arena's knob
  (docs/55 3.2): measured in an earlier round of this research, the first draft's 30 % ward read off the arena's
  stretched health became 216 points and won every duel alone; the core now reads the hero's own.
- **Skill 1 is unchanged.** The shipped Forge Burst and Crucible Vent keep their numbers, their button and their
  autopilot rule, which is what keeps the default batch byte-identical (section 6).

### 3.1 The Vanguard - melee and armour (skill dependence 1, docs/40 4.1)

#### Forge Burst (skill 1, shipped)

- **Role and fantasy:** the anvil slammed down - a shockwave round the hero. Crowd damage.
- **Numbers:** 20 to every enemy within 2.5, every 8 s (`Ability_ForgeBurst`). Unchanged.
- **Measured:** the careful pilot fires it 2.0-3.1 times a minute for about 30 a use (at least 1.5 enemies caught):
  2.7 % of its damage on floor 1, 0.9 % on floors 2 and 3 (section 2).
- **Duel:** a `Strike` of 20 within reach + 0.4 (docs/55's fighter, untouched).

#### Iron Resolve (skill 2, opens at floor 2's first clear)

- **Role and fantasy:** the Vanguard plants its feet and tempers its plate: for a moment it is the wall. The guard
  docs/40 4.1 already named for it (Iron Resolve, 3 s of 35 % damage reduction, 28 s).
- **Numbers (proposal):** **+100 armour for 4 s, every 20 s.** It rides `HeroStats`' armour pool (reduction = armour /
  (armour + 100)) through the one body the scene and the simulation share (`HeroStatsBinding` into
  `HealthState.SetDamageReduction`), so it is a timed `StatModifier`, not a new damage path. On a bare hero that is
  **half the damage taken**; with two common Iron Skins (40 armour, 29 %) it lifts the reduction to 58 % (+29 points):
  the pool's diminishing return keeps it from stacking into immunity. Uptime 20 %. The first draft was +60 (37.5 %);
  the Descent's replay (section 6.3) found it worth about two thirds of Ilya's 2 s ward, and +100 brings the two
  together.
- **Forgiving on purpose.** Four seconds covers a fight's next few blows whenever it is pressed: the Vanguard is the
  skill-1 hero (docs/40 4.1), and its guard should work for a player who presses it late.
- **On the phone:** the puppet's plates flash steel-white and hold a blue sheen for the 4 s (blue is what reads on the
  Foundry's orange, docs/45 7); a ring of rivets stamps the floor under the hero (radius 1, 0.3 s); a small shield glyph
  sits over the health bar and drains with the guard; an anvil clang. The button's ring stays lit while the guard holds.
- **The autopilot:**
  - careful (and standing, first-card): the first blow of a fight with three or more enemies within 2, any blow under
    70 % of the maximum, and at the Warden or the Regent when the blow is due within the guard
    (`RiftbladeRules.BlowDueWithinGuard`'s reading) and the escape has not cleared the mark - between the replay's two
    rules, which differ by two (pressed on cooldown, the guard is worth twice what it is pressed under 70 %);
  - clumsy: whenever it is ready with an enemy within 2, hit or not (section 6.1's `TimesTheSkills` habit is what it
    lacks);
  - auto-farmer: never (no active skill, docs/40 9.4).
- **Measured, the guard laid over the shipped runs** (a first-order replay of the hero's health, section 6.3): **+4.0 %
  effective health for the careful Vanguard pressing it under 70 %, +9.2 % pressing it on cooldown**; of the clumsy
  Vanguard's 139 deaths, **41-44 are given 5 s or more** at the killing moment (14-16 of them at the boss).
- **Projectiles:** none.
- **Duel:** a `Guard` of 500 per mille for 240 ticks, careful under 70 % with the foe at hand. Alone beside the Burst it
  wins **92.8 % of single duels and 99.3 % of best-of-threes** against the shipped kit (measured): about +10 % effective
  health, which an auto-fight turns into most of the wins, as docs/55 3.4 found for gear.

#### Shield Rush (skill 3, opens at floor 3's first clear)

- **Role and fantasy:** shield first through the crowd. The melee hero's missing piece: a way out of a surround and a
  way onto a Captain or into the Warden's open window. Engage and escape in one button.
- **Numbers (proposal):** **4 units in 0.3 s along the stick's heading** (no finger down: toward the nearest enemy within
  6; none: the facing). **15 (scaled) to every enemy within 0.8 of the path, and a 0.5 s daze** (no daze on a gate boss;
  elites are dazed). Walls slide or stop it as they do the walk. **Phase 2 (section 13.2): it stops on impact** - 0.6
  short of the first enemy ahead on its path, striking up to it - where carried through it ended past its target, in the
  pit's band and the Warden's reach. **No guard**: the Riftblade's Phase Dash turns hits
  away (docs/46); the Rush plows and is hit while it does. **Every 12 s.**
- **One body with the Riftblade's dash.** docs/46's Phase Dash is a measured travel rule (`PhaseDashRule`, 3 units in
  0.2 s, in the simulation's `RiftbladeRules`); the Rush is the same travel with other numbers and a strike, so the scene
  gets one `HeroDash` body for both heroes - the stamina lesson (docs/21): one body or the parity breaks.
- **On the phone:** a short steel-blue streak behind the hero and a dust skid at its end; struck enemies flash and
  wobble; a dazed one carries three sparks over its head for the 0.5 s (the crit star's paint, `CombatFeedbackArtSet`).
  The puppet leans into a shield-forward pose for the 0.3 s (one more `PuppetMotion` clip).
- **The autopilot:**
  - careful: **escape** - five or more enemies within 1.5 and its health under 60 %, along the kite's heading when the
    path is clear of the glowing band (`FootworkRule.Kite` and `Guards`); **engage** - a lone Captain beyond the Sword's
    reach with no other enemy within 3 (the Warden in its vulnerability window was dropped in phase 2: rushing it cost
    every timing cohort's Vanguard at the boss, section 13.2);
  - clumsy: on cooldown, toward the nearest enemy - into the crowd, which is how a human misuses a charge, and which the
    relative rule must show does not cost it completions;
  - auto-farmer: never.
- **Projectiles:** none - the hero moves; the strike is one capsule `CollectNear` at the press.
- **Duel:** a `Rush`: closes up to 4 units at once, strikes 15 (scaled) and dazes 30 ticks. Alone beside the Burst:
  **84.4 % / 93.9 %** of a mirror (measured). In the lane it mostly closes on a kiter; its escape, its PvE half, has no
  lane to show in.

### 3.2 Ilya, the Cinder Arcanist - the caster (skill dependence 3)

#### Crucible Vent (skill 1, shipped)

- **Role and fantasy:** every burning enemy near Ilya erupts at once - the payoff of Ember Memory's burn.
- **Numbers:** 8 per Burn stack, to the burning enemy and everything within 1.2 of it, within 3, every 14 s
  (`Ability_CrucibleVent`). Unchanged.
- **Measured:** 0.7-1.0 uses a minute for the careful pilot (three burning enemies in reach is a high bar), 27-29 a
  use, 0.3-0.6 % of Ilya's damage; the burn it spends is itself 1.7-3.1 % (section 2).
- **Duel:** docs/55's Ilya never burned (its Vent was a plain strike of 8). With Ember Memory modelled (every Staff hit
  adds a stack; `BurnRule`'s 3 a second a stack in ticks of 0.5 s, 3 s refreshed), the Vent spends the foe's stacks for 8
  each. One on one there are no neighbours, and a spent burn rebuilds over about four Staff hits, so a Vent is a burst
  more than a gain (section 7).

#### Flame Sigil (skill 2, opens at floor 2's first clear)

- **Role and fantasy:** Ilya draws a furnace glyph on the floor and the crowd that crosses it wades through fire. The
  setup and control half of the burn loop: docs/40 4.1's and docs/45's "Flame Sigil (25 s): 5 s alan".
- **Numbers (proposal):** **a glyph of radius 2.5 at Ilya's feet for 5 s, every 20 s. Inside it enemies walk 40 % slower
  (a gate boss 20 %), their burn deals double, and an unburnt enemy catches a direct stack every 0.5 s** (direct, so its
  fall passes the burn on through Ember Memory's one generation). The glyph stays where it was drawn: Ilya kites out of
  it and the crowd that follows crawls through it.
- **Shaped by the duel and the Descent (measured).** At radius 2.0 (the first draft) it never touched a foe standing
  at its own weapon's reach - the Sword's 2.1, the Staff's 2.4 - so a duel with it read exactly as one without (48.2 %
  of a mirror, the shipped kit's own). A glyph that only added stacks still added nothing (50.2 %): the Staff's strikes
  already hold a foe at the 5-stack cap. Doubling the burn inside is the version that is not capped, and the one a
  player sees (flames taller inside the ring). But in the run, burn is only **2-3 % of Ilya's damage** (section 2), so
  the glyph's run value is the **slow**, not the heat.
- **A slow, not a knockback** (here and wherever a skill moves enemies). A push would move enemies against
  `PackMotion`'s spacing invariant ("no step ever brings an enemy nearer than the spacing to another living enemy"),
  which the scene and the simulation share and the parity tests pin; a slow is a shorter step toward the same point,
  which the invariant already allows: a `SlowStatus` beside `BurnStatus`, read in the pack's step. Cheaper, safer, and
  for a kiting caster the same gift: distance.
- **On the phone:** an ember-orange ring of furnace runes, ignited in 0.2 s from Ilya's feet outward (the Vent's ember
  palette, never the Staff's violet, docs/45 8); enemies inside draw their flames at the "hot" tint and a size up and
  leave a short trail of soot as they wade; the ring dims over its last second. One floor decal, drawn under the actors
  like the pit's band.
- **The autopilot:**
  - careful: drawn when four or more enemies are within 2.5, or beside the gate boss; then the fight is held across it -
    the **Lures** habit's body, which already draws node fights to the pit's rim, with the glyph as the rim;
  - clumsy: on cooldown with any enemy within 2.5, no lure;
  - auto-farmer: never.
- **Projectiles:** none: a stationary zone on a 0.5 s clock (`BurnRule.TickSeconds`), one `CollectNear` a tick.
- **Duel:** a `Sigil` (radius 2,500, 300 ticks; a foe inside walks 400 per mille slower, its burn ticks x2, a stack every
  30 ticks). Alone beside the Vent: **92.7 % / 99.3 %** of a mirror (measured), the strongest single skill of the six in
  the lane, where a foe must stand in it to strike. Its slow changes nothing there (the same 92.7 % without it): two
  kiting casters barely walk. The slow is for the run.

#### Cinder Ward (skill 3, opens at floor 3's first clear)

- **Role and fantasy:** ash and cinders whirl up round Ilya into a shell that blows break on - and the blows come back
  as fire. The glass cannon's guard (90 health, docs/45). Ilya's weaker cohorts die **at the boss** (clumsy: 69 of its
  100 deaths, auto-farmer: 184 of 194; the Vanguard's die in the crowd), and a ward is the measured answer (section 6.3).
- **Numbers (proposal):** **a ward absorbing 20 % of Ilya's maximum health (18 at 90: about one Warden slam, which takes
  20 %, `WardenRule.HeroShare`) for 2 s, every 20 s; every blow it absorbs gives the striker 2 burn stacks** (direct).
  An absorb, not a reduction: the Vanguard's guard is armour, a share of every blow for a forgiving 4 s; Ilya's is a
  pool, drawn over the health bar, for **2 s that must be timed** - the slam's telegraph is the cue. Skill dependence 3
  against the Vanguard's 1 (docs/40 4.1), in one number. It hooks into `HealthState.ApplyDamage` after the armour, the
  one body the scene and the simulation share.
- **Sized by the Descent's replay (measured, section 6.3).** The first draft (30 % for 4 s) absorbed every blow of its
  window: a 4 s window rarely holds more than a tenth of the hero's health, so even a 10 % ward soaked nearly all of
  it, worth 2.5 times the 37.5 % guard. The **window's length**, not the ward's size, binds: at 2 s the ward is worth
  what Iron Resolve's 50 % for 4 s is (+4.0-5.3 % effective health for the careful pilots).
- **On the phone:** a shell of ash-white flakes and orange cinders orbiting Ilya (the robe's colour, one sprite ring,
  with the three ember flasks of docs/45 3 brightening); an ash-white bar laid over the health bar that drains as the
  ward is spent (docs/55 3.2's "the shield drawn over them"); each absorbed blow flares ember at the striker; the
  goggles' flame shows its "angry" face (docs/45 1).
- **The autopilot:**
  - careful (standing, first-card): at the gate boss when its blow is due within the ward's 2 s (the telegraph's
    progress, as docs/46's dash is timed), and a blow taken under 70 % of the maximum elsewhere;
  - clumsy: whenever it is ready with an enemy within 2, hit or not - for a 2 s ward, mostly wasted;
  - auto-farmer: never.
- **Measured, the ward laid over the shipped runs** (section 6.3): **+5.3 % effective health for the careful Ilya
  pressing it under 70 %, +11.8 % on cooldown**; of the clumsy Ilya's 100 deaths, **45-51 are given 5 s or more, 34-36 of
  them at the boss** - the deaths it was chosen for. (The replay presses on blows, not on telegraphs; a timed press at
  the slam is better than either rule.)
- **Projectiles:** none.
- **Duel:** a `Ward` (200 per mille for 120 ticks, 2 stacks to a striker), careful under 70 % with the foe at hand, as
  docs/55's Shield, its size read off the hero's own 90 health, not the arena's 720. Alone beside the Vent: **69.5 % /
  80.1 %** of a mirror (measured).

**Considered and kept as a measured alternative: Backdraft.** A ring of radius 2.5 round Ilya: 6 (scaled), 2 burn
stacks, the walk slowed 50 % for 2.5 s, every 16 s - the guard by space rather than by a shell. It is the more elegant
caster's tool, but its slow does nothing against what kills Ilya's weaker cohorts (the boss's blows, whose telegraphs
are the boss's own clock), and the Sigil now carries the slow. In the duel: **60.1 % / 67.4 %** alone beside the Vent,
and Ilya's three with Backdraft win **42.1 % / 38.1 %** against its three with the Ward. It costs the same to build;
the lead may prefer it (lead question 10).

### 3.3 The six side by side

| Hero | Skill | Opens | Kind | Cooldown | Effect (proposal) | Duel: alone beside skill 1, mirror (duel / Bo3) |
|---|---|---|---|---|---|---|
| Vanguard | Forge Burst | start | burst | 8 s | 20 within 2.5 | - |
| Vanguard | Iron Resolve | floor 2 | guard | 20 s | +100 armour 4 s (half damage bare) | 92.8 % / 99.3 % |
| Vanguard | Shield Rush | floor 3 | charge | 12 s | 4 units in 0.3 s, 15 on the path, 0.5 s daze | 84.4 % / 93.9 % |
| Ilya | Crucible Vent | start | payoff | 14 s | 8 a stack within 3, splash 1.2 | - |
| Ilya | Flame Sigil | floor 2 | zone | 20 s | 2.5 glyph 5 s: slow 40 %, burn x2, a stack every 0.5 s | 92.7 % / 99.3 % |
| Ilya | Cinder Ward | floor 3 | ward | 20 s | absorbs 20 % of maximum health 2 s; strikers catch 2 stacks | 69.5 % / 80.1 % |
| (Ilya) | (Backdraft, considered) | - | ring | 16 s | 6 and 2 stacks within 2.5, slow 50 % 2.5 s | 60.1 % / 67.4 % |

**Measured run value (phase 2, section 13):** the guards carry it, as section 6.3 modelled. Iron Resolve keeps 4-5 % of
the careful Vanguard's damage taken off (11 % of the clumsy one's on floor 1); Cinder Ward absorbs 7-9 % of careful
Ilya's (10-12 % of the clumsy one's); the glyph's heat adds 0.2-0.5 % to careful Ilya's damage and slows 1.2-1.4 enemies
a tick; the Rush deals 1.0-1.9 % of the clumsy Vanguard's damage, and the careful pilot presses it about once in four
runs. In the run: careful +1.5 points of completion, first-card +3.9, standing +4.3, clumsy +15.0 (section 13.3).

**Modelled run value** (section 2's measured inputs; phase 2 measures it; kept for the record). The new damage is small
at these numbers, as the shipped skills' is: the Rush's 15 to the one or two enemies a 4-unit path crosses (its capsule
is 0.43 of the Burst's circle), about twice a minute, is about 1 % of the Vanguard's floor-1 damage, and with build
scaling it stays near that on floor 3 instead of falling to a third; the Sigil's doubled burn, on the share of the crowd
inside the glyph for 5 s of every 20, adds a few tenths of a per cent to Ilya's 2-3 % of burn. **The run value is in the
guards, the escape and the slow**: section 6.3's replay puts each guard at **+4-5 % of effective health** for the
careful pilot (+9-12 % pressed on cooldown); the Rush's escape and the Sigil's slow need the simulation (phase 1) to be
read at all.

## 4. Unlock and progression

### 4.1 How a skill opens

| Option | Pace | Gold sink | For the weaker cohorts | Onboarding | Cost |
|---|---|---|---|---|---|
| **A. First clear of floors 2 and 3** (docs/54 14) | earned by depth | none | they clear floor 2 less often (below) | one new button at a time, when the player is ready | profile flags |
| **B. Bought at the Forge** | any time | small (one-off) | buyable by grinding gold | three buttons possible on run 1 | a Forge tab |
| **C. The clear opens it, the Forge forges it** (recommended) | earned by depth | small, early | the gold of the clearing run pays it | as A | A + B |
| D. Per-hero quests (docs/40 13) | slow | none | hardest | good | a quest system |

**Recommended: C.** The first clear of floor 2 opens skill 2 for every hero (the profile's `deepestFloorCleared` is
profile-wide, as the map starts are, docs/53); the Forge then forges it **per hero**: **300 gold for skill 2, 500 for
skill 3**. A run that first clears floor 2 banks about 340 (docs/54 1: 180 + 159) and a completed one about 500, so the
skill is bought with the gold of the run that opened it, on the result screen's way to the Forge. It is a small sink
(1,600 gold for both heroes' four skills), but an early one: today the Forge sells 530 gold of goods in all (docs/54 1)
and gold piles up after the first completed run.

How long the gates take, from the batch (seeds 1-200, typical hands; the share of runs that clear the floor, so the
expected runs to a first clear is about one over it):

| Cohort | Vanguard: clears floor 2 / floor 3 | Ilya: clears floor 2 / floor 3 | Runs to open skill 2 / skill 3 (with its better hero) |
|---|---|---|---|
| careful | 191 / 190 of 200 | 195 / 194 | about 1 / 1 |
| clumsy | 65 / 61 | 106 / 100 | about 2 / 2 (Ilya); 3 / 3 with the Vanguard |
| auto-farmer | 6 / 5 | 12 / 6 | 17 / 33 - and it presses no skill |

The careful player opens both on its first Descent, the clumsy one within two or three. A skill opens for the
profile, so a player opens it with whichever hero clears; the Forge then sells it to each hero. The weakest cohort is
helped by gear (docs/54's cheap first rungs), not by buttons it would not press.

### 4.2 What gear touches

Only docs/54's **ability-cooldown efsun line** (-2 to -6 % a line, capped at **10 %** over the whole gear), on all
three skills alike. No line, no basma stat and no booster touches a skill's damage, radius or duration. That keeps the
gear index (DPS x EHP - 1, docs/54 7) the only place gear power is counted. Modelled, the capped line is worth about
+11 % skill uptime (1 / 0.9), which on the measured run values (sections 2 and 6.3: guards worth +4-5 % of effective
health, damage skills about 1 % of damage) is under +1 % of the hero's power index.

### 4.3 Ranks: later, capped, no failure

Metin2's skills climb for months (section 1). A light version is the long gold sink the owner wants, and it should come
**after** the six skills are measured and shipped at rank 1:
- **Five ranks, I to V**, per hero and skill; each rank adds **+6 % of the skill's own main number** (damage, armour,
  the glyph's heat and slow, the ward's size), so **rank V is +24 %**. Never the cooldown: that axis stays the efsun
  line's.
- **Costs**: ranks II-V at **150 / 300 / 600 / 1,200 gold**, and from rank IV a floor material (rank IV a Quicksilver
  Bead, rank V a Gloam Spore), so depth gates the top as it does basma (docs/54 4.2). **No failure, no wait**: basma is
  the game's gamble, and the owner chose "not too punishing" there.
- **Size of the sink**: 2,250 gold a skill, **6,750 a hero** for all three to rank V, 13,500 for both heroes - about 27
  completed careful runs, against 16,100 for one item to +9 (docs/54 0).

**The power cap.** Modelled on the measured values (sections 2 and 6.3): the guards are worth +4-5 % of effective health
to the careful pilot (+9-12 % pressed on cooldown) and the damage skills about 1 % of damage each. Rank V's +24 % on
those is **about +1-3 % EHP and under +1 % DPS**; the capped cooldown line adds 11 % uptime to the same; together
**+2-4 % on the power index** at most. docs/54's ceiling (+51 %, gear only) then reads about +55 % for "gear and
skills", and the difficulty tiers (docs/54 8) should be matched to that combined number (lead question 6). In the arena
it is another matter (section 7): rank V wins **80 %** of a Vanguard mirror and the line at -10 % **83 %**, so ranked
plays every skill at rank III and ignores the line.

## 5. The input

### 5.1 Options

| Option | Control | Mis-taps | One-hand play | Readability | The arena's tapped skills (docs/55 P2b) | Build |
|---|---|---|---|---|---|---|
| **Three buttons in an arc** (recommended) | full: each skill when the player wants it | adjacent buttons; measurable | the steering thumb lifts to press (as today) | each button shows its own cooldown | the same three | three `AbilityButtonView`s |
| One button cycling to the next ready skill | the order is the game's | none | as today | the face changes under the thumb | unreadable | small |
| One "smart" button (context picks) | none: the decision is the game's | none | as today | hidden | none | a context rule |
| Gestures on one button (tap / swipe up / swipe left) | full | swipes misread under pressure | poor | no cooldown per skill | possible | gesture code |
| Skills 2-3 automatic (Megabonk-style) | none | none | best | fine | none | the careful rules in the game |

The cycling button is the trap: it fires Iron Resolve and Cinder Ward as attacks, when nothing is hitting yet - the two
skills whose whole value is their moment. A smart button takes the decision that makes a skill a skill.

### 5.2 The layout (S23, the 1080-wide canvas: one canvas unit is one pixel, 16.7 px a millimetre)

| Slot | Size | Anchored bottom-right, pivot (1, 0) | Centre | Physical |
|---|---|---|---|---|
| Skill 1 (Burst / Vent) | 234 | (-40, 68), as today | (923, 185) | 14.0 mm |
| Skill 2 (Resolve / Sigil) | 176 | (-90, 341) | (902, 429) | 10.5 mm |
| Skill 3 (Rush / Ward) | 176 | (-306, 160) | (686, 248) | 10.5 mm |

The two smaller buttons sit on an arc 245 units from skill 1's centre, at 95 and 165 degrees: up and to the left, the
right thumb's natural sweep from its rest on skill 1, at least 40 units (2.4 mm) from skill 1's edge and 106 from each
other. Both are above Material's 48 dp (about 9 mm) and Apple's 44 pt. The cluster covers the right 45 % of the bottom
quarter of the screen; the rest of the screen stays the stick (a press anywhere that is not a button, as today).
- **A locked slot is hidden**, not greyed: a new player sees one button, then two, then three. The unlock is the
  tutorial (a one-line toast on the first run with the new button: "Iron Resolve: 4 s of iron. Tap when the blows
  come.").
- Each button keeps today's parts: the painted ring and cooldown sprite (`HudArtSet`), the radial fill, the countdown,
  the hero's icon per slot.
- The boss's reveal greys all three and a tap on any skips it (`BossRevealView.HeldButton` reads one button today).
- **One hand.** Hoober saw 49 % of the people he watched touching their phones do it with one hand (36 % cradled, 15 %
  with two). With one hand the steering thumb must lift to
  press, and the hero stops - true of today's one button too. The device session counts it (telemetry below) before
  anything is built for it; an automatic option for skills 2-3 (the careful autopilot's rules, which the careful cohort
  already measures, so it cannot exceed measured power) is the fallback, not the default (lead question 7).

### 5.3 Telemetry

`ability_used` gains `slot` (1-3) and the skill's own `ability_id`; `run_start` lists the opened skills; `run_end` counts
uses per slot. New: `ability_refused` (a press while cooling or held - a mis-tap or impatience), `stick_on_button` (a
drag that began on a button and so did not steer), and `ability_wasted` (a Burst, Rush or ring with nothing in reach).
The S23 session reads these against the autopilot's numbers, as docs/46 asked for the dash.

## 6. Balance plan

### 6.1 The cohorts and the simulation

- **The skills join the simulation as levers** (`FoundryLevers.Skills`: 1, 2 or 3, default 1 = the shipped kit;
  per-skill knobs for every number in section 3). The scene and the simulation share each rule body (`HeroStats` for the
  guard, `HealthState` for the ward, one `HeroDash`, a `SlowStatus` read by `PackMotion`'s step, a `SigilZone` on
  `BurnRule`'s clock), with parity tests on the Editor's six seeds, as every rule before.
- **The cohorts use the new skills** by section 3's rules. One habit is added: **`TimesTheSkills`** (append-only, as
  `AutopilotCohort.Habit` requires): the careful, standing and first-card pilots have it and time skills 2 and 3 as
  section 3 says; the clumsy pilot lacks it and presses them on cooldown whenever something is in reach - docs/40 9.4's
  Casual, and a cohort that drops a habit the careful one has (lead question 2). **Skill 1 keeps today's rule for every
  cohort**, so the clumsy cohort's shipped runs do not move. The auto-farmer uses no skill.
- **Human hands** (`PilotRealism`): a press waits the realism's reaction time and is missed in its lapses, as the
  Riftblade's dash was read.
- **What each skill is measured by**: its damage share and damage a use (section 2's table), uses a minute, deaths by
  floor and phase, the bosses' fights, the elites' time under fire, slams and lanes landed, and the Descent's minutes.
  R_skill (docs/40 4.2) is reported, not targeted: docs/46 showed it cannot separate heroes on the Warden.

### 6.2 The rows a skill must pass

Measured in phase 2: section 13.4. The relative ship rule's row is docs/56's rule since 2026-11-17 (lead decision 3 of
docs/56 and of this document).

| Row | Target | Reading |
|---|---|---|
| **Skills off: byte-identical** | the default batch (`FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_REALISM=typical`, `FOUNDRY_FLOORS=4` and `2`): every file byte-identical but `foundry-batch.md`'s clock and wall seconds, as docs/52 read the Wraith | no new `RunRandom` purpose drawn, no trace line, no result field printed while `Skills` is 1 |
| **The relative ship rule** (docs/50, docs/52) | skills on, against the shipped kit on the same seeds 1-400: the clumsy cohort loses at most 1 four-floor completion in 800; careful floor-3 clears within 3 on each range | catches a skill that hurts (the clumsy Rush into the crowd); **docs/56's study of the rule is pending** and may restate it for changes that help |
| The bosses' bands | careful P50 45-65 s for the Warden (floors 1 and 3) and the Regent, either hands; none felled within 5 s | three skills must not make the gate boss stop being a fight |
| The Descent's length | completed runs within 13-16 minutes (docs/47), P50 within 1 minute of skills off | |
| Each new damage skill | its share of the hero's damage holds from floor 1 to floor 3 (it scales, where the Burst's falls from 2.7 % to 0.9 %) and stays under 8 % | visible on every floor, never a new weapon |
| The defensive skills | deaths by floor and phase reported against skills off; the replay's numbers (section 6.3) are the expectation | the clumsy Vanguard's deaths in the halls, the clumsy Ilya's at the boss |
| Skill dependence | the careful cohort's gain from skills 2-3 larger for Ilya (3) than for the Vanguard (1), read in deaths and boss seconds | docs/40's order, held to measurements as docs/46 did for Nix |
| Parity | the simulation on the Editor's six seeds as close as for the shipped kits | |
| Device | p95 at most 20 ms with 30-40 alive, a glyph and three buttons; no frame over 35 ms | docs/41 step 1's row |

### 6.3 What is measured already

Section 2's tables are the shipped kits' baseline. The guards are measured as far as they can be without building
them: `HeroSkillsResearch` plays the shipped Descent (seeds 1-200, typical hands, every cohort and both heroes) and
replays each run's health with a guard laid over it - the same blows, cut inside the guard (a first-order replay: the
enemies do not answer it). Two rules: **careful** (a blow taken under 70 % opens it) and **on cooldown** (the first blow
it is ready for). Because a replay without feedback outlives nearly every death by the damage it took off earlier, a
death is read as **the seconds the guard buys at the killing moment** (the replayed health left over the last 10 s's
rate of damage).

| Guard (every 20 s) | Careful Vanguard: EHP, careful / on cooldown | Careful Ilya | Clumsy Vanguard: deaths given 5 s+ of 139 (at the boss) | Clumsy Ilya: of 100 (at the boss) |
|---|---|---|---|---|
| Iron Resolve 37.5 % 4 s (the first draft) | +3.1 % / +6.8 % | +3.3 % / +7.1 % | 28-34 (11-12) | 36-41 (24-28) |
| **Iron Resolve 50 % 4 s** (proposed) | **+4.0 % / +9.2 %** | +4.5 % / +9.7 % | **41-44 (14-16)** | 45-50 (33-34) |
| Cinder Ward 30 % 4 s (the first draft) | +8.0 % / +20.4 % | +8.8 % / +21.5 % | 69-82 (21-24) | 74-77 (52-53) |
| Cinder Ward 10 % 4 s | +7.3 % / +18.6 % | +8.4 % / +20.4 % | 61-74 (18-23) | 71-74 (49-51) |
| **Cinder Ward 20 % 2 s** (proposed) | +4.7 % / +10.8 % | **+5.3 % / +11.8 %** | 36-39 (13-14) | **45-51 (34-36)** |
| Cinder Ward 10 % 2 s | +4.3 % / +10.1 % | +5.1 % / +11.3 % | 32-37 (12) | 45-51 (34-36) |

What it says:
- **Pressing early is worth twice pressing late.** On cooldown (the first blow) every guard is worth about twice what it
  is under the 70 % rule: blows come in fights, and a guard opened at a fight's first blow covers its next ones. The
  careful rule in section 3 sits between the two, and the device session reads how players press.
- **An absorb's worth is its window, not its size.** A 4 s window rarely holds a tenth of the hero's health, so a 10 %
  ward soaks nearly as much as a 30 % one; halving the window halves it. That is what sized Cinder Ward (2 s) and why
  Iron Resolve was raised to 50 %: the two guards are now worth the same to the careful pilot.
- **Each guard reaches the deaths it was chosen for.** About a third of the clumsy Vanguard's deaths, and half of the
  clumsy Ilya's (most of them at the boss), are given 5 s or more. Not saved: 5 s at the killing moment's pace is time
  to walk out of a crowd or finish a boss, which the simulation with the skills in it (phases 1-2) will read as clears.
- **The auto-farmer would gain as much** if it pressed (its rows in the report), which it never does: the skills widen
  the gap between a player who presses and one who does not - docs/40's skill dependence at work.

The duel's numbers are section 7's. Everything else in section 6.2 waits for phase 1.

## 7. PvP: the skills in the arena

docs/55's duel core now plays the six (`ArenaDuel`'s appended kinds: `Guard`, `Rush`, `Vent`, `Sigil`, `Ward`, and
`Repel` for Backdraft; Ember Memory's burn; a bold policy; a cooldown line; every docs/55 fight bit-identical, pinned).
Measured, seeds 1-2000, careful unless noted (`TestResults/hero-skills-duel.md`):

| Reading | Vanguard | Ilya |
|---|---|---|
| Mirror, one skill each (P50 length) | 49.5 % (28.8 s) | 48.2 % (19.6 s) |
| Mirror, three each (P50 length) | 48.1 % (30.7 s) | 47.8 % (18.1 s) |
| **Two skills against one** (duel / Bo3) | **92.8 % / 99.3 %** | **92.7 % / 99.3 %** |
| **Three against one** | **99.3 % / 100 %** | **96.5 % / 99.9 %** |
| Three against two | 92.7 % / 98.6 % | 64.8 % / 76.7 % |
| Careful against bold, three each | 48.1 % | 47.8 % |
| A carries the cooldown line -10 % | **82.5 % / 94.9 %** | 51.7 % / 56.2 % |
| A at rank V on all three | **79.9 % / 92.2 %** | **74.0 % / 86.9 %** |
| Rank V and the line together | 98.5 % / 100 % | 75.8 % / 88.8 % |
| Gear's small edge (+5.6 % power), one skill / three | 72.7 % / 77.3 % | 77.1 % / 77.7 % |

| The Vanguard (A) against Ilya (B) | A's wins: duel / Bo3 |
|---|---|
| docs/55's fighters (Ilya without its burn) | 99.9 % / 100 % |
| one skill each, Ilya burning | 0.0 % / 0.0 % |
| three skills each | 1.8 % / 0.1 % |
| three each, Ilya's damage x0.775 | 63.6 % / 70.5 % |
| three each, Ilya's damage x0.8 | **51.7 % / 52.4 %** |
| three each, Ilya's damage x0.9 | 12.2 % / 3.8 % |

What it says:
1. **docs/55's "the Vanguard beats Ilya 98-100 %" was the prototype's Ilya, which never burned.** With Ember Memory's
   flat 3 a second a stack - 15 a second at five stacks, two thirds of the Staff's own 21.8 - Ilya wins every duel. In
   the run burn is 2-3 % of Ilya's damage (section 2): enemies die before it ticks. One on one against an 800-health
   hero it is a third of Ilya's damage. Both readings say what docs/55 3.2 said: every hero needs an arena coefficient.
   Measured, about **x0.8 on Ilya's damage** evens the pair with three skills each; the round-robin tunes it in P0.
2. **Every skill is most of a fight.** One skill more wins 65-93 % of a mirror; a +10 % guard decides as much as
   docs/55's small gear edge did. So the arena gives **everyone all three skills** (the unlock is PvE progression, not an
   arena edge): an arena that let a two-skill player meet a three-skill one would be decided before the lane.
3. **Utility lines and ranks are power in an auto-fight.** The cooldown line at its cap wins 83 % of a Vanguard mirror;
   rank V on all three 74-80 %. **Normalised ranked plays every skill at rank III and ignores the cooldown line**, as it
   ignores gear (docs/55 question 5). An Open arena may count them.
4. **The line's worth depends on the kit**: 83 % for the Vanguard, whose guard and charge simply come sooner; 52 % for
   Ilya, whose rules (four stacks for the Vent, a foe at hand for the ward) bind before the clock does. A line whose
   worth is a policy's accident is one more reason ranked ignores it.
5. **A fuller kit does not soften the gear curve** (72.7 % to 77.3 % for the small edge): docs/55's gear verdict stands.
6. **Careful and bold are even** (48-49 %) with these kits: docs/55's defender policy is a matter of style for now. It
   is not with Backdraft as Ilya's third, whose careful rule waits for a foe inside the kite that a kiting mirror never
   brings: the bold Ilya wins **62 %** (38.1 % / 33.5 % for the careful one). The policy table is tuned per hero and per
   kit, apart from the Descent's cohorts.
7. **A skill that reads maximum health must read the hero's own**: in an earlier round the ward read the arena's x8
   health and won every duel alone (section 3.0).

**What changes in docs/55's design.** The duel's five stand-in skills (Cleaving Arc, Quick Jab, Bulwark, Second Wind,
Bellows) are retired: the attacker's three skills **are** the hero's three. Archero 2's variety - each round dealing
both sides picks of the same quality - comes from **dealt boons** (run-card-like passives: a Force level, a Tempo level,
Vitality), not more buttons. P2b's tapped skills are these three buttons, laid out as in the run.

## 8. Phased build plan (one agent and the lead; days of work)

| Phase | Work | Days | Done when |
|---|---|---|---|
| 0 (this branch) | The design, the duel's six skills, the Descent read | - | this document |
| 1. Core and simulation (**done 2026-11-18**, section 13.1) | `AbilityKind` gains Guard, Rush, Sigil, Ward; `HeroDefinition` three abilities, `AbilityController` three slots; `HeroDash` (one body with docs/46's dash), `SlowStatus` (read by `PackMotion`), `SigilZone` (on `BurnRule`'s clock), the guard as a timed armour modifier, the ward as a pool in `HealthState`, build scaling; the assets; `FoundryLevers.Skills`, the pilot's rules and `TimesTheSkills`; EditMode and parity tests | **5** | skills off: the default batch byte-identical, every test green; the parity table for skills on |
| 2. Measurement (**done 2026-11-18**, sections 13.2-13.4) | section 6.2's rows for each skill and for all three, both heroes, seeds 1-400, both hands; tuning; this document's numbers replaced by measured ones | **2-3** | the rows pass, or the lead chooses which bind |
| 3. HUD, art, telemetry (**buttons and icons built 2026-11-19**, section 14; the four effect strips, the Rush's clip and section 5.3's events open) | three `AbilityButtonView` slots (section 5.2), the reveal's hold over all three, four icons (`build_ability_icons.py`'s family), four effect strips (`build_skill_vfx.py`), the Rush's puppet clip; section 5.3's events | **3** | a PlayMode flow fires each slot; telemetry in the log |
| 4. Unlock and the Forge (**built 2026-11-19**, section 14, but the result screen's line) | save version 6 (opened and forged skills per hero, migration adds empties; version 5 went to docs/54's difficulty tiers), the first-clear opening, the Forge's skills tab with the 300 / 500 prices, the result screen's unlock line | **2** | a profile opens and forges skill 2 after a floor-2 clear; `ProfileStoreTests` round-trip and migrate |
| 5. Device | the owner plays both heroes with three skills on the S23: mis-taps, frame time, and whether it is more fun | **1** + the owner | the owner's verdict, telemetry read |
| 6. Ranks (later) | ranks I-V, save, the Forge's rank strip, the simulation's rank lever, the power index note | **2-3** | rank V inside section 4.3's +2-4 % |
| Arena (in docs/55 P0) | the duel's kinds move into `Cryptforge.Arena`; per-hero coefficients by round-robin; dealt boons | inside P0's 12-15 | P0's own gate |

Phases 1-5: **12-14 days**. Phase 1 must land before docs/55's P0 chooses its skills; docs/27 decision 12 (characters
after the V0.1 gate) applies to this as to every character change: the device row of the gate comes first.

### 8.1 Risks

| Risk | Effect | Mitigation |
|---|---|---|
| Three skills make the run easy | the bosses' bands and the 14-minute Descent move | section 6.2's rows; numbers are starting values |
| The clumsy misuse costs completions | weaker players lose runs to a new button | the relative rule reads exactly this; the Rush's clumsy rule is written to show it |
| Thumb mis-taps, one-hand players stopping | frustration | the arc's spacing, telemetry, the device session; automatic skills 2-3 as a fallback |
| Frame time with glyphs, rings and statuses on 40 enemies | the device row | one pooled decal, statuses as data on existing components, measured on the S23 |
| PvP balance per skill | a recurring tuning cost (docs/55 6.1) | normalised ranked, the round-robin, per-hero coefficients and policies |
| The Rush and Phase Dash drift apart | parity breaks | one `HeroDash` body |

## 9. Questions for the owner

Four, each with the recommended answer first.

1. **Are these the skills you want?** The Vanguard keeps its anvil slam and gains **Iron Resolve** (4 seconds of
   much tougher armour) and **Shield Rush** (a short charge through enemies, knocking them dizzy). Ilya keeps its
   Crucible Vent and gains **Flame Sigil** (a fire circle on the floor: enemies in it walk slowly and burn harder) and
   **Cinder Ward** (a shell of ash that soaks up a few hits and sets the attackers on fire). Recommended: **yes**; every
   number is tuned later with the test pilots.
2. **How does a new skill open?** Recommended: **clearing floor 2 (then floor 3) for the first time opens it, and the
   Forge forges it for gold (300, then 500)** - the run that opens it pays for it. Or: free on the clear; or bought with
   gold at any time.
3. **Should skills have ranks, like Metin2's M, G and P, as a long gold sink?** Recommended: **yes, later**, after the
   skills are tried: five ranks, a little stronger each, gold and floor materials, **never failing and never waiting**.
4. **Three skill buttons on the right, or one?** Recommended: **three**, the first big in the corner and two smaller
   beside it, each appearing when it opens. Please tell us whether you play with one hand or two: with one hand the hero
   stops while you press, and that decides whether skills 2 and 3 also get an automatic option.

## 10. Questions for the lead

1. **Build scaling** for the new skills' damage (section 3.0). Should skill 1 (the shipped Burst and Vent) scale too? It
   would change the shipped Descent: recommended as a measured lever under the relative rule, not part of phase 1.
2. **`TimesTheSkills`**, a new habit the clumsy cohort lacks, for skills 2-3 only (6.1). Agree, or keep the clumsy
   cohort's skills on the careful rules?
3. **The upper side of the ship rule.** The relative rule bounds harm; skills help. Wait for docs/56, or set an interim
   bound (the bosses' bands and the 13-16 minutes)?
4. **Slow, not knockback**, for the Sigil and any later skill that moves enemies (PackMotion's invariant). Agree?
5. **One `HeroDash` body** for Shield Rush and docs/46's Phase Dash, built with the Rush even if the Riftblade waits.
6. **The power cap**: count skill ranks with gear in the tiers' matched power (about +55 % at the top), or hold docs/54's
   +51 % by trimming a gear cap?
7. **Automatic skills 2-3** as an option only if the device session shows one-hand players stopping too often. Agree?
8. **The arena**: retire docs/55's five stand-in skills for the heroes' three; dealt boons for variety; ranked at rank
   III, no cooldown line; Ilya's coefficient near x0.8 as the round-robin's start; skills read the hero's own maximum
   health, never the arena's x8.
9. **Save version 5** for skills (version 4 just shipped with gear), or fold the fields into 4 while no build has
   shipped it to players?
10. **Ilya's third: Cinder Ward or Backdraft.** The ward answers what kills Ilya's weaker players (the boss); Backdraft
    is the more caster-like tool and wins less in the lane (section 3.2). Recommended: the Ward.

## 11. Lead decisions 2026-11-17

The lead takes section 10's recommendations:

1. Skill 1 (Burst, Vent) keeps its shipped numbers in phase 1; its build scaling is a measured lever under the
   relative rule, later.
2. `TimesTheSkills` is added for skills 2-3; the clumsy cohort lacks it.
3. Until docs/56 lands, the upper bound is the bosses' 45-65 s bands and the 13-16 minute Descent.
4. Slow, never knockback (PackMotion's spacing invariant).
5. One `HeroDash` body, built with Shield Rush; the Riftblade's Phase Dash reuses it when it comes.
6. Skill ranks count with gear in the tiers' matched power (about +55 % at the top); no gear cap is trimmed.
7. Automatic skills 2-3 only if the device session shows one-hand play stalling.
8. The arena retires docs/55's stand-in skills for the heroes' three, deals boons for variety, plays ranked at rank III
   without the cooldown line, starts Ilya's coefficient near x0.8, and the skills read the hero's own maximum health.
9. Save version 5 for skills. **Superseded 2026-11-17: skills take version 6**, since the difficulty tiers (docs/54 phase 6,
   section 21) took version 5 for each tier's record; the skills' migration is 5 -> 6.
10. Ilya's third is the Cinder Ward; Backdraft stays the measured alternative.

Section 9's four owner questions are open; the build (section 8) starts once the owner answers 1, 2 and 4 (3 is later).

## 12. The owner's answers 2026-11-18

1. **The skills: yes**, as section 3 has them - Iron Resolve and Shield Rush for the Vanguard, Flame Sigil and Cinder
   Ward for Ilya; the numbers are tuned with the test pilots.
2. **The unlock: the first clear and the Forge** - clearing floor 2 for the first time opens skill 2 and the Forge forges
   it for 300 gold; floor 3 and 500 gold for skill 3.
3. **Ranks: later**, as recommended (the owner did not object; phase 6 stays "later").
4. **Three buttons**, the first big in the corner and two smaller beside it, each appearing when it opens. The owner
   plays **with two hands** (left thumb moves, right thumb presses), so automatic skills 2-3 are not built (decision
   7 holds them back until a device session shows one-hand play stalling).

The build starts with phases 1-2 (core, simulation, measurement; skills off by default, so the shipped game is
untouched). Save version: docs/54 phase 6 (the tiers) takes version 5, so phase 4 here writes **version 6**. Phases 3-4
ship the skills into the game; docs/27 decision 12 still puts the V0.1 gate's device row first.

## 13. Phases 1 and 2, built and measured (2026-11-18)

The owner's answers (section 12) started phases 1 and 2. Both are done on this branch: the skills are in the core, the
scene and the simulation, **off by default** (no build, scene test or report plays more than skill 1), and they are
measured with every playing cohort, tuned on the spent seeds 1-400 and confirmed once on the fresh block 8401-11600
under docs/56's rule. Nothing of phases 3-4 is built: no button, icon, effect strip, telemetry event, save field, unlock
or Forge tab.

### 13.1 What phase 1 built

| Where | What |
|---|---|
| `Scripts/Combat/AbilityRuntime.cs` | `AbilityKind` gains `Guard`, `Rush`, `Sigil`, `Ward` (appended; the assets store the number); `AbilityRuntime.Skill` makes one with its `Duration` and `Amount`; `ActiveLeft` / `IsActive` keep its effect's clock from each use. The Burst and the Vent are untouched |
| `Scripts/Combat/SkillRule.cs` | what the skills of a kind share, each product one never-inlined body and every squared radius cast to float: the build scaling (`Scaled`: base x the first weapon's damage over its base), the gate boss's half slow and half heat, the slowed step, the ward's size, the Rush's capsule, its impact (`Along`, `ImpactTravel`) and the glyph's circle; the daze (0.5 s), the glyph's heat (x2) and stack, the ward's 2 stacks |
| `Scripts/Combat/HeroDash.cs` | one dash body (length, travel, heading; `StepOf`, `TravelFor`, `Move` through `HeroMotion`, walls sliding it as they do the walk): the Rush now, docs/46's Phase Dash when the Riftblade is built (lead decision 5) |
| `Scripts/Combat/SlowState.cs`, `SlowStatus.cs` | an enemy's slow, daze and heat: plain state, and the scene's component on a touched enemy; `PackMotion.SetSlow` shortens its step toward the same point (lead decision 4), `EncounterController` hands each slow to the pack before it steps, the enemy's `AttackController` holds its swing while dazed, its `BurnStatus` ticks at the heat |
| `Scripts/Combat/SigilZone.cs` | the glyph: a stationary zone on `BurnRule.TickSeconds`, ten ticks in its 5 s, each slowing and heating what stands inside until the next and lighting the unburnt |
| `Scripts/Combat/HealthState.cs` | the ward: a pool after armour (`RaiseWard`, `ClearWard`; `WardAbsorbed` names the striker, which catches 2 direct stacks) |
| `Scripts/Progression/HeroStats.cs` | the guard: a timed armour layer (`SetGuardArmor`) on the pool, pushed through `HeroStatsBinding` as every card; at 0 the reduction is the armour's to the bit |
| `Scripts/Combat/AbilityController.cs` | three slots (`SkillAt`, `TryUse(slot)`, `TryUse(slot, heading)`, `InitializeSkills`, `SkillUsed`); slot 1 is the shipped ability, its `Used` event and button unchanged |
| `Scripts/Content/*`, `Data/Abilities/*`, `Data/Heroes/*` | `AbilityDefinition` gains duration and amount, `HeroDefinition` skills 2 and 3; `Ability_IronResolve`, `Ability_ShieldRush`, `Ability_FlameSigil`, `Ability_CinderWard` (no icons yet: phase 3) on both heroes |
| `Scripts/Core/SkillFootwork.cs`, `AutopilotCohort.cs` | the autopilot's moments (section 3), one body for `DevelopmentAutopilot` and the simulation; `Habit.TimesTheSkills` (careful, standing, first-card; not the clumsy cohort; outside `All`, so no cohort report's list of dropped habits moves) |
| `Scripts/Core/CombatSetup.cs`, `DevelopmentStart.cs`, `DevelopmentAutopilot.cs`, `UI/HeroMovementInput.cs`, `Combat/Targeting.cs` | the scene's wiring, live only where `<profile>/development/skills.txt` says `2` or `3` (the Editor and development builds): the guard onto the sheet, the stick and the dash, the gate boss, the glyph's and the ward's burns, the autopilot's presses |
| `Tests/Support/DescentSimulation.HeroSkills.cs` | `FoundryLevers.Skills` (1, the default), `SkillTwoOff`, `TuneSkill` and the phase-2 levers; the run's skills through the same bodies, the pilot's presses a reaction after their moment (`PilotRealism`), never in a lapse or the boss's reveal |
| `Tools/CombatChecks/HeroSkillsDescentResearch.cs` | phase 2's harness (`TheSkillsInTheDescent`, `TheEditorSeedsWithTheSkills`); `Tools/BalanceRule/paired_bootstrap.py` reads its CSV, with the first-card cohort now read |

Tests: `HeroSkillRuleTests` (the pure bodies, 13) and `FoundrySkillsTests` (the simulation off and on, 4) run in every
plain `dotnet test`; `FoundrySimulationSceneValueTests.TheFoundryHeroesAreTheAssets` holds the four assets to the
simulation's copies and `HeroSkillsFlowTests` (PlayMode, 3) plays each slot in the scene - both Unity-only, for the lead.

**Skills off, the default batch.** `FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_REALISM=typical`,
`FOUNDRY_FLOORS=4` and `2`, against main's own (f8f31c99) on this machine: every CSV, `foundry-gate.md`,
`foundry-cohorts.md` and `foundry-finale.md` byte-identical; in `foundry-batch.md` only the clock in its header and the
wall-seconds column differ. No `RunRandom` purpose is drawn, no trace line written and no reported field moves while
`Skills` is 1. CombatChecks 934 green (`Başarılı`; 917 before), the Unity compile check green.

### 13.2 How phase 2 read the skills

`HeroSkillsDescentResearch.TheSkillsInTheDescent` plays the whole Descent (floors 1-3 and the finale) for both heroes
and the four playing cohorts with typical hands (`PilotRealism.Typical`), the skills on from the first frame, against
the shipped kit on the same seeds; the auto-farmer presses no skill, so its runs are the shipped ones. Exploration and
tuning on the spent seeds 1-400 (docs/56: 1-2000 are tuning seeds), slow hands on the careful pilot there too; the one
confirmatory read on the fresh block 8401-11600 (13.3). Each skill was read alone beside skill 1 (`s2`, `s3`) and all
three together (`s23`), as section 3 wrote them; then the candidate.

**Round 1: section 3 as written** (seeds 1-400; completion change against `shipped`, both heroes, in points; the
Vanguard's and Ilya's own changes in completions of 400):

| Option | Careful | Standing | First-card | Clumsy |
|---|---|---|---|---|
| `s2` (Iron Resolve, Flame Sigil) | +0.75 (V +9, I -3) | +2.50 (V +11, I +9) | +3.38 (V +18, I +9) | +8.00 (V +21, I +43) |
| `s3` (Shield Rush, Cinder Ward) | 0.00 (V -5, I +5) | +1.25 (V -13, I +23) | +0.25 (V -14, I +16) | +8.25 (V -6, I +72) |
| `s23` (all three) | +1.62 (V +6, I +7) | +3.62 (V +7, I +22) | +3.38 (V +6, I +21) | +12.75 (V +16, I +86) |

Shipped on these seeds: careful 96 %, standing 88 %, first-card 85 %, clumsy 39 % of 800 completed. What round 1 found:
1. **Three of the four skills help every cohort; the Rush hurts the Vanguard in every one.** The Vanguard's deaths at
   the boss rose with it (careful 12 to 19, standing 39 to 50, first-card 31 to 41, clumsy 65 to 85), and with them the
   pit's (clumsy 80 to 97): carried its whole 4 units through what it met, the charge took the hero past the Warden into
   the pit's glowing band and the Warden's reach; the timing pilots' engage on the Warden "in its window" put them
   there on purpose. The careful pilot never once escaped a surround with it (0 of 2,482 presses: five enemies within
   1.5 and the hero under 60 % is a moment a kiting pilot does not meet).
2. **The glyph's heat on the gate boss shortened its fight to the band's edge**: careful Ilya's floor-1 Warden from
   46.8 to 45.2 s at P50 (the Regent 46.3 to 45.5).
3. **The guards carry the run value, as section 6.3's replay said**: Iron Resolve keeps 4-5 % of the careful Vanguard's
   damage taken off (11 % of the clumsy one's on floor 1), Cinder Ward absorbs 7-9 % of careful Ilya's (10-15 % of the
   clumsy one's); the clumsy Ilya's deaths at the boss fall from 142 to 98 of 400.

**Round 2: the Rush and the heat** (the Vanguard alone with `s3`; completions of 400 against `shipped`, boss deaths in
brackets, shipped 12 / 39 / 31 / 65):

| Rush | Careful | Standing | First-card | Clumsy |
|---|---|---|---|---|
| section 3 (through, the Warden rushed) | -5 (19) | -13 (50) | -14 (41) | -6 (85) |
| stops on impact | +1 (12) | -7 (46) | -6 (37) | +6 (83) |
| leaves the Warden to the weapons | -1 (13) | -9 (45) | -1 (31) | -6 (85) |
| both (the candidate's) | 0 (12) | -9 (47) | 0 (32) | +6 (83) |

The gate boss at half the glyph's heat (careful Ilya, all three, seeds 1-400): the floor-1 Warden 47.1 s, the Regent
46.0, floor 3's Warden 57.3, against 46.8 / 46.3 / 57.0 shipped.

**The candidate, `c1`, and its reasons** (written before its confirmatory read): section 3's numbers with three changes,
each read above on the spent seeds, now the shipped defaults (`SkillRule.RushStopsOnImpact`,
`SkillFootwork.RushesTheGateBoss`, `SkillRule.GateBossHeatShare`):
- **The Rush stops on impact**: 0.6 short of the first enemy ahead on its path, striking up to it (what stands beside the
  hero as it sets off is struck in passing). A shield charge stops at the first body it strikes; carried through, it
  ended past its target, in the pit's band and the Warden's reach.
- **The timing pilot rushes only the lone Captain**, never the Warden in its window: that window is the weapons', at
  their reach, and rushing into it cost every timing cohort's Vanguard at the boss.
- **A gate boss takes half the glyph's heat** (its burn x1.5 inside), as it takes half its slow (section 3.0): the
  Warden's fight stays docs/41 step 4's.
- With them, the glyph's lure is built as section 3.2 says (a live glyph holds a node fight across it, the Lures habit's
  body with the glyph as the rim; `SkillFootwork.AcrossTheGlyph`), which round 1 had not yet.

**`c1` on seeds 1-400** (bootstrap 90 % CI, 10,000 resamples of seeds): careful **+2.12** points (+1.12 to +3.25; V +10,
I +7), first-card **+3.25** (+1.62 to +4.88; V +15, I +11), clumsy **+14.50** (+11.75 to +17.25; V +31, I +85), standing
+2.50 (+0.62 to +4.50; V -3, I +23). The careful bosses: the Vanguard's 51.3 / 49.2 / 61.1 s, Ilya's 47.1 / 46.0 / 57.3;
the Descent's P50 14.5 / 14.3 minutes, as shipped. With slow hands (careful, seeds 1-400): the Vanguard completes 335 of
400 against 303, Ilya 358 against 312; Ilya's bosses already sit under 45 s as shipped with those hands (44.0 / 44.6 on
floors 1 and 2) and the skills move them 0.3 / 0.6 s (43.7 / 44.0); the Vanguard's 48.4 to 48.0.

### 13.3 The confirmation (docs/56's rule, seeds 8401-11600)

Read once, `c1` against `shipped` on the same code (`shipped`'s runs are byte-identical to main's), seeds 8401-11600 -
3,200 seeds, the floor-2 count of docs/56's rule 1, since the skills are on from floor 1 - both heroes, typical hands, the
four playing cohorts; beside it the A/A probe from floor 1 (`aa-f1`: the shipped game, one unread draw of the pilot's dice
as floor 1 begins) for the clumsy cohort. One candidate read. `Tools/BalanceRule/paired_bootstrap.py <folder> 8401 11600`:

| Cohort | Shipped completed | `c1` completed | Change, points (90 % CI) | Vanguard / Ilya (completions of 3,200) | SD per 400 seeds | Verdict |
|---|---|---|---|---|---|---|
| Clumsy | 38.5 % | 53.5 % | **+15.00** (+14.02 to +15.98) | +302 / +658 | 13.5 | passes, an easing |
| Careful | 96.3 % | 97.8 % | **+1.45** (+1.08 to +1.83) | +49 / +44 | 5.2 | passes, an easing |
| First-card | 82.7 % | 86.6 % | **+3.88** (+3.27 to +4.48) | +146 / +102 | 8.4 | passes, an easing |
| Standing | 87.2 % | 91.5 % | +4.33 (+3.69 to +4.97) | +120 / +157 | 9.0 | reported |
| A/A `aa-f1`, clumsy | 38.5 % | 39.3 % | +0.77 (-0.19 to +1.73) | | 13.0 | the noise of a reshuffle from floor 1 |

**`c1` passes docs/56's rule**: for clumsy, careful and first-card the point change is far above -1.0 and the CI's lower
bound far above -1.5 - every CI lies wholly above 0, so under rule 3 it passes as an easing and is reported to the lead.
The easing is an order of magnitude past the A/A's band.

The rest of the read on the same runs:
- **The bosses** (careful P50, share of fights in 45-65 s; none felled within 5 s in any cohort): the Vanguard's floor-1
  Warden 51.1 s (71 %), the Regent 49.0 (66 %), floor 3's Warden 60.1 (62 %), as shipped (51.1 / 49.0 / 60.4); Ilya's 45.8
  (54 %), **45.4** (52 %), 56.5 (75 %), against 45.8 / 46.4 / 57.4 shipped. Every careful band holds; Ilya's Regent stands
  0.4 s inside its edge, a second under shipped.
- **The Descent**: every completed run of every cohort within 13-16 minutes; the careful P50 14.50 / 14.28 minutes against
  14.50 / 14.31 shipped, the clumsy Vanguard's 14.23 against 14.36.
- **Deaths, where section 6.3 aimed the guards** (of 3,200 runs): the careful Vanguard's at the boss 102 to 54, careful
  Ilya's 103 to 63; the clumsy Vanguard's in the halls' waves 1,456 to 1,210; the clumsy Ilya's at the boss 1,106 to 736.

### 13.4 Section 6.2's rows, measured

| Row | Target | Measured (`c1`) | Verdict |
|---|---|---|---|
| Skills off: byte-identical | the default batch, `FOUNDRY_FLOORS=4` and `2` | every file byte-identical but `foundry-batch.md`'s clock and wall seconds (13.1) | **passes** |
| The ship rule (docs/56, replacing the relative rule) | clumsy, careful, first-card: point change >= -1.0, lower bound >= -1.5 | +15.00 (+14.02), +1.45 (+1.08), +3.88 (+3.27) on 8401-11600 | **passes**, an easing reported to the lead |
| The bosses' bands | careful P50 45-65 s for the Warden (floors 1 and 3) and the Regent, either hands; none felled within 5 s | typical (fresh block): the Vanguard 51.1 / 49.0 / 60.1, Ilya 45.8 / 45.4 / 56.5; none under 5 s. Slow hands (seeds 1-400): the Vanguard 48.0 / 47.3 / 58.1, Ilya 43.7 / 44.0 / 53.7 against 44.0 / 44.6 / 54.9 shipped | **passes with typical hands**; with slow hands Ilya's floors 1-2 sit under 45 s as shipped, and the skills move them 0.3-0.6 s |
| The Descent's length | completed runs in 13-16 minutes; P50 within 1 minute of skills off | all in 13-16; the P50 moves 0.03 minutes for the careful pilots, 0.13 at most | **passes** |
| Each new damage skill | its share holds from floor 1 to floor 3 and stays under 8 % | the Rush: 1.9 / 1.4 / 1.0 % of the clumsy Vanguard's damage, about 0 for the careful pilot, who presses it about once in four runs; the glyph's heat 0.5 / 0.3 / 0.2 % of careful Ilya's (seeds 1-400) | under 8 %; **does not hold**: it scales by the first weapon, and the build adds weapons it does not read (13.7) |
| The defensive skills | deaths by floor and phase against skills off | the careful Vanguard's at the boss 102 to 54, careful Ilya's 103 to 63; the clumsy Vanguard's in the halls 1,456 to 1,210, the clumsy Ilya's at the boss 1,106 to 736 (of 3,200) | as section 6.3 expected: the guard reaches the crowd, the ward the boss |
| Skill dependence | the careful cohort's gain larger for Ilya than for the Vanguard | typical: +49 / +44 completions (a tie near the ceiling); slow hands (1-400): +32 / +46 | **not met with typical hands**, met with slow ones (13.7) |
| Parity | the Editor's six seeds as close as for the shipped kits | the simulation's side in 13.5; the Editor's rows with the skills on are the lead's to measure | pending (Unity) |
| Device | p95 at most 20 ms with 30-40 alive, a glyph and three buttons | phase 5 | not measured |

### 13.5 The parity table with the skills on (the simulation's side)

`HeroSkillsDescentResearch.TheEditorSeedsWithTheSkills` plays the Editor's six seeds (11-66, the pacing files of
2026-11-11) with the careful pilot frame-perfect, as the Editor's autopilot plays, skill 1 alone and all three. Floor 1:
the floor's seconds and the Warden's fight, and with all three the presses of skills 1 / 2 / 3:

| Seed | Vanguard, skill 1 | Vanguard, all three | Ilya, skill 1 | Ilya, all three |
|---|---|---|---|---|
| 11 | 285, 57 | 285, 57 (17 / 4 / 0) | 282, 55 | 282, 56 (10 / 9 / 3) |
| 22 | 298, 67 | 297, 72 (16 / 5 / 1) | 280, 56 | 291, 63 (6 / 5 / 3) |
| 33 | 272, 46 | 277, 51 (16 / 4 / 2) | 266, 41 | 270, 45 (5 / 4 / 2) |
| 44 | 278, 52 | 279, 51 (16 / 5 / 1) | 268, 44 | 265, 40 (6 / 5 / 2) |
| 55 | 301, 69 | 301, 69 (14 / 3 / 2) | 283, 55 | 296, 64 (7 / 6 / 3) |
| 66 | 284, 54 | 281, 53 (15 / 5 / 1) | 275, 48 | 285, 58 (7 / 6 / 3) |

Every run clears floor 1 and walks out of the finale either way. The Editor's own rows with the skills off are the pacing
files' (the careful Vanguard's floor 274-308 s and its Warden 45-66 s, Ilya's 274-303 s and 40-59 s); its rows with them
on are the lead's to measure: `<profile>/development/skills.txt` holding `3` beside `autopilot.txt` gives the Editor's
run the hero's three skills and the autopilot's presses through the same `SkillFootwork` (HordePacingMeasurement, as for
the shipped kits). The simulation's own spread over the skills is in 13.2-13.4; this table is what the Editor's six seeds
are read against.

### 13.6 What only Unity can check (for the lead, after the merge)

- **EditMode**: `FoundrySimulationSceneValueTests.TheFoundryHeroesAreTheAssets` (the four skill assets against the
  simulation's copies, each description formatting), `AutopilotCohortTests` (the careful pilot now `All |
  TimesTheSkills`), and the two new files the CombatChecks run already passes outside Unity (`HeroSkillRuleTests`,
  `FoundrySkillsTests`).
- **PlayMode**: `HeroSkillsFlowTests` (new, three tests): without `development/skills.txt` a run carries skill 1 alone;
  with `3`, the Vanguard's guard halves its damage for 4 s and its Rush strikes, dazes and halts short of what it struck,
  Ilya's glyph slows, heats and lights and its ward takes a blow and lights its striker. Every existing ability and burn
  flow (`AbilityFlowTests`, `CinderArcanistTests`, `BurnEffectsTests`, `RevealSkipTests`) should be untouched: slot 1 is the
  same runtime, button and `Used` event.
- **Imports**: four new assets (`Data/Abilities/Ability_IronResolve`, `_ShieldRush`, `_FlameSigil`, `_CinderWard`, no
  icons) referenced from both hero assets by hand-written GUIDs; new scripts with fresh `.meta` files, among them
  `SlowStatus`, a component added at run time (its file name is its class's).
- **The Editor's parity with the skills on** (13.5): HordePacingMeasurement with `skills.txt` `3` on seeds 11-66.

### 13.7 Open questions

1. **How much easing the lead accepts.** Within the interim bound (lead decision 3) the skills pass everything, but they
   ease the game a great deal for the weaker players: the clumsy Ilya completes 70 % of its Descents on the fresh block (2,244 of 3,200) against 50 % as shipped, and the clumsy cohort as a whole 53.5 % against 38.5 %. docs/56's rule reports an
   easing and does not bound it; docs/40's Forged band (40-60 %) is where the clumsy cohort now lands. If the lead wants
   it smaller, the measured levers are the ward's size and window and the glyph's slow (the clumsy Ilya presses both on
   cooldown, and they carry its gain), each a new candidate for a fresh block.
2. **The careful pilots barely use the Rush** (about one press in four runs: 110 in 400 careful Vanguards): its escape
   needs a surround a kiting pilot never meets, and its engage is now the lone Captain alone. It does no harm and helps the clumsy Vanguard; whether the
   owner finds a use for it on the device is phase 5's question.
3. **Skill dependence** (section 6.2): not met with typical hands: the careful pilots gain alike (the Vanguard +49 and Ilya +44 completions of 3,200 on the fresh block, deaths -49 and -44), both near their 96 % ceiling, and the boss seconds separate nothing. With slow hands Ilya gains more (+46 against +32 of 400 on seeds 1-400), as docs/40's order says; a device session reads it on people.
4. **The new damage skill's share falls with the floors** (the Rush's 1.9 / 1.4 / 1.0 % of the clumsy Vanguard's damage):
   it scales by the first weapon, as section 3.0 says, while the build adds a second and a third weapon it does not read.
   Scaling by the whole loadout is a lever for later.
5. **The standing Vanguard's Rush** still cost it a little on the spent seeds (-9 completions of 400 alone beside skill
   1, -3 with all three); on the fresh block the standing Vanguard gains +120 of 3,200 with all three. Standing is read
   against its own target (docs/56), and its gate fights already sit under the band as shipped.

### 13.8 The lead's decisions (2026-11-19; gameplay calls are delegated to the lead)

1. **The easing is accepted for now, with a ceiling.** Skills 2 and 3 open only after the first clear, then the Forge
   forges them for 300 and 500 gold (section 12). So a new player's first Descents are not eased, and the easing is
   a reward the player has earned. When phases 3-4 make the skills live, the confirmation is read again with the
   unlock as built. If the clumsy cohort's pooled completion with all three skills goes above **60 %**, the ward is
   trimmed first (its size, then its window), then the glyph's slow, each as a candidate on a fresh block.
2. **The Rush stays as built.** It is an escape and an opening against the lone Captain. The careful pilot's rare use
   is the pilot's habit, not the skill's fault. Phase 5's device session reads whether people find it.
3. **Skill dependence is read on the device**, where hands are slower than the typical model. With slow hands it is
   already met.
4. **Scaling the Rush by the whole loadout** waits for the ranks (later), where a skill's numbers are reopened anyway.
5. **The standing Vanguard needs nothing:** +120 of 3,200 on the fresh block.

## 14. Phases 3-4, built (2026-11-19)

The owner's answers (section 12) as built: the six skills as designed, three buttons for two-handed play with no
automatic skills, ranks later, and the unlock below. Nothing of the simulation moves: `FoundryLevers.Skills` stays 1, and
the default batch is byte-identical (14.6). What is not built: section 5.3's telemetry events, the four effect strips and
the Rush's puppet clip (phase 3's art), the result screen's unlock line and 5.2's one-line toast (14.7).

### 14.1 The unlock rule as built

Section 12's words, the owner's: **floor 2's first clear opens skill 2, floor 3's first clear opens skill 3, and the Forge
forges each for 300 and 500 gold.** Precisely (`Progression/HeroSkillUnlock.cs`):
- **Opened for the profile, not the hero:** a skill opens once `PlayerProfile.DeepestFloorCleared` reaches its floor - a
  clear on any difficulty tier, with either hero, from any map start (the record docs/53's map starts already read;
  section 4.1's recommendation). A floor is cleared once its gate boss has fallen and the hero has stood at the opened
  gate (`EncounterController.FloorCleared`, `CombatSetup.OnFloorCleared`), so the first Descent that clears floor 2 opens
  skill 2 for the next visit to the Forge - and shows its button locked on the HUD at once.
- **Forged per hero:** the Forge's Skills tab forges skill 2 for 300 gold and skill 3 for 500 for the hero the next run
  plays. **Skill 3 waits on skill 2** (the lead's "then"): a hero carries one, then two, then three new buttons, never a
  third without the second. Skill 1 is every hero's own.
- **Gold earned in play:** the price is the profile's gold, which runs bank; nothing is sold for money (docs/58's rule).
- Not "a completed Descent": the task's parenthesis named one, but section 12 names floors 2 and 3, and 13.8's decision
  1 (the first Descents are not eased) holds with either - a first clear of floor 2 is already past the first Descent's
  hardest floor for the clumsy cohort (its deaths are on floor 1, section 2). If the lead prefers the completed Descent,
  it is one line (`HeroSkillUnlock.IsOpen` reading `Tiers.HasCompleted` for both slots).

| Status (`SkillForgeStatus`) | When | The card says (en / tr) | Its look |
|---|---|---|---|
| Forged | skill 1 always; 2 and 3 once bought | "Ready from the first run" / "İlk seferden beri hazır"; "Forged: yours in every run" / "Dövüldü: her seferde seninle" | the gold card |
| Affordable | open, skill 2 forged for skill 3, gold enough | "Forge for 300 gold" / "300 altına döv" (the Relic Forge's own words) | the blue card; a tap forges |
| Too expensive | as above, gold short | "500 gold: need 450 more" / "500 altın: 450 daha gerekli" | the grey card |
| Waiting on skill 2 | skill 3 open, skill 2 not forged | "Forge Iron Resolve first" / "Önce Alev Mührü dövülmeli" | the grey card |
| Locked | its floor not cleared | "Locked: clear floor 3" / "Kilitli: 3. katı temizle" | the plain card, the icon dimmed |

### 14.2 What phases 3 and 4 built

| Where | What |
|---|---|
| `Data/Abilities/Ability_IronResolve`, `_ShieldRush`, `_FlameSigil`, `_CinderWard` | their painted icons (`Art/UI/UI_Icon_*_v1.png`), wired as the Forge Burst and the Vent carry theirs; the importer settings match (a single sprite, no mipmaps, 256 on the phone) |
| `Progression/HeroSkillUnlock.cs` | the rule: opening floors, prices, skill 3 after skill 2, `StatusOf` |
| `Core/ProfileSkills.cs`, `Core/PlayerProfile.cs` | per hero, skills 2 and 3 forged; `HasForgedSkill`, `SkillStatusOf`, `GoldNeededForSkill`, `TryForgeSkill` (the price spent and the skill recorded as one change, the save's trigger) |
| `Save/ProfileSaveData.cs`, `ProfileMigration.cs`, `ProfileSkillsSave.cs`, `ProfileStore.cs` | **save version 6**: `heroSkills` (`hero`, `skillTwo`, `skillThree`; a hero with nothing forged is not written); 5 -> 6 adds an empty list whatever the profile has cleared; `ProfileSkills.Restore` repairs a damaged save as `ProfileTiers.Restore` does (14.4) |
| `Core/CombatSetup.cs` | `CarriesSkill(slot)`: skills 2 and 3 when forged for the run's hero, or where `development/skills.txt` asks (the Editor's and a development build's reads still force them on); `StartSkills` reads it; `NextRunHero` for the Forge |
| `UI/AbilityButtonView.cs`, `Art/AbilityArt.cs` | the HUD's three buttons (14.3) and the padlock glyph |
| `UI/BossRevealView.cs`, `UI/RevealSkip.cs` | the reveal greys all three; a tap on any shown button skips it (`HeldButtons`) |
| `UI/HeroMovementInput.cs` | the stick follows its own finger (14.3) |
| `UI/AnvilView.cs`, `UI/AnvilView.Skills.cs`, `UI/RelicForgeView.cs` | the Forge's Skills tab and its entry (14.5) |
| `Content/AbilityText.cs`, `Content/AbilityDefinition.cs` | an ability's name and description in the language the game speaks (`<id>.name`, `<id>.description`, numbers in `Loc.Numbers`) |
| `Resources/Localisation/Strings.txt` | `anvil.tab.skills` and `skills.*` with their Turkish (docs/60's rules: no suffix on a placeholder, "Önce {0} dövülmeli" in the passive, "{0}. katı" the ordinal); the six abilities' rows on screen; Cinder Ward is "Kül Siperi" |

### 14.3 The HUD

- **Section 5.2's anchors, unchanged:** skill 1 the scene's 234-unit button at (-40, 68); skills 2 and 3 copies of it made in
  code, 176 units, anchored bottom-right with pivot (1, 0) at **(-90, 341)** and **(-306, 160)** - centres (902, 429) and
  (686, 248) on the 1080-wide canvas, on the arc 245 units from skill 1's centre. A copy carries the scene button's parts as
  the painted HUD dressed them (ring, cooldown band, icon well, countdown), scaled by 176/234 about its pivot, behind
  skill 1 in the draw order, its parts renamed (`Skill 2 Button`, `Skill 2 Icon`, ...) so a search for the scene's button
  still finds slot 1 alone.
- **Three states.** *Live* (the run carries the skill): its own icon, the cooldown band draining, the countdown, a tap that
  fires it. *Locked* (its floor cleared, not forged for this hero): the icon dimmed under a padlock, no tap - the lead's
  locked state and the owner's "each appearing when it opens" together. *Nothing* before its floor's first clear: a new
  player sees one button, then two, then three. A floor cleared mid-run shows its skill's button locked at once.
- **Two hands** (the owner's answer 4): the cluster stays in the right 45 % of the bottom quarter, the left thumb's stick
  everywhere else. `HeroMovementInput` read touch 0 alone, so a right thumb that touched a button first held the stick
  until it lifted, and a steering finger that lifted while the other was down could hand its place to it; the stick now
  follows its own finger (a touch that begins off the UI takes it, wherever it stands in the touch list, until it
  lifts). One finger reads as before. **Shield Rush** fires along the stick's steer at the tap (`TryUse(slot, heading)`);
  with no finger down, toward the nearest enemy (`AbilityController.Rush`, as phase 1 built it).
- **The boss's reveal** greys all three, and a tap on any shown button skips it (`BossRevealView.HeldButtons`).
- Not built: 5.2's one-line toast on the first run with a new button, and section 5.3's events (`ability_used`'s slot,
  `ability_refused`, `stick_on_button`, `ability_wasted`); phase 5's device session needs them to count mis-taps.

### 14.4 Save version 6

`heroSkills: [{"hero": "hero_vanguard", "skillTwo": true, "skillThree": false}]`. Version 5 migrates with nothing forged
(the gold untouched: a veteran opens its skills at the Forge with the gold it has). Repairs on load
(`ProfileSkills.Restore`, never throws): a missing record or an empty hero id dropped; a repeated hero's later records
dropped (the first wins); a skill whose floor the profile has not cleared, or skill 3 without skill 2, not kept, so a
damaged save never hands a hero what it did not earn; a hero left with nothing not written; a hero id the roster no
longer knows kept, as the unlocked heroes are.

### 14.5 The Forge's Skills tab

The Relic Forge's panel is full (title, status, three weapon cards, two relic cards, the start row), so the skills live
where docs/54 phase 4 put the Forge's other pages: a fifth tab of the Forge's panel (Forge, Anvil, Efsun, Bag, **Skills**),
its entry at the right end of the weapons' header row (the header's words stand at its left), on the forge's primary
plate while a skill of the next run's hero waits affordable - the forge calls the player to it after the clear. The tab
shows the hero the forge's chooser has chosen, its three skills on cards in the choice cards' painted look (14.1's
table), the wallet above them and the rule under them ("A floor cleared with any hero, on any difficulty, opens its skill
for every hero."); the boosters and the equipped row give the page their room. A tap on an affordable card forges, as a
tap forges a relic; the input delay after each change is the Anvil's.

### 14.6 The default batch

`FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_REALISM=typical FOUNDRY_FLOORS=2`,
`TheFoundryFloorOverASeedBatch`, on this branch's final code against the merged base (`review-fixes`, b3cf6bb9), this
12-thread machine: `foundry-batch.csv`, `foundry-batch-floor2.csv`, `foundry-cohorts-floor2.csv`, `foundry-cohorts.md`
and `foundry-gate.md` **byte-identical**; in `foundry-batch.md` only the clock in its header and the wall-seconds column
differ. Nothing the simulation reads moved: the skills' bodies are untouched, `FoundryLevers.Skills` stays 1, and the
profile's forged skills reach only the scene's run (`CombatSetup.CarriesSkill`). CombatChecks green (1,085, `Başarılı`;
1,077 before), the Unity compile check 0 errors.

### 14.7 For the lead: the Unity filters, the captures, the next read

**EditMode** (new or changed): `HeroSkillUnlockTests` (also in CombatChecks), `ProfileStoreTests` (version 6: the round
trip, a version 5 file, a damaged file; the written version asserts now 6), `AbilityArtTests` (the padlock),
`DifficultyTierTests.VersionFourMigratesItsDepthToForged`, `WeaponForgeTests`, `LocalisationAssetTests` (the ability rows'
English against the assets).

**PlayMode** (new): `SkillButtonsFlowTests` (a new player's one button; a locked slot; a forged slot live at its anchor,
firing and cooling; a floor cleared mid-run; the development file), `SkillForgeFlowTests` (forge skill 2, saved, live on
the next run's HUD; skill 3 after skill 2 and its gold; a new profile's locked cards; Ilya's three in Turkish, fitted).
Changed: `RelicForgeFlowTests.ForgePanelIsOpaqueAndFitsTheShortestPortraitScreenWithoutOverlaps` (the entry on the
header's row), `TurkishScreensTests.TheAnvilsTabsFitInTurkish` (the Skills tab). Worth re-running for regressions:
`HeroSkillsFlowTests`, `AbilityFlowTests`, `CinderArcanistTests`, `GateBossFlowTests`, `RevealSkipTests`,
`AnvilFlowTests`, `PaintedHudFlowTests`, `ArtHudFlowTests`, `PauseFlowTests`, `MapChoiceFlowTests` (its deeper profiles
now show a locked skill button).

    pwsh -File Tools/Run-UnityTests.ps1 -Platform EditMode -SkipApk -Filter "Cryptforge.Tests.HeroSkillUnlockTests;Cryptforge.Tests.ProfileStoreTests;Cryptforge.Tests.AbilityArtTests;Cryptforge.Tests.DifficultyTierTests;Cryptforge.Tests.WeaponForgeTests;Cryptforge.Tests.LocalisationAssetTests"
    pwsh -File Tools/Run-UnityTests.ps1 -Platform PlayMode -SkipApk -Filter "Cryptforge.Tests.SkillButtonsFlowTests;Cryptforge.Tests.SkillForgeFlowTests;Cryptforge.Tests.RelicForgeFlowTests;Cryptforge.Tests.TurkishScreensTests;Cryptforge.Tests.HeroSkillsFlowTests;Cryptforge.Tests.AbilityFlowTests;Cryptforge.Tests.GateBossFlowTests;Cryptforge.Tests.AnvilFlowTests"

**Captures worth taking** (the device, or the Editor at 1080 x 2340): the HUD with a profile that cleared floor 3 and
forged skill 2 (skill 2 live, skill 3 locked under its padlock) - the arc against both thumbs, the copies' painted rings at
176 units, the countdown's size; the same during the boss's reveal (all three greyed); the Relic Forge with the Skills
entry on its primary plate; the Skills tab for each hero in English and Turkish (the descriptions' wrap, Kül Siperi's
card).

**The next balance read** (13.8, decision 1; the lead's): the confirmation again with the unlock as built, the clumsy
cohort's pooled completion with all three skills at a ceiling of **60 %**. What it must model and check:
- **The unlock's timing, not skills from the first frame:** phase 2's `c1` read gave every run all three skills; as built a
  player has none until a run clears floor 2, then skill 2 only after banking 300 gold, skill 3 after floor 3 and 500 more.
  The read is a campaign per pilot - runs 1..N on fresh seeds, the skills entering at the run after the clear that opens
  them and the gold that buys them (gear_economy.py's per-player streams are the model) - reporting the clumsy pooled
  completion over the runs once all three are forged, and how many runs that takes per cohort.
- **Per hero:** the Forge forges per hero, so a player who switches heroes carries the skills of one; read each hero's own
  campaign.
- **The bounds:** docs/56's rule for clumsy, careful and first-card; the bosses' 45-65 s and the 13-16 minute Descent with
  the skills live; if the clumsy pooled completion passes 60 %, the ward first (its size, then its window), then the
  glyph's slow, each a candidate on a fresh block (13.8).
- **The Editor's parity with the skills on** (13.5, still the lead's): `skills.txt` `3` or a forged profile on seeds 11-66.

## How to re-run

    dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~HeroSkillDuelTests"
      # in every plain run: docs/55's fights untouched (a pinned fingerprint), a six-skill duel replays bit for bit
    dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~HeroSkillsResearch.TheSixSkillsInTheDuel"
      # ARENA_SEEDS (2000); writes TestResults/hero-skills-duel.md (about 10 s)
    FOUNDRY_SEEDS=200 dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~HeroSkillsResearch.TheShippedDescentAsTheSkillsWouldMeetIt"
      # FOUNDRY_SEEDS (100), FOUNDRY_FIRST_SEED, FOUNDRY_PARALLEL, FOUNDRY_REALISM (typical); HERO_SKILLS_TRACE=<seed> dumps
      # that seed's trace; writes TestResults/hero-skills-descent.md
    dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~ArenaDuelResearch"
      # docs/55's report: byte-identical to main's but its clocks
    dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~HeroSkillRuleTests|FullyQualifiedName~FoundrySkillsTests"
      # in every plain run: the skills' pure bodies, and the simulation with the skills off (the shipped run, field for
      # field) and on (each skill pressed and at work, a seed replaying line for line)
    FOUNDRY_SEEDS=400 SKILLS_OPTIONS=shipped,s2,s3,s23,c1 dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~HeroSkillsDescentResearch.TheSkillsInTheDescent"
      # phase 2's read: FOUNDRY_FIRST_SEED (1), FOUNDRY_PARALLEL, FOUNDRY_REALISM (typical; slow), SKILLS_COHORTS (careful,
      # standing, first-card, clumsy), SKILLS_HEROES (Vanguard,Ilya); writes hero-skills-rule.md and .csv
    FOUNDRY_FIRST_SEED=8401 FOUNDRY_SEEDS=3200 SKILLS_OPTIONS=shipped FOUNDRY_OUT=<folder>/shipped ...   # then c1 into <folder>/c1
    python Tools/BalanceRule/paired_bootstrap.py <folder> 8401 11600
      # the confirmation (section 13.3): every cohort's change and its seed-level 90 % CI
    dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~HeroSkillsDescentResearch.TheEditorSeedsWithTheSkills"
      # the parity table's simulation side (section 13.5): hero-skills-parity.md

The limits. The duel is docs/55's one lane: the Rush's escape and the Sigil's lure have no crowd to show in, and its
autopilot is two hand-written policies. The Descent's numbers are the shipped game's; the guards are a first-order
replay over health traces (the enemies never answer them, and the replay presses on blows, not on the slam's
telegraph), and every new skill's run value is modelled until phase 1 puts the skills in the simulation. Each finding
that reshaped a skill is in the text where it applies: the Sigil's radius and heat (3.2), the ward's window (3.2,
6.3), Iron Resolve's 50 % (3.1), the ward read off the hero's own health (3.0).

## Sources

- Vampire Survivors: [Steam discussion on controls](https://steamcommunity.com/app/1794680/discussions/0/3194745319523783907)
  (no fire button; weapons fire automatically), [God is a Geek review](https://www.godisageek.com/reviews/vampire-survivors-review/).
- Megabonk: docs/40 section 2.3 (characters, passives) and its sources; [games.gg character guide](https://games.gg/megabonk/guides/megabonk-guide-all-characters-and-how-to-unlock-them/).
- Brotato: [Rogueliker guide](https://rogueliker.com/brotato-guide/) (auto-aim by default, manual aim optional;
  positioning as the control), [Brotato wiki, stats](https://brotato.wiki.spellsandguns.com/Stats).
- Archero 2: [Theria Games, Archero 2 arena](https://theriagames.com/?p=85626) (automatic fights; gear and runes count;
  passive heroes do best), [BlueStacks hero tier list](https://www.bluestacks.com/blog/game-guides/archero-2/ah2-tier-list-en.html)
  (heroes' boosts, a trigger cooldown), [Archero 2 wiki, skills](https://archero-2.game-vault.net/wiki/Skills) (run
  skills; arena skills assigned in groups; the page refused automated reading, read through search).
- Hades: [Hades Wiki, controls](https://hades.fandom.com/wiki/Controls), [Gamepressure, how Call works](https://www.gamepressure.com/hades/call-how-does-it-work/z3ec5f);
  the iOS port's touch controls: [Engadget](https://www.engadget.com/hades-hits-ios-as-a-netflix-mobile-exclusive-on-march-19-170014127.html)
  (customisable buttons), [Pocket Gamer review](https://www.pocketgamer.com/hades/review/) (preset layouts, Cast hit for
  Dash), [TouchArcade](https://toucharcade.com/2024/03/20/hades-ios-review-2024-controller-support-cloud-saves-vs-switch-steam-deck-netflix-games/).
- Brawl Stars: [Game8, controls](https://game8.co/games/Brawl-Stars/archives/316910), [Pocket Gamer beginner's tips](https://www.pocketgamer.com/articles/074501/brawl-stars-tips-and-tricks-a-guide-for-the-beginner-brawler/),
  [LootBar layout guide](https://lootbar.gg/blog/en/best-brawl-stars-control-settings-and-layout-guide.html) (Super near
  the attack and larger; a seller's blog, read as opinion).
- Metin2: [Metin2 wiki, Books](https://en-wiki.metin2.gameforge.com/index.php/Books) (20,000 experience to read; 18-30 h
  between reads; Exorcism Scroll; Concentrated Reading; Soul Stones from G1 to P), [Metin2 wiki, Skill Book](https://en-wiki.metin2.gameforge.com/index.php/Skill_Book)
  (55 successful reads from M1 to G1), [Gameforge, skill book guide](https://gameforge.com/en-GB/games/metin2-skills-book-guide.html).
- Thumbs and targets: Steven Hoober, [How Do Users Really Hold Mobile Devices?](https://uxmatters.com/mt/archives/2013/02/how-do-users-really-hold-mobile-devices.php)
  (one-handed 49 %, cradled 36 %, two-handed 15 % of observed users); [TetraLogical, target sizes](https://tetralogical.com/blog/2022/12/20/foundations-target-size/)
  (Material 48 dp, Apple 44 pt).
- In this repository: docs/27 decision 12; docs/40 sections 2.3, 4.1, 4.2, 9.4, 14.4; docs/41 steps 4-5; docs/42
  section 4 (no projectile objects); docs/45 (Ilya, Ember Memory, Crucible Vent); docs/46 (Phase Dash, R_skill measured);
  docs/47 (13-16 minutes); docs/50 and docs/52 (the relative rule); docs/53 (map starts); docs/54 sections 1, 6.1, 7, 8,
  14, 17; docs/55 (the duel core); docs/56 (the balance-rule study, pending).
