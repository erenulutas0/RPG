# Combined courtyard, bridge, perimeter, crane and shaft pass

The owner explicitly requested completing the proposed multi-part environment package together, rather than approving individual small slices. This pass covers the whole named package in the actual playable foundry.

## Delivered art

- One original low-contrast charcoal basalt source, generated with built-in image_gen; exact prompt in PROMPT.md. Unchanged1254x1254 source basalt-v1.png copied to Assets/_Project/Art/Props/Foundry/FoundryPaving.png. SHA256388300B256E9C04EA4C449006289F51D22C646F0D09DAD1AF270983250EA2AD7 matches both. No raster edits, alpha processing or external assets. The source contains shallow natural wear/fissures; additional procedural crack lines were rejected after visual review.
- Region-specific staggered2x1 paving slabs with narrow joints and flush bevel shading, clipped to the exact existing cell union. Ambient/forge vertex illumination retained. Ordinary hall floor remains unchanged through the optional FoundryPaving material.
- Transverse bridge joints, four tapered piers supporting the bridge's existing beacons. First version left detached-looking wide posts; final version aligns the piers to the beacon/bridge edges and raises the beacons onto them.
- Segmented court border inlays and eight outer stone finials, all on the existing scenery side of the walking perimeter.
- Crane foot gussets, lattice joint plates, winch drum and collars. All ironwork gains a restrained dark underside, sharing the existing material.
- Shaft lip stones, an interior crossbeam and three suspended chain lengths at below-floor scenery ranks.

## Production and constraints

FoundryRegionSite is the new static presentation partial. FoundryRegionView/Finish build the larger paving groups; Dressing supports bridge beacons and iron shading. ArchitecturalTier now clamps the top chamfer to the tapered cross-section. PlatformMaterialSet adds optional FoundryPaving, imported by Tools/ArtReview/BuildFoundryPavingAsset.cs and Import-FoundryPaving.ps1. Source import: non-readable sRGB Default, Mirror, mipmaps/Trilinear, max1024, Android ASTC6. No package/settings/scene changes or Blender use. No moving crane, damaging shaft, gate transport, navigation/collision or combat rule changes. Existing energy deferral remains active.

All meshes are built once and use shared asset-owned materials. Static renderer count remains15; vertices23,160 ->39,740. No per-frame geometry/texture generation or new lights. New source/material add memory; actual phone performance remains subject to testing. Existing FoundryRegionTests now includes the paving material in scene-use/non-readable/reload-survival assertions. Final graphics proof traverses five real waypoints and exports overview, portrait and bridge/crane/shaft close-ups.

## Review

before.png is the preceding architecture export. after.png is the final3200x2400 Unity capture. All close-ups/portrait captures are actual Unity renders, not generated scene illustrations or physical-device screenshots. index.html compares before/after and the whole package. The full reference-quality environment remains unfinished: dynamic atmosphere, more original model silhouettes and character/environment consistency remain future art work; this package completes only the stated courtyard/bridge/perimeter/crane/shaft scope.

Final test/build/device evidence follows below.

## 2026-09-24 - Combined courtyard package verification

Final five-waypoint Unity graphics proof passed (1/1). Full gate: EditMode 405/405, PlayMode 130/130, Android build exit 0. Project integrity: 511 unique project GUIDs, 1099 package GUIDs, 568 scene objects. Static scenery remains 15 renderers; vertices increase from 23,160 to 39,740. No combat rules changed; pure .NET combat suite was not rerun.

APK Builds/Android/Cryptforge-Prototype.apk: 29,999,777 bytes, SHA256 460BD079412E658692CEA1EF32FF79CD5BF54C803E86158B84D326BC45CA1261. Installed on the S23 with install-r Success; installed base.apk hash matched. All 10 stored files byte-identical immediately after installation. Fresh backup and logs: TestResults/foundry-courtyard-20260924.

Owner unlocked and left phone free. Guarded 15-second bridge movement and 12-second return to courtyard reviewed new paving, bridge piers, inlays and live Staff combat. Physical screenshots: device-bridge.png, device-courtyard.png, device-handback.png in ArtDirection/2026-09-24/foundry-courtyard-01. Left PAUSED in Cinder Walk, room 2/6, wave 3/3, 68/100 HP, level 0, XP 4/10, 0 run gold. No upgrades or loadout changes selected. Profile revision 208, gold 31166, Staff/Second Wind/deepest 3 unchanged after review; SHA256 D3D4FF563175BDF50EC2E9D32A772A015D7544E2C4F9C7704374DBD441BB7BCD matches fresh pre-install copy. No save restoration or editing.

Current process log has 0 matches for FATAL EXCEPTION, NullReferenceException, MissingReferenceException or InvalidOperationException. Startup still includes 2185.3 ms and 222.8 ms frames; these remain unresolved and are not hidden by the short visual review. This is not a sustained density or midrange-device benchmark. Crane and shaft details were verified in Unity; physical traversal this turn covered bridge and courtyard only. Energy deferral remains active. No commit/push.
