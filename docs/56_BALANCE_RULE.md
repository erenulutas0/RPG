# 56 - The ship rule for a new creature (balance statistics, 2026-11-17)

A proposal for the lead's decision (docs/52, lead decisions 2026-11-17, point 3). Nothing the game ships changed: no
constant, asset, scene or setting. The study ran in the foundry simulation (`GloamEnemiesResearch`) on
lead-integrate-2's code (15ddc8f5: floor 2's Mirror Sentry `r0-f70` and Glass Regent at its 10 % lane and 1.7 s
telegraph, floor 3's Cinder Bloom `m20-h70-r1.2`). main's later changes to the simulation (b0fa98c2: the Pollen Wraith
played as the Mite itself, the gear that no run reads) leave every run as it was - their own default batch is
byte-identical - so every number below holds on main.

## 0. The rule in five lines

1. **Seeds.** Explore on any seeds; confirm **once** on a fresh block that nothing shipped was tuned on: **1,200 seeds**
   for a change on floor 3 or later, **3,200** for a change on floor 2 (its divergence runs over three floors). Both
   heroes, typical hands, every playing cohort, paired against `shipped` on the same code. A block used to choose
   becomes a tuning block; the next fresh block starts after it (now: seed 2001).
2. **The read.** The change in four-floor completion rate per cohort, in points, with a seed-level bootstrap 90 % CI
   (10,000 resamples of seeds, both heroes of a seed together; `Tools/BalanceRule/paired_bootstrap.py`), beside an A/A
   probe on the creature's floor (`aa-f2`, `aa-f3`).
3. **Pass.** For the clumsy, careful and first-card cohorts: the point change **at least -1.0 point** and the CI's lower
   bound **at least -1.5 points**. The standing cohort is read against the creature's own design target. A CI wholly
   above 0 (an easing) passes and is reported to the lead.
4. **No search inside the confirmation.** At most two candidates a design round go to the confirmatory read, each read
   once; a fail is final for that candidate; a new candidate needs a design reason written before its read, not a lever
   nudged; the report says how many were read.
5. **Everything else is a reported row**: slow hands, roles, section-8 targets, fight lengths, the Descent's time.

## 1. Why the old rule had to go

The old rule (2026-11-12): over seeds 1-400, later 1-800, a creature may cost the clumsy cohort at most 1 four-floor
completion in 800 (later 2 in 1,600) against the shipped floor. Three things measured here break it:

- **The bar sits far inside the noise.** The paired change of a cohort's completions over 400 seeds has an SD of 4-5
  completions for a change on floor 3 and 6-8 for one on floor 2 (the clumsy cohort; careful 2-4). Over 1,600 runs that
  is about 6 and 11: a bar of 2 is a third of an SD. A harmless change fails it about 40 % of the time; a searched one
  passes it by luck.
- **A change that does nothing to the game fails it.** The A/A probe (section 2) - the shipped game, only the pilot's
  later dice shifted - costs the clumsy cohort **23, 27 and 23** completions in 1,600 on seeds 1-800 from floor 2.
- **The tuning seeds are spent.** The same probe on the fresh seeds 801-2000 costs **+1** in 2,400. Floors 2 and 3
  were tuned and accepted on seeds 1-800; the configuration that shipped is, among the ones tried, the one these seeds
  happened to favour, and it reads about 1.5 points better there than it is (the clumsy cohort's completion: 38.9 % on
  1-800, 37.0 % on 801-2000). Every later comparison on those seeds starts from a lucky baseline: a new creature on
  1-800 "costs" what the baseline's luck was worth. That is selection on noise, measured.

## 2. The A/A noise

`FoundryLevers.NudgeFloor` (research, off): on entering the named floor the realistic pilot's dice give `NudgeDraws`
draws that nothing reads. The floors are the shipped ones; only the hands' later draws (lapses, misplaced steps) move,
so the runs diverge from that floor on exactly as any change makes them diverge. Completion change against `shipped`,
points of rate (90 % CI), the SD of the paired change per 400 seeds in completions:

