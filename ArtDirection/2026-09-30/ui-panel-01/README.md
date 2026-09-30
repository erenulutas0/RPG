# Forged UI panel import — 2026-09-30

`Assets/_Project/Art/UI/UI_Panel_ForgedBlank_v2.png` is the shared 9-slice frame of the HUD panels, the level-up and
forge modals and the choice cards (17 Sliced `Image`s in `Gameplay.unity`, 128-px borders, pixels-per-unit
multiplier 7, centre filled).

## Why it was 4.5 MB

Not the non-power-of-two problem of the facades: its import was explicitly **uncompressed** (Default platform
`textureCompression: 0`, max 4096, no Android override), so the Android build carried the 2172x724 RGB source as
RGB24, 4.5 MB. The 2026-09-15 integration note records that as temporary: "Default uncompressed imports prioritise
this first device review over shipping size optimisation", with shipping size/compression to be decided after
on-device readability (`ArtDirection/2026-09-15/UNITY_INTEGRATION.md`, `ui-kit-01/README.md`).

The source is also far larger than its use: with the 1080x1920 reference canvas matched to width, a 128-px border is
drawn about 18 screen pixels wide on the S23, so the frame was minified about 7x, without mipmaps.

## What changed

Only `UI_Panel_ForgedBlank_v2.png.meta` (same GUID, sprite rect and borders untouched):

- Android: override on, max size 1024, ASTC 4x4 (the format and size `ENV_Platform_Coping.png`, the same 2172x724
  shape, already ships with).
- Default: max size 4096 -> 1024 (still uncompressed), so the Editor and these captures show the same resize.

Unity keeps the sprite's borders and pixels-per-unit consistent with the resized texture, so every panel keeps its
size and slice positions (verified in the captures: corner and edge positions are identical). At 1024 the frame is
minified about 3x instead of 7x and the centre is sampled about 1:1.

## Evidence

- Actual Unity captures, same code except the meta: `before/` and `after/` (`hud-choice.png` is a level-up choice over
  the HUD, added to `Tools/ArtReview/FoundryRuntimeCapture.cs` for this review; `start.png`, `point-2.png` the HUD in
  play). Pixel differences are confined to the panel areas; `crops-2x.png` shows corners and edges at 2x.
- Build report: the panel is **344.6 KB** (1024x341 ASTC 4x4); build textures 18.1 MB -> 13.9 MB.
- Full gate: EditMode 405/405, PlayMode 130/130, Android build exit 0; integrity 520 / 1,099 / 568 passed.
- APK 39,339,430 bytes, SHA-256 `CB66934314CD7CC7079E857236F752525A2CC28BE2F9D72ABA0118C357E339CE`, **built, not
  installed** (the phone was locked with the owner's game process open).

## Limits

- The Editor renders the resized texture uncompressed; ASTC 4x4 itself is only seen on the phone. Check the gold frame
  lines on the S23 after installation.
- No mipmaps, as before: at ~3x minification the thin gold lines can still shimmer slightly on a moving panel.
