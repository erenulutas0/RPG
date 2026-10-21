# Foundry gate doorway: a lit hall beyond

Verified 2026-10-21. Baseline `92a8901647eb0df96228b26046cde96a0dcd270c`.

The open gate's doorway was one vertical gradient. It now reads as the forge hall beyond (`HallLight` over a 6 x 6
vertex grid): a warm floor receding from the threshold, the glow of a far furnace mouth on the back wall and a dark
vault above. The open right leaf hides the opening's right half from the camera, so the furnace stands left of centre
(across .32), where it is seen. Same batch, flat texel and vertex-colour approach as before; +32 vertices (region 38 /
154,864). The closed gate is unchanged (the doorway is only seen open).

Candidates (actual Unity captures, `gate-open-detail`): `01-central-furnace` (furnace centred: hidden behind the open
leaf, visible part darker than before - rejected), `02-left-furnace` (identical runtime to the final After).

Verification: 444 .NET / 719 EditMode / 164 PlayMode, zero failures; Verify-Project PASS; APK 36,412,123 bytes, SHA256
6F1993C9227E49DF9737A9BE5BE0AC39B88912C18DD3F906BA082712762A020A, NOT INSTALLED. Source freeze
`7d8937d5cd39806b9a694d7d89c401d1a954e9ba423a94430bb5d0c24a1006da` (1,261 files) matched after the final capture and the
full suites. Bounded: still a painted light, not a modelled hall.
