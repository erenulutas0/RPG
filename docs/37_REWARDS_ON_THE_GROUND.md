# Rewards on the ground — 2026-09-21

The first part of S6d's map work, specified in `27` ("S6d specification — rewards on the ground"). In every room but a
swarm a kill's experience and gold fall as one drop where the enemy fell; the hero picks a drop up by walking within
`DropRule.PickupRadius` of it; drops lie for the rest of the floor, through the pauses between waves and a forge; and when
a floor's last enemy falls whatever still lies flies to the hero, so a floor pays at its end exactly what its kills paid.
The finale pays at once, as it did. The owner chose kill drops over rewards placed in rooms, and the Descent only for now.

The simulation mirrors all of it on the same frames, which meant giving it the frames between waves: the scene has always
had them, and a hero who fetches a drop walks in them. A parity case plays the whole of Ember Halls with a hero who kites
and fetches, scene against simulation, and the two agree to the frame: 31 kills, 30 drops picked up on the way and one
swept at the end, 103 health, the same level, the same gold and the hero on the same spot.

## The criteria, written before the sweep

1. **Reach costs a walk.** At the chosen radius a Sword standing and fighting collects at least three quarters of floor
   one's drops as they fall, and a Staff standing and fighting at most half. The radius is the smallest that satisfies
   the Sword.
2. **Walking pays.** At that radius the calibrated player who fetches is at least one level higher than the one who does
   not when each enters floor one's boss room, at the median, and clears the Descent no less often.
3. **Nothing is lost.** A floor's gold and experience at its checkpoint do not depend on what the hero did.

## The radius, for a hero who stands

Floor one, sixteen seeds a weapon, the owner's card ranking; the share of floor one's drops each weapon picked up as they
fell, standing and fighting:

| Radius | Sword | Staff | Daggers |
|---|---|---|---|
| 1.0 | 0.00 | 0.00 | 0.00 |
| 1.5 | 0.73 | 0.68 | 0.81 |
| 2.0 | **0.80** | 0.78 | 0.92 |
| 2.5 | 0.92 | 0.87 | 0.97 |
| 3.0 | 0.97 | 0.97 | 0.97 |

Criterion one picks **2.0**, the smallest radius at which the Sword keeps three quarters - and its second half fails at
every radius from 1.5 up, because **its premise was wrong**. It assumed a ranged weapon kills far away. The enemies walk to
the hero, so the Staff's kills fall at its feet almost as often as the Sword's. At one unit nobody picks anything up: every
enemy stops just inside its own reach, the Cinder Mite's 1.5, and a drop lies where it died.

## The Descent at three radii

The whole Descent, forty-eight seeds a weapon, the owner's ranking and Mend. "Calibrated" is the back-off player of `33`;
"fetching" is the same player walking to the nearest drop whenever nothing is within its reach plus 1.5.

| | Cleared 2 floors: Sword / Staff / Daggers | Level entering floor one's boss | Health at the end (median of winners) |
|---|---|---|---|
| Before drops, calibrated | 47 / 46 / 46 | 6 | 74 to 84 |
| 2.0, calibrated | 47 / 46 / 46 | 6 | 76 to 82 |
| 2.0, calibrated and fetching | **48 / 48 / 48** | 6 | 83 to 96 |
| 1.5, calibrated | 44 / 45 / 45 | 5 to 6 | 65 to 75 |
| 1.5, calibrated and fetching | 48 / 48 / 48 | 6 | 92 to 100 |
| 1.0, calibrated | 34 / 30 / 35 | **3 to 4** | 46 to 53 |
| 1.0, calibrated and fetching | **48 / 48 / 48** | **6** | **~100** |
| Before drops, standing | 23 / 19 / 26 | 6 | 27 to 32 |
| 1.0, standing | 0 / 0 / 0 | never reached | - |
| 1.0, standing and fetching | 26 / 26 / 33 | 6 | 37 to 54 |

At 2.0 criterion two fails: fetching changes nothing about when the cards come (level 6 at the boss either way), because a
standing hero already picks up four drops in five. It does make the Descent safer - every seed cleared, more health left -
and most of that is not the drops at all but the chests on the way (below). At 1.0 criterion two passes plainly: the hero
who walks is two to three levels ahead at the boss and clears every seed, and the one who does not clears two in three.

## The owner's choice: one unit

Shown the three, the owner chose **1.0** - the Megabonk shape, where the reward for a kill has to be fetched. It is not the
radius criterion one would have shipped, and that is recorded rather than reworded: the criterion was written to keep a
melee hero from being made to chase, and the measurement showed that at the radius it picks there is nothing to chase
for. What 1.0 costs is that a hero who does not walk loses the Descent - a standing hero now picks up nothing all floor
and meets every boss at level zero - and that drops must be seen to be fetched, so the phone waits for them to be drawn.

Over twenty-four seeds with the Forge Burst: a kiting hero who fetches clears 23, 23 and 24 (22, 22 and 21 before drops);
kiting without fetching 19, 15 and 20; standing and fetching 12, 9 and 13 (standing cleared 7, 6 and 10 before); standing
and never fetching clears none.

## What walking found out about the Descent's balance

Every Descent balance number since chests existed (`28`) was measured by a hero who never reached one: the standing
baseline stays in the middle, and the calibrated back-off player of `33` opened none in the 576 Descents it played here. A hero who
walks for its drops passes them and opens two or three a Descent - the owner opened nine in his last run - and that alone
moves claims the project has pinned for a week. Measured as those claims were, with the burst, the two-card pool and Mend
on floor two:

| Claim, as pinned | Standing, before drops | Walking and fetching |
|---|---|---|
| Tempering on floor one leaves too little health to descend | loses on floor 2 | **clears, 67.5 health** |
| Second Wind rescues a damage-first Temper Descent | clears, triggers once | clears without ever triggering |
| Counterweight leaves more health than none | 48.0 against 30.4 | **41.0 against 53.3** |
| The burst spares the Sword health | 30.4 against 0 (dies without it) | **53.3 against 61.4** |
| Daggers with Counterweight carry a Temper Descent | clears | dies on floor 2 |

So **greed is no longer punished on a Descent that is walked**. That was already true on the phone, where the owner has
always walked; drops only made the measuring hero do what he does. The pinned tests keep their claims on the control the
simulation keeps - the game before drops, standing (`groundDrops: false`) - because what they pin is the combat's
arithmetic, which drops did not touch. The game as it plays now is described by the new tests beside them.

This is the brief for the rest of S6d: the floors below are too short and too gentle to carry the difficulty decision 5
gives them, and the Temper/Mend and relic choices only mean something again if a walked Descent can still be lost.

## Limits

- A drop draws nothing yet. It has to be seen to be fetched, so this is not a phone build until it is drawn; that is
  Fable's and Astra's. A pull toward the hero, and a card that widens the radius, are theirs to decide with the owner.
- The fetching player is a stand-in, not calibrated: the owner has never played with drops, so how he fetches is unknown.
- Telemetry now counts a dropped kill's gold as kill gold when it is picked up, not when the enemy falls.
- The finale is untouched; its measurements in `35` and `36` stand.
