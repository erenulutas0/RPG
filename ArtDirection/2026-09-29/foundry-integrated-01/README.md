# Combined foundry environment package - 2026-09-29

Owner requested several art tasks completed together rather than separate incremental deliveries. This package combines four changes and includes all preceding uninstalled arch, paving, and crown work.

## Delivered work

1. New quieter honed basalt albedo. Original v1 is retained; unchanged generated v2 is imported at `Assets/_Project/Art/Props/Foundry/FoundryPavingV2.png`. The existing paving material now references it. Existing slab zones, light/soot shading, and geometry remain intact.
2. Eight recessed louvred panels on gate tower return faces, with brighter cool slat edges after visual review. Painted doors and banners remain unobscured.
3. Two fitted iron roof apron plates on each of four boilers, grounding the existing chimney braces on a distinct metal surface.
4. Irregular precomputed smoke contours with offset soft cores, sheared/rotating outlines, chimney-specific timing, and warmer source/cooler upper tint. Root increased the initially too-faint alpha/colour after review. Smoke remains intentionally restrained, not volumetric.

## Production / source

Basalt v2 was generated using the built-in image_gen tool. Full exact prompt: `PROMPTS.md`. Master: `basalt-v2.png`; copied unchanged into Assets. Both files have SHA256 `63ED64CB8CFA2A6C450ED6EEE2C13B3869E4DD18D7414F6479DAE4DC89049FBA`. No external texture or raster editing was used. The importer `Tools/ArtReview/BuildFoundryPavingAsset.cs` reproduces the versioned source binding. Import is opaque sRGB, mirrored wrap, trilinear/mipmapped, non-readable, Android ASTC 6x6, max 1024. Source v1 remains available.

## Budget and ownership

Static environment: 17 renderers / 52,908 vertices, previously 52,556 (+352). Atmosphere: unchanged 1 renderer / 780 vertices. Added 288 cached smoke-radius floats (~1.1 KB) at construction; no new per-frame allocation, particle objects, textures for smoke, shader, or realtime light. Existing 30 Hz cap, scaled-time pause behavior, source positions, and scene-owned mesh disposal retained. No gameplay, collision, walking-union, scene-asset, or playable camera changes.

## Verification

Two implementation helpers and one independent visual reviewer used, within the owner's maximum of three. Root inspected the actual combat portrait, gate, furnace, and smoke frame output. Reviewer found no blocking placement or readability issue; initial dark louvre edges and faint smoke were adjusted, then recaptured and accepted. Final smoke remains subtle in stills; phone motion/performance is not established by this review.

Actual five-waypoint Unity graphics proof: 1/1 passed. Compile: 0 warnings/errors. Static integrity: 515 project GUIDs / 1,099 package GUIDs / 568 scene objects/components. Full test/build outcome follows below. Before images were saved from the preceding crown package; after images are real Unity output. Eight smoke images are sampled frames, not a continuous video.

No phone operation in this slice. Last verified installed APK remains `18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535`. No commit or push. This completes the named combined package, not final reference-image quality.

## 2026-09-29 - Combined material, architecture, and atmosphere package

Owner requested several art tasks in one delivery. Integrated: unchanged generated honed basalt v2 (versioned source and Assets file, old v1 retained), 8 gate tower return louvre panels, iron roof aprons on 4 boilers, irregular soft smoke outlines with source-to-upper colour transition. Final review lifted panel edges and smoke visibility. Two implementation helpers plus one independent reviewer used (maximum 3). Final actual Unity images accepted with no blocking placement/readability issue. Smoke remains subtle; device motion/performance not claimed. Proof/source/prompt: ArtDirection/2026-09-29/foundry-integrated-01/README.md and PROMPTS.md. No raster editing; built-in image_gen source SHA256 63ED64CB8CFA2A6C450ED6EEE2C13B3869E4DD18D7414F6479DAE4DC89049FBA.

Static 17 renderers / 52,908 vertices; atmosphere 1 / 780 with 288 cached radius floats and existing 30 Hz/pause/ownership behavior. No walking-union, collision, scene, gameplay, or playable camera edits. Graphics traversal 1/1, compile 0 warnings/errors, integrity 515/1099/568. Full gate: EditMode 405/405, PlayMode 130/130, Android exit 0. APK actual size 39,340,606 bytes, local build 2026-09-29 17:21:08, SHA256 FD05B1749FB79D9E1EF7F992DBB61BF0B9FB83CA6E253FD9B74DC938D9F32BEC. READY, NOT INSTALLED. Includes prior uninstalled arches, paving, and crowns. Last verified installed remains 18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535. No phone actions, save edits, commit, or push. Named combined package complete; final reference-image quality remains open.