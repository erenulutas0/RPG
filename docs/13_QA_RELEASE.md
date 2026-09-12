# QA, Testing & Release

## Test pyramid

### EditMode
Fast logic tests:
- damage formulas,
- modifiers,
- economy,
- save migrations.

### PlayMode
- spawn/death,
- room transitions,
- UI binding,
- upgrade application.

### Device
Mandatory:
- low/mid Android,
- different aspect ratios,
- thermal/performance sessions,
- interruption/resume,
- airplane/offline scenarios where supported.

## Critical test cases

### Save
- first launch,
- normal save/load,
- corrupted file fallback,
- version migration,
- app killed during save.

### Combat
- simultaneous death,
- overkill,
- enemy death emits reward once,
- pooled object resets all state,
- status expiration,
- boss transitions.

### UI
- notches/safe area,
- long localization strings,
- rapid taps,
- pause/resume,
- low FPS interactions.

### Purchases later
- success,
- cancel,
- pending,
- restore,
- duplicate callback,
- offline receipt behavior.

### Rewarded ads later
- ad unavailable,
- player closes early,
- completion,
- reward exactly once,
- no network.

## Performance test

Profile on device.
Watch:
- CPU,
- GPU,
- GC alloc,
- memory,
- load spikes,
- battery/thermal behavior.

## Google Play testing note

For personal Play developer accounts created after 2023-11-13, Google currently requires a closed test with at least 12 opted-in testers continuously for at least 14 days before production access application.

Plan this into release timing if it applies to the account.

## Build channels

- Development
- Internal
- Closed Test
- Production

Use versioning:
- semantic-ish app version,
- monotonically increasing Android version code.

## CI later

Once project stabilizes:
- compile/test on push,
- build Android artifact on tagged release,
- optional automated upload.

Do not build a giant CI system before the game loop exists.

## Release gates

No release if:
- save loss reproducible,
- purchase reward duplication,
- blocking crashes,
- tutorial impossible,
- severe FPS instability on target device.

## Bug severity

P0:
- data loss,
- purchase issue,
- crash on launch.

P1:
- run cannot progress,
- major reward wrong.

P2:
- visual/UX defect with workaround.

P3:
- polish issue.
