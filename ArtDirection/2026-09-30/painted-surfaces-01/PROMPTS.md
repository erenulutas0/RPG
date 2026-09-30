# Painted surfaces 01 — ChatGPT texture brief (2026-09-30)

Goal: move the foundry map toward the approved reference
(`ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`) by **painting more and modelling less**. The procedural
geometry stays as skeleton and collision; its surfaces get painted textures made for those exact shapes, the way the
gate and furnace facades already are (`FoundryRegionView.cs` `BuildGate` / `PaintFace`: one quad, full 0..1 UVs, clamp).

Evidence used: `ArtDirection/2026-09-30/foundry-floor-01/after/` (start, point-0..4, overview, detail shots) and
`device.png` (S23). Numbers below were read from the code, `.mat`, `.meta` and PNG headers on 2026-09-30; if another
agent changes `FoundryRegion*.cs` first, re-check the line references before integrating.

> Integration touches art code (`Scripts/UI/Arena/FoundryRegion*.cs`, `Scripts/Art/Unity/PlatformMaterialSet.cs`).
> Route it per the project's art-ownership rule. Nothing in this folder changes code or assets. Do not import textures
> while a Unity batch test run is in flight.

---

## 0. Facts every prompt depends on

**Camera.** Orthographic, `_visibleWidth: 9` world units across 1080 px on the S23 → **120 px per world unit**.
The HUD leaves a free band of roughly 1080 × 1600 px.

**Floor projection.** The map ("design") plane is rotated 45° and squashed 2:1 vertically by the mesh
(`Project(u,v)` = `ToFloor` then `y*0.5`). So anything that lies on the floor must be painted **strictly top-down
orthographic** (as if scanned). The mesh does the isometric squash. Never paint floor art in isometric.

**Vertical faces.** Walls, facades and parapet faces are straight elevations. The mesh shears them along the wall's
slope. Paint them **straight-on, no perspective, no visible top or side**.

**Lighting is in the code, not the texture.** Every material uses `Sprites/Default` (built-in fileID 10753), or the
`Cryptforge/Foundry Cutaway` variant for structures at order ≥ -14. Both are unlit. Light comes from vertex colours:
`FloorLight` (furnace warmth, patina, soot, contact shade, edge darkening), per-face shade on `Block` /
`ArchitecturalTier` (light from screen right), and per-facet shade on round hulls. So **textures must be evenly lit
albedo**: soft ambient light from the front or top, no cast shadows, no one-sided highlights, no baked glow pools
(except fire that belongs to the object itself).

**ChatGPT output sizes.** Currently 1024×1024, 1536×1024 or 1024×1536. Transparent backgrounds can be requested.
Seamless edges, exact grids and exact radii come out only approximately, so plan for a short fix-up pass (below).

**Renderer budget, a hard test.** `Tests/PlayMode/FoundryRegionTests.cs` asserts ≤ 17 MeshRenderers in the region, and
the region already has 17 (`after/static-budget.txt`). It also asserts exactly **one** `Foundry floor decals` renderer.
Each batch is one material × one sort rank. So a new material either replaces an existing batch or costs a renderer and
a deliberate raise of the cap. Each surface below states its renderer delta. SpriteRenderers are not counted.

### How to run each prompt (owner steps)

1. Start one ChatGPT image chat for the whole set and attach `foundry-v1.png` and `Assets/_Project/Art/Props/Foundry/GateFacade.png`
   first, with: *"These are style and palette references only. Do not copy their composition."* Then paste one prompt
   per message.
2. Check the output size. Save the untouched output plus the prompt text in `painted-surfaces-01/sources/`.
3. **Tiling textures:** open in Photopea or GIMP, Offset by half width and half height with wrap-around, heal the cross
   seam, then offset back. Preview 3×3 (GIMP: Filters → Map → Tile) before accepting.
4. Resize with bicubic to the deliverable size and save PNG (RGB, or RGBA where alpha is stated).
5. Place the file and set its import settings as listed in its section.

---

## 1. Ranking by impact at the portrait gameplay camera

