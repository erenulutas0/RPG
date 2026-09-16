# Vanguard equipment and keypose proof — 2026-09-16/17

Original equipment and adjacent body poses generated with the built-in image generation tool, followed by actual Unity scene renders. Folder date follows the art session started on September 16. This is a **rough animation blocking proof**, not an imported production animation or a phone build.

## Completed in this slice

- `sword-v1.png` and `shield-v1.png`: independent transparent masters matching the blue-steel/silver/brass character. In Unity they replace the old pixel equipment **inside the temporary review only**. The narrower clean blade and round buckler now share the armor's material treatment.
- `walk-a-v1.png` and `walk-b-v2.png`: alternating leg-pose candidates, preserving the rear three-quarter character identity. The first `walk-b-rejected.png` repeated almost the same footfall as A; it is retained for provenance and excluded from the preview.
- `attack-windup-v1.png`: a raised right forearm, with a measured new grip anchor for the separate sword.
- Four static pose views, one full room render, four rough walk frames and five attack-blocking frames captured in Unity. The HTML preview plays the captured frames and allows sequence, rate and frame selection.
- `PROMPTS.md` contains all six exact generation prompts, reference roles and original output paths. `asset-metadata.json` records untouched master dimensions, alpha bounds and SHA-256 hashes. Every generated PNG retains true transparency; no painting, masking or resampling was applied to the master files.

## Registration and rendering

All body poses use the same source-space crop: x=240..1000, top=89, bottom=1213 (exclusive). Original idle is 1176×1338, edited poses 1176×1337; the crop's bottom-origin coordinate accounts for that one-row canvas difference. The rendered body canvas is 119×176 at 128 PPU, with pivot (354/760, 0). No frame is independently fit to its alpha bounds. This preserves the intended scale and registration instead of hiding pose drift by stretching each image.

`registration.txt` records source-pixel hand positions. Per-pose anchors move the sword with the right gauntlet; the shield remains behind the left arm. The sword is sampled into 95×96 and the shield into 51×58, at 128 PPU/bilinear, using mipmapped source sampling. Rendering and the lossless format bridge follow the preceding `character-unity-01` proof. The original idle body and Mite sources remain in `character-proof-01`; exact equipment sampling and pivots are in `Tools/ArtReview/CharacterProofCapture.cs`.

The images are actual Unity renders with the real floor/backdrop/HUD, frozen staged Mites and an isolated test profile. They are not screenshots from the phone. There is no live combat, target selection, contact solving or performance benchmark in these frames.

## Art review and limits

The equipment materially improves style consistency compared with the old pixel sword/shield. The upper body is recognizably stable across the edited poses, though painted highlight and cape differences still exist. The corrected B pose alternates the extended leg as intended.

The walk remains rough: the idle image is temporarily reused between the two contact candidates. The soles/raised boots and lack of dedicated passing/down poses make it too springy to call a planted-foot production gait. A character rendered in place does not prove foot locking at the actual movement speed. The attack uses rest and one windup with weapon rotation; its large hand-position jump needs strike/recovery body frames and timing against the actual hit event. These missing poses are not solved by making the browser playback faster.

Closed fists also still need explicit grip/finger occlusion review, particularly during a sweep across the body. The current images do not supply independently rigged fingers. Opposing/front-facing motion, Mite movement/attack/hit/death, staff/dagger art, runtime flash silhouette generation, importer/compression settings and cached ownership remain open. No phone integration is claimed, and the existing gameplay contract is unchanged.

**Next bounded art work:** draw a proper passing/down pair and strike/recovery for this same rear-facing loadout, review the gait at gameplay scale and speed, then validate opposing facings and runtime import/animation integration. Do not expand the hero roster or ship the rough loop as a finished walk.

## Verification and reproduction

```powershell
pwsh -File Tools/ArtReview/Run-CharacterProof.ps1 -Keyposes
```

This reuses the isolated graphics capture fixture, staged temporarily under PlayMode with fresh metadata and removed afterwards. It verifies shared body size/pivots and per-pose weapon attachment. `TestResults/hero-keyposes.xml` and `.log` are ignored local evidence. The default command still reproduces the preceding density/attachment proof; it was rerun after extracting the shared setup.

Verified: .NET 272/272, Unity compile 0 warnings/errors, targeted keypose capture 1/1 and preceding capture 1/1. Browser playback, sequence/rate/frame controls, image loading and narrow layout reviewed. Static integrity and master hashes checked after temporary cleanup. Full EditMode/PlayMode suites were not rerun; no authored gameplay source, scene, imported asset, package lock or project setting changed. No APK build, install or phone input occurred.
