# Industrial furnace complex

Baseline: `2c3c83651faa1303162e39d1cd5b5ad8c436422d`.

The owner requested continued development in this Codex session, with up to five helpers and grouped visual tasks. Five helpers split tall native furnace sources, low retort sources, runtime atlas integration, independent source/contact review and independent integration review. At most three helpers execute concurrently; root alone runs Unity and owns deployment.

## Scope

Four individually authored Blender structures replace two duplicated boiler images: stepped square and ribbed tall exhaust assemblies, a folded low retort hood with seated inspection hatch, and a segmented low retort with servicing plate. Both source families keep the original firebox/foundation meshes, fixed isometric camera, lights, height envelopes and centred steam outlets. New receiver contact and hollow flue mouths are explicit native geometry. The first capped mouths and inherited receiver gap were rejected during independent source review.

The lower original decorative arch lips already overhang the nominal radius-two footing; they are retained, not expanded into new collision. Court, islands, pit, bridge, gate, pipes, drains, navigation, camera, scene and gameplay remain unchanged in this package.

## Runtime contract

Each family uses one 2048x1024 RGBA atlas of two untouched 1024-square native renders. Near assembly uses left tile; far uses right, selected from shared `BoilerCentres`. `PackIndustrialAtlases.py` verifies exact tile pixel equality and authored transparent margins; it performs no resizing or retouching. Atlas UVs replace whole-image UVs on the original cards. Two source materials and four band batches remain; existing resource ownership, fallback and depth/cutaway contracts remain. The old runtime hatch overlay is removed because the near retort now contains its hatch in the source bake.

Tall card: scale10.8, bottom-1.8, height7.2, split2.8. Low card: scale6, bottom-1.86, height2.28, split1.2. Imported texture GUIDs are preserved. Android contract:2048x1024 ASTC6x6,12mips,clamp,non-readable. Each source variant retains1024-pixel resolution. Calculated block payload per atlas including mips is1250704bytes versus626288previously: combined increase1248832bytes, excluding engine overhead. No claim about physical-device performance. Transparent margins protect normal-view sampling; the final1x1 mip necessarily averages the atlas.

## Reproduction

Run the two authoring scripts with Blender4.5.9 in background mode, then pack the PNGs:

```powershell
& 'TestResults/tools/blender-4.5.9-windows-x64/blender.exe' --background --python Tools/ArtReview/BuildBlenderIndustrialFurnaces.py
& 'TestResults/tools/blender-4.5.9-windows-x64/blender.exe' --background --python Tools/ArtReview/BuildBlenderIndustrialRetorts.py
python Tools/ArtReview/PackIndustrialAtlases.py
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryIndustrialProof.ps1
```

Portable Blender's local path is an execution convenience, not a repository dependency; another official4.5.9installation can run the same scripts. Original base .blend files are tracked. New editable variant blends, geometry bounds and source hashes live in sibling industrial-furnace-01 and industrial-retort-01 folders. The packing script writes review atlases only; importing them into the two existing Resources files is a separate reviewed step.

Proof capture is **after-only** and runs against the current runtime Resources. Twelve matched before images plus five diagnostics are unchanged baseline copies, with SHA256 provenance. Two additional furnace contact views are after-only. HUD is hidden and heroes are staged; these are actual Unity images, not phone screenshots. Final image review, test counts, resource observations and APK identity are retained under validation.

## Device and completion limits

No ADB action or fresh phone observation. Earlier concrete restart/install decision remains pending. Before future installation inspect fresh state and back up current original saves/telemetry; never restore historical snapshots. Device movement/crowd, first-ramp, shimmer and midrange checks remain open. Full reference fidelity remains unfinished; this package addresses the industrial structures framing the gate, not every remaining visual task or region encounter/boss progression.
