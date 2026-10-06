# Gate framing study — 2026-10-06

## Method

Test-only comparison at hero design position `(30, 2.82)`, horizontally aligned with the gate pediment. The actual scene is rendered at 1080 × 2340 with HUD enabled. Three renders occur synchronously at the same scene pose:

- `gate-study-production.png`: unchanged live follow position and size.
- `gate-study-up-1p5.png`: same lens, camera raised by 1.5 world units.
- `gate-study-width-11.png`: 11-world-unit lens width, original camera centre retained.

`gate-framing-study.csv` records the lens, camera and hero viewport coordinates. The study restores camera position, orthographic size, follow-enabled state and backdrop framing in `finally`, with equality assertions. It changes no runtime camera/HUD code, serialized values, scene, movement geometry or player profile. The wider variant is an isolated lens comparison, not a complete implementation of wider production follow framing.

## Actual capture findings

All three final `after/gate-study-*.png` images and `after/gate-framing-study.csv` were inspected following the passing graphics run. The production view preserves the large hero and clear door emblem but masks upper gate architecture behind the top HUD. Raising the camera reveals more of the door's upper frame while moving the hero visibly downward. Widening the lens admits more flanking tower architecture and apron, but visibly shrinks the hero, movement ring and all other world detail. **Both alternatives still obscure upper gate architecture with the HUD; neither establishes a complete gate silhouette.** The unchanged HUD continues to occupy the same screen space.

| Variant | Camera Y | Orthographic size | World width | Hero viewport Y, from bottom | Nominal free-band world Y | Nominal vertical span |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Production | 11.8786 | 9.7500 | 9.0 | 0.4859 | 5.137–18.070 | 12.933 |
| Up 1.5 | 13.3786 | 9.7500 | 9.0 | 0.4090 | 6.637–19.570 | 12.933 |
| Width 11 | 11.8786 | 11.9167 | 11.0 | 0.4885 | 3.639–19.446 | 15.807 |

Camera X is 19.2192 throughout. The nominal free-band calculation uses the capture's configured rows 361–1913 from the bottom: `cameraY + (row/2340 - 0.5) * 2 * orthographicSize`. These are projected world-plane extents, not measurements of traversable ground or unoccluded threats: architecture, void and HUD geometry still matter.

The upward alternative moves the hero down by **180 screen pixels** at 1080 × 2340. It trades 1.5 projected world units of coverage below the hero for 1.5 above. The nominal below-hero band falls from 6.467 to 4.967 units, approximately **23.2% less**. The visual gain therefore carries a real directional visibility tradeoff, without any smaller-actor penalty.

The wider lens increases linear coverage by **22.2%**, but makes each world-space actor or pickup **18.2% smaller in pixel dimensions** (`9/11`). Its nominal vertical band increases by 2.874 units; this view includes additional structure/void, not exclusively useful combat floor. Static captures cannot establish how well small enemies, pickups or incoming threats read during play.

## Recommendation and limits

**Keep the production combat camera unchanged in this package.** Neither alternative solves the full landmark reveal, and a global width increase sacrifices actor readability to show architecture. If a full gate reveal becomes a product requirement, consider a separate, temporary non-combat arrival view with a deliberate return to combat framing, or a dedicated HUD/layout investigation. That would require its own implementation and play validation; it is not included here.

The phone is unavailable; no device acceptance or performance result is claimed. Existing route `point-*` captures remain useful for comparing floor materials at the unchanged production lens. The study adds diagnostic evidence rather than shipping a framing change.
