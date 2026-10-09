# Authored salvage workstation

One fixed-view, metal-only group containing the existing anvil and resting hammer, steel parts chest and five staggered ingots. Built in Blender 4.5.9 LTS from authored geometry using the furnace study02 scene, procedural materials and lights. No purchased imagery, generated raster source or new service is required.

## Source and output

- `Tools/ArtReview/BuildBlenderSalvage.py`: reproducible authoring script; writes only this directory.
- `salvage-kit.blend`: editable geometry, materials, lights and orthographic camera.
- `salvage-kit.png`: 1024-square transparent RGBA render.
- `projection.json`: placement and authoring budget.

The authoring mesh evaluates to 3,038 vertices and 5,984 triangles. The intended game representation is one quad, not an imported copy of that mesh.

## Exact runtime placement

Anchor at `Project(-26.725f, 6.7f)`. The full image quad extends x=-2.75..2.75 and y=-1.75..3.75 relative to this ground anchor, with untrimmed UVs0..1. The orthographic span is 5.5; the camera target projects one unit above the anchor. The 30-degree camera and model-height compensation `1/cos(30 degrees)` match the game's 2:1 ground projection.

Use the existing whole-shelf depth contract explicitly: design box `(-29,5,-24.45,8.4)`, height1.6. All three metal groups already belong to that station; a combined card does not merge previously independent actor-depth landmarks. Preserve existing painter relationships with rear clutter, stone bedding and rail scenery; review the full station and the adjacent walkable portrait point in Unity. Do not assume a new material at an equal rank necessarily sorts identically to the old wide iron batch.

Replace only the five ingot meshes, parts chest and anvil metal/resting hammer. Keep the procedural salvage masonry shelf and anvil stone bedding: the metal anvil foot starts at0.48, while chest and bottom ingot course start at0.20. The anvil hammer reaches1.43, chest lid0.92 and upper ingot course0.63. Horn tip and main footprint positions follow the current code. The background barrels, wood crates and sack remain unchanged.

## Art review and limits

The isolated render was inspected. The broad worked anvil face is quieter than the initial trial, with a genuine square hardy hole and a rounded tapered horn. Chest straps sit above the lid rather than on a coplanar surface. Cast ingots retain their original five-piece arrangement. No emissive parts, floor plane, masonry or shadow catcher are included, so transparent surrounding pixels cannot paint a false shelf contact shadow.

This is an isolated source-art review. Runtime compression, scene ordering, footprint/cutaway ownership, actual portrait visibility, renderer cost and device acceptance require separate integration verification. Baked lighting is static and the whole source is specific to this stationary arrangement. Full reference quality is not claimed.
