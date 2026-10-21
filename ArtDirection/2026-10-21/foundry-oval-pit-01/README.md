# Foundry furnace pit: the concept's oval ring and raised corner pedestals

Verified 2026-10-21. Baseline `1b91b67ea0b680f4e0001c3bfadf71912d55ba63`.

The approved layout shows the furnace pit as an oval ring of balustrade; the runtime collar was a rounded square (corner
radius 3.2) that read as a diamond. Owner's choice (2026-10-21): the drawn ring becomes a circle, the walkable square
stays exactly as it was (`FoundryMap` pit -16..-4 / 2..14 unchanged, no movement or AI change), and the four non-walkable
corners of the square read as raised stone pedestals carrying fire instead of floor.

- Collar band: a circle of radius `PitRim` 5.69 about (-10, 8) (`PitRing`), inner edge `PitOpening` 5.45 - the old collar
  line at the mid posts. On screen it is a 2:1 oval. Lining, pool, guard runs, collar stone and post skirts all follow it
  through the same outline function; the collar stone is 24 radial 15-degree stones.
- Posts: eight on the band - the four mid posts unchanged, and .56 corner posts on the diagonals (plain rail posts with
  the brass collar and finial) whose inner corner just passes the band's inner edge.
- Corner pedestals (`PitPedestal`, `PitCorner`): in each corner, the collar stone from .62 beyond the band out to the
  hole's edge, 15 degrees either side of the diagonal, raised to .42 as one masonry block (camera-facing inner, end and
  outer faces; jointed top stones lit toward the pit); beside them the collar's step darkens toward the pedestal's end.
  The far, left and right pedestals carry the rail's tall lantern post at its former position (`PitLantern` .97, so the
  floor lamps are unchanged); the near pedestal, in front of the pit, carries a standing fire basket on a capped stone
  block so the molten pool shows past it. Each corner is one sorted piece (pedestal halves behind the band post, post,
  halves in front, fire; at the far corner the post comes last).
- Pool level: the round collar's near rail stands about .5 higher on screen than the square collar's did and hid the
  pool completely at 5.2 down (candidates 01-03), so the pool now lies 4.2 down (`PitMolten`); the shaft's courses below
  the deck keep their proportions (`PitScale` = 4.2/5.2 on the lining, wash and drum rows, furnace bays, bearing ribs,
  vessel belts and service bays; the bearing course and the first lining row are unchanged). Sparks are born inside the
  round opening (`FoundryAtmosphereView.PitOpening`).

Region 38 renderers (no new batch) / 162,336 vertices (baseline 162,104).

Images: `before/`, `after/` (31 matched views, new: `pit-close`, `pit-near-corner`, `pit-far-corner`, `approach--10-0`,
`approach--2-8`), `compare-concept.png` (before / after / the approved concept's pit). Candidates (actual Unity captures):
`01-four-lanterns` (a tall lantern on every pedestal; the near pillar stood in front of the pit's centre and the pool was
hidden), `02-near-brazier` (near basket on an iron foot that read as an open ring), `03-warm-wash` (coal wash .30 instead of
a raised pool - barely visible, reverted), `04-raised-pool` (pool 4.2 down; final but for the basket's stone block).

Tests: new `FoundryPitRingTests` (ring is a circle and a 2:1 oval; corner posts straddle the band short of the pedestals;
pedestals stay in the hole, reach its corners and carry their lanterns wholly; collar stone yields to them only in the
corners); the PlayMode spark-origin test checks the round opening. `EveryBayRetainsBroadActualOpeningsAboveItsBearingSill`
failed on the four runs beside the side corners (26-29% open at .433): there the round collar turns toward the camera and
is seen nearly edge-on, and a probe (not committed) showed the bare frame - sill and coping only, no infill - already
shows only 46-49% there, against 100% on the square-on runs; the infill keeps the same share in both (.54-.64 of the
frame). The test now requires the infill to leave more than half of what the bare frame shows on every run, and keeps
the absolute 40% rule for runs seen square-on.

Verification: 457 .NET / 736 EditMode / 164 PlayMode, zero failures; Verify-Project PASS; APK 36,418,740 bytes, SHA256
DD31BA2BF112F6EEDECF522459EBD76427CAA7BDE22AD931FFF452466C1130AA, NOT INSTALLED (the owner is using the phone). Source
freeze `315a6c0617495cce790e73bf1f551f9c8d2f44ac2ddc0cf3f649c8461cf9b34d` (1,263 files) taken before the full suites, matched
after them and after a final After capture. Gameplay: none - walkable area, collision and AI are unchanged; the corner
pedestals are visual only, standing on the already non-walkable hole.