| Probe | Seeds | Clumsy | Careful | Standing | SD per 400 (clumsy / careful / standing) |
|---|---|---|---|---|---|
| `aa-f2` (1 draw at floor 2) | 1-800 (tuning) | **-1.44** (-2.56 to -0.38) | +0.25 (-0.38 to +0.94) | -0.31 (-1.12 to +0.50) | 7.4 / 4.6 / 5.6 |
| `aa-f2b` (2 draws) | 1-800 | **-1.69** (-2.81 to -0.56) | +0.44 (-0.12 to +1.00) | +0.00 (-0.81 to +0.81) | 7.8 / 3.9 / 5.7 |
| `aa-f2c` (3 draws) | 1-800 | **-1.44** (-2.62 to -0.25) | -0.31 (-0.94 to +0.31) | +0.38 (-0.50 to +1.25) | 8.0 / 4.5 / 6.1 |
| `aa-f3` (1 draw at floor 3) | 1-800 | -0.25 (-0.94 to +0.38) | +0.25 (+0.00 to +0.56) | -0.50 (-1.00 to +0.00) | 4.5 / 2.0 / 3.6 |
| `aa-f3b` (2 draws) | 1-800 | -0.06 (-0.69 to +0.62) | +0.25 (-0.12 to +0.69) | -0.38 (-0.88 to +0.12) | 4.5 / 2.8 / 3.5 |
| `aa-f3c` (3 draws) | 1-800 | -0.38 (-1.00 to +0.25) | +0.25 (-0.06 to +0.56) | -0.44 (-1.06 to +0.12) | 4.4 / 2.2 / 4.1 |
| `aa-f2` | **801-2000 (fresh)** | **+0.04** (-0.92 to +1.00) | +0.21 (-0.25 to +0.71) | +0.17 (-0.50 to +0.83) | 8.1 / 4.1 / 5.7 |
| `aa-f3` | **801-2000 (fresh)** | **+0.00** (-0.50 to +0.54) | +0.25 (-0.04 to +0.54) | +0.17 (-0.29 to +0.62) | 4.4 / 2.4 / 3.9 |

The three floor-2 probes agree with one another on seeds 1-800 (clumsy 600, 596 and 600 completions of 1,600) and all
sit about 25 below `shipped` (623); on the fresh seeds the probe is where it should be. Floor 3, tuned with fewer
candidates, carries a smaller bias on 1-800 (clumsy -0.06 to -0.38).

**What a read resolves.** The standard error of a cohort's change in points is SD400 x sqrt(N / 400) / (2N) x 100 for N
seeds. The clumsy cohort's: floor 3, 1,200 seeds, **0.32 points**; floor 2, 1,200 seeds, 0.58; floor 2, 3,200 seeds,
**0.36**. Hence rule 1's counts: about a third of a point either way.

## 3. The creatures

Completion change in points (90 % CI). A shipped creature is read against what it replaced (shipped minus the old
floor: `f2-captain`, `f2-warden`, `captain` put the old enemy back alone); a held one against `shipped`. Base rates on
801-2000: clumsy 37.0 %, careful 96.1 %, standing 87.8 %.

**On the fresh seeds 801-2000** (1,200 seeds, 2,400 runs a cohort):

| Creature | Clumsy | Careful | Standing | SD per 400, clumsy |
|---|---|---|---|---|
| Mirror Sentry vs the Grunt Captain (floor 2 elite) | -0.46 (-1.29 to +0.33) | +0.17 (-0.25 to +0.58) | +0.46 (-0.25 to +1.17) | 6.9 |
| Glass Regent vs the Forge Warden (floor 2 gate) | -0.54 (-1.25 to +0.17) | +0.62 (+0.21 to +1.04) | **+5.58 (+4.71 to +6.50)** | 5.9 |
| Cinder Bloom vs the Grunt Captain (floor 3 elite) | +0.33 (-0.12 to +0.79) | +0.00 (-0.29 to +0.29) | +0.29 (-0.17 to +0.75) | 3.9 |
| Thornling `thorn` (0.8 s) | -0.50 (-1.00 to +0.00) | -0.04 (-0.38 to +0.29) | +0.04 (-0.46 to +0.54) | 4.2 |
| Thornling `thorn-t1.2` | -0.33 (-0.88 to +0.21) | +0.00 (-0.38 to +0.33) | +0.17 (-0.29 to +0.67) | 4.5 |
| Thornling `thorn-dv3-6` | -0.75 (-1.29 to -0.21) | -0.12 (-0.42 to +0.17) | +0.21 (-0.29 to +0.67) | 4.5 |
| Pollen Wraith, no pollen (shipped on main) | 0 exactly: the Mite's runs | 0 | 0 | - |