| # | Surface | Why this rank (from the captures) | Today | Fix type |
|---|---|---|---|---|
| 1 | **Court flagstones** (floor tile) | Fills ~80 % of the free band at start, point-2 and on the device, and 45–60 % at points 1, 3 and 4. It reads as a uniform grid of grey 2×1 bricks, where the reference has irregular violet-slate flags with chipped edges. | Fine speckle texture (#5B5A5B) that is sub-pixel on a phone; flags are geometry and vertex tone only. | Texture swap + small code simplification |
| 2 | **Court seal medallion** (decal) | The view every run starts in: the ring spans ~946 of 1080 px in start.png and device.png. Today it is a faint grey ring with dark wedges; the reference has a gold-lined star. | ~150 vertex-alpha quads sampling the coping texture | New painted decal, removes geometry |
| 3 | **Bridge deck** | ~55 % of point-0. It is the flattest surface in the build: blank grey "concrete" with 12 dark lines and 2 brass lines. The reference has coursed flags with bordered lanes. | Court paving texture at world UVs, diagonal to the bridge | New texture + own UVs |
| 4 | **Rim trim sheet** (kerb course, brass inlay, iron strips, parapet face) | A row of dark "vent slots" runs along every rim in point-1, point-3, point-4 and shaft-detail. It is a mapping artefact: the 0.23-wide border strips sample the bottom-left corner of `ENV_Platform_Coping.png` with per-quad UVs. The parapet is 0.28 tall (34 px) and reads as a thin line. | Coping texture with local UVs; parapet sides are one 0.12 slice of the masonry texture | Trim-sheet texture + UV code |
| 5 | **Foundation / sidewall masonry v2** | ~30 % of point-1 and the bridge end in point-0; it dominates the overview silhouette. Small blue bricks (~36 px courses) with Mirror wrap, where the reference has heavy violet-neutral basalt blocks. | `FoundryMasonry.png`, Mirror wrap | Texture swap (+ optional divisor change) |
| 6 | **Shaft interior lining** | ~8 % of point-1, prominent in shaft-detail and the overview. The boiler facade is repeated 8× in the pit, squashed and tinted brown, where the reference has a deep ribbed pit glowing at the bottom. | `FurnaceFacade` on 8 panels | New texture, swap the material only |
| 7 | **Boiler / reservoir cylinder wrap** | ~5 % at points 3 and 4. The pressure tanks read as wooden barrels, because each of the 12 facets per course maps the full riveted iron panel. The reference boilers are banded bronze-iron cylinders with small lit windows. | `FoundryIron` per facet | New texture + UV code |
| 8 | **Chain strip + lantern sprite** | Under 3 % of any gameplay capture, but visible in bridge-detail and undercroft-detail: chains read as thin grey diamond wire, where the reference has heavy black links. | 4 tubes per link, each mapping the full iron panel | Sprites (not counted as MeshRenderers) |

Not ranked: **gate facade v2.** The gate is already the best-looking part (point-4). Its only real defect is import
resolution, so see the note at the end of section 2.

---

## 2. Surfaces

Shared style sentence, already included in every prompt below:
*hand-painted dark fantasy mobile game texture, painterly but clean and readable on a phone screen, broad simple shapes,
2–3 value steps per material, soft brush edges, no photographic noise.*

### S1 — Court flagstones (rank 1)

**Where and how it is mapped now**
- Slot `PlatformMaterialSet._foundryPaving` → `MAT_FoundryPaving.mat` (`_Color` 0.8 grey) → `Art/Props/Foundry/FoundryPavingV2.png`.
- File: 1254×1254 RGB. Import: Wrap **Mirror** (wrapU/V = 2), mips on, trilinear, aniso 1, not readable, NPOT none. The Android override is max 1024 at **ASTC 6×6** (format 50), so it ships as 1024².
- Batch "Layout surface", order -15. It covers every court flag (`FoundryRegionFinish.cs` `Paving`), the stone ring (`BuildCourtStoneRing`) and, today, the bridge deck.
- UVs come from `Builder.Quad(floor:true)`: `u = 1 + (x + 2y)/8.55`, `v = 1 + (−x + 2y)/8.55`. In map units that is `u = 1 + U/6.046`, `v = 1 + V/6.046`: **one repeat per 6.05 × 6.05 map cells**, continuous across cells and aligned to the map grid.
  Image right (+U) points screen up-right; image top (+V) points screen up-left.
- On screen one tile is a diamond of 1026 × 513 px, about one screen width. That is ~1 texel per pixel horizontally and ~2 vertically (2:1 anisotropic minification).
- Flag joints (0.028), bevels (0.06, ×1.10 / ×0.74) and per-flag tone (0.78–1.15) are geometry plus vertex colour. Flags are 2×1 running bond, 1×1 at the west and row ends, and 2×2 inside the seal.

**Deliverable (S1a, recommended)**
- 1024×1024 PNG, RGB, no alpha, **seamless on all four edges**, Repeat.
- **Strictly top-down orthographic.**
- Irregular dressed flagstones like the reference court: 6–8 stones across the tile (0.6–1.2 map cells each; the hero is ~1.2 cells tall).
- Mortar 6–10 px and low contrast. Average stone about #56505C. No stone that would stand out on each repeat.

**Prompt S1a**
```
Seamless tileable hand-painted dark fantasy mobile game texture, 1024x1024, square. Viewed from directly above: strictly top-down orthographic, completely flat, like a scanned floor. Old foundry courtyard paved with irregular dressed basalt flagstones: mostly squarish and short rectangular slabs of mixed sizes, about 6 to 8 stones across the width of the image, laid in loose uneven rows, a few small infill stones. Narrow low-contrast mortar joints 6 to 10 pixels wide in dark violet-grey (#3E3B48). Stone faces in dark violet-grey slate, average #524E5A, some stones slightly lighter (#5E5864) and some slightly darker (#47434F), with soft worn chips along the stone edges in a lighter grey-mauve (#686065) and a few faint shallow scratches. Painterly but clean and readable on a phone screen: broad simple shapes, 2 to 3 value steps, soft brush edges, no photographic noise, no fine speckle. Even soft ambient light from straight overhead. The texture must tile seamlessly: stones cut by the left edge continue on the right edge, stones cut by the top edge continue at the bottom edge.
Negative: no perspective, no isometric angle, no horizon, no camera tilt, no cast shadows, no light coming from one side, no vignette, no brightness gradient across the image, no glow, no text, no letters, no runes, no logos, no symbols, no brass or metal lines, no characters, no creatures, no plants, no moss, no puddles, no rubble, no deep cracks, no single distinctive stone or stain that would visibly repeat.
```

**Prompt S1b (fallback, no code change: a jointless face for the existing geometry flags)**
```
Seamless tileable hand-painted dark fantasy mobile game texture, 1024x1024, square. Strictly top-down orthographic view of the flat worked surface of dressed dark basalt, with NO joints and NO stone edges anywhere: one continuous stone face. Broad soft mottling at a large scale (patches 150 to 300 pixels across) between #4E4A56 and #5A5462, faint diagonal chisel tooling, a few soft pale wear marks (#686065). Painterly, clean, readable at phone scale: no photographic noise, no fine speckle, no pores. Even soft ambient overhead light. Must tile seamlessly left-right and top-bottom.
Negative: no joints, no tile grid, no cracks, no perspective, no isometric angle, no cast shadows, no one-sided light, no vignette, no text, no symbols, no logos, no characters, no moss, no puddles.
```

**Integration**
- Save as `Art/Props/Foundry/FoundryPavingV3.png` and assign it to `MAT_FoundryPaving` `_MainTex`.
- Import: Wrap **Repeat** (the Mirror wrap mirrors every other tile), mips on, trilinear, **aniso 2** (fixes the 2:1 squash blur), Android max 1024, **ASTC 4×4**. The floor is 80 % of the screen, so smooth gradients deserve the finer blocks; compare against 6×6 on the device.
- Set `_Color` to white and paint to the target value, rather than painting 25 % lighter to survive the 0.8.
- Code (S1a only), `FoundryRegionFinish.cs` `Paving`: emit one `LitFloorQuad` per flag with `stone = 1` and a white flag tint. Drop the joint, bevel and corner-repair quads. Keep `FloorLight` and the zone tints.
  Rectangular per-flag tone would draw a 2×1 grid over the painted irregular joints. This also removes a large share of the 60,768 static vertices.
- Renderer delta: 0.

**Check in game**
- start, point-2 and the device: joints must never read as obstacles or cracks. A recognisable stone repeating about once per screen width means repaint it quieter.
- Hero, enemies and the blue ability ring must stay clearly above the floor in value (the floor stays ≥ 2 value steps darker than the hero's steel).
- point-3: the furnace warm-up still reads.
- Compare against the reference court crop: stone #524E5A, joint #3E3B48.

### S2 — Court seal medallion (rank 2)

**Where and how it is mapped now**
- `FoundryRegionHeraldry.cs` `BuildCourtSeal`: rings r 3.36–3.94, 8 radial blocks, star wedges and the forge mark. They are Coping-material quads (`ENV_Platform_Coping.png`, 2172×724 → Android 1024×256 ASTC 4×4, Repeat) at world floor UVs with vertex alpha 0.32–0.4.
- `BuildCourtStoneRing` (Finish) adds a flag annulus at r 3.26–3.97 in the paving batch.
- `BuildCourtCompass` adds brass rays from r 4.1 to a ring at r 8.6. Those stay.
- Batch: `Foundry floor decals`, order -14.5, plain material (no cutaway).

**Deliverable**
- 1024×1024 PNG, **RGBA, transparent background**, Clamp, strictly top-down, perfectly circular (the mesh makes it an ellipse).
- It maps onto one quad covering map (−4..4)², so **128 px per map unit**:
  - outer brass ring at radius ~486–504 px;
  - segmented stone ring ~417–486 px;
  - inner brass ring ~430 px;
  - star tips ~370 px;
  - forge mark within ~186 px.
- **The 4 long star points aim at the image corners**; after projection they point screen up, down, left and right. The 4 short points aim at the edge midpoints.
- Keep the centre 150 px calm, because the hero's ring and telegraphs sit there.
- Open stone between the lines is transparent, so S1 shows through.

**Prompt S2**
```
Hand-painted dark fantasy mobile game decal, 1024x1024, PNG with a transparent background. Viewed from directly above: strictly top-down orthographic, perfectly flat, a perfect circle (not an ellipse), exactly centred. An ancient foundry floor medallion made of flush inlay set into stone; its outer edge almost touches the image edges (outer diameter about 1000 pixels). From outside inward: (1) an outer band of worn aged brass inlay about 18 pixels wide (lit #B78052, worn #8C7566) set in a narrow dark engraved channel (#2E2A33); (2) a ring of dressed dark basalt segments (#4A4552) about 60 pixels wide, divided evenly by eight short radial stone blocks; (3) a thin inner brass ring; (4) a large four-pointed star drawn with brass inlay lines, whose four LONG points aim exactly at the four CORNERS of the image, with four SHORTER points aiming at the middle of each image edge; (5) at the very centre a small quiet forge mark: a split diamond outline around a small rectangular ingot, no wider than 360 pixels. The metal is flat, flush, dull, scratched and worn, never shiny. The open stone areas between the lines are fully transparent; paint only the brass lines, their dark channels and the segmented stone ring. Four-fold symmetric. Even soft overhead ambient light. Painterly, clean, readable at phone scale.
Negative: no perspective, no tilt, no ellipse, no 3D bevel lighting from one side, no cast shadows, no glow, no sparkle, no lens flare, no gems, no runes, no letters, no numbers, no zodiac signs, no skulls, no eyes, no logos, no characters, no background stone texture outside the ring.
```
Fix-up tip: crop the best quadrant (512×512 around the centre) and mirror it horizontally and vertically to rebuild a
perfectly symmetric medallion.

**Integration**
- New slot `_foundryDecals` + `MAT_FoundryDecals` (Sprites/Default). Pack **S2 (left) and S4 (right) into one `FoundryDecals.png` 2048×1024**, then move every current Coping-at-FloorDecal user onto it: seal, compass, brass inlays, borders, layout coping, bridge joints and gate threshold.
  This keeps exactly one floor-decal renderer (the test demands it) and a renderer delta of 0. The seal is one quad `Project(−4,−4)…Project(−4,4)` with UV `((U+4)/8 × 0.5, (V+4)/8)`.
- Remove the geometry in `BuildCourtSeal` and `BuildCourtStoneRing`.
- Import: Clamp, Alpha Is Transparency, mips, trilinear, aniso 2, Android **max 2048**, ASTC 4×4. At 1024 the seal would be ~0.54 texel per screen pixel.

**Check in game**
- start and device: gold star and rings read at arm's length, the star points up, down, left and right, and the rays from `BuildCourtCompass` meet the outer ring.
- No halo from alpha fringes (check on dark flags).
- Ground telegraphs and the hero ring stay readable over the centre.

### S3 — Bridge deck (rank 3)

**Where and how it is mapped now**
- `FoundryRegionView.Build`: the bridge `Project(−27,−23)…(−14,−10)` is 26 steps × 4 lanes of `LitFloorQuad` in the paving batch. It uses the court world UVs, which run diagonal to the bridge.
- Two brass inlays run the length at `u−v = ±2`, i.e. ¼ and ¾ of the width. They are `BuildLandmarks` strips running into the court.
- 12 transverse `Bridge joints` strips (Coping, colour 0.40/0.42/0.47) sit every 1.414 map units along.
- On screen the deck is 5.66 world units wide (**680 px**) and 9.19 tall (1103 px).

**Deliverable**
- 1024×1024 PNG, RGB. **Seamless top↔bottom only**; the left and right edges are the deck edges.
- Strictly top-down. One tile = the full deck width × 5.66 map units along, shown as 680 × 340 px, repeating 3.25× along the bridge.
- Four transverse courses: joints at y = 0/256/512/768 px, which match the existing joint spacing exactly.
- Lanes: outer quarters (x 0–256 and 768–1024) of small squarish setts; centre half of large rectangular flags; lane joints at x = 256 and 768.

**Prompt S3**
```
Hand-painted dark fantasy mobile game texture, 1024x1024, square. Strictly top-down orthographic view, completely flat, of the stone deck of a long fortified foundry bridge. The image shows the FULL width of the deck from its left edge (x=0) to its right edge (x=1024); the deck continues endlessly up and down, so the top edge must tile seamlessly with the bottom edge. Across the width: a left border lane (x 0 to 256) and a right border lane (x 768 to 1024) paved with smaller squarish setts; a central lane (x 256 to 768) paved with large rectangular dressed flagstones. The lanes are separated by straight narrow dark stone joints exactly at x=256 and x=768. The paving is laid in four transverse courses: straight joints running fully across the deck at y=0, y=256, y=512 and y=768. Both outer edges finish with a narrow darker kerb line about 16 pixels wide. Dark violet-grey basalt stones (#524E5A), low-contrast mortar (#3E3B48), soft chipped edges (#686065), subtle foot-traffic wear along the central lane. Even soft overhead ambient light. Painterly, clean, readable at phone scale, broad shapes, no photographic noise.
Negative: no perspective, no vanishing point, no isometric angle, no railings, no walls, no parapet seen from the side, no cast shadows, no one-sided light, no vignette, no medallion, no emblem, no brass lines, no text, no letters, no symbols, no characters, no statues, no puddles, no moss.
```

**Integration**
- New slot `_foundryBridgeDeck` + material, Repeat on V and Clamp on U (set wrapU and wrapV separately), mips, trilinear, aniso 2, Android max 1024, ASTC 4×4.
- Code in the bridge loop: emit the bridge into its own Builder with `uv = (lane/4 … (lane+1)/4, step/8 … (step+1)/8)`. That is 8 steps per tile, so no quad crosses a tile edge and the texture could also live in an atlas.
- Remove the 12 `Bridge joints` strips (they are painted now). Keep the two brass lane inlays, which sit on the painted lane joints.
- Renderer delta: +1 (a new material at -15).

**Check in game**
- point-0 and bridge-detail: courses and lanes line up with the brass strips.
- No visible seam every 340 px vertically.
- The deck is not brighter than the court (it currently shows a pale band in point-0).

### S4 — Rim trim sheet: parapet face, kerb course, brass inlay, iron strip (rank 4)

**Where and how it is mapped now**
- `BuildCourtBorders` (Site): 0.23-wide × 0.8-long strips per edge cell, 0.32 from the edge.
- `Layout coping` trim: 0.18 inward quads.
- Brass `Strip`s, the threshold and joints.
- All of these use `Quad(floor:false)` local UVs `u = screenSpan·0.32`, `v = screenWidth/0.8`. A 0.8 strip therefore samples only the u 0–0.2, v 0–0.23 corner of `ENV_Platform_Coping.png`: brass bar, black frame, panel edge. That is the "slot" row.
- Parapets are `BuildPerimeter` `Block`s, rise 0.28 and 0.3 thick:
  - raised sides use FoundryWall masonry (`WallQuad`: `u = x/2.8`, `v = (y − slope·x)/2.4`), so a 0.28 face shows 12 % of one tile;
  - caps use `MAT_Platform_Floor` (`ENV_Platform_Floor.png`, 8×8 black grid) at floor UVs.

**Deliverable**
- 1024×1024 PNG, RGB, a **trim sheet of 4 horizontal bands, 256 px each**, each **seamless left↔right**, Clamp vertically.
- Keep the top and bottom 8 px of each band plain dark (#3E3B48) so mips do not bleed colour between bands.

| Image rows (top→bottom) | Unity V | Band | Projection | Maps to |
|---|---|---|---|---|
| 0–255 | 0.75–1.00 | A: parapet face | straight elevation | parapet raised sides; one tile = 4 cells along (~340 px on screen) × the rise |
| 256–511 | 0.50–0.75 | B: kerb course | top-down | edge cells; one tile = 2 cells along × 0.5 cell across; the band's top edge faces the void |
| 512–767 | 0.25–0.50 | C: brass inlay | top-down | every brass `Strip`; one tile = 2 cells along × the strip width |
| 768–1023 | 0.00–0.25 | D: iron strip | top-down | gate threshold and wear strips |

**Prompt S4**
```
Hand-painted dark fantasy mobile game trim sheet, 1024x1024, square, made of FOUR horizontal bands stacked from top to bottom, each band exactly 256 pixels tall and each band tiling seamlessly from its left edge to its right edge. Every band is a flat orthographic texture with no perspective. The top 8 and bottom 8 pixels of every band are plain dark violet-grey (#3E3B48).
Band 1 (top, rows 0-255): straight-on front elevation of a low heavy basalt parapet wall: a dark coping course across the top 60 pixels with a slightly lighter chamfered upper edge, a thin dull aged-brass fillet line just under it (#8C7566), below that recessed dark panels (#2E2A33) between square dark stone pilasters (#3F3941); half a pilaster at the left edge, one full pilaster in the middle, half a pilaster at the right edge; a plain plinth course at the bottom.
Band 2 (rows 256-511): seen from directly above, a single row of long dressed basalt kerb stones (#4A4552) with narrow joints (#3E3B48), 2 to 3 stones across the band width; the top edge of the band is a slightly rounded worn outer edge in a lighter tone (#686065), the bottom edge is a straight joint.
Band 3 (rows 512-767): seen from directly above, a flush inlaid strip of worn aged brass running the full width, occupying the middle half of the band (lit #B78052, worn #8C7566, faint scratches), bordered above and below by narrow dark engraved channels, then plain dark stone.
Band 4 (bottom, rows 768-1023): seen from directly above, a flush flat black iron wear plate running the full width (#2A2530 with soft warm bronze wear #4A3A30), with small countersunk bolts every 128 pixels.
Even soft ambient light, painterly but clean, readable on a phone screen.
Negative: no perspective, no isometric angle, no vanishing point, no cast shadows, no one-sided light, no glow, no text, no letters, no runes, no logos, no symbols, no characters, no plants, no gaps between bands, no frame around the image.
```

**Integration**
- Packed into `FoundryDecals.png` (right half) with S2. Borders, layout trim, inlays, threshold and joints all move to it: floor-decal renderer count stays 1.
- UV code: `u = (cell index mod tile + s)/tile × 0.5 + 0.5` and `v = band range`. Split long `Strip`s into pieces no longer than one tile.
- Replace the per-cell 0.8 border strips with a continuous 0.5-cell kerb band on each open edge (band B).
- Parapet: give `BuildPerimeter` its own block variant whose raised faces map band A (v from 0 at the floor to 1 at the top), and **raise the rise from 0.28 to ~0.6**. At 0.28 any painted face is 34 px tall. This costs +1 renderer (a decals material at -14 with the cutaway).
  Optionally map the parapet caps to band B instead of the grid texture (the same -13 rank as other caps, and another material, so hold that until the budget allows).
- Import: see S2.

**Check in game**
- point-1, point-3, point-4 and shaft-detail: the slot row is gone, and the rim reads as a kerb plus a parapet with a brass fillet.
- Brass reads as gold (#B78052-ish when lit), not orange-brown.
- The hero cutaway still dissolves a taller near parapet (south and west edges) and never the decals.

### S5 — Foundation and sidewall masonry v2 (rank 5)

**Where and how it is mapped now**
- Slot `_foundryWall` → `MAT_FoundryMasonry` → `FoundryMasonry.png` 1254² RGB, **Mirror** wrap, Android max 1024 ASTC 6×6.
- Used for:
  - the layout sidewalls (2.8 drop under every rim, the shaft inner walls, the bridge sides);
  - every `Block` foundation and raised side (supports, pipe saddles, crane footing, furnace foundations, the box behind the boiler facades);
  - bridge coping, gate tiers and pediment, boiler crowns, finials, lip stones, gateposts and the undercroft;
  - `ArchitecturalTier` tops at floor UVs.
- `WallQuad`: `u = x/2.8`, `v = (y − slope·x)/2.4` (world units), so **one tile = 336 × 288 px** on screen, courses following the wall's top edge. The tile is shown 1.17× wider than tall.
- Face tints multiply the texture: `Block` (0.63, 0.66, 0.77) and (0.43, 0.46, 0.57); `ArchitecturalTier` (0.76, 0.79, 0.87)…(0.48, 0.52, 0.64). All of them push blue.

**Deliverable**
- 1024×1024 PNG, RGB, seamless all edges, Repeat, straight-on elevation.
- 5 courses per tile (~205 px each, ~58 px on screen).
- Blocks 1.5–3× wider than tall, painted slightly narrow to allow for the 1.17 stretch.

**Prompt S5**
```
Seamless tileable hand-painted dark fantasy mobile game texture, 1024x1024, square. Straight-on front elevation (orthographic, flat, no perspective) of a massive fortress foundation wall of dressed dark basalt ashlar: exactly 5 horizontal courses of heavy rectangular blocks, blocks 1.5 to 3 times wider than tall, courses of slightly varying height, staggered vertical joints. Block faces dark violet-grey (#3F3941) with gentle per-block value variation (#37323B to #48424C), soft chipped arrises in a lighter tone (#5A5360), a few faint vertical soot streaks. Deep narrow joints (#1E1A24). Even soft frontal ambient light, no direction. Painterly but clean and readable on a phone screen: broad shapes, no photographic noise. Must tile seamlessly: left edge continues on the right edge, top edge continues at the bottom edge, with no course visibly thicker at the seam.
Negative: no perspective, no top surface visible, no side faces, no cast shadows, no light from one side, no vignette, no windows, no arches, no doors, no metal, no text, no symbols, no logos, no moss, no plants, no bright highlights, no blue cast.
```

**Integration**
- Save as `FoundryMasonryV2.png` and assign it to `MAT_FoundryMasonry`. Import: Wrap **Repeat**, mips, trilinear, Android max 1024, ASTC 6×6.
- Optional code, one line in `Builder.WallQuad`: raise the 2.8/2.4 divisors (for example 4.2/3.6) for heavier stones. Check the gate and boiler tiers afterwards.
- Palette fix: move the `Block` and `ArchitecturalTier` face tints from blue toward neutral-violet (for example 0.66/0.64/0.70 and 0.46/0.44/0.50).
- Renderer delta: 0.

**Check in game**
- point-1 bottom, point-0 bridge end, undercroft-detail and overview: no mirror seams every 336 px, courses read as heavy blocks, and the stone is violet-neutral rather than blue.

### S6 — Shaft interior lining (rank 6)

**Where and how it is mapped now**
- `FoundryRegionDressing.cs` `BuildShaft`: the two far walls, `(−11.9,9.9)→(−4.1,9.9)` and `(−4.1,9.9)→(−4.1,2.1)`, each have 4 `FullFace` panels (UV 0..1), 2.25 deep.
- Material `FurnaceFacade` (1024×1536 → Android 683×1024, ASTC 6×6, Clamp), vertex tint (0.78, 0.62, 0.48), order -15.5, clipped by the floor.
- Each panel is **166 × 270 px** on screen. The ember basin (`FoundryEmbers`, full face) sits 2.3 below.
- Image left→right runs screen left→right; the image top is at floor level.

**Deliverable**
- 1024×1536 PNG (native portrait), RGB, Clamp, straight-on elevation of **one bay**.
- The left and right edges end in half pilasters, so adjacent panels join.
- The top 10 % is a dark lip course; the bottom third holds a glowing arched opening; the bottom edge is dark soot and ember.

**Prompt S6**
```
Hand-painted dark fantasy mobile game texture, 1024x1536, portrait. Straight-on front elevation (orthographic, flat, no perspective) of one bay of the inner wall of a deep industrial smelting pit. Left and right edges each end exactly in half of a vertical black-iron pilaster banded with dull aged brass straps (#8C7566), so the bay repeats side by side. Top 10 percent: a dark heavy stone lip course. Middle: dark basalt wall (#3F3941) crossed by riveted black-iron ribs (#2A2530) and one horizontal pipe. Bottom third: a tall round-arched opening filled with deep furnace glow (#99432F at the edges, #E56A3E inside, a small hot core #FBC06C near the bottom), behind a few vertical iron bars. Bottom edge: soot-black with a faint ember rim, fading into darkness. Heat light only inside the arch; the upper wall stays dark and cool. Even frontal ambient light otherwise. Painterly, clean, readable on a phone screen, broad shapes.
Negative: no perspective, no top or side surfaces visible, no floor, no cast shadows, no one-sided light, no lava river, no fire above the bottom third, no portholes, no text, no letters, no symbols, no logos, no characters, no creatures.
```

**Integration**
- New slot `_foundryShaftLining` + material, used only by the "Shaft heated lining" batch. It replaces the `FurnaceFacade`@-15.5 batch, so the renderer delta is 0.
- Set the lining tint near white (for example 0.95, 0.9, 0.85).
- Optionally mirror U on every other panel so the 8 panels read as 4 pairs.
- Import: Clamp, mips, Android **max 512** (the panel is 270 px tall), ASTC 6×6.

**Check in game**
- point-1 and shaft-detail: the pit reads deep and warm at the bottom, cool at the top, with no repeated boiler portholes.
- The seam between panels is invisible.

### S7 — Boiler / reservoir cylinder wrap (rank 7)

**Where and how it is mapped now**
- `FoundryRegionDressing.cs` `BuildPressureReservoir` → `ReservoirCourse`: 12 facets per course, 9 stacked courses from height 0.25 to 2.91. Radii are 0.68–0.85, so a tank is ~197 × 320 px on screen.
- Only the front half is drawn: facets 4–9, spanning angles 120°–300°, left→right on screen.
- Each facet is a `FullFace` with `FoundryIron` (1254² → 1024 ASTC 6×6, Clamp, the riveted panel), shaded 0.68 + 0.19·cos(angle + 1.1), with a brass-tinted bar on each course top.

**Deliverable**
- 1024×1024 PNG, RGB, Clamp, the **unrolled front half of a cylinder** from base (bottom) to shoulder (top), painted flat with no cylindrical shading.
- Course lines to respect (image fraction from the bottom): 0.07, 0.17, 0.25, 0.30, 0.71, 0.76, 0.83, 0.95.
- Put brass belts in 0.25–0.30 and 0.71–0.76.

**Prompt S7**
```
Hand-painted dark fantasy mobile game texture, 1024x1024, square. The surface of the front half of a riveted industrial boiler cylinder UNROLLED FLAT like a map projection: a flat rectangle filled edge to edge, with no curvature shading, no highlights and no shadow at the sides. Dark iron with a faint warm bronze cast (#352728 to #3C2822), made of curved riveted plates with vertical seams. Horizontal features from bottom to top: a heavy base ring in the bottom 7 percent; a lower plate course; a dull aged-brass retaining belt (#8C7566, lit #B78052) between 25 and 30 percent of the height with a row of rivets; a tall main plate course from 30 to 71 percent containing three small round-topped gauge windows with a soft orange glow (#E56A3E, core #FBC06C) centred at mid height; a second brass belt between 71 and 76 percent; upper shoulder plates to the top. Even soft frontal ambient light. Painterly, clean, readable on a phone screen.
Negative: no perspective, no cylinder shading, no specular highlights, no cast shadows, no pipes sticking out, no valves, no text, no letters, no numbers, no gauges with dials, no logos, no characters, no wood, no planks.
```

**Integration**
- New slot `_foundryBoilerWrap` + material.
- `ReservoirCourse` sets `u = (i − 4)/6 … (i − 3)/6` for facet i and `v = (height − 0.25)/2.66`; the top caps stay iron.
- Renderer delta: +1 (a new material at -11).
- Import: Clamp, mips, Android **max 512**, ASTC 4×4.
- The same wrap is ready for round boiler towers (see the limits).

**Check in game**
- point-3, point-4 and furnace-detail: the tanks read as banded metal cylinders, not barrels, and the windows are tiny accents.

### S8 — Chain strip and lantern sprite (rank 8)

**Where and how it is mapped now**
- `Chain()` (Dressing) builds each link from 4 `Tube`s, each tube 3 `FullFace`s mapping the whole riveted `FoundryIron` panel onto 0.03–0.05-wide bars.
- Chains are 0.085–0.16 wide (10–19 px on screen), with a link every 2.3 × width.
- Lanterns (`BuildCourtLantern`, 4 posts) and 10 `Beacon`s are tube cages sampling an ember seam of `FoundryEmbers`.

**Deliverables**
- (a) Chain: 256×1024 RGBA, transparent, vertical, **seamless top↔bottom**, 4 links alternating face-on and edge-on.
- (b) Lantern: 512×1024 RGBA, transparent, one caged post lantern in the scene's 2:1 dimetric view (vertical edges stay vertical, the top is squashed to half height, no vanishing points).
- Generate at 1024×1536 and downscale.

**Prompt S8a**
```
Hand-painted dark fantasy mobile game sprite, 1024x1536 portrait, PNG with a transparent background. One heavy forged black-iron chain hanging perfectly vertically, centred, running from the very top edge to the very bottom edge, exactly 4 links visible, alternating a link seen face-on (open oval) and a link seen edge-on (narrow bar); the chain must tile seamlessly from bottom to top. Iron #1E1A24 with soft warm bronze wear on the edges (#4A3A30) and a subtle cool rim from ambient light. Straight-on orthographic view. Painterly, bold, readable at a small size.
Negative: no perspective, no swing, no curve, no background, no ground, no shadow, no rust orange, no glow, no text, no logos, no hook, no weight at the end.
```
**Prompt S8b**
```
Hand-painted dark fantasy mobile game sprite, 1024x1536 portrait, PNG with a transparent background. A single square caged iron lantern for the top of a stone post, seen from a high 2:1 dimetric game angle (vertical edges perfectly vertical, top face squashed to half height, no vanishing point): black-iron frame and pyramid cap with dull aged-brass trim (#8C7566), two visible glass panes glowing warm furnace orange (#E56A3E, small core #FBC06C). The glow stays inside the panes, with only a very small soft halo. The lantern fills most of the image height and is centred on the bottom edge.
Negative: no post, no stone, no background, no ground shadow, no light rays, no big bloom, no perspective distortion, no text, no logos, no characters.
```

**Integration**
- Chain: import as Sprite (Single, **Mesh Type Full Rect**, Wrap Repeat, Android max 256, ASTC 4×4). `Chain()` spawns one SpriteRenderer with `drawMode = Tiled`, size (2.2 × width, length) and PPU so that one tile covers 9.2 × width (4 links), rotated to the run direction, `sortingOrder = SceneryOrder(order)`.
- Lantern: Sprite, bottom-centre pivot, PPU = pixel height / 1.1, placed like `Furnace()`. It replaces the lantern cage and pane quads.
- Renderer delta: 0 (sprites are not MeshRenderers); it also sheds ironwork vertices.

**Check in game**
- bridge-detail, undercroft-detail and crane-detail: chains read as heavy black links, not wire.
- Lanterns at point-2/start corners don't out-glow the furnaces.

### Note — gate facade v2 (not ranked)

`GateFacade.png` is 1536×1024, Clamp, but the Android override is max 1024, so it ships at 1024×683. It sits on a face
~1018 × 780 px on screen (point-4), which is slightly soft, and the 1.5 art is shown at 1.31 (≈13 % horizontal squeeze).
First fix without new art: set Android max 2048 (+0.5 MB). Only if the owner wants a v2: same prompt family as the
original, native 1536×1024, the door seal lit brass #B78052 like the reference, and the whole composition drawn ~13 %
wider than final.

---

## 3. Palette (sampled from `foundry-v1.png`)

| Role | Hex | Sampled from |
|---|---|---|
| Court stone, mid | **#524E5A** | court 40–60 % luminance band |
| Court stone, worn edge / light chip | **#686065** | court 90–98 % |
| Mortar / joint | **#3E3B48** | court darkest 8 % |
| Wall / parapet stone | **#3F3941** | parapet side faces |
| Deep shadow, dark iron, foundation | **#1E1A24** | low-luminance neutral cluster |
| Worked iron, warm (boilers, crane) | **#352728 – #3C2822** | crane mast, boiler body |
| Brass, lit | **#B78052** | warm saturated highlights |
| Brass, worn / in shade (inlay) | **#8C7566** | court inlay lines |
| Furnace glow, hot | **#E56A3E** | bright red-orange cluster |
| Ember, deep | **#99432F** | darker red cluster |
| Flame core | **#FBC06C** | brightest warm pixels |
| Fog, violet haze | **#54455D** | mid-luminance violet cluster |
| Fog, deep indigo | **#302F4B** | upper sky |

**Where our current materials drift**
- **Court floor renders #414245 in game** (start and device). It is neutral grey and ~20 % darker than #524E5A, because `FoundryPavingV2` averages a neutral #5B5A5B, × `_Color` 0.8, × the cool vertex light (0.98, 1.01, 1.075). Fix it with S1 (a violet cast in the texture, `_Color` white).
- **FoundryMasonry #333641 is blue** (B−R = +14 against +2 for the reference stone). The blue `Block` / `ArchitecturalTier` face tints add more blue. Fix it with S5 and neutral tints.
- **FoundryIron #333439 is a neutral blue-grey.** The reference iron is darker (#1E1A24) and warm-bronze on the machines (#352728–#3C2822).
- **Brass inlays render #855227 in game**: darker and more orange than lit #B78052, because the vertex colour (0.70, 0.56, 0.34) multiplies the coping brass #9B7C4E. The gate (#8F724F) and furnace (#916B44) brass match the reference's shaded brass and are fine.
- **Glow is more saturated**: `FoundryEmbers` #E84203 and the furnace windows #E94E0B, against the reference #E56A3E. Keep the glow small and move new art toward #E56A3E.
- The cavern haze (#433446) is close to the reference's #54455D. It is fine.

---

## 4. Limits

**What textures cannot fix**
- **Landmark mass and silhouettes.** The reference reads through its outlines:
  - round banded boiler towers with domes;
  - a round shaft with an iron balustrade and two domed boilers inside;
  - heavy crenellated parapets;
  - chains as thick as the hero;
  - statues and banners at the bridge head.

  Ours are 3.2-tall square boiler boxes, a square shaft, a 0.28-tall parapet (34 px) and 0.1–0.16 chains.
  Paint can dress these shapes but cannot change their outline. The next geometry steps are cylindrical boiler bodies
  (reusing `ReservoirCourse` + S7), a parapet rise of ~0.6 (S4), a round shaft collar, and bridge-head sprites.
- **Depth ordering.** The hero cutaway dissolves structures on every side, including behind him, and distant enemies
  still draw above structures (foundry-floor-01 README). Batches sort by material rank, not per object. No texture
  changes that.
- **Light.** The shaders are unlit and there are no normal maps. Every highlight is either painted (so it must agree with
  the code's light from screen right) or a vertex colour. Painted directional light would fight the code, so keep it
  neutral.
- **Repetition.** The court uses one world-continuous tile (~1 screen width). Variety comes only from vertex patina and
  zone tints; a second floor variant needs a per-zone material (+1 renderer).
- **ChatGPT precision.** Grids, radii, symmetry and seamless edges are approximate, so budget a fix-up pass per image.
  The seal quadrant-mirror trick and the offset-and-heal step cover most of it.
- **Renderer cap.** S3, the S4 parapet and S7 each add one MeshRenderer: 17 → 20 if all three land (S1, S2, the rest
  of S4, S5, S6 and S8 add none). The cap in `FoundryRegionTests` must be raised deliberately in the same change.

**Texture memory (Android, as imported; ASTC 4×4 = 1 B/px, ASTC 6×6 ≈ 0.44 B/px, mips +33 %)**

Current set loaded with `PlatformMaterials`:

| Texture | Imported size | Format | ≈ MB |
|---|---|---|---|
| PavingV2, Masonry, Iron, Embers, ENV_Platform_Floor, ENV_Platform_Wall | 1024² each | 6×6 + mips | 6 × 0.62 = 3.74 |
| FurnaceFacade, GateFacade | 683×1024 / 1024×683 | 6×6 + mips | 0.83 |
| ENV_Platform_Coping | 1024×256 | 4×4 + mips | 0.35 |
| ENV_Furnace_Cavern_v1 (backdrop) | 1024×1536 | 6×6, no mips | 0.70 |
| Furnace.tga sprite | 384×328 | 4×4, no mips | 0.13 |
| **Total today** | | | **≈ 5.7** |

Additions from this brief:

| Texture | Size | Format | ≈ MB added |
|---|---|---|---|
| S1 PavingV3 | 1024² | 4×4 + mips | +0.78 (replaces V2) |
| S2 + S4 FoundryDecals | 2048×1024 | 4×4 + mips | +2.80 |
| S3 BridgeDeck | 1024² | 4×4 + mips | +1.40 |
| S5 MasonryV2 | 1024² | 6×6 + mips | 0 (replaces) |
| S6 ShaftLining | 341×512 | 6×6 + mips | +0.10 |
| S7 BoilerWrap | 512² | 4×4 + mips | +0.35 |
| S8 chain + lantern | 256×1024 + 256×512 | 4×4 + mips | +0.52 |
| **Total after** | | | **≈ 11.7** |

A mid-range Android phone (4–6 GB RAM, Mali-G57 / Adreno 610 class) has headroom for far more than this. Hold the
foundry environment to a self-imposed **16 MB** so characters, VFX, UI and later regions fit beside it. The plan lands
at ~12 MB even with the seal at 2048 wide. The APK grows by roughly the same ~6 MB (ASTC barely compresses further);
today's APK is 39.3 MB. Run-time cost stays trivial: the floor is sampled once per pixel, and the only new blended
overdraw is the seal (~20 % of the screen at start).

**Verification after each landing.** Re-run the art-review capture (`Tools/ArtReview/FoundryRuntimeCapture.cs`, five
points: bridge (−23,−23), west of the shaft (−15,6), court centre (0,0), boiler apron (16,6), gate apron (28,6)). Its
output path is hard-coded, so point it at a new folder. Compare before/after against the reference crops, then do one
device launch at the court start.
