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