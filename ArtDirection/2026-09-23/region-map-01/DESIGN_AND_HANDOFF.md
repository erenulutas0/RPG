> SUPERSEDED 2026-09-23: owner chose one explorable multi-screen region with physical encounters/points of interest, not a node-selection map. Do not implement this old branch workflow. Read ../exploration-proof-01/DIRECTION.md first.

# Approved first-region direction — 2026-09-23

Owner approved: two combat areas -> choice of elite OR chest -> forge -> main boss hall. After the boss choose to leave with rewards OR enter the existing optional finale swarm. Current impressive arena is the main boss visual target; ordinary areas should give room for readable kiting. First deliver one region, not multiple universes or an open world. Names in the browser are provisional art labels.

## Current implementation versus target

FloorDefinition holds an ordered RoomDefinition array; FloorProgress walks it linearly. RunChoices arbitrates upgrades, forge and checkpoints; the scene/simulation parity tests mirror this. The current checkpoint/next-floor mechanism is useful infrastructure, but a mutually exclusive elite/chest branch is not implemented. A browser node click is not a gameplay implementation. Existing authoring, balance and telemetry must not be silently re-labelled as this new route.

## Bounded gameplay handoff (not dispatched)

1. Introduce a pure run-scoped route model with authored node IDs, allowed edges and one current node. Choosing one branch permanently excludes the sibling for that run. Resolve a room exactly once; reject stale choices, invalid edges and double taps. Keep existing authored linear floors functional until the new region is explicitly selected.
2. Add route-choice arbitration after pending combat rewards/upgrades have resolved and before starting a new room. Pause combat during the map; do not grant free healing, stamina refill or extra reward beyond existing room-boundary rules. Boss death settles pending rewards/upgrades before the optional finale decision.
3. Use existing combat/elite/forge/boss definitions as the initial content. A dedicated non-combat chest node needs explicit once-only reward ownership; current floor chests are not automatically equivalent. Fable should specify the elite/chest reward tradeoff before new rewards ship; no invented numeric reward in this art proof.
4. Mirror route choices and chest resolution in DescentSimulation. Extend telemetry with node/branch identity and preserve one run_end. Decide how existing floor-cleared banking/deepest progress maps to a region before altering persistent schema or meanings. No backend/SDK.
5. Test both paths across seeds and all weapons, stale/repeated selections, pause/background, interruption, boss extract, boss-to-swarm, swarm walk-out and death. Validate real scene/simulation parity and measure the new route length and economy rather than carrying old full-run balance targets over unchanged.

## Art work

The browser is an interactive route composition proof, not a new Unity map. Current task also integrates the existing finale exit visual independently, reading the unchanged authoritative rule. Next map integration should receive real node/current/available/completed states from the route model; never infer completion from animation. Reuse existing UI styles, large portrait touch targets and safe-area constraints. Distinct battlefield geometry/props and boss hall art follow validated encounter space; do not block kiting lanes with decorative objects.

Acceptance for the region requires a playable entry-to-boss path on both branches plus the two post-boss decisions. This remains outstanding and must not be marked complete by shipping the exit seal alone.
