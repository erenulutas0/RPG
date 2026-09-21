# The exponential ramp — 2026-09-21

Decision 6 in `27`: the finale's difficulty grows exponentially. The reason is a shape, not a number. The world grew by
**adding** - two per cent of its authored strength a wave, a fifth per evolution - and the hero grows by **multiplying** -
damage times cadence times crits. A linear ramp cannot catch a multiplicative build at any rate, which is what four phone
sessions showed: the owner left every run alive, and `33` found his own builds, replayed, would have died at waves
fifty-eight to ninety-six.

`SwarmRule.DifficultyAt` is now geometric: each wave `GrowthPerWave` times the one before, 1.0 on the first. It is computed
by repeated float multiplication in one body that is never inlined, not with `Math.Pow`, whose last bit has differed
between the Editor's Mono, the .NET runner and the phone's IL2CPP; an IEEE multiply is the same on all three. A swarm is a
few hundred waves at most.

## The criteria, written before the sweep

Measured against the **calibrated** player of `33` - the owner's card ranking and the movement that fits his health curve,
no burst - not the slot-zero player every earlier constant was set against.

1. **A ceiling**: no calibrated run of any weapon reaches wave 80.
2. **Depth still reads the build**: the Staff goes clearly deeper than the Sword and the Daggers.
3. **The finale is where you probably die**: the Staff's median lands between wave 30 and 45, below where the owner left
   his runs (44 to 55).
4. **The early game is not the new killer**: no more runs die by wave 15 than did under the linear ramp.

## The sweep

Sixteen seeds a weapon first, across four rates:

| Ramp | Staff median | Deepest run | Died by wave 15 |
|---|---|---|---|
| linear, 2 % | 53.5 | 82 | 14 / 48 |
| geometric 1.02 | **40.5** | 60 | 16 / 48 |
| geometric 1.03 | 21.0 | 38 | 22 / 48 |
| geometric 1.04 | 15.5 | 28 | 36 / 48 |
| geometric 1.05 | 14.0 | 28 | 41 / 48 |

1.03 and above are walls: the Staff dies in the low twenties and the early game fills with deaths. 1.02 passed criteria
one to three and missed four by two runs in forty-eight, which is inside the noise of a distribution this bimodal - so
instead of arguing it, the two nearest candidates were run again at **forty-eight seeds a weapon**:

| Ramp | Staff median | Staff p95 | Runs past wave 80 | Died by wave 15 | Against linear |
|---|---|---|---|---|---|
| linear, 2 % | 53.5 | 93 | 9 / 144 | 41 / 144 | — |
| **geometric 1.02** | **41.0** | **65** | **1 / 144** | 45 / 144 | z = +0.52 |
| geometric 1.025 | 30.0 | 51 | 0 / 144 | 58 / 144 | **z = +2.11** |

**Shipped at 1.02.** It is the only rate that reshapes the finale - the Staff's median from 53.5 to 41, its ninety-fifth
percentile from 93 to 65, the runs past wave eighty from nine to one - while leaving the first fifteen waves where they
were. 1.025 takes the last long run as well, and makes the early game measurably deadlier to do it: the Daggers lose
thirty-two runs in forty-eight by wave fifteen against twenty-four under the linear ramp. That is the thing criterion four
exists to refuse, and it refused it.

## Where the criteria were not met as written

Two, and they are recorded rather than reworded:

- **Criterion one** was written as a maximum, and a maximum grows with the sample. At sixteen seeds the deepest 1.02 run
  was sixty; at forty-eight, one Staff run in a hundred and forty-four reached eighty-five. The tail went from nine runs
  past wave eighty to one; it did not go to none. Criterion one should have been a percentile.
- **Criterion four** was written as "no more", and 1.02 has four more early deaths in a hundred and forty-four. The
  difference is 2.7 percentage points with z = 0.52 - indistinguishable from noise - so the early game did not get more
  lethal, but the strict wording is not met.

## Limits

- The calibrated player still underestimates the owner: `33` found his exact picks, replayed, go deeper than his card
  ranking does on fresh seeds. So the owner's real depth under 1.02 is likely above the calibrated median of forty-one.
  If he still leaves comfortably, 1.025 is the measured next step and its cost - an early game harder on the Daggers - is
  already known.
- The evolution cost of `32` (0.20 a stack) was left alone. It was set against the slot-zero player and now sits on top of
  a steeper ramp; whether it is still the right number is a question for play, and for a re-measurement if play says so.
- Sixteen and forty-eight seeds a weapon on the pure .NET runner, one swarm ladder, no relics, nothing on a device.
- Difficulty is still invisible. The ramp now accelerates, and the player has no way to see it accelerating.
