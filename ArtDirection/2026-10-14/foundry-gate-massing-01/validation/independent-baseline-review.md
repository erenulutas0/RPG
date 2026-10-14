# Independent gate massing baseline review

Date: 2026-10-14. Baseline: `0e44e8fe3c04a53ec958556131ffa323fb4dede7`.

Read-only review of the latest Docs/38 handoff, the material-family package's next priorities, the production gate construction, and the approved `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`. The fresh package `before/gate-detail.png`, `gate-crown-detail.png`, `full-map.png`, and `approach-20-1.png` were viewed at native resolution. All 33 files recorded in `validation/baseline.json`, including the 21 PNGs, independently match their recorded SHA-256 hashes. The prior package's images have different hashes and are not substituted for this baseline.

Baseline measured region: 36 renderers / 172,340 vertices. Distant geometry: 3,428 + 144 = 3,572 vertices, included in the region total. Ceilings remain 174,000 / 3,600 respectively; available headroom is 1,660 / 28. These are geometry limits, not device performance evidence.

## Bounded direction

The authorized active baked outer-bay replacement and native upper-curtain side construction are appropriate broad-form targets. Higher raked lower haunches, wider side piers and a shorter deep blind bay can make the outer towers look more convincingly seated. A coherent recessed curtain field with raked shoulders and an upper bearing can connect the towers to the wall. Additional fine joints or a barely revealed strip would not be a sufficient curtain improvement.

The active outer bays are two baked cards loaded from `FoundryGateMasonry`. Editing `BuildGateOuterButtress` alone would only change its inactive fallback. The chosen native Blender replacement correctly reaches the active production texture. The approved scope excludes header/cornice, inner pier, portal, roof and shared material changes.

## Required visual proof

- Use gate-detail and gate-crown-detail to judge actual depth, lower support, niche readability, the exposed curtain field, and tower/curtain/crown contact. The field must be a visibly connected plane rather than an invisible edit behind the baked card. Preserve a readable dark recess and vertical cheek rhythm; reject a squat undifferentiated block or a pale mass that competes with the door.
- Use full-map to judge the broad mass relationship. A closer-view improvement may be bounded, but the gate must retain its recognizable silhouette, focal door and stable material value. Do not claim reference parity from added joints or a source render.
- Use approach-20-1 as a portal, floor, stair shoulder and foreground pipe regression view. It crops the crown and most outer bays, so a major tower-form gain is not expected in that camera. Preserve quiet navigation space and character readability.
- Inspect both complete diagonal feed pipes, their collars and masonry intersections in actual images. Retaining one historic witness point alone cannot establish the whole visible connection. Reject opaque masonry over a previously foreground pipe, open contacts, unexplained dark slivers, or curtain overpaint of the baked niches.

## Production constraints

Preserve the true portal and raised landing projection, walkable/collision geometry, cutaway/depth footprints, half-rank card ordering, existing 0.62 material gain, and the same two four-vertex cards. The replacement stays within the existing 512 x 1024 runtime texture, ASTC 6x6, 11 mips, non-readable, Clamp/Trilinear importer contract. No new renderer/material/resource is implied by replacing the bitmap.

The curtain is confined to its upper side plane. Preserve the existing front opening and rear/return ownership, original closed chamfer/cap stations, roof, and contacts. Replace the side face; do not overlay a full wall beneath the recess. Keep one continuous existing quarry finish and measure final geometry after `GateHouseGrain`, not just source vertices. Existing strict opening coverage, bearing station, roof ownership, card projection/depth and borrowed resource lifetime tests remain relevant.

## Source candidate inspection, before Unity acceptance

The new `masonry-bay/gate-masonry.png` (SHA-256 `07a65fe8c065c686cdb1ab509fffb8efed093ef718e9a0d17a9e23e6617cde70`) visibly has the intended taller haunch and shorter, narrower deep bay. It retains a distinct pointed recess, broad cheeks and the existing crown family. The frozen curtain source replaces the old side grid with a six-corner recessed field, real inward returns, raked lower shoulders and a continuous upper bearing. No source contact or painter-order blocker was identified; scene visibility is still pending.

External bounds are unchanged, but the alpha contour is **not identical**. A read-only Pillow comparison to the previous gate-depth source finds 19,697 changed alpha values and 16,366 newly covered pixels at alpha >127, with no lost coverage at that threshold. Changes lie within source x137..886 / y436..1406, using top-left image origin. Both images have alpha bounds `(80,183,943,2008)`. Describe this as preserved external bounds/crown interfaces with intentional internal massing and contour changes. Actual curtain and pipe occlusion must therefore be reviewed after import.

The approved reference remains richer in stepped gate construction, equipment variety and atmospheric material integration. This package can improve a bounded gate support relationship; it cannot establish whole-scene reference parity, phone acceptance, performance improvement or resolution of the existing crowd congestion concern.
