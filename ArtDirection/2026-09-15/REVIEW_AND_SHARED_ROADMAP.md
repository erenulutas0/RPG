# Cryptforge — review and proposed shared roadmap

Date: 2026-09-15. Original review status: proposal for owner review, not an approved change to the GDD. No gameplay, art assets, scene, profile, commit or remote was changed during the original review.

Follow-up: the owner approved the combined art direction (A character prominence + B quiet floor + C choice hierarchy) after sprint-01. See docs/19 and ui-kit-01. Other proposed gameplay stages below remain proposals. The independent telemetry prompt is being executed in Ultracode; do not start a duplicate task.

## Owner direction

Art direction and visual production: Astra. Engineering and integration: Opus/Fable, with one writer per overlapping file. Portrait Android, a player-controlled moving hero, automatic weapons, active and passive skills, temporary loot builds, maps/floors, unlockable heroes and hero-specific mastery goals. A cursor or freely tapped damage circle is not the main interaction. Area damage remains appropriate as a character ability.

The supplied landscape image is a composition/density reference. The likely meaning of "BONK" is Megabonk; this identification is an assumption to confirm, not a settled product requirement. Keep one controlled hero per run initially; an unlockable roster does not require a simultaneous party.

## Evidence and verification

Read handoff 25 first, then product vision 01, GDD 03, architecture 06, implementation history 21, verification guide 22 and device history 23. Also inspected art direction 08, backlog 15, recent decisions 19, analytics 10 and relevant runtime/view code.

Local HEAD: 3690b2c, branch main, origin https://github.com/erenulutas0/RPG. Existing untracked DESIGN_REVIEW_2026-09-15.md was preserved.

Fresh checks in this review:
- .NET CombatChecks: 198 passed, 0 failed, 0 skipped.
- UnityCompileCheck: build succeeded, 0 warnings/errors, against local Editor assemblies.
- Verify-Project: passed, 240 unique project GUIDs, 1,099 package GUIDs, 403 scene objects/components.
- ADB: authorized Samsung SM-S911B at RFCW20W2WFX detected. No new phone gameplay session or capture in this review.
- Unity executable and batch runner present; no Unity process or project lock detected at the check. Current licensing/batch execution not exercised.
- Git available and remote configured. Push credentials/permission not verified; no push attempted.

Unity EditMode 210 and PlayMode 54 are the previous assistant's recorded results, not newly rerun results. Compilation and pure tests do not prove visual quality or device performance.

## Assessment

The project is a sound mechanical foundation: pure combat/progression rules, runtime snapshots, save migrations and recovery tests, explicit encounter composition, simulation/scene parity, device evidence and a frame-time probe. Extend this foundation.

It has not yet demonstrated the full build-discovery promise:
- UpgradeService offers eligible entries in fixed pool order and applies only a WeaponStat modifier. UpgradeOption/WeaponStat currently support Damage and AttackSpeed. Adding asset files alone cannot deliver chain attacks, status interactions or skill transformations.
- Three weapon behaviors exist, but there are only two level-up choices with five stacks each. Counterweight and Second Wind already provide useful behavioral examples.
- ChestRule returns Heal or Gold, not random build loot; its room index is zero-based, so the first visible room heals.
- Floor progression is implemented, but both floors use the same environment appearance. New physical map layouts are not implemented.
- Every character has one facing; hero walking has no actual walk cycle.
- Seven enemies per wave is the supported ceiling. Hundreds of enemies in the reference are a different performance and readability target.
- Stationary simulation plus automatic burst is a balance baseline. Walking, chest collection, evasive play and simultaneous steering/casting need separate tests and player observation.

Documentation reconciliation for a future approved slice:
- The "touch-aimed ability still pending" wording in 22/25 conflicts with the accepted hero-centred burst in 19 and the owner's current direction. Do not implement it merely to tick a stale item.
- The GDD has overlapping first-boss targets (4–6 vs 5–8 minutes). Choose one measurable target and define active combat time separately from choices and travel.
- Earlier historical sections in 21 are superseded by later slice entries; never use old weapon numbers as current tuning.

## Research and takeaways

Accessed 2026-09-15. Store feature descriptions verify mechanics, not the causes of commercial success. Review counts are not sales or mobile retention forecasts.

1. Megabonk: random upgrade offers, loot, unlockable characters with unique abilities, item synergies and quests. The fetched Steam listing reports 93% positive among 58,035 English reviews. Takeaway (design inference): movement should lead to worthwhile discoveries and upgrades should create different runs; do not copy its volume of content or PC controls.
   https://store.steampowered.com/app/3405340/Megabonk/?l=english
2. Brotato: automatic weapons, differentiated characters, 20–90 second waves and a shop between waves. Takeaway: short combat beats and a focused decision space can support build variety in a bounded arena.
   https://store.steampowered.com/app/1942280/Brotato/?l=english
3. Bones and Coins: party-based incremental combat, weapon attack styles, room progression and loot. Takeaway: use the floating platform and readable loot spectacle as references, while keeping our movement, danger, foundry identity and portrait controls distinct.
   https://store.steampowered.com/app/4134650/Bones_and_Coins/
4. Capybara Go: Google Play lists 10M+ downloads and a roguelike RPG built around randomized adventure/choices. This supports interest in adjacent mobile progression games, not a forecast for Cryptforge.
   https://play.google.com/store/apps/details?id=com.habby.capybara

Proposed player promise: "Move through astral forge islands, discover weapon-changing loot, assemble a powerful build, defeat the guardian, and unlock a new way to play."

