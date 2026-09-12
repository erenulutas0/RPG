# Research Round 2 — Reference Game Post-Launch, Mobile Comparables, and the Multi-Floor Dungeon Proposal

Date: 2026-09-12. Compiled after the first combat slice was verified on device. This round answers three questions: what *Bones and Coins* actually became after launch, which mobile titles already solve the problems we will face, and what a "multi-level dungeon" should look like for Cryptforge. Facts are cited in the Sources section; anything marked **Proposal** is a design recommendation, not a verified fact.

`01_PRODUCT_VISION.md`, `03_GAME_DESIGN_DOCUMENT.md` and `06_TECH_ARCHITECTURE_UNITY.md` remain the primary constraints. This document proposes amendments; it does not override them until the decisions in `19_DECISION_LOG_TEMPLATE.md` are accepted.

---

## 1. Reference game: Bones and Coins, post-launch snapshot

### 1.1 Verified facts (Steam, 2026-09-12)

| Item | Value |
|---|---|
| Developer / publishers | Troyd Games (solo developer) / Rogue Duck Interactive, Gamersky Games |
| Release | 2026-09-07, PC (Steam) only; no mobile or console version announced |
| Price | $4.99, launch discount to $3.74 until 2026-09-21 |
| Reviews | "Mostly Positive", ~70% of ~520 reviews |
| Content | 8 dungeon rooms (starting room + 7 named), 16 weapons, 6 heroes, 9 enemies with exclusive drops, ~160–180 skill-tree nodes, 50 achievements |
| Optimized playthrough | 4–5 hours to the "Final Gate" ending; 100% takes longer |

### 1.2 How its systems actually work

- **Rooms are progression containers, not levels.** Each room has its own XP bar and local bonuses that do not transfer. Players "train" a room until enemy density and coin income are high, then unlock the next one through the skill tree. Reviewers praised tying bonuses, enemy caps and loot tables to the room because it forces party rebuilds per destination. Guides warn that skipping room XP creates a wall "that looks like a hero problem and is actually a density problem."
- **Combat is nearly passive.** The player hovers a torch over skeleton packs while heroes auto-attack. Enemies do not fight back and there is no death or failure state. Abilities can auto-cast.
- **Skill tree is a web**, routed "print coins, then print bodies, then print damage": income nodes → party slots → combat nodes. No respec.
- **Heroes**: Barbarian, Ranger, Shaman, Pyromancer, Paladin, Crusader. Five blessing slots per hero define roles. Community consensus is that only the Shaman (marks that amplify the next hit) changes outcomes.
- **Expeditions** send off-party heroes to generate passive income in a second currency. Reviewers say the rewards feel too small.
- **Economy**: copper, silver, bones; blacksmith forging with recipe materials; a max-skeleton cap tied to room size; game-speed control 0.75×–1.5×.

### 1.3 What players praise and what they reject

Praise (repeated across reviews): old-school Diablo-style art, satisfying sounds, steady upgrade cadence, fair price for the length.

Rejection (repeated, highly upvoted):

| Complaint | Mechanic behind it |
|---|---|
| "Enemies stand passively" / "no combat" | no enemy attacks, no failure state |
| "Cursor-dragging clicker" | the torch hover is the only interaction |
| "Only Shaman matters" | numeric weapon tiers, few behavioral effects |
| "No strategy, only percentages" | upgrades are stat lines without interactions |
| "Cannot respec" | permanent tree, bad routes force restarts |
| "Done in 4–5 hours, nothing after" | no prestige, no modes, no leaderboard |
| "~10% of time in menus", forced 2-second waits | no auto-repeat, transition friction |

What players explicitly asked for: a prestige/ascension loop, real hero synergies, risk (score targets, leaderboards, consequences), auto-repeat, skill reset, more dungeons/modes.

### 1.4 Consequence for Cryptforge

The reference validates the *appeal* (party + weapons + dungeon rooms + incremental growth) and simultaneously documents the *ceiling*. Every top complaint maps to a design decision we already lean toward: real auto-combat with retaliation, failure that ends a run, behavioral upgrades, temporary run builds (which makes respec unnecessary), and a meta loop. The market gap is "the same fantasy with real combat, real risk and a reason to run again" — on a platform the reference does not serve.

Two originality warnings follow directly from this comparison:

