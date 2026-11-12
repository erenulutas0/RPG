# 51 - Gloam Gardens Art Direction (Floor 3)

Status: v1 proposal, 2026-11-12. Floor 3 (docs/48) is being built in data on the same map geometry as floors 1 and 2; this
gives it its own look, the way docs/49 gave floor 2 one. Nothing in Assets changes with this doc.
Mood board: `ArtDirection/2026-11-12/gloam-gardens-01/concept/moodboard.png` (untracked, by convention), made by
`Tools/ArtReview/GenerateGloamMoodboard.py` (v1 kept beside it as `moodboard-v1.png`).
Backdrop plate concept: `ArtDirection/2026-11-12/gloam-gardens-01/concept/backdrop.png` (1024x1536, the foundry plate's
size; earlier passes `backdrop-v1..v3.png`), made by `Tools/ArtReview/GenerateGloamBackdrop.py`. Concept only, not in Assets.

## 1. Concept and lore fit

docs/44 section 1.6 names Floor 3 **Gloam Gardens**: plants growing on ash, moving roots; boss **Rootmother Myrr**
("the gardener who grafted lost futures onto trees"); enemies **Cinder Bloom**, **Thornling**, **Pollen Wraith**; the
environmental clue is *trees that feed on memory instead of light*. The timeline puts "the Dawn Engine first warms the
cities" (year -21) on floor 3.

Proposal: **the foundry, abandoned and overgrown at dusk.** Above the frozen vaults the tower's old works stood cold long
enough for a garden to grow out of their ash. The architecture is still the foundry's masonry and iron (same builders,
same procedural layout), now buried in ash-loam, mossed, split by roots; brass has gone to verdigris. Nothing burns any
more. The only light the garden makes is **gloamlight**: a pale chartreuse glow in seed-lanterns, fungi and sap, the
memories the trees feed on. The lava of floor 1 and the quicksilver of floor 2 become luminous sap in the cracks. The sky
is a green-black gloam with the dimmed sun (the Long Dimming) low behind ash cloud: the Dawn Engine's promise seen from
inside its failure.

Three floors, three temperatures: **Foundry** warm (orange, brass, violet-grey stone), **Vaults** cold (blue slate,
cyan, violet), **Gardens** organic (ash-olive, moss, bark, chartreuse, with orchid reserved for enemies).

Enemy fit: Cinder Bloom = the foundry's golems with ash-bark bodies and orchid blooms where the lava was; Thornling = the
Runner's thorned cousin; Pollen Wraith = a pale drifting mote (the Mite's slot). Rootmother Myrr is the Warden's slot
until she gets art of her own (docs/48 3.2 item 5).

## 2. Palette

| Role | Name | Hex |
|---|---|---|
| Ground | Ash loam deep / ash loam / ash loam light | `#1D1F1A` / `#3C3F36` / `#63665A` |
| Ground accent (off-lane only) | Ash drift | `#A7A99C` |
| Walls | Wall stone, mossed / wall cap, lichen | `#2C3029` / `#7C8466` |
| Wood (replacing iron) | Root bark / heartwood | `#2A221E` / `#5B4838` |
| Growth | Moss / moss lit / lichen | `#3F5A32` / `#4F6B3A` / `#8FA35A` |
| Accent replacing brass | Verdigris | `#6FA38C` |
| Accent replacing lava | Gloamlight / gloamlight core | `#C6EE6A` / `#E2F7A0` |
| Light | Shadow tint (moss shade) | `#1F2E2A` |
| Atmosphere | Ash haze | `#767A68` |
| Backdrop | Sky gloam / sky dusk / dimmed sun | `#0B110F` / `#23302A` / `#CDBE8E` |
| Enemies | Bark body / bloom deep / bloom (orchid) / bloom light | `#4A3B33` / `#5A1F66` / `#D86BD0` / `#F6C2F0` |
| Telegraph (unchanged) | Warning red-orange | `#FF5A3C` |

