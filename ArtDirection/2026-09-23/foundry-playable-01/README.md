# Playable foundry prototype â€” 2026-09-23

**Current layout:** photo-led broad court / left shaft / straight south bridge / northeast gate. Earlier952-cell linked rooms are historical. See the final owner-correction section below.

Owner explicitly requested a coherent playable build instead of further separate art studies. This slice integrates the approved five-zone floor and existing perimeter art with real hero movement, enemy placement/pursuit and the existing Descent combat loop. It does not introduce local encounter sites or a new progression/economy authority.

## Entry and scope

In Editor/development builds, `development/region-preview.txt` containing `foundry` selects this map at scene load. `wide` retains the previous diamond proof; missing/unknown flags retain the normal hall. Release builds do not read this flag. Mode freezes for each loaded scene. Removing the flag and restarting restores normal geometry without touching the profile.

The hero starts in the casting square. Drag to explore the entry, side salvage loop, furnace passage and boss approach. Attacks, drops, upgrades, chest rewards, forge, bosses, finale exit and results still follow existing authored Descent rooms, which advance automatically. **Bosses are not yet gated by physically reaching the approach, and the separate boss-hall transition is not implemented.** The whole purpose of this build is to play movement/combat on the approved map and assess scale before that next system.

## Shared geometry and ownership

- `FoundryMap`: immutable seven-rectangle union, 952 cells. Design axes rotate into floor axes; floor depth projects at existing .5 scale. Pure containment, conservative .6 square clearance, continuous segment checks, substepped sliding and deterministic separated pack placement. Decorative annexes are excluded from navigation.
- `ArenaGeometry.IsFoundry`: explicit geometry branch; legacy constructors/default behaviour remain unchanged. HeroMotion and EntrySides branch only in the new mode.
- `FoundryPursuit`: twelve-node immutable visibility graph with precomputed graph distances, per-pack cached hero costs and no per-frame allocations. PackMotion keeps its existing body spacing; it routes around concave voids and validates traversed segments. Direct pursuit and attack rules remain intact.
- `FoundryRegionView`: one-time mesh construction borrowing existing floor/wall/coping materials and Furnace sprite. Generated meshes are destroyed at teardown. No procedural texture creation or generated gate import. Camera follows the hero in the free HUD band at the existing width rather than applying diamond bounds.

The exact segment interval-union check replaced an initial sampled check after the actual PackMotion test exposed an inside-corner stall. This fix is specific to foundry navigation; legacy movement/parity remains unchanged.

## Evidence and limits

`start.png` and `point-0..4.png` are actual Unity captures, not device screenshots. The capture fixture walks HeroMovementInput through five locations without teleporting; attacks and encounter advancement are disabled only for that art/traversal capture. `walk.csv` records those legs. It does not prove combat balance, full simulation replay parity or device performance.

Pure tests cover every interior grid cell at four offsets, ten separated visible spawns, long-step hole blocking, three routed journeys and actual PackMotion pursuit around the void. PlayMode covers real flag loading, live enemy placement, authoritative pause, frozen mode and mesh release/default-hall reload. Initial pause fixture incorrectly set Time.timeScale directly; corrected to RunPause.TryPause and waited for the previously calculated frame delta to complete.

Known unfinished art: visible annex coping joins, sparse environment dressing, no accepted boss gate sprite (all three source attempts retained exterior haze), no final lighting/occlusion polish. No new local-site mission, minimap, boss unlock or elevated traversal. Existing telemetry records the ordinary run events; it does not encode this new geometry for deterministic full-run replay.

External Opus task remains unstarted. This integration was performed here under the owner's new playable-build request. Do not send a second agent into these geometry/art files concurrently.

## Verification gate

.NET387 passed; compile check0 warnings/errors; Unity EditMode402 and PlayMode131 passed. Targeted FoundryRegionTests1 passed before full rerun. Final static integrity494 unique GUIDs/1099 package GUIDs/568 scene objects. The initial full run had130/131 passing with the new pause fixture failing; after correcting the fixture, the complete rerun passed. Android/device identity is recorded below after installation.

## Installed S23 build and handback

APK SHA256 `7FB249A4E8EB25560113ACF417521C25AB6809DD9B8A1EA118812D367B7C4CBD`,33,754,918bytes, Android build exit0. Installed base.apk hash matched. First install command lost its ADB response while Unity verification launched; subsequent read verified the new APK was already installed, so no blind reinstall. All9 preinstall files were byte-identical immediately after install.

Before updating, the owner's old paused finale was completed through ordinary choices and the walked exit:373gold banked, profile194/27795gold/defaultSword/SecondWind/deepest3. Fresh backup in ignored TestResults/foundry-device-20260923-171628. No profile restoration or equipment purchase/change.

