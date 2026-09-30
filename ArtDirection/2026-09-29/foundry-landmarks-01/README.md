# Foundry landmarks — 2026-09-29

## Combined scope

- Gate: shallow masonry pediment over the existing header, with sloped projected roof faces and stone capping. Painted doors/seal remain visible.
- Four boilers: low continuous stone plinths and tapered corner supports inside their existing footprints. Chimney mouths/smoke anchors unchanged.
- Bridge: small cast sockets and paired dark metal/bronze panels on the four existing gateposts; brazier positions and clear bridge width unchanged.
- Shaft: complete four-side segmented inner coping and four narrow iron retaining saddles with hanging anchor legs.

Includes the preceding crane/undercroft refinement which is built but not yet installed. No gameplay, scene, collision, camera or energy edits. No new raster assets/materials/per-frame work. Existing static material batches reused.

## Review and evidence

Actual Unity traversal and close-ups, not generated screenshots. The first capture exposed floating diagonal gate flashing over a tower; those two new metal lines were removed before the final capture. The stone pediment remains. Furnace, shaft and bridge closeups showed clear apertures and supported trim.

before.png is the previous silhouette package. after.png and gate/furnace/bridge/shaft detail images are final integrated captures. Contributor records: architecture-notes.md and site-notes.md.

## Verification

Compile passed 0 warnings/errors. Final graphics/full tests/APK status is recorded below. Device installation deferred while the owner is using the phone; no device operations in this slice. Previous installed build remains BF87942E8C9EAE76AA5DDCB9F575493F1B3C9C260E5EA41E82F4FFD69FC196CC. Do not imply the preceding silhouette build was installed. No commit/push.

Final graphics: 1/1 passed after flashing correction. Static census: 17 renderers / 49,396 vertices, plus existing atmosphere 1 / 780. Integrity: 514 project GUIDs, 1099 package GUIDs, 568 scene objects/components. Full Unity gate passed: EditMode 405/405; PlayMode 130/130; Android exit 0. APK SHA256 4576CC04844ABB7F733AB1F354F8CBFD82A9962A3CAB386CE8EADCCE93B38994, 39,340,606 bytes, built 2026-09-29 14:25:34 local. BUILT, NOT INSTALLED. No device operations this turn; physical review remains pending.

Independent architecture cross-review inspected gate/furnace/whole-map: no blocking visual defect. The front plinth covers lower fire-grille sections but arched window tops and orange fire remain readable. Keep plinth height stable in later passes; it is already visually heavy.


## 2026-09-29 - Landmark package installed (supersedes pending status)

Owner explicitly requested installation. Seven critical files backed up in TestResults/foundry-landmarks-install-20260929/before. adb install returned a blank failure, but immediate installed base.apk hash inspection confirmed the exact intended 4576CC04844ABB7F733AB1F354F8CBFD82A9962A3CAB386CE8EADCCE93B38994 package. No second install attempted. Device became unlocked/launcher; game launched successfully. Post-launch verification: profile, backup, two development flags and rotated telemetry files byte-identical; current telemetry retains its exact preinstall prefix and appends launch events. Profile revision215/32776gold/Staff/SecondWind/deepest3 unchanged. No save edit/restore.

Startup capture and pause confirmed room1/6, wave1/3,93HP,level0,XP0/10,0runGold. Game left PAUSED for owner Resume. Current process log has no fatal/null-reference/missing-reference/invalid-operation matches. Installation/startup smoke only, not a new physical landmark traversal/performance benchmark. Evidence 01-start.png,02-paused.png,game.log,install.txt and before/after-install files. Initial verification script had stale LASTEXITCODE and then compared active telemetry after launch; explicit final six-file hash and telemetry-prefix verification resolved those tooling issues. No code/build changes or commit/push.
