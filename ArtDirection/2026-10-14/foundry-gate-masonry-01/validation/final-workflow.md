# Work and verification workflow

## Baseline and ownership

Root captured and sealed twenty-one fresh Before views at clean Assets and baseline `d34c56115fc0a771f648baf3601ab740a10b871d` before implementation. The matched set adds a gate-crown view to examine the upper masonry. `baseline.json` records the source identity and evidence hashes.

Two implementation helpers owned disjoint code and focused test files: one the lintel profile in `FoundryRegionGateHeader.cs`, the other dressed enclosure masonry in `FoundryRegionGateEnclosure.cs`. Root owned the central threshold rail exclusion, merged-scene PlayMode assertions and capture fixture. A third helper independently reviews source and actual final images. Implementation helpers cross-reviewed each other's geometry and UV contracts. Root alone runs Unity and Android build tools.

Cross-review checked shared cornice corners and exact cap retention, fascia/cap joint registration, partitioned ownership and uninterrupted grain. It flagged a possible enclosure-light interpolation difference where new triangles cross old cap-fan boundaries; the focused actual-colour assertions remain part of verification rather than assuming acceptance. No gameplay or device work was delegated.

## Preserved initial compilation rejection

The initial isolated compile passed because it compiles project code and tests in one assembly. Unity correctly rejected the new cornice test's direct reference to the internal runtime `FoundrySurface` class across the separate EditMode assembly boundary (CS0122). No graphics case or full suite ran, and no APK was built from that source.

The repair changes the test oracle only: it uses public `FoundryMap.ToFloor` and the production lintel's documented 1.25-unit raised landing. Runtime geometry is unchanged. `first-compile/` preserves the failed Unity log and original source freeze. Its test snapshot was taken after the repair and is explicitly named `repaired-test.cs.txt`; it is not represented as the rejected source. The root source freeze was refreshed before recapture.

## First full suite and test-only repairs

The first full run passed603 of605 EditMode cases and all159 PlayMode cases. The APK was correctly skipped. The two failures were new test-oracle assumptions, diagnosed against actual production geometry; retained failure evidence is in `first-suite/`.

The header test assumed more than twelve rendered indices even for the narrow chamfer. That face intentionally has three quads, exactly twelve indices. Its area, ownership and shared-station checks had already passed. Only the preparatory count guard changed to >=12; all geometric and affine-UV assertions retain their thresholds. The passing production-prefix case measured396 old cornice vertices and820 new vertices (+424).

The coping test rejected float-rounded original boundary points with its dimensionless barycentric cutoff. Stored actual geometry and an independent reconstruction identified a maximum one-coordinate-ULP representation discrepancy, including the cap's existing inverse/projection roundtrip. Only emitted-boundary containment now accepts an original-edge intersection with the previous/next-float coordinate box; strict interior coverage, area, original corners, colour and UV checks remain. Every tested profile now also rejects a real eight-ULP outward counterexample. The header helper independently checked the failing profile using a convex hull and segment/rectangle intersection; all284 emitted vertices satisfied the bounded representation guard and the outward counterexample failed as required.

Runtime source remains unchanged through both repairs. Root refreshed the test source freeze and final capture evidence before rerunning the complete gate. `header-failure-analysis.md`, `coping-failure-analysis.md` and `test-repair-peer-review.md` record the diagnoses without representing the failed run as passing.

## Final capture and regression gate

Root reran pure tests and isolated compilation, then refreshed the full source freeze before the final capture and complete regression workflow. No assertion was removed, no runtime budget was raised, and no APK was produced from a failing suite.

The repaired frozen source produced all twenty-one matched After views and a passing graphics test. Actual region resources are 34 renderers / 163,073 vertices; distant geometry remains 3,572 vertices. The threshold presentation oracle passed on the merged scene. Root's final source freeze identifies the Assets used for these images and the subsequent verification run.

The required regression order is pure .NET tests, isolated compile, full Unity EditMode and PlayMode suites, conditional development APK build only after passing suites, then static project integrity. Final counts, APK hash and acceptance are recorded in the package verification summary when established. This workflow note does not assert that an in-progress suite or build has completed.

The image comparison uses actual Editor output, with unchanged camera/actor staging, paused combat and hidden HUD. It is not phone or gameplay-motion evidence. The independent image review owns visual acceptance and any remaining limitations.

## Device boundary

No ADB, phone interaction, installation or profile/telemetry mutation is included. The prior concrete restart/install decision remains pending in authoritative device notes. Any later deployment requires a fresh device-state check and preservation of current original saves and telemetry; historical snapshots must never be restored as current state.
