# Outer buttress massing candidate

Native Blender 4.5.9 LTS render completed successfully. Source: `Tools/ArtReview/BuildBlenderGateButtressMassing.py`. The prior generator, prior gate-depth bake and all Unity Assets remain untouched by this work.

The complete existing shared buttress is rebuilt with a deeper raked lower haunch ending at 3.65 instead of 2.85, a shorter blind upper bay, wider solid piers around the pointed opening, and a 0.48-deep recess instead of 0.36. The broad crown, external foundation footprint, base at 0, upper bearing at 6.55, crown stages and 7.35 overall height remain. The camera, 5-by-10 card field, anchor, unmirrored orientation, lighting and procedural stone family are retained; runtime assumes the existing 0.62 material gain.

## Measured native output

- Editable `gate-masonry.blend`: native geometry, materials, lights and camera.
- Direct native `gate-masonry.png`: 1024 x 2048 RGBA, 2,152,206 bytes. SHA256 `07a65fe8c065c686cdb1ab509fffb8efed093ef718e9a0d17a9e23e6617cde70`.
- `projection.json`: actual evaluated mesh counts/bounds, actual cap bearing measurements, explicit changed internal contact, camera/card metadata and rendered alpha bounds.
- `blender-buttress.log`: sanitized successful assertion/render summary and clean Blender exit; raw diagnostics stay under ignored `TestResults`.

Offline source geometry is **1,314 vertices / 1,584 triangles**, versus the preceding source's 1,242 / 1,476. The added 72 / 108 belongs only to Blender source, not the runtime card mesh. All 142 construction/niche contact stations are present in actual evaluated geometry with zero reported station error. Measured joins have zero vertical gap: five bear on the entire upper foot; the unchanged overhanging crown bears on 71.174% of its footprint. The changed shoulder-to-shaft joint measures 3.649999883 rather than claiming the old internal 2.85 station is retained.

Actual native bounds remain `u=27.05..30.90`, `v=-6.60..-4.15`, height `0..7.35`, within float precision. Projected extrema remain `[0.0799784, 0.0199945, 0.9200212, 0.9100410]` in the unchanged render field. Nonzero rendered alpha lies within pixel box `[80,40,942,1864]` (bottom-origin), clear of every image edge.

## Visual and runtime evidence boundary

Direct inspection of the prior and candidate native renders shows the intended structural hierarchy: the sloping lower mass is taller and more substantial, while the shorter, deeper opening leaves wider supporting stone around it. The crown and lower foot retain their prior dimensions. This is a whole-form replacement, with no appended trim layer or extra prop.

The candidate is ready for root-owned replacement/import of the existing `FoundryGateMasonry.png`. Expected runtime delta is **zero vertices, renderers, texture resources and material resources**, retaining two existing four-vertex cards. The baseline loaded texture is **512 x 1024 ASTC6, 11 mips, non-readable, 630,880 Editor texture bytes**. Those values must be measured again after import; source PNG dimensions and native Blender geometry alone do not establish runtime cost.

Source and native render are frozen for integration. Generator SHA256: `63c44979a98259b75e3fcfad45701d022c5916c696a477b5e15a5a6015b36fba`; editable blend SHA256: `cfea00fba6901bf96a066c5bbf10a9691265e7d97c0813b730cc01156df8e435`. Root inspected the candidate and approved it for actual Unity inspection. Integration copies only the final PNG while preserving the existing resource meta byte-for-byte. The runtime card, material gain, depth owner and native fallback stay unchanged. Check the complete south-feed body/collars and curtain alpha coverage in the matched views, together with the niche and vertical pier readability.

Actual matched Unity gate and full-map acceptance is pending. This render does not prove final in-scene value, pipe occlusion, curtain joins, phone readability, device performance or reference parity. No Unity, Android build, phone operation or git mutation was performed by this source task.

Reproduce from the repository root:

```powershell
& 'E:/MobileGame/TestResults/tools/blender-4.5.9-windows-x64/blender.exe' --background --threads 12 --python 'E:/MobileGame/Tools/ArtReview/BuildBlenderGateButtressMassing.py'
```


## Final root integration

The exact PNG above was imported with the existing meta file unchanged. Final actual Unity capture confirms 512 x 1024 ASTC6, 11 mips, non-readable, 630,880 Editor texture bytes, two inherited card batches and one shared variant. Main and independent image reviews accept the outer-bay form gain; curtain contribution is subordinate and partly occluded. See ../validation/verification.md and ../validation/independent-image-review.md for combined evidence. No physical phone acceptance is claimed.
