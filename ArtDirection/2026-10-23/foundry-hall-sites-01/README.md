# Foundry halls: later rooms open at their own districts

Verified 2026-10-23. Baseline `03a311f`. Gameplay change, owner's choice (2026-10-23: "Odalar haritaya yayilsin").

Until now every room of a foundry floor was fought on the spot where the hero stood, so the map's districts never
mattered. Now, on the foundry region only, a floor's combat and elite rooms after its first open at a district of their
own (`FoundryMap.RoomSite`: the workyard turntable (-25, 7), the court's north (4, 12) and east (8, -8), the forecourt
(-8, -12), then round again): when the previous room clears, the next room waits, marked by the exit seal at that
district with the off-screen arrow and a "NEXT HALL" label (`PrototypeTextDefinition.HallInstruction/HallDirection`),
until the hero walks within `FoundryMap.RoomSiteRadius` 1.6 of it; its first wave then forms up round the hero there as
waves always have. Later waves of the same room, forge rooms (choices in place), the gate boss and the swarm are
unchanged; an open choice keeps a marked room waiting. The hall region and the Descent simulation are unchanged
(scene-only, like the gate boss); ground drops are collected on the way.

Code: `EncounterController` (`TryRoomSite`, `IsRoomSiteOpen`, `RoomSiteFloorX/Y`, the marked-room wait in `Update`),
`FoundryMap.RoomSite`, `ExitSealView` (hall mode: no hold arc, the district's radius), dev floor
`Resources/Development/Floor_RoomSitesProof` (three one-grunt rooms, the last elite).

Tests: new PlayMode `RoomSiteFlowTests` - on the foundry the second hall opens at `RoomSite(1)` on walkable floor,
marked, nothing starts while the hero stays away, stepping in starts it with the pack round the district, the mark goes,
the elite hall opens at `RoomSite(2)`, and the floor clears after it; in the hall region every room runs in place.

Images (actual scene, portrait game camera, `Tools/ArtReview/FoundryHallSitesCapture.cs`): `hall-marked-offscreen`
(arrow and label at the screen edge), `hall-marked-near`, `hall-fight`, `strip.png`.

Verification: 457 .NET / 738 EditMode / 166 PlayMode, zero failures; Verify-Project PASS; APK 36,467,560 bytes, SHA256
B936F55EDF5DF2527DD670CA281E4A08421A2045AE348AC81336BFA4565DBEC4, NOT INSTALLED (the owner is using the phone). Source
freeze `9760f3d616329afc00461f77c8a74ef6294646103df26f1d28bd81fc5467e3c9` (1,277 files) before and after the full suites.
Not yet played on the phone: the walk between districts (up to ~26 units) adds time between rooms; whether that pacing
feels right is the owner's call on device.
