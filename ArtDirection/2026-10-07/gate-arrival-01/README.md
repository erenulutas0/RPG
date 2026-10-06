# Gate arrival art study — 2026-10-07

## Delivered

Actual Unity portrait captures at the current centred gate point: combat, transition, full reveal and restored combat. Interactive page provides timing, manual stages, skip/Escape and background-tab reset. Browser playback crossfades static images; it is not a recorded or shipped Unity camera animation. The title treatment is a browser-only design overlay.

Reveal camera uses centre (19,16.4), width17.6 and portrait orthographic size19.0667. Root HUD canvases are temporarily hidden only within the isolated capture. This admits both gate towers and gable, with apron/boilers below. The hero is necessarily smaller; this lens is suitable for a short non-combat landmark view, not continuous combat.

The tool restores camera position/size, follow enabled state, every root canvas enabled state and backdrop framing in finally. Assertions pass. Combat and restored PNG SHA256 are identical: `8C827148136F303578D7C608F96F775EB560AC18937C81B6916DA4BBA5883701`.

## Integration contract

The current foundry gate is scenery in a preview map, not a dedicated safe arrival state. Do not trigger a camera takeover merely on proximity or AliveEnemyCount==0: a wave may be about to start, rewards may be pending, or the player may be moving through danger.

A future implementation should:
1. Run once per entry only in an explicit pre-encounter arrival state. Delay spawn/attacks through that state's owner rather than independent component toggles.
2. Complete in about4seconds; tap/back skips and returns immediately. Never require waiting after repeat visits.
3. Preserve exact normal follow framing, HUD state and input ownership, including interruption by pause/background/scene unload. Do not globally overwrite Time.timeScale or persist presentation state into the player profile.
4. During return, avoid applying the skip touch as a hero drag or ability activation. Respect existing UI/safe area and pause behavior.
5. Verify active-enemy refusal, incoming-wave refusal, normal end/skip/background/unload restoration, portrait aspect variants and first-control responsiveness before shipping.

These are implementation requirements, not implemented behavior. No new runtime script/scene/asset, APK or phone mutation in this study. The installed B2D5A6F5 package remains current.

## Validation

`Tools/ArtReview/Run-FoundryRuntimeProof.ps1`: actual graphics capture1passed, including state restoration assertions; parent inspected full-reveal image. No runtime change, so the prior545-test/Android gate remains the applicable build evidence rather than a falsely claimed new run. No push. This closes the visual composition/timing study; safe gameplay arrival integration remains separate work.
