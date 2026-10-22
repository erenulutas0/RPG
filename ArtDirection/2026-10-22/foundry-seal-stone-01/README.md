# Foundry court: the seal's field and the processional ways in cut stone

Verified 2026-10-22. Baseline `6fc8a7d68f18e5bb027a98fc060ca148f634f94d`.

After the court took the concept's cut stone (`../../2026-10-21/foundry-cut-stone-01`), the seal's inner field (eight
smooth fan flags round an octagonal keystone) and the two processional ways (broad 3.2-long cross-flags) were the last
large smooth slabs in the court's centre. On the concept the seal's field is laid in the same stone and the ways in
smaller stone between their lane lines.

- Seal field (`BuildCourtSealField`): the exact 48-gon inside the ring (radius 3.75) is laid stone in the flagstone batch
  - the light lattice only, the court's world-continuous V10 field. The twelve fitted ring stones (3.75-5.05), the
  brass rings and the compass star are unchanged.
- Processional ways (`BuildCourtProcessionalWay`, replacing `BuildCourtProcessionalStone`): each way's exact contour is
  laid stone in the same batch, its field turned to run with the way (texture u across it, v along it, from the seal's
  centre) at 1.35x the court's density, so its courses lie across the way like a laid road. Lane lines, the bridge
  continuation (fitted, unchanged) and the gate apron's fitted stone are unchanged.

No new batch or texture. Region 38 renderers / 157,941 vertices (baseline 162,336: the fitted slabs' joint strips and
shoulders cost more than the laid lattice; the court floor test reports laid field and ways 1,330 vertices).

Images: `before/`, `after/` (33 matched views; new: `seal-close`, `way-close`). The after-phase floor ownership check
(`after/court-floor-ownership.txt`) now counts the laid stone batch as a floor owner too: every probe has exactly one.

Tests: `FoundryCourtCompassTests` follow the new owners - the seal field is laid stone with the floor-field light and
the court's world UV and covers the whole inner 48-gon; ring + field partition the seal's 48-gon exactly; the ways carry
the floor-field light and the turned field, stay on the walking surface and cover their full width; replacement and
production ownership count fitted and laid stone together (exactly one owner at every probe).

Verification: 457 .NET / 736 EditMode / 164 PlayMode, zero failures; Verify-Project PASS; APK 36,419,732 bytes, SHA256
9924FBD1942383FC7C550BDDE2FB40807FA20B89D8D18ABA734258692DA97F26, NOT INSTALLED (the owner is using the phone). Source
freeze `49332a1268d92e35cea8385502339d406c144565561c68f9bf2ed78a543caec0` (1,265 files) taken after the final After capture,
matched after the full suites. Gameplay: none.
