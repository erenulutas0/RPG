# Foundry environment completion pass

## Requested outcome

The owner requested the remaining crane, connecting pipework, shaft dressing and architectural finish together in one playable update, rather than separate asset approval rounds.

Implemented in the actual foundry development map:

- Braced salvage crane with twin mast legs, lattice jib, tension member, pulleys, counterweight, chain and hanging basket.
- Paired furnace service pipes, collars, support saddles, handwheel valves and boiler elbow drops.
- Dark molten basin, heated inner shaft lining, hanging rails/chains and low inner-lip guardrails.
- Shared forged-iron roof panels, warm entrance/shaft/gate beacons and exterior suspension chains.
- Existing broad court, south bridge, shaft and gate composition preserved. Ordinary hall, movement boundary, encounter logic, profile and economy unchanged.

These are static scenery. No crane interaction, damaging lava, climbable heights, new collision or boss transport is implied. The reference remains art direction rather than a claim of pixel-identical final art. This pass completes the named dressing package; animation, broader art polish and other gameplay work are separate.

## Sources

Built-in image_gen used for two opaque material masters, furnace-v1.png as style/palette reference. PROMPTS.md contains the exact prompts. Outputs copied unchanged, without raster editing:

- iron-v1.png: 1254x1254 RGB; SHA256 50061D5A7707E31060DD463298D2E5AC029DF9EDCF87E71CAC971B3568F4A7A0.
- embers-v1.png: 1254x1254 RGB; SHA256 827CC25B57A1B451F84D03C788EF8BF8A1E358E8652A097487F20E69E510EDC5.

Original outputs remain under the Codex generated_images directory as exec-44a0b40c-56b9-4966-9bc5-2c51c785b217.png and exec-e23c2c59-7a62-45e5-9c12-f67106ad27af.png. Project-bound sources are the files beside this README.

Import-FoundryDressing.ps1 temporarily stages BuildFoundryDressingAssets.cs in the pinned Editor. Assets/_Project/Art/Props/Foundry contains byte-identical sources and shared materials. Imports: Default/sRGB/non-readable/mipmapped/Trilinear/Clamp/max1024/Android ASTC6. UV selection for beacon heat is performed on the mesh; source pixels remain unchanged.

## Runtime and review

FoundryRegionDressing.cs is a partial view containing scenery construction, using FoundryRegionView's existing batch and mesh ownership. All parts build once; no new Update, runtime texture generation, particles or lights. Shared imported materials survive scene reload. The static renderer ceiling is16 for the complete map. Internal scenery ranks leave cavern(-19), floor(-15), contact shadows(-13) and actors(1) in their existing order; inset walls and roof details receive distinct ranks.

Actual Unity proof loads Gameplay with an isolated test profile and disabled combat for repeatable five-position traversal. Overview and detail images use review-only camera framing, not a gameplay camera change. Graphics proof also records static-budget.txt. Final full-suite/APK/device results are recorded below and in docs22/23.


## 2026-09-23 - Combined foundry dressing verified

Final runtime art: crane/counterweight/chain/basket, connecting pipes/collars/valves, heated shaft lining/basin/guardrails, iron roofs, beacons and hanging chains. No gameplay or navigation edits. Compile0 warnings/errors, static integrity505 GUIDs/568 scene objects, actual five-position Unity traversal1 passed. Close inspection corrected disconnected crane jib and stretched shaft lining. Final static budget15 renderers/16,896 vertices. Full EditMode403/403 and PlayMode131/131 passed, including shared imported material usage/non-readable textures, scenery ordering, <=16 static renderer ceiling and reload ownership.

Android build exit0. APK29,253,465bytes, SHA256 7EE642D10E16A21588E40318D70CCC2F03C3ADFB61FE6CD27BA43851855E42EB. Pure combat suite not repeated for this visual-only package. Sources/prompts/captures in ArtDirection/2026-09-23/foundry-complete-01.


## 2026-09-23 - Combined foundry dressing installed

Under owner authorization to complete and integrate the entire package, fresh inspection showed the assistant's previous paused test run unchanged (42HP/15runGold/room3). Update restarted that unbanked test run; no user run was progressed or banked. Fresh backup TestResults/complete-install-20260923-223213; stored profile199/28507gold/defaultSword/SecondWind/deepest3. Install-r Success, installed base.apk SHA256 7EE642D10E16A21588E40318D70CCC2F03C3ADFB61FE6CD27BA43851855E42EB matched local. All10 files byte-identical immediately after install.

Normal authored run with retained foundry flag. Guarded10-second west drag and12-second northwest drag, with pauses between, reached shaft then crane. Actual S23 captures show heated shaft panels/inner rail/slag basin and complete crane with basket. Movement, combat and camera follow worked. No upgrade/loadout selections in this run. Handed back PAUSED at crane, Cinder Walk room2/6,wave3/3,64/100HP,level0,XP1/10,0runGold. Profile and backup byte-identical after review; telemetry grew normally, no save edit/restore. Current-process log0 matches for FATAL EXCEPTION/NullReferenceException/MissingReferenceException/InvalidOperationException.

Active sampled windows after startup:59.9fps,max16.9-17.1ms; short S23 smoke only. First startup window contains2147.2ms maximum, not excluded from evidence or claimed resolved; paused windows are not gameplay benchmarks. No midrange-device measurement. All five regions traversed in Unity, only shaft/crane physically reviewed this turn. Screenshots foundry-complete-01/device. Supersedes prior0CC7 installed build. No commit/push.
