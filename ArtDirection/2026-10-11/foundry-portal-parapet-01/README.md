# Foundry portal and parapets — actual Unity proof

Baseline: `e61ac5a`. Before images/XML/budget are reused unchanged from the final accepted `foundry-stonework-01/after` capture. New after images use identical cameras, dimensions and staged actor positions. Approved target: `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`.

## Combined implementation

The gate's two overlapping central stone boxes and small diamond are replaced by a dressed octagonal cartouche: recessed rear core, supporting side cheeks, broad bevelled frame, deep inner reveals and a restrained cast-iron locking plate. The existing crown supports it. It stays within the previous housing envelope, below the crest, and the painted door's large gold seal remains primary. Source: FoundryRegionArchitecture.cs and FoundryRegionGateHeader.cs.

Exterior RailSpan bays now have a 0.34-high stone sill, solid end responds, recessed square iron uprights and a broad quiet coping. The original 0.42 footprint band, 0.72 top height, placement exclusions, posts, braziers and depth ownership remain. Pit and bridge retain their separate open balustrades. Source: FoundryRegionMassing.cs. Existing stone/iron batches are reused; no new texture, material or per-frame component.

## Visual acceptance

Independent code review also found no actionable span-bound, painter-order, depth or resource defect in the exterior change. Root and independent reviewer accepted the actual overview, gate detail and production-width gate/workyard/boiler-approach portraits. Exterior edges read as more substantial industrial masonry; open pit/bridge rails preserve variation. No new visible floating cap, bad corner/boiler return or hero obstruction was found in these views. This is visual sampling, not exhaustive device/crowd acceptance.

The portal improvement is most visible close up. Long parapet bays remain regular and the overall reference's richer architectural variety is still incomplete. This package does not finish the reference map. Walking/ramp geometry, camera, gameplay, scene and saves are unchanged.

Region resources: 32 renderers unchanged; 118,082 -> 117,629 vertices (-453, about 0.38%). These counts do not establish frame-time or allocation performance.

## Verification and phone

Final suite/build evidence is recorded in `validation/verification.md`. Before evidence is reused, not a new execution. One new after graphics capture ran. The gallery compares actual Unity renders with the concept; portraits use production camera width and hidden HUD, not phone screenshots.

No ADB action or fresh device observation in this slice. The earlier concrete owner-run restart/install decision remains pending. On authorization inspect fresh, back up current files, install/hash-check the latest accumulated APK and preserve current original saves/telemetry. Never restore historical snapshots. Physical-device/crowd/midrange/first-ramp acceptance remains open. Region encounter/boss progression remains deferred by the art-first direction.

## Reproduction

With Unity closed, run `pwsh -NoProfile -File Tools/ArtReview/Run-FoundryPortalParapetProof.ps1 -Phase after`. Phase chooses output directory only; it does not restore historical source. The runner removes its temporary Assets capture source/meta in finally. Root alone ran Unity; three helpers split gate, exterior parapet and independent review.
