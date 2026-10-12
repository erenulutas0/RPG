# Art Direction & AI-Assisted Asset Pipeline

**Integrated ground reward, 2026-09-22:** `ArtDirection/2026-09-22/reward-runtime-01/` supersedes the source-only status below. One imported96x96/128PPU/bilinear/ASTC4 sprite at fixed full-source registration, .45 visible-unit width; a pooled GroundDropView observes existing events, keeps coincident drops separate and freezes cosmetic flights under game-time pause. Immediate payment and one-unit pickup rule unchanged. Independent shadow/root sorting, quiet idle, no old Descent instant-coin burst; Swarm/chests unchanged. Full gate380/395/124, graphics1, integrity482/568, Android0. APK9DD2A9A758C95DCB88F91E4204B0207D61E30389787AF674414A0805B19A0F28 includes rebuilt player data. S23 only enumerated; no install/live test, availability unanswered. Next: coordinated device review, then walked-exit presentation. See runtime README for source, tests and limits.

**Ground reward source study, 2026-09-22:** `ArtDirection/2026-09-22/reward-proof-01/` contains one unchanged built-in image_gen RGBA master, exact prompt, alpha/hash measurements, a browser density/scale/collection study and a four-part visual plan covering docs/35–37's ground rewards and finale indicators. Prefer the provisional 0.45-world-unit visible width for the combined cyan XP crystal/brass coins; one drop still pays both rewards immediately. Browser checks passed at desktop and 390px layouts. This is a source/proportion proof over an older Unity screenshot, not an integrated view or device result. Sorting, zero-component variants, duplicate-position event matching, pause and shared ownership need actual Unity validation before import/runtime acceptance. No gameplay, Assets, APK or phone change. See its README for reproduction and exact limits.

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

## Ability cast pulse — 2026-09-19

CombatEffectsView now borrows the shared eight-arc range sprite for a pooled, ground-sorted cast pulse. Starts at60% radius, expands to100% by0.14s and expires at0.21s, preserving old total duration and immediate damage. Fade/expansion use scaled time; the visual remains at cast origin. Pool reuse clears pulse state; no per-cast art allocation. Staff splash and other effects unchanged. New AbilityFlowTests case covers origin, radius, pause, expiry and ownership. Seven actual Unity frame captures and reproduction: ArtDirection/2026-09-19/ability-pulse-01/README.md. Full gate and phone evidence will be recorded there and in Docs/23.

## Staff splash visual proof — 2026-09-19

Ten real Unity renders in ArtDirection/2026-09-19/staff-splash-01 compare the current pixel blast with four tapered violet-white arcs below actors, at four stages and two portrait sizes. Preferred candidate has a clear centre and distinct shape from the ability's eight-arc seal. Isolated view-subscriber invocation and temporary pool frame substitution only; no weapon damage, authored Assets/scene, gameplay or phone change. Impact size retains the old shown fraction (1.75 units for Staff), not the full3.5 damage radius. Keep0.21s lifetime; production must use one shared contour with scale/alpha animation, not the proof's seven textures. Tools/ArtReview/Run-StaffSplashProof.ps1 reproduces the sample.

Verified: .NET291/291, compile0 warnings/errors, graphics capture1/1, integrity440/568. Full suites/APK were not rerun for tooling-only changes; latest installed ability-pulse APK66CE2B7B2CB39E273C2DDC35B5795EEA70E518B65FF40BD50F0A00CD87D94165 remains unchanged. No phone/profile/telemetry access. Next: bounded runtime ownership/pool/pause integration and full gate/device review. See artifact README for timing, staging and evidence limits.

## Shared Staff impact integration — 2026-09-19

CombatEffectsView now borrows one StaffSplashArt512x256 unit-radius contour, with four tapered violet-white arcs and no per-hit native allocation. Existing pooled renderer grows24/38→1 of the previous shown radius in0.14s, fades over the final0.11s and expires at0.21s. Target origin remains fixed; scaled time honours pause; ground order0 places the effect below bodies. Immediate damage, Staff3.5 splash reach, authored impact fraction/clamp and attack cadence remain unchanged. The ability pulse shares the animation path with its previous0.6 start/0.85 alpha retained; pool reuse clears both flags.

Files: StaffSplashArt.cs/meta, CombatEffectsView.cs, StaffSplashTests.cs/meta, runtime capture/runner/converter, ArtDirection/2026-09-19/staff-runtime-01. Two tests cover actual attack damage/origin/pause/radius/expiry and texture reload/reset ownership. Runtime visual captures use an actual attack. The proof's seven textures are not shipped. Verification/device results follow in the artifact README and Docs/23.

## Staff weapon source study — 2026-09-19

Built-in image_gen produced a blue-steel/brass/violet Staff candidate and a targeted exterior-glow revision, saved unchanged in ArtDirection/2026-09-19/staff-weapon-01 with exact prompts, hashes, alpha measurements and a browser proportion/grip study beside the existing Vanguard. Prefer v2 for Unity sampling. Small alpha<=16 stray pixels make naive nonzero-alpha fitting unsuitable; source alpha is preserved. Grip(512,940),170–210CSS-pixel source heights and±14-degree lean are provisional browser placements, not verified runtime anchors. Existing shaft/orb split, cast-lit orb, finger occlusion and rear/mirrored continuity still need reconciliation.

No runtime Assets/code/scene/APK or phone operation. No game suites repeated for source art; installed Staff-impact build7A107BD2 remains current. Next: fixed-registration real Unity idle/walk/cast sampling, then shared import and phone verification. See artifact README/PROMPTS for full scope and provenance.

Imported Staff: fixed master crop (416,36,192,1464), uniform 1/8 sampling to 24x183 at 128 PPU, pivot (.5,560/1464), Android ASTC4. Whole crystal/shaft shares one sprite; no runtime pixel generation for this imported sprite. See ArtDirection/2026-09-19/staff-weapon-runtime-01/README.md and Tools/ArtReview/Import-StaffWeapon.ps1. Original PNG is preserved; mirror light and final animation polish remain limitations.

## Daggers source study — 2026-09-19

ArtDirection/2026-09-19/daggers-weapon-01 contains one unchanged built-in imagegen master, exact prompt, alpha measurements, hash and browser two-hand proportion study. Broad silver/blue-steel blade and brass guard match Vanguard; one source is intended for both hands to avoid design drift. Corner alpha0; faint exterior alpha<=16 means no naive alpha-auto-fit. Provisional grip628,975 on1254-square master. Browser comparison64px/35degrees and56px/45degrees/mirrored passed; returned to64/35. Finger masking, rear depth and real .12-unit thrust are not yet validated with this source.

Verified: static integrity443 project GUIDs/1099package GUIDs/568scene objects. Source and hero images rendered in the browser; size/angle/mirror controls checked. No Assets/C# runtime/scene changes, Unity suite/build or phone operation. Installed Staff build4A73EE71 remains current. Next: fixed-registration Unity sampling for both hands, grip/depth/thrust review, then shared import and device verification.

Daggers Unity proof (2026-09-19): fixed crop438,10,380,1220 ->20x64/128PPU, provisional pivot(.5,255/1220). See daggers-unity-01: existing thrust breaks contact; illustrated anchored alternative preferred for integration. Source remains unchanged; only temporary texture/transform substitutions were rendered.

Imported Daggers (2026-09-19): dagger-v1 source remains unchanged; fixed crop438,10,380,1220 ->20x64/128PPU pivot(.5,255/1220), shared for both hands. Optional art reference controls anchored angular presentation, no per-hit pixels. Actual runtime proof in daggers-runtime-01; former daggers-unity-01 displacement fixture is a historical preintegration proof.

## Painted XP-only reward source — 2026-09-22

Created ArtDirection/2026-09-22/xp-reward-proof-01: one unchanged built-in image_gen edit of the combined master, with coins removed; full prompt/provenance and alpha metadata recorded. Prefer provisional0.30 visible width against combined0.45; root623,1143 on1254-square canvas. Browser mixed/density/grayscale/flight controls and390px layout verified. Source/proof only; no Assets, gameplay, APK or device changes. Next: Unity sampling/import and borrowed XP-only sprite binding, then integration/device checks. The common CinderMite fallback remains in the installed game until that work is complete.

XP-only companion integrated 2026-09-23; see ArtDirection/2026-09-22/xp-reward-runtime-01/README.md for full-canvas registration, shared ownership, actual Unity evidence and verification. No source pixel edits.

## Finale exit visual proof — 2026-09-23

ArtDirection/2026-09-23/exit-seal-proof-01 contains a code-native browser study: brass ground seal, gateway glyph, mint hold contour using the existing0.9radius/1second rule. Scope is the existing final swarm exit, not normal-room exits. Idle/moving/holding/ended/Descent states, interruption and pause demonstrated; browser smoke and390px overflow passed. Flattened old background cannot prove actor depth or current camera positioning. Runtime/off-screen direction cue remain next; no Assets/APK/device change. Exact implementation and test plan is in its README.

Finale exit: deterministic brass/gateway contours now integrated through ExitSealView; no new raster source. Actual Unity captures and ownership/gate notes: ArtDirection/2026-09-23/exit-seal-runtime-01/README.md. Approved region-map-01 remains a separate browser composition, not a playable route.

## Casting Courtyard concept — 2026-09-23

Two unchanged built-in image_gen concepts and exact prompts are in ArtDirection/2026-09-23/casting-yard-01. PreferV2: quieter foundry exterior and work lights, open stone floor and peripheral industrial props distinguish the first normal combat area from the boss target. Not a playable geometry or flat runtime background. Next: measured convex greybox/camera/10-enemy comparison before layered production art. No Assets/APK/device changes. Owner says Opus route task has not started. Exit-seal APK28B658D4 remains built/not installed; XP-only1F00CF65 is the latest installed record.

## Superseding region direction and first traversal proof — 2026-09-23

Owner approved a fixed explorable multi-screen isometric region, replacing the small node-selected route. Encounters/chests/forge/elite are physical destinations; boss hall remains separate and optional final swarm direction is preserved. Read ArtDirection/2026-09-23/exploration-proof-01/DIRECTION.md. Old region-map-01/OPUS_TASK.md is explicitly superseded; Opus has not started according to last reply. No boss unlock/key/reward policy invented.

Temporary Unity scene copy enlarged convex arena6x area at unchanged hero/camera scale. Real movement/stamina legs measured6.78/18.53/18.55 game seconds; graphics1 and integrity485/568 passed. Initial unlisted-scene load failed, corrected with EditorSceneManager without build-profile edits. No production Assets/gameplay/settings/APK/device change. Current floor material stretches visibly: next art work needs stable-scale tiled ground. Encounter activation and exploration cadence remain unimplemented; no combat balance or device-performance claim.

## 2026-09-23 — Fixed-scale wide-region paving

PlatformMaterialMeshes now derives floor UVs from planar floor coordinates at the original 9-unit hall scale. The default hall retains its corner UVs; elongated regions no longer stretch/shear the paving. PlatformMaterialTests adds a hall/wide spatial-density check. Actual traversal screenshots in ArtDirection/2026-09-23/exploration-proof-01 were refreshed; ENVIRONMENT_KIT.md defines reusable perimeter scenery and explicitly defers walkable elevations. Wide geometry still exists only in the temporary capture scene. Coping/walls/foundation and physical encounter locations remain open.

Verified: .NET 380; compile 0 warnings/errors; targeted PlatformMaterialTests 5/5; graphics traversal 1/1. No full-suite run, APK rebuild, installation or phone interaction for this bounded material slice. Latest installed XP-only build and previous exit-seal APK remain unchanged. No new performance claim.

## 2026-09-23 — Wide-region coping and furnace source

PlatformMaterialMeshes keeps coping perpendicular floor width at the original hall value (0.45/sqrt(2)), with a conservative half-scale inset cap for tiny geometries. Coping/wall/join repetitions follow floor-space edge length instead of stretching across the region. Shared miter vertices and nine-mesh ownership remain. PlatformMaterialTests checks hall/wide width, texture density and corner joins. Graphics traversal refreshed and inspected. This is repeated material geometry, not separate placed wall prefab modules.

Verified: .NET380, compile clean, PlatformMaterialTests5/5, graphics traversal1/1. No full suite/APK/device change. Wide region remains temporary. New unchanged built-in image_gen furnace source and exact prompt in ArtDirection/2026-09-23/furnace-prop-01; RGBA1358x1159, transparent alpha, browser390px no overflow/all images loaded. Source only, not imported or placed. Next: sample/place reusable perimeter props, then live region integration with explicit local-encounter authority. No collision/elevation mechanic added.

## 2026-09-23 — Furnace imported, isolated Unity placement

Imported shared Furnace.tga (384x328/128PPU/custom pivot/non-readable/ASTC4) under Art/Props/Foundry. Importer samples unchanged source once and does not edit Gameplay. FurnaceCapture places it at north exterior on a temporary stone-textured foundation; real hero traversal and reference portrait capture. Initial oversized grey foundation corrected in second render. No collision, interaction, playable extension or production scene reference.

Verified: compile0 warnings/errors, final graphics1/1, static487GUIDs/1099package/568scene objects, browser390px no overflow/all images loaded. Full suites and APK not rerun for source/import/temporary fixture slice. No phone action. Read ArtDirection/2026-09-23/furnace-unity-01/README.md. Next: bounded region scenery integration and explicit playable entry, full gate then device; local encounters remain separate and pending.

## 2026-09-23 — Development region entry and scenery ownership

Decision: keep the authored hall as default and expose the wide traversal preview through a separate development/region-preview.txt flag (wide). Floor selection remains independent. ArenaView resolves geometry once before the first consumer; release short-circuits IO. This avoids silently changing all authored combat geometry while the physical encounter-site design is still pending. FoundrySceneryView borrows furnace/floor assets and owns its sole generated foundation mesh. No per-frame work, new reward, collision, stamina or progression rules.

The ordinary boss hall is not replaced. Current linear rooms still run in the wide preview; do not call that local exploration. Next mechanics work is authored encounter activation and boss approach, coordinated separately from art. Temporary art captures disable attacks only inside the fixture, not through the development flag.

## 2026-09-23 — Art-first foundry region layout

Owner explicitly deferred Opus and approved shaping the map first. New ArtDirection/2026-09-23/foundry-layout-01 contains an interactive top-down/isometric surface proposal, fixed-scale schematic portrait camera and one untouched built-in image_gen concept. Five identities: entry, large casting square, two-connected salvage court, broad furnace passage and separate boss approach. Routes are flat; concave outline and side hole are proposed only, not current ArenaGeometry.

Browser verified at1280 and390 widths: five selectors, isometric pointer-to-floor conversion, images, no overflow or script errors. Desktop screenshot inspected. Exact image prompt in PROMPT.md; source SHA25698F7D14F6B3E3634A2DCA3D8AABF3910B5883C608244CE4E4868B6862F5C5C5D. Painted concept includes a few entrance steps and differs in exact geometry; those are not approved elevation mechanics. Diagram is placement authority, concept is atmosphere, neither is runtime collision. Design-space isometric coordinates require explicit conversion; schematic camera is not the runtime9-unit framing.

No Assets, runtime, APK, device or tests changed in this concept slice. Current installed C8 build and uninstalled furnace-clearance change remain as recorded. Opus handoff marked ON HOLD, not sent or started. Next: review region shape then isolated traversability/greybox and modular art; concave geometry cannot be installed as only a painted floor.

## 2026-09-23 — Approved foundry shape: static Unity surface and gate source

Owner approved matching the foundry concept. FoundryLayoutCapture creates the exact seven-rectangle union (952 connected cells) using existing materials in an isolated graphics fixture. Five portrait renders use runtime camera width9 and repositioned hero; no traversability, collision, HUD, gameplay or phone claims. Unsupported floating furnace placements were removed before final capture. Read ArtDirection/2026-09-23/foundry-unity-01/README.md.

Boss-gate-01 contains two unchanged generated gate sources and exact prompts/hashes. Both have exterior alpha haze; the cleanup attempt is rejected for production use. Gate is not imported. Next art: clean gate registration and supported perimeter modules. Opus remains ON HOLD.

Checks: final graphics1/1; integrity489GUIDs/1099package/568scene; browser five selectors, all images,390px no overflow/script errors. No runtime Assets/APK/device change, full game suites not repeated. Installed C8 build and prior uninstalled clearance correction remain unchanged.

## 2026-09-23 — Supported perimeter art study

Two furnace annexes and six low buttresses added only to isolated FoundryPerimeterCapture. Existing material/sprite borrowing, owned mesh cleanup, fixed952 walking-cell proposal. Corrected below-floor wall sorting and reduced annex footprint; visible coping seam still needs authored joins. Graphics1/1, integrity489/568, browser five selectors/390px checks passed. Gate V3 edit again has haze and shifted registration; source retained, not imported. Read foundry-perimeter-01/README.md and boss-gate-01. No runtime Assets/APK/phone changes. Opus remains on hold.

## 2026-09-23 — Owner requested playable art integration

Replaced the sequence of isolated map studies with a working development foundry map, shared geometry/pursuit and existing Descent combat. Current installed build7FB249A4, retained foundry flag. See foundry-playable-01 README and latest21–23. No new gate bitmap imported. Next decisions should come from owner play: traversal length, sparse dressing, encounter locations and future boss-room separation. External Opus handoff remains unstarted; this integration was completed here under the updated request.

## 2026-09-23 — Whole-map rough architecture

The playable foundry now has flat route inlays and central ring plus provisional architectural silhouettes for its five areas. See FoundryRegionView and foundry-playable-01/README.md. Meshes are built once from shared borrowed materials and owned until teardown. Raised silhouettes are outside traversal. Final registered artwork, coping joins and lighting remain future refinement; local-site encounter progression and boss transport remain separate mechanics.

## 2026-09-23 — Photograph-led foundry composition

Owner rejected the linked-room silhouette and explicitly requested the photograph structure. Placement authority has changed: the earlier schematic is historical. Current shared geometry is a broad court around a shaft, straight south bridge and northeast gate apron. Continuous bridge clipping, pursuit nodes and full interior spawn sweep were updated together. Exact geometry and rough-art limits are recorded in foundry-playable-01 README. The new layout-comparison.html presents reference and actual Unity overview side by side; it is a review page, not another proposed map. Final environment illustration/props remain unfinished. Code-native rough geometry uses existing imported materials; no new generated image or external asset was introduced.

