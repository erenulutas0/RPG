# Foundry rim stone: the concept's violet battlement tone

Verified 2026-10-21. Baseline `e53eaee5c514c8b74fd287a49f6008ca33a062b4`.

Measured on the full-map framing, the rim's stone had about the concept's value but half its violet saturation
(balustrade .20 / .19 vs the concept's rim .18-.19 / .32; below-deck faces .18-.20 / .21 vs .19-.26 / .32). A first
probe region had sampled floor, not rail - the changed-pixel mask (validation) was used instead.

- Above the deck (balustrade spans, posts, lantern posts, court finials, pit collar guard and posts): tint
  (.80, .77, .92) after construction -> value .18, saturation .32.
- Below the deck (balustrade cornice, pilasters, court sidewalls): tint (.90, .88, 1.06) -> same value, saturation
  .25-.26.

Geometry, texture mapping and relative light are unchanged. Region 38 renderers / 159,304 vertices (unchanged).
Candidates: `01-dark-rail` (above-deck only, value .17 - slightly too dark), `02-violet-faces` (identical runtime to After).

Verification: 457 .NET / 732 EditMode / 164 PlayMode, zero failures; Verify-Project PASS; APK 36,413,341 bytes, SHA256
2AA02A3A7B3701DC093C18C04393DDE4495E84DDB46895FC28B3C132A0DDFAD0, NOT INSTALLED. Source freeze
`23c40e6dd1e9aecd3d4ef8cb2ea5deafc48fc744daee9dfc4014556aeba93f3f` matched after the final capture and the full suites.
Remaining: the concept's rail posts carry brass bands and caps (next package).
