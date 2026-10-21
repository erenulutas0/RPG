# Foundry workyard braziers

Verified 2026-10-21. Baseline `3ea949d432c4aa86e431f2ebbf10e921c3ad38b7`.

The rim survey (`../foundry-rim-fire-01`) left no rail post free beside the workyard, so its fire now stands in the yard
itself, as the concept's fire baskets do. Three standing braziers are new work islands (`FoundryMap.WorkIslands` 2-4,
`YardBrazierFirst`): .9-square solid footprints shared by rendering, hero movement, pack placement and pursuit, exactly as
the trolley and moulding bench are. Positions (design centres): north-west quadrant (-28.6, 12.4), south-west quadrant
(-28.4, 1.5) and beside the south freight rails toward the pit (-20.5, 2.5).

Each is the island's iron skid, a stepped cast foot, a short column and the bridge head's fire basket (`Brazier`) at
rise .55, inside the island's depth structure; each adds a ForgeLamps floor pool (strength .55). No new batch or texture:
region 38 renderers / 156,364 vertices (+1,197).

Constraints (pure test `YardBraziersStandClearOfTheFreightLanesAndLeavePassages`, plus the generic island navigation
tests now run for islands 0-4): clear of the turntable bed, the west loading lane and the freight rails by at least 1.9;
at least 1.9 from every other island; walkable floor on every side. Two placements were moved during the work because
existing layout probes use them: (-30, 12) is the canonical retained-yard point and (-29, 15) a corner-chamfer probe.

Candidates (actual Unity captures): `01-lantern-column` (the south-west basket stood in the same screen column as the
west rail lantern and read as its crown - moved), `02-clear-columns` (identical runtime to the final After).

Verification: 457 .NET / 732 EditMode / 164 PlayMode, zero failures; Verify-Project PASS; APK 36,413,279 bytes, SHA256
2AC415A86ECFEFFEE73702644F183DBAED7DB3D97DA3777FE310D8A0C690A804, NOT INSTALLED (the owner is using the phone). Source
freeze `8427bb5f05008e1879b32b475b7fd57d7b2d4e548c489a11155c33f647b3d24d` (1,261 files) matched after the final capture and
the full suites. Gameplay note: these are real obstacles in the yard (actors walk round them).
