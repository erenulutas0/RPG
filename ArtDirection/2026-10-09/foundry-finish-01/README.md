# Foundry finish — 2026-10-09

Actual Unity source-state comparison after the pressure assembly, focused on formed pit hoods and gate surface refinement. Matched before/after cameras show the respective source snapshots; this is not a reconstructed legacy-prop comparison.

Pit hoods replace segmented masonry shoulders with smooth formed metal, continuous plain-plate grain, restrained crown soot and pool-facing warmth. The existing warm masonry foot, radius 1.1, owner height 1.3, flue anchors, pool, drums and surrounding collar remain. The new hoods read more simply: the old radial straps/rivet ornament is reduced. Reviewed Editor detail and portrait images show no visible near-collar clipping; the separate device check is recorded below.

The gate gain is small, and the production portrait crops part of the structure. Use the full gate detail to judge the change; do not describe this as a new gate silhouette or complete surface overhaul.

| Runtime region | Before | After |
| --- | ---: | ---: |
| Mesh renderers | 24 | 24 |
| Vertices | 99,444 | 100,548 |

No extra renderer or texture is introduced by this finish pass. The inherited `runtime-budget.txt` texture line describes `FoundryShortBoiler`, not a new finish texture; its generic prose also says four portrait points, while this capture actually uses **three**: (-20,6), (-20,-10), (28,6). The reservoir from the preceding package was independently verified at 512 square ASTC 6x6, 10 mip levels, non-readable, from a 1024-square source.

[index.html](index.html) contains matched pit/gate details, overview and portrait approaches. These are Editor graphics with HUD hidden, combat/time frozen and the hero staged at validated walkable points using production portrait framing; closeups use separate review cameras. They are not device screenshots or traversal/performance evidence.

Both graphical capture passes completed; XML is retained. Combined final verification passed **395 .NET, 415 EditMode and 151 PlayMode tests**, including the capture-relative arrival timing regression. Android build exit 0; integrity 561 asset/folder GUIDs / 1,099 package GUIDs / 568 scene objects/components.

Installed/hash-matched S23 APK: 31,209,737 bytes, SHA-256 `871C3772368CFBC082F537CC39DBC9862C449BAD686BAFB8CAEB425892C9E4F7`. [Selected phone captures and logs](../pressure-assembly-01/index.html#device) show the near pressure assembly, east gate approach, pit wall and separate normal Staff post-kill reward view (Room 1, 90 HP, 4 unbanked gold, damage 7, XP/gold/chest). The gate header is cropped by the HUD: this is not phone evidence of its complete surface gain. The far tank's complete upper form is also not claimed visible.

Profile, backup and all three original development flags remained SHA-256-identical after the normal test; no save restoration. Short first-pass late quiet windows: 59.9 fps, maximum 17 ms, GPU averages 4.2–4.3 ms, with a separate initial 229 ms window. No matched old/new or sustained/midrange performance claim; earlier first-ramp hitch remains open.

[Keeper-seat gallery](../keeper-seat-01/index.html): next chained art slice; its targeted bridge device review is now complete. Full reference fidelity remains unfinished.

Preceding package: [pressure-assembly-01](../pressure-assembly-01/index.html).
