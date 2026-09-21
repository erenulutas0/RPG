# Evolutions, and what they cost — 2026-09-21

The phone session of 2026-09-21 measured a hole and, in the same run, measured the reason not to fill it carelessly.
Fifty-five swarm waves, fifty-one levels, **thirty-eight spent**: the nine core cards were full by wave thirty-two and
from wave forty-one onward the upgrade panel never opened again, so thirteen level-ups produced nothing at all. And the
owner left at 102 health losing 2.1 a wave, the third session running in which he stopped rather than died. A slice that
only handed out more power would have closed the first hole by digging the second.

So an evolution is a card **and a cost**. `UpgradeOption.EvolvesFromId` gates a card on another card being at its stack
limit; `UpgradeStat.Difficulty` joins the sheet `HeroStats` holds, starting at 1.0 and multiplying every enemy's authored
health and damage wherever the floor's own multipliers apply; and taking any evolution raises it by
`EvolutionRule.DifficultyPerStack`. Three evolutions are authored, on the three cards every measured run fills first.

## Two gates, not one

An evolution ignores `UpgradePool.WidthAt`. The width takes cards from the front of the authored order and a run that
reached level fifty-one only opens twelve of fourteen, so evolutions at positions twelve to fourteen would never have
arrived. Their gate is their base card being full, which is harder to reach than a level.

That alone was wrong, and a pinned test caught it in one run. **A whole Descent grants eleven level-ups and takes them all
on one card, which is enough to fill a five-stack card** - so a full base card by itself let an evolution into the
Descent, and the guarantee that S2c pinned (a Descent is bit-identical seed for seed) broke immediately. The gate is
therefore both: the base full **and** the run at `EvolutionRule.MinimumLevel`, which is the same level the width's first
step uses, one above the longest run the game has. With that, every Descent is again exactly what it was.

## The cost, measured

The swarm room, sixteen seeds, three weapons, the backing-off player, a two-hundred-wave cap. The control arm is the
shipped pool with the three evolutions removed - the game as it stood after S5c. Medians, and depth against each weapon's
own control:

| Cost per stack | Sword | Staff | Daggers | Staff difficulty | Staff wasted levels |
|---|---|---|---|---|---|
| control | 15.5 | 44.5 | 12.5 | 1.00 | 6.5 |
| **0.00** | 21.0 (+35 %) | **199.5 (+348 %, the cap)** | 12.5 (+0 %) | 1.00 | **59** |
| 0.15 | 17.0 (+10 %) | 53.0 (**+19 %**) | 12.5 (+0 %) | 2.12 | 4.0 |
| **0.20** | 17.0 (+10 %) | **36.0 (−19 %)** | 12.5 (+0 %) | 2.10 | **0** |
| 0.25 | 18.5 (+19 %) | 32.0 (−28 %) | 12.5 (+0 %) | 2.25 | 0 |
| 0.30 | 17.5 (+13 %) | 29.0 (−35 %) | 12.5 (+0 %) | 2.35 | 0 |
| 0.50 | 16.5 (+6 %) | 26.0 (−42 %) | 12.5 (+0 %) | 3.00 | 0 |

**Free evolutions break the game outright.** At no cost the Staff reaches the two-hundred-wave cap in the median run and
wastes fifty-nine levels doing it - more power, still nothing to spend it on, and no ceiling at all. That row is the
argument for the whole design.

**0.15 fails the second claim.** The specification said a run that takes every evolution must not go deeper than its own
control; at 0.15 the Staff goes nineteen per cent deeper. **0.20 is the smallest cost that satisfies it**, and it also
takes the Staff's wasted levels from six and a half to zero. Shipped at 0.20.

At 0.20 a Staff that takes every evolution it is offered ends up slightly **shallower** than one that takes none. That is
not a bug and it is not a trap: it means the right number of evolutions is *some*, which is what a build decision is. A
player who takes two and stops is stronger than both.

## Two limits that matter more than the table

**The simulation cannot decline a card.** Its picker takes whatever sits in slot zero, so every row above is "a player who
takes every evolution offered" - the worst case for the second claim and the best case for the first. The choice this card
exists to create is therefore the one thing the measurement cannot see. A real player reads "+90 % damage, and a harder
world" and decides.

**The policies never reach the depth where the hole lives.** The owner wasted thirteen levels at wave fifty-five. The
scripted players waste **zero** with the Sword and the Daggers, because they die at waves fifteen and twelve, and six and
a half with the Staff at wave forty-four. So the hole is only visible in one of three arms, and even there at half the
size the owner met. The fix is sized against half the problem, and the decisive test is the owner playing it.

This is the same gap that `29`'s ramp has: three sessions of telemetry say the scripted back-off player is roughly twice
as fragile as the owner. Re-measuring both constants against a policy calibrated to his depth is worth its own slice, and
until then every number here is a floor rather than an answer.

## Other limits

- Sixteen seeds a cell on the pure .NET runner, one swarm ladder, no relics, nothing on a device.
- The Daggers take no evolutions at all in simulation: they die at wave twelve and a half, before level twelve and before
  filling a five-stack card. Whether an evolution is reachable for them at all is unmeasured and depends on a player
  getting them further than any policy does.
- Only three evolutions exist, all of them stronger versions of the same stat. **Behavioural** evolutions - a weapon
  changing its pattern, the Daggers' direct hit becoming a cleave - are the interesting half and are not in this slice;
  they need the art the roadmap promised them.
- The evolutions' own magnitudes were not swept. The cost was swept against fixed magnitudes, which answers "how much
  should an evolution cost" but not "how strong should it be".
- Difficulty is invisible, like the book, the pulse and the splinter. A player takes a card that says "and a harder world"
  and has no way to see how much harder the world became. That is four silent mechanics now.
