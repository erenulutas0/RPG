# Independent baseline review — gate depth

Baseline `94243b7`, 2026-10-14. Read-only art/source review; no source edits, Unity, Git, or device activity. Reopened the preceding package's native `full-map`, `gate-crown-detail`, and `approach-20-1`; its `gate-detail` and the approved `2026-09-23/foundry-layout-01/foundry-v1.png` were also inspected in the immediately preceding review. Reviewed the existing gate assembly and focused invariants, plus the latest authoritative doc 38 entry.

## Strongest mismatch

The painted central portal has detailed iron, stone and fabric; its adjacent native masonry reads as much larger, simpler and paler planes. The reference joins the portal, jambs, bearing shoulders and side walls into one carved structure. More thin joints or another overall exposure adjustment would not close that difference. The useful target is a stronger hierarchy of broad exposed faces, dark connected returns, and visibly supported bases within the existing envelope.

`BuildGateInnerReturn` / the jamb rebate, the lower buttress mass and shoulder, and the main header's connected bearing planes are the relevant source areas. Prefer replacing or profiling their existing owned faces over adding decorative overlays. Preserve the portal as the principal focal point; adding more isolated gold, tiny crests or ornamental slits would distract without solving construction.

## Visual acceptance criteria

1. **A construction change must survive ordinary viewing.** In the matched full-map view, the grouped gate should read more clearly as a supported gatehouse around the painted portal. In the native gate-detail view, at least two structural regions should show a useful change in depth/connection, not merely a grain or brightness difference discernible by toggling close-ups. A cropped enlargement is diagnostic evidence, not the main success condition.
2. **Include visible lower work or honest portrait framing.** The current `approach-20-1` portrait crops the crown entirely. Crown-only improvements cannot be called a portrait-scale gain from that view. Lower jamb/base transitions visible in this camera can establish that gain. Alternatively add an earlier matched gate approach to both phases without changing the gameplay camera, and retain the old portrait as a regression check.
3. **Clear depth without black outlining.** Broad lit shoulders and recessed returns should connect to the same physical support. Recesses must stay readable as cavities; no uniform charcoal wash, floating trim, high-contrast course stripes or duplicated projected ledges. Both left and right assemblies need inspection because painter ordering and splay differ.
4. **The painted/native boundary must improve.** Door, banners, medallion, inset backing and stone should remain separable. New native faces must not cover painted banner/door pixels, the cartouche backing, or the cleared sill. Keep warm accents local; do not brighten all stone to compete with the portal.
5. **The approach remains quiet and usable.** Preserve the actor/ring, raised landing, flush threshold, rail returns and brazier supports. In the full map, court-to-gate progression should remain the dominant route. No prop relocation or gameplay/nav change is needed to make this package succeed.

A modest tint-only result should be recorded as such and not used to claim this construction objective complete. The approved reference still has substantially richer small-scale craft and more integrated lighting; even a successful package will not establish overall reference parity.

## Existing technical guardrails

- `FoundryGateRecessTests` protects actual seated buttress contacts, original footprint/crown, exposed backing after complete assembly, closed corner/cap ownership, concave jamb bearing and the absence of intrusion into painted facade pixels. Its strict backing-overpaint oracle previously caught real float-boundary intrusion; do not relax it to accommodate new geometry.
- `FoundryGateCorniceTests`, `FoundryGateCopingTests`, and `FoundryGateCurtainTests` require partitioned visible surfaces, registered stations/joints, exact retained caps/bearings, bounded noncollapsed UVs, continuous grain and inherited light. Replacement profiles need actual emitted-area/interior ownership checks and production-call evidence, not just isolated helper shape checks.
- `FoundryGateStoneTests` protects compressed lock/backing containment and flush processional paving across grade breaks.
- `FoundryRegionTests.AssertGateThresholdPresentation` checks the actual merged scene: the door physically backs the open presentation interval; later-painted masonry cannot fence the sill; one paving batch owns the threshold; central posts cannot return. Preserve brazier-post support and the two rail returns.
- Near/far enclosure passes use different ranks, and `MassDepth` ownership is set from existing wall/tower footprints. Reusing a builder or adding a return under the wrong owning mass can yield a correct isolated mesh and an incorrect assembled scene. Inspect complete gate captures as well as helper tests.
- The current resource baseline is **34 region renderers / 167208 vertices**, including **3572 distant vertices**. Ceilings 174000 / 3600 leave **6792 region and 28 distant vertices**. Measure the complete grouped delta, including grain subdivision and index count where altered. Do not add the distant count twice or call added startup geometry processing free. Reuse existing material/resource ownership; this gate work does not justify spending distant headroom.

## Evidence and phone boundary

The preceding package is fully verified in Editor/build records, but its APK is explicitly **not installed**. The authoritative device notes retain a pending concrete restart/install decision. This review neither clears it nor claims current phone availability. Later deployment must inspect current device state and preserve current original saves and telemetry; historical snapshots are not current state.

Static matched captures do not establish phone motion, shimmer, HUD interaction, dense-crowd readability or midrange performance. Existing immortal-crowd waypoint congestion remains unresolved. Keep these limits separate from source and still-image acceptance.
