# Bastion stonework — 2026-10-07

Six existing foundation heads now use three upward-built stages: recessed neck, chamfered capital and dressed upper slab. The slab samples an interior stone patch rather than wrapping wall mortar over its bevel. Alternating dressed quoins wrap the near corner below the head, clear of the existing central chain anchors.

Runtime edit: FoundryRegionMassing.cs only. Reuses ArchitecturalTier and GateDressedFace from the partial view. Existing footprints/head envelope, chain anchors, undercroft depth shading, textures, materials, camera, gameplay and saves unchanged. No dynamic work. Region18renderers /84,160vertices (+1,760).

Actual Unity same-camera comparison: before/undercroft.png and after/undercroft.png. Overviews show the six-head context. Local structural finish, not completion of the painted target.

Final verification and device result follow below.

## 2026-10-07 - Bastion dressed stonework installed

FoundryRegionMassing.cs refines six existing foundation heads with upward-built recessed necks, chamfered capitals and dressed slab crowns. Crown UVs sample stone interior; alternating dressed corner blocks wrap the near corner below the head. Reuses existing partial-view helpers. Head envelope, shaft forms, chain anchors, undercroft shading, textures/materials, camera, gameplay and saves unchanged. No dynamic work or new renderer:18region renderers /84,160vertices (+1,760). Proof ArtDirection/2026-10-07/bastion-stonework-01: actual same-camera undercroft/overview comparison and S23 edge view. Local structural finish, not full painted-reference fidelity.

Verified .NET390/390, compile0warnings/errors, final graphics1passed including traversal, Edit410/410, Play145/145, Android0, integrity544/1099/568. Installed APK C28CD8853452E55071AD028043199DBB7DD211FC13C2DE11794CA269A8D4D4EF (35,963,068bytes); installed base.apk SHA256matched. Fresh backup TestResults/bastion-device-20261007-230129 with app already stopped. Guarded depth-proof southeast-edge approach shows two bastion crowns/corner stones; full six reviewed in Unity overview. Normal restart captured Room1 combat rewards at90HP. Sampled logs had no selected managed exception/crash patterns; no new performance benchmark.

Profile,bak and all3 original development files SHA256-identical. Temporary depth-proof removed; original region-preview/gate-arrival/start-floor.txt.off retained. Background flush then app stopped. Telemetry retained; no restore. No push.