**On the tuning seeds 1-800** (the reads the old rule used; 1,600 runs a cohort):

| Creature | Clumsy | Careful | Standing | Old rule (2 in 1,600; careful within 3 a range) |
|---|---|---|---|---|
| Mirror Sentry | +0.31 (-0.56 to +1.19) | -0.25 (-0.75 to +0.19) | -0.88 (-1.69 to -0.06) | passes |
| Glass Regent | +0.00 (-0.81 to +0.81) | +0.62 (+0.12 to +1.12) | +4.62 (+3.69 to +5.62) | passes |
| Cinder Bloom | +0.25 (-0.31 to +0.81) | -0.06 (-0.38 to +0.25) | +0.50 (-0.06 to +1.06) | passes |
| Thornling `thorn` | -0.69 (-1.31 to -0.06) | -0.25 (-0.69 to +0.12) | -0.75 (-1.31 to -0.19) | fails (-11; careful -5 on 401-800) |
| Thornling `thorn-t1.2` | -0.19 (-0.75 to +0.38) | +0.19 (-0.12 to +0.50) | -0.38 (-1.00 to +0.25) | fails (-3) |
| Thornling `thorn-dv3-6` | -0.38 (-0.94 to +0.19) | +0.12 (-0.12 to +0.44) | -0.38 (-0.94 to +0.25) | fails (-6) |
| Thornling `thorn-t1.0` | -1.06 (-1.69 to -0.44) | +0.12 (-0.19 to +0.44) | -0.69 (-1.31 to -0.06) | fails (-17) |
| A/A `aa-f2` (no change at all) | -1.44 (-2.56 to -0.38) | +0.25 | -0.31 | **fails (-23)** |

The shipped creatures read 0.3-0.8 points better for the clumsy cohort on the seeds they were tuned on than on fresh
ones (the Sentry +0.31 against -0.46, the Regent +0.00 against -0.54): the same selection, seen from the other side.

## 4. The verdicts under the proposed rule

| Creature | Read on | Clumsy point / lower bound | Careful point / lower bound | Verdict | Note |
|---|---|---|---|---|---|
| Mirror Sentry (shipped) | 801-2000 (1,200; rule asks 3,200 for floor 2) | -0.46 / -1.29 | +0.17 / -0.25 | **passes** | passes even at the wider CI of 1,200 seeds |
| Glass Regent (shipped) | 801-2000 (1,200 of 3,200) | -0.54 / -1.25 | +0.62 / +0.21 | **passes** | eases the standing cohort by 5.6 points (CI +4.7 to +6.5) against the Warden: reported to the lead |
| Cinder Bloom (shipped) | 801-2000 (1,200) | +0.33 / -0.12 | +0.00 / -0.29 | **passes** | |
| Pollen Wraith, no pollen | by construction | 0 / 0 | 0 / 0 | **passes** | the simulation plays it as the Mite |
| Thornling `thorn` (0.8 s) | 801-2000 (1,200) | -0.50 / -1.00 | -0.04 / -0.38 | **passes** | the read docs/52 section 4 designed (careful typical 0.43 hits a floor, slow 0.8); its cost is the pilots' dodge (`thorn-s0-unseen` 0) |
| Thornling `thorn-t1.2` | 801-2000 (1,200) | -0.33 / -0.88 | +0.00 / -0.38 | **passes** | 0.04-0.07 hits a floor: a ring nobody is caught by, no hands read |
| Thornling `thorn-dv3-6` | 801-2000 (1,200) | -0.75 / -1.29 | -0.12 / -0.42 | **passes**, with a measurable cost (CI below 0) | |
| Thornling `thorn-t1.0` | 1-800 only | -1.06 / -1.69 | +0.12 / -0.19 | fails on the tuning seeds; **not confirmed** | |

