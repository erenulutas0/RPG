# Slag Brute runtime polish â€” 2026-09-17

The ordinary Grunt now uses painted front/rear art, mirrored for four diagonal facings, in the actual game. This supersedes the temporary substitution in `../grunt-motion-01/`. The runtime imports are provisional art, not final animation acceptance.

## Sources and registration

Untouched built-in image generation originals, exact prompts and alpha/hash metadata are retained here. Front idle/contact A/contact B/strike reuse `../grunt-motion-01/front-sheet-v2.png`. `rear-sheet-v2.png` supplies the corresponding four rear poses. Its wrong-arm windup is excluded. `rear-sheet-v3.png` is rejected because low-alpha haze covers the supposedly empty background. `passing-v1-rejected.png` repeated the support leg; selected `passing-v2.png` has opposing supports. `death-v1.png` supplies two collapse stages.

`registration.json` records explicit cell rectangles, manually judged floor roots and one uniform scale per sheet. The larger passing/death sources scale by 1024/1254 and 1024/1774 respectively. No per-pose alpha-fit normalization, repaint or stretch. Registered canvas640x640, uniformly sampled by Unity to180x180,128PPU,bilinear,pivot90,18. Runtime sprites are non-readable, no mipmaps, clamp, FullRect, Android ASTC4x4. The masters remain unchanged.

## Runtime behavior

- `EnemyArtSet` retains original indices0 idle,1/2contacts,3strike; optional appended4/5passing support Grunt's four-pose gait1,4,2,5. Mite's four-frame set and .55-unit cycle remain supported. Grunt cycle .7 floor units.
- Visual top1.05 places the bar root at1.17; shadow width .78. These are independent of the transparent canvas bounds. Old procedural attack nudge is zero for Grunt.
- Direction comes from travel, or the actual stationary attack target. Only presentation mirrors. Attack damage remains immediate; no invented windup or combat delay.
- Shared body/flash/death imports are borrowed across actors/reloads. Existing hit compression and independent pooled death renderer are reused. Rewards/enemy removal are immediate; the collapse visual expires separately.
- Gameplay scene, stats, collision, enemy spacing, camera, packages and project settings are unchanged.

## Reproduce and inspect

With Unity closed, `pwsh -File Tools/ArtReview/Run-GruntProof.ps1` runs the graphics capture. Add `-Reimport` to rebuild imports from the retained masters/registration. Temporary Editor/test fixtures are removed by the runner. `runtime-*` images are actual Unity renders; the gallery gait only cycles those captures at illustrative timing.

Verified: .NET272/272; compile0 warnings/errors; full EditMode284/284; full PlayMode100/100 including four new Grunt tests; graphics `GruntRuntimeCapture`1/1; Android exit0; static integrity404 GUIDs/555 scene objects/components. Tests cover shared asset lifetime/reload, four facings and passing poses, pause, immediate strike/target facing, matching hit flash and independent death/instant reward. Existing Mite coverage passes. Gallery pose/direction/play controls, loaded images and390x844 layout checked.

APK SHA-256: `AC9D4FC627CD94484A2923DEBB7AEC2CD2894CF3E51537207123B1BA1A6814B0` (24,739,917 bytes). Phone review is recorded in the newest Docs/23 entry.

## Remaining limits

Four poses improve the gait but do not implement world-space foot locking. Stone volume continuity and mirrored light/handedness need polish. Large enemies can still overlap the hero, chest and one another in projected depth. Gallery close-ups exaggerate sampling; use the actual room/device images for phone-scale judgment. This slice is not a balance or sustained/mid-range performance acceptance. Other large enemies, matching Staff/Daggers, arena/props and audio remain separate work.

## S23 smoke and cleanup

Installed APK hash matched; profile unchanged across install. One Daggers ten-enemy proof with southeast/southwest/northwest drags completed (15.741 active seconds,10 kills,43.144 HP,10 gold). device-crowd/front/rear.png are unchanged phone captures, distinct from runtime-* Unity renders. Temporary proof flag removed; ordinary Ember Hall restored. Final profile revision133 /9794 gold /Daggers /Second Wind; no backup restore. Full preservation details and limits are in Docs/23.
