# Independent actual-image review - 2026-10-13

Decision: accept the current V6 quarry floor and gate-header finish for this material package. No observed material defect warrants another source change. This is acceptance of the matched Editor images, not reference parity or physical-device acceptance.

## Evidence inspected

The reviewer inspected the actual PNG pixels with view_image for both Before and After in these six matched views:

- full-map
- court-detail
- approach-0-0
- approach--8--12
- bridge-detail
- gate-detail

The approved foundry-layout-01/foundry-v1.png reference, the previous bound paving and masonry bitmaps, and the unmodified generated candidate were inspected earlier in this review. No Assets, image pixels, Unity sessions, ADB operations or git state were modified by the reviewer.

## Quarry floor

The new material removes the fine, evenly distributed aggregate that made V5 read as speckled concrete. Calm, layered mineral variation now occupies the slab faces, while the authored joints, worn corners and court medallion remain visually distinct. The floor reads slightly lighter and smoother, but does not become pale or dominate the figures.

Both portrait views retain clear blue hero armour, the small ember enemy, the larger stone enemy, the reward chest, health bars and pale attack-range guide. The broad slab joints are easier to read against the quieter texture. The central brass motif and approach fields remain legible. Local warm light around the pit and the gate continues to describe the floor without becoming a uniform orange wash.

No conspicuous straight repeat boundary or distracting repeated pale landmark was observed in these six views. This observation does not assert mathematical seam matching: the original candidate has measurable edge differences. Mip behaviour, moving shimmer and compression on a physical phone are not established by still Editor captures.

The change is visible at portrait and court-detail scale. The full-map effect is deliberately quieter; the package does not materially change the map silhouette or make the overall scene equivalent to the reference.

## Gate header and neighbouring materials

The header now has more legible stone blocks across the coping rather than an evenly shaded long cap. Its darker vertical band reads as a recessed bearing/contact zone under the brighter upper faces. It remains subordinate to the painted door, banners and gold centrepiece. The door is not swallowed by the dark band, and the gate retains a clear destination hierarchy in the overview.

Native gate faces and the painted door still differ in craftsmanship density. The painted door has finer edge variation and richer material construction; this is a remaining fidelity gap, not a new defect introduced by the current finish. The bright flame shapes also remain stylized. Another global brightness lift would not solve those differences.

## Remaining limits

The floor still has a cleaner, more geometric character than the reference's fitted, weathered dressed stone. The new grain is a useful foundation, but increasing its contrast or adding more noise would not by itself close that gap. Future work should improve selected physical edge and material transitions while preserving the quiet combat surface.

Still-image review cannot establish moving aliasing, touchscreen readability with full HUD, device frame time, gameplay acceptance, or midrange-phone performance. Those remain separate verification work.
