# Foundry rim main: heavy pipes along the north rail

Verified 2026-10-23. Baseline `6d4ce2c4c578d78dc0aa671f228bed1471c0be0a`.

On the approved concept the boilers send heavy pipes along the court's rim. The runtime boilers had short feeds only.

- `FoundryRegionRimPipes.cs` (`BuildRimPipes`, called from `BuildDressing`): a .66 main at height 1.45 from the
  north-west boiler's drum west over its service landing (above the landing's barrels), round an elbow and on along the
  outside of the court's north rail (v North+1.2, clear of the rail's lantern posts) to u -3.5, banded in brass every
  ~2.2 with broader end collars, carried on iron brackets off the rail band, ending in a blind flange; a .34 return line
  runs beside it, farther out and lower. All of it stands off the walking floor behind the rail, in the masonry batch
  (-11) emitted before the massing's rail, posts and lanterns, which paint over its lower part (the rim clutter's
  clean-patch colour mapping, toned as rolled service iron).
- New `FoundryRimPipeTests`: the main stays off the walking floor along its whole run and clear of the lantern posts.

A tried-and-reverted alternative first (not committed): yard stock stacks as work islands in the workyard. One stalled
enemy pursuit round it, one fell in the mouth of the passage west of the pit, and at map scale they read too small; the
owner chose the rim pipes instead.

No new batch or texture. Region 38 renderers / 155,354 vertices (+2,048).

Images: `before/`, `after/` (39 matched views; new: `north-rim`), `compare-concept.png`.

Verification: 457 .NET / 738 EditMode / 164 PlayMode, zero failures; Verify-Project PASS; APK 36,457,456 bytes, SHA256
02AAE77877E59C1F43A6F5CF77FC27D04ECA46FAFCA42E8D5892B782714C5A0B, NOT INSTALLED (the owner is using the phone). Source
freeze `6e06930a03e34c2ea2e8a5347d3fbb652aca5710ce322a0f8de7366d98a108c6` (1,273 files) taken before the full suites
(the new test file was added after the final After capture; runtime sources unchanged since), matched after them.
Gameplay: none.
