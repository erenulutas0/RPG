# Painted surfaces 02 — ChatGPT texture pack (2026-09-30, evening)

Supersedes `../painted-surfaces-01/PROMPTS.md`. Target: `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`.
Evidence: the latest Unity captures `../foundry-chest-light-01/after/` (17 renderers / 58,612 vertices), the S23 captures
`../foundry-device-01/`, today's proof READMEs, the code as of this evening (line numbers below), the texture `.meta`
files and the Android build report `TestResults/android-build.log` (16:26). Nothing in this folder changes code or assets.
`FoundryRegionFinish.cs` was being retuned while this pack was written (forge light, 18:14), so method names are the
stable reference: re-check line numbers before integrating.

---

## 0. Facts every item depends on (checked in code today)

- **Camera.** Orthographic, `_visibleWidth: 9` → **120 px per world unit** on the 1080-wide S23. The band between the HUD
  panels is about 1080 × 1600 px.
- **Design → screen.** `Project(u,v)`: x = (u−v)·0.707, y = (u+v)·0.354, plus height. One design cell is a 170 × 85 px
  diamond (edge 95 px). The hero is about 165 px tall.
- **Floor UVs** (`Builder.Quad(floor:true)`, `FoundryRegionView.cs:391`): tex u = 1 + u/6.046, tex v = 1 + v/6.046.
  - One tile covers 6.05 × 6.05 cells: a 1026 × 513 px diamond, so about 6–7 repeats are visible at once.
  - Image right points screen up-right; image top points screen up-left.
  - Paint floors **strictly top-down**. The mesh does the 2:1 squash.
  - With a 1024 texture, one screen pixel spans ~1.4 texels horizontally and ~2.8 vertically. Aniso 2 helps, and
    detail smaller than ~6 texels is lost.
  - The 8.55 divisor is shared by every floor-mapped quad on every material (caps, bastion tops). To rescale the
    paving alone, do it inside `LitFloorQuad` (Finish.cs:288), which only the paving batch uses.
- **Wall UVs** (`Builder.WallQuad`, View.cs:381): u = x/2.8, v = (y − slope·x)/2.4 in world units.
  - One tile is 336 × 288 px on screen.
  - The mesh shears an elevation along its edge's 26.6° slope, and vertical lines stay vertical.
  - Paint elevations **straight-on**.
- **Light is code, not texture.**
  - All materials are `Sprites/Default` and unlit. Every renderer at order ≥ −14, except the floor decals, gets the
    `Cryptforge/Foundry Cutaway` variant: its colour pass uses **ZTest Always**, so painter order decides what draws on
    top, and a second pass writes depth only.
  - Heat, patina, soot and face shade are vertex colours (`FloorLight`, face tints).
  - So paint **evenly lit albedo**: no directional light, and glow only where the object itself burns.
- **Power of two.**
  - In this project's Android build, mip-mapped non-power-of-two (NPOT) textures with nPOTScale None shipped **uncompressed** despite an ASTC override (GateFacade, FurnaceFacade); non-mipped NPOT textures (Coping, UI panel) did compress. Request every deliverable at power-of-two size anyway and confirm in the build report.
    The gate and furnace facades were 3.6 MB RGBA32 each until today.
  - Every deliverable below is POT. ChatGPT's 1536 side is always cropped or scaled by the session.
  - **Trap:** the six 1254² sources (Embers, Iron, Masonry, PavingV2, ENV_Platform_Floor, ENV_Platform_Wall) are POT in
    the build only because max size 1024 clamps them to 1024². Never raise their max size without setting nPOTScale.
- **Memory.** ASTC 6×6 ≈ 0.44 B/px, 4×4 = 1 B/px, mips add 33 %. From the build report:
  - each 1024² at 6×6: 612 KB;
  - GateFacade and FurnaceFacade: 1.2 MB each;
  - Coping (1024×256, 4×4): 342 KB;
  - cavern backdrop: 685 KB;
  - environment total ≈ 7.2 MB of 13.9 MB of build textures.
- **Shared materials.** `_floor`, `_coping` and `_wall` are also the ordinary halls' materials
  (`ForgePlatformView` → `PlatformMaterialSet.ForPart`). Foundry art gets new foundry slots; never re-texture those three.
- **Renderers.** 17 of 17 are used (§3).
  - One renderer per (material, order).
  - Batches that land on the same sorting order have **no painter guarantee**, so a new material needs its own order.
  - `FoundryRegionTests.cs:74` asserts exactly one `Foundry floor decals` renderer.
- **Import route.** Follow the `Tools/ArtReview/BuildFoundryPavingAsset.cs` + `Import-FoundryPaving.ps1` pattern: copy the
  master from ArtDirection, set the importer, bind the slot. Never run it while a Unity batch run is in flight.

---

## 1. Status of every surface at the portrait gameplay camera