Hue map (degrees): telegraph 9, Ilya's ember `#E77232` 21, gold pickups ~40, gloamlight 78, XP crystal ~205, Vanguard
`#234097` 225, the Vaults' violet 255, orchid bloom 304. The orchid sits at least 65 degrees from every other signal
colour in the game. Floor values sit in the 10-45 % luminance band. The mood board's paving preview measures a mean
luminance of 0.30 and a mean saturation of 0.16: inside docs/49's rule (saturation below 0.2) and a little darker than
the ice paving.

Against the other floors: the Foundry's floor is violet-grey and its accents orange and brass; the Vaults' floor is blue
slate and its accents cyan and violet. The Gardens' floor is olive-grey and its accents green-yellow and verdigris. No
floor shares an accent hue.

## 3. Materials

Re-tinted (same rasters' structure, same UVs and layouts, as docs/49):
- Paving: FoundryPavingV10 -> **GloamPavingV1**: values on the ash-loam ramp, moss gathered in the joints and in soft
  patches, sparse pale ash flecks (no gloss, no glints). Previewed on the mood board.
- Masonry: FoundryMasonryV4 -> **GloamMasonryV1**: the same recipe, more moss on the lower courses. Previewed.
- The ice pass 7 list, by the same script: FoundryPavingV8 (outside the court, at GloamPavingV1's value), FoundryIronV2
  and FoundryCastIronV2 (iron to dark bark-brown iron with verdigris stains), FoundryBoilerWrap (fire window to a dim
  gloamlight), ENV_Platform_Floor (wall caps, lichen-grey).
- Court flagstones, coping, platform floor/wall: the tint table through FoundryRegionTheme.
- Embers (FoundryEmbers) -> **ash flakes and pollen motes**: same particle/sprite system; ash flakes grey `#A7A99C`, slow
  and drifting sideways; one mote in five gloamlight.

New:
- **Sap channel**: replaces floor 2's mirror-ice inlay on the court rails: dark bark-brown with a thin, dim gloamlight
  line (about 50 % of `#C6EE6A`), never neon.
- **Root well** (the pit): the lava pool becomes dark sap, near-black green, with gloamlight veins at 60-65 % (the ice
  pit's 0.65 glow scale). The pit's eruption warning stays red-orange (section 7).
- **Moss cap**: the ice pass 5 rime on upward faces becomes moss (`#3F5A32` to `#4F6B3A`), same triangle rule (structures
  only, never the floor or decals).
- Backdrop plate (section 6).

## 4. Props

All are swaps of floor 2's theme props, one for one, on the same rules and the same vertex cap:
- **Hanging roots** replace icicles: thin, slightly curved, dark bark with a heartwood edge, from the camera-facing outer
  rim and gate crowns only. Never over a lane, where a dark vertical line could read as a projectile or a hazard.
- **Fungus and bloom clusters** replace ice crystals: low bracket fungi and seed pods at wall bases and corners, tallest
  at the region edge; tips in gloamlight. No orchid on any prop (orchid is the enemies' colour).
- **Root-bound chains**: the foundry chains, re-tinted dark iron, wrapped by a root at the low point.
- **Verdigris** on every brass, bronze and gold trim (the ice pass's silver mapping, to `#6FA38C`).
- **Ash drifts** (decals) against walls and in corners only, never on lanes or spawn points: a pale ash drift is the
  same value as Ilya's robe (section 7).
- **Seed-lanterns**: the parapet braziers become hanging pods of gloamlight, same placement and cost.
- **Lore objects (later)**: a grafted tree with a name-plate, a child's boot grown into a root (docs/44's clue style).

## 5. Lighting and atmosphere

- Key light dim and slightly warm-green (dusk), desaturated; shadows tinted moss shade `#1F2E2A`, not black and not
  violet (violet is floor 2's shadow).
- **Gloamlight braziers**: `#E2F7A0` core, `#C6EE6A` falloff, same placement and baked/sprite cost as the ember
  braziers. Lower intensity than the foundry fires: the garden is darker than the foundry, lighter than nothing.
- Cracks and sap glow gloamlight instead of lava orange or quicksilver.
- **Ash and pollen**: the ember emitter budget 1:1 (no extra emitters). If the S23 frame time is short, halve the
  count before anything else (docs/49's rule).
- Ash haze (`#767A68`) at the far edge only, to separate the backdrop. The atmosphere view's existing material carries
  it (docs/48 3.2 item 3): no new geometry.

## 6. Backdrop

Concept: `concept/backdrop.png`, 1024x1536, the foundry plate's composition: two giant root-trunks frame the chasm
where the foundry had cliffs (bark ridges, moss, lichen, a rim of sky light on the edge); hanging roots from the top
corners where it had stalactites; bracket fungi and seed-lanterns in gloamlight where it had lava windows; in the far
middle, ruined foundry towers overgrown under tree crowns, three great trees in the mid-ground, a low ash field with
gloamlight sprouts; the dimmed sun pale and low behind ash cloud bands; ash flakes and pollen motes; the dark lower band
where the arena meets the plate. Painted once as a texture at the plate's size; the lanterns may get a slow pulse
through the existing material (one material, no extra renderer).

Known gaps of the concept (procedural, for direction only): the crowns read as soft cloud-shapes, the far towers are
faint, and the sun can read as a moon. A painted v2 should give the crowns leaf-mass and drape, the towers a clearer
foundry silhouette (chimneys, the gate's crenellation), and the sun a warmer, flatter disc with a darker corona.

## 7. Readability rules

1. **Hero first.** The Vanguard's blue (`#234097`) is complementary to the olive floor, so hue carries it; value does
   not (both dark). Keep the floor's mean luminance in 25-40 % and its saturation below 0.2. On the darkest loam the
   Vanguard loses his edge (mood board, first panel): keep `#1D1F1A` to joints and shade, never a lane's base, and keep
   his contact shadow as on floor 2.
2. **Ilya's robe.** Ilya's ash-white robe (`#D8CFC2`) is nearly the value of the ash drift (`#A7A99C`): on the drift
   panel she keeps only her outline and hood. Ash drifts and pale ash stay off lanes (at most ~10 % of a lane's area, less than
   floor 2's frost); her staff's ember flame reads on every floor value.
3. **Orchid is the enemies' colour.** No prop, light, VFX or UI on this floor uses hue 280-330. Enemy bodies are charred
   bark a step lighter than the floor with the dark outline kept, so they read by value on ash loam and by the orchid
   bloom on light loam.
4. **Telegraphs stay red-orange.** The pit eruption and the slam mark keep `#FF5A3C` and are the only red-orange on the
   floor. Gloamlight must never shift toward amber, and no seed-lantern may be warm. Check the pit warning on
   GloamPavingV1 in the first Unity pass (the ice capture's pit-warning view, per floor).
5. **Gloamlight is not a pickup.** Pickups are XP blue and gold; gloamlight must stay out of the lanes (sap lines dim,
   fungi at walls), so a bright chartreuse dot on the floor is never mistaken for a drop. Check any green heal VFX
   against it.
6. **Damage numbers** white with a dark stroke (unchanged). **HUD** unchanged; gold stays gold.

## 8. Budget

Floor 2's region as captured (`ice-map-01/floor2/runtime-budget.txt`): 39 renderers, 162,743 vertices, 3,572 distant,
against ceilings of 174,000 and 3,600 (docs/48 3.3).
- Phase 1 (textures and tints): **zero** renderer or vertex change.
- The garden growth mesh (roots, fungi) takes the ice crystal mesh's place under the same `IceCrystalVertexCap` (6,000);
  only one theme mesh is shown at a time, and the ice mesh must be hidden on floor 3 as it is on floor 1.
- Particles: the ember budget, not more.
- APK and memory: the floor-2 theme added about 20 MB of PNG sources in Resources/Ice, 2.9 MB of environment textures,
  182 enemy drawings (5.2 MB) and a 2 MB plate, before ASTC. Floor 3 adds the same again. Resources/<Theme> textures
  are loaded by name only on their floor; measure the resident texture memory after floors 1-3 in one run on the S23
  and unload the previous floor's set if it does not fit.
- Measure S23 frame time before any new geometry (docs/48 step 6).

## 9. Production plan

Reuse the floor-2 theme pipeline step for step:

1. **Theme hook.** `FloorTheme { Foundry = 0, Ice = 1, Gloam = 2 }` in FloorDefinition.cs; Floor_GloamGardens.asset
   sets `_theme: 2`. FloorThemeTests gains the Gloam rows (each copy loads, floor 1 and 2 unchanged).
2. **Region re-tint.** FoundryRegionTheme.ApplyTheme: the `ice` bool becomes a per-theme table (paving, masonry, bare
   stone, glow, rail colours, atmosphere), with GloamTint/GloamRail beside IceTint/IceRail; property blocks take
   GloamPavingV1/GloamMasonryV1 from PlatformMaterialSet. FoundryAtmosphereView: haze, motes and pit glow per theme.
   One shared path, never a copied body per theme (the stamina-divergence lesson).
3. **Textures.** `GenerateGloamTextures.py` (GenerateIceTextures.py's structure; the recipe is
   GenerateGloamMoodboard.gloam_surface) writes Art/Environment/Gloam; `GenerateGloamGlowTextures.py`
   (GenerateIceGlowTextures.py's SOURCES list: orange to gloamlight, brass to verdigris, rime cap to moss cap) writes
   **Resources/Gloam/<same name>.png**, loaded by name only on floor 3 (IceVariant generalised to the theme's folder).
4. **Enemies.** `GenerateGloamEnemyArt.py` (GenerateIceEnemyArt.py's masks; recipe GenerateGloamMoodboard.bloom) writes
   `<set>/Gloam/` copies and a `<Set>GloamArtSet` per set. EnemyArtSet gains `_gloam` beside `_frost`, or better a
   `ForTheme(FloorTheme)` lookup; EnemyLookView.UseFrost becomes UseTheme; EncounterController.CreateEnemy passes the
   floor's theme. Per-set value table: the Runner's re-colour reads too pale on the mood board (its body lands near
   Ilya's robe), so the Runner takes a lower body value than the golems.
5. **Props.** Hanging roots, fungi and moss cap through the ice crystal builder's slots (FoundryRegionIceCrystals.cs
   generalised), same cap, same outer-rim rule.
6. **Backdrop.** A painted ENV_Gloam_Garden_v1.png from the concept (GenerateGloamBackdrop.py writes it when promoted),
   with ENV_Ice_Vault_v1.png's import settings; the plate selected by theme.
7. **Check.** The ice capture (IceMapCapture.cs / Run-IceMapProof.ps1) gains a floor-3 column: portraits, crowd,
   enemy lineup, pit warning, region budget. Then a phone pass: hero, Ilya, telegraphs, numbers.

Order: 1-3 first (a playable garden look in one pass, no geometry), then 4, then 5-6.

## 10. Owner questions

1. The concept: is "the foundry abandoned and overgrown at dusk" the Gloam Gardens you want, or a stranger garden
   (giant flowers, no foundry left)?
2. Enemies: re-colour the foundry enemies now (charred bark, orchid bloom), or keep floor 2's frosted or the painted
   ones until the Cinder Bloom, Thornling and Pollen Wraith get their own art?
3. Orchid for enemies: happy with a pink-violet enemy glow on this floor (it is reserved for them), or prefer another?
4. Ilya and the ash: accept keeping ash drifts off the lanes (fewer than snow on floor 2), or give Ilya a darker outline
   on this floor?
5. The sky: the dimmed sun in the backdrop (the Long Dimming visible for the first time), or no sun at all?
6. Should floor 2 -> floor 3 show a transition (frost melting into ash), or a hard cut as today?
