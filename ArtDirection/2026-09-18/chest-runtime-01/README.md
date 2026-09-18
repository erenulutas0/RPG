# Foundry chest runtime integration — 2026-09-18

The three imported chest sprites replace the procedural chest through an optional ChestArtSet reference. Missing/invalid art retains the previous procedural fallback. Original built-in imagegen masters and exact prompts remain unchanged in ../chest-proof-01.

## Import contract and ownership

- Full 1254-square source sampled to 192-square TGA; fixed pivot (620/1254,174/1254), PPU = 192*788/(1254*.75). Reference box width approximately .75 world units. No per-state alpha-fit or recentering.
- Bilinear, Clamp, no mipmaps, non-readable, Android ASTC4x4. Three texture payloads total 110,592 bytes (108 KiB), excluding Unity object/platform overhead.
- One shared ChestArtSet owns sprite references. ChestSpawner borrows assets without texture destruction on scene reload. Procedural fallback still owns/releases its generated sprites.
- Scene change: one serialized art-set reference. Combat, rewards, encounter placement, movement/collision, camera and simulation rules are unchanged.

## Opening and depth

Contact sets IsOpen and awards the reward immediately, once. A .18-second visual opening frame precedes the spent sprite. Scaled delta time freezes the visual during pause; a new room resets the closed state. This is restrained three-state blocking, not a final smooth lid animation.

GroundedSorting groups each actor at its root so weapons, body and hit flash sort together against other actors and the chest. Health bars, contact shadows and the ability range ring must remain independent root-sorted groups at their global overlay/ground orders. This does not introduce physical hero blocking or solve all projected crowd overlap.

## Reproduction

- Import: `pwsh -File Tools/ArtReview/Import-ChestAssets.ps1`
- Actual imported-art capture: `pwsh -File Tools/ArtReview/Run-ChestRuntimeProof.ps1`
- Test class: ChestPresentationTests; existing ArenaWalkTests also verifies heal/gold contact behavior.

The capture fixture freezes a ten-enemy proof room, uses a temporary profile, stages front/behind relationships, and does not save scene changes. Historical chest-unity-01 captures show the preceding incorrect sorting; do not overwrite them with a newer runtime and call them the baseline.

## Remaining limits

Small generated rim/body differences remain between states. No additional lid in-betweens, custom painted chest shadow, loot flight animation, sound, rarity or reward-type art is included. Only this chest slice is integrated; replacement ability-ring artwork is separate. Device results and final gate are recorded below once completed.

## Integrated foundry chest — 2026-09-18

Three shared imported 192-square ASTC4x4 sprites (closed/opening/spent), a ChestArtSet and one Gameplay scene reference replace the procedural prop when valid art is present. Common full-source registration, bilinear filtering, .75-unit reference box width; generated originals/prompts unchanged. Contact reward remains immediate/exactly once; the cosmetic .18-second opening respects pause. Reload borrows existing sprite/texture assets. GroundedSorting keeps each actor body/equipment/flash together at the floor root, while bars, shadows and ability ring retain independent global groups. This also changes actor/actor overlap presentation; no physical movement/spacing/balance rules changed.

Files: ChestSpawner, ChestArtSet, GroundedSorting, HeroLookView, EnemyLookView, ContactShadowView, AbilityRingView, imported Art/Props/FoundryChest, one scene reference, ChestPresentationTests/ArenaWalkTests and Tools/ArtReview chest import/capture scripts. See `ArtDirection/2026-09-18/chest-runtime-01/README.md` for reproduction and limits. Remaining: small generated frame drift, smooth lid animation, crowd occlusion and physical-device review; ability-ring artwork itself is unchanged.

Verified: .NET 272/272; compile 0 warnings/errors; EditMode 284/284; PlayMode 106/106; graphics capture 1/1; integrity 424 GUIDs / 555 scene objects/components; Android build exit 0. APK 24,751,592 bytes, SHA-256 `BA1F79545C8B3D70779A0380B2BD22A26BA0A8B88A3C09187B019DDCD852D814`. Two presentation tests cover immediate single reward, pause/resume, import format, common registration, independent ground/overlay groups and reload ownership. The first targeted test exposed an unsaved null art reference; importer now reloads the saved asset in the scene context and explicitly marks the scene dirty. A graphics check exposed the range ring inheriting the hero group; it now sorts independently.

Device status: NOT installed or phone-tested. An asynchronous device-availability question remains unanswered; no ADB input/capture/install or profile/telemetry mutation occurred in this slice. The previously installed platform APK is still the latest verified phone record. Do not claim this build's depth, performance or opening was verified on S23.
