# Quarry candidate review - 2026-10-13

Candidate: C:/Users/pc/.codex/generated_images/01a095bd-7c87-7c33-83a1-013aa1757c47/exec-82b7ac6f-db31-43f2-97c4-2952473a6410.png

Independent review used view_image and read-only PIL/NumPy analysis. No pixels or Assets were changed.

## Provisional decision

Suitable for an actual Unity material proof. The new image is matte, calm flaked slate with much less salt-and-pepper aggregate. It contains no mortar grid, fire, directional cast shadow, strong vignette or specular glitter. Its mineral layers are a closer match for worked stone than the old dense photographic aggregate.

The improvement is mainly the removal of tiny contrast, not substantially stronger broad variation. It may still look uniformly quiet in the overview, and must be judged on portrait stone surfaces. Existing mesh contours, abrasion, joints and lighting should continue to supply the structure.

## Read-only measurements

Both images are 1254 by 1254 RGB. Values use 0-255 sRGB channels with Rec.709 luma weights; they are visual diagnostics, not linear physical luminance.

| Measurement | Active V5 | Candidate |
| --- | ---: | ---: |
| Mean RGB | 85.919, 79.698, 82.126 | 93.789, 89.214, 88.813 |
| Mean luma | 81.196 | 90.158 |
| Pixel luma standard deviation | 18.206 | 5.759 |
| Luma 5th percentile | 57.78 | 81.99 |
| Luma 95th percentile | 113.06 | 100.42 |
| Standard deviation of 64-pixel patch means | 1.578 | 1.798 |
| Horizontal repeat-edge mean absolute jump | 14.774 | 4.691 |
| Vertical repeat-edge mean absolute jump | 15.137 | 5.036 |
| Horizontal ordinary adjacent-pixel jump | 11.114 | 2.718 |
| Vertical ordinary adjacent-pixel jump | 12.489 | 3.539 |

The candidate is about 11 percent brighter on average and about 68 percent lower in pixel contrast. It is also less purple. A material multiplier around 0.72 instead of 0.8 would approximately match the old average, but this is only a diagnostic reference; do not darken pre-emptively without inspecting the actual render and existing floor vertex tint.

## Repeat risk and acceptance

The bitmap is not mathematically edge-matched. Absolute edge mismatch is markedly below V5, but continuous flake contours can visibly reset even when average values agree. Existing world UV repeat is approximately 4.03 design units, so large slabs sample a substantial part of each repeat. Do not call it seamless solely because it has no tile grid.

Accept only after inspecting repeated actual surfaces in court-detail, approach-0-0, approach--8--12, bridge-detail and full-map. Reject visible straight repeat boundaries, periodic pale patches, loss of brass/joint readability, or a floor becoming brighter than the hero and reward actors. Compare gate and shaft views as well because nearby old masonry must remain a coherent stone family.