## 2026-09-23 — Detailed boss-gate facade

First detailed architecture surface integrated in FoundryRegionView: opaque orthographic generated facade mapped onto the physical gate plane. Read gate-facade-01 README/PROMPT. Source1536x1024RGB retained unchanged; no alpha cleanup. Optional shared material in PlatformMaterialSet, clamped/mipmapped/ASTC6/max1024 import, borrowed asset ownership. Source and runtime PNG hash match F4B36B1BDA91DDDA076B5062E5BC39FA468C2F4299355AE0A66FCD20B21455B7. Graphics1/compile clean/integrity496/568; final gate/device follows in docs22/23. Sidewall and other architecture still coarse; gate remains scenery.


## 2026-09-23 - Foundry furnace and masonry integration

Four detailed furnace elevations now wrap the existing scenery footprints, with exposed chimney geometry. Foundry-only masonry replaces the earlier slit-pattern walls on parapets, deep supports and architectural sides; ordinary hall materials remain unchanged. World-continuous wall UVs and painter-ordered chimney caps address cell seams and roof overlap. All art is borrowed from imported assets; static meshes are still batched and owned. Geometry, camera, combat and progression unchanged.

Sources, exact built-in image_gen prompts, import settings and actual Unity captures: ArtDirection/2026-09-23/foundry-surfaces-01/README.md. No manual raster cleanup. Final silhouette/pipework/crane/shaft dressing remains unfinished. Compile clean, graphics traversal1, static integrity500/568; full gate and device result recorded separately in docs22/23.


## 2026-09-23 - Complete foundry dressing package

Owner requested the remaining environment dressing in one integrated update. FoundryRegionDressing adds braced crane, pulleys, hanging salvage basket, pipe runs/collars/valves/supports, heated panelled shaft lining and molten basin, iron roofs, inner guardrails, beacons and suspension chains. Shared map geometry, camera, encounters and economy unchanged. Two unchanged built-in image_gen material masters imported as shared non-readable ASTC6/max1024 assets. Static code-native meshes give these materials the needed shapes; no new Update, texture generation or light system.

Review/source/prompt contract: ArtDirection/2026-09-23/foundry-complete-01/README.md. Actual integrated five-position Unity traversal and close-up render proof passed; scene uses15 static renderer groups. Crane attachment and shaft panel aspect corrected after close review. Scenery rank allocation keeps background/floor/contact shadows/actors consistent. Full gate and final phone state follow in docs22/23. Named dressing scope complete; no functional crane/lava hazard/boss transport is implied.

## 2026-09-24 - Foundry static illumination and masonry depth

Owner authorized continued visual development toward the reference. FoundryRegionFinish adds build-time floor vertex colours (cool ambient, warm furnace/beacon pools, boundary darkening, subtle deterministic stone variation), subdivided bridge illumination, inset cap highlights and directional masonry face tones. Existing projected meshes/material batches remain; no realtime lights, normal maps, per-frame work, new raster source or Blender asset. Footprints, movement, combat and scene data were not changed. Prior energy deferral remains in the build.

Actual Unity five-position walk/capture passed. Matching full-map before/after and portrait views are in ArtDirection/2026-09-24/foundry-lighting-01. Static budget remains15 renderers, vertices16,896 ->20,996. This is a measured incremental shading improvement, not a claim of final reference-quality architecture. Blender is still a potential next production step for modeled silhouettes; it was not used here. Final suite/build/device evidence belongs in docs22/23 and the proof README.

## 2026-09-24 - Gate and boiler architecture

Owner continued reference-led art development. FoundryRegionArchitecture adds static projected chamfered/tapered gate pillars, stacked courses, cornice/crowns and four open-mouth boiler stacks. Uses existing imported masonry/iron; no generated raster or Blender asset in this pass. Door/window facade details retained; gate remains scenery, no walking/collision changes. Draw order corrected after close-up inspection: each higher shaft occludes the rear cap of the preceding course. Old rectangular pillars reduced to their foundations. Static15 renderers/23,160 vertices. Sources, exact limits and actual Unity full-map/portrait/detail captures: ArtDirection/2026-09-24/foundry-architecture-01/README.md. Full test/build/device gate follows docs22/23.

## 2026-09-24 - Combined courtyard and surroundings

Owner requested the whole courtyard/bridge/perimeter/crane/shaft package in one pass. Added optional imported FoundryPaving material from an unchanged built-in image_gen basalt source (source/hash/prompt in ArtDirection/2026-09-24/foundry-courtyard-01). Staggered2x1 paving clips to the existing map union; restrained flush joints and material fissures replace the old small repeated tile grid. Bridge piers support the beacons; transverse bridge bands, court border inlays/outer finials, crane gussets/drum/joint plates, shaft lip stones/crossbeam/chains share existing architecture materials. Narrower aligned bridge piers and removal of artificial line cracks followed actual graphics review.

No map/collision/combat/scene edits or Blender use. Runtime mesh/material ownership unchanged; optional region material leaves ordinary hall intact. Non-readable/mipmapped/ASTC6/max1024 texture adds asset memory. Geometry23,160 ->39,740 vertices; static renderer count remains15. Full five-point graphics traversal passed. FoundryRegionTests now also verifies paving material usage and shared resource survival across reload. Final suite/build/device evidence follows docs22/23. This finishes the named combined package, not the entire reference-quality art direction.

## 2026-09-24 - Below-deck architecture package

Owner continued art development after the combined courtyard installation. FoundryRegionUndercroft builds static exterior arch spans from the existing cell union, tapered deep ribs, two suspended chains and two furnace discharge casings/heat channels. Existing shared materials and ownership; 16 batched renderers, 44,716 vertices (previous 15 / 39,740). No new bitmap, Blender asset, per-frame animation, realtime lights or gameplay/collision/camera changes. Shaft lining is excluded from exterior arch placement. Energy remains deferred.

Actual five-position Unity traversal and close-up review passed; arch thickness and discharge painter order corrected before the final proof. Before/after full-map and details: ArtDirection/2026-09-24/foundry-undercroft-01/README.md and index.html. Full tests/build/device evidence is recorded separately in docs22/23. Reference-quality atmospheric effects and final model refinement remain unfinished; this package delivers supporting architecture only.

## 2026-09-25 - Live foundry atmosphere

FoundryAtmosphereView adds four chimney plumes, contained chimney/shaft embers and six restrained glow pulses using the existing shared vertex-colour material. One scene-owned dynamic mesh, 780 vertices / 720 triangles; fixed reusable arrays, scaled-time animation capped at 30 updates per second. Static foundry remains 16 renderer groups / 44,716 vertices, plus one atmosphere renderer. Actors remain in front of effects. No generated bitmap, texture processing, shader, realtime light, gameplay or camera changes.

FoundryRegionTests now verifies active motion, pause freeze, atmosphere mesh disposal and ordinary-hall exclusion on reload. Actual five-point graphics traversal and sampled animation proof use real Unity output; dedicated review camera exposes the full chimney plume without changing the playable framing. Production notes, comparison and frames: ArtDirection/2026-09-25/foundry-atmosphere-01/README.md. Final full-gate and device results follow in docs22/23. Reference-quality model refinement and startup performance remain open; these smoke discs do not claim volumetric rendering.

## 2026-09-29 - Combined foundry material and workshop pass

Integrated quieter paving (narrower joints, restrained stone variation/patina and broader furnace bounce), explicit gate/flue iron overlays, west ingot/parts-bin storage, north pipe/tool storage, a translucent engraved court seal and supported bright-core braziers. Workshop shelves remain outside the walking union. Existing textures are reused; no new raster generation, movement, collision, balance or progression change. Static environment census: 17 renderers / 46,476 vertices, up from 16 / 44,716. The existing atmosphere adds one renderer / 780 vertices separately. One additional explicit sorting group resolves gate hardware hidden under masonry.

Five delegated contributions (architecture, workshop, floor, independent review and presentation) were integrated and reviewed together. Actual Unity captures led to corrections for hidden metal overlays, overly solid seal marks, flat workshop UVs and dark flame sampling. Final reviewer found no blocking visual issue in the inspected views. This is an incremental pass, not reference-image quality completion; repeated masonry and silhouette richness remain future art work.

Proof: ArtDirection/2026-09-29/foundry-art-package-01. The before image is the correctly framed September 24 undercroft capture; atmosphere was already added September 25 and is not new in this package. The runtime capture helper now refreshes Backdrop.FrameCavern after camera/target changes: this fixes export framing, not game art. Device and final verification outcomes are recorded in docs/22 and docs/23.

## 2026-09-29 - Crane and supporting facade refinement

Built on the installed combined art package. Crane now has a cast mounting shoe/open winch cradle, broad bearing collar, solid counterweight with retaining straps and a depth-bearing hanging basket with scrap and open front bars. Existing Site winding drum retained. Undercroft flat ribs replaced with faceted tapered piers, deeper/wider supports every third span, terminal arch supports and dark iron anchor plates. All changes static and reuse existing texture/material batches; no gameplay, collision, scene or walking-union edits.

Actual five-point Unity graphics capture passed and the combined overview/crane/bridge details were visually inspected. Independent facade review found no material blocker. Static census 17 renderers / 47,096 vertices versus 17 / 46,476 in the preceding package; existing atmosphere remains 1 / 780. Proof: ArtDirection/2026-09-29/foundry-silhouette-01. This is incremental silhouette refinement, not completion of the painted reference target. Full test/build/device outcomes follow in docs22/23.

## 2026-09-29 - Combined landmark architecture

Gate header gained a shallow continuous masonry pediment; four boilers gained low plinths and tapered corner shoes; four bridge posts gained small cast sockets and dark metal/bronze inset panels; the shaft gained four-side segmented inner coping with narrow retaining saddles/anchors. Existing footprints, painted door/fire-window faces, brazier/smoke anchors and walking space remain unchanged. The preceding crane/undercroft package is included but still not installed. No new raster assets, materials, gameplay, scene/collision or camera changes.

Final actual five-point Unity graphics proof passed. Reviewed gate/furnace/bridge/shaft close-ups. First capture showed new diagonal metal flashing crossing the foreground tower; removed those lines and confirmed the correction in a second capture. Static scene is 17 renderer groups / 49,396 vertices (previous silhouette: 47,096); existing atmosphere separately 1 / 780. Proof: ArtDirection/2026-09-29/foundry-landmarks-01. This is incremental reference-led art development, not final painted-reference parity. Full suite and APK outcomes follow docs22/23. Owner was using phone; no phone action this slice.

## 2026-09-29 - Local masonry bounce and undercroft depth

FoundryRegionArchitecture wraps only stone boiler/gate tiers in source-local RGB bounce from existing furnace/brazier positions, fading out by elevation 3.5. Original ArchitecturalTier remains unchanged for iron caps and other Site users. FoundryRegionUndercroft adds smooth RGB attenuation relative to each local deck edge, darkening lower piers/arches/anchor plates while retaining alpha. Existing textures, topology, materials and batching unchanged: 17 static renderer groups / 49,396 vertices, plus existing atmosphere 1 / 780. No realtime light, per-frame work, new asset, gameplay/collision/scene/camera edit.

Actual five-point graphics capture passed. Root furnace/undercroft review and independent undercroft visual/code review accepted the subtle treatment. Before/after and closeups: ArtDirection/2026-09-29/foundry-depth-01. This is tonal integration, not completion of reference-level art. Full gate/device status follows docs22/23.

## 2026-09-29 - Workshop use and local soot

FoundryRegionWorkshop adds a compact anvil (wide foot, shaped waist, heel, horn, hardy-hole mark) and one resting hammer. Existing stock retained; west shelf extends outward to u=-23 while court-facing edge stays -18.45, v=5..8.4. All scenery stays outside the walking union and is supported by existing Block masonry. FoundryRegionFinish adds smooth irregular RGB-only soot near the shaft and stock edge, capped at17percent darkening; no cracks/raised rubble/obstacles. No new textures/materials/renderer groups, gameplay, scene/collision or camera change.

Actual five-point Unity graphics capture passed; root inspected workshop and shaft closeups. Static census17renderers/49,556vertices, previously49,396; unchanged atmosphere separately1/780. Preview ArtDirection/2026-09-29/foundry-use-01. Includes previous material-depth package which remains uninstalled. Full gate/device status follows docs22/23. This is an incremental environmental-storytelling detail, not final reference-level fidelity.

## 2026-09-29 - Three-agent visual closeout

Owner limited the work to at most three helper agents. Exactly three reviewed architecture, workshop and portrait readability. Closed two material findings: flue bands/slots now sample the actual tapered octagon at each height instead of extending past corners; anvil head/horn/existing edge use restrained steel tint to separate from dark stock. No geometry-count/material changes. The portrait audit found a capture-only HUD sorting artifact: production canvases are ScreenSpaceOverlay, but capture converts them to camera mode with order0 below actor order1. Capture now adds30000 to existing canvas order to preserve relative overlay/modal ordering. Production HUD/scene untouched. Fresh point3 confirms actors no longer cover HUD text/frame; earlier capture artifacts are not evidence of a runtime HUD bug.

Actual five-point graphics capture1/1 passed, furnace/workshop/portrait reviewed. Static17renderers/49,556vertices unchanged; atmosphere1/780. Includes prior uninstalled depth/use slices. Proof ArtDirection/2026-09-29/foundry-finish-01. No expansion into new decor, gameplay or map layout. Full gate/device status follows docs22/23; final reference fidelity remains open.

## 2026-09-29 - Segmented undercroft arch masonry

Replaced continuous arch faces with 13 individually separated stone faces over recessed mortar, a projecting central key, narrow lit upper arrises, and dark inner returns. Existing static wall material and local depth shading reused. Static budget: 17 renderers / 51,348 vertices (previous 49,556); atmosphere remains separately 1 / 780. No gameplay, collision, scene, camera, or raster asset changes. Actual Unity five-waypoint graphics traversal passed; undercroft, bridge, and full-map renders inspected. This is a subtle close-range refinement, not reference-art completion.

Compile: 0 warnings/errors. Integrity: 514 project GUIDs / 1,099 package GUIDs / 568 scene objects/components. Full gate: EditMode 405/405, PlayMode 130/130, Android build exit 0. APK actual size 39,340,606 bytes, built 2026-09-29 16:26:53 local; SHA256 F5723AB3A6204118B349301B3834043B215C5BFD9134664B066A2DF99DBB499F. READY, NOT INSTALLED. Last verified installed package remains 18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535. No phone operations or new device-performance/save claims. Proof: ArtDirection/2026-09-29/foundry-arches-01. No helper agents, commit, or push this slice.
## 2026-09-29 - Court, approach, and workshop paving

Added three flush paving regions in FoundryRegionFinish/FoundryRegionView: central 2x2 seal flags, crosswise staggered 1x2 gate-approach flags, and darker 1x1 workshop stones. General court remains staggered 2x1, with half flags at zone ends. Existing shared texture, static lighting, and batches retained. No walking-union, collision, scene, gameplay, camera, or raster asset change. Includes the prior uninstalled arch masonry slice. Static budget 17 renderers / 51,780 vertices (previous 51,348); atmosphere separately 1 / 780.

Actual five-waypoint Unity graphics traversal passed. Reviewed overview and center/approach/workshop portraits; comparison in ArtDirection/2026-09-29/foundry-paving-01. Compile 0 warnings/errors; integrity 514 project GUIDs / 1,099 package GUIDs / 568 scene objects/components. Full gate EditMode 405/405, PlayMode 130/130, Android exit 0. APK actual size 39,340,606 bytes, local build 2026-09-29 16:43:29, SHA256 EED797DA0FFA60A586E6523759A3261A6F35B6B53F91455DFBABB484C68C5BFB. READY, NOT INSTALLED. Last verified installed remains 18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535. No phone actions, new device measurements, helper agents, commit, or push. Reference-quality art is still in progress.
## 2026-09-29 - Gate finials and boiler shoulder braces

Added two supported gate finials with cast sockets, bronze collars, and tapered tips, plus two visible roof-mounted shoulder braces on each of four boiler chimneys. Existing mouth/smoke anchors, painted facade, footprints, and static iron batches retained. Capture-only gate framing expanded; baseline recaptured with the same new review framing before runtime edits. Gameplay camera unchanged. Root reviewed matched gate/furnace details, overview, and gate portrait. Finials sit above the close-follow gate portrait; their main impact is the wide silhouette. Proof: ArtDirection/2026-09-29/foundry-crowns-01.

Static 17 renderers / 52,556 vertices (previous 51,780); atmosphere separately 1 / 780. Graphics traversal 1/1 passed, compile 0 warnings/errors, integrity 514/1099/568. Full gate EditMode 405/405, PlayMode 130/130, Android build exit 0. APK actual size 39,340,606 bytes, local build 2026-09-29 16:57:43, SHA256 6DBD8583D538C3C77458F1DBFA8823D20146FEDF3D5E0C284E241BE373158EC8. READY, NOT INSTALLED. Includes the prior uninstalled arch and paving slices. Last verified installed remains 18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535. No phone actions, save changes, helper agents, commit, or push. Reference-quality art remains in progress.
## 2026-09-29 - Combined material, architecture, and atmosphere package

Owner requested several art tasks in one delivery. Integrated: unchanged generated honed basalt v2 (versioned source and Assets file, old v1 retained), 8 gate tower return louvre panels, iron roof aprons on 4 boilers, irregular soft smoke outlines with source-to-upper colour transition. Final review lifted panel edges and smoke visibility. Two implementation helpers plus one independent reviewer used (maximum 3). Final actual Unity images accepted with no blocking placement/readability issue. Smoke remains subtle; device motion/performance not claimed. Proof/source/prompt: ArtDirection/2026-09-29/foundry-integrated-01/README.md and PROMPTS.md. No raster editing; built-in image_gen source SHA256 63ED64CB8CFA2A6C450ED6EEE2C13B3869E4DD18D7414F6479DAE4DC89049FBA.

