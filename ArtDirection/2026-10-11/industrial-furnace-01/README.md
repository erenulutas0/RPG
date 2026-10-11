# Industrial furnace pair — native source renders

Two coordinated tall-furnace replacements, 2026-10-11. These are Blender renders for the existing projected runtime cards; they are not Unity screenshots or approved mobile integration.

- `variant-0.png` / `.blend`: near furnace, a circular pipe receiver supporting a stepped rectangular pressure chest, broad bearing piers, recessed service panels and an open square exhaust.
- `variant-1.png` / `.blend`: far furnace, a circular receiver with a tapered ribbed exhaust chamber, narrow upright drum and open round outlet.

Both preserve the complete original lower foundation, stone courses, arched fire mouths, grates and dressed cornice from `../../2026-10-09/blender-furnace-02/furnace-study.blend`. Original light rig, camera angle and AgX view transform remain. Native quarry material variation and forged-surface roughness accompany the new load-bearing upper forms. There is no raster painting, generative-image edit, external texture, or paid service.

## Projection and attachment contract

- Each PNG: 1024×1024 RGBA, full uncropped source, transparent background.
- Orthographic scale10.8; image bottom−1.8; source camera targets design height3.6. Same30-degree orthographic projection and vertical compensation1/cos30 as the production source.
- Nominal foundation radius2, original lower-source silhouette unchanged. The pre-existing arch/threshold overhang reaches measured radius2.375079; this is **not** a newly expanded runtime collision footprint. The original radius2.03 cornice is also preserved.
- Circular receiver radius1.6 retains the existing pipe contact at design height4.05. Receiver and bronze foot begin3.58, overlapping the original3.60 cornice by.02.
- Outlet stays centred at(0,0,7.2). Both lips have actual open centres and recessed dark floors; no emissive cap or new steam anchor.
- Complete height measured7.199999871. Existing split-band runtime registration and ownership must be retained by the integrator.

First source review rejected the solid-capped mouths. `rejected-capped/` retains those images and measurements. Final square mouth uses four supported rim walls; final round mouth uses a true annulus. The existing tiny receiver/cornice gap was closed in the same correction. Final PNGs were visually inspected after the correction; both show open mouths and distinct upper silhouettes.

## Source and budget

Generator: `Tools/ArtReview/BuildBlenderIndustrialFurnaces.py`. It loads the original blend without overwriting it and reuses only constructor function definitions from `BuildBlenderFurnace.py`. Each output blend contains the full editable scene and procedural materials.

Evaluated authoring meshes: variant0 **10,888 vertices /21,148 triangles**; variant1 **11,760 vertices /22,884 triangles**. These meshes are not intended for runtime import. `model-budget.json` records actual measurements; `render-verification.json` records PNG alpha bounds and SHA256 hashes. Runtime atlas memory, imported compression and visible neighboring-pipe ordering require Unity verification by the integrator.

```powershell
& 'E:/MobileGame/TestResults/tools/blender-4.5.9-windows-x64/blender.exe' --background --python Tools/ArtReview/BuildBlenderIndustrialFurnaces.py
```

Blender4.5.9LTS, Cycles48samples with denoising. No Unity Assets, scene, gameplay, phone or build changes were made by this source-art slice. These silhouettes are substantially more individual than the prior shared kettle hood, but the full map/reference-quality claim depends on actual Unity and device review.
