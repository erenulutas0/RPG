# Foundry backdrop depth

Baseline `49cf944`. Before views are unchanged accepted `foundry-workyard-identity-01/after` captures; hashes are in `validation/provenance-sha256.txt`. Target remains `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`.

## Art and integration

Built-in image_gen edited the old furnace cavern plate, with the approved foundry concept as a supporting style/depth reference. The giant foreground flank buildings are replaced by smaller staggered tower, crane and bridge groups with haze gaps. The middle remains dark and quiet for the independent playable island. No painted playable floor, characters or interface. `source/prompt.txt` is the complete final prompt. `source/FoundryCavernV2.png` is the unmodified generated output (1024x1536 RGB), copied byte-for-byte to `Assets/_Project/Resources/FoundryCavernV2.png`. Original generator output: `C:/Users/pc/.codex/generated_images/01a095bd-7c87-7c33-83a1-013aa1757c47/exec-6d1e4255-174b-4080-b048-dd51228d1ecb.png`. The project does not rely on that external path. No CLI/API fallback or raster postprocessing was used. The original generic cavern texture and scene reference remain intact.

VoidBackdropView loads the new borrowed sprite only for foundry geometry when the caller already supplies a cavern and camera. Missing replacement falls back to supplied art. Other arenas keep supplied art; missing original art/camera still selects procedural void. Existing cover fitting, bounded parallax, tint, atmosphere shader, ranks and owned material cleanup are unchanged. New tests exercise those selection/fallback/lifetime contracts and camera coverage at four aspect ratios, three zoom levels and four world positions. An internal construction overload allows missing-resource tests without moving real assets.

No playable map/ramp, camera, scene, combat, progression or save change. Same backdrop renderer; region remains 32 renderers and 118,709 vertices. One additional borrowed Resources sprite/texture, no new per-frame work. Resource counts do not demonstrate device performance.

## Actual comparison

Root and independent reviewer accepted the first actual Unity composition. Whole-map benefit is meaningful: more separate structures and haze pockets, less dominance by giant side walls, clearer island outline. Bridge-side depth has more scale variation. Portrait change is restrained because the central crop remains quiet; hero/range-ring contrast is retained. No new image edges, seams or actor obstruction in reviewed views.

Limits: repeated painted cranes remain, and newly exposed native distant shafts/broken arch are simpler and darker than the surrounding painted architecture. This does not complete the reference map. Native skyline material/depth coherence and broader workyard distribution remain worthwhile next tasks. Keep gameplay/boss progression deferred under the owner's art-first direction.

## Verification

See `validation/verification.md` for final suites, package identity and device status. Initial graphics import reported RGBA32 with 11 mip levels. Explicit Android recapture confirmed that result despite an ASTC6x6 override. The pinned engine only compresses mipmapped textures with power-of-two dimensions. Disabling mipmaps preserves this 1024x1536 sprite and restores actual ASTC6x6, one mip level, non-readable. Editor texture-memory observation fell from 16,778,104 to 1,401,728 bytes; that is not a physical-device measurement. Loaded-format and mip-count assertions guard against recurrence. Initial metadata/XML/full-map are preserved under `validation/initial-import`.

Three after graphics captures ran: initial import, explicit Android diagnosis and final corrected import. Final composition remains accepted after compression. The capture runner explicitly selects Android and reports active target, platform settings and actual loaded texture, so requested import settings are not confused with observed format. Before XML is historical evidence, not another execution. Test/build verification is repeated after the import/test correction; the earlier uncompressed APK is superseded.

With Unity closed, reproduce: `pwsh -NoProfile -File Tools/ArtReview/Run-FoundryBackdropDepthProof.ps1 -Phase after`. Phase selects the output directory only; it does not revert source. Temporary Assets source/meta are removed in `finally`. Three helpers covered integration/tests, proof/gallery tooling and independent review; root generated/imported the plate and exclusively ran Unity.

No ADB or fresh phone observation in this slice. Earlier concrete owner-run restart/install decision remains pending. On authorization inspect current state, back up current files, install/hash-check latest accumulated APK, and preserve current original saves/telemetry. Never restore stale snapshots. Physical-device, crowd, midrange and first-ramp acceptance remain open. Gallery portraits are actual Unity captures with production camera width and hidden HUD, not phone screenshots.
