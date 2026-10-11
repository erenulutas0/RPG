# Foundry workyard identity

Baseline `10d5e01`. Before images are unchanged accepted `foundry-mass-rhythm-01/after` captures. After views use the same cameras and staged actors. Approved target: `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`.

## Changes

Two existing Blender clusters are recomposed in place, rather than adding more props to already occupied shelves. Stock has two upright coils, supported five-pipe bundle and staged billets; casting has a broad active mould, an angled removed half, a taller closed flask and finished stock. Original press, quench tank and tools remain. Editable sources, unchanged transparent PNG renders and projection metadata are in `../workyard-clusters-01`. The two existing Resources PNGs are replaced without changing their GUIDs, import contracts, card anchors, dimensions, depth ownership or renderer counts. Runtime imports remain 512 ASTC6x6 with 10 mips and no CPU-readable copy. The Blender source meshes are not runtime geometry.

The west squat boiler receives a shallow seated inspection lid with hinges and a small handle. Its native geometry is emitted immediately after that boiler's image card in the same upper-shell batch and samples opaque metal from the existing texture. Original cards, material ownership, two bands per pair, footprint, height, flame and pipe anchors remain. No new material, renderer or per-frame component. Camera, walkable map/ramp, scene, gameplay and saves are unchanged.

## Review and limits

The first capture was rejected: the roof hatch read as a table with legs and the tall boiler's light oval cover looked pasted onto the richer bake. The tall overlay was removed completely; the roof hatch became a lower, smaller lid seated directly on the hood. First rejected gate/portrait/full-map images and graphics XML remain in `validation/rejected-first`.

Final actual Unity review shows grounded pipe supports, distinct mould stages and a clear walking lane. The live crane partly hides the coils; this is consistent spatial overlap and limits their benefit. The lid is a local detail, not a new boiler silhouette. Workyard improvement at whole-map scale is modest. Larger boiler variation needs a coordinated authored bake, not bright flat overlays. The reference's broader workyard distribution and dominant backdrop proportions still need work.

Three helpers split native source authoring, boiler/test integration and independent visual review. Root alone ran Unity. Region remains 32 renderers; 118,537 -> 118,709 vertices (+172, about 0.15%). This is a resource count, not a performance measurement.

## Verification and deployment

Final results and APK identity belong in `validation/verification.md`. Before XML is historical evidence, not another execution. Two new after graphics captures ran. Browser controls compare matching Unity views with the approved concept. Portraits use production camera width with hidden HUD; these are not phone screenshots.

No ADB action or fresh device observation in this slice. Earlier concrete owner-run restart/install decision remains pending. On authorization inspect current state, back up current original files, install/hash-check the latest package, and preserve current saves/telemetry. Never restore stale snapshots. Device/crowd/midrange/first-ramp acceptance remains open. Regional encounters/boss progression remain deferred under the owner's art-first direction.

## Reproduce

Close Unity, then run `pwsh -NoProfile -File Tools/ArtReview/Run-FoundryWorkyardIdentityProof.ps1 -Phase after`. Phase selects the output directory; it does not revert source. Temporary capture source/meta files are removed in `finally`. To regenerate the authored sources, run portable Blender with `--background --python Tools/ArtReview/BuildBlenderStockStation.py` or `BuildBlenderCastingBench.py`. Each script writes its versioned output path defined by `OUT`; retain matching projection metadata with each render.
