# Books from chests — the share, measured — 2026-09-20

S3 of `27` lets a chest carry a **book**: an `UpgradeOption` granted from a second source rather than a second system.
The mechanism was specified before anything was edited (`27` section 7) with one number left open — how many chests in a
hundred hold one — because a Descent places about ten chests against eleven level-ups, and that share decides whether a
build comes from the walk or from the levels. This is the measurement that set it, and the behaviour that came out of
building it.

## What a chest is today, and what changed

A chest stands 1.8 floor units from the centre of every combat room, on a spot that circles the arena room by room, and
opens when the hero walks onto it: a quarter of maximum health on even rooms, ten gold on odd ones. **A book is
additive.** The chest keeps the reward it has and some chests hold a book as well. The kind a chest reports is still
Heal or Gold, so nothing that shows a chest opening had to change, and the heal on room one - which every pinned
balance claim in this project leans on - is untouched.

The book itself is one stack of one card, drawn uniformly from exactly the eligibility a level-up uses (below its stack
limit, prerequisite held) at a tier drawn from the same rarity table at the run's luck. It is granted, not offered:
there is no panel, and no art was touched.

## The Descent simulation had never modelled a chest

Worth stating plainly, because it had been true since routes existed: `DescentSimulation` placed no chests and opened
none. A hero that walked onto a chest in the scene healed; the same walk in the simulation did not. It never showed up
as a parity failure because the scene parity driver **switched the chest spawner off** before its first frame, with the
reason written beside it: chests were not in the simulation, a walking hero steps on one, and a heal on one side only
would have made the comparison meaningless. It was the single remaining difference between the test scene and the game
a player runs.

S3 had to close it, because a book is not something a simulation can ignore the way it could ignore an unused heal. The
simulation now places a chest in every combat room on the same rule, opens it on the same reach check once the hero has
moved, and applies the same three effects in the same order; the parity driver stops disabling the spawner; and a new
case walks a scene hero straight onto a chest and holds the two together. The proof cases that already walked - turning
round, and the fetching one - now open a chest on both sides.

## The sweep

The authored Descent (Ember Halls, Quicksilver Vaults), twenty-four seeds, three weapons, the Forge Burst, the shipped
five-card pool, four walks: standing, kiting, fetching the chest and then standing, fetching and then kiting. Seventy-two
runs a cell. `ChestRule.BookPercent` is a compile-time constant, so each share is its own build of the harness.

| Share | fetch-stand deaths | fetch-kite deaths | Books min/median/max | Median HP left | Median fight |
|---|---|---|---|---|---|
| 0 % | 10 of 72 | 1 of 72 | 0 / 0 / 0 | 60.2 | 89.5 s |
| 10 % | 7 of 72 | 0 of 72 | 0 / 1 / 3 | 83.2 | 89.1 s |
| 15 % | 4 of 72 | 0 of 72 | 0 / 1 / 3 | 91.8 | 88.3 s |
| **25 %** | **1 of 72** | **0 of 72** | **1 / 2 / 6** | **104.8** | **85.3 s** |
| 50 % | 0 of 72 | 0 of 72 | 2 / 5 / 9 | 135.2 | 76.5 s |
| 100 % | 0 of 72 | 0 of 72 | 10 / 10 / 10 | 180.7 | 63.7 s |

Standing and kiting are in every one of these builds and never move, because a standing hero never leaves the middle and
a kiting one reaches a chest only by accident: standing clears 23 of 72 at every share, kiting 65 of 72. **The share
changes the game only for the player who walks to the chest**, which is what a reward standing away from the middle is
for.

### Three things the sweep says

**Without books the walk to a chest is a cost, not a reward.** At a share of zero, fetching and then standing dies ten
times in seventy-two and ends on 60 HP, against kiting's seven and 92. The hero spends its movement budget crossing the
arena and gets a heal it usually does not need, because a room's chest is reached before its pack has landed much. The
chest has been decoration for anyone not already hurt.

**Books flatten the Descent quickly.** At 50 % a fetching hero never dies in a hundred and forty-four runs, and at 100 %
it ends the run at the health cap with the fight a third shorter. Five books out of ten chests is already more build than
the eleven level-ups give.

**25 % is the lowest share that pays the walker reliably.** Every one of the seventy-two runs found at least one book, the
median is two against eleven chosen upgrades - about a sixth of the build - and a fetching hero can still die, though only
just. Below it a Descent can hand a player who walked to ten chests nothing at all, which is a worse thing to explain
than a small payoff.

## Limits of this measurement, and when the number has to move again

- **The Descent is too short for this to be a real choice.** Any hero that moves clears it: standing dies in 49 of 72
  runs and everything else clears. The share was chosen on the curve's shape, not on a difficulty it is actually
  defending, and the shape is what will still be true when S4 and S6 give the run some length.
- **The share is tuned to ten chests.** S6 re-authors floors into longer maps; more rooms means more chests at the same
  share, so a run that holds twenty chests hands over four books rather than two. When the floor count changes, this
  constant comes down with it. The target to hold is **about two books a Descent**, not the percentage itself.
- Two scripted policies, not a player. `ChestRoute` walks straight at the chest and then stands or kites; a person
  weighs the walk against what is chasing them, which no policy here does.
- Twenty-four seeds a cell on the pure .NET runner, no relics, nothing on a device.
- A fetching hero is now the strongest of the four walks. That is the intended shape - the budget needed something to
  buy - but it is only safe while the chest is reached before the pack lands. A swarm room (S4) is where that stops
  being true, and it is the honest place to test whether fetching is a choice or a habit.

## What it cost elsewhere

Nothing pinned moved. Every existing test passes unchanged, including the balance claims (Temper fails floor 1,
Counterweight rescues it) and the seed distribution of `26`. Two facts carry that: a standing run never opens a chest,
and at the shipped share the first chest of seed 0 - the seed the proof floors run on - holds no book. The second is a
coincidence with a bound attached: raise the share to 50 % and the book that chest then holds lengthens the Daggers'
looping run enough for a fourth enemy step-flicker, and `DensityProofTests` fails. That failure is the test working.
