# Single baked boiler runtime integration — 2026-10-09

North boiler index0, design(18.39,15.81), now uses the authored Blender study02 bake. Other boilers retain their production construction. No layout, walkability, collision, gameplay, camera, profile or balancing changes.

## Runtime implementation

- `FoundryRegionBakedBoiler.cs` loads one shared imported texture at scene construction and owns one source material; the existing visibility view owns its cutaway variant. Both are destroyed with their scene owners. Texture remains asset-owned.
- Two static quads divide the image at projected height2.8. The drum uses the existing drum rank; the upper band uses the iron-shell rank. Both inherit the original mass footprint and existing actor-depth/cutaway flags. This preserves foreground rails and pipes without importing the23,104-triangle Blender study mesh.
- Radial contact shade shares the existing coping/floor-decal batch. An initial separate shade batch caused the existing floor-order regression test to fail; it was merged into the existing batch rather than loosening that test.
- The foundation, boiler service-pipe endpoints and flue atmosphere anchor are unchanged. There was no visible connection gap in the reviewed actual Unity closeup. Bake lighting/fire are static; existing scene smoke remains animated.
- Texture `Assets/_Project/Resources/FoundryBoiler.png` is byte-identical to the study02 PNG. Import:1024square, ASTC6x6,11mips, non-readable, clamp, alpha transparency. Calculated ASTC block payload including mips626,288bytes, excluding engine/native overhead. Original Blender source remains in study02.

## Visual evidence

`furnace-foundation-pipes.png` shows the actual replacement, original foundation and original pipes. `furnace-before.png` reconstructs the unchanged old BuildTallBoiler in the same frozen scene/camera, hiding the new two bands. It is an explicit comparison construction, not a screenshot from a previously installed APK. The shared new contact shade remains in both images.

`overview.png` and four `approach-*.png` captures show production geometry. Hero positions are staged at validated walkable design points(12,12),(16,9),(22,9),(28,9), through the movement model; camera uses production follow framing and HUD is hidden. These are Editor graphics evidence, not device traversal. No temporary extra courtyard furnace is used in this proof.

Static region:20renderers /101,392vertices, versus previous18 /102,100. Two added renderers,708fewer vertices. Two depth-tested cards total8vertices/4triangles;96shadow vertices are merged into existing decals. These counts do not establish frame time or battery impact.

The new model is more coherent in curved metal and arches, but still cleaner than the target reference. One deliberate replacement is shipped by this slice; it does not finish the entire reference map. The earlier ramp first-use hitch remains outside this change. Side views are the game's fixed isometric projection, not a freely rotatable3Dmodel.

## Verification

Actual graphical capture passed; see `graphics-verification.xml` and `runtime-budget.txt`. Import, scene material/depth-band constraints, existing decal ordering, scene reload disposal and full regression suites are checked in the pinned Editor. Final suite/APK/device results are recorded below when complete.

Reproduce graphics with `pwsh -File Tools/ArtReview/Run-BakedBoilerRuntimeProof.ps1`. Full test/build gate: `pwsh -File Tools/Run-UnityTests.ps1`. The wrapper owns a temporary PlayMode capture file/meta and removes both after execution. No new paid service or API was used.

### Final verified state

.NET395passed; EditMode415passed; PlayMode150passed; graphical capture1passed. Android build exit0. Static integrity554asset/folderGUIDs,1099packageGUIDs,568sceneobjects/components. An earlier149/150PlayMode run exposed the separate shadow-decal batch; the final run includes the corrected shared decal batch.

APK36,074,875bytes, SHA256 `DA3961080FB0823D60C230DDC0C883C39F89AE31C15A24D9FA405773375FA5D3`. Installed on S23 and installed base.apk hash matched. Fresh backup `TestResults/baked-boiler-runtime-device-20261009-181315`; all11saved files before/after background installation hash-identical.

**Device visual review pending.** Owner initially authorized installation/testing, but a later foreground check showed YouTube; the follow-up question about opening the game remains unanswered. Installation was performed while Cryptforge was stopped, without launching it or sending input. Final foreground was Launcher. No device capture, gameplay, diagnostic flags or performance sample in this slice. Installed package is ready for the next owner-approved review; the APK does not need rebuilding. Earlier ramp hitch remains open.

## Device acceptance completed — 2026-10-09

Supersedes the pending-device status above. On the next continuation, the phone was unlocked on Home and Cryptforge was stopped. Fresh review backup: TestResults/baked-boiler-review-20261009-181855. Installed APK hash still matched DA3961080FB0823D60C230DDC0C883C39F89AE31C15A24D9FA405773375FA5D3; no reinstall. Guarded real movement reached the new boiler; front and step-back captures show arches, foundation, pipe connection and foreground ordering. HUD partly crops the hood; complete form is covered by Editor capture. An earlier route overshot toward the old second boiler and is excluded from the selected first-boiler proof.

Separate normal Staff run showed Room1,90HP,4unbanked gold and XP/gold/chest rewards; normal-combat.png is a post-kill reward view. Profile.json, profile.json.bak and all three original development files remained SHA256-identical. Temporary depth-proof/render-stats flags removed, telemetry retained, no save restoration, app stopped. Selected crash/reference/index patterns absent from sampled process logs.

Targeted route ended with short quiet windows at59.9fps, maximum17.1ms and GPU average3.8ms. Startup separately contained2305.7ms and the first sampled window39.1ms. This is a short S23 check, not sustained crowded-combat or midrange acceptance; no matched old/new GPU benchmark. The earlier first-ramp hitch remains open. Selected screenshots and process logs are in device/.

Historical reproduction note: the wrapper now captures the paired integration. Use the capture tool at e901a33 to reproduce this single-boiler proof.
