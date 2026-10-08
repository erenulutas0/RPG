# Gate processional approach — 2026-10-08

Extends the reference-led foundry map after `north-workshop-paving-01`. Code-native architecture and flush inlay; no generated/edited raster asset.

## Implementation

- A chamfered double border occupies design u15.8..30.6, v1.3..8.7, within the existing gate lane. The outer border is stronger than the inner hairline. Four small ties mark its rhythm.
- A four-point brass medallion at (23.2,5) follows the lane, using the court's shared coping atlas, subdued groove and local floor illumination. Open star facets expose the paving. Longitudinal guides connect it to the ends of the panel. No emission, timer, animation or interaction; it must not read as an ability telegraph.
- Five crosswise dressed stones cover u30.5..32,v0..10 at the threshold, with the existing ashlar shading and continuous paving texture. They replace eight sets of tiny metal brackets. Two thin brass bindings terminate the runner. The former double band at u28 is removed so it does not cut across the panel.
- The court's older straight apron guides stop at u13.5; no redundant parallel line continues alongside the new panel.

Files: FoundryRegionHeraldry.cs, FoundryRegionArchitecture.cs and FoundryRegionView.cs. Existing floor/decal batches are reused. Region18renderers /94,727vertices, versus18 /92,951 (+1,776/about1.9%). No new texture, material, update loop, camera, collision, progression or combat change.

The threshold overlays the existing flush paving in the same material/order batch, emitted after it; no second raised collision surface is implied. Graphics review shows the broad stones and continuous illumination. Full floor ownership/coverage/depth guards remain applicable.

## Comparison and scope

`before` is the preceding package's actual Unity render. `after` is the new render using identical review cameras. Gate approach portraits use the production follow width. Whole-map/detail views show environmental layout; they are not gameplay framing or a painted mockup.

The long processional panel addresses a visible missing reference motif. This does not finish the reference map. Actual walkable gate elevation remains a separate integration: ground projection, actor/body/bar/shadow placement, pooled effects, floor telegraphs, drops/chest, camera and depth/cutaway must agree while deterministic gameplay floor coordinates remain stable. A decorative stair image alone would leave actors sliding through it, so this package deliberately keeps the panel flush.

## Verified before the full build

.NET395 passed; compile zero warnings/errors. Graphics1 passed, including integrated traversal/depth/motion/mesh-size checks. No new test duplicates visual constants. Full suite/device results are recorded below when complete.

## Final build and S23 closure

Full .NET395/Edit415/Play145, graphics1, compile zero warnings/errors, Android exit0; integrity545asset/folderGUIDs /1099packageGUIDs /568sceneobjects. APK C5C78DF97A5090830159C295D262DE935F8540E39C60920740EB8D5E34D6E0F7 (35,981,571 bytes), installed base.apk SHA256 matched.

Fresh backup TestResults/gate-procession-device-20261008-155803. Guarded staged walk from court into the new panel, over the medallion and to the threshold; normal fresh gate entry and Staff combat/loot readback afterward. First startup capture was a loading screen, not accepted as visual evidence; subsequent actual views verified the complete route. `device/gate-seal.png`, `gate-threshold.png` and `normal-combat.png` show the installed game.

Profile.json, backup and all3 original development files SHA256-identical after review. Temporary depth-proof removed; owner region-preview/gate-arrival/start-floor.txt.off retained. Test telemetry retained, app stopped. No selected managed exception/crash patterns in sampled logs. No new FPS, thermal or midrange benchmark; no save restoration or push.
