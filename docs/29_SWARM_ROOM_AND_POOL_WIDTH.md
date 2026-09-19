# The swarm room, and a pool that grows with the run — 2026-09-20

S4 and S2c, built together because `26` Part 6 measured them and found they could not be separated: a swarm room grants
seventy-plus level-ups and the five-card pool holds twenty-two stacks, so a swarm without a wider pool pours most of what
it pays into a full cup. This is what was built, and the two constants the measurements set.

## S2c — the pool is a function of the run, not a constant

S2b measured a nine-card pool on today's Descent and it collapsed the game: a standing hero cleared one run in ten, a
kiting one fewer than half. That measurement stands, so the four held-back cards could not simply be added. What changed
instead is that the pool follows the run. Cards are authored in one order - Tempered Edge, Quickened Grip, Long Reach,
Keen Edge, Vital Surge, then Iron Skin, Swift Boots, Deep Lungs, Fortune - and `UpgradePool.WidthAt(level)` takes that
many of them from the front: five until level twelve, then one more every six levels.

**Twelve, because a whole Descent reaches eleven.** The threshold sits one level above the longest run the game has, so
a Descent is offered exactly the five cards it has always been offered. That is pinned rather than argued: over
twenty-four seeds, three weapons, standing and kiting, a run with all nine cards in the pool and a run with a pool of
exactly five return the same cleared floors, the same health, the same level, the same upgrades and the same fight
seconds - the same run, seed for seed. The scene now carries all nine cards and nothing a Descent player can see moved.

What it buys is the swarm. A swarm run reaches level twenty-six to thirty at the shipped ramp, which opens the seventh
and eighth cards, and a long one reaches all nine.

## S4 — a room whose waves do not run out

`RoomKind.Swarm` authors its waves as a **ladder** rather than a list that finishes: the ordinary `WaveDefinition[]`
every combat room has, handed out in order, with the last wave repeating for as long as the player stays. The
development floor's ladder is twelve waves, the shape `26` Part 6 measured - three enemies growing to the ten the
formations hold, grunts from the third, a runner from the sixth, the heavies from the tenth. Once it is spent the arena
is full, and the only thing left to escalate is each enemy.

`FloorProgress` gained endless rooms for it: an endless room's ordinal climbs past its ladder, it never clears itself,
and `LeaveEndlessRoom` is what the player's Leave choice calls. Leaving is asked through `CheckpointService` - the same
service, the same prompt and the same panel as Extract and Descend after a boss - with two new `CheckpointKind` values
and no new UI. The swarm room lives on a **development floor** reached through `development/start-floor.txt`, not in the
authored Descent: putting one into Ember Halls would move every pinned number in the project, and S6 is the slice that
re-authors floors.

### The ramp, measured

Sixteen seeds, three weapons, three walks - standing, backing off when something is inside reach, and fetching the room's
chest first - against the shipped ladder, the nine-card pool and books from chests. A cap of two hundred waves, so a run
that comes back having fought all of them has not been beaten.

| Ramp per wave | Standing (median wave) | Backing off | Fetching first | Median fight | Median level | Immortal runs |
|---|---|---|---|---|---|---|
| flat (0 %) | 11 | **200 (the cap)** | **200 (the cap)** | 590 s | 106 | 23 of 48 |
| 2 % (shipped) | 9 | 22 | 26 | 109–142 s | 26–30 | none |
| 5 % | 8 | 12 | 12 | 64–75 s | 12–14 | none |
| 10 % | 7 | 9 | 9 | 47–50 s | 8 | none |
| 20 % | 6 | 7 | 7 | 36–40 s | 5 | none |

Three things set the number.

**A flat room has no ceiling.** Backing off reached the two-hundred-wave cap in twenty-three of forty-eight runs and the
Staff in sixteen of sixteen, which is `26` Part 6's finding again with everything this project has built since. Any
positive ramp fixes it: at 2 % nobody reached the cap.

**Above 2 % the swarm stops being worth entering.** At 5 % a backing-off hero reaches level twelve to fourteen, which is
what a whole Descent already grants, in about a minute; at 10 % and 20 % it is less. A room whose purpose is to be where
a run levels has to pay more than the rooms around it, and only 2 % does: two to three times a Descent's level-ups, in
about two minutes, reaching the seventh and eighth cards of the pool.

**It still kills.** No run in forty-eight survived the cap at 2 %, the deepest reached one hundred and twenty-four waves
(enemies three and a half times their authored strength), and a hero that stands still dies at wave nine whatever it is
carrying. The owner's rule - the strongest build dies if it stays too long - holds by measurement rather than by hope.

## What building it caught

Three things, all of them in the scene rather than the rule, and all of them found by the parity case rather than by
reading.

**`CheckpointOption` validated its kind against a hand-written pair** - Extract or Descend - so the swarm's first Press
on threw out of the scene. It reads the enum now. The rule, the room and the choice all worked in the simulation; only
the scene knew about that list.

**The room asked the same question forever.** Answering cleared the flag that said a question was open, but the wave had
still not advanced, so the next frame asked again; the scene sat on wave one until the test's deadline. A second flag
now says the question has been answered *for the wave on the floor*, and a new wave clears it.

**A question cost a frame the simulation does not have.** Advancing on the frame after the answer would have given the
hero's weapon one more frame of cooldown between waves than every other room gives it. `ResumeSwarm` advances inside the
answer instead, so asking costs frozen time and nothing else.

## Limits, and what is deliberately unfinished

- **The exit is offered after every wave.** That is the plainest reading of "leave when you like" and it is one
  constant, `SwarmRule.ExitEveryWaves`. Whether it nags is a question about feel that no simulation can answer; it is
  the first thing to ask the owner after playing.
- **A swarm room has one chest, because chests are per room.** A twenty-six-wave stay passes one chest, so S3's books
  barely reach a swarm - the median swarm run took none. Chests that arrive with the waves are a real gap and belong
  with S6 or a slice of their own.
- **Nothing in the authored Descent is a swarm yet.** The room exists, is measured and can be played through the
  development hook; where swarm rooms belong in a run is S6's decision.
- **Three scripted policies, not a player.** "Back off when something is inside reach" is a good player and a poor one
  at once: it never retreats early and never commits. What a person does with a leave prompt is unknown until the owner
  plays it.
- Sixteen seeds a cell on the pure .NET runner, one ladder, no relics, nothing on a device. The policies press the
  Forge Burst whenever it is ready; a player who forgets it fights a harder room, and the scene parity case - which has
  nobody to tap the button - reaches wave seven where a policy with the burst reaches eight.
- The ramp is linear because a linear ramp is one number to argue about. Whether a curve that flattens or steepens is
  better is untested.
