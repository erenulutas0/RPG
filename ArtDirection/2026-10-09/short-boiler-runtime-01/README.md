# Short foreground retorts — 2026-10-09

A distinct low boiler variant, authored in Blender, replaces both short foreground boilers at design(17.9,-4.08) and(26.9,-4.08). The approved radius2 /height2.28 envelope, foundations, service-main height1.8, atmosphere anchors and walkable layout stay unchanged. The north pair retains its earlier tall bake.

## Artwork and pipeline

Tools/ArtReview/BuildBlenderShortBoiler.py loads the committed study02 Blender scene for its shared procedural materials, lights and projection, removes only mesh objects, and authors a low masonry body, segmental arches/grates, bronze lid band, broad pressed hood and short vent. Output short-boiler-01 contains the editable self-contained .blend and transparent1024PNG. Evaluated authoring geometry:6,704vertices /13,036triangles; this mesh is not rendered in Unity. The orthographic view remains30degrees /2:1ground projection; vertical compensation1/cos30. Ortho span6; ground relative image bounds-1.86..4.14. Fire/light are static in the bake; existing smoke remains animated. No subscription, external service or image API was used.

Unity imports the byte-identical PNG as FoundryShortBoiler,1024ASTC6x6,11mips,non-readable,Clamp. Calculated block payload626,288bytes including mips, excluding native/engine overhead. The pair shares one scene-owned source material and two eight-vertex bands, split at projected height1.2, using original drum/iron ranks. Each card keeps independent mass depth and actor-cutaway flags. Material cleanup is explicit; texture is asset-owned. Original service sockets remain runtime geometry and contact shade joins existing floor decals.

Static region21renderers /99,492vertices, versus preceding20 /100,684: net+1renderer,-1,192vertices. Two short-bake bands are added; the now-unused old FurnaceFacade batch disappears. Legacy resource/constructor remain available for fallback and comparison. The import-use assertion now explicitly checks that the retired wrap is absent and both new short bands exist; existing floor ordering and lifecycle checks remain. The bounded renderer assertion is21. These counts do not establish FPS/battery performance.

## Actual graphics evidence

Graphical capture1passed. pair-before reconstructs both old short boilers in the same frozen scene/camera; baked bands hidden. New contact shadows and runtime sockets remain in both views. pair-after restores the new bands. This is an explicit comparison reconstruction, not older-device footage. Socket duplicate surfaces coincide in the legacy comparison only.

Four portrait approaches stage the hero on validated walkable design points(16,1),(20,1),(24,1),(28,1) using production follow width, HUD hidden. Full-map overview and closeup show original foundations, pipes and foreground ordering. These are actual Unity graphics, not phone captures. Socket and pipe shading remains coarser than the new bake; a separate pipe-material pass remains open.

Reproduce: portable Blender4.5.9 --background --python Tools/ArtReview/BuildBlenderShortBoiler.py; then Tools/ArtReview/Run-ShortBoilerRuntimeProof.ps1. Full gate Tools/Run-UnityTests.ps1. An initial capture compile found a test-local variable name collision; corrected before successful graphics/full gate.


## 2026-10-09 — Short boiler pair verified and installed

Completed a distinct low retort design in Blender and integrated both foreground boilers. Shared study02 materials/light, new segmental fire arches, broad pressed hood and short vent. Source Tools/ArtReview/BuildBlenderShortBoiler.py loads the committed tall study scene and replaces its mesh geometry. Editable source/bake: ArtDirection/2026-10-09/short-boiler-01. Model6,704vertices/13,036triangles is authoring-only; Unity uses two shared depth bands and the original runtime service sockets. Existing low height2.28, foundations, smoke anchors, layout/gameplay/camera/save behavior retained.

One new1024ASTC6x6,mipmapped,non-readable texture,626,288calculated payload bytes including mips. Both shorts share one source material and two batches, explicit scene cleanup. Old furnace-wrap batch is now absent (legacy resource/constructor retained for fallback/comparison); region21renderers /99,492vertices vs previous20 /100,684. No new per-frame component. Regression test verifies original independent short depths, both bands, retired wrap absence and21renderer limit; existing floor ordering/lifecycle assertions retained.

Verified .NET395passed, actual graphics1passed, EditMode415passed, PlayMode150passed, Android exit0 and no C#warning/error matches in Android log. Static integrity555asset/folderGUIDs /1099packageGUIDs /568sceneobjects, resolved refs/JSON. Initial capture compilation found a test-local name collision, corrected before successful graphics and full gate; its stale Unity lock was removed only after confirming Editor exit. APK36,076,056bytes; SHA256 `A3630A5F5249B7D486BBA8406DCDAE0CEA8DAA1F2062F421B7512C0A89F6D54C`; installed S23 base.apk hash matched.

Owner explicitly authorized closing the background run and using the phone. Fresh pre-stop/install backup TestResults/short-boiler-device-20261009-201142. Guarded real drags checked the foreground boiler and behind route; the first route did not reach the second boiler closely enough, so a separate longer targeted route captured the actual east boiler beneath the gate ramp. Both are visible in the retained device images; pipe/socket and foundation alignment intact, hero readable. Proof route contains staged enemies/outlines and is not normal encounter/balance evidence.

Separate normal Staff run showed Room1,90HP,4unbanked gold, damage7 and XP/gold/chest in post-kill reward view. Profile.json,bak and all3original development files SHA256-identical after normal run and final targeted review. Temporary depth-proof/render-stats removed, telemetry retained, no save restoration. Game stopped; phone released. Selected fatal/reference/index/invalid-operation/shader-error patterns absent in sampled process logs.

First route's late quiet windows59.9fps,max16.9ms,GPUaverage4.2–4.3ms; startup separately2289.9ms and216.3ms. This short S23 check is not matched prior/new GPU testing, sustained crowded combat or midrange acceptance. Earlier first-ramp hitch remains open.

Actual Unity before/after, four validated walkable portrait approaches, overview and device evidence: ArtDirection/2026-10-09/short-boiler-runtime-01. Legacy comparison reconstructs both old shorts; new shared contact shade/runtime sockets remain in both views, with coincident legacy socket geometry only in before. Fire/light are baked static, smoke remains animated. Foreground pipe shading and central pressure vessel still look coarser than the new furnace bakes; next focused art work is their material/shape coherence. Full-reference fidelity, encounter/actor art and long-device acceptance remain unfinished.
