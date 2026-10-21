# Foundry atmosphere: the concept's violet void

Verified 2026-10-21. Baseline `f682cdda0d8698e2e85d70adf87e6aa27c26dce5`.

Measured against the approved concept at the full-map framing, the void around the platform was ~35-60 % darker
(value .17-.18 vs .24-.29) and greyer, with almost none of the concept's lava light. The painted cavern
(`FoundryCavernV2`) is already a saturated violet/lava painting; `FoundryCavern.shader` had desaturated it to 72 %,
blended 22 % toward a dark indigo, and the renderer tint dimmed it further (.84/.87/.91). Now the shader keeps the
painting's saturation (1.08), blends only 10 % toward a lighter violet (.20/.15/.30), and the tint is ~neutral
(1.04/1/1.06). The distant native works share the shader, so their 0.44 haze now reads violet instead of grey.

Measured void after (concept): top value .24 (.29), saturation .41 (.37); left .21 (.24); right .25 (.24).
No geometry, batch or texture change. Gameplay views keep their contrast: the platform floor stays brighter than the void.

Verification: 457 .NET / 732 EditMode / 164 PlayMode, zero failures; Verify-Project PASS; APK 36,413,268 bytes, SHA256
B7BE24A69A6DA77AE162C5527AEE9191586F9F224608134C3265D98144DF3344, NOT INSTALLED. Source freeze
`0e1c4985e750447946af89c44c18730222f3d06e421be893c2250bb65839459a` matched after the final capture and the full suites.
`validation/comparison.png`: concept / Before / After. Remaining gap: the concept's void also has more lit structures
and lava falls in the foreground below the platform than the painting and distant works provide.
