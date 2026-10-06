# Gate arrival runtime preview — 2026-10-07

## Behavior

Opt-in entry presentation for the development foundry. `development/gate-arrival.txt` with non-empty content plus `region-preview.txt=foundry` enables it; depth-proof takes precedence. Release builds never read/enable this preview. It is an entry vista, not a proximity-triggered boss doorway or new map progression system.

CombatSetup initializes encounter data with the first encounter deferred. EncounterController owns the hold and blocks its update until BeginFirstEncounter releases it exactly once. Default initialization remains synchronous and unchanged. GateArrivalView holds hero movement and UI event dispatch, hides the existing HUD, pans/zooms to the full gate, then restores normal follow and controls after about 4 seconds. Touch, Escape, Return or Space skips; the gesture must be released before dispatch/spawning resumes. This prevents the skip gesture activating the ability or starting a drag.

The view restores camera once (avoids overwriting a safe-area refit), preserves canvas/EventSystem/movement enabled states, never changes global time scale, and cancels on pause/focus loss. Disabling stops its coroutine and restores controls without spawning during unload; re-enabling completes the cancelled handback exactly once. Guards prevent pre-Start disable/re-enable from launching another reveal. Existing pause ownership remains authoritative. Telemetry receives zero active delta while the initial encounter is held; unscaled pause/flush handling continues.

## Validation

Fast compile passed without warnings/errors. Seven initial targeted tests passed; an additional pre-Start interruption test was then added after review. Final full gate passed: EditMode 410/410, PlayMode 143/143, Android exit 0. Coverage includes spawn deferral, duplicate release, natural completion, skip, depth-proof precedence, unload/reload, disable/re-enable, early interruption, pause preservation and pre-initialize rejection.

No scene, material, mesh, save schema or balance change. Previous 17-renderer/80,316-vertex foundry art is retained. The earlier four-keyframe study is an art reference; device runtime evidence is recorded here separately. Device backup at TestResults/arrival-device-20261007. No push.

## Device closure

Installed APK SHA256: `520E96960DDE255A99D13D32E668E9EE5BB9F6D0EEEAF885367C1CE672424EDD` (31,113,605 bytes); installed base.apk matched. Guarded S23 captures show the complete gate, natural return to Room 1 combat, and touch skip at the ability-button location with the ability still ready. Natural-run telemetry records the first room at 4.183 seconds and the first kill at 1.118 active seconds, excluding presentation time. The short skip run was force-stopped before its telemetry flush; skip evidence is the device capture and automated tests, not a persisted ability-event audit.

Profile and backup plus both original development files remained SHA256-identical. Existing telemetry retained. The new gate-arrival.txt flag is intentionally left enabled for the owner to try on the next launch; remove it to disable this preview. No depth/render diagnostics enabled. Original plugged-in awake setting (2) restored; app stopped. Process logs contain Android AHardwareBuffer allocation messages, but no managed exception/crash was found in these samples. No new FPS benchmark performed.
