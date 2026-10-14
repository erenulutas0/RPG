# Next visual priorities from actual workyard captures

This is the implementing helper's self-review and planning note, not independent acceptance. I inspected this package's fresh native `full-map`, `stock-detail`, `casting-detail`, `crane-detail`, `yard-detail`, `gate-detail` and `shaft-detail` images against the approved `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`. These are Editor captures, not phone evidence.

## Workyard self-review

The native mould-bench timber is visibly less orange, and its iron remains distinct from the wood. The native winch's upper planes are slightly clearer while its lower contact stays dark. The untouched baked stock/casting equipment remains a useful fixed comparison. This is a modest local gain: the full-map composition changes very little, and the native iron still has denser visible grain and more angular surface transitions than the baked equipment. The colour pass does not turn those native pieces into finished reference-quality assets. Further tiny tint adjustments to the same bench are unlikely to produce the next meaningful improvement.

The current paving pass adds local use variation without filling the playable centre. The reference still has stronger large-scale foreground construction, more convincing material depth at object contacts and a richer hierarchy between major gate/shaft/industrial landmarks. Those are larger gaps than the number of joints or cracks.

## Recommended next grouped package: authored gate masonry depth

The clearest remaining mismatch in the inspected images is the large pale, faceted native gate envelope around the detailed baked door. The two surfaces do not yet read as one authored structure. At full-map scale the gate's broad side masses and top bearing remain simple; `gate-detail` makes the difference especially clear. Repeating another uniform seam treatment will not resolve that material/shape relationship.

1. **Prove one complete gate bay as a quality target.** Use the existing Blender-to-Unity art workflow (`Tools/ArtReview/BuildBlenderCastingBench.py` and related scripts as pipeline references) to author one bounded full-height masonry bay: structural plinth, tapered pier, deep pointed recess and broad top bearing. Its original envelope, opening, attachment stations and camera projection must be copied from the actual production gate, not invented from a better-looking concept render. A proposed new `BuildBlenderGateMasonry.py` should preserve an editable source and explicit projection contract. Judge a real Unity replacement at both the unchanged portrait framing and native gate/full-map views before extending it to the whole portal.
2. **Integrate the proven bay as a replacement.** Scope `FoundryRegionGateRecesses.cs`, `FoundryRegionGateButtress.cs` and the relevant gate assembly only after the candidate is visually convincing. Remove the replaced faces rather than layering authored stone over old masonry. Keep existing arch voids, pipe penetrations, flames, banner stations, footprint/depth ownership and hero clearance. The target is a coherent large construction unit with convincing bevel/contact value, not extra tiny ornaments.
3. **Carry the same bearing family across the portal.** Coordinate only the existing header/enclosure junctions in `FoundryRegionGateHeader.cs` and `FoundryRegionGateEnclosure.cs` with the accepted bay. Keep the detailed door image unchanged as the comparison anchor. Match the gate-to-furnace contact and upper bearing in the same package so the result can be judged as one landmark, not an isolated enlarged asset crop.

This is a proposed art-production route, not authorization to silently add a new texture/material or replace the whole map. A new baked resource, if the proof requires one, must have explicit texture size, compression, mip/memory, material and renderer accounting before runtime adoption. Prefer replacing or consolidating existing cost; do not consume the remaining mesh budget on dense decorative subdivision. If the proof does not clearly improve the native portrait view, retain the existing runtime gate rather than importing an asset merely because its offline render is attractive.

## Budget and verification boundaries

The fresh capture reports **34 region renderers / 167,208 vertices**, including **3,428 distant stone + 144 distant slit vertices**. Against unchanged ceilings of 174,000 region and 3,600 distant, headroom is 6,792 and 28 vertices. Do not count distant geometry twice. This package's material work preserves geometry/resource counts but changes startup colour arithmetic; static totals are not device performance evidence.

Root reported focused 258/258, pure 438 and clean isolated compile before this image review. Full regression/build completion must be recorded by root; this note does not claim it. No device operation or installation is included. Preserve the pending concrete restart/install decision and inspect then-current phone/save state before any future deployment. Existing crowd-waypoint congestion and regional progression remain separate unresolved gameplay work. Reference parity remains unfinished.

## Final regression/build closure

438 .NET / 660 EditMode / 159 PlayMode = 1,257 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed separately. PASS: 622 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,391 bytes; SHA256 A967EFA4B65337DD7F0B401611520600E47EC5D1172C8A416D8009676049774B; NOT INSTALLED.

The final frozen-source suite/build gate is complete. This closure supersedes the earlier pending full-suite/build statements in this document; phone acceptance remains pending.
