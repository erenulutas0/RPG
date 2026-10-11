# Industrial low retorts — editable authored source

Two original native Blender models replace the smooth kettle-like upper dome while retaining the existing short boiler's base, low fire mouths, placement, and projection contract. No Unity Assets are modified by this source generator.

- `variant-0`: folded octagonal hood, flat service deck, seated inspection hatch with gasket, hinges, handle and locking dog. This includes the inspection detail that replaces the former separate runtime hatch.
- `variant-1`: twelve-panel pressed hood, attached radial stiffeners, seated flange shoes and a recessed-profile service plate. Its service plate and bearing positions are evaluated against the polygon face's apothem, not a circular approximation.

Both models preserve the two existing segmental fire mouths and their original masonry/grates. Roof forms use procedural rolled-steel weathering and restrained old bronze fasteners. The second model has a different hood silhouette and hardware arrangement; it is not a recolour.

## Projection and contacts

Each render is 1024 x 1024 transparent RGBA, with orthographic scale6 and the existing30-degree camera. Vertical geometry uses compensation1/cos(30deg). Local image bounds remain x[-3,3], y[-1.86,4.14]. Blender camera target remains(0,0,1.14*compensation).

The centred smoke outlet ends at design height2.28. Evaluated geometry is asserted at or below2.2801 and fully inside the image. The radius2 basalt footing and original decorative fire-mouth overhangs remain unchanged. The fire mouths extend beyond the circular plinth; this is preserved existing source geometry, not a new collision footprint. `original-contract-comparison.json` compares evaluated bounds against the archived original `.blend` directly.

The existing1.8 runtime service socket remains external, on its existing cornice/corner contact. No additional socket is baked here, and no world anchor is changed. Variant0's hatch gasket starts at the flat roof1.98; its lid and hinges are supported by that deck. Variant1's service door is seated on the actual inclined polygon face. Both flues are carried by their roofs.

## Reproduce

```powershell
TestResults/tools/blender-4.5.9-windows-x64/blender.exe -b --python Tools/ArtReview/BuildBlenderIndustrialRetorts.py
```

The generator loads the existing `blender-furnace-02/furnace-study.blend` procedural material/light scene, replaces its meshes, saves two new editable `.blend` files and renders the source PNGs directly. It also reads the archived `short-boiler-01/short-boiler.blend` for contract comparison. Neither original source is overwritten. No downloaded textures, generated raster inputs, or raster postprocessing are used.

Per-variant evaluated mesh counts and projected/physical bounds are in the budget JSON files. These counts describe the authored Blender meshes; Unity receives image cards, not those meshes. Renderer/memory/phone performance and final mobile legibility require root's separate Unity validation.

Root owns atlas packing and import. Preserve source PNG pixels when arranging variant0 left / variant1 right in a2048x1024 atlas. Render inspection alone is not Unity or device acceptance.
