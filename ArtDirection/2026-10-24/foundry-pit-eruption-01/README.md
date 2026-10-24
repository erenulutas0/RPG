# Foundry pit eruptions

Verified 2026-10-24. Baseline `6e30a74`. Gameplay: the furnace pit as a hazard (owner's "devam", after the altars).

While a wave stands on the foundry (not in a swarm room, not while a choice is open or the boss reveal holds), every
`PitEruptionRule.Period` 10 seconds the pit spills: for the last `Warning` 1.5 seconds its danger band - the line
`Reach` 2 design units beyond the hole's edge, a rounded square on the floor - glows in, pulsing faster; on the spill it
flashes, and the hero (if inside the band) loses `HeroShare` 10% of its maximum health while every standing enemy inside
it loses `EnemyShare` 40% of its own. The clock starts over between fights. So a hero who draws a pack to the rim and
steps out of the band in time uses the pit as a weapon; one who fights on the rim pays.

- `Combat/PitEruptionRule.cs` (pure: band, `PitEruptionClock`), `Core/PitEruptionService.cs` (scene, foundry only),
  `UI/PitEruptionView.cs` (the band line), `CombatSetup.Pit`. Scene-only: the Descent simulation is unchanged; the
  hall region has no pit hazard.

Tests: EditMode `PitEruptionRuleTests` (band extent with rounded corners, walkable floor inside the band, the clock's
warning-then-spill cycle, enemy share well above the hero's); PlayMode `PitEruptionFlowTests` (the clock runs during a
fight, the band glows in the warning, the hero in the band loses exactly 10% at the spill, out of it nothing).

Images (actual scene, portrait game camera, `Tools/ArtReview/FoundryPitEruptionCapture.cs`): `pit-warning`, `pit-spill`
(hero and grunt flash as both are burnt), `strip.png`.

Verification: 457 .NET / 744 EditMode / 169 PlayMode, zero failures; Verify-Project PASS; APK 35,889,848 bytes, SHA256
D69AF2811E7EA4B0CC7311DBD8374E78C4B95ED74E1F2EA1AEA55DA84560F8BB, NOT INSTALLED. Source freeze
`2db7d58798c8733b5bdae01016802fd1729d3dbe03f1f0e0217700c8a45c59e0` (1,297 files) before and after the full suites. The
period, band and shares are chosen, not measured; device play decides them.
