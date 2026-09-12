# Art Direction & AI-Assisted Asset Pipeline

## Main recommendation

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
