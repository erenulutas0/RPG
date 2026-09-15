# Portrait camera comparison — 2026-09-16

## Decision

Use width 9 for the next on-device movement review, replacing width 6. Keep portrait, actor world scale, movement speed, collision geometry and HUD sizes unchanged. Width 7.5 is a compromise, but width 9 gives more space to see approaching packs while retaining readable placeholder silhouettes. This is a framing step, not a claim that dense kiting or final art is complete.

| Visible width | Hero canvas height at 1080 px width | Mite canvas height | Horizontal span vs width 6 |
|---|---:|---:|---:|
| 6 | 247.5 px | 90 px | 100% |
| 7.5 | 198 px | 72 px | 125% |
| 9 | 165 px | 60 px | 150% |

These are canvas bounds at 32 texels/world unit (44-texel hero and 16-texel mite), not opaque silhouette measurements. At 540 px screen width all sizes halve. The visible world rectangle has 2.25 times the area at width 9 versus 6 for a fixed aspect ratio; the usable platform portion is smaller where the rim intersects it.

The current platform is 18 world units wide. A full-island boss composition therefore needs separate geometry/framing work; width 9 cannot show both lateral corners at once. Do not force the normal-room camera to fit the whole island, or promise the reference composition from this single zoom change.

## Evidence and limits

Six PNGs compare widths 6/7.5/9 at 1080x2340 and 1080x1920. They are actual Unity offscreen renders of current procedural artwork and authored HUD, using the same frozen hero and seven manually placed enemy visuals. Those seven visuals have no combat controllers or health bars: this isolates scale/spacing, not pursuit, combat density, spawn timing or performance. Their world positions and sprite scale are identical across renders. A chest and the existing ability ring remain for scale context.

The offscreen renderer temporarily converts overlay canvases to camera-space canvases and assumes 100 px top / 76 px bottom insets; framing uses the conservative HUD band from the analytical tests. It is not a physical-device screenshot or proof of real safe-area behavior. An early export pass used stale Game View coordinates after changing target height; those previews were replaced with explicit target-size framing. Final device evidence is separate in docs/23.

The one-shot capture fixture is preserved locally in `TestResults/CameraCaptureTemporary.cs`, with `camera-capture.xml` and `.log`. It was removed from Assets before the full gate and APK build. Its final targeted run passed 1/1. Initial export compilation failed because this minimal project lacks optional Unity image-conversion support; BMP export plus lossless PNG format conversion avoided adding a package. An intermediate fixture run failed on its own unexpected informational log messages; those were removed. No project dependency changed. Raw BMPs are local ignored evidence under `TestResults/camera-review-raw/`.

## Next technical slice

S23 follow-up passed a short smoke review on the width-9 APK: horizontal and diagonal drags, a Staff hit during the horizontal gesture, small enemies/chest, a left-side guardian body/bar, and extraction. See docs/23 for hashes, profiles and actual capture paths. Near the far rim, substantial void becomes visible above the platform because the camera still follows without room-bound clamping; treat this as a remaining composition issue for room-specific camera work. This review does not establish ten-enemy survivability or performance.

The owner confirmed Opus has not started the proposed 10-enemy task. That task remains in `../arena-target/OPUS_KITING_BRIEF.md`. It must test moving-hero behavior and costs before increasing live density. This camera-only slice leaves the existing maximum pack of seven intact. Judge the future ten-enemy test at width 9 first, and revisit framing if combat evidence requires it.