Static 17 renderers / 52,908 vertices; atmosphere 1 / 780 with 288 cached radius floats and existing 30 Hz/pause/ownership behavior. No walking-union, collision, scene, gameplay, or playable camera edits. Graphics traversal 1/1, compile 0 warnings/errors, integrity 515/1099/568. Full gate: EditMode 405/405, PlayMode 130/130, Android exit 0. APK actual size 39,340,606 bytes, local build 2026-09-29 17:21:08, SHA256 FD05B1749FB79D9E1EF7F992DBB61BF0B9FB83CA6E253FD9B74DC938D9F32BEC. READY, NOT INSTALLED. Includes prior uninstalled arches, paving, and crowns. Last verified installed remains 18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535. No phone actions, save edits, commit, or push. Named combined package complete; final reference-image quality remains open.
## 2026-09-29 - Combined foundry service, shaft hardware and floor-light pass

Completed in one visual package: supported north-shelf quenching basin, reserve billets and hollow strip coils; 32 recessed-looking shaft coping trays and three chain clevis/saddle connections; subtle gate/boiler contact shade, worn approach and outer bridge torch pools. Bridge floor is subdivided across its width for localized lighting. Reuses existing materials/textures and static batches; no gameplay, walkable bounds, collision or balance change.

Evidence: ArtDirection/2026-09-29/foundry-service-01/index.html and README.md. Before/after and closeups are real Unity captures. Graphics capture 1/1 passed; 405 EditMode + 130 PlayMode passed; static integrity 515 project GUIDs / 1099 package GUIDs / 568 scene objects/components passed. Final static budget 17 renderers / 54,840 vertices (+1,932 from prior package); unchanged separate atmosphere renderer. Three helpers covered workshop, shaft hardware and visual review. No blocking visual defect found; reference-image fidelity is still a work in progress.

Android build succeeded. APK Builds/Android/Cryptforge-Prototype.apk: 39,340,606 bytes, 2026-09-29 17:45:55 local; SHA-256 DFF2E84065DA0CCA3B80C8580B89814C30107D2AC889B5514D3E6176ED62529D. NOT installed or device-tested in this session. Last known installed phone APK remains 18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535; do not infer current device state.
## 2026-09-29 - Combined perimeter and service hardware finish

Eight exterior posts received stepped feet/collars, tapered stone crowns, bronze terminals and inset panels. Existing exterior pipe runs received four paired bolted unions and two valve bodies. Gate apron received eight flush metal wear-strip frames. Initial dark threshold plates were rejected in graphics review because they resembled holes; final strips leave stone visible and reuse the coping batch. Modified FoundryRegionSite, FoundryRegionDressing and FoundryRegionArchitecture only. No new bitmap/material, gameplay, collision, walking union, scene, camera or per-frame work. No helper agents used.

Actual Unity five-position capture passed 1/1; gate/furnace/undercroft/portrait/full-map reviewed. Static budget 17 renderers / 56,816 vertices (+1,976); existing atmosphere 1 / 780 separately, unchanged. Compile check passed 0 warnings/errors before final threshold adjustment; final Unity gate passed EditMode405/405, PlayMode130/130, Android exit0. Integrity515/1099/568 passed. Preview references11/11 resolved. Proof ArtDirection/2026-09-29/foundry-perimeter-finish-01/index.html and README.md.

APK Builds/Android/Cryptforge-Prototype.apk: actual39,340,606bytes, localtime2026-09-29 18:02:11, SHA2561A55731F755FC6C42D72392474DBBC2E7A696D243300AC47E0227E19AFF3386A. READY NOT INSTALLED. Includes previous uninstalled service/material/architecture changes. Last verified installed remains18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535. No device operations, save edits, new phone performance claims, commit or push. Reference-image fidelity remains in progress.
## 2026-09-29 - Combined court stonework finish

Added a flush 24-flag stone annulus beneath the existing central engraved seal (radius3.97, inside the +/-4 inset), restrained gate-road shoulder shading, sparse in-slab corner repairs, softer joints and reduced slab value variation. FoundryRegionFinish and FoundryRegionView only; existing paving batch and continuous UVs reused. No new material/texture, per-frame behavior, walking/collision, scene, gameplay or camera edits. No helpers used.

Actual Unity five-position capture1/1 passed; center combat and gate views reviewed. Compile0warnings/errors, integrity515/1099/568, full EditMode405/405 and PlayMode130/130 passed, Android exit0. Static17renderers/57,432vertices (+616), atmosphere1/780 unchanged. Preview HTTP200 and7/7 local references verified. Before/after and production notes: ArtDirection/2026-09-29/foundry-court-finish-01.

APK actual39,340,606bytes, localtime2026-09-29 19:11:24, SHA256103811AA22B982D4363C509FA4BBA8EBD6ADD7DDD2ED3D5D7567355D70E5FCE3. READY NOT INSTALLED. Includes all prior uninstalled art packages. Last verified installed remains18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535. No phone operations, save edits, new device performance claims, commit or push. Reference-image fidelity remains unfinished.
## 2026-09-29 - Exterior post lanterns and warm floor pools

Four existing exterior post tips now carry enclosed iron lanterns with two visible window faces, opaque cage bars, supported caps and hipped roofs. Four matching static floor-light sources added. FoundryRegionSite and FoundryRegionFinish only; existing texture reused without editing. No gameplay, walking/collision, scene, camera, save, per-frame light or particle changes. No agents used.

First full gate caught a renderer regression: 18 static groups exceeded the existing <=17 test. Kept the test unchanged; split panes into inset windows around bars and reused the existing flame batch. Final actual Unity capture1/1 passed with 17renderers/57,560vertices (+128 from prior), atmosphere1/780 unchanged. Final closeup and overview reviewed; cap UVs corrected after dark first render. Final full gate EditMode405/405, PlayMode130/130, Android exit0. Earlier integrity515/1099/568 and compile0warnings/errors passed. Proof ArtDirection/2026-09-29/foundry-lanterns-01.

APK actual39,340,606bytes, localtime2026-09-29 19:36:46, SHA2561C4A1D885F27C7A5B2DFDA74FF16B990D24BC0193887DD27229C34C3EECA6688. READY NOT INSTALLED. Includes previous uninstalled art passes. Last verified installed remains18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535. No phone operations, save edits, new device performance claim, commit or push. Reference-image quality remains in progress.
## 2026-09-29 - Pressure reservoirs integrated and verified

Two exterior tanks at (22.3,-5.8)/(22.3,15.8), each on supported2x2 masonry feet, now connect existing boiler service mains. Added faceted metal hulls, retaining belts, tapered shoulders, relief stems/wheels and short wall-to-main branches. All feet outside walking union. FoundryRegionDressing only; existing materials/static batches. No gameplay, collision, scene, camera, per-frame work or new raster assets. No agents used.

First visual review caught belt caps painting over the upper hull; corrected bottom-to-top course order. Near tank now emits after pipes, far tank before; branches begin on tank wall. Final actual five-point Unity capture1/1 passed; furnace closeup/overview reviewed. Static17renderers/59,584vertices (+2,024), atmosphere1/780 unchanged. Full gate EditMode405/405,PlayMode130/130,Android exit0. Compile/integrity earlier passed0warnings/errors and515/1099/568. Preview HTTP200,7/7 local links. Proof ArtDirection/2026-09-29/foundry-reservoirs-01.

APK actual39,340,606bytes, localtime2026-09-29 20:15:24, SHA25679B4C59EE5E4D7AA44BA53663228BA9D198BD7920DCCD1F004B7F0CDB7E62DC1. READY NOT INSTALLED. Last verified installed phone package1C4A1D885F27C7A5B2DFDA74FF16B990D24BC0193887DD27229C34C3EECA6688 remains; prior paused phone snapshot is historical until reinspected. No device operations/save edits/commit/push. Reference-image fidelity remains unfinished.
## 2026-09-29 - Reservoir package installed on S23

Owner explicitly authorized restarting the advanced Forge-choice run (40HP,5unbanked gold). Fresh backups under TestResults/foundry-reservoirs-install-20260929/before. adb install-r Success; installed base.apk SHA25679B4C59EE5E4D7AA44BA53663228BA9D198BD7920DCCD1F004B7F0CDB7E62DC1 matches local. All7 saved files byte-identical immediately after install. Supersedes pending installation above.

Startup run ended in Slag Gate defeat with0banked gold. Restarted through Try again; guarded northeast drags reached the boiler/tank group. Actual08-tank-detail.png confirms near reservoir hull, base, pipe connections and surrounding furnace art on S23. Movement/camera follow worked. A visual limitation is especially apparent here: foreground actor/ring rendering can make the hero look on top of projected boiler/pipe silhouettes while the walkable bounds remain unchanged. Record this as the next depth/readability issue; it was not fixed or claimed accepted in this device-only turn.

Left PAUSED in Cinder Walk2/6,wave1/3,77HP,level0,XP3/10,0runGold,Staff/SecondWind. Profile revision215/32776gold/deepest3 unchanged; profile,backup,two development flags,rotated telemetry byte-identical after review. Active telemetry retains exact prior prefix. Current-process log0matches for FATAL EXCEPTION/NullReferenceException/MissingReferenceException/InvalidOperationException. No save restore/edit/equipment purchase. Short art/movement smoke, not sustained performance or full-map acceptance. All input/capture gated by focus/awake/no-keyguard; post-capture focus rechecked. Evidence copied unchanged to ArtDirection/2026-09-29/foundry-reservoirs-01/device-tank.png. No code/build change,commit,push. Reinspect before future phone actions.
## 2026-09-29 - Foundry ground readability and hero cutaway

The foundry ability range ring and pooled ability/Staff pulses now use the arena ground-effect sorting rank (-13), instead of painting above every structure. Ordinary halls retain their prior effect rank. FoundryVisibilityView and the Resources FoundryCutaway shader reveal the existing floor through above-floor architecture in a soft ellipse around the living hero. Scene-owned material variants reuse source textures, update one uniform per variant, disable cleanly, and are destroyed on reload; shared imported materials survive. Static mesh construction and batches remain unchanged. This is a hero-centred readability cutaway, not physical depth sorting; distant enemies still render above structures.

Matched actual Unity captures at walkable design point (22,0), traversal portraits and full-map review: ArtDirection/2026-09-29/foundry-ground-readability-01. Both matched images use the new ring sorting; only cutaway enable differs. Five waypoint traversal1/1 passed. Static17renderers/59,584vertices and separate atmosphere1/780 unchanged. Gameplay, movement/collision, balance, scene, camera and save rules unchanged.

Targeted first test caught the old assertion that every material is borrowed; updated ownership checks require new variants to be destroyed and imported assets to survive. Final full gate EditMode405/405,PlayMode130/130,Android exit0. Final compile0warnings/errors; integrity518projectGUIDs/1099packageGUIDs/568sceneobjects passed. Proof HTTP200/local links resolved. Added checks cover cutaway focus/disable/reload ownership, grounded ring SortingGroup and real ability-event pulse rank.

APK Builds/Android/Cryptforge-Prototype.apk: actual39,340,731bytes, local2026-09-29 20:55:31, SHA25644566462D0203760FF3EA8B918175EEE553636DF6F35E7FDB565BA3E32780288. READY NOT INSTALLED. Last verified installed remains79B4C59EE5E4D7AA44BA53663228BA9D198BD7920DCCD1F004B7F0CDB7E62DC1. Device read-only check found S23 awake/unlocked at launcher; asked whether it is free, no reply yet. No phone inputs/install/save writes or device-performance claims. New shader cost and appearance on Android remain to be tested. No helper agents, commit or push.
## 2026-09-29 - Ground-readability package installed on S23

Owner said to continue after the pending installation question. Fresh guarded screenshot showed the same paused Cinder Walk run (77HP,0runGold). Backed up profile/backup/development flags/telemetry before adb install-r. Success; installed base.apk SHA25644566462D0203760FF3EA8B918175EEE553636DF6F35E7FDB565BA3E32780288 matches tested local build. All7 saved files byte-identical immediately after installation. Supersedes READY NOT INSTALLED above.

Actual phone inspection reached near boilers/reservoirs through controlled northeast drags. Cutaway renders on S23 and reveals floor around feet; range ring remains beneath solid architecture. Chose Long Reach at first level-up to continue traversal. Short live movement/combat smoke only, not sustained performance acceptance. Remote-enemy occlusion remains open. Current-process log: no FATAL EXCEPTION, NullReferenceException, MissingReferenceException, InvalidOperationException or shader error/unsupported matches.

Left PAUSED room3/6,wave2/2,28HP,level1,XP16/22,5at-risk gold,Staff/SecondWind. Profile revision215/32776gold/deepest3 unchanged; backup,development flags and rotated logs unchanged. Active telemetry preserves pre-test prefix. No profile restore/purchase. Evidence TestResults/foundry-visibility-install-20260929; unchanged actual screenshot at ArtDirection/2026-09-29/foundry-ground-readability-01/device.png. Inspect fresh before future phone inputs. No new code/build/commit/push this installation turn.

## 2026-09-30 - Foundry court floor at the gameplay camera

Ranked the gaps against the approved reference (`ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`) at the portrait gameplay camera first: the court paving fills about four fifths of that screen and its per-flag variation (+/-6 %) did not read on a phone. No raster art was generated or edited; this is procedural vertex colour and flush geometry on the existing paving and coping materials.

Integrated: skewed per-flag tone (darker replacement stones, paler warm ones), subtle warm/cool flag tint, darker and wider joints, stronger lit/shaded bevels (`FoundryRegionFinish.cs`); a court compass - eight brass rays from the forge seal to an outer ring of radius 8.6, clipped to the walking union so it breaks at the shaft (`FoundryRegionHeraldry.cs`). Fixed: flush floor decals shared rank -14 with raised parapet sides and therefore took the hero cutaway, so brass lines vanished around the hero's feet; they are now one `Foundry floor decals` batch on the plain material at the same rank (`FoundryRegionView.cs`).

Built and installed: see 22 and 23. Proof `ArtDirection/2026-09-30/foundry-floor-01/` (matched before/after actual Unity captures, device capture, README). Not addressed: landmark scale and silhouettes, parapet mass, chains and foundations, and depth ordering of tall structures against actors; the cutaway still opens structures behind the hero.


## 2026-09-30 - Foundry depth ordering and landmark mass

No raster art was generated or edited; procedural meshes, vertex colour and one shader change.

Integrated: real depth for tall structures - structure vertices carry z = 50 + ground-diagonal position, and the Resources `FoundryCutaway` shader gains a depth-only pass that writes it; actor sprites move to their feet depth only while the camera renders and are restored afterwards (`FoundryRegionDepth.cs`, `FoundryVisibilityView.cs`, `GroundedSorting.cs`). The hero cutaway now fades only structures in front of the hero and over his body. Landmark mass (`FoundryRegionMassing.cs`): four round boilers replace the square furnace towers (north pair tall, south pair squat so the hero never reads as standing on a roof at the gameplay camera), a pierced balustrade with posts and lanterns, a round furnace pit, boiler foundations, six bastion piers and chains; the square shaft, shaft collar, tower caps, shaft-corner beacons and short elbow drops were removed and the smoke sources follow the new flues.

Built and installed: see 22 and 23. Proof `ArtDirection/2026-09-30/foundry-depth-mass-01/`. Not addressed: hidden actors have no silhouette behind structures; chains read thin; piers are uniform; combat effects are not depth-sorted; surfaces are still vertex-coloured rather than painted (painted-texture brief in `ArtDirection/2026-09-30/painted-surfaces-01/PROMPTS.md`, nothing generated yet).


## 2026-09-30 - Hidden enemy silhouettes in the foundry

No raster art generated. Integrated: `Resources/FoundryActorXray.shader` (Sprites/Default pass plus a `ZTest Greater` pass that draws an ember inner outline and faint fill only where structure depth covers the sprite); `FoundryVisibilityView` gives live enemy bodies one shared scene-owned material with it, restores their materials when disabled and destroys it with the view; `EnemyLookView.Body` exposed. Hero and ground drops get no silhouette. Built and installed: see 22 and 23. Proof `ArtDirection/2026-09-30/foundry-xray-01/`. Not yet seen on the phone; one extra draw per enemy body, not measured on device.


## 2026-09-30 - Foundry rim trim: the vent-slot row removed

No raster art generated. Integrated: flush decal strips map onto named clean regions of `ENV_Platform_Coping.png` (six stone panel interiors, the top brass rail) instead of its corner, through `CopingTrim` and `Builder.MapLast` (`FoundryRegionView.cs`); brass inlays, compass lines, the forge mark and gate wear strips take the rail, channels, joints, borders and coping trim take stone. The per-cell 0.8 border dashes became a continuous kerb course (`FoundryRegionSite.cs`). Built, not installed (see 23). Proof `ArtDirection/2026-09-30/foundry-rim-01/`. The painted trim sheet (PROMPTS S4) remains the real rim fix; parapets are still low.


## 2026-09-30 - Foundry parallel pass: gate texture, seal, bridge, chains, cylinders, piers, loot silhouettes

No raster art generated. Integrated: gate facade import at 2048 with power-of-two scaling (the build report showed non-power-of-two textures ship uncompressed RGBA32: the gate was 1024x683 RGBA32 ~3.6 MB, now 2048x1024 ASTC 6x6 1.2 MB); court seal redrawn as a brass-lined eight-point star with a ring pair (`FoundryRegionHeraldry.cs`); bridge deck as coursed setts and flags (`FoundryRegionView.cs` `BuildBridgeDeck`, bridge joints in `FoundryRegionSite.cs`); heavy alternating chain links and reservoir plate UVs (`FoundryRegionDressing.cs`); boiler plate UVs and three pier forms (`FoundryRegionMassing.cs`); gold hidden-loot silhouettes (`FoundryVisibilityView.cs`, `GroundDropView.cs`). Built, not installed (see 23). Proof `ArtDirection/2026-09-30/foundry-parallel-01/`. Open: `FurnaceFacade.png` has the same non-power-of-two cost (3.6 MB RGBA32); chests are never depth-tested (actor root without children).


## 2026-09-30 - Foundry chest depth, forge light, bridge pass two, furnace texture

No raster art generated. Integrated: FurnaceFacade import with power-of-two scaling (683x1024 RGBA32 3.6 MB -> 1024x2048 ASTC 1.2 MB; build textures 18.1 MB); childless actor roots (the chest) move to feet depth during rendering and hidden chests get the loot silhouette (`FoundryVisibilityView.cs`, `ChestSpawner.cs`); floor light rebuilt from the real pit, boiler, lantern and beacon positions with a cool open court, retuned after the first capture (`FoundryRegionFinish.cs`); bridge stones moved onto the court's diagonal grid with rail kerbs (`FoundryRegionView.cs`, `FoundryRegionSite.cs`). Built, not installed (see 23). Proof `ArtDirection/2026-09-30/foundry-chest-light-01/`. Open: UI_Panel_ForgedBlank_v2 (9-slice sprite, 4.5 MB); pools read sandy rather than orange.