**No shipped creature fails.** Three caveats belong with the table:
- Rule 4 was not followed here, on purpose: this study is the rule's calibration, so three Thornling variants were
  read on the fresh block. Block 801-2000 is now a tuning block. A Thornling decision under the rule reads its one or
  two chosen candidates on seeds 2001-3200.
- The first-card cohort, which the rule gates, was not read in this study (the careful cohort's footwork is its own, so
  the careful row stands in for it here); a confirmation under the rule reads it.
- The floor-2 creatures were read on 1,200 fresh seeds, not the rule's 3,200. Both pass at that width (lower bounds
  -1.29 and -1.25); a narrower read would only tighten them.

## 5. Why these thresholds

- **-1.0 point for the point change.** The clumsy cohort completes 37-39 % of Descents, under docs/40's Forged band
  (40-60 %) and section 11's 40 % line (docs/52), so it has no room to give. A point of completion is 1 in 37 of the
  clumsy cohort's completions. Floor 3 carries three creature slots and floor 2 two; at -1.0 each, a floor's creatures
  can cost at most about 2-3 points even when every one lands at the edge, and the point estimate of a harmless
  creature falls below -1.0 about one time in a thousand at the rule's seed counts (SE 0.32-0.36).
- **-1.5 points for the lower bound.** It keeps a noisy read from passing on its point alone: a creature that truly
  costs 1.5 points passes about one time in twenty (its point must come out at -0.97 or better, 1.65 SE above its
  truth); a harmless one fails it about one time in two hundred.
- **The same for careful and first-card.** docs/52 sections 1 and 11: no creature may cost the cohorts it does not
  read. Careful's noise is half the clumsy cohort's, so the same bar is stricter for it in practice.
- **Standing on the creature's own target.** The standing cohort is the read for the Wraith and the Bloom (section 8:
  standing 5-10 points below), so a bar would forbid the design.
- **The A/A probe in every confirmation.** It shows the band a reshuffle of the same game moves the read by, so that a
  result inside it is reported as "no measurable effect", never as a gain.

## 6. What this touches

No shipped constant changed. For the lead:
- **The Glass Regent eases the standing cohort by 5.6 points** against the Forge Warden it replaced (fresh seeds; 4.6
  on the tuning seeds), while the clumsy cohort's change is -0.5. docs/50's lesson ("no creature eases the floor")
  would call that out; under the proposed rule it passes and is reported. Leaving it, or giving the Regent's lanes back
  some of the Warden's bite for the standing cohort, is the lead's call.
- **The Thornling** passes the proposed rule at its starting values (`thorn`, 0.8 s). Under section 11's hold it stays
  held; if the lead wants it, rule 4 sends `thorn` (and at most one more) to seeds 2001-3200 once.
- **Seeds 1-2000 are tuning seeds** from now on. `FOUNDRY_FIRST_SEED=2001` starts the next confirmation.

## 7. How to run it

```
# each option on the fresh block, both heroes, the playing cohorts, typical hands
GLOAM_OPTIONS=shipped,aa-f3,<candidate> GLOAM_COHORTS=careful,standing,first-card,clumsy GLOAM_ROLES=0 \
  FOUNDRY_FIRST_SEED=2001 FOUNDRY_SEEDS=1200 FOUNDRY_REALISM=typical FOUNDRY_OUT=<folder>/<name> \
  dotnet test Tools/CombatChecks -c Release -p:NuGetAudit=false --filter "FullyQualifiedName~GloamEnemiesResearch"
python Tools/BalanceRule/paired_bootstrap.py <folder> 2001 3200
```

`paired_bootstrap.py` lists its comparisons at the top (an option, and whether to flip it); add a candidate there.
The study's runs (seeds 1-2000) lie in `E:/gloam-horde-research` on the development machine, outside the repository.

## 8. Checks on this branch

Built on main (b0fa98c2) with the Wraith-and-Thornling research and this rule on top; every research lever is off by
default. CombatChecks 897 green (`Başarılı`); the Unity compile check green. The default batch (`FOUNDRY_SEEDS=200
FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_REALISM=typical`, `FOUNDRY_FLOORS=4` and `2`) against main's own on the same
machine: every CSV, `foundry-gate.md`, `foundry-cohorts.md` and `foundry-finale.md` byte-identical; in
`foundry-batch.md` only the clock in its header and the wall-seconds column differ.