On the new build, initial unattended combat reached defeat before the controlled walk (0gold). Restarted through UI; an8second northwest drag moved from the square toward the furnace passage with pursuit/combat active. Paused, resumed and returned over drops:5gold and13XP collected, level1 choice opened at80/100HP. Left at that choice for the owner. Primary and backup profiles remain byte-identical to preinstall. Telemetry naturally advanced. `region-preview.txt=foundry` is deliberately RETAINED so the owner can keep playing/restarting this map; no active start-floor override. Remove only that flag and restart to return to the old hall.

Final graphics traversal1/1 passed after the segment fix; all five real-input legs reached, no teleports. Final integrity494/1099/568 after temporary fixture cleanup. Physical screenshots in device/. No matched NullReference/MissingReference/InvalidOperation/FATAL exceptions in captured process Unity log. Sample windows ran59.7â€“59.9fps, with a33.4ms maximum in one active/restart window; many other windows were paused. This is a short S23 smoke, not a sustained or midrange performance claim.

No commit/push in this slice. Further art and site progression should now be judged from this playable build rather than another isolated image study.

## Rough architecture pass â€” 2026-09-23

Owner explicitly chose a complete rough map first, followed by visual refinement toward the approved foundry concept. FoundryRegionView now adds flat brass route inlays, a casting-square circle, paired entry piers, salvage shed/crane masses, furnace service hall and a closed boss-gate facade with towers. These are coarse replaceable architectural masses, not final props. All raised footprints are outside the same 952-cell walkable union; route inlays are flat and have no collision. No balance, room progression, spawn, camera scale or profile changes.

Wall UV repetition now follows face dimensions instead of stretching one sample across a building. Hidden rear faces of solid blocks are omitted for the fixed isometric view. Existing material ownership and teardown apply; nothing is generated each frame. The gate is a visual destination only: it does not unlock or transport the player yet.

Current start/point-0..4.png and walk.csv were refreshed from the integrated Unity scene. The five real-input traversal legs passed again (11.8/24.5/19.0/14.9/9.7 game seconds with fixture attacks disabled). Compile check is clean, integrity remains494 GUIDs/568 scene objects. Android gate and fresh device review are recorded separately below when complete; the older installed-build section above describes the preceding APK.

Refinement order after owner scale review: replace large gate and salvage masses with correctly registered modular art; refine perimeter corners/supports and paving variation; add restrained local light/industrial details; review character visibility and phone performance. Do not treat the concept image as an exact promised final screenshot. Local encounters and the separate boss room remain a gameplay slice.

### Rough-pass build result (not installed)

Full Unity gate402 EditMode /131 PlayMode passed; Android exit0. APK SHA256648EA6B0CB5283F8FF8AA3687DF4035A3C8BE5BE9E3476B5B3350B67B3FBDE75,33,755,809bytes. This supersedes the local build file only. Device still has prior7FB build.

Fresh device inspection found an active floor3 finale paused at an upgrade choice with348 run gold,129/175HP,level13. Stored profile196/28118gold/defaultSword/SecondWind/deepest3. Snapshot TestResults/foundry-rough-20260923-180733/before. Asked owner whether to finish personally, allow a controlled exit, or abandon and install. No phone input, app restart, installation or save restoration performed while that answer is pending. Always inspect fresh before resuming.

The final graphics fixture also exports overview.png: an actual Unity whole-map view with HUD hidden and a temporary zoomed-out camera solely for review. The playable portrait camera remains unchanged. Final graphics run1 passed after adding this overview. No device action was performed.

## 2026-09-23 â€” Rough map installed after owner closed game

Owner confirmed the game was closed and authorized continuation. Fresh backup TestResults/foundry-install-20260923-182322. Installed APK648EA6B0CB5283F8FF8AA3687DF4035A3C8BE5BE9E3476B5B3350B67B3FBDE75 with adb install -r; Success and installed base.apk SHA256 matched. All10 files byte-identical immediately after installation. Fresh profile196/28118gold/defaultSword/SecondWind/deepest3; primary and backup still byte-identical after test. No restore or manual save edit; telemetry naturally advanced. Previous open348-gold run was already closed by owner; assistant did not bank it.

Launched normal authored Ember Hall1/6 with retained region-preview=foundry. Guarded pause/resume,8-second northwest drag, pause: camera moved across square toward side/furnace junction;3 enemies defeated, XP crystals visible. Left paused at85/100HP,level0,XP0/10,runGold0. No upgrade or loadout selection. This is a short device movement/render smoke, not full map traversal/performance acceptance. All five paths were verified in Unity in the previous slice. Current-process log had no matched FATAL EXCEPTION/NullReferenceException/MissingReferenceException/InvalidOperationException. Evidence in foundry-playable-01/device-rough. Latest installed648 supersedes7FB and pending-install entries. Device ready for owner Resume.

## 2026-09-23 — Owner correction: photograph owns the structure

Owner rejected the narrow linked-platform interpretation and explicitly requested the same structure as foundry-v1.png. This supersedes the earlier diagram-as-authority instruction for the foundry region. Current runtime now uses one broad court with an inset shaft on the left, a straight south entry bridge and a wide northeast gate apron. The concept remains the visual target; current rough masses are not final art or an exact reproduction of every painted detail.

