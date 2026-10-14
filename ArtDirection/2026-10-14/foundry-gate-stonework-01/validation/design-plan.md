# Gate stonework plan

Baseline e0bd846e4ba7ecc0f1ec3b8ca6da70847bcfb1d3. Root captured and sealed 21 fresh scene views with unchanged runtime Assets before releasing implementation.

The local reference gap is the tall smooth stone beside the richly detailed painted portal. We considered stronger global texture contrast and added decorative strips, but neither supplies believable stone construction. This package instead fits broad courses into existing architectural faces.

- Recessed shafts: replace long single-piece body/jamb faces with fitted broad courses and a seated pointed head. Keep the exact opening, splay, backing, corner and cap stations. An indexed stone emitter is intended to retain the existing face vertex allowance rather than multiply repeated quad corners.
- Adjacent curtains: replace the old complete wall plus coplanar overlaid ashlar with a single partition of real stone and mortar. Retain the existing taper/cap and previous dressed coping. Use one bounded mineral field per visible wall face, with broad courses comparable to the towers.
- Lower buttresses: retain geometry, UVs, upper bearing colours and existing contact shading. A small neutral material gain on the lower wall's actual vertices adds a washed mineral body above the damp foot. It is applied once per complete face, never per bevel or stone. No general brightness or saturation change is intended.

Two implementation helpers own disjoint recess and enclosure source/test files. Root owns the lower buttress finish, integration and verification. A third helper reviews source and actual images independently. Root alone runs Unity and Android builds; Assets stay frozen during them.

Keep the current map, gameplay, camera, navigation, resources and existing 174,000 region vertex ceiling. Visual acceptance requires stone joints to read as construction at portrait scale without stripes, extra silhouettes, opaque niche filling or lost hero hierarchy. Full reference fidelity remains unfinished. Phone acceptance is still pending under docs/23 and docs/38.
