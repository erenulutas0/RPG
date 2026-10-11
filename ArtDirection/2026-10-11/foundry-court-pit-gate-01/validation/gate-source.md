# Gate architecture source handoff

Baseline: `66e68d3`. Prepared 2026-10-11. This is source and analytic geometry evidence, not Unity or device acceptance.

## Change

- The two inner jamb shafts and two outer buttress middle shafts now have connected blind recesses: a recessed backing, five depth returns and broad dressed bearing/frame stones. The earlier planar niche and underlying solid front faces are removed from these shafts. The outer curtain's smaller strip relief is unchanged.
- Existing tapered shaft footprints, top/bottom elevations, corner chamfers and caps are preserved. Recess depth is 0.28 design units on jambs and 0.36 on outer buttresses. All backing vertices remain inside the original projected prism.
- Two continuous header shoulders replace the four interrupted small coping ends. The central lock recess, its surrounding blocks and mechanical lock are unchanged. The dressed lintel is slightly lighter than the supporting shafts. The header's former gaps are filled within its existing height/footprint envelope; this intentionally changes that local contour, not the envelope.
- Old unused planar jamb-relief methods were removed. Shared masonry, weathering and projection helpers are unchanged.

## Preserved contacts and ownership

No changes to door/banner facade/socket, gate offset, ramp, walking surface, thresholds, lower gate bases, landing shoulders, boilers/pipes, iron straps/collars, fire placement, material ownership, mesh renderer ownership, depth rank, fade owner, navigation or gameplay. No textures, materials, runtime updater or renderer are introduced.

The new shaft copies the original corner and cap vertices exactly after emitting its side/front faces. Each face paints backing first, connected returns next, then the broad bearing and frame patches. No full rectangular face lies underneath the intended opening.

## Verification added

`FoundryGateRecessTests` has six cases: two opposite projected recess directions test both throat visibility after painter ordering and continuous wall coverage; four production shaft placements test the old projected envelope, unchanged chamfer/cap contacts, finite mesh channels, valid triangle indices and a bounded local vertex count.

A separate analytic projection check completed before handoff: both projected recess directions had zero missing wall samples and zero throat-overpaint samples; both distinct production shaft shapes had zero points outside their original projected prism. This is a mathematical cross-check only. Root still needs to run the actual EditMode cases, graphics capture and runtime budget checks.

## Files

- `Assets/_Project/Scripts/UI/Arena/FoundryRegionGateRecesses.cs` and metadata
- `Assets/_Project/Scripts/UI/Arena/FoundryRegionArchitecture.cs`
- `Assets/_Project/Scripts/UI/Arena/FoundryRegionGateEnclosure.cs`
- `Assets/_Project/Scripts/UI/Arena/FoundryRegionGateHeader.cs`
- `Assets/_Project/Tests/EditMode/FoundryGateRecessTests.cs` and metadata

## Review still required

Compare gate-detail, approach-20-1 and full-map against the fresh before images. Confirm the broader native stones and actual inset faces read as one gate structure without losing the painted leaf/banner hierarchy, and that the painted recess backing does not visually read as a black cutout. A local composition improvement cannot establish full reference parity or phone acceptance. Parent alone performs Unity and any later device checks.


## Later integration outcome

Root completed the actual paired-image review, all final suites and the Android build. See `actual-image-review.md` and `verification.md`; the earlier pending statements above describe the implementation handoff stage. Device acceptance remains pending.