## 2026-09-30 - Forged UI panel import for shipping

No raster art generated. `UI_Panel_ForgedBlank_v2.png` (the shared 9-slice frame of HUD panels, modals and choice cards) had the temporary first-review import recorded on 2026-09-15: uncompressed, native 2172x724, shipping as RGB24 4.5 MB while each 128-px border is drawn about 18 px wide on the S23. Integrated in its meta only: Android override 1024 ASTC 4x4, Default max 1024 (uncompressed). Sprite borders and slice positions are unchanged in actual captures (`ArtDirection/2026-09-30/ui-panel-01/`, including a new level-up choice capture in `Tools/ArtReview/FoundryRuntimeCapture.cs`). Build report: 344.6 KB; build textures 13.9 MB. Built, not installed (see 23). ASTC appearance is still to be checked on the phone.

Installed 2026-09-30: APK CB669343... with all of the day's foundry packages is on the S23 (see 23); silhouettes still unobserved on device.


## 2026-09-30 - Foundry effect depth, forge-orange light, calmer bridge; texture pack v2

No raster art generated. Integrated: above-floor combat effects take their owner's ground depth during rendering in the foundry, numbers stay on top (`CombatEffectsView.cs`, `FoundryVisibilityView.cs`); floor light keeps hue under the soft ceiling and reads forge-orange (`FoundryRegionFinish.cs`); bridge margins calmed (`FoundryRegionView.cs`). Built, not installed (see 23). Proof `ArtDirection/2026-09-30/foundry-fx-light-01/`. New prompt pack `ArtDirection/2026-09-30/painted-surfaces-02/` supersedes painted-surfaces-01: ranked P1 court flagstones, P2 balustrade trim, P3 foundation masonry, P4 pit lining, P5 boiler drum wrap, all power-of-two; renderer-budget options for the owner. Nothing generated yet.

Installed 2026-09-30 evening: APK DEFA1712... (effect depth, forge-orange light, calmer bridge) is on the S23 (see 23).

Seen on S23 2026-09-30 (depth-proof flag): enemy ember and loot gold silhouettes behind the south-west boiler match Unity; render stats show ~3 ms GPU at 60 fps (see 23).


## 2026-09-30 - Foundry stone balustrade; review fixes; depth proof with effects

No raster art generated. Integrated: the slit parapet became a stone balustrade (plinth, turned balusters, handrail; `FoundryRegionMassing.cs`), possibly removing the need for painted P2; review fixes in `FoundryVisibilityView.cs` (frame-stamped re-apply for hand-rendered cameras, newest-first restores, null material guard, accurate header); the development depth proof now produces hidden and visible hit sparks (`DepthProofArrangement.cs`). Installed (see 23), not yet looked at on the phone. Proof `ArtDirection/2026-09-30/foundry-rail-01/`.


## 2026-09-30 - Foundry reference pass: calibrated floor light, lit decals, abyss chains, bridge head

No raster art generated. Integrated: floor light calibrated against measurements of the reference (violet-grey open court, tighter rose/orange pools; `FoundryRegionFinish.cs`); flush floor decals lit by the floor light at emit, compass lozenges on brass (`FoundryRegionView.cs`, `FoundryRegionHeraldry.cs`); six new heavy chains into the abyss, bridge-head braziers and banners cut from the gate facade, finial paint order fixed (`FoundryRegionMassing.cs`, `FoundryRegionDressing.cs`, `FoundryRegionSite.cs`). Installed and seen on S23 (see 23). Proof `ArtDirection/2026-09-30/foundry-reference-pass-01/`.


## 2026-10-01 - Foundry night pass: ashlar court, rounded pit collar, rim clutter, decal order

No raster art generated. Integrated: mixed-size ashlar court paving (`FoundryRegionFinish.cs`), a rounded furnace-pit collar (`FoundryRegionMassing.cs`), rim clutter and gate fire braziers (`FoundryRegionClutter.cs`), floor decals sorted under raised structures (`FoundryRegionView.cs`), chain bronze under the colour ceiling, and a vertex-colour range test. Built, installed and seen on S23 (see 23). Proof `ArtDirection/2026-10-01/foundry-night-01/`.


## 2026-10-01 - Foundry day pass: molten pit floor, bridge lanterns, bridge west rail

No raster art generated. Integrated: a molten pool and warm lining gradient in the furnace pit and a softened collar corner (`FoundryRegionMassing.cs`); lantern posts on the bridge rails with light pools (`FoundryRegionSite.cs`, `FoundryRegionFinish.cs`); the bridge west rail and kerbs end at the court (`FoundryRegionView.cs`, `FoundryRegionSite.cs`). Installed on S23, not yet viewed there (see 23). Proof `ArtDirection/2026-10-01/foundry-day-01/`.

## 2026-10-04 - Pending foundry work completed

Finished the inherited pit atmosphere (20 rising sparks, heat wisps and glow), gate-brazier support checks, bridge coping/kerb corrections, arch-before-pier ordering, screened anchor plates and framed molten discharge. Extended the west bridge paving margin to d=-4 for s=-32..-24, covering the bare joint bed after the kerb ends. No gameplay, movement bounds, save or balance changes. Proof: ArtDirection/2026-10-04/foundry-completion-01 (actual Unity before/after).

Graphics capture1/1; .NET390/390; compile0warnings/errors; EditMode410/410; PlayMode134/134; Android exit0. Static17renderers/73,244vertices; atmosphere832vertices. Integrity526/1099/568 checked while gate was in progress. APK37,175,012bytes SHA25623BB5C7B4C2A51BA12A6609193B6D358E867270DD278A755486CFA2858ED0058. Built, NOT installed; latest known installed remains F6776526... from October1. Fresh read-only phone check found launcher foreground and game not running. No device inputs. New painted masters are being prepared separately; this package contains no texture changes. Commit locally only, no push.
## 2026-10-04 - Jointless painted court (P1-b)

Integrated generated FoundryPavingV3 on the existing paving material; geometric ashlar joints, bevels and calibrated .8 tint retained. Raw master and prompt preserved in ArtDirection/2026-10-04/painted-surfaces-02-masters. Matched proof: ArtDirection/2026-10-04/painted-paving-01. Graphics 1/1, EditMode 410/410, PlayMode 134/134, Android exit 0; integrity 526/1099/568. Static budget unchanged: 17 renderers, 73,244 vertices. Import uses POT rescale, 1024 max, mipmaps, Repeat and Android ASTC6x6; Android packed-assets report lists FoundryPavingV3 at 611.7 KB (compressed mip-chain footprint, not raw RGBA). APK 37,175,012 bytes, SHA256 C68DA0594EAE850A99CA45F3D6EA71BA2F2FA37EEB4AC6DE48774C27FD74C2B3. Built, not installed. No gameplay changes or push.
## 2026-10-04 - Painted foundation masonry (P3)

Integrated FoundryMasonryV2, preserving geometry and the original asset/GUID. Recalibrated the sampled masonry texel mean used by clutter so wood/cloth tones remain correct. Matched Unity proof: ArtDirection/2026-10-04/painted-masonry-01. Graphics 1/1; compile 0 warnings/errors; EditMode 410/410; PlayMode 134/134; Android exit 0; integrity 527/1099/568. Static 17 renderers / 73,244 vertices. Android build log now explicitly reports actual target textures: both P1-b and P3 are 1024x1024 ASTC_6x6, 11 mips. APK 32,374,835 bytes SHA256 7C72EA9421300E14655AAB8FE6EB9D8CB4D42818521C9CA497812127CA57F423. Built, not installed. USB phone no longer appears in adb devices; owner was asked to reconnect while desktop work continues. No push.
## 2026-10-04 - Dedicated painted pit lining (P4)

Integrated FoundryPitLining with its own shared material and preserved 17-renderer batching. Adapted UV/heat to the current molten pool rather than the obsolete pre-pool prompt-pack layout. Repeated facade windows removed; molten pool/discharge and actor depth preserved. Proof ArtDirection/2026-10-04/painted-pit-01. Graphics 1/1; compile 0 warnings/errors; integrity 529/1099/568; EditMode 410/410; PlayMode 134/134; Android exit 0. New shared-surface checks cover pit independence and POT dimensions. Static 17 / 73,244 vertices. Android actual 256x512 ASTC_6x6, 10 mips; packed texture 78.0 KB. APK 32,374,835 bytes SHA256 F3512B910F9AA85B14AAA2F769B74E3063AD99D017DCA82D452B4668BB10CF7E. Built, not installed (ADB device list empty). No push.
## 2026-10-05 - Painted boiler wrap (P5) and combined package complete

Integrated FoundryBoilerWrap on the existing court boiler slot; dedicated pit material unchanged. Matched actual Unity proof ArtDirection/2026-10-04/painted-boiler-01; combined reference/before/after page ArtDirection/2026-10-04/painted-foundry-01. Session started October 4; final gate completed after midnight October 5. Graphics 1/1; EditMode 410/410; PlayMode 134/134; Android exit 0; integrity 531/1099/568. Static budget remains 17 renderers / 73,244 vertices. Android actual formats: paving and masonry 1024x1024 ASTC_6x6 (11 mips), pit 256x512 ASTC_6x6 (10), boiler 512x512 ASTC_6x6 (10). Packed boiler 154.8 KB, pit 78.0 KB. Final APK 32,374,745 bytes SHA256 B6B4BFD0DCA089A0FF514767BC94E356CA58AEE68A1B9649A60F17C9810BE551.

Generated, integrated, captured and built; NOT installed at this record. Phone reappeared after disconnecting; another app is foreground and Cryptforge is not running. Asked owner whether phone is free before launching/testing. Read-only backup TestResults/painted-final-backup-20261005 includes profile, profile backup, development and telemetry; profile revision 221. No device inputs or flags changed. No push. All four raw masters and exact prompts/hashes preserved in painted-surfaces-02-masters; original imported assets/GUIDs retained. The target's full lighting/volume/detail fidelity is still future work; P2 deliberately skipped without adding a renderer.
## 2026-10-05 - Architectural weathering package verified and installed

Combined work: static vertical masonry deposits, localized ledge shade, height-aware fire spill on stone faces/caps, soot around iron roof/vent centres, and variable rubbed paving bevels. Existing UVs and depth ownership retained; no gameplay, collision, movement bounds, profile logic, telemetry logic or energy changes. A small vertical subdivision resolves the shallow ledge shadow: static budget 17 renderers / 76,420 vertices (+3,176, 4.3%). All treatment runs at map construction, with no new material/texture or per-frame update.

Proof: ArtDirection/2026-10-05/foundry-weathering-01 (matched Unity before/after, matched device boiler views, device walk and raw RenderStats lines). Compile 0 warnings/errors; graphics capture 1/1; EditMode 410/410; PlayMode 135/135 including the new height-falloff/ledge check; Android exit0; integrity532/1099/568; diff check clean. APK32,374,745 bytes SHA256 AFEEE1AF9D8065F240DD15C5AAF4BD96EA51D3753320208E3001B57FF8763E40.

Installed on S23 after owner confirmed it was free; device base.apk hash equals that APK. Fresh backup TestResults/weathering-install-20261005. Profile and profile.json.bak hashes match before install, after install and after the test (revision221); no older backup restored. Guarded before/after and 3.5-second leftward drag confirm rendered surfaces, camera following and the existing enemy/loot x-ray. Stable matched depth-proof view: previous59.9fps/GPU3.7ms/57draws/62.0ktriangles/127.3MB; new59.9fps/GPU3.7-3.8ms/57draws/63.8ktriangles/~127.6MB. This is an S23 static depth-review arrangement, not a combat-load or midrange-device benchmark. Current-process Unity exception/shader-error matches0.

App stopped after review. Both temporary depth-proof.txt and render-stats.txt removed. Remaining development files match the pre-test filenames and hashes exactly (region-preview.txt and start-floor.txt.off). Ordinary foundry preview is ready on next launch. Generated, integrated, built, installed and seen on device are now all complete for the painted package and this weathering pass. Local commit only; no push.
## 2026-10-05 - Gate and site stone finish

LitFoundryTier now uses ArchitectureLight with the actual authored lower/upper elevation instead of hardcoded obsolete boiler coordinates and projected-Y height inference. Bridge piers/lantern supports and court finials use the same treatment. Four gate header straps with clean iron patch UVs and inset front mullions reuse the existing gate owner and iron batch. No new material, texture or gameplay changes. Matched actual Unity captures: ArtDirection/2026-10-05/foundry-gate-finish-01. Static budget 17 renderers / 76,492 vertices (+72); full validation/device result recorded in that proof and docs22/23.
## 2026-10-05 - Painted masonry V3

Built-in image_gen edit adds slate/charcoal/warm-grey block variation, mineral facets, restrained cracks and runoff to the existing five-course masonry. Untouched raw image and exact prompt: ArtDirection/2026-10-05/masonry-variety-01/P3b-raw.png and P3b-prompt.txt; master SHA25640155D49CBD7A535826177423E2798EE461BE1126BD414010F42D979683658B6. New sibling FoundryMasonryV3.png (GUID12911d8c992245547b22475af21020a4) replaces the texture reference in the existing masonry material; V2 stays intact. Import-PaintedSurface supports P3b, Android1024x1024 ASTC6x6/11mips. Clutter's existing masonry UV patch mean is recalibrated for V3, preserving its intended wood/burlap colour. No geometry/material-count changes:17 renderers/76,492vertices. Actual matched Unity views and full test/device results are recorded in the proof README and docs22/23.
## 2026-10-05 - Forged iron V2

P6 built-in image_gen edit/refinement replaces fractured stone-like iron with a calmer forged steel field, retaining the existing border/rivet/corner atlas layout. Exact prompts and untouched master: ArtDirection/2026-10-05/forged-metal-01. Master SHA25601106A441563FBFF4C04318DD2906E46651E41AAAA2C71CA5E1D7068797626DD. FoundryIronV2 is a sibling; original retained. Existing material reference updated, P6 import stage added, Android1024ASTC6x6/11mips. Iron clutter patch mean recalibrated without UV movement. No new geometry/material/batch:17 renderers/76,492vertices. Verification and device status in proof README and docs22/23.
## 2026-10-05 - Workshop props combined pass

Existing crate, barrel and sack clusters now have board variation/diagonal braces, stave joins/hoop lips/lid details, and gathered folds/tie bands. Code-native geometry in FoundryRegionClutter only; no new textures/materials or gameplay changes. Existing footprints and depth ownership retained. Static budget:17 renderers,78,232 vertices (+1,740 from76,492). Actual Unity before/after proof:ArtDirection/2026-10-05/workshop-props-01. Compile0/0,graphics1/1,Edit410/410,Play135/135,integrity534/1099/568. Device review pending unlock; do not reuse the prior package's FPS as a measurement of this pass.
## 2026-10-05 - Paving contrast integrated and installed

Court/apron joint-bed tone .50->.66, lit bevel1.10->1.055 and shadow bevel.74->.86 soften the dark grid seen during the workshop device review. Seal inset/setts keep their existing contrast. Colour-only change in FoundryRegionFinish: same geometry, UVs, light sampling, materials, gameplay and walking union. Static17/78,232 unchanged. Proof ArtDirection/2026-10-05/paving-softness-01 contains matched Unity views and guarded phone capture/walk.

Compile0/0,graphics1/1,Edit410/410,Play135/135,Android0,integrity534/1099/568. APK32,374,745 bytes SHA2562AE2ACAC31F1D126CCE099F176922C24444575FDCBD851FF6D428239A9F165AC installed and base.apk hash matched. Matched S23 static59.9fps/GPU3.8ms/57draws/65.6ktriangles. No current-process Unity exceptions/shader errors. Not a combat or midrange benchmark. The capture includes the depth-proof actors' periodic white hit flash; this is not a texture change.

Fresh backup TestResults/paving-device-20261005. Profile/bak unchanged by hash; original development files unchanged, test flags removed, app stopped, normal preview ready next launch. No push. This corrects floor emphasis; it does not close the broader reference-image fidelity gap.
## 2026-10-05 - Three-agent foundry identity package installed

Three explicitly requested agents edited disjoint files: gate tympanum/bevel frame/keystone (FoundryRegionArchitecture), boxed crane jib/sheave/coupling/weight seams/basket brace (FoundryRegionDressing), and worked anvil face/distinct tongs-hammer/tank ribs (FoundryRegionWorkshop). Parent integrated, compiled, captured, ran the full gate and device review. Static geometry/material batches retained:17 renderers,78,820 vertices (+588 from78,232). No gameplay/collision/camera/scene/new asset changes.

Proof ArtDirection/2026-10-05/foundry-identity-01 pairs actual Unity overview/gate/crane/workshop/pipe-shelf views plus phone start and gate approach. Owning agents reviewed captures read-only; no blocking visual regression. Anvil horn remains darker than the working face; fine details are subtle at overview scale. Existing portrait HUD partly hides the high gate pediment at close approach: full gate shape was checked in Unity, not falsely claimed fully visible on the phone. Crane/workshop closeups are Unity evidence, not device closeups.

Compile0/0,graphics1/1,Edit410/410,Play135/135,Android0,integrity534/1099/568. APK32,374,745 bytes SHA25646D65F0CA8A2007F87B26B749BE8EFF9014EF141121CE84E46CE3E7DD9BB077F installed and base.apk hash matched. Matched S23 static59.7-59.9fps/GPU3.8ms/57draws/66.2ktriangles. One observed sample reached33.4ms max; do not claim zero hitches. No current-process Unity exception/shader-error matches. Not a combat/midrange benchmark.

Fresh backup TestResults/identity-device-20261005. Guarded four-drag gate route recorded in proof/device-route.txt; timing yields slight position differences. Profile/bak and original two development files unchanged by hash; temporary flags removed, app stopped. Normal preview ready next launch. No push.
## 2026-10-05 - Three-agent structural package 02 installed

