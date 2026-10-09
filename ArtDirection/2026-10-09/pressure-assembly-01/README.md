# Pressure assembly — 2026-10-09

Actual Unity source-state comparison of the two pressure reservoirs, service mains and high boiler feeds. Before and after were captured from their respective source snapshots with matched cameras; the old appearance was not reconstructed inside the new scene.

The reservoirs use a shared Blender-authored bake. Formed pipe sleeves and broader continuous iron grain replace the narrow repeated bands; original centres, sockets, foundations and gameplay remain unchanged. See [index.html](index.html) for the north/south details, overview and four portrait approaches.

| Runtime region | Before | After |
| --- | ---: | ---: |
| Mesh renderers | 21 | 24 |
| Vertices | 99,492 | 99,444 |

The reservoir resource source is 1024 square. Its independently verified runtime import is **512 square, ASTC 6x6, 10 mip levels, non-readable**. The texture line in the inherited `runtime-budget.txt` describes `FoundryShortBoiler` (1024 / 11 mips), not the reservoir; do not use it as reservoir-memory evidence. Renderer/vertex totals refer to the whole region, not just the new parts.

The before/after captures are Editor graphics, with HUD hidden and time/combat frozen. Four heroes were staged at validated walkable design points (16,1), (20,1), (24,1), (28,1), using production portrait framing. These are not phone footage or movement/performance evidence. Broad closeups intentionally use a separate review camera.

Both graphical capture passes completed; their XML is retained. The combined final gate passed **395 .NET, 415 EditMode and 151 PlayMode tests**, including the capture-relative arrival timing regression. Android build exit 0; static verification resolved 561 asset/folder GUIDs, 1,099 package GUIDs and 568 scene objects/components.

Installed APK: 31,209,737 bytes, SHA-256 `871C3772368CFBC082F537CC39DBC9862C449BAD686BAFB8CAEB425892C9E4F7`; device installation hash matched. Guarded S23 checks covered the near pressure vessel/pipe connection, east gate approach and pit wall. The HUD crops the gate header and some distant upper equipment; their complete form is covered by Editor graphics, not claimed fully visible on the phone. See [selected device captures and logs](index.html#device).

The separate normal Staff run reached Room 1, 90 HP and 4 unbanked gold. The retained normal-combat image is a post-kill reward view with damage 7, XP/gold and a chest, not an active enemy-attack frame. Profile, backup and all three original development flags remained SHA-256-identical after the normal test; no save restoration.

Late quiet windows of the first short pass measured 59.9 fps, maximum 17 ms, GPU averages 4.2–4.3 ms; its initial window separately contained 229 ms. This is not a matched old/new benchmark or sustained/midrange acceptance. The earlier first-ramp hitch and full reference fidelity remain open.

The [keeper-seat gallery](../keeper-seat-01/index.html) records the next chained art slice; its targeted bridge device review is now complete.

The next chained comparison is [foundry-finish-01](../foundry-finish-01/index.html).
