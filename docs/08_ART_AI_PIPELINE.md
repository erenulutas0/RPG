# Art Direction & AI-Assisted Asset Pipeline

**Painted platform runtime, 2026-09-18:** V2 floor, coping and near walls are integrated through `PlatformMaterialSet` and nine shared meshes. Original PNGs remain byte-identical to the accepted masters; Unity imports floor/wall1024-square ASTC6 and coping1024x256 ASTC4, sRGB/non-readable/trilinear/mips/Repeat. A first NPOT341-height coping import silently fell back to RGBA32; nearest-power-of-two sampling and Android-format tests fix this. Total calculated ASTC block payload including mips is1,602,160 bytes, excluding foundation/native overhead. Foundation-only procedural art removes the old top/faces/towers/flames, eliminating rim bleed; exact corner joins and a dark wall/keel lip preserve the existing boundary. Imported materials/textures are asset-owned; generated meshes/foundation are session-owned with private disposal for different geometry. See `ArtDirection/2026-09-18/platform-runtime-01/` and latest Docs/21–23. Lower keel/crystal/chains, chest/ring and remaining roster remain provisional. This supersedes the proof-only/no-phone status immediately below.

**Platform material proof, 2026-09-17:** `ArtDirection/2026-09-17/platform-material-01/` retains unchanged generated floor V1/V2, coping and near-wall masters, exact prompts/hashes and fourteen actual Unity comparison renders. Prefer darker V2: reduce grout/bevel competition with painted actors. Seven temporary UV-mapped quads preserve the existing diamond, ground-effect sorting and combat state; old keel/crystal/corner fixtures remain beneath. No authored runtime Assets, scene, APK or phone change. Full-size mipmapped sources are proof inputs, not a mobile import budget. Resolve corner/old-outline and wall-to-keel joins, then reduced-size/compression/cache/reload/device validation before shipping. See README/mapping.json; this does not supersede the installed Grunt build.

**Grunt runtime update, 2026-09-17:** `ArtDirection/2026-09-17/grunt-polish-01/` now supplies the ordinary Grunt in the game. Selected rear angle and opposing-leg passing poses join the retained front sheet: 12 body/flash pairs and two pooled collapse sprites, all180x180/128PPU/bilinear/pivot90,18. Rigid registration and uniform per-sheet sampling; untouched originals and rejected attempts retained. Optional passing indices4/5 extend EnemyArtSet without changing Mite's original four-frame layout. Grunt walks1,4,2,5 over .7 floor units, visual top1.05/bar root1.17, shadow .78, zero old attack nudge. Damage remains immediate; no source windup is imported. Shared lifetime, four facings, pause, strike/flash and death/reward pass runtime tests. This supersedes the no-runtime-import status below. Strict foot locking, volume/lighting continuity and depth occlusion remain open.

**Grunt motion/sampling proof, 2026-09-17:** `ArtDirection/2026-09-17/grunt-motion-01/` extends the design into front/rear six-pose blocking sheets and actual Unity captures. The first front strike used the wrong arm and was corrected. Explicit source rectangles isolate imperfect sheet cells; manually judged floor roots receive rigid translation into a common 640x640 canvas, uniformly sampled to 180x180 at128PPU/bilinear, pivot90,18. Front idle visible height is approximately .936 units. Do not normalize each pose by alpha bounds or use the padded 1.40625-unit canvas top for runtime bar placement. Twelve reduced pose captures, before/after real density-room captures and close-pair captures are retained. Rear angle, shape continuity, proper passing poses, planted feet and raised-fist/bar clearance are not final. No runtime import or phone change; the current four-pose `EnemyArtSet` and immediate-damage attack events were not changed to match this six-pose illustrative timeline.

**Grunt design proof, 2026-09-17:** `ArtDirection/2026-09-17/grunt-proof-01/` contains untouched front/rear 1254-square RGBA Slag Brute masters, exact prompts, measured alpha/SHA-256 and a browser comparison with Vanguard/Mite. Broad violet basalt, heavy fists and a small furnace vent distinguish the ordinary brawler from the flame-crowned Mite. Start at 112.5 visible source pixels at 1080/width9, an approximate .9375-world-unit candidate; this normalizes each master independently and is not a production pivot contract. Close depth pairs still overlap. Rear camera angle/shoulder shapes, shared registration, movement/strike and imported hit/death remain pending. No Assets, runtime, APK or phone change belongs to this proof; the preceding contact/grip build is still the installed baseline.

