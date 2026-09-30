# Foundry ground readability — 2026-09-29

## Problem and result

The S23 reservoir review showed the hero and ability ring painted over projected boiler/pipe silhouettes. The floor remained walkable, but the composition suggested standing on roofs.

The foundry ring and pooled ability/Staff pulses now use the arena ground-effect rank (-13). Ordinary halls retain their previous rank. A hero-centred elliptical cutaway in above-floor scenery reveals the existing walking floor without hiding the player or changing movement/collision.

`FoundryVisibilityView` borrows source textures and owns one material variant per source. `FoundryCutaway.shader` is under Resources so the Android build includes it. The focus is world-space, follows the hero, clears when disabled/dead, and has no save state. Variants are destroyed on scene reload; imported materials survive. Geometry is still built once. One uniform update per variant per frame; no per-frame mesh generation or new textures.

## Evidence

- `ground-readability-on.png` / `ground-readability-off.png`: actual Unity renders at the same walkable (22,0) design point, camera and state. Both use corrected ring sorting; only cutaway enable changes.
- `point-3.png`, `overview.png`: actual integrated traversal/overview.
- `walk.csv`: five route points reached inside the walking union.
- `static-budget.txt`: unchanged 17 static renderer groups / 59,584 vertices. Atmosphere remains separate.
- `before-point-3.png`: previous reservoir capture, preserved before this change.

Reproduce with `pwsh -File Tools/ArtReview/Run-FoundryRuntimeProof.ps1`. Capture tooling stages one extra hero position for matched review; the gameplay scene and playable camera are not edited.

## Scope and limitations

This is a local visibility cutaway, not physical depth sorting or walkable roofs. Remote enemies still render above structures; there is no per-enemy cutaway. Near the outer rim, the revealed space can include the existing drop beyond the floor. No balance, collision, profile or telemetry behavior changes. No helper agents, commit or push.

## Validation

Graphics traversal passed 1/1. Integrity passed 518 project GUIDs / 1,099 package GUIDs / 568 scene objects/components. Full suites, APK and device status will be recorded after completion.

## 2026-09-29 - Foundry ground readability and hero cutaway

The foundry ability range ring and pooled ability/Staff pulses now use the arena ground-effect sorting rank (-13), instead of painting above every structure. Ordinary halls retain their prior effect rank. FoundryVisibilityView and the Resources FoundryCutaway shader reveal the existing floor through above-floor architecture in a soft ellipse around the living hero. Scene-owned material variants reuse source textures, update one uniform per variant, disable cleanly, and are destroyed on reload; shared imported materials survive. Static mesh construction and batches remain unchanged. This is a hero-centred readability cutaway, not physical depth sorting; distant enemies still render above structures.

Matched actual Unity captures at walkable design point (22,0), traversal portraits and full-map review: ArtDirection/2026-09-29/foundry-ground-readability-01. Both matched images use the new ring sorting; only cutaway enable differs. Five waypoint traversal1/1 passed. Static17renderers/59,584vertices and separate atmosphere1/780 unchanged. Gameplay, movement/collision, balance, scene, camera and save rules unchanged.

Targeted first test caught the old assertion that every material is borrowed; updated ownership checks require new variants to be destroyed and imported assets to survive. Final full gate EditMode405/405,PlayMode130/130,Android exit0. Final compile0warnings/errors; integrity518projectGUIDs/1099packageGUIDs/568sceneobjects passed. Proof HTTP200/local links resolved. Added checks cover cutaway focus/disable/reload ownership, grounded ring SortingGroup and real ability-event pulse rank.

APK Builds/Android/Cryptforge-Prototype.apk: actual39,340,731bytes, local2026-09-29 20:55:31, SHA25644566462D0203760FF3EA8B918175EEE553636DF6F35E7FDB565BA3E32780288. READY NOT INSTALLED. Last verified installed remains79B4C59EE5E4D7AA44BA53663228BA9D198BD7920DCCD1F004B7F0CDB7E62DC1. Device read-only check found S23 awake/unlocked at launcher; asked whether it is free, no reply yet. No phone inputs/install/save writes or device-performance claims. New shader cost and appearance on Android remain to be tested. No helper agents, commit or push.
## 2026-09-29 - Ground-readability package installed on S23

Owner said to continue after the pending installation question. Fresh guarded screenshot showed the same paused Cinder Walk run (77HP,0runGold). Backed up profile/backup/development flags/telemetry before adb install-r. Success; installed base.apk SHA25644566462D0203760FF3EA8B918175EEE553636DF6F35E7FDB565BA3E32780288 matches tested local build. All7 saved files byte-identical immediately after installation. Supersedes READY NOT INSTALLED above.

Actual phone inspection reached near boilers/reservoirs through controlled northeast drags. Cutaway renders on S23 and reveals floor around feet; range ring remains beneath solid architecture. Chose Long Reach at first level-up to continue traversal. Short live movement/combat smoke only, not sustained performance acceptance. Remote-enemy occlusion remains open. Current-process log: no FATAL EXCEPTION, NullReferenceException, MissingReferenceException, InvalidOperationException or shader error/unsupported matches.

Left PAUSED room3/6,wave2/2,28HP,level1,XP16/22,5at-risk gold,Staff/SecondWind. Profile revision215/32776gold/deepest3 unchanged; backup,development flags and rotated logs unchanged. Active telemetry preserves pre-test prefix. No profile restore/purchase. Evidence TestResults/foundry-visibility-install-20260929; unchanged actual screenshot at ArtDirection/2026-09-29/foundry-ground-readability-01/device.png. Inspect fresh before future phone inputs. No new code/build/commit/push this installation turn.