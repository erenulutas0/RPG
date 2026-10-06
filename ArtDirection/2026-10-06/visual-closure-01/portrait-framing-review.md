# Portrait gate composition review — 2026-10-06

## Evidence and scope

Inspected actual Unity captures `after/gate-approach-22.png`, `after/gate-approach-26.png`, `after/gate-approach-28.png`, the final second-pass `after/gate-approach-centred.png`, and `after/gate-approach.csv`. All are 1080 × 2340 with HUD enabled. The CSV confirms the current production follow width of 9 world units and orthographic size 9.75 at each point. These are staged positions in an isolated test profile, not walking, combat, phone or performance evidence. The phone was unavailable.

## Observed composition

| Design position | Visible landmark information | Cropping and HUD obstruction |
| --- | --- | --- |
| (22, 6) | Broad apron, clearly readable hero, near boiler and balustrades establish the route. A narrow portion of gate-side masonry/fire enters the upper-right edge. | The actual door and central gate identity are outside the right edge. The top HUD also covers a broad strip of boiler/tower scenery. This cannot establish gate framing. |
| (26, 6) | Gate-side tower, hanging blue banner, brazier and part of the door enter the upper-right area. Hero and movement ring remain unobstructed. | The door is strongly cropped on the right; the top HUD crosses upper masonry. The complete pediment is not readable. |
| (28, 6) | Door seal, much of the door, banner and brazier clearly identify the destination. The threshold and open approach remain readable around the hero. | The right edge cuts the door/facade and excludes the far side. Upper architecture crosses the top HUD and continues beyond the top edge. This still does not show the complete landmark. |

The approach successfully reveals the gate progressively, but the three points all place its projected centre to the hero's right. They do **not** isolate vertical framing from lateral cropping. The HUD remains useful and the hero silhouette reads; neither establishes that the full gate silhouette fits.

## Additional diagnostic capture

The capture tool now also stages **(30, 2.82)** as `gate-approach-centred.png`. This is on the existing apron and has the same `u-v` as the pediment centre `(32.18, 5)`, so the unchanged follow camera centres its screen x on that landmark. The same walkability, camera-settling and enabled-HUD assertions apply. This test-only point helps distinguish genuine vertical/HUD obstruction from the earlier lateral approach. No camera, HUD, scene or movement bounds were modified.

The final centred capture passed and was inspected. The door seal sits near horizontal centre; the broad door face, both blue banners and both braziers are identifiable. The hero and nearby approach floor remain unobstructed. This resolves the earlier strong lateral cropping of the central doorway. However, the top HUD masks a broad strip of upper door/header architecture, the pediment continues into the top screen edge, and the full flanking towers still do not fit horizontally. The complete gate silhouette therefore **does not fit this portrait composition**, even when horizontally centred. The CSV records camera `(19.2192, 11.8786)`, floor position `(19.2192, 23.2072)`, unchanged size `9.75` and width `9.0`.

This is a usable destination view with a readable central emblem, not a full architectural reveal. Further small gate details will not resolve the measured composition limit. Any camera or HUD adjustment should be a separate deliberate decision supported by these portrait views; a square closeup cannot settle it. No such adjustment was made in this package, and phone review remains pending.
