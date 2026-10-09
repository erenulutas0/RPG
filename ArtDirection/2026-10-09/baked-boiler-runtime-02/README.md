# Shared north boiler pair — 2026-10-09

Extends the accepted Blender study02 appearance from north boiler0 to north boiler1. The two short east boilers retain their distinct construction. No layout, foundations, service anchors, camera, collision, gameplay or save changes.

FoundryRegionBakedBoiler creates one source material lazily at scene construction; both boilers borrow it. Two shared depth-band batches each contain8vertices; each four-vertex card retains its own original mass depth and cutaway ownership. Texture remains the same asset-owned1024square ASTC6x6 bake with11mips, non-readable. Existing scene cleanup owns source material, visibility owns the variant. Contact shades share existing floor decals.

Static region20renderers /100,684vertices, versus the single replacement20 /101,392:708fewer vertices, no additional renderer, material, texture or per-frame component. This is not an FPS claim. Full PlayMode assertions retain the20renderer bound and verify the two distinct footprint depths inside each shared batch.

## Evidence

Actual graphical capture1passed. pair-before reconstructs both legacy tall boilers inside the same frozen scene with unchanged pipes/foundations; both baked bands are hidden. pair-after restores both baked boilers. New shared contact shades remain in both images. This is a comparison construction, not a capture from an older APK. Other views show production construction, with hero staged at four validated walkable points, production follow width and HUD hidden.

The repeated pair intentionally shares one authored furnace design and light direction. Bake fire/light remain static; scene smoke is animated. The full painted-reference surface richness is still unfinished. The first-use ramp hitch and sustained/midrange acceptance remain open.

Reproduce this current pair with Tools/ArtReview/Run-BakedBoilerRuntimeProof.ps1. Historical single-boiler capture tool is at commit e901a33. Full gate: Tools/Run-UnityTests.ps1.


## 2026-10-09 — Shared north boiler pair verified and installed

Accepted the first boiler on S23, then extended the same study02 bake to the second tall north boiler. FoundryRegionBakedBoiler now lazily owns one source material shared by both; two existing depth-band batches each contain8vertices with independent mass depths. Original foundations, service pipes, smoke anchors, short-boiler variants and gameplay remain unchanged. Runtime region20renderers /100,684vertices (previous single replacement20 /101,392); no additional texture/material/renderer or per-frame component.

Verified .NET395passed, graphical capture1passed, EditMode415passed, PlayMode150passed, Android exit0, no C# warning/error matches in Android log. Final static integrity554asset/folderGUIDs /1099packageGUIDs /568sceneobjects, valid references/JSON. Existing ordering and renderer-budget assertions retained; batch test now verifies both original footprint depths. APK36,074,650bytes, SHA256 `64DC9D243D84BBF414B7F39E0F2B30118EDBD441B12B7445076282BC07FB43D5`; installed S23 base.apk hash matched.

Fresh backup TestResults/baked-boiler-pair-device-20261009-183514; before installation phone unlocked on Home, game stopped. Guarded actual movement checked both north boilers and their foundation/pipe/foreground contacts. Phone HUD crops much of the second boiler's upper form; complete pair is separately verified in actual Unity graphics, not claimed fully visible on phone. During the first targeted return attempt ADB daemon connection failed, the guard stopped input and finally cleanup removed flags/stopped app. Rechecked Home/unlocked, absent flags and stopped app; repeated that targeted route successfully. The failed attempt is not acceptance evidence.

Separate normal Staff run: Room1,90HP,4unbanked gold, damage7 and XP/gold/chest visible in post-kill reward capture. Profile.json,bak and all3original development files hash-identical; temporary depth-proof/render-stats removed, telemetry retained, no save restoration. App background-flushed/stopped, phone released. Selected fatal/reference/index/invalid-operation/shader-error patterns absent from sampled process logs.

Short quiet route windows59.9fps with maximum17.2ms, GPU averages around3.9–4.0ms; first launch separately contained2330.4ms and249.2ms startup intervals. No matched GPU baseline, sustained crowded combat or midrange acceptance; earlier ramp first-use hitch remains open.

Proof: ArtDirection/2026-10-09/baked-boiler-runtime-02, actual pair before/after, four validated walkable portrait approaches, full-map view and selected actual device captures/logs. Same-frame legacy comparison reconstructs both old boilers and keeps new contact shades in both views. Current capture tool reproduces pair02; use e901a33 for historical single01. Full reference fidelity remains unfinished. Next substantial art candidate is a distinct short-boiler variant, then pipe-material coherence; these are not implemented by this slice.
