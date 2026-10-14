# Authored outer gate masonry bay

This is an editable Blender source and fixed-view transparent render of one complete existing outer buttress above its deck. It was produced from code-native geometry and procedural stone nodes. It is not an edited concept image, and it does not establish phone acceptance.

## Reproduce

From the repository root, using the existing portable Blender installation or another official Blender 4.5.9 LTS installation:

```powershell
& 'TestResults/tools/blender-4.5.9-windows-x64/blender.exe' --background --python Tools/ArtReview/BuildBlenderGateMasonry.py
```

The generator opens the tracked furnace study02 blend to reuse the established camera/light pipeline, removes its mesh objects, authors a new masonry model and stone material nodes, and writes only this external art directory. The resulting `gate-masonry.blend` remains editable; `gate-masonry.png` is the 1024 x 2048 RGBA master. `projection.json` records the actual evaluated geometry and camera contract. No Unity import, build, device operation or save change is performed by the generator.

## Geometry and projection contract

The original near footprint is `(27.05,-6.60)..(30.90,-4.15)`. The bay extends from elevation 0 to 7.35. Its original foundation from -3.45 to 0 remains native. The bearing heights are 0, 0.48, 2.05, 2.85, 6.55, 6.82, 7.20 and 7.35. The shaft retains its original tapered octagonal envelope, pointed opening/frame, 0.36 blind depth and quarter-depth throat. The model's actual tier rings and frame/throat/backing stations are checked against evaluated vertices, not only copied into metadata.

The authored difference is broad individual stone construction, physically recessed bed joints, a fitted pointed head, dressed crown contacts and a cool basalt material with bounded pores and bevel shading. Individual stone boundaries retain the original contact stations; the incised bed starts just inside each boundary. Dark joint backing sits behind the dressed surface rather than duplicating it on the same plane.

The card anchor is `(28.975,-5.375)`. Relative projected bounds are **x -2.5 to 2.5, y -1.25 to 8.75**, a 5 x 10 unit card. The camera direction is `(-12,-12,sqrt(96))`, with 30-degree elevation, vertical target 3.75 and orthographic scale 10. Model height uses `1/cos(30 degrees)` compensation. The bake excludes the map's +1.25 ramp lift; Unity's `Project(anchor)` already includes it. Adding the lift again would detach the bay from its foundation.

The far counterpart may use the same unmirrored source with anchor `(28.975,15.375)` and footprint `(27.05,14.15)..(30.90,16.60)`. Broad cool light and restrained neutral warm bounce avoid baking a strong local flame specific to one side. It must retain its existing separate painter rank/depth owner. Neither copying nor mirroring a texture is evidence of correct runtime sorting; root owns actual integration checks.

## Resource tradeoff and limits

The proposed maximum import dimension 1024 produces a 512 x 1024 texture. ASTC 6x6 plus a complete mip chain is approximately 0.30 MiB of texture blocks; actual import/build memory must be measured. The runtime replacement needs one borrowed texture and one scene-owned source material plus its cutaway variant; placing it at both existing painter ranks is expected to add two batches. Replacing the original native bay faces should reduce mesh vertices. These are anticipated costs, not a measured runtime count or frame-time result.

The bake carries fixed view-dependent lighting and visible depth cues, not a real-time volumetric stone model. Existing camera orientation is essential. It cannot establish mobile shimmer, crowd readability or performance from the offline render. The gate's inner piers, curtain walls and header remain separate architecture; replacing the outer buttresses alone does not complete reference fidelity.

`first-proof/` preserves the initial rejected PNG/metadata. That trial retreated the complete stone boundary into the bay by 0.035, which moved niche throat/contact stations despite keeping aggregate bounds. The accepted external source preserves those stations and recesses only the interior bed. Root's actual Unity capture determines whether this proof is suitable for integration.
