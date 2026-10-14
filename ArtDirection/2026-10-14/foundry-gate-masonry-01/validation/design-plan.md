# Gate and masonry package

Baseline: d34c56115fc0a771f648baf3601ab740a10b871d. Twenty-one fresh matched scene views were sealed before Assets edits, including a new gate-crown view that shows the top of the outer towers.

1. Header: articulate the existing lintel fascia as a supported recessed stone profile. Preserve its upper bearing, footprint and iron contacts. This replaces the plain wall surface instead of adding another decorative overlay or crest.
2. Dressed enclosure stone: fit broad stones and registered joints across the existing fascia/caps. Preserve the tower/curtain envelopes while bridging the material-scale difference beside the painted door. Do not increase global grain noise.
3. Threshold: remove only the procedurally generated central rail behind the closed portal. The facade at u=27.99 already backs the landing's u=28 edge. Retain side returns and both explicit brazier posts; the movement boundary and closed-door semantics remain unchanged.

We considered increasing global texture contrast or adding more decorative layers. Neither fixes the oversized plain profiles, disconnected grain or fenced threshold. The chosen work changes the existing architectural surfaces and keeps the same renderer/material system.

Root owns the rail and actual scene verification; two helpers own disjoint header/enclosure source and focused tests; a third independently reviews source and actual scene evidence. Root alone runs Unity. Final acceptance requires readable bearing/recess depth, no extra overpainting or unseated caps, preserved actor hierarchy and full regression/build checks. Phone acceptance remains pending, as recorded in docs/23 and docs/38.
