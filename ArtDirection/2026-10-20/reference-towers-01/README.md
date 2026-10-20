# Reference furnace towers — native Blender source renders

2026-10-20. Two tall-furnace renders that replace the 2026-10-11 industrial pair in the runtime atlas
`Assets/_Project/Resources/FoundryBoiler.png`. They move the upper towers toward the approved concept's towers:
stacked blackened-iron drums ringed with small glowing arched windows, brass bands and a crenellated (near tower) or
stepped, ribbed dome (far tower) crown.

- `variant-0.png` / `.blend`: near tower — lower and upper window drums (10 windows each), three brass bands, corbelled
  parapet with ten merlons, open round flue.
- `variant-1.png` / `.blend`: far tower — one window drum (9 windows), stepped ribbed dome with a six-window lantern,
  open round flue.

The complete original lower foundation (stone courses, arched fire mouths, grates, cornice) is retained from
`../../2026-10-09/blender-furnace-02/furnace-study.blend`, with the same camera, light rig and AgX view.
Contracts kept: foundation radius 2, receiver radius 1.6 with the pipe contact at height 4.05, open flue centred at
(0, 0, 7.2), 1024×1024 RGBA, orthographic scale 10.8. Iron is neutral-warm (the source "forged blue" read teal under the
court light). Everything is native mesh/procedural material work; nothing is painted onto the renders.

Rejected iterations are kept: `rejected-small-windows/` (windows too small for the game camera) and
`rejected-light-iron/` (upper iron ~35 % lighter than the old upper; read beige in Unity). `model-budget.json` records
the evaluated authoring meshes (15,264 / 15,304 vertices — authoring only, not runtime geometry);
`atlas-provenance.json` records the lossless packing hashes.

```powershell
& 'E:/MobileGame/TestResults/tools/blender-4.5.9-windows-x64/blender.exe' --background --python Tools/ArtReview/BuildBlenderReferenceTowers.py
python Tools/ArtReview/PackReferenceTowerAtlas.py
```

Unity integration evidence: `../foundry-reference-towers-01/`.
