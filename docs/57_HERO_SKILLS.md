# 57 - Hero skills: three active skills a hero (design proposal, 2026-11-17)

A proposal for the lead's and the owner's decision. The owner asked for "karakterlerin daha çok skilleri olsun": more
skills for the heroes, the PvP arena first in mind. docs/54 section 14 outlined it in one paragraph ("three active
skills per hero: a second opens at floor 2's first clear, a third at floor 3's, a skill bar of three on the right
thumb"); docs/55 built an integer duel core whose "dealt skills" stood in for the second and third. This document
designs them.

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
| Balance | the cohorts measure every skill; **skills off, the default batch stays byte-identical**; skills on, the Descent stays inside the relative ship rule (docs/56's study of that rule is pending) | section 6 |
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
- **Gate bosses are not dazed and are slowed by half the slow**: the Warden's and the Regent's telegraphs stay the
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
  elites are dazed). Walls slide or stop it as they do the walk. **No guard**: the Riftblade's Phase Dash turns hits
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
    reach with no other enemy within 3, or the Warden in its vulnerability window beyond reach;
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

**Modelled run value** (section 2's measured inputs; phase 2 measures it). The new damage is small at these numbers, as
the shipped skills' is: the Rush's 15 to the one or two enemies a 4-unit path crosses (its capsule is 0.43 of the Burst's
circle), about twice a minute, is about 1 % of the Vanguard's floor-1 damage, and with build scaling it stays near that
on floor 3 instead of falling to a third; the Sigil's doubled burn, on the share of the crowd inside the glyph for 5 s
of every 20, adds a few tenths of a per cent to Ilya's 2-3 % of burn. **The run value is in the guards, the escape and
the slow**: section 6.3's replay puts each guard at **+4-5 % of effective health** for the careful pilot (+9-12 % pressed
on cooldown); the Rush's escape and the Sigil's slow need the simulation (phase 1) to be read at all.

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
| 1. Core and simulation | `AbilityKind` gains Guard, Rush, Sigil, Ward; `HeroDefinition` three abilities, `AbilityController` three slots; `HeroDash` (one body with docs/46's dash), `SlowStatus` (read by `PackMotion`), `SigilZone` (on `BurnRule`'s clock), the guard as a timed armour modifier, the ward as a pool in `HealthState`, build scaling; the assets; `FoundryLevers.Skills`, the pilot's rules and `TimesTheSkills`; EditMode and parity tests | **5** | skills off: the default batch byte-identical, every test green; the parity table for skills on |
| 2. Measurement | section 6.2's rows for each skill and for all three, both heroes, seeds 1-400, both hands; tuning; this document's numbers replaced by measured ones | **2-3** | the rows pass, or the lead chooses which bind |
| 3. HUD, art, telemetry | three `AbilityButtonView` slots (section 5.2), the reveal's hold over all three, four icons (`build_ability_icons.py`'s family), four effect strips (`build_skill_vfx.py`), the Rush's puppet clip; section 5.3's events | **3** | a PlayMode flow fires each slot; telemetry in the log |
| 4. Unlock and the Forge | save version 6 (opened and forged skills per hero, migration adds empties; version 5 went to docs/54's difficulty tiers), the first-clear opening, the Forge's skills tab with the 300 / 500 prices, the result screen's unlock line | **2** | a profile opens and forges skill 2 after a floor-2 clear; `ProfileStoreTests` round-trip and migrate |
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
