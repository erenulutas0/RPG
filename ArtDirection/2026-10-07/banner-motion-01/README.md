# Bridge banner motion â€” 2026-10-07

Two existing bridge-head banners use six vertical rows per each of four texture columns. Their shared top rod coordinates stay fixed, with a squared falloff toward the hem and a slow travelling sine wave. Duplicate edge vertices use the same position-dependent drop and banner phase to keep seams closed. UVs, vertex shading and structure Z are preserved. Max horizontal displacement is 0.115 world units, vertical 0.025; 0.15 padding per bound side covers both.

FoundryBannerMotion borrows the existing facade batch (which also includes stationary facade art). Cached arrays are uploaded at up to 30Hz; this is not free or shader-only motion. No new texture/material/renderer. Two existing scenery batches now animate: emission for fire, facade for bridge cloth. Added 160 vertices for the cloth bend. Mesh ownership remains with the region; pause freezes, disable restores, re-enable resumes.

Scope: bridge-head cloth only. Banners painted into the gate facade remain still. This adds local life to the entrance; it does not resolve the broader concept-to-runtime fidelity gap.

Desktop validation: compile check 0 warnings/errors; graphics capture 1 passed. The actual map asserts one cloth component in the shared facade batch, vertex motion and pause freeze. Rest and eight actual Unity frames were visually reviewed. Geometry measured at 17 renderers /80,504 vertices. Preview HTTP200. Full suites and device closure completed below.


## 2026-10-07 - Bridge banner motion installed

Two bridge-head banners now use six rows per texture column for gentle cloth curvature. FoundryBannerMotion tags cloth through the existing facade batch, keeps rods/untagged faces/structure Z fixed and uses identical drop/phase at duplicated seams. Scaled time freezes on pause; disable restores, re-enable resumes. Cached arrays upload the existing facade vertex buffer at up to30Hz. Max sway0.115 /vertical0.025, padded bounds0.15 per side. Region owns mesh. No new texture/material/renderer:17 renderers /80,504vertices (+160). Two existing region batches now dynamic (flame emission and cloth facade); gate-painted banners remain static. This is local entrance motion, not broad reference-fidelity completion.

Proof ArtDirection/2026-10-07/banner-motion-01: rest +8 actual Unity frames, two guarded S23 bridge poses. Compile0warnings/errors; graphics1passed (one actual cloth batch, motion and pause assertions); Edit410/410, Play145/145, Android0; integrity541/1099/568. Installed APK376E38C2E1F9F53B06F2DB00DB6B1EAA3ACFF43420F717B7C44BECD05755D8A8,31,113,605bytes; base.apk SHA256 matched. Short staged S23 bridge windows t92-107:59.9fps, p50/p95 16.7ms, max17.1ms, GPU3.6ms. Not dense combat, midrange testing or an isolated before/after cost measurement. No managed exception/crash pattern in sampled process log.

Fresh TestResults/banner-device-20261007 backup; profile/bak and three original development files SHA256-identical. Temporary depth-proof/render-stats removed, owner arrival opt-in retained, app stopped. No push. Further work should address broad silhouette/material/readability limits rather than presenting small ambient motion as reaching the concept image.
