# Foundry parallel pass — 2026-09-30

Six changes made in parallel (five helper agents, each owning its own files; Unity was run once, serially, afterwards).
Matched actual Unity captures: `../foundry-rim-01/after/` (before) and `after/`.

## What changed

- **Gate facade texture** (`Art/Props/Foundry/GateFacade.png.meta`: Default and Android `maxTextureSize` 1024 -> 2048,
  `nPOTScale` None -> ToLarger; same GUID, nothing else changed). The Android build report showed why the gate was soft
  and costly: the 1536x1024 source is not a power of two, and non-power-of-two textures here ship **uncompressed**
  (RGBA32), not ASTC. At the old 1024 cap it shipped as 1024x683 RGBA32 (~3.6 MB); with only the cap raised it was
  1536x1024 RGBA32 (8.0 MB). Scaled to 2048x1024 it now ships as ASTC 6x6: **1.2 MB**, sharper than before and ~2.4 MB
  less memory than the old import. At the gameplay camera (`point-4`) the visible gain is small.
- **Court seal** (`FoundryRegionHeraldry.cs`): the faint grey ring with dark wedges became a brass-lined eight-point
  star (major points screen-up/down/left/right, tips r 3.12; minor points r 2.05) on a darkened stone field, with a
  brass ring pair at r 3.44 and 3.82 and lozenges tying the rings to the compass rays. Same floor-decal batch. See
  `start.png`.
- **Bridge deck** (`FoundryRegionView.cs` `BuildBridgeDeck`, `FoundryRegionSite.cs` bridge joints): the flat slab
  became coursed stone — squarish setts in the outer quarters, large staggered flags in the centre half, per-stone
  tone, joints and bevels like the court floor; lane joints sit under the existing brass lanes; transverse joint bands
  reduced from 12 to 3 and aligned with course joints. Walking quad unchanged. See `point-0.png`, `bridge-detail.png`.
- **Chains** (`FoundryRegionDressing.cs` `Chain`): alternating face-on rings and edge-on bars, thicker and larger,
  near-black iron with a warm bronze lit rim. See `undercroft-detail.png`, `overview.png`.
- **Pressure reservoirs** (`FoundryRegionDressing.cs`): one riveted plate per quarter turn instead of one full panel
  per facet, riveted belts, per-facet-edge shading. They no longer read as barrels. See `furnace-detail.png`.
- **Boilers and piers** (`FoundryRegionMassing.cs`): iron courses map one plate per two or three facets with staggered
  seams and riveted hoops; the six bastion piers now differ (banded, buttressed, stepped; feet -16.8 to -20.8).
  South boilers stay squat.
- **Loot silhouettes** (`FoundryVisibilityView.cs`, `GroundDropView.cs`, test): ground drops hidden behind a structure
  get a gold silhouette from a second shared material of the x-ray shader. Not visible in these captures (no drop
  behind a structure); covered by `FoundryRegionTests`.

## Budget and gate

- Static budget: 17 renderers (unchanged, limit unchanged), 54,168 → 58,692 vertices (+4,524: bridge ~+3.5k, seal
  ~+650, piers ~+340, chains ~+80).
- Full gate: EditMode 405/405, PlayMode 130/130, Android build exit 0; integrity 520 / 1,099 / 568 passed. Build
  report textures 20.5 MB. APK 39,339,970 bytes, SHA-256
  `C1E500CD0301A2AACEECA4588F611D1972443B4FC98A8F7D446CDC1A2D7CECBE`, **built, not installed** (the owner's run is
  open on the phone).
- The captures in `after/` were taken before the `nPOTScale` change; its only visual effect is the gate's resampling.

## Limits

- The bridge's stones are axis-aligned on screen (the bridge runs vertically), so at a glance the deck can read a
  little like coursed wall masonry; the hero and the rails make it read as floor in play.
- Chests are actor roots without children, so the depth pass never moves them: a chest still draws over every
  structure, and has no silhouette. Pre-existing; not changed here.
- `FurnaceFacade.png` (1024x1536) has the same non-power-of-two problem: 3.6 MB RGBA32 in the build. Not changed.
- The gate is still a painted facade; 2048 import only recovers resolution the source already had.
- Pier ledges and edge strips are thin at overview scale.
