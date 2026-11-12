# 49 - Ice Map Art Direction (Floor 2)

Status: v1 proposal, 2026-11-12. Owner asked: "start the visual design of the 2nd map; make it ice-themed".
Mood board: `ArtDirection/2026-11-12/ice-map-01/concept/moodboard.png` (untracked, by convention), made by `Tools/ArtReview/GenerateIceMoodboard.py`.
First textures: `Assets/_Project/Art/Environment/Ice/IcePavingV1.png`, `IceMasonryV1.png`, made by `Tools/ArtReview/GenerateIceTextures.py`.

## 1. Concept and lore fit

docs/44 names Floor 2 the Quicksilver Vaults: liquid mirror, copies, narrow sight lines, boss The Glass Regent, enemies Mirror Sentry, Silver Wisp and Faceless Clerk.

Proposal: **the vault's quicksilver has frozen into mirror-ice.** The foundry's heat stops at the stair; below it the quicksilver pools set into polished, reflective ice. The architecture is the same masonry and iron as the foundry (same builders, same procedural layout), now cold, rimed and glazed. The mirror theme survives: frozen panels reflect, the Glass Regent sits in a hall of ice mirrors, Mirror Sentries are silver-and-ice, Silver Wisps are cold motes. The lava of floor 1 becomes a cold quicksilver-cyan glow in the cracks.

