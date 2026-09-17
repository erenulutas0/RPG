# Grunt / Slag Brute registered motion proof

2026-09-17. Twelve keyposes sampled in the real Unity room under `TestProfile` isolation. **A graphics/animation blocking proof, not runtime integration or a phone build.**

## Deliverables and decision

- Two selected six-pose RGBA sheets: `front-sheet-v2.png`, `rear-sheet-v1.png`. Each contains idle, two alternating contacts, windup, strike and recovery.
- `front-sheet-v1-rejected.png` is retained as the source of the correction: its windup and strike used opposite arms. The selected version uses the same image-right arm. Do not sample the rejected sheet.
- `registration.json` records twelve source rectangles and manually judged floor roots, all mapped to one canvas at one scale.
- Sixteen **actual Unity** captures: twelve close-up poses, the same density room before/after substitution, and front/rear close depth pairs. The runtime Vanguard, eight Mites, floor, bars, HUD, camera and shadows remain in the scene.
- `index.html` compares front/rear pose cuts, walk/attack blocking, mirroring and room captures. Animation timing is illustrative; it is not a recording of live combat.

Retain the compact baseline scale. Violet basalt and the small orange vent match the existing roster; a taller version would cover more of the hero. Rendering the candidate in the real room confirms that art replacement alone does **not** fix crowd occlusion. At the captured pack position one Grunt is behind the hero and the other intersects the chest silhouette; the deliberate .45-projected-unit close pair also overlaps. Do not hide these limitations by presenting an unusually spread-out composition as gameplay evidence.

## Provenance

Built-in image generation was used, with the prior front/rear design masters as references. Exact prompts and the correction chain are in `PROMPTS.md`; measured dimensions, alpha and hashes are in `asset-metadata.json`. All three PNG outputs are retained unchanged at 1024x1536. Although the tool's displayed preview appeared to have a smoky backdrop, actual channel inspection found transparent outer pixels in **all three** sheets; no background-removal processing was needed or performed.

Original output files under `C:/Users/pc/.codex/generated_images/01a095bd-7c87-7c33-83a1-013aa1757c47/`:

| Workspace file | Tool output |
| --- | --- |
| front-sheet-v1-rejected.png | exec-6ab10d1c-495d-4072-a3f8-fe764036e557.png |
| front-sheet-v2.png | exec-c9c4751d-9d6c-4110-bda4-62161f161529.png |
| rear-sheet-v1.png | exec-c77cdffe-9502-47fb-8f5c-f4c88229790d.png |

## Registration and sampling

The generated sheets do not obey exact 512-square cell boundaries. In particular the rear strike reaches into the preceding row's empty margin. Explicit non-overlapping occupied rectangles isolate the creatures; do not slice this sheet by equal cells.

For every pose, source point `(sx,sy)` in top-left image coordinates is placed at `(320 + sx - rootX, 64 + rootY - sy)` in a bottom-left **640x640** transparent working canvas. The same uniform GPU sample reduces it to **180x180 at 128 PPU**, bilinear, pivot **(90,18)**. No pose is independently rescaled to its alpha bounds. Root coordinates are manual visual registration estimates, not motion-capture or world-space contact measurements.

- Source-to-import scale: 180/640 = .28125; source-to-world scale: .28125/128.
- Front idle visible height at alpha>16: 426 source pixels, approximately **.936 world units**. Rear idle: 424 pixels, approximately **.932**. The .9375 previous canvas height is the approximate visual target, not an assertion that all poses must be equally tall.
- All pixels with alpha>16 lie inside the retained rectangles. The rectangular isolation omits 341 front-sheet and 757 rear-sheet pixels whose alpha is only 1–16; source PNGs are unchanged. Rectangular cropping/padding and ordinary Unity sampling are the only technical image operations.
- The 180x180 sprite includes generous transparent padding. Its full bounds are 1.40625 world units high; a future runtime bar must **not** use that padded top as the creature's visual top. Existing Grunt bars are deliberately retained in this fixture.
- The raised fist almost touches the current bar. Revisit visible-height/bar clearance during integration rather than treating this capture as final acceptance.

## Motion limits and integration implications

Both walking contact configurations exist, but proper passing/down poses and world-space planted-foot control do not. The browser uses idle as an explicitly temporary passing pose. There are shape/volume changes between poses; the rear view is still too square-on for a fully accepted diagonal facing set. Mirror controls expose the composition only; they do not solve lighting or anatomical handedness across views.

The windup → strike → recovery sequence demonstrates the same-arm action and readable silhouette. **Do not insert a windup delay into combat from this art study.** The current attack event deals damage immediately, and `EnemyArtSet` supports four poses per side plus two deaths. Integrating this six-pose proof requires an explicit presentation/timing choice; a genuine anticipatory telegraph would need gameplay/simulation agreement. No such change is included here.

No hit/death assets, final gait, runtime animator, art-set import, shared cache change, health-bar change, shader, prefab, scene save, APK, device input or profile/telemetry modification was made. The phone remains on the preceding contact/grip build; inspect its current state afresh before future device work.

## Reproduce

With the pinned Unity editor closed and project access coordinated:

```powershell
pwsh -NoProfile -File Tools/ArtReview/Run-GruntMotionProof.ps1
```

The runner stages unchanged RGBA channels, installs one temporary PlayMode fixture, checks pure tests and compilation, renders under an isolated temporary profile, removes its own fixture/meta and converts rendered BMP evidence to PNG losslessly. Retained sources are `Tools/ArtReview/GruntMotionCapture.cs`, `Run-GruntMotionProof.ps1` and the new `-GruntMotion` path in `Convert-CharacterProof.ps1`. It does not save the scene or build/install an APK.

## Verification

- .NET **272/272**; compile **0 warnings / 0 errors**.
- Graphics-enabled PlayMode capture **1/1**: common sample dimensions/pivots, valid source bounds, ten real enemies/two Grunts, no unexpected logs. Twelve pose captures and four room captures retained.
- Static integrity **375 GUIDs / 555 scene objects/components** after fixture cleanup.
- Original source hashes, alpha coverage and capture inventory checked. Browser layout/controls reviewed at desktop and 390x844; all displayed images loaded with no horizontal overflow.
- Full EditMode **284/284** and PlayMode **96/96**, then static integrity **375/555**, passed. APK explicitly skipped because this is an isolated visual proof with no authored runtime changes. No new APK or physical-device validation applies to this slice.

Next: refine the facing and contact/transition poses, resolve the bar clearance at fixed visual height, then a Grunt-only runtime import with shared ownership and hit/death presentation. Keep immediate damage timing and balance unless a separate gameplay change is explicitly agreed and covered by simulation parity.
