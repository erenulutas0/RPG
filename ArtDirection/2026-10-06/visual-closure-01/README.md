# Foundry visual closure — 2026-10-06

## Scope and evidence

Five requested agents covered worked anvil steel, enclosed lantern panes, foundry backdrop cohesion, portrait gate framing and a remaining-art audit. Parent combined and reviewed actual Unity captures. The phone was unavailable: no ADB access, installation, profile modification or device performance measurement in this session.

Runtime changes:
- `FoundryRegionWorkshop.cs`: continuous neutral iron sampling across the anvil casting, head, horn and resting hammer; material-specific working-face highlights replace framed atlas artifacts.
- `FoundryRegionSite.cs`: the existing lantern pane quads sample a quiet orange patch with an upward soot gradient. Final orange/amber tint supersedes the first, overly yellow trial. Cage and exposed flames remain unchanged.
- `VoidBackdropView.cs`: foundry-only cavern tint `(0.84, 0.87, 0.91, 1)` slightly reduces background competition. Ordinary halls retain white tint.
- `Tools/ArtReview/FoundryRuntimeCapture.cs`: four HUD-on portrait approach captures with walkability and settled-camera assertions. Test profile isolation remains in place. No runtime camera change.

No new textures, materials, renderers or geometry. Static budget remains **17 renderers / 80,316 vertices**, matching the installed October 5 baseline. Gameplay, scene, collision, movement, balance and HUD are unchanged.

## Review

Open [comparison page](index.html). The `before/` images are the October 5 `foundry-structure-02` baseline (HEAD `5d9cea6`); `after/` images are final actual Unity captures after the lantern tint correction. Five matching views cover overview, bridge, workshop, gate and furnace. [Before budget](before/static-budget.txt) and [after budget](after/static-budget.txt) accompany the images.

The anvil is more coherent, though still visibly faceted and smooth rather than richly painted. Lantern panes now read as enclosed amber glass, distinct from exposed fire. The background still carries warm depth. These are incremental cohesion improvements, not a claim that the target concept fidelity has been reached.

The four portrait images are staged positions at 1080 × 2340, with HUD enabled and production 9-unit follow width. See [coordinates](after/gate-approach.csv) and [framing review](portrait-framing-review.md). Centring improves the door/cloth/seal read, but the tall gate still exceeds the view and crosses the top HUD. A deliberate camera/HUD investigation remains separate; these captures are not walking, combat or phone evidence.

## Verification

- Fast runtime compile: zero warnings/errors before final tint-only correction.
- Final actual graphics capture: 1 passed; final screenshots inspected.
- Static project integrity: 534 asset/folder GUIDs, 1099 package GUIDs, 568 scene objects/components, passed.
- Final full Unity gate: **410/410 EditMode**, **135/135 PlayMode**, Android build exit 0.
- Final APK: `Builds/Android/Cryptforge-Prototype.apk`, 32,374,745 bytes, SHA-256 `74297A6B721A4251F7C0E07E06AD3F8E1CD03848B984DD54955CDD7F4680B978`. Built, **not installed**.
- Read-only combined review found no new blocker. Local preview returned HTTP 200; all 32 page references resolved. No push.

## Remaining work

See [art audit](art-audit.md). Highest-value next decisions are broad material cohesion and gate composition, not another layer of tiny hardware. Full reference fidelity, dynamic combat readability, device texture appearance and midrange performance are not established by this package.

When the phone becomes available, install the final APK after checking current run state and taking a fresh backup. Review amber glass, steel face, backdrop depth and gate approach while moving and in combat. Compare profile/development-file hashes and clean only session-owned diagnostic flags. Do not reuse an old backup or prior-package FPS as current evidence.
