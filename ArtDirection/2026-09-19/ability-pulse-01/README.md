# Ability cast pulse — 2026-09-19

The ability's thick pixel burst now uses the same eight-arc artwork as its range marker. CombatEffectsView borrows the existing session-owned sprite in a one-element array created during setup; casts reuse the existing effect pool. No new textures, meshes or GameObjects per cast. Staff splash, coins, hit sparks, damage numbers and other effects retain their current art and timing.

## Behaviour

- Damage remains immediate through AbilityController; OnAbilityUsed changes presentation only. No expanding damage wave, new hitbox, delayed reward or balance change.
- Preserve the old three-times-0.07-second duration: total lifetime0.21s. Start at60% of ability radius, ease out to100% by0.14s, fade during the final0.11s from maximum alpha0.85. The range contour still shows actual full reach throughout.
- Stay at the cast origin even if the hero moves. Use ground order0 below actor groups1 rather than the old over-body effect order5. Existing range indicator also occupies ground order0; their near-coincident outlines briefly reinforce each other at the end.
- Scaled time freezes elapsed time, expansion and fade during pause/choices. The pool slot is disabled at expiry; its ability-specific flags/radius are reset on every generic reuse so other effects retain their normal transforms/colour rules.
- Session ownership stays in AbilityRingArt; the borrowed sprite is excluded from CombatEffectsView's owned destruction list. Existing session-release and scene-reload tests remain applicable.

## Verification and visual evidence

AbilityFlowTests adds a real cast test for initial radius, below-body sorting, fixed cast origin, pause stability, expansion bounded by the real ability radius, expiry and retained shared art. Existing ability tests still verify immediate damage and cooldown. No additional pure logic was introduced.

Seven actual Unity renders: ready at two aspect ratios, rim, then pulse start and after6/12/16 simulation frames at fixed1/60s. The ability is really fired through TryUse; effects advance through the normal Update. Scene/profile are isolated and never saved. The test pauses between captured frames; the gallery is a sequence of stills, not real-time video. Old effect comparison links to the prior runtime capture; the separate captures are not a synchronized A/B recording. In all cast captures, the dim full-size outline is the range marker and the brighter expanding line is the cast pulse.

Reproduce: `pwsh -File Tools/ArtReview/Run-AbilityPulseProof.ps1`. Source: Tools/ArtReview/AbilityPulseCapture.cs. The runner stages/removes its temporary test/meta, runs pure tests and compile checks, then graphics capture and lossless BMP-to-PNG conversion. Hashes in sha256.txt. Full gate/build: Tools/Run-UnityTests.ps1, then Tools/Verify-Project.ps1.

Remaining: Staff's separate splash is still pixel art; coins and some impact effects are still provisional. No boss telegraphs, new weapons or game-balance changes in this slice.

Verified: .NET291/291; compile0 warnings/errors; EditMode306/306; PlayMode111/111; graphics capture1/1; integrity440 GUIDs /568 scene objects/components. Android exit0, actual APK29,153,822 bytes, SHA256 `66CE2B7B2CB39E273C2DDC35B5795EEA70E518B65FF40BD50F0A00CD87D94165`. Installed S23 base.apk hash matched.

## Ability cast pulse installed on S23 — 2026-09-19

Owner's device permission/continuation remained in effect; announced install and short recording. Installed verified APK29,153,822 bytes, SHA256 `66CE2B7B2CB39E273C2DDC35B5795EEA70E518B65FF40BD50F0A00CD87D94165`; base.apk matched. Gate291/306/111, clean compile, graphics1, integrity440/568, Android0. New pulse uses the shared contour and existing effect pool; damage/cooldown/total0.21s duration unchanged.

Before: Ember Hall1/6, level2 choice,71HP,10 at-risk gold; announced reinstall ended that active run. Fresh primary/backup profile and telemetry copied under TestResults/ability-pulse-device-2026-09-19/before. Profile hashes match before, after install and after play: primary `6CD3D4D338536FE77993A3D52E88E55C6577F7FD3C6FC32FFDE81AAE45BC8DE3`, backup `8617F6460C901005EFF3CEB0D6CFDFADD072C36D1068A3187002878287C39B41`. Revision150/gold11596/Daggers/Second Wind, unlocks/deepest2 unchanged. No restore, purchase, equipment change or development flag; telemetry not rolled back.

Selected Tempered Edge and fired ability in ordinary Ember Hall. A six-second screenrecord, reviewed as a20fps contact sheet, shows the small bright contour expanding to the outer indicator and disappearing, leaving the dim range marker. No thick pixel burst remains on ability use. It is a visual check, not a timing/performance benchmark; pause/origin/expiry are also covered by Unity regression. Focus/unlocked/awake checks preceded inputs/capture/recording and followed capture/recording before retention. Filtered last1500 log lines contained no FATAL EXCEPTION, NullReferenceException or MissingReferenceException; not exhaustive.

Final observed: Ember Hall1/6 level2 choice,71HP,10run gold,XP25/36, full movement. Phone handed back; inspect fresh before further use. Raw recording/screens and private backups remain in TestResults; committed device/pulse.mp4 is a540-pixel-wide H264 transcode of the unaltered-speed screenrecord, with selected original PNGs and hashes under ArtDirection/2026-09-19/ability-pulse-01/device. No private saves/telemetry committed. The previous installed300759 build is now superseded. Staff splash still uses its old ring; that separate presentation, coins and remaining equipment art are future work.
