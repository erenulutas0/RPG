# Walked finale exit — visual proof, 2026-09-23

Read docs/35, docs/36 and ExitRule.cs. This is the final swarm's existing exit, not a new Descent room transition. The authority is ExitRule.Radius0.9 and DwellSeconds1.0; walking across does not complete it. EncounterController exposes ExitFloorX/Y and ExitHeldSeconds. The visual must never independently complete or bank a run.

## Direction

Low brass boundary with four inward corner marks and a pale gateway/arrow glyph, sitting flush to the floor. A thin mint progress contour follows the same boundary; no pillar, immunity bubble, bloom, persistent particles or interact button. The angular doorway gives a shape distinction from the blue ability circle even in grayscale. A tall painted portal was rejected because it would obscure combat; precise ground geometry is authored in code rather than generated as a bitmap.

The browser uses the prior September19 flattened Unity capture at width9 (120pixels/unit), with a provisional centre at540,900 representing the farward5floor-unit exit. This is compositing, not a current finale capture or camera registration proof. The old enemies are baked into the background, so the overlay can draw over them; runtime must draw the seal below actors. The hero shown is not actually holding the exit; controls simulate its visual state.

## Checked

Playwright: page has no JavaScript errors; holds freeze during example pause, interruption resets to0, normal-room state hides the marker;390px viewport has no horizontal overflow. Desktop screenshot visually reviewed. Reduced-motion users receive a static completed example. Source is index.html and preview.js, no generated raster asset or imagegen prompt. No Assets, gameplay, APK or phone changes in this slice.

## Runtime slice next

Create an observing exit view at the actual ExitFloorX/Y only while IsInSwarmRoom and the run has not ended. Reuse the project's shared contour/glyph approach and ground sorting beneath actors; derive scale from ExitRule.Radius, never artwork padding. Use a reusable mesh for the progress arc, driven directly by clamp(ExitHeldSeconds / DwellSeconds), without a second timer. Lifecycle must hide/reset on room end/reload and leave game ownership intact.

Moving-inside wording requires exact movement state, not guessing from Held==0 alone. Bind existing authoritative motion state or use neutral instructional text until it can be read accurately. Translate project-facing text through existing text assets. Add a viewport-clamped directional cue for an off-screen exit in the same bounded integration; this browser does not implement or prove that cue. Do not relocate the exit.

Verify real finale versus Descent visibility, floor-space radius, traversing without exit, stationary1second hold, interrupted reset, pause, exact banking authority, camera-edge cue, actor occlusion and scene reload. Produce actual Unity captures, run required .NET/compile/EditMode/PlayMode/integrity/build gate, then coordinate device review. Existing scene/simulation parity stays unchanged. Installed phone build remains XP-only APK1F00CF65; inspect fresh state before any interaction.
