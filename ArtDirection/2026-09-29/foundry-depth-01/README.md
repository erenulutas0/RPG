# Foundry material depth — 2026-09-29

This restrained lighting pass changes vertex RGB only. Boiler/gate stone tiers receive source-local furnace/brazier bounce with vertical falloff. Upper tiers retain cool ambient; imported windows and iron/site callers are unchanged. Undercroft arches/piers/anchor plates darken smoothly with depth below their own deck line. Alpha stays unchanged: there is no transparency fade, new mesh, texture, material, realtime light or per-frame work.

Actual five-position Unity capture passed. Root inspected furnace and undercroft details; independent undercroft visual/code review found no blocker. Brick detail/edges and arch gaps remain readable. Static census unchanged: 17 renderer groups / 49,396 vertices; existing atmosphere separately 1 / 780. Before is the installed landmark package, after is the new Unity capture. This incremental tonal pass does not claim reference-image visual parity.

Compile check passed 0 warnings/errors. Integrity: 514 project GUIDs, 1099 package GUIDs, 568 scene objects/components. Full Unity gate passed: EditMode 405/405, PlayMode 130/130, Android exit 0. APK SHA256 374F0327B734E3DC2009E5B7F047370CA44AFC9392D6AA92E58092BB8DDA65A3, actual 39,340,606 bytes, built 2026-09-29 15:01:43 local. READY, NOT INSTALLED. No adb/device action in this turn. Last installed APK remains 4576CC04844ABB7F733AB1F354F8CBFD82A9962A3CAB386CE8EADCCE93B38994 until a separate confirmed installation record. No gameplay/collision/scene/camera edits, save edits, commit or push.