## 9. Lead decisions 2026-11-17

1. **The rule is adopted** as section 0 states it, and it replaces the relative rule of 2-in-1,600 everywhere a new
   creature ships. Seeds 1-2000 are spent; the next confirmation block starts at 2001, and each read records the block
   it used here so no block is read twice.
2. **First-card is read from now on.** This study let careful stand in for it; the next confirmation reads all three
   gated cohorts.
3. **The Glass Regent stays shipped, and it is reopened as a design miss.** It passes the harm rule, but docs/50 built
   it to separate the careful player from the standing one ("mechanics that a careful player reads and a standing one
   does not"), and it moved the standing cohort the wrong way (+5.6 points against the Warden while careful moved
   +0.6). The next Regent round gives the standing hero back the bite the Warden had: at most two candidates with a
   written design reason, read once on a fresh 3,200-seed block. Pass means the rule's harm bar for clumsy, careful
   and first-card, plus standing no better than the Warden's (point change <= 0).
4. **The Thornling stays on hold.** When floor 3 gets its next content pass, `thorn` (0.8 s) and at most one more
   candidate go once to a fresh 1,200-seed block.
5. **The Wraith's pollen stays unbuilt** (docs/52's decision); `PollinatingWraith` remains a research lever, off.

## 10. Blocks used

Each confirmatory read records here the block it spent, so that no block is read twice (decision 1). A block is spent
once any option on it has been read, whatever the verdict; the next confirmation starts at the next fresh seed.

