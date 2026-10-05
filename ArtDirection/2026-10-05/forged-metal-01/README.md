# Forged iron material separation — 2026-10-05

Built-in image_gen edit plus one refinement, preserving the existing border/corner/rivet UV layout. Exact prompts: P6-prompts.txt. Untouched selected master P6-raw.png SHA25601106A441563FBFF4C04318DD2906E46651E41AAAA2C71CA5E1D7068797626DD. Original first draft remains in Codex generated_images; it was rejected for stone-like damage. The final version reduces fractured rock features and uses a calmer hammered/brushed steel field with restrained rubbed edges and brass rivets.

New sibling Assets/_Project/Art/Props/Foundry/FoundryIronV2.png (GUID b63aa825485fd064798ff407c424293f), original retained. Existing MAT_FoundryIron references V2. Import stage P6; actual Android import1024x1024 ASTC6x6/11mips. Existing IronPatch UV(.624,.235) stays; its 51-pixel window mean, sampled every2pixels, is recalibrated to RGB(.2039,.2181,.2418) so shared-batch clutter maintains its authored colours.

No new geometry, shader, material or renderer: static17/76,492vertices unchanged. No gameplay, collision, camera, save, telemetry or energy changes. Compile0warnings/errors; actual Unity capture1/1. Matched overview/gate/workshop/undercroft/furnace before and after. Full gate passed; device status below.
## Verified build; device review pending

Compile0warnings/errors; graphics capture1/1; EditMode410/410; PlayMode135/135; Android exit0; integrity534/1099/568. Build log confirms FoundryIronV2 1024x1024 ASTC_6x6/11mips, packed611.7KB. Static17renderers/76,492vertices unchanged. APK32,374,745 bytes SHA2564E8ED3A10C2215EA4AA61AE795A1E4985BEE8D5BBAED68C48A1739DF073353CA.

NOT installed and NOT seen on device for this package. Phone was in another app; asked whether it was free, no reply at closure. No app launch, input, flags or install performed. Read-only backup TestResults/iron-install-20261005. Previous installed package remains masonry V3 (0D719910...). Do not reuse its FPS result as evidence for this iron update. Next: with owner availability confirmed, fresh device-state check/backup, install the verified APK, guarded same-angle capture and movement check, then clean up only test-owned flags. Local commit only; no push.