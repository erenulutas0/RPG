# Foundry surface cohesion — 2026-10-08

Three visual tasks in one package:

1. Court stone: replace the independent dark/pale flag lottery with restrained individual variation, quieter inset joints and bevels. Existing continuous patina, service soot, fire lighting, texture, floor mesh layout and gameplay boundaries remain. This reduces the checkerboard introduced by the recent smaller paving; it is not new imported stone art.
2. Boiler hoods: continuous plain-plate sampling, a broad directional metal highlight, flue soot and sparse radial seams. Cover the far slope as well as the near slope so the flat cap no longer shows as a dark dish behind the flue. Retain all lower riveted shells, fire doors, vents, footprints and depth owners. The first actual capture was too plain; retained in review/first-roof-too-plain.png for the design decision.
3. Distant cavern: a foundry-only sprite shader compresses contrast and saturation into violet air. One texture sample in the existing backdrop draw, no additional fullscreen layer or bloom. The material is scene-owned and released on destruction; ordinary halls retain their previous sprite material and color. No source image is modified.

Before images are the preceding gate-elevation production capture. After images are actual Unity graphics-harness output with the same cameras. Staged review poses/effects are not played battles. Device screenshots are actual guarded S23 traversal and normal combat; see the verification below.

No gameplay, balance, camera, movement, loot, save, scene or import changes. Full reference fidelity is still unfinished; this package addresses material hierarchy rather than claiming the concept is complete.

## Final verification and installation

Verified: .NET 395 passed; compile 0 warnings/errors; final actual graphics capture 1 passed; EditMode 415 passed / PlayMode 149 passed (including new owned-material lifecycle test); Android exit 0; final integrity 552 asset/folder GUIDs /1,099 package GUIDs /568 scene objects/components. Static region 18 renderers /95,622 vertices (+832, about0.9%); largest mesh27,373. APK36,061,418 bytes; SHA-256 `BA39FFFAB4EB3D650CBC0609B7211DF6BCDA3990EDCCE1655EE45519B8CFCA67`; installed S23 base.apk hash matched.

Phone was on Home with the game stopped and no keyguard before installation. Fresh backup: `TestResults/surface-cohesion-device-20261008-233826`. Guarded actual drags traversed the approach, gate ramp and threshold, then descended. Device captures show new boiler hood seams/material, quieter paving, graded distant scenery in the margins, and enemy/loot silhouettes through the near boiler. Complete far/near hood surfaces are checked in Unity detail; the close phone HUD crops the north hood. Normal Staff combat was captured separately (Room1,90HP,4unbanked gold, damage and XP/gold drops). Profile.json, profile.json.bak and all3 original development files had identical SHA-256 hashes before/after. Temporary depth-proof flag removed; original gate-arrival,region-preview,start-floor.txt.off and test telemetry retained. No save restoration. App background-flushed, stopped and phone released. Sampled process logs contained no selected fatal/reference/invalid-operation/index/shader error patterns. No new FPS, sustained combat or midrange benchmark; earlier gate-elevation first-use hitch remains unisolated.

Source changes and this proof are saved in a local commit; no push.
