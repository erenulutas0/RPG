# Foundry rail lantern shrines

Verified 2026-10-23. Baseline `5ccf3f7`.

On the concept the long rail runs alternate open fire with small stone lantern shrines - blocks with a lit, round-headed
niche, a cornice and a pyramid roof. Owner's choice (2026-10-23): convert some of the fire-basket posts. Four rail
lantern posts (`RailShrinePosts` 7, 8, 9, 11: the south rail's two mid-run posts and the north rail's u 1.5 and u 6)
now carry `RailShrine` instead of the fire basket: a .62 stone block on the post's cap (1.69-2.42) with a moulded
cornice, a pyramid roof to 2.8 and a brass finial, a dark reveal and a glowing round-headed niche in each camera face
(the court lantern's amber pane mapping in the flame batch, brighter). The corner posts, the north rail's u -2 post and
the causeway post keep their fire. Placement, depth pieces and floor pools are unchanged; no new batch.

Region 38 renderers / 155,650 vertices (-432: a shrine is lighter than a basket's flames).

Images: `before/`, `after/` (39 matched views), `compare-concept.png` (north rim before / after / concept rim).

Verification: 457 .NET / 738 EditMode / 164 PlayMode, zero failures; Verify-Project PASS; APK 36,465,383 bytes, SHA256
F8C1C1491FA4D63DDC26B60A709086D9F41F04C906A02D6E1B79D61C35C7A4AB, NOT INSTALLED (the owner is using the phone). Source
freeze `eea42775f59d3ba8ef8b15e86838fb8b7fc3f212964c63027a03bd5801788a59` (1,273 files) taken after the final After capture,
matched after the full suites. Gameplay: none.
