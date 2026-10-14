# Service-tower stone value integration

Baseline: `d4424447705aa85bf078ed5fb46ab11c92555b91`. Source ownership is limited to `FoundryRegionDistantWorks.cs` and `FoundryServiceShaftTests.cs`.

The existing service-bay bearings receive a restrained neutral quarry response instead of a blanket tower exposure adjustment:

- Exposed pier contours gain 12–18% on the darker west face and 8–12% on the near face, rising gradually between the original sill and head vertices.
- The recessed pier backs retain their previous 0.78 value response. Deep jamb/arch returns, sill, fire panes and the already bright projecting dressed arch remain unchanged.
- Outer spandrel stone receives the smaller matching west/near gain of 8% / 5%, while retaining its existing broad course variation.
- Supported corner blocks gain 7%; their sloping shoulder bearings gain 8.5%. Existing directional face ratios and all original alpha values are retained.

Only RGB construction values change. Geometry, indices, UV coordinates, silhouette, open crowns, fire openings, fixed tower stations, foundations, shoulder/sill contacts and machine/portal contacts stay unchanged. The existing final weather/heat/fade function is untouched. There is no new texture, material, renderer, shader, updater, per-frame work or geometric sampling. Expected region/distant totals therefore remain the baseline 167,208 / 3,572; root capture must verify them.

Six new focused test cases inspect actual production output. Four cases cover both bay orientations at both authored dimensions: existing 260 vertices / 426 indices per complete facade; neutral exposed gains; unchanged recessed backs, deep returns and projecting arch values; preserved alpha; and the complete existing facade UV field. Two cases inspect the full 1,208-vertex / 1,884-index service-shaft emitter, compare supported corner geometry/UV with the unchanged block/taper emission at its prior tone, verify only bounded neutral colour gains, and compare the complete unchanged head's positions/UV/colours. Existing actual tower envelope, open mouth, fire, attachment, partition and global budget cases remain unchanged.

Source and tests are frozen for the root's compile/capture/regression gate. No Unity, build, Git, ADB or phone operation was performed by this helper. Test outcomes and visual acceptance are pending. The desired result is a clearer broad stone/recess hierarchy around the existing bays; this does not establish reference parity or permit more distant geometry within the remaining 28-vertex headroom.
