# Correction of the initial keeper-overpaint claim

The independent reviewer initially reported new overpaint of the right entrance keeper in the matched scene-cohesion bridge-detail and forecourt-detail captures. That report was incorrect. Root challenged it with a numeric comparison, and the reviewer independently verified the actual bridge-detail PNGs with Pillow 12.3.0 and NumPy. No image was edited for this analysis.

For each pixel, the metric is the maximum absolute difference of its RGB channels. Rectangles below use zero-based coordinates and exclusive right/bottom edges, as in NumPy slicing.

| Region | Rectangle (left, top, right, bottom) | Pixels | Any changed | Changed by more than 1 | Maximum RGB difference |
| --- | --- | ---: | ---: | ---: | ---: |
| Broad hood and silhouette | (829, 340, 897, 388) | 3264 | 0 | 0 | 0 |
| Hood core | (851, 350, 872, 383) | 693 | 0 | 0 | 0 |
| Chest core | (848, 388, 878, 435) | 1410 | 45 | 0 | 1 |
| Body core | (850, 435, 880, 490) | 1650 | 94 | 0 | 1 |
| Pedestal core | (840, 507, 892, 526) | 988 | 82 | 0 | 1 |
| Original claimed rectangle, including background | (830, 345, 870, 500) | 6200 | 333 | 156 | 21 |

The whole hood/silhouette being bit-identical directly disproves the reported new hood clipping. The broader initial claim rectangle crosses changing masonry beside the keeper; those changes cannot be attributed to opaque statue overpaint. Small changes at the body's translucent edges can reflect background changes, but no such edge explanation is needed for the identical hood. Core differences are at most one channel level.

Input SHA256:

- Before bridge-detail.png: `367E1759C4D1DB897C133C8AF26E40E52FC3BD4FAA03FC6BCE6F168B2B581FAB`
- After bridge-detail.png: `16325E395EFEB4AAD662C84D5456F1166377BA0079F5369276827D5939E04B84`

The preliminary rejection is withdrawn, and independent-image-review.md now accepts this same candidate as a bounded static art improvement. No source or image change was required to resolve the review error. This is a review correction, not evidence of a repaired runtime defect.
