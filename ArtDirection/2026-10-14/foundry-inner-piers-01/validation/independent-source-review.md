# Independent inner-pier source review

Date: 2026-10-14. Read-only review of `FoundryRegionGateInnerPiers.cs`, `FoundryRegionGatePierFinish.cs`, the active `BuildGateArchitecture` integration, unchanged shared recessed-shaft/grain/Builder implementations, and `FoundryGateInnerPierTests.cs`.

**No actionable source blocker identified. Actual image acceptance remains pending.** This reviewer did not edit Assets, run Unity or claim tests passed.

## Construction and contact

The new 1.70..2.65 haunch replaces part of the former tall shaft. Its top cap carries the shorter neck within the existing lower envelope. The neck has a real recessed front and side; the old full shaft is not retained beneath it. The consolidated 5.45..6.05 capital has one closed indexed cap and supports the unchanged sloping head. Its lower overhang is deliberate and has broad central bearing on the shaft; demanding that its entire wider foot fit inside the shaft would incorrectly prohibit a normal projecting capital.

The `-.18` inset is intentional and geometrically valid: it flares the neck outward toward U32.30..34.70 and V+.28..1.72 at height 5.45 (U coordinates before GateOffset). Those match the former broad top planes; the octagonal corner stations are not exactly identical because the neck's starting width determines its chamfer. The new fixed-height collar test correctly checks actual face/iron overlap rather than equality to old chamfer vertices. A narrow but positive overlap is expected at 5.43; inspect it in the actual image.

The lower footing/collars and 6.05..7.25 head are retained. The new mass emitter takes lower wall stations from its lower envelope and upper wall/cap stations from its upper envelope, with inherited source illumination. It does not emit a duplicate full tier or extra cap underneath the new bearing. Existing hidden-backface conventions are unchanged and are not a defect in this fixed painter view.

## Finish and painter integration

`GateInnerPierGrain` applies the same clean atlas patch at density .11 only to the complete pier family. Header, inner returns and cartouche retain the .075 field. All finish operations preserve the source lighting; changing density does not explicitly raise material brightness.

The active order is header, first pier, its inner return, second pier, its inner return, cartouche. Each part is finished before append. `AppendGateStone` copies vertices/UVs/colors, offsets triangles and retains triangle sequence; it rejects animated geometry. The unchanged final `Create` registers structure depth for the completed static batch. No additional material, renderer, resource or updater is introduced by this integration. The later shared grain call that would overwrite the family field is absent.

Final geometry cost still needs measurement after .11 fold tessellation. Fewer raw mass vertices alone are not sufficient budget evidence.

## Meaningful coverage and image risks

The six new test cases check unchanged foot/head pieces, envelope, emitted cap support, actual collar contact, visible recessed shaft samples, source light/coverage after finish, family/neighbour UV interpolation and exact appended triangle ordering. They target the changed contracts without modifying the shared shaft routine. The full production capture must still establish active integration, depth and overall resource counts.

The remaining acceptance risks are visual: the narrow neck and outward flare might read as an hourglass instead of a strong pier, the consolidated capital might remain oversized beside the new shorter shaft, and finer .11 grain might still look busier than the baked outer bays. Judge complete mass and material in gate-detail/crown, the door/outer-bay relationship in full-map, and lower/iron/fire contacts in approach-20-1. Do not infer acceptance from the source silhouette or passing geometry tests alone.
