# Foundry rim: open fire baskets on the balustrade posts

Verified 2026-10-21. Baseline `15a5574c4479d3c7d1b60b2befa3772f8d9b6c9f`.

The concept lines its rim with open fires on stone posts; our balustrade lantern posts carried enclosed amber lanterns
that read small and pale at map scale. `LanternPost` now seats the gate's / bridge head's fire basket (`Brazier`, cast
foot, bowl, bronze rim, coals and three animated tongues) on the post's 1.69 cap (rise 1.79). This covers every
`RailLanterns` post and the pit collar's corner posts, which share `LanternPost`. Placement is unchanged (the post still
passes the lantern-era 2.8 screen test); the post's depth piece rises to 3.1 to cover the flames. Floor pools unchanged.
No new batch (the brazier iron and flame batches already exist): region 38 renderers / 159,800 vertices (+2,108).
Bridge-rail lanterns and the four court lantern finials stay lanterns, as in the concept's bridge.

Verification: 457 .NET / 732 EditMode / 164 PlayMode, zero failures; Verify-Project PASS; APK 36,412,761 bytes, SHA256
8895C265833F4B3DF1F845F72CE60E8E3E8ABCFE7DB3D021EA8EDF65B9017F66, NOT INSTALLED. Source freeze
`ef540c22b29b06877595d71e431fa1d2694fab24d46777cf0082b5272c8558da` matched after the final capture and the full suites.
`validation/comparison.png`: concept / Before / After; `candidates/01-baskets` is identical runtime before the freeze.
