# Splintering Crit — the claim that held — 2026-09-21

`30` ended with a falsified claim and a diagnosis. The Forge Pulse was supposed to be the Daggers' card and turned out to
be the Sword's, because a per-strike burst inherits the weight of a strike and the Daggers strike smallest; no radius and
no scaling changed that. The diagnosis pointed at what a light weapon's card would have to look like: gated on something
the Daggers own outright rather than on damage. This is that card, and this time the claim holds.

## The card

A **critical** strike also strikes every other living enemy within `CritSplashRule.Radius` of its target, for a share of
the crit's damage. Centred on the target, off the critical damage, and gated on the crit.

The gate is the whole design. The Daggers' authored pattern crits **every third strike** and they swing every half
second, so they arrive critting twice a second having spent nothing. The Sword and the Staff author no rhythm at all and
crit only from the chance Keen Edge grants - three stacks to crit a quarter of the time at two thirds of the cadence.
That asymmetry is in the weapon definitions, not in a tuned number, which is why it cannot be undone by tuning.

## The claim, and the arm that tests it fairly

`docs/27` S5c stated it so it could fail: *Splintering Crit must take the Daggers deeper, in relative terms, than it
takes the Sword or the Staff.*

The fair test is not the weapons as authored, because the nine-card pool sells Keen Edge to everyone and a swarm run
levels to fifty, so the Sword buys crits along the way. So every weapon is **held at the same 25 % crit chance** - about
three stacks of Keen Edge - before the comparison. Same ground as `30`: the swarm room, sixteen seeds, a backing-off
player, a two-hundred-wave cap, and a pool holding neither behavioural card.

Control medians with crits held equal and no splinter: **Sword 54, Staff 66, Daggers 18.**

| Radius | Share | Sword | Staff | Daggers |
|---|---|---|---|---|
| 1.4 | 0.4 | 60.5 (+12 %) | 66.5 (+1 %) | 43.0 (**+139 %**) |
| 1.4 | 0.7 | 75.0 (+39 %) | 67.0 (+2 %) | 56.5 (**+214 %**) |
| 1.4 | 1.2 | 76.0 (+41 %) | 66.0 (+0 %) | 57.5 (**+219 %**) |
| **2.0** | 0.4 | 65.0 (+20 %) | 67.0 (+2 %) | 47.0 (**+161 %**) |
| **2.0** | **0.7** | 65.0 (+20 %) | 67.0 (+2 %) | 55.0 (**+206 %**) |
| **2.0** | 1.2 | 69.0 (+28 %) | 69.5 (+5 %) | 56.0 (**+211 %**) |
| 2.8 | 0.4 | 62.0 (+15 %) | 68.0 (+3 %) | 63.0 (**+250 %**) |
| 2.8 | 0.7 | 64.0 (+19 %) | 66.5 (+1 %) | 47.0 (**+161 %**) |
| 2.8 | 1.2 | 69.5 (+29 %) | 68.5 (+4 %) | 51.5 (**+186 %**) |

**The Daggers gain most in nine cells out of nine**, by seven to ten times what the Sword gains and by fifty times what
the Staff gains. Unlike the Pulse, the result does not turn on the radius: shrink it to 1.4 or widen it to 2.8 and the
ordering is the same. That is what a structural gate buys.

Run with the weapons as authored - the arm where the Sword only crits if the pool happens to sell it one - the Daggers
still win every cell: +216 %, +268 % and +348 % against the Sword's +206 %, +197 % and +190 %. The held-equal arm above is
the honest one, and it is also the harsher one for this card.

## What it does not do

It does not make the Daggers the best weapon. At the shipped setting they reach wave 55 against the Staff's 67 and the
Sword's 65 - the card closes most of a gap that was 18 against 66 and leaves them slightly behind. That is the intended
shape: the Daggers were unplayable in a swarm and are now a choice.

And it barely touches the Staff, +2 % at the shipped setting, even though a splinter and the Staff's own splash both land
on the same enemies by design. The overlap that `docs/27` flagged as the way this card could turn out to be the Staff's
did not matter, because the Staff was already clearing the crowd it would have splintered into.

## What shipped

**Radius 2.0, one stack, tiers 0.4 / 0.6 / 1.0** of the crit's damage. Radius 2.0 because it holds the Sword to +20 %
where 1.4 lets it reach +39 %, and because 2.8 is noisy without being stronger. One stack because a share of one is
already the knee of the curve - 0.7 to 1.2 buys the Daggers five more waves - and two stacks would put a full card
outside anything measured. Eleventh in the authored order, behind the Forge Pulse, so `UpgradePool` opens it only to a
run that has levelled far past a Descent; nothing about the Descent moves.

Six contract clauses, each pinned by a test: only a crit splinters, from the rhythm or from a roll; a splinter is not a
strike and cannot splinter again; it never reports itself critical; it is dealt off the crit damage rather than the plain
damage; it never strikes the enemy the crit already hit; and an area weapon's splash and a splinter both land.

## Limits

- Sixteen seeds a cell on the pure .NET runner, one swarm ladder, one player policy, no relics, nothing on a device.
- 25 % is a stand-in for "the run bought Keen Edge". A real run buys it at some point rather than starting with it, and
  when it buys it changes how the two cards interact. Unmeasured.
- The two behavioural cards were measured **separately**. What a run carrying both does - a Sword with a Pulse and a
  Dagger-style crit build - is not in either document, and with `UpgradePool` opening both by level forty-two a finale
  can hold them at once.
- The splinter is **silent**, like the Pulse and like a book. Three invisible mechanics is now the largest thing waiting
  on Astra.
