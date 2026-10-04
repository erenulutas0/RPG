# Gate, bridge and court stone finish — 2026-10-05

- Replaced LitFoundryTier's obsolete, hardcoded furnace lighting with the shared source-height-aware architecture light. Authored lower/upper elevation is recovered from the tier's emission order, never inferred from projected screen Y. Existing face shading and UVs remain.
- Applied that treatment to stone bridge piers, lantern supports and court finials. Iron tiers retain their material treatment.
- Added four compact header ties with return faces/bolt heads and central mullions in the gate's front recesses. Existing gate depth scope and iron batch are reused. Painted doors remain untouched.

This is a restrained architectural finish, not a new layout or a claim of concept-image fidelity. No gameplay, collision, camera, save or telemetry changes. Static build-time code only. Actual Unity capture passed 1/1; compile 0 warnings/errors; 17 renderers / 76,492 vertices (+72). Matched before/after views accompany this file. Full gate and device verification completed below.
## Verification and device closure

Compile: 0 warnings/errors. Actual graphics capture: 1/1. EditMode: 410/410. PlayMode: 135/135. Android build exit0. Integrity: 532/1099/568. Diff check clean. APK 32,374,745 bytes, SHA256 76E4B83DFD613A939EDF5DD03899832181D9822FBB5A64769B7E93A99863FDB4. Installed S23 base.apk has the identical hash.

Fresh backup: TestResults/gate-install-20261005. Existing profile revision221 and profile backup hashes unchanged through install and review. No profile restored. Temporary depth-proof/render-stats flags removed; original development filenames and hashes match (region-preview.txt, start-floor.txt.off). Game stopped after review, normal foundry preview on next launch.

Matched device depth view: before and after 59.9fps / GPU3.7ms / 57 draws / 63.8k triangles / about127.5-127.6MB. This is a static S23 development review, not a combat-load or midrange benchmark; startup windows contain frame spikes and are retained in raw stats. Two guarded drags (3s up-left,6s up-right) reached the gate approach; paired captures show movement/camera and the modified court posts. Gate header close-ups are Unity captures, not visible in that phone frame. Current-process Unity exception/shader-error matches0. Local commit only, no push.