Three agents in disjoint files refined bridge support courses (Site), undercroft arch/pier face shading (Undercroft), and active pit hood collars/neck bands/straps (Massing). Dead legacy BuildShaft was identified and left untouched; pit work was reassigned to the actual BuildFurnacePit path. Parent inspected captures and ran integration. Static17renderers/80,316vertices (+1,496,+1.90%). Existing materials/depth ownership/walking boundaries preserved; no new assets, scene, gameplay or camera changes.

Actual matched Unity and phone evidence: ArtDirection/2026-10-05/foundry-structure-02. Bridge/undercroft reviewed in Unity closeups and overview; pit also inspected after a guarded two-drag approach on S23. Fine changes are subtle at overview scale and parts of the undercroft remain obscured by existing bastions. Phone route timing yields slight framing differences; this is not an exact pixel-aligned comparison or a reference-fidelity claim.

Compile0/0,graphics1/1,Edit410/410,Play135/135,Android0,integrity534/1099/568. APK32,374,745bytes SHA2567DA6010C1C646642EE5B12F22EDD7347D68CF6360CC32FBEEA00E9BA7F4EAEEC installed and base.apk hash matched. S23 static59.9fps/GPU3.8ms/57draws/67.2ktriangles; captured settled samples max16.9ms. No current-process Unity error matches. Not a combat/midrange benchmark.

Owner explicitly approved restarting their Forge run (2HP,40unbanked gold). Fresh backup TestResults/structure-device-20261005 taken first. Profile/bak and original development files unchanged by hash after validation; test flags removed, app stopped. Persistent progress retained; the in-flight run was restarted as authorised. No push.
## 2026-10-06 - Five-agent visual closure, desktop complete

Proof: ArtDirection/2026-10-06/visual-closure-01. Five requested agents covered anvil material continuity, enclosed amber lantern glass, foundry-only backdrop tint, four-point portrait gate capture, and remaining-art review. Parent combined and inspected final Unity captures. The first lantern trial was too yellow and was corrected before the final capture and full gate. No new textures/materials/geometry: static17 renderers/80,316 vertices unchanged. Existing cage/flames, depth owners, scene, camera, HUD, movement and gameplay unchanged.

Before/after overview, bridge, workshop, gate and furnace are preserved. Portrait capture records coordinates, current9-unit width and enabled HUD; staged positions are not movement/combat evidence. Centred gate view reveals door/seal and both banners, but top architecture remains HUD-obscured/cropped. Do not claim concept fidelity is achieved. Further broad material cohesion and deliberate gate framing offer more value than indiscriminate small hardware.

Final graphics1passed,Edit410/410,Play135/135,Android0;integrity534/1099/568. APK32,374,745bytes SHA25674297A6B721A4251F7C0E07E06AD3F8E1CD03848B984DD54955CDD7F4680B978. Phone unavailable: no ADB, installation, profile changes or device measurements. No push.


## 2026-10-06 - Surface composition desktop closure

Proof ArtDirection/2026-10-06/surface-composition-01, baseline7e2332b. Existing agents continued disjoint tasks: stock soot now matches both current workshop shelves; architectural deposit field is broader/shallow (.31frequency,.905..1 range, same midpoint.9525); test-only gate framing compares production,+1.5Y,width11 at one pose. Parent inspected actual comparisons and accepted the surface changes as subtle corrections, not a large fidelity leap. Geometry/materials/renderers/depth/gameplay unchanged; static17/80,316.

Gate alternatives both retain upper HUD obstruction. Upward shifts hero180px and reduces nominal below-hero band23.2%; width11 makes world actors18.2%smaller. Production combat camera retained. Future full reveal requires separate arrival-presentation or HUD design, with play validation. Existing lane/boiler wear already existed: no duplicate system was added.

Compile0/0,graphics1,Edit410,Play135,Android0,integrity534/1099/568. APK B2D5A6F5000A1A7EF4099E595E19DA35BB1BF33921F65557ED9F722937B2E42B,32,374,745bytes, includes visual-closure changes. No phone access or installation; no current device performance claim. No push.


## Device closure - surface composition (device logs 2026-10-07)

APK B2D5A6F5000A1A7EF4099E595E19DA35BB1BF33921F65557ED9F722937B2E42B installed and base.apk hash matched after fresh backup TestResults/surface-device-20261007. Supersedes pending device status. Guarded foundry gate walk and normal Staff room2, pickups, upgrade choice and enemy-contact captures reviewed; proof ArtDirection/2026-10-06/surface-composition-01/device. No new code/rebuild. Gate top/HUD crop remains; no new visual blocker in these samples.

S23 settled depth59.9fps/GPU3.8ms, initial226.5ms max; normal active samples59.4–59.9fps/GPU4.0–4.4ms, initial57.3ms max. Paused-choice samples excluded. Short smoke review, not dense-wave/midrange benchmark. No current-process error matches. ADB server interrupted before input; focus guard aborted then recovered. Profile/bak and original development files SHA256-identical; temporary depth/render flags removed, app stopped. Telemetry test events retained, no backup restore. Latest cumulative build ready for normal launch. No push.


## 2026-10-07 - Gate arrival art study

Proof ArtDirection/2026-10-07/gate-arrival-01: actual Unity combat/transition/full-gate/restored portrait captures and interactive crossfade timing page with skip/Escape/background reset. Graphics1passed; camera/follow/HUD restoration assertions passed; combat/restored PNG hashes identical. Browser control logic harness passed and preview HTTP200 verified. Full reveal uses centre(19,16.4),width17.6 and temporarily hidden HUD in the capture only.

Not a shipped Unity animation: current gate has no safe pre-encounter arrival state. README records future trigger/skip/input/interruptions requirements. No runtime camera or scene edit, APK rebuild or phone mutation; installed B2D5A6F5 remains current. This completes composition study, not gameplay integration. No push.


## 2026-10-07 - Gate arrival runtime preview and device closure

Implemented opt-in development foundry entry vista (non-empty development/gate-arrival.txt plus foundry region preview, depth-proof wins). EncounterController defers the first wave; GateArrivalView presents the full gate for about 4 unscaled seconds, consumes skip until release, restores camera/HUD/input, then starts once. Cancellation/re-enable, pre-Start interruption, scene unload and pause ownership are covered. Telemetry active time excludes the deferred encounter. No proximity boss trigger, scene/art geometry, save schema or balance change; static art remains 17 renderers / 80,316 vertices.

Compile 0 warnings/errors; final EditMode 410/410, PlayMode 143/143, Android exit 0. APK 520E96960DDE255A99D13D32E668E9EE5BB9F6D0EEEAF885367C1CE672424EDD (31,113,605 bytes) installed; base.apk matched. Proof: ArtDirection/2026-10-07/gate-arrival-runtime-01. Device natural completion and ability-position tap skip reviewed, ability remained ready. Natural telemetry first room at 4.183s, first kill at 1.118 active seconds. Short skip session was stopped before flush; do not claim persisted skip telemetry verification.

Fresh TestResults/arrival-device-20261007 backup; profile/bak and both original development files hash-identical. New arrival preview flag intentionally remains enabled for next owner launch. Awake setting restored to original 2, app stopped, no depth/render diagnostics. Android AHardwareBuffer messages present; no managed exception/crash found in sampled process logs. No new performance benchmark. Next work should evaluate arrival pacing in play and broad visual fidelity; avoid treating more tiny trim as major progress. No push.


## 2026-10-07 - Yard surface hierarchy installed

Proof ArtDirection/2026-10-07/yard-surface-01. FoundryRegionFinish only: broader workshop apron deposits, soft boiler service grime, stronger existing gate-lane wear; original pit deposit preserved. Small material-value refinement, not a map redesign or concept-fidelity completion. Same textures/materials and 17 renderers; adaptive light mesh 80,344 vertices (+28). Same-camera before/after captures reviewed; central court remains substantially unchanged.

Graphics1, Edit410, Play143, Android0, integrity536/1099/568. Installed APK CAC7489FB29F0AF91781D85731B3BC154B31236154B2DF3D01E8B8C85E2B3170 (35,934,327 bytes); base.apk hash matched. Guarded S23 staged boiler/gate walk reviewed, no current-process managed exception/crash match. Not a fresh combat/performance benchmark. Fresh backup TestResults/yard-device-20261007; profile/bak and all original development files hash-identical. Temporary depth-proof removed, previous arrival opt-in retained, app stopped, no push.

Further minor floor-value tuning has diminishing benefit. Next substantive art slice should target a clearly visible asset/material limitation with an explicit before/after reference, or dynamic combat readability; do not advertise another small contrast adjustment as a major map upgrade.


## 2026-10-07 - Masonry V4 cohesion installed

New built-in image_gen edit of original FoundryMasonryV3: V4 removes repeated large diagonal fracture facets while preserving the five-course bond and dark grey-violet value. MAT_FoundryMasonry points at the new versioned texture; V3/GUID untouched. Full prompt/provenance and Unity gate/portrait/bridge/pit/overview comparisons: ArtDirection/2026-10-07/masonry-cohesion-01. Source1254square, Unity POT scaling, Android max1024 ASTC6x6, mipmapped/trilinear/repeat/non-readable. No geometry, lighting, camera or gameplay edits; static17/80,344 unchanged. Improvement is surface coherence, not concept completion.

Graphics1, Edit410, Play143, Android0, integrity537/1099/568. Installed APK59FCABA6D557886D68D2E0E1230CB2D8C1076F2C3FC776A48F18E67D203C0A2E (35,934,327 bytes); base.apk matched. Guarded S23 full gate reveal and natural Staff combat handback inspected. No managed exception/crash pattern in sampled process log; no new performance benchmark. Fresh backup TestResults/masonry-device-20261007; profile/bak and original development files SHA256-identical. No test flags added, arrival opt-in retained, app stopped. No push.


## 2026-10-07 - Open brazier flame readability installed

FoundryRegionDressing: open beacons/braziers now sample the existing smooth hot atlas patch rather than a wider coal-crusted seam. Layered gold/orange vertex tones and less opaque cooler tips remove black wedges from the flame interior. Coal mounds, boiler-window and undercroft mapping untouched. No new texture/material/geometry/update logic; static17/80,344. Static angular silhouettes remain; this is not a flame-animation package. Proof ArtDirection/2026-10-07/brazier-fire-01 (same-camera gate/bridge/portrait/overview and actual device).

Graphics1, Edit410, Play143, Android0, integrity537/1099/568. APK350870192B69AE45BB80E3AC8F1518C8E1A7F9233A2759DC50DED3A48295724C,31,113,605bytes installed; base.apk matched. Guarded S23 gate reveal and normal combat handback captured; no managed exception/crash pattern in sampled process log. No new FPS benchmark. Fresh TestResults/fire-device-20261007 backup; profile/bak and development files hash-identical. No test flags added, existing arrival opt-in retained, app stopped. Comment-only corrections followed build. No push.

## 2026-10-07 - Open flame motion installed

FoundryFlameMotion animates tagged upper flame corners in the existing emission batch at up to 30Hz, using scaled time and cached vertex arrays. Bases, untagged vertices and depth stay fixed; pause freezes, disable restores, re-enable resumes. Fixed bounds padding covers authored displacement. Region owns the mesh. No added materials/renderers/geometry; 17 renderers /80,344 vertices retained, one existing batch now dynamic. Full batch vertex upload is an explicit cost, not shader-only motion.

Proof: ArtDirection/2026-10-07/flame-motion-01 (six actual Unity poses plus guarded S23 entry still). Graphics check asserts exactly one animated batch, actual motion and pause freeze. Compile 0 warnings/errors; EditMode410/410, PlayMode144/144, Android exit0; integrity539/1099/568. APK8C0B5FA725943CDE6BF86039EF73E9A7BC7FD23862A3991EFA0EC839CAC0AB5B,31,113,605bytes installed and base.apk hash matched.

Fresh TestResults/flame-motion-device-20261007 backup; profile/bak and all three original development files SHA256-identical after review. Temporary depth-proof/render-stats flags removed, original arrival opt-in retained, app stopped. Sampled process log had no managed exception/crash pattern. Short staged S23 gate sample before recording:59.7-59.9fps, p50/p95 16.7ms, one window maximum33.4ms; GPU about4.2ms. This is not a dense-combat or midrange-device benchmark. Entry still confirms on-device flame appearance; real-map automated captures verify motion. Local first device video crops the open flames and is not promoted as visual motion evidence. No push.

## 2026-10-07 - Bridge banner motion installed

Two bridge-head banners now use six rows per texture column for gentle cloth curvature. FoundryBannerMotion tags cloth through the existing facade batch, keeps rods/untagged faces/structure Z fixed and uses identical drop/phase at duplicated seams. Scaled time freezes on pause; disable restores, re-enable resumes. Cached arrays upload the existing facade vertex buffer at up to30Hz. Max sway0.115 /vertical0.025, padded bounds0.15 per side. Region owns mesh. No new texture/material/renderer:17 renderers /80,504vertices (+160). Two existing region batches now dynamic (flame emission and cloth facade); gate-painted banners remain static. This is local entrance motion, not broad reference-fidelity completion.

Proof ArtDirection/2026-10-07/banner-motion-01: rest +8 actual Unity frames, two guarded S23 bridge poses. Compile0warnings/errors; graphics1passed (one actual cloth batch, motion and pause assertions); Edit410/410, Play145/145, Android0; integrity541/1099/568. Installed APK376E38C2E1F9F53B06F2DB00DB6B1EAA3ACFF43420F717B7C44BECD05755D8A8,31,113,605bytes; base.apk SHA256 matched. Short staged S23 bridge windows t92-107:59.9fps, p50/p95 16.7ms, max17.1ms, GPU3.6ms. Not dense combat, midrange testing or an isolated before/after cost measurement. No managed exception/crash pattern in sampled process log.

Fresh TestResults/banner-device-20261007 backup; profile/bak and three original development files SHA256-identical. Temporary depth-proof/render-stats removed, owner arrival opt-in retained, app stopped. No push. Further work should address broad silhouette/material/readability limits rather than presenting small ambient motion as reaching the concept image.


## 2026-10-07 - Basalt paving V4 ready; device install pending

Built-in image_gen edited FoundryPavingV3 into versioned Assets/_Project/Art/Props/Foundry/FoundryPavingV4.png. The new continuous stone surface reduces directional diagonal streaks and adds irregular wear/pitting. Source1254square RGB; mean87.76/80.85/84.14 versus V3 90.22/83.65/88.06. Slightly darker, not brightness-identical. MAT_FoundryPaving references V4, all other settings unchanged; V3/GUID preserved. No raster postprocessing, new geometry, layout, lighting, camera or gameplay change. Same17 region renderers /80,504vertices. Actual Android import1024square ASTC6x6,11mips. Real Unity before/after portrait/overview/pit/gate/bridge: ArtDirection/2026-10-07/basalt-surface-01, prompt.txt records built-in provenance. Accepted as a surface refinement, not a major reference-fidelity milestone.

Graphics1passed. First full gate Edit410pass, Play144/145: ExitSealPresentationTests compared pre-LateUpdate view state to post-Update encounter state;0.02053 difference narrowly exceeded0.02. Fixed test synchronization by freezing the encounter then yielding through LateUpdate. Tightened value comparison to0.00001 and pause comparison to exact equality; no runtime behavior change. Targeted3/3passed, final full Edit410/410 and Play145/145 passed. Initial failing XML/log retained in TestResults/basalt-initial-playmode.*. Integrity542/1099/568; Android exit0. Ready APK DD3E70BCF9E6835632E1A81D0DF0301D2B9D54087DEE1875E61A8780169D4F38,31,113,605bytes.

NOT installed or reviewed on device. ADB showed the app had reopened (PID2763) after the previous package was stopped. Asked whether the active run can be restarted; no answer received by closure. Do not kill it or install until this is resolved. This turn performed read-only device checks; no flags, profiles, backups or app lifecycle changes. Last package installed/verified by this assistant remains banner build376E38C2. No new FPS claim. On approval: fresh backup, guarded install, texture/gameplay visual review, diagnostic cleanup and persistent-file verification. No push.

Additional original concept, not in APK: ArtDirection/2026-10-07/forge-keeper-proof-01/forge-keeper-source.png (1024x1536 RGBA, transparent exterior, alpha0..254). Built-in image_gen; full prompt.txt, no raster edits. Browser size/background study and README explain the frontal-view/projection, paired-lighting, depth/cutaway and renderer-budget questions. Candidate for the missing large bridge guardian silhouettes; no statue implementation or final-art approval implied. Next substantial visual study should test that silhouette in the actual camera, instead of more minor floor tuning.


## 2026-10-07 - Basalt V4 device closure

The owner's continuation resolved the restart/install question. Fresh backup TestResults/basalt-device-20261007-182726, then installed ready APK DD3E70BCF9E6835632E1A81D0DF0301D2B9D54087DEE1875E61A8780169D4F38; base.apk SHA256 matched. Guarded S23 gate-entry and natural Staff combat screenshots reviewed. Hero, ember enemies and cyan XP remain legible against the more irregular paving. Proof includes device-entry.png and device-combat.png in ArtDirection/2026-10-07/basalt-surface-01. No new code/build/test run; uses the already-passed final410Edit/145Play gate. No managed exception/crash pattern in sampled process log. Short visual smoke check, not a new FPS or dense-wave benchmark.

Profile, profile backup and all three original development files SHA256-identical after review. No diagnostic flags added; existing region/arrival settings retained, telemetry test events retained, app stopped. No backup restoration or push. Supersedes the preceding pending-install status. Forge-keeper concept remains outside Assets and the APK; its actual-camera placement and depth/budget proof are the next substantive art task.


## 2026-10-07 - Bridge guardian package ready; device review blocked by USB

Integrated original ForgeKeeper.png (unchanged built-in image_gen source from ArtDirection/2026-10-07/forge-keeper-proof-01) as two identically oriented architecture cards. One shared optional material in PlatformMaterialSet. Replaced bridge-head fire baskets with carved keepers on expanded caps and below-deck consoles, moved the supports outside the complete walking union, shifted animated banners/brackets outboard, and removed the replaced lamps from floor illumination. Existing rail lanterns and bridge-foot beacons retained. Explicit structure footprints support actor depth and local hero cutaway; no gameplay, collision, camera, balance or save change.

