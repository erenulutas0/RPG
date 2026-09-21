# Calibrating the measurement to the owner — 2026-09-21

Every constant since `29` - the swarm ramp, the evolution cost, the behavioural cards' magnitudes - was set against a
scripted player, and four phone sessions said that player was much weaker than the owner: the policies died between waves
twelve and forty-five, and he left, alive, at thirty-two, forty-four, fifty and fifty-five. Before the exponential ramp
the owner approved can be tuned (decision 6 in `27`), the measurement has to stand where he stands. This is S6a. Nothing in
the game changed.

## The instrument: replaying his builds exactly

The telemetry holds each of his swarm runs wave by wave - health, level and every card he took, in order - and each
run's seed. Offers are drawn from the seed and the stacks, so a simulated run that takes the same card at the same offer
sees the same offers: **his build is reproduced exactly**, and what is left between his health curve and the simulation's
is whatever the simulation does differently. `DescentSimulation.Run` gained a `replay` - the k-th recorded card at the k-th
offer - and a per-wave trace, and a test now pins that a run replayed from its own picks returns the same picks, the same
depth, the same health and the same kills, with no misses.

Each run is replayed against the pool of the build he played it on: nine cards for the S4 build, eleven for S5a and S5c,
fourteen for S5b. No Forge Burst: he used it once or not at all a run, where every simulated policy fires it whenever it
is ready - thirty-odd times - which on its own would make the simulation *stronger* than him.

## The hypothesis was wrong

`27` section 12 said this step would "isolate the one thing the simulation gets wrong: how he moves." It isolated
something else. Given **his** cards, the simulation's movement fits him closely:

| Movement policy | Run cc1c76c8 (Staff, 55 waves) | Run d84ca3cb (Staff, 32 waves) | Depth the replay reaches |
|---|---|---|---|
| stand | health 32 below his, every wave | 45 below | 42 – 59 |
| back-off (release 0.6) | 12 below | 4 below | 58 – 89 |
| **back-off (release 1.5)** | **1 above** (mean error 10) | **4 below** (mean error 6) | **58 – 96** |
| kite | 10 above | 17 above | 110 – 129 |

Standing undershoots badly, kiting overshoots, and a back-off player that retreats a little further before stopping sits
within six to ten points of his health on every wave both played. **The simulation was never wrong about moving. It was
wrong about choosing.** Every measurement since `29` took whatever card sat in slot zero, and he does not.

The replays also answer the question every session has left open. Played on with his own builds, his runs would have
died at waves **fifty-eight to ninety-six**. He left each of them with that much margin still in hand.

## How he chooses

Every offer he answered with two cards on it, ranked by how often each card was taken when shown:

| Card | Taken / shown |
|---|---|
| Ruinous Edge, Blurred Grip (evolutions) | 3 / 3, 3 / 3 |
| Tempered Edge (damage) | 20 / 23 |
| Quickened Grip (attack speed) | 20 / 25 |
| Ironblood (health evolution) | 3 / 4 |
| Deep Lungs (stamina) | 9 / 16 |
| Long Reach | 11 / 22 |
| Keen Edge (crit) | 16 / 35 |
| Vital Surge (health) | 20 / 47 |
| Swift Boots | 10 / 27 |
| Fortune | 6 / 17 |
| Iron Skin (armor) | 16 / 51 |
| Forge Pulse | 0 / 1 |

Evolutions whenever offered, then damage and cadence, then everything else. It is a clear, stable order across four runs
and three builds, and it is the opposite of slot zero, which takes whatever the draw put first.

## The calibrated player

His ranking plus the back-off movement that fits him, no burst, on sixteen fresh seeds - against the reference player that
`29` to `32` were measured with (slot zero, plain back-off, burst whenever ready):

| Weapon | Reference: median / reach 50 | Calibrated: median / reach 50 |
|---|---|---|
| Sword | 17.0 / 6 of 16 | 19.5 / 4 of 16 |
| Staff | 36.0 / 7 of 16 | **53.5 / 10 of 16** |
| Daggers | 12.5 / 2 of 16 | 14.5 / 2 of 16 |

A two-by-two on the same seeds says the card policy carries the difference and the movement change adds almost nothing on
fresh seeds. For the Staff, which he plays three runs in four, the calibrated player goes about half as deep again as the
reference one did, and its median lands between where he left his runs and where the replays say they would have ended.

## What this changes, and what it does not

- **Every constant set since `29` is soft, for the Staff most of all.** The ramp, the evolution cost and the swarm depth
  figures were measured against a player about two thirds as strong as the calibrated one with the weapon the owner uses.
  That is consistent with every session: he never died.
- It does **not** change the conclusion that the finale needs a new shape. The replays show his builds dying at fifty-eight
  to ninety-six under the current linear ramp - a ceiling exists, it is just far past where anyone will stay to find it.
- S6b tunes the exponential ramp against the calibrated player, not the reference one.

## Limits

- Four runs, three with the Staff and one with the Daggers. The Sword has no recorded run at all, so its calibration rests
  on the card ranking alone.
- A static ranking is only an approximation of him: his exact picks, replayed, go deeper than the ranking does on fresh
  seeds, so he chooses in context and the ranking does not.
- The distributions are **bimodal** - a run either dies by wave fifteen or goes deep - so a median moves a lot with a few
  seeds. Sixteen seeds a cell, the pure .NET runner, nothing on a device.
- One replay of run 5bef1df8 missed sixteen picks in every arm that did not fetch the chest, because that run's chest held a
  book; only the arm that walks to the chest reproduces it. It is the only replay that needed that, and the book is the
  reason.