FoundryMap now combines five axis-aligned rectangles with one exact diagonal bridge (u-v in[-4,4],u+v in[-50,-24]). Main court bounds[-18,-14]..[14,18], shaft[-12,2]..[-4,10], apron[12,-2]..[32,12]. Bridge predicates and continuous segment clipping share the rendered straight bridge. FoundryPursuit nodes moved with these regions. The old952-cell/seven-rectangle layout and old waypoints are historical.

FoundryRegionView adds deep sidewalls and regularly spaced exterior supports, low edge parapets, a central three-ring brass seal, perimeter furnace plinths, a simple crane silhouette and a northeast closed gate. Static faces batch by shared material/sorting layer rather than adding a renderer for every support. Mesh ownership remains local, materials borrowed, no per-frame geometry. The four furnace sprites are existing imported art. Decorative silhouettes still need final art, registered joints, lighting and occlusion polish. No elevated walking or functioning gate transition was added.

Verification: final pure suite388 passed, compile clean, graphics walk1 passed, integrity494GUIDs/568scene objects. Capture walkthrough legs29.4/30.6/18.2/16.8/11.7game seconds, attacks disabled in fixture. Refreshed overview.png uses a temporary overview camera; actual portrait camera stays width9. Android gate/install follow separately. Opus remains on hold; no commit/push.


## Photograph-led build ready — installation pending active-run reply

Final gate: pure388; Unity EditMode403 /PlayMode131; graphics1; compile clean; integrity494 GUIDs/568 scene objects. Android exit0, APK33,758,335bytes, SHA256 A8ED8E1C8F0DEFCF341CA998237E32A19E354794A9873D9B130EDC4B1C9C64A3. Built but NOT installed. Device still has648EA6 rough linked-map build.

Read-only screenshot during this slice found a new owner run at Warden's Crucible1/6's room6,116 run gold,21/100HP,level7 upgrade choice. Asked whether owner will finish or authorizes abandoning for install; no reply yet. No tap/swipe/install/force-stop/save edit performed in this slice. Previous last-known profile196/28118 is not freshly verified here; inspect again before install. Screenshot TestResults/foundry-install-20260923-182322/live/before-layout-update.png. Owner's earlier closed-game authorization applied to the completed648 install, not this newly observed active run.


## 2026-09-23 — Photograph-led map installed on S23

Owner said continue; fresh inspection found launcher focused, game closed. Fresh backup TestResults/foundry-photo-install-20260923-195830. Profile197/28234gold/defaultSword/SecondWind/deepest3: the previous116gold had been banked before this session. Installed APK A8ED8E1C8F0DEFCF341CA998237E32A19E354794A9873D9B130EDC4B1C9C64A3 via install-r; Success and installed base.apk hash matched. All10 files identical immediately after installation; profile and backup identical after smoke. No restore or save edit. Foundry flag retained.

First launch request failed during an ADB daemon connection interruption; game remained on launcher and input guards rejected taps. Rechecked device and retried launch only, not installation. Successful start paused at100HP. Guarded resume /12-second leftward drag /pause confirmed camera movement, combat and visible new court/shaft area. Left Ember Hall1/6,wave3/3,77HP,level0,XP0/10,runGold0,paused. No loadout or upgrade choice. Current-process log had no matched FATAL EXCEPTION/NullReferenceException/MissingReferenceException/InvalidOperationException. Short S23 smoke only, not complete physical-device traversal or performance proof. Earlier five-region Unity walk remains the traversal evidence. Screenshots device-photo. Supersedes pending-install and648 device status. No code or rebuild in this device turn.


## Latest art: detailed gate facade

Current Unity captures now include the generated opaque front-elevation facade on a real vertical gate plane. See ../gate-facade-01/README.md and PROMPT.md for source, import, ownership, verification and pending device installation. Geometry unchanged; supporting side masonry remains coarse.


## Final build status — 2026-09-23

Full gate403 EditMode /131 PlayMode passed; Android exit0. APK48AB4DFA3F0AC71D8B9B6AF56A25F95B8E57B5D1FDADB897805FBFCD49050CA5,29,253,015bytes, built NOT installed. Device read-only check found NotificationShade/keyguard showing; guarded screenshot refused as designed. Asked owner to unlock and leave game closed or continue only on computer. No answer yet; no taps, installation, force-stop or profile operation this slice. Installed phone remains A8ED8E1C8F0DEFCF341CA998237E32A19E354794A9873D9B130EDC4B1C9C64A3. Last recorded profile197/28234 is historical until freshly read again.

## Note (2026-10-01)

`Tools/ArtReview/Run-FoundryRuntimeProof.ps1` renders into this folder and overwrites its captures on every run, so
the capture images here stopped matching this 2026-09-23 README long ago and are not committed. The `device*/`
screenshots are the original 2026-09-23 phone evidence. Every later state has its own matched captures in its dated
proof folder (`ArtDirection/2026-09-24..30/`).
