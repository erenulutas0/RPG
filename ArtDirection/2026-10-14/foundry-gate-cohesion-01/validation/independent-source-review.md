# Independent source review — gate cohesion

Read-only review against baseline `3474e35`, 2026-10-14. No Assets edits, Unity, device or Git operations by this reviewer. Initial scope covers the root gate-local field, its production call sites, the fitted inner-jamb implementation and their new tests. Curtain implementation and merged-image acceptance are pending a final freeze.

## Initial finding

No runtime blocker found in the inspected root/jamb scope. A focused test-strengthening recommendation was sent to root: assert the actual gate UV field independently inside triangles, including the nonzero gate datum, rather than only patch bounds/coincidence and fold locations. The generic stone-grain suite already tests interior UV and colour interpolation, but its datum is zero and does not alone prove the gate wrapper's phase.

## Root gate-local grain

`GateHouseGrain` wraps the existing `StoneDetailGrain` with one datum at projected `(GateU(32),-3)`, screen-right axis, protected atlas rectangle `(.04,.44,.39,.564)` and density `.075`. It is applied once after complete construction of the carved gate, active curtain walls and each outboard landing shoulder, before `Create`. Shared `GateRecessGrain`, material/texture assets, fallback outer buttresses, distant works and generic tier emitters remain unchanged.

The extra vertices are fold intersections. Original vertex positions and colours remain intact; inserted colours interpolate the original triangle field, while triangle replacement preserves original painter sequence. No new texture, material, renderer, shader or updater is introduced. This is a shared projected physical field consistent with the existing fixed-view fortress treatment; it is not a full world-space volumetric stone material. Mirrored grain periodicity remains an image-review risk.

Production ownership is sound: the carved gate is created inside `BuildGate`'s explicit `DepthStructure`; curtains and landing shoulders have their own `MassDepth` scopes. `Create` assigns that owner to every appended vertex. Thus the new vertices do not depend on a shifted unscoped component centroid for landmark ownership. Geometry finishing precedes depth assignment, and the animated-metadata guard in the unchanged grain helper prevents accidental use on flames or cloth. Existing light/contact values are retained rather than sampled through a new global lighting pass.

The three new surface cases build both actual inner-return variants and the actual header. They verify original positions/colours, total coverage, UV bounds, equal UV at coincident points, real fold subdivision and a bounded additional vertex count. Their fold tests inspect geometry independently of the production mapping helper. Add the independent interior UV check above to distinguish correct geometry from a wrong texture phase/assignment. These are source observations, not test-execution claims.

## Inner-jamb construction

Each existing six-plane L-shaped jamb now has four broad fitted courses and three true mortar beds. The old full-face backing beneath three overlay rows is removed. Course levels register around the same L-section. The indexed stone emitter is reused unchanged; exact section corners, front-to-back face order, bottom/top stations, base/upper supporting tiers and two cap rectangles remain. The missing concave cap region stays empty. Grain maps the complete face once before the later parent finish.

Six new tests inspect the actual builder: projected area and strict interior single ownership, bounded envelope, independently reconstructed L-section/cap stations, production inclusion between unchanged base/upper pieces, continuous local UVs, nondegenerate visible triangles and independently owned dark mortar. Production inclusion uses the same builders for unchanged supporting pieces, while the critical new L-section, coverage and cap assertions have independent geometry oracles. No existing geometric tolerance or 400-vertex jamb guard is relaxed.

The helper records a real local tradeoff: each jamb changes from 392 vertices/196 triangles to 272 vertices/280 triangles before parent finishing. The fourth course reduces duplicate vertices but increases indices. Final merged subdivision may outweigh that vertex saving and must be reported from capture, not inferred from the local count.

## Pending acceptance

Review the frozen curtain construction, final emitted atlas field and actual matched gate/crown/full-map/portrait views. Require retained aperture/footing/door/banner/pipe relationships, coherent grain scale, quiet recesses and no visible mirrored streaks or atlas seams. Full suites, resource totals, APK and device checks remain parent-owned gates and are not asserted here.

## Frozen curtain and strengthened-field follow-up

The curtain helper has now frozen its new active builder and six cases. The unchanged lower prefix and bearing/cap stations remain; only the upper front uses the newly recessed construction. Its throat retains the former relief stations, while the backing moves 0.18 design units inward. Backing is emitted first, then returns, partitioned outer stock and fitted frame. The former whole front plane and flat relief overlay are not also emitted. The inherited colour field is interpolated over the whole parent face rather than restarted on individual pieces. Closed chamfer and original upper-tier cap triangles remain. The outer baked buttress fallback and shared helpers are not modified by this curtain scope.

The upper coping now has a shallow pitched roof, 0.08 lower eaves inside the old envelope, original 6.82 bearing and 7.02 maximum, closed gables, three broad rows and separately owned mortar. There is no retained full flat roof beneath the pitched planes. The new tests independently reconstruct the opening, clip the visible backing area against the throat, require later overpaint below 1e-8, test exact retained stations and supported roof bearings, and sample roof/gable ownership. Those pre-finish geometry checks are materially useful; the merged parent finish and image gate remain separate.

Root also added the requested asymmetric barycentric UV witness to each new gate-surface case. It computes the translated mirror field with independent double arithmetic from the actual sample position, and compares interpolated rendered UV to that expected value. It does not call the production grain helper to calculate the expected field. This closes the identified phase/assignment blind spot without changing the existing geometric checks. No current source blocker found; tests and native image acceptance remain pending.

## Inherited-source and compile-repair clarification

Independent SHA256 comparison with the prior accepted gate-depth source freeze confirms `FoundryRegionGateRecesses.cs`, `FoundryRegionStoneGrain.cs`, `FoundryRegionGateHeader.cs`, `FoundryRegionDistantWorks.cs`, `FoundryRegionGateMasonry.cs` and `FoundryRegionMassing.cs` are unchanged. The quarter-depth shaft throat and face-dependent throat-return lighting observed during review are inherited behavior, not additions by this package. The new grain wrapper changes their final UV field only at the selected completed gate builders. This corrects the review's earlier scope query; it is not a newly discovered rendering defect.

Root's first test compile failed because the new surface oracle referenced internal `FoundrySurface.Project` from the test assembly. The corrected test independently reconstructs its datum using public `FoundryMap.ToFloor` and the existing half-Y plus 1.25 plateau projection. This preserves the expected physical field while avoiding that inaccessible method and improves oracle independence. The rejected test source/log remain in `rejected-test-access`. No runtime repair was needed for this test-access failure; focused/full results are not asserted here.

## Final material correction review

The initial rectangle `(.04,.44,.39,.564)` described above is superseded by `(.04,.44,.21,.564)`, retaining the .075 physical density and shared gate datum. This excludes an authored atlas drip which the first native capture exposed conspicuously across the crown. The final test oracle changes its horizontal patch width from .35 to .17 and its upper U bound to .21; independent interior interpolation and geometric checks remain. No shared grain helper, texture, material or rank changes are involved.

I reopened the exact final wrapper/test and four corrected native views after their 18:08:49 capture. Image acceptance is now recorded in `independent-image-review.md`, with the initial rejected capture retained. Measured region geometry is 169119 vertices with 36 renderers: +4447 vertices from baseline, including +867 over the rejected wider patch. No new source blocker found. Final full-suite/build execution remains parent-owned and is not claimed by this review.
