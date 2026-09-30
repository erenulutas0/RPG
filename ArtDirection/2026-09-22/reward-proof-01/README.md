# Ground reward source and browser study — 2026-09-22

First presentation slice after the owner's September 22 handoff: a small painted cyan crystal with round brass coins, representing one GroundDrops.Drop carrying both experience and gold. Source is original built-in image_gen output, copied unchanged; exact prompt in PROMPTS.md, dimensions/alpha bounds/SHA-256 in asset-metadata.json. No image API/CLI fallback was used.

## Deliverables and provisional contract

- reward-v1.png: 1254x1254 RGBA, transparent corner; SHA-256 `2556ee74e8f5a0a44524bf4dfa9b9ddc241a08a5b898f21bc7c8ec08320575f5`.
- Alpha>16 bounding box (left,top,right-exclusive,bottom-exclusive): (219,138,1054,1122). Weak exterior alpha reaches (162,57,1234,1200). No alpha thresholding, crop, resizing or painting was performed on the saved master.
- Provisional floor root (636,1122), full-canvas pivot (636/1254,132/1254). Root is an artistic registration estimate and still needs Unity contact/shadow verification. Browser uses the full source with one fixed scale; no per-frame auto-fit.
- Preferred study size: 0.45 visible world-unit width, 54 pixels in a 1080-wide / width-9 camera; compare 0.35–0.60. The source is taller than wide, so its visible height is roughly 0.53 units. It is a small collectible, not a pickup-radius decal.
- VISUAL_PLAN.md covers the four priority systems: ground rewards, walked exit, finale clock/pressure and pending cards, with sizes, frame budgets, API concerns and acceptance checks. Only the reward source and its browser animation are produced in this slice.

## Preview and limits

Serve ArtDirection as root (`python -m http.server 8342 --bind 127.0.0.1 --directory E:/MobileGame/ArtDirection`), then open `http://127.0.0.1:8342/2026-09-22/reward-proof-01/`.

The backdrop is the unchanged September 19 `daggers-runtime-01/runtime-room.png`, a historical actual Unity render. The browser places illustrative rewards over it; it does not show the new ground-drop code running. Five, eighteen and thirty-six samples test visual density. Grayscale, size and an optional exact 2:1 projected one-unit pickup contour are review controls, not planned runtime UI. A nearby sample flies over 0.22s; all remaining samples can fly over 0.42s. Actual gameplay rewards must remain immediate. Reduced-motion mode removes flight.

Observed: the blue crystalline top and round gold base remain distinct in the scaled portrait preview; 36 samples visibly crowd the floor. No idle bob/glow was added. Dense occlusion is unresolved because a flattened screenshot cannot put rewards behind actors, and the test layout is illustrative rather than a recorded kill distribution. The bright material may need adjustment after real depth/crowd sampling. No final device-readability or performance claim.

Current source represents both currencies together. A zero-component drop needs a separate component/variant in any general-purpose production view. Current GroundDrops has value-based events, no ID and no sweep flag; integration must handle coincident drops and game-time pause correctly without changing game rules.

## Verification

`node --check preview.js` passed. Headless Chromium browser checks passed: assets load, only in-radius sample picked up, floor sweep completes, reset restores, 36-drop grayscale/radius view works, 390px layout has no horizontal overflow, reduced-motion clears immediately, no page/HTTP errors. `browser-verification.json` records checks; preview-desktop.png, preview-density-grayscale.png and preview-mobile.png preserve captures. Desktop and dense grayscale captures were visually reviewed.

Reproduce with `node verify-preview.cjs` while the local server is running and Playwright is available on NODE_PATH. ART_PROOF_CHROMIUM optionally points to an existing Chromium executable. This machine used bundled Playwright and the installed Chromium 1243; default Playwright expected an unavailable 1234 binary, so the explicit executable override was used. No dependency or browser installation occurred. The in-app browser automation failed at kernel initialization; a separate headless browser performed the checks and the Codex browser-open request was queued.

No authored Assets, runtime C#, gameplay, scene, project settings, packages, APK, phone, save or telemetry changed. No Unity/game suite was rerun for this source-only deliverable. Previously untracked ArtDirection/2026-09-14/DESIGN_REVIEW_2026-09-15.md was left alone. No commit or push.

Static project integrity also passed: 478 unique asset/folder GUIDs, 1099 package GUIDs, 568 scene objects/components, resolved references and valid JSON. `git diff --check` passed. Mobile full-page capture was visually reviewed; controls remain within the 390px viewport.

Next bounded slice: actual Unity sampling of this fixed source/pivot, then shared import and a pooled GroundDrops presentation with collection/pause/duplicate/reload coverage. After the required runtime gate, coordinate a fresh device review; do not treat the September 19 phone state as current. Exit art and HUD follow the plan separately.
