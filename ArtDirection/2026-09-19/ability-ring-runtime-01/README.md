# Integrated eight-arc ability ring — 2026-09-19

The preferred contour from ability-ring-01 now replaces the runtime dashed placeholder. No authored scene, ability radius/damage/cooldown, enemy data, movement budget or targeting change. Old AbilityArt.DrawRangeRing remains available as placeholder art with its existing tests; AbilityRingView no longer calls it.

## Ownership and geometry

AbilityRingArt owns one immutable 512×256 RGBA32 texture and one unit-radius sprite per session. Raw texture payload is 524,288 bytes; no mipmaps, bilinear, clamp, CPU readability discarded after upload. A single temporary Color32 buffer is used on first creation. It is not a measured total-memory or first-frame budget. Domain/subsystem reset and application quit release both native objects. Scene reloads borrow the same sprite/texture; renderers and independent ground sorting groups remain per view.

Sprite PPU is 240, equal to the mathematical horizontal contour radius in texels. Each view scales it by the actual ability radius; vertical radius is half the horizontal radius to match ArenaFloor projection. The transparent 16-texel horizontal padding is deliberately excluded from the reach assertion. The antialiased stroke has finite thickness around the precise contour centreline. Eight long arcs and four small inward lozenges are deterministic code-native art, matching the selected proof. No image model or external raster source was used.

Existing ready breathing (0.65–0.75 alpha at default settings), cooling alpha0.22, scaled-time pause and run-ended behaviour are retained. The renderer remains a child of the hero with an independent ground sorting group, below bodies and above the platform. At the rim the marker still extends beyond the arena: no new clipping/masking rule was introduced.

## Files and checks

- Runtime: Assets/_Project/Scripts/Art/Unity/AbilityRingArt.cs and UI/AbilityRingView.cs; fresh meta for the new script.
- AbilityFlowTests now checks the contour rather than padded texture bounds. New cases check actual renderer translation with the hero, pause colour stability, texture format/readability/payload, sprite/texture reuse after a real scene reload, and native release/recreation at session reset. Existing burst/targeting/cooldown tests still apply.
- Four actual Unity captures from Tools/ArtReview/AbilityRingRuntimeCapture.cs: ready, shorter portrait, rim and cooling after AbilityController.TryUse. Unlike the earlier proof, the real view selects/owns the displayed art and real ability state drives cooling. Test-local profile and frozen ten-enemy room; no scene save or manual alpha replacement. Camera framing is staged for each aspect ratio. This is not device evidence or a dynamic crowd-spacing benchmark.

Reproduce graphics with `pwsh -File Tools/ArtReview/Run-AbilityRingRuntimeProof.ps1`. It stages/removes its own test and meta, runs pure checks and compilation, then the graphics capture and lossless BMP-to-PNG bridge. Full verification/build uses `pwsh -File Tools/Run-UnityTests.ps1`, followed by `pwsh -File Tools/Verify-Project.ps1`. Hashes are in sha256.txt.

Remaining art: cast/impact presentation, Staff/Daggers matching painted equipment, remaining enemies/boss, platform foundation and strict support-foot contact. This slice does not address low-density encounter pressure or add boss danger telegraphs.

The cooling capture deliberately includes the existing bright pixelated cast burst near the hero. That separate expanding effect is unchanged; the new range contour is the faint outer line. Matching the cast burst to the new contour is a later art slice, not evidence of a range regression.

Verified: .NET291/291; compile0 warnings/errors; EditMode306/306; PlayMode110/110; graphics capture1/1; static integrity440 asset/folder GUIDs and568 scene objects/components. Android exit0, actual APK29,152,854 bytes, SHA256 `300759ECC845E8CB12FA9988E6BC6FF481E9C96ABF703CB63DBE1E3A30D76ED6`. Installed on S23; installed base.apk hash matched. Device evidence and limits follow in Docs/23 and the runtime README.

## Integrated ability ring on S23 — 2026-09-19

Following the owner's phone handover and continuation, installed the verified eight-arc ring APK with adb install -r. APK29,152,854 bytes; installed/base hash matched `300759ECC845E8CB12FA9988E6BC6FF481E9C96ABF703CB63DBE1E3A30D76ED6`. Gate: .NET291, clean compile, EditMode306, PlayMode110, graphics1, integrity440/568, Android0.

Fresh before-install screenshot: Cinder Walk2/6, level4 choice,56HP,23 at-risk run gold. Announced reinstall ended that active run. Fresh profile/backup/telemetry copied to local TestResults/ability-ring-device-2026-09-19/before. Primary/backup profile SHA256 remained identical before/after installation and after play: `6CD3D4D338536FE77993A3D52E88E55C6577F7FD3C6FC32FFDE81AAE45BC8DE3` and `8617F6460C901005EFF3CEB0D6CFDFADD072C36D1068A3187002878287C39B41`. Revision150, banked11596gold, Daggers/Second Wind, both weapon/relic unlocks and deepest2 unchanged. No save restore, equipment change, purchase or development flag. Telemetry was not rolled back.

Normal Ember Hall: selected Tempered Edge, dragged toward the right rim and observed the bright new contour centred under the hero. Fired the ability; next capture shows the dim contour and cooldown8. No apparent floor/body sorting error in these samples. Focus, awake and unlocked checks preceded each input/capture and followed captures before keeping. Filtered last1500 logcat lines contained no FATAL EXCEPTION, NullReferenceException, MissingReferenceException or AbilityRingView binding error; not an exhaustive log/performance check. Session reuse and paused breathing are covered by Unity tests, not a separate phone restart/performance experiment. The cast burst itself is still the old pixel effect.

Final observed state: Ember Hall1/6, level2 choice open,71HP,10 at-risk run gold,XP25/36, full movement, ability countdown4 held by the choice. Phone handed back; inspect fresh before future use. Selected unmodified screenshots are in ArtDirection/2026-09-19/ability-ring-runtime-01/device; raw captures and private backups remain local under TestResults. This supersedes earlier proof-only ring and97A5 latest-installed statements. Next art: match the cast/impact effect to the new contour without delaying damage.
