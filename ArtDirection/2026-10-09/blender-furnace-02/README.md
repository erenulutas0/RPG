# Arched Blender furnace and staged Unity depth probe

Second editable furnace study, 2026-10-09. Preserves the earlier study in `../blender-furnace-01/`; that study's generator/capture source is available at commit8480016. Current tools reproduce this second study.

## Completed together

1. Two arched fire mouths with individually modelled radial stones, jamb courses, thresholds and arch-fitting grates. Upper inspection ports restore the production design's vertical rhythm.
2. Cooler basalt and less warm fill; procedural coal heat concentrated low in the firebox, with irregular upward tongues and a dark upper recess. The fire is baked/static, not an animation. Metal remains intentionally quiet. This is authored geometry and shader nodes, not generated raster editing.
3. Actual Unity isolated comparison at1200px and360px, plus staged map placement using the existing Foundry Cutaway material/shader and an ordinary textured quad. Real hero transform is staged in front/behind at frozen simulation time; the original boilers remain in the map. This is a technical/art probe in empty court space, not an accepted new obstacle or a shipped replacement.

## Evidence

- `unity-current*.png`: unchanged production boiler, isolated at the same scale.
- `unity-blender*.png`: new transparent bake displayed inside Unity.
- `map-front.png` / `map-front-control.png`: hero in front, with/without candidate focus; pixel-identical (0 changed pixels).
- `map-behind-no-cutaway.png` / `map-behind-cutaway.png`: hero behind, candidate cutaway disabled/enabled;22,410changed pixels and hero visibility reviewed in actual captures.
- `map-portrait.png`: staged1080x2340view, production follow width reframed through `Frame(1080,2340,361,1913)`, HUD hidden, review camera recentered. Not a device screenshot or normal-follow traversal. The first capture used the initial desktop aspect/size and was rejected; this final image uses portrait framing.
- `unity-contract.txt`, `verification.xml`, `model-budget.json`: raw contract/test/budget evidence.

The new arches and contained fire are materially closer to the established design than study01. The bake is still cleaner and more regular than the target reference. Map review exposes remaining work: authored ground/contact shadow, nearby light integration, soot/wear variation, and replacement geometry/pipe seam alignment. The simple fixed-depth quad supports the tested front/back positions but does not prove every side approach, overhang or neighboring structure ordering. No production quality claim for all map views.

## Source and integration choice

`furnace-study.blend` has full procedural materials, lighting and geometry. FBX is only a mesh study export, not a Unity-ready material package. Evaluated study mesh:11,928vertices /23,104triangles. This probe renders a4vertex/2triangle quad with an uncompressed1200x1200texture (5.76MB base-level RGBA, excluding overhead); it does not render the study mesh in Unity. No mobile atlas/compression, sustained performance or APK acceptance is claimed.

Keep the existing production asset until one deliberate replacement proves ground contact, service pipe/foundation alignment, depth from all approaches, compression and mobile readability. The source model can generate additional variants later; do not automatically migrate the whole map. No new paid service/API used.

## Reproduce and validation

```powershell
& '<Blender4.5.9 path>/blender.exe' --background --python Tools/ArtReview/BuildBlenderFurnace.py
pwsh -NoProfile -File Tools/ArtReview/Run-BlenderFurnaceProof.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

Final graphical PlayMode capture:1/1passed, including front/back pixel assertions. Static integrity:552asset/folderGUIDs,1099packageGUIDs,568sceneobjects, valid references/JSON. Temporary test/meta cleaned. No runtime Assets, package/settings changes, gameplay changes or phone operations. Full gameplay suites and Android rebuild were not rerun for external-art/capture tooling only. Installed phone build remains material-readability8ffec82; the first-ramp hitch remains open.