"Share" is the share of the free band in the chest-light-01 captures: start and point-2 (court centre, where every room
starts), point-0 (bridge), point-1 (west of the pit), point-3 (boiler apron), point-4 (gate apron), plus the device captures.

| Surface | Now (code / batch) | Reference | Gap at the portrait camera | Recommendation | Renderers |
|---|---|---|---|---|---|
| **Court flagstones** | `FoundryPavingV2` speckle (1254²→1024² ASTC 6×6, Mirror, `_Color` 0.8) under procedural 2×1 flags with joints, bevels and per-flag tone (`Paving`, Finish.cs:223); `Layout surface` @−15 | Irregular 4–6-sided violet-slate flags about the hero's height, thin dark joints | **85–95 %** at start, point-2 and the device; 40–70 % elsewhere. Reads as a regular grid of large grey bricks; renders #393B41 against the reference's #524E5A | **Paint now — P1** | 0 |
| Bridge deck | Procedural diagonal lattice (`BuildBridgeDeck`, View.cs:89) in the same paving batch (fixed today) | Court-like flags with an emblem | ~38 % of point-0 (the approach only); fine today, but under P1 the lattice would double the joints | Covered by P1 (lattice removed, deck shows P1) | 0 |
| Court seal | Procedural brass 8-point star, ring pair r 3.44/3.82 (Heraldry.cs:12), floor decals | Gold-lined star medallion | ~15 % at start; reads on the phone (device-start) | Not needed now | 0 |
| Compass, brass inlays | Coping brass-rail region × vertex brass (0.70, 0.56, 0.34) | Paler worn-gold lines | Lines read; brass is orange-brown | Later, as a vertex tint (not a texture) | 0 |
| Kerb course, layout coping, gate threshold | Coping panel and rail regions via `MapCoping` (the slot artefact was fixed today) | Light coping stone along the rim | Artefact gone; the kerb is subtle (<2 %) | Later: one foundry decal atlas for the **whole** floor-decal batch (the test requires exactly one) | 0 |
| **Balustrade rail, posts, lantern posts** | Masonry @−11 (`BuildBalustrade`, Massing.cs:351): 0.72-tall rail (86 px), dark pointed "arrow-slit" opening quads, 1.34 posts | Pierced dark iron-and-stone balustrade with brass-capped posts | **8–20 %** at points 1, 3, 4 and the device boiler corner. Reads as a crenellated castle wall; rail face renders ≈#1D202C | **Paint now — P2** | +1 (0 with the P4 merge, §3) |
| Pit collar and pit pedestals | Same `RailSpan`/`LanternPost` code, masonry @−11, painter-interleaved with the pit domes | Brass-trimmed balustrade ring | ~4 % of point-1 | Later (moving it alone breaks the collar/dome layering) | 0 |
| **Foundation masonry** (sidewalls, bastions, below-deck cornices and pilasters, gate towers, finials, gateposts, cornices) | `FoundryMasonry` 1254²→1024², **Mirror**, blue #333641; face tints push blue further | Heavy violet-charcoal basalt blocks | **15–35 %** at points 0, 1 and 4; dominates the overview. Navy small-brick look, mirror seams | **Paint now — P3** | 0 |
| **Pit lining + inner boilers** | `FurnaceFacade` repeated on 8 panels plus a coal course, in one batch with the inner boilers' fronts @−15.5 (`BuildFurnacePit`, Massing.cs:252) | Deep ribbed pit glowing at the bottom | ~6 % of point-1; prominent in shaft-detail. A wall of repeated portholes and grates | **Paint now — P4** | 0 (−1 with the discharge merge) |
| Round boiler drums | `FurnaceFacade` wrapped twice around the tall drums; grate band only (v 0.14–0.5) on the squat ones; @−12 | Banded bronze-iron towers, fire doors, small lit windows | 10–16 % at point-3, point-4 and the device corner; reads acceptably as a fire cage | **Optional now — P5** (after P4) | 0 |
| Iron: shells, domes, pipes, reservoirs, crane, lantern cages | `FoundryIron` riveted panel; plates continuous today | Warm dark bronze-iron | 5–10 %; neutral blue-grey #333439 | Later: recolour the existing texture (no generation needed) | 0 |
| Chains | Procedural rings and bars sampling a plate-centre patch (today) | Heavy black links | <3 % | Not needed | 0 |
| Block caps, boiler plinths | `ENV_Platform_Floor` 8×8 grid (shared with the halls) @−13 | — | <3 % | Not needed | 0 |
| Gate facade | 1536×1024 → 2048×1024 ASTC 6×6 (fixed today) | Brass-sealed gate | ~25 % of point-4; good | Not needed | 0 |
| Beacons, lantern panes, fire windows | `FoundryEmbers` seam patch | Small warm lights | Tiny; more saturated (#E84203) than #E56A3E | Later (UV/tint) | 0 |
| Cavern backdrop | `ENV_Furnace_Cavern_v1` | Violet haze over the abyss | 5–25 % at points 0 and 1; good | Not needed | — |
| UI panel | 9-slice, 1024 ASTC 4×4 (fixed today) | — | Clean on the phone | Not needed | — |
| Old S8 chain and lantern sprites | — | — | — | Dropped: static SpriteRenderers are not moved by the depth pass, so they would draw over structures | — |

---

## 2. Generate now (ranked)

**Owner setup.**
1. Use one ChatGPT image chat for the whole set.
   - First message: attach `foundry-v1.png` with *"Style and palette reference only. Do not copy its composition, camera
     angle or perspective."*
   - For P4 and P5, also attach `Assets/_Project/Art/Props/Foundry/FurnaceFacade.png` (how we render iron, brass and fire).
2. Paste one prompt per message. If a result is close but flawed, correct it with a one-line edit instead of regenerating.
3. Save every raw output untouched (see README). The session does the cropping, seam checks and POT scaling.

**Seam healing** (P1 and P3: both axes; P2a, P4 and P5: left↔right only).
1. The session writes a copy of the image shifted by half its width (and half its height) and checks edge continuity.
2. If the seam shows, the owner uploads that copy with the prompt below.
3. The session shifts the result back and re-checks the edges.

```
This is the same texture shifted by half its width and half its height, so its old edges now meet in a cross through the middle. Repaint only a band about 120 pixels wide along that vertical and horizontal cross so the pattern continues naturally across it, matching the surrounding style, colours, sizes and joint width exactly. Keep the outer 150 pixels of every image edge exactly unchanged. Same size, same flat view, no border, no text.
```
For left↔right-only items, replace the first two sentences with *"...shifted by half its width, so its old left and right
edges now meet in a vertical line down the middle. Repaint only a vertical band about 120 pixels wide..."*

### P1 — Court flagstones v3 (rank 1)

**Why.** It is the largest surface in every run and the largest remaining gap: rectilinear 2×1 bricks against irregular
flags, and neutral grey against violet slate.

**Deliverable.**
- 1024×1024 PNG, RGB, no alpha, **seamless on all four edges**, Repeat, strictly top-down.
- About 5 stones across the tile (4–6), so a stone is ~1–1.4 cells (~170–240 px wide on screen), roughly the reference's
  hero-to-flag ratio.
- Joints 6–10 px.
- Average stone #5A5258. This assumes `_Color` 1.0 and today's cool open-court light (0.85, 0.895, 1.0), and renders
  ≈#4A4755–#524E5A.

**Prompt P1**
```
Seamless tileable hand-painted dark fantasy mobile game texture in the style of the attached foundry concept: painterly but clean and readable on a phone screen, broad simple shapes, 2 to 3 value steps per material, soft brush edges, no photographic noise, no fine speckle. Square, 1024x1024. Viewed from directly above: strictly top-down orthographic, completely flat, like a scanned floor. The paving of an old fortress foundry courtyard: irregular dressed basalt flagstones, mostly four- to six-sided slabs with slightly uneven straight edges and softly chipped corners, of similar size, about 5 stones across the width of the image (between 4 and 6), fitted closely in no regular rows, with a few small infill stones between them. Narrow dark joints 6 to 10 pixels wide in dark violet-grey (#34303A), with a thin lighter worn rim along the stone edges (#756C72). Stone faces in dark violet-grey slate with an average of #5A5258; neighbouring stones differ clearly but gently in value, from #4C4550 to #665D64, with faint broad mottling inside each stone and a few shallow scratches. Even soft ambient light from straight overhead. The texture must tile seamlessly: stones cut by the left edge continue on the right edge, stones cut by the top edge continue at the bottom edge, and no single stone is larger, brighter, darker or more distinctive than the rest.
Negative: no perspective, no isometric angle, no vanishing point, no camera tilt, no cast shadows, no light from one side, no vignette, no brightness gradient across the image, no glow, no text, no letters, no numbers, no runes, no symbols, no logos, no watermark, no characters, no creatures, no plants, no moss, no puddles, no frame or border, no brass or metal lines, no inlays, no rectangular bricks, no grid, no running bond, no deep cracks, no rubble, no holes.
```

**Fallback P1-b** (only if the owner prefers to keep today's geometric flags): a jointless worked-stone face. Use the P1
prompt with the flagstone sentences replaced by *"one continuous dressed basalt surface with NO joints and NO stone
edges; broad soft mottling in patches 150 to 300 pixels across between #4E4750 and #5E5662, faint diagonal chisel
tooling"*. That needs no code change beyond `_Color`.

**Integration.**
- Save as `Assets/_Project/Art/Props/Foundry/FoundryPavingV3.png` (V2 stays for rollback). Bind it to
  `MAT_FoundryPaving._MainTex` and raise `_Color` from 0.8 to 1.0.
- `FoundryRegionFinish.Paving` (Finish.cs:223):
  - replace the joint bed, face, four bevels and the corner repair with **one** `LitFloorQuad` over the flag at
    `stone = 1`, and drop `flagTint`;
  - the flag outlines stay only as light-sampling corners, so `FloorLight` is unchanged;
  - keep the zone tints (seal inset, apron, west strip) as broad washes.
- Shader range: `FloorLight` peaks below 1.74 per channel. The Finish.cs:74–82 comment sizes that headroom for
  `_Color` 0.8 × the brightest bevel (×1.435). With `_Color` 1.0, `stone = 1` and the seal-inset tint (×1.08), the peak
  is ≈1.88, still under the ±2 limit. Do not reintroduce per-flag tone above ×1.06 while `_Color` is 1.0.
- `BuildBridgeDeck` (View.cs:89): keep the 26×4 bed loop at tone 1 and drop `DeckLattice`/`BridgeStone`. Otherwise the
  procedural diamonds draw a second joint pattern over the painted flags. The court flags then continue onto the bridge;
  the kerb and brass lanes (decals) stay.
- `BuildCourtStoneRing` (Finish.cs:264): keep it for the first capture, and drop it if it reads as a second, geometric
  joint ring.
- Optional, only if the repeat shows: scale UVs by 1/1.5 inside `LitFloorQuad`. That gives 9.07 cells per tile, ~3
  repeats per screen and ~0.95 texel/px horizontally.
- About −12k to −15k of the 58,612 static vertices (estimate). Renderer delta 0.

**Import and memory.**
- Repeat on both axes, Trilinear, **aniso 2**, mips, sRGB, not readable, alpha none.
- Default and Android max size 1024. Android ASTC 6×6: **0.61 MB, net 0** against V2.
- If the phone shows block artefacts in the soft gradients, try 4×4 (1.40 MB, +0.79).

**Judge in game.**
- start, point-2 and device-start: irregular laid stones at arm's length, no recognisable stone or seam repeating, and
  joints never read as cracks or obstacles.
- The hero, enemies, the blue range ring, telegraphs, gems and gold stay clearly readable: the floor stays at least 2
  value steps below the hero's steel.
- The open court samples ≈#4A4755–#524E5A (today #393B41).
- point-1 and point-3: the warm pools still read.
- point-0: the bridge reads as the same paving, with no lattice.
- The seal and compass still read over the new flags.

### P2 — Balustrade trim (rank 2)

**Why.** The balustrade is the eye-level boundary of the playfield in every edge view. Today it reads as a battlement with
arrow slits. The shape (rail, posts, lanterns) is right; the surface and openings are not, and paint fixes both.

**Deliverable.** Two raw images. The session composes `FoundryTrim.png`, 1024×512 RGB (POT):
- **Top half, rows 0–255: band A**, the rail elevation from P2a.
  - Crop the strip, heal it left↔right, scale it to 1024×256 (6 bays of ~171 px).
  - Add 8 px guard rows of #1A171F at the top and bottom of the band.
- **Bottom half, rows 256–511:** four 256×256 cells.
  - C1: the post face from P2b. A post is shown at ~49 × 134 px, so squashing the art into the cell is harmless.
  - C2: a plain coping-top stone patch cut from P2a, for rail tops, post caps and post pyramids.
  - C3–C4: spare (later a lantern-post face and the pit collar).
- Scale: band A repeats every **3.6 design units** along an edge (~344 × 86 px on screen, 4:1 like the band).

**Prompt P2a — rail elevation**
```
Hand-painted dark fantasy mobile game texture in the style of the attached foundry concept: painterly but clean and readable on a phone screen, broad simple shapes, 2 to 3 value steps per material, soft brush edges, no photographic noise. Landscape, 1536x1024. One long straight strip of a heavy fortress balustrade seen exactly straight-on in front elevation: orthographic and flat, only the front face, no top surface and no side faces visible. The strip runs across the full width of the image and fills the middle 40 percent of the image height; above and below it is a plain flat dark background (#15121A). The strip shows exactly 6 identical bays and tiles seamlessly from its left edge to its right edge: the left edge cuts a baluster exactly in half and the right edge cuts the same baluster in half. From top to bottom the strip has: a heavy dark basalt coping course (#3F3941) with a lighter worn upper arris (#5A5360), about 14 percent of the strip height; directly under it a thin dull aged-brass fillet line (#8C7566); then a row of tall rectangular openings filled with deep shadow (#120F16), separated by thick square balusters of dark iron-bound stone (#35303A), each baluster with a small dull brass collar at its top and bottom; then a plain dark plinth course (#2E2A33), about 20 percent of the strip height, with a thin brass line along its top edge. The metal is flat, dull and worn, never shiny. Even soft frontal ambient light.
Negative: no perspective, no vanishing point, no top surface, no cast shadows, no light from one side, no vignette, no glow, no text, no letters, no runes, no symbols, no logos, no watermark, no characters, no plants, no frame or border, no crenellations, no battlements, no arrow slits, no arches, no pointed openings, no posts rising above the rail, no lanterns, no scenery or floor visible through the openings.
```

**Prompt P2b — post face**
```
Hand-painted dark fantasy mobile game texture in the style of the attached foundry concept: painterly but clean and readable on a phone screen, broad simple shapes, 2 to 3 value steps per material, soft brush edges, no photographic noise. Portrait, 1024x1536. One square stone-and-iron balustrade post seen exactly straight-on: orthographic and flat, only its front face, no side face and no top visible. The post is centred, about 45 percent of the image width and 85 percent of the image height, on a plain flat dark background (#15121A). From bottom to top: a plain dark plinth block (#2E2A33); a tall shaft of dark basalt (#3F3941) with one recessed vertical panel (#2A2630) edged by a thin dull brass line (#8C7566); a dull aged-brass band with small rivets (#8C7566, lit edge #B78052) at one third of the height and a second one just below the top; at the top a projecting cap moulding in lighter worn stone (#5A5360), about 10 percent of the post height. The metal is flat, dull and worn, never shiny. Even soft frontal ambient light.
Negative: no perspective, no side face, no top surface, no cast shadows, no light from one side, no glow, no flame, no lantern, no pyramid roof, no statue, no ornament on top, no text, no letters, no runes, no symbols, no logos, no watermark, no characters, no frame or border.
```

**Integration.**
- New slot `_foundryTrim` (in `PlatformMaterialSet` and `PlatformMaterials.asset`) → `MAT_FoundryTrim` (Sprites/Default).
- Scope: only the `BuildBalustrade` path.
  - `AddRailSpan` (Massing.cs:449), `PlaceRailPost` (:472) and `LanternPost` (:538) create "Balustrade rail/post/lantern
    post" with the trim material at **order −11.5**. That sorts at −9: above the boiler drums (−10), below masonry and
    iron −11 (−8).
  - `LanternPost` is also called by `BuildFurnacePit` for the pit pedestals, so pass the material in; the pit collar and
    pedestals stay masonry at −11.
  - Below-deck cornices and pilasters (−16) stay masonry.
- UVs:
  - Long rail faces (body 0–0.62 and coping 0.62–0.72): u = along/3.6, v = band A scaled by h/0.72. Here "along" is
    design u on v-faces and −design v on u-faces, so the art runs left→right on screen and continues across spans.
  - Drop `RailOpenings` (:506); the openings are painted now.
  - End faces, caps and post pyramids → C2. Post and lantern-post faces → C1 (u 0..1 across the face, v over body + cap).
- Face tints: the texture carries the value, so use near-neutral tints for these pieces: lit ≈ (0.95, 0.93, 0.97),
  shade ≈ (0.68, 0.66, 0.72). Today's `LitFace`/`ShadeFace` are what make the rail #1D202C.
- Painter checks:
  - The court finials (masonry −11) paint over rail ends. They stand in the rail line, so this is correct.
  - The gate towers now cover the last ~0.1 unit of the apron rails (gate-detail).
  - Rails behind boilers are already culled by `MassConflicts`.
- Renderer delta **+1**; see §3. Fewer vertices (the opening quads go).

**Import and memory.**
- **wrapU Repeat, wrapV Clamp** (set per axis), Trilinear, aniso 1, mips, not readable.
- Android max 1024, ASTC 6×6: **0.31 MB**.

**Judge in game.**
- point-1, point-3, point-4, the device boiler corner and gate-detail: a pierced balustrade with brass fillets and capped
  posts, not a battlement.
- The pattern runs on across spans and around corners.
- The brass reads as worn gold.
- The cutaway still opens rails in front of the hero.
- No rail draws over a boiler or under the wrong finial.

### P3 — Foundation masonry v2 (rank 3)

**Why.** Zero renderer cost and the widest reach: sidewalls, bastions, gate towers, finials, cornices, and the balustrade
too until P2 lands. It fixes the navy small-brick look and the Mirror seams.

**Deliverable.**
- 1024×1024 PNG, RGB, **seamless on all edges**, Repeat, straight-on elevation.
- 5 courses per tile (~205 px each, ~58 px on screen).
- Average #5A525C. The face tints multiply it, so it is painted about 1.4× the reference wall value.

**Prompt P3**
```
Seamless tileable hand-painted dark fantasy mobile game texture in the style of the attached foundry concept: painterly but clean and readable on a phone screen, broad simple shapes, 2 to 3 value steps per material, soft brush edges, no photographic noise, no fine speckle. Square, 1024x1024. Straight-on front elevation, orthographic and completely flat, of a massive fortress foundation wall of dressed dark basalt ashlar: exactly 5 horizontal courses of heavy rectangular blocks, each course about 205 pixels tall with slight variation, blocks 1.5 to 3 times wider than tall, vertical joints staggered from course to course. Block faces dark violet-charcoal with an average of #5A525C and clear but gentle per-block value variation (#4E4750 to #665D66), softly chipped lighter arrises (#766C75), a few faint vertical soot streaks. Deep narrow joints 8 to 10 pixels wide (#221E27). Even soft frontal ambient light. The texture must tile seamlessly: the left edge continues on the right edge and the top edge continues at the bottom edge, and no course is thicker or thinner at the seam.
Negative: no perspective, no top surface, no side faces, no cast shadows, no light from one side, no vignette, no brightness gradient, no glow, no windows, no arches, no doors, no metal, no text, no letters, no runes, no symbols, no logos, no watermark, no characters, no plants, no moss, no frame or border, no bright highlights, no blue cast.
```

**Integration.**
- Save as `FoundryMasonryV2.png` and bind it to `MAT_FoundryMasonry` (slot `_foundryWall`). V1 stays for rollback.
- No mapping change. Wall UVs, cylindrical UVs (boiler cornices, foundations, pit domes) and floor UVs on caps all keep
  working.
- Recommended (optional) code palette fix: every face tint pushes blue. They are:
  - `Block` (0.63, 0.66, 0.77) / (0.43, 0.46, 0.57);
  - `LitFace`/`ShadeFace` (same values);
  - the `ArchitecturalTier` shades (0.76, 0.79, 0.87)…(0.48, 0.52, 0.64);
  - the sidewalls and bridge coping (0.7, 0.7, 0.76);
  - `StoneLight` (×1.13 blue).

  Move them to neutral-violet, e.g. lit (0.72, 0.70, 0.72) and shade (0.50, 0.48, 0.53). Lit faces then render ≈#413942,
  matching the reference wall #3F3941. With today's tints they render ≈#393647.
- Renderer delta 0.

**Import and memory.**
- **Repeat** (was Mirror, which mirrors every 336 px), Trilinear, aniso 2 (caps are floor-mapped), mips.
- Android 1024, ASTC 6×6: **0.61 MB, net 0**.

**Judge in game.**
- point-0 (bridge sidewall), point-1 (below-deck face, bastion), undercroft-detail, gate-detail and overview.
- Heavy blocks, no mirror seams, violet-charcoal rather than navy.
- The gate towers and boiler cornices still read.

### P4 — Furnace pit lining (rank 4)

**Why.** The pit is the reference's signature landmark. Ours repeats the boiler facade (portholes and grate) eight times.
Zero renderers, and it can free one (§3).

**Deliverable.**
- Generate 1024×1536. The session heals it left↔right and scales the whole image to **512×1024** (POT, non-uniform).
- A panel is 1.725 design units along the wall × 2.6 deep, ~146 × 312 px on screen.
- The left and right edges end in half ribs, so panels join.
- The bottom 12 % is an ember bed; it also serves the coal course and the undercroft discharge.

**Prompt P4**
```
Hand-painted dark fantasy mobile game texture in the style of the attached foundry concept and the attached furnace front: painterly but clean and readable on a phone screen, broad simple shapes, 2 to 3 value steps per material, soft brush edges, no photographic noise. Portrait, 1024x1536, filled edge to edge. Straight-on front elevation, orthographic and flat, of ONE bay of the inner wall of a deep industrial smelting pit. The bay will be squeezed horizontally in the game, so draw every element about 30 percent wider than natural. The left and right image edges each end exactly in half of a heavy vertical rib of black riveted iron (#2A2530) with dull aged-brass straps (#8C7566), so identical bays join seamlessly side by side. Top 8 percent: a dark heavy stone lip course (#2E2A33). Between the ribs: large simple soot-stained basalt blocks (#3A3239) that grow warmer and redder toward the bottom as if lit by fire below (#5A2E26 at two thirds of the way down, #99432F near the bottom), with one slim vertical iron pipe. Bottom 12 percent: a bed of glowing coals and embers across the full width (#E56A3E, a small hot core #FBC06C), with a slightly irregular top edge. The upper half stays dark and cool; heat only rises from the bottom. Otherwise even frontal ambient light.
Negative: no perspective, no top or side surfaces, no floor surface, no cast shadows, no light from one side, no portholes, no round windows, no grates, no doors, no arches, no lava river, no flames above the bottom 12 percent, no text, no letters, no runes, no symbols, no logos, no watermark, no characters, no creatures, no frame or border.
```

**Integration.**
- Save as `FoundryPitLining.png`. New slot `_foundryPitLining` → `MAT_FoundryPitLining`.
- `BuildFurnacePit` (Massing.cs:252–271): create "Furnace pit lining and inner boilers" with the new material (falling
  back to `FurnaceFacade`) at the same −15.5.
  - Lining panels keep their UVs (0..1; v 0 at 2.6 below the deck). Tint (0.84, 0.70, 0.58) → ≈(0.95, 0.92, 0.90).
  - The coal course moves from facade region (0.31–0.69, 0.16–0.26) to (0.1–0.9, 0.02–0.10), the new ember bed.
  - Inner boilers (`PaintedDrum`, one front): set v to 0..0.55.
- `FurnaceFacade` stays in use by the boiler drums, so the surfaces test still passes.
- Renderer delta **0**: the `FurnaceFacade`@−15.5 batch becomes the pit batch.
- **Optional −1:** in `FurnaceDrain` (Undercroft.cs:160–164), map the discharge strip onto the ember bed and create it
  with the pit material at −15.5. The `FoundryEmbers`@−15.5 renderer then disappears. Nothing overlaps on screen: the
  discharges hang at u 18.5 and 26.5, the pit is at u −12..−4, and within the batch the discharge is emitted first.

**Import and memory.**
- Clamp, Trilinear, mips, not readable.
- Android max 512 → ships 256×512 ASTC 6×6, **0.08 MB** (1.7 texel/px).

**Judge in game.**
- point-1 and shaft-detail: the pit reads deep, cool at the top and hot at the bottom.
- No repeated portholes or grates, and the panel joins are invisible.
- If merged: the undercroft discharges still glow (overview, undercroft-detail).

### P5 — Boiler drum wrap (rank 5, optional; requires P4)

**Why.** The reference's round boilers read as banded bronze-iron towers with fire doors. Ours bend the flat furnace
facade around the drum. It costs 0 renderers once the pit has its own material.

**Deliverable.**
- 1024×1024 PNG, RGB, **seamless left↔right**, Clamp.
- The unrolled front half is ≈2.9 × 2.9 design units per repeat, because `PaintedDrum` wraps it twice around the visible
  half.
- The fire-door band sits at v 0.14–0.50 because the squat boilers show only that band.

**Prompt P5**
```
Hand-painted dark fantasy mobile game texture in the style of the attached foundry concept and the attached furnace front: painterly but clean and readable on a phone screen, broad simple shapes, 2 to 3 value steps per material, soft brush edges, no photographic noise. Square, 1024x1024, filled edge to edge. The curved front of a large round industrial boiler UNROLLED FLAT like a map projection, straight-on, with no cylinder shading and no highlight or shadow at the sides; the left and right edges must join seamlessly. Dark riveted iron plates with a faint warm bronze cast (#3A2E2C to #43332C) and lapped vertical plate seams. From bottom to top: 0 to 14 percent a heavy base ring (#2A2226); 14 to 50 percent a fire-door band with one wide round-arched fire opening centred horizontally, about 45 percent of the image width, with thick vertical iron grate bars in front of glowing coals (#E56A3E, small hot core #FBC06C), set in a heavy dull bronze frame (#8C7566, lit edge #B78052); 50 to 55 percent a dull brass retaining belt with a row of rivets; 55 to 92 percent a tall plate course with two small arched gauge windows glowing faint orange, one at the left third and one at the right third; 92 to 100 percent a second brass belt. The metal is flat, dull and worn. Glow only inside the fire opening and the two small windows.
Negative: no perspective, no cylinder shading, no specular highlights, no cast shadows, no light from one side, no pipes or valves sticking out, no gauges with dials or numbers, no pillars or columns, no portholes, no wood, no planks, no text, no letters, no runes, no symbols, no logos, no watermark, no characters, no frame or border.
```

**Integration.**
- Save as `FoundryBoilerWrap.png` with a new `MAT_FoundryBoilerWrap`, and repoint the `_furnaceFacade` slot to it.
  `MAT_FurnaceFacade` and `FurnaceFacade.png` stay in the project for rollback.
- No code change: `BoilerDrumFaces` / `PaintedDrum` (Massing.cs:147, :828) already map u 0..1 per 5 facets, v 0..1 on
  the tall drums (0.45–3.35) and v 0.14–0.50 on the squat ones.
- Renderer delta 0.

**Import and memory.**
- Clamp, Trilinear, mips.
- Android max 512 → 512² ASTC 6×6, **0.16 MB**.
- `FurnaceFacade.png` (1.2 MB) leaves the build once nothing references it, so the net is ≈ −1.0 MB.

**Judge in game.**
- point-3, point-4, furnace-detail and the device boiler corner: banded iron cylinders with a fire door.
- The squat boilers show the fire band.
- The windows stay small accents, and the join between the two wraps is invisible.

**Memory for the set** (ASTC 6×6): P1 0, P2 +0.31, P3 0, P4 +0.08, P5 −1.04 → about +0.4 MB without P5, about −0.65 MB
with it. Draw cost is unchanged except P2's renderer.

---

## 3. Renderer budget decision

Today, one renderer per (material, order); sorting order in brackets:

| Material @ order | Contents |
|---|---|
| Embers @−17 (−18) | pit coal bed |
| Masonry @−16 (−17) | sidewalls, block foundations, undercroft, balustrade cornices and pilasters, boiler foundations, bastions |
| Iron @−16 (−17) | exterior and undercroft suspension chains |
| Iron @−15.4 (−16) | undercroft anchors, discharge casings, bastion chains |
| Embers @−15.5 (−16) | undercroft furnace discharge (2 strips) |
| FurnaceFacade @−15.5 (−16) | pit lining + inner boilers |
| Paving @−15 (−15) | court, apron, bridge deck, seal stone ring |
| Coping @−14.5 (−14) | **floor decals** (plain shader; exactly one is asserted) |
| Masonry @−14 (−14) | bridge coping rails, raised block sides |
| Floor @−13 (−12) | block caps, boiler plinths |
| Iron @−12.5 (−11) | crane-footing iron roof (**one quad**) |
| GateFacade @−12 (−10) | gate facade |
| FurnaceFacade @−12 (−10) | boiler drums |
| Masonry @−11 (−8) | gate architecture, finials, gateposts, balustrade, pit collar and domes, cornices |
| Iron @−11 (−8) | crane, reservoirs, pipes, workshop |
| Iron @−10.5 (−7) | boiler shells and domes, mains, gate and post metal, lantern cages |
| Embers @−10 (−6) | beacons, lantern panes, boiler windows |

Item costs: P1 0 · **P2 +1** · P3 0 · P4 0 (or −1 with the discharge merge) · P5 0.

- **A — stay at 17 (recommended).** Land P4 with the discharge merge (17 → 16), then P2 at −11.5 (→ 17).
  - No test change.
  - No visible cost expected: the discharge glow comes from the pit texture's ember bed instead of `FoundryEmbers`.
  - Makes P2 depend on P4.
- **B — stay at 17 without P4.** Drop the crane footing's one-quad iron roof (`MetalCap`, Dressing.cs:27/278); the
  footing then shows its stone cap.
  - Visual cost: the crane plinth top (west edge and overview).
  - Merging the roof into iron −11 instead is **not** safe: the finial at (−18.5, 12) stands on that footing and shares
    sorting order −8 with it, so their draw order would be undefined.
- **C — owner raises the cap to 18.** Change `FoundryRegionTests.cs:51` and its message in the same change as P2.
  - Cost: two more draw calls (colour and depth-only pass). Not measured, but small on the S23.
  - The cap loses some value as a fragmentation guard, and later items that need their own material (a second floor
    variant, a separate bridge material: +1 each) reopen the question.
- **Not an option:** P2 at order −11 in its own batch. It would sort at −8 like masonry −11, with no painter guarantee.

---

## 4. What changed since painted-surfaces-01 (why 01 is superseded)

- **Memory table wrong.** It listed FurnaceFacade and GateFacade as 683×1024 / 1024×683 ASTC 6×6 (~0.83 MB together).
  They were NPOT and shipped RGBA32, ~3.6 MB each. Today both ship at 2048 ASTC 6×6, 1.2 MB each. The "≈5.7 MB today"
  total is wrong; the build report shows ≈7.2 MB of environment textures.
- **Gate note wrong.** Raising only Android max to 2048 would have shipped 1536×1024 RGBA32 (8 MB). It needed nPOTScale
  ToLarger, which was done today.
- **S4 (rim trim).** It targeted `BuildPerimeter` (0.28 rise), which is no longer called. The rim is now the 0.72
  balustrade (`BuildBalustrade`), and the slot row was fixed with UV regions. Its "+1 decals renderer at −14" is moot.
- **S6 (shaft lining).** It targeted `BuildShaft` "Shaft heated lining", which is now dead code. The live pit is
  `BuildFurnacePit` (6.9-unit collar-inset walls, 2.6 lining + 0.6 coal course), with the inner boilers in the same batch.
  Its delta is 0 only if both move together.
- **S7 and S8.** Reservoir and boiler facets no longer map the full iron panel, and chains are procedural heavy links.
  The lantern sprite would bypass the depth system.
- **S2 and S3.** The seal is now a procedural brass star, so S2's radii spec no longer matches it. The bridge is now a
  diagonal lattice in the paving batch; any painted floor must remove it, and a separate bridge texture (+1) is unneeded.
- **S1.** It did not note that the `Quad(floor:true)` divisor is shared by every floor-mapped material, or that the
  1254² sources depend on the max-size clamp to stay POT. Its ASTC 4×4 default costs +0.79 MB; this pack starts at 6×6.
