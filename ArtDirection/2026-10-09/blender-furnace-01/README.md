# Blender furnace workflow study — 2026-10-09

One external, editable furnace study tests whether a free Blender workflow is useful for Cryptforge. No live game asset, camera, gameplay, save or device build is changed. No new subscription or paid API was used; this does not mean production time is free.

## Evidence and decision

- `unity-current.png`: actual production `BuildTallBoiler`, isolated in the existing Unity scene with unchanged production materials and structure shader.
- `unity-blender-study.png`: the transparent Blender render displayed by an ordinary Unity SpriteRenderer, at the same orthographic camera scale and background.
- `unity-current-small.png` / `unity-blender-small.png`: actual 360px Unity renders. These are small-size readability checks, not phone screenshots.
- `furnace-blender.png`: original transparent Cycles render, 1200px square.

The candidate shows clearer rounded metal, bevel highlights and cohesive lighting. The production version has richer arched openings and architectural variation. The candidate is too regular and clean; its fireboxes read as uniformly bright rectangles. This is **not an accepted replacement** or proof of full-map reference fidelity. Lighting and geometry both differ, so it is a rendering-workflow comparison, not a material-only A/B.

Next useful experiment: preserve the existing arched-opening design in Blender, add restrained soot/heat variation, then prove ground contact and actor depth in one actual map placement. Choose a baked layered sprite or optimized mesh only after that review. Do not migrate the whole map or buy a service on the strength of this study.

## Editable sources and budget

`furnace-study.blend` contains authored geometry, procedural node materials, lights and camera. `furnace-study.fbx` is a mesh study export: it does **not** carry the rendered node-material appearance into Unity. Evaluated budget: 9,512 vertices / 18,432 triangles before any mobile reduction. Camera elevation 30 degrees matches the game's 2:1 ground projection; vertical geometry is compensated by 1/cos(30°). Full orthographic height 10.8 units.

The shown sprite is uncompressed. It has no production depth/cutaway, atlas, mobile compression, ground-contact or performance acceptance. A real integration still needs those checks. No new APK/device test; installed build remains the previously verified material-readability package (`8ffec82`). The prior ramp first-use hitch remains open.

## Reproduce

Use official Blender 4.5 LTS (this run: 4.5.9), then from the repository root:

```powershell
& '<Blender path>/blender.exe' --background --python Tools/ArtReview/BuildBlenderFurnace.py
pwsh -NoProfile -File Tools/ArtReview/Run-BlenderFurnaceProof.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

Portable Blender was downloaded from `https://download.blender.org/release/Blender4.5/` and its ZIP SHA-256 checked against the official checksum manifest. Binaries/downloads stay in ignored `TestResults/tools`, outside committed project assets. No system installation.

The capture wrapper temporarily adds one PlayMode test and always removes it. Because ImageConversion is absent in this project, input is losslessly decoded to BGRA outside Unity; capture uses raw BMP converted losslessly to PNG. No project module/package changes. One initial compilation failure exposed that missing module; after adapting the tool, the actual graphical PlayMode capture passed (1/1). Static integrity passed: 552 asset/folder GUIDs, 1,099 package GUIDs, 568 scene objects/components. Full gameplay suites were not rerun for this external tooling-only change.