1. **Visual identity.** `08_ART_AI_PIPELINE.md` option A ("dark-cute crypt: skulls, moss, candles") and the "bones/crypts" direction in `01_PRODUCT_VISION.md` sit on top of the reference's skeleton-catacomb identity. **Proposal:** test options B (arcane guild) and C (cursed treasure dungeon) first, and keep skeletons as one enemy family among several rather than the whole roster.
2. **Naming.** "Cryptforge" is fine as a codename. The shipped title should avoid the bones/coins/crypt/skeleton word cluster.

---

## 2. Mobile comparables and what each one teaches

| Game | Structure | Lesson for us |
|---|---|---|
| **Archero / Archero 2** | Chapter = intro → 4 enemy stages → angel (heal *or* new ability) → 4 stages → boss; each stage under a minute; three random choices per level-up | The stage → relief → boss cadence is the proven mobile rhythm. Energy systems are the main complaint; we do not need one. |
| **Survivor.io** | Continuous waves, XP orbs, level-ups mid-run; still ~$5M/month three years after launch | Frequent level-up choices carry a session; live-ops keeps revenue. Wave mode is an alternative to rooms, but `03` already prefers rooms for pacing and reward screens. |
| **Shiba Story Go** (2026, F2P) | Auto-battle + roguelite floors; 5–20 minute runs; loadout resets per run; several team setups viable at the same tier | Closest to our target: portrait-friendly, auto-battle, floor-based, praised for build variety without a power hierarchy. |
| **Capybara Go** | AFK dungeon runs, class/skill picks per run | Criticized because "skill options do not interact"; run variance stays low. Interactions, not option count, create replay. |
| **Legend of Mushroom** | 11 progression verticals unlocked over 14 days; RNG gear lamps | Deconstructed as an "RPG-themed slot machine"; hits a wall at chapters 15–20 when duplicate-gated evolutions appear. Do not stack verticals or bait-and-switch generosity. |
| **Shattered Pixel Dungeon** | Solo-developer floor-based roguelike, free, no ads | Depth and clarity earn word of mouth without marketing. Turn-based; not our loop. |
| **Adventure to Fate: Dungeons** | Procedural floors, class system, "mechanical clarity" | Small teams win on clarity. |
| **Dunidle / Idle Pixel Battle** | Indie idle dungeon crawlers with endless floors / biomes, 3-hero parties | Our direct indie competitors on Android; differentiation must come from synergy and readable combat. |

Structural takeaway: every praised mobile roguelite offers a **choice every 30–60 seconds** and a **checkpoint every 3–5 minutes**. Every criticized one either removes agency (Bones and Coins, Capybara Go) or gates progress behind duplicates/energy (Legend of Mushroom, Archero).

---

## 3. 2026 market and retention context

- Sensor Tower's State of Mobile 2026: mobile game IAP revenue ~$82B (+1.3% YoY) while downloads decline; the market is "shifting from new-user volume to lifetime value." Teams are advised to prioritize retention and reactivation and to combine high-attention ad formats with IAP. Europe is mixed (UK up, France flat, Germany softer).
- Retention benchmarks differ by source and sample. GameAnalytics 2026 (already in `02`): median D1 ~22%, D7 <4%. Playio's 2026 synthesis: median D1 ~26%, D7 ~10%, D30 ~3–4%; "good" 30–35/15/5; top quartile 40+/20+/10+. Some aggregators claim mid-core RPG holds D7 ~20% and D30 ~11%; treat those as optimistic. Keep `02`'s rule: benchmarks are context, not pass/fail.
- Diagnostic framing worth adopting: low D1 = first-session problem; low D7 = habit/loop problem; low D30 = content/live-ops problem.
- Unity 6 guidance now positions URP as the default mobile renderer and highlights the GPU Resident Drawer and Render Graph. The prototype's built-in renderer is still correct for solid-color sprites. **Proposal:** decide built-in vs URP at the Gate A → Gate B transition, before 2D lights, pixel-perfect and VFX accumulate; switching later gets more expensive.

---

## 4. Proposal — the multi-floor dungeon ("Descent") structure

Goal: turn the GDD's "5 rooms/checkpoints" run into a repeatable, deepening dungeon without adding a second progression vertical.

### 4.1 Hierarchy

```text
Run
 └─ Floor 1 … N          biome + 1 floor modifier + scaling tier
     ├─ Room 1–3          normal encounters (1–3 waves)
     ├─ Room 4            forge room: heal OR relic OR reroll (Archero "angel" analog)
     ├─ Room 5            elite encounter
     └─ Room 6            floor boss → checkpoint
```

