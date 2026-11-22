# 43. Backlog - gameplay animation, story and lore, character achievements (owner, 2026-10-26)

Three items the owner raised on 2026-10-26, recorded so they are not lost while V0.1 (docs/41, docs/42) is built.
None is scheduled yet; each waits for the owner's ChatGPT research round (lore, achievements) or a slot after the
weapons-and-tomes UI (animation).

## 1. Animation during play

The owner wants the play itself animated. What exists: the Vanguard's swing (VanguardAnimator on `Attacked`), strike
effects per weapon (CombatEffectsView: sword slash, staff ring, double slash, crit star), enemy looks (EnemyLookView),
foundry ambience (flames, banners, gate doors, trolley), the pit's band and grips, the Warden's mark. Gaps seen today:
- hero: idle/run/hit/death states, a reaction to a level-up and to a weapon picked up; three weapons firing need their
  own small motions (only slot 0 swings the body, docs/42 risks);
- enemies: walk cycles, hit flinch, death (bodies today simply stop and vanish after `HordeRule.RetireSeconds`), the
  horde's spawn-in at the edge of the view;
- boss: the Warden's windup before a slam (the mark exists, the body does not tell), its phase-two change, a death;
- feedback: level-up burst, rarity colour on taking a card, evolution moment, chest opening (partly done), XP vacuum;
- performance budget: 30-40 enemies on the S23 (decision 10) - animation must stay within the frame time the density
  probe measures; sprite-sheet animation over per-bone rigs.

### Built 2026-11-22: the game-feel pass

**Checked first.** Much of the list above was built after 2026-10-26: the hero's idle, walk, strike, cast, flinch and
collapse (the Vanguard's cut-out puppet, PuppetMotion; Ilya's rendered idle, walk, hit and death frames; the painted
frames' bob, lean and breath), the painted enemies' walk cycles, strike windups and own deaths (art round 3), a hit
compression beside CombatantView's silhouette flash, the horde's arrival fading in over an ember puff, the Warden's raised
hammer at a walk through the telegraph with its hotter second-phase frames and its fall, the level-up's gold ring and
sparks, the card's rarity ring, the evolution's flash and the picked weapon's embers (RunFeedbackView). The pass filled
what was left with procedural motion and light on the existing art - transform tweens, squash, silhouette glows, pooled
motes and LineRenderer rings. **No new sprite frame**: the one new drawing is a code-drawn soft disc (FeelArt), every
glow, mote, ember and shard of the pass. **Visual only**: no timing, damage, hitbox or AI moved; the default batch
(`FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_REALISM=typical FOUNDRY_FLOORS=2`) is byte-identical to
main's.

1. **Weapon slots 1 and 2** (HeroFeelView, on AttackController.StruckWith). The body still swings with slot 0 alone; each
   weapon in a later slot cues on the hero at the moment it strikes - the Sword an off-hand flourish (a ghost of its blade
   sweeping from behind the off shoulder over the top toward the struck enemy, a steel arc trailing it), the Staff a raised
   glow pulse (a ghost staff lifted at the off side, a glow swelling at its tip, a thin ring spreading from it), the
   Daggers a quick double flick at the waist. The cue is GameFeel.CueFor the weapon's strike behaviour (the hero's three
   weapons are the whole list) and lasts 0.18-0.28 s, never past 0.8 of its interval. Ghosts draw behind the whole figure
   in the hero's sorting group, trails and glows thin and translucent over it: a cue never hides the hero or an enemy.
2. **The hero** (HeroFeelView, over what its look plays). A blow squashes the figure away from it and shoves it a few
   texels back for 0.18 s - on the puppet's rig roots and its root's place, on Ilya's drawing
   (RenderedCharacterAnimator.Squash) or on the painted frames' weight transform; never the hero, its shadow or its
   weapons' loadouts. Death lets the collapse play while a thin stream of ash rises off the falling body, then at 0.8 s
   the body bursts into ash and embers along where it lies. A level stretches the figure up and settles it over a gold
   halo behind it. A weapon picked up rises over the head as a ghost of itself, turning once, as the run moves again.
   Idle breathing and the run's bob already existed for all three looks and are kept.