Three motivations should connect: moment-to-moment movement/impact; a temporary build across a run; a visible mastery/unlock goal across runs.

## Art direction and framing

Keep the accepted astral foundry (08/19), not the rejected furnace cavern. Dark charcoal stone and restrained brass; violet space below combat contrast; ember enemies, blue-steel hero and silver constructs. No skulls, skeletal glyphs or crypt identity.

The old full-island mockup is a palette/mood reference, not an exact moving-camera gameplay layout. Current platform width is 18 units; the camera sees 4.6, about 26% of that width. Requiring a complete island silhouette and this close camera simultaneously is incompatible. Compare a combat view at 6 and 7.5 units as exploratory candidates; retain a separate arrival/transition overview if it helps orientation. Neither candidate is approved tuning.

Boss clipping has a concrete geometric cause: 1.6 units from the hero plus half a 49-texel body at 32 PPU is about 2.37 units, already greater than the current 2.3-unit half-screen width, before a margin or camera lag. Fit body bounds, not only feet. Keep rules/reach independent of visual fixes.

First deliverable after approval: one revised gameplay composition with two camera-scale variants, plus a choice-panel state. Show actual supported combat density, readable travel space, one visible chest objective, compact top progress, a lower HP/XP group and one active button. Reconsider the persistent bright radius ring; it should not be the dominant visual. Any timing/visibility change is a later integration decision.

Respect the current asset contract for the first imported sample: 32 PPU, hero 32x44, enemy sizes from 25, bottom-centre pivots, separate weapons, point filtering and defined poses. UI uses its own screen-space sizing. If these sizes cannot meet the visual target, compare a bounded alternative and agree a contract revision before producing a roster.

Concept approval is not asset completion. Each approved asset needs a clean grid/palette, transparent edges, stable pivot, consistent frame alignment, weapon anchors, hit silhouette and in-engine/device review. AI-assisted concepts do not guarantee animation consistency; difficult frame cleanup may require dedicated pixel-art work.

## Work ownership and order

| Stage | Astra art output | Opus engineering | Fable integration | Exit condition |
|---|---|---|---|---|
| 1 | Gameplay composition and camera comparison; art contract sheet | Local telemetry slice, then report existing pacing | Wait for approved layout; inspect importer requirements | Combat hierarchy and ownership agreed |
| 2 | HUD parts, cards, icons, one hero test asset | One seeded offer/behavior slice with shared simulation rules | Optional LookDefinition adapter with procedural fallback | One real asset works without combat drift |
| 3 | Hero directions/walk/attack, one enemy, core VFX and sound brief | Chest build reward and one readable threat, separately | Approved HUD/camera/floor integration; animation driver | A short playable segment has movement, discovery and a visible combo |
| 4 | First biome/enemy family/boss, second biome palette, first six SFX | Pacing, one hero mastery chain and opposite-hero preview | Performance cache/lifetime follow-up | Testers understand choices and voluntarily restart |

One integration writer owns Gameplay.unity and each UI/view file at a time. Do not have Opus and Fable modify CombatSetup or shared progression classes concurrently. Cache ownership belongs with the asset adapter; imported assets must not be destroyed by per-instance fallback cleanup. Keep runtime state and animation clocks per instance even when sprites are shared.

### First UI integration slice (handoff item A)

After the layout is approved, likely files: RunHud.cs, Gameplay.unity HUD/Safe Area, PrototypeText.asset, PlatformArt.cs (local floor tones), FollowFraming.cs, ArenaCameraFollow.cs. Preserve current character drawings in this slice. Avoid a global palette edit that unexpectedly changes enemies and effects.

Relevant tests: PlatformArtTests, FollowFramingTests, ArenaViewTests and existing flow text assertions; preserve EveryButtonSitsOnACanvasThatReceivesTouches and EveryButtonLabelSitsInsideItsButton. Add body-bound framing assertions at centre/rim, short/tall portrait and safe-area variants. Update visual fixture expectations deliberately, never weaken assertions simply to pass.

Verification: .NET tests -> Unity compile check -> full Unity EditMode/PlayMode -> APK only on passing suites -> static integrity, following 25. Phone checks at centre/rim, boss on either side, chest visibility, actual drag and ability use, pause and choice overlay. Focus/awake/keyguard gating before every touch/capture, after captures too. Record fresh counts/APK hash in 21, manual steps/counts in 22, phone evidence in 23, approved decisions in 19 and roadmap completion in 15. No fabricated device pass if unavailable.

## Proposed first playable quality target

Validate one hero, three existing weapons, about 10–12 well-chosen upgrades (including several behavioral changes), two visibly different build paths, one meaningful chest reward and one boss attack the player can read and evade. These are proposals, not current content.

Examples to assess mechanically before drawing final VFX: every third strike produces a secondary pulse; a temporary barrier strengthens the next strike after it absorbs damage; a marked enemy spreads an effect on death. Avoid unbounded on-kill recursion and mandatory damage-taking achievements.

Mastery should teach play: finish a floor with the hero, complete a weapon-specific challenge, then achieve a distinct build objective. Rewards can unlock a sidegrade, an alternate ability or a cosmetic. Do not begin with a large kill-count grind or several heroes differing only in base stats.

Keep banked gold for permanent unlocks initially and make chest buffs temporary. If run gold becomes spendable mid-run, specify available/secured/spent amounts and settlement first; current bank-at-checkpoint logic must not pay or spend the same gold twice.

Do not reach a minutes target by multiplying HP alone. Measure combat, walking, choices and idle gaps separately. Add meaningful threats, discoveries and build decisions. Local telemetry supports device experiments, not population retention estimates by itself.
