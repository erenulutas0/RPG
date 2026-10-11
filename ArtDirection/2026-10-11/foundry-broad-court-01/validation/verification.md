# Final verification — broad court

- .NET:421 passed,0 failed/skipped (`combat.trx`).
- Unity EditMode:458 passed,0 failed/skipped (`editmode.xml`).
- Unity PlayMode:158 passed,0 failed/skipped (`playmode.xml`).
- Distinct total:1037. Repeated/focused runs are not added.
- Compile check:0 warnings/errors.
- Static:586 unique asset/folder GUIDs,1099 package GUIDs,568 scene objects/components; references/JSON valid.
- Android build exit0. No C# or shader diagnostics in final suites/build/graphics logs.
- Actual Android-target graphics:1 passed (`../after/graphics.xml`). No additional capture required after test-only query repair; production source unchanged.
- Region:32 renderers;120677->128573 vertices (+7896/about6.54%). Distant2496 unchanged. No new texture/material instance/updater. No phone performance claim.
- Browser: eleven paired views present; before/after/reference and full-map selection verified; all ten page images loaded. Deliverable tab retained.

## Failed gate and targeted repair

First full PlayMode run had156/157 passed and skipped APK after a scene-floor assertion failed. A focused diagnostic reproduced it (2/3 passed). The expected empty southwest corner was correct: a far-away near-degenerate clipped triangle made float barycentrics cancel to(0,1,0). Retained `floor-query-diagnostic.txt` records actual bounds. Test-only bounds rejection plus double orientation corrected the oracle; production geometry was unchanged. Focused4/4 passed, then final full suites/build above passed. After final build an additional exact float32 counterexample assertion was added to that same test (old helper gives(0,0,1) at(-199.7f,75.1f)); final focused4/4 passed (`focused-regression-playmode.xml`). That final change is editor-test-only, excluded from the APK; no runtime/source mismatch. Failed/focused XML retained explicitly, not represented as passes.

## APK

Path:`Builds/Android/Cryptforge-Prototype.apk`

Bytes:36221770

SHA-256:`5DCAD46FFB2CCDA8A16BFCFA03D7AD9354A170748A7A05AF6C55117A9BF1E041`

NOT INSTALLED. No ADB action or fresh device observation. Earlier concrete owner-run restart/install decision remains pending. Historical021b95f and Room2/74HP/8unbankedgold are not current device observations. After authorization inspect fresh, back up current original files, install/hash-check latest APK, inspect accumulated map and preserve current saves/telemetry. Never restore stale snapshots.

Device movement/crowd/midrange/first-ramp/shimmer acceptance, full reference fidelity and encounter-density evaluation remain open. Region encounter/boss progression remains deferred.