3. **Enemies.** A struck painted enemy tips up to 7 degrees away from the blow and rights itself in 0.12 s, beside the
   existing compression and flash (EnemyLookView; a float and a sign an enemy). A fallen enemy dissolves - ash motes
   lifting off its body and thinning to nothing over about 0.3 s over its remains, frost on the ice and violet in the
   garden - and a horde newcomer rises out of embers drifting up round its feet as it fades in (CombatEffectsView, 64
   mote slots of its own past the deaths'; the pool, the remains and HordeRule.RetireSeconds are untouched). The motes
   scale down as a crowd falls (GameFeel.MotesPerDeath over the last 0.3 s: 6, 3, then 1 past thirty at once).
4. **Bosses** (BossFeelView). From the frame a mark goes down to the frame its blow lands, the Warden's body rises,
   leans back away from the mark and glows ember - its own frame's silhouette, on the clock's own TelegraphProgress,
   beating with the mark's edge and trembling through the last quarter - then kicks forward as the blow lands; the glow
   never passes 0.5 through a telegraph, so the mark stays the clearest thing on screen. Phase two bursts in two thin
   rings and embers and keeps a slow, hotter glow (the tint lives in the overlay: the painted second phase keeps its own
   colours). The Warden's fall opens in embers and its remains dissolve in ash. The Glass Regent's lane cast reads on its
   body in glass violet (raised and leaning when it raises its mirror; the glow alone when it keeps its rod at its feet),
   and it shatters - shards flung out, turning and falling - its remains ending in glints. Its violet feet ring (docs/50)
   now reads on the floor: 0.6 alpha and 0.075 wide at rest (was 0.3 and 0.05, about 3 px on the S23) over a dark violet
   shade line, widening as it pulses.

**Performance.** Everything is made at load and pooled (about 150 objects: 37 under the hero, 64 effect slots, 45 under
the boss's root, the feet ring's shade), nothing is allocated per frame or per event in steady combat, and every mote,
glow and shard is the same disc on one texture. New per-frame work carries markers that the perf probe reads (in
PerformanceMarkers.PickNames): Cryptforge.Late.HeroFeel, Cryptforge.Effects.Tick (the effects' whole tick, the motes
among them) and Cryptforge.Late.BossFeel; PerfCensus counts the pass's renderers as "game feel". The density probe on the
S23 is still to be run on this build.

**Tests.** GameFeelTests (pure, also in CombatChecks); PlayMode SlotCueTests, HeroFeelTests, EnemyFeelTests and
BossFeelTests - each cue on its event, the rigs reused, nothing left across reloads, and the boss's tell beginning on the
mark's frame and following the clock exactly. The capture for review: `pwsh -File Tools/ArtReview/Run-GameFeelProof.ps1`
(frames into ArtDirection/2026-11-22/game-feel-01, untracked).

**Left - waits for new frames from the art tools** (the owner's AI-tools research):
- weapons in slots 1 and 2 drawn in the hero's hands: today a ghost of the weapon's sprite plays each cue; dual-wield or
  off-hand poses would let the body itself use three weapons;
- the hero: a hit and a death for the painted-frames look (only the puppet and Ilya collapse; it bursts into ash where
  it stood), a level-up and a pick-up pose, and Ilya's hit while she walks, strikes or casts (her hit frames skip then);
- enemies: drawn hit reactions (today a compression and a tip), a dissolve drawn per kind (today motes over the crumble)
  and a spawn-in drawn rising out of the floor rather than faded;
- bosses: a windup with drawn anticipation beyond the raised hammer and mirror (the rise and the lean are tweens), a
  phase-two transformation, the Regent's shatter as drawn glass and its phase-two glass copies (GlassRegentCopyArtSet,
  held), and an entrance as the boss walks out of the gate for the reveal;
- still open without new art: the chest opening (partly done), the XP vacuum, and the floor 2 and 3 enemies' own tells
  past their existing views (the Mirror Sentry's glint, the Cinder Bloom's bark).

## 2. Story and lore - a world players attach to

Goal (owner): a story and lore that bind players, invite fan art and grow a community. Constraints from the game as it
is: the run climbs upward floor by floor (the fiction flipped from "Descent" to an ascent, map-progression vision), floor 1
is the Foundry with the Forge Warden at its gate, later floors stack above it, the finale is a swarm you extract from;
existing names (The Foundry, Ember Hall, Cinder Walk, Slag Gate, The Forge, Captain's Post, Warden's Crucible, Quicksilver
Vaults, Vault Gate, Sentry Hall, Warden's Vault, The Final Swarm; Vanguard, Forge Warden, Grunt, Grunt Captain, Cinder
Mite, Runner, Tank; relics Second Wind, Counterweight; Forge Burst; tomes Force, Tempo, Reach, Precision, Vitality) and
the six planned characters of docs/40 section 4 (Vanguard, Cinder Arcanist, Riftblade, Bastion, Voltwright, Sanguine).
Delivery on a portrait phone has to be light: codex entries unlocked by play, item and card flavour text, short
boss lines, environmental storytelling in the maps. The owner runs a ChatGPT research round and brings the result back.

## 3. Character-based achievements

A list of achievements per character, tied to each one's mechanics (skill dependence 1-5, docs/40 section 4.2) and
doubling as the unlock and quest layer (docs/40 section 13: e.g. Cinder Arcanist unlocked by applying Burn). Needs:
counters in telemetry/profile (most events already exist: kills, boss_end, altar_taken, pit_eruption, horde_sample,
wave_clear, weapon_acquired once PR 8 lands), a profile section that persists them (ProfileSaveData - a save version
bump), and a screen. The owner drafts the list in the same ChatGPT round.
