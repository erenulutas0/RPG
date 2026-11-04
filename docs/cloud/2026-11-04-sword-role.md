# The Sword's role without the Staff (decision 3): measured options

Cloud research, 2026-11-04, branch `claude/research-sword-1104` from `research/pilot-cohorts` (1723274). The foundry
simulation (`DescentSimulation.RunFoundry`), seeds 1-200 a hero, every candidate on the same seeds; the leading
candidates again on seeds 201-400. Computed, not measured in the Editor. Nothing shipped changes: every candidate is a
research lever on `FoundryLevers` that only the foundry simulation reads (defaults = the shipped Sword; the default
batch reproduces the baseline below exactly). The owner decides.

## Summary

- **The Sword can own the Warden, cheaply.** The smallest change that gives it a role is a foundry-only damage
  multiplier on what the Sword lands on the Warden: x1.4 (Sword 25 a second against the Staff's 22, a 14 % lead; 26 to
  22 on seeds 201-400) or x1.5 for margin (27 to 22 on both seed sets). The careful cohort stays at 199/200 and 200/200,
  the Warden's P50 stays inside 45-65 (Vanguard 64 -> 57-58 s, Ilya 55 -> 50-51 s). Roles become "Staff = horde,
  Sword = Warden".
- **No Sword change gives the Daggers a role.** The gate's roles row reads each weapon's own damage when the Vanguard
  starts with it, so a Sword lever cannot move the Daggers' figures (2 a second on the horde, 0 on the Warden).
- **The Daggers' 0 on the Warden is reach against the careful pilot's kite ring**, not their damage: the pilot keeps
  the Warden at its reach + `FootworkRule.GateKiteMargin` = 1.7 + 0.3 = 2.0 units, and the Daggers reach 1.8. At reach
  2.2 the Daggers deal the Warden 22 a second (from 0); at 2.5, 25 a second and they lead it. The Sword (2.1) is only
  just outside the ring, which is also why the Sword's reach levers barely move its Warden damage.
- **"Staff = horde, Sword = Warden/elites, Daggers = ?" is reachable for the first two only.** With two jobs, at most
  two weapons can lead every other on one job (the strict reading). The gate's code reads it pairwise (a weapon has a
  role when, against each other weapon, it is ahead by more than 10 % on the horde or on the Warden), which allows a
  third, "between" weapon. For the Daggers to be that weapon beside a Warden Sword they need, at once, horde damage over
  1.1x the Sword's (about 11-12 a second; they have 2-8) and Warden damage over 1.1x the Staff's (about 22-25; they
  have 0). The one measured configuration with all three roles is Staff = horde, **Daggers** = Warden, Sword = between
  (Sword Warden x1.5, Daggers reach 2.5 and Warden x2); it moves the Daggers' values and shortens the Warden fight to
  37/35 s, under the 45-65 window.
- **Sweeps, cleaves and knockback are survival levers, not role levers.** They lift the weaker cohorts' Vanguard clears
  (cleave on kill 60 %: clumsy 74 -> 112, standing 130 -> 162; a 2.5 sweep at 60 %: clumsy 74 -> 96) but the Sword's own
  horde damage a second stays 8-13 against the Staff's 28-32: the horde comes at the director's rate, and what the Sword
  sweeps the Staff would have splashed anyway. None gives the Sword a role. A faster Sword (interval x0.67) does reach
  the Warden role (27 to 22) but lifts every cohort's clears (standing V 130 -> 165, auto-farmer I 19 -> 38) and is the
  Sword's shared interval unless made foundry-only.
- The node waves (the halls' and elites' waves) as a third job: the Staff leads them too (20-21 a second; Sword 10-16,
  Daggers 6, 11-13 at reach 2.2-2.5). No Sword candidate leads it; "elites and Warden x1.4" takes the Sword to 14.