Proof ArtDirection/2026-10-07/guardian-runtime-01: actual Unity bridge/overview, production portrait approaches and left-court behind-statue overlap. The right statue has no equivalent nearby walkable rear position; do not claim a symmetric device occlusion test. Independent image review found no alpha fringe, floor-covering console or join blocker. Supports remain outside the walking union; transparent card bounds remain outside bridge width. Two cards share exactly one new renderer; other region batches retain17 limit. Total18renderers /79,832vertices (previous17 /80,504); two pre-existing flame/cloth batches remain dynamic. Guardian adds no update loop.

Verified: .NET390/390, compile0warnings/errors, final graphics1passed, EditMode410/410, PlayMode145/145, Android exit0, integrity544asset/folderGUIDs /1099packageGUIDs /568sceneobjects. Expanded tests verify one8-vertex/12-index keeper batch, clamp/non-readable/POT texture, depth/fade flags, unchanged texture orientation, footprint clearance and scene-owned resource cleanup. Initial new portrait capture failed because its staged hero point was too near the rim; corrected capture placement to respect HeroMotion.EdgeMargin, no gameplay change. Android import512x1024 ASTC6x6,11mips.

Ready APK SHA25693380BACB4BFAE6BB17BFCF1FB1814E153E9524DEFAA8071F0C83959E5C49470 (31,113,605bytes on disk; Unity BuildReport totalSize is a different aggregate). NOT INSTALLED. Phone was available at first; read-only fresh backup TestResults/guardian-device-20261007-205856 succeeded with app stopped. By build completion ADB had no devices and Windows no Samsung/Android device entry; repeated discovery remained empty. Asked owner to reconnect. No install, game launch, phone input, diagnostic flag or save modification this turn. Last verified installed build remains basalt DD3E70BCF9E6835632E1A81D0DF0301D2B9D54087DEE1875E61A8780169D4F38. No new device performance claim.

Next on reconnection: inspect fresh state and refresh backup if changed; install the ready APK and compare installed hash; guarded staged bridge walk then ordinary launch/combat; remove temporary depth/render flags and compare profile/bak/original development-file hashes. Preserve original region/arrival settings. Do not rebuild unchanged content or call this package device-verified yet. No push.


## 2026-10-07 - Guardian package installed and reviewed

USB returned on owner continuation. Fresh backup TestResults/guardian-device-20261007-212948. Installed ready APK93380BACB4BFAE6BB17BFCF1FB1814E153E9524DEFAA8071F0C83959E5C49470, base.apk hash matched. First guarded launch stopped because NotificationShade remained foreground; temporary flags cleaned in finally. Collapsed the panel without screen capture/input to other apps, relaunched game and verified normal Staff combat. Then staged guarded bridge walk verified keeper, stone console and relocated banner on S23. Short bridge windows t32-52:59.9fps,p50/p95=16.7ms,max16.8-16.9ms,GPU3.7ms; not dense combat or a midrange benchmark. No managed exception/crash pattern in sampled staged-process log.

Profile,bak and all3 original development files SHA256-identical after review. Temporary depth-proof/render-stats removed; original arrival/region settings retained. App stopped. Proof device-bridge.png and device-normal.png in ArtDirection/2026-10-07/guardian-runtime-01. Supersedes pending USB/install status. No additional build for this installation; prior full410Edit/145Play verified package used. No push.


## 2026-10-07 - Crane structure package installed

Code-native crane silhouette revision in FoundryRegionDressing.cs and crane-only FoundryRegionSite.cs: broader cast mast/jib members with plain iron sampling, sparse alternating braces and end joints, stronger hoist chain, larger open scrap basket/solid lower tray, filled mast bearing and winch hubs, plain drum with sparse winding bands. Existing anchors, footing, generic wheels and gameplay/collision/camera remain unchanged. No new texture/material/renderer/update loop. Region18renderers /79,288vertices, down544 from guardian baseline79,832; existing flame/banner batches remain dynamic.

Actual same-camera Unity before/after and production portrait: ArtDirection/2026-10-07/crane-structure-01. Portrait stages a reachable court point with HeroMotion.EdgeMargin and demonstrates local cutaway; restores hero position before square closeup so the drum remains visible. At the close production camera the jib extends behind HUD and the outboard basket approaches the left edge; no camera change was made to fit the complete crane. This improves local structure/readability, not full painted-reference fidelity.

Verified: .NET390/390, compile0warnings/errors, final graphics1passed including map traversal, EditMode410/410, PlayMode145/145, Android exit0, integrity544/1099/568. No new tests mirroring visual constants; existing region depth/ownership/budget guards pass. Installed S23 APK24BCDD64BD851636A0DCB4948ADAC7FCBACE2D19753A32A04497B3F56EC4E4AD (35,958,239bytes); base.apk SHA256 matched.

Fresh TestResults/crane-device-20261007-215052 backup. Guarded staged route reached bridge, then pit corner and west court beside crane; changed routing to go around the shaft, no off-map teleport/camera change. Phone court/edge captures show main members, bearing, drum and partial outboard basket; full basket verified in square Unity capture. Final staged windows t332-347:59.9fps,p50/p95=16.7ms,max16.7-16.9ms,GPU2.3-2.4ms. Not dense combat, midrange hardware or an isolated performance comparison. Sampled staged/normal process logs had no managed exception/crash pattern. After removing depth-proof/render-stats, normal Staff battle reopened and active combat was captured. Background flush then app stopped.

Profile,bak and all3 original development files SHA256-identical after review; no restoration, loadout change or permanent test flags. Existing region-preview/gate-arrival/start-floor.txt.off retained; test telemetry retained. Earlier guardian package93380BAC was also installed/verified this turn before the crane revision; this24BC package is now latest. No push. Next art work should address another visible material/silhouette limitation with a real comparison, not claim the concept is finished.


## 2026-10-07 - Floating amount glyph contract
Floating damage/heal/gold amounts now use code-native5x7 glyphs, one-texel gap/outline, normal white and critical gold at2x. Max5digits and pooled renderer unchanged. Supersedes historical3x5 specification; HUD font unchanged. Validated .NET390/Edit410/Play145 and S23 normal combat; critical samples in actual Unity. See docs38 and ArtDirection/2026-10-07/damage-type-01.

## 2026-10-08 - Reference-led layout template

Owner approved correcting overall composition before further local polish. The foundry court now spans design u[-24,14], v[-14,18], around a 12x12 furnace opening u[-16,-4], v[2,14] (previously 8x8). Gate causeway is u[14,28], v[0,10], with a wider landing u[28,32], v[-2,12]. The diagonal entrance bridge remains connected. Its keeper supports moved 0.6 along each negative design axis to keep their complete footprints outside the widened court.

FoundryMap is the shared floor/movement/spawn boundary. Pit bounds are consumed directly by rendering, floor soot and the undercroft exclusion. West crane, loading shelf, anvil/stock, clutter, finials, foundation heads, chains and depth owners moved 6 design units west together. Furnace opening uses the existing rounded-square construction with longer straight sides; molten surface is 3.6 below the deck, bed 5 below, with inner boiler spacing and atmosphere adjusted. No new imported artwork, renderer, currency or combat rule.

This is a first playable composition template, not reference completion. The concept's bevelled outer outline, raised gate terrace, finer apparent masonry scale and denser workshop staging remain open. Current gameplay floor remains one plane; no climbable heights are implied. Proof: ArtDirection/2026-10-08/reference-layout-01. Before/after overview uses identical camera framing; detail cameras follow relocated structures.
## 2026-10-08 - Entrance corner silhouette and gate shoulders

FoundryMap now clips the southwest and southeast court corners by 6 design units: u+v >= -32 and u-v <= 22 apply to the four court rectangles only. The entrance bridge and gate rooms retain their own union. Exact segment intervals, movement clearance and spawning share this boundary. The existing ashlar and flush decals are clipped geometrically with interpolated UV/light; no stepped-cell approximation. Straight perimeter pieces stop at the cut; diagonal coping, balustrades and lower arches close it. Southeast bastion/chain/finial placement follows the new outline. Northern corners remain rectangular.

Two stepped gate shoulders stand outside the walking lane and use explicit depth owners. Their foundations are below-floor geometry; flush arrival bands mark the wider landing. The playable gate floor is still single-plane: these are side terraces, not climbable stairs. No new texture/material/renderer or gameplay rule. 18 region renderers /90,080 vertices. Actual Unity before/after and two new corner walks: ArtDirection/2026-10-08/court-silhouette-01. Full reference fidelity remains unfinished, particularly northern silhouette, terrace elevation, paving scale and workshop grouping.

## 2026-10-08 - North court, grouped workshop and smaller paving

Completed the owner's three-task package in ArtDirection/2026-10-08/north-workshop-paving-01. Four-unit north chamfers share FoundryMap boundary/segment clipping with renderer floor clipping; existing south cuts remain six. New diagonal walls/rails and moved northwest lantern/bastion/chain close the silhouette. StockShelf shifts the full basin/pipe/tool/reserve-stock station twenty design units west, including support, depth footprint, rail exclusion, clutter and soot. The crane/anvil remain anchors. No new props/materials/textures/update loops.

Ashlar candidates now 1-2 design units per side, area at most3 (packer may use smaller infill); centre seal uses one-unit flags. Joint/bevel widths and joint contrast reduced. Texture and UV scale unchanged. Region18renderers /92,951vertices (+2,871 versus90,080), largest mesh27,023. New graphics mesh-limit assertion guards 16-bit indices. Full actual-camera before/after and grouped workshop inspection in proof. S23 north route and ordinary gameplay/loot readback checked; full automated gate passed. This completes the three named tasks, not the reference map: true walkable gate elevation and richer painted light/material character remain open.

## 2026-10-08 - Gate processional artwork

The reference's long framed gate approach is now code-native flush art in FoundryRegionHeraldry: chamfered double border, open four-point longitudinal seal, ring pair and short axial guides. Existing court guides stop before the panel. FoundryRegionArchitecture replaces tiny repeating metal threshold brackets and the overlapping landing band with five broad dressed stones and two narrow bindings. Reuses current coping/paving textures, floor illumination and existing batches; no raster generation or import changes. Proof: ArtDirection/2026-10-08/gate-procession-01.

Initial graphics review:18regionrenderers /94,727vertices (+1,776/about1.9%), largest27,383. No new update loop. The medallion is matte environmental brass, with open paving inside, no glow/timing/interaction. Normal follow portraits distinguish it from the cyan ability range ring. Floor and actor/camera coordinates remain single-plane; this package does not implement walkable gate elevation. Full gate and installed device status are recorded in the latest docs23/38 entry.

## 2026-10-08 — Shared gate elevation presentation

The gate's walking surface now rises 1.25 visual units along design u29..31 and remains level past31. FoundrySurface owns projection/inverse; scenery, ground-contact depth, actors, bars, chest, effects, number anchors and follow camera share it. Actor/effect placement is render-only and fully restored. Ground rings/shadows/pulses bend using cached16x16 sprite grids and one scene-owned shader material, borrowing unchanged textures. New sprite geometry work must be included in future device budgets; static region remains18renderers/94,790vertices. This is continuous 2.5D visual elevation over unchanged floor combat/navigation, not stacked geometry or height combat. Before/after proof: ArtDirection/2026-10-08/gate-elevation-01. Full gate/device status is recorded in docs22/23/38.


## 2026-10-08 — Unified stone, formed boiler hoods and distant cavern

Foundry paving uses restrained individual flag variation and quieter joint/bevel contrast across the seal and court. Existing connected patina, soot and authored forge illumination remain. Boiler hood slopes now sample plain metal continuously over complete far/near courses, with broad directional highlights, flue soot and sparse radial seams; lower riveted shells and all source artwork remain unchanged. FoundryCavern.shader grades only the existing foundry backdrop sprite with one sample, compressing saturation/contrast into violet air. Its material belongs to VoidBackdropView and is released on destruction; ordinary halls retain their material/tint. No image generation/edit, import changes or extra renderer. Static region 18 renderers /95,622 vertices (+832 versus gate elevation); backdrop shader adds ALU to its existing draw. Proof: ArtDirection/2026-10-08/surface-cohesion-01. Full verification/device results are recorded in docs22/23/38 after completion.


## 2026-10-09 — Reflected forge light on industrial metal

FoundryRegionWeathering.MetalLight reads the existing ForgeLamps at authored design coordinates/source heights, including gate surface elevation. It modulates existing directional metal tones with bounded reflected warmth. Applied to opted-in boiler iron courses/discs, formed hood panels/seams, pressure reservoir courses/caps and elevated mains/collars. Fire fronts, glow windows, ordinary rods/valves, floor lighting and imported textures are unchanged. Long lit pipes sample at at most0.75 projected-unit spacing using their explicit surface height, preserving their existing cross section/depth owner/material. Static build-time vertex colours; no new renderer, shader, material, texture or update loop. Region18renderers /96,558vertices (+936, about1%); largestmesh27,373. Proof: ArtDirection/2026-10-09/forge-metal-light-01. Final gate/device status is recorded below in docs22/23/38 after verification.


## 2026-10-09 — Worked-stone and bastion-face treatment

FoundryRegionFinish retains the fine ashlar layout but adds a shallow within-stone mineral wash and sparse five-point edge flakes on larger non-seal/non-workshop flags. Existing source images and continuous floor UVs remain; all chips are flush and share source lighting and gate projection. FoundryRegionMassing frames blind recesses in banded and buttressed foundation faces, preserving contours, anchors and the stepped variant. Static geometry only, same18 renderers. Proof: ArtDirection/2026-10-09/stone-relief-01; complete verification follows in docs21-23.


## 2026-10-09 — Combined gate, workshop and undercroft refinement

Three isolated agents worked on Architecture, Workshop and Undercroft files, with read-only cross-reviews and root-owned integration/capture. Broad gate lancets replace repeated shutters; stored pipes use round sections and plain worked-iron sampling; recessed masonry backs the existing arch rings. Same source assets, static batches and layout. The smoothed stock metal is a form/readability improvement, not new texture realism. Proof: ArtDirection/2026-10-09/architecture-cohesion-01.


## 2026-10-09 — Three-agent material readability pass

Three isolated helper files: Workshop broad unframed iron UVs/continuous stock pipe grain; Massing pit collar-contact shadow and continuous side shading; Heraldry matte inlaid brass and lower groove contrast. Existing raster sources/materials only, no generated texture or extra frame work. Root and helpers cross-reviewed actual matched-camera captures and source; no blocking regression. Use ArtDirection/2026-10-09/material-readability-01 for evidence. These refinements do not finish painted-reference fidelity.

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

## 2026-10-09 — Workshop metal kit, crane housing and service paving

Completed a coordinated three-part art package: enclosed cast lower crane housing, Blender-baked anvil/hammer/parts chest/five ingots, and two local repair patches in the west/north service paving. Original crane silhouette width and upper rigging, station anchors, masonry support, walkable union, camera, combat and profile behavior are retained. Floor repairs subdivide existing parent stones exactly; no overlay or new obstacle.

Editable source: Tools/ArtReview/BuildBlenderSalvage.py and ArtDirection/2026-10-09/salvage-kit-01. Authoring mesh3,038vertices/5,984triangles is baked to one runtime quad;1024source imported512ASTC6x6,10mips,non-readable. Calculated block payload158,432bytes including mips, excluding engine overhead. Original whole-shelf mass depth and explicit material cleanup remain. Actual Unity review caught the equal-rank rear landing hiding the anvil horn; rank-10.5/sortingOrder-7 fixes it without moving the art. Source/image review accepted final contacts and silhouette.

Region24renderers/100,572vertices becomes25/100,968; no new per-frame component. Actual matched source-state before/after, detail/overview and four walkable staged-hero portraits: ArtDirection/2026-10-09/foundry-surface-01. The inherited budget texture line describes unchanged FoundryShortBoiler, not salvage. Final tests/device evidence is recorded in docs22/23/38 and the proof validation directory.

Reference fidelity is unfinished. The crane base remains busy around its old front crank; local floor changes are deliberately restrained. Next higher-value art work should address actor/encounter presentation or a clearly visible architectural material gap, not another marginal bevel pass. Cold-first-ramp diagnosis and sustained crowded/midrange-device acceptance remain open.


## 2026-10-09 — Map shape takes priority over small polish

Owner requested closer reference map size, shape and major features. Implemented westboundary-24→-30 with complete exteriorassembly relocation, +192walkable squareunits, continuous tapered fortressfoundation and deeper industrialpit. Original gate/bridge/pitfloorboundaries and camera remain. Actual proof: ArtDirection/2026-10-09/foundry-massing-01. Full verification/device status: latestdocs38/23entry. Next priority: gateapproach/enclosure, entrancebridgeproportions and purposefulworkyardfeatures. This supersedes the preceding suggestion to switch immediately to actor/encounter art. Fullreference fidelity is notcomplete.

## 2026-10-10 - Reference approach and industrial yard

Implemented a real wider/longer entrance bridge, fortified gatehouse wings and a flush freight-transfer yard. Existing materials/batches;25 renderers unchanged,113,622 vertices (+5,970). Actual comparisons: ArtDirection/2026-10-09/foundry-approach-01. Corrected bridge support ordering, court paving overlap and heavy joints during review. Surface richness and populated machinery remain unfinished. Verified409 .NET /429 EditMode /151 PlayMode, Android exit0; phone installation/review pending owner-run restart permission. Exact evidence and limits: proof validation/verification.md.

## 2026-10-10 - Art-first courtyard craft package

Owner explicitly chose reference-map art before further gameplay/region progression. Three coordinated slices are visually accepted: mixed ashlar workyard/court with broad mineral families and contained corner wear; broad tapered-face gate recesses with dressed caps/quoins; stock-shelf hoist with bearing-supported drum, braces, two chains and lifting yoke. Existing layout, walking routes, doorway, camera and gameplay remain unchanged. Stock feet stay within the original non-walkable shelf and upper machinery has explicit3.6-height depth ownership. No new texture, material or per-frame component.

Actual Unity before/after evidence: ArtDirection/2026-10-10/foundry-craft-01. Region25renderers unchanged;113,622 to117,754vertices (+4,132/3.64%). Yard and gate gains are clear; central paving delta is moderate. Fine irregular stone grain and richer metal surface remain below the approved reference; this is not map-art completion. Local encounters, boss transition and optional finale implementation remain deferred. Full tests/build/device status belongs to proof validation/verification.md and docs22/23/38.
## 2026-10-10 - Authored hoist and coherent stone surfaces

