# Anvil working-face finish — 2026-09-29

Source ownership: FoundryRegionWorkshop.cs only.

The actual prior workshop-detail capture showed the anvil face/horn merging in value with adjacent stock. This pass recolours only the existing anvil-head top quad and horn top to restrained cool steel (.67,.70,.75), plus the existing near bevel quad to (.73,.75,.79). The dark waist, foot, horn side, parts bin, ingots, hammer and footprint remain unchanged.

No extra vertices, triangles, batches, textures, shaders or frame work. The head-top indices follow WorkshopMetalHull's two visible wall quads, four bevel quads, then top quad; the scope is local to this call, not a change to the common hull helper.

Pending root compile and real Unity recapture. Inspect for clearer working face at workshop closeup without a white/emissive-looking prop. No Unity or adb performed by this subtask.
