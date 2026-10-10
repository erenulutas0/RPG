# Workshop coherence verification - 2026-10-10

## Scope and visual review

Baseline: ccb0f35. Replaced procedural stock-station metal and crane body with authored Blender geometry rendered to transparent cards. Stock contains the existing basin, billets, coils, hollow pipes and tools. Crane contains the original mast, winch, gussets, bearing, open jib and counterweight. Existing wood, crane footing, chain, tip sheave, basket and lights remain live. Source scenes, scripts and projection contracts are retained in stock-station-01 and crane-01.

One before and two successful after graphics passes were executed. The first after revealed a real ordering regression: the northwest chamfer lantern's masonry covered the crane's middle mast. Only that post's masonry now sorts at -10, behind the crane at -9; lantern iron and panes retain their original foreground ranks. Main and lower flange material was also darkened. Root reviewed the corrected detail and production-width portrait: continuous mast restored, barrel/crate silhouettes preserved, pipe openings and basin form improved. Full reference fidelity is still incomplete; this is a peripheral material improvement.

The first attempted after run failed on a new test-local variable collision, fixed before successful verification. Its stale zero-byte Unity lock was preserved under ignored TestResults after checking Unity had exited; no project content was deleted. Final graphics XML is retained in after/graphics.xml. Baseline XML was overwritten by later capture runs and is not claimed as retained evidence.

## Executed checks

- Pure .NET: 409 passed earlier in this package.
- Focused FoundryRegionTests: 3 passed before the final post-order fix; XML retained. Final full gate is recorded below when complete.
- EditMode: 432 passed.
- Final full PlayMode: 151 passed, zero failures/skips. Distinct-suite total: 992 (409 .NET +432 EditMode +151 PlayMode); focused reruns are not counted again. Android build exit0; no C# warning/error matches in android-build.log. Final static integrity with Unity closed:577asset/folderGUIDs,1099packageGUIDs,568sceneobjects/components; references/JSON valid.
- Actual Unity resource capture: stock512x512 ASTC6x6,10mips; crane1024x1024 ASTC6x6,11mips; both non-readable. Per-resource text in after/ records these observations.
- Source/render PNG hashes match their Resources copies; source-sha256.txt records the hashes.

Final region:26 to29 renderers,118,902 to116,178 vertices (-2,724). Two card/material batches plus one specifically ordered lantern masonry batch. The source Blender meshes are not imported as runtime meshes. Calculated new compressed texture blocks:784,720bytes including mips, excluding engine overhead. No new per-frame component; no FPS improvement is claimed.

## Phone and reproduction limits

NOT INSTALLED. No ADB operation or fresh phone observation this turn. The prior concrete owner-run restart/install request remains unanswered. Last recorded installed package021b95f and paused Room2/74HP/8unbankedgold are historical observations. On approval, take a fresh preflight and backup, install the latest package, verify installed hash, inspect the accumulated map changes and normal combat, compare current original files and remove only new diagnostic flags. Never restore a stale save over owner progress.

Physical-device acceptance, sustained crowd/midrange performance and the earlier first-ramp hitch remain open. Progression and encounter redesign remain deferred by the owner's art-first direction.

Reproduce current graphics with Tools/ArtReview/Run-WorkshopCoherenceProof.ps1. The phase parameter only selects the output directory; baseline reproduction requires baseline source. Portraits use production camera width, hidden HUD and staged actors; overview and complete-map views use separate wider review cameras. These are Unity captures, not phone screenshots.

APK: Builds/Android/Cryptforge-Prototype.apk, 36162989bytes, SHA256 F2349888220988321C52C440C7F29C3001AD20FC2A808BD6C8EE953FB054CB51. Ready, NOT INSTALLED.