**Contact/grip update, 2026-09-17:** `ArtDirection/2026-09-17/contact-grip-01/` records a runtime polish of the imported hero and Mite. The hero's 0.22-unit upward attack nudge is removed; the four-pose stride is 0.8 floor units rather than 1.2. Eight 11x8 knuckle crops reuse the existing sampled front-body pixels at 128 PPU with exact local registration, bilinear filtering, no mips/readback and Android ASTC 4x4. One borrowed-sprite overlay draws above the front sword and below the hit silhouette; rear views and other loadouts do not use it. This is technical extraction, not new generated painting. Mite bar parent scale is `(0.65,0.8,1)` with unchanged health/fill math. Real Unity and S23 captures are retained; full gate passed and APK installed. Strict foot locking, additional gait poses, mirrored lighting/handedness, matching Staff/Daggers and large-enemy art remain open. This supersedes the historical preview-only and absent-grip statements below.

**Equipment/keypose update, 2026-09-17:** `ArtDirection/2026-09-16/hero-motion-01/` contains original matching sword/buckler masters, two alternating step candidates, one attack windup and actual Unity captures with a shared body canvas/pivot. One same-footfall B attempt was rejected and corrected. This is rough animation blocking: idle remains a temporary passing pose, planted-foot motion and strike/recovery are not accepted. Hand anchors and world height are checked; gait, grip occlusion, opposing facings, hit/death and production imports remain open. Runtime actors and phone build remain unchanged.

**Character sampling proof, 2026-09-16:** `ArtDirection/2026-09-16/character-unity-01/` now compares the original Vanguard/Mite masters in actual Unity renders at 32/64/128 PPU, constant world height, with existing weapon layers. 128 PPU/bilinear is the next production **candidate**, not an approved global contract or installed asset. Old hand anchors do not fit this pose; measured candidate anchors retain the grip through a rigid translation/sword-rotation diagnostic. This does not prove walking, facing, finger occlusion or production attack frames. Matching equipment art and a planted-foot movement proof remain before runtime integration. The existing shipped actors/floor/weapons still use 32 PPU. The temporary review fixture and format-only bridge live under `Tools/ArtReview`; no authored Assets or settings changed.

**Stone/contact contract, 2026-09-16:** `ArtDirection/2026-09-16/stone-contact-01/` records a deterministic material pass: low-contrast slab bevels/patches/chips and flush segmented coping with brass clamps. Platform geometry, 581x419 surface and two light frames, 32 PPU/point filtering and pivots remain. A single 32x16, centre-pivot, point-filtered RGBA32 contact shadow is borrowed by hero/enemy renderers from `ArenaSpriteCache`; it adds 2,048 texel bytes, survives scene reload and is released on session reset/quit. Shadows follow feet, hide on death and shrink inside the rim; they are not directional cast shadows. Corner props, sidewalls, chest and actor sheets still need their later production pass. No new generated raster or scene/prefab edit belongs to this slice.

**Latest environment slice, 2026-09-16:** the authored scene now uses one imported dim furnace-cavern plate behind its existing procedural platform and actors, with room-bound width-9 camera framing. `ArtDirection/2026-09-16/furnace-layer-01/` records the exact built-in generation prompt, untouched source hash, import contract and actual Unity review renders. This supersedes the cosmic-runtime statement below. The distant plate deliberately uses bilinear filtering at a 64-PPU import; characters/floor retain their point-filtered 32-PPU contract. It is the first environment layer, not completed reference-quality floor/props/characters or a new boss room. See the latest docs/21–23 for verification.

## Main recommendation

**Owner update 2026-09-16 — combat-scene priority:** after the first HUD/result/Forge integration, the owner selected the supplied furnace-cavern image as the next visual target: a contained boss platform and broader, denser normal rooms that support kiting. For this two-room target this supersedes the earlier cosmic-only environment preference below; the foundry identity, no-skeleton rule and combat/background contrast hierarchy remain. `ArtDirection/2026-09-16/arena-target/` preserves the reference, first comparison, exact prompt, production order and technical handoff. These are concept deliverables, not imported runtime art. The current shipped scene still has its procedural actors and cosmic environment. Pause-menu polish is deferred behind the combat-scene proof.

