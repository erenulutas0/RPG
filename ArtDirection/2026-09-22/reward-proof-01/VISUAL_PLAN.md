# Reward and finale visual plan — 2026-09-22

Status: first reward source and browser proof only. Proposed visual timings and sizes below are not gameplay constants or accepted runtime assets. The owner's September 22 handoff and docs/35–37 supersede the older art handoff's gameplay state.

## 1. Ground rewards — current slice

One compact painted cyan experience crystal with two round brass coins at its base, one pickup silhouette. Shape as well as colour distinguishes experience from gold. This depicts the single combined GroundDrops.Drop; it must never become two separately collectible objects.

- Source: unchanged 1254-square RGBA reward-v1.png, generated through built-in image_gen. Fixed provisional floor root (636,1122) in top-left source coordinates. Strong alpha bounds x219–1053, y138–1121; preserve weak alpha rather than thresholding it away.
- Import candidate: full canvas uniformly sampled to 96x96, 128 PPU, bilinear, no mipmaps, non-readable, Android ASTC4. Full-canvas pivot (636/1254,132/1254). At natural scale the visible width would be about 0.50 world units; use a constant view scale to reach the preferred browser candidate 0.45. Do not change physics to match padding.
- Compare 0.35–0.60 visible world-unit widths against the width-9 camera, initially prefer 0.45 (54 physical pixels at a 1080-wide capture). The footprint stays far smaller than the 1-unit pickup radius. No idle bob, rotating coin, permanent halo or per-drop label. Contact shadow is separate, tight and dim.
- One painted frame, no generated animation sheet. Drop: stationary at recorded floor coordinates. Pickup candidate: 0.22s travel/shrink/fade after Collecting. End-of-floor candidate: 0.42s travel. One restrained 0.18s contact accent, aggregated for simultaneous rewards; never a full-screen flash.
- Collecting fires before run XP/gold mutation and offers may pause the game that same frame. Payment remains immediate and authoritative; cosmetic flight must not postpone XP, gold or cards. Use scaled time; freeze under pause/choice and resume or clear on scene teardown. Browser timing is not evidence of Unity pause behaviour.
- Reward components with zero XP or zero gold require distinct component presentation before this combined master is treated as general-purpose; current browser proof only represents a drop carrying both. Do not paint a misleading empty-currency reward.
- Current API has value-only drops, no stable ID or sweep flag. Runtime should subscribe early, retain insertion-ordered visual entries including duplicate coordinates, remove the first matching entry on Collecting, and reconcile remaining entries on Changed. It must not invent an ID from position or change game events just for art. Derive a longer *visual* flight for more distant pickups or use one duration if no reliable sweep context is available; no new collection logic.
- Sorting must put a drop at its floor root among actors and props using GroundedSorting. The browser overlay cannot validate this: its background is a flattened previous Unity render.
- Pool renderer objects and borrow one immutable imported sprite. Never cap/hide uncollected drops to meet the VFX budget; active cosmetic flights can be merged/capped without losing rewards. No texture generation per kill.

Player takeaway: “Walk over this small blue-and-gold object to receive the kill's rewards.”

## 2. Walked exit — next visual slice

A low-profile brass extraction seal on the floor: four inward corner brackets and a central upward gateway glyph. Cool ivory/teal, distinct from the blue ability ring and violet Staff impact. No tall portal obscuring enemies. Mark the exact ExitFloorX/ExitFloorY; contour radius follows ExitRule's 0.9, independently of transparent padding and camera projection.

- Extend the project's deterministic ring/glyph geometry rather than asking a painted bitmap to define a precise gameplay radius. One 256x128 ground plate/contour candidate, one dynamic fill mesh (or existing UI-independent radial implementation). Two 64-square glyph candidates if needed: gateway and stationary footprints. No painted frame sequence required.
- States: visible/idle; inside but moving (still empty, “Stand still to leave”); stopped (exact clamp of ExitHeldSeconds / ExitRule.DwellSeconds); interrupted (reset exactly with game state); room ended (hide). Do not infer exit success from a visual timer.
- Thin directional cue within the combat viewport if the exit is off-screen; never relocate the real exit to make it visible. No new input surface or interact button.
- Runtime verification must include moving across the seal without leaving, a true 1-second stationary hold, interrupted hold, pause, front/back occlusion, final exit and absence in Descent.

Player takeaway: “This is the way out. Stop here briefly to bank the run.”

## 3. Finale clock and pressure — after exit

Reuse the compact upper HUD, with a clearly labelled elapsed time and current swarm ramp multiplier (example typography only: FINAL / 01:45 / PRESSURE x2.00). The ramp step can be a secondary labelled value for the first proof; avoid calling it a wave because the pack is continuous and the ladder clock differs from the ramp clock.

- Reuse existing panel/font. One small 48x48 code-native clock or pressure glyph if it helps; no generated HUD sheet.
- Show SwarmSeconds and the authoritative SwarmDifficulty. Clarify in the tooltip/help copy that this is current spawn pressure; existing enemies retain their spawn difficulty and it is not necessarily a multiplier on every living enemy. Evolution's HeroStats.Difficulty is separate; do not silently multiply or relabel it.
- Brief restrained highlight on SwarmStepped, no permanent flashing. Hidden outside the swarm. Size candidate: 28–32 reference pixels at the 1080 HUD reference width, to be sampled at both portrait aspect ratios.

Player takeaway: “Time here is raising the pressure; decide when to leave.”

## 4. Pending upgrade cards — after finale readout

A small stacked-card glyph plus exact pending count beside the XP row, visible only while the swarm is holding choices and PendingUpgrades is positive. It is a status indicator, not a tappable choice button.

- One 64x64 code-native stacked-card icon, same brass border style as current cards. Count is live text. No animation frames.
- A subtle one-shot emphasis on count increase; preserve readability in grayscale. No invented countdown unless the real sitting clock is exposed.
- Distinguish pending count from offered cards, remaining picks and level number. Hide when the queue is consumed or the run ends; test multi-choice sittings and release on leaving the swarm.

Player takeaway: “You earned choices; they will be offered together shortly.”

## Integration gates

Read the full current project verification instructions before runtime edits. Smallest runtime slice is ground drops only: optional art definition/imports, presentation subscriber at CombatSetup, pooled views, focused presentation tests, actual Unity proof, then full required gate/build and coordinated phone review. No gameplay constants, profile schema, Packages lock or ProjectSettings changes. Preserve .meta GUIDs. Verify ordinary collection, exact coincident drops, repeated floor transitions, death (no false pickup), card pause, restart cleanup, shared asset ownership and eventual floor sweep. Run existing simulation parity tests unchanged. Re-inspect current device/profile state before any device operation; the old September 19 build is not assumed current.
