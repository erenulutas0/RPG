# First-suite court material oracle failure

The first full EditMode run passed 620/621 cases. The existing `FoundryPavingLayoutTests.LaidCourtStoneKeepsOneMineralBodyUnderContinuousWorldLight` failed at line 106: expected red residual `0.886074007` within `0.0001`, received `0.885867774`. PlayMode subsequently passed 159/159. The conditional APK step was skipped after the EditMode failure.

The original XML files, source-freeze manifest and pre-correction test source are retained under `first-suite/`. No runtime source was changed in response to the failure.

## Cause

The test removes `FloorLight` from each emitted face color and then requires the remaining quarry body to be constant across the interior of one laid stone. That residual now also contains the deliberately world-continuous `PavingCourtUse` scalar. A broad use gradient can cross a stone even though its underlying mineral body remains constant.

A read-only managed diagnostic loaded the existing `UnityCompileCheck.dll` and invoked the real flag emitter and color fields through reflection; it did not launch Unity or compile another assembly. It reproduced the exact reported failure at vertex 44 of the flag starting at design `(-23,-10)`. The compared face points were:

| Sample | Design position | Light-only red residual | Broad use gain | Mineral red after removing both fields |
| --- | --- | --- | --- | --- |
| Actual | `(-22,-9)` | `0.8858678` | `0.9870117` | `0.8975251260` |
| Reference interior | approximately `(-21,-9)` | `0.8860740` | `0.98724145` | `0.8975251285` |

The difference is accounted for by the intended material-use field. Across all seven original flag locations, the largest remaining channel difference after removing both fields was approximately `1.81e-7`. A second entry flag had a larger light-only difference (`0.00711775`) but likewise recovered a constant mineral body. This confirms a test interpretation mismatch rather than a local per-stone wash or geometry defect.

`first-suite/material-body-diagnostic.ps1` and its JSON output preserve the sample evidence. The diagnostic assembly hash is retained separately. The script reads the normal compile-check output path and computes the original light-only residual directly, so it can also be run after the corrected helper is compiled. This diagnostic is not a replacement for the Unity test rerun.

## Test-only correction

After Unity finished, root released only `FoundryPavingLayoutTests.cs`. Its extraction helper was renamed from `Unlit` to `StoneMineral` and now removes both source illumination and broad material use before checking quarry-body constancy, local shoulder gain and mortar relationships. All existing numerical bounds and tolerances remain unchanged. Runtime code, geometry and material gains remain unchanged.

The seven new court-material cases separately inspect actual referenced emitted colors to verify that the broad field is applied exactly once, remains within its intended bounds, crosses fitted/ordinary joints continuously and protects the central/gate regions. Consequently the corrected legacy test does not excuse an omitted or duplicated field, nor does it relax the original mineral-body invariant.

Sources were frozen again for root-owned recompilation and a fresh full validation gate. Final rerun/build outcomes belong to the final verification record, not this failure diagnosis.
