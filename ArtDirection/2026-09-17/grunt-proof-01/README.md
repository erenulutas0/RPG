# Slag Brute / Grunt design proof

2026-09-17. Two neutral design masters and a browser scale study. **Not imported, animated or installed.** The live Grunt remains procedural. This proof does not change its role, stats, reach, physical spacing or wave composition.

## Design decision

Use a compact basalt brawler with a low head, broad shoulders, heavy fists and a small sternum furnace vent. A taller, flame-crowned creature would compete with the Mite and cover more actors; a fully armoured figure would weaken the distinction from the Vanguard. Restrained orange joints connect this creature to the foundry while the violet stone separates it from the hero's blue steel.

The front reads as a heavy ordinary enemy, not a boss. The rear preserves the material language but faces more squarely away. It remains a design candidate: shoulder shape, camera angle and planted-foot registration must be reconciled before making production poses.

## Art and provenance

Both PNGs are untouched built-in image generation outputs, copied into this folder. The front used the existing Cinder Mite and front Vanguard masters as style/camera references; the rear used the new front. Exact instructions are in `PROMPTS.md`, including the requested 1024-square canvas. Actual outputs are **1254 x 1254 RGBA**. `asset-metadata.json` records actual dimensions, alpha bounds/counts and SHA-256 for both outputs and both references. Alpha is transparent outside the creature; no background removal or repainting was performed.

- Front tool output: `C:/Users/pc/.codex/generated_images/01a095bd-7c87-7c33-83a1-013aa1757c47/exec-7b22699e-7354-457c-98d3-3b9baa38f664.png`.
- Rear tool output: `C:/Users/pc/.codex/generated_images/01a095bd-7c87-7c33-83a1-013aa1757c47/exec-3a9fc11d-3635-48f7-bc44-ce64e76f5106.png`.

These are painterly antialiased masters, not pixel-grid-aligned sheets. Front alpha>16 bounds are `[143,231,1111,1086]`; rear bounds are `[168,237,1133,1060]`, right/bottom exclusive. Their width/height ratios are 1.132 and 1.173. The gallery crops display bounds using CSS only and scales each view independently. That display normalization is **not** proof of a shared pivot or animation registration. The two master PNG files remain byte-for-byte unchanged.

## Scale and overlap study

Open `index.html`, or serve `ArtDirection` locally and open `/2026-09-17/grunt-proof-01/`. The scene has one Vanguard body, two Grunts and eight Mites. Controls switch front/rear, change only Grunt height from 85–115%, show silhouettes/foot points, and put the Grunts into a deliberately close depth pair.

- Width-9 camera at 1080 source pixels gives 120 pixels per world unit.
- Start Grunt visible height at 112.5 px, corresponding to the old 30/32-unit **canvas height**, not a measured old opaque bound. Front width is approximately 127.4 px. This slightly exceeds the old 31/32-unit canvas width (116.25 px); actual overlap needs a Unity check.
- Hero visible-height target is 165 px and Mite 60 px. These master-based comparisons are approximate; the runtime uses padded, sampled, registered sprites and separate equipment. The hero body preview deliberately has no weapon layer.
- Close-pair projected separation is 54 source px: 0.9 floor units x 0.5 depth projection x 120. This is an illustrative arrangement, not a replay or guarantee about `PackMotion`/current pair-specific spacing.
- Shadows and floor are schematic CSS. The gallery has no HUD, combat, movement, hit/death states or collision simulation.

**Review finding:** front and rear are recognizable at the baseline scale and remain distinct from the Mite's flame crown and the Vanguard's upright silhouette. The close pair still overlaps substantially, particularly at 115%; retain 100% as the initial candidate. Do not claim the new painting fixes projected overlap. The back angle needs refinement in the next pose pass.

The included link to `../contact-grip-01/device-crowd.png` is a previous actual S23 capture showing the older Grunts, not evidence of this art running on device.

## Verification and scope

- Inspected original image alpha/dimensions and recorded SHA-256; kept output bytes unchanged.
- Browser reviewed at its default desktop size and a 390 x 844 viewport; no horizontal overflow and all 17 displayed images loaded.
- Exercised front/rear, 115% size, close pair, feet and silhouette controls; confirmed 11 staged actors. Restored front/100%/spread/default viewport.
- `Tools/Verify-Project.ps1`: 375 GUIDs and 555 scene objects/components passed.
- No Assets, scene, code, import settings, profile, telemetry, APK or device input changed. Unity/.NET suites and device validation were not rerun for this static art study; the preceding contact/grip build remains the latest verified runtime. No new APK hash applies.

## Next bounded production slice

1. Reconcile the front/rear facing and foot landmarks on one shared canvas, then prepare alternating contacts and readable windup/strike poses.
2. Compare 128-PPU/bilinear reduced samples at the existing world height in the real room, including the current health bar, contact shadow, close depth pairs and rim.
3. Integrate only Grunt with shared art ownership and isolated hit/death rendering after the pose proof; keep other enemy looks procedural.
4. Run the full project gate, then inspect live movement/attack/overlap on S23 with current profile safeguards. Coordinate Unity/APK/ADB access with Opus first.
