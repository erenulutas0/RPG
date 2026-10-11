# Workyard clusters — authored source refresh

Two Blender-authored clusters are recomposed within their existing shelf, height and fixed-camera projection contracts. This is source evidence; Unity integration and runtime acceptance belong to the combined package proof.

- Stock: five staged billets in a retaining cradle, two upright wound-strip coils held in low cradles, and five hollow pipes in a triangular bundle on supported saddles. The quenching tank and hanging tool rack are retained. Pipe centres use the radius/spacing contact calculation; billet layers touch rather than float.
- Casting: one broad active sand mould, the angled removed half, and one taller closed strapped flask replace the three similar mould boxes. Five finished billets are grouped on the existing rear rack with retaining stops. Press and bench remain; the rammer moves into the free gap between the two open moulds.

Generators: `Tools/ArtReview/BuildBlenderStockStation.py` and `Tools/ArtReview/BuildBlenderCastingBench.py`. Portable Blender 4.5.9, original study02 materials/lights, 1024-square RGBA transparent renders. Native geometry only; no image generation, raster postprocessing, floor or shadow catcher. Each subfolder retains the editable `.blend`, unchanged rendered PNG, projection/bounds metadata and successful render log. Previous September/October authoring outputs are preserved.

Both generators assert physical equipment bounds, existing depth height, and projected bounds within the unchanged camera frame. Stock bounds: u2.30..6.80, v18.78..22.10, height0.165..1.30 inside the existing [2,18.5,7,22.4]/1.6 contract. Casting bounds: u-17.56..-13.42, v19.425..22.07, height0.18..2.47 inside [-18,19.1,-13,22.4]/2.5. Stock's existing rounded tool-rack feet extend below the shelf top; this is retained rather than a new floating support. Exact values and normalized screen extents are recorded in each `projection.json`.

Source images were visually inspected: silhouettes are separated, the active mould cavity and closed-flask distinction remain readable, and the stock bundle has grounded supports. Whole-map benefit, interaction with the live crane/wood/rail, 512-pixel import readability and actor depth still require actual Unity capture. These local changes do not resolve the reference's broader workyard layout differences.
