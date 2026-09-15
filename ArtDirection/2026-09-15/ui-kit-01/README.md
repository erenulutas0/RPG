# Cryptforge — UI kit 01

Date: 2026-09-15. Direction approved: A character prominence + B quiet floor + C upgrade hierarchy (docs/19). This package is the first text-free component master set, produced with the built-in image-generation tool. Original outputs retained; copies below are unchanged. No runtime files or current character sprites were edited.

## Files

| Master | Native dimensions | Intended use |
|---|---|---|
| UI_Panel_ForgedBlank_v2.png | 2172x724 | Reusable dark forged-panel frame; cards and grouped HUD |
| UI_Icon_TemperedEdge_v1.png | 1254x1254 | Damage upgrade icon, blue sword with sharpening spark |
| UI_Icon_QuickenedGrip_v1.png | 1254x1254 | Attack-speed icon, gauntlet with two speed strokes |
| UI_Icon_ForgeBurst_v1.png | 1254x1254 | Active ability emblem; bezel/cooldown rendered independently |

All PNGs are opaque RGB, not transparent cutouts. Their near-navy backgrounds differ slightly. Use them in deliberate inset icon wells; do not chroma-key dark pixels, which would damage outlines. A later transparent/clean-grid export can replace the image in the same slot. These large originals are source masters, not shipping texture sizes or the world's 32-PPU sprite assets.

Open index.html for an isolated component preview: generated masters with real HTML text, nine-sliced frame, icon size comparisons and ready/cooldown/unavailable treatments. English/Turkish switches exercise text lengths. CSS states and example strings are design fixtures only, not Unity implementation or finalized localization. The preview's two viewport widths help evaluate wrapping; they do not validate Android safe areas.

## Design decisions made in this round

- No words, percentages or cooldown numbers baked into PNGs. Keep legacy Text above art in Unity.
- A blank panel is shared by cards and grouped HUD. Generated v1 contained distracting diagonal seams; v2 removed them. Residual colour variation means the preview draws only its sliced frame and supplies a separate solid centre.
- Attack speed uses the same gauntlet motif on the choice card and compact buff HUD. Do not import the boot from the first gameplay concept.
- Forge Burst's emblem contains no baked progress ring. Ready/cooldown/unavailable are separate presentation layers driven by the existing ability state. Do not regenerate an entire button every tick.
- No currency, rarity, reroll, extra upgrade or new skill is introduced.

## Suggested first import experiment

Wait for the current telemetry worker to finish its scene/build/device session before running another Unity build or phone automation. One integration writer owns Gameplay.unity, RunHud and RunChoiceView. This package does not require that worker to modify its active task.

Begin with the two choice icons and shared frame, retaining current flow and sprites. Use Image/Sliced for the panel border with a separate opaque centre, and Image/Simple with preserveAspect for icons. The preview uses 128 source-pixel slices on all four sides of the 2172x724 master, displayed at 18 CSS pixels. This is a visual starting point: verify corners and top/bottom lines in Unity at target size. If the image is resized during import, scale all border coordinates consistently; native source coordinates cannot be reused on a smaller copy.

Icon target display: around 64–96 UI pixels in cards, with a smaller compact badge variant checked at 32–48. The master is not an exact 20x20 AbilityArt replacement. First compare point and bilinear sampling at actual display size; current world-sprite point filtering/32PPU remain unchanged. Do not change the font system, renderer or packages. Determine shipping size/compression after on-device readability, not by blindly retaining the 1254px source.

Panel Text is a separate child; allow descriptions to wrap. Both whole cards receive input with identical priority; decorative children should not intercept touches. Preserve the modal pause behaviour, event ordering and exactly-once selection. Hide/occlude the previous HUD while a choice is open and restore it afterwards. The browser demo merely updates its own local selection note.

## Validation performed and pending

Performed: inspected all generated outputs; checked no baked words/numbers or skull motifs; confirmed four native dimensions and RGB format; verified saved copies by SHA-256. Inspected the initial panel and generated the quieter v2. Exact prompts, input roles and output provenance are in PROMPTS.md.

Browser check: opened the local preview in Codex's in-app browser; inspected the initial English layout, switched the preview to 360 CSS px and Turkish, selected Quickened Grip, and confirmed the selection highlight and translated note. The two card names/descriptions fit visibly at that width. Inspected the grouped lower panel with cooldown numeral/ring and the muted unavailable emblem; returned to ready. This verifies the browser composition only, not Unity behaviour. The preview tab is retained as a deliverable. The local server serves only ArtDirection/2026-09-15 on 127.0.0.1:8341; index.html also works directly without that server.

Not yet verified: Unity import/slicing, final texture memory, exact palette/grid cleanup, screen-space contrast thresholds, touch interaction, cooldown truth, Android safe area and installed device result. No APK installed or phone input sent in this component round. No Unity/.NET tests run because there is no runtime change. Do not describe the mockups or browser preview as the installed game.

| File | SHA-256 |
|---|---|
| UI_Panel_ForgedBlank_v2.png | 811087029C8F895F0174468CCD595822CB2D8B2393AF5F2C940311682969DFEB |
| UI_Icon_TemperedEdge_v1.png | 08E3DCF447162278E0F3CAA4C7CE1C3503B268F8702C8E3808C1462BC355ED59 |
| UI_Icon_QuickenedGrip_v1.png | 834E7FEC04B345F761B0E7ECB03503CE69ECF2DC58CCF4C1A6DAF6F88D79F42D |
| UI_Icon_ForgeBurst_v1.png | A8FBCC3E569E0A775CF13DC108FC047D457089E3007AA7656156A5ABA823ED0C |
