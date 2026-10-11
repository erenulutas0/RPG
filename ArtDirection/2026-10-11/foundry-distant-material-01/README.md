# Foundry distant material — actual Unity proof preparation

Baseline: `e98d4ac`. Before images and accompanying metadata/XML are the accepted final `ArtDirection/2026-10-11/foundry-backdrop-depth-01/after` evidence, copied unchanged. Every copy was SHA-256 checked against its source; see `validation/provenance-sha256.txt`. These are reused historical captures, not a new before execution.

Target remains `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`. This slice evaluates whether the native distant towers and broken service arch read coherently beside the painted foundry backdrop. The native surface implementation is integrated; final review and verification are recorded below and in validation/verification.md.

## Matched capture contract

`Tools/ArtReview/FoundryDistantMaterialCapture.cs` retains all standard views from the previous accepted proof, including overview, complete map, skyline detail, bridge foot, workshop/detail views and production-width portrait approaches. The gallery focuses on overview, full map, skyline detail, gate/workyard portraits, bridge foot and casting portrait. Actors are staged and HUD hidden for comparison; these are Unity graphics captures, not physical-phone screenshots.

The seven versioned gallery views are `overview`, `full-map`, `skyline-detail`, `expanded-30-5`, `expanded--26-15`, `bridge-foot` and `casting-portrait`. Other standard views remain local review output; they are not part of the selected versioned image set. Budgets, diagnostic metadata and the baseline provenance manifest accompany the selected images. The manifest records every local baseline copy checked when this proof was prepared, including views retained only locally.

The runner explicitly passes `-buildTarget Android`. Texture reporting retains actual cavern format/graphics format, dimensions, mip count, readability, Editor target/platform importer settings and memory observation; region and distant geometry budgets are also recorded. Requested importer configuration is not a substitute for observed loaded format. Such Editor observations do not establish physical-device memory or frame-time performance.

`distant-material.txt` separately inspects the native distant mesh batches: scene-owned masonry material identity and atmosphere value, each batch's material/shader/rank and vertex count, finite/nonfinite RGBA and UV entry counts, and component minima/maxima. It reads shared materials without creating extra instances. This native material report is distinct from the painted backdrop's `cavern-texture.txt`.

## Reproduction

With Unity closed: `pwsh -NoProfile -File Tools/ArtReview/Run-FoundryDistantMaterialProof.ps1 -Phase after`. Phase selects the output folder only; it does not restore old source. The runner installs a temporary PlayMode capture source/meta and removes both in `finally`. Root alone operates Unity and ADB.

## Verification status

- Actual Android-target Unity capture passed; independent review is recorded in validation/independent-art-review.md.
- Final suites, static checks, build identity and the initial telemetry-test failure are recorded in validation/verification.md.
- Browser view selector and before/after switching verified; visible comparison images loaded successfully.

No phone operation is performed by preparing these tools or copying historical evidence. Device state must be inspected fresh if root later operates it. Full reference fidelity and broader workyard composition remain incomplete; gameplay and progression scope is not changed by this proof.

## Implementation

One runtime partial changes: FoundryRegionDistantWorks.cs. Existing native shafts and broken arch retain their positions, outlines, transparent lower fade and opaque upper tiers. The borrowed wall texture now uses a finer 2.1 by 1.8 facade scale; cap surfaces use the same smaller design-plane scale. All bridge front segments share the full span course direction instead of deriving it from the changing curved underside. Recess facade mapping likewise retains one common direction. Geometry and triangles are unchanged.

Cooler neutral base stone, restrained height-based weathering and face lighting reveal existing raised arch frames and shoulder volumes. Only the scene-owned distant material changes its atmosphere blend from .62 to .44; the shared cavern shader and painted backdrop are untouched. Furnace slit geometry and brightness remain unchanged. No new texture, renderer, material instance or per-frame component is added. Actual region remains 32 renderers / 118,709 vertices; distant batches remain 2,244 masonry + 144 furnace = 2,388 vertices under the 2,500 ceiling. This is not a performance measurement.

No camera, walkable map/ramp, gameplay, scene or save changes. Existing lifecycle/rank/budget tests remain; the material expectation is updated and invalid UV coverage strengthened. One actual after capture ran with Android as the active target. Before images are reused historical evidence. Three helpers split runtime implementation, proof/gallery tooling and independent visual review; root exclusively ran Unity.

## Review boundaries

Root reviewed actual full-map, skyline and production-width workyard portrait. Arch reveals and coping separate more clearly, the enormous dark blocks carry finer stone detail, and the playable court remains the visual foreground. Whole-map improvement is restrained; tower silhouettes and masonry remain too regular to claim the reference has been matched. Broader workyard arrangement and richer authored structural shapes remain follow-up work. Physical-device and motion acceptance are still open.
Independent review accepted all seven views without a new visual blocker. Next substantial work is a coherent northwest workyard composition with explicit circulation/blocked-footprint contracts, or (if navigation stays frozen) authored replacement silhouettes for the native north shafts and broken span. Avoid another isolated tint/trim iteration.

The initial full gate exposed an old chest telemetry test that capped health at100 despite a possible MaxHealth book. A deterministic regression and exact event-time HP checks repair the test without gameplay changes. See validation/chest-test-repair.md and the preserved failing XML; final suites are rerun before building.