| Seeds | Spent by | Date | What was read | Verdict |
|---|---|---|---|---|
| 1-800 | the tuning of floors 2 and 3 (docs/50, docs/52) | 2026-11-12..16 | every shipped creature's choice | tuning seeds |
| 801-2000 | this rule's study (sections 2-4) | 2026-11-17 | the A/A probes, the shipped creatures against what they replaced, three Thornlings | the study's verdicts |
| 2001-5200 | the Glass Regent's round (docs/50 "The Regent's bite for the standing hero") | 2026-11-17/18 | `regent-feet` against `f2-warden`, beside `shipped` and `aa-f2`; careful, standing, first-card, clumsy; both heroes, typical hands | `regent-feet` passes (standing -1.12, clumsy -0.25, careful +1.06, first-card +1.34 points against the Warden; the A/A probe within -0.31 to +0.56); one candidate read |
| 5201-8400 | docs/54 phase 6, the difficulty tiers (docs/54 section 21) | 2026-11-17..19 | Tempered, Inferno and Abyssal, each with its matched geared cohort against the cohort ungeared on Forged; clumsy, careful, first-card; one candidate a tier | all three pass; seven of nine cells an easing |
| 8401-11600 | docs/57's hero skills, phases 1-2 (docs/57 section 13.3) | 2026-11-18/19 | `c1` (the three skills a hero as phase 2 chose them) against `shipped`; careful, standing, first-card, clumsy; both heroes, typical hands; the A/A `aa-f1` (one unread draw as floor 1 begins) for the clumsy cohort | `c1` passes as an easing (clumsy +15.00, CI +14.02 to +15.98; careful +1.45, +1.08 to +1.83; first-card +3.88, +3.27 to +4.48; standing +4.33); one candidate read; `aa-f1` +0.77 (-0.19 to +1.73), SD 13.0 completions per 400 seeds |
| 11601-14800 | docs/57's skills live, the re-read of its 13.8 with the unlock as built (docs/57 section 15), and docs/54 21's standing and auto-farmer cohorts on the same block | 2026-11-23 | `live` (`SkillsCampaignResearch`): 32 campaigns of 100 runs a cohort and hero, campaign k on seeds 11601 + 100k .. + 99, both heroes and every cohort on the same seeds; the skills, the Forge, the Anvil and the tiers as built; each run beside its shadow (the same seed, gear and tier, skill 1 alone); clumsy, careful, first-card, standing, auto-farmer; typical hands | under 13.8's ceiling: the clumsy cohort's pooled completion with all three skills 57.0 % (55.8 to 58.3) against 60 %, no trim; against the shadows (paired_bootstrap.py, every run) clumsy +12.52 (CI +11.58 to +13.47), careful +0.88 (+0.61 to +1.16), first-card +2.12 (+1.66 to +2.61), standing +3.81 (+3.12 to +4.50): an easing; one design read |
| 14801-18000 | docs/63's weapon roster, phase 2's candidate C3 (the guarantee, the Mortar as built and the Cinder Saw striking 8) | 2026-11-25 | `p3-saw8` (`WeaponRosterResearch`, `ROSTER_CONFIRM=14801-18000`), with `shipped` and the A/A `aa-f1`, four cohorts, both heroes, typical hands, 3,200 seeds | **pass**: careful -0.64 (-1.20 to -0.08), first-card +1.77 (+0.83 to +2.69, an easing), clumsy +14.36 (+12.78 to +15.94, an easing, reported), standing +7.58; Ilya's guard holds (her Staff 41.3 / the Mortar 39.7 where held, top in 69.7 % of her runs); the Vanguard alone careful -4.03, first-card -2.91; docs/63 section 14 |
| 18001-19200 | docs/64's Rootmother Myrr at floor 3's gate, against the Forge Warden (the roster's read took 14801-18000 beside it, its own row) | 2026-11-25 | `mr:share=4.7:sap=0` (`MyrrRoundResearch`), with `shipped` and the A/A `aa-f3`, four cohorts, both heroes, typical hands, 1,200 seeds | **pass**: clumsy +0.62 (+0.33 to +0.96, an easing, reported), careful +0.17 (-0.08 to +0.42), first-card +0.38 (-0.17 to +0.92), standing -0.29 (no better than the Warden); docs/64 section 15 |
| 19201-22400 | docs/63 section 17's cost-neutral gate fight under the roster: C4, floor 1's Forge Warden at share 10 with its slam and its hammer scaled by 8.5 / 10 (the fight longer, the harm the same) | 2026-11-26 | `shipped~f1=10:blow:weapon` (`WeaponRosterResearch`, `ROSTER_CONFIRM=19201-22400`), with `shipped` (the roster, `p3-saw8`) and the A/A `aa-f1`, four cohorts, both heroes, typical hands, 3,200 seeds | **pass**: clumsy -0.39 (-1.11 to +0.33), careful +0.23 (-0.09 to +0.56), first-card +0.81 (+0.38 to +1.25, an easing, reported), standing -0.22 (-0.73 to +0.30); `aa-f1` inside its noise (clumsy +0.27, careful +0.16, first-card +0.25, standing -0.33); Ilya's guard holds (Staff 41.1 / Mortar 39.9 where both are held); docs/63 section 17.8 |

A change that is on from floor 1 (docs/57's skills) reads the floor-2 count,
3,200 seeds, and nudges floor 1 for its A/A (`HeroSkillsDescentResearch`'s `aa-f1`), whose SD per 400 seeds (13.0 for the
clumsy cohort) is wider than `aa-f2`'s 8.1: the divergence runs over four floors. A campaign read (docs/57 section 15)
spends its block as campaigns of runs, each run a fresh seed: 32 campaigns of 100 runs are 3,200 seeds a hero, the same
count. The next fresh block after these started at seed 14801; with the roster's 14801-18000, Myrr's
18001-19200 (2026-11-25) and the roster's cost-neutral gate fight's 19201-22400 (2026-11-26) the next fresh block starts
at seed **22401**.

The tiers' read was begun on 2026-11-17 and stopped by a shutdown in its last tier (Abyssal). It was read again whole on
the same code on 2026-11-18/19, and the rows the first read had written came back byte for byte (the simulation is
deterministic), so the block was read once in substance. A difficulty tier changes the whole Descent from floor 1, and
its gear changes every crit and offer. Its paired change's SD per 400 seeds is about 13 completions for the clumsy cohort,
10 for first-card and 6 for careful (a floor-2 creature's: 6-8, section 2). So at 3,200 seeds a tier's SE is about
0.60 / 0.44 / 0.27 points, and a tier that changes nothing fails the -1.5 lower bound about one time in five for the
clumsy cohort. A whole-Descent change wants its candidate set a little inside the bar on the exploration seeds.
