# Authored foundry crane metal

Editable Blender replacement for the existing camera-space crane assembly. The cast mast, inspection hatch, lower winch/crank, anchor gussets, upper bearing, open box jib and counterweight use the shared study02 procedural iron, steel and bronze materials/lights. No purchased raster, image generation or post-render bitmap editing.

Reproduce from the repository root:

```powershell
& 'TestResults/tools/blender-4.5.9-windows-x64/blender.exe' --background --python Tools/ArtReview/BuildBlenderCrane.py
```

The script reads `ArtDirection/2026-10-09/blender-furnace-02/furnace-study.blend` and writes this directory only. `crane.blend` retains editable geometry/materials/lights/camera; `crane.png` is the transparent 1024-square bake. Evaluated source geometry: 6,768 vertices / 13,308 triangles, measured in Blender4.5.9. Runtime uses one four-vertex card, not this mesh.

Runtime anchor is `WestProject(-25.5,13)+Vector3.up*1.1`. Quad relative to that anchor: x[-7.4,3.4], y[-0.4,10.4], full UVs, orthographic span10.8. Mast(0,6.5), rear(1,6.5), tip(-5.303301,8.998097) match the original projected endpoints. `projection.json` records the contract. World-up height compensation and camera-direction extrusion preserve these fixed-view anchors while giving the authoring geometry genuine bevels, rounded bearings and cast shading.

`Resources/FoundryCrane.png` is a byte-for-byte copy of the source bake. Import:1024 ASTC6x6,11mips,Clamp,non-readable; calculated compressed block payload626,288bytes including mips, excluding engine overhead. One scene-owned source material and one existing-style cutaway variant; cleanup is implemented by the root integration. Original crane footprint(-27+WestShift,11)-(-24+WestShift,15),height8.6 remains an explicit non-landmark owner. Rank-11.5 quantizes to-9, ahead of rear stock and behind live wood/chain/lantern geometry.

Masonry footing, metal footing cap, hanging chain, tip sheave, suspended payload and lantern stay live. Original mast/jib/counterweight and separate gusset/drum helper are missing-resource fallbacks. The attachment points and walking footprint are unchanged. The thin jib requires the chosen1024 runtime import; the source alone does not prove compression or device quality.

The isolated bake was inspected for a complete open truss, mast contact, bearing joints and counterweight. Static baked lighting and fixed-view geometry remain limitations. Actual Unity comparison, full gates and device status belong to the root's combined package evidence; no Unity/device tests were run by the asset author.

First Unity image review found the original northwest chamfer lantern's masonry covering the baked mast at the new card rank. The single `CraneRearLantern` post now emits masonry at -12 (quantized -10), behind the crane; its cage/panes and all other posts retain their ranks and depth owners. This also preserves the legacy mast ordering. Both pale bearing flanges were rerendered with darker, rougher machined iron following that review. Projection and model geometry are unchanged. Corrected actual Unity acceptance remains part of the combined package review.
