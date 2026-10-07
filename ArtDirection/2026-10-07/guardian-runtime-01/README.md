# Bridge guardian runtime package â€” 2026-10-07

INSTALLED AND REVIEWED. See device closure below; earlier pending notes are historical.

Original built-in image_gen asset from ../forge-keeper-proof-01/forge-keeper-source.png, copied unchanged to ForgeKeeper.png. Full generation prompt and provenance remain in that source proof. No raster edits. Same orientation on both sides preserves baked lighting; projected camera-facing cards, not 3D statues.

Scope: paired bridge guardians; stepped outboard bases with below-deck bridge consoles; repositioned animated blue banners/brackets; removal of replaced head-brazier illumination; explicit actor-depth and hero-cutaway ownership. Walk/collision, camera, combat, progression and saves unchanged. Existing bridge lanterns and foot beacons retained.

Material decision: one shared guardian texture/material adds exactly one renderer beyond the previous 17. No per-statue material or update loop; two cards, 8 vertices. Android max1024, ASTC6x6, mipmaps, clamp, non-readable. Scene-generated mesh/cutaway material are owned and released; imported assets are borrowed.

The prototype architecture still differs from the painted reference. This package addresses the missing guardian silhouette; it does not claim matching every material, atmosphere or architectural detail.

## Verified so far

- Pure tests390/390; compile0 warnings/errors; actual graphics capture1/1.
- Region18 mesh renderers /79,832vertices (previous17 /80,504). Removal of two brazier heads reduces geometry; existing flame/banner batches remain dynamic. Guardian pair adds no Update work.
- Graphics proof includes walking five existing map waypoints, portrait bridge approach, a reachable left-court point behind the guardian (normal hero cutaway), and right approach. Square capture shows console/rail/banner joins. Source alpha exterior has no visible painted halo in these captures.
- First new portrait attempt chose a point closer to the rim than HeroMotion.EdgeMargin and stopped. Corrected the staged capture points and now assert the same margin before placing the hero; gameplay boundary unchanged. Final graphics run passed.
- Full EditMode410/410 and PlayMode145/145 passed. Android build exit0; integrity544/1099/568. APK93380BACB4BFAE6BB17BFCF1FB1814E153E9524DEFAA8071F0C83959E5C49470; 31,113,605bytes. Android keeper512x1024 ASTC6x6 /11mips. Device review pending because USB disappeared. No phone changes or new performance claim. Read-only backup TestResults/guardian-device-20261007-205856; refresh against live state before later installation.

The right guardian is outside the bridge walking strip; there is no reachable near-ground point directly behind it equivalent to the left court. Do not claim a symmetric two-sided in-game occlusion walkthrough. Both cards share tested depth/fade flags; actual left overlap and both visible approaches are captured.


## 2026-10-07 - Guardian package installed and reviewed

USB returned on owner continuation. Fresh backup TestResults/guardian-device-20261007-212948. Installed ready APK93380BACB4BFAE6BB17BFCF1FB1814E153E9524DEFAA8071F0C83959E5C49470, base.apk hash matched. First guarded launch stopped because NotificationShade remained foreground; temporary flags cleaned in finally. Collapsed the panel without screen capture/input to other apps, relaunched game and verified normal Staff combat. Then staged guarded bridge walk verified keeper, stone console and relocated banner on S23. Short bridge windows t32-52:59.9fps,p50/p95=16.7ms,max16.8-16.9ms,GPU3.7ms; not dense combat or a midrange benchmark. No managed exception/crash pattern in sampled staged-process log.

Profile,bak and all3 original development files SHA256-identical after review. Temporary depth-proof/render-stats removed; original arrival/region settings retained. App stopped. Proof device-bridge.png and device-normal.png in ArtDirection/2026-10-07/guardian-runtime-01. Supersedes pending USB/install status. No additional build for this installation; prior full410Edit/145Play verified package used. No push.
