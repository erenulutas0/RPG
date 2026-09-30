# Integrated XP-only crystal — 2026-09-23

The Cinder Mite's common XP-only drop now borrows a painted crystal instead of drawing the procedural diamond. Combined XP/gold rewards retain their preceding sprite. Gold-only and missing-art fallbacks remain. GroundDrops payment, pickup radius, flight timing, pooling, movement and combat are unchanged.

## Asset and lifetime

The unchanged xp-reward-proof-01/xp-v1.png master (SHA256463b0c4c819ac4da1f01aba1be34a0ba70021670ed039de20263951fdfc033d0) is sampled full-canvas to96x96 by BuildExperienceRewardAsset.cs / Import-ExperienceReward.ps1. No image generation or repainting in this integration. Reward_Experience.tga uses128PPU, bilinear, no mipmaps, non-readable, ASTC4x4 Android (9216-byte compressed pixel payload). Fixed pivot623/1254,111/1254 in bottom-origin coordinates; visible width0.30 based on the source's800-pixel strong silhouette. Scene receives one additional CombatSetup reference through the temporary Editor importer; existing GUIDs are preserved.

GroundDropView borrows the new asset through Initialize, selects it only for positive XP with zero gold, and applies its own scale. The owned procedural fallback remains separately stored and destroyed; imported textures are never destroyed by the view. Existing shadows and ground sorting are reused. No per-drop texture creation on the imported path.

## Evidence

Run-ExperienceRewardProof.ps1 renders the actual integrated scene with isolated TestProfile/seed42 and the density proof floor. A real Cinder Mite is killed with fixture damage;17 additional XP-only drops are staged through GroundDrops. Eight captures include ordinary/short portrait, front/back depth, local collection, choice pause, sweep and empty pool. This is controlled Unity evidence, not physical-device footage or a performance benchmark. The source master is unchanged. Ground/depth captures were visually reviewed; the8-image browser gallery loaded without errors or390px horizontal overflow.

GroundDropPresentationTests now verifies the XP import/pivot, selection, scale and borrowed lifetime across scene reload; the existing real upgrade-pause flight test uses XP-only payment and checks that no gold is credited. Existing combined-drop and pool tests remain. Full verification/build results are recorded below when complete.

No phone operations in this integration so far. The prior installed build is the combined-reward APK; do not infer current phone/profile state from historical captures.

## XP-only painted companion — 2026-09-23

Cinder Mite's frequent XP-only reward now uses Reward_Experience.tga through a separate CombatSetup reference and a borrowed GroundDropView sprite. Full-canvas96x96/128PPU, bilinear, non-readable, no mips, Android ASTC4x4; root623/1254,111/1254;0.30 visible width versus combined0.45. No payout, pickup, timing, pooling, combat or simulation changes. Missing references and gold-only drops keep existing fallbacks. The imported asset is not destroyed by the view.

Updated GroundDropPresentationTests verifies XP import/pivot, selection/scale and survival across scene reload; the level-choice test now exercises XP-only collection without crediting gold. Actual Unity capture kills a Cinder Mite and stages17 more XP-only drops, then checks depth, short portrait, local/sweep flight, choice pause and pool return. Reproduction: Tools/ArtReview/Import-ExperienceReward.ps1 and Run-ExperienceRewardProof.ps1. Evidence: ArtDirection/2026-09-22/xp-reward-runtime-01/README.md. Source image hash unchanged; no new generation in integration.

Verified: .NET380/380, compile0 warnings/errors, EditMode395/395, PlayMode124/124, graphics1/1, integrity483project GUIDs/1099package GUIDs/568scene components. Browser8images,390px no overflow/errors. Android result follows below. Phone availability requested but unanswered; no device interaction or assumed profile state.

Android verification complete: exit0, APK24,752,438bytes, SHA2561F00CF65D105882E8976A0B3167B5A600A57022B38D9A0B9ED2D1FC997BBA168. Build log confirms scene data rebuilt; packaged shared assets contain experience sprite references. ProjectSettings and Packages/packages-lock.json unchanged; final static integrity and git diff --check passed. Not installed: phone availability remains unanswered. No commit/push in this slice.

## XP-only installation and device presence check — 2026-09-23

Owner explicitly handed over the idle phone and requested installation. S23 was awake/unlocked with game focus. Force-stopped the app, backed up all9 external app-files into ignored TestResults/xp-device-20260923-001139/before, installed with --no-streaming -r (Success), pulled after-files and verified all9 byte-identical. Installed base.apk SHA256 matches1F00CF65D105882E8976A0B3167B5A600A57022B38D9A0B9ED2D1FC997BBA168. Stored profile revision186/gold26321/defaultSword/SecondWind/deepest3; no save restore, purchase, loadout or flag change.

Launched normally. Guarded capture shows painted XP-only crystals on the actual phone at Ember Hall1/6, level0/XP0,0run gold,84/100HP. A guarded pause tap followed; handback capture saved. Evidence: ArtDirection/2026-09-22/xp-reward-runtime-01/device/installed.png and handback.png. This verifies installation, launch and actual sprite appearance; no new collection-timing or performance claim. The force-stop/install restarted the active run, not the persistent profile. Phone handed back paused. Previous not-installed statements are historical and superseded.
