# Art Direction & AI-Assisted Asset Pipeline

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
