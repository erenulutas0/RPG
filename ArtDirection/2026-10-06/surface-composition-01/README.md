# Foundry surface composition — 2026-10-06

## Completed scope

Continuation of `7e2332b` (visual-closure-01), with no phone access. Existing agents handled disjoint floor, weathering and test-capture work. Parent reviewed and integrated actual Unity images.

- `FoundryRegionFinish.cs`: the existing stock-soot distance now covers the actual anvil/stock shelf `(-23,5)..(-18.45,8.4)` and pipe/quenching shelf `(2,18.5)..(7,22.4)`. Same 2-unit falloff, 17% maximum darkening, deterministic patina, shared light field and colour ceiling. Existing gate lane, boiler contact and central combat floor retained. This is a stale-footprint correction, not a newly added wear system.
- `FoundryRegionWeathering.cs`: architectural deposit sampling frequency 0.83 to 0.31; multiplicative range 0.87–1.035 to 0.905–1.0. Midpoint stays 0.9525; individual samples change and no identical overall image brightness is claimed. Painted stone texture supplies fine variation while the overlay becomes broader and calmer. Fire height, contact shade and roof treatment are unchanged.
- `FoundryRuntimeCapture.cs`: same-pose production / +1.5 camera Y / 11-unit lens comparison at centred gate, with camera/follow restoration in `finally` and assertions. Test-only; runtime camera, HUD and gameplay unchanged.

## Accepted visual evidence

[Comparison page](index.html) includes matched overview, gate, bridge, workshop and two portrait route views. `before/` is the prior October 6 visual-closure state; `after/` is this final combined capture. The surface improvement is subtle. No new dark hole, hard stain boundary, actor contrast loss or shadow merging was found in these views. Renderer and vertex counts remain **17 / 80,316**; see [before](before/static-budget.txt) and [after](after/static-budget.txt).

[Framing study](framing-study.md) and [CSV](after/gate-framing-study.csv) document why neither simple alternative is shipped. Upward framing lowers the hero 180 pixels and sacrifices nominal coverage below; width 11 reduces actor pixel size by 18.2%. Both leave upper architecture under the HUD. Keep the existing combat camera. These staged images cannot establish moving threat readability or device acceptance.

## Verification

- Fast compile: zero warnings/errors.
- Final actual Unity graphics capture: 1 passed.
- Integrity: 534 asset/folder GUIDs, 1099 package GUIDs, 568 scene objects/components; passed.
- Final full gate: **410/410 EditMode**, **135/135 PlayMode**, Android build exit 0.
- APK `Builds/Android/Cryptforge-Prototype.apk`: 32,374,745 bytes, SHA-256 `B2D5A6F5000A1A7EF4099E595E19DA35BB1BF33921F65557ED9F722937B2E42B`. Ready, **not installed**; includes prior visual-closure improvements.
- Read-only combined review found no blocker. Preview HTTP 200; all 33 page references resolved.

## Remaining boundaries

Phone installation, current-package movement/combat visual review, Android texture appearance and performance remain pending. No ADB command, device profile modification or installation took place. Last installed build remains October 5 structure02; prior device measurements must not be presented as evidence for this build.

The bounded surface tasks and simple gate-camera study are complete. Full concept fidelity is not achieved. A full landmark reveal now needs a separate arrival-presentation/HUD design, not more global camera enlargement or added trim. Keep that future slice separate from the accepted surface corrections. No push.
