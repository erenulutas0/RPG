# The continuous swarm and the walked exit — 2026-09-21

S6c in `27`. The finale's waves are gone: a swarm room holds one pack of at most ten for its whole life, a fallen enemy's
place waits `SwarmRule.RespawnSeconds` and fills again from the ladder read as a queue (`SwarmDirector`), and the ramp of
`34` runs on the clock - one step of 1.02 every `SwarmRule.StepSeconds` - with each enemy keeping the difficulty of the
moment it walked in. The per-wave question is gone with the waves. Leaving is a place: `ExitRule` puts an exit on the
arena's far side and the hero leaves by **standing still** on it for `ExitRule.DwellSeconds`.

Three constants were measured here, against the calibrated player of `33` (the owner's card ranking, back-off movement
with a release of 1.5, no burst), forty-eight seeds a weapon unless stated. The simulation mirrors the room frame for
frame, and two scene parity cases pin it: a hero standing in the swarm, and one walking out of it through the pack.

## The criteria, written before the sweep

For the two clocks, `34`'s four, restated for a room without waves:

1. **A ceiling**, as a percentile this time: the calibrated player's ninety-fifth percentile survives under eight minutes.
2. **Depth reads the build**: the Staff survives longest.
3. **The finale is where you probably die**: the Staff's median survival lands between 150 and 240 seconds.
4. **The opening is survivable**: at most one calibrated run in four dies inside the first sixty seconds.

For the exit's distance, one: **a calibrated hero who decides to leave early in the finale almost always gets out, and one
who decides late often does not.**

## First measurement: at level zero, the criteria contradict each other

The first sweep entered the swarm the way every measurement since `29` had, on the proof floor at level zero:

| Step s | Respawn s | Median s: Sword / Staff / Daggers | Died inside 60 s | p95 s | 1 2 3 4 |
|---|---|---|---|---|---|
| 3 | 1 | 41 / 162 / 37 | 91 / 144 | 196 | Y Y Y n |
| 3 | 2 | 40 / 199 / 49 | 74 / 144 | 229 | Y Y Y n |
| 3 | 2.5 | 51 / 204 / 55 | 61 / 144 | 240 | Y Y Y n |
| 4 | 1 | 36 / 205 / 38 | 90 / 144 | 251 | Y Y Y n |
| 4 | 2 | 77 / 261 / 55 | 53 / 144 | 293 | Y Y n n |
| 4 | 2.5 | 203 / 275 / 66 | 37 / 144 | 312 | Y Y n n |
| 5 | 1 | 37 / 260 / 38 | 86 / 144 | 304 | Y Y n n |
| 5 | 2 | 157 / 306 / 61 | 46 / 144 | 352 | Y Y n n |
| 5 | 2.5 | 269 / 322 / 75 | 27 / 144 | 367 | Y Y n Y |

No cell passes. Criterion four fails everywhere but the slowest corner, which fails three. The reason is not in the clocks:
a level-zero Sword or Daggers has one weapon and no area, and a crowd that never clears kills it inside a minute at almost
any setting, while the Staff's reach keeps it alive for three. That is the condition decision 5 in `27` says the finale is
never met in - it comes at the end of the Descent, with the build the Descent gave the hero. So the measurement was run
again the way the game will enter the room: Ember Halls, then the Quicksilver Vaults, then the swarm.

## The two clocks, measured at the end of the Descent

| Step s | Respawn s | Reach the finale | Median s: Sword / Staff / Daggers | Died inside 60 s | p95 s | 1 2 3 4 |
|---|---|---|---|---|---|---|
| **3** | 1 | 139 / 144 | 158 / 169 / 109 | 4 / 139 | 202 | Y Y Y Y |
| **3** | **2** | 139 / 144 | **176 / 202 / 146** | **2 / 139** | **236** | **Y Y Y Y** |
| **3** | 2.5 | 139 / 144 | 187 / 213 / 152 | 0 / 139 | 248 | Y Y Y Y |
| **4** | 1 | 139 / 144 | 193 / 218 / 147 | 2 / 139 | 255 | Y Y Y Y |
| 4 | 2 | 139 / 144 | 232 / 258 / 207 | 0 / 139 | 298 | Y Y n Y |
| 4 | 2.5 | 139 / 144 | 243 / 268 / 214 | 0 / 139 | 310 | Y Y n Y |
| 5 | 1 | 139 / 144 | 238 / 250 / 216 | 3 / 139 | 310 | Y Y n Y |
| 5 | 2 | 139 / 144 | 272 / 310 / 271 | 0 / 139 | 361 | Y Y n Y |
| 5 | 2.5 | 139 / 144 | 303 / 323 / 297 | 0 / 139 | 376 | Y Y n Y |

Four cells pass all four. **Shipped at a step of three seconds and a respawn of two.** At 1.02 a step every three seconds
the room doubles in difficulty about every hundred and five seconds; the Staff's median lands at 202, nearest the middle
of the window, and a kill buys the hero two seconds before its place fills. Between the passing cells the choice is a
preference and is stated as one: a faster ramp with a slower refill puts the room's pressure in the ramp, which is the
mechanism decision 6 chose, rather than in a crowd that never thins. A one-second refill costs the Sword about twenty
seconds of median survival and the Staff and the Daggers over thirty, the Daggers most because they have no area.

## A blind stand-in leaves by accident

