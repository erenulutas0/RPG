# Combat number readability — 2026-10-07

The code-native damage/amount numerals in EffectArt use distinct 5×7 glyphs instead of 3×5. One-texel gaps and outlines, white normal hits, gold 2× critical hits, five-digit saturation and the existing number pool/layout are retained. This also affects shared heal/gold amount readouts. No damage values, lifetime, movement, camera or gameplay changes; no imported texture or new renderer.

An outlined single normal digit is now7×9 texels (previous5×7). A three-digit outlined number is19×9; critical38×18. The sprites are cached at initialization as before; no new per-hit raster generation. Larger number bounds are intentional.

EffectArtTests retains numeric capping/order, unique glyph, transparency/spacing, outline/color and integer-upscale checks with the new design dimensions. FoundryRuntimeCapture explicitly stages 8,128critical,99999critical through the actual private pooled ShowNumber method for the typography proof, then waits for them to expire before the existing traversal. These are specimens, not inflicted combat damage. unity-specimens.png is a real Unity render; no screenshot editing.

Final checks and device result follow below.

## 2026-10-07 - Combat amount typography installed

EffectArt now draws original5x7 numerals instead of3x5. White normal hits, gold2x criticals, one-texel outlines/gaps, five-digit saturation, duration/rise and existing pooled digit renderers are retained. Shared healing/gold amounts also use this font. Glyph canvases are7x9 normal; three-digit outlined row19x9 and critical38x18. Intentional larger visual bounds; no value/balance/gameplay/HUD-text changes and no new renderer or per-hit raster generation. Region remains18renderers /84,160vertices.

EffectArtTests updated for new dimensions with existing uniqueness, numeric order/capping, gap/transparency, outline/color and upscale checks preserved. FoundryRuntimeCapture stages8normal,128critical,99999critical via the actual pooled ShowNumber method, captures then expires them before traversal. Proof ArtDirection/2026-10-07/damage-type-01. Unity specimen values are synthetic typography samples, not damage dealt in that encounter. S23 actual Staff combat captures show normal7/8/11 and fade; critical color/size verified in Unity, not claimed captured on device. No dense-combat or performance benchmark.

Verified .NET390/390, compile0warnings/errors, graphics1passed, Edit410/410, Play145/145, Android0, integrity544/1099/568. Installed APK CA50F2DF7650C8DA07F896B4F53433E49F62E698B7354C257B6A7429E2D43ECB (35,963,012bytes), installed base.apk SHA256matched. Fresh backup TestResults/damage-type-device-20261007-233031. Profile,bak and all3 original development files SHA256-identical. No temporary flags added. Sampled process log had no selected managed exception/crash patterns. Background flush then app stopped, telemetry retained, no restore/push. Supersedes the older3x5 typography specification.