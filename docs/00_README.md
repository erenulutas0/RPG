# Mobile Idle Auto-Battler RPG — Project Starter Pack

> Version: 0.1  
> Date: 2026-09-12  
> Status: Pre-production / prototype planning  
> Primary platform: Android first, iOS later  
> Engine: Unity 6.x (pin one stable version and do not upgrade casually during MVP)

## What this repository is

This folder is the project's **single source of truth** before implementation starts.

The goal is not to clone *Bones and Coins*. The goal is to use the validated appeal of:
- incremental progression,
- auto-combat,
- loot,
- hero identity,
- weapon identity,
- build synergies,
- short repeatable sessions,

and redesign the formula as a **mobile-first original game**.

The project should be small enough for a solo developer using AI-assisted development, but structured well enough that it can scale if early metrics are promising.

## Recommended reading order

1. `01_PRODUCT_VISION.md`
2. `02_MARKET_RESEARCH.md`
3. `03_GAME_DESIGN_DOCUMENT.md`
4. `04_CORE_LOOP_RETENTION.md`
5. `05_ECONOMY_BALANCING.md`
6. `06_TECH_ARCHITECTURE_UNITY.md`
7. `07_CODE_STANDARDS.md`
8. `08_ART_AI_PIPELINE.md`
9. `09_CONTENT_PIPELINE.md`
10. `10_ANALYTICS_EXPERIMENTATION.md`
11. `11_MONETIZATION.md`
12. `12_MARKETING_ASO_UA.md`
13. `13_QA_RELEASE.md`
14. `14_STORE_LEGAL_PRIVACY.md`
15. `15_ROADMAP_BACKLOG.md`
16. `16_FIRST_PROMPT.md`
17. `17_AGENT_PROMPTS.md`
18. `18_RISK_REGISTER.md`
19. `19_DECISION_LOG_TEMPLATE.md`
20. `20_SOURCES.md`

## The most important rule

**Do not start by producing dozens of final sprites.**

First prove:
1. enemies die satisfyingly,
2. upgrades change the feel of play,
3. the player understands the game in the first minute,
4. the first 5–10 minutes create a reason to continue.

Use placeholder art until the core loop is fun.

## MVP definition

The first playable MVP should target:

- portrait mobile layout,
- 1 playable hero,
- 3–5 weapons,
- 4–6 normal enemies,
- 1 boss,
- 1 dungeon biome,
- 10–20 run upgrades,
- 1 lightweight meta-progression layer,
- local save,
- basic analytics,
- no PvP,
- no guild,
- no server authority,
- no gacha,
- no live-service backend dependency.

## Project gates

### Gate A — Mechanical prototype
Pass when:
- auto-combat works reliably,
- player can enter a run, kill enemies, gain a reward, pick an upgrade,
- 5 minutes can be played without debug intervention.

### Gate B — Fun prototype
Pass when:
- at least 5–10 external testers understand the loop,
- testers voluntarily play multiple runs,
- at least some testers discuss builds/weapons rather than only visuals.

### Gate C — Vertical slice
Pass when:
- final-ish art exists for one biome,
- onboarding works,
- analytics events are live,
- performance is stable on a mid/low Android test device.

### Gate D — Soft launch
Pass when:
- crash-free sessions are acceptable,
- store page is ready,
- economy has no obvious dead zones,
- rewarded ad/IAP hooks can be enabled without redesigning progression.

### Gate E — Scale / stop
Scale only if:
- retention is competitive for the acquisition source,
- players reach intended progression milestones,
- CPI/organic response is not catastrophically bad,
- feedback indicates content demand rather than fundamental confusion.

## Working philosophy

- Prototype with ugly art.
- Balance with data.
- Build systems before content volume.
- Prefer composition over inheritance.
- Prefer data-driven content.
- Keep monetization optional and readable.
- Use AI to accelerate iteration, not to replace art direction.
- Never add backend complexity without a concrete product reason.
- Record major decisions in `19_DECISION_LOG_TEMPLATE.md`.
