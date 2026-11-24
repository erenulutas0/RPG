# 64 - Rootmother Myrr, the Gloam Gardens' own gate boss (design, 2026-11-23)

A proposal for the lead's decision (the owner delegated the gameplay calls, docs/50 and docs/52). It is the "later design
of its own" docs/52 section 6 promised: "keep the Warden until Myrr is designed after the Regent". Four stages were
built for it (branch `worktree-agent-a6955bc29a63ebec1`), and only the first changes what the game shows:

1. **The bosses' barks** (section 13, built, ships on): the Forge Warden and the Glass Regent say docs/44 section 1.8's
   spawn, telegraph and death lines on a short plate over them. Visual only.
2. **This design** (sections 0-12).
3. **The simulation research slice** (section 9): Myrr behind `FoundryLevers.GloamMyrr`, off by default, in the Forge
   Warden's place at floor 3's gate only; measured on the exploration seeds 1-2000; a candidate and its confirmation
   read proposed for a fresh block from seed 11601, not spent.
4. **The art** (section 7): `Tools/ArtReview/BuildBlenderRootmother.py`, a script-built, rigged and posed toon model with
   her five clips in both facings, and its review sheets (`ArtDirection/2026-11-23/rootmother-01/`, untracked). Not
   wired into any prefab: the scene build waits for the confirmation read.

Numbers named "measured" come from section 9's runs; every other number is a starting value.

**Since 2026-11-25** she ships: the confirmation read passed (section 15), and the scene build (section 16) made her
floor 3's gate boss in the game and in the simulation's default, at share 4.7 without saplings.

## 0. The short version

| | Forge Warden (floors 1 and 3 today) | Glass Regent (floor 2) | **Rootmother Myrr (floor 3, proposed)** |
|---|---|---|---|
| The read | A circle at the hero's spot, r 2.2 | A lane from the Regent through the hero's spot | **A root snare that follows the hero**: a ring r 1.5 at the hero's spot that creeps after it at 1.5 a second through the telegraph |
| The dodge | Run out of the circle (about 2.2 units) | Sidestep across the lane | **Keep walking**: step out and stop and the ring overtakes you; walk on and you leave it |
| What it costs | 20 % of the hero's maximum | 10 % | 10 % and **planted** 1.0 s (no walking; the weapons still strike), her hook free through the recovery |
| The standing hero | Hammer in reach (22 % a fight) | Rod at its feet (docs/50 2026-11-17) | **Her feet from the start**: her hook is never held for a hero within 1.5 as the snare goes down |
| Phase two (50 %) | Faster marks, the horde thickens | Faster marks, the horde thickens (copies unbuilt) | Faster marks, the horde thickens, and **grafted saplings**: 2 at once and one every 8 s, beside her, rooted; a sapling not cut in 8 s grows into a Thornling that joins her |
| Clock | 6 s / 4 s, telegraph 1.1 s, hold to 1.5 s after | 6 s / 4 s, 1.7 s, the same hold | 6 s / 4 s, **1.2 s**, the same hold |
| Lines (docs/44 1.8) | "Shift's not over." / "Stand clear of the mark." / "Door... was never locked for you." | "Which one of you is original?" / "Hold still. I need your face." / "I kept the wrong one..." | "Your tomorrow can still take root." / "Be still. Be planted." / "Something... is growing underneath." |
| The weapon it favours | The Sword (x1.5 on the gate boss) | The Sword | The Sword on her; the Staff's splash cuts her saplings |

The recommendation (section 10): ship Myrr as floor 3's gate boss at the candidate `mr:share=4.7` (her share 4.7), after
one confirmation read on seeds 11601-12800 against the Forge Warden. On the exploration seeds 1-2000 (section 9) she
eases the clumsy, careful and first-card cohorts by 0.75, 0.20 and 0.62 points (her mark is half the Warden's blow), takes
0.35 from the standing one (her feet), and keeps the careful fight's P50 at 62.7 / 59.3 s. Her saplings, as specified,
measured inert in the pilot model (question 4).

## 1. Who she is

docs/44 section 1.8: *Yok olan gelecekleri ağaçlara aşılamış bahçıvan* - the gardener who grafted lost futures onto
trees. docs/44 section 1.5 puts floor 3 at year -21, "the Dawn Engine first warms the cities", and section 1.6 gives
the floor its clue: *trees that feed on memory instead of light*.

**Her story (for the owner's pen).** She kept the tower's orchard when the Engine was young. Each time the Engine took
a person's unspent tomorrow, something was left over - a seed of the life they would have had - and Myrr could not let
it be swept into the furnace with the ash. She grafted each seed onto a tree. The trees took, and they fed on the
memories instead of the light the Long Dimming was taking from them. She grafted for so long that the orchard grew
into her: her skirt is roots, her back carries the canopy, her buds are the futures she keeps. She is not cruel. She
wants to keep the hero's tomorrow safe too, the only way she knows - by planting it ("Your tomorrow can still take
root."). Her death line is the floor's open question: the garden grows from something under the tower ("Something...
is growing underneath."), which the Final Swarm's Unfinished and docs/44's canon question 1 (is the tower conscious?)
can both answer.

**Her lines** (EN docs/44's; TR drafts for docs/60's table when her scene build adds the rows):

| Moment | English | Turkish (draft) |
|---|---|---|
| Spawn | Your tomorrow can still take root. | Yarının hâlâ kök salabilir. |
| Telegraph (first snare of each phase) | Be still. Be planted. | Kımıldama. Kök sal. |
| Death | Something... is growing underneath. | Altta... bir şey büyüyor. |

"Kımıldama" rather than "Kıpırdama", the Glass Regent's (`bark.glass_regent.telegraph`), so the two bosses do not open
their telegraphs with the same word in Turkish.

**Codex (draft, after C33):** C34 "The Grafted Orchard" - unlocked by felling Myrr: "Every bud was someone's next year.
She labelled none of them. She remembered them all."

## 2. What the earlier rounds bind

| Lesson (measured) | Where | What it binds here |
|---|---|---|
| "Leave the circle" is read by every cohort but the auto-farmer | docs/50 (the Clerk), docs/52 section 4 | Her mark is not a static circle: it follows |
| A stillness read charges the clumsy cohort as much as the standing one | docs/52, the Pollen Wraith's measurement | Her snare is not a stillness meter: it catches the hero who stops *in it*, after one telegraph, on her beat |
| A ring at the hero's feet costs the clumsy cohort through the dodge, not the damage (`thorn-s0` costs what `thorn` costs) | docs/52, the Thornling | One ring a beat, on her clock (11 a fight, as the Warden's), not one every 2.5 s |
| The Warden's hold made a hero at a long-telegraph boss's side safe | docs/50, the Regent's round | Her feet rule from the start (RegentRule.FeetReach's 1.5) |
| Bodies to break "add little" | docs/50, the Regent's copies | Her saplings threaten on a clock: cut them or they grow into Thornlings |
| Keep the Warden's clock | docs/52 section 6 | 6 s, 4 s below half health, the hold to 1.5 s after the blow; only the telegraph moves (1.2 s, the phone guide's top) |
| The Sword stays the gate boss's weapon | WardenRule.SwordShare | Unchanged: x1.5 on her alone |
| The clumsy cohort has no room (37-40 % completion) | docs/56 | No part of her fight may read a habit the clumsy cohort lacks |

## 3. Her fight and its numbers

**The root snare (the beat).** Every 6 s after the last eruption (4 s below half her health), as the Warden's mark goes
down, a ring of radius 1.5 appears at the hero's spot. Through its telegraph (1.2 s) the ring **creeps after the hero**
at 1.5 units a second - under half the hero's 3.5. When it fills, roots erupt in it: a hero inside takes 10 % of its
maximum health and is **planted** for 1.0 s - it cannot walk; its weapons and its ability still strike. Her hook is held
from the snare until 1.5 s after the eruption, as the Warden's hammer, except (a) on a beat whose snare went down
with the hero within 1.5 of her - her feet - and (b) through the recovery after roots caught the hero: then she swings.

- A hero that reads the ring and walks straight out of it leaves it: from its centre, a typical reaction (0.25 s) and
  the 2.0 a second the hero gains on the ring cover the radius with 0.4 units to spare before the eruption. Slow hands
  (0.4 s) have 0.1 to spare.
- A hero that steps out and stops - the circle's habit - is overtaken: the ring keeps coming.
- A hero that stands in it is planted where she can reach it.

**Her feet.** The ring round her feet (orchid, harmless in itself) is the Regent's rule, built in: a hero at her side as
the snare goes down gets no window. The careful pilot's kite holds 2.0 from a gate boss; the standing hero stands in
her reach.

**Phase two: the grafted saplings.** At half health she plants 2 saplings, then one every 8 s (at most 4 standing),
3 units to her sides (square to her line to the hero, alternating). A sapling is rooted, harmless and has 2.5 % of her
maximum health. One that stands 8 s grows into a Thornling - docs/52's body (the Runner's 30 health and 1.2 a strike every
0.4 s, speed 3.0, at floor 3's tier as a hall's enemy: x1.96 health, x1.80 damage, and the run's difficulty), **without
its dive** (docs/56 decision 4 holds the dive) - that joins her fight; at most 3 stand. Saplings and grown Thornlings carry no reward and wither when she
falls. No pilot habit cuts them: a hero's weapons strike the nearest target, so the Staff's splash and the Sword's cleave
cut saplings near her, the Daggers do not.

