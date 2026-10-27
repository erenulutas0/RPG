# Ilya Kest, the Cinder Arcanist: one Blender model, pre-rendered to sprites

2026-10-27. The second hero (docs/45, bible docs/44 section 2.2) gets its own look. No painted art exists and none can be
generated here, so Ilya is a stylised 3D model built entirely by a script and rendered to sprite frames. Every frame
and every facing comes from the same model, so details cannot drift or boil between frames - the owner's complaint
about the hand-edited Vanguard poses.

## Sources

Everything is authored in `Tools/ArtReview/BuildBlenderArcanist.py`: lofted and deformed geometry, vertex weights
computed from the geometry, a toon shading node group, an inverted-hull contour, the rig, and every pose as a
function of the frame. No generated imagery, no external textures, no paid service. The comparison floor in the
review images is cropped from the existing in-game capture `ArtDirection/2026-10-24/foundry-pit-eruption-01/pit-spill.png`;
the molten patch is synthesised in the pit's sampled colours.

- `ilya-kest.blend` - the model, materials, rig and one action per clip (scrub them on the rig `Ilya`). The flame
  flicker and flares are driven by the script at render time, not keyed.
- `frames.json` - every packed frame: atlas, rect, pivot, the crucible's screen anchor, and the memory figures.

## Design (docs/45 P1)

Slim, hunched and quick, with a large head for a small figure. A tall asymmetric cowl whose crooked tip leans over to
Ilya's left and back and droops, burnt to pale ash toward a live ember at the point, like a wick. Ash-white robe
(#D8CFC2) open at the front over dark trousers, soot trims (#201B22) on the front edges and a ragged, scorched hem with
smouldering ember threads; big bell sleeves with soot cuffs. A soot mantle with an ember sigil on the back (a ring
round a flame). Ember orange (#E77232) only as accents: the threads, the sigil, three glowing flasks on a belt slung
low over the hips. Brass goggles pushed up on the brow, the right lens holding a small living flame (it flickers in
the idle), the left lens cracked. A long, crooked, cracked wooden staff bound in brass, glowing in its cracks, with a
brass crucible head, prongs, embers and a small flame.

One departure, made in review: the cowl and mantle are soot-dark, not ash. With an ash hood the figure read as a
single pale column and as a tall pale pointed hood over a pale robe; the dark cowl gives three readable shapes (cowl,
mantle, robe), frames the face, and is very legible on orange. `--hood light` reproduces the rejected variant.

## Camera, light and render

- Orthographic, looking north and down 30 degrees: the game's 2:1 floor (`ArenaFloor.DepthScale`), as
  `BuildBlenderFurnace.py`. Heights are modelled in screen units and the rig is stretched by 1/cos 30.
- Five facings (south, south-east, east, north-east, north) by turning the rig; the camera and the key light never
  move, so the light comes from the screen's upper left in every frame. The game mirrors the east facings for the west.
- The root stays on the floor origin, which projects to the same pixel corner in every frame: the sprite pivot.
- Cycles CPU, materials emit their own toon shading (no lights): soft painted value steps under a strong top-left key
  (the planes turned full to it lift further, as on the painted Vanguard), a brush-like value drift fixed to the
  undeformed surface, deep baked occlusion in the folds and under hood, mantle and belt (rays from every vertex in the
  stance), a cool rim, a thin warm rim from behind on the lower right (the foundry's fires), a faint ember bounce from
  below, brass highlights with pin-point glints, and a soot gradient climbing the robe toward its scorched hem.
- The crucible is a light: a warm point placed at the staff's head every frame and scaled with its flare lights what
  faces it (the cowl's side, the shoulder, the fist, the brass cup); its embers and flame are emissive cores; and the
  pack step lays a soft ember halo round it after the outline (no dark ring), wider and stronger at the strike's and
  the Vent's flare, nearly out in death.
- Rendered at 4x (880 px for 2.2 units) and box-filtered down in linear light with premultiplied alpha, then given a
  one-texel dark outline: 100 texels a world unit. The cyan pivot marks on the review contact sheets are drawn by
  `ReviewArcanistSprites.py` only; the renders and the atlases contain none.

## Clips

| Clip | Frames | Timing |
|---|---|---|
| idle (breath, lens flame flicker, hood tip sway) | 8 | 5 a second, loops |
| walk (robe panels and hem, hood tip follow through) | 8 | by distance: 1.2 floor units a cycle of two steps |
| walk strike (the walk's legs under the strike's arms) | 8 | by distance, while a strike lasts |
| strike (crucible whipped forward and up, flare, overshoot, recoil) | 5 | 20 a second (0.25 s, shorter for fast weapons) |
| cast, Crucible Vent (gather, both hands push, eruption flare, recover) | 5 | 14 a second |
| hit (flinch) | 3 | 20 a second, standing idle only |
| death (sinks into the robe, staff topples, lens flame dims) | 5 | 9 a second, holds the last frame |

210 frames in all. Root in place throughout.

## Atlases and memory

`Assets/_Project/Art/Characters/CinderArcanist/CHR_CinderArcanist_{0,1,2}.png`: 1024x1024, 1024x1024 and 1024x652,
2,764,800 texels at 91% fill. As ASTC 4x4 (8 bits a texel, the Android override in each .meta): **2.64 MB**, against
the 4 MB budget (RGBA32 would be 10.5 MB). Sprite rects and pivots are in each texture's .meta; the art set
`CinderArcanistArtSet.asset` (a `RenderedCharacterArtSet`) lists the frames per motion and facing. Bilinear, no mipmaps,
not readable, 100 pixels a unit; the S23 shows 120 pixels a unit, so Ilya is drawn at 1.2x (the painted Vanguard,
at 128, slightly below 1x).

## Unity

`HeroDefinition._renderedArt` (set on `Hero_CinderArcanist.asset`, empty on the Vanguard). `HeroLookView.Start` hands a
hero with such a set to `RenderedCharacterAnimator`, which steps the look aside as the development puppet does and
draws the frames beside the body; the frame rules are `RenderedCharacterPlayer` (pure, `Scripts/Art`). The six loadout
renderers stop drawing: the staff is in the render. Hit flash: `Resources/RenderedCharacterFlash.shader`.

## Reproduce

```powershell
& '<Blender 4.5.9>/blender.exe' --background --python Tools/ArtReview/BuildBlenderArcanist.py   # about 2 minutes
python Tools/ArtReview/PackArcanistSprites.py                                                   # atlases, metas, set
python Tools/ArtReview/ReviewArcanistSprites.py --out <dir>                                     # GIFs, sheets, comparisons
```

The raw 4x frames go to `TestResults/arcanist-render` (ignored). Re-packing keeps every GUID.

## Limits and next steps

- The value range is now close to the painted Vanguard's, but the surfaces stay cleaner and more matte than its
  painted metal; the robe could take deeper modelled folds and a painted cloth texture. The soot-dark cowl sits near
  the dark stone floor's value and separates there by its outline, rim and ash tip (on orange it reads at once).
- The flame in the lens is about 3 pixels on the phone: it reads as a living ember in the goggles, not as a face. The
  four flame faces (docs/45 P4) would be a separate small overlay.
- Mirroring the west facings flips the light and the asymmetric details (hood lean, flame lens) to the other side, as
  for the Vanguard; three more facings would cost about 1.6 MB.
- The walk is a brisk 1.2-unit stride; at the foundry's 3.5 units a second it steps about six times a second.
