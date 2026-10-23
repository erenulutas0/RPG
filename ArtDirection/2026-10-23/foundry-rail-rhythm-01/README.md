# Foundry rim: a closer post rhythm

Verified 2026-10-23. Baseline `56be5dc`.

The concept's rim keeps a close, heavy rhythm of posts; ours spaced its regular posts up to three units apart between
the fixed ones (lanterns, finials, corners). `RailPostSpacing` is now 2 (`BuildRailRun`), so the long south-west and west
runs gain posts, each with the brass collar and finial. Placement rules are unchanged (every post still passes
PlaceRailPost's tests); no new batch.

Region 38 renderers / 157,434 vertices (+1,784).

Images: `before/`, `after/` (39 matched views), `compare-concept.png` (south-west rim before / after / concept).
Also tried this session and reverted (not committed): procedural kneeling keepers on the bridge parapets - hidden behind
the bridge lantern posts and unreadable; they need painted card art.

Verification: 457 .NET / 738 EditMode / 164 PlayMode, zero failures; Verify-Project PASS; APK 36,465,043 bytes, SHA256
DD3B3B99BE4560598E0C4457264476F031A8B13E22726CDFF21D1DDE367FF0CF, NOT INSTALLED (the owner is using the phone). Source
freeze `b74105503f5b94b396834ae68df3bab7733f28b182538e5017092ae3b2a3ce33` (1,273 files) taken after the final After capture,
matched after the full suites. Gameplay: none.
