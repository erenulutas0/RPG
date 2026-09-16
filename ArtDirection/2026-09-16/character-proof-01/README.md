# Vanguard and Cinder Mite — character proof 01

Original concept masters generated with the built-in image generation tool on 2026-09-16. This is a character design and browser scale study, not imported game art or an animation sheet. No runtime files or APK were changed.

## Files and selection

- `vanguard-body-v2.png`: selected **candidate** body. Blue steel, silver bevels, sparse brass trim, a shorter split cape and exposed boots. Hands are empty so Sword, Staff and Daggers can remain separate equipment. One rear three-quarter pose only.
- `cinder-mite-v1.png`: selected **candidate** enemy. Low basalt quadruped, broad orange seams and a compact flame crown. One front three-quarter pose only.
- `vanguard-master-v1.png`: exploratory predecessor retained for provenance. Rejected for integration because the weapon/shield forms merge and the cape hides too much of the legs.
- `PROMPTS.md`: exact prompts, reference roles and original generated file paths.
- `asset-metadata.json`: dimensions, SHA-256 hashes, transparency counts and alpha bounds. All generated PNGs are copied unchanged; browser clipping does not alter the files.
- `index.html`: interactive detail, scale, silhouette, foot-point and crowd review, beside an earlier actual Unity render.

## Review and scale

Serve the dated ArtDirection folder, so the preceding runtime screenshot remains available:

```powershell
python -m http.server 8341 --bind 127.0.0.1 --directory E:/MobileGame/ArtDirection/2026-09-16
```

Open `http://127.0.0.1:8341/character-proof-01/`. The virtual floor is 1080 pixels wide, fit down to the panel. Default visible heights are 165 pixels for the hero and 60 for the Mite, based on the current 32x44 and 15x16 sprite canvases at 32 PPU and camera width 9. These are **candidate visible-height targets**, not an exact match to the old sprites' opaque bounds. Alpha >16 bounds normalize the new images for display; the foot markers use bottom-centre of those bounds, not validated production pivots.

The close Mites have foot points 54 source pixels apart vertically: 0.9 floor units at width 9 and half-depth projection. This illustrates possible depth occlusion after Opus's physical-spacing fix. Positions are hand-staged, not a simulation or a guarantee about every legal pack. No shadows, movement, weapon layers, attacks or collision are rendered.

At reduced size, the hero's blue cape and pale helmet/shoulders carry recognition. Mite flame versus dark stone survives better than the fine cracks and eyes. Detail magnifications on the left are deliberately different; compare relative size on the right. The schematic quiet floor isolates silhouette but does not establish contrast over the full live furnace scene.

## Production limits and next slice

These are smooth painted, antialiased masters. They do not satisfy the current point-filtered 32-PPU sprite contract directly, and this proof does not approve a global PPU/filtering change. Next, compare one reduced sprite sample with a denser candidate at the same world size in Unity before choosing an import contract. Keep feet, silhouette and floor contact ahead of micro-detail.

Then prove one hero and one Mite with fixed foot pivots, opposing facings, separate equipment anchors and a small idle/move/strike sequence. Frame consistency, sorting, weapon alignment and cached sprite ownership must be checked before expanding the roster. A generated still is not evidence that these animation requirements are solved.

## Verification

Browser-reviewed at the normal desktop viewport and a temporary 390x844 viewport (375 CSS pixels available after the scrollbar). All referenced images loaded, no horizontal page overflow at the narrow width, no captured console warnings/errors. Actor scale 125% showed 206/75-pixel targets; crowd toggle changed eight Mites to two; silhouettes and foot points displayed correctly. Defaults restored to 100%, eight Mites, normal colour and no foot markers. Narrow detail views stack vertically. Original PNG hashes checked against metadata.

No Unity suites, APK build, install or phone interaction was performed for this art-only slice. Latest runtime verification remains Opus's spacing slice (`c94856c`, device documentation `a51ed75`): .NET 272, EditMode 284, PlayMode 83 and matching S23 APK hash, as recorded in docs/21-23. Those results were not rerun here.
