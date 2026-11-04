# Difficulty research, 2026-11-04: a human's hands, why Ilya is safe, structural options

Branch `claude/research-difficulty-1104` (from `research/pilot-cohorts`). Cloud research for the lead; every gameplay
change below is the owner's call. All numbers are the foundry simulation (`DescentSimulation.RunFoundry`), floor 1,
seeds 1-200 a hero, the same seeds in every cell (paired). Format everywhere: **Vanguard / Ilya** clears of 200.
Raw tables (every cell's floor, Warden, slam/hazard hits, lapses, deaths): `docs/cloud/2026-11-04-difficulty-tables/`.

**How to reproduce.** `dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~FoundryDifficultyResearch"`
with `DIFFICULTY_PARTS` = `reaction,threats,parts,fine,why,staff,structural,structural2` (about 75 min on 4 cores for all).
The batch report also takes `FOUNDRY_REALISM=typical|slow|<seconds>` (cohorts with a human's hands) and
`FOUNDRY_LEVERS=4` (the structural candidates). With neither, the default batch is byte-for-byte what it was.

**Noise floor.** The floor is chaotic: a step misplaced by 0.05 units (no skill change at all) moves a cell by up to
10 runs (clumsy Vanguard 74 -> 64, standing Ilya 156 -> 166). Read differences under ~10 of 200 as noise.

## Summary

1. **Reaction time matters for one threat on the shipped floor: enemies closing in** (the kite). The Warden's mark
   only matters past ~0.45 s, where it is a cliff (the slam's 1.1 s telegraph less the 0.63 s escape). The pit's glow
   does not matter at all. Careful Vanguard drops from 199 to 162-171 at any reaction of 0.15-0.40 s (81-86 %);
   careful Ilya stays at 191-200 until 0.45 s, and both collapse at 0.5 s (94 / 119).
2. **No pilot setting puts both careful heroes in 80-90 %, because the gap is the hero, not the pilot.** The
   recommended "typical human" (reaction 0.25 s, aim 0.5 units, a 0.4 s lapse 4 times a minute, 0.5 s to read the
   floor after a card) gives careful 149 / 185 (75 % / 93 %), standing 100 / 119 (50 / 60 %), clumsy 32 / 94
   (16 / 47 %), auto-farmer 6 / 16. Against the lead's illustrative bands this lands standing, and puts careful and
   clumsy across the bands with the Vanguard below and Ilya above.
3. **Ilya is safe because of the Staff's splash** (radius 3.5 at 75 % of the hit). The Vanguard with the Staff plays
   like Ilya (200 / 185 careful frame-perfect / typical); Ilya with the Sword plays like the Vanguard (194 / 140).
   Reach (2.4 vs 2.1) changes nothing; Ember Memory adds at most ~15; Crucible Vent is weaker than Forge Burst. With
   a 2.0 splash, careful Ilya under the typical human clears 86 of 200.
4. **Structural options:** slag lobbed at the hero is the one that separates skill without the frame-perfect pilot
   noticing it. A Warden echo mark separates hardest but is a cliff: at 0.8 s the bot never sees it and every human
   run dies, and at 1.1 s it still halves the careful humans. The elite stomp and ambush surges are within the noise.
5. **Recommendation:** measure difficulty with `FOUNDRY_REALISM=typical` from now on (frame-perfect stays the
   regression and Editor-parity pilot); build **slag (every 6-8 s)** and a **gentler, enraged-only Warden echo**
   (needs one more tuning round); and put the **Staff's splash** to the owner as the hero-balance lever, since no
   floor value reaches careful Ilya while the Staff clears the horde before it arrives.

## 1. Human realism

### The model (`PilotRealism`, Tests/Support/FoundryRealism.cs; simulation only)

| Part | What it does | Parameter |
|---|---|---|
| Reaction, by threat | A threat is answered only once it has been visible this long: the Warden's mark (and every other mark), the pit's glow (from the warning's start), an enemy entering the cohort's kite reach (each enemy noticed on its own; one that stepped back out stays noticed for 1 s) | `MarkReaction`, `GlowReaction`, `FoeReaction` |
| Aim / positioning error | Each answer (an escape from a mark, a shelter from the glow, a kite stride, redrawn every 0.5 s) aims at its point plus an offset uniform in a disc | `AimError` (units) |
| Lapses | Poisson lapses: the stick is held where it was, nothing new is noticed; a threat that appears during one is answered a reaction after it ends | `LapsesPerMinute`, `LapseSeconds` |
| Card re-read | After every upgrade or forge panel closes, the floor is re-read with the stick released | `CardLapseSeconds` |

The draws come from `RunRandom` on a purpose of the pilot's own (4101), so a realistic pilot plays the same floor
(offers, chests, crits, horde) as the frame-perfect one; a realism of zeros reproduces it exactly
(`FoundryRealismTests`). Presets: `PilotRealism.Typical()` (0.25 s, 0.5, 4/min x 0.4 s, 0.5 s) and
`PilotRealism.Slow()` (0.4 s, 0.8, 8/min x 0.5 s, 0.8 s).

### Reaction on every threat (nothing else)

| Reaction | careful | standing | first-card | clumsy | auto-farmer |
|---|---|---|---|---|---|
| 0 (frame-perfect) | 199 / 200 | 130 / 156 | 196 / 200 | 74 / 160 | 6 / 19 |
| 0.15 s | 171 / 200 | 133 / 169 | 186 / 200 | 47 / 128 | 6 / 19 |
| 0.25 s | 165 / 200 | 137 / 171 | 187 / 200 | 54 / 139 | 6 / 19 |
| 0.30 s | 166 / 200 | 135 / 163 | 189 / 200 | 59 / 152 | 6 / 19 |
| 0.35 s | 166 / 199 | 125 / 144 | 188 / 200 | 50 / 133 | 6 / 19 |
| 0.40 s | 162 / 195 | 117 / 143 | 184 / 200 | 58 / 147 | 6 / 19 |
| 0.45 s | 154 / 191 | 114 / 142 | 182 / 200 | 42 / 122 | 6 / 19 |
| 0.50 s | 94 / 119 | 11 / 15 | 173 / 192 | 28 / 63 | 6 / 19 |

Slam hits per Warden fight for careful: 0.00 up to 0.30 s, 0.06 at 0.30, 0.14-0.17 at 0.35, 0.25-0.33 at 0.40,
2.1-2.2 at 0.50. The auto-farmer has no habit a reaction can delay.

### Which threats: one at a time (the others read at once)

| Delayed threat | careful | standing | first-card | clumsy |
|---|---|---|---|---|
| none | 199 / 200 | 130 / 156 | 196 / 200 | 74 / 160 |
| the Warden's mark, 0.35 s | 199 / 200 | 129 / 155 | 196 / 200 | 78 / 172 |
| the Warden's mark, 0.50 s | 186 / 196 | **8 / 11** | 196 / 199 | 36 / 88 |
| the pit's glow, 0.35 s | 195 / 200 | 126 / 158 | 194 / 200 | 68 / 162 |
| the pit's glow, 0.50 s | 197 / 200 | 131 / 150 | 196 / 200 | 81 / 155 |
| enemies closing, 0.35 s | **169 / 197** | 130 / 156 | 187 / 200 | **45 / 129** |
| enemies closing, 0.50 s | **159 / 195** | 130 / 156 | 185 / 200 | 50 / 136 |

- **Enemies closing** is what a reaction costs on the shipped floor: the kite keeps the hero only 0.15 past each
  enemy's reach, so a late kite is a struck kite. It costs the Vanguard 30-40 clears (deaths in rooms 2-3 to the
  horde's Grunts and the wave's Tanks) and Ilya almost nothing (the Staff's splash has cleared the crowd).
- **The Warden's mark** is a cliff, not a slope: the hero walks 3.5 a second and the mark is 2.2 wide, so an
  escape from the mark's centre needs 0.63 s of a 1.1 s telegraph. Up to ~0.45 s nothing; at 0.5 s the standing
  cohort, which stands on the mark's centre, dies at the Warden in 189 of 200; the careful cohort, already moving
  when the mark goes down, mostly still escapes. The 0.6 s telegraph cliff the values sweep found is the same
  arithmetic: telegraph less reaction less 0.63 s.
- **The pit's glow** does not matter: a 1.5 s warning is long, and the careful pilot keeps out of the band anyway.

### Aim and attention

| Configuration | careful | standing | first-card | clumsy | auto-farmer |
|---|---|---|---|---|---|
| frame-perfect | 199 / 200 | 130 / 156 | 196 / 200 | 74 / 160 | 6 / 19 |
| aim error 0.5 | 195 / 200 | 133 / 162 | 195 / 200 | 63 / 159 | 6 / 19 |
| aim error 1.0 | 195 / 200 | 126 / 166 | 195 / 200 | 71 / 162 | 6 / 19 |
| lapses 4/min x 0.4 s | 198 / 200 | 127 / 160 | 195 / 200 | 72 / 166 | 5 / 25 |
| lapses 10/min x 0.6 s | 193 / 200 | 124 / 151 | 193 / 200 | 66 / 157 | 5 / 26 |
| card re-read 0.5 s | 194 / 200 | 112 / 149 | 195 / 200 | 61 / 158 | 6 / 18 |
| card re-read 1.0 s | 191 / 200 | 111 / 136 | 195 / 200 | 67 / 150 | 5 / 23 |
| reaction 0.25 s alone | 165 / 200 | 137 / 171 | 187 / 200 | 54 / 139 | 6 / 19 |
| typical human (all four) | 149 / 185 | 100 / 119 | 182 / 198 | 32 / 94 | 6 / 16 |

Alone, aim and attention are within or near the noise; the card re-read costs the standing cohort ~20 (it already
stands in the crowd). Together with the reaction they compound: the typical human takes ~1 slam a Warden fight
(a lapse or a short escape on top of a 0.25 s reaction), which the reaction alone never does.

### The reaction with the typical human's aim and attention

| Reaction (+ aim 0.5, 4/min x 0.4 s, card 0.5 s) | careful | standing | first-card | clumsy | auto-farmer |
|---|---|---|---|---|---|
| 0.15 s | 165 / 197 | 105 / 131 | 189 / 200 | 41 / 110 | 6 / 16 |
| 0.20 s | 159 / 196 | 105 / 121 | 183 / 200 | 44 / 107 | 6 / 16 |
| 0.25 s (typical) | 149 / 185 | 100 / 119 | 182 / 198 | 32 / 94 | 6 / 16 |
| 0.30 s | 138 / 181 | 90 / 117 | 176 / 198 | 37 / 96 | 6 / 16 |
| 0.35 s | 122 / 168 | 80 / 104 | 170 / 194 | 34 / 81 | 6 / 16 |
| 0.40 s (slow human: aim 0.8, 8/min x 0.5 s, card 0.8 s) | 80 / 129 | 50 / 85 | 124 / 176 | 19 / 55 | 5 / 24 |

**What puts careful near 80-90 %?** For the Vanguard, any reaction of 0.15-0.40 s alone (81-86 %), or 0.15-0.20 s with
the typical aim and attention (80-83 %). For Ilya, nothing short of the slow human (65 %) or a 0.45 s reaction
(96 %) moves it below 90 %; with the typical aim and attention it takes 0.30-0.35 s (84-91 %), where the Vanguard is
at 61-69 %. The two heroes cannot be put in one band by the pilot: see section 2.

### The floor in human hands (frame-perfect, typical, slow)

| Pilot | Cohort | Cleared V / I | Floor P50 V / I (clears) | Warden P50 V / I | Slam hits a fight V / I | Deaths, Vanguard: where / blow | Deaths, Ilya |
|---|---|---|---|---|---|---|---|
| frame-perfect | careful | 199 / 200 | 299 (299) / 290 (290) | 64 / 55 | 0.00 / 0.01 | 1: room 3 / horde Runner | - |
| frame-perfect | standing | 130 / 156 | 276 (279) / 274 (275) | 47 / 43 | 0.00 / 0.00 | 70: boss 26, room 2 23, room 3 19 / Warden 21, horde Grunt 21, wave Tank 12 | 44: boss 44 / Warden 35, horde Grunt 7 |
| frame-perfect | first-card | 196 / 200 | 295 (296) / 286 (286) | 62 / 53 | 0.00 / 0.00 | 4: room 2 3, room 3 1 | - |
| frame-perfect | clumsy | 74 / 160 | 102 (281) / 277 (278) | 53 / 51 | 0.00 / 0.00 | 126: room 3 72, room 2 29, boss 20 / pit 35, horde Grunt 33, wave Tank 30 | 40: boss 37 / pit 26, Warden 12 |
| frame-perfect | auto-farmer | 6 / 19 | 231 (259) / 239 (256) | 33 / 30 | 2.97 / 3.40 | 194: boss 113, room 3 43, room 2 36 / slam 80, wave Tank 34 | 181: boss 178 / slam 120, Warden 47 |
| typical | careful | 149 / 185 | 287 (290) / 284 (285) | 57 / 51 | 1.06 / 0.89 | 51: room 3 18, boss 17, room 2 12, elite 4 / horde Grunt 15, slam 14, horde Runner 5 | 15: boss 14 / slam 8, Warden 2 |
| typical | standing | 100 / 119 | 266 (277) / 269 (273) | 44 / 40 | 0.77 / 0.72 | 100: boss 54, room 2 28, room 3 15 / Warden 27, slam 18, wave Tank 16 | 81: boss 81 / Warden 55, slam 12 |
| typical | first-card | 182 / 198 | 289 (289) / 281 (281) | 57 / 49 | 0.95 / 0.83 | 18: room 3 8, room 2 7, boss 3 / horde Grunt 7, wave Tank 7 | 2: boss 2 |
| typical | clumsy | 32 / 94 | 77 (285) / 270 (281) | 53 / 50 | 0.70 / 0.65 | 168: room 2 85, room 3 52, boss 29 / pit 49, wave Tank 48, horde Grunt 33 | 106: boss 85, room 3 21 / pit 36, slam 23, Warden 20 |
| typical | auto-farmer | 6 / 16 | 228 (259) / 240 (258) | 35 / 31 | 3.26 / 3.42 | 194: boss 101, room 3 49, room 2 42 / slam 72 | 184: boss 180 / slam 127 |
| slow | careful | 80 / 129 | 266 (284) / 276 (280) | 51 / 47 | 1.70 / 1.41 | 120: boss 75, room 3 23, room 2 22 / slam 41, Warden 21, horde Grunt 20 | 71: boss 71 / slam 31, Warden 22 |
| slow | standing | 50 / 85 | 254 (275) / 259 (271) | 42 / 38 | 1.37 / 1.23 | 150: boss 98, room 2 32, room 3 20 / Warden 43, slam 40 | 115: boss 113 / Warden 59, slam 42 |
| slow | first-card | 124 / 176 | 277 (282) / 276 (277) | 50 / 45 | 1.66 / 1.53 | 76: boss 53, room 2 13, room 3 10 / slam 26, Warden 18 | 24: boss 23 / slam 15 |
| slow | clumsy | 19 / 55 | 77 (284) / 258 (279) | 53 / 48 | 1.02 / 1.01 | 181: room 2 88, boss 47, room 3 45 / pit 53, wave Tank 44 | 145: boss 117, room 3 26 / pit 51, Warden 39 |

The Warden fight gets shorter in human hands (careful 64/55 -> 57/51 s): the human stands closer and longer, so it
deals more but takes more. A human-like cleared floor is ~285-290 s, about what the frame-perfect pilot reads.

**An observation not pursued:** the first-card cohort beats the careful one under every human setting (182 / 198 vs
149 / 185 typical; 124 / 176 vs 80 / 129 slow), though frame-perfect they tie. The careful picker maxes all three
weapons (8/8/8 on 97 % of runs) and takes two tomes; the first-card picker ends with three tomes (Vitality often)
and stat cards. A build with health in it forgives mistakes the frame-perfect pilot never makes; the careful
pilot's CardScore is tuned for a pilot that is never hit.

## 2. Why Ilya is safe

### Where health is lost (mean a run, every run; near-death = lowest health under 25 % and survived)

| Pilot | Cohort | Hero | Cleared | Health lost a run; largest place / blow | Near-deaths: where / blow | Lowest health P10 / P50 |
|---|---|---|---|---|---|---|
| frame-perfect | careful | Vanguard | 199 | 50; boss / horde Grunt 7, room 2 / wave Tank 5, room 3 / wave Tank 5 | 7: room 3 / horde Mite 3, Grunt 2, Runner 2 | 41 / 77 % |
| frame-perfect | careful | Ilya | 200 | 28; boss / horde Grunt 5, room 3 / wave Tank 5, room 2 / wave Tank 4 | 0 | 71 / 83 % |
| frame-perfect | standing | Vanguard | 130 | 149; boss / Warden 46, room 2 / wave Tank 16, room 1 / wave Grunt 14 | 53: boss 44 / Warden 27 | 0 / 15 % |
| frame-perfect | standing | Ilya | 156 | 134; boss / Warden 51, room 2 / wave Tank 14, room 3 / wave Tank 13 | 63: boss 60 / Warden 37 | 0 / 21 % |
| frame-perfect | clumsy | Vanguard | 74 | 167; boss / pit 19, room 2 / wave Tank 18, boss / Warden 14 | 35: room 3 17, boss 15 / pit 10 | 0 / 0 % |
| frame-perfect | clumsy | Ilya | 160 | 173; boss / pit 36, boss / Warden 28, room 2 / wave Tank 15 | 71: boss 36, room 3 28 / pit 19 | 0 / 23 % |
| typical | careful | Vanguard | 149 | 142; boss / slam 23, room 2 / wave Tank 13, room 1 / wave Grunt 11 | 45: boss 33, room 3 10 / horde Grunt 11, Mite 10, slam 7 | 0 / 26 % |
| typical | careful | Ilya | 185 | 114; boss / slam 21, room 2 / wave Tank 11, room 3 / wave Tank 11 | 39: boss 37 / horde Grunt 14, slam 9 | 7 / 37 % |

Frame-perfect careful Ilya loses 28 health a run and never drops below 25 %: nothing reaches it. In human hands
both heroes take about the same Warden damage (slams 21-23 a run); the Vanguard's extra losses and deaths are in the
halls (rooms 2-3: the horde's Grunts and Runners, the wave's Tanks), where the crowd reaches it.

### The heroes swapped piece by piece (clears, frame-perfect / typical human)

| Hero | Careful | Standing | Clumsy | Health lost a run, careful |
|---|---|---|---|---|
| Vanguard | 199 / 149 | 130 / 100 | 74 / 32 | 50 / 142 |
| Vanguard + Staff | 200 / 185 | 163 / 105 | 158 / 91 | 27 / 119 |
| Vanguard + Ember Memory | 198 / 152 | 147 / 104 | 76 / 45 | 45 / 132 |
| Vanguard + Crucible Vent | 183 / 125 | 117 / 57 | 54 / 18 | 66 / 157 |
| Ilya | 200 / 185 | 156 / 119 | 160 / 94 | 28 / 114 |
| Ilya + Sword | 194 / 140 | 118 / 78 | 60 / 35 | 53 / 135 |
| Ilya without Ember Memory | 200 / 178 | 141 / 91 | 151 / 64 | 32 / 125 |
| Ilya + Forge Burst | 200 / 193 | 162 / 121 | 177 / 105 | 25 / 109 |
| Ilya + Sword, no Ember Memory | 181 / 119 | 99 / 54 | 50 / 14 | 64 / 154 |

### The Staff taken apart (Ilya)

| Staff | Careful | Standing | Clumsy | Health lost a run, careful |
|---|---|---|---|---|
| shipped: reach 2.4, splash 3.5 at 75 % | 200 / 185 | 156 / 119 | 160 / 94 | 28 / 114 |
| reach 2.1 (the Sword's) | 200 / 180 | 165 / 119 | 152 / 98 | 31 / 121 |
| splash radius 2.0 (the Sword's cleave) | 200 / 86 | 91 / 34 | 75 / 2 | 66 / 220 |
| splash share 0.4 | 200 / 156 | 147 / 91 | 118 / 21 | 45 / 169 |
| no splash (single target) | 75 / 1 | 0 / 0 | 0 / 0 | 168 / 152 |
| reach 2.1 and splash 2.0 | 200 / 81 | 74 / 28 | 68 / 2 | 68 / 214 |

**It is the splash.** A Staff hit lands 75 % on everything within 3.5 of its target, which stands up to 2.4 away:
Ilya's blows cover a disc ~6 units across round itself, and the horde dies before it is within reach. Reach is not
it (2.1 changes nothing), the burn is a minor part (Ember Memory: +0-15 clears, at most), the ability is negative
(Crucible Vent is the weaker ability on either hero), and 90 health against 100 does not show. This is why the
values-only sweep could not move careful Ilya: horde damage, node damage and the pit only matter to a hero the
horde reaches. The splash is also why the Staff leads both of the V0.1 gate's weapon-role readings (docs/41,
2026-11-04: the Staff ahead on the horde and the Warden).

A splash share of 0.4 under the typical human gives careful 156 (78 %), standing 91 (46 %), clumsy 21 (11 %) for
Ilya: the lead's bands for careful and standing in one change. It is a values change to a weapon (and to the
Vanguard's Staff when it takes one), so it is the owner's call; it also touches the weapon-role criterion.

## 3. Structural options

Each is behind `FoundryLevers` (simulation only, off by default; `FoundryBatch.DifficultyLevers`, `FOUNDRY_LEVERS=4`).
Every mark is read by a cohort that escapes marks as it reads the Warden's (the careful, standing, first-card and
clumsy cohorts; the auto-farmer does not), with the realistic pilot's reaction and aim.

| Option | What it is |
|---|---|
| Warden echo | After each blow, a second mark where the hero stands then, with the slam's radius, landing a telegraph later (0.8 or 1.1 s; 10 or 20 % of maximum health) |
| Slag | Every 5 or 8 s while a wave or the horde stands outside the Warden's fight, a 1.6-wide mark on the hero, 8 % a second later: range does not help and standing does not either |
| Elite stomp | The elite room's Captain, when the hero is within 2.4, marks 2.6 round itself every 3 s (first after 1 s), 15 % after 0.9 s: trading at its reach is punished, kiting wide is not |
| Ambush surges | A combat hall's surge waves enter 4 units ahead of the hero's walk instead of the corners |

### Clears (Vanguard / Ilya)

| Option | Pilot | careful | standing | first-card | clumsy | auto-farmer |
|---|---|---|---|---|---|---|
| shipped | frame-perfect | 199 / 200 | 130 / 156 | 196 / 200 | 74 / 160 | 6 / 19 |
| shipped | typical | 149 / 185 | 100 / 119 | 182 / 198 | 32 / 94 | 6 / 16 |
| shipped | slow | 80 / 129 | 50 / 85 | 124 / 176 | 19 / 55 | 5 / 24 |
| echo 0.8 s, 20 % | frame-perfect | 199 / 200 | 137 / 171 | 196 / 200 | 78 / 167 | 0 / 1 |
| echo 0.8 s, 20 % | typical | **0 / 0** | 0 / 0 | 9 / 19 | 3 / 23 | 0 / 0 |
| echo 0.8 s, 20 % | slow | 0 / 0 | 0 / 0 | 5 / 12 | 1 / 3 | 1 / 3 |
| echo 1.1 s, 20 % | frame-perfect | 198 / 199 | 139 / 173 | 195 / 200 | 76 / 164 | 0 / 1 |
| echo 1.1 s, 20 % | typical | 34 / 71 | 39 / 61 | 125 / 163 | 23 / 67 | 0 / 0 |
| echo 1.1 s, 10 % | frame-perfect | 198 / 199 | 139 / 173 | 195 / 200 | 76 / 164 | 1 / 5 |
| echo 1.1 s, 10 % | typical | 69 / 107 | 66 / 93 | 149 / 185 | 26 / 81 | 0 / 6 |
| echo 1.1 s, 10 % | slow | 11 / 24 | 11 / 24 | 72 / 114 | 14 / 47 | 3 / 8 |
| slag every 5 s | frame-perfect | 186 / 200 | 138 / 173 | 193 / 200 | 77 / 163 | 0 / 1 |
| slag every 5 s | typical | 138 / 184 | 74 / 123 | 162 / 193 | 14 / 73 | 0 / 1 |
| slag every 5 s | slow | 33 / 72 | 20 / 45 | 78 / 110 | 9 / 15 | 0 / 2 |
| slag every 8 s | frame-perfect | 198 / 200 | 136 / 167 | 195 / 200 | 79 / 166 | 1 / 4 |
| slag every 8 s | typical | 141 / 186 | 79 / 124 | 173 / 199 | 20 / 79 | 0 / 6 |
| slag every 8 s | slow | 58 / 100 | 33 / 65 | 103 / 133 | 11 / 31 | 1 / 5 |
| slag 8 s + echo 1.1 s, 10 % | typical | 59 / 85 | 51 / 87 | 138 / 175 | 21 / 70 | 0 / 1 |
| elite stomp | frame-perfect | 199 / 200 | 133 / 167 | 196 / 200 | 73 / 158 | 2 / 8 |
| elite stomp | typical | 146 / 185 | 89 / 125 | 181 / 200 | 36 / 94 | 1 / 7 |
| elite stomp | slow | 74 / 118 | 52 / 76 | 121 / 164 | 14 / 61 | 3 / 15 |
| ambush surges | frame-perfect | 199 / 200 | 128 / 160 | 197 / 200 | 65 / 168 | 7 / 21 |
| ambush surges | typical | 144 / 189 | 96 / 136 | 180 / 198 | 36 / 105 | 7 / 18 |
| ambush surges | slow | 73 / 114 | 54 / 79 | 139 / 168 | 17 / 57 | 4 / 25 |

### Floor, Warden and deaths, careful cohort

| Option | Pilot | Floor P50 V / I (clears) | Warden P50 V / I | Hazard hits a run V / I | Deaths, Vanguard: where / blow | Deaths, Ilya |
|---|---|---|---|---|---|---|
| shipped | typical | 287 (290) / 284 (285) | 57 / 51 | - | 51: room 3 18, boss 17, room 2 12 / horde Grunt 15, slam 14 | 15: boss 14 / slam 8 |
| echo 1.1 s, 10 % | frame-perfect | 319 / 307 | 87 / 74 | 0 / 0 | 2 | 1 |
| echo 1.1 s, 10 % | typical | 283 (306) / 285 (297) | 73 / 64 | 1.5 / 1.6 | 131: boss 97, room 3 18 / slam 29, Warden 27, echo | 93: boss 92 / Warden 41, echo 14, slam 12 |
| echo 0.8 s, 20 % | typical | 246 / 247 | - | 3.0 / 3.6 | 200: boss 166 / echo 126 | 200: boss 199 / echo 152 |
| slag every 5 s | typical | 286 (291) / 283 (284) | 58 / 51 | 2.3 / 2.5 | 62: room 3 22, boss 19, room 2 19 / horde Grunt 20, slam 10, slag 5 | 16: boss 16 / slam 8 |
| slag every 8 s | typical | 287 (291) / 283 (283) | 57 / 50 | 1.4 / 1.6 | 59: room 2 22, room 3 21, boss 13 / horde Grunt 23, wave Tank 10 | 14: boss 14 / slam 9 |
| slag every 8 s | slow | 259 (285) / 271 (281) | 54 / 47 | 2.9 / 3.7 | 142: boss 79, room 2 36, room 3 26 / slam 41, horde Grunt 22, pit 21 | 100: boss 95 / slam 60 |
| elite stomp | typical | 288 (291) / 283 (284) | 58 / 51 | 0.2 / 0.2 | 54 | 15 |
| ambush surges | typical | 287 (290) / 283 (283) | 57 / 50 | - | 56: room 3 26, room 2 14, boss 13 / horde Grunt 21 | 11: boss 11 |

### What each does

- **Warden echo: separates hardest, and shows why a frame-perfect pilot cannot be the difficulty measure.** At
  0.8 s the bot clears 199 / 200 and never notices the echo; the typical human dies at the Warden on every seed
  (0.25 s + 0.63 s escape > 0.8 s). At the slam's own 1.1 s it is readable, yet it still costs careful humans 80 / 78
  clears at 10 % of health: the second escape keeps the hero away (the fight runs 57 -> 73 s, 87 s frame-perfect),
  and the lapses and short escapes now land twice a cycle. As built it is too strong; a candidate for one more round:
  the echo only while enraged, at 1.1 s and 5-10 %, or a smaller echo radius (1.4).
- **Slag: the best separator per unit of harshness.** Invisible to the frame-perfect careful pilot at 8 s
  (198 / 200), it costs the typical careful 8 / 0, the typical standing 21 / -5 (Ilya's +5 is noise), the typical clumsy 12 / 15, and
  the slow careful 22 / 29 - it widens the gap between skill levels without moving the top, and it reaches Ilya
  (range does not help against a mark on yourself; Ilya's slow-human clears 129 -> 100). Every 5 s is harsher on
  the Vanguard (frame-perfect careful 186: dodging slag walks it into the horde). Floor and Warden length unchanged.
- **Elite stomp: no effect** (0.2 hits a run). The Captain dies within seconds of reaching the hero; it would only
  matter if elites were longer fights, which is a different change.
- **Ambush surges: within the noise.** Waves already enter from every corner (EntrySides), so a pack ahead of the
  walk is no new kind of threat to a pilot that kites from whatever is near.

## Recommendation for the owner

**Measurement.** From now on, read difficulty with the cohorts under `FOUNDRY_REALISM=typical` (reaction 0.25 s on
every threat, aim error 0.5, a 0.4 s lapse 4 times a minute, a 0.5 s re-read after a card), and `slow` beside it as
the lower bound of the audience. Keep the frame-perfect pilot as the regression and Editor-parity pilot (the scene's
DevelopmentAutopilot has no realism, and should not: it is the parity reference). The typical setting is an
assumption, not a measurement of the owner: its reaction can be calibrated once telemetry carries the slam hits per
Warden fight and the pit hits per run on a real player (the typical human takes ~1 slam a fight; the bot takes 0).

**Structural options worth building** (each needs the owner's yes; costs are estimates for the scene plus the
simulation's parity):

| Option | Effect (typical human, careful / standing / clumsy, V / I) | Cost |
|---|---|---|
| 1. Slag lobbed at the hero, every 6-8 s outside the Warden, 1.6 wide, 1.0 s, 8 % | 141 / 186, 79 / 124, 20 / 79 (shipped 149 / 185, 100 / 119, 32 / 94); frame-perfect within the noise | ~1-1.5 days: a slag service on the pit's pattern (a clock that runs while anything stands, a mark at the hero, the slam's decal smaller, damage, paused by panels and the reveal), telemetry, simulation parity (the simulation's version exists) |
| 2. A Warden echo, enraged only, 1.1 s, 5-10 % (one more tuning round first) | at 10 % on every blow: 69 / 107, 66 / 93, 26 / 81 - too strong; enraged-only not yet measured | ~0.5-1 day on WardenSlamClock (a second mark state) and WardenSlamService's view; re-measure the 45-65 s window (it lengthens the fight 15-20 s) |
| 3. The Staff's splash (a value, not a structure; the hero-balance lever) | splash share 0.4: Ilya 156, 91, 21 (78 / 46 / 11 %) | ~0.5 day: one asset value and a re-measure; touches the V0.1 gate's weapon roles |

Not worth building: the elite stomp (no effect while elites die in seconds) and ambush surges (within the noise).

**Before any of these:** the gap between the heroes is the Staff, not the floor; floor-wide changes will keep moving
the Vanguard about twice as much as Ilya. And the careful picker is tuned for a pilot that is never hit: under human
error the first-card picker survives better, which is worth a look at CardScore before the cohort bands are pinned.

## What was built (all simulation-side, default off)

- `Assets/_Project/Tests/Support/FoundryRealism.cs`: `PilotRealism` (the model and its presets, `Named` for
  `FOUNDRY_REALISM`) and `PilotDice` (its own `RunRandom` purpose).
- `DescentSimulation.Foundry.cs`: `FoundryLevers.Realism` and the structural levers (`WardenEchoTelegraph`,
  `WardenEchoShare`, `SlagEvery`/`SlagTelegraph`/`SlagRadius`/`SlagShare`, `EliteStomp*`, `AmbushSurges`,
  `AmbushDistance`); the pilot's reaction, aim and lapses; the hazards' marks; `FoundryResult.TakenBy` (health lost by
  place and blow), `LowestHealthShare`/`LowestRoom`/`LowestPhase`/`LowestHarm`, `HazardMarks`/`HazardHits`,
  `TakenFromHazards`, `Lapses`.
- `FoundryBatch.DifficultyLevers` (`FOUNDRY_LEVERS=4`), `FoundryCohorts.Run/Levers(..., realism)`, `FOUNDRY_REALISM` in
  the batch report.
- `Tools/CombatChecks/FoundryDifficultyResearch.cs`: the research's batches on demand (explicit test).
- `Assets/_Project/Tests/EditMode/FoundryRealismTests.cs`: off leaves the floor as it was, a realism of zeros plays
  the frame-perfect floor, a late mark read is hit, the lapses are seeded, each hazard puts marks down, health lost
  adds up by place.

Not done: floor 2 under realism (`FOUNDRY_FLOORS=2` works with `FOUNDRY_REALISM`, not run here); the brief's "V2"
values (floor-clock damage growth, floor-1 modifier damage, pit 0.15) are not levers on this branch, so they were not
combined with the realism model.
