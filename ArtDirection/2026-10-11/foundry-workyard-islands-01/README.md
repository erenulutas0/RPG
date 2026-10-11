# Foundry workyard islands — actual Unity proof

Baseline: `93f9f27`. The selected before images and diagnostic/XML files were copied unchanged from the accepted `foundry-north-silhouette-01/after` directory. Source and destination SHA-256 values were compared during copying and recorded in `validation/provenance-sha256.txt`. These are historical baseline observations, not a new execution.

Approved visual reference: `../../2026-09-23/foundry-layout-01/foundry-v1.png`.

## Integrated scope

Two purposeful work islands now occupy the northwest court: a loaded material trolley at u[-26,-22], v[-4,-1.8] and an open mould bench at u[-21.8,-18.6], v[11.8,14]. The original turntable connects them as loading, transfer and casting spaces. A new flush southern rail segment joins the trolley to the turntable; rails remain freely traversable. No moving machinery or new interaction is implied.

FoundryRegionProductionYard builds original native iron geometry with the existing material: grounded skids, wheels, frame and warm mill-scale billet stack; four-legged bench, unequal recessed sand moulds, seated patterns/feed channels and clamps. Material identity was refined after the first actual capture. Each full skid visibly marks its collision footprint; all components remain within it. The bench's highest small clamp is 1.51, under the 1.55 depth owner. Two islands reuse the existing iron batch; no new texture, material instance, renderer or updater.

## Shared floor and movement contract

FoundryMap.WorkIslandCount/WorkIslandBound are the sole rectangle source for art and movement. Cell/UncutCell and SurfaceContains retain the continuous physical floor. Contains and Segment reject the same actor-margin-expanded rectangles analytically, with 1e-4 conservative relief for float round trips. Long input cannot tunnel; existing sliding and spawning inherit the common queries. FoundryPursuit adds four derived .70-clearance corners per island to its static visibility graph (16 to 24 nodes), with no per-frame allocation introduced.

Three surface-only art queries and exterior-prop test queries explicitly use SurfaceContains, so blocked equipment cannot masquerade as a hole or as exterior ground. Existing camera points, north passage and pit circulation remain open. Pure tests cover each island's occupancy versus paving, long/corner/grazing sweeps, sliding, bidirectional pursuit and four-wave ten-enemy entries. The existing full scene test now inspects actual floor triangles and low grounded iron vertices at the shared corners, including depth/cutaway flags. This is a map-collision change required by the new art; progression, combat balance, scene, camera, profile and telemetry formats are unchanged.

The originally considered western trolley location was rejected before implementation because it overlapped an existing route anchor and left a narrow west lane. The final location preserves a four-unit west-side corridor. No existing route assertions were removed or loosened.

## Capture contract

The runner retains all preceding standard cameras and diagnostics. The eight matched gallery views are `overview`, `full-map`, `yard-detail`, `workshop-detail`, `crane-detail`, `expanded--27-6`, `expanded--26-15`, and `casting-portrait`. Remaining standard shots are extra local evidence unless separately selected for tracking.

Three additional after-only views have no matched baseline: `islands-yard-wide` (projected centre u=-22.5, v=6, orthographic size 10, square frame), `islands-trolley-portrait` (hero u=-24, v=-6), and `islands-mould-portrait` (hero u=-20, v=10). The gallery labels these separately rather than implying before/after equivalence.

The runner explicitly selects the Android build target. Cavern texture target/import settings, loaded format and memory, native distant-material atmosphere/RGBA/UV ranges, and region budgets are retained. These are Editor observations, not device performance measurements. Images are actual staged Unity captures with HUD hidden; portrait shots use the production camera width. They are not phone screenshots.

## Reproduction

```powershell
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryWorkyardIslandsProof.ps1 -Phase after
```

The phase parameter selects output only; it does not restore old source. The runner temporarily installs its test source/meta and removes them in `finally`. Root owns Unity and ADB execution.

## Review and verification

Two actual Android-target after captures passed; first XML and final graphics XML retained. Root reviewed the complete map, wide workyard and trolley portrait: the two interior masses are visible at whole-map scale, feet remain readable, and load versus mould function is clearer after the material refinement. Independent final review is in `validation/independent-art-review.md`; final suites/build identity are in `validation/verification.md`.

Observed region: 32 renderers, 120,677 vertices, up 1,860 (about 1.57%) from 118,817. Existing distant works remain 2,496 vertices. These are Editor resource counts, not device frame-time evidence. Native geometry remains visibly simpler than the approved reference's authored equipment; this is a coherent layout step, not complete reference fidelity.

Existing matched camera points remain walkable. Three helpers split shared navigation/tests, proof/tooling/contact review and independent art review; root implemented the visual kit and exclusively ran Unity. No phone action or fresh device observation occurred. Earlier concrete owner-run restart/install decision remains pending; later device work requires fresh preflight/current-file backup. Do not restore stale saves. Physical-device movement/crowd/midrange/first-ramp/motion acceptance is open. New encounters, bosses and progression remain deferred.