**Do not begin by generating final sprite sheets.**

Start with:
1. style exploration,
2. concept sheet,
3. gameplay prototype with placeholders,
4. choose target sprite resolution,
5. create one production-quality hero,
6. validate full pipeline,
7. scale.

## Why

General image models are strong at:
- concepts,
- mood,
- silhouettes,
- UI ideation,
- marketing art.

They are weaker at:
- frame-to-frame animation consistency,
- exact pixel grid discipline,
- repeated proportions,
- deterministic equipment swaps,
- production-ready sprite atlases.

Therefore AI should accelerate art direction, not dictate asset production blindly.

## Art bible — initial constraints

Suggested prototype:
- 32×32 or 48×48 logical character footprint,
- final exact choice after device readability test,
- limited palette per biome,
- clear dark outline or controlled silhouette separation,
- exaggerated weapons,
- readable status VFX,
- avoid micro-detail.

## Visual tone hypotheses

Test 2–3 concepts only:

### A. Dark-cute crypt
- skulls,
- moss,
- candles,
- charming proportions.

### B. Arcane guild
- saturated spells,
- mystical equipment,
- cleaner fantasy UI.

### C. Cursed treasure dungeon
- coins,
- mimics,
- relics,
- greed theme.

Pick one after:
- thumbnail readability,
- creative marketing appeal,
- production speed.

