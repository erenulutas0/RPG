# Foundry combat rooms — target and production plan

## Owner direction, 2026-09-16

The owner reaffirmed the furnace-cavern reference as the visual target, suggested that contained platform as a boss arena, and requested denser normal-room enemies with room for player-controlled kiting. This updates the earlier cosmic-only preference for this proposed two-room slice. It does not authorize describing the current APK as having these assets or mechanics. The world remains an original arcane foundry, without skulls or skeletal imagery.

## Deliverables in this folder

- owner-reference.png: preserved original input.
- normal-and-boss-v1.png: generated comparison, not runtime art. Left: extended floor and a trailing pack with open left/lower escape space. Right: contained diamond and a dominant golem with two adds.
- PROMPT.md: exact built-in generation prompt and provenance.
- OPUS_KITING_BRIEF.md: bounded technical handoff; not dispatched automatically.

## Art review of v1

The two room types read differently while sharing stone, brass, blue steel and ember materials. The hero remains identifiable and both layouts retain open floor. The generated normal room has roughly sixteen visible foes rather than the exact prompt count; treat the count as illustrative, not encounter data. The board contains generic buff glyphs and an upper bar in both views: these are visual placeholders, not implemented content or approval of an always-present boss bar. Runtime HUD must show real acquired effects only.

The lava backdrop is still too bright for a busy fight. Reduce contrast/saturation outside the walkable area in production; preserve bright local attack/hero cues. The boss wedge is a proposed attack telegraph, not an existing boss ability. The normal-room hero pose suggests movement but proves neither animation nor playable kiting. The square board does not validate phone text size, safe areas, or camera scale.

## Production order and ownership

1. **Framing and movement proof (Astra visual review, Opus technical implementation):** keep current actors as placeholders, compare camera widths 6/7.5/9 in the same room with the same actor scale. Normal room may extend beyond the viewport; the boss room should expose its usable escape space. A full island is a boss composition goal, not a reason to shrink every actor in normal combat. Select on S23 and short-screen layout after movement testing.
2. **Environment art (Astra):** create separate dim cavern backdrop, quiet walkable stone surface, brass rim/sidewall pieces, and corner furnace props. Keep tall decoration outside movement space. Establish contact shadows and consistent isometric projection before adding detail. Do not bake actors, bars, telegraphs or damage numbers into the floor or background.
3. **One hero and one enemy production proof (Astra art, technical owner integration):** blue-steel hero plus ember mite; idle, movement/facings, attack and hit response with matching scale/pivot. Existing 32-texel/world-unit contract remains until a tested replacement is explicitly selected. The smooth painted reference is not evidence that generated images can be directly used as consistent animation frames.
4. **One normal room:** introduce density gradually (candidate live counts 7 -> 10 -> 14 -> 18, proposals only), keep at least one practical escape route, validate hero visibility and frame time. Reuse one enemy family first. Avoid simultaneous four-sided closures at the hero's feet.
5. **One boss room:** large quicksilver golem, limited adds, one readable directional attack with windup and recovery. Telegraphs must match damage geometry. Implement and test this as its own subsequent gameplay slice.

## Acceptance targets, not completed checks

- Hero and immediate threats remain distinguishable at actual phone size, including near the rim and behind large enemies.
- Dragging while targets are in weapon range does not interrupt automatic attacks. Sword must have viable strike-and-retreat opportunities; sustained damage while permanently out of range is not promised.
- In a controlled movement route the player can create space without an unavoidable spawn collision. Faster enemies add pressure without making every route a guaranteed trap.
- Dense combat is measured separately from paused menus: frame time, spawn spikes, allocation and memory over repeated room/restart cycles. Current S23 performance with seven enemies does not prove eighteen-enemy performance.
- Short portrait HUD and ability touch area leave usable movement space. Boss tells are distinguishable from friendly blue effects and warm scenery.
- Gold, upgrades, saves and telemetry stay comparable to the preceding build; increased population requires economy/pacing review rather than multiplying rewards accidentally.

## Current implementation gap

Update after the owner's portrait decision: `../camera-review/README.md` records the 6/7.5/9 comparison and width-9 camera slice. The paragraph below describes the pre-comparison baseline; pack capacity and the remaining art/behavior gaps still apply.

PackLayout.MaxPackSize is 7; PackMotion stores fixed-capacity arrays using that limit. ArenaCameraFollow uses the scene's current width of 6 and follows the hero. HeroMotion and enemy tracking already exist, but their existence alone does not prove satisfying kiting. There is no new runtime change in this folder. No Unity build, phone install or performance claim belongs to this concept-only slice.

Next review artifact: an in-engine camera/movement comparison using the present actors, then a separately layered environment kit. Pause-menu polish is deferred behind this combat-scene priority, not forgotten.
