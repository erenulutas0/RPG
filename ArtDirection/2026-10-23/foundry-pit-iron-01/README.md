# Foundry pit vessels in blackened iron with brass straps

Verified 2026-10-23. Baseline `dc1f2ee`.

The concept's pit vessels are dark violet-black iron banded in brass; the runtime hoods read olive-green (PitMetalTone's
warm-neutral ramp on the cast-iron atlas) with pale grey straps. `PitMetalTone` now ramps (.30,.29,.38) -> (.66,.62,.74)
by facing (was (.37,.38,.41) -> (.88,.84,.79)), and the hoods' hold-down straps take the braziers' bronze (x.82). Shape,
heat, soot and every other pit part unchanged; no new batch. Region 38 renderers / 155,354 vertices (unchanged).

Also tried this session and reverted (not committed): an east rim main from the south-west boiler down the east rail -
its elbow crossed the squat boiler's grate on screen and its run was hidden behind the gate's south tower.

Images: `before/`, `after/` (39 matched views), `compare-concept.png` (pit close-up before / after / concept).

Verification: 457 .NET / 738 EditMode / 164 PlayMode, zero failures; Verify-Project PASS; APK 36,457,765 bytes, SHA256
FDDBEAD4B6F97AEC24C273CC637C4A9591B28C74A7F9D8FF9312D66939877A48, NOT INSTALLED (the owner is using the phone). Source
freeze `fc244b8ca7c0387b9fb27ddd415d74f2a6299b9eaf603362c4c5a4398cb71177` (1,273 files) taken after the final After capture,
matched after the full suites. Gameplay: none.
