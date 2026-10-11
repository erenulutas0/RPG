# Chest telemetry regression found during the art gate

Initial full PlayMode run: 155/156 passed. WalkingOntoTheFirstChestLogsItWithTheRoomAndTheHealedHealth expected HP <=100 and observed115; APK was correctly skipped. The original failed XML is retained under initial-gate.

ChestSpawner.Open applies its heal, optionally grants a book, then announces Opened. A MaxHealth book can legitimately raise current and maximum health after healing. Telemetry records that post-reward value. The test's fixed100 ceiling predates chest books; production behaviour was correct.

TelemetryFlowTests now covers deterministic seed0 (no book) and seed34 (rare health book), snapshots Current/Maximum inside Opened, verifies a single opening and exact telemetry HP/book identity, and retains room/reward/envelope checks. The regression case requires maximum HP to rise and current HP to exceed the old maximum, so logging the pre-book heal or clipping at100 still fails. No production gameplay or telemetry code changed. Independent review accepted the stronger contract.

Focused actual Unity result:2/2 passed, zero failures/skips; chest-regression.xml. Those cases are included in the final full PlayMode total, not added twice. The first failed run and focused rerun do not inflate the distinct suite count.
