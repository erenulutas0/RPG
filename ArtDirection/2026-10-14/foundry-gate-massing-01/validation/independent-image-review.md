# Independent image review — ACCEPT

Date: 2026-10-14. Baseline: `0e44e8f`. Decision applies to the completed matched Unity capture in this package's `before/` and `after/` folders, with the frozen native bay render whose SHA-256 is `07a65fe8c065c686cdb1ab509fffb8efed093ef718e9a0d17a9e23e6617cde70`.

**ACCEPT the bounded combined result:** the outer bays have a visible, substantive change in construction and support; the curtain change is a modest exposed side improvement and geometry simplification. It is not a second prominent gate transformation. No blocking visual regression was found.

Viewed Before/After gate-detail, gate-crown-detail, full-map, overview, and approach-20-1 at native resolution; also inspected After forecourt-detail and the approved foundry-v1 reference. All 21 Before and all 21 After image hashes are recorded in `independent-image-evidence.json`. Baseline integrity was independently checked against `baseline.json` before comparison.

## Visible outcome

The taller raked lower haunch now carries a shorter, deeper upper bay with wider solid cheeks. This is clearly visible in both close gate views and still readable in full-map/overview. The pointed recess remains distinct, the upper mass is seated, and the towers have not become undifferentiated blocks. Their retained crown envelope and darker quarry value preserve the door, gold seal and blue banners as the primary gate focus. This is a structural form gain rather than a trim-only change.

The curtain result needs a narrower description. The far broad field is mostly hidden by the existing native inner pier. In gate-detail, only 112 of its 12,434 projected field pixels change by more than 3 RGB levels; in crown-detail, 180 of 20,579 do. A small exposed shoulder and upper side fragment improves the junction, but these close images do not prove a prominent broad curtain bay.

The near curtain lies outside the two close gate frames. Full-map/overview reveal a connected dark field and bearing on the outer side of the near tower. In full-map, 364 field pixels and 495 pixels across the side change by more than 3 RGB levels after excluding both old/new baked tower alpha. The field change occupies x1217..1228 / y374..412 in the 1600 x 1200 image. That is visible and coherent, but small at scene scale. Retaining it is justified as a subordinate construction improvement that also removes 452 finished vertices; claiming two equally substantial visual upgrades would overstate this capture.

These counts use production projection and capture cameras, with bilinear old/new source-alpha exclusion. They are diagnostic bounds, not a renderer-ID mask, quality score or device measurement. Source-only visibility against baked alpha would miss the native inner-pier occlusion.

## Regression checks

- The complete visible feed-pipe runs and collars retain their foreground continuity at the bay intersections. Narrow core samples along the far and near pipes are bit-identical: 1,561 and 1,647 sampled pixels respectively. This supports, rather than replaces, the visual contact inspection.
- The portal surface, native jambs, header and threshold retain their existing presentation. A 37,800-pixel interior door sample is bit-identical. The actual capture's gate-threshold diagnostic also reports covered lower portal witnesses, retained threshold paving and absent central posts.
- Approach-20-1 remains a regression view: its crop contains almost none of the changed upper architecture. The actor, landing, stair shoulders, quiet floor and foreground equipment remain readable. No approach-scale tower benefit is claimed.
- The gate's surrounding value hierarchy stays stable. A 56,100-pixel quiet forecourt sample changes by at most 2 RGB levels, with only six pixels above 1; its channel means differ by less than 0.001. No new floor contrast, broad light wash or navigation obstruction appears.
- Exterior bounds and crown interfaces remain aligned. The intentional taller haunch changes the local alpha contour, as documented in the baseline review; it has not opened a new visible support gap or hidden a foreground pipe.

## Measured limits and remaining gap

The completed capture reports **36 renderers / 171,888 region vertices**, down 452 from 172,340. Distant geometry remains 3,572. Remaining room is 2,112 vertices under the 174,000 region ceiling and 28 under the 3,600 distant ceiling. The loaded gate resource remains **512 x 1024 ASTC 6x6, 11 mips, non-readable**, with two inherited card batches, one shared cutaway variant and 630,880 bytes of Editor texture memory. Geometry reduction and unchanged resource size do not establish a frame-time improvement.

The approved reference still has a richer stepped gate silhouette, more integrated native/baked surface treatment and a more developed equipment family. Existing tall inner piers and shallow horizontal crown rhythm remain evident; they were outside this package's scope. The far curtain remains largely occluded. This acceptance covers a better-supported outer gate and a limited side refinement, not whole-scene reference parity.

No phone build was installed or tested by this reviewer. Phone acceptance, the existing device decision and crowd congestion remain unresolved. Runtime/tests/bitmaps were not edited and Unity was not run by this reviewer.
