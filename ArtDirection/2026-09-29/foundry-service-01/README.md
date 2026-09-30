# Foundry service package - 2026-09-29

Combined code-native scenery pass following foundry-integrated-01. Three helpers handled workshop geometry, shaft hardware and independent visual review; root integrated floor shading and bridge lighting.

## Changes
- Extended the existing north decorative shelf outward, supporting an open quenching basin, six billets on sleepers and two hollow strip coils. All props remain outside the walkable area.
- Added 32 metal drainage trays within the existing shaft coping and three clevis/saddle connections at existing chain heads.
- Added subtle static contact shading around the gate and four boilers, and a restrained worn approach strip.
- Added outer bridge torch sources; subdivided the bridge floor across its width to keep light pools near the edges.
- Reused existing textures, materials and rendering orders. No gameplay, movement, collision, camera or balance changes.

## Evidence
Before images preserve the prior integrated package. After images are actual graphics-enabled Unity captures, not concept images. Capture test passed 1/1. Static integrity passed (515 project GUIDs, 1099 package GUIDs, 568 scene objects/components).

Final static geometry: 17 renderers, 54,840 vertices (previous 52,908; +1,932). Existing atmosphere renderer is separate and unchanged. No new per-frame art work.

Visual review found no blocking clipping, unsupported props or combat readability regression. Tank rim, stock supports, coping recesses and bridge torch pools were checked in closeups. This is a detail pass, not a claim of full reference-image fidelity.

Android installation and device performance have not been tested for this package. Full-suite/build results are recorded below when complete.

## 2026-09-29 - Combined foundry service, shaft hardware and floor-light pass

Completed in one visual package: supported north-shelf quenching basin, reserve billets and hollow strip coils; 32 recessed-looking shaft coping trays and three chain clevis/saddle connections; subtle gate/boiler contact shade, worn approach and outer bridge torch pools. Bridge floor is subdivided across its width for localized lighting. Reuses existing materials/textures and static batches; no gameplay, walkable bounds, collision or balance change.

Evidence: ArtDirection/2026-09-29/foundry-service-01/index.html and README.md. Before/after and closeups are real Unity captures. Graphics capture 1/1 passed; 405 EditMode + 130 PlayMode passed; static integrity 515 project GUIDs / 1099 package GUIDs / 568 scene objects/components passed. Final static budget 17 renderers / 54,840 vertices (+1,932 from prior package); unchanged separate atmosphere renderer. Three helpers covered workshop, shaft hardware and visual review. No blocking visual defect found; reference-image fidelity is still a work in progress.

Android build succeeded. APK Builds/Android/Cryptforge-Prototype.apk: 39,340,606 bytes, 2026-09-29 17:45:55 local; SHA-256 DFF2E84065DA0CCA3B80C8580B89814C30107D2AC889B5514D3E6176ED62529D. NOT installed or device-tested in this session. Last known installed phone APK remains 18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535; do not infer current device state.