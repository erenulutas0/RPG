# Remaining art audit — 2026-10-06

Read-only audit; no Unity or phone commands, runtime edits, or new measurements. Sources: docs/38 sections 8–18, latest docs/08 entries, actual `foundry-structure-02/after/overview.png`, and `foundry-identity-01/device-gate-after.png`. The latest installed baseline is the structural package, 17 static renderers / 80,316 vertices. Historical S23 results are not measurements of the next package.

## Assessment

The map now has the reference's broad landmarks: bridge, suspended court, left pit, crane, boiler yards, large north gate, undercroft and chains. This is substantial progress. The remaining mismatch is primarily material and compositional cohesion. More bolts, straps and small supports have diminishing benefit at portrait play size.

The runtime overview is a very long, regularly paved rectangle with repeated orange lantern points. The reference is more irregular, with localized occupation/wear and stronger hierarchy between its gateway, pit, paths and background. Changing the rectangular footprint is gameplay/map work and is outside this art-only pass. Existing floor ashlar already varies block dimensions and tone; do not claim that it lacks any variation, or replace it with another uniformly noisy texture.

The background's large, bright molten columns compete with the much smaller foreground crane and stone supports. The foreground gate has a convincingly painted door, but surrounding masonry is visibly faceted and its texture orientation varies. Those are broad-surface issues; another layer of hardware will not resolve them.

## Highest-value bounded options

1. **Material/value cohesion at existing footprint.** Review foreground masonry caps, frontal walls and iron roofs together at the actual portrait camera. Preserve separate material identities, but reduce conspicuous value discontinuities that make adjacent parts seem assembled from unrelated assets. Candidate files: `FoundryRegionWeathering.cs`, `FoundryRegionArchitecture.cs`, and existing massing helpers. Use current material batches and vertex colours; avoid global brightness increases that weaken actor contrast.
2. **Localized occupation on the floor.** If a floor change is chosen, prefer a few broad, low-amplitude tone fields near workshop/boiler thresholds and a lightly worn approach to the gate. Reuse existing `PavingVertex`/`FloorLight` sampling; no raised rubble, dark holes, new collision or uniformly scattered crack marks. The current `AshlarFlag` already includes replacement stones and corner repairs. Keep the central combat area quiet and the brass routes readable. Compare at portrait scale before accepting: variation invisible except in closeup is insufficient reason to add geometry.
3. **Backdrop separation, cautiously.** A small foundry-only tint adjustment on the existing cavern sprite could subordinate the outer towers without a new renderer. `VoidBackdropView.Build` currently creates `CavernRenderer` with default white colour; `FrameCavern` only places/scales it. Any adjustment must be foundry-scoped because the backdrop component is shared. Do not simply darken everything: preserve the cavern's depth and molten landmarks, and compare the bridge/perimeter as well as the court. This is an experiment, not a required change.

## Tall landmark framing: concrete next proof, no runtime camera change

The phone gate image confirms a framing problem: the hero and central gate seal are visible, but the high pediment is hidden by the top HUD and one tower is clipped above the frame. The gate is not missing; it is taller than the visible play band at close approach. Raising the gate, adding more top detail, or brightening the obscured part does not solve this.

Add a **test-only three-distance portrait gate sheet** to the existing capture workflow: design positions around `(22,6)`, `(26,6)`, `(28,6)` with the same current `ArenaCameraFollow.Frame(1080,2340,361,2340-427)`, HUD on, and unchanged camera settings. Resolve any position through the existing valid movement geometry. Existing route point 4 is `(28,6)` and can be reused; capture earlier approach points as extra evidence. Record how much of the central seal, gable and each tower remains inside the safe play band. This proves whether the complete landmark is readable during approach even though its top is cropped at the closest position.

A later deliberate camera/HUD slice may compare a mild gate approach offset, wider view, or more compact HUD. That must preserve combat threat visibility and touch targets and needs device judgement. Do not make that choice inside this art closure package merely to produce an attractive screenshot. Similarly, shrinking the whole gate to fit changes its intended monumental scale and should not be done invisibly.

## Defer and report honestly

- No phone available: build and desktop evidence only. Device installation, ASTC-on-device appearance, movement readability and performance remain pending.
- No claim that the target image is matched. It combines painted surface richness, uneven composition and lighting that the runtime still only approximates.
- Dynamic combat readability, multiple overlapping silhouettes, warm enemies beside orange pools, and moving UI-line shimmer remain more useful device checks than another static FPS sample.
- Midrange-device performance has not been established by the S23 results.
- Keep the 17-renderer cap; avoid increasing the current 80k static vertices for detail that disappears at play scale.
- New zones, routes, collision obstacles, elevation traversal, gameplay cameras and HUD redesign are separate scopes, not unfinished decoration tasks.
