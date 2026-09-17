# Painted platform runtime — 2026-09-18

The selected quieter V2 basalt floor, brass coping and near masonry now render in the actual Gameplay scene. Arena geometry, movement, encounters, stats, camera and HUD behavior are unchanged. This is the platform material integration, not a new boss room or completion of the entire environment.

## What changed

- `PlatformMaterialSet` holds three imported asset-owned materials/textures. `ArenaView` receives one serialized reference; that is the only scene change.
- `ForgePlatformView` borrows nine meshes: one floor, four coping wedges, two near walls and two narrow foundation lips. Exact shared outer/inner vertices create closed miter joins. The outer diamond is exactly the existing boundary; the floor inside the coping is scaled .95. No collider or walkable limit changes.
- `PlatformArt.DrawFoundation/DrawFoundationLights` retains the lower keel, chains and crystal while omitting the old top, faces, towers and flames. The old pixel outline can no longer protrude through the new coping. The lower keel itself still needs matching painted art; a dark .16-unit lip covers its join to the new wall.
- Foundation sprites and material meshes have bounded session ownership in `ArenaSpriteCache`. Scene reloads borrow the same resources. A different geometry gets a private correctly sized set, released with its view. Imported textures/materials are never destroyed by the cache.
- No per-frame material cloning, mesh generation, texture readback or raster painting. The existing crystal flicker remains; the obsolete corner-lantern flicker is absent in the painted presentation. The full procedural platform stays available when no valid material set is supplied.

## Sources and imports

The three PNGs in `Assets/_Project/Art/Environment/Platform/` are unchanged copies of the accepted masters from `ArtDirection/2026-09-17/platform-material-01/`. Original prompts and hashes remain there. Unity performs technical sampling/compression: sRGB, non-readable, mipmaps, trilinear, Repeat, anisotropy2, Android1024 cap, ASTC6x6 for floor/wall and ASTC4x4 for coping. The narrow source uses nearest-power-of-two sampling: its first1024x341 import silently became RGBA32, so this was corrected and Android-format assertions added. This sampling changes texel aspect, not the UV-mapped world proportions.

Surface sorting is -14, below ground effects at-13. Foundation surface/light sprites use -16/-15. Floor repeats2x2, coping2x1 per side, near walls3x1; the lip uses the same wall material and a dark vertex tint. Right masonry receives a restrained cooler/darker tint. See `asset-metadata.json` for measured source/import details.

## Evidence and reproduction

`pwsh -File Tools/ArtReview/Run-PlatformRuntimeProof.ps1 -Reimport` configures the imports/materials and scene reference through a temporary Editor script, then captures the actual scene with a temporary PlayMode fixture. Without `-Reimport`, it captures existing imports. Scripts are removed in `finally`; no phone is touched by this command. Full game verification uses the normal .NET -> compile -> full Unity suites/APK -> integrity gate.

Seven Unity renders cover centre, near/right/far rim, shorter portrait, whole-platform and detail views. The real ten-enemy room advances160 frames with attacks disabled, then freezes; rim diagnostics reposition only the hero/camera. These are not live kiting or performance measurements. The gallery's previous images come from the retained pre-integration capture, using the same recipe. Phone evidence and final test/build counts are recorded in the newest Docs/21–23 entries. Gate: .NET272, clean compile, EditMode284, PlayMode104, graphics1, Android exit0 and integrity415/555. Installed APK SHA-256: 64F49049A08DDB92396831F28FEC24DEFF42F902B8F849E6D87931F59F439475. S23 controlled proof victory10 kills/10 gold; southeast and near-rim movement and normal warm restart checked. Temporary proof flag removed; profile preserved at revision137/10233 gold/Daggers/Second Wind. No backup restoration. The user also played the earlier normal run; it is not controlled evidence. Gallery controls/images/desktop and390x844 layout passed.

Four new PlayMode tests check actual imported texture settings and Android compression, sorting/boundary and exact corner joins, scene reload sharing, alternate-geometry disposal, and session-reset ownership. Existing procedural fallback/cache tests remain.

## Remaining art work

The old lower keel/crystal/chains, chest, ability ring and unconverted enemy types remain visibly different from the painted actors and floor. Dedicated corner props can return once their materials match. Crowded depth overlap remains. No sustained or mid-range-device performance claim follows from this slice. Next art priority is a matching chest/loot prop and ability-floor indicator, followed by the remaining large enemy/boss and lower-foundation treatment; do not change gameplay to conceal presentation issues.
