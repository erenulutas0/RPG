# Staff splash art proof — 2026-09-19

An isolated four-arc violet/white Staff impact candidate, compared with the current thick blue pixel blast in the actual ten-enemy Unity room. Preferred direction: tapered arcs with a clear centre, beneath actor groups. Four arcs and no lozenges distinguish it from the hero's eight-arc blue ability seal. This is a presentation proof, not an integrated runtime replacement.

## Scope and contract

- Tools only: StaffSplashCapture.cs, Run-StaffSplashProof.ps1 and the converter's StaffSplash destination. No authored Assets, scene, weapon data, damage, movement, cooldown or profile changes.
- TestProfile seeds a disposable Staff loadout and density proof. Enemy attacks/movement are frozen. The actual CombatEffectsView.OnHeroStruck presentation subscriber is invoked directly for each comparison; no weapon damage is applied and no gameplay outcome is claimed.
- Old view uses its three 70ms frames. Candidate temporarily borrows seven generated 512x256 RGBA32, bilinear/clamped, non-readable sprites at 30ms/frame in the same pool, explicitly retaining the old 0.21s lifetime. Sorting changes from order5 to ground0 in the isolated test only. No authored scene is saved.
- Candidate radius eases from24/38 to1 of the old shown radius over0.14s, then fades in the last0.11s. Sprite contour radius240 pixels maps to38/32 world units before the existing RingScale is applied. The authored Staff still has3.5 gameplay splash radius; its shown impact maximum radius is1.75 (old splash fraction0.5), not a full damage-range indicator. Transparent padding is not reach.
- Colours interpolate violet(0.44,0.23,0.9) to pale violet(0.91,0.79,1), with four tapered angular sectors and restrained halo. Original code-native contour source is retained in CreateContour; no external or generated raster artwork was used.
- Seven textures are temporary proof overhead (3,670,016 raw RGBA bytes); cleanup destroys all owned sprites/textures after scene unload. Do not ship this frame allocation. Integration should generate/cache one immutable contour and animate scale/alpha on existing pooled renderers.

## Evidence and verification

Ten unchanged Unity renders: room; old/new at0,5,9,14 simulation frames; candidate at1080x1920 as well as1080x2340. Time.captureDeltaTime=1/60; pauses between captures. Approximate elapsed labels describe frame steps, not measured device timings. Old and new are sequential staged comparisons, not identical simultaneous animation states. Body idles may differ between the two passes. The proof checks Staff radius, fixed impact origin and disappearance after lifetime. Initial/middle/end images were inspected: character silhouettes remain unobscured and the new contour is absent at the end. Crowded actors naturally hide some arcs.

Reproduce: `pwsh -File Tools/ArtReview/Run-StaffSplashProof.ps1`. The runner stages/removes its exact temporary test/meta, runs .NET and compile checks, runs graphics PlayMode capture, then losslessly encodes BMP to PNG. sha256.txt records the PNGs.

Verified: .NET291/291; compile0 warnings/errors; graphics PlayMode capture1/1; integrity440 GUIDs /568 scene objects/components. The first compile caught CS0136 (local attack name shadowing); renamed the loop variable and reran successfully. Full EditMode/PlayMode suites and APK were not repeated because authored runtime is unchanged. The prior runtime gate306/111 and installed ability-pulse build66CE2B7B2CB39E273C2DDC35B5795EEA70E518B65FF40BD50F0A00CD87D94165 remain the baseline, not verification of an integrated Staff candidate.

No ADB enumeration, input, capture, install, equipment change or save/telemetry access occurred in this slice. Phone state was not inspected. The previous device record remains historical and must be refreshed before future use.

## Next bounded slice

Integrate a single shared contour and continuous pooled transform/fade; keep impact-only size semantics, target origin, immediate damage, 0.21s lifetime and pause. Test pool reuse after Staff/ability effects, reload ownership, expiry and actual Staff attack/parity. Run full gates/build and a coordinated physical-device review with owned Staff; restore the user's loadout through UI if changed. Remaining Staff weapon sprite is still procedural; this effect proof does not replace it or establish sustained performance.