Name question (owner): keep "Quicksilver Vaults" (ice is the vault's state, lore unchanged), or rename, e.g. "The Frozen Vaults" / "Rime Vaults". Recommendation: keep the name, add the subtitle "frozen".

## 2. Palette

| Role | Name | Hex |
|---|---|---|
| Ground | Floor slate deep / slate / light | `#1C2129` / `#3B4350` / `#67717F` |
| Walls | Wall stone / wall cap, rime | `#2A3140` / `#8C9AAB` |
| Iron | Black iron (rimed) | `#1E242C` |
| Ice | Mirror-ice deep / mirror-ice / ice edge | `#1F4E66` / `#5FA8C8` / `#BFE6F2` |
| Frost, snow | Frost / snow | `#B9CCDA` / `#E4ECF2` |
| Accent replacing brass | Silver | `#A9B4BF` |
| Accent replacing lava | Quicksilver glow | `#7FE3FF` |
| Light | Frost-light (braziers, key light) / violet shade (shadow tint) | `#A6F0FF` / `#4A3E6E` |
| Backdrop | Sky / aurora teal / aurora violet | `#0D1424` / `#3FD9B0` / `#8B6CE8` |
| Atmosphere | Haze | `#6E8299` |
| Telegraph (unchanged) | Warning red-orange | `#FF5A3C` |

Floor values sit in the 10-45% luminance band; snow and frost are accents, never the fight-lane base.

## 3. Materials

Reused with a cold re-tint (same rasters' structure, same UVs and layouts):
- Paving: FoundryPavingV10 -> **IcePavingV1** (done).
- Masonry: FoundryMasonryV4 -> **IceMasonryV1** (done).
- Iron: FoundryIronV2 -> IceIronV1 (re-tint to black iron, rime on top edges) - next.
- Court flagstones, coping, platform floor/wall: same re-tint recipe through the script.
- Embers (FoundryEmbers) -> frost motes: same particle/sprite system, new tint and slower fall.

New:
- Mirror-ice panel (a glazed, mostly dark reflective sheet with an ice-edge rim; fake reflection by a scrolling gradient, no real-time reflection).
- Ice crystal cluster (sprite/low-poly), icicle strip, snow-drift decal, rime decal for iron.
- Frozen quicksilver pool (replaces lava channels): dark mirror-ice with a thin `#7FE3FF` crack glow.
- Backdrop painting (spires + aurora) replacing the purple-and-lava backdrop.
- Baked boilers: re-baked as frozen boilers (iced shell, cold glow in the grille) in phase 2.

## 4. Props

- **Ice crystals**: clusters at wall bases and corners only; tallest at the region edge, low inside.
- **Frozen chains**: the foundry chains, re-tinted, with icicles hanging from the low point.
- **Icicles**: strips under wall caps and gate crowns; never over the lane where they could read as a projectile.
- **Rime on iron**: white-blue top-edge highlight, applied in texture, not as extra meshes.
- **Frozen mirror panels**: on walls and at the boss arena; dark-valued so they do not glare behind enemies.
- **Snow drifts**: against walls and in corners only, never on fight lanes or spawn points; a drift is a bright shape and would hide telegraphs and pickups.

## 5. Lighting and atmosphere

- Key light cool and slightly desaturated; shadows tinted violet (`#4A3E6E`), not black.
- Braziers become **frost-light braziers**: `#A6F0FF` core, same placement as the ember braziers, same (baked/sprite) cost.
- Cracks in the ground glow quicksilver-cyan instead of lava orange.
- **Frost motes / drifting snow**: re-use the ember emitter budget 1:1 (no extra emitters), lower speed, fade on the ground. If the S23 frame time is short, halve the count before anything else.
- Light haze (`#6E8299`) at the far edge only, to separate the backdrop.

## 6. Backdrop

Two flat layers of frozen spires (near `#111722`, far `#2A3850`) under an aurora band fading teal to violet across the width, over a `#0D1424` sky. Painted once as a texture at the current backdrop's size; the aurora may get a slow UV scroll (one material, no extra renderer).

## 7. Readability rules

1. **Hero first.** The Vanguard's blue is saturated but dark (sampled `#234097`). The floor stays desaturated cool grey with mean luminance about 35-45%, and its saturation stays below 0.2 so the hero's hue pops. On the mood board the hero loses value contrast on the light slate and the frost tone: give the Vanguard a thin ice-edge (`#BFE6F2`) rim or a slightly stronger contact shadow on this floor. Owner decision (Q3).
2. **No bright fight lanes.** Frost and snow (`#B9CCDA`+) cover at most ~15% of a lane's area; drifts sit outside lanes.
3. **Enemies.** Mirror Sentry silver and Silver Wisp violet must keep a dark outline; no enemy may be tinted the floor's slate. Faceless Clerk: dark robe with a silver mask, reads by value against frost.
4. **Telegraphs stay warm.** The pit eruption and the slam mark keep their red-orange (`#FF5A3C`) and are the only warm colour on the floor; they must not be re-tinted cold. Check them on IcePavingV1 in the first Unity pass.
5. **Damage numbers** keep white with a dark stroke; never pale blue (lost on frost).
6. **HUD** unchanged; panels keep their dark backing. Cold-coloured HUD accents are allowed, gold stays gold.
7. **Pickups** (gold, XP) stay warm-coloured; they are now the warm accents of the scene.

## 8. Budget

Region today: 38 renderers, ~161k vertices against ceilings of 174,000 total and 3,600 distant. S23 frame time not measured yet.
- Phase 1 changes textures and tints only: **zero** renderer or vertex change. APK grows by the new textures only (two textures, about the size of their foundry sources once ASTC-compressed).
- Phases 2-4 must stay inside ~13k vertices of headroom and the distant ceiling: prefer texture detail (rime, cracks) over geometry, and swap foundry props 1:1 rather than add.
- Particles: the ember budget, not more.
- Measure S23 frame time before phase 2.

## 9. Production plan

1. **Re-tint pass** (now): IcePavingV1, IceMasonryV1 done; iron, flagstones, coping and platform next by the same script; tint table for light/brazier/crack colours; the engineering hook selects the floor-2 set. Validate on device: hero, telegraphs, numbers.
2. **New props**: crystals, icicles, frozen chains, mirror panels, snow-drift decals, frozen boilers; 1:1 swaps.
3. **Backdrop**: spires + aurora painting, optional slow aurora scroll.
4. **Boss arena**: the Glass Regent's hall of ice mirrors; mirror panels around the edge, the telegraph rules above held strictly.

## 10. Owner questions

1. Name: keep "Quicksilver Vaults" with a frozen look, or rename (e.g. "The Frozen Vaults")?
2. Do enemies get ice variants now, or keep their current art until phase 2?
3. Vanguard on ice: accept a thin ice-edge rim / stronger contact shadow on floor 2?
4. How much snow: drifts at walls only (recommended), or falling snow on screen too?
5. Should floor 1 -> floor 2 show a transition (the heat stopping at the stair), or a hard cut?
