# Day 1–Day 3: smallest mechanical prototype plan

## Painted platform runtime — 2026-09-18

The real scene now uses the accepted V2 floor, flush coping and near-wall materials through one new ArenaView reference. Nine generated meshes are session-cached by one geometry; alternate geometry owns/disposes its own meshes. Asset-owned textures/materials survive view destruction. Foundation-only procedural sprites retain keel/chains/crystal and omit the old top/faces/towers/flames. Exact corner vertices and a narrow dark join lip remove the old rim bleed without changing movement boundaries, stats, encounters, camera or HUD behavior.

Files: PlatformMaterialSet.cs, PlatformMaterialMeshes.cs, ArenaSpriteCache.cs, PlatformArt.cs, ForgePlatformView.cs, ArenaView.cs, one Gameplay.unity field, Art/Environment/Platform imports and PlatformMaterialTests.cs. Reproduction: Tools/ArtReview/BuildPlatformMaterials.cs, PlatformRuntimeCapture.cs and Run-PlatformRuntimeProof.ps1. Seven Unity renders, two unchanged device captures, source hashes and gallery are in ArtDirection/2026-09-18/platform-runtime-01.

**Verified:** .NET272/272; compile0 warnings/errors; EditMode284/284; PlayMode104/104; graphics1/1; Android exit0; integrity415 GUIDs/555 scene objects/components. New tests cover actual Android formats/settings, joins/boundaries/sorting, reload reuse, private-geometry disposal and session reset without destroying borrowed assets. APK24,740,096 bytes, SHA-256 `64F49049A08DDB92396831F28FEC24DEFF42F902B8F849E6D87931F59F439475`; installed S23 hash matches. See Docs/23 for owner/agent coordination and profile preservation.

Runtime imported dimensions: floor/wall1024x1024 ASTC6x6; coping1024x256 ASTC4x4. Mipmapped payload1,602,160 bytes excludes driver/foundation overhead. Original PNG bytes unchanged. Lower keel/crystal/chains, chest/ring, other enemies and crowded depth remain open; this is not a separate boss-room or final environment delivery.

## Platform material comparison — 2026-09-17

`ArtDirection/2026-09-17/platform-material-01/` contains four unchanged generated source masters (floor V1/V2, coping, masonry), exact prompts/hashes, mapping contract and fourteen actual Unity images. V2 is the preferred floor for lower grout/bevel contrast. Seven temporary meshes map these onto existing geometry beneath ground effects; original keel/crystal/corner fixtures remain. Captures share a frozen real ten-enemy scene; hero/camera-only rim samples and a whole-platform diagnostic camera are explicitly labelled.

`Tools/ArtReview/PlatformMaterialCapture.cs`, `Run-PlatformMaterialProof.ps1` and the new converter switch reproduce the proof without saving authored Assets or a scene. Capture resources are owned/disposed; temporary fixture is removed. No gameplay, APK or phone change. Full source-resolution allocation is not accepted for mobile: reduce/compress and prove shared cache/reload on integration. Corner/old-outline/keel joins, props and crowded depth remain open. Latest gate is in Docs/22.

## Grunt painted runtime integration — 2026-09-17

The ordinary Grunt now uses the Slag Brute imports: front/rear idle, two contacts, two opposing-leg passing poses and immediate strike, mirrored for four directions, plus matching silhouette flashes and two independent collapse stages. EnemyArtSet supports either the existing four-frame Mite set or six frames; EnemyLookView consumes per-set stride, visible top and shadow width. Grunt prefab alone selects the set and removes the old procedural nudge. Gameplay.unity, combat timing/stats, spacing, camera and project settings are unchanged.

Files: Art/Characters/SlagBrute; EnemyArtSet.cs; EnemyLookView.cs; Grunt.prefab; GruntAnimationTests.cs; updated MiteAnimationTests and explicit Mite-only capture selection. Tools/ArtReview/BuildGruntAssets.cs and Run-GruntProof.ps1 reproduce rigid registration/import and real runtime capture. Exact source prompts, accepted/rejected masters, metadata, registration and gallery are in ArtDirection/2026-09-17/grunt-polish-01.

**Verified:** .NET **272/272**, compile **0 warnings/errors**, full EditMode **284/284**, full PlayMode **100/100**, graphics capture **1/1**, Android exit **0**, integrity **404 GUIDs/555 scene objects/components**. Four new tests cover shared imports/reload, four facings/passing gait/pause, immediate target-facing strike and matching flash, independent death with immediate reward/removal. APK SHA-256 **`AC9D4FC627CD94484A2923DEBB7AEC2CD2894CF3E51537207123B1BA1A6814B0`**. Device evidence is in the newest Docs/23 section.

Rear angle, alternating supports and raised-fist clearance are improved; strict foot locking, stone volume continuity and mirrored light/handedness remain provisional. Crowded depth overlap and hero/chest occlusion persist. No sustained performance or balance acceptance is implied.

## Grunt registered keyposes in Unity — 2026-09-17

`ArtDirection/2026-09-17/grunt-motion-01/` contains selected front/rear six-pose blocking sheets, one retained rejected front attempt, exact prompts/metadata, rigid source-root registration and sixteen actual Unity renders. The first front attack swapped arms; the selected correction keeps the raised arm for the strike. `Tools/ArtReview/GruntMotionCapture.cs` temporarily substitutes only the two Grunt body renderers in the isolated real ten-enemy density room, preserving runtime Vanguard/Mites, bars, floor, camera and shadows. It captures the baseline/candidate, front/rear close pairs and twelve poses. No authored Asset/scene or gameplay changes. `Run-GruntMotionProof.ps1` installs/removes the temporary fixture; `Convert-CharacterProof.ps1 -GruntMotion` stages source channels and encodes render evidence.

Common 640x640 registered canvas ->180x180 at128PPU/bilinear, pivot90,18. Visible front idle is approximately .936 units high; sprite padding is not a valid runtime bar-height measurement. Close-pair and hero/chest overlap persist; rear facing, stone-volume consistency, passing/foot contact and raised-fist/bar clearance remain open. Six-pose illustrative timing does not change the four-pose runtime art contract or immediate damage events. No hit/death/import/phone integration is claimed.

**Verified:** .NET **272/272**, compile **0 warnings/errors**, graphics capture **1/1**, full EditMode **284/284**, full PlayMode **96/96**, integrity **375 GUIDs / 555 scene objects/components**. Source hashes/alpha coverage, 12x480-square and4x1080x2340 captures, browser controls and desktop/390x844 layout passed. APK explicitly skipped for this isolated art proof; installed contact/grip hash remains **`1DCB931D6ECCA73424870687AB3AB32E21AC4E2C6BACD67A3C3B66492F4FB01C`**, not reinstalled or reverified on device this turn.


## Slag Brute design proof — 2026-09-17

`ArtDirection/2026-09-17/grunt-proof-01/` adds two original front/rear masters, metadata/prompts and a browser scale study with the existing Vanguard/Mite sources. The 11-actor composition, close-depth pair, facing, size, silhouette and foot controls support a bounded visual review. It does not import Grunt art, animate it, change gameplay or fix overlap. The rear angle and registration remain open before production poses.

**Verified:** original RGBA alpha/dimensions/SHA-256; browser desktop and 390x844 layout (17 images loaded, no horizontal overflow), interactive controls and 11 staged actors; static integrity **375 GUIDs / 555 scene objects/components**. No .NET/Unity suite or device rerun for this art-only study. No new APK. Latest runtime validation and installed hash remain the preceding contact/grip entry below.


## Vanguard contact/grip and compact Mite bars — 2026-09-17

The hero's scene `CombatantView._attackNudge` changes from .22 to 0, keeping the imported feet at floor level during attacks. `VanguardAnimator` uses a .8-unit displacement cycle (previously 1.2), and one child renderer borrows the appropriate front-pose knuckle crop above the Sword. It mirrors with the body, hides on death and is disabled for Staff/Daggers and rear views. The imported hero's hit silhouette sorts above the grip. Eight 11x8, 128-PPU imports contain unchanged sampled body pixels; `VanguardArtSet.Frame` stores optional grip/registration. No new AI painting, runtime texture generation, gameplay changes or other scene edits.

`EnemyLookView` scales the Mite health-bar parent to (.65,.8,1), leaving shared sprites, fill ratio, anchoring and all other enemy bars intact. Existing source assets/GUIDs, package lock and settings are preserved. Tooling: `BuildVanguardAssets`, `Run-VanguardProof -ContactGrip [-Reimport]`, and retained `VanguardContactCapture`. Real four-direction poses, 24 consecutive movement frames, unchanged before captures and physical S23 images are in `ArtDirection/2026-09-17/contact-grip-01/`.

Tests add front-grip pose/mirror/flash/death coverage and compact Mite width/fill/death coverage; existing attack tests now assert unchanged body position and other loadouts assert no sword-grip layer. Strict world-space foot locking remains unimplemented: four sampled poses still slide, and attacking replaces the walk pose. Large-enemy art/occlusion, matching equipment and final lighting remain next.

**Verified:** .NET **272/272**, compile **0 warnings/errors**, targeted Vanguard/Mite **13/13**, graphics capture **1/1**, full EditMode **284/284**, PlayMode **96/96**, Android exit **0**, integrity **375 GUIDs / 555 scene objects/components**. APK **24,739,917 bytes**, SHA-256 **`1DCB931D6ECCA73424870687AB3AB32E21AC4E2C6BACD67A3C3B66492F4FB01C`**. Installed S23 hash matches. See newest Docs/23 for the completed proof run and preserved owner progress.


## Cinder Mite and imported hit effects — 2026-09-17

The first imported enemy now uses a shared `EnemyArtSet`: front/rear idle, two travelling contacts and strike, plus two collapse/ember frames. `EnemyLookView` chooses four diagonal facings from displacement or the actual struck target, mirrors only the body, keeps the current pose during pause and briefly compresses on damage. Other enemy looks retain the procedural path. `EnemySpriteCache.EnsureBars` initializes the shared bars even when the first actor is imported. Only the Mite prefab's art-set reference and cosmetic nudge (.04) change; enemy data, movement, damage, rewards and cadence are untouched.

`StrikeArtSet` supplies the new sword slash and shared impact spark. The slash follows the target vector; pooled Mite remains survive removal of the actor, fade over .4 seconds and do not delay death, XP or room progress. Imported textures are borrowed and never destroyed by views. All effects continue using the existing bounded pool; saturation may truncate older visuals. Staff/Daggers-specific strikes, coins and the ability ring remain procedural.

Files: two art-set types; `EnemyLookView`, `EnemySpriteCache`, `CombatEffectsView`; CinderMite prefab; one effects-set reference in Gameplay; imported art under Characters/CinderMite and Effects/FoundryStrikes; `MiteAnimationTests` and the existing art-lifetime tests; reproducible importer/capture/source bridge in Tools/ArtReview. Retained source PNGs, exact prompts, corrected/rejected front contact, hashes, real scene captures and browser gallery: `ArtDirection/2026-09-17/mite-runtime-01/`. No project settings, package lock or existing GUID changed.

Two existing art-lifetime tests now select the actual imported/procedural path instead of assuming the first enemy is procedural; their original lifetime, independent health/flash and cleanup assertions remain. The initial full run stopped at 92/94 and skipped the APK until these checks were updated. Five new scene tests cover shared ownership and reload, four facings/travel/pause, actual strike and hit silhouette, immediate kill/XP with actor-independent death, and pooled VFX rotation/tint/flip reuse. The ten-actor art capture disables attacks for readability; it is not a balance or performance run. World-space foot planting, finger occlusion and the remaining enemy roster are still open.

**Verified:** .NET **272/272**, compile **0 warnings/errors**, targeted Mite **5/5**, art/HUD **9/9**, graphics capture **1/1**, full EditMode **284/284**, PlayMode **94/94**, Android exit **0**, integrity **367 GUIDs / 555 scene objects/components**. APK **24,739,942 bytes**, SHA-256 **`23EDF4B3D2FA6DC853A4B579254D5200D5483009D1B4B1A948EB6EDCC56BEED1`**. Physical-device review and preserved progress are recorded in the newest Docs/23 entry.


## Four-direction Vanguard and support-foot revision — 2026-09-17

Eight imported front body/flash pairs join the rear set; two rear passing poses lower the support sole. `VanguardArtSet` retains optional front compatibility and shared ownership. `HeroLookView` observes the existing actual-target `Struck` event; `VanguardAnimator` chooses front/rear from travel or a stationary hit target and mirrors only the presentation subtree for left facings. A normalized 0.15 axis dead band retains cardinal/idle facing; strike/recovery hold direction. Sword/buckler anchors, draw order and sword angles differ by facing. Movement, attacks, economy, telemetry, spacing, scene, package/project settings and existing GUIDs are unchanged.

Front registration is 128x180 at 128 PPU with a common crop/pivot preserving its lower boot pixels; rear stays 128x176. `BuildVanguardAssets.cs` and the source bridge rebuild both sets. `Run-VanguardProof.ps1 -Directions` captures the actual scene for all four facings and three loadouts. Two new `VanguardAnimationTests` cover travel/idle/dead-band/pause and lethal-target facing/front flash/shared reload lifetime, bringing the class to six tests. Art masters, exact built-in generation prompts/hashes, renders, gallery and limits: `ArtDirection/2026-09-17/vanguard-directions-01/`.

**Verified:** .NET **272/272**, compile **0 warnings/errors**, targeted PlayMode **6/6**, graphics capture **1/1**, full EditMode **284/284**, PlayMode **89/89**, Android exit **0**, integrity **333 GUIDs / 555 scene objects/components**. APK SHA-256 **`C793A3F11F58AB82049892BB939D9CEC606893D3ABCBEC2974FAA0F8173A329A`**; installed S23 hash matched. All four Sword facings were captured during live drags; fresh saved profile was preserved byte-for-byte through install/review. See `23`.

**Limits:** two painted views plus mirroring, not eight independently painted directions. Mirrored lighting/handedness, exact planted-foot motion and finger occlusion remain. Staff/Daggers and enemies/effects retain earlier art. Their directional loadouts were covered in Unity; this phone slice used Sword only and is not a crowd/performance benchmark.

## First imported Vanguard runtime animation — 2026-09-17

Eight body/flash pairs and separate sword/buckler sprites now drive the real hero. The new `VanguardArtSet` is referenced once by Gameplay; `HeroLookView` borrows its sprites and retains the procedural fallback/Staff/Daggers. `VanguardAnimator` observes actual floor displacement and selects four walking frames, returning to idle on stop and freezing with combat pause. Sword windup observes a read-only `WeaponRuntime.CooldownRemaining` and an in-range target; the existing post-damage attack event starts strike immediately, followed by recovery. Weapon cadence, damage, movement, rewards, enemy spacing, simulation and save schema are unchanged.

Imported body and flash textures use a shared 128x176 canvas at 128 PPU, bilinear filtering, fixed pivot and per-frame hand anchors. Android uses ASTC 4x4, no CPU readback/mipmaps. Views never destroy imported references. Existing flash/death/contact-shadow ownership remains. The scene changes only the art-set reference; project/package settings and existing metas are preserved.

Four new `VanguardAnimationTests` exercise movement/pause/release, damage-aligned strike/recovery/windup, moving flash/death/shared lifetime through reload, and owned Staff/Daggers attack/walk. The arena test now expects the imported set. The separate graphics-enabled runtime capture passed **1/1** and produced all observed walk frames plus sword attack phases. Captures, source masters/hashes, exact prompts, import choices and known limitations: `ArtDirection/2026-09-17/vanguard-runtime-01/`. Reproduction: `Tools/ArtReview/Run-VanguardProof.ps1`.

**Verified:** .NET **272/272**, compile **0 warnings/errors**, EditMode **284/284**, PlayMode **87/87**, Android exit **0**, integrity **317 unique GUIDs / 555 scene objects/components**. Development APK SHA-256 **`971FEF6BCEBF5EB8EAE06DEA51ECF08780833485D8FE80E1142511D016A90ADE`**. Full scene/simulation parity remained green. Installed S23 package hash matched; the physical review is recorded in `23`.

**Limits:** single rear northeast facing; opposing facings, planted-foot cleanup and finger occlusion remain. Staff/Daggers, enemies and effects retain prior procedural artwork. This is an integrated character prototype, not final animation or a completed roster. No claim of reduced hero-startup allocation or mid-range performance.


## Matching equipment and registered keyposes — 2026-09-17

`ArtDirection/2026-09-16/hero-motion-01/` adds original transparent sword/buckler masters, two candidate step poses, one windup, provenance/metadata and actual Unity scene evidence. One duplicate-footfall attempt was corrected and excluded from playback. All body poses use one 119×176 canvas at the 128-PPU candidate density and the same pivot; anchors are measured per pose. The temporary capture tool now shares setup between the preceding resolution proof and `RenderHeroEquipmentKeyposes`, selected through `Run-CharacterProof.ps1 -Keyposes`. No authored game source, scene, sprite imports or project settings changed.

**Verified:** .NET **272/272**, compile **0 warnings/errors**, targeted keypose PlayMode capture **1/1** and preceding resolution capture **1/1** after setup refactoring; common size/pivot and grip placement assertions, browser preview and source hashes. Static integrity after temporary cleanup: **293 GUIDs / 555 scene objects/components**. Full runtime suites were not rerun; no APK build/install or phone actions. Latest recorded device APK remains **`24F1FD800362ACBDFB518C8F86D78367E3068FDF11271BF1C071F1A3476CABDA`**. The loop is rough blocking, not accepted walking or attack animation: passing/down, strike/recovery, contact timing, grip occlusion, opposing facings and Mite animation remain.

## Character sampling and weapon attachment in Unity — 2026-09-16

`Tools/ArtReview/Run-CharacterProof.ps1` reproduces an isolated, graphics-enabled scene review from the unchanged hero/Mite concept masters. It stages one temporary PlayMode fixture with fresh metadata, uses TestProfile, and removes the temporary files afterwards. Actual Unity captures in `ArtDirection/2026-09-16/character-unity-01/` compare 32/64/128 PPU at 1.375/0.5-unit hero/Mite height, point versus bilinear at 128, old versus candidate hand anchors, all three existing loadouts, and twelve rigid translation/sword-rotation frames. The browser board exposes these comparisons. Mipmapped GPU sampling is a resolution experiment, not final importer/compression validation. No actual gait, facing or new attack frames were authored; matching weapon art and finger occlusion remain open.

**Verified:** .NET **272/272**, compile **0 warnings/errors**, targeted graphics-enabled PlayMode capture **1/1**, fixed world-height assertions and zero grip-transform error across twelve diagnostic samples; static integrity **293 GUIDs / 555 scene objects/components** after cleanup. Desktop/narrow browser controls and image loading checked. Full EditMode/PlayMode suites were not rerun because no runtime source or authored Assets changed. No APK build/install or phone input occurred. Existing APK hash checked unchanged: **`24F1FD800362ACBDFB518C8F86D78367E3068FDF11271BF1C071F1A3476CABDA`**; recorded full runtime/device gate remains Opus's spacing slice below. Initial review-tool failures and their corrections are recorded in the proof README.

## Vanguard/Mite character concept pair — 2026-09-16

Art-only deliverable: `ArtDirection/2026-09-16/character-proof-01/` contains three unchanged original generated PNGs (one rejected hero exploration, the revised empty-handed hero and one Mite), exact prompts, alpha/hash metadata and an interactive browser review. The review compares detail, candidate source heights of 165/60 pixels, two/eight Mites, silhouettes and foot markers against an earlier Unity screenshot. The floor is schematic and the actors are static; no simulation or phone acceptance is implied. Production imports, pivots, weapon layers, animation and the existing 32-PPU contract are unchanged. Pipeline/roadmap/decision/handoff and the room production plan record what remains.

**Verified:** desktop and narrow 390x844 browser review, image loading, scale/crowd/silhouette/foot-point controls, no horizontal page overflow or captured console warnings/errors; original PNG SHA-256 values match the saved metadata. No Unity tests, APK build/install or device actions were run for this concept-only slice. Latest runtime gate remains Opus's recorded spacing gate: .NET **272**, EditMode **284**, PlayMode **83**, APK SHA-256 **`24F1FD800362ACBDFB518C8F86D78367E3068FDF11271BF1C071F1A3476CABDA`**; see the spacing section below and `23`. Those checks were not rerun here.

## Stone/rim materials and actor contact shadows — 2026-09-16

`PlatformArt` now draws quiet upper bevels/lower lips, broad per-slab mineral patches and sparse chips, plus flush coping stones and brass clamps inside the existing rim. Surface dimensions, silhouette, central inlay/emblem, light frames and walkable geometry remain. Sidewall and lantern art are deliberately still the earlier prototype drawing. Actual centre/far/right Unity review renders at both portrait sizes and provenance are in `ArtDirection/2026-09-16/stone-contact-01/`.

`ContactShadowArt` supplies one 32x16 translucent ground patch and a pure conservative diamond-fit rule. `ContactShadowView` attaches below the hero/enemy root, independently of the animated body, sorts above the platform lights and below actors, follows movement, contracts at the edge and hides when health is dead. It borrows a lazily generated `ArenaSpriteCache` sprite across waves/reloads. Session reset/quit destroys sprite and texture. The extra texture costs 2,048 RGBA32 texel bytes; movement does not regenerate art. Chests have not received this actor component. There is no scene/prefab, gameplay, economy, telemetry, package-lock or project-settings edit.

Added `Art/ContactShadowArt.cs`, `UI/ContactShadowView.cs`, `Tests/EditMode/ContactShadowArtTests.cs` and new metadata. Changed `Art/PlatformArt.cs`, `Art/Unity/ArenaSpriteCache.cs`, `UI/ArenaView.cs`, `HeroLookView.cs`, `EnemyLookView.cs`, `Tests/PlayMode/ArenaArtCacheTests.cs` and `ArtHudFlowTests.cs`. Two pure tests validate the contact image's alpha/symmetry and sweep complete sprite bounds at three actor widths over symmetric and shifted arenas. One new scene test covers sorting, body-nudge independence, death, rim contraction and scene reload/unused-asset cleanup; the existing session-release test now includes the shadow resource. The temporary visual-staging fixture was removed before the full gate.

The first full gate caught `NullReferenceException` in `ContactShadowView.Start` when an existing test cloned a live enemy, including its generated child whose private state Unity does not serialize. `Attach` now rebinds the existing child/renderer to the new owner instead of leaving an orphan component and creating another patch. The clone test also verifies one shadow per clone and independent health/death visibility. The targeted nine-case `ArtHudFlowTests` passed after the fix; no APK was built from the failed gate.

**Verified:** .NET **262/262**, compile **0 warnings/errors**, EditMode **274/274**, PlayMode **80/80**, Android exit **0**, integrity **292 unique asset/folder GUIDs / 555 scene objects/components**. APK **24,739,335 bytes**, SHA-256 **`8DD7866A84537B9B4D91644C685F894717460A86DC1E8261BC506E2F3BEF5E3D`**, installed S23 hash matched. The fallback environment plus shadow cache retains **38 textures / 8,368,004 RGBA32 texel bytes**, excluding native overhead; the authored imported-cavern mode does not build unused void textures. Device observations and limits are in the corresponding `23` entry.

## Latest slice — wider portrait movement framing (2026-09-16)

Following the owner's normal-room/boss-arena direction, the gameplay camera width changes from 6 to 9. Actor scale, arena geometry, controls, HUD, balance and pack capacity are unchanged. Widths 6, 7.5 and 9 were compared with identical staged visuals at tall and short portrait target sizes; rationale and evidence limits are in `ArtDirection/2026-09-16/camera-review/README.md`. The furnace-cavern board in `arena-target/` is a future art target, not the current runtime scene.

Files: `Scenes/Gameplay/Gameplay.unity` (one camera field), `Tests/PlayMode/ArtHudFlowTests.cs` (extend existing guardian bounds coverage to width 9). The temporary offscreen render fixture was removed before the full gate. Live ten-enemy kiting, boss-specific geometry and new environment/actor art remain separate work.

**Verified:** .NET **239/239**, Unity compile **0 warnings/errors**, EditMode **251/251**, PlayMode **63/63**, Android build exit 0 and static integrity passed (272 project GUIDs; 555 scene objects/components). APK SHA-256 **`0E6CE9B82B47B6999B0AF68D8708B2882915AEDE0F16E861685755158FD1C4E2`**. The offscreen comparison has a separate final 1/1 capture-fixture pass; it is not included in the shipped suite count. Phone results are in the latest docs/23 entry.

## Latest slice — foundry result and Forge menus (2026-09-16)

The owner authorised extending the approved UI to the result and Relic Forge screens. This slice reuses the imported frame and two relic icons. It adds no artwork dependency, shop rule or save schema.

- Result: one opaque framed summary with outcome/cause, progress, banked/lost gold, build, next-unlock hint and two framed actions. Existing result strings and input-delay behaviour remain unchanged.
- Forge: separate weapon and relic sections, five full-row card targets, a persistent bottom Start Run action, and safe-area-relative vertical layout. Existing prices, effects and state text remain visible. Equipped/affordable/owned/too-expensive states have green/brass/blue/grey accents in addition to their written labels. Relic icons resolve from the current effect instead of being assumed from card order.
- Files: `Scenes/Gameplay/Gameplay.unity`, `Scripts/UI/RelicForgeView.cs`, `Tests/PlayMode/ArtHudFlowTests.cs`, `Tests/PlayMode/RelicForgeFlowTests.cs`. The one-shot Editor migration was removed after authoring; its local record is `TestResults/ApplyFoundryMenus.cs`. Art rationale and remaining work: `ArtDirection/2026-09-16/MENU_INTEGRATION.md`.
- Verification adds text-fit coverage at 1080x1760 and 1080x2232 safe-area sizes and extends the existing purchase/equip test with icon identity and the affordable-to-equipped accent change. Existing double-tap, price, persistence and touch-bound tests remain required.

**Verified:** .NET **239/239**, Unity compile check **0 warnings/errors**, EditMode **251/251**, PlayMode **63/63**, development APK build and static integrity passed (272 project GUIDs; 555 scene objects/components). APK SHA-256 **`B8176A69112EAFB7E8E7322B4768124102FACAE631F77E3B8BE6915D5B5D37E0`**. Device observations are recorded in `23_ANDROID_DEVICE_VALIDATION.md`.

## Latest slice — compact foundry UI (2026-09-15–16)

The owner approved the combined art direction and resumed Unity/device work after telemetry commit `127f4de`. Eight UI images from the approved art kits are now imported; no generated combat screenshot or replacement character sprite is used.

- Compact floor/room text, room pips, one gold counter, actual upgrade-stack badges, equipped relic state and a boss-only readout. Detailed weapon, target, relic and gold-at-risk text is available through Pause. Second Wind's badge shows remaining charges (1/0); Counterweight's number is triggers, not stacks or damage.
- A grouped lower HP/XP panel, a red health fill and blue XP fill. Forge Burst has separate glyph, circular mask, bezel, cooldown fill and countdown text. Imported assets survive scene reload; the procedural fallback owns only its own generated texture.
- Opaque, framed choice cards with live text and full-card hit areas. Upgrade icons resolve by stable content ID through the active offer, including when reaching a stack cap changes slot order. Forge, checkpoint and unillustrated future options do not inherit unrelated icons.
- A quieter local floor palette and fewer speckles in `PlatformArt`; dim inlays and a retained bright rim. Geometry, movement, characters, balance, telemetry and save rules are unchanged.
- Camera width 6 instead of 4.6. Full guardian body/bar bounds are checked analytically for both 6 and 7.5 on short/tall portrait layouts, with horizontal follow-lag allowance. The actual S23 review uses 6; a paired 6/7.5 phone comparison is not claimed.

Files: `Scripts/UI/RunHud.cs`, `RunChoiceView.cs`, `AbilityButtonView.cs`, `Scripts/Art/PlatformArt.cs`, `Data/UI/PrototypeText.asset`, `Scenes/Gameplay/Gameplay.unity`, eight textures and import metadata in `Art/UI/`, `Tests/EditMode/PlatformArtTests.cs`, and new `Tests/PlayMode/ArtHudFlowTests.cs`. Two temporary Editor migrations authored the scene/imports and corrected the first phone review's health colour and square icon corners; both were removed. Their local copies are under ignored `TestResults/`.

Art rationale, import limits and handoff: `ArtDirection/2026-09-15/UNITY_INTEGRATION.md`. Device evidence and any remaining limits: `23_ANDROID_DEVICE_VALIDATION.md`. Result/Relic Forge screens, the large pause title/resume control, per-badge tap details, walk/facing animation and shipping texture optimisation remain later art work.

**Verified:** after the device-driven corrections, .NET **239/239**, Unity compile check **0 warnings/errors**, EditMode **251/251**, PlayMode **62/62**, development APK build and static integrity passed (272 project GUIDs; 515 scene objects/components). APK SHA-256 **`8D1515975F8C3F1E0987FACC61379AFEF9B1E5239EA7D92C38DE23E0C517A149`**; the installed `base.apk` hash matches. S23 captures confirm final red HP/blue XP, circular ability artwork, cooldown response, readable choices and pause details; the earlier same-layout pass also reached the right-side guardian and Extract. See `23` for distinctions and remaining device coverage.

Date: 2026-09-12. Scope follows `16_FIRST_PROMPT.md`, with `01`, `03`, and `06` as primary constraints. All 22 original Markdown documents were read before implementation. The repository initially contained only `docs/`, with no Unity project, source code, or Git repository.

The first requested implementation stops after **one hero automatically attacks one enemy until the enemy dies**. Day 3 is planned, not implemented. This is not the full mechanical milestone or Gate A.

## Project and package setup

- Project root: `E:/MobileGame`; Git initialized on `main`, with no commit or remote created.
- Pin Unity **6000.0.65f1**, actual installed revision `a18e2220bd50`. Unity corrected the initially hand-authored revision during first import; the Editor version number is unchanged. This is a fixed prototype baseline, not a claim that it is the newest release. See [official release notes](https://unity.com/releases/editor/whats-new/6000.0.65f1).
- Built-in renderer, orthographic 2D camera, portrait. No render pipeline dependency is needed for solid-color SpriteRenderers.
- Pin Unity Test Framework **1.6.0** in `Packages/manifest.json`, matching the version bundled with the installed 6000.0.65f1 Editor. The initial documentation-based choice was 1.4.3, but the actual Editor resolves its built-in 1.6.0; the manifest now states that effective version explicitly. Unity generated `packages-lock.json`, resolving built-in NUnit 2.0.5 plus IMGUI and JSON serialization modules. Both test suites passed with this resolved stack.
- The built-in IMGUI module supplies the temporary, read-only HUD. Text rendering types ship with the Editor and do not have a separate `com.unity.modules.textrendering` package in this version; the first real import exposed and removed that invalid dependency. Before Day 3's interactive upgrade choice, add uGUI and replace this HUD with a Canvas using the package version resolved for the pinned Editor. No input package is needed for automatic combat.
- Android-first player settings: portrait, ARM64/IL2CPP, minimum API 26, target API automatic. These are prototype build settings; store submission policy validation is later work.
- Install Android Build Support, SDK/NDK, and OpenJDK through Unity Hub when doing the first Android build. Desktop Play Mode does not require these modules.
- Point-filtered 16x16 solid white placeholder texture, tinted/scaled in scene. This does not lock final character resolution or art style. Defer Pixel Perfect and Aseprite packages until an actual pixel-art pipeline is being validated.

## Day 1 — project and authored scene

Implementation order:

1. Add `.gitignore`, `.gitattributes`, project version, package manifest, portrait PlayerSettings, text serialization/visible meta settings, and build scene list.
2. Author `Assets/_Project/Scenes/Gameplay/Gameplay.unity` with this hierarchy:

```text
Main Camera                   orthographic; dark solid background
Vanguard                      Health, Targeting, AttackController, CombatantView
  Hero Body                   blue SpriteRenderer
    Sword Placeholder         narrow light rectangle
    Shield Placeholder        dark blue rectangle
Grunt                         Health, CombatantView
  Enemy Body                  orange SpriteRenderer
Combat Setup                  definition-to-runtime composition
Prototype HUD                 read-only labels and health bars
```

3. Add one shared shape asset and three static gameplay definitions, then wire every reference explicitly in the scene.

Acceptance: project imports without errors; the gameplay scene contains one hero and one enemy; both are legible at 9:16 and 9:19.5; Gameplay is the only enabled build scene. No bootstrap/menu scene is needed before there are persistent services.

## Day 2 — first playable combat slice (implemented)

Implementation order:

1. `DamageContext`, `IDamageable`, and `HealthState`: validate damage, clamp health, and emit one death transition.
2. `Health`: component adapter for per-instance health and events.
3. `WeaponDefinition` and `WeaponRuntime`: author stats and snapshot them into per-attacker state.
4. `Targeting`: scan an explicitly serialized candidate list for the nearest active living target within range. No scene searches, physics, or global registry.
5. `AttackController`: tick cadence, acquire target, apply direct damage, and emit attack feedback. First hit is immediate; no accumulated burst after a long frame.
6. `CombatSetup`: initialize both health instances and one weapon in `Awake`. Views bind events separately; combat begins in `Update` after initialization.
7. `CombatantView` and `PrototypeHud`: show hits, HP, death, and attack count. HUD string formatting occurs on events rather than every frame.
8. Add calculation/state tests, authored-scene tests, static reference checks, and this verification guide.

Acceptance:

- With no input: five hits of 10 damage defeat the 50 HP Grunt in about 3.2 seconds at normal frame rates (well within the documented 15-second first-kill target).
- Enemy HP follows 50 → 40 → 30 → 20 → 10 → 0; the first update may already show 40.
- Death fires once, the Grunt body disappears, and attacks remain at five afterward.
- Outside a 3-unit center-to-center range or while disabled/destroyed, the target is not attacked. Returning a living target to range resumes attacks.
- Pausing scaled time stops attacks; a dead/disabled hero cannot attack.
- Combat does not modify any ScriptableObject. A fresh Play Mode session has fresh health and weapon state.
- Both Unity test suites pass (26 EditMode and five PlayMode cases). The Android development APK also passed a short smoke test on Samsung SM-S911B; see `23_ANDROID_DEVICE_VALIDATION.md` for evidence and remaining profiling limits.

## Day 3 — reward and upgrade slice (implemented: kill → XP, upgrade choice, next encounter)

### Implemented 2026-09-12: kill → XP once

| Files | Change |
|---|---|
| `Scripts/Core/RunState.cs` | New. Plain C# run XP with `ExperienceChanged`; rejects negative amounts. Level, phase and upgrade state are not added yet. |
| `Scripts/Economy/RewardService.cs` | New. `TryAwardKill(IDamageable)` rewards a dead victim once, tracked in a set, so repeated or re-entrant death callbacks cannot duplicate XP. |
| `Scripts/Content/EconomyConfig.cs` | New. Only `_experiencePerKill`; the level threshold arrives with the upgrade slice. |
| `Data/Economy/PrototypeEconomy.asset` | New. 10 XP per kill. |
| `Scripts/Core/CombatSetup.cs` | Requires `EconomyConfig`, creates `RunState` and `RewardService`, awards on `Grunt.Died`, unsubscribes on destroy; exposes `Run`. |
| `Scripts/UI/PrototypeHud.cs`, `Scripts/Content/PrototypeTextDefinition.cs`, `Data/UI/PrototypeText.asset` | XP label from `prototype.experienceFormat`. The HUD listens to `ExperienceChanged`, because `Health.Changed` fires before `Died` and would refresh before the award. |
| `Scenes/Gameplay/Gameplay.unity` | Two references: `CombatSetup._economy` and `PrototypeHud._setup`. |
| `Tests/EditMode/RewardTests.cs`, `Tests/PlayMode/CombatSceneTests.cs`, `Tools/CombatChecks/CombatChecks.csproj` | Eight reward/run-state cases; the kill test asserts 10 XP; a new test applies lethal damage twice and expects one award. |

Deliberate deviation from the table below: the tests live in `RewardTests.cs` rather than `RewardAndUpgradeTests.cs` until upgrade tests exist.

Verified: Unity EditMode 34/34, PlayMode 6/6, `Verify-Project.ps1`, .NET CombatChecks 34/34, development APK on Samsung SM-S911B showing `XP: 0` during combat and `XP: 10` after the fifth hit (see `23`).

### Implemented 2026-09-13: XP threshold → choose one of two upgrades

| Files | Change |
|---|---|
| `Scripts/Progression/ModifierOperation.cs`, `StatModifier.cs`, `ModifiableStat.cs`, `WeaponStat.cs` | New. `Value = (base + Σflat) × (1 + Σpercent)`, clamped to a safety minimum. Percents are summed, not compounded, per `05`. Enum values are serialized, so they are append-only. |
| `Scripts/Progression/UpgradeOption.cs`, `UpgradeOffer.cs`, `UpgradeService.cs` | New, plain C#. One offer per pending level, deterministic pool order, stack limits. `TrySelect(offer, slot)` closes the offer before side effects and rejects stale or repeated offers by reference, so double taps and re-entrant callbacks apply one upgrade. |
| `Scripts/Content/UpgradeDefinition.cs`, `Data/Upgrades/Upgrade_Damage.asset`, `Upgrade_AttackSpeed.asset` | New. Tempered Edge: +5 flat damage. Quickened Grip: +25% attack speed (0.8 s → 0.64 s). Five stacks each. `CreateOption()` snapshots data like `WeaponDefinition.CreateRuntime()`. |
| `Scripts/Combat/WeaponRuntime.cs` | Damage and attack speed are `ModifiableStat`s; `Interval = baseInterval / attackSpeed`. `AddModifier` raises `StatsChanged`. A running cooldown keeps its remaining time. |
| `Scripts/Core/RunState.cs`, `Scripts/Content/EconomyConfig.cs`, `Data/Economy/PrototypeEconomy.asset` | Level, pending and applied upgrade counts; `_experiencePerLevel: 10` (linear until encounters exist) and `_upgradeChoiceCount: 2`. |
| `Scripts/Core/CombatSetup.cs` | Builds upgrade options from `_upgrades` and composes `UpgradeService`. Sets `Time.timeScale` to 0 while an offer is open and restores it on close or destroy. uGUI input uses unscaled time. |
| `Scripts/UI/RunHud.cs`, `UpgradeChoiceView.cs`, `SafeAreaFitter.cs` | New uGUI Canvas HUD replacing IMGUI (`PrototypeHud.cs` removed). The weapon label now reads runtime stats. Two 320-unit touch cards on a dimmed overlay; cards disable on the first tap and stay disabled for 0.25 s unscaled after showing. Content is kept inside `Screen.safeArea` because the Android player renders behind the cutout. |
| `Scripts/UI/CombatantView.cs` | Feedback uses unscaled time so a hit flash settles during the pause. |
| `Scenes/Gameplay/Gameplay.unity` | HUD Canvas, Upgrade Canvas and EventSystem, generated by a temporary Editor builder that was deleted after saving. The scene is now Unity-serialized. |
| `Packages/manifest.json`, `packages-lock.json`, asmdefs | `com.unity.ugui` 2.0.0 (built into this Editor); lock gained only `com.unity.ugui` and `com.unity.modules.ui`. Gameplay, Editor and PlayMode test assemblies reference `UnityEngine.UI`. |
| `Tests/EditMode/UpgradeTests.cs`, `Tests/PlayMode/UpgradeFlowTests.cs`, `CombatSceneTests.cs` | 19 cases: modifier order, percent summing, clamps, validation, cooldown behaviour, level thresholds, single/stale/re-entrant selection, stack limits. 4 PlayMode tests: kill opens two choices and pauses, rapid taps apply once, attack speed changes only runtime stats and the HUD, taps before the input delay are ignored. The source Sword asset is asserted unchanged. Existing waits after a kill use real time. |
| `Tools/Verify-Project.ps1`, `Tools/CombatChecks/CombatChecks.csproj` | Package script GUIDs resolve from `Library/PackageCache` or the Editor's built-in packages; the .NET mirror compiles `Scripts/Progression`. |

Deviations from the planned table below: no `UpgradeChoicePanel.prefab` yet (one panel, authored in the scene; extract when a second screen reuses it); `ModifiableStat`, `UpgradeOption` and `UpgradeOffer` were added so the service stays plain C# and runs in the .NET mirror; offers are deterministic rather than random.

Verified: Unity EditMode 53/53, PlayMode 10/10, `Verify-Project.ps1`, .NET CombatChecks 53/53, development APK on Samsung SM-S911B (see `23`).

### Implemented 2026-09-13: next Grunt encounter after the choice

| Files | Change |
|---|---|
| `Prefabs/Enemies/Grunt.prefab` | New. The authored Grunt (Health, CombatantView, Enemy Body) extracted by a temporary Editor builder, which was deleted afterwards. The scene no longer contains a Grunt. |
| `Scripts/Combat/EncounterProgress.cs` | New, plain C#. Encounter number, hits taken this fight, scaled clear time (elapsed stops at the clear), and `Tick(deltaTime, canAdvance)` that counts the advance delay only while progression is allowed. |
| `Scripts/Combat/EncounterController.cs` | New. Spawns every encounter from the prefab at its own position with fresh `Health`, sets the hero's targeting candidates, raises `EncounterStarted`, `ProgressChanged` and `EnemyDefeated(Health)`. Starts the next encounter 1 s (scaled) after a clear, only when no upgrade offer is open; unsubscribes from and destroys the previous enemy first. |
| `Scripts/Combat/Targeting.cs` | `SetCandidates(Health[])`; the serialized list is empty in the scene. |
| `Scripts/Core/CombatSetup.cs` | No longer owns an enemy. Subscribes rewards to `EnemyDefeated` before `EncounterController.Initialize(Upgrades)` spawns the first Grunt. |
| `Scripts/UI/RunHud.cs`, `Scripts/Content/PrototypeTextDefinition.cs`, `Data/UI/PrototypeText.asset` | HUD follows the current encounter: `Encounter {0}: combat is automatic`, `Hits this fight: {0}`, and `{0} defeated in {1} hits, {2:0.0}s`. The enemy name now comes from the definition instead of a fixed "Grunt defeated" string. Subtitle shortened to fit one line. |
| `Scenes/Gameplay/Gameplay.unity` | `Encounter` object at (0, 1.2, 0) with the controller; `CombatSetup._encounters` and `RunHud._encounters` wired. |
| `Tests/EditMode/EncounterTests.cs`, `Tests/PlayMode/UpgradeFlowTests.cs` | 11 cases: numbering and reset, hit and clear-time counting, single clear, delay gated by `canAdvance`, validation, an earlier victim never rewarded again, and 60 Hz simulations showing +5 damage kills in 4 hits (~2.4 s) and +25% attack speed in 5 hits (~2.56 s vs 3.2 s). 3 PlayMode tests: no next encounter while the choice is open; damage upgrade → next Grunt is a new instance with 50 HP, targeted, killed in 4 hits, XP exactly 20 and a second offer; attack-speed upgrade → next clear at least 0.4 s faster. |

Unchanged by design: enemy stats do not scale between encounters (floors and scaling belong to the Descent slices), and every encounter is a Grunt.

Verified: Unity EditMode 64/64, PlayMode 13/13, `Verify-Project.ps1`, .NET CombatChecks 64/64, development APK on Samsung SM-S911B (see `23`).

### Implemented 2026-09-13 (Day 4 item): enemy attacks, hero death, result screen and restart

| Files | Change |
|---|---|
| `Data/Weapons/Weapon_GruntStrike.asset`, `Scripts/Content/EnemyDefinition.cs`, `Data/Enemies/Enemy_Grunt.asset` | New enemy weapon: 6 damage every 1.0 s, range 3. Enemies reference a `WeaponDefinition` and attack through the same `WeaponRuntime` path as the hero. |
| `Prefabs/Enemies/Grunt.prefab` | Gains `Targeting` and `AttackController` (owner and targeting wired inside the prefab); `CombatantView` lunges the Grunt 0.22 units toward the hero on each strike. Edited by a temporary builder that was deleted afterwards. |
| `Scripts/Combat/EncounterController.cs` | Requires the hero `Health`; each spawned enemy targets the hero and gets a fresh weapon runtime. The next encounter only starts while the hero is alive. Validates that the prefab is armed. |
| `Scripts/Core/RunState.cs` | `End()` raises `Ended` once; experience after the end is ignored. |
| `Scripts/Progression/UpgradeService.cs` | `Pool` for the result build list; ending the run withdraws an open offer and rejects further selections. |
| `Scripts/Combat/EncounterProgress.cs` | `EncountersCleared`. |
| `Scripts/Core/CombatSetup.cs` | Hero death ends the run. `RestartRun()` restores time scale and reloads the Gameplay scene once, rebuilding every runtime object from definitions. |
| `Scripts/UI/RunResultView.cs`, `Scripts/Content/PrototypeTextDefinition.cs`, `Data/UI/PrototypeText.asset` | Result canvas above the upgrade panel: **Defeated**, `{hero} fell to a {enemy} in encounter {n}`, encounters cleared / level / XP, the build (`Tempered Edge x5, Quickened Grip x5` or `no upgrades`), and a 260-unit **Try again** button that stays disabled for 0.5 s unscaled and disables itself on the first tap. |
| `Scenes/Gameplay/Gameplay.unity` | `Result Canvas` (sorting order 20) with the view; `Encounter._hero` wired. |
| `Tests/EditMode/RunEndTests.cs`, `Tests/PlayMode/RunResultTests.cs`, `CombatSceneTests.cs` | 7 cases: single end and ignored late XP, offer withdrawal, no reward after the end, pool stacks, cleared count, strike damage against the hero (76 HP after the first fight, 58 after an upgraded second), and a full-run simulation that must die within 5–20 encounters. 4 PlayMode tests: strikes deal authored damage; the Grunt kills a wounded hero and the result explains it while all attacks and encounters stop; the result lists cleared encounters and the build; Try again ignores early taps and reloads a fresh run exactly once. The first-kill scene test now expects the hero alive but damaged. |

Balance consequence, recorded for tuning: every encounter starts with a guaranteed Grunt strike, so a run always ends. Taking the first card every time dies in encounter 11 after 10 clears with both upgrades maxed, in about 30 seconds on device. Healing (the planned forge room), enemy scaling and Extract belong to the Descent slices.

Verified: Unity EditMode 71/71, PlayMode 17/17, `Verify-Project.ps1`, .NET CombatChecks 71/71, development APK on Samsung SM-S911B (see `23`).

### Implemented 2026-09-13 (Day 4 item): Runner and Tank archetypes

| Enemy | HP | Weapon | Windup | Prefab |
|---|---|---|---|---|
| Grunt | 50 | Grunt Strike 6 / 1.0 s | 0 | `Grunt.prefab`, orange 1.1 × 0.75 |
| Runner | 30 | Runner Slash 2 / 0.4 s | 0 | `Runner.prefab` variant, yellow 0.55 × 0.95, lunge 0.3 |
| Tank | 120 | Tank Slam 12 / 2.5 s | 1.5 s | `Tank.prefab` variant, purple 1.7 × 1.25, lunge 0.12 |

Encounters follow `Data/Encounters/PrototypeEncounters.asset`: Grunt, Runner, Grunt, Tank, repeating. XP per kill stays 10 for every enemy.

| Files | Change |
|---|---|
| `Scripts/Combat/WeaponRuntime.cs`, `Scripts/Content/WeaponDefinition.cs` | Optional `initialDelay` (windup before the first attack only); `_initialDelay` defaults to 0, so the Sword and Grunt Strike are unchanged. |
| `Scripts/Content/EnemyDefinition.cs` | `_prefab` (Health with Targeting and AttackController). |
| `Scripts/Content/EncounterSequenceDefinition.cs`, `Scripts/Combat/EncounterSchedule.cs` | Ordered, repeating enemy list; plain `IndexFor(encounterNumber, count)`. A stand-in for the Descent floor/room definitions. |
| `Scripts/Combat/EncounterController.cs` | Spawns `sequence.EnemyFor(n)` from its own prefab; validates every entry at startup; exposes `CurrentDefinition` (used by the HUD and result screen). |
| `Prefabs/Enemies/Runner.prefab`, `Tank.prefab`, `Data/Enemies/Enemy_Runner.asset`, `Enemy_Tank.asset`, `Data/Weapons/Weapon_RunnerSlash.asset`, `Weapon_TankSlam.asset` | New content. Variants override only body color, body scale and lunge distance; silhouettes differ so archetypes are readable without color. Created by a temporary Editor builder, deleted afterwards. |
| `Tests/EditMode/EnemyArchetypeTests.cs`, `Tests/PlayMode/EncounterSequenceTests.cs`, `UpgradeFlowTests.cs` | 13 cases: schedule order and validation, windup only delaying the first attack, Runner fight (3 hits, 4–5 chip strikes), Tank fight (12 hits, first slam at 1.5 s, 3 slams, 64 HP left), mixed run lasting longer than Grunts alone. 3 PlayMode tests: authored order with per-archetype prefabs and HP, Runner dying to two upgraded hits while chipping, Tank holding its first slam for the windup. Next-encounter tests became data-relative because encounter 2 is now a Runner. |

Balance measured with the 60 Hz simulation before choosing values (hero always takes the first card):

| Variant | Damage-first run | Speed-first run |
|---|---|---|
| Grunts only (previous slice) | dies in encounter 11 | dies in encounter 7 |
| Chosen Grunt/Runner/Grunt/Tank values | dies in encounter 15 | dies in encounter 7 |
| Same enemies, Quickened Grip +35% | 19 | 11 |
| Same enemies, Quickened Grip +50% | 19 | 15 |

Finding: +25% attack speed was clearly weaker than +5 damage, so choosing it first halved the run. The on-device run (damage first) died in encounter 15, matching the simulation.

**Tuning applied 2026-09-13 (approved by the owner):** `Upgrade_AttackSpeed.asset` is now **+50%** (0.8 s → 0.53 s on the first stack; percents still sum, so five stacks give +250% and 0.23 s). Simulated runs now die in encounter 19 when taking damage first and 15 when taking attack speed first. `EnemyArchetypeTests.EitherFirstPickSurvivesAComparableNumberOfEncounters` guards that neither card becomes a trap again (speed first must reach encounter 10 and at least 70% of the damage-first run). PlayMode expectations read the authored interval.

Verified: Unity EditMode 84/84, PlayMode 20/20, `Verify-Project.ps1`, .NET CombatChecks 84/84, development APK on Samsung SM-S911B including Runner, Tank windup and slam captures (see `23`).

### Implemented 2026-09-13: Descent floor 1 (Ember Halls)

| Room | Kind | Content |
|---|---|---|
| 1 Ember Hall | Combat | Grunt, Runner |
| 2 Cinder Walk | Combat | Runner, Grunt |
| 3 Slag Gate | Combat | Tank |
| 4 The Forge | Forge | **Mend** (restore 40% of maximum health) or **Temper** (one extra upgrade choice) |
| 5 Captain's Post | Elite | **Grunt Captain**: 140 HP, 9 damage / 1.2 s, 0.6 s windup |
| 6 Warden's Crucible | Boss | **Forge Warden**: 300 HP, 10 damage / 1.8 s, 1 s windup; at 50% health +100% attack speed |

| Files | Change |
|---|---|
| `Scripts/Combat/FloorProgress.cs`, `FloorStep.cs`, `FloorStepKind.cs`, `RoomKind.cs` | New, plain C#. Walks rooms and waves; zero-wave rooms are non-combat steps; tracks rooms cleared and the final wave. |
| `Scripts/Combat/IHealable.cs`, `HealthState.cs`, `Health.cs` | `Heal(amount)` clamps to maximum and never revives. |
| `Scripts/Combat/EnrageRule.cs`, `EnrageBehaviour.cs`, `AttackController.cs` | One-time threshold rule (never on the killing blow) and a boss behaviour module that adds an attack speed modifier to the enemy's runtime weapon. `AttackController.Weapon` exposes that runtime. |
| `Scripts/Core/RunState.cs`, `RunOutcome.cs` | `End(RunOutcome)` records Victory or Defeat once; `GrantBonusUpgrade()` and `PendingUpgradesChanged` let the forge add upgrade choices without levelling. |
| `Scripts/Progression/ForgeEffect.cs`, `ForgeOption.cs`, `ForgeOffer.cs`, `ForgeService.cs` | New. One selection per visit, same stale/re-entrant guards as upgrades; withdrawn when the run ends. |
| `Scripts/Progression/RunChoices.cs`, `ChoicePrompt.cs`, `ChoiceCard.cs`, `ChoiceKind.cs` | New single source for "the player is choosing": combines upgrade and forge offers into prompts for the panel, the pause and encounter gating. |
| `Scripts/Content/FloorDefinition.cs`, `RoomDefinition.cs`, `ForgeOptionDefinition.cs`, `Data/Floors/Floor_EmberHalls.asset`, `Data/Forge/*.asset`, `Data/Enemies/Enemy_GruntCaptain.asset`, `Enemy_ForgeWarden.asset`, `Data/Weapons/Weapon_CaptainCleave.asset`, `Weapon_WardenHammer.asset` | Floor data with rooms authored inline; forge options and the elite/boss content. |
| `Prefabs/Enemies/GruntCaptain.prefab`, `ForgeWarden.prefab` | Variants of Grunt and Tank; the Warden adds `EnrageBehaviour`, and its `CombatantView` switches to a red resting color when enraged. Created by a temporary builder, deleted afterwards. |
| `Scripts/Combat/EncounterController.cs` | Now runs the floor: validates every room at startup, spawns waves, opens the forge, waits for choices, and clears the floor immediately on the final wave's kill. Replaces the looping `EncounterSequenceDefinition`/`EncounterSchedule`, which were removed with their asset and tests. |
| `Scripts/Core/CombatSetup.cs` | Composes `ForgeService` and `RunChoices`; pauses on any open choice; hero death → Defeat, floor cleared → Victory. |
| `Scripts/UI/RunChoiceView.cs` (renamed from `UpgradeChoiceView`, same GUID), `RunHud.cs`, `RunResultView.cs`, `CombatantView.cs`, `PrototypeTextDefinition.cs`, `Data/UI/PrototypeText.asset` | Panel shows upgrade or forge prompts with the matching title; HUD shows `Ember Halls | Room n/6 | room`, wave or forge status and the enraged warning; result screen shows Floor cleared or Defeated with the room, rooms cleared n/6, level, XP and build. |
| Tests | EditMode `FloorTests` (12) with a floor simulation through the real run, reward, upgrade, forge and choice services; `EnemyArchetypeTests` keeps its fight cases. PlayMode `FloorFlowTests` (4): authored order to victory, Mend, Temper, Warden enrage and kill. `RunResultTests` and `UpgradeFlowTests` follow the new texts and view name. |

Balance chosen with the floor simulation (hero always takes the first upgrade card):

| | Damage first | Speed first |
|---|---|---|
| Mend at the forge | cleared, 38 HP left | cleared, 34 HP left |
| Temper at the forge | cleared, 17 HP left | cleared, 4 HP left |

Mend is the safer pick when hurt, Temper when healthy; the Warden threatens every path. XP stays at one level per kill for this floor. The measured fight time is only about 22 s plus choices and delays, so a first playthrough takes roughly a minute; floor 2 scaling and longer fights are the next lever toward the GDD's 3–4 minute floors.

Verified: Unity EditMode 89/89, PlayMode 21/21, `Verify-Project.ps1`, .NET CombatChecks 89/89, development APK on Samsung SM-S911B: a full floor cleared in 46 s with 42 HP left (see `23`).

### Implemented 2026-09-13: gold, the Extract/Descend checkpoint and Descent floor 2 (Quicksilver Vaults)

Clearing Ember Halls no longer ends the run. After the Warden's level-up the panel asks **Checkpoint: extract or descend?**

- **Extract** banks all gold and ends the run as **Extracted**.
- **Descend** secures the gold earned so far and, after the one-second advance delay, starts floor 2 with the hero's current health: no heal.
- A death banks the secured gold plus half of the gold earned since the last checkpoint, rounded down (`PrototypeEconomy.asset` At Risk Gold Loss 0.5). Extract and the final victory bank everything. Gold is shown and settled but not yet spent or saved; that is the Forge meta slice.

Gold per kill: Grunt 5, Runner 3, Tank 10, Grunt Captain 20, Forge Warden 50. Ember Halls pays 96. Floor 2 applies its tier (enemy health ×1.4, enemy damage ×1.25) and then the **Cursed Gold** modifier (+20% enemy damage, +50% gold, rounded away from zero), so it pays 154 and a full Descent 250.

| Room | Kind | Floor 2 content |
|---|---|---|
| 1 Mercury Stair | Combat | Grunt (70 HP, 9 damage / 1 s, 8 gold), Runner (42 HP, 3 damage / 0.4 s, 5 gold) |
| 2 Cold Crucible | Combat | Tank (168 HP, 18 damage / 2.5 s after 1.5 s, 15 gold), Runner |
| 3 Vault Gate | Combat | Grunt, Grunt |
| 4 The Deep Forge | Forge | Mend or Temper |
| 5 Sentry Hall | Elite | Grunt Captain (196 HP, 13.5 damage / 1.2 s after 0.6 s, 30 gold) |
| 6 Warden's Vault | Boss | Forge Warden (420 HP, 15 damage / 1.8 s after 1 s, enrages at 50%, 75 gold) |

| Files | Change |
|---|---|
| `Scripts/Core/RunState.cs`, `RunOutcome.cs` | Gold, secured gold and settlement on `End`: `GoldBanked`, `GoldLost`; new outcome `Extracted`; the loss fraction is validated to 0–1 and an ended run earns nothing more. |
| `Scripts/Economy/RewardService.cs` | `TryAwardKill(victim, gold)` pays experience and gold once per victim. |
| `Scripts/Combat/FloorScaling.cs` | New, plain C#: health = base × tier × (1 + modifier); damage bonus = tier × (1 + modifier) − 1, applied as a percent modifier on the enemy's runtime weapon; gold rounds away from zero. |
| `Scripts/Progression/CheckpointKind.cs`, `CheckpointOption.cs`, `CheckpointOffer.cs`, `CheckpointService.cs`, `RunChoices.cs`, `ChoiceKind.cs` | New checkpoint decision with the same stale and re-entrant guards, withdrawn when the run ends. `RunChoices` priority is upgrade, forge, then checkpoint, so the boss level-up comes first. |
| `Scripts/Content/FloorDefinition.cs`, `FloorModifierDefinition.cs`, `EnemyDefinition.cs`, `EconomyConfig.cs`; `Data/Floors/Floor_QuicksilverVaults.asset`, `Modifier_CursedGold.asset`; enemy, floor 1 and economy assets | Floors link through Next Floor with a scaling tier and an optional modifier; enemies author Gold Reward. |
| `Scripts/Combat/EncounterController.cs` | Validates the whole floor chain at startup (rejects cycles), scales every spawn, tracks the floor number and total rooms, and `DescendToNextFloor()` starts the next floor after the advance delay. |
| `Scripts/Core/CombatSetup.cs` | Floor cleared → checkpoint when a next floor exists, otherwise Victory. Extract → Extracted; Descend → secure gold, then descend. |
| `Scripts/UI/RunHud.cs`, `RunResultView.cs`, `RunChoiceView.cs`, `PrototypeTextDefinition.cs`, `Data/UI/PrototypeText.asset`, `Scenes/Gameplay/Gameplay.unity` | HUD: `Floor n \| Room n/6 \| room`, and a gold **Gold n (n at risk)** label replaces the attack counter. Result: Descent complete, Extracted or Defeated; `Floor n \| rooms \| Level \| XP`; a gold line with any loss. The scene was wired by a temporary builder, deleted afterwards. |
| Tests | EditMode `DescentTests` (11 cases), including a two-floor simulation through the real run, reward, upgrade, forge, choice and scaling code. PlayMode `DescentFlowTests` (3): descending with secured gold and scaled enemies, death on floor 2 losing half the unsecured gold, and both floors to Descent complete with 250 gold. `FloorFlowTests` now ends floor 1 at the checkpoint and extracts; `RunResultTests` and `CombatSceneTests` check the gold. |

Balance chosen with the Descent simulation (first upgrade card every time, Mend at floor 2's forge, always descending):

| Floor 1 forge | Damage first | Speed first |
|---|---|---|
| Mend | 38 HP after floor 1; clears floor 2 with 17 HP | 34 HP; clears floor 2 with 13 HP |
| Temper | 17 HP; dies in Vault Gate, floor 2 room 3 | 4 HP; dies in Mercury Stair, floor 2 room 1 |

Descending is a real bet: a healthy hero makes it, a hero who tempered on floor 1 should extract. Rejected alternatives in the same simulation: a 30% heal on descend let every path survive; health ×1.6 with the same ×1.5 damage killed the speed-first Mend path and left damage-first Mend at 2 HP.

Verified: Unity EditMode 100/100, PlayMode 24/24, `Verify-Project.ps1`, .NET CombatChecks 100/100, development APK built only after both reports passed (SHA-256 `567C672B40F18698A305AF57B0BE91639E85FA777C2DDCEAE038D6975862487D`). On Samsung SM-S911B an automated Descend run cleared both floors in about 78 s with 21 HP left and banked 250 gold (see `23`).

### Implemented 2026-09-13: the Forge meta layer (local save, Relic Forge, two relics)

What the player sees:

- **Result screen:** a gold line names the nearest unlock, for example **Forge gold 96: Second Wind is ready to forge** or **Forge gold 0: Second Wind costs 80**. A **Relic Forge** button sits under **Try again**; both wait out the half-second input delay.
- **Relic Forge:** a panel over the result screen with **Gold n | Deepest floor cleared: n**, one card per relic (name, effect, then **Forge for n gold**, **n gold: need n more**, **Owned: tap to equip** or **Equipped**) and **Start run**.
  - Tapping an affordable relic forges and equips it; tapping an owned relic equips it. One relic is equipped at a time.
  - Unaffordable cards are dimmed. Every change briefly ignores taps, so rapid taps cannot buy twice.
- **During a run:** the HUD shows **Relic: name** under the weapon line and adds a trigger count once it fires. The result build lists the relic first, for example **Build: Second Wind (1x), Tempered Edge x5**.

| Relic (`Data/Relics`) | Price | Effect |
|---|---|---|
| Second Wind | 80 | Once per run, when a hit the hero survives leaves 25% health or less, restore 25% of maximum health |
| Counterweight | 150 | Every enemy hit the hero survives strikes that enemy back for 60% of the hero's current weapon damage |

Both change behavior rather than raw stats, as `03` and `24` §4.5 require. Counterweight fits the Vanguard's shield/counter identity and makes damage upgrades worth more; Second Wind is a one-time safety net that makes a greedier forge pick or descent survivable. A floor 1 extraction (96 gold) buys Second Wind; a full Descent (250) buys both.

**Gold flow:** `RunBank` deposits gold into the profile as soon as a checkpoint secures it, and the rest of the banked gold when the run ends, so a descent keeps its secured gold even if the app closes mid-floor. Clearing a floor raises the depth record. Every profile change is saved at once.

**Save:** `profile.json` in `Application.persistentDataPath`, versioned JSON (`saveVersion` 1) with a `revision` that grows on every save.

- **Saving:** writes and flushes `profile.json.tmp`, moves the old `profile.json` to `profile.json.bak`, then moves the temp file into place.
- **Loading:** takes the readable file with the highest revision among the three. An unreadable main file is copied to `profile.json.unreadable`, and a file from a newer build is never read.
- **Repairs:** negative gold, duplicate relic ids and an equipped relic that is not owned are fixed on load.
- **When it loads:** on every Gameplay scene load, so no Bootstrap scene or persistent service object is needed yet (decision in `19`).

| Files | Change |
|---|---|
| `Scripts/Core/PlayerProfile.cs` | New, plain C#: gold, owned and equipped relic, deepest floor cleared; forging spends, owns and equips as one change; damaged saved values are repaired. |
| `Scripts/Economy/RunBank.cs` | New: deposits secured gold at the checkpoint and the remainder when the run ends. |
| `Scripts/Progression/RelicEffect.cs`, `RelicOption.cs`, `RelicRuntime.cs`, `RelicStatus.cs`, `RelicShop.cs` | New: relic data with validation; one run's relic reacting to hits the hero survives (a killing blow never triggers it); the Relic Forge rules (status, next unlock, forge or equip on tap). |
| `Scripts/Combat/IHealable.cs`, `RelicBehaviour.cs` | `IHealable` exposes `Current`. `RelicBehaviour` on the Vanguard reports each drop in hero health with the current enemy as the attacker. |
| `Scripts/Save/ProfileSaveData.cs`, `ProfileMigration.cs`, `ProfileLoadStatus.cs`, `ProfileStore.cs`, `ProfileLocation.cs` | New: schema, version gate, atomic save and newest-readable load, and a test-only folder override. |
| `Scripts/Content/RelicDefinition.cs`, `Data/Relics/Relic_SecondWind.asset`, `Relic_Counterweight.asset` | Relic definitions with price, effect, amount and threshold. |
| `Scripts/Core/CombatSetup.cs` | Loads the profile, logs recovered or reset saves, composes `RelicShop` and `RunBank`, saves on every profile change (a failed write is logged and retried on the next change), applies the equipped relic and records cleared floors. |
| `Scripts/UI/RunHud.cs`, `RunResultView.cs`, `RelicForgeView.cs`, `PrototypeTextDefinition.cs`, `Data/UI/PrototypeText.asset`, `Scenes/Gameplay/Gameplay.unity` | Relic HUD line, forge hint and Relic Forge button on the result screen, the Relic Forge panel. The scene was wired by a temporary builder, deleted afterwards. |
| Tests | EditMode `RelicForgeTests` (12 cases), `ProfileStoreTests` (6: first launch, round trip, corrupt main file, interrupted saves, newer version, repaired values) and a relic case in the `DescentTests` simulation. PlayMode `RelicForgeFlowTests` (4): the locked Forge with an empty profile; forging, re-equipping, saving and starting a run with the relic; Counterweight answering Grunt strikes; Second Wind healing once. Every scene test now uses its own temporary profile folder; `FloorFlowTests`, `DescentFlowTests` and `RunResultTests` check the saved gold and forge hint. |

Balance chosen with the Descent simulation (first upgrade card every time, Mend at floor 2's forge, always descending; health after floor 1 → end of floor 2):

| Floor 1 path | No relic | Counterweight 60% | Second Wind 25%/25% |
|---|---|---|---|
| Damage first, Mend | 38 → 17 | 60 → 39 | 38 → 42 |
| Damage first, Temper | 17 → dies in floor 2 room 3 | 39 → 18 | 42 → 21 |
| Speed first, Mend | 34 → 13 | 42 → 21 | 34 → 38 |
| Speed first, Temper | 4 → dies in floor 2 room 1 | 12 → dies in floor 2 room 1 | 29 → dies in floor 2 room 3 |

Each relic rescues the damage-first Temper descent but not the greediest path. Rejected in the same simulation:

- Counterweight at 50% rescued no path.
- Second Wind once per floor let every path survive, including speed first with Temper.

Known gaps:

- After both relics (230 gold) gold has no sink; weapon unlocks from the next slice are the planned sink.
- The analytics events `currency_spent` and `meta_upgrade` wait for an analytics service.
- There is no in-game progress reset.
- Closing the app mid-floor forfeits gold not yet secured.

Verified: Unity EditMode 119/119, PlayMode 28/28, `Verify-Project.ps1`, .NET CombatChecks 113/113, development APK built only after both reports passed (SHA-256 `32523A4D00FCC7142DBB59361FCEEB9208769A9B29C24F9BE553EE9BBE05628E`).

### Implemented 2026-09-13: pause control

Owner feedback from the Relic Forge device run: a run starts at once and there is no way to pause it.

- **Pause button:** a **II** button in the HUD's top-right corner pauses the run. Combat, the advance delays and all timers freeze, and a **Paused** overlay (**Combat waits until you resume**) offers **Resume**. Resume ignores taps for 0.3 s after the overlay appears.
- **Leaving the app:** Home, a call or switching apps pauses the run too, so returning shows **Paused** instead of combat carrying on. A choice panel that is already open stays in charge: it already waits, so no second overlay appears.
- **When the button shows:** only during live combat; it is hidden while a choice is open, while paused and after the run ends.

| Files | Change |
|---|---|
| `Scripts/Core/RunPause.cs` | New, plain C#: the single owner of "is the run frozen?". An open choice and the player's pause each hold the run; pausing is refused during a choice or after the run ends, and the end releases a pause. |
| `Scripts/Core/CombatSetup.cs` | Applies `RunPause` to `Time.timeScale` (the choice handler no longer sets it directly), reports choice state to it, and pauses on `OnApplicationPause(true)`. |
| `Scripts/UI/PauseView.cs`, `PrototypeTextDefinition.cs`, `Data/UI/PrototypeText.asset`, `Scenes/Gameplay/Gameplay.unity` | HUD pause button and a **Pause Canvas** (sorting order 30, above choices and results) with the overlay. The scene was wired by a temporary builder, deleted afterwards. |
| Tests | EditMode `RunPauseTests` (4). PlayMode `PauseFlowTests` (4): the button freezes combat until Resume; leaving the app pauses the run while an open choice stays in charge; the button hides when the run ends; every button's canvas can receive touches. |

Verified first: Unity EditMode 123/123, PlayMode 31/31, `Verify-Project.ps1`, .NET CombatChecks 117/117, development APK built only after both reports passed (SHA-256 `07CDD30A40ADDD2DD1D0FAE38923DE995B043379E2368CA91D336AD6E3DCFC9B`).

**Device finding and fix:** on the phone the pause button ignored real touches.
- **Cause:** the HUD canvas had never needed input, so it had no `GraphicRaycaster`. The flow tests click buttons with `ExecuteEvents` directly and therefore bypass raycasting.
- **Fix:** a temporary builder added the raycaster (deleted afterwards).
- **Guard:** `PauseFlowTests.EveryButtonSitsOnACanvasThatReceivesTouches` now checks that every button's root canvas has one. Run against the unfixed scene it failed with **Pause Button is on HUD Canvas, which has no GraphicRaycaster**.

Verified after the fix: Unity EditMode 123/123, PlayMode 32/32, `Verify-Project.ps1`, development APK built only after both reports passed (SHA-256 `15A602FB9EA45855429BF319088453A2C11E650050E7775EC43AD863290E80C5`). On the phone the button now pauses the run, and leaving the app pauses it too (see `23`).

**Second device finding:** the **II** label floated above its 120-unit button. It had been copied from the 260-unit **Try again** label, which is anchored to the bottom with a fixed height; the Relic Forge button label was 30 units high for the same reason. Both labels now stretch to fill their buttons. `PauseFlowTests.EveryButtonLabelSitsInsideItsButton` guards every button; run against the scene before this fix it failed with **Forge Button Label extends outside Forge Button**.

Verified after both fixes: Unity EditMode 123/123, PlayMode 33/33, `Verify-Project.ps1`, development APK built only after both reports passed (SHA-256 `0F8D8EB6E8F2821C2426774A215031285650C8D05BD8EBD9E9B34D0D62BBD6AD`). The corrected label was later captured on the phone (see `23`).

### Implemented 2026-09-13: packs, the Cinder Mite and the Sword's cleave (weapon behaviors, part 1)

The GDD's weapon set (Sword cleave, Bow pierce, Staff AoE) only matters against several enemies at once, so the owner chose to add packs and three weapons together. This first part adds packs and the Sword's cleave; Staff, Daggers and weapon unlocks follow.

- **Packs:** a wave now spawns one to three enemies in a row in front of the hero, centre first, then left and right, 1.7 units apart. Targeting breaks distance ties in favour of the earlier slot, so the hero always fights the first living enemy in slot order. The HUD bar follows that enemy, and the label counts the rest, for example **Cinder Mite | 15 / 15 HP  (+2 more)**. A wave clears when its last enemy falls: **3 enemies defeated in 6 hits, 2.4s**.
- **Cinder Mite:** a new fodder enemy, a small ember-red variant of the Grunt prefab. It has 15 HP, deals 1 damage every 1.5 s, and gives 1 gold and 3 XP. The named enemies keep their stats and now give 10 XP each.
- **Sword cleave:** each swing also strikes the nearest other living enemy within 2 units of the target, for 60% of the damage.
- **Hits carry their attacker:** `DamageContext.Source` is set by `WeaponRuntime`, and health raises `Damaged` before `Changed`. Counterweight strikes back at the actual attacker in a pack, and a defeat names the last enemy whose hit landed.
- **Experience:** XP per kill moved from `EconomyConfig` onto each enemy. Each level now costs one more than the last (10, 11, 12, ...; `_experienceGrowth` 1), so frequent small kills do not use up the ten upgrade stacks on floor 1.

| Floor | Room | Waves |
|---|---|---|
| Ember Halls | Ember Hall | Grunt · 2 Mites · 3 Mites |
| | Cinder Walk | Runner + Mite · 3 Mites · Grunt + 2 Mites |
| | Slag Gate | Tank · 3 Mites |
| | Captain's Post | Grunt Captain + 2 Mites |
| | Warden's Crucible | Forge Warden |
| Quicksilver Vaults | Mercury Stair | Grunt + Mite · 3 Mites |
| | Cold Crucible | Tank + Mite · Runner + 2 Mites |
| | Vault Gate | Grunt + 2 Mites |
| | Sentry Hall | Grunt Captain + Mite |
| | Warden's Vault | Forge Warden |

Ember Halls now pays 109 gold (a floor 1 extraction still buys Second Wind) and a full Descent 270.

| Files | Change |
|---|---|
| `Scripts/Combat/PackLayout.cs`, `WeaponBehavior.cs` | New: pack slot offsets; DirectHit and Cleave. |
| `Scripts/Combat/WeaponRuntime.cs`, `AttackController.cs`, `Targeting.cs` | Cleave with splash radius and fraction; attacks pass their owner as the source and gather splash candidates, nearest first, only when ready; earlier candidates win distance ties. |
| `Scripts/Combat/DamageContext.cs`, `HealthState.cs`, `Health.cs`, `RelicBehaviour.cs` | Damage source and the `Damaged` event; the relic reacts to the actual attacker. |
| `Scripts/Combat/EncounterController.cs`, `Scripts/Content/WaveDefinition.cs`, `RoomDefinition.cs` | Waves of one to three enemies, per-enemy gold, `WaveEnemyAt`, `DefinitionOf`, `GoldRewardOf`, validation of pack sizes. |
| `Scripts/Core/RunState.cs`, `CombatSetup.cs`, `Scripts/Economy/RewardService.cs`, `Scripts/Content/EnemyDefinition.cs`, `EconomyConfig.cs`, `WeaponDefinition.cs` | Experience growth; per-enemy experience and gold; the last attacker; weapon behavior data. |
| `Scripts/UI/RunHud.cs`, `RunResultView.cs`, `PrototypeTextDefinition.cs`, `Data/UI/PrototypeText.asset` | Pack health line and pack victory status; the defeat names the last attacker. |
| `Data/Enemies/Enemy_CinderMite.asset`, `Data/Weapons/Weapon_MiteBite.asset`, `Prefabs/Enemies/CinderMite.prefab`, both floors, every enemy and weapon asset, `PrototypeEconomy.asset`, `Gameplay.unity` | Content and data; the prefab variant and scene re-save came from a temporary builder, deleted afterwards. |
| Tests | New `DescentSimulation` shared by `FloorTests` and `DescentTests`: packs in slot order, cleave, enrage and relics through the real services. New `PackTests` (6). PlayMode tests describe waves by what spawned; the hero's own cleaves may finish enemies in any order. New `FloorFlowTests.PacksSpawnSideBySideAndTheHeroFightsThemInSlotOrder`. |

Balance was chosen with a scored search over mite stats, experience growth, cleave strength and floor 2 packs. A single-target Sword against these packs died in room 2 on every path. Results (first upgrade card every time, Mend on floor 2, always descending; health after floor 1 → end of floor 2):

| Path | No relic | Counterweight | Second Wind |
|---|---|---|---|
| Damage first, Mend | 54 → 40 | 75 → 61 | — |
| Damage first, Temper | 24 → dies in floor 2 room 3 | 35 → 21 | 49 → 35 |
| Speed first, Mend | 33 → 13 | — | — |
| Speed first, Temper | 12 → dies in floor 2 room 1 | 19 → dies in floor 2 room 3 | 37 → 20 |

- **Level-ups:** 8 upgrades by the end of floor 1, and the tenth on floor 2.
- **Counterweight:** cuts the damage-first Mend fight time from 22 s to 15 s.
- **Second Wind:** now also carries the greediest path.
- **Pacing:** simulated fight time for floor 1 is still only about 16 s, so pacing remains the open gap; longer rooms and more waves are the lever.

Verified: Unity EditMode 129/129, PlayMode 34/34, `Verify-Project.ps1`, .NET CombatChecks 123/123, development APK built only after both reports passed (SHA-256 `AA38FA1DD344F8E50E0567397F60362DD0BF813F48983302FD3ECF13EE0D1863`). Not captured on the phone before part 2.

### Implemented 2026-09-13: Staff, Daggers and weapon unlocks (weapon behaviors, part 2)

The second half of the owner's choice: two weapons bought with gold in the Relic Forge, each with its own behavior.

- **Staff (120 gold):** area damage. Every 0.9 s it deals 10 damage to its target and to every other living enemy within 3.5 units, which covers a whole pack. It clears mite packs fastest and is the slowest weapon against lone enemies.
- **Daggers (180 gold):** fast strikes with crits. They deal 6 damage every 0.55 s to one enemy, and every third strike is critical for double damage. They are slowest against packs and fastest against the Wardens.
- **Crit rhythm:** crits follow a fixed rhythm (every Nth attack) instead of a chance, so fights stay readable and the simulation stays exact. A critical hit, splash included, carries `DamageContext.IsCritical`, and the struck body flashes gold instead of white.
- **Carrying a weapon:** the Relic Forge now shows **Weapons: carry one** (Sword, Staff, Daggers) above **Relics: equip one**. Forging a weapon spends its price once and equips it; an owned weapon is equipped again for free. The Sword is the starting weapon: it is always owned and is never stored in the save. The next run creates the hero's weapon from the equipped definition. The HUD, the result build (**Build: Staff  |  ...**) and the hero's placeholder loadout (sword and shield, a staff with a violet orb, or two blades) follow it.
- **Nearest unlock:** the result hint names the cheapest unowned relic or weapon; a relic wins a tie, and the final hint reads **everything is forged**.
- **Save version 2:** `profile.json` gains `ownedWeaponIds` and `equippedWeaponId`. `ProfileMigration` upgrades a version 1 file in place: no weapons, the starting weapon carried. This is the first real migration.

| Files | Change |
|---|---|
| `Scripts/Combat/AttackPattern.cs` (new), `WeaponBehavior.cs`, `WeaponRuntime.cs`, `AttackController.cs`, `DamageContext.cs` | The behavior, splash and crit rhythm move into one validated `AttackPattern` value passed to `WeaponRuntime`; new `Area` behavior; `AttacksMade` counts the rhythm; `IsCritical` on hits. |
| `Scripts/Content/WeaponDefinition.cs`, `Data/Weapons/Weapon_Staff.asset`, `Weapon_Daggers.asset` (new), every weapon asset | Crit rhythm fields, a Forge description format and a Forge price. Enemy weapons keep zeros and are never sold. |
| `Scripts/Core/PlayerProfile.cs`, `Scripts/Save/ProfileSaveData.cs`, `ProfileMigration.cs`, `ProfileStore.cs` | Owned and equipped weapons alongside relics; schema version 2 with the version 1 step. |
| `Scripts/Progression/WeaponOption.cs`, `WeaponShop.cs` (new), `UnlockStatus.cs` (was `RelicStatus.cs`), `RelicShop.cs` | The weapon rack's rules: the starting weapon is always owned and is the fallback for a missing or unowned saved weapon; card states shared with relics. |
| `Scripts/Core/CombatSetup.cs`, `Scripts/UI/RelicForgeView.cs`, `HeroWeaponView.cs` (new), `CombatantView.cs`, `RunHud.cs`, `RunResultView.cs`, `PrototypeTextDefinition.cs`, `Data/UI/PrototypeText.asset` | The Forge catalog on Combat Setup, `HeroWeapon` for the run, weapon cards and section headers, placeholder loadouts, the gold crit flash, the weapon in the HUD and result build, and the nearest unlock across both shops. |
| `Scenes/Gameplay/Gameplay.unity` | A temporary builder, deleted afterwards, added the loadouts under **Hero Body** and three weapon cards and two headers to the Forge panel, and re-laid out all cards at 220 units so everything fits a 1920-unit canvas. |
| Tests | EditMode `WeaponBehaviorTests` (6): area splash, the crit rhythm and a critical splash, crit validation, weapon identity and sidegrade balance in the Descent simulation. `WeaponForgeTests` (4): profile slots, the weapon rack, catalog validation, the version 1 migration. `ProfileStoreTests` adds a version 1 file loading and saving as version 2. PlayMode `RelicForgeFlowTests` adds forging the Staff into the next run, Daggers crits flashing gold, and a layout check that the Forge panel is opaque and fits 1920 units without overlaps. |

Values were chosen with a search over damage, interval, area fraction and crit rhythm. The hard targets were the relic slice's: Mend descents survive, Temper descents fall on floor 2 without a relic, and both relics rescue damage-first Temper. On top of that, no weapon may be a straight upgrade: Mend-path health stays within 15 of the Sword's, the Staff must clear mite packs faster, and the Daggers must kill the Wardens faster. The Staff ends up the weakest against a single target (11.1 damage per second against the Sword's 12.5) and the slowest against the Wardens.

Results with the 1-second advance delay between waves (health after floor 1 → end of floor 2; D = damage first, S = speed first; boss and mite-pack times are the simulated fight seconds on the D + Mend path):

| Weapon | D + Mend | S + Mend | D + Temper | S + Temper | D + Temper + Counterweight | D + Temper + Second Wind | S + Temper + Counterweight | S + Temper + Second Wind | Wardens | Mite packs |
|---|---|---|---|---|---|---|---|---|---|---|
| Sword | 54 → 40 | 33 → 13 | 24 → dies F2 room 3 | 12 → dies F2 room 1 | 35 → 21 | 49 → 35 | 19 → dies F2 room 3 | 37 → 20 | 5.8 s | 4.4 s |
| Staff | 63 → 38.5 | 34 → 9.5 | 23 → dies at the F2 Warden | 3 → dies F2 room 1 | 43 → 18.5 | 48 → 23.5 | 11 → dies F2 room 2 | 28 → 3.5 | 6.5 s | 0 s |
| Daggers | 60 → 37 | 44 → 21 | 31 → dies F2 room 3 | 6 → dies F2 room 1 | 44 → 21 | 31 → 33 | 24 → dies F2 room 3 | 31 → dies F2 room 3 | 3.6 s | 5.8 s |

- **Staff:** after one Tempered Edge a single blast kills a mite pack, so mite waves cost it no time and floor 1 fights shrink to about 14 s. Its small hits keep Counterweight's counters small; only Second Wind carries speed-first Temper, with 3.5 HP left.
- **Daggers:** Counterweight answers with 60% of a 6-damage hit, and neither relic carries speed-first Temper.

Known gaps:
- Crit upgrades and a crit chance wait for the upgrade pool to grow.
- Weapon placeholders are flat shapes; a visible area blast or crit number would read better.
- Mid-run weapon swaps do not exist by design.

The first Unity run failed two of the new PlayMode tests: this Editor formats decimals in Turkish, so the HUD read **0,80s**, not **0.80s**. The HUD follows the device language on purpose, so the tests now format expected numbers in the current culture.

Verified (commit cc1a217, first Staff tuning): Unity EditMode 140/140, PlayMode 37/37, `Verify-Project.ps1`, .NET CombatChecks 133/133, development APK built only after both reports passed (SHA-256 `8558B44BFB50FA307AA417882F646A6E4DF0B94D87F4EEE6AC8B51D14CAF48C5`).

**Device run and fixes (see `23`):**
- **Staff health gap:** the first Staff tuning (18 damage every 1.2 s, 75% to the rest of the pack) ended floor 1 at 38 HP on the phone, against 52 in the simulation; the Sword matched (56 against 54). The simulation had readied the hero's weapon at every wave. In the scene the weapon only cools down during the 1-second advance delay, so a weapon slower than that delay starts each wave late.
- **Simulation fix:** `DescentSimulation` now ticks the advance delay between waves. The Sword and Daggers results are unchanged. The first Staff tuning then showed 36 HP after floor 1 and died on floor 1 on the damage-first Temper path.
- **Staff retune:** a repeated search found 8 of 540 candidates meeting every target. The Staff became 10 damage every 0.9 s, with full damage to everything in reach (the values above).
- **Forge backdrop:** the result screen's build line showed through the 92% Relic Forge backdrop, between the weapon and relic cards. The backdrop is now opaque, and `ForgePanelIsOpaqueAndFitsTheShortestPortraitScreenWithoutOverlaps` checks it.

Verified after the fixes: Unity EditMode 140/140, PlayMode 37/37, `Verify-Project.ps1`, .NET CombatChecks 133/133, development APK built only after both reports passed (SHA-256 `5949CEDE5117510EFA1BBA815FD18AF497BFAD6C5AC73AB7A8AADA7C1FF147FB`).

### Implemented 2026-09-14: scene and simulation parity tests

The Staff gap showed that the balance simulation can drift from the scene without any test failing. `SimulationParityTests` (PlayMode) now play whole Descents in the real scene the way `DescentSimulation` plays them, and compare the results.
- **Choices:** the same upgrade card every time, Mend or Temper at the first forge, Mend afterwards, Descend at the checkpoint.
- **Time:** `Time.captureDeltaTime` fixes game time at 1/60 s per frame, like the simulation, and the frame-rate cap is lifted, so a two-floor Descent takes about a second of real time.
- **Cases:** each weapon on the damage-first Mend path; the Sword's speed-first Temper death; Counterweight and Second Wind carrying damage-first Temper; the Staff dying on floor 2 with Counterweight.
- **Result:** all seven match exactly: outcome, floor, death room, kills, gold banked and lost, upgrades applied, relic triggers, and health after floor 1 and at the end. The tolerance is 1 HP.

To make this possible, `DescentSimulation` and the relic mirrors moved into `Tests/Support` (`Project.TestSupport`, compiled only with the test framework). Both test assemblies reference it; the Android build does not include it.

Verified: Unity EditMode 140/140, PlayMode 44/44, `Verify-Project.ps1`, .NET CombatChecks 133/133, development APK built only after both reports passed (SHA-256 `C06E064B7F24BE0F8601B01860D491AC35739FD8A25D9B701B365F7AC0266495`; no game code changed since the build installed on the phone).

### Implemented 2026-09-14: Forge catalog checks at startup

A code review of the packs and weapon commits found one robustness gap. A bad Forge entry threw an exception halfway through `CombatSetup.Awake`, after the profile loaded but before the run existed. Examples: a weapon listed twice, a description format with an unknown placeholder, or crit data the weapon runtime rejects. Every view then logged its own missing-reference error, and nothing named the bad asset.

`Content/ForgeCatalog` now builds the relic and weapon options before the profile loads. It checks every entry: empty slots, duplicate ids, relic amounts, description formats, and each weapon's combat data (by creating a runtime, so a broken Daggers asset fails even while the Sword is carried). Combat Setup logs one error naming the asset and disables itself, as it already did for missing references. EditMode `ForgeCatalogTests` (3) cover each case.

Verified: Unity EditMode 143/143, PlayMode 44/44, `Verify-Project.ps1`, .NET CombatChecks 133/133, development APK built only after both reports passed (SHA-256 `8AF1C71DF37E3703A9B21A3DA55BD281DFDB6EE3F5F132DDDB4B6C735B1EAA6F`).

### Implemented 2026-09-14: truncated save files

`13` and `18` rate save corruption as critical. Two EditMode tests now cut a profile file at every character and reload it:
- **Temp file:** a save killed mid-write can leave a partial temp file with a higher revision. At every cut length the last complete save still loads (70 gold, the equipped Staff), and only the complete newer file wins.
- **Main file:** storage damage can cut `profile.json` itself. At every cut length the backup loads instead.

Unity's JSON parser rejected every partial file, so no change to `ProfileStore` was needed; the tests keep it that way. Verified: Unity EditMode 145/145 (no game code changed).

### Implemented 2026-09-14: the arena floor and walking packs (isometric arena, part 1)

The first slice of the astral foundry direction in `19`. The owner chose enemies that walk in toward the hero over enemies that appear in place. This part moves combat onto a two-dimensional floor and rebalances the Descent; the floating platform, backdrop and camera follow in part 2.

- **Arena floor:** every position is a point on a flat floor with the hero at the origin. The screen shows depth at half length (`ArenaFloor.DepthScale` 0.5), the way an isometric view foreshortens it. Reach, cleave and area radii are floor distances, so an enemy straight ahead is as far away as one to the side. The Vanguard now stands at the world origin, and the camera moved up to keep it framed.
- **Formations:** a wave holds up to seven enemies. `PackLayout` fills the front row first and puts later slots behind and to the sides, never more than 2 floor units off the centre line.
- **Walking in:** a wave enters 6 floor units from the hero (`_entryDepth`). `PackMotion` walks each enemy toward its own point on an arc just inside its weapon's reach (0.1 inside, so rounding never leaves it on the edge). Enemies from the centre slots come straight on; the widest slots approach from 60° off the centre line. Enemies step nearest first and wait whenever a step would bring them closer than 0.9 units (`_bodySpacing`) to a nearer enemy, so the pack queues instead of stacking.
- **Combat order:** the hero strikes the nearest living enemy within its reach, the earlier slot on equal distance; an enemy strikes once the hero is within its own reach. Nothing walks on the frame a wave spawns, while time is frozen, or after the hero falls. `EncounterController` runs before other scripts, so enemies move before anyone attacks, the same order the Descent simulation uses.
- **Victory names the boss:** Warden's Vault now holds two mites with the Warden, and the result named whichever enemy fell last. It now names the final wave's toughest enemy.

| Enemy | Speed (units/s) | Reach | Damage |
|---|---|---|---|
| Grunt | 1.6 | 1.2 | 3.6 every 1 s |
| Runner | 3 | 1.2 | 1.2 every 0.4 s |
| Tank | 0.9 | 1.4 | 7.2 every 2.5 s |
| Grunt Captain | 1.4 | 1.3 | 5.4 every 1.2 s |
| Forge Warden | 1 | 1.4 | 6 every 1.8 s |
| Cinder Mite | 2.2 | 1 | 0.6 every 1.5 s |

| Floor | Room | Waves |
|---|---|---|
| Ember Halls | Ember Hall | Grunt + Mite · 3 Mites · Grunt + 3 Mites |
| | Cinder Walk | Runner + 2 Mites · 4 Mites · Tank + 2 Mites |
| | Slag Gate | Grunt + Runner + 2 Mites · Tank + 2 Mites |
| | Captain's Post | Grunt Captain + Grunt + 2 Mites |
| | Warden's Crucible | Forge Warden |
| Quicksilver Vaults | Mercury Stair | Grunt + 3 Mites · Runner + 3 Mites |
| | Cold Crucible | Tank + 3 Mites · 5 Mites |
| | Vault Gate | 2 Grunts + 3 Mites |
| | Sentry Hall | Grunt Captain + Grunt + 2 Mites |
| | Warden's Vault | Forge Warden + 2 Mites |

**Rebalance.** With walking packs the old values failed four balance tests. The Staff's 3.2 reach blasted packs well before they arrived, so it outclassed the other weapons. A flat +5 Tempered Edge added 83% to the Daggers' 6 damage but 50% to the other weapons' 10, and the search found no Daggers tuning around that. The new values:
- **Enemies:** every enemy weapon deals 60% of its old damage; floor 2 hits 1.4× instead of 1.25× before Cursed Gold.
- **Cinder Mites:** 1 XP and no gold instead of 3 XP and 1 gold, and each level costs two more than the last (10, 12, 14, ...; `_experienceGrowth` 2), so the larger packs do not use up the ten upgrade stacks early.
- **Tempered Edge:** +50% damage instead of +5. That is the same for 10-damage weapons and proportional for the Daggers.
- **Weapons:** the Sword reaches 1.8; the Staff deals 11 every 1.1 s from 2.1 units away and 75% of that to the rest of the pack within 3.5 units; the Daggers strike every 0.45 s at 1.5 units.
- **Warden:** reach 1.4 instead of 1.5. It stopped exactly at 1.4, one of the Daggers reaches the search tried, where float rounding would decide whether they connect.

The search kept the hard targets of the relic and weapon slices (Mend descents survive; Temper descents fall on floor 2 without a relic; both relics rescue damage-first Temper; every weapon within 15 HP of the Sword on the Mend paths; the Staff fastest on mite packs and the Daggers fastest on the Wardens). Small changes flipped outcomes, so it preferred candidates whose one-step neighbours also met every target.

Ember Halls pays 116 gold and a full Descent 273, because only named enemies pay. A Descent now has 60 kills and about 83 s of simulated fighting with the Sword (54 s on floor 1, up from about 16 s), with 7 upgrades by the end of floor 1 and all 10 on floor 2.

| Weapon | D + Mend | S + Mend | D + Temper | S + Temper | D + Temper + Counterweight | D + Temper + Second Wind | S + Temper + Counterweight | S + Temper + Second Wind | Wardens | Mite packs |
|---|---|---|---|---|---|---|---|---|---|---|
| Sword | 46 → 37 | 45 → 23 | 12 → dies F2 room 2 | 11 → dies F2 room 2 | 33 → 30 | 37 → 34 | 27 → 18 | 36 → 26 | 15.2 s | 9.1 s |
| Staff | 46 → 42 | 41 → 34 | 12 → dies F2 room 2 | 7 → dies F2 room 2 | 40 → 37 | 37 → 33 | 23 → 16 | 32 → 25 | 16.2 s | 6.5 s |
| Daggers | 55 → 38 | 51 → 33 | 21 → dies F2 room 3 | 18 → dies F2 room 2 | 35 → 20 | 46 → 29 | 29 → dies F2 room 3 | 43 → 24 | 13.9 s | 10.4 s |

(Health after floor 1 → end of floor 2; D = damage first, S = speed first; Warden and mite-pack times are simulated fight seconds on the D + Mend path.)

- **Counterweight now carries speed-first Temper** for the Sword and the Staff. The relic decision in `19` wanted that path to fail; it still fails for the Daggers. Counterweight still favours damage: on the Sword's Mend paths it adds 16 HP to damage first and 5 to speed first. `DescentTests` now checks that difference, and that Second Wind carries speed-first Temper.
- **Parity:** the Staff's speed-first Temper death with Counterweight became a victory, so that parity case now plays the Daggers' death on the same path. All seven cases match the simulation exactly.

| Files | Change |
|---|---|
| `Scripts/Combat/ArenaFloor.cs`, `PackMotion.cs` (new), `PackLayout.cs`, `Targeting.cs` | Floor distances; walking in; two-dimensional slots for up to seven enemies; targeting and splash by floor distance. |
| `Scripts/Combat/EncounterController.cs`, `Scripts/Content/EnemyDefinition.cs`, `Scripts/UI/RunResultView.cs` | Entry depth, formation and body spacing; moving the pack each frame; move speed per enemy; the toughest enemy of the wave names a victory. |
| Both floors, every enemy and weapon asset, `Enemy_CinderMite.asset`, `Upgrade_Damage.asset`, `PrototypeEconomy.asset`, `Gameplay.unity` | Packs, speeds, reaches and the rebalance; the hero at the origin, the camera and the encounter's arena settings. |
| Tests | EditMode `PackMotionTests` (5): walking at speed and stopping inside reach, approach angles, waiting behind a nearer enemy, validation, floor distances from world positions matching floor coordinates exactly. `PackTests` checks the slots and plays the Sword's cleave on mites that walked in. `DescentSimulation` walks packs through `PackMotion` with the new data. PlayMode tests follow walking packs: formation positions at spawn and mirrored paths, the first pack falling to six swings for 11 XP, enemies striking only in reach, pause and a dead hero freezing movement, Counterweight answering whichever enemy strikes, Staff splash on the Grunt beside the mite, Daggers crits across the mite and the Grunt, and the victory naming the Warden although its mites fell last. |

Before the Unity runs, a frame-by-frame replay of single waves with the simulation's classes predicted the scene counts the new PlayMode tests assert (six swings and eight hits for the first pack; three swings for three mites after Tempered Edge, four after Quickened Grip). All held on the first run.

Verified: Unity EditMode 150/150, PlayMode 44/44, `Verify-Project.ps1`, .NET CombatChecks 138/138. The development APK is built after part 2.

### Implemented 2026-09-14: the floating platform and camera framing (isometric arena, part 2)

The placeholder look of the astral foundry arena. No image or art asset is added; final art replaces one component.

- **Arena view:** `ArenaView` builds one vertex-coloured mesh at startup and draws it with the built-in Sprites-Default material.
  - **Void:** a violet gradient, four nebula glows, 220 stars with a few sparkles, and six small floating rocks.
  - **Platform top:** a rhombus on the arena floor, from 3 floor units behind the hero to 14 in front and 3.3 to either side. It has a brass rim, 8 × 8 stone tiles that darken toward the far corner under the top HUD, a brass inlay at the centre, and ember lanterns at the near and side corners.
  - **Below the top:** the near faces with seams and ember slits, and a keel with ember cracks and a glowing seam.
- **Depth:** the camera sorts sprites along the screen's vertical axis (`TransparencySortMode.CustomAxis`). An enemy higher on the screen stands farther away on the floor, so it is drawn behind nearer enemies and the hero. The arena mesh draws at sorting order −20, below every sprite.
- **Framing:** `ArenaCameraFraming` fits the arena into the rows between the HUD's enemy bar and hero label, and frames again when the screen size or safe area changes. The fitted area runs from 0.7 below the hero to 4.5 above it (beyond the deepest pack slot) and 2.6 to either side.
  - **1080 × 2340 phone:** the widest pack slots set the zoom (orthographic size 5.63, down from 6).
  - **1080 × 1920:** the free band sets it (5.96). Part 1's fixed camera left the hero's feet 28 rows inside the bottom HUD block at this size.

Why a rhombus, not a 2:1 isometric diamond: a square platform turned 45° with the same near corner that holds every pack slot would be 13.6 units wide, 2.6 times the phone's visible width. Stretching the rhombus along the floor's depth keeps the mockup's tall diamond silhouette and holds every slot.

| Files | Change |
|---|---|
| `Scripts/UI/ArenaView.cs`, `ArenaCameraFraming.cs`, `ArenaFraming.cs` (new) | The placeholder arena mesh and depth sorting; framing between the HUD blocks; the framing arithmetic as a pure function. |
| `Scenes/Gameplay/Gameplay.unity` | A temporary builder, deleted afterwards, added the **Arena** object and the framing component on **Main Camera**, and set the camera's clear colour to the void's. |
| Tests | EditMode `ArenaFramingTests` (4): the pack width setting the zoom on a tall phone with the arena centred in the free band, a shorter screen zooming out to fill the band, the whole-screen fallback when the HUD leaves too little, validation. PlayMode `ArenaViewTests` (3): depth sorting with the arena behind every sprite, every slot of every pack size entering on the platform, and on a 1080 × 2340 phone the hero's feet and every slot fitting between the HUD blocks and inside the screen width. |

Before the tests, a temporary Editor script (not committed) rendered the scene with two walking packs at 1080 × 2340 and 1080 × 1920. After the first render the floating rocks were shrunk, because they competed with the platform, and ember cracks were added to the keel.

Placeholder limits: combatants are still flat shapes; both floors share one platform, although `19` gives Quicksilver Vaults cool silver tones; nothing in the void moves.

Verified: Unity EditMode 154/154, PlayMode 47/47, `Verify-Project.ps1`, .NET CombatChecks 138/138, development APK built only after both reports passed (SHA-256 `A95A3A66D91892792DCA8DC801AC8C76E2072620E69536C4B6B9C6FE4CCF481A`).

### Implemented 2026-09-14: pixel-art placeholders for the astral foundry (isometric arena, part 3)

The owner asked for the arena to reach the look of `ArtDirection/2026-09-14/mockups/cosmic-v1.png`, then the characters and the combat effects. Everything is still placeholder art, now **pixel art generated from code**: no image file enters `Assets`, and final art (doc `08`) replaces single components later.

- **Pixel canvas:** `Cryptforge.Art` is engine-independent and compiled by the .NET checks: `Rgba`, `PixelCanvas` (rects, ellipses, triangles, lines, dither, string pixel maps, outline, silhouette), `PixelNoise` (deterministic hash noise, so the art is identical on every run), `PixelPalette` (sampled from the mockup), `ArenaGeometry`. `PixelSpriteFactory` turns a canvas into a point-filtered sprite at **32 texels per world unit**; every sprite is drawn at its final size, never scaled.
- **Void backdrop** (`VoidArt`, `VoidLayout`, `VoidBackdropView`): a violet gradient sky mesh with nebula glows, two star layers, nine twinkling sparkles, a spiral galaxy, three haze clouds, four floating rock islands with lantern flames, orbital rings and hanging chains, and eight pieces of drifting rubble. Islands drift ±0.05 units over 8 s and flames flicker; pausing freezes it. Everything stays outside the platform and lower in contrast than the combat.
- **Forge platform** (`PlatformLayout`, `PlatformArt`, `ForgePlatformView`): one 217×403 texel sprite for the whole platform: 8×8 two-tone stone tiles with grout, a brass rim, brass lines corner to corner with a nested-diamond emblem where they cross, corner towers with lanterns, stacked stone faces with lava seams, a keel narrowing to a lava crystal, and chains. A second overlay sprite flickers the flames, seams and crystal on a 2-frame swap and breathes its colour.
- **Hero** (`HeroArt`, `HeroLookView`): the Vanguard as a 32×44 blue-steel knight seen from behind with a cape, three body frames (two idle breaths, an attack), and the weapons of the carried loadout drawn as their own sprites: sword and shield, staff with a violet orb, two daggers. The existing loadout objects keep their names, so `HeroWeaponView` is unchanged. On each attack the sword sweeps ±40° about its grip, the daggers thrust and the orb lights.
- **Enemies** (`EnemyArt`, `EnemyLookView`): six looks with a 1-texel outline, feet on the pivot and a translucent ground shadow — Grunt (slag brute, 31×30), Runner (21×32), Tank (45×36), Grunt Captain (brass pauldron, 35×40), Forge Warden (quicksilver golem with one violet visor line, 49×56), Cinder Mite (15×16 ember with a flame crown). Two idle frames (lava cracks pulse, walking bobs) and an attack frame. Each enemy carries a 26×5 health bar above its head from the moment it spawns, hidden on death.
- **Hit flash:** a white tint means nothing on a textured sprite, so `CombatantView` now flashes a **silhouette overlay** (`ILookSprites.SilhouetteOf`) tinted white, or crit gold, for the feedback duration; scenes without a look view keep the old tint. `IsFlashing` and `FlashColor` expose it to tests.
- **Effects** (`EffectArt`, `CombatEffectsView`): sword crescents, staff blast rings, dagger double slashes with a gold star on crits, ember sparks on every hit, smoke and coins on a kill that pays gold, floating 3×5-pixel damage numbers (crit numbers gold at 2×), and a 0.1 s camera shake when the hero is hit. Sprites and a 24-effect, 12-number pool are built once; nothing allocates per frame. `AttackController` gained a `Struck(Health)` event beside `Attacked`.
- **Reach:** with real sprites, an enemy stopping 0.9 floor units in front of the hero stood hidden behind the hero's helmet. Every reach grew (Cinder Mite 1.0 → 1.5; Grunt and Runner 1.2 → 1.5; Grunt Captain 1.3 → 1.6; Tank and Forge Warden 1.4 → 1.7) and the hero weapons with them (Sword 1.8 → 2.1, Staff 2.1 → 2.4, Daggers 1.5 → 1.8), so melee happens 0.7–0.8 world units above the hero's feet and the enemy's face stays visible. A +0.4 step for every enemy failed one balance target (the Daggers with Counterweight died on floor 2); this step keeps every target.

Balance after the reach change (health after floor 1 → end of floor 2; Wardens and mite packs are fight seconds on the D + Mend path):

| Weapon | D + Mend | S + Mend | D + Temper | S + Temper | D + Temper + Counterweight | D + Temper + Second Wind | S + Temper + Counterweight | S + Temper + Second Wind | Wardens | Mite packs |
|---|---|---|---|---|---|---|---|---|---|---|
| Sword | 46 → 37 | 43 → 21 | 12 → dies F2 room 2 | 9 → dies F2 room 1 | 29 → 26 | 37 → 34 | 13 | 23 | 14.6 s | 9.5 s |
| Staff | 46 → 36 | 40 → 32 | 12 → dies F2 room 2 | 6 → dies F2 room 2 | 40 → 37 | 37 → 34 | 21 | 30 | 15.6 s | 6.1 s |
| Daggers | 54 → 34 | 52 → 30 | 20 → dies F2 room 2 | 18 → dies F2 room 2 | 33 → 15 | 45 → 26 | dies F2 room 3 | 21 | 13.3 s | 10.0 s |

How it was made: the drawing was split across five parallel agents (backdrop, platform, hero, enemies with the flash and bars, effects), each owning disjoint files, verified with a dotnet compile of the gameplay scripts against the Editor's UnityEngine assemblies plus the .NET tests; Unity itself ran once for the wiring (a temporary Editor builder, deleted afterwards, added `HeroLookView` to the Vanguard, `CombatEffectsView` to the Arena and `EnemyLookView` to every enemy prefab, and set the bodies to white at scale 1) and once for the tests. A temporary PlayMode test rendered the running scene to PNG frames for review; after the first render the platform faces were darkened (the warm brown competed with the ember enemies) and the enemy bars were shown from spawn.

| Files | Change |
|---|---|
| `Scripts/Art/*.cs` (new) | Rgba, PixelCanvas, PixelNoise, PixelPalette, ArenaGeometry, VoidArt, PlatformArt, HeroArt, EnemyArt, EffectArt; `Art/Unity/PixelSpriteFactory.cs`. |
| `Scripts/UI/Arena/` (new) | MeshBuilder, VoidBackdropView, ForgePlatformView; `ArenaView` coordinates them. |
| `Scripts/UI/HeroLookView.cs`, `EnemyLookView.cs`, `ILookSprites.cs`, `CombatEffectsView.cs` (new), `CombatantView.cs`, `Scripts/Combat/AttackController.cs` | Looks, flash overlay, health bars, effects, the `Struck` event. |
| `Scenes/Gameplay/Gameplay.unity`, `Prefabs/Enemies/*.prefab`, every weapon asset | The wiring above; white bodies at scale 1; the new reaches. |
| Tests | EditMode `PixelCanvasTests` (6), `ArenaGeometryTests` (2), `VoidArtTests` (6), `PlatformArtTests` (7), `HeroArtTests` (6), `EnemyArtTests` (7), `EffectArtTests` (6). PlayMode: `ArenaViewTests` checks the drawn platform and the sorting of backdrop, platform and combatants; the Daggers crit test reads the flash from `CombatantView`; `UpgradeFlowTests` expects the Sword's new range. |

Known limits: one facing for every figure (the hero always faces away, enemies always toward the hero); the sword swing rotates the sprite off the texel grid for 0.12 s; the staff ring shows half the splash radius so it does not ring the whole platform; both floors share one platform and backdrop, although `19` gives Quicksilver Vaults cool silver tones; the HUD is still the old text HUD (the icon HUD is the next slice after the ability).

Verified: Unity EditMode 194/194, PlayMode 47/47, `Verify-Project.ps1`, .NET CombatChecks 178/178, development APK built only after both reports passed (SHA-256 `5FF7C1FF5FA33E6A96EA120D4EC6EDBF5EA838BF0C4F561B0C6E463D09238908`; installed and checked on the phone the next morning, see `23`).

### Implemented 2026-09-14: the Forge Burst (the hero's active ability)

The first player action inside a fight. The owner chose a burst around the hero over the mockup's touch-aimed ring (`19`), because the coming slice puts the finger on movement.

- **Ability:** `AbilityDefinition` (`Data/Abilities/Ability_ForgeBurst.asset`: 20 damage, 2.5 floor units, 8 s cooldown) on `HeroDefinition`; `AbilityRuntime` (pure) holds the cooldown and strikes every living target once; `AbilityController` on the Vanguard collects the enemies within the radius through `Targeting.CollectNear` around the hero, fires them, and ticks the cooldown on scaled time, so a pause or an open choice holds it. `CombatSetup` creates the runtime with the weapon and exposes it as `Ability`.
- **Button:** `Ability Button` on the HUD canvas at the bottom right (220 units, the built-in round knob sprite, above the hero's health line): a blue burst glyph drawn by `AbilityArt`, a radial fill that drains through the cooldown, and `AbilityButtonView`, which answers only while the run is live, no choice is open and the run is not paused. A tap fires; a burst with nobody in reach is still spent, as the player chose the moment.
- **Ring:** `AbilityRingView` lays a dashed isometric ring (`AbilityArt.DrawRangeRing`, twice as wide as tall, 2.5 units) on the floor around the hero's feet, sorted between the platform and the combatants: bright with a slow breath while ready, faint while cooling. `CombatEffectsView` flashes the blast ring at the burst's full radius on use; the struck enemies get their sparks and numbers from their own damage events.
- **Simulation:** `DescentSimulation.Run` takes an optional `HeroAbility`; the simulation fires it whenever it is ready and an enemy stands within the radius, after the hero's swing of that frame. The scene never fires it by itself, so the parity tests stay exact. Fired that way the burst is the player's edge, not the balance (health after floor 1 → end; D + Mend, S + Mend, S + Temper):

| Weapon | Without the burst | With the burst (uses) |
|---|---|---|
| Sword | 46 → 37, 43 → 21, dies F2 room 1 | 60 → 52 (9), 48 → 27 (10), dies F2 room 2 (7) |
| Staff | 46 → 36, 40 → 32, dies F2 room 2 | 57 → 47 (9), 45 → 44 (9), dies F2 room 2 (7) |
| Daggers | 54 → 34, 52 → 30, dies F2 room 2 | 62 → 48 (10), 58 → 42 (10), dies F2 room 3 (8) |

- **Smoke:** the death puff is drawn at 55% so it no longer hides the hero standing beside a fallen enemy (seen on the phone).

| Files | Change |
|---|---|
| `Scripts/Combat/AbilityRuntime.cs`, `AbilityController.cs`, `Scripts/Content/AbilityDefinition.cs`, `Data/Abilities/Ability_ForgeBurst.asset` (new); `HeroDefinition.cs`, `Hero_Vanguard.asset`, `Core/CombatSetup.cs` | The ability and its wiring into the run. |
| `Scripts/Art/AbilityArt.cs`, `Scripts/UI/AbilityButtonView.cs`, `AbilityRingView.cs` (new); `CombatEffectsView.cs`; `Gameplay.unity` | Glyph, ring, button and effects; a temporary builder, deleted afterwards, added the controller and ring to the Vanguard and the button to the HUD. |
| Tests | EditMode `AbilityTests` (3: strikes and cooldown, validation, the simulated edge without carrying greed), `AbilityArtTests` (2). PlayMode `AbilityFlowTests` (3): the button bursts both enemies of the first pack once they stand in the ring and then cools down; the button is dead behind a choice; the floor ring lies under the hero and fades while cooling. |

Known limits: one ability, no upgrades touch it, and the simulation's automatic use is a stand-in for the player's timing.

Verified: Unity EditMode 199/199, PlayMode 50/50, `Verify-Project.ps1`, .NET CombatChecks 183/183, development APK built only after both reports passed (SHA-256 `ECF52FA70B7EF17684C9CF7D76C81DA2D2AA0AD52C9CF9CFAFBF4BEA6B8BEC70`).

### Implemented 2026-09-14: the walkable arena (packs from every corner, a walking hero, chests, a following camera)

The owner's request after seeing the reference game again (`19`): a wider platform in the middle, the hero walkable, packs from left and right and from all four sides, chests to survive by, the art larger on screen.

- **Platform:** `ArenaGeometry(-9, 9, 9)`: a diamond 18 world units wide and 9 tall on screen, the hero at its centre; `PlatformArt` draws it as before with 16 tiles per edge (581×419 texels). The backdrop's islands, rubble, sparkles, haze and galaxy moved beyond the corners and the keel (`VoidLayout`), the star field grew to 24×27 units.
- **Corners:** `EntrySide` and `EntrySides`: a wave's slots go round the far, right, near and left corners in turn, starting one corner further for each wave of a floor, so a pack of four comes from every side at once. `PackLayout` still forms the enemies that share a corner. Packs enter 5 floor units from the centre (`_entryDepth`).
- **Motion:** `PackMotion` takes the hero's position each step (`Step(dt, heroX, heroY)`); each enemy heads for its own point on the arc round the hero, turned by its corner and its lateral slot, stops just inside its reach, and walks again when the hero leaves. `EncounterController` feeds it the hero's floor position (`HeroFloorX/Y`) and picks the wave's nearest enemy relative to it.
- **Hero:** `HeroMotion` (pure): 2.5 floor units per second, a steer with a dead band, the rim as a wall with sliding along the edge (0.6 units inside it). `HeroMovementInput` on the Vanguard: a touch or mouse press anywhere that is not a button starts a drag; past 24 px the hero walks, at 110 px at full speed; screen up is floor depth, so a diagonal drag walks the same diagonal on screen; `Hold`/`Release` steer it without a touch (tests, and later an auto-walk option). It runs before the encounter so the packs chase this frame's position.
- **Chests:** `ChestRule` (pure) puts one chest per combat room on a spot circling the centre (right, far, left, near) 1.8 units out, inside the edge of what the camera shows; even rooms mend 25% of maximum health, odd rooms pay 10 gold at the floor's rate; the hero opens it by standing within 0.75 units. `ChestSpawner` draws it (`ChestArt`, closed and open with a coin glow) and applies the reward; `CombatEffectsView` shows the heal amount or pops coins with the sum.
- **Camera:** `ArenaCameraFollow` replaces the fixed framing: 4.6 world units across the screen (`FollowFraming`, pure), the hero on the middle row between the HUD blocks, gliding after the hero at 8 per second and re-framed on screen or safe-area changes.
- **Balance:** the simulation's hero stands still at the centre and fires the burst whenever it is ready with an enemy in reach, which is now the baseline for every balance test (`Burst(...)` helpers); chests are not simulated. With every corner striking at once the old numbers fell on floor 1, so floor 1 scales enemy damage by 0.92 (`_enemyDamageMultiplier`), floor 2 by 1.288 (its ×1.4 kept), and the Daggers strike every 0.5 s instead of 0.45 (they alone carried a greedy Temper descent at 0.92). Every earlier target holds:

| Weapon | D + Mend | S + Mend | D + Temper | S + Temper | D + Temper + Counterweight | D + Temper + Second Wind | Wardens | Mite packs |
|---|---|---|---|---|---|---|---|---|
| Sword | 60 → 30 | 55 → 29 | dies F2 | dies F2 | survives | survives | 12.1 s | 7.2 s |
| Staff | 42 → 29 | 48 → 39 | dies F2 | dies F2 | survives | survives | 13.6 s | 5.8 s |
| Daggers | 69 → 34 | 59 → 22 | dies F2 | dies F2 | survives | survives | 11.6 s | 8.3 s |

- **Parity:** unchanged in spirit; the scene and the simulation share `EntrySides` and `PackMotion`, the scene's hero never moves in the parity tests, and all seven cases match exactly.

| Files | Change |
|---|---|
| `Scripts/Combat/EntrySide.cs`, `HeroMotion.cs`, `ChestRule.cs`, `ChestSpawner.cs` (new), `PackMotion.cs`, `EncounterController.cs`, `ArenaFloor.cs` | Corners, the walking hero's rules, chests, the moving target. |
| `Scripts/UI/HeroMovementInput.cs`, `ArenaCameraFollow.cs`, `Scripts/Art/FollowFraming.cs`, `ChestArt.cs` (new); `ArenaCameraFraming.cs`, `ArenaFraming.cs` (removed); `VoidArt.cs`, `PlatformArt.cs`, `CombatEffectsView.cs` | Drag input, the following camera, chest art and effects, the wider void and 16 tiles. |
| `Gameplay.unity`, `Floor_EmberHalls.asset`, `Floor_QuicksilverVaults.asset`, `Weapon_Daggers.asset` | The 9-unit diamond, entry depth 5, the new components (a temporary builder, deleted afterwards), the damage rates and the Daggers' interval. |
| Tests | EditMode `PackMotionTests` (corners, a walking hero), `HeroMotionTests` (2), `ChestRuleTests` (2), `ChestArtTests` (1), `FollowFramingTests` (3); the balance tests run with the burst. PlayMode `ArenaWalkTests` (3: walking, chasing and the rim; the mending chest; the paying chest), `ArenaViewTests` (corners on the platform, the following camera), `FloorFlowTests` (corner entries), and the first-pack counts (7 swings, 7 hits; 3 swings and 4 hits after Tempered Edge). |

Known limits: every figure keeps one facing, so enemies from the near corner walk with their backs to the camera; blocked enemies wait rather than step round each other; the hero has no walk animation; the chests and the hero's position are not part of the balance.

Verified: Unity EditMode 205/205, PlayMode 53/53, `Verify-Project.ps1`, .NET CombatChecks 193/193, development APK built only after both reports passed (SHA-256 `16FCB13983917E04C9D854609893CCFC7B2878BDD7010120F659DF98F96DE69C`).

### Implemented 2026-09-14: packs stay on the platform when the hero stands at the rim

A read-through of the walkable arena found two rules that only held while the hero stood near the centre. Packs entered round the arena's centre, so a hero at the right corner had the right corner's pack appear on its left and the far corner's pack behind it. And an enemy's stopping point beside the hero was never checked against the platform: the hero keeps 0.6 units inside the rim and enemies stop up to 1.6 away, so a Grunt that followed the hero to a corner stopped over the void.

- **Entry:** `EntrySides.Place` lays each wave out round the hero's position when it enters, 5 floor units out as before. An enemy whose spot lies beyond the rim, or inside the 0.6-unit margin the hero keeps, starts short of the rim on the line from the hero, as long as that leaves it at least 3 floor units away (`MinimumEntryDistance`, beyond every enemy's and weapon's reach). A corner with an enemy that has no such room closes for that wave and the open corners take its enemies in turn (`Formation` over an open-corner mask). So halfway to the right corner all four corners still send their enemies, the right, far and near ones starting nearer; from about 5 units out (5.4 for a lone enemy per corner, 4.9 for pairs), and at the corner itself, the whole wave comes from the left; on an edge, from the two corners that have floor. If every corner closes, which only a platform too small for the wave can cause, each enemy is drawn in toward the hero until it stands inside.
- **Found on the phone:** the first version closed a corner as soon as any of its spots lay beyond the rim, without drawing it in. On the phone the level-up froze the hero halfway to the right corner, and every later wave of the room came from the left; the rule above keeps the four sides the owner asked for.
- **Stopping:** `PackMotion` takes the platform when a scene or the simulation creates it. An enemy whose own point beside the hero lies outside the margin turns round the hero in 5-degree steps, both ways, to the first point on the platform, and takes the way nearer to where it stands, so it never crosses in front of the hero. The hero always stands inside the same margin, so the half of the circle facing the platform's centre always qualifies.
- **Nothing changes near the centre:** from the centre every corner fits every pack of up to seven and no point at reach is near the rim, so each enemy enters and stops exactly where it did before; a test compares every slot of every pack size bit for bit, and the balance and parity numbers are unchanged. `DescentSimulation.Platform` mirrors the scene's arena, checked by `ArenaViewTests`.
- **Checked and left alone:** a press that starts on the burst or pause button does not walk the hero. The event system runs at execution order -1000, before `HeroMovementInput` at -60, so a press is already known to be on a button in its first frame, and the HUD labels do not catch raycasts. An earlier note in `23` had claimed otherwise; a drag from the burst button on the phone confirmed the input was right, and the note is corrected.

| Files | Change |
|---|---|
| `Scripts/Combat/EntrySide.cs` | `EntryPlacement`, `EntrySides.AllSides` and `MinimumEntryDistance`, `Formation` over open corners, `Place` round the hero with spots drawn in short of the rim and closing corners. |
| `Scripts/Combat/PackMotion.cs` | A platform-bounded constructor with the hero's position, `Add` from a placement, turning onto the platform. |
| `Scripts/Combat/EncounterController.cs`, `Gameplay.unity` | The encounter holds the `ArenaView` and places each wave round the hero; the scene reference was added to the encounter's serialized fields. |
| `Tests/Support/DescentSimulation.cs` | The simulation places and walks its packs with the same calls on the same platform. |
| Tests | EditMode `PackMotionTests` (+3): packs from the centre form up as before bit for bit, with open-corner formations and validation; halfway to the right corner every corner still sends enemies, the right corner's drawn in short of the rim; most of the way there, at a corner and on an edge the corners without room close; every enemy on the platform, at least 3 units away and apart from the others, and a too-small platform drawing them in; a Grunt that followed the hero to the right corner turns onto the platform where it would have stood over the void. PlayMode `ArenaWalkTests` (+1): with the hero at the right corner the next wave enters from the platform's side, out of reach, and every enemy walks in, stays on the platform every frame and strikes; `ArenaViewTests` places packs of up to seven from the centre and checks the simulation's platform. |

Known limits: enemies still wait behind a nearer one instead of stepping round it, so at a corner a pack can queue; a hero who walks along the rim while an enemy is turning can make it walk a longer arc.

Verified: Unity EditMode 208/208, PlayMode 54/54, `Verify-Project.ps1`, .NET CombatChecks 196/196, development APK built only after both reports passed (SHA-256 `58E9C943607C34E1C997403ABD4DD35684BBA267700B0D30C09B7DE054BBC31A`; the first version, which only closed corners, was `080FF9A7F5FA79FCABBD38AFB1A122C7540728A9634864B2195AB4E17BBD90F3`).

### Implemented 2026-09-15: a frame-time probe for phone sessions

No frame rate had been measured on a device (`22`, `23`), and doc 06's budget asks for stable 60 fps and no GC spikes from spawn-heavy systems. The probe measures a development build on the phone itself, without a profiler connection and without drawing anything.

- **Probe:** `FrameTimeProbe` installs itself after the scene loads in a development player only (`Application.isEditor` or a release build leaves it out, so tests and the Editor never see it) and survives scene reloads. Every 5 s of real time it logs a window through `FrameStats`: frames, fps, average, p50, p95, p99, the longest frame, frames over 20 ms and over 35 ms. It adds the longest main-thread frame, the managed memory allocated and the GC runs, and the managed and total memory in use (Unity `ProfilerRecorder` counters). It logs a line for every wave that enters, with the enemy count and the `Cryptforge.StartWave` marker's time, and one for any other frame past 35 ms. Lines carry `[Perf]`, use the invariant culture and skip stack traces.
- **Stats:** `FrameStats` (pure) keeps up to 4096 samples per window for nearest-rank percentiles and counts every frame for the average and the longest; it sorts into its own buffer, so summaries allocate nothing.
- **Marker:** `PerformanceMarkers.StartWave` wraps `EncounterController.StartWave`: instantiating, drawing and setting up every enemy of a wave and everything that answers it. It costs nothing outside development builds.
- **First session:** see `23`. On the S23 the game held 60 fps; a wave costs about 11 ms of main thread and up to 700 KB of garbage, and a new run 17.6 MB in one frame, the costs a mid-range phone will feel first.

| Files | Change |
|---|---|
| `Scripts/Core/FrameStats.cs`, `FrameTimeProbe.cs`, `PerformanceMarkers.cs` (new) | Window statistics, the development-only probe, the wave marker. |
| `Scripts/Combat/EncounterController.cs` | `StartWave` runs its body (`SpawnWave`) inside the marker. |
| Tests | EditMode `FrameStatsTests` (2): a window's average, percentiles and missed deadlines; frames past the capacity, invalid durations and validation. The probe itself runs only on a device; its output is checked in `23`. |

Verified: Unity EditMode 210/210, PlayMode 54/54, `Verify-Project.ps1`, .NET CombatChecks 198/198, development APK built only after both reports passed (SHA-256 `03443B303B76A1E97E09239A460B768021B66CC34A640115C91023BE4C025605`), then a 150-second session on the phone.

### Implemented 2026-09-15: a local telemetry log

Handoff item D (`25`), specified in the owner's prompt through Astra's shared roadmap: a bounded local event log for device sessions, with no backend, SDK or network, beside the profile and separate from it. Built by a five-agent workflow in dependency order: the log core and the gameplay events in parallel, then the recorder, the Unity bridge and an adversarial review. The review found one real defect and fixed it: an old scene disposed after a new run began wrote `run_interrupted` under the new run's id.

- **File:** `<persistentDataPath>/telemetry/events.jsonl`; on Android `/sdcard/Android/data/com.cryptforge.prototype/files/telemetry/`. The format is JSON Lines, schema 1, UTF-8 without a byte-order mark. At 256 KB the file rotates into `events.1.jsonl` and `events.2.jsonl`, never more than three files, and a line never splits across files.
- **Envelope:** each line carries, in order:
  - `v`: the schema version.
  - `seq`: strictly increasing within a session, across runs and restarts.
  - `t` and `st`: UTC time and seconds since the session started.
  - `session`: a random id per app launch.
  - `run`: `<session>-<ordinal>`, only while a run is open.
  - `event`, then the event's fields.

  Numbers use the invariant culture.
- **Events:**
  - Session and run: `session_start`, `run_start`, `run_end`, `run_interrupted`. `run_end` is written exactly once, with result, cause (`killed_by:<enemy_id>`), floors, rooms, level, kills, upgrades, gold, banked and lost gold, and active, choice, pause and background seconds. `run_interrupted` marks a run whose scene unloaded before it ended; no end is ever fabricated.
  - Progress: `room_start` and `room_complete`, `boss_start` and `boss_end`, `floor_complete`.
  - Play: `first_kill`, `chest_opened`, `ability_used`.
  - Choices: `upgrade_offered`, `upgrade_selected`, `forge_selected`, `extract_choice`.
  - Currency: `currency_earned` (run gold per room, by source: kills or chest; forge gold banked at run end) and `currency_spent` (Relic Forge purchases, outside any run).
  - App and log: `app_background`, `app_foreground`, `telemetry_dropped`.

  Not logged: frames, ordinary hits, the device model, hardware ids or any account.
- **Time:** active seconds come from scaled time while no choice is open and the player has not paused: fighting, walking and the delays between waves. Choice and pause seconds come from unscaled time. Background seconds come from a monotonic clock between leaving and returning, with the resume frame skipped. All are kept per room, per floor and per run.
- **Writes:** lines wait in a buffer of at most 512 (the oldest are dropped and reported). They are written on the main thread only when a choice opens, at a run end or interruption, when the app goes to the background, at quit, and every 64 lines or 10 seconds.
- **Failures:** only IO and access errors are caught. The lines stay buffered, combat goes on, nothing reaches the console and the profile is never touched. After a failure only the 10-second timer retries.
- **Composition:** `CombatSetup` adds `RunTelemetryBridge` at runtime, so the scene is unchanged. It is attached before `CombatSetup` subscribes to the checkpoint choice, kills and floor clears, so each cause is logged before its effect (Extract ends the run inside `Checkpoint.Chosen`); that subscription moved below it, and `CombatSetup` is its only subscriber.
- **Bridge and session:** the bridge forwards encounter progress (compared on each hit, logged only on a change), kills, floor clears, the ability and the scene's chest spawner, and disposes the recorder on scene unload. `TelemetryRuntime` keeps one session per folder across restarts.
- **Gameplay events:** new `UpgradeService.Selected`, `ForgeService.Selected`, `RelicShop.Forged` and `WeaponShop.Forged`, with no behaviour change. `Selected` fires after the choice applied and before the offer that follows it.

| Files | Change |
|---|---|
| `Scripts/Analytics/TelemetryFields.cs`, `TelemetryRecord.cs`, `TelemetryJson.cs`, `ITelemetryClock.cs`, `StopwatchTelemetryClock.cs`, `ITelemetryStore.cs`, `FileTelemetryStore.cs`, `TelemetryLog.cs`, `TelemetrySession.cs` (new) | The pure log: fields, lines, the rotating store, the bounded buffer, the session. |
| `Scripts/Analytics/RunTelemetry.cs`, `RunTelemetryContext.cs` (new) | The pure recorder: every event, the ordering rules, time accounting, room currency. |
| `Scripts/Analytics/Unity/RunTelemetryBridge.cs`, `TelemetryRuntime.cs`, `TelemetryLocation.cs` (new); `Scripts/Core/CombatSetup.cs` | The Unity side and the wiring. |
| `Scripts/Progression/UpgradeService.cs`, `ForgeService.cs`, `RelicShop.cs`, `WeaponShop.cs` | The four events. |
| `Tools/CombatChecks/CombatChecks.csproj` | The pure telemetry sources and tests. |
| Tests | EditMode (+41): `TelemetryJsonTests` (5), `TelemetryStoreTests` (5), `TelemetryLogTests` (8), `TelemetrySessionTests` (5), `RunTelemetryTests` (12), and 6 event tests in `UpgradeTests`, `FloorTests`, `RelicForgeTests`, `WeaponForgeTests`. PlayMode `TelemetryFlowTests` (4): the first pack and level-up logged in order beside a valid profile; a defeat and restart with one `run_end`, a second run id and no duplicated selection; a telemetry folder blocked by a file, with combat going on and no console errors; the first chest walk logged with its room and healed health. |

Known limits:
- Chest gold picked up while no room is open (during the descent after Descend) appears only in `chest_opened`, not in a room's `currency_earned`.
- A flush that spans a rotation and fails part-way can repeat whole lines, with the same `seq`, on the retry.
- The monotonic clock stops during deep sleep, so `background_sec` can undercount with the screen off.
- `Application.quitting` is unreliable on Android; the other flush points cover it.
- Telemetry is not part of the balance simulation.

Verified: Unity EditMode 251/251, PlayMode 58/58 (the seven parity cases unchanged), `Verify-Project.ps1`, .NET CombatChecks 239/239, development APK built only after both reports passed (SHA-256 `BAA8E568E0B8D3D9DFB00A3FBB243311F45916AAD17C7D73CA80FF6E90623A97`), then one run on the phone whose log matched its result screen (`23`).

### Original Day 3 plan (kept for reference)

Keep the existing gameplay scene. Implement one repeatable Grunt encounter and a two-choice numeric upgrade proof before adding enemy types or weapon behaviors.

Exact additions planned under `Assets/_Project/`:

| Files | Purpose |
|---|---|
| `Scripts/Core/RunState.cs` | Plain C# run XP, level, phase, and selected upgrade state |
| `Scripts/Economy/RewardService.cs` | Award a configured kill reward once, from death events |
| `Scripts/Content/EconomyConfig.cs` | XP-per-kill and level threshold data |
| `Scripts/Content/UpgradeDefinition.cs` | Stable ID, display text, stat, operation, amount, stack limit |
| `Scripts/Progression/StatModifier.cs` | Additive then multiplicative stat modification with clamps |
| `Scripts/Progression/UpgradeService.cs` | Apply a selected definition to runtime stats; prevent double selection |
| `Scripts/Combat/EncounterController.cs` | Own one active encounter and supply targeting candidates; continue after selection |
| `Scripts/UI/UpgradeChoiceView.cs` | Two large touch choices on a uGUI Canvas |
| `Scripts/UI/RunHud.cs` | Replace the temporary IMGUI HUD; show HP and XP |
| `Data/Economy/PrototypeEconomy.asset` | Configured XP reward and threshold |
| `Data/Upgrades/Upgrade_Damage.asset` | Numeric damage choice |
| `Data/Upgrades/Upgrade_AttackSpeed.asset` | Numeric attack cadence choice |
| `Prefabs/Enemies/Grunt.prefab` | Extract current enemy setup for sequential spawning |
| `Prefabs/UI/UpgradeChoicePanel.prefab` | Reusable touch choice panel |
| `Tests/EditMode/RewardAndUpgradeTests.cs` | Reward idempotency, stack limits, modifier order, definition immutability |
| `Tests/PlayMode/UpgradeFlowTests.cs` | Kill → award → choose → stronger next encounter |

Modify `WeaponRuntime`, `Targeting`, `CombatSetup`, and `Gameplay.unity` only to integrate the new slice. Remove `PrototypeHud` when replacing it, and migrate its text data to the new UI.

Order: reward/state tests → death-to-XP flow → data/modifier calculation → choice UI and pause → choice application → next Grunt encounter.

Acceptance: kill awards XP once; a configured threshold opens two choices and pauses encounter progression; one tap applies exactly one upgrade; rapid repeated taps cannot duplicate it; the next encounter uses modified runtime stats; the source Sword asset is unchanged. Tune the first-choice time toward 30–60 seconds once sequential encounter pacing exists. No persistence is required for this temporary run state.

## Actual first-slice file inventory

All Unity assets and asset directories include committed-ready `.meta` files with stable GUIDs. No empty future-system folders or placeholder abstractions are created.

```text
.gitignore
.gitattributes
README.md
Packages/manifest.json
Packages/packages-lock.json          generated by the Editor; retained
ProjectSettings/
  ProjectVersion.txt
  ProjectSettings.asset
  EditorSettings.asset
  VersionControlSettings.asset
  EditorBuildSettings.asset
Assets/_Project/
  Art/Placeholders/Square.png
  Data/
    Heroes/Hero_Vanguard.asset
    Enemies/Enemy_Grunt.asset
    Weapons/Weapon_Sword.asset
    UI/PrototypeText.asset
  Scenes/Gameplay/Gameplay.unity
  Scripts/
    Project.Gameplay.asmdef
    Core/CombatSetup.cs
    Combat/
      DamageContext.cs
      IDamageable.cs
      HealthState.cs
      Health.cs
      WeaponRuntime.cs
      Targeting.cs
      AttackController.cs
    Content/
      HeroDefinition.cs
      EnemyDefinition.cs
      WeaponDefinition.cs
      PrototypeTextDefinition.cs
    UI/
      CombatantView.cs
      PrototypeHud.cs
    Editor/
      Project.Editor.asmdef
      AndroidPrototypeBuild.cs
  Tests/
    EditMode/
      Project.EditModeTests.asmdef
      CombatStateTests.cs
    PlayMode/
      Project.PlayModeTests.asmdef
      CombatSceneTests.cs
Tools/
  Verify-Project.ps1
  CombatChecks/CombatChecks.csproj
docs/
  21_DAY_1_3_IMPLEMENTATION_PLAN.md
  22_FIRST_COMBAT_VERIFICATION.md
  23_ANDROID_DEVICE_VALIDATION.md
```

`19_DECISION_LOG_TEMPLATE.md` also gains an actual decision entry after the original template. All other original documentation remains unchanged.

## Deliberate tradeoffs and next boundary

- One runtime assembly plus two test assemblies supports tests without fragmenting the gameplay code.
- `HealthState` is the small testable rule object; `Health` bridges it to Unity. No damage-receiver or death-handler class is necessary yet.
- Sword uses one direct hit. Cleave, Bow projectiles, Staff AoE, Runner, Tank, room escalation, boss, result screen and one-tap restart belong to later mechanical slices. The Day 2 victory label is only combat feedback, not a run result system.
- The stationary hero and enemy start within range. Movement and enemy retaliation are not needed to prove this first slice.
- No pooling for two authored entities, no singleton/GameManager, no save, no backend, no SDK integration, and no final art.
- UI text lives in `PrototypeText.asset`; stable localization keys are `prototype.<fieldNameWithoutUnderscore>` and `<content_id>.name`. No localization framework is needed yet.
- The temporary IMGUI HUD deliberately trades production layout/performance for a small read-only prototype. Device performance has not been measured; replace it for the interactive upgrade slice.

## Movement and density proof: ten enemies and a kiting route — 2026-09-16

One bounded slice that answers a single question with measurements instead of opinion: what does a normal room cost, and what does it cost differently, once ten enemies enter and the hero walks away from them. It adds no boss attack, no upgrade, no seed replay, no currency and no art, and it does not touch `Gameplay.unity`, the camera or the HUD.

**Capacity.** `PackLayout.MaxPackSize` goes from 7 to 10 with formation rows for 8, 9 and 10; rows 1-7 are byte-identical, so every authored wave forms up exactly where it always did. The new rows stay symmetric about their centre line, within the half-width of 2 and the depth limit of 4, and their closest pair is 1.5 spacing units against the required 1.1.

**Spawning ten without overlap.** `EntrySides.Place` gains an overload taking a `minimumSeparation`, and `EncounterController.SpawnWave` passes the encounter's body spacing (0.9) to it, so the scene and the Descent simulation place a wave identically. A spot that lands within a body of an earlier one closes its own corner exactly as a spot over the void does, and the open corners take the wave again. Swept over every hero spot on a 0.3 grid, four waves and pack sizes 1-10: a pack of six or fewer never moves at all, whichever corner the hero stands in, and every authored wave in `Data/Floors` holds five or fewer. From seven up, a layout moves if and only if the plain one would have started two bodies overlapping, and every layout it moves it also separates.

**A hero that walks.** `Scripts/Combat/HeroRoute.cs` defines `RouteView` and `IHeroRoute.Steer`; `StationaryRoute` holds still, `ScriptedRoute` follows an authored list of legs, and `KiteRoute` steers away from whatever is about to reach the hero while keeping clear of the rim. All three are pure, allocation-free and deterministic, and none can return a steer that is NaN, longer than one, or that walks the hero off the platform.

**The same rules in the simulation.** `Tests/Support/DescentSimulation.Run` takes an optional route and a hero speed, drives one `HeroMotion`, asks the route for a steer at the end of a frame and applies it at the start of the next - the scene's one-frame input lag - and measures every distance from the hero's own position with the scene's operand order. A run without a route is bit-identical to the old stationary baseline, which a dedicated test pins over the whole Descent. New result fields record hits and damage taken, frames off the platform, strikes landed while moving, the earliest strike after a spawn, the closest hero-to-enemy and enemy-to-enemy gaps and the hero's final floor position.

**Reaching the proof encounter without editing the scene.** `Scripts/Core/DevelopmentStart` lets the Editor or a development build start the run on a floor from `Resources/Development` when `<profile>/development/start-floor.txt` names its id. A release build returns the authored floor before touching the file system, a missing or unreadable file is the authored floor, and nothing logs in any case. `Resources/Development/Floor_DensityProof.asset` is that floor: one combat room, one wave of two Grunts and eight Cinder Mites at Ember Halls' damage rate, no next floor. Only development floors may live there.

**A rim that every runtime agrees on.** An entry pulled in short of the rim used to converge onto the rim line exactly, where whether the spot counts as on the platform is decided by the last bit of a float - the Editor's Mono and the pure .NET runner disagreed about 40 of the 56 grid spots that sit on that line, and put the same pulled-in spot on opposite sides of it by one bit (measured: 1.14e-7 in the normalized measure, about a millionth of a floor unit). `EntrySides` now gives up a ten-thousandth of the way in (`RimRelief`), which moves an entry by a thousandth of a unit at most and puts it hundreds of bits clear of the line. No authored wave is pulled in at all, so no existing number moved.

Files added: `Scripts/Combat/HeroRoute.cs`, `ScriptedRoute.cs`, `KiteRoute.cs`, `Scripts/Core/DevelopmentStart.cs`, `Resources/Development/Floor_DensityProof.asset`, `Tests/EditMode/HeroRouteTests.cs`, `Tests/EditMode/DensityProofTests.cs`, `Tests/PlayMode/DensityProofParityTests.cs`. Files changed: `Scripts/Combat/PackLayout.cs`, `EntrySide.cs`, `EncounterController.cs`, `Tests/Support/DescentSimulation.cs`, `Tests/EditMode/PackTests.cs`, `PackMotionTests.cs`, `Tools/CombatChecks/CombatChecks.csproj`.

**What this slice does not do.** It does not raise the capacity of any floor a player can reach: every authored wave still holds five enemies or fewer, and the ten-enemy encounter exists only on the development floor. It does not give the hero a body that blocks enemies, does not model between-wave frames for a routed hero, and does not close the scene's upgrade-withdrawal gap (both recorded in `22`). It measures; it does not retune. The balance question it surfaced - kiting the Staff through the authored Descent - is left for a later slice.

## Shared enemy sprites across waves and restarts — 2026-09-16

`Art/Unity/EnemySpriteCache` lazily draws each enemy look once per application session. Three body frames and three matching flash silhouettes per look are shared; the two bar textures are shared across all looks. Six looks therefore retain at most 38 sprites/textures. `EnemyLookView` borrows this art and still owns its animation clocks, renderers, flash state through `CombatantView`, and health-fill scale. Destroying an enemy or reloading Gameplay does not destroy the shared art. Application quit and subsystem registration release both native sprites and their textures; the latter also prevents stale state with domain reload disabled. The ownership exception and alternatives are recorded in `19`.

Four added PlayMode cases cover the six-look art contract and repeated borrowing, release of every native resource and fresh recreation, scene restart followed by `Resources.UnloadUnusedAssets`, and independent health/critical flash state when another enemy is destroyed. Existing movement, economy, telemetry and simulation parity cases remain unchanged. The first Unity attempt exposed missing friend-assembly access (`CS0122`), which the single-assembly compile check cannot detect; this was fixed before the full passing gate. No APK was built from that failed attempt.

The Windows Unity runner now waits for the Editor process itself with `WaitForExit`, rather than waiting for its long-lived licensing/helper descendants, and starts it hidden. This fixes the observed post-exit stall without weakening either suite or the APK gate. One stale lock from the failed compiler run was archived under ignored `TestResults/` only after confirming no Unity process remained.

Files added: `Scripts/Art/Unity/EnemySpriteCache.cs`, `Tests/PlayMode/EnemySpriteCacheTests.cs` and their new GUID metadata. Changed: `Scripts/UI/EnemyLookView.cs`, ownership comments in `PixelSpriteFactory.cs`, `Tests/PlayMode/ArtHudFlowTests.cs`, `Tools/Run-UnityTests.ps1`, documentation and the arena production-plan status. No scene, prefab, art canvas, gameplay rule, package lock or project setting changed.

**Verified:** .NET **257/257**, Unity compile **0 warnings/errors**, EditMode **269/269**, PlayMode **74/74**, Android build exit **0**, static integrity **284 unique asset/folder GUIDs / 555 scene objects/components**. Development APK **24,738,586 bytes**, SHA-256 **`4A2F37C48A7DC451776772479780F9D9C16D2D488DA33F54365A40A4B92AF4CD`**. The installed S23 package hash matches. Device measurements and their whole-frame attribution limits are recorded in the corresponding entry in `23`.

## Shared platform and backdrop sprites across restarts — 2026-09-16

Following the enemy cache measurement, `ArenaSpriteCache` retains the current platform's surface and two light frames, plus the fixed void backdrop's sprites. `VoidSprites` generates haze, galaxy, star layers, sparkle/flame poses, islands, rings, chains and rubble once; it retains only sprites, layout and small attachment coordinates, not the temporary CPU canvases. Renderers, sky mesh, drifter transforms, flicker phases and platform animation clocks still belong to the scene. Existing drawing functions, palette, pivots, sorting, geometry, camera and gameplay are preserved.

Platform sharing checks near corner, far corner and half-width exactly. The first geometry is retained; a different geometry receives private art that its view destroys, keeping the session cache bounded and avoiding incorrect dimensions. Application quit and subsystem registration release both sprites and textures. The architecture choice and the limitation for future multi-room art are recorded in `19`.

Four added PlayMode cases cover repeated builds reusing every visible sprite and attachment/sorting state, destroying one view while another survives unused-asset cleanup, three different geometry variants releasing only their private art, all native resources including hidden frames being released on session reset, and actual Gameplay reload followed by pause/resume animation. The build-cost diagnostic records CPU-side Editor timings and real `GC.Alloc` event counts, never treating the unsupported zero-byte Mono counter as a valid measurement. The phone probe gains separate `Cryptforge.BuildBackdrop` and `Cryptforge.BuildPlatform` markers and labels reported bytes as whole-frame allocation.

Added: `Scripts/Art/Unity/ArenaSpriteCache.cs`, `VoidSprites.cs`, `Tests/PlayMode/ArenaArtCacheTests.cs` and metadata. Changed: `UI/Arena/ForgePlatformView.cs`, `VoidBackdropView.cs`, `UI/ArenaView.cs`, `Core/PerformanceMarkers.cs`, `FrameTimeProbe.cs`, ownership comments in `PixelSpriteFactory.cs`, `ArtHudFlowTests.cs`, and documentation. Scene YAML, existing metadata, imported assets, combat/economy/profile/telemetry logic, project settings and package lock remain untouched.

**Verified:** .NET **257/257**, Unity compile **0 warnings/errors**, EditMode **269/269**, PlayMode **78/78**, Android build exit **0**, static integrity **287 unique asset/folder GUIDs / 555 scene objects/components**. Development APK **24,738,586 bytes**, SHA-256 **`6DE8F2A9A2B92B3CA15A50FA5EA71980ACB8E91C60CF832C7A126F1D8CC39ED6`**; installed S23 package hash matches.

The cache holds **37 textures**, **8,365,956 RGBA32 texel bytes** (about 7.98 MiB, excluding native object overhead). In the full Editor suite, repeated platform/backdrop builds took **0.06/0.65 ms**, with **6/234 allocation events**, versus the preceding uncached diagnostic's **18.08/27.51 ms**, **82/1033 events**. Three S23 warm restart frames allocated **1008.5–1131.4 KB**, versus **17,620.8–17,742.4 KB** in the preceding enemy-cache session (approximately 94% less). Platform/backdrop markers measured **0.03–0.04 / 0.97–1.06 ms**. Device frame/main-thread limits and cold-start exclusions are recorded in `23`. This completes the bounded environment reuse slice, not all restart/startup optimisation or production environment art.

## Room-bound framing and the first furnace layer — 2026-09-16

`FollowFraming.ConstrainToArena` moves the free HUD band's centre toward the room interior near the rim, while reserving space around the hero for nearby enemy bodies/bars. `ArenaCameraFollow` reads the scene's actual `ArenaView` geometry and origin; width 9 and smooth movement remain. It no longer promises the hero's feet always sit exactly at the band's centre. The rule adapts to the available band height, including the existing thin-band fallback.

The scene now assigns `ENV_Furnace_Cavern_v1.png`, an original generated 1024 x 1536 distant environment layer. `VoidBackdropView` uses that imported asset with aspect-preserving cover and bounded parallax, leaving the existing procedural sky mesh underneath and skipping unused stars/islands. Without the imported sprite/camera it still builds the session-cached procedural void. Platform, actors, HUD, collision and all gameplay/progression systems remain independent. No full-island boss room, new encounter density, seed or upgrade was implemented.

Changes: `FollowFraming.cs`, `UI/ArenaCameraFollow.cs`, `UI/ArenaView.cs`, `UI/Arena/VoidBackdropView.cs`, two new scene references, the environment PNG/import metadata, `FollowFramingTests`, `ArenaViewTests`, the reload/animation case in `ArtHudFlowTests`, and docs/art provenance. Three new pure cases sweep the platform at two portrait heights and exercise shifted/small/large geometry. One new scene case checks actual hero body bounds, room binding and background coverage at the centre and all four corners on both aspect ratios. Reload testing checks imported sprite identity, skipped procedural stars, mesh cleanup and platform pause/resume.

The temporary Editor setup changed only two scene references; it and the offscreen capture fixture were removed before the full suite/build. Seven actual Unity renders compare centre/far/right at 1080x2340 and 1080x1920, plus unbounded far follow. Their ten frozen visual actors are staging without combat/bar controllers, using real spawn placements. Safe insets are simulated. See `ArtDirection/2026-09-16/furnace-layer-01/README.md` for source/import settings and visual limits; these renders are not device evidence.

**Verified:** .NET **260/260**, compile **0 warnings/errors**, EditMode **272/272**, PlayMode **79/79**, Android exit **0**, static integrity **289 unique asset/folder GUIDs / 555 scene objects/components**. Development APK **24,739,335 bytes**, SHA-256 **`3704897005DA235B5C23DD70761DD2D784387489D5A896091F9241735CCB6F6F`**, installed S23 hash matched. Live far-rim and right-corner drags, choice overlays, proof-floor warm restart and normal-floor cleanup passed the short phone review in `23`. One warm proof restart allocated **1005.5 KB** across the whole frame, with backdrop/platform build **0.18/0.06 ms**. This is not a new sustained-combat or mid-range benchmark.

## Two-sided enemy spacing while packs move — 2026-09-16

**The defect.** `PackMotion` held an enemy back only when its step would end inside the spacing of an enemy nearer the hero that had already stepped that frame. A nearer enemy was never held back by a farther one, so it walked straight into it, and nothing ever separated them again. Measured in the Editor's runtime on the development proof floors, the closest two living enemies came to **0.704** with the hero standing still, **0.214** while kiting and **0.072** while turning round, against a body spacing of 0.9; on the authored Descent a walking hero's packs came to **0.718**. Only a standing hero on the authored Descent never crowded its packs (closest **1.271**).

**The rule.** `PackMotion` (the only thing that moves enemies, in the scene and in `DescentSimulation`) now refuses any step that ends nearer than the spacing to *any* living enemy, whichever of the two is nearer the hero, unless the step widens a gap that is already too small. A step refused that way slides instead: it keeps only the part of its heading that runs along the spacing of every neighbour it could touch this step (two projection passes), so an enemy squeezing between two walks down the gap instead of glancing off each in turn. The slide is taken only if it still brings the enemy strictly nearer its own point and keeps it on the platform inside `HeroMotion.EdgeMargin`; otherwise the enemy waits and tries again on its next step, remembering nothing. A frame long enough to carry the fastest enemy more than `MaxStride` (0.25 units) is walked in equal sub-steps, at most `MaxSubSteps` (8); every ordinary frame is a single step, and a third of a second, the longest frame Unity hands a script, stays within the cap for every authored enemy. `IsStalled(i)` reports an enemy whose straight step was refused and whose slide brought it no nearer its point. Every buffer is sized for `PackLayout.MaxPackSize` when the pack is made: a step allocates nothing (measured 0 bytes over 1,200 steps of a ten-enemy pack under .NET; the Editor's Mono cannot measure it).

What this guarantees: two living enemies that start a body apart - as `EntrySides` spawns them - stay a body apart on every frame, at the rim, through deaths and at any frame time; two that start overlapping only ever move apart; every step an enemy takes brings it nearer its point, so a pack around a standing hero settles and comes to rest. What it does not guarantee: that an enemy never turns sharply (a fast Mite squeezing past a slow Grunt can be nudged a third of a texel one way and then the other, which is counted as a turn, not a flicker), or that an enemy heading exactly through a neighbour standing on its way ever gets round it while the hero stands still - the straight slide is exactly zero, as the one-sided rule's wait was, and with authored reaches this cannot arise.

**A side memory, built and dropped.** A version of the rule remembered which neighbour an enemy last slid round and on which side, and refused to switch sides until the enemy's point lay clearly round the other way. An adversarial review found that with the hero standing still this could freeze an enemy for as long as its neighbour lived, while a pack with no memory walked the same enemy to its point in 118 frames; measured against the memoryless rule it bought about twenty fewer turns over 24 proof runs and no fewer flickers. It was removed, and the frozen position the review found is now a test.

**Why the balance baseline did not move.** On the authored Descent a standing hero's packs never come within two strides of a body of each other (the fastest enemy, the Runner, walks 0.05 units a frame), so no step ever ends inside anyone's spacing, nothing waits or slides, and every enemy walks exactly the straight steps it walked before. A test pins that argument (closest gap at least 1.0, zero stall time, zero turns). Measured with the burst for all three weapons, every number is identical before and after in both the Editor and .NET, and the test holds the same argument without the burst.

**Measurement.** `DescentSimulation` gained observation-only numbers, which change no outcome: `EnemyStallSeconds` and `LongestEnemyStallSeconds` (time a living enemy that had not arrived was held back), `EnemyStepReversals` (a visible step, at least a quarter texel, taken back against the one before; the first step after standing still 0.1 s starts afresh) and `EnemyStepFlickers` (two of those within 0.1 s: stepping back and forth). The full tables, before and after, and the changed baselines are in `22`.

**Tests.** New `Tests/EditMode/PackSpacingTests.cs`, eight cases: a nearer enemy no longer walks into a farther one standing beside its path (the exact hole, reproduced with the walker nearer the hero at first contact); an enemy waiting behind another walks in once that one dies; two enemies that start overlapping only move apart and then stay a body apart; an enemy sliding round a neighbour when the hero stops still reaches its point (the review's frozen case); an enemy squeezing between two walks down the gap without a single visible turn back; ten enemies chasing a hero who walks away and back, turns round every 0.75 s, circles, and runs into the right corner and along the rim, with a death mid-chase - every living pair a body apart, every enemy on the platform and every position finite on every frame, no flicker once the hero stops and the pack at rest within thirty seconds; one 0.375 s frame equals four 0.09375 s frames bit for bit, and the spacing holds at 30 Hz, 144 Hz, through hitches up to a third of a second and through a five-second frame; stepping allocates nothing. `PackMotionTests`' ten-pack walk now checks every living pair on every frame instead of only nearer ones. `DensityProofTests` adds both proof floors x three weapons x standing, kiting, turning round and a rim loop (all clear, closest gap at least the spacing, no flicker, and no walking run holds an enemy back 3 s at a stretch) and the authored-Descent argument above. `DensityProofParityTests` adds a turning-round case per weapon, which proves the scene and the simulation agree under direction reversals - the hero's final position within 1e-3 - and checks the spacing in the scene itself, from the transforms, on every frame of all nine cases. `DescentSimulation.TurningRound()` is the shared script. Mutations checked against the suite: restoring the side memory, forbidding a step that widens a too-small gap, and a single projection pass each fail a test.

Files changed: `Scripts/Combat/PackMotion.cs`, `Tests/Support/DescentSimulation.cs`, `Tests/EditMode/PackMotionTests.cs`, `Tests/EditMode/DensityProofTests.cs` (also two stale comments), `Tests/PlayMode/DensityProofParityTests.cs`, `Tools/CombatChecks/CombatChecks.csproj`. Added: `Tests/EditMode/PackSpacingTests.cs` and its metadata. `EncounterController`, `Gameplay.unity`, art, HUD, camera, look/shadow views, weapons, authored floors and the proof floor asset are untouched.

**Not in this slice.** The hero still has no body enemies must walk round. `KiteRoute` itself zigzags frame by frame and an enemy standing at its point follows it: the only flicker left in any measured run (one, on the authored Descent with the Daggers kiting). The Editor's Mono and the pure .NET runner still disagree in the last bit of some float comparisons inside a crowded pack, so some crowded fights end differently on the two runtimes - 11 of 36 measured runs did before this slice, 14 of 36 after; standing authored runs agree on both. The phone check covered one scripted walking run; see `23`.

**Verified:** .NET **272/272**, compile **0 warnings/errors**, EditMode **284/284**, PlayMode **83/83**, Android build exit **0**, static integrity **293 unique asset/folder GUIDs / 555 scene objects/components**. Development APK **24,739,335 bytes**, SHA-256 **`24F1FD800362ACBDFB518C8F86D78367E3068FDF11271BF1C071F1A3476CABDA`**. Installed on the S23 with a matching hash and checked there with a standing Staff run and a scripted turning-round Daggers run: the pack kept apart, 59.9 fps while fighting; see `23`.

## Chest Unity sampling proof — 2026-09-18

`ArtDirection/2026-09-18/chest-unity-01/` records eleven actual Unity renders of the closed/opening/spent candidates in the ten-enemy scene, at .75 and .675 visible-box-width candidates and two portrait sizes. Temporary shared-canvas sampling only: no authored sprite import, scene save, gameplay change, APK or phone action. Prefer .75 for the next candidate. Front/behind hero staging exposes incorrect prop/body occlusion; resolve grounded depth sorting before runtime acceptance. Fixed pivot alone is not a finished depth solution. See README for sampling contract, scope and reproduction through `Tools/ArtReview/Run-ChestProof.ps1`.

Verified this slice: .NET 272/272, compile 0 warnings/errors, targeted graphics PlayMode capture 1/1, integrity 415 GUIDs / 555 scene objects/components. Full EditMode/PlayMode suites were not rerun; existing platform APK remains installed. Source art and prompts remain in `chest-proof-01`. Next: depth/ownership/import/opening integration and then coordinated device verification.

## Integrated foundry chest — 2026-09-18

Three shared imported 192-square ASTC4x4 sprites (closed/opening/spent), a ChestArtSet and one Gameplay scene reference replace the procedural prop when valid art is present. Common full-source registration, bilinear filtering, .75-unit reference box width; generated originals/prompts unchanged. Contact reward remains immediate/exactly once; the cosmetic .18-second opening respects pause. Reload borrows existing sprite/texture assets. GroundedSorting keeps each actor body/equipment/flash together at the floor root, while bars, shadows and ability ring retain independent global groups. This also changes actor/actor overlap presentation; no physical movement/spacing/balance rules changed.

Files: ChestSpawner, ChestArtSet, GroundedSorting, HeroLookView, EnemyLookView, ContactShadowView, AbilityRingView, imported Art/Props/FoundryChest, one scene reference, ChestPresentationTests/ArenaWalkTests and Tools/ArtReview chest import/capture scripts. See `ArtDirection/2026-09-18/chest-runtime-01/README.md` for reproduction and limits. Remaining: small generated frame drift, smooth lid animation, crowd occlusion and physical-device review; ability-ring artwork itself is unchanged.

Verified: .NET 272/272; compile 0 warnings/errors; EditMode 284/284; PlayMode 106/106; graphics capture 1/1; integrity 424 GUIDs / 555 scene objects/components; Android build exit 0. APK 24,751,592 bytes, SHA-256 `BA1F79545C8B3D70779A0380B2BD22A26BA0A8B88A3C09187B019DDCD852D814`. Two presentation tests cover immediate single reward, pause/resume, import format, common registration, independent ground/overlay groups and reload ownership. The first targeted test exposed an unsaved null art reference; importer now reloads the saved asset in the scene context and explicitly marks the scene dirty. A graphics check exposed the range ring inheriting the hero group; it now sorts independently.

Device status: NOT installed or phone-tested. An asynchronous device-availability question remains unanswered; no ADB input/capture/install or profile/telemetry mutation occurred in this slice. The previously installed platform APK is still the latest verified phone record. Do not claim this build's depth, performance or opening was verified on S23.

## Two development arms for the enemy-speed question — 2026-09-18

**Why.** `26_MOVEMENT_CADENCE_EXPERIMENT` Part 3 measured a candidate — the graded 2.3 walking speeds with enemy damage
at ×0.78 — and ended at two questions no simulation answers: whether enemies at 2.3–2.6 still read as different
archetypes, and whether being caught more often feels like pressure or like helplessness. This slice builds the content
that lets the candidate be played beside today's game, and nothing else. **The game is unchanged**: no authored enemy,
weapon, floor, scene, HUD, camera or art file was touched, and the candidate is not adopted.

**What was added.** Two development floors in `Resources/Development`, reached the same way the density proof floor is —
a floor id in `<profile folder>/development/start-floor.txt`, read only in the Editor and in a development build, so a
player's release build returns the authored floor before it looks at the file system. `Floor_KiteArmA`
(`floor_kite_arm_a`, "Kite Test A") runs today's enemies at Ember Halls' damage rate of 0.92. `Floor_KiteArmB`
(`floor_kite_arm_b`, "Kite Test B") runs six new definitions in `Data/Development` that copy their authored twins and
change exactly one field each, `_moveSpeed`: Tank 0.9 → 2.3, Warden 1.0 → 2.3, Captain 1.4 → 2.4, Grunt 1.6 → 2.5, Mite
2.2 → 2.6, Runner 3.0 → 3.2. The damage compensation is the floor's own rate, 0.7176 = 0.92 × 0.78, because
`FloorScaling` applies it as one uniform percentage modifier to every enemy weapon — the same arithmetic as scaling six
weapon assets, in one authored number instead of six.

Both arms run one shape: Approach and Heavy Ground (combat), The Post (elite), The Forge, Warden Gate (boss); eighteen
enemies over six waves, 1,135 enemy health, 99 experience, six level-ups. The forge offers only Mend, so the visit
cannot become a decision that differs between the arms, and equal experience means both arms offer the same upgrade
cards at the same moments — `UpgradeService` builds offers in pool order, not at random.

**Expected, from the simulation.** Damage taken standing / kiting, and the ratio between them: Sword 108.7 / 55.2 =
**1.97** on arm A against 103.8 / 73.2 = **1.42** on arm B; Staff 118.7 / 40.3 = **2.95** against 121.0 / 78.8 =
**1.54**; Daggers 98.3 / 59.6 = **1.65** against 97.3 / 69.3 = **1.40**. Standing is as hard on both arms (health left
31.3 / 21.3 / 41.7 against 36.2 / 19.0 / 42.7), which is the compensation working on a floor it was not fitted on. A
kiting Staff finishes arm A at 99.7 health, untouched; on arm B it finishes at 61.2. A kiting Daggers run takes 104.6 s
on arm A and 32.1 s on arm B. Full tables and the playing protocol are in `26` Part 4.

**Tests.** New `Tests/EditMode/ComparisonArmTests.cs`, three cases: both arms load through the development hook and
nothing but floors lives in `Resources/Development`; the arms run the same rooms, kinds, waves and enemy slots, with
equal health, gold, experience, weapon assets and prefabs, so `_moveSpeed` is the only difference, and arm A's speeds
are checked against today's authored numbers — which is how the experiment states that it changed no shipped data; arm
B carries 0.92 × 0.78 and neither arm has a modifier or a next floor. The first run of the suite caught my own
arithmetic: the arms send eighteen enemies, not the seventeen the test first claimed.

Files added: `Data/Development/` (six enemy definitions), `Resources/Development/Floor_KiteArmA.asset`,
`Floor_KiteArmB.asset`, `Tests/EditMode/ComparisonArmTests.cs` and their metadata. No file was changed.

**Not in this slice.** The candidate is not adopted; `Data/Enemies` keeps today's speeds and every pinned balance number
stands. The arms are proven as assets and in the simulation, not in the scene — no PlayMode case starts a run on them,
because `DensityProofParityTests` already proves the development hook reaches a floor in that folder. Nothing has been
played on a device: no install, no adb input and no profile or telemetry was touched in this slice.

**Verified:** .NET **272/272**, compile **0 warnings/errors**, EditMode **287/287** (284 before this slice), PlayMode **106/106**, Android build exit **0**, static integrity **434 unique asset/folder GUIDs / 555 scene objects/components** (424 before). Development APK **24,752,344 bytes**, SHA-256 **`48504C0A81369E4F4B575D98438B80505A6310C7DC37EFFD0D5145A5B4D26E60`**, built but **not installed**: the phone session waits for the owner's USB window, and both arms are unplayed.

## The hero's movement budget — 2026-09-18

**Why.** `26` Part 5 measured four levers against the complaint that running away always wins. Three of them act on what
the enemies do, and the owner played the best of those on the phone and reported running away *more*. The fourth acts
on what the hero can do: walking spends a bar, standing refills it, and an empty bar slows the hero instead of pinning
it. It is the only one that changes the cheapest way to play, and the only one that costs nothing in balance.

**The rule.** `Scripts/Combat/HeroStamina.cs`, pure and shared: `Bar` seconds of walking at full speed, `Refill`
bar-seconds restored for every second the hero really stands, and `EmptySpeed` of its speed once the bar is empty.
Shipped at 2 s, 1.5 and 0.4, the setting Part 5 recommends. Spending is measured on displacement, not on the steer that
was asked for, so a steer under the dead band or one into the rim costs nothing. Every wave starts with a full bar, and
only a live wave spends it: walking between waves, to a chest or across an empty room, is free.

`HeroMovementInput` reads `SpeedFactor` before walking and calls `Step` after, on the displacement that really happened,
fills the bar on `EncounterController.EncounterStarted` and spends only while `AliveEnemyCount` is above zero.
`DescentSimulation` does the same things in the same order and at the same moments, so the scene and the simulation stay
one rule. `CombatSetup` gained one accessor, `Encounters`, so the movement can hear the wave start without a scene edit.

**What it does, measured through the shipped code.** Damage taken, standing / kiting / back-off / stop-and-go, where
stop-and-go is a hero that retreats from contact but stands as soon as the bar is nearly dry:

| | Ten-enemy proof floor | Authored Descent |
|---|---|---|
| Sword, before | 48.6 / **18.8** / 24.3 / 24.3 | 149.6 / **55.0** / 73.0 / 73.0 |
| Sword, with the bar | 48.6 / 51.9 / 52.4 / **45.3** | 149.6 / 64.7 / 72.5 / **59.3** |
| Staff, before | 35.9 / 19.3 / **15.5** / 15.5 | 151.1 / **15.9** / 36.0 / 36.0 |
| Staff, with the bar | 35.9 / 39.2 / 28.7 / **18.8** | 151.1 / 39.7 / 76.6 / **35.0** |
| Daggers, before | 47.5 / **27.0** / 48.6 / 48.6 | 146.3 / **107.5** / 128.8 / 128.8 |
| Daggers, with the bar | 47.5 / 59.1 / 41.4 / **38.1** | 146.3 / 126.1 / 100.6 / **98.6** |

The bold cell is the cheapest route available. Before the bar it is "run the whole fight" in five of the six; with it,
"stop and go" in all six. The Staff's free ride, 15.9 damage for a whole two-floor Descent, becomes 35.0. The worst
pacing case in the project, the Daggers' kiting Descent, falls from 132.4 s to 73.8 s.

**Why no balance number moved.** A hero that stands still never spends any, so every standing fight is the fight it
was, frame for frame: the standing column above is identical before and after, and a test runs both Descent cards and
all three weapons against a bar nothing can empty and asserts equality of health, damage, seconds, kills and floors.
This is what the graded speed set of `26` Part 3 could not offer; it needed a damage pass over six enemy assets and
still moved the baseline.

**What the budget uncovered: two distance functions.** The first gate failed one parity case of 106, the Sword kiting
on the proof floor, with the scene's hero 0.044 units from the simulation's after 13 s. A frame-by-frame, bit-exact
diff of both walks (hero, bar, health, cooldown, every enemy's position, health and attack count) found them identical
for 533 frames and then different in one field: the hero's target. A Grunt stood 1.4 units straight across and a Mite
1.4 units diagonally, and `Targeting` struck the Mite while `DescentSimulation.Acquire` struck the Grunt. The two
copies of `dx * dx + dy * dy`, compiled in two assemblies, were evaluated differently by the Editor's JIT and disagreed
about the last bit: one saw the Mite nearer, the other a tie, which the earlier slot wins. (The pure .NET runner sees
the Mite nearer for both; the Editor now sees a tie for both - the runtimes differ, as `22` already records, but within
a runtime the two callers no longer can.) The slowed hero made the pack bunch up, which is what produced a tie close
enough to expose it. With a bar too long to empty, the case passes exactly, so nothing in the budget's own bookkeeping
was wrong.

The fix is `ArenaFloor.FloorDistanceSquared`, one never-inlined method that every reach, splash and target decision in
both the scene (`Targeting`) and the simulation (`Acquire`, `Nearby`, `WithinReachOfHero`, `IsHeroInReach`) now
measures through. One body cannot disagree with itself. `TargetingParityTests` pins, on the floats of that frame, that the body
answers nearer-or-tied in either runtime and the same float every time, and that measuring after the world halving
changes no bit.

**Tests.** New `Tests/EditMode/HeroStaminaTests.cs`, nine cases: the bar drains exactly the seconds it walks and never
below empty; standing pays back at the refill rate and never past the bar; full speed until empty and the empty share
after, with one frame of standing enough to walk again; a spent hero covers exactly its share of a fresh hero's
distance over the same seconds, walked through `HeroMotion`; the bar steps the same whatever the frame length; changes
are reported only when the bar really moves; impossible settings and frames are rejected; a standing hero's Descent is
untouched; and a hero that runs the whole fight runs dry and pays more than half as much again in damage. New
`Tests/EditMode/TargetingParityTests.cs`, two cases, above.

`DensityProofTests` keeps its spotless flicker claim for every run that never ran dry and bounds a dry run at three,
with both runtimes' measured counts and the reason in the comment: a hero slowed to `EmptySpeed` is caught, and an
enemy standing at its point follows `KiteRoute`'s frame-by-frame zigzag - the known last flicker of the spacing slice,
now reachable on the proof floors. It is the slowdown and not the bar that does it, measured by keeping the bar and
removing the slowdown, which returns every one of those runs to its pre-budget flickers, gap and damage.
`ArenaWalkTests` no longer walks to the rim on a stopwatch, because how long crossing the arena takes now depends on
the budget; it walks until the hero reaches the corner, with a deadline.

Files changed: `Scripts/Combat/ArenaFloor.cs`, `Scripts/Combat/Targeting.cs`, `Scripts/UI/HeroMovementInput.cs`,
`Scripts/Core/CombatSetup.cs`, `Tests/Support/DescentSimulation.cs`, `Tests/EditMode/DensityProofTests.cs`,
`Tests/PlayMode/ArenaWalkTests.cs`, `Tools/CombatChecks/CombatChecks.csproj`. Added: `Scripts/Combat/HeroStamina.cs`,
`Tests/EditMode/HeroStaminaTests.cs`, `Tests/EditMode/TargetingParityTests.cs` and their metadata. Enemy data,
weapons, authored floors, the scene, the camera and the art are untouched.

**Not in this slice.** **The player cannot see the bar.** `HeroStamina.Changed` and `HeroMovementInput.Stamina` are the
seam a view binds to, but no HUD element was added, because the HUD is the art owner's. Until it exists the mechanic is
invisible, and an invisible budget is a bad one: a player who cannot see it can only learn it by being slowed. Nothing
has been played on a device. The two Kite Test arms still carry the enemy-speed candidate, not this; feeling the budget
on the phone needs this build, not an asset swap.

**Verified:** .NET **291/291**, compile **0 warnings/errors**, EditMode **306/306** (287 before this slice), PlayMode **106/106**, Android build exit **0**, static integrity **438 unique asset/folder GUIDs / 555 scene objects/components**. Development APK **29,150,299 bytes**, SHA-256 **`C6ADAFEF15FD05711B4DCAB67CE79516D38E67FB6C726BB02DD2B93A8B53C113`**, built but **not installed**. That build came out 4.4 MB larger than the morning's from a script-only change; the next build, an hour later, was back to 24,752,344 bytes, so it was transient and not something this slice introduced.

## The movement bar on the HUD — 2026-09-18

The budget shipped in the slice above with no way for the player to see it, which made it a bad mechanic: a hero that
slows down for a reason nobody is shown can only be learned by suffering it. This adds the bar.

**What the player sees.** A thin bar directly under the health bar, at the same width and left edge as the health and
experience bars, drawn from the same shared square sprite with the same track-and-fill recipe: a dark track with one
stretched Fill child. The fill is brass, so it reads apart from the health red above it and the experience blue below.
Neither image takes a touch.

The fill alone cannot say why a hero is slow, because an empty bar has no fill left to look at. So the **track** carries
that: dark and cool while there is anything to spend, dull ember once there is not. That is the whole visual language of
the mechanic - one length and one background colour.

**How it is driven.** `Scripts/UI/HeroStaminaView.cs` binds to `HeroStamina.Changed` and sets `fillAmount` and the track
colour. It works on the bar's own change event, never per frame, the way the rest of the HUD does, and it reads the
budget rather than owning it: `HeroMovementInput` still spends it and `DescentSimulation` still mirrors it, so nothing
about the fight changed. `Fill` and `Track` are exposed for tests.

**The scene.** `Gameplay.unity` gained one object, `Stamina Bar`, under the HUD canvas's Safe Area, next to the health
bar, with its Fill child and the view wired to the hero's movement input. It was made by a temporary Editor builder run
in batch mode and deleted straight after, as scene edits in this project are.

**Tests.** `ArtHudFlowTests` gains one case: a wave starts with a full bar, neither image is a raycast target, holding a
walk past the bar's length empties the fill and changes the track, and standing still fills it again and puts the track
back. The hero's weapon is switched off for it, so the wave cannot end mid-test and refill the bar for a reason other
than standing still.

Files changed: `Assets/_Project/Scenes/Gameplay/Gameplay.unity`, `Tests/PlayMode/ArtHudFlowTests.cs`. Added:
`Scripts/UI/HeroStaminaView.cs` and its metadata. No combat rule, no data and no other art moved.

**Not in this slice.** The bar is always on screen; it does not fade when full, and nothing animates when it empties. No
sprite or font was authored - it reuses the square every other bar uses - but the two colours are a choice, and the
placement crowds the lower surface a little, so both are worth a look by whoever owns the art. Still nothing has been
played on a device.

**Verified:** .NET **291/291**, compile **0 warnings/errors**, EditMode **306/306**, PlayMode **107/107**, Android build exit **0**, static integrity **439 unique asset/folder GUIDs / 564 scene objects/components** (555 before: the bar, its fill and their components). Development APK **24,752,344 bytes**, SHA-256 **`D77FFFA6D385BBB017F9EBCBA8C9D6A7271250F5318E4021A8FD9F7BFA6DEED2`**, built but **not installed**. The bar was also rendered from the real scene in the Editor, full, part spent and empty, through a temporary PlayMode capture that was deleted after it ran; the frames are in `TestResults/stamina-bar/` and are not committed.

## Movement HUD visual hierarchy — 2026-09-19

The bottom panel now separates health, labelled MOVE and quieter XP into distinct rows. Full movement uses reduced visual emphasis; a nonempty-to-empty transition gives one 0.24-second scaled-time ember track pulse. Pause freezes it and refilling cancels it. The fraction is exact; HeroStamina values, combat, enemy density, camera and ability rules are unchanged.

Implementation and seven actual Unity renders at two portrait sizes: ArtDirection/2026-09-19/movement-hud-01/README.md. Runtime files: HeroStaminaView, Gameplay HUD layout; regression: ArtHudFlowTests. Reproduction: Tools/ArtReview/Style-MovementHud.ps1 and Run-MovementHudProof.ps1. No new raster generation. Device review remains pending coordination; no phone operation occurred. See the proof README for final verification and build identity.

Verified: .NET 291/291; compile 0 warnings/errors; EditMode 306/306; PlayMode 108/108; isolated graphics capture 1/1; integrity 439 asset/folder GUIDs and 568 scene objects/components. Android build exited 0. Actual APK file: 29,151,985 bytes; SHA-256 `97A5F7511F0968E43AAF785BAAC2F0B8D654F91905CE4C858DECBACA3175A7AA`. Not installed or phone-tested in this slice. Full tests cover existing gameplay and the added depletion/pause regression; they do not establish physical-device readability or performance.

## Ability ring comparison — 2026-09-19

ArtDirection/2026-09-19/ability-ring-01/ contains eight actual Unity stills comparing the existing dashed marker, a continuous contour and the preferred strengthened eight-arc seal, with cooling, shorter portrait and rim views. The first seal was too faint; its rejected capture is retained. Source is deterministic geometry in Tools/ArtReview/AbilityRingCapture.cs, staged only by Run-AbilityRingProof.ps1. No image generation, authored asset/scene save, gameplay change, new APK or device operation beyond ADB enumeration. The contour centreline maps to the actual ability radius; candidates preserve the clear centre and independent ground sorting. Cooling is a frozen alpha study, not live timing evidence. Next: bounded shared ownership/runtime integration, movement/cooldown verification and coordinated device review. See README for exact geometry, reproduction and limits.
Verified: .NET 291/291, compile 0 warnings/errors, graphics PlayMode capture 1/1, integrity 439 GUIDs / 568 scene objects/components. No runtime Assets change; full EditMode/PlayMode and APK build were not repeated. The previous movement-HUD full gate (306/108) and APK remain valid as the unchanged runtime baseline, not a test of an integrated new ring.

## Movement HUD device acceptance — 2026-09-19

Installed on S23 and verified base.apk SHA256 `97A5F7511F0968E43AAF785BAAC2F0B8D654F91905CE4C858DECBACA3175A7AA`. In normal Daggers combat, HUD separation, spend/refill/full emphasis and the single depletion pulse were observed; empty stays visible during a choice. Fresh primary/backup profile hashes unchanged, revision150/gold11596/Daggers/Second Wind. Final observed Cinder Walk2/6 level4 choice,56HP,23run gold. No code/build change or additional suites; prior full gate applies. See newest Docs/23 and movement-hud-01/device for evidence and limits. This supersedes pending-phone statements; ability-ring geometry remains proof-only.

## Integrated eight-arc ability ring — 2026-09-19

AbilityRingView now borrows one session-owned 512x256 bilinear RGBA32 sprite from AbilityRingArt. Unit-radius contour at 240 PPU is scaled by the actual ability radius; transparent padding is not gameplay reach. Repeated scenes reuse the native art, subsystem reset and quit release it. Existing ready/cooling alpha, scaled-time breathing, ground sorting and hero following remain. No scene, damage, cooldown, density, targeting or movement-budget change. Old placeholder factory remains unused by this view.

Two new AbilityFlowTests cover real reload reuse, hero following, paused colour stability, immutable texture contract and native reset/recreation; the reach assertion now checks the contour centreline. Four real integrated Unity captures include two portrait sizes, rim and actual ability use/cooling. Source/reproduction/limits: ArtDirection/2026-09-19/ability-ring-runtime-01/README.md. Final verification and device status are recorded there and in latest Docs/23. This supersedes proof-only status for the ring implementation, but not earlier device records until an installation is recorded.

Verified: .NET291/291; compile0 warnings/errors; EditMode306/306; PlayMode110/110; graphics capture1/1; static integrity440 asset/folder GUIDs and568 scene objects/components. Android exit0, actual APK29,152,854 bytes, SHA256 `300759ECC845E8CB12FA9988E6BC6FF481E9C96ABF703CB63DBE1E3A30D76ED6`. Installed on S23; installed base.apk hash matched. Device evidence and limits follow in Docs/23 and the runtime README.

## Ability cast pulse — 2026-09-19

CombatEffectsView now borrows the shared eight-arc range sprite for a pooled, ground-sorted cast pulse. Starts at60% radius, expands to100% by0.14s and expires at0.21s, preserving old total duration and immediate damage. Fade/expansion use scaled time; the visual remains at cast origin. Pool reuse clears pulse state; no per-cast art allocation. Staff splash and other effects unchanged. New AbilityFlowTests case covers origin, radius, pause, expiry and ownership. Seven actual Unity frame captures and reproduction: ArtDirection/2026-09-19/ability-pulse-01/README.md. Full gate and phone evidence will be recorded there and in Docs/23.

Verified: .NET291/291; compile0 warnings/errors; EditMode306/306; PlayMode111/111; graphics capture1/1; integrity440 GUIDs /568 scene objects/components. Android exit0, actual APK29,153,822 bytes, SHA256 `66CE2B7B2CB39E273C2DDC35B5795EEA70E518B65FF40BD50F0A00CD87D94165`. Installed S23 base.apk hash matched.

## Staff splash visual proof — 2026-09-19

Ten real Unity renders in ArtDirection/2026-09-19/staff-splash-01 compare the current pixel blast with four tapered violet-white arcs below actors, at four stages and two portrait sizes. Preferred candidate has a clear centre and distinct shape from the ability's eight-arc seal. Isolated view-subscriber invocation and temporary pool frame substitution only; no weapon damage, authored Assets/scene, gameplay or phone change. Impact size retains the old shown fraction (1.75 units for Staff), not the full3.5 damage radius. Keep0.21s lifetime; production must use one shared contour with scale/alpha animation, not the proof's seven textures. Tools/ArtReview/Run-StaffSplashProof.ps1 reproduces the sample.

Verified: .NET291/291, compile0 warnings/errors, graphics capture1/1, integrity440/568. Full suites/APK were not rerun for tooling-only changes; latest installed ability-pulse APK66CE2B7B2CB39E273C2DDC35B5795EEA70E518B65FF40BD50F0A00CD87D94165 remains unchanged. No phone/profile/telemetry access. Next: bounded runtime ownership/pool/pause integration and full gate/device review. See artifact README for timing, staging and evidence limits.

## Shared Staff impact integration — 2026-09-19

CombatEffectsView now borrows one StaffSplashArt512x256 unit-radius contour, with four tapered violet-white arcs and no per-hit native allocation. Existing pooled renderer grows24/38→1 of the previous shown radius in0.14s, fades over the final0.11s and expires at0.21s. Target origin remains fixed; scaled time honours pause; ground order0 places the effect below bodies. Immediate damage, Staff3.5 splash reach, authored impact fraction/clamp and attack cadence remain unchanged. The ability pulse shares the animation path with its previous0.6 start/0.85 alpha retained; pool reuse clears both flags.

Files: StaffSplashArt.cs/meta, CombatEffectsView.cs, StaffSplashTests.cs/meta, runtime capture/runner/converter, ArtDirection/2026-09-19/staff-runtime-01. Two tests cover actual attack damage/origin/pause/radius/expiry and texture reload/reset ownership. Runtime visual captures use an actual attack. The proof's seven textures are not shipped. Verification/device results follow in the artifact README and Docs/23.

Verified: .NET291/291, compile0 warnings/errors, EditMode306/306, PlayMode113/113, graphics1/1, integrity442/568, Android0. APK29,153,541 bytes SHA2567A107BD2F871D2EC8AE660BFC7ABE565D25095EF47BB8E15B6F95CB49AD42279 installed and matched on S23. Actual Staff screenrecord verified; two proof wins banked20, owned Staff equipped and Daggers restored through UI, flag removed, normal Ember Hall returned. Final profile154/11616gold/Daggers/Second Wind, unlocks/deepest2 intact. Full evidence/limits in staff-runtime-01/README.md and Docs/23.

## Staff weapon source study — 2026-09-19

Built-in image_gen produced a blue-steel/brass/violet Staff candidate and a targeted exterior-glow revision, saved unchanged in ArtDirection/2026-09-19/staff-weapon-01 with exact prompts, hashes, alpha measurements and a browser proportion/grip study beside the existing Vanguard. Prefer v2 for Unity sampling. Small alpha<=16 stray pixels make naive nonzero-alpha fitting unsuitable; source alpha is preserved. Grip(512,940),170–210CSS-pixel source heights and±14-degree lean are provisional browser placements, not verified runtime anchors. Existing shaft/orb split, cast-lit orb, finger occlusion and rear/mirrored continuity still need reconciliation.

No runtime Assets/code/scene/APK or phone operation. No game suites repeated for source art; installed Staff-impact build7A107BD2 remains current. Next: fixed-registration real Unity idle/walk/cast sampling, then shared import and phone verification. See artifact README/PROMPTS for full scope and provenance.

## Painted Staff equipment integration — 2026-09-19

VanguardArtSet now borrows the fixed-registration Staff master through one optional shared 24x183 sprite at 128 PPU, bilinear, non-readable, Android ASTC4. VanguardAnimator follows existing right-hand anchors, keeps the whole crystal/shaft rigid during the unchanged cast lean, hides the old orb, extends the front knuckle overlay to Staff and places rear Staff behind the body. Art sets without Staff keep the procedural fallback. The crystal keeps its painted light; the old independent lit-orb swap is not shown. No scene, combat, movement, balance or camera change.

Source, registration, four-facing Unity captures, reproducible tooling and limitations: ArtDirection/2026-09-19/staff-weapon-runtime-01/README.md. New VanguardAnimationTests case verifies grip/depth through four facings, no duplicate orb, real cast, pause/reset and shared asset survival after reload.

Verified: .NET 291/291, compile zero warnings/errors, EditMode 306/306, PlayMode 114/114; Android exit 0. APK 24,752,344 bytes, SHA-256 `4A73EE71112FE26005D97A9AD4AE0E986C9A6EF4AD7F8E4ED434D1AB86EF2E42`; installed S23 base.apk matched. Primary/backup profiles unchanged. Physical in-game review awaits phone availability; installation is not a visual phone test. See latest Docs/23.

Final graphics capture: 1/1, including four observed cast-lean frames (53 renders total). Static integrity: 443 unique project GUIDs, 1099 package GUIDs, 568 scene objects/components.

Staff device follow-up completed (2026-09-19): owner handed over S23; normal-floor movement/front/rear/cast reviewed in 12-second video, no visible staff/crystal separation. Profiles unchanged. Left level2 choice,80HP,10run gold,XP27/36. Latest Docs/23 and device evidence supersede pending review; no new build/code/test run.

## Daggers source study — 2026-09-19

ArtDirection/2026-09-19/daggers-weapon-01 contains one unchanged built-in imagegen master, exact prompt, alpha measurements, hash and browser two-hand proportion study. Broad silver/blue-steel blade and brass guard match Vanguard; one source is intended for both hands to avoid design drift. Corner alpha0; faint exterior alpha<=16 means no naive alpha-auto-fit. Provisional grip628,975 on1254-square master. Browser comparison64px/35degrees and56px/45degrees/mirrored passed; returned to64/35. Finger masking, rear depth and real .12-unit thrust are not yet validated with this source.

Verified: static integrity443 project GUIDs/1099package GUIDs/568scene objects. Source and hero images rendered in the browser; size/angle/mirror controls checked. No Assets/C# runtime/scene changes, Unity suite/build or phone operation. Installed Staff build4A73EE71 remains current. Next: fixed-registration Unity sampling for both hands, grip/depth/thrust review, then shared import and device verification.

## Daggers registered Unity proof — 2026-09-19

Tools/ArtReview/Run-DaggersWeaponProof.ps1 captured 57 actual scene renders with an isolated profile: four facings/gait, actual thrust and anchored contact alternatives, portrait room. One source sampled from fixed(438,10,380,1220) to20x64/128PPU, pivot(.5,255/1220). No authored Assets/scene/runtime import or device change.

Finding: existing .12-unit weapon-only upward thrust separates the new hilt from the neutral hand. Prefer the illustrated anchored alternative (both grips at authored hand anchors, blades behind body/gauntlets, modest outward pivot) for subsequent integration, subject to full animation review. This temporary render substitution is not a shipped animator. Old large pixel hit effect remains. See ArtDirection/2026-09-19/daggers-unity-01/README.md for scope/limits.

Verified: .NET291/291, compile0 warnings/errors, graphics PlayMode fixture1/1, static integrity443project GUIDs/1099package GUIDs/568scene objects. No full runtime suite/APK repeated because no runtime changes. Browser comparison rendered correctly. Next: shared optional Daggers import with contact-preserving presentation, pause/reset/reload and real motion verification, then full gate/device. Installed Staff build4A73EE71 remains current.

## Imported Daggers equipment — 2026-09-19

VanguardArtSet gains one optional shared dagger sprite (fixed20x64/128PPU, bilinear, non-readable, ASTC4 on Android). Both renderers use it; VanguardAnimator follows each hand without translational drift, adds a small outward pivot on actual hit and draws blades behind the gauntlets. Procedural fallback keeps its old motion. No scene, balance, critical rhythm, range, timing, movement or camera changes.

New VanguardAnimationTests case covers both hand anchors through four directions, immediate real attack/angular reaction, pause, recovery, death hiding and borrowed sprite survival across reload. Reproducible import and actual runtime capture tools are in Tools/ArtReview; 53 images are in ArtDirection/2026-09-19/daggers-runtime-01. The source master remains unchanged. The old large pixel hit effect and final arm animation remain open.

Verified: .NET291/291, compile0 warnings/errors, EditMode306/306, PlayMode115/115, actual runtime graphics1/1, integrity444project GUIDs/1099package GUIDs/568scene objects; Android exit0. APK24,752,344bytes SHA256 `62D5A2707299AB8865B581ED3B5FD69F135EB727E8D562362F997B7F6055D603`; installed S23 base.apk matched. No live phone visual test yet: phone was in another app and availability question has no reply. Profiles unchanged; newest Docs/23 records details. Existing full suite counts supersede earlier proof-only checks.

## S1 — the offer engine and the run seed — 2026-09-19

**Why.** docs/27 redraws the roadmap around Megabonk and Bones and Coins; S1 is its first slice, and the handoff
prompt's first priority. It changes nothing a player can see, because the pool still holds two cards and the game
offers two, and that is deliberate: it is the foundation the stat sheet (S2), books from chests (S3) and Forge Pulse
(S5) stand on, and it makes every offer reproducible from one recorded number. Every pinned balance figure is
bit-identical by construction.

**The rule.** `RunState` carries a `Seed`. `RunRandom` (`Scripts/Progression`) is the run's only source of randomness:
a SplitMix-style generator over 32-bit unsigned integers, integer arithmetic only, so Mono, .NET and IL2CPP cannot
disagree about a draw. Streams are derived, never consumed in sequence — `RunRandom.Stream(seed, purpose, index)` —
with a purpose constant per consumer (`Offers` now, `Chests` reserved) and the consumer's own index, so a chest opened
between two level-ups never moves the second one. `NextBelow(n)` is unbiased.

`UpgradeService.CreateOffer()` builds the eligible list in pool order: below `MaxStacks`, and if the card names a
prerequisite (`UpgradeOption.RequiresId`, `UpgradeDefinition._requires`), that card holds a stack. An eligible list no
larger than the choice count is offered whole, in pool order, consuming no randomness; otherwise `choiceCount` distinct
cards are drawn without replacement from the stream `(seed, Offers, offerIndex)`. `UpgradeOffer.Index` counts the
run's offers from zero; `UpgradeService.LastSelected` remembers the offer a pick came from.

The scene seeds the run in `CombatSetup`: from `<profile folder>/development/run-seed.txt` when it exists (Editor and
development builds only, same rules as `start-floor.txt`; a whole number, or any text hashed — `RunSeeds.Parse`), else
`RunSeeds.Fresh()` from the clock and uptime. `DescentSimulation.Run` takes `seed` and `preferIds`, a card policy by id
that survives a pool larger than the choice count; a `CardPicker` replaces the bare slot inside the simulation.

**Telemetry.** `run_start` gains `seed`; `upgrade_offered` and `upgrade_selected` gain `offer_index`. No profile change,
no save-format change.

**Tests.** `RunRandomTests` (six): the same stream yields the same words; three streams' first four words pinned as
numbers; six known draws below six pinned; streams torn apart by purpose, by index and by seed; five thousand draws
below five stay in range and land within a tenth of even; a count of one consumes nothing and bad arguments are
refused. `OfferEngineTests` (eight, over a synthetic pool of five with two choices): the same seed shows the same cards
at every level; different seeds differ; an offer never repeats a card and only shows eligible ones, over forty seeds;
a maxed card leaves the pool and a prerequisite gates its dependant; a pool no larger than the choice count is offered
whole in pool order whatever the seed, and still counts its index; a draw on the chest stream between two offers moves
neither; the selected offer is remembered; a card cannot require itself. `UpgradeTests` re-states one claim: two
eligible cards for one slot is now a draw, one card from the pool and the same card for the same seed, where it used
to be the first in pool order. `RunTelemetryTests` re-pin the new fields. `DensityProofParityTests` writes
`run-seed.txt` into its profile folder, hands the same seed to the simulation and asserts the scene took it, so the
plumbing is proven end to end even while nothing is drawn.

Files changed: `Scripts/Core/RunState.cs`, `Scripts/Core/DevelopmentStart.cs`, `Scripts/Core/CombatSetup.cs`,
`Scripts/Progression/UpgradeOption.cs`, `UpgradeOffer.cs`, `UpgradeService.cs`, `Scripts/Content/UpgradeDefinition.cs`,
`Scripts/Analytics/RunTelemetry.cs`, `RunTelemetryContext.cs`, `Tests/Support/DescentSimulation.cs`,
`Tests/EditMode/UpgradeTests.cs`, `RunTelemetryTests.cs`, `Tests/PlayMode/DensityProofParityTests.cs`,
`Tools/CombatChecks/CombatChecks.csproj`. Added: `Scripts/Progression/RunRandom.cs`, `Scripts/Core/RunSeeds.cs`,
`Tests/EditMode/RunRandomTests.cs`, `OfferEngineTests.cs` and their metadata. No scene, data, art or HUD change.

**Not in this slice.** Rarity: a rarity that scales nothing would be a lie on a card, so magnitudes wait for S2's stat
sheet, where each card defines its tiers. New cards, stats beyond Damage and AttackSpeed, chests, Luck. Exact run replay
from the seed is not claimed and never will be: the finger is not seeded. `SimulationParityTests` still runs the
authored Descent without a chosen seed, which is correct while nothing is drawn and must change with S2.

**Handoff for Astra (contracts that changed).** Upgrade ids are unchanged: `upgrade_damage`, `upgrade_attack_speed`.
Two telemetry events grew a field, `offer_index`, and `run_start` grew `seed`; nothing was renamed. `UpgradeOffer` has
an `Index`; `UpgradeService` has `LastSelected`; `UpgradeDefinition` has an optional `Requires` reference an author can
set in the Inspector. No timing or presentation changed. Future visual needs, none blocking: rarity on the choice cards
(S2), a book inside the chest's opening frames (S3), a swarm clock and a leave prompt (S4), a stat readout on the pause
screen (S2).

**Verified:** .NET **307/307**, compile **0 warnings/errors**, EditMode **322/322**, PlayMode **115/115**, Android build exit **0**, static integrity **449 unique asset/folder GUIDs / 568 scene objects/components**. Development APK **29,157,379 bytes**, SHA-256 **`2F5311CA802B17573D789A67EF774E07A7D33386BEFDB6C6A0DDFDA9FC24C240`**, built, **not installed**. Note on size: builds since 2026-09-18 have alternated between about 24.7 and 29.2 MB with no corresponding source change; the earlier note calling the larger size transient was premature, and the cause has not been investigated.

## S2a — the stat sheet a card can reach — 2026-09-19

**Why, and why it is half a slice.** docs/27's S2 was one slice for seven stats plus rarity. Splitting it was the right
call once the code was read: the plumbing (a card that targets something other than damage or cadence) is independent
of the balance work (authoring those cards, putting them in the pool, and re-pinning what that moves), and rarity
without magnitudes to vary is a label that lies. So **S2a is the plumbing**: six stats exist, each reachable by a card,
each proven to change the thing that reads it, with the game's pool still holding its two cards so **no authored number
moves**. S2b will author the cards, widen the pool, measure distributions across seeds and re-pin what changes. Luck
goes with rarity into S2b, because luck without rarity has nothing to bite.

**The stats.** `WeaponStat` is renamed `UpgradeStat` — the same serialized values, so the two upgrade assets keep their
meaning — and gains six entries. Four are the weapon's, because that is where they are used: Damage, AttackSpeed, Range
and CritChance on `WeaponRuntime`. Four are the hero's, on a new `HeroStats` sheet: MaxHealth, Armor, MoveSpeed and
StaminaBar. `UpgradeService` routes a card by `HeroStats.Owns`, so one card type reaches either.

- **Range** joins damage and cadence as a `ModifiableStat` on the weapon, with a safety floor of 0.1.
- **MaxHealth** raises `HealthState.Maximum`, and what it adds arrives filled, so the card is worth taking mid-fight.
- **Armor** is a pool, not a percentage: reduction = armor / (armor + 100), so 50 armor takes a third off a hit, 100
  takes half, and no amount ever takes all of it. A hard cap would make the last points worthless without saying so;
  this says so in its shape. It is taken off *before* anything observes the hit, so views, relics and the movement
  report all read the damage that actually landed.
- **MoveSpeed** and **StaminaBar** set `HeroMotion.Speed` and `HeroStamina.Bar`; the bar's new length arrives filled,
  since a longer empty bar would do nothing until the hero next stood still.
- **CritChance** is a chance on top of the authored rhythm, drawn from `RunRandom.Stream(seed, Crits, attackNumber)`.
  The rhythm is checked first and never spends a draw, so the Daggers' every-third-strike is untouched and a weapon
  with no chance never draws at all. A rolled crit doubles, the multiplier the Daggers already use, since a weapon
  whose pattern authors no crits has none to borrow.

**One rule for both sides.** `HeroStatsBinding.Apply` pushes the sheet into the health, the walk and the bar. It sets
absolute values, never deltas, so calling it twice changes nothing the second time and the scene and the simulation
cannot drift apart by applying a card a different number of times. `CombatSetup` calls it on `HeroStats.Changed`;
`DescentSimulation` does the same, in the same order, and builds its sheet from the bar the run actually carries so a
caller that injects its own budget is not overwritten.

**Tests.** New `Tests/EditMode/HeroStatsTests.cs`, eleven cases: a range card lengthens reach; a maximum-health card
raises the ceiling and arrives filled; armor takes a share of every hit and a million armor still never takes all of
it; what observers are told is what landed; a move-speed card walks the hero exactly its share further over a second,
through `HeroMotion`; a stamina card lengthens the bar and fills what it added; a crit-chance card crits on the same
swings for the same seed, differs across seeds, and lands near its stated chance over four hundred swings; with no
chance nothing is rolled and the Daggers' authored rhythm is exact; a hero card cannot write to the weapon nor a weapon
card to the hero; applying the same sheet three times is one application; and every safety floor holds against a card
that subtracts more than the stat has.

Files changed: `Scripts/Progression/WeaponStat.cs` → `UpgradeStat.cs` (renamed, six entries appended),
`UpgradeOption.cs`, `UpgradeService.cs`, `RunRandom.cs`, `Scripts/Combat/WeaponRuntime.cs`, `HealthState.cs`,
`HeroMotion.cs`, `HeroStamina.cs`, `Health.cs`, `Scripts/Core/CombatSetup.cs`, `Tests/Support/DescentSimulation.cs`,
`Tools/CombatChecks/CombatChecks.csproj`, and every file naming the old enum. Added:
`Scripts/Progression/HeroStats.cs`, `HeroStatsBinding.cs`, `Tests/EditMode/HeroStatsTests.cs` and their metadata. No
scene, data, art or HUD change, and no upgrade asset changed.

**Not in this slice.** No card in the game targets any of the new stats yet, so nothing a player can see has changed
and no balance number moved — deliberately. Rarity, Luck, regeneration and crit damage are S2b or later. Armor is on
the hero only; enemies have none and the field defaults to zero for them.

**Handoff for Astra.** `WeaponStat` is now `UpgradeStat` with the same values; no upgrade asset changed and no id moved.
`UpgradeDefinition` can target the new stats in the Inspector, but none does yet. `CombatSetup.HeroStats` and
`Health.State` are new public surfaces. No timing, presentation or art contract changed. Coming visual needs, none
blocking: rarity on the choice cards and a stat readout on the pause screen (S2b).

**Verified:** .NET **318/318**, compile **0 warnings/errors**, EditMode **322/322**, PlayMode **115/115**, Android build exit **0**, static integrity **452 unique asset/folder GUIDs / 568 scene objects/components**. Development APK **29,160,078 bytes**, SHA-256 **`78ABE0F2A89FA11E8F5D9F60A557EB9CD23E564FB769C42A4D5649B1FA733062`**, built, **not installed**. EditMode and PlayMode counts are unchanged from S1 because the new cases are pure and run in the .NET suite as well, which is where the count rose from 307.

## S2b — rarity, luck, and how wide a pool the run can carry — 2026-09-19

**What shipped.** Three rarity tiers, luck, nine authored cards, and a pool of **five** of them in the game: Tempered
Edge, Quickened Grip, Long Reach, Keen Edge and Vital Surge. A level-up now draws two of five and rolls a tier for
each, so two runs differ. The other four cards - Iron Skin, Swift Boots, Deep Lungs and Fortune - are authored,
mirrored in the simulation and deliberately out of the pool; the measurement below says why.

**Rarity.** `UpgradeRarity` is Common, Rare, Epic - three, not Megabonk's five, because a level-up shows two cards on a
phone and a scale a player cannot tell apart at a glance is a label rather than a choice. A card carries a magnitude
per tier and `UpgradeOffer` carries the tier each card was offered at, beside the list rather than inside it, so the
twelve places that read an offer still read `UpgradeOption`. Weights are 70/25/5 out of a hundred, drawn from the same
stream as the cards and **after** them, so changing the tier weights can never change which cards a seed shows.
Measured over 8,000 offered cards: 70.2 %, 24.9 %, 5.0 %.

**Luck** is a hero stat that touches nothing in combat. It lowers the common tier's weight - `70 * 10000 / (10000 +
luck*10000)`, in integers so every runtime draws the same tier - and the other two keep theirs, so they take a larger
share of what is left. It can never reach zero, so a common card is always possible. Fortune is the card that grants
it, and it waits outside the pool with the other three.

**The measurement that set the pool size.** A level-up is the only place a hero grows, and today's Descent grants about
seven. Every card added past a point thins that growth. Over twenty-four seeds and three weapons, share of runs that
cleared both floors:

| Pool | Standing (Sword/Staff/Daggers) | Kiting (Sword/Staff/Daggers) | Median health, kiting |
|---|---|---|---|
| 3 cards | 54 / 41 / 79 % | 100 / 100 / 100 % | 91.7 |
| **5 cards (shipped)** | **29 / 25 / 41 %** | **91 / 91 / 87 %** | **91.7** |
| 7 cards | 20 / 20 / 29 % | 75 / 83 / 62 % | 54.1 |
| 9 cards | 12 / 4 / 8 % | 45 / 54 / 33 % | 0.0 |

Two decisions came out of it. **Five cards**, because seven already halves the median health a kiting run ends with and
nine leaves a standing hero clearing one run in ten. And **the common tier keeps today's magnitude** for the two
existing cards - +50 % damage, +50 % attack speed - with Rare and Epic above at +75 % and +125 %. Measured first with
Common at half of today's value, the whole matrix collapsed: every standing run died and four kiting runs in five. A
tier that quietly makes the familiar card weaker is a nerf wearing a new word, so rarity is upside only.

**What this costs, written down rather than discovered later.** A standing hero used to clear every Descent, because a
two-card pool made the damage card certain. With five it clears 7, 6 and 10 of 24 seeds. That is a real difficulty
increase and it is the direction the owner asked for, but it is a consequence of variety rather than a tuned choice,
and `26`'s pinned claims would not survive it. So the balance pins - relics, weapons, the burst - now name
`DescentSimulation.TwoCardPool()` explicitly and keep measuring what they are about, unmoved by any card added later.
The shipped pool's own effect is claimed once, as a distribution, in `DescentSeedTests`.

**Tests.** New `Tests/EditMode/DescentSeedTests.cs`, five cases: a kiting hero clears most seeds with every weapon; a
standing hero clears some but not all, which is the cost above; the same seed is the same run, to the decimal; eight
seeds do not all end the same way; and the authored nine-card pool clears strictly fewer seeds than the shipped five,
which is the finding that set the size. `RunTelemetryTests` pin the two new fields. Every balance pin in
`DescentTests`, `FloorTests`, `WeaponBehaviorTests` and `AbilityTests` names the two-card pool. In PlayMode, the whole-
Descent parity cases and the two flow tests that need one particular card now write `development/run-seed.txt` after
the test profile is redirected, so the scene and the simulation build the same hero; the flow tests use seed 3, which
was measured to offer Tempered Edge first and Quickened Grip within two.

Files changed: `Scripts/Progression/UpgradeStat.cs`, `UpgradeOption.cs`, `UpgradeOffer.cs`, `UpgradeService.cs`,
`HeroStats.cs`, `RunChoices.cs`, `Scripts/Content/UpgradeDefinition.cs`, `Scripts/Analytics/RunTelemetry.cs`,
`Tests/Support/DescentSimulation.cs`, four EditMode suites, `Data/Upgrades/Upgrade_Damage.asset` and
`Upgrade_AttackSpeed.asset`, `Gameplay.unity` (the pool array only), `Tools/CombatChecks/CombatChecks.csproj`. Added:
`Scripts/Progression/UpgradeRarity.cs`, seven upgrade assets, `Tests/EditMode/DescentSeedTests.cs` and their metadata.
No art, HUD, camera or enemy data changed. The scene edit was made by a temporary Editor builder, deleted after it ran.

**Not in this slice.** The four cards outside the pool. Weapon evolutions. A rarity a player can *see*: `ChoiceCard`
carries its tier as a field for a view to bind to, and nothing shows it yet, which is the art owner's. The first
attempt glued the tier to the card's name, which broke every test that reads a name and would have broken every
translation; a field is the right seam. Nothing has been played on a device.

**Handoff for Astra.** New upgrade ids: `upgrade_range`, `upgrade_crit`, `upgrade_health`, and authored but unused
`upgrade_armor`, `upgrade_move_speed`, `upgrade_stamina`, `upgrade_luck`. `upgrade_offered` gains `choice_rarities`
and `upgrade_selected` gains `rarity`, both alongside the existing fields; nothing was renamed. `ChoiceCard.Rarity` is the tier the card was
offered at, waiting for a visual treatment - the first visual need of this slice, and nothing shows it today. The second is a stat readout on the pause screen, since a run now grows reach, crit, and health.

**Verified:** .NET **323/323**, compile **0 warnings/errors**, EditMode **338/338**, PlayMode **115/115**, Android build exit **0**, static integrity **460 unique asset/folder GUIDs / 568 scene objects/components**. Development APK **24,752,344 bytes**, SHA-256 **`CF2EC63FC95D880A90626CF3728EA6883EAD6EEC26DDF1B8EA9B7594CC9AE651`**, built, **not installed**.

Seven PlayMode cases had to be taught the new world, which is worth recording because each names an assumption a two-card pool had let stand: the whole-Descent parity cases assumed the scene and the simulation would build the same hero without being told a seed; two flow tests assumed a named card would be in the first offer; one assumed a card's magnitude was its asset's; one took whichever card sat in slot zero and then asserted the card it wanted by name; one filled a card to its cap by taking slot zero, which quietly stacked the card it was about to count; and the Mend case assumed the hero's maximum health was still a hundred. None of them were wrong before.

## S3 — books from chests, and the chest the simulation never had — 2026-09-20

**What shipped.** A chest can now hold a **book**: one stack of one card, granted on the spot, drawn from exactly the
eligibility a level-up uses and at a tier from the same rarity table. It is additive - the chest still mends or pays,
and `ChestReward.Kind` is unchanged, so nothing that shows a chest opening had to move and the room-one heal every
pinned balance claim leans on is untouched. A fourth `RunRandom` purpose keeps "does this chest hold one" and "which
one is it" from reading the same word of the same stream, and `chest_opened` now names the book and its tier.

**The share was measured, not chosen** (`28`). The authored Descent, twenty-four seeds, three weapons, four walks,
seventy-two runs a cell, six builds of the harness because `ChestRule.BookPercent` is a constant. At a share of zero the
walk to a chest is a cost rather than a reward - fetching then standing dies ten times in seventy-two against kiting's
seven, because the hero spends its budget crossing the arena for a heal it does not need yet. At 50 % a fetching hero
never dies in a hundred and forty-four runs; at 100 % it finishes at the health cap with the fight a third shorter.
**25 %** is the lowest share at which every measured run found at least one book - median two against eleven chosen
upgrades - and a fetching hero can still die. What the number holds is about two books a Descent, so S6's longer floors
bring it down with them.

**The gap this opened by accident.** `DescentSimulation` had never modelled chests at all, since before routes existed.
A walking scene hero healed and the simulation's did not, and it had never shown as a parity failure for a reason that
is luck rather than design: every walk the parity cases run reaches the room's chest **before anything has struck it**,
so the heal lands on a hero at full health and restores nothing. A book is not a no-op, so the simulation now places a
chest in every combat room on the same rule, opens it on the same reach check, applies the same three effects in the
same order, and a new parity case walks a scene hero onto one and holds the two together.

**What did not move.** Every pinned number. A standing run never leaves the middle, so it never opens a chest; and at
the shipped share the first chest of seed 0 - the seed the proof floors run on - holds no book. The second is a
coincidence with a bound attached, written into `DensityProofTests`: raise the share to 50 % and the book that chest
then holds lengthens the Daggers' looping run into a fourth enemy step-flicker and the proof fails. That failure is the
test working, not a flake to widen.

**Still missing, and it is Astra's.** A book has no presentation. A chest that holds one shows its heal or its coins and
the stack arrives silently; the telemetry names it, the player does not see it. It is on the visual-needs list in `27`
with the rarity treatment and the pause-screen stat readout.

**Verified:** .NET **329/329**, compile **0 warnings/errors**, EditMode **344/344**, PlayMode **116/116**, Android build exit **0**, static integrity **460 unique asset/folder GUIDs / 568 scene objects/components**. Development APK **29,163,321 bytes**, SHA-256 **`07C990A2BB56977BB1DA72E097B626ADE0389258605F10E01D24758FB77D5697`**, built, **not installed**.

## S4 and S2c — the swarm room, and a pool that grows with the run — 2026-09-20

**What shipped.** `RoomKind.Swarm`: a room whose waves do not run out. Its authored waves are a **ladder** rather than a
list that finishes - the ordinary `WaveDefinition[]` every combat room has - handed out in order, with the last wave
repeating for as long as the player stays, and `SwarmRule` hardening every enemy in it wave by wave. `FloorProgress`
gained endless rooms; leaving one is asked through `CheckpointService`, the same service and the same panel that asks
Extract or Descend after a boss, with two new `CheckpointKind` values and no new UI. It lives on a development floor
reached through `development/start-floor.txt`, not in the authored Descent, because putting one into Ember Halls would
move every pinned number in the project and S6 is the slice that re-authors floors.

Beside it, S2c: all nine authored cards are in the pool and `UpgradePool.WidthAt(level)` decides how many of them a run
is offered - five until level twelve, one more every six levels after.

**Two constants, both measured** (`29`). The ramp is **2 % a wave**: flat leaves a backing-off hero immortal in
twenty-three of forty-eight runs (and the Staff in sixteen of sixteen), while 5 % and above cut the room down to about
what a Descent already grants. At 2 % a swarm pays two to three times a Descent's level-ups in about two minutes, the
deepest run reached wave one hundred and twenty-four against enemies three and a half times their authored strength, and
nothing survived the two-hundred-wave cap. The pool's sixth card arrives at **level twelve**, one above the longest run
the game has, and that is pinned rather than argued: over twenty-four seeds, three weapons, standing and kiting, a run
with nine cards in the pool and a run with exactly five return the same floors, health, level, upgrades and fight
seconds. The scene carries all nine now and nothing a Descent player can see moved.

**What the suites caught.** `CheckpointOption` validated its kind against a hand-written pair - Extract or Descend - so
the swarm's first Press on threw out of a scene test. It reads the enum now. That is the whole reason the parity case
exists: the rule, the room and the choice all worked in the simulation, and only the scene knew about that list.

**Two things the specification did not foresee.** A swarm room has **one chest**, because chests are per room, so S3's
books barely reach it - the median swarm run took none. Chests that arrive with the waves are a real gap and belong with
S6 or a slice of their own. And the exit is offered after **every** wave, which is the plainest reading of "leave when
you like" and one constant, `SwarmRule.ExitEveryWaves`; whether it nags is a question about feel that only playing it
can answer.

**Still Astra's.** A swarm has no clock, no wave counter and no sign that the enemies are harder than they were; the
choice panel's two cards are all the player sees. It is on the visual-needs list in `27` with the rarity treatment, the
book's presentation and the pause-screen stat readout.

**Verified:** .NET **336/336**, compile **0 warnings/errors**, EditMode **351/351**, PlayMode **117/117**, Android build exit **0**, static integrity **462 unique asset/folder GUIDs / 568 scene objects/components**. Development APK **24,752,438 bytes**, SHA-256 **`07B0F8E6615FCFA0433B00B5AC4538A6BA695E98AFF56638098ADD2B49FD3697`**, built, **not installed**.

## S5a and S5c — two behavioural cards, one failed claim and one that held — 2026-09-21

**What shipped.** The first two upgrades that change what an attack *does* rather than a number it carries, both riding
the existing stat pipeline so nothing that reads an offer had to learn about them.

**Forge Pulse** (`30`): every fourth strike bursts round the hero for a share of its damage. Seven contract clauses, each
pinned by a test - a pulse is not a strike, a crit can trigger one but the pulse never crits, an area strike counts once,
a pulse kills nothing onward, relic damage is not a strike. Shipped at one stack, radius 2.2.

**Splintering Crit** (`31`): a critical strike also strikes everything within 2.0 units of its target, for a share of the
crit's damage. Six clauses, each pinned. One stack.

**The Pulse's claim failed and is recorded rather than rewritten.** `docs/27` had said since the roadmap was drawn that
the Pulse should favour the Daggers and that if it favoured the Sword the design was wrong. Measured over about three
hundred and eighty swarm runs - sixteen seeds, three weapons, four radii, four magnitudes, and two different ways of
scaling the damage - it favours the Sword at eleven settings out of twelve. The reason is structural: a per-strike burst
inherits the weight of a strike, the Sword's strike is 10 against the Daggers' 6, and in a swarm every weapon has enemies
inside the circle whatever its reach. Even a **flat** pulse, which should have rewarded the most strikes rather than the
biggest, still went to the Sword - because attack-speed cards lift everyone and the Sword survives longer to collect
more of them, so it ends up pulsing as often as the Daggers do (0.73 a second against 0.68).

It shipped anyway, with the reasoning changed rather than the numbers: it covers a hole nobody had measured, which is that
the Sword lasts 15.5 waves in a swarm against the Staff's 45.

**The second card was designed from that diagnosis and its claim held.** If an effect scaled to damage belongs to the
heaviest weapon, then a card for a light one has to be gated on something that weapon owns in its own definition. The
Daggers author a crit every third strike; the Sword and the Staff author no rhythm at all. With crits **held equal** at
25 % across all three - the harsher arm, since the nine-card pool sells Keen Edge to everyone - the Daggers gain most in
nine settings out of nine: +206 % at the shipped radius against the Sword's +20 % and the Staff's +2 %. Unlike the Pulse,
shrinking or widening the radius does not change the ordering. It takes the Daggers from 18 waves to 55, still under the
Staff's 67, so they became a choice rather than the best.

**What the two together are worth keeping.** A behavioural card aimed at a particular weapon has to be gated on something
that weapon owns in its definition. Scale an effect to damage and it belongs to the heaviest weapon whatever else is done
to it.

**Two mistakes, both mine, both worth writing down.** The first sweep was thrown away: the control arm granted itself a
Pulse, because a swarm run reaches level fifty and `UpgradePool` opens every card by level forty-two, so the Staff pulsed
a hundred and six times in the row that was supposed to have none. And a Unity batch run was left running while a second
card's asset and a scene edit went in underneath it - seventeen scene tests failed on a half-imported reference, which
looked like a defect and was a process error.

**Astra's queue is now three invisible mechanics**: a book, a pulse and a splinter, none of which draws anything.

**Verified:** .NET **351/351**, compile **0 warnings/errors**, EditMode **366/366**, PlayMode **117/117**, Android build exit **0**, static integrity **466 unique asset/folder GUIDs / 568 scene objects/components**. Development APK **24,752,438 bytes**, SHA-256 **`88AD795206787DDEB0CFE40F935E1711F59C76CEE2AB246DDF96DA204B665C6C`**, built and **installed** to the owner's S23 for play; installed hash matches.

## S5b — evolutions, what they cost, and what the owner did with them — 2026-09-21

**What shipped.** A card at its stack limit, in a run past a Descent's length, unlocks its **evolution**
(`UpgradeOption.EvolvesFromId`); and taking any evolution raises a run-level **Difficulty** - a new stat on `HeroStats`,
starting at 1.0 - that multiplies every enemy's health and damage in the scene and the simulation alike. Three evolutions:
Ruinous Edge, Blurred Grip and Ironblood, on the three cards every measured run fills first. Cost **0.20** a stack
(`32`). An evolution ignores the pool's width, because a run that reached level fifty-one opens twelve of fourteen cards
and evolutions behind that would never arrive.

**What the pinned tests caught.** A full base card alone let evolutions into the **Descent** - eleven level-ups spent on
one card fill a five-stack card, and an evolution ignores the width, so nothing stopped it; the seed-for-seed Descent
guarantee broke on the first run. The gate is now both: base full **and** level twelve, one above the longest run the
game has. And Unity's bundled NUnit has no `Has.Exactly(n).Items` where the .NET harness's newer one does, so the pure
suite and the compile check both passed on an assertion Unity could not compile.

**What the phone said.** The owner played it the same night. The hole is closed - thirty-five levels, thirty-six stacks
applied, none wasted, against thirteen wasted the session before. Evolutions arrived at wave eleven, while the build was
still being made, which is where the Pulse and the splinter should have arrived and did not. He took **all nine**
evolution stacks the moment each was offered: the "and a harder world" line stopped him not once. Combined difficulty
reached 4.54 by wave thirty-two - and his health **rose** through it, from 58 at wave thirteen to 191 at wave thirty-two.

That contradicts `32`'s central row, which said a player who takes every evolution ends shallower than one who takes
none. The simulation's Staff took six and died near wave thirty-six; the owner took nine and was comfortable. It is the
calibration gap `32` named, now confirmed in play, and it is not fixed by picking a larger number: the world grows by
adding and the hero grows by multiplying, and a linear ramp cannot catch a multiplicative build at any rate. The owner
approved an exponential ramp the next morning; calibrating the measurement policy to his play comes first.

**Verified:** .NET **359/359**, compile **0 warnings/errors**, EditMode **374/374**, PlayMode **117/117**, Android build exit **0**, static integrity **470 unique asset/folder GUIDs / 568 scene objects/components**. Development APK **24,752,438 bytes**, SHA-256 **`6C4C70647BE693D0A7C200BFD56669169F7D161D2FD47D9263B76FA8284BAEDD`**, installed and played on the owner's S23.

## S6a — calibrating the measurement to the owner, and the hypothesis that was wrong — 2026-09-21

**What changed.** Nothing in the game. `DescentSimulation.Run` gained a **replay** - the k-th recorded card at the k-th
offer - a per-wave trace, and `Result.Picks`, and a test pins that a run replayed from its own picks comes back identical
with no misses. With that, the owner's four recorded swarm runs were replayed exactly from their seeds and cards, each
against the pool of the build he played it on (`33`).

**The hypothesis was wrong, in writing.** `27` section 12 said this step would isolate what the simulation gets wrong
about how he moves. Given his cards, a back-off player with a slightly wider release fits his health within six to ten
points a wave; the simulation was never wrong about moving. It was wrong about **choosing**: every measurement since `29`
took the card in slot zero, and his two-card answers rank evolutions first, damage and cadence next, armor and the Forge
Pulse last - the same order across four runs and three builds. The calibrated player, his ranking plus that movement,
goes about half as deep again with the Staff as the reference player the ramp and the evolution cost were set against.

**The question three sessions left open, answered.** Replayed with his own builds, his runs would have died at waves
fifty-eight to ninety-six. He left each of them with that much margin in hand.

**Verified:** .NET **360/360**, compile **0 warnings/errors**, EditMode **375/375**, PlayMode **117/117**, Android build exit
**0**, static integrity **470 unique asset/folder GUIDs / 568 scene objects/components**. Nothing in the runtime changed,
so nothing was installed.

## S6b — the exponential ramp — 2026-09-21

**What shipped.** `SwarmRule.DifficultyAt` is geometric: each swarm wave `GrowthPerWave` times the one before, **1.02**
(`34`). Computed by repeated float multiplication in one never-inlined body rather than `Math.Pow`, because a library
power function's last bit has differed between Mono, .NET and IL2CPP and this project has paid for one last-bit
divergence already.

**Why the shape and not the rate.** The world grew by adding and the hero by multiplying, so raising a linear rate only
moved the moment it lost. Measured against the calibrated player of `33`, not the slot-zero one.

**How it was chosen.** Four criteria written before the sweep. 1.02 is the only rate that reshapes the finale - the Staff's
median 53.5 to 41, runs past wave eighty nine to one in 144 - without making the early game deadlier; 1.025 took the last
long run but raised early deaths significantly (z = 2.11), and 1.03 and above are walls. Two criteria were not met as
written: the ceiling was a maximum, which grows with the sample, and the early-game test a strict inequality missed by a
non-significant four runs. Both are recorded rather than reworded.

**Verified:** .NET **360/360**, compile **0 warnings/errors**, EditMode **375/375**, PlayMode **117/117**, Android build exit **0**, static integrity **470 unique asset/folder GUIDs / 568 scene objects/components**. Development APK **24,752,438 bytes**, SHA-256 **`340678528DFC7942FF00F112E7D5C80859663CB92E28C66E85B7DF2EDFAD0FED`**, built, not yet installed.


## S6c — a continuous swarm and a walked exit — 2026-09-21

**What shipped.** The finale has no waves. `SwarmDirector` holds one pack of at most ten for the room's whole life: a
fallen enemy's place waits `SwarmRule.RespawnSeconds` and fills again from the ladder read as a queue, entering by the same
`EntrySides` rule a wave uses. The ramp of `34` runs on the clock, one step of 1.02 every `SwarmRule.StepSeconds`, and each
enemy keeps the difficulty of the moment it walked in. The per-wave question is gone - the prompt, its event, the two
checkpoint kinds (retired, not reassigned) and its text. Leaving is `ExitRule`: a spot five units toward the far corner,
held for a second while standing still. Telemetry reports `swarm_step` and `swarm_exit` in place of `swarm_wave`. The
exit and the clock draw nothing yet; they are Astra's.

**How it was chosen (`35`).** At level zero no setting met the criteria - a Sword or Daggers with nothing dies inside a
minute in a crowd that never clears - so the clocks were measured the way decision 5 says the room is met, at the end of
the Descent. Four cells passed; a step of **three seconds** and a respawn of **two** put the Staff's median at 202 seconds
with two early deaths in 139. The respawn was to be fixed at one second and was measured instead. The exit's distance
turned out not to be the lever: at three, five and seven units the decision's timing decides everything.

**What the measurement found about itself.** A stand-in blind to the exit left by accident in thirteen runs of 144 by
stopping to fight on it, and that had inflated the late exits too. `ExitRoute` now plays a player who does not stop on an
exit it does not mean to take, and every table in `35` was measured with it. On the phone, until the exit is drawn, a real
player can make the same mistake.

**Two tests re-stated.** The Forge Pulse and evolution tests each pinned one seed and each broke under the new clocks;
they now sum eight and six seeds, the evolution one entering the swarm at the end of the Descent.

**Scene parity.** A second PlayMode case walks out of the swarm through the pack, scene against simulation, on the same
frame.

**A flaky test, fixed in passing.** `FloorFlowTests.TemperOpensOneExtraUpgradeChoice` asserted that the hero's health is
unchanged after Temper's bonus card, but the class does not pin the run's seed, so the card is sometimes a health card and
raises the health by what it adds to the maximum. It failed one full run in this slice and one of two class runs; it now
asserts exactly that difference.

**Verified:** .NET **367/367**, compile **0 warnings/errors**, EditMode **382/382**, PlayMode **118/118**, Android build exit **0**, static integrity **473 unique asset/folder GUIDs / 568 scene objects/components**. Development APK **29,185,542 bytes**, SHA-256 **`C93D0A4958D9C96A8756A44F7DAE60A43EB7A0C04CE3E8A87CC394F5BA5ADF92`**, installed on the owner's S23 with the swarm as its start floor.


## S6c, second pass — the crowd by the clock, and level-ups in sittings — 2026-09-21

**Why.** The owner's first continuous run (`35`) died at 94 seconds and called the room much too hard. It showed two
things: a card panel every three and a half seconds - 27 % of the run spent choosing - and a crowd whose make-up advanced
with every kill, so a fast killer met the heaviest wave within half a minute. Offered the choices, he picked the same
levels shown less often, and a crowd that hardens with time (decisions 7 and 8 in `27`).

**What shipped.** `SwarmDirector` gives each of the pack's places a role in each wave and reads the wave from the room's
clock, one every `SwarmRule.LadderSeconds` (**12**). `UpgradeService` can hold level-ups; a swarm room holds them and opens a
sitting every `SwarmRule.OfferEverySeconds` (**10**) that offers every pending one in a row, and leaving the room releases
the rest. The Descent holds nothing and is unchanged. The simulation mirrors both on the same frames, and the parity case
standing in the swarm takes its three cards in sittings, identically in scene and simulation.

**How it was chosen (`36`).** At the end of the Descent neither constant moves the finale - the Staff's median is 239 to
249 seconds across every setting - while at level zero they decide everything: a ladder that tops out in half a minute
with held cards kills a level-zero Staff in 33 seconds, and a twelve-second ladder passes all four of `35`'s criteria even
there. The rule written to pick the ladder's pace assumed it was part of the finale's tuning; it was not, so the pace
was chosen after the sweep, for the level-zero opening and for a make-up the player lives through, and recorded as such.
The finale now sits four seconds above `35`'s window (Staff 244 against 240): the roles stop heavies stacking and made it
slightly easier, and with the owner calling it too hard the ramp is left alone until he plays the finale where it belongs.

**Tests.** The director's tests were rewritten for roles and the clock, including two rooms at the same minute - one
idle, one killing everything - that hold the same wave and the same places. A new `UpgradeSittingTests` pins the hold,
the sitting, the release and that a sitting shows the cards the seed would have shown anyway. Two swarm tests moved: the
replay test now runs the Descent into the swarm so its sittings replay too, and the leave-early test stays twenty-five
seconds rather than ten because the opening's three mites pay too little for a level in less.

**Verified:** .NET **372/372**, compile **0 warnings/errors**, EditMode **387/387**, PlayMode **118/118**, Android build exit **0**, static integrity **474 unique asset/folder GUIDs / 568 scene objects/components**. Development APK **29,186,418 bytes**, SHA-256 **`E5845F8EFBF7F91D8B21A08B60FC505BD11343EE94234BF682C8FC9057A15517`**.

## S6d, first part — the finale behind the last boss — 2026-09-21

**Why.** Three rounds of tuning had measured the swarm's opening against a level-zero hero, because the phone could only
reach the room through the development start floor, and decision 5 says no hero walks in that way. The owner asked
whether the swarm should wait for a strength threshold and whether today's cards are enough to stand in it. Measured
(`36`): the threshold is one floor - after Ember Halls no calibrated run of any weapon dies inside ninety seconds - and the
second floor adds nothing against the swarm. So the finale went where decision 5 put it.

**What shipped.** `Floor_FinalSwarm`, the proof floor's ladder and multipliers unchanged, and Quicksilver Vaults leads to
it. The Warden's fall now opens a checkpoint: Extract banks everything as the old victory did; Descend secures the
Descent's gold and walks into the finale, where only what the finale pays is at risk, and walking out is the victory. The
simulation names the finale as the proof floor's single definition, and a scene test that descends into it holds the
asset to that definition wave by wave.

**Tests.** The Descent parity cases now end by extracting at the finale's door, which banks what the simulation's victory
banks; the flow test that cleared both floors now finds the finale behind the Warden and extracts there; a new flow test
descends and checks the secured gold, the floor and the ladder.

**Noticed, not fixed.** `ChestPresentationTests.RewardIsImmediateAndSingleWhileOpeningHonorsPause` failed one full run
(health 100 where 85 was expected) and passed four class runs and the full rerun. The chest can hold a health book since
S3 and the class does not pin the run's seed, so a book of Vital Surge lifts the health past the heal. It is an art
presentation test, left for its owner.

**Verified:** .NET **372/372**, compile **0 warnings/errors**, EditMode **387/387**, PlayMode **119/119**, Android build exit **0**, static integrity **475 unique asset/folder GUIDs / 568 scene objects/components**. Development APK **29,186,418 bytes**, SHA-256 **`882383B2C27A4B4329504A388042D893EAD6F2807213DDDB483A61A5BB68C331`**.

## S6d, second part — rewards on the ground — 2026-09-21

**What shipped.** `GroundDrops` and `DropRule` (Economy): in every room but a swarm a kill's experience and gold fall as
one drop where the enemy fell (`RewardService.TryDropKill`, the same once-per-victim guarantee); `EncounterController`
picks up whatever lies within `DropRule.PickupRadius` of the hero every frame after it walks, pauses between waves and a
forge's walk out included; `CombatSetup` sweeps what is left when a floor's last enemy falls, before its checkpoint. The
finale pays at once, as before. Telemetry counts a dropped kill's gold when it is picked up. Drops draw nothing yet -
that is Fable's - so this is not a phone build.

**The radius (`37`).** Criterion one picked 2.0 and its premise failed: enemies walk to the hero, so a ranged weapon's
kills fall at its feet too, and at 2.0 walking changed nothing about when cards come. Shown three radii, the owner chose
**1.0**: a hero who never fetches picks up nothing and loses the Descent; one who walks for its drops is two to three
levels ahead at floor one's boss and cleared every seed measured. Criterion two passes at 1.0; criterion one is recorded
as not met.

**What it found.** No Descent balance number since chests existed had been measured by a hero who opens one. A walked
Descent opens two or three, and with them greed stops being punished: Temper on floor one clears, Second Wind's rescue is
never needed, Counterweight's gain reverses. The pinned balance tests keep their claims on the simulation's control
(`groundDrops: false`, the game before drops), which is what they measure - the combat's arithmetic - and new tests
describe the game as it plays.

**The simulation** now lives the frames between waves whenever drops lie, as the scene always has; a frame on which a
drop opens a level-up does not count toward the pause, in both. A new parity case fetches through all of Ember Halls,
scene against simulation, and agrees to the frame: 31 kills, 30 drops picked up and one swept, 103 health, the same
spot. `FetchRoute` is the stand-in that walks to the nearest drop when nothing is within reach plus 1.5.

**Tests.** New `GroundDropsTests`; `DescentSeedTests` re-stated for heroes who fetch and one who never does; the replay
test picks its seed by the run, since the Editor's Mono and the .NET runner walk it down different paths. Scene tests
about the reward rule now assert the drop; scene tests about other things (the choice panel, the pause, telemetry, the
result screen) attach `DropVacuum`, which picks every drop up on its own frame - the game before drops - so they keep
testing what they test. Two art tests' reward lines were changed with them.

**Verified:** .NET **380/380**, compile **0 warnings/errors**, EditMode **395/395**, PlayMode **120/120**, Android build exit **0**, static integrity **478 unique asset/folder GUIDs / 568 scene objects/components**. The build was a scripts-only one ("player data was not rebuilt"), 24,752,438 bytes; not installed, and the next phone build should be a clean one.

## Ground reward presentation — 2026-09-22

GroundDropView makes the docs/37 ground rule visible without changing collection or payment. CombatSetup attaches the observer with one optional imported sprite reference; resting drops use floor-root sorting and independent shared contact shadows. One immutable 96-square/128PPU/bilinear/ASTC4 combined XP/gold sprite is borrowed across reloads. Sixteen pooled roots grow as required without overwriting uncollected drops; value-identical drops stay distinct. Collection starts a .22s flight (.42s when distant), uses scaled time, and never delays XP/gold or upgrade choices. Ending a run clears presentation without paying remaining drops. Old Descent instant-coin effects are suppressed; chest and Swarm effects stay.

Files: GroundDropView.cs/meta, CombatSetup.cs, CombatEffectsView.cs, GroundRewards asset/folder/meta, one Gameplay sprite reference, GroundDropPresentationTests.cs/meta, ChestPresentationTests.cs, Tools/ArtReview reward importer/capture/runner, and ArtDirection/2026-09-22/reward-runtime-01. Original source/prompt are retained in reward-proof-01. Single-currency exceptions use provisional honest fallback symbols; no new gameplay, density, balance, camera, save, package or project-setting change.

Four new tests cover actual death/collection, coincident rewards/pool/rehydration, upgrade pause/death, import and reload ownership/subscription cleanup. The previously documented flaky chest heal test now pins a seed without a first-chest book. Eight actual graphics-enabled Unity captures show controlled model events at two portrait sizes, front/back overlap and pickup/sweep/choice states. These are staged captures, not a phone or performance measurement.

**Verified:** .NET **380/380**, compile **0 warnings/errors**, EditMode **395/395**, PlayMode **124/124** (existing scene/simulation parity included), targeted presentation **6/6**, graphics **1/1**, static integrity **482 project GUIDs / 1099 package GUIDs / 568 scene objects/components**. Android exit **0**, APK **24,752,438 bytes**, SHA-256 **`9DD2A9A758C95DCB88F91E4204B0207D61E30389787AF674414A0805B19A0F28`**. Log confirms player data rebuilt for the changed scene; APK inspection finds the view in IL2CPP metadata and reward art in shared assets. Packages lock/ProjectSettings/source hash preserved. No commit/push.

**Phone:** S23 enumerated only. Availability question was unanswered; no install, launch, input, capture, profile or telemetry access. Next is coordinated device acceptance, then the separate walked-exit visual slice. Details/reproduction/limits in the runtime README.

Ground reward device follow-up (2026-09-22): APK9DD2A9A758C95DCB88F91E4204B0207D61E30389787AF674414A0805B19A0F28 installed on S23 and installed hash matched. Fresh profile186/26321gold/defaultSword/SecondWind/deepest3 and all nine backed-up app-files unchanged. Phone locked/dozing; unlock requested, no launch/input/capture. Live visual acceptance remains pending. See newest docs/23 and reward-runtime-01 README; this supersedes not-installed status above.

## XP-only painted companion — 2026-09-23

Cinder Mite's frequent XP-only reward now uses Reward_Experience.tga through a separate CombatSetup reference and a borrowed GroundDropView sprite. Full-canvas96x96/128PPU, bilinear, non-readable, no mips, Android ASTC4x4; root623/1254,111/1254;0.30 visible width versus combined0.45. No payout, pickup, timing, pooling, combat or simulation changes. Missing references and gold-only drops keep existing fallbacks. The imported asset is not destroyed by the view.

Updated GroundDropPresentationTests verifies XP import/pivot, selection/scale and survival across scene reload; the level-choice test now exercises XP-only collection without crediting gold. Actual Unity capture kills a Cinder Mite and stages17 more XP-only drops, then checks depth, short portrait, local/sweep flight, choice pause and pool return. Reproduction: Tools/ArtReview/Import-ExperienceReward.ps1 and Run-ExperienceRewardProof.ps1. Evidence: ArtDirection/2026-09-22/xp-reward-runtime-01/README.md. Source image hash unchanged; no new generation in integration.

Verified: .NET380/380, compile0 warnings/errors, EditMode395/395, PlayMode124/124, graphics1/1, integrity483project GUIDs/1099package GUIDs/568scene components. Browser8images,390px no overflow/errors. Android result follows below. Phone availability requested but unanswered; no device interaction or assumed profile state.

Android verification complete: exit0, APK24,752,438bytes, SHA2561F00CF65D105882E8976A0B3167B5A600A57022B38D9A0B9ED2D1FC997BBA168. Build log confirms scene data rebuilt; packaged shared assets contain experience sprite references. ProjectSettings and Packages/packages-lock.json unchanged; final static integrity and git diff --check passed. Not installed: phone availability remains unanswered. No commit/push in this slice.

## Finale exit visual integration and region direction — 2026-09-23

ExitSealView now reads the existing final-swarm exit position and dwell. Brass0.9radius boundary/gateway sits below actors; mint arc clamps authoritative held/1second; off-screen directional cue stays inside the combat viewport without moving the destination. No new timer, input surface, banking or progression rule. PrototypeTextDefinition provides neutral English stand-still/direction strings. CombatSetup composes the view. The view owns one material and reusable lines/arc buffer; shared font is borrowed. Descent, ended floor/run and disabled view hide all indicators.

Three new ExitSealPresentationTests passed: hidden Descent and reload, actual hold/pause/interruption/completion, camera-offscreen cue and disable. Actual graphics capture1 passed; final portait camera aspect fixed in fixture; idle/hold/pause/direction reviewed. Source/evidence: ArtDirection/2026-09-23/exit-seal-runtime-01. .NET380 passed, compile0 warnings/errors, static485project GUIDs/1099package GUIDs/568scene components. Full gate/APK result follows separately.

Owner approved two fights -> elite OR chest -> forge -> main boss -> extract OR optional final swarm. region-map-01 is a tested interactive design, not a Unity route; current floors remain linear. DESIGN_AND_HANDOFF.md and OPUS_TASK.md document pending gameplay/model/parity/reward decisions. No external task dispatched. Distinct combat/boss map geometry is not implemented here. No phone changes.

Verified final gate (2026-09-23): .NET380/380, compile0 warnings/errors, EditMode395/395, PlayMode127/127, targeted exit3/3, graphics1/1, static485project GUIDs/1099package GUIDs/568scene components. Android exit0; APK29,252,655bytes, SHA25628B658D4AFC4FBBFB68623F0EA97A3F2F3AD1CA9ECB747B10930ED57B95E0B41. No install/device operations, commit or push in this slice. The prior installed XP-only build remains the latest device record. Map flow is approved and prototyped in the browser only; branching gameplay, separate battlefield geometry and new region balancing remain outstanding. ProjectSettings and Packages/packages-lock.json unchanged.

## 2026-09-23 — Fixed-scale wide-region paving

PlatformMaterialMeshes now derives floor UVs from planar floor coordinates at the original 9-unit hall scale. The default hall retains its corner UVs; elongated regions no longer stretch/shear the paving. PlatformMaterialTests adds a hall/wide spatial-density check. Actual traversal screenshots in ArtDirection/2026-09-23/exploration-proof-01 were refreshed; ENVIRONMENT_KIT.md defines reusable perimeter scenery and explicitly defers walkable elevations. Wide geometry still exists only in the temporary capture scene. Coping/walls/foundation and physical encounter locations remain open.

Verified: .NET 380; compile 0 warnings/errors; targeted PlatformMaterialTests 5/5; graphics traversal 1/1. No full-suite run, APK rebuild, installation or phone interaction for this bounded material slice. Latest installed XP-only build and previous exit-seal APK remain unchanged. No new performance claim.

## 2026-09-23 — Wide-region coping and furnace source

PlatformMaterialMeshes keeps coping perpendicular floor width at the original hall value (0.45/sqrt(2)), with a conservative half-scale inset cap for tiny geometries. Coping/wall/join repetitions follow floor-space edge length instead of stretching across the region. Shared miter vertices and nine-mesh ownership remain. PlatformMaterialTests checks hall/wide width, texture density and corner joins. Graphics traversal refreshed and inspected. This is repeated material geometry, not separate placed wall prefab modules.

Verified: .NET380, compile clean, PlatformMaterialTests5/5, graphics traversal1/1. No full suite/APK/device change. Wide region remains temporary. New unchanged built-in image_gen furnace source and exact prompt in ArtDirection/2026-09-23/furnace-prop-01; RGBA1358x1159, transparent alpha, browser390px no overflow/all images loaded. Source only, not imported or placed. Next: sample/place reusable perimeter props, then live region integration with explicit local-encounter authority. No collision/elevation mechanic added.

## 2026-09-23 — Furnace imported, isolated Unity placement

Imported shared Furnace.tga (384x328/128PPU/custom pivot/non-readable/ASTC4) under Art/Props/Foundry. Importer samples unchanged source once and does not edit Gameplay. FurnaceCapture places it at north exterior on a temporary stone-textured foundation; real hero traversal and reference portrait capture. Initial oversized grey foundation corrected in second render. No collision, interaction, playable extension or production scene reference.

Verified: compile0 warnings/errors, final graphics1/1, static487GUIDs/1099package/568scene objects, browser390px no overflow/all images loaded. Full suites and APK not rerun for source/import/temporary fixture slice. No phone action. Read ArtDirection/2026-09-23/furnace-unity-01/README.md. Next: bounded region scenery integration and explicit playable entry, full gate then device; local encounters remain separate and pending.

## 2026-09-23 — Wide region and furnace development integration

Gameplay now binds Furnace.tga and ArenaView supports a once-resolved development/region-preview.txt=wide flag. Default hall and release behaviour preserved; wide preview is -27/+27/18. FoundrySceneryView borrows art/material and owns its foundation mesh. No local encounters, collision/elevation or new progression rules. Graphics capture now loads actual Gameplay, not an enlarged scene copy. See ArtDirection/2026-09-23/region-runtime-01/README.md for commands, proof and limits.

Verified: .NET380, compile0 warnings/errors, EditMode395/395, PlayMode130/130, targeted region2/2, graphics1/1, browser390px checked. Android exit0; actual APK29,252,655bytes, SHA256 C8B4592C03EF35A437BF7EB027B54D484DD333C48946EEF212D0B793D9F12704. APK is BUILT, NOT INSTALLED. USB device enumerated only; availability question unanswered, no launch/input/screenshot/profile access. Prior XP-only installed record remains current. No commit/push in this slice.

Next: coordinated fresh app-files backup, install and hash verification, temporary wide flag (preserve any existing value), physical traversal/visual review, remove only the created flag and return to normal hall. Playing normally may award/persist progress; never restore stale profile data. After that: local encounter-site gameplay and richer perimeter scenery.

## 2026-09-23 — Furnace clearance and physical exploration handoff

Moved furnace to an offset north-east ornamental foundation (x1.6, worldTop+.45); playable geometry unchanged. Actual Gameplay graphics now walk to the reachable tip and side using HeroMotion.EdgeMargin. An initial waypoint at raw corner27 failed as unreachable; fixed the fixture to use the existing0.6 margin rather than changing movement. Final captures show hero/furnace separation at both inspected limits. Fixture HUD overdraw remains an Editor capture limitation; this does not establish new device acceptance.

Verified: .NET380, compile clean, RegionSceneryTests2/2, graphics1/1. No full-suite/APK/device operation for this bounded layout correction; installed C8B4592C build still has original centred furnace. New OPUS_EXPLORATION_TASK.md covers two physical combat sites, optional single-use chest and unlocked boss entrance; drafted only, not sent or started. Existing region-map node task remains superseded. See region-runtime-01 updated screenshots and handoff.

## 2026-09-23 — Playable foundry integration (owner scope change)

Owner requested a playable build rather than more isolated art previews. Integrated fixed concave FoundryMap, shared floor rendering, conservative hero clearance/sliding, deterministic separated entry and twelve-node pursuit into development mode region-preview.txt=foundry. Camera follows at existing width; default hall and old wide mode preserved. Uses authored Descent combat/reward/choice/end loop; local encounter-site activation and separate boss-hall transition remain unimplemented. External Opus task remains unstarted.

Actual PackMotion corner stall exposed a sampled segment-check weakness; replaced with exact rectangle interval-union validation. New pure tests cover four-offset interior spawn sweeps, long-step hole crossing, route connectivity and real pack pursuit; scene test covers entry, authoritative pause and unload/default return. Gate387 .NET/402 EditMode/131 PlayMode, compile clean. See foundry-playable-01 README for detailed scope and final APK/device evidence. No full-run simulation parity claim for development geometry.

## 2026-09-23 � Owner correction: photograph owns the structure

Owner rejected the narrow linked-platform interpretation and explicitly requested the same structure as foundry-v1.png. This supersedes the earlier diagram-as-authority instruction for the foundry region. Current runtime now uses one broad court with an inset shaft on the left, a straight south entry bridge and a wide northeast gate apron. The concept remains the visual target; current rough masses are not final art or an exact reproduction of every painted detail.

FoundryMap now combines five axis-aligned rectangles with one exact diagonal bridge (u-v in[-4,4],u+v in[-50,-24]). Main court bounds[-18,-14]..[14,18], shaft[-12,2]..[-4,10], apron[12,-2]..[32,12]. Bridge predicates and continuous segment clipping share the rendered straight bridge. FoundryPursuit nodes moved with these regions. The old952-cell/seven-rectangle layout and old waypoints are historical.

FoundryRegionView adds deep sidewalls and regularly spaced exterior supports, low edge parapets, a central three-ring brass seal, perimeter furnace plinths, a simple crane silhouette and a northeast closed gate. Static faces batch by shared material/sorting layer rather than adding a renderer for every support. Mesh ownership remains local, materials borrowed, no per-frame geometry. The four furnace sprites are existing imported art. Decorative silhouettes still need final art, registered joints, lighting and occlusion polish. No elevated walking or functioning gate transition was added.

Verification: final pure suite388 passed, compile clean, graphics walk1 passed, integrity494GUIDs/568scene objects. Capture walkthrough legs29.4/30.6/18.2/16.8/11.7game seconds, attacks disabled in fixture. Refreshed overview.png uses a temporary overview camera; actual portrait camera stays width9. Android gate/install follow separately. Opus remains on hold; no commit/push.

## 2026-09-23 — Gate art implementation

New GateFacade.png and MAT_GateFacade in Props/Foundry; unchanged generated RGB source retained in gate-facade-01. PlatformMaterialSet gains optional _gateFacade; existing IsValid still checks only floor/coping/wall. FoundryRegionView.BuildGate builds a masonry body and dedicated facade plane; generated meshes owned by region, material borrowed. No Gameplay scene edit, geometry or gameplay change. Full gate403/131 passed. Art review source/prompt/captures in gate-facade-01. Sidewalls/outer columns and remaining environment still provisional.


## 2026-09-23 - Foundry furnace and masonry integration

Four detailed furnace elevations now wrap the existing scenery footprints, with exposed chimney geometry. Foundry-only masonry replaces the earlier slit-pattern walls on parapets, deep supports and architectural sides; ordinary hall materials remain unchanged. World-continuous wall UVs and painter-ordered chimney caps address cell seams and roof overlap. All art is borrowed from imported assets; static meshes are still batched and owned. Geometry, camera, combat and progression unchanged.

Sources, exact built-in image_gen prompts, import settings and actual Unity captures: ArtDirection/2026-09-23/foundry-surfaces-01/README.md. No manual raster cleanup. Final silhouette/pipework/crane/shaft dressing remains unfinished. Compile clean, graphics traversal1, static integrity500/568; full gate and device result recorded separately in docs22/23.


## 2026-09-23 - Complete foundry dressing package

Owner requested the remaining environment dressing in one integrated update. FoundryRegionDressing adds braced crane, pulleys, hanging salvage basket, pipe runs/collars/valves/supports, heated panelled shaft lining and molten basin, iron roofs, inner guardrails, beacons and suspension chains. Shared map geometry, camera, encounters and economy unchanged. Two unchanged built-in image_gen material masters imported as shared non-readable ASTC6/max1024 assets. Static code-native meshes give these materials the needed shapes; no new Update, texture generation or light system.

Review/source/prompt contract: ArtDirection/2026-09-23/foundry-complete-01/README.md. Actual integrated five-position Unity traversal and close-up render proof passed; scene uses15 static renderer groups. Crane attachment and shaft panel aspect corrected after close review. Scenery rank allocation keeps background/floor/contact shadows/actors consistent. Full gate and final phone state follow in docs22/23. Named dressing scope complete; no functional crane/lava hazard/boss transport is implied.


## 2026-10-07 - Bridge guardian package ready; device review blocked by USB

Integrated original ForgeKeeper.png (unchanged built-in image_gen source from ArtDirection/2026-10-07/forge-keeper-proof-01) as two identically oriented architecture cards. One shared optional material in PlatformMaterialSet. Replaced bridge-head fire baskets with carved keepers on expanded caps and below-deck consoles, moved the supports outside the complete walking union, shifted animated banners/brackets outboard, and removed the replaced lamps from floor illumination. Existing rail lanterns and bridge-foot beacons retained. Explicit structure footprints support actor depth and local hero cutaway; no gameplay, collision, camera, balance or save change.

Proof ArtDirection/2026-10-07/guardian-runtime-01: actual Unity bridge/overview, production portrait approaches and left-court behind-statue overlap. The right statue has no equivalent nearby walkable rear position; do not claim a symmetric device occlusion test. Independent image review found no alpha fringe, floor-covering console or join blocker. Supports remain outside the walking union; transparent card bounds remain outside bridge width. Two cards share exactly one new renderer; other region batches retain17 limit. Total18renderers /79,832vertices (previous17 /80,504); two pre-existing flame/cloth batches remain dynamic. Guardian adds no update loop.

Verified: .NET390/390, compile0warnings/errors, final graphics1passed, EditMode410/410, PlayMode145/145, Android exit0, integrity544asset/folderGUIDs /1099packageGUIDs /568sceneobjects. Expanded tests verify one8-vertex/12-index keeper batch, clamp/non-readable/POT texture, depth/fade flags, unchanged texture orientation, footprint clearance and scene-owned resource cleanup. Initial new portrait capture failed because its staged hero point was too near the rim; corrected capture placement to respect HeroMotion.EdgeMargin, no gameplay change. Android import512x1024 ASTC6x6,11mips.

Ready APK SHA25693380BACB4BFAE6BB17BFCF1FB1814E153E9524DEFAA8071F0C83959E5C49470 (31,113,605bytes on disk; Unity BuildReport totalSize is a different aggregate). NOT INSTALLED. Phone was available at first; read-only fresh backup TestResults/guardian-device-20261007-205856 succeeded with app stopped. By build completion ADB had no devices and Windows no Samsung/Android device entry; repeated discovery remained empty. Asked owner to reconnect. No install, game launch, phone input, diagnostic flag or save modification this turn. Last verified installed build remains basalt DD3E70BCF9E6835632E1A81D0DF0301D2B9D54087DEE1875E61A8780169D4F38. No new device performance claim.

Next on reconnection: inspect fresh state and refresh backup if changed; install the ready APK and compare installed hash; guarded staged bridge walk then ordinary launch/combat; remove temporary depth/render flags and compare profile/bak/original development-file hashes. Preserve original region/arrival settings. Do not rebuild unchanged content or call this package device-verified yet. No push.


## 2026-10-07 - Crane structure package installed

Code-native crane silhouette revision in FoundryRegionDressing.cs and crane-only FoundryRegionSite.cs: broader cast mast/jib members with plain iron sampling, sparse alternating braces and end joints, stronger hoist chain, larger open scrap basket/solid lower tray, filled mast bearing and winch hubs, plain drum with sparse winding bands. Existing anchors, footing, generic wheels and gameplay/collision/camera remain unchanged. No new texture/material/renderer/update loop. Region18renderers /79,288vertices, down544 from guardian baseline79,832; existing flame/banner batches remain dynamic.

Actual same-camera Unity before/after and production portrait: ArtDirection/2026-10-07/crane-structure-01. Portrait stages a reachable court point with HeroMotion.EdgeMargin and demonstrates local cutaway; restores hero position before square closeup so the drum remains visible. At the close production camera the jib extends behind HUD and the outboard basket approaches the left edge; no camera change was made to fit the complete crane. This improves local structure/readability, not full painted-reference fidelity.

Verified: .NET390/390, compile0warnings/errors, final graphics1passed including map traversal, EditMode410/410, PlayMode145/145, Android exit0, integrity544/1099/568. No new tests mirroring visual constants; existing region depth/ownership/budget guards pass. Installed S23 APK24BCDD64BD851636A0DCB4948ADAC7FCBACE2D19753A32A04497B3F56EC4E4AD (35,958,239bytes); base.apk SHA256 matched.

Fresh TestResults/crane-device-20261007-215052 backup. Guarded staged route reached bridge, then pit corner and west court beside crane; changed routing to go around the shaft, no off-map teleport/camera change. Phone court/edge captures show main members, bearing, drum and partial outboard basket; full basket verified in square Unity capture. Final staged windows t332-347:59.9fps,p50/p95=16.7ms,max16.7-16.9ms,GPU2.3-2.4ms. Not dense combat, midrange hardware or an isolated performance comparison. Sampled staged/normal process logs had no managed exception/crash pattern. After removing depth-proof/render-stats, normal Staff battle reopened and active combat was captured. Background flush then app stopped.

Profile,bak and all3 original development files SHA256-identical after review; no restoration, loadout change or permanent test flags. Existing region-preview/gate-arrival/start-floor.txt.off retained; test telemetry retained. Earlier guardian package93380BAC was also installed/verified this turn before the crane revision; this24BC package is now latest. No push. Next art work should address another visible material/silhouette limitation with a real comparison, not claim the concept is finished.

## 2026-10-08 - North court/workshop/paving implementation

FoundryMap gains two north chamfers shared by exact line clipping, movement/pursuit/spawn and FoundryRegionCorners floor/wall/rail rendering. FoundryRegionMassing moves the northwest lantern/bastion/chain; FoundryRegionWorkshop/Depth/Clutter/Finish consolidate stock support, all station props, footprint and soot near crane. Finish uses smaller ashlar and softer joints. FoundryMapTests extends removed-tip/long-step/pursuit cases to north cuts; FoundryRegionTests checks actual floor coverage. FoundryRuntimeCapture adds three real walk destinations, workshop view and per-mesh16-bit limit.

Verified: .NET395/Edit415/Play145, compile clean, graphics1, Android0, integrity545/1099/568. APK CD9510549BA6F748145AE7CF7A4D8ABBFF538A4B57948991692FD2C3FE2814C6 installed/hash matched on S23. Guarded north traversal and ordinary gameplay/loot inspected; profile/bak/original flags unchanged, temporary diagnostic removed. See docs23 and ArtDirection/2026-10-08/north-workshop-paving-01. Renderer18/vertices92,951; no new textures/materials/update loops. Gate elevation and final reference fidelity remain open.

## 2026-10-08 - Gate approach artwork

Implemented flush processional border/medallion/axial guides in FoundryRegionHeraldry, five dressed threshold stones and bindings in FoundryRegionArchitecture, and removed competing apron guides in FoundryRegionView. Existing batches/materials only; no gameplay/height/camera change. Region18renderers /94,727vertices. Verified .NET395/Edit415/Play145, graphics1, compileclean, Android0, integrity545/1099/568. Installed/hash-matched APK C5C78DF97A5090830159C295D262DE935F8540E39C60920740EB8D5E34D6E0F7. Guarded S23 panel/threshold walk plus normal combat readback; profile/originalflags unchanged, diagnostic removed. Proof gate-procession-01; details docs23/38.

## 2026-10-08 — Gate elevation integration

FoundrySurface supplies design projection, inverse and world-contact lift. FoundryRegionView/Finish/Architecture/Massing/Corners and atmosphere adopt the surface; paving UVs remain authored on the original design plane. FoundryVisibilityView applies height/depth only at camera render, restores complete positions and owns FoundryGroundProjection's cached sprite grids/material. CombatEffectsView retains full floor contacts for hit/death/chest effects and numbers; ground pulses use the surface shader. ArenaCameraFollow follows visual feet; ExitSealView places its cue at surface height. Gameplay roots, floor rules, save/profile data and scene assets unchanged. FoundryElevationTests verifies real generated paving, actors/bars/chest, numbers/effects, restore/disposal and camera; FoundrySurfaceTests covers inverse round-trips, seams and unchanged court. Tools/ArtReview/FoundryRuntimeCapture includes both ramp seams. Evidence and final verification: gate-elevation-01 and docs22/23/38.


## 2026-10-08 — Three-task foundry surface package

Modified FoundryRegionFinish (stone variation/joints/bevels), FoundryRegionMassing (formed boiler hood courses/seams), VoidBackdropView (owned foundry-only material). Added Resources/FoundryCavern.shader and PlayMode/FoundryCavernTests with new metas. The lifecycle test builds two foundry backdrops and an ordinary hall with shared art, destroys foundry views independently and verifies owned materials release while the borrowed sprite/texture and other view survive. Existing region geometry/depth/budget, floor light and gameplay suites remain the regression checks. Actual Unity capture uses unchanged cameras and traversal harness. No scene, input, collision, balance, save, texture or import changes. Final verified counts and APK/device evidence follow in docs22/23/38.


Verified: .NET 395 passed; compile 0 warnings/errors; final actual graphics capture 1 passed; EditMode 415 passed / PlayMode 149 passed (including new owned-material lifecycle test); Android exit 0; final integrity 552 asset/folder GUIDs /1,099 package GUIDs /568 scene objects/components. Static region 18 renderers /95,622 vertices (+832, about0.9%); largest mesh27,373. APK36,061,418 bytes; SHA-256 `BA39FFFAB4EB3D650CBC0609B7211DF6BCDA3990EDCCE1655EE45519B8CFCA67`; installed S23 base.apk hash matched. S23 approach/ramp and normal combat reviewed; profile/backup/original flags unchanged, temporary depth-proof removed. Proof: `ArtDirection/2026-10-08/surface-cohesion-01/`.


## 2026-10-09 — Industrial metal light integration

FoundryRegionWeathering adds shared reflected-light sampling and bounded metal tint. FoundryRegionMassing opts boiler courses/discs and hood panels/seams into the treatment and passes explicit height into pressure feeds. FoundryRegionDressing applies it to service mains/collars and reservoir courses/caps; only lit long tubes subdivide for intermediate samples. FoundryRegionTests adds a regression for warm-near/cool-high behavior, ambient outside source range, continuous ramp seams and opaque bounded color across the region. No new asset, scene, camera, input, movement, save or balance changes. Actual matched Unity captures/traversal passed; full verification and installed state follow in docs22/23/38.


Verified: .NET 395 passed; compile 0 warnings/errors; actual graphics capture 1 passed; EditMode 415 passed / PlayMode 150 passed; Android exit 0. Final integrity: 552 unique asset/folder GUIDs, 1,099 package GUIDs and 568 scene objects/components; references and JSON valid. Static region: 18 renderers /96,558 vertices (+936, about 0.98%); largest mesh 27,373. No new renderer, material, texture or update loop. APK 36,063,810 bytes; SHA-256 `43BD507FD72AE0D91093D96BA86771C172B0DB7C00006BBE993EAE6A11743CDD`; installed S23 base.apk hash matched.


## 2026-10-09 — Worked stone and foundation recesses

Files: FoundryRegionFinish.cs (within-stone wash, edge flakes replacing tiny corner repairs) and FoundryRegionMassing.cs (blind recesses between banded belts and beside buttress ribs). Existing floor coverage, UV/elevation, color, geometry budget, depth, traversal, ownership and combat tests are retained. No new test duplicates these reversible art choices. Actual graphics capture passed;18renderers /99,988vertices (+3,430). Final verified counts/APK/device evidence follows after the gate.

Code review found one threshold flake spanning the ramp crest (u30.756..31.505 across31). Exclude flakes from flags crossing either grade break. Strengthen the existing FoundryElevationTests test with actual paving triangle-centroid/UV comparisons against FoundrySurface, not only vertex comparisons. Initial passing results are retained under TestResults/stone-relief-initial-*; provisional APK not installed. Final capture and full gate follow this correction.


Verified: .NET 395 passed; compile 0 warnings/errors; final actual graphics capture 1 passed; EditMode 415 passed / PlayMode 150 passed, including the strengthened triangle-interior elevation assertion. Android exit 0. Final integrity: 552 asset/folder GUIDs /1,099 package GUIDs /568 scene objects/components, resolved references and valid JSON. Static region:18 renderers /99,828 vertices (+3,270, about3.4%); largest mesh30,443. No new material, texture, renderer or per-frame work. APK 36,067,857 bytes; SHA-256 `209FC325C9E98DAA292EE10AE9F16AE350D53590774FB61BEA6CB8E7C24D5D70`; installed S23 base.apk hash matched.

Final evidence: ArtDirection/2026-10-09/stone-relief-01. Device status recorded in doc23; the final99,828vertex budget supersedes the initial99,988 capture.


## 2026-10-09 — Three-agent architectural package

Files: FoundryRegionArchitecture.cs, FoundryRegionWorkshop.cs, FoundryRegionUndercroft.cs. Gate:8 stone lancets in existing pier faces and dressed roof sampling. Workshop:round stored pipes, oval bores and local plain-metal UV/light on stock/hulls/coils. Undercroft:spandrel fill, continuous ring shading, soffit and bevel. Three agents implemented isolated files, then cross-reviewed source and actual captures. Root alone runs Unity/adb. Existing test suite retained without duplicate art-shape tests. Actual capture1 passed;18renderers /102,016vertices (+2,188/about2.2%). Final gate and device result follow.


Verified: .NET395 passed; compile0warnings/errors; actual graphics capture1 passed; EditMode415 passed / PlayMode150 passed; Android exit0. Integrity:552 unique asset/folder GUIDs /1,099 package GUIDs /568 scene objects/components; references/JSON valid. Static region18renderers /102,016vertices (+2,188/about2.2%), largestmesh30,443. APK36,068,501bytes; SHA-256 `0CA694C76762E3F5D855AADF43BDF69E38144FB85DC4B4E075A10FCC0677380C`.

Device installation and review are pending. USB is connected and keyguard was not showing, but the phone was on NotificationShade. Root asked whether the device would be free for installation/traversal and short performance measurement; no reply arrived during this work. No installation, app launch, gameplay input, save writes or device captures were performed for this package. Latest verified installed art remains the preceding stone-relief package; do not label this package phone-verified. Once availability is confirmed, recheck for an active owner run, take a fresh backup, install/hash-match the ready APK, run guarded gate/normal checks, then optionally the short no-capture traversal measurement. Prepared local scripts: TestResults/architecture-cohesion-device-review.ps1, -normal.ps1 and -perf.ps1. No fresh device backup exists yet; never reuse the previous session snapshot. No new FPS claim; prior first-use hitch and sustained/midrange acceptance remain open.


## 2026-10-09 — Architecture-cohesion installed after owner availability confirmation

Owner explicitly made the phone available and authorized pushing completed commits. Installed/hash-matched ready APK `0CA694C76762E3F5D855AADF43BDF69E38144FB85DC4B4E075A10FCC0677380C` (36,068,501 bytes); no rebuild. Fresh backup `TestResults/architecture-cohesion-device-20261009-133914`, with app stopped on unlocked Home before install. Guarded gate approach, threshold and descent reviewed; normal Staff run separately showed Room1,90HP,4unbanked gold, damage7 and loot. Profile, backup and three original development flags hash-identical. Temporary depth-proof/render-stats removed, test telemetry retained, app stopped. No selected exception/crash pattern in sampled logs.

Short no-capture traversal measurement: after the startup window,59.5–59.9fps, one50.0ms frame; GPU averages3.9–4.6ms. Initial startup window contained2272.6ms, which is not a steady-state frame claim. The50ms traversal hitch remains unisolated; this does not close cold-first-use, sustained crowded-combat or midrange acceptance. Captures/log in `ArtDirection/2026-10-09/architecture-cohesion-01/device/`. Supersedes pending-device status above.


## 2026-10-09 — Material readability implementation

Files: FoundryRegionWorkshop.cs, FoundryRegionMassing.cs, FoundryRegionHeraldry.cs. Wider continuous existing-iron sampling and axial pipe grain; bounded mill-scale/lower-wall deposits; continuous pit facing/contact soot and attenuated side heat wash; matte inlay brass, softer stone grooves and deterministic floor-space wear. No layout/UV topology/depth/camera/gameplay/save/source-image/import/shader change; pipe topology unchanged. Static region18renderers /102,100vertices (+84/about0.08%); largestmesh30,443. Existing support, UV, light-bound, elevation, depth, lifecycle and combat tests retained; no test merely mirrors reversible art settings. .NET395, clean compile and actual graphics1 passed. Full gate/device record follows on completion.


## 2026-10-09 — Material readability verified and installed

Completed together: broader continuous iron sampling and restrained stock deposits; curved pit lining/contact shadow; matte brass and quieter stone grooves. Files FoundryRegionWorkshop/Massing/Heraldry.cs. Existing source textures, floor/layout/depth/camera/gameplay/save behavior retained. Three-agent source and actual-image cross-review found no blocker; before/after gain is restrained, not painted-reference completion.

Verified .NET395, compile0warnings/errors, actual graphics1, EditMode415, PlayMode150, Android exit0. Integrity552unique asset/folderGUIDs /1099packageGUIDs /568sceneobjects, valid refs/JSON. Static region18renderers /102,100vertices (+84/about0.08%), largestmesh30,443. Installed/hash-matched S23 APK36,072,971bytes; SHA-256 `1B79022F776AA523259193DD64532CFB0D369EF4A63620B1B3A3A2FF0BE287C6`.

Fresh backup `TestResults/material-readability-device-20261009-135248`. App was stopped/unlocked before install; no owner run interrupted. Guarded real drags reviewed the west stock shelf, pit collar/wall and gate route; separate normal Staff run showed Room1,90HP,4unbanked gold, damage7 and XP/gold/chest. First route image named pit-front shows central court instead and is excluded from pit proof; a subsequent targeted route reached the actual pit wall. No teleport/camera override. Profile.json,bak and three original development files SHA256-identical after normal run and both performance passes. Temporary depth-proof/render-stats removed, original owner flags and test telemetry retained, app stopped/released. Selected crash/reference/index/invalid-operation/shader error patterns absent in sampled logs.

Performance limitation: first short no-capture pass had one216.9ms frame (main214.5ms) near first ramp entry; other post-startup windows59.9fps. Startup contained2259.5ms and is explicitly separate. A fresh-process control plus same-process full descent/reclimb had first-entry33.4ms; the repeat phase remained59.7–59.9fps with maximum33.4ms, no repeat of the217ms stall. Control startup also had300.1ms and2309.2ms, and reported initial refresh30Hz; do not treat the two launches as identical cold-driver conditions.

Read-only source diagnosis: FoundryGroundProjection.Apply lazily creates a material and16x16 sprite grids at first ramp overlap. Grid arrays total5384bytes, compatible with window allocation peaks, but the actual217ms frame reported only0.3KB, so allocation, shader/driver initialization or external stall causation is NOT established. All new art code runs once at region construction. Next focused engineering task: profiler markers around material/resource creation, grid creation/OverrideGeometry and first draw; compare first/warm crossings before selecting controlled-loading prewarm. The earlier first-use hitch remains open; no sustained crowd or midrange acceptance claimed.

Proof `ArtDirection/2026-10-09/material-readability-01/` contains matched Unity views, actual device stock/pit/normal views and both raw performance logs. Broad surface fidelity, actual encounter/boss transition, actor articulation/roster and extended acceptance remain open.

## 2026-10-09 — External Blender furnace workflow study

Owner agreed to continue without a new paid service. Built one editable furnace in portable Blender4.5.9LTS (official ZIP checksum verified), using authored geometry and procedural materials. Sources: Tools/ArtReview/BuildBlenderFurnace.py; BlenderFurnaceCapture.cs; Run-BlenderFurnaceProof.ps1. Proof: ArtDirection/2026-10-09/blender-furnace-01 with .blend, mesh-only FBX, transparent render and actual isolated Unity A/B at1200/360px. No runtime Assets, settings, packages, saves or device changes.

Evaluation: candidate improves rounded-metal lighting/bevel coherence, but existing arched openings are architecturally richer. Candidate remains too clean/regular and is NOT accepted as a production replacement. Lighting and geometry differ, so this is a workflow comparison, not a material-only A/B. Model9,512vertices/18,432triangles; FBX lacks baked node-material appearance. Sprite study has no production depth/cutaway, atlas/compression or device acceptance. Next candidate should preserve arched openings, improve restrained soot/heat variation, then prove one map placement before any wider migration. No new subscription/API spend; authoring/integration work remains.

Initial capture compile failed because ImageConversion is intentionally absent. Fixed tooling with external lossless BGRA decode and BMP capture/PNG conversion; no module change. Final graphical PlayMode capture1/1passed; static integrity552asset/folderGUIDs,1099packageGUIDs,568sceneobjects, valid references/JSON. Temporary capture test/meta removed. No full gameplay/Android rebuild needed or claimed for this tooling-only change. Phone retains material-readability8ffec82; first ramp hitch and full reference fidelity remain open.
## 2026-10-09 — Arched Blender furnace and staged map depth proof

Completed together: individually modelled arched fire mouths/jambs/grates, upper inspection ports, cooler basalt and procedural bottom-weighted coal heat. Fire is a static bake. Source Tools/ArtReview/BuildBlenderFurnace.py now reproduces study02; study01 generator remains at8480016. Actual Unity isolated1200/360px comparison and temporary court placement using the existing cutaway shader. Real hero staged front/back at frozen time: front focus toggle0changed pixels; behind toggle22,410changed pixels with hero visibly revealed. Added1080x2340review using production Frame dimensions/width, HUD hidden and camera recentered; not device footage or normal-follow traversal.

Source model11,928vertices/23,104triangles; Unity probe uses4vertices/2triangles and1200x1200uncompressed bake (5.76MB base RGBA, excluding overhead). It does not render the full study mesh. Actual graphic PlayMode1/1passed; static integrity552/1099/568passed. Temporary test/meta removed. No runtime Assets, settings, package, gameplay or phone changes; no Android rebuild/full suite claim. Existing installed8ffec82and first-ramp hitch remain unchanged.

Review: arches/fire restore design character, metal has coherent broad light; candidate still cleaner than target reference. Next integration requires ground/contact shadow, environment light/soot, service pipe/foundation matching, side-approach/neighbor depth and mobile compression acceptance. The temporary added court prop is not a production replacement or collision proof. Sources and evidence: ArtDirection/2026-10-09/blender-furnace-02. No new paid service/API.
## 2026-10-09 — Single baked north boiler integrated; installed, device visual review pending

Replaced only north boiler index0 at design(18.39,15.81) with the Blender study02 bake. Original source PNG retained byte-for-byte in Resources/FoundryBoiler.png; imported1024square ASTC6x6,11mips,non-readable,Clamp. Calculated block payload626,288bytes including mips, excluding engine/native overhead. FoundryRegionBakedBoiler emits two static bands at the existing drum/iron ranks inside the unchanged MassDepth footprint. Scene owns source material and cutaway variant; shared texture is asset-owned. Foundation, pipe endpoints, smoke anchor, other boilers, movement/layout/gameplay/saves/camera remain unchanged.

Initial full PlayMode run149/150caught a second floor-decal batch violating existing ordering. Fixed by merging contact shade into the existing Coping/floor-decal batch, preserving the original test. Final .NET395passed, EditMode415passed, PlayMode150passed; actual graphical capture1passed; Android exit0; static integrity554asset/folderGUIDs,1099packageGUIDs,568sceneobjects/components with valid references/JSON. Static region20renderers /101,392vertices (prior18 /102,100). Two added renderers,708fewer vertices; this is not an FPS claim. No new per-frame component. Graphics show matched old/new construction, intact pipe/base contacts and four validated walkable approach points with production follow width and HUD hidden. No temporary courtyard prop in these views.

APK36,074,875bytes; SHA256 DA3961080FB0823D60C230DDC0C883C39F89AE31C15A24D9FA405773375FA5D3. Owner authorized installation/testing. Later read-only window check showed YouTube in front, so root asked whether opening the game could interrupt use; no further answer before this record. Background installation proceeded under the existing installation authorization while Cryptforge was stopped; no input, app launch or screenshot. Fresh backup TestResults/baked-boiler-runtime-device-20261009-181315. adb install-r succeeded, installed base.apk hash matched; all11files in fresh before/after-install snapshots SHA256-identical. Final foreground was Launcher. No diagnostic flags written. Phone visual/traversal/performance acceptance remains pending; do not claim this package was played or benchmarked on S23. The prior ramp first-use hitch remains open.

Proof: ArtDirection/2026-10-09/baked-boiler-runtime-01. Resume with fresh device focus/keyguard/active-run check and the pending availability reply; installed APK does not need rebuilding. Then guarded north-boiler route, normal combat, cleanup/save hashes and optional short no-capture measurement. Prepared ignored device scripts are starting points only: device-review currently includes reinstall/backup, so reuse or split carefully rather than blindly replacing the known install snapshot. Full painted-reference fidelity and other furnace variants remain unfinished.

## 2026-10-09 — Accepted single boiler; shared north pair

The preceding single-boiler device-review checkbox is now closed: guarded S23 movement, normal run and fresh profile/development-file hashes passed; evidence in baked-boiler-runtime-01/device. Short stationary samples do not close the separate first-use ramp hitch or sustained/midrange acceptance.

The second tall north boiler now shares the accepted study02 texture/material and two depth bands. Both preserve independent mass ownership; short east boilers keep their original distinct design. Actual Unity pair comparison and four walkable approaches passed; region remains20renderers and drops to100,684vertices. Full gate and device results follow in the pair handoff. Proof: ArtDirection/2026-10-09/baked-boiler-runtime-02.

Next substantial art work: author a distinct short-boiler variant using the same editable Blender workflow, compare it at actual game size before integration; then address coarse foreground pipe shading against the new baked forms. Do not replace the whole map indiscriminately. Full reference surface richness, actor/encounter art and first-ramp engineering investigation remain separate open work.


## 2026-10-09 — Distinct short boiler artwork and runtime pair

Authored the low foreground variant with broad segmental fire doors, pressed lid and short vent, using the committed study02 Blender material/light setup. Script Tools/ArtReview/BuildBlenderShortBoiler.py; editable source ArtDirection/2026-10-09/short-boiler-01. Authoring mesh6,704vertices/13,036triangles is baked, not imported as runtime geometry. Existing height2.28/radius2 design, walkability, foundations, service sockets and smoke anchors are retained.

Both short boilers share one new1024ASTC6x6 texture with11mips and two eight-vertex depth bands; scene source material and visibility variant retain explicit ownership. Original sockets are runtime geometry; shades share floor decals. Two bands replace one now-unused legacy furnace-wrap batch, so region20->21renderers and100,684->99,492vertices. New texture calculated payload626,288bytes including mips, excluding engine overhead. Existing tall pair unchanged.

Actual Unity matched legacy/new pair, four validated walkable portrait approaches and overview: ArtDirection/2026-10-09/short-boiler-runtime-01. Legacy constructor/resource retained for fallback/comparison; import-use assertion explicitly checks retired wrap absence and replacement short bands. Full device/gate results are in the latest docs22/23/38entry. Fire/light remain static baked; pipe shading still needs a separate coherence pass. No paid service. Full-reference completion, actor/encounter work and earlier first-ramp hitch remain open.

## 2026-10-09 — Pressure equipment, pit hoods and architectural seating

Completed a combined visual slice with three helper agents: formed service pipes and high feeds; two Blender-baked pressure reservoirs; continuous formed-metal pit hoods; quieter gate roof/header sampling; better-seated keeper mounting slabs. Existing layout, walkability, foundations, gameplay and artwork anchors remain. Gate and keeper gains are deliberately modest; pit metal is smoother but plainer, not reference-complete ornament.

Reservoir source: Tools/ArtReview/BuildBlenderReservoir.py and ArtDirection/2026-10-09/reservoir-01. The 2,952-vertex authoring model is rendered to a shared1024source and imported at512ASTC6x6,10mips,non-readable. Calculated block payload158,432bytes including mips, excluding engine overhead. Runtime uses two quads with explicit2x2mass ownership; far/main/near ranks survive batch quantization. Near relief geometry has its own existing-iron rank. New source material has scene cleanup; no new per-frame visual component.

Whole region changes21renderers/99,492vertices to24/100,572 (+3renderers,+1,080vertices). Pressure slice alone24/99,444; pit/header24/100,548; keeper seats add24vertices with no renderer. Actual source-state before/after galleries: pressure-assembly-01, foundry-finish-01 and keeper-seat-01 under ArtDirection/2026-10-09. Graphics use staged heroes and hidden HUD, not live device traversal. Historical comparison capture scripts require the corresponding old source state; they do not recreate legacy props in the latest scene.

The first full PlayMode run caught a genuine entry timing issue: the first reveal iteration counted the preceding scene-loading frame. GateArrival now measures four seconds from view capture on a monotonic unscaled clock. A regression supplies an actual1.2-second preceding frame and requires4..6seconds from capture, preserving skip/handback/encounter guards. Original natural-completion threshold was not weakened. Final full-suite, build and device evidence is recorded separately in docs22/23/38.

Forge keeper source provenance correction: the older prompt.txt contains literal undefined and a generic tool note; the complete original generation prompt was not preserved. README now states this explicitly; no prompt was invented.

Next substantive art work remains broader authored surface richness and encounter/boss presentation, followed by actor articulation/roster. Do not spend repeated slices on barely visible bevels. Full reference fidelity is not complete; cold-first-ramp hitch, sustained crowded-combat and midrange acceptance remain open.
## 2026-10-10 - Foundry approach, gatehouse and yard

FoundryMap/Pursuit share expanded bridge bounds with RegionView/Site/Massing and endpoint lights/chains. Added FoundryRegionBridgeSupports, FoundryRegionGateEnclosure and FoundryRegionProductionYard partials; Architecture and Workshop hook their respective features. No scene or gameplay progression edit. New far-end/pursuit checks and expanded spawn/render/fixture coverage verify the real footprint. Bridge presentation clips at the court seam while collision retains its overlap.

Verified: .NET409, EditMode429, PlayMode151; three graphics passes; integrity568/1099/568; Android exit0 with no C# warning/error matches. APK36,146,073 bytes; SHA256 AB290A8824C5C3FF91D822DDC50C6F19C0A254BCD1392E76B4DC462583B2789F. Not installed: owner-run restart permission pending. Full evidence/reproduction: ArtDirection/2026-10-09/foundry-approach-01.

## 2026-10-10 - Art-first courtyard craft package

Owner explicitly chose reference-map art before further gameplay/region progression. Three coordinated slices are visually accepted: mixed ashlar workyard/court with broad mineral families and contained corner wear; broad tapered-face gate recesses with dressed caps/quoins; stock-shelf hoist with bearing-supported drum, braces, two chains and lifting yoke. Existing layout, walking routes, doorway, camera and gameplay remain unchanged. Stock feet stay within the original non-walkable shelf and upper machinery has explicit3.6-height depth ownership. No new texture, material or per-frame component.

Actual Unity before/after evidence: ArtDirection/2026-10-10/foundry-craft-01. Region25renderers unchanged;113,622 to117,754vertices (+4,132/3.64%). Yard and gate gains are clear; central paving delta is moderate. Fine irregular stone grain and richer metal surface remain below the approved reference; this is not map-art completion. Local encounters, boss transition and optional finale implementation remain deferred. Full tests/build/device status belongs to proof validation/verification.md and docs22/23/38.
## 2026-10-10 - Materials package implementation

Added BuildBlenderStockHoist.py, editable stock-hoist-01 source, FoundryStockHoist resource and FoundryRegionBakedStockHoist partial. StockHoist fallback guard/View destruction hook integrate it; FoundryRegionTests cover import/card/depth/rank and cleanup. GateEnclosure adds finer masonry/material treatment. PavingWear and Finish replace coarse edge wear and remove its unused helper. No scene, map bounds, camera, movement or progression edits. Root alone ran Unity; three helpers worked on disjoint slices. Capture tools: Run-FoundryMaterialsProof.ps1 / FoundryMaterialsCapture.cs. Exact final results in foundry-materials-01/validation/verification.md.
## 2026-10-10 - Workshop art continuation

Implemented authored stock and crane replacements, deterministic surrounding ordering, explicit depth ownership and owned-resource cleanup. A real image-review regression was corrected in the rear northwest lantern masonry; original live light, chain and basket remain. Current camera, walking footprint, gameplay, progression and saves are unchanged. See ArtDirection/2026-10-10/workshop-coherence-01/validation/verification.md for exact final gates and phone status. Further reference composition work remains separate from deferred encounter/progression changes.

## 2026-10-10 - Court/brass/pit surface package

Implemented compact ashlar mix and softer joints (FoundryRegionFinish/PavingWear), readable matte ceremonial metal (Heraldry/View), contained shaft flues and faceted vessel crowns (Massing/PitHoods). Same camera, walk bounds, progression, combat and saves. First flue review prompted quieter backing/ties and a narrower deeper heat slot. Actual matching before/finalafter accepted; floor gain is modest and irregular reference flags remain future work.

Verified:409 .NET, clean compile0warnings/0errors,432 EditMode,151 PlayMode (992total), graphics1before+2after, integrity577/1099/568. Android exit0/no C#diagnostics. APK36,165,000bytes; SHA256199F9356AE439D119C5F5EDCFD0A95810D4C46841D1A74897710E65864C1CE54. No phone installation this turn; prior restart decision pending. Evidence/reproduction: ArtDirection/2026-10-10/foundry-surface-cohesion-01.

## 2026-10-10 - Hand-quarried paving contour integration

Added FoundryRegionFlagContours.cs and one guarded path in AshlarFlagFace: varied convex silhouettes, safe inward bevels and adaptive-light-cell face clipping over complete mortar beds. No layout, collision, navigation, camera, gameplay or profile changes. Regular ceremonial/repair/infill and cross-ramp flags retain the established path. Added14meaningful EditMode cases for convexity/inset safety, clipping area, protected flags and emitted mesh UV/elevation/coverage. Actual image review accepts the contour gain while recording stronger mortar lines and remaining rectangular courses.

Final gate409pure/446Edit/151Play (1,006total), clean compile, graphics1before/1after, Android exit0/no C#diagnostics, integrity579/1099/568. Region29renderers/113,995vertices. APK36,180,484bytes; SHA256 AA3859B559D48F21E3971A2A4ACDF1B14E07890D92EC45A27867F419A6E559E0. No phone install or fresh observation; prior restart/install question pending. Exact evidence and initial corrected integration failures: ArtDirection/2026-10-10/foundry-flagstone-01/README.md.

## 2026-10-10 - Distant works integration complete in Unity

Root and independent final Unity review accepted five world-anchored towers, a broken arched span, atmospheric separation/lower-shaft fade and quieter paving joints. The first capture was rejected for crisp floating bases and foreground-like brick contrast; final haze/material/UV changes resolve those issues. One scene-owned material borrows existing wall art, with owned mesh/material cleanup and explicit cavern -21 / masonry -20 / slits -19 / island >= -18 ordering. Normal backdrops retain old ranks. Region 29 to 31 renderers, 113,995 to 115,042 vertices (+1,047/about 0.92%); no new textures or per-frame component. Counts do not establish frame-time or allocation performance. Walking bounds, camera, combat and saves are unchanged.

PASS: 409 pure .NET, 446 EditMode, 152 PlayMode (1,007 distinct-suite tests; no failures/skips in the final gate). Three focused region cases also passed and are included, not double-counted. Compile check: 0 warnings/errors. One before and two successful after graphics captures; before/final-after XML retained. Android exit 0, no C# warning/error or shader-error matches. Final static integrity: 581 asset/folder GUIDs, 1099 package GUIDs, 568 scene objects/components; valid references/JSON.

APK: 36,185,695 bytes; SHA256 357024F64DC7D9C040486CF85FE4B00978DBDE9273454E3C0894F68780B5D595. NOT INSTALLED. Device review remains pending; no phone actions this pass. Evidence: ArtDirection/2026-10-10/foundry-depth-01. Gameplay progression stays deferred; next reference gap is richer working-yard composition and architectural forms.

## 2026-10-10 - Casting workbay integration

FoundryRegionCastingBench adds the supported scenic shelf, borrowed Resources/FoundryCastingBench texture, owned source material and explicit non-landmark card depth. FoundryRegionView builds it once and destroys the owned material; existing mesh/visibility cleanup owns the rest. Missing art falls back to a bounded simple bench in the same shelf. No scene, map/collision, economy, input, progression or save changes. FoundryCastingBenchTests covers real scene projection, UV/depth, raised support coverage, imported texture lifetime, owned resource teardown, outside-walk placement and north passage clearance. Region budget adjustment matches measured32renderers/115106vertices. Final tests/build/device evidence: ArtDirection/2026-10-10/foundry-workbay-01/validation/verification.md.

## 2026-10-10 - Distant furnace architecture implementation

FoundryRegionDistantWorks alone adds recessed core/facade geometry, dressed eight-piece arch frames, chamfered jambs, tapered upper shafts, short gradient fire panes and stronger broken-span supports. Existing two batches, ownership/cleanup, world anchoring, background ranks and atmosphere remain. Distant meshes contain 2,276 vertices, below the unchanged 2,500 ceiling; region remains 32 renderers. No scene, navigation, combat, camera, save or per-frame behavior changes. Existing FoundryDistantWorksTests exercises actual camera-independent placement, ranks, finite bounded meshes, unchanged budget and borrowed/owned lifecycle; no art-constant mirror tests added. Final gate and evidence: ArtDirection/2026-10-10/foundry-skyline-01/validation/verification.md.

## 2026-10-10 - Gate and ceremonial court integrated

Visual-only combined slice in FoundryRegionArchitecture/Header/Heraldry/Finish: supported stepped crown, continuous ashlar gate piers/deep returns; single radial stone medallion; flush dressed approach border. Existing materials, depth ownership, facade, bounds/ramp/camera/combat/saves preserved. Three helpers split gate, court and independent review; root alone ran Unity. First border visibility and second overlapping-corner findings were corrected; final shared miter joins accepted in actual Unity detail/portrait views. Region32renderers unchanged;116326->115944vertices. No frame-time claim.

Verified:409.NET/446EditMode/154PlayMode (1009distinct cases), zero failures/skips; compile0warnings/errors; graphics1before/3after; integrity584/1099/568; Android exit0/no C# or shader diagnostics. APK36196072bytes; SHA256 EB02AA8CB3BC649753FE9367E1599ECE99B1AE9C2CFCB24E3D87A3CD84243119. NOT INSTALLED. Evidence/reproduction: ArtDirection/2026-10-10/foundry-gate-court-01, baselinef76b8f3. No gameplay/simulation change.

## 2026-10-11 - Foundry stonework package complete in Unity

Finer mixed ashlar weights, narrower quiet joints, removal of redundant slab cloud wash and shared world grain span 5.7 replace 8.55 in foundry floor emitters. Existing contour/ramp tests keep their projection/coverage assertions and use the shared span. Gate lintel/crown/returns gain bonded courses and restrained edge wear; niches map continuously. Original gate silhouette, doors, banners, courtyard medallion, approach border and walking/ramp geometry remain. No camera, gameplay, scene or save change.

Built-in image_gen V4 edit produced versioned FoundryPavingV5.png, imported unchanged; older textures/GUIDs preserved. MAT_FoundryPaving alone changes its texture binding. Source 1254 square RGB; imported 1024 square ASTC6x6, 11 mips, non-readable. Mean RGB 87.76/80.85/84.14 -> 85.92/79.70/82.13: slightly darker. Full prompt and provenance: ArtDirection/2026-10-10/foundry-stonework-01/prompt.txt.

First actual Unity review rejected cloudy paving and overly blank upper caps. Final finer mineral texture and real clipped bevelled coping blocks resolve those blockers. Root and independent review accept final portraits/details; overview gain is subtle, not full reference fidelity. Region remains 32 renderers; 115,944 -> 118,082 vertices (+2,138/about 1.84%), one new texture asset, no new material instance or per-frame component. No performance claim. Three helpers split floor, gate and independent QA; root alone ran Unity. Before captures are reused unchanged from accepted baseline 22e32ea; two after captures ran. Tools/ArtReview/Run-FoundryStoneworkProof.ps1 and FoundryStoneworkCapture.cs reproduce the after state. Comparison gallery/evidence: ArtDirection/2026-10-10/foundry-stonework-01. Browser switching/image loads verified.

Verified: 409 .NET, 446 EditMode and 154 PlayMode = 1,009 distinct tests, zero failures/skips. Compile 0 warnings/errors, final graphics passed, static integrity 585/1099/568 valid, Android exit 0, no C# or shader diagnostics. APK 31,210,207 bytes; SHA256 1C0C01119360CEE32312ADDD9FA97C1202F8FBDB0857F5C1D4D04CDBB9AC3196. NOT INSTALLED.

No ADB action or fresh device observation. Earlier concrete owner-run restart/install question remains pending; last recorded package 021b95f and Room 2 / 74 HP / 8 unbanked gold are historical. On authorization take fresh preflight/current-file backup, install and hash-check the latest accumulated package, review map and normal combat, and preserve current original saves/telemetry. Never restore stale saves. Physical-device/crowd/midrange/first-ramp acceptance remains open. Regional encounter/boss progression remains deferred under the owner art-first direction.

Next reference gap: more naturally varied paving rhythm and architectural richness, particularly the simple central gate housing and supporting stone masses. Do not describe the material pass as complete reference fidelity or start another isolated color tweak without matched evidence.
## 2026-10-11 - Portal and exterior parapets complete in Unity

Combined art slice: supported recessed gate cartouche/iron lock and exterior stone-sill/square-iron parapets. Existing door/crown, boundary/rail top, pit/bridge balustrades and depth owners preserved. Three runtime partials changed; no new texture/material/per-frame component. Root and independent visual/code review accepted actual captures. Region32renderers unchanged;118082->117629vertices (-453/about0.38%), not a performance claim. Map/ramp/camera/combat/scene/saves unchanged.

Evidence/gallery/reproduction: ArtDirection/2026-10-11/foundry-portal-parapet-01. Baselinee61ac5a before images reused unchanged and hash-checked; one new after graphics capture passed. Browser comparison verified. Final gate:409.NET/446Edit/154Play =1009distinct tests,0failures/skips; compile0warnings/errors; static585/1099/568; Androidexit0, no C# or shader diagnostics. APK31210207bytes; SHA2568EB463B2E82A24CCFA1C827B681CCC5FC27F385CF03604D612E05058462C0EBD. NOT INSTALLED.

No ADB action/fresh observation. Earlier concrete owner-run restart/install decision remains pending. Historical021b95f and Room2/74HP/8unbankedgold are not fresh device state. On authorization inspect fresh, back up current files, install/hash-check latest accumulated APK and preserve current original saves/telemetry. Never restore stale snapshots. Phone/crowd/midrange/first-ramp acceptance remains open; region encounter/boss progression stays deferred under art-first direction.

Next substantial reference gap: regular long perimeter bays and surrounding building variety; paving rhythm remains regular. Portal relief is mainly a close-view gain. Do not claim complete reference fidelity or repeat isolated trim/color changes without matched evidence.

## 2026-10-11 - Distant works and perimeter rhythm complete in Unity

Two runtime files: FoundryRegionDistantWorks.cs adds supported tapered shoulders/necks and three distinct exhaust crowns to the remaining generic shafts; FoundryRegionMassing.cs groups exterior rails into wide recessed stone-panel bays and lower-sill open returns. Anchors, footprint/rail height, depth owners, pit/bridge rails, materials, camera, walking/ramp geometry, gameplay and saves retained. No new renderer or per-frame work. Region32renderers;117629->118537vertices (+908/about0.77%). Distant2388vertices remains under2500; no performance claim.

Matched actual Unity views accepted by root/independent reviewer. Whole-map gain is modest; large painted flank buildings still dominate. Baselinec532902 before images reused/hash-checked; one new after graphics capture. Evidence/gallery/reproduction: ArtDirection/2026-10-11/foundry-mass-rhythm-01. Browser comparison verified.

Verified:409.NET/446Edit/154Play =1009distinct tests,zero failures/skips; compile0warnings/errors; static585/1099/568; Androidexit0, no C# or shader diagnostics. APK31210207bytes; SHA2565F4BD9A151A6F688091D52DEF6EA8E1DD013B7688D80C1D65A4B83411D11BA75. NOT INSTALLED. No ADB action or fresh observation. Earlier concrete owner-run restart/install decision remains pending. Historical021b95f and Room2/74HP/8unbankedgold are not current phone state. On authorization inspect fresh, back up current files, install/hash-check latest package, preserve current original saves/telemetry; never restore stale snapshots.

Next substantial visual task: workyard clusters and boiler individuality, plus investigate oversized painted backdrop composition. Avoid another small parapet/crown pass. The rail across the closed gate is unchanged, pending transition design. Full reference fidelity, accumulated phone/crowd/midrange/first-ramp acceptance remain open. Regional encounter/boss progression stays deferred under art-first direction.

## 2026-10-11 - Workyard identity package verified; device pending

Stock/casting native Blender clusters recomposed in existing shelves; two existing Resources textures replaced unchanged with GUIDs/import/depth retained. Squat boiler gets one shallow seated lid. First graphics review rejected a table-like hatch and pasted bright tall overlay; final hatch lowered and tall overlay removed. Root/independent actual Unity review accepted local improvement, not full reference fidelity.32renderers;118537->118709vertices (+172), no performance claim. Map/ramp/camera/gameplay/scene/saves unchanged.

Final PASS409.NET/446EditMode/154PlayMode =1009distinct tests,zero failures/skips. Compile0warnings/errors; strengthened existing boiler card/ownership/bounds/lifecycle tests; two after graphics captures. Static585/1099/568 valid; Androidexit0, no C# or shader diagnostics. Beforebaseline10d5e01 images reused/hash-checked. Gallery/evidence/reproduction: ArtDirection/2026-10-11/foundry-workyard-identity-01; editable source: workyard-clusters-01 beside it. Browser controls verified.

APK31,210,207bytes; SHA256 A2F326D1743A401D508E6AAAE7C58C2167D4E97ED525E0F6356CB2D657B32DD7. NOT INSTALLED. No ADB/fresh device observation; earlier concrete owner-run restart/install decision remains pending. Historical021b95f and Room2/74HP/8unbankedgold are not fresh state. After authorization: inspect current state, back up current original files, install/hash-check latest package, review accumulated map/normal combat and preserve current original saves/telemetry. Never restore stale snapshots. Phone/crowd/midrange/first-ramp acceptance remains open; regional encounters/boss progression remain deferred.

Next substantial visual priority: foundry backdrop recomposition. Current ENV_Furnace_Cavern_v1 has giant flank buildings; VoidBackdropView.FrameCavern cover-fit magnifies their dominance in landscape overviews. Consider a versioned foundry-specific plate with smaller staggered tower/bridge masses and deeper quiet centre, keeping original generic cavern and cover/parallax/resource contracts. Do not merely shrink the sprite and expose edges. Recommendation only, not implemented. Compare overview plus bridge/west-rim/gate portraits before accepting. Wider workyard distribution and coherent authored boiler variants also remain incomplete.
## 2026-10-11 - Foundry backdrop depth verified; device pending

Built-in image_gen created FoundryCavernV2 from the old cavern plate and approved reference. Smaller staggered furnace, crane and bridge groups replace giant flank walls. The original 1024x1536 RGB output is imported unchanged. Only foundry geometry selects the new borrowed sprite; original hall art, missing-resource fallback, procedural prerequisites, cover fit, bounded parallax, tint, shader, ranks and ownership remain. No scene, camera, walking/ramp, gameplay or save change. Region remains 32 renderers and 118,709 vertices; one additional Resources asset, no device performance claim.

Root and independent actual Unity review accept clearer overview depth and island outline; portrait gain is restrained. Initial NPOT import with mipmaps loaded RGBA32 despite the ASTC override. Explicit Android inspection identified the cause; disabling mipmaps restored ASTC6x6, one level, non-readable. Editor texture memory fell from 16,778,104 to 1,401,728 bytes, not a phone measurement. Loaded-format and mip assertions guard against recurrence. Three after graphics captures ran; final compressed view accepted. Native distant shafts remain comparatively dark/simple, and broader workyard distribution still differs from reference.

Final PASS: 409 .NET, 446 EditMode and 156 PlayMode = 1,011 distinct cases, zero failures/skips. Compile: 0 warnings/errors. Static: 586/1099/568. Android exit0, no C# or shader diagnostics. Suites/build repeated after the import fix without inflating the distinct total. Before images reused from baseline49cf944 and hash-checked. Gallery, source, full prompt, provenance, reproduction and final verification: ArtDirection/2026-10-11/foundry-backdrop-depth-01. Browser controls verified.

Final APK: 31,210,301 bytes; SHA256 44F3650572568D69C3536F517270182372A9E896A2FF5318E1E754B79269D24F. NOT INSTALLED. No ADB or fresh phone observation; earlier concrete owner-run restart/install decision remains pending. Historical021b95f and Room2/74HP/8unbankedgold are not current state. After authorization inspect fresh, back up current files, install/hash-check latest APK and preserve current original saves/telemetry. Never restore stale snapshots. Physical-device, crowd, midrange, first-ramp and motion-shimmer acceptance remain open. Regional encounters/boss progression remain deferred under the art-first direction.

## 2026-10-11 - Native distant material package verified; device pending

FoundryRegionDistantWorks applies finer coherent facade courses, design-plane cap mapping, fixed broken-span course direction and restrained face/height lighting. The private distant material atmosphere is .44; shared shader/backdrop and borrowed wall texture unchanged. Anchors, silhouettes, two batches/ranks, opaque upper tiers and lower fade remain. Region:32 renderers /118,709 vertices; distant:2,388 under2,500, unchanged. No new texture/material instance/updater, no performance claim. Camera, walking/ramp geometry, gameplay, scene and saves unchanged.

Root and independent seven-view actual Unity review accept clearer arch/coping volumes and finer stone while recording rigid native shapes as unfinished reference work. Baselinee98d4ac before views reused/hash-checked; one after graphics capture. Gallery/evidence/reproduction: ArtDirection/2026-10-11/foundry-distant-material-01. Browser comparison verified. Three helpers split runtime/test implementation, tooling/code review and independent art QA; root alone ran Unity.

The first full gate failed the stale chest-telemetry100HP ceiling when a health book legitimately raised HP to115 after healing; APK was skipped. Deterministic no-book/rare-health-book cases now check exact event-time HP/maximum/book and single opening, without production gameplay changes. Failed/focused/final XML preserved. Final PASS409.NET/446EditMode/157PlayMode =1,012 distinct cases, zero failures/skips; focused2/2 included in total. Compile0warnings/errors; static586/1099/568; Androidexit0, no C# or shader diagnostics.

APK 36210895 bytes; SHA256 AFE4F820B60956F22C6C8A9A317585BAD37E3A00C386C6BDA97D2FED8B45B502. NOT INSTALLED. No ADB/fresh phone observation. Earlier concrete owner-run restart/install decision remains pending; historical021b95f and Room2/74HP/8unbankedgold are not current state. After authorization inspect fresh, back up current files, install/hash-check latest package and preserve current original saves/telemetry. Never restore stale snapshots. Device/crowd/midrange/first-ramp/motion acceptance remains open; region gameplay/boss progression stays deferred.

Next substantial visual priority: authored north-shaft/broken-span silhouettes if navigation stays frozen, or a whole northwest-workyard composition with explicit circulation/blocked-footprint contracts. Do not repeat an isolated tint/trim pass or place raised scenery on walkable floor without shared navigation.


## 2026-10-11 - Native north silhouette integration

One runtime partial adds distinct receding crowns, connected corner buttresses and closed broken-span fracture profiles. Geometry contact fix reviewed before capture; existing lifecycle/order/finite-geometry/budget tests retained. No scene, camera, navigation or combat change. Actual region 32 renderers / 118,817 vertices; distant 2,496 under the unchanged 2,500 ceiling. PASS 409 pure / 446 EditMode / 157 PlayMode, compile clean, static 586/1099/568, Android exit 0. Review, final XML and package identity: ArtDirection/2026-10-11/foundry-north-silhouette-01/validation/verification.md. No ADB/device validation this slice; see newest docs23 before any device operation. Broader workyard circulation and reference richness remain follow-up work.


## 2026-10-11 - Shared work-island art and navigation

FoundryMap exposes two immutable equipment bounds and separates SurfaceContains from walkability; Segment sweeps actor-clearance-expanded rectangles analytically with1e-4 relief. Existing movement/spawn delegate to them; pursuit statically derives8new corners. ProductionYard reads the same rectangles for low iron skids, loaded trolley and mould bench. Surface-only art/exterior tests retain actual floor semantics. No new texture/renderer/updater; scene, camera, balance and progression unchanged. Eight new pure/EditMode cases and expanded existing full-region scene assertions verify floor/contact/depth and navigation. Final417/454/157, compileclean, static586/1099/568, Androidexit0. Evidence/package/device state: ArtDirection/2026-10-11/foundry-workyard-islands-01/validation/verification.md. No ADB this slice; device motion/crowd acceptance remains open.


## 2026-10-11 - Broad court integration

FoundryMap width and shared island coordinates, FoundryRegionProductionYard rail placement and equipment detail integrated as one package. Width-dependent chamfer, paving packing and imported-card depth tests now derive actualbounds; old removed-corner points are explicitly tested as newfloor. Four new pure cases verify new-strip movement, four ten-enemy waves and bidirectional pursuit to court/gate/bridge. Camera Foundry path follows hero without generic-clamp limitation. Three helpers split width/tests, equipment and independent art QA; root alone owns Unity/ADB.

First full gate caught a floor-test oracle falsepositive: a near-degenerate clipped triangle far outside the queried corner returnedfloatbarycentrics(0,1,0). Recorded diagnostic retained; double orientations and triangle bounds reject remote slivers without changing expected geometry or production source. Failed, focused and final test evidence is retained under ArtDirection/2026-10-11/foundry-broad-court-01/validation. No phone operation or fresh observation; latest finalbuild record in docs22/23/38.

## 2026-10-11 - Shared compact gate integration

FoundryMap owns GateOffset-4 and landing24..28; FoundrySurface derives ramp25..27 and pursuit ends at the new landing centre26,6. GateU translates rigid architecture without warping the shared projection. Header/ramp contacts, far boiler feeds, reservoirs and undercroft drains follow their owners. New pure tests exercise removed old floor, legal landing movement, bidirectional pursuit and separated10-enemy entries. Scene checks inspect actual portal corner/UVs, low service support footprints and discharge collar/tip geometry, rejecting the former far drain anchor.

First import exposed a test-only internal-access compile error; tests now derive ramp limits from the public map contract. First full gate exposed a test's false4vertex gate-batch assumption: bridge banners share that texture/rank, making196vertices. Corrected to actual portal corners/full-image UVs, not relaxed runtime geometry. Initial and focused evidence retained. Final source is frozen during Unity execution; final counts/build identity live in the package verification and docs22/23/38. No phone work in this slice.

## 2026-10-11 - Industrial atlas integration

FoundryRegionBakedBoiler now chooses left/right atlas tiles using the existing near/far boiler centres. Same card sizes, vertical split, draw order, footprint depth and cutaway flags. Removed the old172vertex inspection-hatch overlay; source variant0 includes a physically seated Blender hatch. Two existing Resources textures retain GUIDs and now import2048x1024ASTC6x6/12mips/non-readable/clamp. PlayMode checks exact8vertex/12index bands, atlasUVs, geometry/depth, four batch ranks, metadata and borrowedtexture/ownedmaterial destruction.

Tools/ArtReview/PackIndustrialAtlases.py packs unscaled images with byte-exact per-tile checks. FoundryIndustrialCapture is explicitly after-only; baseline12matched cameras and diagnostics are copied/hash-checked from2c3c836. Two new furnace contact views are after-only. Root runs Unity; final gates/build identity and remaining device restrictions are retained in the proof validation and docs22/23/38.

## 2026-10-11 - Gate/stone integration contract

Native presentation changes stay in FoundryRegionArchitecture, GateEnclosure, GateHeader and Heraldry. Door anchors, materials, scene, camera, shared map/ramp, sockets and save/gameplay contracts remain. Reduced outer height7.35 has a matching depth envelope; inner terminal7.94 fits the central8.2. Broad landing shoulders remain outboard under1.05. Both stone relief and lock use the same compressed rise.

FoundryGateStoneTests adds five production-geometry cases for contour coverage, UV continuity, finite/index/winding validity, triangle-interior coplanarity at ramp foot/crest, separated apron bands and lock containment in its actual backing. Existing PlayMode checks retain portal corners, ownership and region contracts. Tooling is after-only; fourteen baseline857a99c images are copied with hashes. First graphics pass rejected for faint paving, second accepted after bounded contrast/joint refinement. Final suite/build evidence is in ArtDirection/2026-10-11/foundry-gate-stone-01/validation. Root alone owns Unity/ADB.


## 2026-10-11 - Court, bridge and dressed masonry verified; device pending

Court and workyard zones favor larger unequal flags (maximum area3), with broad correlated quarry tones and clearer restrained joints. Bridge central deck uses staggered 1x1/2x1 oblongs clipped to the original lane/court boundary; quiet outer bands and continuous world grain/light remain. Gate ashlar faces gain narrow coplanar dressed rims; long coping fascia gains broad fitted blocks while original roof bearing, exact face envelopes, tiers, sockets and depth ownership remain. Gate candidate family/treatment is retained, but shared packer joint scores can change its layout. No new runtime texture/material/renderer/updater; camera, navigation, ramps, scenes, gameplay and saves unchanged.

Files: FoundryRegionFinish.cs, FoundryRegionView.cs, FoundryRegionGateEnclosure.cs; new FoundryBridgePavingTests.cs and FoundryMasonrySurfaceTests.cs with fresh metas; Tools/ArtReview/FoundryCourtStoneCapture.cs and Run-FoundryCourtStoneProof.ps1.

Three helpers split court finish, gate masonry/tests, and proof/source/actual-image review; root implemented the bridge and ran all Unity checks. Fourteen baseline9150049 matched before images were copied and hash-verified; capture is after-only. Browser controls and loaded images verified after restarting the existing localhost preview server. Two tapered/skewed masonry cases guard exact silhouette, area partition and UV/light continuity; one actual deck case guards complete floor coverage, continuous grain, flush surface and court overlap ownership.

Verified: 425 .NET / 470 EditMode / 159 PlayMode = 1,054 distinct suite cases, zero failures/skips. Compile zero warnings/errors; actual Android-target graphics capture 1/1. PASS: 589 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0, no C# or shader diagnostics. APK 36,223,974 bytes; SHA256 E559FD12666CCB1599648266CE3A02B23C707585C2A8268AD80495EB3F06A5C9. NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-court-stone-01.


## 2026-10-11 - Pit machinery and exterior masonry verified; device pending

Pit vessels now have distinct broad octagonal pressure and stepped receiver heads, seated hold-downs, recessed annular mouths and below-deck service bays/cages. All remain within the existing radius1.1/height1.3 hood and deck-to-molten drum envelopes. Foundation curtains use broad fitted courses, arch wedges use quiet mineral grain and tapered piers have full-width dressed courses. Existing silhouettes, pit/collar/pool, flue sockets, depth owners, material batches, navigation, camera, scene, gameplay and saves remain unchanged.

Files: FoundryRegionPitHoods.cs, pit-only FoundryRegionMassing.cs, FoundryRegionUndercroft.cs; new FoundryPitVesselTests.cs and FoundryUndercroftStoneTests.cs with fresh metas; Tools/ArtReview/FoundryPitWallCapture.cs and Run-FoundryPitWallProof.ps1.

Three helpers split pit machinery, outer masonry/tests, and proof plus independent review; root alone ran Unity. Fourteen c549f06 baseline views were copied/hash-verified; captures are after-only at matched cameras. Independent source review caught design-collinear wall merging across gate ramp grade breaks. The fix requires projected collinearity at both the deck and contracted foot; a regression uses the real south landing and the foot-only bend at26. First capture diagnostics/images are retained. Eight new EditMode cases cover emitted vessel containment, wall joins, projected ramp preservation, tapered/sloping masonry, UVs and depth cooling.

425 .NET / 478 EditMode / 159 PlayMode = 1,062 distinct suite cases, zero failures/skips. Compile zero warnings/errors. Two successful Android-target graphics captures; the first was superseded after source review. PASS: 591 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0, no C# or shader diagnostics. APK 36,232,266 bytes; SHA256 F44358DE1552E9DC7FE46AB084E2E488A0F2FAD5352C3B6A24FC0546C1E454A4. NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-pit-wall-01.


## 2026-10-11 - Bridge architecture and workyard equipment verified; device pending

Entrance bridge gains connected outboard parapets, bevelled coping with broad joints, fitted support-arch wedges and coursed legs/fascia. Short inscribed depth owners remain outside the walking union, both asymmetric court joins are clipped, and keeper consoles remain exposed below the existing statue sockets. Yard trolley gains broad cast retaining yokes, sloped haunches, split axle bearings and unequal billet ends with seated straps. Mould bench gains a supported lower shelf, trestle gussets and distinct ingot/circular bearing patterns. Existing shared equipment footprints and 1.55 height envelope remain. No map/navigation, camera, scene, encounter, gameplay or save change.

Files: FoundryRegionBridgeSupports.cs, FoundryRegionProductionYard.cs, bridge-only FoundryRegionView.cs and one FoundryRegionSite.cs call; new FoundryBridgeArchitectureTests.cs and FoundryYardEquipmentTests.cs/metas; Tools/ArtReview/FoundryBridgeYardCapture.cs and Run-FoundryBridgeYardProof.ps1.

Three helpers split bridge architecture/tests, yard equipment/tests, and proof plus independent source/image review; root alone ran Unity. Fourteen matched baseline b91ac1a views were copied from tracked files and Git-object/SHA256 verified. Three new portraits are explicitly after-only. Source review caught far trolley-yoke painter ordering and it was corrected before capture. First actual review found quiet ruler-flat parapet caps, rather than self-overpaint; bounded bevels and broad coping joints were added, then recaptured. Selected first images/budget/XML are retained. Five new EditMode cases cover actual bridge geometry/owners, both court seams, keeper seating and emitted yard equipment containment/UVs/indices. Initial full EditMode found two failures in the new cap test: its inverse chose the lower edge of a float32 projection plateau at the exact bridge-start boundary. The test was corrected to verify forward projection within the physically legal interval, with a quantified two-output-ULP allowance, instead of an arbitrary inverse representative. A second run exposed the final midpoint rounding to the worse bracket endpoint; selecting the endpoint with smaller forward residual fixed it without enlarging the allowance or design bounds. Focused bridge3/3 and final complete EditMode passed. No runtime correction was needed. Both failed XML files and focused results are retained. Full PlayMode completed against final runtime source; after the test-only correction the complete EditMode suite was rerun before building. The APK was never built on a failed gate.

425 .NET / 483 EditMode / 159 PlayMode = 1,067 distinct suite cases, zero failures/skips. Compile zero warnings/errors. Two successful Android-target graphics captures; the first was superseded after visual review. PASS: 593 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0, no C# or shader diagnostics. APK 36,241,090 bytes; SHA256 1421DB036D32A3D4C6DB98A3E8AE58FAF348F54D80A0A53B331DAB427D62680B. NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-bridge-yard-01.


## 2026-10-11 - Western loading assemblies verified; device pending

Western service shelves and crane footing replace solid faces with recessed coursed infill, connected tapered corbels and continuous bearing courses; shelf decks gain bounded iron channel edges. Casting cantilevers are tapered within their existing attachment volumes. Three native Blender renders replace the existing crane, receiving stock station and casting station images: rough cast crane channels/yokes/bearing cases, a loaded receiving cradle with six staggered hollow pipes, and a real annular mould cavity/runner with a supported pouring vessel. Existing card framing, anchors, GUIDs, import settings and depth owners remain. Camera, map/navigation, scene, encounters, gameplay and saves are unchanged.

Files: FoundryRegionWorkshop.cs, FoundryRegionCastingBench.cs, FoundryRegionDressing.cs; three existing FoundryCrane/FoundryStockStation/FoundryCastingBench resource PNGs; new FoundryLoadingSupportTests.cs/meta; three matching Blender generators and FoundryLoadingBayCapture.cs/Run-FoundryLoadingBayProof.ps1.

Three helpers authored support geometry/tests, crane source, and stock/casting sources. Root integrated all three resources, reviewed the source diffs and both source renders and actual Unity images, and alone ran Unity. Helpers reached the account usage limit during the requested cross-review, so independent cross-review did not finish and is not claimed. Eight matched baseline 3044b83 views were copied from tracked files and Git-object/SHA256 verified; three close equipment views and two local portraits are explicitly after-only. The source-before directory retains all three verified baseline resource PNGs. Initial graphics import found a new test referencing internal FoundrySurface across the Unity assembly boundary; the test now invokes the existing private FoundryRegionView.Project through reflection. The initial diagnostic is retained. This was an import failure, not a completed graphics capture. Five new EditMode cases validate actual emitted geometry against projected original support prisms, unchanged deck corners, scenic footprint interiors, below-floor separation, finite UVs/indices and bounded vertex cost.

425 .NET / 488 EditMode / 159 PlayMode = 1,072 distinct suite cases, zero failures/skips. Isolated compile zero warnings/errors. One successful Android-target graphics capture (1/1). PASS: 594 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0, no C# or shader diagnostics. APK 36,242,956 bytes; SHA256 1130DFD56C5AAFEBBE3CA034661F370AF86C03D9E9E4C10CDA5B0FBE9283E728. NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-loading-bay-01.


## 2026-10-11 - Connected service shafts verified; device pending

Two existing northwest distant furnace shafts receive narrower necks, joined dressed shoulders, real recessed neck panels with side returns and hollow exhaust crowns. The original leaning buttress heads move inward to meet the narrowed neck. The mouths have low throats, inner walls and separate annular rims instead of solid roof caps. Existing lower furnace body/fire, other distant variants, atmospheric fade, materials, sorting, camera, navigation, gameplay and saves remain unchanged.

Files: FoundryRegionDistantWorks.cs, new FoundryServiceShaftTests.cs/meta, the combined geometry budget in FoundryDistantWorksTests.cs, FoundryServiceShaftCapture.cs and Run-FoundryServiceShaftProof.ps1.

425 .NET / 497 EditMode / 159 PlayMode = 1,081 distinct suite cases, zero failures/skips. Final isolated compile zero warnings/errors; Android-target graphics capture passed. PASS: 595 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit0 with no C# or shader diagnostics. APK 36,248,250 bytes, SHA256 B8225B648A17854B3EB00D07B5A7CF4EDFC0B86DC32D080EB2353157B32C8DC7; NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-service-shafts-01.


## 2026-10-11 - Court, pit and gate construction verified; device pending

The court compass now uses restrained paired brass facets and a continuous engraved ring over three fitted stone courses. The pit upper lining has a joined below-deck bearing course and four relief ribs with side returns, leaving all furnace bays, vessels and the molten seam unchanged. Four gate shafts receive genuine blind recesses with backing, returns and broad dressed surrounds; two connected header shoulders replace the former interrupted coping ends. Existing court/pit/gate footprints, door and lock sockets, ramps, navigation, camera, material batches, depth ownership, scene, gameplay and saves remain unchanged.

425 .NET / 510 EditMode / 159 PlayMode = 1,094 distinct suite cases, zero final failures/skips. Isolated compile zero warnings/errors; before and after Android-target graphics captures passed. PASS: 599 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0, no C# or shader diagnostics. APK 36,256,060 bytes; SHA256 3405AA3C2A261B803A36F7B25BCED0D1AEDDF64CB422BABB56EBC9E53F375501; NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-court-pit-gate-01.


## 2026-10-12 - Court, pit and gate material integration verified; device pending

Gate recesses now map the existing stone grain by real block dimensions, continue it over bevels and retain inherited corner lighting plus a shaded-head/reflected-foot gradient. Pit ribs use a quiet cast-iron atlas interior at bounded grain scale with restrained integral worn arrises; the under-rail bearing uses cool masonry and actual narrow block beds. The medallion ring and fitted centre flags gain flush dressed shoulders and share the surrounding court quarry family, reducing the separate warm checker pattern. Existing footprints, silhouette, pit fire and vessels, door/lock, navigation, camera, scene, gameplay and saves remain unchanged.

Verified: 425 .NET / 512 EditMode / 159 PlayMode = 1,096 distinct suite cases, zero failures/skips. Isolated compile zero warnings/errors; Android-target actual graphics capture passed. PASS: 602 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0, no C# or shader diagnostics. APK 36,260,221 bytes; SHA256 993B2DA7B72A5A7D6A0FF2923DF29EB41A55917D33B45AF2EE105760DB08D8D0; NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-12/foundry-material-integration-01.


## 2026-10-12 - Foundry court hierarchy verified; device pending

The western receiving edge, transfer turntable and stock/trolley approach now share one connected flush working apron. Its broad repair flags replace existing packed parents, retain exact floor coverage and world lighting, and fade back to ordinary court stone before the pit-side passage. The two northwest scenic shafts have lower unequal crowns and flatter joined shoulders; their original feet and furnace sills remain fixed, six separated furnace panes fit the shorter bays, and the broken connecting span remains borne by the revised cores. Bridge and lower fortress masonry use fewer broader courses, locally scaled cool mineral grain, continuous spandrel/coping mapping and restrained contact shade. Existing walking outline, camera, pit/gate/bridge footprints, scene, encounters, gameplay and saves are unchanged.

425 .NET / 519 EditMode / 159 PlayMode = 1,103 distinct suite cases, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed. PASS: 605 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 36,263,938 bytes; SHA256 68C7867361911A55C3CAB46F01A8826B30EA2F3F725558EE8B1F46DD06033C8A; NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-12/foundry-court-hierarchy-01.


## 2026-10-12 - Foundry forecourt layout verified; device pending

The main court now assigns four design units from the western working strip to the entrance forecourt: West -36 to -32 and South -14 to -18. The bounding rectangle changes from 50 by 32 to 46 by 36; the strip below the fixed furnace pit grows from 16 to 20 units, while the western strip decreases from 20 to 16. These are design coordinates, not measurements extracted from the concept. The projected outer span and comparison camera stay unchanged. Pit, gate landing and bridge endpoints retain their anchors; more of the same bridge now meets the expanded court.

Perimeter masonry, southern bastions, finials, lantern pools and exterior chains follow the new boundary. The two bridge lantern rows and three below-deck supports fit the shorter exposed approach. The south brass guide and the stone packer's forced joints share one position. The long hanging chain uses an actually emitted, unscreened major-pier socket with deterministic tie-breaking. Western equipment and interior work islands use their existing derived offsets. No scene, encounter, balance, profile, camera or runtime texture change is included; the geometry change can affect encounter spacing and travel, so it is not balance acceptance.

438 .NET / 535 EditMode / 159 PlayMode = 1,132 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed. PASS: 605 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 36,264,048 bytes; SHA256 3BE71A740AE61ADA3816815C0D05FCCFAE20E408D1F9AA7AFDD6D20BFA688CFB; NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-12/foundry-forecourt-layout-01.


## 2026-10-12 - Foundry ceremonial architecture verified; device pending

The ceremonial medallion moves from the court origin to design (-3,-5), inside the deeper entrance forecourt. Its fitted stone field grows to radius3.75 and its outer dressed setting to radius5.05; the compass arms retain their existing small dimensions. One restrained brass setting remains. Two paired segmented stone courses connect the entrance and gate direction to this single assembly. The former angular brass court box, its forced packing seams and the unit-flag square at the old centre are retired. The global polar helper and the western working turntable remain independent.

The two outer gate towers retain their footing and crown but replace the mid-height waist with a low battered plinth and one continuous tall recessed bay. Both inner jambs use a seated L-shaped stone section with a longitudinal rebate and a concave bearing cap. The existing painted door/banner/lock plane stays fixed; it is the masonry around it that changes.

Lower fortress stone now distinguishes broad primary responds and arches from quieter secondary projected recesses. The bridge's existing three support stations remain, with one primary mouth bent and two subordinate rear bents. The mouth's bearing course partitions its old spandrel instead of layering over it. Existing open intrados, support contacts and chain socket eligibility are retained.

This is a combined visual mesh package. The accepted walking outline, map size, camera, scene, actors, encounter data, movement solver, rewards and saves are unchanged. No new runtime texture, material, renderer or per-frame updater is introduced. It does not close the reference-fidelity or physical-device acceptance backlog.

438 .NET / 546 EditMode / 159 PlayMode = 1,143 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed. PASS: 606 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 36,268,279 bytes; SHA256 6EE8F295DA4D39B0908B119F2D2E0B1BFEE583A9EB83B085CDE28744037FF70B; NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-12/foundry-ceremonial-architecture-01.


## 2026-10-12 - Foundry court paving and workyard verified; device pending

The centre court and its two approaches now replace the ordinary paving within three exact, disjoint footprints. All old floor layers are cut out, including the bridge portion, while source grain and sampled lighting are retained at each clipped boundary. The radius5.05 medallion at (-3,-5) contains an octagonal keystone, eight fitted fan stones and twelve broad radial outer stones. Full-width4.18 approach fields use broad transverse flags and alternating fitted pairs. Mortar, dressed shoulders and faces partition each replacement; they do not cover another complete coplanar bed. Existing brass dimensions, map outline, camera and walking geometry stay fixed.

The western crane gains a grounded native cast-iron machinery base on its original scenic deck. A sloped rear casing, joined cheeks and header surround an exposed cable drum. The active baked-crane path and fallback both receive the same base; the existing mast card, upper pivot, hoist chain and load retain their anchors. One existing iron batch and the original depth owner are reused. No runtime texture, material, renderer, per-frame updater, gameplay, encounter, scene or save change is included.

438 .NET / 552 EditMode / 159 PlayMode = 1,149 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed. PASS: 608 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 36,291,532 bytes; SHA256 D3B7D53094316D3B89BA50C515D78A5BA7194ACC7FDC889D2E959F1DACE26804; NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-12/foundry-court-workyard-01.


## 2026-10-13 - Foundry stone craft verified; device pending

Ordinary court quarry slabs now reach 3 by 2 design units with varied proportions, fewer joints and selected worn corners. Local mineral abrasion uses the existing bevel/face vertices and preserves world lighting and grain; no added crack overlay covers the court. The gate approach retains its previous smaller stock. The fitted central medallion and two processional fields retain their separate replacement domains.

Both outer gate buttresses now rise from a broad coursed base through a seated battered shoulder into the recessed upper bay. The shoulder ends at height 2.85; the original footprint and crown 7.35 remain. Mortar, dressed edges and faces partition the lower masonry surfaces instead of adding coplanar stone over a full hidden wall. Door, banners, inner jambs, ramp and actual support boundaries are retained.

The furnace pit replaces its repeated turned balusters with broad stone guard bays, recessed panels and sparse square iron stems. A continuous sill and substantial coping retain the original curved band and height. Actual openings remain visible above the sill. The existing posts, lining, fire, floor illumination, vessels and outer corner railing are unchanged; weak local reflected warmth describes pool-facing guard returns.

This combined visual package changes no walking outline, map dimensions, camera, scene, actors, encounter data, movement, economy or saves. No runtime texture, material, renderer or per-frame updater is added.

438 .NET / 564 EditMode / 159 PlayMode = 1,161 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed. PASS: 611 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 36,301,580 bytes; SHA256 27887CDF79CF6F5049ECC63BC103AAF0FAEA7B78C04C4E1FED98DA32646616A9; NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-13/foundry-stone-craft-01.


## 2026-10-13 - Foundry surface finish verified; device pending

The active paving surface now uses versioned FoundryPavingV6, a calmer jointless slate diffuse generated with the built-in image_gen tool. The selected PNG is copied byte-for-byte into the project; prompt and provenance are retained in texture-prompt.txt and paving-source-integration.json. Existing geometry still owns stone boundaries, chipped outlines, joints, metal inlays and authored forge light. The material tint, UV span, shader and Android import contract are unchanged.

Gate header sidewalls use broad dressed stones partitioning the original faces into mortar, arrises and stone interiors. Coping, closed niche returns and caps use bounded mineral UVs; their original exterior envelope and bearings are preserved. A restrained local crown/bearing treatment multiplies the existing light rather than replacing it. MasonryV4 remains the existing five-course atlas; no global wall texture replacement is made.

This is a combined surface-quality pass, not reference parity. The walkable outline, gameplay camera, encounters, collision, progression and shaders are unchanged. Further authored variation in landmark stone and machinery remains open.

438 .NET / 569 EditMode / 159 PlayMode = 1,166 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed. PASS: 612 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 36,300,736 bytes; SHA256 D8C3D2B0E284A57D74DBD9CB5EB3707DA934805C4966C8C851CE3E07765369CD; NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-13/foundry-surface-finish-01.


## 2026-10-13 - Foundry pit craft verified; device pending

The furnace pit guard now has broad fitted coping stones, restrained joints and selected worn arrises. The new faces partition the previous front and top planes, independently of the narrow panel and iron-stem cuts below. Grain unfolds over each stone edge. Actual guard openings, existing posts, contacts and the curved exterior envelope remain intact.

The two formed pit hoods now use a continuous angular cast-metal light treatment, coherent local grain and restrained crown soot. Upright plate courses receive a narrow seating partition; sloping shoulders remain broad between their dark straps. The exposed head cap shares its adjoining shoulder material, while the recessed mouth remains dark. The existing pressure/receiver profiles, collars, mouths and below-deck vessel art retain their geometry.

This package adds no runtime texture, material, renderer or per-frame updater. The new partial class separates bounded metal-surface generation from the existing hood assembly. Map outline, camera, scene, navigation, encounters, progression, economy and saves are unchanged. This is an incremental material-craft improvement; reference parity is not claimed.

438 .NET / 579 EditMode / 159 PlayMode = 1,176 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed. PASS: 613 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 36,306,068 bytes; SHA256 0D88970AA95AAB3FF0C2F75CB1A64C40C609C4ED6392BA302C68417EB6AF0B23; NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-13/foundry-pit-craft-01.


## 2026-10-13 - Foundry entrance craft verified; device pending

The fitted entrance field now continues from the existing court approach across the bridge to its actual far kerb. A shallow tapered quadrilateral shares the old rotated approach cap exactly. Broad transverse courses alternate with fitted pairs, with whole bearing stones at both ends. The exact new contour is subtracted from all old paving layers before its replacement is emitted.

Quarry stone gains restrained per-stone mineral bedding within the existing continuous material family. Ordinary court mortar follows its stone tone more closely. Selected exposed contoured flags receive wider sampled mineral abrasion on their existing face. Their geometric bevel inset and exterior contour are retained; no additional crack or wear overlay is introduced.

Map boundaries, bridge dimensions, brass routes, camera, collisions, actors, encounters, progression and saves retain their existing contracts. No new runtime texture, material, renderer or per-frame updater is added. The active V6 paving texture and world-space grain/light remain. This package improves route continuity and stone craft; the complete concept reference is still a work in progress.

438 .NET / 586 EditMode / 159 PlayMode = 1,183 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed. PASS: 613 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 36,307,934 bytes; SHA256 35B17010AAB7E4A7075C602CE9DC9F0830868D939954C9C396E702B7B43FEAF5; NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-13/foundry-entrance-craft-01.


## 2026-10-14 - Foundry landmark craft verified; device pending

The existing pointed gate bay frames now recede into a shallow splayed reveal. Their inner arrises consume one quarter of the existing niche depth, and connected returns shorten to meet the same stations. Backing, outer frame, caps, bearing contacts, door and tower envelopes remain fixed. A bounded directional scalar across the existing splay vertices darkens the receding far jamb and head while retaining the outer bearing and sill illumination. It preserves local forge-light RGB relationships. The changed geometry replaces the old flat frame; it adds no decorative layer or vertices.

The working apron gains local material use beyond the trolley loading end and the stock bench, plus a restrained scrubbed receiving threshold. Existing emitted stone samples receive a bounded neutral scalar, preserving world grain, forge-light channel ratios, geometry, UVs and triangle ownership. The open transfer centre and unused repair stone remain neutral. No texture, material, renderer or per-frame updater is added.

This package changes no map bounds, camera, walking geometry, gameplay, encounters, progression or saves. It is a local landmark/material improvement, not completion of the approved reference.

438 .NET / 593 EditMode / 159 PlayMode = 1,190 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed. PASS: 613 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 36,308,109 bytes; SHA256 D5D31767220EADA46B523957EB97267FFCE5D68805698694D2CC91E5FB2C9793; NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-13/foundry-landmark-craft-01.


## 2026-10-14 - Foundry material source study verified; device pending

The active paving uses the new V7 jointless slate source with mean-matched neutral material exposure. V6 remains intact as the baseline. Existing stone contours, geometry-owned joints, world UVs and illumination are retained.

The two formed furnace hoods and complete loading trolley use an isolated V2 cast-iron source. One coherent local UV field now covers the trolley; the hoods keep their prior UV field. Other ironwork and the mould bench retain the original framed iron atlas. No billboard replacement, map/layout, camera, navigation, encounter, progression or save change is included.

Both accepted sources are generated by the built-in image_gen tool, copied byte-for-byte into the repository and imported as mipmapped non-readable Android 1024 ASTC6x6 textures. Slate exposure matches the old mean luminance; cast iron matches the old plain field's mean RGB to retain the cold-metal family. Full prompts and rejected V1 provenance are preserved.

438 .NET / 595 EditMode / 159 PlayMode = 1,192 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed. PASS: 616 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,391 bytes; SHA256 15027ED59A3BAE423DA100723BF8A98F7598FADCFD0270F6D4B293FBF358E725; NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-material-study-01.


## 2026-10-14 - Foundry gate masonry verified; device pending

# Final source scope

This package groups three changes to the existing closed gate and its stone architecture.

## Profiled header stone

The continuous lintel now has a 0.09-unit recessed middle fascia between connected sloping returns. Directional vertex tones describe the carved depth in the existing unlit renderer. The three visible faces share their octagonal stations and one mineral field per complete face. The original cap, coping, outer bearing stations, corbels, door and iron contacts remain in place. The shared `GateCraftTier` behaviour is preserved; only the first lintel in `BuildGateHeaderFinish` uses the new profile.

## Fitted enclosure coping

Dressed curtain, tower-capital, jamb-belt and landing-plinth tiers use broad fitted stones with joints registered across their fascia and bearing caps. The original visible surfaces are partitioned once, replacing the former complete fascia beneath overlaid stone faces. Coplanar arrises retain the existing bearing plane. One bounded mineral field spans each complete tier. New vertices sample the original lit shell, with neutral joint and edge gains; non-dressed wall ashlar retains its existing path.

## Clearer closed-door threshold

The procedural rail excludes its central units at the landing's closed-portal edge. The existing door facade physically backs that interval; removing the visual fence exposes the door sill and its approach. Both explicit brazier supports and the outer rail returns remain. The movement boundary, collision/navigation geometry and closed-door behaviour are unchanged. This is a presentation opening, not a newly traversable route or a boss transition.

## Runtime cost and preserved contracts

The final actual scene capture reports 34 region renderers and 163,073 vertices, versus 34 renderers and 162,361 vertices before: +712 static vertices for the combined package. Distant geometry remains 3,572 vertices. No new texture, material, renderer, shader, component, owned resource type or per-frame updater is added. The earlier cast-iron material arrangement remains unchanged.

Map boundaries, camera framing, actors, combat, encounters, progression, saves, movement and collision contracts are unchanged. The existing region vertex budget is retained. These resource counts are not device frame-time measurements.

## Evidence boundary

Twenty-one matched Before and twenty-one matched After views were captured in the Android-target Unity Editor with the same camera and actor setup, paused combat and hidden HUD. The new gate-crown detail view is present in both phases. The final graphics test passed, including the actual merged-scene threshold presentation oracle. Floor ownership probes remain in the capture fixture.

Focused new tests inspect header depth and surface ownership, exact bearing/cap retention, continuous mineral UVs and rendered facet contrast; enclosure tests inspect fitted surface coverage, registered joints and original-light inheritance. Existing PlayMode coverage now checks the physical door backing, clear presentation interval, threshold paving ownership and absence of regenerated centre posts. Full suite/build results belong to the package verification record and must be established before commit acceptance.

No ADB operation, phone installation, save/telemetry operation or physical-device review was performed for this package. Reference fidelity, device motion/shimmer/crowd review and midrange performance remain unfinished. This package improves the existing gate's stone construction and threshold legibility; it does not complete the approved reference.

Verified: 438 .NET / 605 EditMode / 159 PlayMode = 1,202 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed. PASS: 618 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,391 bytes; SHA256 5A7B64935F344A70089E8E7F304E1F105228C0E03C05ACF4B44521C62FE29C02; NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-gate-masonry-01.


## 2026-10-14 - Foundry gate stonework verified; device pending

Broad fitted courses now divide the existing recessed shaft jambs and pointed heads. Adjacent curtain ashlar partitions the old wall instead of retaining its duplicate base; existing decorative relief remains. A bounded neutral mineral/damp finish refines lower buttress faces. Openings, backing stations, caps, map, camera and gameplay contracts are retained. Files: FoundryRegionGateRecesses.cs, FoundryRegionGateEnclosure.cs, FoundryRegionGateButtress.cs; FoundryGateRecessTests.cs and new FoundryGateCurtainTests.cs; reproducible Tools/ArtReview/FoundryGateStoneworkCapture.cs and launcher.

Verified: 438 .NET / 612 EditMode / 159 PlayMode = 1,209 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed. PASS: 619 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,391 bytes; SHA256 EC908F08891544E2FE3939166ED5C327B2143721326A4ABA59C370AB2C55C639; NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-gate-stonework-01.


## 2026-10-14 - Foundry court cohesion verified; device pending

Broad material use now distinguishes the scrubbed entry from the settled working apron while preserving the quiet combat centre. The moulding bench stores supported open spare flasks and timber packing; its worked iron agrees with the crane casting. Exposed gate stone has a bounded face-dependent quarry value lift, and rolled service pipes use cooler iron beneath existing reflected fire.

Files: FoundryRegionPavingWear/Finish, ProductionYard/CraneHousing, Architecture/PipeFinish; focused court, yard and crane tests plus the corrected mineral oracle. Capture: Tools/ArtReview/FoundryCourtCohesionCapture.cs and launcher.

438 .NET / 621 EditMode / 159 PlayMode = 1,218 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed separately. PASS: 620 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,391 bytes; SHA256 4F696238FBB970EC4965E71B4D92E0210EBB453DBB5D9B5E231487E664BD390E; NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-court-cohesion-01.


## 2026-10-14 - Foundry bearing architecture verified; device pending

Service-tower crowns now have joined recessed bearing bands and whole-head mineral sampling. Four working shelves and two casting cantilevers use raked stone knees beneath continuous bearing beds. The entrance bridge gains a carved inner return and shoulder while its front and soffit share one physical grain field; near-island arch bays and responds receive coherent parent-surface grain.

Files: FoundryRegionDistantWorks, Workshop, CastingBench, FortressSurface, BridgeSupports and Undercroft; focused ServiceShaft, LoadingSupport and FortressHierarchy tests. Capture: Tools/ArtReview/FoundryBearingArchitectureCapture.cs and launcher.

438 .NET / 631 EditMode / 159 PlayMode = 1,228 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed separately. PASS: 620 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,391 bytes; SHA256 6AF3C51E2A249A856391B4D5B214820C3E2E008AE78D4DDFDF842FAFD6B57DD4; NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-bearing-architecture-01.


## 2026-10-14 - Foundry stone detail verified; device pending

Fixed-scale mineral grain restores finer stone detail across bridge bearings, undercroft bays/responds and workshop shelf/casting supports. Service-tower piers gain recessed blind fields inside their existing outlines. The entrance bridge haunches gain broad joined courses with separately owned mortar cells.

Files: FoundryRegionStoneGrain (new), FortressSurface, Workshop, Undercroft, DistantWorks and BridgeSupports; StoneGrain (new), ServiceShaft, LoadingSupport and FortressHierarchy tests. Capture: Tools/ArtReview/FoundryStoneDetailCapture.cs and launcher.

438 .NET / 645 EditMode / 159 PlayMode = 1,242 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed separately. PASS: 622 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,391 bytes; SHA256 7B9DF62AAB90B43B35713D07A14181BC2111EB63DDEE4852ABDC70D6D70E5FA3; NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-stone-detail-01.


## 2026-10-14 - Foundry workyard finish verified; device pending

Existing service-tower exposed bearings gain restrained neutral quarry value while their sheltered fields remain dark. Native winch and mould-bench iron share a quieter cool finish, and workbench timber is less orange. Existing paving receives one soft equipment-contact albedo field that reduces the amber cast near supported stock/casting/salvage shelves and working islands, preserving the quiet centre and raised gate.

Files: FoundryRegionDistantWorks, CraneHousing, ProductionYard, PavingWear and Finish; ServiceShaft, CraneMachinery, YardEquipment, CourtMaterial and PavingLayout tests. Capture: Tools/ArtReview/FoundryWorkyardFinishCapture.cs and launcher.

438 .NET / 660 EditMode / 159 PlayMode = 1,257 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed separately. PASS: 622 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,391 bytes; SHA256 A967EFA4B65337DD7F0B401611520600E47EC5D1172C8A416D8009676049774B; NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-workyard-finish-01.


## 2026-10-14 - Foundry gate depth verified; device pending

The two outer gate buttresses now use an authored Blender masonry bake with broad fitted courses, bevel light and pointed recessed niches, retaining the original contact stations and native foundations. The two native upper header shoulders gain shallow pitched roofs and closed gables within their original bounds. Runtime cards share an unmirrored texture and calibrated 0.62 RGB gain, retain physical depth ownership and paint after their curtains; the south boiler feed body, collars and elbow paint strictly in front of the opaque near bay.

Files: FoundryRegionGateMasonry, GateEnclosure, GateHeader, Massing and View; GateShoulder EditMode and GateMasonry PlayMode tests; gate masonry resource/import and Blender authoring tools. Capture: Tools/ArtReview/FoundryGateDepthCapture.cs and launcher.

438 .NET / 666 EditMode / 160 PlayMode = 1,264 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and final After Android-target graphics captures passed separately. PASS: 626 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,485 bytes; SHA256 649AB25FB971D8362CB0E08FA9EA60253531CEE2A3D7E834005B8AF10674D2A9; NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-gate-depth-01.


## 2026-10-14 - Foundry gate cohesion verified; device pending

Inner gate jambs now use four fitted stone courses and separately owned mortar beds instead of full backing faces with overlaid blocks. Active curtain wings gain real blind pointed bays with connected 0.18-unit returns and closed shallow pitched coping. A gate-local fixed-scale mineral field joins the native inner piers, crown, curtains and landing shoulders, using a clean area of the existing atlas and preserving the original face light, door opening and bearing stations.

Files: FoundryRegionArchitecture, GateEnclosure, new GateCurtain and GateSurface; GateJamb, GateCurtainDepth and GateSurface EditMode tests. Matched capture: Tools/ArtReview/FoundryGateCohesionCapture.cs and launcher.

438 .NET / 681 EditMode / 160 PlayMode = 1,279 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and final After Android-target graphics captures passed separately. Focused Foundry 279/279 passed. PASS: 631 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,485 bytes; SHA256 E7B574E2F7B9CA149DC978C338A3A669D92E906BD8A32C1379BB760D21A3C64C; NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-gate-cohesion-01.


## 2026-10-14 - Foundry scene cohesion verified; device pending

Three coordinated scene changes: floor-only fire radii localize warmth to the pit collar, furnace fronts and lanterns while wall/iron source radii stay unchanged; shared paving grain uses a finer 4.3 span and quieter broad patina; supported billets and open casting flasks fill existing off-walkway shelf slots; lower fortress, bridge bearings and near-island curtains use a clean existing atlas field with restrained exposed-stone lift.

Files: FoundryRegionFinish, FortressSurface, Undercroft, Workshop, CastingBench, new WorkyardStock; CourtMaterial, CourtCompass, FortressHierarchy, FortressSurface and new WorkyardStock EditMode tests. Matched capture: Tools/ArtReview/FoundrySceneCohesionCapture.cs and launcher.

438 .NET / 687 EditMode / 160 PlayMode = 1,285 accepted suite executions, zero failures/skips. Focused Foundry 285/285 passed separately. Isolated compile zero warnings/errors. Fresh Before and final After Android-target graphics captures passed separately. PASS: 633 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,485 bytes; SHA256 2D4BAB5DBE3300908BAF8D3CC850FD9B07AE588A32709787C634AE48B0110B45; NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-scene-cohesion-01.
