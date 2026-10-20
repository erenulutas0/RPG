# Reference retorts — native Blender source renders

2026-10-20. Two low retort renders that replace the 2026-10-11 pair in `Assets/_Project/Resources/FoundryShortBoiler.png`,
so the causeway retorts join the reference towers' family (`../reference-towers-01`): blackened iron, brass bands and a
ring of ten small glowing arched windows in a low drum; variant 0 carries a stepped ribbed dome, variant 1 a corbelled
parapet with ten merlons.

The basalt footing, masonry, squat segmental fire doors, grates and cornice are rebuilt by the original retort
constructors (`Tools/ArtReview/BuildBlenderIndustrialRetorts.py`, reused verbatim). Contracts kept and asserted by the
generator: height <= 2.28 (south structures stay squat), exhaust outlet (0, 0, 2.28), physical bounds equal to the
original source, same camera, ortho scale 6 and lights. No raster post-processing.

`rejected-flat-hood/` keeps the first render (dome read as a flat disc, windows too small). `model-budget.json` records
the evaluated authoring meshes (8,208 / 8,176 vertices — authoring only); `atlas-provenance.json` records the lossless
packing.

```powershell
& 'E:/MobileGame/TestResults/tools/blender-4.5.9-windows-x64/blender.exe' --background --python Tools/ArtReview/BuildBlenderReferenceRetorts.py
python Tools/ArtReview/PackReferenceRetortAtlas.py
```

Unity integration evidence: `../foundry-reference-retorts-01/`.
