# Gate architectural hierarchy — source record

Baseline: `7c0f057`. Compared the verified `foundry-forecourt-layout-01/after/gate-detail.png` and `full-map.png` directly with the approved `2026-09-23/foundry-layout-01/foundry-v1.png` before implementation. Root captured a fresh Before set before authorizing Assets edits.

## Intent and implementation

The old outer gate towers read as two square storeys, with a strong waist dividing each short blind recess from a tall blocky base. The reference's gate has longer vertical stone bays rising from a more compact, firmly seated foot. This package replaces that subdivision rather than applying another row of trim.

- `FoundryRegionGateEnclosure.cs`: the two outer towers retain their complete footprint, foundation and 7.35 crown. Their low battered plinth now terminates at a bearing course from 1.58 to 1.84; one continuous recessed shaft runs from 1.84 to 6.55 instead of beginning at 3.63. The obsolete mid-storey belt and applied quoins are no longer emitted on these towers. The existing capital, shoulder and crown remain borne by the shaft.
- `FoundryRegionArchitecture.cs`: each inner jamb's solid rectangular vertical return is replaced by an L-shaped section, with real missing volume along its inner edge, a shaded backing and three broad dressed courses. Back planes emit before front planes. Two rectangular cap patches tile the concave bearing without filling the rebate. Existing base and capital seat the full section. The painted facade plane, image/UVs, banner/door/lock pixels and floor threshold are unchanged. The physical rebate belongs to the stone jamb; this is not a claim that the painted door has moved deeper into the wall.

No `GateHeader`, court, massing, map, ramp, floor, scene, gameplay, material or texture change is included. Existing gate depth scopes and material batch ownership are retained. Four shaft assemblies/niche pairs remain; no new standalone structure or per-frame updater is introduced.

## Validation and resource bounds

The existing `FoundryGateRecessTests.cs` now has four additional cases: both actual outer tower assemblies must stay inside their old footprint/crown silhouette and expose the tall recess through the former waist, and both jambs must preserve their containing volume, painted-facade exclusion and genuinely absent cap corner. Existing tests for recess exposure, closed corner/cap and the 550-vertex shaft budget remain; the two outer-shaft inputs now reflect the real taller production shaft.

Each replacement jamb body emits 392 vertices, including hidden bearing closure and dressed faces, with a 400-vertex guard. The entire outer tower helper has a 2400-vertex upper guard. These are bounded source-level checks, not a combined renderer budget or a phone-performance result. Root must record actual mesh totals and the complete suites.

Isolated compile passed: `dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --no-restore --verbosity quiet`, zero warnings/errors. No new test file or GUID was required. No Unity, ADB or Git operation was run by this helper.

## Image acceptance still required

Inspect actual matching gate detail, gate actor approach and full-map captures for a clearly longer vertical pier silhouette, seated plinth/crown, continuous rear curtain contact and no doorway/banner masking. Check the L-section at both jambs for painter-order seams and unexpectedly dark narrow bands. Reference fidelity, complete physical-device testing and performance remain separate acceptance gates.

## Jamb bearing correction after the first Unity suites

Both new jamb cases initially failed the containing-volume check. That first oracle reconstructed a chamfered tier from the new rectangular box, rather than the original body. Investigation also found a real small support defect, so this was not repaired merely by widening an oracle: the first square corner at `(32.06, a+.035)` extended past the existing base's beveled top. The base top begins at `(32.05, a+.03)` and has a `.06075` chamfer; its two-axis corner inset was only `.015`, leaving `.04575` of diagonal overhang in design coordinates.

The jamb body box is now trimmed `.04` inward on all four sides: `u=32.10..32.96`, `v=a+.075..b-.075`, plus the existing gate offset. Its height, concave section proportions, material, topology and 392-vertex cost are unchanged. The tightest base-cap corner inset sum is now `.095` against the `.06075` chamfer, a `.03425` positive clearance. It also fits the original body's contracted top: inset sum `.060` against `.055125`, a `.004875` positive clearance. These are design-coordinate diagonal clearances, not perpendicular world-space distances.

The test now independently reconstructs the original body with its actual `.025` inset. A separate oracle extracts only the unchanged base's cap triangles, excluding all three projected wall faces, and requires all actual section corners and edge midpoints to sit on that chamfered top. Cap-notch exclusion, painted-facade exclusion, finite mesh and budget assertions remain. Root's reflection fix for the separate Unity test-assembly boundary is preserved. No tolerance was expanded.

The isolated compile passed again with zero warnings/errors. Assets were frozen after this bounded production/test repair; root must repeat actual capture and the Unity suites. The earlier After image review predates this inward trim.
