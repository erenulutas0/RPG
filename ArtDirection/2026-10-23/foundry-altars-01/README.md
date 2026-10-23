# Foundry altars: a power for danger

Verified 2026-10-23. Baseline `4a96d27`. Gameplay, owner's choice (2026-10-23): docs/27 S6d's shrines as altars.

Two altars per floor on the foundry, before the lantern shrines on the court's south rail (-7.5, -15.2) and north rail
(1.5, 15.4) (`FoundryMap.AltarSite`). Standing still on an unused altar for `AltarRule.DwellSeconds` 1 grants a bonus
upgrade (`RunState.GrantBonusUpgrade`, so its offer of cards opens as a level-up's does) and raises the run's difficulty
by `AltarRule.DifficultyCost` 0.1 - half an evolution's 0.2 - on the same stat (`HeroStats` Difficulty), which every
later enemy multiplies its health and damage by. Each altar answers once per floor; a new floor restores both. Moving or
stepping off restarts the hold; nothing counts while a choice is open.

- `Combat/AltarRule.cs` (pure: `AltarRule`, `AltarField`), `Core/AltarService.cs` (scene wiring, foundry only, floor
  reset), `UI/AltarView.cs` (an ember ring, a violet inner ring and a flame glyph on the floor; a hold arc; the bargain
  "ALTAR - Stand still: a power, +10% danger" when the hero is within 3 units; a taken altar goes dark),
  `PrototypeTextDefinition.AltarInstruction`, `CombatSetup.Altars`.
- The forecourt hall district moved from (-8, -12) to (-10, -9) so no hall district lies within 4 units of an altar.
- Scene-only: the Descent simulation and its measured constants are unchanged; the hall region has no altars.

Tests: EditMode `AltarFieldTests` (the hold, reset by moving or stepping off, once per altar, floor reset, cost = half
an evolution, altars on walkable floor clear of every hall district); PlayMode `AltarFlowTests` (on the foundry the hold
shows, the card offer opens, difficulty rises by 0.1, the altar goes dark and answers once; the hall has none).

Images (actual scene, portrait game camera, `Tools/ArtReview/FoundryAltarsCapture.cs`): `altar-near`, `altar-hold`,
`altar-taken`, `strip.png` (crops). The first placement (-7.5, -16.8) put the sigil over the rail on screen; moved in.

Verification: 457 .NET / 742 EditMode / 168 PlayMode, zero failures; Verify-Project PASS; APK 36,472,489 bytes, SHA256
FC7D832BC4FB22A3367C5D6AF5FF042B6CAD4AA9CD7D3366A9EAE5F39E1D0EE7, NOT INSTALLED (the owner is using the phone). Source
freeze `7890166e2d3b19794a0c1edc203dff5d26dd93b3161081808c44b50b034b2235` (1,287 files) before and after the full suites.
The 0.1 cost is chosen, not measured; the owner's device play decides whether the bargain is worth it.
