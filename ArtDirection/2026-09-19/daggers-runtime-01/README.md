# Integrated Daggers equipment — 2026-09-19

One shared optional VanguardArtSet.Dagger replaces both procedural blade renderers. Source dagger-v1.png is unchanged; fixed crop(438,10,380,1220) ->20x64,128PPU,bilinear,non-readable,no mipmaps,Android ASTC4, pivot(.5,255/1220). Tools/ArtReview/Import-DaggersWeapon.ps1 reproduces the import while retaining existing GUIDs. Source channels are staged losslessly by Convert-CharacterProof.ps1.

VanguardAnimator places both blades behind the body/gauntlets at their respective per-frame hand anchors. Rest angle is35degrees outward, opposite signs per hand/facing; a real hit pivots up to12 additional degrees and returns over the existing attack duration. The old .12-unit translation remains only for art sets without an imported dagger. Damage, critical rhythm, targeting, range, movement budget and scene are unchanged. No new per-hit texture/sprite allocation. Imported sprites are borrowed; per-view procedural fallbacks retain their previous ownership.

New PlayMode coverage checks both grip anchors and depth through four travel facings, immediate real damage and angular reaction, pause, return to rest, death hiding and shared non-readable sprite survival across reload. Tools/ArtReview/Run-DaggersRuntimeProof.ps1 captures actual runtime, with no sprite/transform substitution; 53 images cover the room, four facings, gait and observed strike. daggers-unity-01 is the historical comparison; its displacement-triggered fixture targets the preintegration implementation (1daef46), not the new anchored animator.

Limits: this is a compact neutral-body wrist pivot, not a newly painted arm/thrust animation. Mirrored lighting remains; the old large pixel hit effect remains conspicuous. Close layering was checked in sampled facings, not every enemy/body overlap. Full device acceptance is recorded separately in Docs/23.

Current verification: .NET291, compile0 warnings/errors, actual runtime graphics fixture1/1. Full suites/Android/device pending below.

Verified: .NET291/291, compile0 warnings/errors, EditMode306/306, PlayMode115/115, actual runtime graphics1/1, integrity444project GUIDs/1099package GUIDs/568scene objects; Android exit0. APK24,752,344bytes SHA256 `62D5A2707299AB8865B581ED3B5FD69F135EB727E8D562362F997B7F6055D603`; installed S23 base.apk matched. No live phone visual test yet: phone was in another app and availability question has no reply. Profiles unchanged; newest Docs/23 records details. Existing full suite counts supersede earlier proof-only checks.
