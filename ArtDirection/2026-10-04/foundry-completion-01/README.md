# Foundry pending-work completion — 2026-10-04

Completed the inherited atmosphere, bridge and undercroft changes before new texture integration. The west bridge margin now reaches the deck edge after the kerb ends (s=-32); DeckLattice accepts a longitudinal window. Preserved and reviewed the inherited arch-before-pier ordering, screened iron anchors and framed furnace drain with exposed molten discharge. Gate brazier support and atmosphere colour/spawn assertions are retained.

Actual Unity capture passed 1/1. Static budget: 17 renderers / 73,244 vertices. Compare `before/` (October 1 day package) with `after/`: bridge-detail, undercroft-detail, shaft-detail, overview and point-0. The source of the molten discharge is visible in the furnace closeup. Atmosphere remains one cached dynamic mesh, now 832 vertices, updated at the existing bounded rate.

Full gate and commit status recorded below after completion. This package does not change gameplay or walkable bounds. No device install yet.

## 2026-10-04 - Pending foundry work completed

Finished the inherited pit atmosphere (20 rising sparks, heat wisps and glow), gate-brazier support checks, bridge coping/kerb corrections, arch-before-pier ordering, screened anchor plates and framed molten discharge. Extended the west bridge paving margin to d=-4 for s=-32..-24, covering the bare joint bed after the kerb ends. No gameplay, movement bounds, save or balance changes. Proof: ArtDirection/2026-10-04/foundry-completion-01 (actual Unity before/after).

Graphics capture1/1; .NET390/390; compile0warnings/errors; EditMode410/410; PlayMode134/134; Android exit0. Static17renderers/73,244vertices; atmosphere832vertices. Integrity526/1099/568 checked while gate was in progress. APK37,175,012bytes SHA25623BB5C7B4C2A51BA12A6609193B6D358E867270DD278A755486CFA2858ED0058. Built, NOT installed; latest known installed remains F6776526... from October1. Fresh read-only phone check found launcher foreground and game not running. No device inputs. New painted masters are being prepared separately; this package contains no texture changes. Commit locally only, no push.