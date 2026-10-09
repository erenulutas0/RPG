# Bridge keeper seating — 2026-10-09

Actual Unity comparison of a small architectural integration refinement. The two existing keeper statues retain their imported artwork, aspect, orientation, position and cutaway ownership. Only the stone mounting slab beneath each baked octagonal base changes.

## Change

`FoundryRegionKeeperSeat.DressedKeeperSeat` replaces the keeper-only upper `LitFoundryTier` call in `FoundryRegionSite.BridgePiers`. The slab retains the original design bounds +/-0.7 and height0.18..0.45. Deeper front/rear corner cuts shorten the diamond tips; shallow lateral cuts support the wide painted base. A narrow top bevel and continuous stone-interior UVs replace the conspicuous tiled mounting surface. Existing directional tones and `SurfaceTone` remain in use.

Lower pier, below-deck console, statue raster, banners, lanterns, walking union, gameplay, camera and saves are unchanged. No new imported asset, material, renderer or update loop. The same existing stone batch and depth landmark own the geometry.

## Evidence and review

- `before/` and `after/` are actual matching Unity captures, with HUD hidden and the hero staged at three validated walkable design points. These are not device screenshots or ordinary gameplay traversal.
- `approach--15--18.png` shows the right keeper's complete base and mounting slab. Its exact hero point is(-15.85,-18.55); filenames use truncated integers.
- `approach--17--12.png` shows the left keeper from the reachable court side with the existing local hero cutaway active. Exact hero point(-17.3,-12.1). The statue fade is expected, not missing artwork.
- `approach--18--18.png` covers the central bridge approach.
- `bridge-detail.png` is preserved, but its framing crops the keepers at both sides. It is not evidence of a complete paired-statue detail review; use the portrait views above.

Read-only visual acceptance: the slab joins the statue base more convincingly and the exposed diamond corners are reduced. No new floating join, banner collision or actor-readability regression was visible in these comparisons. The gain is modest and local, not a redesigned statue or complete reference-fidelity claim. A recentered single-keeper closeup could provide extra presentation detail, but the two selected portraits already show the changed seating.

## Verification status

The after graphical capture reports1/1passed in `after/graphics.xml`. Runtime budget files report24region renderers for both versions:100,548vertices before and100,572after (+24total). The texture line in those files describes the unchanged `FoundryShortBoiler` resource sampled by the capture helper, not a new keeper texture.

Final combined-package checks passed: .NET395, EditMode415, PlayMode151, Android build exit0 and static integrity561asset/folderGUIDs /1099packageGUIDs /568sceneobjects/components. Installed APK SHA-256: `871C3772368CFBC082F537CC39DBC9862C449BAD686BAFB8CAEB425892C9E4F7`. These are the combined package's results, not a separate keeper-only APK.

## Device acceptance completed

The targeted S23 route reached the actual right bridge keeper and new seating. `device/keeper-bridge.png` shows the changed slab, statue/base contact, nearby banner and hero; no floating join or clipping was seen in review. Its current-process log is retained as `device/keeper-log.txt`. An earlier route did not reach the keeper; its keeper-front/side images are excluded from this proof. This phone image establishes the right approach, not a symmetric left/right rear-occlusion walkthrough. The left cutaway evidence above remains an Editor capture.

A separate normal Staff combat check also passed as part of the combined package. Fresh backup: `TestResults/pressure-finish-device-20261009-210229`. Profile.json, profile.json.bak and all three original development flags remained hash-identical. Temporary diagnostic flags were removed and the app stopped. No save restoration was performed. Prior first-ramp hitch, sustained crowded-combat and midrange-device acceptance remain separate open work; this short route is not a new performance benchmark.

Reproduction tooling: `Tools/ArtReview/KeeperSeatCapture.cs` and `Run-KeeperSeatProof.ps1`. Before and after correspond to the actual respective runtime source states; the current tool does not reconstruct the old seat automatically.
