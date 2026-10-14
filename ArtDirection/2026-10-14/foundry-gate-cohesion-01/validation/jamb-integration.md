# Fitted inner-jamb integration

Baseline: `3474e356e0535e145f94e824eadaeda57231875f`. Owned changes are confined to `BuildGateJambRebate` in `FoundryRegionArchitecture.cs` and the new `FoundryGateJambTests.cs`/meta. The root separately owns the completed gate material treatment and parent method integration.

Each of the six planes in the existing L-shaped jamb now has four broad fitted courses and three real 0.018-unit mortar beds. Indexed outer/inner arris rings replace the former three overlaid dressed stones and their coplanar complete backing. Course boundaries align around the return; one physical grain field spans each complete face. The existing exposed/reveal tone family remains. This adds no ornament, material, renderer, texture or updater.

`GateJambSection`, sorted face order, supporting base, upper bearing tier and the two final cap rectangles are unchanged. The absent corner remains empty. Door/banner clearance, threshold, footprint, iron stations and gameplay geometry remain outside the change. Shared stone/UV helpers are called without modification.

Six new focused cases inspect actual emitted faces: complete area, strict interior single ownership, original envelope, exact L-section/cap and production base/body/upper-tier inclusion, continuous grain, nondegenerate UVs and separately owned mortar. The existing 400-vertex jamb guard is retained. Expected local cost from the emitted topology is **392 to 272 vertices per jamb (-120)** and **196 to 280 triangles (+84)**: the fourth broad course trades more indices for indexed vertex reuse. The pair therefore expects -240 vertices/+168 triangles before root's separate completed-parent grain treatment. Root-run Unity output, followed by the complete scene measurements, remains authoritative.

A supplemental float32 arithmetic reconstruction checked all twelve wall faces and their 17×13 strict interior witnesses: no gap or duplicate owner was found, with face-area differences below 0.0000006. This is not a substitute for executing Unity tests. Compilation, focused/full suites and native visual acceptance are pending. No Unity, build, phone, save or telemetry operation occurred in this helper scope.
