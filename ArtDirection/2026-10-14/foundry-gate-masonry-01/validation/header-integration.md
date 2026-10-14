# Profiled gate lintel integration

## Scope

`FoundryRegionGateHeader.cs` replaces only the first continuous lintel emitted by `BuildGateHeaderFinish` (height 6.93–7.22 at the production height 6.5). A real 0.09-unit carved middle fascia sits between sloping lower and upper returns. The three visible faces share their octagonal corner stations, so the chamfer cannot pull apart from its neighbouring wall. The recessed middle is darker than the upper return in the existing unlit renderer.

The outer bottom and upper bearing stations, original cap/coping mesh, corbels, door, locks, iron contacts, camera, material and scene resource ownership remain unchanged. `GateCraftTier` retains its behaviour; its original cap code was extracted verbatim into `GateCraftCap`. Other tower and shoulder calls still use `GateCraftTier`. `GateClipCoping` is unchanged for the parallel enclosure work.

The continuous stone grain is sampled once across each complete three-band face. It cannot restart on individual returns. Existing broad stone courses partition each new band; no old complete wall remains behind them. No new texture, material, renderer, shader or per-frame updater is introduced.

## Verification contract

Five new EditMode cases in `FoundryGateCorniceTests.cs` inspect:

- The actual `BuildGateHeaderFinish` prefix, exact retained cap geometry/UV/colour and a bounded static vertex increase.
- Each of the three visible faces against independently authored 0.09-deep octagonal rings: complete area ownership, interior one-owner probes and rendered shared stations.
- An affine mineral field across each entire carved face and its return joints.
- Noncollapsed texture triangles and the actual middle/upper rendered-colour hierarchy.

Source-derived production delta is expected to be +424 static vertices (old wall 212, new three-band walls 636). The actual test output and final scene capture are authoritative; no Unity was run by this helper. Root owns all compilation, tests, image capture, Android build and device decisions.

## Review risks

The header is a small part of the complete gate. This is a local stone-depth improvement, not reference parity. The exact fascia contrast and the thin upper return need acceptance in the matched `gate-crown-detail`, `gate-detail` and walking approach views. No physical-device acceptance is claimed.

## Cross review

The enclosure helper independently checked the new header geometry and tests: shared octagonal stations, continuous three-band profile, unchanged cap extraction and the one-pass grain field were coherent. No source blocker was found. Portrait-depth readability remains an image acceptance item.

This helper independently reviewed the parallel enclosure replacement. The fitted fascia and cap use matching top-edge stock stations, clipped complete coverage and one shared projected mineral field. No immediate ownership blocker was found. A possible illumination interpolation issue was flagged: new cap triangles can span old cap-fan boundaries even though each new vertex samples the old field; exact interior RGB may therefore differ. Targeted colour witnesses must be inspected before any tolerance change if that risk materializes.

First Unity import found a test-only assembly boundary error: the single-assembly isolated compile could see internal FoundrySurface, but the EditMode assembly could not. The oracle now independently projects the production lintel on its authored 1.25-unit raised landing using public FoundryMap.ToFloor. No runtime source changed; no suite had executed before this repair.