Each floor reuses the room template with a different biome palette, enemy pool weights, one **floor modifier** (e.g. "Brittle: enemies −20% HP, packs +50% size", "Cursed gold: +40% coins, enemies +25% damage") and the geometric scaling from `05_ECONOMY_BALANCING.md` (`HP_floor = base_hp × growth^floor`). Modifiers are data, drawn from a small pool per biome.

### 4.2 The checkpoint decision (risk the reference lacks)

At every floor boss checkpoint the player chooses **Extract** (bank all run rewards, end the run with a result screen) or **Descend** (continue; rewards earned since the last checkpoint are at risk on death — a fraction, not everything, so failure still "grants some progress" as `03` requires). This single choice supplies the risk, score and "one more floor" tension that reference players asked for, without leaderboards or a backend.

### 4.3 Pacing targets (portrait, early prototype)

| Beat | Target |
|---|---|
| Room | 20–40 s |
| Upgrade choice | every 30–60 s (level-ups), plus the forge room |
| Floor | 3–4 min |
| First boss | 4–6 min (inside the GDD's 5–8 min window) |
| Full early run | 8–15 min, 2–3 floors |

### 4.4 Failure and result

Hero HP 0 → run ends → result screen shows floor and room reached, banked vs lost rewards, the killer ("Tank, Floor 2, Room 5") and the build; one-tap restart. This implements "clearly show why" and "restart in one tap" from `03` and `04`.

### 4.5 Meta layer — "The Forge"

One permanent layer only: gold buys hero/weapon unlocks, small account upgrades and **relics** (run-start modifiers that change behavior, not raw stats). Depth record and codex entries are the visible long-term goals. Prestige ("Reforge") is deliberately deferred until testers hit a wall; the reference shows that shipping without any loop is punished, but Legend of Mushroom shows that stacking loops is punished too.

### 4.6 Data and code additions (extends `06`, no new manager)

| Addition | Purpose |
|---|---|
| `FloorDefinition` | biome, room template, modifier pool, boss reference, scaling tier |
| `RoomDefinition` / `EncounterDefinition` | enemy pools, spawn weights, wave count, elite flag, reward |
| `BossDefinition` | one readable mechanic (shield phase, summons, enrage) |
| `FloorModifierDefinition` | id, display text, stat/behavior effects |
| `RelicDefinition` | run-start behavior modifier for the meta layer |
| `RunState` (extend) | floor index, room index, banked vs at-risk rewards, depth record |
| `RunFlow` | small state machine: Combat → Reward → Choice → Transition → Boss → Result |
| `EncounterController` | already planned; spawns from the current `EncounterDefinition` |
| Events | `RoomCompleted`, `FloorCompleted`, `BossStarted`, `RunEnded`, `ExtractChosen` |

Analytics additions to `10`: `floor_index` and `room_index` on `room_start` / `room_complete`; new `floor_complete`, `extract_choice` (extract/descend, floor), `run_end.cause`.

### 4.7 Portrait layout

Top strip: floor · room progress and boss proximity. Middle: arena. Bottom third: large touch cards for choices and the Extract/Descend prompt. Never show more than one currency during combat.

### 4.8 Slicing into the existing roadmap

The Day 3 XP/upgrade slice and the Day 4 "rooms/waves, 3 enemy types, restart/end-run" slice in `15_ROADMAP_BACKLOG.md` are unchanged. After them:

1. Floor 1 as a `FloorDefinition` with six rooms and one boss; result screen and restart.
2. Floor 2 with a different modifier and scaling; the Extract/Descend prompt.
3. The Forge meta screen with unlocks and the first two relics; local save.

Then Gate A is measurable: enter a run, kill, reward, choose, reach a boss, fail or extract, restart — five minutes without debug intervention.

---

## 5. Feature candidates for the future, ranked

Evidence column names the source of the lesson. "P" follows the backlog categories in `15`.

| P | Candidate | Why | Evidence |
|---|---|---|---|
| P0 | Enemy retaliation and hero death | The reference's top complaint is passive enemies | Steam reviews |
| P0 | Result screen with cause of death and one-tap restart | "Clearly show why", "one more run" | `03`, `04` |
| P0 | Floor/room descent with boss checkpoints | Proven mobile cadence | Archero analysis |
| P0 | Game-speed toggle (1× / 1.5× / 2×) | Reference players value it; Unity `Time.timeScale` | Bones and Coins reviews |
| P0 | Zero-delay transitions and auto-continue | "~10% of time in menus" | Steam reviews |
| P1 | Behavioral upgrades with tag interactions (30–40% numeric, 40–50% behavioral, 10–20% synergy) | Option *interactions* create replay | Capybara Go critique, `03` |
| P1 | Run upgrades are temporary → no respec needed; meta upgrades small and refundable | "Cannot respec" | Steam reviews |
| P1 | Extract/Descend risk decision | Players asked for risk and score | Steam reviews |
| P1 | Relics and codex in the Forge | Visible long-term goals | `04`, reference "no endgame" |
| P1 | Floating damage numbers, kill feed, boss phase banner | "Readable chaos" | `01` |
| P2 | Idle "expedition" of unused heroes as a return hook | Reference has it but rewards felt too small; make them matter | Wiki, reviews |
| P2 | Daily modifier seed ("today's crypt") | Cheap live-ops variety | Survivor.io longevity |
| P2 | Optional rewarded ads: extract multiplier, one revive | Hybrid monetization is the 2026 norm | Sensor Tower, `11` |
| P3 | Deepest-floor leaderboard | Needs backend; players want it | `06` backend triggers |
| Avoid | Energy systems, duplicate-gated evolutions, 10+ progression verticals, paid random loot | Documented churn causes | Archero, Legend of Mushroom, `14` |

---

## 6. Proposed amendments to existing documents

- `01`: add the originality warning about the crypt/skeleton identity; prefer directions B or C for the first style board.
- `03`: replace "5 rooms/checkpoints" with the floor hierarchy in §4.1; add the Extract/Descend checkpoint to "Failure".
- `05`: add floor modifiers and the at-risk reward fraction to the spreadsheet fields.
- `10`: add the analytics fields in §4.6.
- `15`: insert the three post-Day-4 slices from §4.8 before "3 weapon behaviors".
- `19`: record the run-structure decision (entry added as *Proposed*).

None of these edits are applied here; they wait for the decision entry to be accepted.

---

## Sources

Accessed 2026-09-12.

- Bones and Coins — Steam store page: https://store.steampowered.com/app/4134650/Bones_and_Coins/
- Bones and Coins — Steam user reviews (JSON API sample of English reviews): https://store.steampowered.com/appreviews/4134650?json=1&filter=all&language=english
- Bones and Coins Wiki — overview, dungeon rooms, skill tree, expeditions: https://bones-and-coins.wiki/ , https://bones-and-coins.wiki/guides/dungeon-rooms/ , https://bones-and-coins.wiki/guides/skill-tree/ , https://bones-and-coins.wiki/guides/expeditions/
- Bones and Coins guide site (player pain points, progression route): https://bonesandcoins.site/
- 9Puz — 100% guide, ending condition: https://9puz.com/5555-bones-and-coins-100-percent-guide/
- Wanderer review (5.5/10): https://playwanderer.online/game-reviews/bones-and-coins
- InsertCoins review (7/10): https://insertcoins.press/en/articles/bones-and-coins-test
- Game Developer — Finding the Fun: Archero, Part 1: https://www.gamedeveloper.com/design/finding-the-fun-archero-part-1---gameplay
- Mobile Game Report — best mobile roguelites 2026: https://www.mobilegamereport.com/articles/best-mobile-roguelites-2026
- Mobile Game Report — idle RPGs worth starting 2026: https://www.mobilegamereport.com/articles/best-idle-rpg-mobile-2026
- Mobile Game Report — underrated mobile roguelites: https://www.mobilegamereport.com/articles/mobile-roguelite-underrated-2026
- Mobile Game Report — Capybara Go build ceiling: https://www.mobilegamereport.com/articles/capybara-go-depth-vs-casual-2026
- Mobile Game Report — Legend of Mushroom progression wall: https://www.mobilegamereport.com/articles/legend-of-mushroom-progression-wall-2026
- Deconstructor of Fun — The Magic of Legend of Mushroom: https://www.deconstructoroffun.com/blog/2024/4/15/the-magic-of-legend-of-mushroom
- Shiba Story Go — store listing: https://play.google.com/store/apps/details?id=com.proofofplay.shibastorygo
- Gamesforum — Survivor.io revenue three years on: https://www.globalgamesforum.com/news/how-survivor.io-continues-to-pull-in-5-million-a-month-three-years-later
- Sensor Tower — State of Mobile 2026: https://sensortower.com/blog/state-of-mobile-2026
- Playio — D1/D7/D30 benchmarks 2026: https://blog.playio.co/d1-d7-d30-retention-benchmarks-2026
- Unity — Unity 6 optimization guides: https://unity.com/blog/unity-6-game-optimization-guides
