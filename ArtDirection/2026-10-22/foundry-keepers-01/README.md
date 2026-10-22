# Foundry bridge keepers at the concept's scale

Verified 2026-10-22. Baseline `b4662882f76552ed4c5423b90c24e516251511ef`.

The approved concept flanks the bridge head with two large, dark hooded keepers on stone blocks - about four heroes
tall. The runtime keepers were the same painted statue (`ForgeKeeper`, a 2:3 card) at 3.8 high, pale, on a low seat,
and could not grow where they stood: their card already reached the bridge's walking width on screen (the PlayMode
guardian test requires every card corner outboard of it).

- Posts moved out and down the bridge: `BridgeHeadD` 6.8 -> 7.6 from the axis (so a larger card stays outboard of the
  walking width on screen: inner edge 3.57 against 3.54) and `BridgeHeadS` 3.3 -> 5 before the west seam (so the west
  keeper's plinth stays clear of the court's south rim; the first try at 4.1 put its corner .33 onto the court and the
  scenic-box test caught it).
- Card height 3.8 -> 5.4 (`KeeperHeight`), tinted (.64, .62, .70) toward the concept's dark iron.
- A plinth block (`KeeperPlinth` .62 high, half-size `KeeperPlinthHalf` 1.08) under a seat scaled with the card
  (`KeeperSeatHalf` 1.0), so the keeper stands on a block as on the concept; the seat and card foot are lifted with it.
  The below-deck console and the hanging banners follow the post (console to `BridgeHeadD`+.8, banners from +.85).
- Depth footprints (scope and landmark) and the balustrade's post box use the plinth's half-size and the new height.

Region 38 renderers / 159,317 vertices (+312).

Images: `before/`, `after/` (36 matched views; new: `bridge-head`), `compare-concept.png` (map-scale before / after /
concept). Tests: the bridge architecture test now checks the keepers' plinth footprints (1.08, was .7) outside the
walking union; the PlayMode guardian-card test (cards outboard of the walking width, one shared batch, baked lighting
orientation) passes unchanged.

Verification: 457 .NET / 737 EditMode / 164 PlayMode, zero failures; Verify-Project PASS; APK 36,458,659 bytes, SHA256
A7266655680FEB6CECCD4E749D0120FDEC8DF5B861B313F01DC9B39FFD27E07A, NOT INSTALLED (the owner is using the phone). Source
freeze `a9b57bed480ca872570f0250bffe95dccf6fb5ac63e1e087c7324e6627d00139` (1,269 files) taken after the final After capture,
matched after the full suites. Gameplay: none - the keepers stand outside the walking union.
