# Utility Prompts for AI Agents

## Architecture review prompt

> Review the current Unity project against `06_TECH_ARCHITECTURE_UNITY.md` and `07_CODE_STANDARDS.md`. Identify only issues that are currently causing coupling, bugs, test difficulty, or near-term scaling risk. Do not propose enterprise abstractions. Rank fixes by impact and cost.

## Feature implementation prompt

> Implement [FEATURE]. First inspect the existing implementation. Preserve current architecture unless there is a concrete blocker. State acceptance criteria, modify only necessary files, add tests for logic-heavy parts, and provide manual Unity verification steps. Do not implement adjacent backlog features.

## Bug prompt

> Reproduce/trace this bug: [BUG]. Form hypotheses, inspect related state transitions and logs, identify the root cause, implement the smallest safe fix, and add a regression test if practical. Do not refactor unrelated code.

## Performance prompt

> Profile/inspect [SYSTEM] for allocations, repeated component lookup, excessive Instantiate/Destroy, overdraw, or update-loop waste. Do not optimize speculative issues; prioritize measurable hot paths.

## Economy prompt

> Using `05_ECONOMY_BALANCING.md`, evaluate the current progression curve. Show assumptions, compute time-to-upgrade and enemy TTK across early progression, identify dead zones/explosive scaling, then propose data-only balance changes before code changes.

## Playtest analysis prompt

> Here are playtest notes/analytics: [DATA]. Map each issue to the first-session funnel. Separate comprehension, pacing, difficulty, build depth, UX, and technical problems. Recommend the top three experiments, each with a primary metric and guardrail.

## Art direction prompt

> Using `08_ART_AI_PIPELINE.md`, create three original visual directions for the same hero/enemies without imitating an existing franchise. Prioritize small-screen silhouette readability and production feasibility. These are concept boards, not final sprite sheets.

## Sprite production prompt

> Convert the approved concept into a production specification for Aseprite: canvas size, PPU, palette constraints, anchor/pivot, idle/attack/hit/death frame counts, timing, naming, export/import checklist. Do not redesign the character.

## Marketing creative prompt

> Using actual game mechanics only, create 10 short-form ad concepts. Each must include a 0–3 second hook, visible gameplay moment, payoff, text overlay, and metric hypothesis. Avoid fake gameplay.

## Store listing prompt

> Draft Google Play and App Store metadata for this game using the documented positioning. Emphasize build discovery, fast dungeon runs, heroes/weapons and progression. Avoid claims unsupported by the current build.

## QA prompt

> Generate a regression checklist for [FEATURE] based on `13_QA_RELEASE.md`. Cover normal path, edge cases, interruption, save state, low-network behavior if relevant, and duplicate-event/reward risks.