The first finale sweep reported the chosen cell as a Staff median of 196 and seven early deaths - and thirteen of its
runs as having **walked out**, one of them fifteen seconds in, though that sweep never asked anyone to leave. The stand-in
fights by standing still until something is in reach, and the exit is part of the floor to it: stop on it for a second
while fighting and the run is over. Nine of the thirteen were the Staff, the weapon that stands still most.

The first build had shown a cousin of this - a kiting hero crossing the exit at twelve seconds - which is why the hold
counts only while the hero is not moving. Standing still is what a hero does most of the time, though, so that rule alone
does not stop it. A player who can see the exit does not stop on it by accident, so the measurement now plays one
that does not: `ExitRoute` plays the calibrated player until it means to leave and turns any stand on the exit into a step
off it. Every table in this document was measured with it; the accidental exits went to zero. The contaminated sweep had
also flattered leaving late: a decision at 240 seconds looked 1 to 13 % successful and is 0 to 3 %.

**This is a gap in the game today, not only in the measurement:** the exit draws nothing yet (it is Astra's), so a real
player on the phone can leave the way the blind stand-in did.

## The exit's distance

Twenty-four seeds a weapon at each of three distances from the arena's centre, the hero deciding to leave at the time
shown and then walking straight to the exit and holding it:

| Distance | 30 s | 60 s | 90 s | 120 s | 150 s | 180 s | 240 s | Decision to out, median |
|---|---|---|---|---|---|---|---|---|
| 3 | 100 % | 100 % | 100 % | 94 % | 65 % | 42 % | 3 % | 2.8 s |
| **5** | 100 % | 100 % | 100 % | 94 % | 65 % | 49 % | 0 % | 3.7 s |
| 7 | 100 % | 100 % | 100 % | 99 % | 65 % | 49 % | 0 % | 4.6 s |

The criterion holds at every distance, and that is the finding: **the distance is not the lever.** Four more units of
walking change almost nothing, because what leaving costs is still being alive when the decision is made in a crowd that
keeps hardening - the walk itself takes a few seconds. Shipped at **5** units toward the far corner, well beyond the chest
as the specification asked. By weapon at five units the bet reads the build: deciding at 180 seconds, the Staff gets out
92 % of the time, the Sword 43 % and the Daggers 12 %.

## What changed from the specification

- `RespawnSeconds` was to be fixed at one second. It was measured instead, because the first sweep showed it decides the
  opening as much as the step does; one second passes, two was chosen for the reason above.
- The hold is on the exit **and not moving**, not merely on it.
- The movement bar fills once, as the room opens: there are no wave starts left to refill it.

## Two tests re-stated

Both pinned a single seed and both broke under the new clocks, which says more about single seeds than about the rules.
The Forge Pulse test now sums eight seeds: a standing level-zero Sword dies in well under a minute, and one run that short
can end a moment either side of the other - with the pulse the eight lived 408 seconds in all against 315 without. The
evolutions test enters the swarm at the end of the Descent and sums six seeds, because an evolved run pays for its
evolutions in a harder room and can die sooner than the plain one, so one pair compares two runs of different lengths.

## Limits

- **The phone reaches the swarm only through the development start floor, at level zero.** The room is tuned for a hero
  arriving with a Descent's build, as decision 5 has it; entered at level zero, the Sword and the Daggers die in under a
  minute at the median and the Staff lasts about three. Putting the swarm at the end of the authored Descent is S6d.
- The calibrated player still underestimates the owner (`33`), so his survival under these clocks is likely longer than
  the medians above.
- The exit and the swarm's clock draw nothing; the room's hardening is still invisible.
- One swarm ladder, one arena, the pure .NET runner, nothing on a device yet.

## The first phone run — 2026-09-21

One run on the owner's S23, the Staff, entered at level zero through the development start floor. He died at **94
seconds** of play, level 28, 186 kills, to a Forge Warden, having taken eight evolution stacks - enemies at the end
stood at 1.85 from the ramp times 2.6 from the evolutions. He called it much harder. In the last wave-based build he left
every run alive at waves 44 to 55; this run ended at step 32, the difficulty of wave 32, so at the same point on the ramp
a room that never pauses is far deadlier than one that did.

Replayed from its seed in the simulation:

| Run | Survived |
|---|---|
| The owner, on the phone | 94 s |
| His 28 cards, back-off movement | 125 s |
| His 28 cards, back-off movement, the burst | 151 s |
| His seed, the calibrated ranking | 177 s |
| The calibrated player, 48 seeds (median) | 200 s |

For the first time the owner played **below** the calibrated player rather than above it. His cards followed his
recorded ranking at 22 offers of 28; the six others were attack speed over damage twice and health over range or crit
four times, taken as his health fell - which in the simulation costs this seed about fifty seconds. The remaining thirty
are play the replay does not capture. One run is one sample, and it is recorded as that.

Two things the run shows that the measurements did not ask about:

- **The menus did not go away.** 28 level-ups in 94 seconds of play put 34 seconds into choosing, 27 % of the time -
  against 30 % in his last wave-based run, question included. The per-wave question is gone; the level-up panel now
  arrives every three and a half seconds.
- **The crowd's make-up hardens by kills, not by time.** The ladder is a queue of spawns, and each place refills after a
  fall, so a hero who kills faster reaches the twelfth wave's list - a Warden and a Tank in every ten - sooner. Ten or more
  Wardens walked in during this run.
