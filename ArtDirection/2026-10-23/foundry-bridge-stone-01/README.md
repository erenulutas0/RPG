# Foundry bridge: the deck in cut stone, with the concept's diamond

Verified 2026-10-23. Baseline `884776d3920ac84f8af2a7e3ac390021526f860c`.

After the court, the seal and the processional ways took the concept's cut stone, the bridge was the last large field of
smooth fitted slabs: a central continuation of broad cross-flags (`BuildCourtBridgeStone`) between darker lattice margins
(`BuildBridgeDeck`, fitted on the V8 grain). On the concept the bridge is laid in the same small stone, with a brass
diamond set into it below the keepers.

- `BuildBridgeDeck` now lays the whole walking quad short of the court (which owns v >= South) as cut stone in the
  flagstone batch - the light lattice only - in the bridge's own field (`BridgeField`: courses across the bridge, at the
  processional ways' 1.35x density). The way's continuation down the deck (`CourtBridgeContour`) and the way's end, which
  dips onto the deck, are cut from it; the continuation is laid once in the same field. The processional way to the
  bridge also takes `BridgeField` (its axis is 4 degrees off the bridge's), so way, continuation and deck run on with no
  seam. `BuildCourtBridgeStone` and the fitted deck lattice (`DeckLattice`, `BridgeStone`, `DeckInset`, `DeckPolygon`)
  are removed. Lane inlays, kerbs and the end cap are unchanged.
- `BridgeDiamond`: a brass diamond in the court inlay batch at s = -52 between the lanes - a frame (half 1.1), an inner
  frame and a cross - square in the design plane so it reads as a diamond on screen.

No new batch or texture. Region 38 renderers / 156,729 vertices (baseline 159,317: the laid deck is lighter than the
fitted slabs).

Images: `before/`, `after/` (37 matched views; new: `bridge-deck`), `compare-concept.png` (bridge deck before / after /
concept). The after-phase floor ownership probes along the continuation each have exactly one owner.

Tests: `FoundryBridgePavingTests` - the laid deck runs in the bridge's field, stays within the bridge, covers the whole
walking quad together with the way's end, and lays the continuation exactly once from the way to the threshold;
`FoundryCourtCompassTests` follow the new owners (the deck and continuation are laid stone; the way to the bridge may
use the bridge's field).

Not done: the concept's two small kneeling keepers further down the bridge need their own figure art (only the
standing keeper exists).

Verification: 457 .NET / 737 EditMode / 164 PlayMode, zero failures; Verify-Project PASS; APK 36,454,646 bytes, SHA256
CB35D7379218BBF4C01CA2B20BB36EDF8222EC758D73BA39F3B1FB128E54EDBD, NOT INSTALLED (the owner is using the phone). Source
freeze `dfcd5dd1a1f3ca09f746cdeacab034254cdf6aaa65b625b06c375e5439436108` (1,269 files) taken after the final After capture,
matched after the full suites. Gameplay: none - the walking quad and collision are unchanged.