Status 2026-09-12: **A is excluded** (it is the reference game's identity; see `24`). **B is the working assumption** with a foundry twist: the guild hall sits above an ancient forge, floors descend through furnace biomes (ember halls, quicksilver vaults, arcane kiln), and saturated spell/status colors carry combat readability. The first style board compares B against C only; C's coin/greed motif may survive as loot presentation inside B, never as the theme.

Status 2026-09-14: the owner chose **astral foundry**, a cosmic take on B. Each floor is a forge island floating in a violet void, and floors differ by island materials and nebula color. `ArtDirection/2026-09-14/mockups/cosmic-v1.png` is the layout and palette target. `furnace-cavern-v1.png` was compared and not chosen: its lava and orange fissures compete with the ember-colored enemies. Rules from the review:
- dark stone and brass tiles, not purple ones;
- no skulls or skeletal faces on enemies or glyphs (the first cosmic render had a skull boss marker and a skull-like golem, both removed);
- backgrounds lower in contrast than combat.

The mockups are references, not assets; see `19`.

Status 2026-09-15: the owner approved the combined direction in `ArtDirection/2026-09-15/sprint-01/`: A's character prominence, B's quieter floor and C's opaque themed upgrade-card hierarchy. This supersedes the old full-island mockup as the combat composition target; its astral-foundry identity and palette remain. Compact progress/build HUD above, grouped HP/XP below, one hero-centred active ability. Preserve current character sprites for the first UI integration. Exact camera width, sprite grid and on-device layout still require a real scene comparison; generated camera variants are approximate. Text-free UI masters and a separate browser component preview are in `ArtDirection/2026-09-15/ui-kit-01/`; they are not yet imported or device-validated production sprites. See the accepted decision in `19`.

Status 2026-09-14 (later): the gameplay prototype now draws its placeholders from code as pixel art at 32 texels per world unit (`Scripts/Art`, see `21`): the platform, the void, a 40-texel hero, six enemy looks and the combat effects. This is step 3 of the list above with a first candidate for step 4; nothing here is a production sprite, and the device readability test decides the density.

## AI concept workflow

### Step 1 — style board
Generate:
- hero lineup,
- enemy lineup,
- one room,
- weapons,
- UI accents.

### Step 2 — silhouette lock
For selected hero:
- front/3/4 concept,
- idle silhouette,
- weapon scale.

### Step 3 — sprite target
Translate design manually/with assistance into:
- exact pixel canvas,
- fixed palette,
- fixed proportions.

### Step 4 — Aseprite cleanup
Human cleanup:
- remove anti-aliasing,
- correct clusters,
- enforce palette,
- animation timing,
- readability.

### Step 5 — Unity
Use official Aseprite importer or exported sprite sheets.
Use Pixel Perfect camera.

## Production animation budget

Prototype hero:
- Idle: 4 frames
- Attack: 4–6
- Hit: 2–3
- Death: 4–6

Enemies can be cheaper:
- Idle/move: 2–4
- Attack: 3–5
- Death: 3–5

Do not animate six heroes before one hero feels good.

## VFX strategy

VFX can carry much of the perceived quality:
- hit flash,
- tiny screen shake,
- impact burst,
- crit number,
- coin magnet,
- upgrade pulse.

Use restrained effects; mobile readability first.

## UI art

AI can help generate:
- frame motifs,
- icon ideas,
- backgrounds,
- store art concepts.

But production UI should use:
- scalable panels,
- consistent typography,
- reusable components.

Avoid AI-generated text baked into images.

## Marketing art

This is where high-fidelity generative tools can be very valuable:
- key art,
- ad concepts,
- social post compositions,
- trailer storyboards.

Marketing visuals must not misrepresent gameplay.

## Asset naming

```text
CHR_Vanguard_Idle
CHR_Vanguard_Attack
ENM_Skeleton_Grunt
WPN_Sword_Iron
VFX_Hit_Physical
UI_Icon_Crit
ENV_Crypt_Wall_A
```

## Asset acceptance checklist

- readable at target phone size,
- palette consistent,
- no accidental anti-aliasing,
- pivot correct,
- PPU correct,
- animation loops cleanly,
- no legal/IP resemblance to reference games,
- source/project file retained,
- license/provenance documented.

## First Astra/Image prompt template

Use a concept prompt, not a final sheet request:

> Create a cohesive visual style exploration for an original portrait mobile idle auto-battler dungeon RPG. Show one hero, three enemies, three weapon silhouettes, one crypt room and a compact mobile HUD direction. Prioritize small-screen readability, strong silhouettes, limited palette, charming dark fantasy, production-friendly pixel-art design. Do not imitate any existing game or franchise. This is an art-direction board, not a final sprite sheet.

## When to generate final sprites

Only after:
- target resolution is locked,
- combat camera is locked,
- prototype screen density is tested on device,
- hero proportions are accepted,
- import pipeline is tested end-to-end.

## Foundry UI integration — 2026-09-16

The approved `ArtDirection/2026-09-15/ui-kit-01` and `ui-kit-02` masters now supply eight sprites in `Assets/_Project/Art/UI`. This is the first integrated HUD/card/ability art slice, not approval of a final character roster. See `ArtDirection/2026-09-15/UNITY_INTEGRATION.md` for source provenance, import settings, mask/slice use, ownership and remaining limitations; `21`/`22`/`23` hold runtime and device verification. Existing actors, weapons and arena geometry remain under the 32 PPU procedural-art contract.

## First character concept pair — 2026-09-16

`ArtDirection/2026-09-16/character-proof-01/` contains original Vanguard and Cinder Mite RGBA masters, exact built-in generation prompts, alpha bounds/hashes and an interactive browser scale study. Vanguard v2 is an unarmed body with separate-equipment intent; v1 is retained as a rejected exploration. The Mite uses low basalt masses and a compact flame. These are candidates for review, not owner-approved production sprites. Their antialiased painted rendering requires a reduced-sprite/import comparison before any change to the existing 32-PPU point-filtered actor contract. Fixed pivots, opposing facings, weapon anchors and animation consistency remain unproved. No Unity import or phone installation occurred in this art-only slice.

## First imported Vanguard runtime set — 2026-09-17

The eight-frame Vanguard prototype in `ArtDirection/2026-09-17/vanguard-runtime-01/` is now imported under `Assets/_Project/Art/Characters/Vanguard`. Its common 128x176 canvas, 128 PPU, bilinear filtering, fixed bottom pivot, per-frame hand anchors and Android ASTC 4x4 form a **character-specific import contract**. Procedural enemies/environment remain at their existing densities. Shared imported body/flash/equipment sprites are borrowed by the runtime view and are not destroyed on restart. Source masters, exact prompts, source hashes, actual Unity captures and reproduction tools are retained. One rear facing, imperfect foot contact, pending finger occlusion and procedural Staff/Daggers/effects mean this is still a prototype, not a final character sheet. See its README and current Docs/21–23.

## Vanguard directional extension — 2026-09-17

The latest set in `ArtDirection/2026-09-17/vanguard-directions-01/` supersedes the one-facing limitation above: eight front poses join the rear eight, with left-facing presentation mirrored at runtime. Two rear passing poses improve support-foot height. Rear registration stays unchanged; front uses 128x180 at 128 PPU, the common crop `(224,89,816,1152)` and pivot `(370/816,16/1152)` to preserve its lower boot pixels. Fixed source registration and per-pose hand anchors replace auto-fit bounds. Exact built-in imagegen prompts/references and unchanged source hashes are retained. This is four diagonal facings, not eight independently painted directions. Mirrored lighting/handedness, strict planted-foot animation and full finger occlusion remain unfinished; matching Staff/Daggers and enemies are separate work.

## Cinder Mite and contact effects — 2026-09-17

The first imported enemy has a character-specific contract: eight front/rear body/flash pairs and two death frames, all 80x68 at 128 PPU with a single source-coordinate registration. Four slash and four spark frames are 128x128 at 128 PPU. All are shared imported assets, bilinear, no mips, Android ASTC 4x4. Existing procedural looks keep their prior contract. Source PNGs remain unchanged; the Unity importer performs technical cropping/downsampling and silhouette derivation. Exact prompts, source hashes, rejected/replaced front walk contact and actual runtime captures are retained under `ArtDirection/2026-09-17/mite-runtime-01/`. Right facing mirrors the left-facing art, including lighting. Two walking contacts and one shared death facing are prototype constraints, not final animation acceptance. See the README for crop/pivot, ownership, reproduction and limitations.

## Chest source-art proof — 2026-09-18

`ArtDirection/2026-09-18/chest-proof-01/` now holds three transparent 1254-square closed/opening/spent candidates, two preceding concept boards, exact built-in imagegen prompts, hashes/alpha measurements and a responsive browser scale study. All poses share one source-root/scale in the preview; no individual bounds fitting. The emblem stays on the lower box and goes dark after opening. Small generated base/rim differences remain. This is art-only: no runtime imports, Unity capture, APK or phone change; existing installed platform build remains current. Next is real-scene sampling and continuity/occlusion review before integration. Reward timing and gold/heal rules are unchanged.

## Chest Unity sampling proof — 2026-09-18

`ArtDirection/2026-09-18/chest-unity-01/` records eleven actual Unity renders of the closed/opening/spent candidates in the ten-enemy scene, at .75 and .675 visible-box-width candidates and two portrait sizes. Temporary shared-canvas sampling only: no authored sprite import, scene save, gameplay change, APK or phone action. Prefer .75 for the next candidate. Front/behind hero staging exposes incorrect prop/body occlusion; resolve grounded depth sorting before runtime acceptance. Fixed pivot alone is not a finished depth solution. See README for sampling contract, scope and reproduction through `Tools/ArtReview/Run-ChestProof.ps1`.

Verified this slice: .NET 272/272, compile 0 warnings/errors, targeted graphics PlayMode capture 1/1, integrity 415 GUIDs / 555 scene objects/components. Full EditMode/PlayMode suites were not rerun; existing platform APK remains installed. Source art and prompts remain in `chest-proof-01`. Next: depth/ownership/import/opening integration and then coordinated device verification.

## Integrated foundry chest — 2026-09-18

Three shared imported 192-square ASTC4x4 sprites (closed/opening/spent), a ChestArtSet and one Gameplay scene reference replace the procedural prop when valid art is present. Common full-source registration, bilinear filtering, .75-unit reference box width; generated originals/prompts unchanged. Contact reward remains immediate/exactly once; the cosmetic .18-second opening respects pause. Reload borrows existing sprite/texture assets. GroundedSorting keeps each actor body/equipment/flash together at the floor root, while bars, shadows and ability ring retain independent global groups. This also changes actor/actor overlap presentation; no physical movement/spacing/balance rules changed.

Files: ChestSpawner, ChestArtSet, GroundedSorting, HeroLookView, EnemyLookView, ContactShadowView, AbilityRingView, imported Art/Props/FoundryChest, one scene reference, ChestPresentationTests/ArenaWalkTests and Tools/ArtReview chest import/capture scripts. See `ArtDirection/2026-09-18/chest-runtime-01/README.md` for reproduction and limits. Remaining: small generated frame drift, smooth lid animation, crowd occlusion and physical-device review; ability-ring artwork itself is unchanged.

Verified: .NET 272/272; compile 0 warnings/errors; EditMode 284/284; PlayMode 106/106; graphics capture 1/1; integrity 424 GUIDs / 555 scene objects/components; Android build exit 0. APK 24,751,592 bytes, SHA-256 `BA1F79545C8B3D70779A0380B2BD22A26BA0A8B88A3C09187B019DDCD852D814`. Two presentation tests cover immediate single reward, pause/resume, import format, common registration, independent ground/overlay groups and reload ownership. The first targeted test exposed an unsaved null art reference; importer now reloads the saved asset in the scene context and explicitly marks the scene dirty. A graphics check exposed the range ring inheriting the hero group; it now sorts independently.

Device status: NOT installed or phone-tested. An asynchronous device-availability question remains unanswered; no ADB input/capture/install or profile/telemetry mutation occurred in this slice. The previously installed platform APK is still the latest verified phone record. Do not claim this build's depth, performance or opening was verified on S23.

## Movement HUD visual hierarchy — 2026-09-19

The bottom panel now separates health, labelled MOVE and quieter XP into distinct rows. Full movement uses reduced visual emphasis; a nonempty-to-empty transition gives one 0.24-second scaled-time ember track pulse. Pause freezes it and refilling cancels it. The fraction is exact; HeroStamina values, combat, enemy density, camera and ability rules are unchanged.

Implementation and seven actual Unity renders at two portrait sizes: ArtDirection/2026-09-19/movement-hud-01/README.md. Runtime files: HeroStaminaView, Gameplay HUD layout; regression: ArtHudFlowTests. Reproduction: Tools/ArtReview/Style-MovementHud.ps1 and Run-MovementHudProof.ps1. No new raster generation. Device review remains pending coordination; no phone operation occurred. See the proof README for final verification and build identity.

## Ability ring comparison — 2026-09-19

ArtDirection/2026-09-19/ability-ring-01/ contains eight actual Unity stills comparing the existing dashed marker, a continuous contour and the preferred strengthened eight-arc seal, with cooling, shorter portrait and rim views. The first seal was too faint; its rejected capture is retained. Source is deterministic geometry in Tools/ArtReview/AbilityRingCapture.cs, staged only by Run-AbilityRingProof.ps1. No image generation, authored asset/scene save, gameplay change, new APK or device operation beyond ADB enumeration. The contour centreline maps to the actual ability radius; candidates preserve the clear centre and independent ground sorting. Cooling is a frozen alpha study, not live timing evidence. Next: bounded shared ownership/runtime integration, movement/cooldown verification and coordinated device review. See README for exact geometry, reproduction and limits.
Verified: .NET 291/291, compile 0 warnings/errors, graphics PlayMode capture 1/1, integrity 439 GUIDs / 568 scene objects/components. No runtime Assets change; full EditMode/PlayMode and APK build were not repeated. The previous movement-HUD full gate (306/108) and APK remain valid as the unchanged runtime baseline, not a test of an integrated new ring.

## Integrated eight-arc ability ring — 2026-09-19

AbilityRingView now borrows one session-owned 512x256 bilinear RGBA32 sprite from AbilityRingArt. Unit-radius contour at 240 PPU is scaled by the actual ability radius; transparent padding is not gameplay reach. Repeated scenes reuse the native art, subsystem reset and quit release it. Existing ready/cooling alpha, scaled-time breathing, ground sorting and hero following remain. No scene, damage, cooldown, density, targeting or movement-budget change. Old placeholder factory remains unused by this view.

Two new AbilityFlowTests cover real reload reuse, hero following, paused colour stability, immutable texture contract and native reset/recreation; the reach assertion now checks the contour centreline. Four real integrated Unity captures include two portrait sizes, rim and actual ability use/cooling. Source/reproduction/limits: ArtDirection/2026-09-19/ability-ring-runtime-01/README.md. Final verification and device status are recorded there and in latest Docs/23. This supersedes proof-only status for the ring implementation, but not earlier device records until an installation is recorded.
