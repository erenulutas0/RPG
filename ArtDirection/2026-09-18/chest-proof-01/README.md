# Foundry chest concept — 2026-09-18

## Transparent pose follow-up

Three unchanged built-in imagegen RGBA candidates now exist: `chest-closed-v1.png`, `chest-opening-v1.png`, `chest-open-v1.png`. All actual outputs are **1254x1254**, not the requested 1024 square. Measured alpha counts, threshold-16 bounds and SHA-256 hashes are in `asset-metadata.json`. Background alpha is genuinely zero; do not assume interior alpha is uniformly 255.

`index.html` provides a responsive checkerboard view and 72/90/108-pixel body-width comparison. All poses use the same source root (620,1080), full canvas and scale (requested body width / 788). No independent alpha-bound fitting or raster modification is performed. The ground is schematic, not a Unity capture. The open lid naturally extends beyond the lower box. The preview is a three-pose timed state change, not a smooth or production-accepted animation.

Browser review: all three images decoded successfully, the narrow in-app layout rendered without horizontal clipping, and the replay control was exercised. Small base/panel differences remain; the opening front rim shifts slightly, while the foot silhouettes are closely registered. A 90-pixel body is the initial comparison candidate, not a final runtime width. No gameplay or Unity files changed; no game tests, build, install or phone interaction in this source-art slice. Next: real Unity scene sampling, body/foot continuity and overlap review, then a bounded import/ownership integration.

The original board notes below describe the preceding stage; the transparency limitation applies to the boards, not the new individual candidates.

Art-only three-state concept, generated with built-in image_gen. Preferred candidate: `chest-states-v2.png`; v1 is retained for provenance. Exact prompts and edit input are in `PROMPTS.md`. Neither image is a production sprite sheet or imported runtime asset.

## Direction

Compact charcoal iron strongbox with broad brass reinforcement and one ember diamond fixed to the lower front body. Closed = thin seam and lit emblem; opening = brief confined reward light; spent = dark empty interior and extinguished emblem. The broad silhouette should distinguish the prop from fiery enemies. Compared with a wooden chest or a flame-covered furnace, the iron/brass design matches the platform while limiting combat clutter.

Reviewed `platform-runtime-01/detail-runtime.png`, `ChestSpawner.cs` and `ChestArt.cs`. Current runtime uses two procedural 24x22 sprites at 32 PPU, bottom-center pivot. Walking into reach awards gold or health immediately; an open chest remains until the next room. This concept adds no reward, rarity, interaction or timing rule. Future opening animation must follow the existing reward event without delaying or repeating it. Gold-only contents were avoided because some chests heal.

## Visual review and limitations

- V1 incorrectly moved the emblem from lid to base across states; V2 places it on the base throughout.
- The broad bands, dark panels and spent interior read at board scale.
- Base shape, perspective and emblem registration still drift slightly between generated states. Do not slice and play these images as animation.
- Background is opaque with residual shading, not transparent. There is no alpha-ready sprite master yet.
- The finish is more textured than current actors. Small-size readability, appropriate painted simplification, floor contrast and crowd occlusion require a scene-scale proof.
- No Unity capture, suite run, APK, phone input or installation in this slice. No runtime behavior changed. Existing installed build record remains authoritative.

## Next bounded slice

Derive an isolated transparent closed master; lock a single base footprint and pivot; obtain consistent lid poses and a spent state. Compare at the existing .75-unit canvas width and a smaller candidate against the painted floor/actors before runtime integration. Do not independently auto-fit animation frames. Keep flash separate from persistent interior light and honor pause. Imported art must be shared/borrowed rather than regenerated per chest. Ability-ring work follows the chest proof.