**Numbers (starting values; section 9 measures them).**

| | Forge Warden on floor 3 | Rootmother Myrr |
|---|---|---|
| Health (authored), floor-3 share | 300, 5.102041 (11.5 / (1.96 x 1.15)) | 300, **5.102041** at the start (`MyrrHealthShare` moves it; section 9) |
| Weapon | Warden Hammer 6.0 / 1.8 s / 1.7 (1.0) | Grafting Hook **5.5** / 1.8 s / 1.7 (1.0) |
| Speed | 1.0 | **0.9** (she walks on her roots) |
| Mark | circle r 2.2, 20 %, every 6 s / 4 s | snare r 1.5 creeping 1.5 / s, 10 % and planted 1.0 s, every 6 s / 4 s |
| Telegraph, recovery | 1.1 s, 1.5 s | 1.2 s, 1.5 s |
| Hold | mark to 1.5 s after the blow | the same, never for a hero at her feet (1.5) nor after roots caught the hero |
| Phase two (50 %) | faster marks, the horde thickens | the same, and saplings: 2, then 1 every 8 s, 2.5 % of her health, grow in 8 s into Thornlings (at most 4 saplings, 3 Thornlings) |
| Escort | two Cinder Mites | two Cinder Mites |
| Gold / XP | 50 / 10 | 50 / 10 |

**Fight length.** docs/40 section 9.6 and docs/50/52's band: the careful pilot's P50 in 45-65 s, both heroes. The
health share is the lever, as for the Warden and the Regent; section 9 measures where it lands.

## 4. Her tells and their timings

