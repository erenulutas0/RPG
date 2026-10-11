# Pit construction source handoff

Baseline: `66e68d3`. Compared the approved `foundry-layout-01/foundry-v1.png`, the service-shafts whole-map capture, and this package's fresh `before/shaft-detail.png` before authoring.

The reference pit has a substantial supported rim and spaced deep bearing ribs. The existing pit already has a correct glowing bottom, two vessel silhouettes and a seated rail, but its lining reads mostly as a flat repeated texture. The options were more exposed iron trim, different vessel heads, or making the lining itself carry the coping. The last option gives a coherent structural change without crowding the court or replacing the recent vessel work.

## Changed

Only pit methods in `FoundryRegionMassing.cs` changed. The first two flat lining rows (depth 0 to 0.55) are replaced by one continuous chamfered bearing course. It starts at the exact original deck/rail edge and returns to the exact retained wall station at depth 0.55. Its greatest inward projection is 0.22; no vertex rises above deck level.

Four deep ribs have broad shoulders, side returns, continuous bodies and tapering feet. They follow existing atlas strap boundaries at stations 3, 9, 12 and 18. Boundaries 6 and 15 were deliberately omitted because they conflict with existing furnace-bay frames. The four ribs leave all three furnace opening columns clear. Maximum inward relief is 0.28; their feet return to the original lining at depth 4.90, above the original 5.13 molten seam start. Continuous vertical atlas coordinates prevent the shoulder/body segments from repeating the full strap texture.

`PitLiningFronts` still emits its original lower wall, molten seam and heat wash. It then emits the new bearing course and ribs. `PitFurnaceBays` and the vessel bodies still draw afterward, preserving their foreground silhouettes. `FoundryRegionPitHoods.cs` is unchanged. No pool, fire level, glow color, collar footprint, rail/post, actor depth owner, collision/navigation, material, texture, batch or updater changed.

The source count predicts lining 924 -> 1,264 vertices, a net **340** increase within the permitted growth cap of 1,200 additional vertices. Root's actual capture count is authoritative; this is not a runtime measurement.

## Verification and limits

New `FoundryPitLiningConstructionTests` contributes five cases covering generated relief containment in the original projected far-lining prism, finite mesh data and atlas bounds, exact rail/lower-wall station joins, untouched furnace aperture columns, retained molten-seam stations and the approved growth cap. Existing vessel-envelope tests remain unchanged.

Isolated `UnityCompileCheck` passed with zero warnings/errors after the new runtime and tests. This author did not run Unity or ADB. Root must run the focused/full suites and inspect actual matched shaft, pit-side portrait and full-map captures, particularly the new shoulders beside the original furnace bays and whether the bearing course darkens the upper lining too much. Static geometry cannot establish device motion, shimmer or performance acceptance.


## Later integration outcome

Root completed the actual paired-image review, all final suites and the Android build. See `actual-image-review.md` and `verification.md`; the earlier pending statements above describe the implementation handoff stage. Device acceptance remains pending.
