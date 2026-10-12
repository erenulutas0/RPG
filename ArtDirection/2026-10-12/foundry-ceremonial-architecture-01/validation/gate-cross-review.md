# Gate source cross-review

Read-only review of `FoundryRegionGateEnclosure.cs`, `FoundryRegionArchitecture.cs` and the changed `FoundryGateRecessTests.cs` after the gate helper's implementation. No source blocker found.

- The near/far jamb sections retain a rear spine and omit the correct inner-front corner. Their two cap rectangles tile that same concave L section: no fan spans the missing corner. The last-eight-vertices cap test checks both the genuinely absent front corner and three supported regions in design coordinates.
- Jamb bodies begin exactly at the existing base's 1.15 top and finish at the capital's `height-.38` bottom. The base's tapered top still contains the complete body section; the capital remains supported by its rear spine and outer leg. Both v extents stay outside the facade's `[-1,11]` interval. The original facade plane, walking threshold and depth owner remain unchanged.
- Outer towers retain the original complete foot and 7.35 crown. The new low bearing ends at 1.84 where the tall shaft begins; its 6.55 top meets the capital with no vertical gap. The old middle belt/quoins are removed from this emitter, so they cannot mask the elongated recess.
- The replacement faces emit back-to-front by design-depth edge midpoint. Test probes compare the complete tower's final covering triangles with the isolated recessed shaft, covering the former waist. Existing shaft envelope/cap guards remain, alongside local 400/2400 vertex bounds.

This is source/test-design review, not image acceptance. The actual capture must still establish whether both L-section returns read cleanly in the shared transparent material and whether the slimmer continuous bays materially improve the full-map gate silhouette.
