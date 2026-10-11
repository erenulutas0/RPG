# Compact gate and furnace crowns

Baseline: `f5e14874869e792179cdaf86bd4327958ab1c11f`. Ten selected actual Unity views and five diagnostics were copied unchanged from `foundry-broad-court-01/after`. SHA-256 provenance is retained under validation. Baseline copies are not a new execution.

## Combined scope

The owner requested continued multi-agent visual development toward the approved broad foundry reference. This package addresses the long gate arm and the large native northern silhouettes together.

The main court remains 50 design units wide, with its 20-unit western workyard. Gate landing shifts from u28..32 to u24..28; court edge to gate end becomes 18 to 14 units (22.2% shorter). Ramp shifts from u29..31 to u25..27 and retains its 1.25 rise and two-unit length. Gate door/towers retain their size. The pit, bridge and interior work islands stay fixed. Forty square units are removed from the gate approach, not from the central court.

FoundryMap owns the gate offset/landing boundaries, FoundrySurface derives the ramp, and pursuit uses the relocated landing. Rigid gate masonry, facade, contact shade, depth owners, lights, rail posts, threshold and suspension chain translate coherently. The processional frame shortens while its entrance stays fixed; the centre ornament moves halfway. No global warp of the projection is used.

Near boilers retain their positions. Only the far pair moves four units closer. Pressure reservoirs move outward to avoid the tighter boiler cluster; short service mains use a single seated saddle and low connecting masonry ties tank foundations back to the near boiler plinths. All such scenery remains outside the shared walking surface. The old unrendered south-bench reservation is removed after a literal/emitter audit, not to clear a real obstacle.

Northern furnace shafts keep their anchors, arched bays, connected buttresses, facade UVs, atmosphere and vertex budget. Existing upper tiers are recomposed into compact shoulders, longer upright necks and different seated exhaust crowns. One is tall/open, the other a broad low hood. No extra texture, material instance, renderer or updater is introduced for this silhouette work.

## Evidence and reproduction

Ten matched cameras: overview, full-map, gate-detail, court-detail, skyline-detail, approach-20-1, approach-0-0, expanded--27-6, expanded--26-15 and broad-yard. Cameras retain identical positions/sizes to show proportion changes honestly. Additional after-only gate-new-22 and gate-new-26 inspect the new approach/ramp with the production portrait framing and placed hero. HUD is hidden. These are actual staged Unity views, not phone screenshots.

```powershell
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryCompactGateProof.ps1 -Phase after
```

Phase selects output only; it does not restore earlier source. Root alone owns Unity execution. Three helpers cover compact layout/services/tests, native crowns and independent reference review. Root and independent final-image review accept the compact gate composition. The initial graphics capture exposed a stale far-boiler discharge; both mouths now follow BoilerCentres and the corrected final capture removes the floating strip. First diagnostics, emitted-mesh contact checks and the corrected image review are retained under validation. Suite counts, resource budget and final APK identity are recorded in validation/verification.md.

## Delivery limits

No ADB action or fresh device observation is part of this package. APK build is separate from installation. The earlier concrete owner-run restart/install decision remains pending; any later phone review requires fresh state inspection and current-file backup, preserving current original saves/telemetry. Never restore historical snapshots. Device movement, crowd, midrange, first-ramp and shimmer acceptance remain open. Full reference fidelity and authored material richness remain incomplete. Regional progression and bosses remain deferred under the art-first direction.


## Next substantial art candidate (not implemented)

Independent reference review recommends the gate's industrial flanks: whole furnace silhouettes still read as repeated rounded boilers compared with the reference's upright stacks and asymmetric service architecture. A later package can rework the authored furnace bakes through Tools/ArtReview/BuildBlenderFurnace.py and BuildBlenderShortBoiler.py while preserving centres, footings, sockets and foreground height envelopes. Validate matched overview/full-map, both gate portraits, gate detail, approach and foundation/drain contacts. Distinct upper profiles and coherent stone/iron finish should remain visible at whole-map scale. Do not add unsupported exhausts or break existing pipe contacts merely to make the silhouette busier. This is a candidate scope, not a completed source-asset change.
