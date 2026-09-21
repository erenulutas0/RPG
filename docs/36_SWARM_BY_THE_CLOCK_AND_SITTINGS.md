# The swarm by the clock, and level-ups in sittings — 2026-09-21

Two changes the owner chose after the first phone run of the continuous swarm (`35`), decisions 7 and 8 in `27`.

**The crowd hardens with time.** The ladder was a queue: every spawn took the next enemy in line, so the crowd's make-up
advanced with every kill, and a hero who killed fast met the last wave - a Warden and a Tank in every ten - within half a
minute. Now each of the pack's ten places has a **role** in each wave (place *i* is that wave's *i*-th enemy), the wave is
the room's clock - one every `SwarmRule.LadderSeconds`, the last for as long as the room lasts - and a place that opens or
fills again takes its role in the wave the clock is on. What stands on the floor at a given minute no longer depends on
how fast the hero kills. A side effect of roles: there is at most one Warden and one Tank alive at a time, where the queue
could stack them.

**Level-ups come in sittings.** In a swarm room `UpgradeService` holds pending level-ups; every
`SwarmRule.OfferEverySeconds` of the room's clock a sitting offers all of them one after another, and the hold resumes.
The cards, the seed's draws and what a choice applies are exactly what an immediate offer would have been; only the
moment changes. Leaving the room releases whatever is still held. The Descent is untouched - nothing is held outside a
swarm - and the scene and the simulation open a sitting on the same frame, which the parity case standing in the swarm
now exercises (three cards taken in sittings, identical in both).

## The criteria, written before the sweep

1. **The finale stays where `35` put it**: entered at the end of the Descent, the calibrated player meets `35`'s four
   criteria - ninety-fifth percentile under eight minutes, the Staff longest, the Staff's median between 150 and 240
   seconds, at most one run in four dead inside the first minute.
2. **The ladder's pace changes only the coupling, not the tuning**: `LadderSeconds` is the value at which the clock
   reaches the last wave when the queue did for the calibrated player at the finale - a median of 28.9 seconds, so 2.6.
3. **Sittings cost little**: `OfferEverySeconds` is the largest of 5, 10 and 15 seconds whose delay lowers the Staff's
   median survival by no more than a tenth against offering each level-up at once.

## The sweep

Forty-eight seeds a weapon, the calibrated player of `33`, at the end of the Descent and, for information, at level zero
as the phone reaches the room today. "Now" is a sitting every frame - no hold.

At the end of the Descent:

| Ladder s | Sittings | Median s: Sword / Staff / Daggers | Died inside 60 s | p95 s | Last wave at |
|---|---|---|---|---|---|
| 2.6 | now | 218 / 245 / 170 | 1 / 139 | 264 | 29 s |
| 2.6 | 10 s | 214 / 249 / 176 | 2 / 139 | 267 | 29 s |
| 5 | 10 s | 219 / 244 / 172 | 0 / 139 | 271 | 55 s |
| 8 | 10 s | 211 / 249 / 170 | 0 / 139 | 272 | 88 s |
| 12 | now | 202 / 242 / 174 | 0 / 139 | 267 | 132 s |
| **12** | **10 s** | **197 / 244 / 168** | **0 / 139** | **265** | **132 s** |
| 12 | 15 s | 205 / 241 / 176 | 0 / 139 | 262 | 132 s |

At level zero:

| Ladder s | Sittings | Median s: Sword / Staff / Daggers | Died inside 60 s |
|---|---|---|---|
| 2.6 | now | 36 / 224 / 35 | 99 / 144 |
| 2.6 | 10 s | 34 / **33** / 35 | 135 / 144 |
| 5 | 10 s | 43 / 216 / 47 | 107 / 144 |
| 8 | 10 s | 53 / 210 / 54 | 78 / 144 |
| 12 | now | 165 / 230 / 80 | 9 / 144 |
| **12** | **10 s** | **72 / 209 / 66** | **19 / 144** |
| 12 | 15 s | 65 / 200 / 62 | 22 / 144 |

## What it says

**At the end of the Descent neither constant moves the finale.** Across every ladder pace from 2.6 to twelve seconds and
every sitting interval up to fifteen, the Staff's median sits between 239 and 249 seconds. A hero with a build kills
what is in front of it whatever it is; when the heavies arrive and when its cards land are details to it.

**At level zero they decide everything.** A ladder that reaches its last wave in half a minute kills a level-zero Sword
or Daggers there, and held cards on top of it kill the Staff too - 33 seconds, from 224. A ladder that takes two minutes
leaves the opening small for long enough to build in: at twelve seconds all four of `35`'s criteria pass even at level
zero.

## Where the criteria were not met as written

- **Criterion two's premise failed.** It chose 2.6 seconds to keep the room's tuning, assuming the ladder's pace was part
  of that tuning. At the end of the Descent it is not - 2.6 and twelve give the same finale - so the rule preserves
  nothing. The ladder ships at **twelve seconds** instead, for two stated reasons: it is the only pace at which the room
  is survivable for a hero without a build, which is how the phone reaches it until S6d; and it makes the crowd's
  make-up something the player lives through - the heavies arrive at two minutes twelve, in the second half of a median
  run - rather than something settled in the first half-minute. This is a choice made after the sweep, and it is
  recorded as one.
- **Criterion one is missed by four seconds.** The Staff's median at the end of the Descent is 244, above the window's
  240, and it is above it at every setting swept: the role structure made the finale slightly easier (202 under the
  queue in `35`), probably because the heavies can no longer stack. The owner's first run called the room much too
  hard, so the ramp is left alone until he has played the finale where it belongs.

Criterion three chose **ten seconds**: at level zero with a twelve-second ladder, sittings every ten seconds cost the
Staff 8.9 % and every fifteen 12.7 %; at the end of the Descent no interval costs anything measurable. At the owner's
pace of levelling that is about three cards a sitting, and a panel every ten seconds instead of every three and a half.

## The exit, re-checked

The criterion of `35` still holds, less sharply because the room is gentler: a hero who decides to leave by two minutes
always gets out, one who decides at three gets out 72 % of the time, at four 24 %. Distance is still not the lever -
three, five and seven units give the same curve within three points.

## Limits

- The sweep cannot say what sittings feel like, only what they cost. Whether three cards at once reads as a reward or a
  chore is the owner's to judge on the phone.
- The HUD says nothing while cards are held. A pending count, or a clock to the next sitting, is Astra's.
- One ladder, one arena, the pure .NET runner; the calibrated player still underestimates the owner on average (`33`),
  though his first continuous run was below it (`35`).
