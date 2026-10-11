# Gatehouse and dressed-stone approach

Baseline: 857a99c1170222a27a0b446b43d707657183fcd8.

The owner requested grouped visual work toward the approved foundry reference, with up to five helpers. Three source authors own disjoint files; the fourth handles integration review and meaningful geometry tests; the fifth reviews actual Unity images independently. Root alone runs Unity and deployment.

## Combined visual scope

- Lower the outer gate buttresses from9.2 to7.35 by removing the third storey and seating a broad coping against the curtain wall. Retain the original foundations, footprints, service sockets and walking space.
- Replace nested portal shoulders with a low interrupted parapet. Header maximum is7.66 rather than8.05. The central relief and iron lock share a compressed rise so the lock stays in its reveal. Shorten inner iron terminals8.45->7.94 so they fit the existing8.2 depth envelope. Broader dressed jamb returns and quieter niches keep the door primary.
- Replace four small side-landing steps with three broad supported tiers, within the original outboard boxes and1.02 height. The playable ramp remains unchanged.
- Assemble the processional field from four staggered courses of flush flagstones and connect quiet edge courses to the original threshold. Retain the brass motif, common floor lighting/UVs and triangle splitting through the u25/u27 ramp breaks. No raised obstacle is added.

Existing door texture, furnace atlases, materials, camera, HUD, navigation, encounter data and save formats remain. This is native procedural architecture; no generated raster or external asset was introduced.

## Proof and reproduction

`pwsh -NoProfile -File Tools/ArtReview/Run-FoundryGateStoneProof.ps1` runs an **after-only** Android-target Unity capture. Fourteen before images are byte-identical copies from the industrial proof, recorded in validation/provenance-sha256.txt. The frozen portraits stage the hero with the existing production follow-camera width. HUD is hidden; these are Unity screenshots, not phone or crowded-combat acceptance.

Review whole-map, gate detail, gate portraits22/26 and north/south pipe contacts together. Final runtime observations:32renderers and129812vertices, versus125234baseline (+4578/about3.7%). No additional runtime texture/material instance/updater. These are not physical-device measurements. Final tests/build identity is retained under validation. Native sources are in FoundryRegionArchitecture, GateEnclosure, GateHeader and Heraldry partial classes.

## Remaining acceptance

Reference material/architectural richness and accumulated device circulation, crowds, ramp motion/shimmer and midrange performance remain unfinished. No phone action is part of this package while the prior concrete install/restart decision is pending. Before any future install inspect fresh state and preserve current original saves/telemetry; never restore historical snapshots. Region encounter and boss progression remains deferred by the owner's art-first direction.

## Actual-image acceptance

The first capture passed geometry checks but its slab bond was too faint to read. Independent art review rejected that aspect. Retained evidence:validation/first-floor-pass. Mortar .80, joint .024 and bevel .045 preserve the face tones while making the course structure visible. Second capture was accepted for the combined package. Fourteen baseline views remain byte-identical; their cameras are unchanged. See validation/actual-art-review.md for remaining reference and device limits.
