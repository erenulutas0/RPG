# Pressure reservoir body study

Authored in Blender 4.5.9 LTS using the approved furnace study02 materials and lighting. This is an external art candidate, not proof of runtime or device integration.

## Files

- `reservoir.blend`: editable mesh, procedural material nodes, lights and matched camera.
- `reservoir.png`: 1024-square RGBA body bake, transparent surroundings.
- `projection.json`: exact projection bounds and evaluated authoring mesh budget.
- Source: `Tools/ArtReview/BuildBlenderReservoir.py`.

The script loads the committed `blender-furnace-02/furnace-study.blend`, replaces its mesh objects and writes only this directory. It uses no external image or paid service.

## Integration contract

Place the quad at the existing `Project(u,v)` tank anchor. Quad bounds relative to that anchor are x=-2..2 and y=-0.5..3.5 with full 0..1 UVs. The camera uses a 30-degree elevation, 4-unit orthographic span and a 1/cos(30 degrees) model-height compensation, matching the existing 2:1 ground projection. Do not crop transparent margins without recalculating the pivot/bounds.

The authored body reproduces the current pressure-vessel section dimensions: metal footing spans +/-0.76 in each design axis, its bottom/top are0.25/0.43; hull radius0.82, retaining-belt radius0.85; tapered crown and valve-seat top3.04. The metal body is the only replacement. Retain the existing procedural 2x2 masonry service footing, branch connection at1.72, relief stem beginning3.04 and wheel at3.35. No ground shadow, branch, stem or wheel is baked.

Existing anchors are(22.3,15.8) and(22.3,-5.8). Both can use the same image; their opposite branch directions remain runtime geometry. Retain distinct near/far painter relationships: the far tank appears behind the main; the near tank occludes the main. A shared source material alone does not guarantee correct ordering after static batching. Confirm each branch/body contact, footing contact and moving-actor cutaway in actual Unity graphics before accepting.

## Review and limits

The isolated rendered image was inspected. It supplies continuous rolled metal, understated warm reflected light and dressed edges matching the new boiler bakes, while keeping the established small utility-vessel silhouette. The authoring mesh has2,952evaluated vertices and5,756triangles; a runtime billboard need not import that geometry. The1024source can be capped at512after judging actual portrait sharpness. At source-study creation the candidate had not yet been compressed or installed. The subsequent production integration caps the runtime texture at512ASTC6x6; see ../pressure-assembly-01 for current verification and device status. Baked lighting is static; no scene-responsive light or new animated mechanism is claimed.
