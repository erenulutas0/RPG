# Brazier fire readability — 2026-10-07

Open beacon/brazier flames sampled a wide hot-seam patch containing dark coal crust. At portrait scale that produced black wedges inside opaque red tongues, competing with the newly quieter masonry. Keep the coal mound, boiler windows and undercroft fire mapping intact; only open tongues use the lanterns' existing measured smooth hot patch. Layered vertex colours now give an orange outer tongue, gold middle/core and cooler, partially transparent tips.

No new bitmap or image-generation call. Source: FoundryRegionDressing.cs. Same three quads per flame, static mesh construction, materials, depth scopes and sorting. No flame animation added. Geometry silhouette remains stylized/angular; this pass fixes surface breakup and heat hierarchy, not every aspect of fire realism.

Existing source atlas and shader retained. Colour components remain below 2. Scene, floor lighting, actors and gameplay unchanged. Static17 renderers /80,344 vertices. Same-camera before/after gate, bridge, portrait and overview evidence. Graphics capture1 passed. Full test/build/device closure below.


## Closure

Final EditMode410/410, PlayMode143/143, Android exit0, integrity537/1099/568. Installed APK350870192B69AE45BB80E3AC8F1518C8E1A7F9233A2759DC50DED3A48295724C (31,113,605 bytes), base.apk hash matched. S23 guarded gate reveal and return to normal combat captured. No managed exception/crash patterns in sampled current-process log. No new performance benchmark.

Fresh backup TestResults/fire-device-20261007. Profile/bak and original development files hash-identical. No diagnostic flags added, original gate-arrival opt-in retained, app stopped. Post-build source edits correct comments only. No push.