The stock hoist now uses authored Blender4.5.9 geometry/materials/lights, rendered as a transparent1024 source and imported512ASTC6x6/10mips/non-readable. Editable source and projection: ArtDirection/2026-10-10/stock-hoist-01; generator Tools/ArtReview/BuildBlenderStockHoist.py. One quad and owned material replace procedural hoist geometry with a missing-resource fallback. Exact stock shelf, crosshead height and explicit non-landmark depth ownership remain. Scene cleanup releases owned source/variant/mesh, not shared texture. Calculated compressed-block payload158,432bytes including mips, excluding engine overhead.

Floor wear replaces broad pasted-on polygons with narrow fractured edges, small mineral corners and rare shallow incisions. Packer/palette/coverage/exclusions unchanged. Gate bodies use finer staggered stone with belt soot/damp runs; original tapered faces, caps, quoins and doorway remain. Matched actual Unity review accepted all three tasks; no floor gaps/door spill observed. Crane stays in front of the hoist in accordance with its spatial position, partially hiding chains/yoke.

Combined region25->26renderers,117,754->118,902vertices (+1,148). Material gain is clearest on the hoist; stone improvements are restrained, not full reference fidelity. Fixed-view baked light and remaining coarse stock surfaces are limitations. Proof and final checks: ArtDirection/2026-10-10/foundry-materials-01.
## 2026-10-10 - Coherent crane and stock materials

Authored Blender replacements now cover the existing metal stock station (basin, billets, coils, hollow pipes and tools) and crane body (mast, winch, bearing, open jib and counterweight). Editable scenes, source scripts and projection contracts are retained under stock-station-01 and crane-01. Original wood, crane footing, live chain, tip sheave, basket and lights remain separate. No layout, navigation, camera or gameplay changes.

Actual Unity image review found and corrected a middle-mast occlusion: only the rear northwest lantern masonry was moved behind the crane; its cage and panes retain their foreground ranks. Bearing flanges were darkened. Final independent review accepted mast continuity, chain/basket attachment and stock readability. Two bounded cards retain explicit non-landmark depth, fallback geometry and owned material/mesh cleanup. Stock imports at 512 ASTC6x6/10mips; crane at 1024 ASTC6x6/11mips, both non-readable. Region: 29 renderers / 116,178 vertices, versus 26 / 118,902 before. This is not a frame-time measurement. Proof: ArtDirection/2026-10-10/workshop-coherence-01; exact verification and device status in its validation/verification.md.

## 2026-10-10 - Court, brass and furnace surface cohesion

Three coordinated art changes: compact oblong ashlar weights with softer nonceremonial joints/bevels and neutral mineral tone; warmer ceremonial brass with clean rail UV sampling and wider metal inside unchanged grooves; three below-deck flue bays and faceted blue-black vessel crowns. No layout, camera, movement, encounter, save, texture or material changes.

Actual matching Unity views accept the brass and crowns as clearer improvements. Floor changes remain modest: rectangular packing still differs from the reference's irregular flags. First pit review found an overly black ladder-like inset; backing/ties were softened and the narrower heat slot moved deeper before final capture. No visible floor spill, floating vessel or new broken inlay observed. Runtime remains29renderers;116,178->117,045vertices (+867/0.75%). No FPS claim. Evidence: ArtDirection/2026-10-10/foundry-surface-cohesion-01.

## 2026-10-10 - Actual hand-quarried flagstone contours

Nonceremonial flags now have asymmetric corner setbacks and quarry cuts in their real face geometry. Safe inward bevels follow the contours; a complete mortar bed remains underneath. Faces are clipped into the existing adaptive light cells, preserving floor projection and world-continuous UVs. Ceremonial flags, service repairs, small infill and stones spanning a ramp seam keep their regular path. No texture, material, renderer, layout, camera or gameplay additions.

Matched actual Unity images accept a visible shape improvement in the yard and west-pit portrait, with continuous light and readable actors/range ring. The exposed mortar network is stronger, especially in the yard and gate ramp; rectangular courses remain obvious. This is partial progress toward reference masonry, not the final stone finish. Further work should improve mortar/course composition rather than piling on corner chips. Region29renderers,117,045->113,995vertices (-3,050; about2.6%). No frame-time claim. Proof: ArtDirection/2026-10-10/foundry-flagstone-01.

## 2026-10-10 - Distant industrial works and calmer paving

Five world-anchored towers and a broken arched service span now occupy the space between the painted cavern and the playable island. Their footprint and crowns remain outside/below the walking deck. The first image review rejected crisp floating column ends and foreground-like masonry contrast. The accepted correction uses the existing cavern shader with stronger indigo atmospheric blending, opaque upper tiers, fading lower shafts and quiet furnace slits. One scene-owned material borrows the existing wall texture; fire still borrows the existing ember material. No new texture or per-frame component.

Foundry-only sky/cavern ranks are -22/-21, distant masonry/fire -20/-19, and the existing map stays at -18 or above. This keeps the new geometry behind even the deep basin, without bounds-centre sorting ties. Non-foundry backdrops retain their prior ranks. The previous paving contours remain, with narrower setbacks and quieter nonceremonial mortar. Movement, camera, combat, navigation, inlays and saves are unchanged.

Root and independent actual Unity review accepted the final atmospheric and floor corrections. The northwest tower is still simply shaped and large in a close portrait; the reference's architectural richness and working-yard composition are not complete. Region counts: 29 to 31 renderers and 113,995 to 115,042 vertices (+1,047, about 0.92%). Counts do not establish frame time or allocation improvements. Evidence: ArtDirection/2026-10-10/foundry-depth-01; final gate/device status belongs to its validation/verification.md and docs22/23.

## 2026-10-10 - Casting workbay

Added an original Blender-authored screw press, moulding bench, open sand moulds, cooled billets and hand tools behind the north pit rim. The new shelf u[-18,-13], v[19.1,22.4] stays outside the walking union and clears the existing finial. Its top is .18; actual equipment is .18-2.47 high. Below-deck supports join the shelf to the court wall. The north rail remains intact. Card projection and non-landmark depth ownership are explicit; the fixed-view card sorts behind the rail/finial and owns only its material/mesh, borrowing the imported texture.

Actual Unity detail, overview and production-width portrait review accepted by root and an independent helper. Main gain is local workshop identity; whole-map contribution is modest. Repeated light moulds look cleaner than surrounding iron, a future polish limit. Source1024RGBA imports512ASTC6x6/10mips/non-readable. Region31->32renderers,115042->115106vertices (+64), no new per-frame behavior. This is a resource count, not a performance measurement. Camera, navigation, gameplay and saves unchanged. Editable source: ArtDirection/2026-10-10/casting-bench-01. Integration/proof and final gate: ArtDirection/2026-10-10/foundry-workbay-01.

## 2026-10-10 - Recessed distant furnace architecture

The two dominant north skyline towers at (-5,39) and (12,42) now use recessed furnace bays on both visible faces, raised eight-piece arch frames, chamfered jambs, sill/impost shoulders and distinct tapered upper shafts. The existing broken viaduct gains deeper underside masonry, bearing shoes, low parapets and sparse posts; its central break remains. Facade UVs share each wall's fixed orientation, so sloping arch segments do not rotate brick courses.

The first actual Unity review rejected weak frame definition and long uniform fire strips. Final raised frames and three shorter dull gradient panes resolve that issue; real masonry crosspieces occupy gaps with no fire geometry. Root and independent review accept joins, recesses and foreground separation. The portrait gain is modest because the arch heads sit above the camera crop. Full reference richness and building scale rhythm remain incomplete.

No new texture, material or per-frame component. Existing world anchors, background ranks -20/-19, atmosphere .62 and lower-shaft fade remain. Region: 32 renderers unchanged, 115,106 to 116,326 vertices (+1,220/about 1.06%). Distant batches contain 2,276 vertices within the existing 2,500 bound. No frame-time claim. Source: FoundryRegionDistantWorks.cs. Evidence/reproduction/final gate: ArtDirection/2026-10-10/foundry-skyline-01. Camera, navigation, gameplay and saves unchanged.

## 2026-10-10 - Gate and ceremonial court composition

Three coordinated changes bring the gate/court closer to the approved reference: supported stepped portal crown with continuous ashlar buttress piers and deep inner jamb returns; one radius-3.97 radial stone medallion replacing the remote radius-8.6 bullseye; segmented flush stone approach border replacing the double gold frame. Door art, banners, existing gate boundary, ramp, camera, navigation, combat and saves remain unchanged. Original material batches and depth owners are reused.

First actual Unity review accepted the gate/court but rejected a near-invisible approach border. Modest stone-value separation and narrow mortar improved readability; a second review found overlapping corner strips. Final shared miter joins and bed-before-face ordering fix those joins. Root and independent review accept final gate detail, court detail and production-width portraits. Baseline f76b8f3; matching proof in ArtDirection/2026-10-10/foundry-gate-court-01. No image-generation or new texture required.

Region remains 32 renderers; vertices 116,326 to 115,944 (-382/about 0.33%). No per-frame component or performance claim. Full reference fidelity remains incomplete: masonry grain/scale, wear and wider architectural variety still differ. Final suite/build/device status is in the proof validation record.

## 2026-10-11 - Finer foundry stonework and gate material

Built-in image_gen edited the existing FoundryPavingV4 surface into versioned FoundryPavingV5.png. Its fine mineral pores/flecks replace broad scalloped cloudy wear; generated pixels are imported unchanged. The approved reference remains foundry-layout-01/foundry-v1.png. Both sources1254square RGB; meanRGB87.76/80.85/84.14 ->85.92/79.70/82.13, slightly darker. Android import1024square ASTC6x6/11mips/non-readable. Old sources/GUIDs preserved; only MAT_FoundryPaving texture binding changed. Full prompt/provenance: ArtDirection/2026-10-10/foundry-stonework-01/prompt.txt.

Ordinary ashlar now favours1x1/1x1.5 flags, with quieter narrower joints and restrained stone value variation. Per-slab cloudy vertex wash removed; shared soot, quarry family, lamp field and adaptive ramp sampling retained. Shared public PavingGrainSpan5.7 replaces8.55 in all foundry paving emitters (1.5xgrain density); unrelated small-arena mapping stays8.55. Court medallion and approach border remain. Gate lintel/crown/returns receive bonded courses and restrained edge losses; blind-niche UVs are continuous. Coping blocks restore shallow real bevels/joints inside the original cap footprint/top height.

First actual Unity review rejected persistent cloudy paving and blank gate caps. Final V5 plus clipped coping blocks accepted by root and independent review; overview gain is subtle, close-up mineral texture clearer and actor/effect contrast retained. Baseline22e32ea before captures reused unchanged; two after captures run, first rejection retained. Region32renderers unchanged;115944->118082vertices (+2138/~1.84%). One new texture asset, no new material/renderer/per-frame component. No frame-time or allocation claim. Map/ramp/camera/navigation/combat/scene/saves unchanged. Full reference fidelity, regular-paving rhythm and wider architectural richness remain open. Final tests/build/device status in proof validation record.
## 2026-10-11 - Recessed portal and industrial exterior parapets

Completed together: a supported recessed stone cartouche with a restrained iron lock replaces the gate's central stacked boxes; exterior rail bays receive a solid stone sill, square iron web and broad coping. Existing gate crown/door/banners and rail footprint/top/posts/depth owners are retained. Pit and bridge keep their separate open balustrades. No image generation or new texture/material/per-frame component. Sources: FoundryRegionArchitecture.cs, FoundryRegionGateHeader.cs, FoundryRegionMassing.cs.

Matched actual Unity overview/details/production-width portraits accepted by root and independent reviewer. No new visible hero obstruction or corner/boiler-return regression in reviewed views. Region remains 32 renderers; 118082 -> 117629 vertices (-453/about 0.38%), not a performance measurement. Baseline e61ac5a before evidence reused unchanged; one new after graphics capture. Gallery, reproduction and final verification: ArtDirection/2026-10-11/foundry-portal-parapet-01.

Remaining reference gap: long regular perimeter bays, paving rhythm and wider architectural variety. Portal relief is a close-view improvement, not a whole-map transformation. Map/ramp/camera/gameplay/scene/saves remain unchanged. Physical-device/crowd/midrange/first-ramp acceptance remains open; regional encounter/boss progression remains deferred by the art-first decision.

## 2026-10-11 - Distant furnace variety and exterior bay rhythm

Three simple distant shafts now have sloping shoulders/narrow necks, splayed bearing ribs and single/twin/offset exhaust crowns. Existing anchors, fade, material, light openings and background ranks retained; two arched northern furnaces unchanged. Exterior spans now group into wide recessed masonry panels flanked by iron openings and short lower-sill open bays. Post locations, span bounds, top height0.72, depth owners, light anchors and pit/bridge balustrades remain. No new texture/material/renderer/per-frame component; map/ramp/camera/gameplay/saves unchanged.

Root and independent reviewer accepted matched actual Unity views. Gain is most visible at close perimeter views; whole-map impact is modest. Region32renderers,117629->118537vertices (+908/about0.77%). Distant2244masonry+144fire=2388, below unchanged2500bound. No performance claim. Evidence/reproduction/final verification: ArtDirection/2026-10-11/foundry-mass-rhythm-01, baselinec532902. Before images reused unchanged; one new after graphics capture.

Next substantial priority: purposeful workyard clusters and more individual boiler silhouettes, with investigation of dominant painted backdrop scale. Avoid another tiny parapet/crown pass. Existing rail across closed gate is unchanged, pending transition design. Full reference fidelity and accumulated physical-device/crowd/midrange/first-ramp acceptance remain open. Regional encounters/boss progression remains deferred.

## 2026-10-11 - Recompose foundry workyard clusters

Two existing native Blender sources now distinguish stock handling (upright coils, supported pipe bundle, staged billets) from casting stages (active mould, removed half, closed flask, finished stock). Source PNGs imported unchanged into the two existing Resources assets; GUIDs/settings/card projection/depth/512 ASTC6x6/10mips/non-readable contracts retained. Editable outputs: ArtDirection/2026-10-11/workyard-clusters-01. No source mesh is shipped as runtime geometry.

A shallow inspection lid distinguishes the west squat boiler, emitted inside its existing upper-shell batch and using the existing opaque metal texture sample. First capture rejected a raised table-like hatch and bright flat tall-boiler overlay; the tall overlay was removed completely and the hatch lowered/seated. Root and independent reviewer accepted the final actual Unity views. Region32renderers unchanged;118537->118709vertices (+172/about0.15%), no performance claim. No new material/per-frame component, no map/ramp/camera/gameplay/scene/save changes.

Baseline10d5e01. Before evidence reused/hash-checked; two after captures. Gallery, provenance, reproduction and final verification: ArtDirection/2026-10-11/foundry-workyard-identity-01. These are Unity captures, not phone screenshots. Local improvement is accepted; whole-map benefit is modest. Crane partly hides coils. Further boiler individuality should use coordinated native source/bake, not flat overlays. Larger workyard distribution and dominant backdrop scale still differ from reference. Physical-device/crowd/midrange/first-ramp acceptance remain open; regional encounter/boss progression remain deferred under art-first direction.
## 2026-10-11 - Foundry-specific cavern depth

Built-in image_gen edited ENV_Furnace_Cavern_v1 with the approved foundry concept as a style/depth reference. New versioned FoundryCavernV2 replaces giant foreground flank walls with smaller staggered tower/crane/bridge groups, keeping quiet central indigo depth. Source1024x1536RGB copied byte-for-byte to Resources; original generic plate/scene binding remain. Complete prompt, unmodified source and provenance: ArtDirection/2026-10-11/foundry-backdrop-depth-01/source. No raster postprocessing or CLI/API fallback.

VoidBackdropView selects the borrowed V2 only for foundry with existing cavern/camera prerequisites, falls back to supplied art if absent, and retains ordinary-arena/procedural behaviour. Cover-fit/parallax/tint/atmosphere shader/ranks/lifecycle unchanged. Tests cover selection, fallback, borrowed lifetime and roaming camera coverage. Region stays32renderers/118709vertices; one new Resources texture/sprite and no new per-frame work; no device performance claim. Camera/map/ramp/gameplay/scene/save unchanged.

Actual Unity review by root and independent reviewer accepts clearer whole-map depth/island outline and bridge-side scale progression. Portrait gain is restrained; actor readability retained. Repeated cranes and comparatively simple/dark native distant shafts remain limits; full reference fidelity is incomplete. Matched baseline49cf944 before captures reused/hash-checked. Gallery/reproduction/final verification: ArtDirection/2026-10-11/foundry-backdrop-depth-01. Initial importformat ambiguity is documented separately from final Android inspection; never equate requested compression with observed texture format.
## 2026-10-11 - NPOT backdrop import correction

The first FoundryCavernV2 import loaded RGBA32/11mips even with an ASTC6x6 Android override. Explicit Android capture confirmed this; installed Unity engine reports that only power-of-two textures can be compressed when mipmaps are enabled. Keep the original1024x1536 sprite dimensions and disable mipmaps, matching the old cavern strategy. Final observed import: ASTC6x6/one mip/non-readable, with Editor texture-memory reading16778104->1401728bytes. This is not a phone measurement. An Android loaded-format assertion and mip-count assertion prevent recurrence. Source pixels unchanged; root/independent final compressed-image review accepted the composition. Evidence: foundry-backdrop-depth-01/validation and after/cavern-texture.txt.

## 2026-10-11 - Native distant masonry coherence

FoundryRegionDistantWorks now uses finer 2.1 x 1.8 facade courses, design-plane cap mapping, fixed bridge-wide course direction, restrained height weathering and clearer face/recess light separation. Its private material atmosphere changes .62->.44; the shared cavern shader, borrowed texture and painted backdrop are unchanged. The existing silhouettes/anchors, opaque upper tiers, lower fade, geometry, two ranks and ownership remain. Region: 32 renderers / 118,709 vertices; distant works: 2,388 under 2,500. No new texture/material instance/updater, no performance claim.

One actual Android-target Unity capture and independent seven-view review accepted bounded material/depth improvement. Native silhouettes remain rigid and regular; full reference fidelity is incomplete. Matched evidence/reproduction/gallery: ArtDirection/2026-10-11/foundry-distant-material-01, baseline e98d4ac reused/hash-checked. No camera, map/ramp, gameplay, scene or save edits. Final suite/package/device status lives in its validation/verification.md and latest docs 22/23.

