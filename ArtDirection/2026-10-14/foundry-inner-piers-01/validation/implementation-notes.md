# Inner-pier implementation notes

Baseline: `e6f681792a53d9c3e3204291e67d51738f2c1abd`; matched 21-view Before set in this package. These notes describe the frozen source. Capture acceptance, final suite results and measured resource totals belong in the final validation record and are not asserted here.

## Complete form replacement

`FoundryRegionGateInnerPiers.cs` introduces `BuildGateInnerPier(Builder,float v)` for both existing inner piers at V=-3 and V=11. All U coordinates below are authored coordinates before the unchanged `GateOffset`.

| Height | Construction |
| --- | --- |
| 0–1.70 | Original three footing/body/collar-seat tiers retained exactly. |
| 1.70–2.65 | One raked haunch contracts from U32.26–34.74 / V+.24–1.76 to U32.48–34.52 / V+.46–1.54. |
| 2.65–5.45 | Shorter recessed shaft uses the unchanged shared `GateRecessedShaft`, with inset -.18 and recess depth .28. Its gentle outward flare reaches the old upper planes U32.30–34.70 / V+.28–1.72. |
| 5.45–6.05 | One substantial capital replaces the two previous stepped tiers. Its upper bearing is U32.25–34.75 / V+.20–1.80. |
| 6.05–7.25 | Original sloping head and terminal seat retained exactly. |

`GateInnerPierMass` emits three visible walls and one closed indexed cap, using lower and upper octagonal stations and inherited directional/forge light. Each new mass has 41 source vertices. Its broad fitted faces replace the old construction; there is no duplicate full tier beneath them. The capital intentionally overhangs its shaft with central bearing. Source counts alone do not establish final cost after grain subdivision.

The lower iron collar at 1.48 remains on unchanged geometry. The upper collar at 5.43 retains actual overlap with the new face; that face differs slightly from the old taper, so equality to former shaft vertices is not a contract. The terminal bearing remains at 7.25. Portal, `BuildGateInnerReturn`, fire/iron geometry, footing, camera, navigation and depth ownership are unchanged. The narrower shaft changes its octagonal chamfer stations; its upper rectangular planes are preserved.

## Family finish and assembly

`FoundryRegionGatePierFinish.cs` applies the existing clean atlas patch (.04,.44)–(.21,.564), common datum and horizontal orientation at density .11 to each complete pier. Header, inner returns, cartouche and other gate stone retain the shared .075 density. The family field therefore samples finer grain; it adds no explicit brightness or colour lift. Existing source vertex lighting is preserved, and any new fold vertices interpolate it.

`BuildGateArchitecture` finishes separate static builders before combining them: header, first pier, its inner return, second pier, its inner return, then cartouche. `AppendGateStone` copies positions, UVs and colours and offsets indices while preserving triangle order. It rejects cloth/flame geometry. The former final shared-grain call is removed, so it cannot overwrite the .11 family finish. The existing final `Create` still assigns the assembled batch's structure depth and material.

No new runtime texture, material, shader, renderer batch or updater is introduced. Geometry and UV work remain startup work. Final post-grain vertex counts must be measured against the unchanged 174000 region / 3600 distant ceilings; static counts are not phone performance evidence.

## Validation and remaining limits

Six new near/far cases cover retained footing/head pieces, original envelope, actual cap bearing, collar overlap, exposed recessed-shaft ownership, finish coverage/source light, .11 versus .075 field isolation and append order. Existing shared recess tests remain applicable; the shared emitter was not edited.

Visual acceptance remains pending in this note. Review complete piers in gate-detail/crown, the gate family in full-map, and lower contacts in approach-20-1. Specifically check whether the flare reads as a supported pier rather than an hourglass, whether the capital remains too dominant, and whether finer grain still clashes with the softer baked bays. The portrait crops much of the head. Reference completion, device motion, HUD overlap, shimmer and frame time are not established by this source change.
