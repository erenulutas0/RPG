# Cast loading crane — native source candidate

This version keeps the existing crane's fixed-camera contract while replacing the pale flat jib treatment with a coherent dark cast assembly. The large chord faces are rough iron; genuine channel lips carry restrained working-edge light. A broad tapered pivot yoke bridges the upper mast chords, and the lower winch has connected bearing cheeks and a seated gearcase. These are native geometry/material changes, with no image generation or raster postprocessing.

The main and lower bearing stacks use overlapping depth intervals. The previous narrow extrusion gaps were removed: each 0.08-deep front ring overlaps the next by 0.01, and the rear flange seats in its supporting casting. Jib lip extrusions overlap their channel webs. The winch cheeks meet the existing cable drum/flanges; the open cable spool remains visible.

Reproduce from the repository root:

```powershell
& 'TestResults/tools/blender-4.5.9-windows-x64/blender.exe' --background --python Tools/ArtReview/BuildBlenderCrane.py
```

The generator reads the original `blender-furnace-02/furnace-study.blend`, preserves its procedural material/light family and writes only this new output directory. The earlier committed `2026-10-10/crane-01` source and evidence remain unchanged. `crane.blend` contains editable native geometry, materials, lights and camera; `crane.png` is the unmodified transparent 1024-square RGBA render.

Before saving/rendering, the generator evaluates every bevelled mesh vertex, verifies finite camera coordinates and containment in the unchanged frame, and checks the exact mast/rear/tip projected anchors against the analytic original rectangle. `projection.json` retains the resulting geometry counts, projected bounds and anchors. The original quad x[-7.4,3.4], y[-0.4,10.4], span10.8 and runtime footprint/rank/height remain unchanged.

The source render was inspected for a connected main yoke, visible open truss, restrained edges, seated winch gearcase and clean frame clearance. This is source acceptance only. The runtime resource has not been edited by the source author; Unity import, compression/readability, existing live sheave/chain/load contact and physical-device acceptance remain the root integration's responsibility. Static lighting and fixed-view geometry are retained limitations. Blender mesh counts describe authoring complexity, not runtime GPU geometry.
