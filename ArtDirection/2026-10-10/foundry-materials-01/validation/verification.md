# Foundry materials verification - 2026-10-10

## Scope and actual image acceptance

Integrated authored Blender stock hoist, finer weathered gate ashlar and restrained fractured floor edges. Reference-map art remains the owner priority; no layout, navigation, scene, camera, combat, progression or save edits. Before baseline is7e0c860. One baseline and one combined after graphical capture passed. Same-camera before/after stock, gate, court, yard, overview and portrait reviewed by root and respective helper agents.

Hoist metal is the clearest improvement. The crane correctly occludes some chain/yoke because it is spatially in front; no rank change should hide the crane merely to expose the hoist. Gate courses better match architectural scale but their weathering is subtle in full-map view. Floor improvement mainly removes broad pasted-on polygon artifacts. No visible floor holes, brass spill, floating relief or door overlap observed. Full reference fidelity remains incomplete; fine natural stone richness and remaining procedural stock/crane materials are open.

## Executed verification

- .NET:409 passed (`dotnet test Tools/CombatChecks/CombatChecks.csproj --no-restore --nologo --verbosity minimal`).
- Focused FoundryRegionTests:3 passed; hoist resource import/card/rank/explicit depth and scene-lifetime ownership covered.
- Full Unity6000.0.65f1:432 EditMode and151 PlayMode passed, zero failed/skipped. Full distinct-suite total992; focused rerun is not counted again.
- Android build exit0; no C# warning/error matches in android-build.log.
- Final static integrity with Unity closed:573asset/folderGUIDs,1099packageGUIDs,568sceneobjects/components; references/JSON valid. Whitespace and gallery links checked.
- Source render/runtime source PNG hashes identical; see hoist-source-sha256.txt. Imported Unity texture512x512 ASTC6x6/10mips/non-readable. Calculated block payload158,432bytes including mips, excluding engine overhead.

Combined region25->26renderers and117,754->118,902vertices (+1,148;~0.98%). Baked hoist is one quad/new material; floor/gate refinements add geometry. No new per-frame component. These are mesh/resource facts, not a frame-time benchmark.

APK `Builds/Android/Cryptforge-Prototype.apk`:36,160,229bytes, SHA256 `D3F339A225CA0AF15C93D35E5D226D21E8279FD5DE1D236D0DD18F337C97F649`.

## Device and remaining limits

NOT INSTALLED. Prior owner-run restart/install question remains unanswered. No ADB input, install, stop, flag or profile operation this turn. Last recorded installed package021b95f and previously paused Room2/74HP/8unbankedgold were not reobserved. Fresh preflight/backup required after approval; never restore stale profile over owner progress. Physical-device acceptance, cold-first-ramp hitch investigation, sustained crowd/midrange performance remain open.

Reproduce current graphics with Run-FoundryMaterialsProof.ps1. Phase selects output only; old source is needed to reproduce baseline. Portraits are staged actors with hidden HUD at production camera width; full-map uses a separate wider review camera. They are not phone screenshots.