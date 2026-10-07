# Foundry crane structure — 2026-10-07

COMPLETE AND INSTALLED. Final verification and device closure below supersede the progress note.

Code-native visual package: broad cast mast/jib members, fewer alternating braces, restrained upper-left iron rims, stronger end joints and chain, larger open scrap basket with a solid tray, filled mast bearing and winch hubs, plain iron drum and sparse winding bands. Existing authored masonry, anchors, counterweight, generic wheels, gameplay/collision/camera and save behavior preserved. No new textures/materials/renderers/animation. Existing foundry iron batch reused.

The old tiny repeated plate texture on narrow rods made the crane read like a wire drawing. Plain metal sampling and wider members improve its outline at phone scale. This is a local structural-readability improvement, not final painted-reference fidelity. Dense rivets and subpixel details are deliberately avoided.

Proofs are actual Unity scene renders, not an external concept. before/crane.png and after/crane.png use the same square camera. Production portrait separately shows the reachable court and local hero cutaway; the upper jib can extend behind HUD at that close approach. No camera framing changed to fit it. The square shot moves the hero away so the winch remains visible.

Changed code: FoundryRegionDressing.cs and crane-only lines of FoundryRegionSite.cs. Capture harness additionally records a production-scale crane approach then restores the previous hero position. No mirror tests added for visual constants; existing region ownership, budget, depth and traversal tests remain the acceptance gate.

## Validation progress

Pure390/390; compile0warnings/errors; final graphics1passed, including existing five-waypoint traversal and new reachable crane portrait. Final geometry18renderers /79,288vertices (guardian baseline18 /79,832). EditMode410/410 passed; PlayMode/APK/device pending. No new hot-loop work or atlas import.

The earlier keeper build93380BAC was installed and reviewed during this work; see guardian-runtime-01 device closure. Do not treat those device images/performance samples as proof of this newer crane revision.


## 2026-10-07 - Crane structure package installed

Code-native crane silhouette revision in FoundryRegionDressing.cs and crane-only FoundryRegionSite.cs: broader cast mast/jib members with plain iron sampling, sparse alternating braces and end joints, stronger hoist chain, larger open scrap basket/solid lower tray, filled mast bearing and winch hubs, plain drum with sparse winding bands. Existing anchors, footing, generic wheels and gameplay/collision/camera remain unchanged. No new texture/material/renderer/update loop. Region18renderers /79,288vertices, down544 from guardian baseline79,832; existing flame/banner batches remain dynamic.

Actual same-camera Unity before/after and production portrait: ArtDirection/2026-10-07/crane-structure-01. Portrait stages a reachable court point with HeroMotion.EdgeMargin and demonstrates local cutaway; restores hero position before square closeup so the drum remains visible. At the close production camera the jib extends behind HUD and the outboard basket approaches the left edge; no camera change was made to fit the complete crane. This improves local structure/readability, not full painted-reference fidelity.

Verified: .NET390/390, compile0warnings/errors, final graphics1passed including map traversal, EditMode410/410, PlayMode145/145, Android exit0, integrity544/1099/568. No new tests mirroring visual constants; existing region depth/ownership/budget guards pass. Installed S23 APK24BCDD64BD851636A0DCB4948ADAC7FCBACE2D19753A32A04497B3F56EC4E4AD (35,958,239bytes); base.apk SHA256 matched.

Fresh TestResults/crane-device-20261007-215052 backup. Guarded staged route reached bridge, then pit corner and west court beside crane; changed routing to go around the shaft, no off-map teleport/camera change. Phone court/edge captures show main members, bearing, drum and partial outboard basket; full basket verified in square Unity capture. Final staged windows t332-347:59.9fps,p50/p95=16.7ms,max16.7-16.9ms,GPU2.3-2.4ms. Not dense combat, midrange hardware or an isolated performance comparison. Sampled staged/normal process logs had no managed exception/crash pattern. After removing depth-proof/render-stats, normal Staff battle reopened and active combat was captured. Background flush then app stopped.

Profile,bak and all3 original development files SHA256-identical after review; no restoration, loadout change or permanent test flags. Existing region-preview/gate-arrival/start-floor.txt.off retained; test telemetry retained. Earlier guardian package93380BAC was also installed/verified this turn before the crane revision; this24BC package is now latest. No push. Next art work should address another visible material/silhouette limitation with a real comparison, not claim the concept is finished.