## 2026-10-11 - North furnace silhouettes and fractured span

One native geometry partial, FoundryRegionDistantWorks.cs, now gives the two north shafts distinct receding crowns, a dark open exhaust versus a seated offset chimney, and substantial inward-leaning corner buttresses. Two different closed fracture profiles replace square bridge ends; terminal posts are removed. Hidden foundation caps are omitted. Code review corrected detached buttress heads before the first actual capture; positive shoulder contact is independently verified.

Region remains 32 renderers; 118,709 -> 118,817 vertices. Distant batches total 2,496, within the unchanged 2,500 ceiling. Existing .44 atmosphere, UVs, lower fade, anchors/ranks, borrowed textures and lifecycle retained. No new texture/material/updater, camera/map/ramp/gameplay/scene/save change or performance claim. Proof, matched baseline bd5b162, reproduction and final validation: ArtDirection/2026-10-11/foundry-north-silhouette-01. Distinct crowns improve the outline; fracture detail is mostly close-view. Full reference fidelity and broader workyard composition remain incomplete.


## 2026-10-11 - Interior workyard composition

Two original native equipment islands now occupy the broad west court: a loaded trolley and an open mould bench, joined visually through the existing turntable and new flush south rails. Warm mill-scale billets, recessed ochre sand and seated ingot patterns distinguish function while frames remain cool iron. Existing material/rank batch reused; no new texture/material instance/renderer/updater. Region32renderers,118817->120677vertices (+1860/about1.57%). Counts are not phone performance evidence.

The full shallow skids mark exact shared FoundryMap.WorkIslandBound footprints. Raised equipment stays inside; 1.55 depth ownership covers max1.51 clamp. Actual floor remains continuous while navigation excludes the two islands. See latest decision entry and proof README for SurfaceContains, swept clearance and pursuit contracts. No progression/balance/scene/camera/save-format change. Two after captures; matched baseline93f9f27 reused/hash-checked and three new views explicitly after-only. Evidence/reproduction: ArtDirection/2026-10-11/foundry-workyard-islands-01. This adds real working masses to the yard; native equipment remains simpler than the reference.


## 2026-10-11 - Broad working court and manufactured equipment

Owner requested real width closer to the reference and multiple visual tasks together. Main court expands44->50u; west working strip14->20u, with pit/bridge/gate unchanged. Shared map boundary moves western assemblies-6u; interior turntable/islands move-3u through WorkyardShift. West freight rail extends toWest+.35 rather than leaving a gap. Two existing native equipment islands receive connected load bands/axles/wheel ribs and a warm planked bench with supported clamps/handles. Existing shared blocked footprints remain; equipment height<=1.54 under1.55 depth owner. Same32renderers;120677->128573vertices (+7896/about6.54%); distant2496unchanged, no new texture/material/updater and no phone performance claim.

One actual Android-target graphics capture, eleven matched baselineb5bcbed views and six after-only new-strip views. Root/independent review accept the broadened composition, attached equipment and portrait floor contact. Reference breadth is closer, but gate-causeway proportion and material richness remain unfinished. Gallery/reproduction/evidence: ArtDirection/2026-10-11/foundry-broad-court-01. No device action; exact gate/build status in its validation/verification.md and latest docs22/23/38.

## 2026-10-11 - Compact gate and furnace crowns

The 50-unit main court and 20-unit west yard remain. The gate arm from court edge to landing end contracts18->14 design units: the landing, ramp, facade, foundations, lights, depths and far boiler pair move4u inward together. Near boilers stay fixed. Short mains and outward reservoirs receive connected low masonry footings. North shafts exchange long sloping crowns for compact shoulders, upright necks and distinct exhausts within the same distant face budget. This is a proportion/composition package; full reference material richness remains incomplete.

Two actual Android-target captures, with10 matched baselinef5e1487 views and2 after-only gate portraits. Root flagged a detached far drain in the first full-map; independent source audit traced its stale26.5 anchor and unintended new-ramp lift. Both drains now derive from their boiler centres, and final image review accepts attached mouths/tips. First image/diagnostic retained. Region32renderers,128573->125406vertices; distant2496 unchanged. No new texture/material/updater or phone performance claim. Evidence/reproduction: ArtDirection/2026-10-11/foundry-compact-gate-01. Final test/build and device status are recorded in validation/verification.md and docs22/23/38.

## 2026-10-11 - Four authored industrial furnace variants

Two tall and two low native Blender4.5.9 structures now replace the duplicated boiler bakes. Tall forms are a stepped square pressure chest and ribbed exhaust stack; low forms are a folded hood with seated inspection hatch and a segmented servicing hood. Original lower fireboxes, fixed-view light/camera, footings, height envelopes and centred steam outlets remain. Source review caught and corrected capped flue mouths and an inherited0.02receiver/cornice gap before Unity import. Original decorative fire-mouth overhangs are documented as retained source geometry, not new walkable obstacles.

Two2048x1024RGBA atlases preserve each1024-square source tile exactly; existing resource GUIDs retained. Leftnear/rightfar selection derives from BoilerCentres; no new material/batch. Android actual import observedASTC6x6,12mips,clamp,non-readable. Four original band batches retain sizes/splits/depth/order/cutaway/lifecycle. Native172vertex hatch overlay removed because the detail now exists in the baked source. Region32renderers/125234vertices, down172. Calculated total compressed-block delta1248832bytes excluding engine overhead; Editor reported2502304bytes per importedatlas. Neither is device performance evidence.

Five helpers across at most3concurrent workers split tall/low native sources, runtime integration, independent source/actual-image QA and independent code QA. Root alone ran Unity. Final source and actual Unity reviews accept distinct profiles and connected feet/pipes/drains with clear hero silhouettes; full reference gate/support architecture remains unfinished. Evidence/reproduction: ArtDirection/2026-10-11/foundry-industrial-01 and sibling industrial-furnace-01/industrial-retort-01. Newsource.blend files are authoring artifacts; no3Dmesh ships at runtime. Test/build/device status is recorded in package validation and docs22/23/38.

## 2026-10-11 - Supported gatehouse and dressed approach

The combined native package lowers the outer buttresses 9.2->7.35, replaces the portal's nested crest with a restrained parapet (8.05->7.66), broadens dressed jamb returns, narrows blind recesses and replaces miniature landing steps with three seated tiers. Inner iron terminals now end at7.94 instead of8.45, fitting the existing8.2 depth envelope. Original door rectangle, foundations, boiler socket contacts, walking/ramp geometry and camera remain fixed. The compressed cartouche and lock share one height mapping.

The processional field gains four staggered flush flagstone courses and two edge courses to the original threshold. New triangles share world-continuous paving UVs and split through both ramp grade breaks. First actual-image review rejected their faint read, despite valid geometry. Mortar/joint/bevel changes (.80/.024/.045) made the second capture readable without brightening the faces. Preserve this rejection and final acceptance as evidence; pixel differences alone are not art acceptance.

Five helpers split three disjoint source filesets, independent geometry tests/review and actual-image review. Two Android-target captures; final region32renderers/129812vertices versus125234baseline (+4578). No new runtime texture, material instance or updater; no device-performance claim. Fourteen before images copied/hash-checked from857a99c. Sources, images, reproduction, final validation and limits: ArtDirection/2026-10-11/foundry-gate-stone-01. Full reference fidelity is still open.


## 2026-10-11 - Court, bridge and dressed masonry verified; device pending

Court and workyard zones favor larger unequal flags (maximum area3), with broad correlated quarry tones and clearer restrained joints. Bridge central deck uses staggered 1x1/2x1 oblongs clipped to the original lane/court boundary; quiet outer bands and continuous world grain/light remain. Gate ashlar faces gain narrow coplanar dressed rims; long coping fascia gains broad fitted blocks while original roof bearing, exact face envelopes, tiers, sockets and depth ownership remain. Gate candidate family/treatment is retained, but shared packer joint scores can change its layout. No new runtime texture/material/renderer/updater; camera, navigation, ramps, scenes, gameplay and saves unchanged.

Three helpers split court finish, gate masonry/tests, and proof/source/actual-image review; root implemented the bridge and ran all Unity checks. Fourteen baseline9150049 matched before images were copied and hash-verified; capture is after-only. Browser controls and loaded images verified after restarting the existing localhost preview server. Two tapered/skewed masonry cases guard exact silhouette, area partition and UV/light continuity; one actual deck case guards complete floor coverage, continuous grain, flush surface and court overlap ownership.

Root and independent seven-pair actual Unity review accept reduced bridge checkerboard, clearer court bond and restrained gate edges. Actor/feet/ability guide and pipe contacts read correctly in inspected views. Whole-map gain is modest; full reference material/structural richness remains incomplete. Region32renderers/135688vertices versus129812baseline (+5876/about4.5%); distant2496 unchanged. These are resource counts, not device performance evidence. No ADB or fresh phone observation; earlier concrete install/restart decision remains pending. Before future deployment inspect fresh state and preserve current original saves/telemetry; never restore historical snapshots. Physical-device circulation/crowd/HUD/motion/shimmer/midrange acceptance and region gameplay/boss progression remain open.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-court-stone-01.


## 2026-10-11 - Pit machinery and exterior masonry verified; device pending

Pit vessels now have distinct broad octagonal pressure and stepped receiver heads, seated hold-downs, recessed annular mouths and below-deck service bays/cages. All remain within the existing radius1.1/height1.3 hood and deck-to-molten drum envelopes. Foundation curtains use broad fitted courses, arch wedges use quiet mineral grain and tapered piers have full-width dressed courses. Existing silhouettes, pit/collar/pool, flue sockets, depth owners, material batches, navigation, camera, scene, gameplay and saves remain unchanged.

Three helpers split pit machinery, outer masonry/tests, and proof plus independent review; root alone ran Unity. Fourteen c549f06 baseline views were copied/hash-verified; captures are after-only at matched cameras. Independent source review caught design-collinear wall merging across gate ramp grade breaks. The fix requires projected collinearity at both the deck and contracted foot; a regression uses the real south landing and the foot-only bend at26. First capture diagnostics/images are retained. Eight new EditMode cases cover emitted vessel containment, wall joins, projected ramp preservation, tapered/sloping masonry, UVs and depth cooling.

Root and independent actual-image review accept clearer machinery construction and broader quieter exterior stone faces. Whole-map gain is modest; full reference material/structural richness is unfinished. Region remains32renderers;135688->148756vertices (+13068, 9.6%). Distant2496 unchanged. No new texture/material/updater and no phone-performance claim. No ADB or fresh device observation; the earlier concrete install/restart decision remains pending. Future deployment must inspect fresh state and preserve current original saves/telemetry; never restore historical snapshots. Device circulation/crowd/HUD/motion/shimmer/midrange acceptance and regional encounter/boss progression remain open.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-pit-wall-01.


## 2026-10-11 - Bridge architecture and workyard equipment verified; device pending

Entrance bridge gains connected outboard parapets, bevelled coping with broad joints, fitted support-arch wedges and coursed legs/fascia. Short inscribed depth owners remain outside the walking union, both asymmetric court joins are clipped, and keeper consoles remain exposed below the existing statue sockets. Yard trolley gains broad cast retaining yokes, sloped haunches, split axle bearings and unequal billet ends with seated straps. Mould bench gains a supported lower shelf, trestle gussets and distinct ingot/circular bearing patterns. Existing shared equipment footprints and 1.55 height envelope remain. No map/navigation, camera, scene, encounter, gameplay or save change.

Three helpers split bridge architecture/tests, yard equipment/tests, and proof plus independent source/image review; root alone ran Unity. Fourteen matched baseline b91ac1a views were copied from tracked files and Git-object/SHA256 verified. Three new portraits are explicitly after-only. Source review caught far trolley-yoke painter ordering and it was corrected before capture. First actual review found quiet ruler-flat parapet caps, rather than self-overpaint; bounded bevels and broad coping joints were added, then recaptured. Selected first images/budget/XML are retained. Five new EditMode cases cover actual bridge geometry/owners, both court seams, keeper seating and emitted yard equipment containment/UVs/indices. Initial full EditMode found two failures in the new cap test: its inverse chose the lower edge of a float32 projection plateau at the exact bridge-start boundary. The test was corrected to verify forward projection within the physically legal interval, with a quantified two-output-ULP allowance, instead of an arbitrary inverse representative. A second run exposed the final midpoint rounding to the worse bracket endpoint; selecting the endpoint with smaller forward residual fixed it without enlarging the allowance or design bounds. Focused bridge3/3 and final complete EditMode passed. No runtime correction was needed. Both failed XML files and focused results are retained. Full PlayMode completed against final runtime source; after the test-only correction the complete EditMode suite was rerun before building. The APK was never built on a failed gate.

Root and independent actual-image review accept clearer arch construction, seated keeper statues and better supported/recognisable yard equipment. Parapet and whole-map gains remain modest; full reference material/structural richness is unfinished. The center/near-island portraits show clear actor feet/guide, not exhaustive moving-edge/crowd acceptance. Region remains32renderers;148756->162568vertices (+13812, 9.3%). Distant2496 unchanged. No new texture/material/updater and no phone-performance claim. No ADB or fresh device observation; earlier concrete install/restart decision remains pending. Before eventual deployment inspect fresh state and preserve current original saves/telemetry; never restore historical snapshots. Device circulation/crowd/HUD/motion/shimmer/midrange acceptance and regional encounter/boss progression remain open.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-bridge-yard-01.


## 2026-10-11 - Western loading assemblies verified; device pending

Western service shelves and crane footing replace solid faces with recessed coursed infill, connected tapered corbels and continuous bearing courses; shelf decks gain bounded iron channel edges. Casting cantilevers are tapered within their existing attachment volumes. Three native Blender renders replace the existing crane, receiving stock station and casting station images: rough cast crane channels/yokes/bearing cases, a loaded receiving cradle with six staggered hollow pipes, and a real annular mould cavity/runner with a supported pouring vessel. Existing card framing, anchors, GUIDs, import settings and depth owners remain. Camera, map/navigation, scene, encounters, gameplay and saves are unchanged.

Three helpers authored support geometry/tests, crane source, and stock/casting sources. Root integrated all three resources, reviewed the source diffs and both source renders and actual Unity images, and alone ran Unity. Helpers reached the account usage limit during the requested cross-review, so independent cross-review did not finish and is not claimed. Eight matched baseline 3044b83 views were copied from tracked files and Git-object/SHA256 verified; three close equipment views and two local portraits are explicitly after-only. The source-before directory retains all three verified baseline resource PNGs. Initial graphics import found a new test referencing internal FoundrySurface across the Unity assembly boundary; the test now invokes the existing private FoundryRegionView.Project through reflection. The initial diagnostic is retained. This was an import failure, not a completed graphics capture. Five new EditMode cases validate actual emitted geometry against projected original support prisms, unchanged deck corners, scenic footprint interiors, below-floor separation, finite UVs/indices and bounded vertex cost.

Root actual-image review accepts better connected loading architecture and clearer receiving/casting identities. Live crane chain/sheave/load contact remains seated in the inspected images. The existing foreground lamp pole crosses the mast and the large northwest shaft still dominates some close views. Local gains are visible; whole-map gain is modest and full reference material/structural richness remains unfinished. Region remains 32 renderers; 162568->165392 vertices (+2824, 1.7%). Distant2496 unchanged. Existing crane import1024 ASTC6x6/11mips and stock/casting512 ASTC6x6/10mips remain non-readable; no extra runtime texture, material or updater. Native Blender mesh counts are authoring counts, not runtime GPU geometry. These resource counts are not phone-performance evidence.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-loading-bay-01.


## 2026-10-11 - Connected service shafts verified; device pending

Two existing northwest distant furnace shafts receive narrower necks, joined dressed shoulders, real recessed neck panels with side returns and hollow exhaust crowns. The original leaning buttress heads move inward to meet the narrowed neck. The mouths have low throats, inner walls and separate annular rims instead of solid roof caps. Existing lower furnace body/fire, other distant variants, atmospheric fade, materials, sorting, camera, navigation, gameplay and saves remain unchanged.

Region32 renderers/166468 vertices versus165392 (+1076); distant geometry2496->3572. The combined distant vertex ceiling is intentionally revised from2500 to3600 for two complete hollow crowns/recessed necks; all existing lifetime, material, rank, fade and bounds assertions stay intact. This is a measured resource-budget decision, not a frame-time result. No new texture, material instance or per-frame updater.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-service-shafts-01.


## 2026-10-11 - Court, pit and gate construction verified; device pending

The court compass now uses restrained paired brass facets and a continuous engraved ring over three fitted stone courses. The pit upper lining has a joined below-deck bearing course and four relief ribs with side returns, leaving all furnace bays, vessels and the molten seam unchanged. Four gate shafts receive genuine blind recesses with backing, returns and broad dressed surrounds; two connected header shoulders replace the former interrupted coping ends. Existing court/pit/gate footprints, door and lock sockets, ramps, navigation, camera, material batches, depth ownership, scene, gameplay and saves remain unchanged.

Region 32 renderers/165232 vertices versus 166468 (-1236); distant geometry 3572 unchanged. No new runtime texture, material, renderer or updater. Counts are resource observations, not device frame-time measurements.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-court-pit-gate-01.


## 2026-10-12 - Court, pit and gate material integration verified; device pending

Gate recesses now map the existing stone grain by real block dimensions, continue it over bevels and retain inherited corner lighting plus a shaded-head/reflected-foot gradient. Pit ribs use a quiet cast-iron atlas interior at bounded grain scale with restrained integral worn arrises; the under-rail bearing uses cool masonry and actual narrow block beds. The medallion ring and fitted centre flags gain flush dressed shoulders and share the surrounding court quarry family, reducing the separate warm checker pattern. Existing footprints, silhouette, pit fire and vessels, door/lock, navigation, camera, scene, gameplay and saves remain unchanged.

Region 32 renderers/166467 vertices versus 165232 (+1235); distant geometry 3572 unchanged. No new runtime texture, material, renderer or per-frame updater. These resource observations are not device-performance measurements.

Evidence/reproduction: ArtDirection/2026-10-12/foundry-material-integration-01.
