# The Forge Pulse, and a design claim that failed — 2026-09-21

S5a builds the first **behavioural** upgrade: a card that changes what an attack does rather than a number it carries.
Every fourth strike bursts round the hero for a share of its damage. The mechanism works, is contract-correct and is
shipped. The claim the roadmap attached to it does not hold, and that is the substance of this document.

## The claim, and how it was written down in advance

`docs/27` has said since the roadmap was drawn: *"Pulse should favour the Daggers, and if it favours the Sword the design
is wrong."* The reasoning was that the Daggers fight from 1.8 units every half second, so they stand inside their own
pulse and trigger it twice as often as the Sword, while the Staff already owns the crowd.

It is false. Measured in the swarm room over sixteen seeds, three weapons, four radii, four magnitudes and two different
ways of scaling the damage — about three hundred and eighty runs — **the Pulse is the Sword's card at almost every
setting**.

## What was measured

The swarm room, because it is the only place the game is crowded enough for a burst round the hero to mean anything; the
same backing-off player the ramp was measured against; a two-hundred-wave cap; and a pool with no Pulse card in it, so
the control arm is genuinely pulse-free. Median swarm wave reached, with no pulse at all:

| Weapon | Median wave, no pulse |
|---|---|
| Sword | 15.5 |
| Staff | 45.0 |
| Daggers | 12.5 |

Then the same runs with a pulse, as a share of the weapon's damage:

| Radius | Sword | Staff | Daggers |
|---|---|---|---|
| 1.6, share 0.5 | 36.0 (**+132 %**) | 47.5 (+6 %) | 15.0 (+20 %) |
| 1.9, share 0.5 | 39.0 (**+152 %**) | 48.5 (+8 %) | 17.5 (+40 %) |
| **2.2, share 0.5** | 46.5 (**+200 %**) | 49.0 (+9 %) | 22.5 (+80 %) |
| 2.2, share 1.0 | 42.0 (+171 %) | 52.5 (+17 %) | 37.5 (**+200 %**) |
| 3.0, share 1.0 | 65.5 (**+323 %**) | 61.0 (+36 %) | 47.0 (+276 %) |

The Sword gains most at eleven of the twelve cells. Shrinking the radius below the Sword's reach does not help the
Daggers — it takes from everyone.

### And it is not the scaling

The obvious suspicion was that a share of the weapon's damage rewards the biggest strike, so the same sweep was run with
a **flat** pulse, which rewards the most strikes instead:

| Flat damage | Sword | Staff | Daggers |
|---|---|---|---|
| 3 | 40.5 (**+161 %**) | 47.5 (+6 %) | 18.5 (+48 %) |
| 6 | 47.5 (**+206 %**) | 48.5 (+8 %) | 16.5 (+32 %) |
| 12 | 47.0 (**+203 %**) | 49.5 (+10 %) | 25.5 (+104 %) |
| 20 | 48.0 (+210 %) | 50.5 (+12 %) | 39.5 (**+216 %**) |

Still the Sword, until the flat amount is more than three Dagger strikes. And the reason the original reasoning failed
shows up in the pulse counts: over a run, the Sword fires **0.73 pulses a second** and the Daggers **0.68**. The Daggers
do not pulse more often. Their half-second cadence is real, but attack-speed cards lift every weapon, and the Sword
*survives longer*, so it collects more of them — the slower weapon ends up pulsing as fast as the quick one.

## Why the Sword, stated plainly

Three things compound, and none of them is about reach:

1. **In a swarm, every weapon has enemies inside the pulse.** Ten enemies press in on the hero whatever its reach, so a
   circle round the hero catches a crowd for the Daggers and the Sword alike. Reach decides where the *hero* stands
   relative to its target, not how many enemies are near it.
2. **The pulse is a per-strike effect, so it inherits the strike's weight.** The Sword's strike is 10 and the Daggers' is
   6; the Sword also cleaves, so a pulse compounds with area it already had.
3. **The Sword had the most room to gain.** Its pulse-free baseline of 15.5 waves against the Staff's 45 is a balance
   hole of its own, and anything that helps the weakest build shows the largest percentage.

The honest conclusion is that **a per-strike burst cannot be made the Daggers' card by tuning it**. What the Daggers lack
in a swarm is not area, it is damage; a card that multiplies what a strike does will always favour the weapon whose
strike is largest. A card for the Daggers has to key on something they have more of than anyone - their every-third-strike
crit rhythm is the obvious candidate, and it is a different card.

## What shipped anyway, and why

The Pulse itself. It is a good card and it fixes a real hole: the Sword is the weakest weapon in a swarm by a factor of
three, and this is the first thing the project has that helps it. It is shipped as **one stack**, tiers 0.4 / 0.65 / 1.0
of the weapon's damage, at **radius 2.2** and **every fourth strike**. One stack because a share of one already doubles or
trebles how deep a run reaches, so a card stacking to four would not be a card. Radius 2.2 because 1.6 takes from
everyone and 3.0 makes the card dominant. It is tenth in the authored order, so `UpgradePool` only opens it to a run that
has levelled far past a Descent - which is the finale, and nothing about the Descent moves.

What did **not** happen is quietly renaming the claim so it passed. `docs/27`'s S5 row now records that the claim was
measured and failed, and the Daggers' problem is an open question for the owner rather than a thing this slice solved.

## Limits

- Sixteen seeds a cell on the pure .NET runner, one swarm ladder, one player policy, no relics, nothing on a device.
- `PulseRule.Every` was not swept. Halving it is close to doubling the share, which the magnitude sweep spans, so the
  axis is covered indirectly rather than directly - but only indirectly, and a reader should not take 4 as measured the
  way the radius is.
- The share is handed to the weapon directly rather than earned from a card, because a run long enough to be offered the
  card is a different run. What the card feels like to be offered is unmeasured.
- The first sweep of all was thrown away: the control arm granted itself a Pulse, because a swarm run reaches level fifty
  and `UpgradePool` opens all ten cards by level thirty-six. The Staff pulsed a hundred and six times in what was
  supposed to be the row without pulses. Every number above comes from the rerun with a nine-card pool.
- The pulse is **silent**. No ring, no flash, nothing; a player will feel enemies die without seeing why. That is on the
  visual-needs list in `docs/27` with the book and the swarm clock.