| Moment | Seconds | Floor | Body (BossFeelView's grammar) | Bark |
|---|---|---|---|---|
| Walks out of the gate | the 2.6 s reveal | - | the canopy unfurls as she steps through | "Your tomorrow can still take root." (spawn) |
| Snare goes down | 0.0 | a red-orange ring r 1.5 at the hero's spot, orchid root tips along its rim | the telegraph clip's gather (windup-0): she stoops, root hand reaching to the floor | "Be still. Be planted." with the first snare of each phase |
| The snare creeps | 0.0-1.2 | the ring slides after the hero at 1.5 / s, its fill growing from the centre (WardenSlamClock.TelegraphProgress) | she rears, hook high, root hand thrust at the hero (windup-1), quivering at the peak (windup-2) over the last quarter; her buds flare | - |
| Roots erupt | 1.2 | roots burst in the ring; a caught hero shows planted roots round its feet for 1.0 s | the root strike: the hook swung down and driven into the floor (strike-0..2) | - |
| Recovery | 1.2-2.7 | - | the hook dragged back; held (the window to strike back) unless the hero is at her feet or planted | - |
| Her feet | while fought | an orchid ring r 1.5 round her (the Regent's violet ring's rule: a creature's mark, never hurts by itself), pulsing on a beat that found the hero inside | no windup raised on that beat | - |
| Phase two | at 50 % | two saplings sprout with an orchid burst | phase change: she rears back, the canopy bursts into bloom, she settles (phase-0..2); from then the second phase's colours | - |
| A sapling grows | 8 s after it sprouts | its bud swells through its last 2 s, then a Thornling claws out of it | - | - |
| Death | the fall | her saplings and Thornlings wither | rocked back, sinking into her roots, slumping, a stump (fall-struck..stump) | "Something... is growing underneath." |

The plate of a bark never stands over the snare (section 13's placement test reads a circle; the snare's ring is one,
moving - the view reads its centre each frame).

## 5. Readable on a portrait phone

- **Distinct at a glance from the other two marks**: the Warden's ring is large and still, the Regent's a long stripe,
  Myrr's a small ring that moves. The movement is the read: nothing else on the floor's red-orange slides.
- **Colours** (docs/52 section 2.1 rule 3): the ring is red-orange (it hurts on a timer); the root tips on its rim, her
  feet ring, her buds and her saplings are orchid (creatures); nothing of hers is gloamlight (docs/51 rule 5).
- **Scale**: at 1080 px across 4.6 world units her ring is about 700 px across and 350 px deep on the floor, the Warden's
  about 1,030 by 520 (nearly the screen's width): hers is the smaller, moving shape.
- **Planted**: a hero caught shows roots round its feet for the second it stands (the HUD's stick greys); the hook's
  strike follows if she is in reach - the cause and the cost read together.

## 6. Fight length

The careful pilot's P50 in 45-65 s for both heroes is the band (docs/40 9.6; docs/50 and 52 keep it). The share moves;
the snare's numbers and the saplings' stay fixed during the share's sweep (docs/50's rule: the band is bought with one
number). Section 9 reads P10 / P50 / P90 by cohort and hero.

## 7. Art brief and pass 1

**Brief.** The boss contract the Warden and the Regent use (one Blender model renders every frame, both facings, both
phases; `EnemyArtSet` with its walk, windup, strike and fall rows, about 48 drawings and flashes). Her silhouette at
80 px: a wide canopy of branches over a hunched dark column on a skirt of roots wider at the floor than any golem; the
pale mask the first thing read under the canopy; the hook's verdigris crescent beside it. Colours: docs/51's enemy bark
(`#4A3B33`) a step lighter than the ash loam with the dark outline, a heartwood mask, a plum leaf mantle (`#5A1F66`),
orchid buds and heart (`#D86BD0`, `#F6C2F0`), a verdigris blade (`#6FA38C`); no red-orange, no gloamlight. Phase two:
the buds open into blooms and orchid veins run down her roots - a shape change (petals) as well as a colour one.

**Pass 1 (built, stage 4).** `Tools/ArtReview/BuildBlenderRootmother.py` (Blender 4.5, the Regent's toon group,
camera, contract and solver; no legs - she walks on six root panels, each on its own bone) renders 15 poses: idle (2),
the telegraph (3), the root strike (3, with the roots bursting round the blade on their own bone), the phase change (3)
and death (4, a floor holdout hiding what sinks), in the first phase's and the second's colours, both facings (the
last two death drawings shared, as the Regent's topple and down are) - 44 frames, each a beauty and a glow pass.
`Tools/ArtReview/review_rootmother.py` lights them (the glow channels coloured orchid, a small halo) and lays out the
review. The walk (step A/B, pass A/B) and the windup-at-a-walk rows the scene's `EnemyArtSet` needs are not drawn yet:
the next pass, with the pack, once the confirmation read passes. (Drawn and packed 2026-11-25: section 14.)

**The review** (`ArtDirection/2026-11-23/rootmother-01/`, untracked): `1-clips-front.png` and `2-clips-rear.png` (every
clip, both phases, on the ash loam), `3-phone-scale.png` (idle, idle in phase two, the telegraph's peak and the strike at
the S23's scale beside the Forge Warden, the Glass Regent and the Vanguard, on the deep, mid and light loam, and a
squint), `4-value-map.png` (luminance and the orchid mask), `5-clips-front.gif` (the clips at their seconds),
`checks.json`, `sprites/` (the 44 lit frames), `rootmother.blend` (the model, rig and a pose action a frame), and the
first look kept as `*-pass1.png`.
- **Checks** (`checks.json`): warning red-orange texels 0, gloamlight texels 0 over all 44 frames; orchid 10.6 % of her
  opaque texels (the buds, the mantle, the heart; the Cinder Bloom's open bloom stays the floor's saturated orchid mass in
  the halls); her idle's mean luminance 0.26 against the mid loam's 0.24 (a step lighter, docs/51 rule 3), with the dark
  outline; the idle 1.82 x 2.60 world units at the pivot.
- **The look, pass 1 to 1.1** (my own review): at her design scale she stood 1.24 times the Regent's height with her root
  skirt wider than any boss at the phone's scale, and the root tips met the canvas's foot. Drawn at 0.88 with the pivot
  raised (92 px), she stands the tallest boss without crowding the hero; the dark core between her root panels makes the
  skirt read as roots, not a dress; her buds wither to a dark husk as she dies. The squint keeps the canopy's dots, the
  plum mantle and the mask: a silhouette apart from the Warden's block and the Regent's column.
- **Still to judge** (the lead's and the owner's look): the canopy's many bright points against the Pollen Wraith crowd;
  the phase-two veins (bright orchid lines down the skirt) at 80 px; the hook's crescent reading as a blade rather than a
  crook.

## 8. How she fits the floor

- **Pollen Wraith** (the horde's role 0, shipped as the Mite reskinned): the trees' shed memories. Myrr's lore makes
  them hers: what her orchard lets go of. No mechanic links them; the Wraiths at her gate are the horde's.
- **Cinder Bloom** (Thorn Post's elite, shipped): a foundry golem her garden grew in. Its bark-and-bloom cycle is the
  floor's first lesson in "strike the open window"; her hold's window is the same lesson at the gate.
- **Thornling** (held, docs/56 decision 4): her saplings grow into Thornlings, the Runner's body without the dive, so the
  floor introduces the creature's body before its dive is decided. If the lead later ships the dive, a grown Thornling
  could dive too - a separate read on a fresh block.
- **The floor's colours**: her ring is the floor's only moving red-orange; her orchid is the floor's creature colour; the
  Grove's gloamlight stays in the walls.
- **The Warden twice**: with Myrr at floor 3 the Forge Warden is floor 1's alone, the Regent floor 2's: docs/48 1.4's
  "same boss again" is solved.

## 9. Measured (simulation only, exploration seeds 1-2000)

Simulation research: nothing the game plays changed. Every read is `MyrrRoundResearch` at `5181db0c` (this branch), the
four-floor Descent (`GloamChain`, the shipped floors 1 and 2 above floor 3), both heroes, typical hands, the careful,
standing, first-card and clumsy cohorts, paired by seed against `shipped` - the Forge Warden at floor 3's gate on the same
code. Seeds 1-2000 only: the spent exploration block (docs/56 section 10); no fresh seed was read.

**What was built.**
- `Scripts/Combat/MyrrRule.cs` (compiled by CombatChecks and Unity, read by nothing the game plays yet): the starting
  values and the shared bodies a later scene and the simulation both call, never inlined, squares cast to float -
  `Creep`, `InSnare`, `AtHerFeet`, `HoldsHook`, `SnareDamage`, `SaplingSpot`, `Grown`, `SaplingHealth` - and `RootSnare`
  (one beat's ring: down, creeping, erupting).
- `Tests/Support/DescentSimulation.Myrr.cs`: `RootmotherMyrr` (300, Grafting Hook 5.5 / 1.8 s / 1.7, speed 0.9),
  `GraftedSapling`, `GraftedThornling`; the levers (`GloamMyrr` and her tuning); `StepWarden`'s hooks where the scene's
  `WardenSlamService` would step her (her beat tuned on the clock, the snare's creep before the clock steps - the pilot's
  mark is the ring's centre each frame - its mark, its eruption and the plant); the planted hero's walk held in `Frame`;
  her hook's hold (`GateBossHeld`); the garden stepped where `GloamService` steps (`StepGloam`), withered with her; the
  accounting. `GloamChain` puts her at the Warden's Grove alone, its two Mites kept.
- `Tools/CombatChecks/MyrrRoundResearch.cs` (explicit): options `shipped`, `myrr`, `aa-f3`, `myrr-nosaplings`,
  `myrr-still`, `myrr-nofeet`, `myrr-noplant` and `mr:` lever strings; `myrr-round.md` and `.csv`;
  `Tools/BalanceRule/paired_bootstrap.py` reads `myrr-round.csv` against `shipped`.
- Tests: `MyrrRuleTests` (the creep never passes the hero, the snare's edge, her feet's boundary, the hook's hold, the
  saplings' spot), `FoundryMyrrTests` (the lever off: the chain and the runs seed for seed; on: her place alone, her
  fight's rules on every floor 3 that reached her, floors 1 and 2 unchanged, repeatable).

**The screen (seeds 1-200, 400 runs a cohort).** Completions of 400 and their change against the Warden (the bootstrap's
points and 90 % CI); the careful pilot's gate fight P50 for the Vanguard / Ilya with P10-P90, and the share of fights in
45-65 s.

| Option | Clumsy | Careful | First-card | Standing | Careful fight P50 s, V / I (P10-P90) | In band V / I |
|---|---|---|---|---|---|---|
| Forge Warden (`shipped`) | 160 | 384 | 356 | 323 | 61.5 / 56.6 (49-75; 45-70) | 55 / 71 % |
| Myrr at the Warden's share 5.10 (`myrr`) | -1 (-0.25; -1.25 to +0.75) | -1 (-0.25) | -1 (-0.25) | -3 (-0.75; -2.00 to +0.50) | 66.8 / 61.2 (54-80; 50-75) | 43 / 60 % |
| share 4.8 | +0 | -2 (-0.50) | +2 (+0.50) | -3 (-0.75) | 64.6 / 59.5 | 52 / 68 % |
| share 4.6 | +2 (+0.50) | +0 | +2 (+0.50) | -1 (-0.25) | 62.4 / 58.4 | 56 / 69 % |
| no saplings (`myrr-nosaplings`, 5.10) | +0 | -1 | +0 | -5 (-1.25; -2.50 to +0.00) | 67.0 / 61.3 | 43 / 59 % |
| a static snare (`myrr-still`, creep 0, 5.10) | +0 | +0 | +4 (+1.00) | +1 (+0.25) | 61.4 / 57.7 | 58 / 70 % |

Her fight as the hero stood in it (means a fight, seeds 1-200, `myrr`; the Warden's beside it where it has one):

| | Careful | Standing | First-card | Clumsy |
|---|---|---|---|---|
| Beats (Warden's) | 12.0 (11.0) | 9.6 (8.4) | 10.9 (10.0) | 11.8 (11.5) |
| Marks that caught (Warden's) | 1.01 (0.74) | 0.82 (0.60) | 0.78 (0.57) | 0.90 (0.53) |
| Health to the mark, share of maximum (Warden's) | 4.1 % (6.2 %) | 3.5 % (5.0 %) | 4.2 % (6.0 %) | 2.6 % (3.0 %) |
| Health to the boss's weapon (Warden's) | 4.0 % (4.1 %) | 19.8 % (19.0 %) | 7.4 % (7.8 %) | 6.2 % (7.6 %) |
| Beats at her feet | 0.12 | 1.97 | 0.09 | 0.25 |
| Saplings planted / cut / grown | 4.7 / 2.1 / 2.0 | 4.0 / 2.5 / 0.9 | 4.4 / 2.3 / 1.5 | 4.6 / 3.1 / 0.9 |
| Health to grown Thornlings | 0.1 % | 0.1 % | 0.1 % | 0.2 % |
| Deaths at floor 3's gate, of 400 (Warden's) | 2 (1) | 15 (12) | 12 (11) | 8 (7) |

What the screen says:
- **The creep is her fight.** With the ring still (`myrr-still`) a pilot leaves it as it leaves the Warden's circle and it
  catches 0.2-0.3 times a fight; the fight is the Warden's length; the standing cohort does a little better than with the
  Warden. With the creep it catches 0.8-1.0 times a fight, the pilots walk longer out of it (out of their weapons' reach
  of her), the fight runs about 5 s longer at the same share, and the standing cohort does a little worse than with the
  Warden - the direction docs/56 decision 3 asks of a gate boss.
- **Her mark costs less than the Warden's** (4 % of the hero's health a fight against 6 %): a catch is 10 % and a planted
  second, against the Warden's 20 %. Her hook takes what the hammer takes; her feet take the standing hero's window (2.0
  beats at her feet a fight).
- **The saplings change nothing measurable.** Without them the fight is as long and every cohort within a run or two.
  The pilots' weapons strike the nearest target: half the saplings are cut where they stand and the Thornlings that grow
  are felled within a second or two - 778 grew in 386 careful fights and took 120 health in all. docs/52 section 6 asked
  for "a reinforcement that threatens"; in the pilot model these do not threaten (section 11, question 4).
- **The share moves the fight and little else** (4.6-5.1: every cohort within 3 runs of 400). 4.8 puts the Vanguard's
  careful P50 at 64.6 s, the band's edge; 4.7, between 4.6 and 4.8, is the candidate (section 10).

**The read on seeds 1-2000 (the candidate `mr:share=4.7` against the Warden).** 2,000 seeds, both heroes (4,000 runs a
cohort), the exploration block only; `paired_bootstrap.py <folder> 1 2000`. An exploration read on spent seeds, not the
confirmation (docs/56 rule 1: that is section 10's, on fresh seeds).

| Cohort | Warden: completed of 4,000 | Myrr 4.7 | Change | Points | 90 % CI | SD per 400 seeds | First / second 1,000 seeds |
|---|---|---|---|---|---|---|---|
| Clumsy | 1,505 (37.6 %) | 1,535 | +30 | **+0.75** | +0.47 to +1.05 | 3.1 | +15 / +15 |
| Careful | 3,834 (95.9 %) | 3,842 | +8 | **+0.20** | +0.03 to +0.40 | 2.0 | +4 / +4 |
| First-card | 3,287 (82.2 %) | 3,312 | +25 | **+0.62** | +0.17 to +1.07 | 5.0 | +25 / +0 |
| Standing | 3,263 (81.6 %) | 3,249 | -14 | **-0.35** | -0.65 to -0.05 | 3.4 | -5 / -9 |

The gate fight, P10 / P50 / P90 seconds over the fights the boss fell in, and the share in 45-65 s (Vanguard; Ilya):

| Cohort | Forge Warden | Myrr 4.7 |
|---|---|---|
| Careful | 48.5 / **60.8** / 74.0 (60 %); 45.8 / **57.2** / 68.5 (72 %) | 50.4 / **62.7** / 76.1 (55 %); 48.1 / **59.3** / 71.0 (70 %) |
| Standing | 39.6 / 48.0 / 58.7 (66 %); 38.4 / 46.3 / 55.5 (59 %) | 42.4 / 51.5 / 62.4 (75 %); 41.2 / 49.6 / 59.6 (73 %) |
| First-card | 46.1 / 57.0 / 67.2 (76 %); 44.0 / 53.8 / 62.6 (81 %) | 48.9 / 59.1 / 69.9 (72 %); 45.6 / 55.8 / 65.5 (80 %) |
| Clumsy | 50.2 / 61.6 / 73.4 (61 %); 50.7 / 62.5 / 75.4 (59 %) | 50.3 / 61.9 / 73.5 (60 %); 51.1 / 62.4 / 75.4 (58 %) |

And as the hero stood in it (means a fight; the Warden's in brackets): marks that caught careful 0.97 (0.70), standing
0.84 (0.66), first-card 0.75 (0.57), clumsy 0.74 (0.56); health to the mark 3.9 % (5.6 %), 3.4 % (5.4 %), 4.2 % (6.2 %),
2.2 % (3.3 %); to the boss's own weapon 3.8 % (4.0 %), 18.4 % (18.4 %), 7.7 % (8.2 %), 5.8 % (7.6 %); beats at her feet
0.11, 1.85, 0.10, 0.25; saplings 4.5 / 3.8 / 4.2 / 4.5 a fight, of which grown 1.7 / 0.7 / 1.4 / 0.9, their Thornlings
taking 0.05-0.15 % of the hero's maximum. Deaths at floor 3's gate, of 4,000: careful 40 to 32, standing 121 to 135,
first-card 176 to 151, clumsy 96 to 66. The Descent's P50 of completed runs is unchanged within 0.1 minutes (careful
14.5 / 14.3, standing 14.0 / 13.9, first-card 14.3 / 14.2, clumsy 14.4 / 14.4; Vanguard / Ilya), every completed run in
13.2-15.8.

What the read says:
- **Every gated cohort passes docs/56's harm bar with room**, and the clumsy, careful and first-card changes are easings
  (their CIs wholly above 0): her mark takes 10 % where the Warden's took 20 %, and her fight is no longer. Reported to
  the lead under rule 3, as the Regent's +1.1 / +1.3 points for careful and first-card were.
- **The standing cohort completes 0.35 points fewer Descents than with the Warden** (CI wholly below 0): her feet keep
  the window from the hero at her side (1.85 beats a fight), and its deaths at her gate rise 121 to 135. The gate boss's
  standing target (no better than the Warden) holds - the direction the Glass Regent missed before its round.
- **The band holds**: the careful P50 62.7 and 59.3 s, a second or two over the Warden's, 55 % and 70 % of fights inside
  45-65 s (the Warden's 60 % and 72 %).
- The first-card easing came all in the first 1,000 seeds (+25 / +0): inside the noise of an SD of 5.0 per 400 seeds.

**The default batch** (`FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_REALISM=typical FOUNDRY_PARALLEL=6`,
`FOUNDRY_FLOORS=2` and `4`, filter `FullyQualifiedName~FoundryBatchReport.TheFoundryFloorOverASeedBatch`), main
`8121816c` against this branch's `5181db0c` (stages 1 and 3), each built in a clean folder of its own sources: at both
floor counts every CSV, `foundry-gate.md`, `foundry-cohorts.md` and (at 4) `foundry-finale.md` match byte for byte -
`foundry-cohorts-floor3.csv` (465,847 bytes) and `foundry-finale.csv` among them, so floor 3 and the finale are unchanged
with the lever off; in `foundry-batch.md` only the header's clock and the wall-seconds column differ. CombatChecks at
`5181db0c`: 1,132 green ("Başarılı"). The Unity compile check: 0 errors, 0 warnings.

## 10. The candidate and its confirmation read

**The candidate: `myrr-s4.7`** - Myrr at section 3's values with her gate share at **4.7** (`MyrrHealthShare` 4.7;
`mr:share=4.7` in `MyrrRoundResearch`). Its design reason, written before any confirmatory read: her fight should cost
what the Warden's costs in seconds, so that the band holds and the comparison reads her mechanics, not a longer fight.
At the Warden's own share (5.102041) she runs about 5 s longer for the careful pilot (her saplings take strikes, she
walks slower, the hero walks out of a creeping snare rather than out of a circle); 4.7 / 5.102 = 0.92 takes those
seconds back (section 9: 4.8 lands at the band's top for the Vanguard, 4.7 inside it). Nothing else is tuned: the
snare, the plant, her feet and the saplings stay at section 3's starting values.

**The read, proposed and not spent** (docs/56 rule 1: a floor-3 change confirms on 1,200 fresh seeds):

- Block **11601-12800** (the next fresh block, docs/56 section 10), both heroes, typical hands, every gated cohort and the
  standing one, paired by seed against `shipped` on the same code - the Forge Warden at floor 3's gate - with the A/A
  probe `aa-f3` beside it. One candidate: `mr:share=4.7`. If the lead drops the saplings first (question 4: they
  measured inert in the pilot model), the round's second and last candidate is `mr:share=4.7:sap=0` instead - one or the
  other is read, whichever would ship, never both searched.
- **The exploration read predicts a pass** (section 9: clumsy +0.75, careful +0.20, first-card +0.62, standing -0.35 on
  seeds 1-2000, every bar met with room); the spent block's luck is about half a point either way (docs/56 section 1), so
  the read on fresh seeds is still the decision.
- **Pass** (docs/56 section 0 and decision 3): clumsy, careful and first-card each at least -1.0 point with the CI's lower
  bound at least -1.5; **standing no better than the Warden** (its point change at most 0: the Regent's lesson, the
  standing read is the gate boss's design target). An easing CI wholly above 0 passes and is reported.
- Reported beside it: the careful pilot's gate fight P10 / P50 / P90 for both heroes (in 45-65 s at P50), the Descent's
  P50, the snares that caught, the planted seconds, saplings cut and grown, deaths at her gate by cohort.
- Record the block in docs/56 section 10 when it is read.

```
MYRR_OPTIONS=shipped,aa-f3,mr:share=4.7 MYRR_COHORTS=careful,standing,first-card,clumsy \
  FOUNDRY_FIRST_SEED=11601 FOUNDRY_SEEDS=1200 FOUNDRY_REALISM=typical FOUNDRY_OUT=<folder>/confirm \
  dotnet test Tools/CombatChecks -c Release -p:NuGetAudit=false --filter "FullyQualifiedName~MyrrRoundResearch"
python Tools/BalanceRule/paired_bootstrap.py <folder> 11601 12800
```

About 24,000 four-floor runs (three options); on this machine with other work beside it, two to three hours.

**If it passes**, the scene build (about 6-8 days): `MyrrRule` is already the shared body; `WardenSlamService` keeps her
beat and steps a `RootSnare` (the creep before the clock, as the simulation does), `WardenSlamView` draws the moving ring,
`HeroMovementInput` holds the planted walk, a `GloamService` step grows her saplings, `Enemy_RootmotherMyrr` and
`Weapon_GraftingHook`, the art's walk rows and pack, her three bark rows (section 1), `Floor_GloamGardens`' gate, and the
parity and flow tests (`FoundrySimulationSceneValueTests`, a `MyrrFlowTests` on a proof floor). **If it fails**, the
round's rule (docs/56 rule 4) allows one more candidate with a written reason; the levers to reach for are
`MyrrSaplings` 0 (no saplings) and `MyrrCreepSpeed` 0 (a static ring), both measured on the spent seeds in section 9.

## 11. Risks

- **The pilot model's escape is not a person's.** The pilots read her snare with the Warden's escape (leave the mark's
  centre by its reach, away from her) re-read every frame as the ring moves; a person who steps out and stops - the
  habit the Warden taught - is the one the snare punishes, and the model's pilots never stop while in reach. The S23
  play is the read of whether people learn "keep walking" in one or two beats or are planted beat after beat.
- **A moving red-orange ring is new.** Every telegraph so far is fixed where it went down. A ring that follows the hero
  may read as a homing attack rather than a mark (the owner's look); the lever `MyrrCreepSpeed` 0 is the fallback (a
  static ring 1.5 that plants), measured as `myrr-still` (section 9).
- **Planted under a horde.** In phase two the horde thickens; a planted second inside a crowd costs more than the
  simulation's crowds show (the scene's crowds are denser at the gate; docs/41's parity is within about 6 s of fight).
- **The saplings' threat is weapon-dependent.** The Staff's splash cuts saplings beside her as it strikes her; the
  Daggers do not. A Daggers build meets more Thornlings (section 9 reads saplings cut and grown by cohort, not by weapon).
- **Phone frame time.** Up to 4 saplings and 3 Thornlings over the gate wave and the thickened horde: about 7 more bodies
  at the gate than the Warden's fight. Unmeasured on the S23.
- **Art pass 1 is not the scene's contract.** The walk and windup-at-a-walk rows are not drawn; the fall is a sink, not
  a topple, so `EnemyArtSet`'s fall frames read differently; the scene build pairs the pack with the contract.
- **Not run in Unity.** Stage 1's PlayMode flows and every EditMode test are compiled, not run (no Unity in a worktree).

## 12. Questions for the lead

1. **Ship Myrr as floor 3's gate boss** (section 10's candidate), subject to the confirmation read on 11601-12800?
2. **The snare follows the hero.** Keep the creep (her identity: "roots that follow"), or the static ring (`myrr-still`)
   if the owner's look finds the moving ring reads as homing?
3. **Planted.** 1.0 s, with her hook free through the recovery if she is in reach - acceptable as the snare's cost, beside
   the 10 %?
4. **The saplings.** They measured inert in the pilot model (section 9: the pilots' weapons cut or fell them before they
   hurt; 0.05-0.15 % of the hero's health a fight), so the rule passes with or without them, and they cost the scene a
   service, art and up to seven more bodies at the gate. Ship them as phase two's look and pressure (people may cut them
   slower than the autopilot), drop them for the first build (`MyrrSaplings` 0, the confirmation's second candidate), or
   hold them for a round with the Thornling's dive (docs/56 decision 4)? My recommendation: drop them for the first
   build; her identity is the snare and her feet.
5. **The fight's telegraph** at 1.2 s (the phone guide's top), or 1.1 s as the Warden's?
6. **The room's name**: "Warden's Grove" names a boss who is no longer there. Keep it (the Warden once kept the floor), or
   rename it ("The Grafted Orchard", "Myrr's Grove")? The rows are `room.wardens_grove.name`; the Sanguine quest does not
   name floor 3's room.
7. **Her lines' Turkish** (section 1): "Kımıldama. Kök sal." so the two bosses' telegraphs differ in Turkish - agreed?
8. **The art direction** of pass 1 (section 7, the sheets): a hunched tree-matriarch with a canopy of orchid buds, a pale
   mask and a verdigris hook - agreed before the walk rows and the pack?

## 13. Stage 1: the bosses' barks (built 2026-11-23)

The owner asked for the story in the game: docs/44 section 1.8's lines, spoken by the bosses that ship.

**What the player sees.** As the Forge Warden or the Glass Regent walks out of the gate (the reveal's camera on it), a
short plate over it says its spawn line; with the first mark of phase one, and again with the first of phase two, its
telegraph line; as it falls, its death line where it fell. Each bark fades in over 0.15 s, holds, and fades out over its
last 0.3 s: 1.8 s in all (the owner's 1.5-2 s). The telegraph line is said at most once a phase (twice a fight); a fight
that has said it stays quiet through every later mark of that phase.

**The plate.** The HUD's own: the smoked band behind the boss's name (`HudArtSet.BossNamePlate`; a flat smoked band if the
painted HUD is off), the HUD's font and ivory (`#EEE8D9`), the line in italics so it reads as speech, not an instruction.
At most 640 of the HUD's 1080 canvas units wide; a line at 34, fitting down to 24, on one line or two. On its own overlay
canvas (order 1: over the HUD's 0, under the level-up's 10, the result's 20 and the pause's 30), so a panel covers it.

**Never over the mark.** While a mark is down - and through the blow's 0.35 s flash - the plate takes the first of nine
places that keeps clear of it by 0.15 floor units: over the head, under the feet, beside the body either way, beside the
head either way, above the mark's top edge, below its bottom, higher over the head. The test is on the floor's own scale
(`BossBark.ClearOfCircle` for the Warden's circle, `ClearOfLane` for each Regent lane: a segment-to-box distance), the
mark's foundry lift taken off as `WardenSlamView` draws it. Every place stays inside the free band between the HUD's
panels and the screen's sides; with none clear the plate hides (its clock still runs) until one is. The same test will
take Myrr's moving ring (a circle read each frame) when her scene build comes.

**Language.** Six rows under `bark.<boss>.<moment>` (docs/60 section 11): the English docs/44's, the Turkish short -
"Vardiya bitmedi.", "İşaretten uzak dur.", "Kapı... sana hiç kilitli değildi.", "Hanginiz asıl?", "Kıpırdama. Yüzün
lazım.", "Yanlış olanı sakladım...".

**Built.**
- `Scripts/Art/BossBark.cs` (pure, compiled by CombatChecks): the lines (`TryLine`, `Lines`), the bark's life and fades
  (`Seconds`, `Alpha`, `Shows`), the placement test (`ClearOfCircle`, `ClearOfLane`, `SegmentToBoxSquared`) and
  `BossBarkMemory` (spawn once, the telegraph once a phase, death once).
- `Scripts/UI/BossBarkView.cs`: added by `CombatSetup` after `BossFeelView`, reading `WardenSlamService` (`Boss`,
  `Marked`, `Slammed`, `IsEnraged`, `MarkX/Y`, `Lanes`) and the gate boss's definition id; scaled time (a pause or a
  panel holds a bark with the fight); a bark is dropped when the run ends or its floor changes. It writes nothing to the
  service, the encounter or the simulation.
- `Resources/Localisation/Strings.txt`: the six rows.

**Tests.** `BossBarkRuleTests` (EditMode, and in CombatChecks): docs/44's six lines under their keys, the 1.5-2 s life and
its fades, the once-a-phase memory, the plate against the circle (lifted too) and against lanes (beside, past the end,
behind the caster, across a diagonal). `BossBarkFlowTests` (PlayMode): the Warden barks as it walks out (1.5-2 s, on the
painted plate), at its first mark, not at its second, again at phase two's first and not its second, and as it falls; the
Regent says its own three lines and its plate keeps off its lanes through three beats; every frame a shown plate is checked
against the mark down; every line laid in Turkish fits its plate at its smallest best fit (`TurkishScreensTests.Check`),
within the plate's widest and two lines, and no key shows.

**The default batch** is byte-identical (section 9): nothing here is read by the simulation.

**Unity filters** for the lead: `BossBarkRuleTests` (EditMode), `BossBarkFlowTests` (PlayMode), with `BossFeelTests`,
`WardenFlowTests`, `GlassRegentFlowTests`, `TurkishScreensTests` and `LocalisationAssetTests` beside them (the HUD canvas
gains a sibling canvas; the boss views gain a component).

**Risks.**
- Not run in Unity (no Unity in the worktree): the compile check is green; the PlayMode flows are the lead's gate.
- The plate over a boss at the band's top edge is pushed down into the band; with a mark down and every place blocked it
  hides - a line can then show for less than its 1.8 s (the telegraph's, rarely).
- The bark plate sits over the world under every panel; the boss name banner at the HUD's top says the boss's name at
  the same time as the spawn bark: two texts at once for 1.8 s. The owner's look on the S23 judges it.

## 13. The lead's answers (2026-11-23, the night shift)

1. **Myrr ships at share 4.7, subject to one confirmation read.** The read takes the next free block **at the time
   of the read**, as docs/56 section 10's ledger records it. The skills re-read and the weapon roster's phase 2 may
   already take the blocks from 11601, so it must not assume 11601.
2. **The saplings are dropped from the first build.** The confirmation reads `mr:share=4.7:sap=0`, the one candidate.
3. **The creeping snare stays.** The phone session judges whether the moving ring reads as homing; the still ring is
   the fallback.
4. **The 1.0 s plant, with her hook free during it, is accepted.**
5. **The telegraph is 1.2 s.** Readability on a phone comes before matching the Warden's 1.1 s.
6. **"Warden's Grove" is renamed** when Myrr ships, with the owner's docs/60 review.
7. **Myrr's Turkish telegraph is "Kımıldama. Kök sal."** It starts with a different word from the Regent's line.
8. **The art direction stands:** a hunched tree-matriarch, orchid buds, a pale mask, a verdigris hook. The walk rows
   come with the scene wiring.

## 14. Her art set: the walk rows and the pack (built 2026-11-25)

The owner approved her look after pass 1.1 (section 7). This takes her from review frames to a game-ready
`EnemyArtSet`, the Glass Regent's way. Nothing wears it yet: the fight's wiring waits on section 10's confirmation read.

**The new rows** (`Tools/ArtReview/BuildBlenderRootmother.py`; the same model, camera, contract - 128 texels a unit,
rendered at 4x - and pivot):
- **The walk** on her six root panels, each on its own bone, in two tripods (the front and the two back panels, then the
  two front sides and the back). One tripod is planted and slides back under her while the other lifts and reaches
  forward. The body rises as one passes the other. Four quarters (step-a, pass-a, step-b, pass-b) over a stride of 1.0
  design units: a **walk cycle of 0.88 world units** at her 0.88 scale, about the Regent's cadence at her 0.9 a second.
  A lifted panel rises (a per-bone lift in the solver, empty in every earlier pose) more than it turns out, so a step
  never spreads her wider than her stance.
- **The telegraph at a walk**: windup-1's raised hook and thrust root hand, with its roots drawn in, over the walking
  roots (windup-step-a, windup-pass-a, windup-pass-b). Its contact B, `windup`, is the standing hold (the Warden's
  contract).
- **The fall** renders on a canvas of its own (544 x 480, pivot 272, 184) and records its floor footprint for the
  contact shadow. On the standing canvas the stump's hook ran off the foot.
- The approved poses are untouched: idle-a and strike-1 re-render pixel for pixel.

**The pack** (`Tools/ArtReview/PackRootmotherSprites.py`, on `PackGlassRegentSprites.py`):
- **Glow and outline.** The glow channels are coloured orchid per phase, tuned to the approved review's lighting: about
  the same near-white share in the buds and blooms, the same pink. Then the one-texel outline in her ink and the bloom
  halo. The halo's last fringe (alpha under 12) is cut so it never widens a rect.
- **Rects.** The first phase, the second and the flash share one rect, symmetric about the pivot, in whole 4x4 blocks.
- **The set's poses.** idle = idle-a, strike = strike-1 (the hook driven into the floor, roots bursting round it; the
  game shows it for her hook's swing and the snare's eruption alike). The fall is struck, sink, slump, stump at 0.16 /
  0.44 / 0.16 / 0.56 s (the Regent's 1.32 s). From the slump the two facings share drawings. The sink and the stump are
  the two remains.
- **Measures.** visualTop 2.174 (the Regent's 2.01, the Warden's 1.642), shadow 1.2, walk 0.88.
- **The asset.** `Art/Characters/RootmotherMyrr/RootmotherMyrrArtSet.asset` (guid 25275b3c224747828e689cb76c268d86) is
  laid out field for field as the Regent's. `_frost` and `_gloam` are empty: she is born on floor 3.
- **66 textures**, as the Regent's: 20 drawings a phase, 20 flashes the second phase borrows, and the fall's 6.
- **The import** is the Regent's .meta: one sprite, 128 texels a unit, bilinear, no mipmaps, not readable, Android at
  most 512 (her largest is 416).

**Memory.** On Android, as ASTC, she takes **4.32 MiB** against the **Regent's 3.27 MiB** (1.32 times). Each of her
drawings is about 1.6 times the Regent's area (the canopy, the hook beside it, the root skirt), so the white flashes,
which carry no colour, take ASTC 6x6; the drawings stay 4x4. All at 4x4 she would take 5.22 MiB. The next lever, if the
lead wants her nearer, is half-resolution flashes (about 3.9 MiB). It is not taken, because it breaks the contract's
"the flash shares its body's rect".

**Checks** (`review_rootmother.py`'s, run by `Tools/ArtReview/review_rootmother_set.py`):

| Frames | Red-orange texels | Gloamlight texels | Orchid share | Idle luminance (mid loam 0.242) |
|---|---|---|---|---|
| The packed set (46 drawings; the flashes aside) | 0 | 0 | 11.8 % | 0.253 |
| The 32 new raw frames, lit as pass 1 lit its frames | 0 | 0 | 9.7 % | 0.26 |

- Pass 1.1 measured an orchid share of 10.6 %.
- Walk luminance (phase one): front 0.238-0.261; rear 0.227-0.238. Pass 1.1's rear idle is 0.234, so the rear sits
  near the loam's value, and the dark outline keeps her separate from the floor.

**Tests.**
- `RootmotherMyrrArtSetTests` (EditMode) checks:
  - every row present with its count, in both phases and both facings;
  - the hold is contact B;
  - the walk quarters;
  - the fall's beat and shared drawings;
  - flash rects and pivots;
  - every texture in her folder belongs to the set;
  - no red-orange, no gloamlight, the orchid share, and her idle against the loam (decoded from the source PNGs);
  - the import and the 4.5 MiB budget, at most 1.4 times the Regent's;
  - no themed copy;
  - the Warden and the Regent keep their own sets.
- `EnemyThemeArtTests` lists her among floor 3's own.

**The review** (`ArtDirection/2026-11-25/rootmother-02/`, untracked):
- `1-walk-cycle.png`;
- `2-walk-cycle.gif` (at the phone's scale beside the Regent's and the Warden's walks);
- `3-phone-scale.png` (both phases beside the Glass Regent and the Forge Warden's floor-3 copy, on three loams, and a
  squint);
- `4-art-set.png`;
- `checks.json`, `frames.json`, `rootmother.blend`.

**What the scene wiring still needs** (not built here):
- **The prefab.** Her prefab (a variant of the Forge Warden's, as the Regent's) with `EnemyLookView._paintedArt` = this
  set, and her `EnemyDefinition` on Floor_GloamGardens' gate.
- **BossFeelView.** It knows only the Warden and the Regent: her tell and phase-two glow want orchid, and her death
  wants withering petals. Today she would take the Warden's ember.
- **WardenSlamView.** The creeping red-orange ring with orchid root tips, her orchid feet ring, and the planted roots on
  a caught hero.
- **BossBarkView.** Her three `bark.*` rows (section 1).
- **For the owner's look.** `skirt()`'s "flare" turns the root panels in, not out, despite its docstring. The approved
  telegraph, phase change and strike show this as a darker, narrower skirt. It is kept as approved, and the walking
  telegraph keeps it too.

## 15. The confirmation read (2026-11-25): pass

The one candidate of section 13's answer 2, `mr:share=4.7:sap=0` (share 4.7, no saplings, the creeping snare, the 1.0 s
plant, the 1.2 s telegraph), read once on the fresh block **18001-19200** (docs/56 section 10) against `shipped` - the
Forge Warden at floor 3's gate - with the A/A `aa-f3` beside it: four cohorts, both heroes, typical hands, the code of
main 8921a7b1 (a frozen build). 28,800 four-floor runs in 88 minutes beside the roster's read.

| Cohort | Myrr vs the Warden (points, 90 % CI) | The A/A `aa-f3` | docs/56's bar |
|---|---|---|---|
| clumsy | **+0.62** (+0.33 to +0.96) | -0.25 (-0.75 to +0.25) | passes: an easing wholly above 0, reported |
| careful | +0.17 (-0.08 to +0.42) | -0.12 | passes |
| first-card | +0.38 (-0.17 to +0.92) | -0.38 | passes |
| standing | -0.29 (-0.71 to +0.12) | +0.54 | passes: no better than the Warden (her feet) |

The exploration read (section 9) predicted +0.75 / +0.20 / +0.62 / -0.35 with the saplings; without them and on fresh
seeds the read lands within a quarter point of each. **Myrr ships as floor 3's gate boss at share 4.7 without saplings**;
the scene wiring (section 14's list, section 10's build) is next.

Reported beside it (`myrr-round.md`, the careful pilot unless named):
- **The gate fight**, P10 / P50 / P90: the Vanguard 50.7 / 61.9 / 76.3 s (the Warden 49.3 / 60.1 / 73.9), Ilya 47.8 /
  59.2 / 71.6 s (45.8 / 56.4 / 69.8); in the 45-65 s band at P50 for both; 58 % / 70 % of fights inside the band (the
  Warden's 64 % / 72 %). About 2 s longer at the median, as section 10's share was set to hold.
- **Her beats** a fight 11.3 (the Warden's 10.9); **snares that caught** 0.98 a fight, **planted** 0.98 s a fight; her
  **feet** struck 0.13 times a fight for the careful pilot and **1.83** for the standing one (the design target: the
  standing hero pays at her roots).
- **Deaths at her gate:** careful 27 (the Warden 31), standing 95 (88), first-card 93 (102), clumsy 48 (63) of 2,400 runs.
- The data: `E:/confirm-li17/myrr` (the CSV and `myrr-round.md`), the bootstrap in
  `E:/confirm-li17/rule-myrr/paired-bootstrap.txt` (outside the repository).

## 16. The scene build (2026-11-25)

Section 15's pass, built into the game: Rootmother Myrr is floor 3's gate boss - `Floor_GloamGardens`' Warden's Grove - at
share 4.7, without saplings, with the creeping snare, the 1.0 s plant with her hook free through it and the 1.2 s
telegraph (`mr:share=4.7:sap=0`). The Forge Warden stays floor 1's gate boss; the Glass Regent floor 2's. The scene plays
what the simulation read, and the simulation's default plays her too.

**What was built.**
- **The shared bodies** (`Scripts/Combat/MyrrRule.cs`). The scene now calls the bodies the simulation read with:
  `RootSnare` (its `Creep` through `MyrrRule.Creep`, its `Erupt` through `MyrrRule.InSnare`), `MyrrRule.AtHerFeet`,
  `MyrrRule.HoldsHook` and `MyrrRule.SnareDamage`. Three are new, and the simulation calls them too, so the scene and the
  simulation share one body each, never inlined: `PlantedUntil` and `IsPlanted` (the plant's clock) and `EscapeReach`
  (the pilots' escape from a snare: its radius and the Warden's circle's 0.6). `ShippedSaplings` (0) is the shipped value;
  `Saplings` (2) stays section 3's design, read by the research only.
- **The beat** (`Core/WardenSlamService.cs`). A gate boss carrying a `RootmotherMyrr` (`Scripts/Combat/RootmotherMyrr.cs`,
  a marker on her prefab, as `GlassRegent` is on the Regent's) tunes the clock to `MyrrRule.Every`, `EveryPhaseTwo` and
  `Telegraph`. The service steps her snare as `DescentSimulation.Myrr.cs` does:
  - every running frame, before the clock, the snare creeps after the hero, and the mark (`MarkX`, `MarkY`) is its centre;
  - as the mark goes down, the snare goes down at the hero's spot, and her feet are read from where her pack holds her
    (`HookAtHerFeet`);
  - as the roots erupt, a hero inside is caught. It takes `SnareDamage` with Myrr as the source (the result screen names
    her) and is planted until `PlantedUntil(Time.time, PlantSeconds)`. Her hook is then free through the recovery
    (`HookFree`). The hook's hold is `HoldsHook(clock's hold, HookFree)`.
- **The planted walk** (`UI/HeroMovementInput.cs`, `IsPlanted`). While the service says the hero is planted, nothing
  walks it: not the stick, not the Shield Rush's travel, not a pad. That is the simulation's `Frame` block. Its weapons
  (`AttackController`) and its ability are untouched.
- **The autopilot** (`Core/DevelopmentAutopilot.cs`). It leaves her snare by `EscapeReach(SnareRadius)` from the snare's
  centre, read each frame as it creeps, as the simulation's pilot does.
- **The mark's look** (`UI/WardenSlamView.cs`):
  - **The ring.** A ring of `SnareRadius` on its own transform, laid each frame at the snare's centre. Its edge and fill
    use the ring's rules in the telegraph's red-orange: the edge pulses faster as the roots near, and the fill grows to
    the rim. A crown of 14 orchid root tips (`#D86BD0`) stands on the rim. The tips reach out with the telegraph and
    burst pale (`#F6C2F0`) as the roots erupt. No Warden's ring shows, and no lane.
  - **Her feet.** Her feet ring is the Regent's ring in orchid, over a plum shade. It pulses through a beat whose snare
    went down at her feet. On that beat she raises no windup.
  - **The planted roots.** Orchid tips over a bark ring (`#4A3B33`) show round the caught hero's feet for its planted
    second, and fade over the last 0.2 s.
- **Her body** (`UI/BossFeelView.cs`). The Warden's ember was her default; she now has her own:
  - **The tell.** An orchid glow. She stoops and rears less than the Warden (rise 0.06, lean 5 degrees).
  - **Phase two.** She bursts into bloom: an orchid ring and a pale ring, and 12 petals thrown up from the canopy that
    turn as they fall. Her slow glow then runs in the pale bloom.
  - **Death.** She withers. A plum ring forms, and 18 petals are shed from the canopy in three waves. They drift out
    and tumble down, from bloom to plum. Her remains end in a husk's dust (bark and plum) with a faint plum ring.
    `LastDeathWithered` reports it.
- **Her lines** (`Art/BossBark.cs`, `UI/BossBarkView.cs`, `Strings.txt`). Spawn, telegraph and death are said under
  `bark.rootmother_myrr.*`, with section 1's Turkish and the lead's "Kımıldama. Kök sal.". Her name has a row too:
  `enemy_rootmother_myrr.name`, "Kök Ana Myrr" (for the owner's review; docs/60 section 11). The plate keeps clear of
  her snare at its own radius. It reads the snare's centre each frame as it creeps, and after the eruption it uses where
  the snare erupted.
- **Content**, every asset with a fresh GUID:

  | Asset | What | GUID |
  |---|---|---|
  | `Data/Enemies/Enemy_RootmotherMyrr.asset` | 300 health, 50 gold, 10 experience, speed 0.9 | `0a4d9db3f0bb4e1b949740b56a63b537` |
  | `Data/Weapons/Weapon_GraftingHook.asset` | 5.5 every 1.8 s at 1.7, 1.0 s before the first | `5d3484c578414b75b50cf6fad2a569b5` |
  | `Prefabs/Enemies/RootmotherMyrr.prefab` | A variant of the Forge Warden's, as the Regent's: `EnemyLookView._paintedArt` = `RootmotherMyrrArtSet`, a `RootmotherMyrr` added, the Warden's `EnrageBehaviour` inherited | `567fcf241adc44579163376a86bdbf7d` |
  | `Scripts/Combat/RootmotherMyrr.cs` | The marker | `56a2de25f5dd4c3d9ecf74caf80cc176` |
  | `Resources/Development/Floor_MyrrProof.asset` (`floor_myrr_proof`) | Her alone at the gate | `50b6f3aa2ae14cd9b644a25d62fa6db4` |
  | `Tests/PlayMode/MyrrFlowTests.cs` | Her flow tests | `51c954e8f5694fb682528696c185f539` |

  `Floor_GloamGardens`' gate wave holds her and two Cinder Mites, and its `_foundryGateHealthShare` is 4.7 (it was
  5.102041). `git ls-files -z '*.meta' | xargs -0 grep -h '^guid:' | sort | uniq -d` prints nothing.
- **The room's name stays.** "Warden's Grove" (`room.wardens_grove.name`) is renamed only after the owner's docs/60
  review (the lead's answer 6).

**The simulation's default.** The shipped game's floor 3 gate is Myrr, and so is the simulation's:
- `DescentSimulation.GloamGardens`' Grove holds `RootmotherMyrr`. That definition moved to `DescentSimulation.cs`,
  ahead of the floor, so the static field is set when the floor reads it.
- `FloorThreeGateShare` is 4.7.
- `FoundryLevers.MyrrSaplings` defaults to `ShippedSaplings` (0).
- `GloamMyrr` is gone. `FoundryLevers.GloamWarden` puts the Forge Warden back at her gate at its share then
  (`FloorThreeWardenShare`, 5.102041), for comparisons on the same seeds, as `VaultsWarden` does on floor 2.
- `GloamChain` reshapes floor 3 for `GloamWarden` and for Myrr's share, hook or walk. At the shipped levers it is the
  shipped chain, run for run.
- In `MyrrRoundResearch`, `shipped` is now Myrr as she ships, and `warden-f3` is the Forge Warden at floor 3. `myrr`, the
  `myrr-*` parts and every `mr:` string still start from section 3's Myrr (the Warden's share, two saplings;
  `Docs64Start`), so sections 9-15's strings mean what they meant, and `mr:share=4.7:sap=0` is `shipped` run for run.
- `paired_bootstrap.py` reads Myrr against the Warden as `warden-f3` (flipped: Myrr minus the Warden), and each research
  option against `warden-f3` and against `shipped`. A comparison whose base was not run is skipped, so an older
  `myrr-round.csv`, where `shipped` was the Warden, reads as before.
- The batch report's floor-3 section names her as floor 3's gate boss.

**The parity points** (scene = simulation, and the check that holds each):

| Point | Scene | Simulation | Held by |
|---|---|---|---|
| Her body: 300, hook 5.5 / 1.8 s / 1.7 / 1.0 s, walk 0.9, 50 gold, 10 xp, enrage at half | `Enemy_RootmotherMyrr`, `Weapon_GraftingHook` | `DescentSimulation.RootmotherMyrr` | `TheRootmotherMyrrIsTheSimulations` (`AssertEnemy`), `TheFoundrysThirdFloorIsTheAsset` (`AssertFloor`, wave for wave) |
| Gate share 4.7 | `Floor_GloamGardens._foundryGateHealthShare` through `WardenRule.GateHealthShareOn` | `FloorThreeGateShare`, `LaterFloorLevers` | `TheFoundrysThirdFloorIsTheAsset`, `ClimbFloorContentTests.FloorThreesGateBossTakesTheBossGrowthAndItsOwnShare`, `FoundryFloorThreeTests` |
| Beat: 6 s / 4 s, telegraph 1.2 s, the clock's hold to 1.5 s after | `WardenSlamClock.Tune(MyrrRule...)` | `_slam.Tune(levers.Myrr...)`, `WardenRecoverySeconds` | `TheRootmotherMyrrIsTheSimulations` (the shipped levers are `MyrrRule`'s), `MyrrFlowTests` (the telegraph, phase two's beat) |
| Snare: radius 1.5, creep 1.5 / s | `new RootSnare()` | `new RootSnare(levers...)` at `MyrrRule`'s | the same, and `MyrrFlowTests` |
| The creep before the clock, after the hero's walk; the mark is the snare's centre | `WardenSlamService` at -40, after `HeroMovementInput` (-60) | `StepWarden`'s `MyrrCreep`, after `Frame`'s walk | `TheScenesScriptOrderIsTheSimulations`; `MyrrFlowTests` |
| Her feet at 1.5 as the snare goes down, from her pack position | `MyrrRule.AtHerFeet(mark, TryFloorPositionOf(boss), FeetReach)` | `MyrrRule.AtHerFeet(mark, boss.X/Y, MyrrFeetReach)` | `MyrrFlowTests.HerFeetHoldNoHookAndThePlantedHerosWeaponsStillStrike` |
| The eruption, 10 % with her as the source | `RootSnare.Erupt` (`InSnare`, squares cast to float), `SnareDamage` | the same | `MyrrRuleTests`, `MyrrFlowTests` |
| The plant, 1.0 s; walking resumes the frame after it ends | `PlantedUntil(Time.time, PlantSeconds)`, `IsPlanted` in `HeroMovementInput` (scaled time: a pause or a panel holds it, as the simulation answers a panel inside its frame) | `PlantedUntil(_seconds, MyrrPlantSeconds)`, `IsPlanted` in `Frame` | `MyrrRuleTests.SheShipsWithoutSaplingsAndHerPlantEndsOnTime`, `MyrrFlowTests` (60 frames at 1/60 s) |
| The hook: the clock's hold, free at her feet or after a catch | `MyrrRule.HoldsHook(_clock.HoldsWeapon, HookFree)` | `MyrrRule.HoldsHook(HoldsWeaponFor(1.5), _myrrHookFree)`, `MyrrPlantedFreesHook` | `TheRootmotherMyrrIsTheSimulations`, `MyrrFlowTests` |
| The pilots' escape from the snare | `MyrrRule.EscapeReach(MyrrRule.SnareRadius)` | `MyrrRule.EscapeReach(levers.MyrrSnareRadius)` | `TheRootmotherMyrrIsTheSimulations`, `MyrrFlowTests.TheAutopilotWalksOutOfHerSnares` |
| The skills' "blow coming" | the Warden's 2.2 round the mark (unchanged) | the Warden's 2.2 round the mark (unchanged) | the same body each side; not hers yet (see what is left) |
| No saplings | no sapling step | `MyrrSaplings` = `ShippedSaplings` = 0 | `TheRootmotherMyrrIsTheSimulations`, `FoundryMyrrTests.HerFightPlaysHerRules`, `MyrrFlowTests` (no enemy grows at phase two) |

**The default batch** (`FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_REALISM=typical FOUNDRY_PARALLEL=6`,
filter `FullyQualifiedName~FoundryBatchReport.TheFoundryFloorOverASeedBatch`, `FOUNDRY_EDITOR` an empty folder).
"Before" is `d49ed8a1` (lead-integrate-18, the Forge Warden at floor 3) and "after" is this branch. Each was built in
its own folder of its own sources and floor assets.

- **`FOUNDRY_FLOORS=2` stops after floor 2** (`FoundryFloorThreeTests.TheFloorsAskedForStopWhereTheySay`), so it never
  meets her. It is byte-identical: every CSV, `foundry-gate.md` and `foundry-cohorts.md` match byte for byte. In
  `foundry-batch.md` only the header's clock and the wall-seconds column differ.
- **`FOUNDRY_FLOORS=4`** plays floor 3 and the finale, so floor 3 changes.
  - Unchanged: `foundry-batch.csv`, `foundry-batch-floor2.csv` and `foundry-cohorts-floor2.csv` match byte for byte.
    The `foundry-gate.md` file is unchanged too.
  - Changed: floor 3's rows, the finale's and the Descent's.

All three floors cleared, of 200 a hero (Vanguard + Ilya), the Warden before and Myrr after:

| Cohort | Warden (before) | Myrr (after) | Change |
|---|---|---|---|
| careful | 190 + 194 = 384 | 190 + 193 = 383 | -1 |
| standing | 164 + 159 = 323 | 165 + 157 = 322 | -1 |
| first-card | 172 + 184 = 356 | 174 + 184 = 358 | +2 |
| clumsy | 61 + 99 = 160 | 61 + 101 = 162 | +2 |
| auto-farmer | 5 + 6 = 11 | 5 + 7 = 12 | +1 |

The Warden's column is section 9's `shipped` row (seeds 1-200), and the first four changes sit inside section 15's
confirmed CIs.

Floor 3's gate fight, P50 / P90 in seconds, Vanguard; Ilya:

| Cohort | Warden (before) | Myrr (after) |
|---|---|---|
| careful | 62 / 75; 57 / 70 | 64 / 78; 59 / 72 |
| standing | 48 / 59; 46 / 56 | 51 / 63; 50 / 60 |
| first-card | 56 / 67; 54 / 61 | 58 / 69; 56 / 65 |
| clumsy | 60 / 73; 62 / 74 | 60 / 69; 63 / 76 |
| auto-farmer | 37 / 40; 35 / 42 | 37 / 39; 35 / 41 |

- The careful P50 stays inside 45-65 s, about 2 s longer, as section 15 read.
- Deaths at floor 3's gate fall for the standing cohort (Vanguard 8 to 7; Ilya 4 to 6) and the first-card Vanguard
  (7 to 5). The killing blows now read "Rootmother Myrr", and "Warden slam" stays the report's word for her snare's
  eruption.
- The Descent's P50 moves by 0.1 minutes at most (careful 14.5 / 14.3 either side).

**Tests.**
- New:
  - `MyrrFlowTests` (PlayMode, `floor_myrr_proof`):
    - the snare creeps after a hero that stepped out and stopped, and catches it;
    - it catches a hero that stood, without moving;
    - a hero that keeps walking leaves it;
    - the catch takes 10 % and plants the hero for 60 frames, through which the stick walks it nowhere and her hook stays
      free;
    - the view draws the moving ring where the snare is, with no Warden's ring and no lane;
    - her feet: no hook held and no windup, the orchid ring pulsing, and the planted hero's own weapons still strike;
    - phase two at half health: her next snare on the 4 s beat, and no enemy grows;
    - she withers as she falls;
    - the careful autopilot walks out of three snares (at most one catch).
  - `FoundrySimulationSceneValueTests.TheRootmotherMyrrIsTheSimulations`.
  - `MyrrRuleTests.SheShipsWithoutSaplingsAndHerPlantEndsOnTime`.
  - `BossBarkRuleTests.APlateKeepsClearOfMyrrsSnareWhereverItCreeps`.
  - `BossBarkFlowTests.MyrrSaysHerOwnLinesAndHerPlateKeepsOffHerCreepingSnare`.
  - `BossFeelTests.MyrrTellsInOrchidBloomsAtPhaseTwoAndWithersAsSheFalls`.
  - `FoundryMyrrTests.HerSaplingsForTheResearchAreCutOrGrow`.
- Changed (they assumed the Warden at floor 3, or six bark lines):
  - `FoundrySimulationSceneValueTests.TheFoundrysThirdFloorIsTheAsset`.
  - `ClimbFloorContentTests.FloorThreesGateBossTakesTheBossGrowthAndItsOwnShare`.
  - `FoundryFloorThreeTests`:
    - `FloorThreeStandsBetweenFloorTwoAndTheFinale`;
    - `FloorThreesWardenTakesTheBossGrowthAndItsOwnShare`, renamed `FloorThreesGateBossTakesTheBossGrowthAndItsOwnShare`;
    - `AFloorTwoClearGoesDownToFloorThreeAndAFloorThreeClearToTheFinale`.
  - `FoundryMyrrTests`:
    - `MyrrTakesTheWardensPlaceAtFloorThreesGateAlone`, now `MyrrHoldsFloorThreesGateAndTheWardenComesBackAlone` (it
      also holds the confirmed `mr:share=4.7:sap=0`, through `GloamChain`, to the shipped run, run for run);
    - `HerFightPlaysHerRules`;
    - `HerFightRepeats`.
  - `BossBarkRuleTests.EachGateBossSaysDocs44sLines` (nine lines).
  - `BossBarkFlowTests`' plate check (her radius).
  - `ForgeWardenArtSetTests.OnlyTheForgeWardenWearsTheSet` (her prefab overrides the Warden's set).
  - `RootmotherMyrrArtSetTests.TheWardenAndTheRegentKeepTheirOwnSets` (her prefab wears her set).
  - `PerfCensus` counts her snare and roots as the gate boss's mark.
- `Tools/ArtReview/GameFeelCapture.cs` gains a `myrr` part: her feet ring, the snare creeping after a hero that stepped
  out and stopped, its roots erupting and the planted roots, her tell, phase two and her fall.

  ```
  pwsh -File Tools/ArtReview/Run-GameFeelProof.ps1 -Only myrr
  ```

**What is left.**
- **Not run in Unity.** No Unity in a worktree. The compile check is green, CombatChecks is green, and the EditMode and
  PlayMode suites above are the lead's gate.
- **The owner's look on the S23:**
  - whether the moving ring reads as homing (the lead's answer 3; the still ring, `MyrrCreepSpeed` 0, is the fallback);
  - the orchid root tips' size on the ring;
  - the planted roots;
  - the withering fall against her painted sink.
- **The HUD's stick does not grey while planted** (section 5): the game draws no stick, so there is nothing to grey.
  The planted roots round the hero's feet are the read.
- **The skills' "blow coming" reads the Warden's 2.2 circle round her snare's centre**, in the scene and the simulation
  alike, as the simulation read her. A skill that waits for a blow will judge her smaller ring as the Warden's until
  both sides change together.
- **The saplings stay research only.** No `GloamService` step was built.
- **The room's rename** waits for the owner's docs/60 review.
- **`PerfProbe`'s parts** do not yet include her proof floor.
- **Older floor-3 research plays her now.** `ThirdFloorResearch`, `GloamEnemiesResearch` and every harness that plays
  the shipped floor 3 now meet Myrr at its gate. Their earlier reads (the Warden's shares in `ThirdFloorResearch`, the
  Bloom, Wraith and Thornling rounds) were made with the Warden there; `GloamWarden` puts it back on a chain built by
  `GloamChain`, and a harness that builds its own floor 3 needs the same switch before it is read again.