## Baseline (shipped, careful cohort default; FOUNDRY_COHORTS=1, FOUNDRY_FLOORS=2, seeds 1-200)

Per weapon, the Vanguard started with each (the gate's roles row), P50 (P10-P90):

| Starting weapon | Horde a second | Warden a second | Node waves a second | Share of the weapons' damage | Evolved |
|---|---|---|---|---|---|
| Sword | 10 (8-12) | 18 (14-24) | 12 | 32 % | 36 % |
| Staff | 30 (26-36) | 23 (18-27) | 21 | 74 % | 40 % |
| Daggers | 2 (1-5) | 0 (0-2) | 6 | 9 % | 43 % |

Roles: not met - the Staff leads both jobs; the Sword and the Daggers have none. (main's gate report with the old pilot
read Staff 36/30, Sword 10/26, Daggers 4/13; the careful pilot's kite ring is what takes the Daggers' and part of the
Sword's Warden damage away.)

| Cohort | Vanguard cleared | Ilya cleared | Vanguard floor P50 / Warden P50 | Ilya floor P50 / Warden P50 |
|---|---|---|---|---|
| careful | 199 (100 %) | 200 (100 %) | 299 / 64 s | 290 / 55 s |
| first-card | 196 | 200 | 295 / 62 | 286 / 53 |
| standing | 130 | 156 | 276 / 47 | 274 / 43 |
| clumsy | 74 | 160 | 102 / 53 | 277 / 51 |
| auto-farmer | 6 | 19 | 231 / 33 | 239 / 30 |

Health lost at the Warden (careful, mean of the runs that reached it): Vanguard 13 of 112, Ilya 10 of 90; no deaths
there. Floor 2 after a floor-1 clear (careful): Vanguard 197 of 199 cleared it, Ilya 200 of 200. The gate's other rows:
synergy met (87 % / 89 % of clears evolved), engaged uptime not met (0.71 / 0.56).

## The candidates (seeds 1-200 a hero)

Clears: of 200, per cohort. Roles (gate): the gate's pairwise reading, 10 % margin. Leaders (strict): weapons that lead
every other on one job. Dps columns: the starting weapon's own damage a second (horde: of the floor it was held;
Warden: of the fight; node: of the node fights), P50, careful cohort, Sword / Staff / Daggers starts. Floor, Warden
P50 and health lost at the Warden: careful cohort. Ilya starts with the Staff; a Sword lever reaches it only through
the Sword it picks up, which is why its weaker cohorts move too.

| Lever | Clears V (careful / first-card / standing / clumsy / auto-farmer) | Clears I (same) | Roles (gate) | Leaders (strict) | Horde dps Sword / Staff / Daggers | Warden dps Sword / Staff / Daggers | Node dps Sword / Staff / Daggers | Floor P50 V / I | Warden P50 V / I | Health lost at the Warden V / I | Sword share of the Vanguard's weapon damage |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Shipped Sword | 199 / 196 / 130 / 74 / 6 | 200 / 200 / 156 / 160 / 19 | not met: Sword, Daggers without | Staff | 10 / 30 / 2 | 18 / 23 / 0 | 12 / 21 / 6 | 299 / 290 | 64 / 55 | 13 / 10 | 32% |
| Reach 2.1 -> 2.5 | 196 / 195 / 137 / 66 / 6 | 200 / 200 / 169 / 159 / 23 | not met: Sword, Daggers without | Staff | 11 / 28 / 2 | 20 / 23 / 0 | 12 / 20 / 6 | 298 / 289 | 63 / 54 | 15 / 10 | 36% |
| Reach 2.1 -> 3.0 | 199 / 197 / 134 / 76 / 8 | 200 / 200 / 173 / 164 / 26 | not met: Sword, Daggers without | Staff | 12 / 27 / 2 | 20 / 23 / 0 | 13 / 20 / 6 | 295 / 287 | 62 / 54 | 14 / 10 | 38% |
| Sweep 2.5 round the hero, 60% | 200 / 200 / 141 / 96 / 13 | 200 / 200 / 177 / 163 / 26 | not met: Sword, Daggers without | Staff | 9 / 30 / 2 | 21 / 23 / 0 | 14 / 20 / 6 | 297 / 288 | 62 / 54 | 13 / 9 | 31% |
| Sweep 2.5 round the hero, 40% | 200 / 199 / 141 / 80 / 11 | 200 / 200 / 169 / 158 / 20 | not met: Sword, Daggers without | Staff | 8 / 30 / 2 | 20 / 23 / 0 | 13 / 20 / 6 | 298 / 289 | 62 / 55 | 13 / 9 | 30% |
| Arc 180 deg, 2.5, 60% | 200 / 198 / 132 / 87 / 8 | 200 / 200 / 167 / 159 / 23 | not met: Sword, Daggers without | Staff | 8 / 30 / 2 | 20 / 22 / 0 | 12 / 20 / 6 | 298 / 288 | 63 / 54 | 14 / 10 | 29% |
| Arc 120 deg, 2.5, 60% | 198 / 196 / 131 / 67 / 7 | 200 / 200 / 165 / 163 / 21 | not met: Sword, Daggers without | Staff | 8 / 31 / 2 | 19 / 23 / 0 | 12 / 20 / 6 | 298 / 289 | 63 / 55 | 16 / 10 | 29% |
| Arc 120 deg, 2.1, 60% | 190 / 195 / 127 / 61 / 5 | 200 / 200 / 162 / 162 / 22 | not met: Sword, Daggers without | Staff | 7 / 32 / 3 | 18 / 24 / 0 | 10 / 21 / 6 | 299 / 290 | 65 / 55 | 18 / 10 | 26% |
| Evolved: sweep 2.5, 60% | 198 / 196 / 132 / 71 / 6 | 200 / 200 / 171 / 162 / 17 | not met: Sword, Daggers without | Staff | 9 / 31 / 2 | 19 / 23 / 0 | 11 / 21 / 6 | 300 / 290 | 65 / 55 | 13 / 8 | 30% |
| Evolved: pulse x1.5 | 199 / 196 / 131 / 74 / 6 | 200 / 200 / 158 / 159 / 19 | not met: Sword, Daggers without | Staff | 10 / 30 / 2 | 19 / 23 / 0 | 12 / 21 / 6 | 299 / 289 | 64 / 55 | 13 / 10 | 32% |
| Evolved: pulse x2 | 199 / 196 / 133 / 76 / 6 | 200 / 200 / 156 / 161 / 19 | not met: Sword, Daggers without | Staff | 10 / 30 / 2 | 19 / 23 / 0 | 12 / 21 / 6 | 298 / 289 | 64 / 55 | 13 / 9 | 33% |
| Cleave on kill 40% | 200 / 200 / 150 / 92 / 9 | 200 / 200 / 162 / 163 / 22 | not met: Sword, Daggers without | Staff | 12 / 29 / 2 | 20 / 23 / 0 | 14 / 21 / 6 | 298 / 290 | 63 / 55 | 13 / 9 | 37% |
| Cleave on kill 60% | 200 / 200 / 162 / 112 / 10 | 200 / 200 / 172 / 162 / 28 | not met: Sword, Daggers without | Staff | 13 / 28 / 2 | 21 / 23 / 0 | 15 / 20 / 6 | 297 / 289 | 62 / 55 | 13 / 9 | 38% |
| Knockback 0.5 unit | 200 / 196 / 136 / 73 / 7 | 200 / 200 / 162 / 159 / 23 | not met: Sword, Daggers without | Staff | 10 / 30 / 2 | 18 / 23 / 0 | 12 / 21 / 6 | 299 / 290 | 64 / 55 | 13 / 10 | 32% |
| Knockback 1 unit | 200 / 196 / 144 / 84 / 12 | 200 / 200 / 182 / 169 / 34 | not met: Sword, Daggers without | Staff | 10 / 30 / 2 | 18 / 23 / 0 | 12 / 21 / 6 | 299 / 290 | 64 / 55 | 12 / 9 | 32% |
| Interval x0.8 (0.64 s) | 200 / 198 / 148 / 85 / 15 | 200 / 200 / 175 / 164 / 25 | not met: Sword, Daggers without | Staff | 12 / 29 / 2 | 23 / 23 / 0 | 14 / 20 / 6 | 294 / 286 | 60 / 53 | 11 / 8 | 38% |
| Interval x0.67 (0.53 s) | 200 / 200 / 165 / 100 / 24 | 200 / 200 / 187 / 166 / 38 | not met: Daggers without | Sword, Staff | 14 / 27 / 2 | 27 / 22 / 0 | 16 / 20 / 6 | 290 / 284 | 56 / 50 | 8 / 6 | 42% |
| Warden x1.3 | 199 / 196 / 141 / 77 / 8 | 200 / 200 / 163 / 161 / 23 | not met: Sword, Daggers without | Staff | 10 / 30 / 2 | 23 / 23 / 0 | 12 / 21 / 6 | 292 / 286 | 59 / 52 | 12 / 9 | 34% |
| Warden x1.5 | 199 / 196 / 139 / 80 / 9 | 200 / 200 / 170 / 164 / 30 | not met: Daggers without | Sword, Staff | 10 / 30 / 2 | 27 / 22 / 0 | 12 / 21 / 6 | 289 / 284 | 56 / 50 | 11 / 8 | 35% |
| Elites and Warden x1.4 | 199 / 196 / 147 / 90 / 12 | 200 / 200 / 179 / 164 / 33 | not met: Daggers without | Sword, Staff | 10 / 30 / 2 | 25 / 22 / 0 | 14 / 20 / 6 | 293 / 284 | 59 / 51 | 13 / 9 | 35% |
| Horde x0.8, Warden x1.5 | 191 / 195 / 144 / 68 / 6 | 200 / 200 / 175 / 167 / 30 | not met: Daggers without | Sword, Staff | 10 / 30 / 2 | 27 / 22 / 0 | 12 / 21 / 6 | 290 / 284 | 57 / 50 | 12 / 9 | 34% |
| Reach 2.5 + Warden x1.2 | 196 / 195 / 143 / 71 / 7 | 200 / 200 / 175 / 160 / 24 | not met: Sword, Daggers without | Staff | 11 / 28 / 2 | 23 / 22 / 0 | 12 / 20 / 6 | 293 / 286 | 60 / 52 | 13 / 9 | 37% |
| Knockback 0.5 + Warden x1.3 | 200 / 196 / 144 / 76 / 9 | 200 / 200 / 174 / 158 / 30 | not met: Sword, Daggers without | Staff | 10 / 30 / 2 | 23 / 23 / 0 | 12 / 21 / 6 | 292 / 286 | 59 / 52 | 11 / 8 | 34% |
| Evolved sweep 2.5 + Warden x1.3 | 198 / 196 / 138 / 72 / 9 | 200 / 200 / 175 / 161 / 23 | not met: Sword, Daggers without | Staff | 9 / 31 / 2 | 24 / 23 / 0 | 11 / 21 / 6 | 294 / 286 | 59 / 52 | 12 / 8 | 32% |
| Warden x1.4 | 199 / 196 / 139 / 78 / 8 | 200 / 200 / 170 / 161 / 27 | not met: Daggers without | Sword, Staff | 10 / 30 / 2 | 25 / 22 / 0 | 12 / 21 / 6 | 291 / 285 | 57 / 51 | 11 / 8 | 34% |

What the Daggers would need (their values; not candidates for the Sword's question):

| Lever | Clears V (careful / first-card / standing / clumsy / auto-farmer) | Clears I (same) | Roles (gate) | Leaders (strict) | Horde dps Sword / Staff / Daggers | Warden dps Sword / Staff / Daggers | Node dps Sword / Staff / Daggers | Floor P50 V / I | Warden P50 V / I | Health lost at the Warden V / I | Sword share of the Vanguard's weapon damage |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Daggers: reach 1.8 -> 2.2 | 199 / 196 / 144 / 75 / 7 | 200 / 200 / 180 / 178 / 27 | not met: Sword, Daggers without | Staff | 10 / 29 / 3 | 17 / 21 / 22 | 11 / 20 / 11 | 282 / 276 | 48 / 43 | 9 / 7 | 30% |
| Daggers: reach 1.8 -> 2.5 | 200 / 195 / 151 / 78 / 9 | 200 / 200 / 183 / 172 / 28 | not met: Sword without | Staff, Daggers | 11 / 24 / 8 | 16 / 20 / 25 | 11 / 19 / 13 | 280 / 277 | 47 / 43 | 8 / 7 | 30% |
| Warden x1.5 + Daggers reach 2.5 | 200 / 195 / 158 / 84 / 15 | 200 / 200 / 189 / 176 / 42 | not met: Daggers without | Staff | 11 / 23 / 8 | 24 / 19 / 23 | 11 / 19 / 13 | 276 / 274 | 44 / 40 | 7 / 6 | 32% |
| Warden x1.5 + Daggers reach 2.5, Warden x2 | 200 / 195 / 163 / 86 / 23 | 200 / 200 / 199 / 186 / 72 | met | Staff, Daggers | 11 / 23 / 8 | 22 / 18 / 42 | 11 / 19 / 13 | 269 / 268 | 37 / 35 | 6 / 6 | 31% |

The leading candidates again, seeds 201-400:

| Lever | Clears V (careful / first-card / standing / clumsy / auto-farmer) | Clears I (same) | Roles (gate) | Leaders (strict) | Horde dps Sword / Staff / Daggers | Warden dps Sword / Staff / Daggers | Node dps Sword / Staff / Daggers | Floor P50 V / I | Warden P50 V / I | Health lost at the Warden V / I | Sword share of the Vanguard's weapon damage |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Shipped Sword | 199 / 190 / 130 / 83 / 0 | 200 / 200 / 162 / 168 / 10 | not met: Sword, Daggers without | Staff | 10 / 31 / 2 | 19 / 23 / 0 | 12 / 21 / 6 | 300 / 290 | 65 / 56 | 15 / 10 | 32% |
| Warden x1.5 | 199 / 190 / 136 / 83 / 1 | 200 / 200 / 180 / 174 / 18 | not met: Daggers without | Sword, Staff | 10 / 30 / 2 | 27 / 22 / 0 | 12 / 21 / 6 | 292 / 284 | 57 / 50 | 12 / 9 | 35% |
| Elites and Warden x1.4 | 200 / 196 / 142 / 86 / 4 | 200 / 200 / 175 / 178 / 14 | not met: Daggers without | Sword, Staff | 11 / 30 / 2 | 26 / 22 / 0 | 14 / 21 / 6 | 293 / 285 | 58 / 51 | 12 / 8 | 35% |
| Warden x1.4 | 199 / 190 / 133 / 87 / 1 | 200 / 200 / 173 / 175 / 18 | not met: Daggers without | Sword, Staff | 10 / 30 / 2 | 26 / 22 / 0 | 12 / 21 / 6 | 293 / 285 | 58 / 51 | 13 / 9 | 35% |

How each lever is modelled (`FoundryLevers`, `DescentSimulation.FoundrySword`, `FoundryRun.HeroAttacks` /
`FoeDamaged` / `EnemyAttack`):

- Reach: the Sword's range. Sweep: an area pattern whose circle is drawn round the hero, not the target, at the given
  radius and share (the target takes the full strike); an arc keeps the enemies within half the arc of the hero-target
  line. Evolved sweep: the shipped cleave until Resonant Edge, the sweep after.
- Evolved pulse: Resonant Edge's pulse share scaled (0.4/0.65/1.0/1.33 x1.5 or x2).
- Cleave on kill: `KillBurstRule` on the Sword from the start (the Staff evolution's mechanic, its circle).
- Knockback: modelled as a stagger, since the simulation's packs cannot be displaced - a struck enemy (not the Warden)
  cannot strike for units / its speed seconds. A real knockback would also open space; read it as a lower bound.
- Interval: the Sword's strike interval scaled (category and base).
- Damage by target: a multiplier on every Sword landing (procs included) on the horde, on the node waves, on the Warden.

## Recommendations for the owner

1. **Sword = the Warden's weapon: a foundry-only boss multiplier of x1.4-1.5 on the Sword** (recommended; x1.5 if a
   margin is wanted on more seed sets). Gives the Sword its role on both seed sets with the Staff's and Daggers' values
   untouched and the careful cohort's clears unchanged (199 / 200). Costs: the Warden fight shortens by about 7 s for
   the Vanguard (64 -> 57 s) and 4 s for Ilya (55 -> 51 s), still in 45-65; the weaker cohorts gain a little (Vanguard
   standing 130 -> 139, Ilya standing 156 -> 170, Ilya auto-farmer 19 -> 27-30); health lost at the Warden 13 -> 11.
   Build cost: one new value (a gate-boss damage share on the Sword's foundry pattern, read by AttackController on the
   foundry only), its tooltip line ("deals +50 % to the Forge Warden"), and a WeaponContentTests/scene-value test to
   hold the simulation to it; the hall and Descent keep Weapon_Sword.asset. "Elites and Warden x1.4" is the variant if
   the owner wants "Sword = elites and Warden": the same role, the Sword 25-26 to 22 on the Warden, and the weaker
   cohorts a few clears higher (clumsy V 74 -> 86-90).
2. **Decide what the Daggers are before tuning them, and first check the pilot.** The Daggers' 0 on the Warden comes
   from reach 1.8 inside the careful pilot's 2.0 ring; a player who steps in would hit it. Two honest options: (a) treat
   it as real - the Daggers are the weapon that must stand inside the hammer, and the gate's roles row needs a third job
   (node waves/elites, or the horde's single targets) or a reading that accepts two roles; (b) reach 2.2 for the Daggers
   (foundry-only like the Sword's), which gives them 22 a second on the Warden and shortens the Warden by about 16 s
   (64 -> 48 s Vanguard, 55 -> 43 s Ilya, the Ilya figure under 45), since the Vanguard and Ilya carry Daggers later
   too. Neither alone gives the Daggers a role beside recommendation 1: they still need horde damage above the Sword's.
3. **If all three roles must be met now**: the measured route is Staff = horde, Daggers = Warden, Sword = between
   (Sword Warden x1.5 + Daggers reach 2.5 and Warden x2: roles met). It moves the Daggers' values, puts the Warden at 37 /
   35 s (under the window; the Warden's health would need to rise again, see docs/41 2026-11-03) and lifts the weaker
   cohorts strongly (auto-farmer I 19 -> 72, standing I 156 -> 199). Not recommended without the owner's wish to make
   the Daggers the boss weapon.

Not recommended for a role: the sweeps, arcs, evolved pulse, cleave on kill and knockback - none gives the Sword a role;
cleave on kill 40-60 % and the 2.5 sweep are worth keeping in mind as a survival or feel lever for the Vanguard's weaker
cohorts, at the cost of pushing those cohorts' clears up.

## How to run

From the repository root, a .NET 9 SDK (a newer one with `DOTNET_ROLL_FORWARD=Major`):

```
FOUNDRY_SEEDS=200 FOUNDRY_PARALLEL=4 FOUNDRY_OUT=TestResults/sword-role \
  dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~FoundryBatchReport.TheSwordsRoleOverASeedBatch"
```

writes `sword-role.md` (the table above, a row a lever) and `sword-role.csv` (a line a cohort, hero and starting
weapon). `SWORD_LEVERS=0,18,26` picks rows of `FoundryBatch.SwordLevers()` by index; `FOUNDRY_FIRST_SEED=201` for the
second seed set. About 1 m 50 s a lever on four cores (12 batches of 200 floors). The batch report runs the same list
as levers with `FOUNDRY_LEVERS=4`. The baseline above: `FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1
FOUNDRY_FLOORS=2` on `TheFoundryFloorOverASeedBatch`.

## Independent replication (a second run of the same task, 2026-11-04 15:10 UTC)

A second run of this task, started before it saw this branch, wrote its own levers from scratch and ran 35 candidates.
Its shape: a hero-centred arc sweep that replaces the cleave, cleave on kill, a stagger standing for knockback, an
evolution pulse bonus, and damage by horde / nodes / Warden, on the Sword and on the Daggers. It used seeds 1-200 a
hero with every cohort, and the Vanguard's three starts in both the careful and the clumsy pilot's hands. Its code is
not on this branch, because the two implementations overlap file for file. The figures below are what it adds to the
sections above, and every conclusion above held:

- **Warden x1.4 / x1.5 on fresh seeds 201-600** (careful starts): Sword 25.3 / 26.9 against the Staff's 22.2 / 21.9 on
  the Warden, leads of 14 % / 23 %; the Warden's P50 59 / 57 s (Vanguard) and 52 / 51 s (Ilya). x1.3 falls short (23.2
  to 22.6 on seeds 1-200). On seeds 1-200 the Vanguard's cohorts with x1.5: careful 199, standing 139, first-card 196,
  clumsy 80, auto-farmer 9 (shipped 199 / 130 / 196 / 74 / 6). Ilya's standing cohort rises 156 -> 170: it picks up the
  Sword.
- **Under the clumsy pilot** (the old pilot, which main's gate report reads), x1.3 already gives the Sword the Warden
  (33.1 to the Staff's 29.7), and x1.4 / x1.5 give 35.4 / 37.5. The Sword's Warden role holds under both pilots.
- **The smallest measured three-role set**: Sword Warden x1.4 with Daggers reach 2.4 and Daggers Warden x1.3 (Sword
  10.5 / 21.3, Staff 27.5 / 19.1, Daggers 7.2 / 28.6 horde / Warden a second; roles met, careful). Daggers reach 2.4
  alone gives the Daggers the Warden (24.1 to the Staff's 20.4), but then no Sword value fits between them: the Sword
  would need more than 22.4 on the Warden to pass the Staff and less than 21.9 to stay under the Daggers. The Warden's
  fight falls to 43 / 39 s, and with the Warden's share raised from 8.5 to 10 it is still 44 / 41 s, so the share would
  need about 11 or more. **Under the clumsy pilot this set fails**: the Sword's horde lead over the longer-reach Daggers
  is 1.08 (9.5 against 8.8), so the Sword reads "none".
- **Survival levers** (Vanguard clumsy / standing clears, shipped 74 / 130): a 360° sweep of 2.5 units at 60 % replacing
  the cleave 127 / 172; with Warden x1.5, 149 / 180; a 1.0 s stagger 88 / 163 (Ilya's auto-farmer 19 -> 37); cleave on
  kill at 100 % for up to 3 hops 109 / 176. A 120-180° sweep at 2.5 units *lowers* the Sword's horde damage (8.4-9.4
  against the cleave's 10.3), because the careful pilot's enemies stand off the hero. The evolution-tied levers (a sweep
  from the evolution, a pulse share +0.6) move nothing: the Sword evolves in 36 % of the Vanguard's runs, late.
