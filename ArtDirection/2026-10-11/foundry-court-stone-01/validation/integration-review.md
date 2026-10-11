# Independent integration source review

Reviewed frozen runtime changes in FoundryRegionFinish.cs, FoundryRegionGateEnclosure.cs and FoundryRegionView.cs, plus FoundryBridgePavingTests.cs and FoundryMasonrySurfaceTests.cs. This is a source review; root-owned compile, Unity tests, graphics, APK and device results are pending at the time of this record. No reviewer ran Unity or edited Assets.

## Bridge coordinates and clipping

No blocking source issue found. Deck coordinates use s=u+v and d=u-v. The new four-corner construction inverse-maps exactly to (u,v), (u,v+g), (u+width,v+g), (u+width,v), so each varying-width stone is an ordinary quarry-axis rectangle. Advancing u by width lays adjacent flags without lattice gaps or overlaps; alternating rows start half a unit apart. Existing broad loop bounds retain margin around the physical deck extent. The original s/d rectangle clipping and BridgeCourtClip still own the exact lane and bridge/court seam; the old margin lattice is retained.

The new ActualDeck test exercises the production builder, checks finite colour, indices, exact physical inequalities, world-UV/projected surface agreement and dense walking-deck coverage. Coverage includes the existing mortar bed, so it proves a complete presentation surface rather than every individual new flag face. Actual image review is still necessary to judge the visual course pattern.

## Gate dressed rims

No blocking source issue found. GateStoneEdges copies the old convex four-corner envelope, replaces its indexed interior with an inset bilinear face and emits four non-overlapping boundary bands. The original corners remain in those bands; no position leaves the old envelope. Relative inset is capped at .12 on each axis, so normal authored faces retain positive interior dimensions. UV and light samples are interpolated from the same old corners; directional rim multipliers preserve opaque alpha. Each invocation adds four quads:16 vertices and24 indices, with no texture, material, renderer or updater allocation.

DressedTier saves originalEnd before appending new faces, preventing later roof UV rewriting from accidentally including the new stone bands. New fitted fascia blocks remain inside old corners, while the existing underlying fascia acts as a darker joint bed. This is a continuation of the old layered masonry presentation. Exact screen depth ordering/shimmer and resulting gate prominence need actual graphics/device acceptance; the helper alone cannot prove those.

Two skewed/tapered production-helper tests check consistent winding, exact silhouette retention, area partition without duplication, coverage, finite attributes and continuous bilinear grain/light. They intentionally test geometry contracts rather than freeze decorative vertex counts.

## Court and gate family

The court gets larger unequal flag candidate weights and broad correlated quarry fields, with stronger restrained joint/bevel visibility. Existing area cap, forced brass lines and ceremonial fixed seal remain. Gate zone2 still chooses crosswise AshlarFamily(true), retaining its old candidate weights; emitted zone2 tone/joint treatment excludes the new court branch.

Caveat: retained gate family does not prove byte-identical gate layout. Fill(0) precedes Fill(2), and shared boundary joint scores can propagate court layout differences into gate packing choices. Documentation should say gate family/treatment retained, not unchanged gate mesh or unchanged layout. Existing packing tests and final matched images provide integration evidence.

## Pending acceptance

Root must append actual test/capture results and measured region counts. No frame-time, phone texture observation, combat clearance, deployment or final reference-completion claim follows from this source review. Approved reference remains a concept image. Reference comparison and portrait circulation must be judged from the produced scene evidence.

## Final root integration evidence

425 .NET / 470 EditMode / 159 PlayMode = 1,054 distinct suite cases, zero failures/skips. Compile zero warnings/errors; actual Android-target graphics capture 1/1. PASS: 589 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0, no C# or shader diagnostics. APK 36,223,974 bytes; SHA256 E559FD12666CCB1599648266CE3A02B23C707585C2A8268AD80495EB3F06A5C9. NOT INSTALLED.

Root and independent seven-pair actual Unity review accept reduced bridge checkerboard, clearer court bond and restrained gate edges. Actor/feet/ability guide and pipe contacts read correctly in inspected views. Whole-map gain is modest; full reference material/structural richness remains incomplete. Region32renderers/135688vertices versus129812baseline (+5876/about4.5%); distant2496 unchanged. These are resource counts, not device performance evidence. No ADB or fresh phone observation; earlier concrete install/restart decision remains pending. Before future deployment inspect fresh state and preserve current original saves/telemetry; never restore historical snapshots. Physical-device circulation/crowd/HUD/motion/shimmer/midrange acceptance and region gameplay/boss progression remain open.
