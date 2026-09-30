# Foundry chest depth, forge light, bridge and furnace texture — 2026-09-30

Three helper agents (each owning its own files) plus a texture import fix and a lighting retune by the main session.
Matched actual Unity captures: `../foundry-parallel-01/after/` (before) and `after/`.

## What changed

- **FurnaceFacade texture** (`Art/Props/Foundry/FurnaceFacade.png.meta`: Default and Android `maxTextureSize`
  1024 -> 2048, `nPOTScale` None -> ToLarger; same GUID). Like the gate, the 1024x1536 source shipped as a
  non-power-of-two 683x1024 **RGBA32** (3.6 MB); it now ships as 1024x2048 ASTC 6x6, **1.2 MB**. Build-report textures
  20.5 MB -> 18.1 MB.
- **Chest depth and silhouette** (`FoundryVisibilityView.cs`, `ChestSpawner.cs`, tests). The chest is an actor root
  that is its own sprite and has no children, so the depth pass never moved it and it drew over every structure. Now a
  childless actor root moves itself to its feet depth during rendering and is restored after, like other actors'
  children; a hidden chest gets the gold loot silhouette. The sprite stays on the root because existing tests and
  capture tools look it up there. The chest has no physics body and opens by its stored floor position; its `Opened`
  event now reports that stored spot (same value) instead of reading the transform. Tests: the foundry chest moves to
  feet depth during apply, returns after restore, gets and loses the loot material; in an ordinary hall it keeps
  Sprites/Default and there is no visibility view.
- **Forge light on the floor** (`FoundryRegionFinish.cs`). The old pools were anchored on the removed square furnace
  boxes and most lanterns cast no light. The floor light now comes from the actual furnace pit, the four boilers, the
  pit collar lanterns, the court lantern posts, the balustrade lanterns and the bridge/gate beacons; open court falls
  to a cool slate. Main-session retune after the first capture (pools were half as hot and the whole court turned
  brown): saturated heat gains 2.1x, lanterns and beacons are smaller and weaker, each fire's ambient reach is 1.35x its
  radius, soft ceiling 1.75 (keeps the shader's colour under its guaranteed +/-2 range).
- **Bridge deck, second pass** (`FoundryRegionView.cs` `BuildBridgeDeck`, `FoundryRegionSite.cs`). The first pass
  laid stones in running bond along and across the walk; on screen they were 2:1 bricks continuing the sidewall below,
  so the deck read as a wall. Stones now sit on the court's diagonal grid (diamonds on screen, like every other floor),
  with finer, quieter setts in the margins and a kerb along each rail; the three cross bands are gone.

## Budget and gate

- Static budget: 17 renderers (unchanged, limit unchanged), 58,692 -> 58,612 vertices.
- Full gate: EditMode 405/405, PlayMode 130/130, Android build exit 0; integrity 520 / 1,099 / 568 passed.
- APK 39,339,790 bytes, SHA-256 `A18311F4904C9190488C6957A320BF707E67D6F3BD0C043368F032D059CD0BA0`, **built, not
  installed**: the owner's run is still open on the phone and the phone was in use.

## Limits

- No capture shows a chest or a drop behind a structure; both silhouettes are covered by tests only.
- The warm pools read sandy-yellow more than orange, and the boiler aprons are still dimmer than the old
  out-of-range glow.
- The bridge's diagonal joints read as a lattice from a distance; the margin setts are busy.
- Any future actor root without children would also be moved during rendering; today only the chest is one.
- `UI_Panel_ForgedBlank_v2.png` (a 9-slice UI sprite, 4.5 MB in the build) likely has the same uncompressed cost; not
  changed (UI, and NPOT scaling does not apply to sprites).
