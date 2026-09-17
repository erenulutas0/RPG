# Platform material study — 2026-09-17

An isolated real-Unity comparison of painted basalt floor, flush brass coping and two near masonry faces. The preferred floor is **V2**: quieter bevels and grout leave more attention for the blue Vanguard, ember enemies and combat indicators. This is a partial surface/material proof, not the completed boss arena or a new runtime import.

## Review

Open `index.html` through the ArtDirection server: <http://127.0.0.1:8342/2026-09-17/platform-material-01/>. Select centre, near/right/far rim, whole-platform or combat detail; use the reveal slider or Current/Painted buttons. A separate selector retains the stronger V1 floor for comparison. The 1080x1920 render checks the shorter portrait framing.

Four original masters are retained unchanged: floor V1/V2 (1254 square), coping (2172x724) and wall (1254 square). Exact built-in image-generation prompts are in [PROMPTS.md](PROMPTS.md); source dimensions, formats and SHA-256 hashes are in [asset-metadata.json](asset-metadata.json). V2 was generated as an edit of V1. No code repainted, resized or composited these masters. The bridge only stages channels and losslessly encodes Unity BMP captures as PNG.

## Actual Unity mapping

`PlatformMaterialCapture.cs` loads the real Gameplay scene with an isolated test profile and density-proof floor. The actual ten-enemy pack advances for 160 frames with attacks disabled, then freezes. Both material versions use identical actors and camera. Rim samples move the hero/camera while enemies remain at the centre; they are framing diagnostics, not live kiting evidence. The hero's contact shadow is refreshed after each reposition, including return to centre for detail/overview.

Seven temporary meshes overlay the current diamond: one floor, four flush coping bands and two near walls. They use unchanged source textures, sRGB, mipmaps, trilinear sampling and Repeat wrapping. Floor repeats twice in each axis (nominal sixteen slabs per edge); coping repeats twice along each side; wall repeats three times per face. Geometry, sorting and ownership are recorded in [mapping.json](mapping.json). Sorting stays below ground effects/actors. The old platform is retained beneath the material layer for the keel/crystal and corner fixtures. Temporary meshes, materials and textures are disposed at teardown. No authored Assets, scene, project settings, APK or phone state changed.

## Acceptance and limitations

- Prefer the darker V2 floor. It matches the painted actors better and reduces the bright procedural tile noise.
- Coping and masonry establish the material direction, but corners, the old pixel outline and the join into the old lower keel remain provisional. Lanterns/crystal, chest and ability ring still use earlier art.
- Existing crowd depth overlap and hero/chest occlusion remain visible. No spacing, stats, combat timing or camera behavior was changed.
- This proof loads full-size source textures. It is not the mobile texture budget: three selected RGBA32 textures plus mipmaps are approximately 25 MB before other overhead. Runtime integration needs reduced-size/compression comparisons and shared ownership; do not ship these proof allocations unchanged.
- Static Unity renders do not establish physical-device readability, moving-floor shimmer, warm-reload ownership or sustained performance.

## Reproduce and verify

Close the Unity Editor, then run `pwsh -File Tools/ArtReview/Run-PlatformMaterialProof.ps1` from the repository root. It stages source channels, temporarily installs the capture fixture, runs .NET and compile checks, runs the graphics-enabled PlayMode capture, encodes fourteen renders and removes the fixture in `finally`. No APK is built. XML/log evidence is under `TestResults/platform-material-capture.*` (local).

Verification at this checkpoint: .NET272/272, clean compile and graphics capture1/1. Full regression/integrity results are recorded in the latest Docs/22 entry. Gallery buttons/selectors/images and desktop/390x844 layout were checked, without horizontal overflow.

## Next bounded slice

Resolve coping corner/old-outline and wall-to-keel joins, then integrate only the accepted materials with explicit shared cache lifetime and mobile texture imports. Check compression at real screen size, sorting/contact shadows and scene reload. Run the full gate and then controlled S23 centre/rim movement and restart review, preserving the current profile. Keep the actual arena geometry and encounter rules unchanged during this art integration. A separate boss-room layout remains a later design task.
