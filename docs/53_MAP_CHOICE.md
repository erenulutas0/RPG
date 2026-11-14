# 53 - The map choice: starting a run on floor 1, 2 or 3

Status 2026-11-14: built (`MapChoiceView`, `DescentStart`, `RunStartRequest`), all three maps unlocked. Asked by the owner
to show the game on the phone: a run can start on the Foundry, the Quicksilver Vaults or the Gloam Gardens.

## What the player sees

- Every scene load that starts a run (the app's launch, the result screen's Try again, the Relic Forge's Start) opens the
  map choice over the foundry before the first wave: the title "Choose your map" and three full-width cards, one per map,
  stacked for a thumb on a portrait phone (each 912 x 380 canvas units on the 1080-wide canvas). A card shows the map's
  name, a line about it, what a hero starting there begins with, and a crop of the floor's own backdrop plate (the
  foundry's cavern plate, `ENV_Ice_Vault_v1`, `ENV_Gloam_Garden_v1`, through `ArenaView.CavernBackdrop` and
  `PlatformMaterialSet.BackdropFor`). It is drawn in the run's choice cards' look (`ChoiceCardPainter` with
  `RunChoiceView`'s `ChoiceCardArtSet`: the dim, the surface, the title plate; the Foundry's card in the neutral treatment,
  the deeper two in the checkpoint's Climb), on its own overlay canvas (sorting order 100) above the HUD. The hero stands
  while it is open; nothing spawns, and the run's telemetry has not begun.
- **The Foundry** closes the panel and begins the run there and then, exactly as every run began before.
- **Quicksilver Vaults / Gloam Gardens** reload the scene with the choice (`RunStartRequest`, read once by the next
  `CombatSetup.Awake`). That load starts on the chosen floor with no map choice, the hero raised to the floor's arrival
  level; the level-up panel opens at once and offers every level-up of it one after another, and the first wave walks in
  after the last pick.
- The hero chooser stays on the Relic Forge (between runs); the map is chosen at the start of each run.

## Starting deeper is the floor a descent reaches

`EncounterController.Initialize(..., startFloor)` walks the authored Descent from the scene's floor 1
(`EncounterController.DescentFloor`) and enters the chosen floor with:
- `FloorNumber` = 2 or 3 (and `StartFloorNumber`): the climb's scaling (`ClimbScaling.Health/Damage/Boss`), the horde's
  seed (`hordeSeed + FloorNumber`), the HUD's and the result screen's floor, the telemetry's `floor_index`;
- the halls the floors above add to the round (`LaterHalls`), so its rooms open at the districts a descent would open;
- the floor asset's own theme and dress (`ArenaView.ApplyFloorTheme`: ice or garden, the enemies' frosted or garden
  palette as each stands up), rooms, elites (the Mirror Sentry on floor 2), gate, Warden and modifier;
- the chain onward: floor 2 goes on to floor 3 and the finale, floor 3 to the finale, through the usual checkpoint.
- No entry transition: the frost and the melt belong to the descent onto a floor (`ArenaView.FrostsOnEntry`,
  `MeltsOnEntry`), and a run that starts there did not descend.

The development start-floor file (`DevelopmentStart.FirstFloor`) is unchanged and wins over a chosen map: it still keeps
`FloorNumber` 1 (the scene tests that start on a proof or Descent floor through it measure that), and no map is offered
with it. `DevelopmentStart.OffersMapChoice` also offers none with the gate arrival, the depth proof, the autopilot, the
density probe, the hall arena, or `development/map-choice.txt` saying `off` - which `TestProfile.Begin` writes, so every
existing scene test starts its run at once on floor 1, as before.

## Catch-up power: the numbers and their source

`DescentStart.CatchUpLevel`: **floor 2 starts at level 31, floor 3 at level 44**, every level of it a level-up left to
choose (`RunState.PendingUpgrades`), offered by the run's own seeded offer streams (`RunRandom` by the run's seed and the
offer's index), so the same seed offers the same cards.

Source: the Descent simulation (`Tests/Support/DescentSimulation*.cs`) on this branch's floors, the batch report run as

    FOUNDRY_FLOORS=4 FOUNDRY_COHORTS=1 FOUNDRY_LEVERS=0 FOUNDRY_REALISM=typical \
      dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~FoundryBatchReport.TheFoundryFloorOverASeedBatch"

seeds 1-200 a hero, both heroes; the level a run carries down the checkpoint (`foundry-cohorts-floor2.csv` and
`-floor3.csv`, column `floor1_level_end`, the previous floor's level at its end):

| Arriving on | Careful, typical hands (both heroes) | Standing | First-card | Clumsy | Chosen |
|---|---|---|---|---|---|
| Floor 2 | P50 31 (P25 30, range 29-33; 393 runs) | 30 | 30 | 31 | **31** |
| Floor 3 | P50 44 (range 42-46; 385 runs) | 43 | 44 | 44 | **44** |

The careful cohort's P50, the upper of the cohorts' medians, because the catch-up gives the levels alone: a walked run
also brings the chests' books, the forge rooms' Temper and the altars' blessings (and their difficulty), which a fresh
start does not have.

**No gold.** A run's gold buys nothing during the run; it is banked into the profile's forge gold as the run ends. The
gold a hero would typically carry down would be forge gold the player never earned, so a deeper start carries none.

## Telemetry and the profile

- `run_start` gains `start_floor` (2 or 3) on a run begun deeper; a floor-1 run's line is unchanged. Its rooms and floors
  carry their real floor numbers.
- `DevicePull` (the V0.1 gate's device rows) leaves such runs out of the owner's floors, runs and whole Descents and counts
  them apart (`DeviceEvidence.DeeperStartRuns`, named in the row's basis).
- A floor-1 run that the player leaves for a deeper map never began its telemetry (it begins as the Foundry is tapped), so
  no empty run is logged.
- The profile records what the run really did: `DeepestFloorCleared` rises if the floor is cleared; the Foundry's hero
  unlocks still need floor 1 cleared.

## Tests

- EditMode: `DescentCatchUpTests` (also in CombatChecks), `MapStartFloorTests`, `RunTelemetryTests.ARunStartedOnADeeperMapTagsItsStartFloor`.
- PlayMode: `MapChoiceFlowTests`.
- CombatChecks: `DevicePullTests.ARunBegunOnADeeperMapIsLeftOutOfTheOwnersFloors`.
