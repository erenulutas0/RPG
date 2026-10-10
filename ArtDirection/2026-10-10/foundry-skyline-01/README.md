# Distant furnace architecture — Unity comparison

Baseline: `ecea0e8`. This slice develops the two north skyline towers at design anchors `(-5,39)` and `(12,42)`: recessed arched furnace bays on both visible faces, stepped shoulders and stronger bearings/parapets on their existing broken span. The aim is more convincing industrial architecture behind the casting workbay, while retaining the subdued indigo atmosphere and fading lower shafts.

These changes concern scenic architecture. The existing walking boundary, camera, workbench placement and gameplay are outside this slice. The approved concept remains an art target, not evidence that the runtime already matches its detail or proportions.

## Comparison evidence

The gallery switches the matched `overview.png` between before and after, with the approved `foundry-v1.png` concept as a separate reference. `full-map.png` provides the complete map. `skyline-detail.png` isolates the tower/span treatment. `casting-portrait.png` shows its effect from the north walking lane at production camera width, with a staged actor and hidden HUD.

All cameras in this proof are matched between phases, including `casting-detail.png` and `casting-portrait.png`. This differs from the earlier workbay proof, where those casting views were after-only. Every image in the before/after folders is an actual Unity capture; none is a phone screenshot or a synthetic before/after composite.

## Verification status

Actual Unity visual review is accepted by root and an independent helper. The first after capture was rejected for weak arch definition and long uniform orange strips. The final version adds projecting dressed arch rings, chamfered jambs, foot/impost shoulders and three short gradient furnace panes separated by real masonry and glow-free gaps. The rejected first detail is retained as `validation/first-skyline-review.png`.

Region renderers remain 32; vertices increase from 115,106 to 116,326 (+1,220, about 1.06%). The two distant batches contain 2,132 masonry and 144 fire vertices, totaling 2,276 within the unchanged 2,500 test ceiling. No new texture, material or per-frame component. Existing ranks -20/-19, borrowed textures, owned atmosphere material, .62 atmosphere factor and fading lower shafts remain unchanged. These are geometry counts, not performance measurements.

PASS: 409 pure .NET, 446 EditMode and 154 PlayMode cases (1,009 distinct-suite tests, zero failures/skips). The focused distant-works scene test also passed and is included in the full total. It covers static placement across camera changes, layer ordering, culling bounds, geometry budget and owned-resource cleanup. Compile: 0 warnings/errors. One before and two successful after graphics captures; baseline/final-after XML retained. Final static integrity and APK identity are recorded in `validation/verification.md`. No phone installation is claimed.

Review priorities: arches must follow the real tower faces; shoulder and bridge joins must remain connected; the darker structures and subdued furnace openings must sit behind the playable foreground. Check both the full-map composition and the normal-width casting portrait. Lower tower ends should still disappear into the haze without hard floating cutoffs.

The architectural gain is clearer in the skyline detail and overview. The casting portrait crops the arch heads, so its improvement is modest; the shafts intentionally remain subdued behind the playable foreground. Full reference richness and varied building proportions are not complete.

## Reproduction and device limits

With Unity closed, run `pwsh -NoProfile -File Tools/ArtReview/Run-FoundrySkylineProof.ps1 -Phase after`. Phase only selects output and does not reconstruct historical source. The temporary capture file is removed in `finally`. Source: `Assets/_Project/Scripts/UI/Arena/FoundryRegionDistantWorks.cs`. Native mesh construction, no image editing or new external assets.

No ADB action or fresh device observation this pass. The earlier concrete owner-run restart/install decision remains pending. On approval, take a fresh preflight and backup, install/hash-check the latest accumulated package, review map and normal combat, preserve current original saves/telemetry and remove only new diagnostic flags. Never restore stale saves. Physical-device/crowd/midrange/first-ramp acceptance remains open. Region encounter/boss progression stays deferred under the owner's art-first direction.
