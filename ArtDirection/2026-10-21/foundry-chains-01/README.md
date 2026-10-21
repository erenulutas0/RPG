# Foundry abyss chains: heavier and more of them

Verified 2026-10-21. Baseline `fd4de4f3db895cee2efe9ae5914814672399accd`.

The concept hangs its floating platform with massive chains; ours were thin and few. `HangChain` now builds links at
1.4x the authored width (`ChainScale`), and two chains are added: from the west rim's middle bastion (parallel to the one
below it) and from the east south pier (out past the corner bastion's chain without crossing it). Existing anchors,
sockets and the tested south chain contact are unchanged. Longer links need fewer per chain: region 38 renderers /
159,304 vertices (-496).

Verification: 457 .NET / 732 EditMode / 164 PlayMode, zero failures; Verify-Project PASS; APK 36,412,611 bytes, SHA256
66C39BEE7DBD8400EB13A971A12ADA6EDDFE966945C7EA6B22C020578DAA7EA4, NOT INSTALLED. Source freeze
`0cecb8ac1b74f6a27c9cf426aaf191d1b65281d791b3b7d5de3435f54063d661` matched after the final capture and the full suites.
