# Foundry layout 01 — art-first region shaping

**Superseded placement authority:** Owner now explicitly requests the photograph structure. The old diagram is historical. Read the newest foundry-playable-01 README for the broad court, inset shaft, straight south bridge and northeast gate implemented in the shared runtime geometry.

Owner deferred Opus and asked to shape the map before gameplay delegation. OPUS_EXPLORATION_TASK.md remains drafted but explicitly on hold. No agent or external task has been started.

## Layout proposal

One connected irregular region with five identities: entry court, central casting square, salvage side court, furnace passage and boss approach. The boss arena is separate and not drawn as another ordinary room. The salvage court connects twice so the region has a short optional loop. The two links are6units deep; the main passage is8units wide. This deliberately avoids single-file bridges that undermine kiting. The central20x22 square is the largest clear battle space. Perimeter machinery and apparent vertical depth stay outside routes. No walkable stairs/elevations in this proposal.

index.html contains the exact proposed rectangular surface union. Coordinates are design-plane units, NOT a new Unity ArenaGeometry or installed collision boundary. Entry(-4,-20)-(4,-10); square(-10,-10)-(10,12); salvage(-22,-6)-(-12,12); passage(4,10)-(12,22); approach(0,20)-(16,30). Side links(-14,-6)-(-8,0) and(-14,6)-(-8,12). Coordinates are rotated45degrees then vertically compressed in the isometric illustration. This is different from directly using current game floor X/Y, so convert explicitly if adopting the plan.

The surface union is flat, connected and concave, with a recessed nonwalkable hole between the two side links. Entry/square meet along an8unit edge. Proposed rewards and battle identities are placeholders, not activated sites or balance data. The furnace landmark is scenery, not the existing Relic Forge service.

## Camera and art

Interactive overview switches top-down/isometric and selects zones; a portrait schematic follows the selected point with fixed scale. Overview dragging accepts only points in the surface union. Phone width12 in this design-space projection is illustrative and does NOT change the current runtime camera width9. No runtime movement, stamina, enemies or collision are simulated here.

Painted concept foundry-v1.png is atmosphere/silhouette reference. It is not an authoritative collision map, guaranteed exact match to the schematic, production texture or screenshot. Keep its source intact and exact prompt in PROMPT.md. Use dark stone, subdued brass, differentiated paving and perimeter industrial silhouettes; no dense central props or pervasive bright lava. Avoid baking the complete concept into one runtime image.

## Production handoff after layout review

1. Validate space and route readability in an isolated greybox, keeping the original hall and character scale.
2. Current ArenaGeometry is convex: this concave union/hole cannot be installed by merely replacing a floor sprite. A shared traversability boundary and movement/spawn/spacing/simulation tests are required before portraying the holes as real gameplay obstacles. Do not silently let actors walk across painted voids.
3. Produce reusable floor patches, inside/outside corners, sidewalls, edge caps, furnaces, crane silhouette and boss gate as independent assets. Match pivots, floor sampling and depth sorting.
4. Review front/side occlusion and the actual phone HUD. Real ramps, elevation, pathfinding and encounter activation remain separate future work.

No Assets, Gameplay, APK or device changes in this concept slice. Prior furnace clearance change remains uninstalled; current installed APK remains C8B4592C03EF35A437BF7EB027B54D484DD333C48946EEF212D0B793D9F12704.

## 2026-09-23 — Art-first foundry region layout

Owner explicitly deferred Opus and approved shaping the map first. New ArtDirection/2026-09-23/foundry-layout-01 contains an interactive top-down/isometric surface proposal, fixed-scale schematic portrait camera and one untouched built-in image_gen concept. Five identities: entry, large casting square, two-connected salvage court, broad furnace passage and separate boss approach. Routes are flat; concave outline and side hole are proposed only, not current ArenaGeometry.

Browser verified at1280 and390 widths: five selectors, isometric pointer-to-floor conversion, images, no overflow or script errors. Desktop screenshot inspected. Exact image prompt in PROMPT.md; source SHA25698F7D14F6B3E3634A2DCA3D8AABF3910B5883C608244CE4E4868B6862F5C5C5D. Painted concept includes a few entrance steps and differs in exact geometry; those are not approved elevation mechanics. Diagram is placement authority, concept is atmosphere, neither is runtime collision. Design-space isometric coordinates require explicit conversion; schematic camera is not the runtime9-unit framing.

No Assets, runtime, APK, device or tests changed in this concept slice. Current installed C8 build and uninstalled furnace-clearance change remain as recorded. Opus handoff marked ON HOLD, not sent or started. Next: review region shape then isolated traversability/greybox and modular art; concave geometry cannot be installed as only a painted floor.
