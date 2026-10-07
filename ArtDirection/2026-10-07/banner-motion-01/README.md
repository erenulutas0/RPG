# Bridge banner motion â€” 2026-10-07

Two existing bridge-head banners use six vertical rows per each of four texture columns. Their shared top rod coordinates stay fixed, with a squared falloff toward the hem and a slow travelling sine wave. Duplicate edge vertices use the same position-dependent drop and banner phase to keep seams closed. UVs, vertex shading and structure Z are preserved. Max horizontal displacement is 0.115 world units, vertical 0.025; 0.15 padding per bound side covers both.

FoundryBannerMotion borrows the existing facade batch (which also includes stationary facade art). Cached arrays are uploaded at up to 30Hz; this is not free or shader-only motion. No new texture/material/renderer. Two existing scenery batches now animate: emission for fire, facade for bridge cloth. Added 160 vertices for the cloth bend. Mesh ownership remains with the region; pause freezes, disable restores, re-enable resumes.

Scope: bridge-head cloth only. Banners painted into the gate facade remain still. This adds local life to the entrance; it does not resolve the broader concept-to-runtime fidelity gap.

Desktop validation: compile check 0 warnings/errors; graphics capture 1 passed. The actual map asserts one cloth component in the shared facade batch, vertex motion and pause freeze. Rest and eight actual Unity frames were visually reviewed. Geometry measured at 17 renderers /80,504 vertices. Preview HTTP200. Full test suites, Android build and device closure pending.
